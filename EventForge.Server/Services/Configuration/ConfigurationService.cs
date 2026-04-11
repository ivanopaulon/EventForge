using Microsoft.EntityFrameworkCore;
using System.Net.Mail;
using System.Security.Cryptography;

namespace EventForge.Server.Services.Configuration;

/// <summary>
/// Service for managing system configuration settings.
/// </summary>
public class ConfigurationService(
    EventForgeDbContext context,
    ITenantContext tenantContext,
    IAuditLogService auditLogService,
    IConfiguration configuration,
    ILogger<ConfigurationService> logger) : IConfigurationService
{
    // Prefix used to identify AES-256-GCM encrypted values stored in the DB.
    // Values without this prefix are treated as legacy Base64 (read-only backward compat).
    private const string AesPrefix = "AES256GCM:";

    /// <summary>
    /// Derives a 32-byte AES key from the configured Encryption:Key value (or falls back
    /// to the JWT SecretKey) using SHA-256 to guarantee the correct length.
    /// </summary>
    private byte[] DeriveEncryptionKey()
    {
        var keySource = configuration["Encryption:Key"]
                        ?? configuration["Jwt:SecretKey"]
                        ?? "EventForge-DefaultKey-ChangeInProduction!";
        return SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(keySource));
    }

    public async Task<IEnumerable<ConfigurationDto>> GetAllConfigurationsAsync(CancellationToken ct = default)
    {
        try
        {
            var configurations = await context.SystemConfigurations
                .AsNoTracking()
                .OrderBy(c => c.Category)
                .ThenBy(c => c.Key)
                .ToListAsync(ct);

            return configurations.ToDto();
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("GetAllConfigurationsAsync operation was cancelled");
            throw;
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<IEnumerable<ConfigurationDto>> GetConfigurationsByCategoryAsync(string category, CancellationToken ct = default)
    {
        try
        {
            var configurations = await context.SystemConfigurations
                .AsNoTracking()
                .Where(c => c.Category == category)
                .OrderBy(c => c.Key)
                .ToListAsync(ct);

            return configurations.ToDto();
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("GetConfigurationsByCategoryAsync operation was cancelled for category {Category}", category);
            throw;
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<ConfigurationDto?> GetConfigurationAsync(string key, CancellationToken ct = default)
    {
        try
        {
            var configuration = await context.SystemConfigurations
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Key == key, ct);

            return configuration?.ToDto();
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("GetConfigurationAsync operation was cancelled for key {Key}", key);
            throw;
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<ConfigurationDto> CreateConfigurationAsync(CreateConfigurationDto createDto, CancellationToken ct = default)
    {
        try
        {
            // Check if configuration with the same key already exists
            var existing = await context.SystemConfigurations
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Key == createDto.Key, ct);

            if (existing is not null)
            {
                throw new InvalidOperationException($"Configuration with key '{createDto.Key}' already exists.");
            }

            var configuration = new SystemConfiguration
            {
                Key = createDto.Key,
                Value = createDto.IsEncrypted ? EncryptValue(createDto.Value) : createDto.Value,
                Description = createDto.Description,
                Category = createDto.Category,
                IsEncrypted = createDto.IsEncrypted,
                RequiresRestart = createDto.RequiresRestart,
                DefaultValue = createDto.Value,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = tenantContext.CurrentUserId?.ToString() ?? "System"
            };

            _ = context.SystemConfigurations.Add(configuration);
            _ = await context.SaveChangesAsync(ct);

            // Log the creation
            _ = await auditLogService.LogEntityChangeAsync(
                nameof(SystemConfiguration),
                configuration.Id,
                "Configuration",
                "Create",
                null,
                $"Key: {configuration.Key}, Category: {configuration.Category}",
                configuration.CreatedBy,
                $"Configuration '{configuration.Key}'",
                ct
            );

            return configuration.ToDto();
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("CreateConfigurationAsync operation was cancelled for key {Key}", createDto.Key);
            throw;
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<ConfigurationDto> UpdateConfigurationAsync(string key, UpdateConfigurationDto updateDto, CancellationToken ct = default)
    {
        try
        {
            var configuration = await context.SystemConfigurations
                .FirstOrDefaultAsync(c => c.Key == key, ct);

            if (configuration is null)
            {
                throw new InvalidOperationException($"Configuration with key '{key}' not found.");
            }

            if (configuration.IsReadOnly)
            {
                throw new InvalidOperationException($"Configuration '{key}' is read-only and cannot be modified.");
            }

            var oldValue = configuration.IsEncrypted ? "[ENCRYPTED]" : configuration.Value;
            var newValue = configuration.IsEncrypted ? EncryptValue(updateDto.Value) : updateDto.Value;

            configuration.Value = newValue;
            configuration.Description = updateDto.Description ?? configuration.Description;
            configuration.RequiresRestart = updateDto.RequiresRestart;
            configuration.ModifiedAt = DateTime.UtcNow;
            configuration.ModifiedBy = tenantContext.CurrentUserId?.ToString() ?? "System";

            _ = await context.SaveChangesAsync(ct);

            // Log the update
            _ = await auditLogService.LogEntityChangeAsync(
                nameof(SystemConfiguration),
                configuration.Id,
                "Value",
                "Update",
                oldValue,
                configuration.IsEncrypted ? "[ENCRYPTED]" : updateDto.Value,
                configuration.ModifiedBy,
                $"Configuration '{configuration.Key}'",
                ct
            );

            return configuration.ToDto();
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("UpdateConfigurationAsync operation was cancelled for key {Key}", key);
            throw;
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task DeleteConfigurationAsync(string key, CancellationToken ct = default)
    {
        try
        {
            var configuration = await context.SystemConfigurations
                .FirstOrDefaultAsync(c => c.Key == key, ct);

            if (configuration is null)
            {
                throw new InvalidOperationException($"Configuration with key '{key}' not found.");
            }

            if (configuration.IsReadOnly)
            {
                throw new InvalidOperationException($"Configuration '{key}' is read-only and cannot be deleted.");
            }

            _ = context.SystemConfigurations.Remove(configuration);
            _ = await context.SaveChangesAsync(ct);

            // Log the deletion
            _ = await auditLogService.LogEntityChangeAsync(
                nameof(SystemConfiguration),
                configuration.Id,
                "Configuration",
                "Delete",
                $"Key: {configuration.Key}, Category: {configuration.Category}",
                null,
                tenantContext.CurrentUserId?.ToString() ?? "System",
                $"Configuration '{configuration.Key}'",
                ct
            );
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("DeleteConfigurationAsync operation was cancelled for key {Key}", key);
            throw;
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<string> GetValueAsync(string key, string defaultValue = "", CancellationToken ct = default)
    {
        try
        {
            var configuration = await context.SystemConfigurations
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Key == key, ct);

            if (configuration is null)
            {
                return defaultValue;
            }

            return configuration.IsEncrypted ? DecryptValue(configuration.Value) : configuration.Value;
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("GetValueAsync operation was cancelled for key {Key}", key);
            throw;
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task SetValueAsync(string key, string value, string? reason = null, CancellationToken ct = default)
    {
        try
        {
            var configuration = await context.SystemConfigurations
                .FirstOrDefaultAsync(c => c.Key == key, ct);

            if (configuration is null)
            {
                // Create new configuration
                var createDto = new CreateConfigurationDto
                {
                    Key = key,
                    Value = value,
                    Description = reason,
                    Category = "General"
                };
                _ = await CreateConfigurationAsync(createDto, ct);
            }
            else
            {
                // Update existing configuration
                var updateDto = new UpdateConfigurationDto
                {
                    Value = value,
                    Description = reason ?? configuration.Description
                };
                _ = await UpdateConfigurationAsync(key, updateDto, ct);
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("SetValueAsync operation was cancelled for key {Key}", key);
            throw;
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<SmtpTestResultDto> TestSmtpAsync(SmtpTestDto testDto, CancellationToken ct = default)
    {
        var result = new SmtpTestResultDto();
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            // Get SMTP configuration from database
            var smtpServer = await GetValueAsync("SMTP_Server", "localhost", ct);
            var smtpPort = int.Parse(await GetValueAsync("SMTP_Port", "587", ct));
            var smtpUsername = await GetValueAsync("SMTP_Username", "", ct);
            var smtpPassword = await GetValueAsync("SMTP_Password", "", ct);
            var smtpEnableSsl = bool.Parse(await GetValueAsync("SMTP_EnableSSL", "true", ct));
            var smtpFromEmail = await GetValueAsync("SMTP_FromEmail", "noreply@eventforge.com", ct);
            var smtpFromName = await GetValueAsync("SMTP_FromName", "PRYM System", ct);

            using var client = new SmtpClient(smtpServer, smtpPort);
            client.EnableSsl = smtpEnableSsl;

            if (!string.IsNullOrEmpty(smtpUsername))
            {
                client.Credentials = new System.Net.NetworkCredential(smtpUsername, smtpPassword);
            }

            var message = new MailMessage
            {
                From = new MailAddress(smtpFromEmail, smtpFromName),
                Subject = testDto.Subject,
                Body = testDto.Body,
                IsBodyHtml = false
            };
            message.To.Add(testDto.ToEmail);

            await client.SendMailAsync(message, ct);

            result.Success = true;
            stopwatch.Stop();
            result.DurationMs = stopwatch.ElapsedMilliseconds;

            logger.LogInformation("SMTP test successful. Email sent to {Email} in {Duration}ms",
                testDto.ToEmail, result.DurationMs);
        }
        catch (OperationCanceledException)
        {
            result.Success = false;
            result.ErrorMessage = "Operation was cancelled";
            stopwatch.Stop();
            result.DurationMs = stopwatch.ElapsedMilliseconds;

            logger.LogInformation("SMTP test was cancelled for {Email}", testDto.ToEmail);
            throw;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            stopwatch.Stop();
            result.DurationMs = stopwatch.ElapsedMilliseconds;

            logger.LogError(ex, "SMTP test failed for {Email}", testDto.ToEmail);
        }

        return result;
    }

    public async Task ReloadConfigurationAsync(CancellationToken ct = default)
    {
        try
        {
            // This would trigger a configuration reload in the application
            // Implementation depends on how configuration is managed in the app
            // NOTE: CancellationToken will be used when actual implementation is added
            logger.LogInformation("Configuration reload requested");

            // Here you could implement logic to:
            // 1. Clear configuration cache
            // 2. Reload configuration from database
            // 3. Notify other services about configuration changes

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<IEnumerable<string>> GetCategoriesAsync(CancellationToken ct = default)
    {
        try
        {
            var categories = await context.SystemConfigurations
                .AsNoTracking()
                .Select(c => c.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync(ct);

            return categories;
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("GetCategoriesAsync operation was cancelled");
            throw;
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    private string EncryptValue(string value)
    {
        var key = DeriveEncryptionKey();
        var nonce = new byte[AesGcm.NonceByteSizes.MaxSize];   // 12 bytes
        var tag   = new byte[AesGcm.TagByteSizes.MaxSize];    // 16 bytes
        var plaintext  = System.Text.Encoding.UTF8.GetBytes(value);
        var ciphertext = new byte[plaintext.Length];

        RandomNumberGenerator.Fill(nonce);

        using var aes = new AesGcm(key, AesGcm.TagByteSizes.MaxSize);
        aes.Encrypt(nonce, plaintext, ciphertext, tag);

        // Format: nonce(12) + tag(16) + ciphertext — all Base64-encoded with prefix
        var blob = new byte[nonce.Length + tag.Length + ciphertext.Length];
        Buffer.BlockCopy(nonce, 0, blob, 0, nonce.Length);
        Buffer.BlockCopy(tag,   0, blob, nonce.Length, tag.Length);
        Buffer.BlockCopy(ciphertext, 0, blob, nonce.Length + tag.Length, ciphertext.Length);

        return AesPrefix + Convert.ToBase64String(blob);
    }

    private string DecryptValue(string encryptedValue)
    {
        // Backward compatibility: values without the AES prefix are legacy Base64
        if (!encryptedValue.StartsWith(AesPrefix, StringComparison.Ordinal))
        {
            try
            {
                var bytes = Convert.FromBase64String(encryptedValue);
                return System.Text.Encoding.UTF8.GetString(bytes);
            }
            catch (FormatException)
            {
                return encryptedValue;
            }
        }

        try
        {
            var blob  = Convert.FromBase64String(encryptedValue[AesPrefix.Length..]);
            var nonceSize = AesGcm.NonceByteSizes.MaxSize;  // 12
            var tagSize   = AesGcm.TagByteSizes.MaxSize;    // 16

            var nonce      = blob[..nonceSize];
            var tag        = blob[nonceSize..(nonceSize + tagSize)];
            var ciphertext = blob[(nonceSize + tagSize)..];
            var plaintext  = new byte[ciphertext.Length];

            var key = DeriveEncryptionKey();
            using var aes = new AesGcm(key, tagSize);
            aes.Decrypt(nonce, ciphertext, tag, plaintext);

            return System.Text.Encoding.UTF8.GetString(plaintext);
        }
        catch (Exception ex) when (ex is FormatException or CryptographicException)
        {
            logger.LogError(ex, "Failed to decrypt configuration value; returning raw value.");
            return encryptedValue;
        }
    }
}
