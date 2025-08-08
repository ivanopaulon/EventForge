using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;

namespace EventForge.Server.Services.Printing;

/// <summary>
/// Service for creating digital signatures for QZ Tray payloads using RSA-SHA256
/// </summary>
public class QzDigitalSignatureService
{
    private readonly ILogger<QzDigitalSignatureService> _logger;
    private readonly string _privateKeyPath;
    private readonly string _certificatePath;

    public QzDigitalSignatureService(ILogger<QzDigitalSignatureService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _privateKeyPath = configuration["QzSigning:PrivateKeyPath"] ?? "private-key.pem";
        _certificatePath = configuration["QzSigning:CertificatePath"] ?? "digital-certificate.txt";
    }

    /// <summary>
    /// Signs a QZ Tray payload and returns the payload with signature and certificate
    /// </summary>
    /// <param name="payload">The original payload object to sign</param>
    /// <returns>A new payload object with signature and certificate fields added</returns>
    public async Task<object> SignPayloadAsync(object payload)
    {
        try
        {
            // Convert payload to JSON for signing
            var payloadJson = JsonSerializer.Serialize(payload);
            _logger.LogDebug("Signing payload: {PayloadJson}", payloadJson);

            // Load private key and certificate
            var privateKey = await LoadPrivateKeyAsync();
            var certificate = await LoadCertificateAsync();

            // Create signature
            var signature = CreateSignature(payloadJson, privateKey);

            // Create signed payload with signature and certificate
            var signedPayload = new
            {
                call = GetPropertyValue(payload, "call"),
                @params = GetPropertyValue(payload, "params"),
                signature = Convert.ToBase64String(signature),
                certificate = certificate
            };

            _logger.LogInformation("Successfully signed QZ Tray payload");
            return signedPayload;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error signing QZ Tray payload");
            throw new InvalidOperationException("Failed to sign QZ Tray payload", ex);
        }
    }

    private async Task<RSA> LoadPrivateKeyAsync()
    {
        try
        {
            var privateKeyPath = GetFilePath(_privateKeyPath);
            if (!File.Exists(privateKeyPath))
            {
                throw new FileNotFoundException($"Private key file not found: {privateKeyPath}");
            }

            var privateKeyPem = await File.ReadAllTextAsync(privateKeyPath);
            var rsa = RSA.Create();
            rsa.ImportFromPem(privateKeyPem);
            
            _logger.LogDebug("Successfully loaded private key from {PrivateKeyPath}", privateKeyPath);
            return rsa;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading private key from {PrivateKeyPath}", _privateKeyPath);
            throw;
        }
    }

    private async Task<string> LoadCertificateAsync()
    {
        try
        {
            var certificatePath = GetFilePath(_certificatePath);
            if (!File.Exists(certificatePath))
            {
                throw new FileNotFoundException($"Certificate file not found: {certificatePath}");
            }

            var certificate = await File.ReadAllTextAsync(certificatePath);
            
            _logger.LogDebug("Successfully loaded certificate from {CertificatePath}", certificatePath);
            return certificate.Trim();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading certificate from {CertificatePath}", _certificatePath);
            throw;
        }
    }

    private byte[] CreateSignature(string data, RSA privateKey)
    {
        try
        {
            var dataBytes = Encoding.UTF8.GetBytes(data);
            var signature = privateKey.SignData(dataBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            
            _logger.LogDebug("Successfully created RSA-SHA256 signature for data length: {DataLength}", dataBytes.Length);
            return signature;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating RSA-SHA256 signature");
            throw;
        }
    }

    private string GetFilePath(string relativePath)
    {
        // If the path is already absolute, use it as-is
        if (Path.IsPathRooted(relativePath))
        {
            return relativePath;
        }

        // Otherwise, make it relative to the application's base directory
        return Path.Combine(AppContext.BaseDirectory, relativePath);
    }

    private object? GetPropertyValue(object obj, string propertyName)
    {
        var type = obj.GetType();
        var property = type.GetProperty(propertyName) ?? type.GetProperty($"@{propertyName}");
        return property?.GetValue(obj);
    }

    /// <summary>
    /// Validates that the signing configuration is properly set up
    /// </summary>
    /// <returns>True if signing is properly configured</returns>
    public async Task<bool> ValidateSigningConfigurationAsync()
    {
        try
        {
            var privateKeyPath = GetFilePath(_privateKeyPath);
            var certificatePath = GetFilePath(_certificatePath);

            if (!File.Exists(privateKeyPath))
            {
                _logger.LogWarning("Private key file not found: {PrivateKeyPath}", privateKeyPath);
                return false;
            }

            if (!File.Exists(certificatePath))
            {
                _logger.LogWarning("Certificate file not found: {CertificatePath}", certificatePath);
                return false;
            }

            // Try to load both files to ensure they're valid
            using var privateKey = await LoadPrivateKeyAsync();
            var certificate = await LoadCertificateAsync();

            _logger.LogInformation("QZ Tray signing configuration is valid");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "QZ Tray signing configuration validation failed");
            return false;
        }
    }
}