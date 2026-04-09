using EventForge.DTOs.Common;
using EventForge.DTOs.External.WhatsApp;
using EventForge.Server.Data;
using EventForge.Server.Data.Entities.Business;
using EventForge.Server.Data.Entities.Chat;
using EventForge.Server.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;

namespace EventForge.Server.Services.External.WhatsApp;

/// <summary>
/// Business logic for WhatsApp conversations: inbound messages, replies, blocking, association with BusinessParty.
/// </summary>
public class WhatsAppConversazioneService(
    EventForgeDbContext dbContext,
    IWhatsAppService whatsAppService,
    IHubContext<ChatHub> hubContext,
    ILogger<WhatsAppConversazioneService> logger) : IWhatsAppConversazioneService
{
    // Per-number semaphore to prevent duplicate conversation creation under concurrent load
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

    private static string NormalizzaNumero(string numero)
    {
        var digits = new string(numero.Where(char.IsDigit).ToArray());
        // If starts with 0, assume Italian mobile: replace leading 0 with country code 39
        if (digits.StartsWith('0')) digits = "39" + digits[1..];
        return digits;
    }

    public async Task GestisciMessaggioEntranteAsync(string numero, string testo, string msgId, DateTime timestamp, Guid tenantId, CancellationToken cancellationToken = default)
    {
        try
        {
            var normalizzato = NormalizzaNumero(numero);

            var bloccato = await dbContext.NumeriBloccati
                .AsNoTracking()
                .AnyAsync(n => n.TenantId == tenantId && n.NumeroDiTelefono == normalizzato && !n.IsDeleted, cancellationToken);
            if (bloccato)
            {
                logger.LogInformation("Ignoring message from blocked number {Number}", normalizzato);
                return;
            }

            var sem = _locks.GetOrAdd(normalizzato, _ => new SemaphoreSlim(1, 1));
            await sem.WaitAsync(cancellationToken);
            try
            {
                var conversazione = await GetOrCreateConversazioneAsync(normalizzato, tenantId, cancellationToken);

                var messaggio = new MessaggioWhatsApp
                {
                    TenantId = tenantId,
                    ConversazioneWhatsAppId = conversazione.Id,
                    Testo = testo,
                    Direzione = DirezioneMessaggio.Entrante,
                    StatoInvio = StatoInvioMessaggio.Consegnato,
                    WhatsAppMessageId = msgId,
                    Timestamp = timestamp,
                    IsLetto = false,
                    CreatedBy = "WhatsApp"
                };
                dbContext.MessaggiWhatsApp.Add(messaggio);

                conversazione.UltimoMessaggioAt = timestamp;
                dbContext.ConversazioniWhatsApp.Update(conversazione);

                await dbContext.SaveChangesAsync(cancellationToken);

                var messaggioDto = new MessaggioWhatsAppDto
                {
                    Id = messaggio.Id,
                    ConversazioneId = conversazione.Id,
                    Testo = testo,
                    Direzione = DirezioneMessaggio.Entrante,
                    Timestamp = timestamp,
                    StatoInvio = StatoInvioMessaggio.Consegnato
                };

                await hubContext.Clients.Group($"tenant_{tenantId}")
                    .SendAsync("NuovoMessaggioWhatsApp", messaggioDto, cancellationToken);

                if (conversazione.NumeroNonRiconosciuto)
                {
                    var convDto = await MapToDto(conversazione, cancellationToken);
                    await hubContext.Clients.Group($"tenant_{tenantId}")
                        .SendAsync("NumeroNonRiconosciuto", convDto, cancellationToken);
                }
            }
            finally
            {
                sem.Release();
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing inbound WhatsApp message from {Number}", numero);
            throw;
        }
    }

    public async Task<ConversazioneWhatsApp> GetOrCreateConversazioneAsync(string numero, Guid tenantId, CancellationToken cancellationToken = default)
    {
        try
        {
            var normalizzato = NormalizzaNumero(numero);
            var existing = await dbContext.ConversazioniWhatsApp
                .Include(c => c.BusinessParty)
                .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.NumeroDiTelefono == normalizzato && !c.IsDeleted, cancellationToken);

            if (existing != null) return existing;

            var businessParty = await TrovaBusinessPartyByTelefonoAsync(normalizzato, tenantId, cancellationToken);

            var nuova = new ConversazioneWhatsApp
            {
                TenantId = tenantId,
                NumeroDiTelefono = normalizzato,
                BusinessPartyId = businessParty?.Id,
                NumeroNonRiconosciuto = businessParty == null,
                Stato = StatoConversazioneWhatsApp.Attiva,
                UltimoMessaggioAt = DateTime.UtcNow,
                CreatedBy = "System"
            };
            dbContext.ConversazioniWhatsApp.Add(nuova);
            await dbContext.SaveChangesAsync(cancellationToken);
            return nuova;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting or creating WhatsApp conversation for {Number}", numero);
            throw;
        }
    }

    public async Task<BusinessParty?> TrovaBusinessPartyByTelefonoAsync(string numero, Guid tenantId, CancellationToken cancellationToken = default)
    {
        try
        {
            var normalizzato = NormalizzaNumero(numero);
            return await dbContext.BusinessParties
                .AsNoTracking()
                .Include(bp => bp.Contacts)
                .Where(bp => bp.TenantId == tenantId && !bp.IsDeleted)
                .FirstOrDefaultAsync(bp => bp.Contacts.Any(c =>
                    c.ContactType == ContactType.Phone &&
                    c.Value.Replace(" ", "").Replace("-", "").Replace("+", "")
                     .EndsWith(normalizzato.TrimStart('3', '9'))
                ), cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error finding BusinessParty by phone {Number}", numero);
            return null;
        }
    }

    public async Task AssegnaAdBusinessPartyEsistenteAsync(string numero, Guid businessPartyId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        try
        {
            var normalizzato = NormalizzaNumero(numero);
            var conversazione = await dbContext.ConversazioniWhatsApp
                .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.NumeroDiTelefono == normalizzato && !c.IsDeleted, cancellationToken);
            if (conversazione == null) return;

            conversazione.BusinessPartyId = businessPartyId;
            conversazione.NumeroNonRiconosciuto = false;
            conversazione.ModifiedAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);

            var dto = await MapToDto(conversazione, cancellationToken);
            await hubContext.Clients.Group($"tenant_{tenantId}")
                .SendAsync("ConversazioneAggiornata", dto, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error assigning BusinessParty {Id} to number {Number}", businessPartyId, numero);
            throw;
        }
    }

    public async Task<BusinessParty> CreaBusinessPartyEAssegnaAsync(AssegnaNumeroDto dto, Guid tenantId, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            var nuovaAnagrafica = dto.NuovaAnagrafica ?? throw new ArgumentNullException(nameof(dto.NuovaAnagrafica));
            var businessParty = new BusinessParty
            {
                TenantId = tenantId,
                Name = nuovaAnagrafica.Name,
                TaxCode = nuovaAnagrafica.TaxCode,
                VatNumber = nuovaAnagrafica.VatNumber,
                Notes = nuovaAnagrafica.Notes,
                CreatedBy = currentUser
            };
            dbContext.BusinessParties.Add(businessParty);
            await dbContext.SaveChangesAsync(cancellationToken);

            if (!string.IsNullOrEmpty(dto.NumeroDiTelefono))
                await AssegnaAdBusinessPartyEsistenteAsync(dto.NumeroDiTelefono, businessParty.Id, tenantId, cancellationToken);

            return businessParty;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating BusinessParty and assigning to WhatsApp number");
            throw;
        }
    }

    public async Task BloccaNumeroAsync(string numero, string? note, Guid tenantId, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            var normalizzato = NormalizzaNumero(numero);
            var esistente = await dbContext.NumeriBloccati
                .FirstOrDefaultAsync(n => n.TenantId == tenantId && n.NumeroDiTelefono == normalizzato, cancellationToken);
            if (esistente != null) return;

            dbContext.NumeriBloccati.Add(new NumeroBloccato
            {
                TenantId = tenantId,
                NumeroDiTelefono = normalizzato,
                BloccatoAt = DateTime.UtcNow,
                Note = note,
                CreatedBy = currentUser
            });

            var conversazione = await dbContext.ConversazioniWhatsApp
                .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.NumeroDiTelefono == normalizzato && !c.IsDeleted, cancellationToken);
            if (conversazione != null)
            {
                conversazione.Stato = StatoConversazioneWhatsApp.Bloccata;
                conversazione.ModifiedAt = DateTime.UtcNow;
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error blocking WhatsApp number {Number}", numero);
            throw;
        }
    }

    public async Task<MessaggioWhatsApp> InviaRispostaOperatoreAsync(Guid conversazioneId, string testo, Guid operatoreId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        try
        {
            var conversazione = await dbContext.ConversazioniWhatsApp
                .FirstOrDefaultAsync(c => c.Id == conversazioneId && c.TenantId == tenantId && !c.IsDeleted, cancellationToken)
                ?? throw new InvalidOperationException($"Conversation {conversazioneId} not found");

            var waMessageId = await whatsAppService.SendTextMessageAsync(conversazione.NumeroDiTelefono, testo, cancellationToken);

            var messaggio = new MessaggioWhatsApp
            {
                TenantId = tenantId,
                ConversazioneWhatsAppId = conversazioneId,
                Testo = testo,
                Direzione = DirezioneMessaggio.Uscente,
                StatoInvio = waMessageId != null ? StatoInvioMessaggio.Inviato : StatoInvioMessaggio.Errore,
                WhatsAppMessageId = waMessageId,
                MittenteOperatoreId = operatoreId,
                Timestamp = DateTime.UtcNow,
                IsLetto = true,
                CreatedBy = operatoreId.ToString()
            };
            dbContext.MessaggiWhatsApp.Add(messaggio);

            conversazione.UltimoMessaggioAt = messaggio.Timestamp;
            dbContext.ConversazioniWhatsApp.Update(conversazione);
            await dbContext.SaveChangesAsync(cancellationToken);

            var operatore = await dbContext.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == operatoreId, cancellationToken);
            var messaggioDto = new MessaggioWhatsAppDto
            {
                Id = messaggio.Id,
                ConversazioneId = conversazioneId,
                Testo = testo,
                Direzione = DirezioneMessaggio.Uscente,
                Timestamp = messaggio.Timestamp,
                NomeOperatore = operatore?.FullName,
                StatoInvio = messaggio.StatoInvio
            };
            await hubContext.Clients.Group($"tenant_{tenantId}")
                .SendAsync("NuovoMessaggioWhatsApp", messaggioDto, cancellationToken);

            return messaggio;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending operator reply on conversation {ConvId}", conversazioneId);
            throw;
        }
    }

    public async Task<List<ConversazioneWhatsAppDto>> GetConversazioniAttiveAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        try
        {
            var conversazioni = await dbContext.ConversazioniWhatsApp
                .AsNoTracking()
                .Include(c => c.BusinessParty)
                .Where(c => c.TenantId == tenantId && c.Stato == StatoConversazioneWhatsApp.Attiva && !c.IsDeleted)
                .OrderByDescending(c => c.UltimoMessaggioAt)
                .ToListAsync(cancellationToken);

            var result = new List<ConversazioneWhatsAppDto>(conversazioni.Count);
            foreach (var c in conversazioni)
                result.Add(await MapToDto(c, cancellationToken));
            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting active WhatsApp conversations for tenant {TenantId}", tenantId);
            return [];
        }
    }

    public async Task<List<MessaggioWhatsAppDto>> GetMessaggiAsync(Guid conversazioneId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        try
        {
            var messaggi = await dbContext.MessaggiWhatsApp
                .AsNoTracking()
                .Include(m => m.MittenteOperatore)
                .Where(m => m.ConversazioneWhatsAppId == conversazioneId && m.TenantId == tenantId && !m.IsDeleted)
                .OrderBy(m => m.Timestamp)
                .ToListAsync(cancellationToken);

            return messaggi.Select(m => new MessaggioWhatsAppDto
            {
                Id = m.Id,
                ConversazioneId = m.ConversazioneWhatsAppId,
                Testo = m.Testo,
                Direzione = m.Direzione,
                Timestamp = m.Timestamp,
                NomeOperatore = m.MittenteOperatore?.FullName,
                StatoInvio = m.StatoInvio
            }).ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting messages for conversation {ConvId}", conversazioneId);
            return [];
        }
    }

    public async Task AggiornaStatoMessaggioAsync(string whatsAppMessageId, StatoInvioMessaggio stato, Guid tenantId, CancellationToken cancellationToken = default)
    {
        try
        {
            var messaggio = await dbContext.MessaggiWhatsApp
                .FirstOrDefaultAsync(m => m.WhatsAppMessageId == whatsAppMessageId && m.TenantId == tenantId, cancellationToken);
            if (messaggio == null) return;

            messaggio.StatoInvio = stato;
            if (stato == StatoInvioMessaggio.Letto) messaggio.IsLetto = true;
            messaggio.ModifiedAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);

            await hubContext.Clients.Group($"tenant_{tenantId}")
                .SendAsync("StatoMessaggioAggiornato", new { Id = messaggio.Id, StatoInvio = stato }, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating message status for WA message {WaMsgId}", whatsAppMessageId);
        }
    }

    private async Task<ConversazioneWhatsAppDto> MapToDto(ConversazioneWhatsApp c, CancellationToken cancellationToken)
    {
        var ultimoMsg = await dbContext.MessaggiWhatsApp
            .AsNoTracking()
            .Where(m => m.ConversazioneWhatsAppId == c.Id && !m.IsDeleted)
            .OrderByDescending(m => m.Timestamp)
            .Select(m => m.Testo)
            .FirstOrDefaultAsync(cancellationToken);

        var nonLetti = await dbContext.MessaggiWhatsApp
            .AsNoTracking()
            .CountAsync(m => m.ConversazioneWhatsAppId == c.Id && !m.IsLetto
                          && m.Direzione == DirezioneMessaggio.Entrante && !m.IsDeleted, cancellationToken);

        return new ConversazioneWhatsAppDto
        {
            Id = c.Id,
            NumeroDiTelefono = c.NumeroDiTelefono,
            NomeAnagrafica = c.BusinessParty?.Name,
            NumeroNonRiconosciuto = c.NumeroNonRiconosciuto,
            UltimoMessaggio = ultimoMsg,
            UltimoMessaggioAt = c.UltimoMessaggioAt,
            StatoConversazione = c.Stato,
            MessaggiNonLetti = nonLetti,
            IsWhatsApp = true
        };
    }
}
