using Microsoft.EntityFrameworkCore;
using System.Net.Mail;

namespace EventForge.Server.Services.Configuration;

/// <summary>
/// Service for managing system configuration settings.
/// </summary>
public class ConfigurationService : IConfigurationService
{
    private readonly EventForgeDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<ConfigurationService> _logger;

    public ConfigurationService(
        EventForgeDbContext context,
        ITenantContext tenantContext,
        IAuditLogService auditLogService,
        ILogger<ConfigurationService> logger)
    {
        _context = context;
        _tenantContext = tenantContext;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task<IEnumerable<ConfigurationDto>> GetAllConfigurationsAsync(CancellationToken ct = default)
    {
        try
        {
            var configurations = await _context.SystemConfigurations
                .AsNoTracking()
                .OrderBy(c => c.Category)
                .ThenBy(c => c.Key)
                .ToListAsync(ct);

            return configurations.ToDto();
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("GetAllConfigurationsAsync operation was cancelled");
            throw;
        }
    }

    public async Task<IEnumerable<ConfigurationDto>> GetConfigurationsByCategoryAsync(string category, CancellationToken ct = default)
    {
        try
        {
            var configurations = await _context.SystemConfigurations
                .AsNoTracking()
                .Where(c => c.Category == category)
                .OrderBy(c => c.Key)
                .ToListAsync(ct);

            return configurations.ToDto();
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("GetConfigurationsByCategoryAsync operation was cancelled for category {Category}", category);
            throw;
        }
    }

    public async Task<ConfigurationDto?> GetConfigurationAsync(string key, CancellationToken ct = default)
    {
        try
        {
            var configuration = await _context.SystemConfigurations
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Key == key, ct);

            return configuration?.ToDto();
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("GetConfigurationAsync operation was cancelled for key {Key}", key);
            throw;
        }
    }

    public async Task<ConfigurationDto> CreateConfigurationAsync(CreateConfigurationDto createDto, CancellationToken ct = default)
    {
        try
        {
            // Check if configuration with the same key already exists
            var existing = await _context.SystemConfigurations
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Key == createDto.Key, ct);

            if (existing != null)
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
                CreatedBy = _tenantContext.CurrentUserId?.ToString() ?? "System"
            };

            _ = _context.SystemConfigurations.Add(configuration);
            _ = await _context.SaveChangesAsync(ct);

            // Log the creation
            _ = await _auditLogService.LogEntityChangeAsync(
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
            _logger.LogInformation("CreateConfigurationAsync operation was cancelled for key {Key}", createDto.Key);
            throw;
        }
    }

    public async Task<ConfigurationDto> UpdateConfigurationAsync(string key, UpdateConfigurationDto updateDto, CancellationToken ct = default)
    {
        try
        {
            var configuration = await _context.SystemConfigurations
                .FirstOrDefaultAsync(c => c.Key == key, ct);

            if (configuration == null)
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
            configuration.ModifiedBy = _tenantContext.CurrentUserId?.ToString() ?? "System";

            _ = await _context.SaveChangesAsync(ct);

            // Log the update
            _ = await _auditLogService.LogEntityChangeAsync(
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
            _logger.LogInformation("UpdateConfigurationAsync operation was cancelled for key {Key}", key);
            throw;
        }
    }

    public async Task DeleteConfigurationAsync(string key, CancellationToken ct = default)
    {
        try
        {
            var configuration = await _context.SystemConfigurations
                .FirstOrDefaultAsync(c => c.Key == key, ct);

            if (configuration == null)
            {
                throw new InvalidOperationException($"Configuration with key '{key}' not found.");
            }

            if (configuration.IsReadOnly)
            {
                throw new InvalidOperationException($"Configuration '{key}' is read-only and cannot be deleted.");
            }

            _ = _context.SystemConfigurations.Remove(configuration);
            _ = await _context.SaveChangesAsync(ct);

            // Log the deletion
            _ = await _auditLogService.LogEntityChangeAsync(
                nameof(SystemConfiguration),
                configuration.Id,
                "Configuration",
                "Delete",
                $"Key: {configuration.Key}, Category: {configuration.Category}",
                null,
                _tenantContext.CurrentUserId?.ToString() ?? "System",
                $"Configuration '{configuration.Key}'",
                ct
            );
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("DeleteConfigurationAsync operation was cancelled for key {Key}", key);
            throw;
        }
    }

    public async Task<string> GetValueAsync(string key, string defaultValue = "", CancellationToken ct = default)
    {
        try
        {
            var configuration = await _context.SystemConfigurations
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Key == key, ct);

            if (configuration == null)
            {
                return defaultValue;
            }

            return configuration.IsEncrypted ? DecryptValue(configuration.Value) : configuration.Value;
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("GetValueAsync operation was cancelled for key {Key}", key);
            throw;
        }
    }

    public async Task SetValueAsync(string key, string value, string? reason = null, CancellationToken ct = default)
    {
        try
        {
            var configuration = await _context.SystemConfigurations
                .FirstOrDefaultAsync(c => c.Key == key, ct);

            if (configuration == null)
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
            _logger.LogInformation("SetValueAsync operation was cancelled for key {Key}", key);
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
            var smtpFromName = await GetValueAsync("SMTP_FromName", "EventForge System", ct);

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

            _logger.LogInformation("SMTP test successful. Email sent to {Email} in {Duration}ms",
                testDto.ToEmail, result.DurationMs);
        }
        catch (OperationCanceledException)
        {
            result.Success = false;
            result.ErrorMessage = "Operation was cancelled";
            stopwatch.Stop();
            result.DurationMs = stopwatch.ElapsedMilliseconds;

            _logger.LogInformation("SMTP test was cancelled for {Email}", testDto.ToEmail);
            throw;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            stopwatch.Stop();
            result.DurationMs = stopwatch.ElapsedMilliseconds;

            _logger.LogError(ex, "SMTP test failed for {Email}", testDto.ToEmail);
        }

        return result;
    }

    public async Task ReloadConfigurationAsync(CancellationToken ct = default)
    {
        // This would trigger a configuration reload in the application
        // Implementation depends on how configuration is managed in the app
        // NOTE: CancellationToken will be used when actual implementation is added
        _logger.LogInformation("Configuration reload requested");

        // Here you could implement logic to:
        // 1. Clear configuration cache
        // 2. Reload configuration from database
        // 3. Notify other services about configuration changes

        await Task.CompletedTask;
    }

    public async Task<IEnumerable<string>> GetCategoriesAsync(CancellationToken ct = default)
    {
        try
        {
            var categories = await _context.SystemConfigurations
                .AsNoTracking()
                .Select(c => c.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync(ct);

            return categories;
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("GetCategoriesAsync operation was cancelled");
            throw;
        }
    }

    private string EncryptValue(string value)
    {
        // Simple Base64 encoding for demonstration
        // In production, use proper encryption like AES
        var bytes = System.Text.Encoding.UTF8.GetBytes(value);
        return Convert.ToBase64String(bytes);
    }

    private string DecryptValue(string encryptedValue)
    {
        // Simple Base64 decoding for demonstration
        // In production, use proper decryption like AES
        try
        {
            var bytes = Convert.FromBase64String(encryptedValue);
            return System.Text.Encoding.UTF8.GetString(bytes);
        }
        catch
        {
            return encryptedValue; // Return as-is if decryption fails
        }
    }
}