using EventForge.Server.Extensions;
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

    public async Task<IEnumerable<ConfigurationDto>> GetAllConfigurationsAsync()
    {
        var configurations = await _context.SystemConfigurations
            .OrderBy(c => c.Category)
            .ThenBy(c => c.Key)
            .ToListAsync();

        return configurations.ToDto();
    }

    public async Task<IEnumerable<ConfigurationDto>> GetConfigurationsByCategoryAsync(string category)
    {
        var configurations = await _context.SystemConfigurations
            .Where(c => c.Category == category)
            .OrderBy(c => c.Key)
            .ToListAsync();

        return configurations.ToDto();
    }

    public async Task<ConfigurationDto?> GetConfigurationAsync(string key)
    {
        var configuration = await _context.SystemConfigurations
            .FirstOrDefaultAsync(c => c.Key == key);

        return configuration?.ToDto();
    }

    public async Task<ConfigurationDto> CreateConfigurationAsync(CreateConfigurationDto createDto)
    {
        // Check if configuration with the same key already exists
        var existing = await _context.SystemConfigurations
            .FirstOrDefaultAsync(c => c.Key == createDto.Key);

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

        _context.SystemConfigurations.Add(configuration);
        await _context.SaveChangesAsync();

        // Log the creation
        await _auditLogService.LogEntityChangeAsync(
            nameof(SystemConfiguration),
            configuration.Id,
            "Configuration",
            "Create",
            null,
            $"Key: {configuration.Key}, Category: {configuration.Category}",
            configuration.CreatedBy,
            $"Configuration '{configuration.Key}'"
        );

        return configuration.ToDto();
    }

    public async Task<ConfigurationDto> UpdateConfigurationAsync(string key, UpdateConfigurationDto updateDto)
    {
        var configuration = await _context.SystemConfigurations
            .FirstOrDefaultAsync(c => c.Key == key);

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

        await _context.SaveChangesAsync();

        // Log the update
        await _auditLogService.LogEntityChangeAsync(
            nameof(SystemConfiguration),
            configuration.Id,
            "Value",
            "Update",
            oldValue,
            configuration.IsEncrypted ? "[ENCRYPTED]" : updateDto.Value,
            configuration.ModifiedBy,
            $"Configuration '{configuration.Key}'"
        );

        return configuration.ToDto();
    }

    public async Task DeleteConfigurationAsync(string key)
    {
        var configuration = await _context.SystemConfigurations
            .FirstOrDefaultAsync(c => c.Key == key);

        if (configuration == null)
        {
            throw new InvalidOperationException($"Configuration with key '{key}' not found.");
        }

        if (configuration.IsReadOnly)
        {
            throw new InvalidOperationException($"Configuration '{key}' is read-only and cannot be deleted.");
        }

        _context.SystemConfigurations.Remove(configuration);
        await _context.SaveChangesAsync();

        // Log the deletion
        await _auditLogService.LogEntityChangeAsync(
            nameof(SystemConfiguration),
            configuration.Id,
            "Configuration",
            "Delete",
            $"Key: {configuration.Key}, Category: {configuration.Category}",
            null,
            _tenantContext.CurrentUserId?.ToString() ?? "System",
            $"Configuration '{configuration.Key}'"
        );
    }

    public async Task<string> GetValueAsync(string key, string defaultValue = "")
    {
        var configuration = await _context.SystemConfigurations
            .FirstOrDefaultAsync(c => c.Key == key);

        if (configuration == null)
        {
            return defaultValue;
        }

        return configuration.IsEncrypted ? DecryptValue(configuration.Value) : configuration.Value;
    }

    public async Task SetValueAsync(string key, string value, string? reason = null)
    {
        var configuration = await _context.SystemConfigurations
            .FirstOrDefaultAsync(c => c.Key == key);

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
            await CreateConfigurationAsync(createDto);
        }
        else
        {
            // Update existing configuration
            var updateDto = new UpdateConfigurationDto
            {
                Value = value,
                Description = reason ?? configuration.Description
            };
            await UpdateConfigurationAsync(key, updateDto);
        }
    }

    public async Task<SmtpTestResultDto> TestSmtpAsync(SmtpTestDto testDto)
    {
        var result = new SmtpTestResultDto();
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            // Get SMTP configuration from database
            var smtpServer = await GetValueAsync("SMTP_Server", "localhost");
            var smtpPort = int.Parse(await GetValueAsync("SMTP_Port", "587"));
            var smtpUsername = await GetValueAsync("SMTP_Username", "");
            var smtpPassword = await GetValueAsync("SMTP_Password", "");
            var smtpEnableSsl = bool.Parse(await GetValueAsync("SMTP_EnableSSL", "true"));
            var smtpFromEmail = await GetValueAsync("SMTP_FromEmail", "noreply@eventforge.com");
            var smtpFromName = await GetValueAsync("SMTP_FromName", "EventForge System");

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

            await client.SendMailAsync(message);

            result.Success = true;
            stopwatch.Stop();
            result.DurationMs = stopwatch.ElapsedMilliseconds;

            _logger.LogInformation("SMTP test successful. Email sent to {Email} in {Duration}ms",
                testDto.ToEmail, result.DurationMs);
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

    public async Task ReloadConfigurationAsync()
    {
        // This would trigger a configuration reload in the application
        // Implementation depends on how configuration is managed in the app
        _logger.LogInformation("Configuration reload requested");

        // Here you could implement logic to:
        // 1. Clear configuration cache
        // 2. Reload configuration from database
        // 3. Notify other services about configuration changes

        await Task.CompletedTask;
    }

    public async Task<IEnumerable<string>> GetCategoriesAsync()
    {
        var categories = await _context.SystemConfigurations
            .Select(c => c.Category)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();

        return categories;
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