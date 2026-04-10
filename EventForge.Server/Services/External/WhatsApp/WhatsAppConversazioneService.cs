using EventForge.DTOs.Chat;
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
/// Business logic for WhatsApp conversations — now backed by the unified
/// <see cref="ChatThread"/> / <see cref="ChatMessage"/> entities.
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
                var thread = await GetOrCreateConversazioneAsync(normalizzato, tenantId, cancellationToken);

                var messaggio = new ChatMessage
                {
                    TenantId = tenantId,
                    ChatThreadId = thread.Id,
                    Content = testo,
                    SenderId = null, // incoming — no internal user
                    MessageDirection = MessageDirection.Entrante,
                    WhatsAppMessageId = msgId,
                    WhatsAppDeliveryStatus = WhatsAppDeliveryStatus.Consegnato,
                    Status = MessageStatus.Delivered,
                    SentAt = timestamp,
                    CreatedBy = "WhatsApp"
                };
                dbContext.ChatMessages.Add(messaggio);

                thread.UpdatedAt = timestamp;
                dbContext.ChatThreads.Update(thread);

                await dbContext.SaveChangesAsync(cancellationToken);

                var messaggioDto = MapMessageToDto(messaggio, thread.Id, null);
                await hubContext.Clients.Group($"tenant_{tenantId}")
                    .SendAsync("NuovoMessaggioWhatsApp", messaggioDto, cancellationToken);

                if (thread.IsUnrecognizedNumber)
                {
                    var convDto = await MapThreadToDto(thread, cancellationToken);
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

    public async Task<ChatThread> GetOrCreateConversazioneAsync(string numero, Guid tenantId, CancellationToken cancellationToken = default)
    {
        try
        {
            var normalizzato = NormalizzaNumero(numero);
            var existing = await dbContext.ChatThreads
                .Include(t => t.BusinessParty)
                .FirstOrDefaultAsync(t => t.TenantId == tenantId
                    && t.Type == ChatType.WhatsApp
                    && t.ExternalPhoneNumber == normalizzato
                    && !t.IsDeleted, cancellationToken);

            if (existing != null) return existing;

            var businessParty = await TrovaBusinessPartyByTelefonoAsync(normalizzato, tenantId, cancellationToken);

            var nuova = new ChatThread
            {
                TenantId = tenantId,
                Type = ChatType.WhatsApp,
                ExternalPhoneNumber = normalizzato,
                Name = businessParty?.Name ?? normalizzato,
                BusinessPartyId = businessParty?.Id,
                IsUnrecognizedNumber = businessParty == null,
                WhatsAppLastStatus = WhatsAppConversationStatus.Attiva,
                IsPrivate = true,
                UpdatedAt = DateTime.UtcNow,
                CreatedBy = "System"
            };
            dbContext.ChatThreads.Add(nuova);
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
            // Use the local digits suffix (strip country code) for matching stored contact values
            const int CountryCodeLength = 2; // Italian country code "39" is 2 digits
            var suffixToMatch = normalizzato.Length > CountryCodeLength ? normalizzato[CountryCodeLength..] : normalizzato;
            return await dbContext.BusinessParties
                .AsNoTracking()
                .Include(bp => bp.Contacts)
                .Where(bp => bp.TenantId == tenantId && !bp.IsDeleted)
                .FirstOrDefaultAsync(bp => bp.Contacts.Any(c =>
                    c.ContactType == ContactType.Phone &&
                    c.Value.Replace(" ", "").Replace("-", "").Replace("+", "")
                     .EndsWith(suffixToMatch)
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
            var thread = await dbContext.ChatThreads
                .FirstOrDefaultAsync(t => t.TenantId == tenantId
                    && t.Type == ChatType.WhatsApp
                    && t.ExternalPhoneNumber == normalizzato
                    && !t.IsDeleted, cancellationToken);
            if (thread == null) return;

            thread.BusinessPartyId = businessPartyId;
            thread.IsUnrecognizedNumber = false;
            thread.ModifiedAt = DateTime.UtcNow;

            // Update name to the BusinessParty's name
            var bp = await dbContext.BusinessParties.AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == businessPartyId, cancellationToken);
            if (bp != null) thread.Name = bp.Name;

            await dbContext.SaveChangesAsync(cancellationToken);

            var dto = await MapThreadToDto(thread, cancellationToken);
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

            var thread = await dbContext.ChatThreads
                .FirstOrDefaultAsync(t => t.TenantId == tenantId
                    && t.Type == ChatType.WhatsApp
                    && t.ExternalPhoneNumber == normalizzato
                    && !t.IsDeleted, cancellationToken);
            if (thread != null)
            {
                thread.WhatsAppLastStatus = WhatsAppConversationStatus.Bloccata;
                thread.ModifiedAt = DateTime.UtcNow;
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error blocking WhatsApp number {Number}", numero);
            throw;
        }
    }

    public async Task<ChatMessage> InviaRispostaOperatoreAsync(Guid chatThreadId, string testo, Guid operatoreId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        try
        {
            var thread = await dbContext.ChatThreads
                .FirstOrDefaultAsync(t => t.Id == chatThreadId && t.TenantId == tenantId && !t.IsDeleted, cancellationToken)
                ?? throw new InvalidOperationException($"ChatThread {chatThreadId} not found");

            if (string.IsNullOrEmpty(thread.ExternalPhoneNumber))
                throw new InvalidOperationException($"ChatThread {chatThreadId} is not a WhatsApp conversation");

            var waMessageId = await whatsAppService.SendTextMessageAsync(thread.ExternalPhoneNumber, testo, cancellationToken);

            var messaggio = new ChatMessage
            {
                TenantId = tenantId,
                ChatThreadId = chatThreadId,
                Content = testo,
                SenderId = operatoreId,
                MessageDirection = MessageDirection.Uscente,
                WhatsAppDeliveryStatus = waMessageId != null ? WhatsAppDeliveryStatus.Inviato : WhatsAppDeliveryStatus.Errore,
                WhatsAppMessageId = waMessageId,
                Status = waMessageId != null ? MessageStatus.Sent : MessageStatus.Failed,
                SentAt = DateTime.UtcNow,
                CreatedBy = operatoreId.ToString()
            };
            dbContext.ChatMessages.Add(messaggio);

            thread.UpdatedAt = messaggio.SentAt;
            dbContext.ChatThreads.Update(thread);
            await dbContext.SaveChangesAsync(cancellationToken);

            var operatore = await dbContext.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == operatoreId, cancellationToken);
            var messaggioDto = MapMessageToDto(messaggio, chatThreadId, operatore?.FullName);
            await hubContext.Clients.Group($"tenant_{tenantId}")
                .SendAsync("NuovoMessaggioWhatsApp", messaggioDto, cancellationToken);

            return messaggio;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending operator reply on conversation {ThreadId}", chatThreadId);
            throw;
        }
    }

    public async Task<List<ChatResponseDto>> GetConversazioniAttiveAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        try
        {
            var threads = await dbContext.ChatThreads
                .AsNoTracking()
                .Include(t => t.BusinessParty)
                .Where(t => t.TenantId == tenantId
                    && t.Type == ChatType.WhatsApp
                    && t.WhatsAppLastStatus == WhatsAppConversationStatus.Attiva
                    && !t.IsDeleted)
                .OrderByDescending(t => t.UpdatedAt)
                .ToListAsync(cancellationToken);

            var result = new List<ChatResponseDto>(threads.Count);
            foreach (var t in threads)
                result.Add(await MapThreadToDto(t, cancellationToken));
            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting active WhatsApp conversations for tenant {TenantId}", tenantId);
            return [];
        }
    }

    public async Task<List<ChatMessageDto>> GetMessaggiAsync(Guid chatThreadId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        try
        {
            var messaggi = await dbContext.ChatMessages
                .AsNoTracking()
                .Where(m => m.ChatThreadId == chatThreadId && m.TenantId == tenantId && !m.IsDeleted)
                .OrderBy(m => m.SentAt)
                .ToListAsync(cancellationToken);

            // Bulk-load operator names for outgoing messages
            var operatorIds = messaggi
                .Where(m => m.SenderId.HasValue)
                .Select(m => m.SenderId!.Value)
                .Distinct()
                .ToList();
            var operatoriNomi = operatorIds.Count > 0
                ? await dbContext.Users.AsNoTracking()
                    .Where(u => operatorIds.Contains(u.Id))
                    .ToDictionaryAsync(u => u.Id, u => (string?)u.FullName, cancellationToken)
                : new Dictionary<Guid, string?>();

            return messaggi.Select(m => MapMessageToDto(
                m, chatThreadId,
                m.SenderId.HasValue && operatoriNomi.TryGetValue(m.SenderId.Value, out var n) ? n : null
            )).ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting messages for thread {ThreadId}", chatThreadId);
            return [];
        }
    }

    public async Task AggiornaStatoMessaggioAsync(string whatsAppMessageId, WhatsAppDeliveryStatus stato, Guid tenantId, CancellationToken cancellationToken = default)
    {
        try
        {
            var messaggio = await dbContext.ChatMessages
                .FirstOrDefaultAsync(m => m.WhatsAppMessageId == whatsAppMessageId && m.TenantId == tenantId, cancellationToken);
            if (messaggio == null) return;

            messaggio.WhatsAppDeliveryStatus = stato;
            if (stato == WhatsAppDeliveryStatus.Letto)
            {
                messaggio.Status = MessageStatus.Read;
                messaggio.ReadAt = DateTime.UtcNow;
            }
            else if (stato == WhatsAppDeliveryStatus.Consegnato)
            {
                messaggio.Status = MessageStatus.Delivered;
                messaggio.DeliveredAt = DateTime.UtcNow;
            }
            messaggio.ModifiedAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);

            await hubContext.Clients.Group($"tenant_{tenantId}")
                .SendAsync("StatoMessaggioAggiornato", new { Id = messaggio.Id, WhatsAppDeliveryStatus = stato }, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating message status for WA message {WaMsgId}", whatsAppMessageId);
        }
    }

    // ─── Mapping helpers ─────────────────────────────────────────────────────

    private async Task<ChatResponseDto> MapThreadToDto(ChatThread t, CancellationToken cancellationToken)
    {
        var ultimoMsg = await dbContext.ChatMessages
            .AsNoTracking()
            .Where(m => m.ChatThreadId == t.Id && !m.IsDeleted)
            .OrderByDescending(m => m.SentAt)
            .Select(m => m.Content)
            .FirstOrDefaultAsync(cancellationToken);

        var nonLetti = await dbContext.ChatMessages
            .AsNoTracking()
            .CountAsync(m => m.ChatThreadId == t.Id
                && m.MessageDirection == MessageDirection.Entrante
                && m.Status != MessageStatus.Read
                && !m.IsDeleted, cancellationToken);

        return new ChatResponseDto
        {
            Id = t.Id,
            TenantId = t.TenantId,
            Type = ChatType.WhatsApp,
            Name = t.Name,
            UpdatedAt = t.UpdatedAt,
            CreatedAt = t.CreatedAt,
            CreatedBy = Guid.Empty,
            UnreadCount = nonLetti,
            ExternalPhoneNumber = t.ExternalPhoneNumber,
            BusinessPartyName = t.BusinessParty?.Name,
            IsUnrecognizedNumber = t.IsUnrecognizedNumber,
            LastMessage = ultimoMsg != null ? new ChatMessageDto { Content = ultimoMsg } : null
        };
    }

    private static ChatMessageDto MapMessageToDto(ChatMessage m, Guid chatThreadId, string? senderName) =>
        new()
        {
            Id = m.Id,
            ChatId = chatThreadId,
            SenderId = m.SenderId ?? Guid.Empty,
            SenderName = senderName,
            Content = m.Content,
            Status = m.Status,
            SentAt = m.SentAt,
            DeliveredAt = m.DeliveredAt,
            ReadAt = m.ReadAt,
            Direction = m.MessageDirection,
            WhatsAppDeliveryStatus = m.WhatsAppDeliveryStatus
        };
}
