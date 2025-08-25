using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace EventForge.Server.Services.Printing;

/// <summary>
/// Service for creating digital signatures for QZ Tray payloads using RSA-SHA256
/// with complete certificate chain, timestamp, and uid support
/// </summary>
public class QzDigitalSignatureService
{
    private readonly ILogger<QzDigitalSignatureService> _logger;
    private readonly string _privateKeyPath;
    private readonly string _certificatePath;
    private readonly string _intermediateCertificatePath;
    
    // Lightweight certificate chain caching
    private string? _cachedCertificateChain;
    private DateTime _cacheTimestamp = DateTime.MinValue;
    private readonly TimeSpan _cacheExpiry = TimeSpan.FromMinutes(5); // 5 minute cache

    public QzDigitalSignatureService(ILogger<QzDigitalSignatureService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _privateKeyPath = configuration["QzSigning:PrivateKeyPath"] ?? "private-key.pem";
        _certificatePath = configuration["QzSigning:CertificatePath"] ?? "digital-certificate.txt";
        _intermediateCertificatePath = configuration["QzSigning:IntermediateCertificatePath"] ?? "";
    }

    /// <summary>
    /// Signs a QZ Tray payload and returns the payload with complete certificate chain, timestamp, uid, and signature
    /// </summary>
    /// <param name="payload">The original payload object to sign</param>
    /// <returns>A new payload object with signature, certificate chain, timestamp, uid, and position fields added</returns>
    public async Task<object> SignPayloadAsync(object payload)
    {
        try
        {
            _logger.LogDebug("Starting to sign QZ Tray payload");

            // Load private key and certificate chain
            var privateKey = await LoadPrivateKeyAsync();
            var certificateChain = await LoadCertificateChainAsync();

            // Generate timestamp (UTC milliseconds) and uid
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var uid = GenerateUid();

            // Create payload with all required fields except signature
            var unsignedPayload = new
            {
                call = GetPropertyValue(payload, "call"),
                @params = GetPropertyValue(payload, "params"),
                certificate = certificateChain,
                timestamp = timestamp,
                uid = uid,
                position = new { x = 960, y = 516 } // Default position as per QZ Tray demo
            };

            // Serialize the unsigned payload for signature calculation
            var unsignedPayloadJson = JsonSerializer.Serialize(unsignedPayload);
            _logger.LogDebug("Unsigned payload for signing: {PayloadJson}", unsignedPayloadJson);

            // Create signature
            var signature = CreateSignature(unsignedPayloadJson, privateKey);

            // Create final signed payload with signature
            var signedPayload = new
            {
                call = unsignedPayload.call,
                @params = unsignedPayload.@params,
                certificate = unsignedPayload.certificate,
                timestamp = unsignedPayload.timestamp,
                uid = unsignedPayload.uid,
                signature = Convert.ToBase64String(signature),
                position = unsignedPayload.position
            };

            _logger.LogInformation("Successfully signed QZ Tray payload with timestamp: {Timestamp}, uid: {Uid}", timestamp, uid);
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

    private async Task<string> LoadCertificateChainAsync()
    {
        try
        {
            var certificatePath = GetFilePath(_certificatePath);
            if (!File.Exists(certificatePath))
            {
                throw new FileNotFoundException($"Certificate file not found: {certificatePath}");
            }

            var leafCertificate = await File.ReadAllTextAsync(certificatePath);

            // Start with the leaf certificate
            var certificateChain = leafCertificate.Trim();

            // Check for intermediate certificate and concatenate with proper markers
            if (!string.IsNullOrEmpty(_intermediateCertificatePath))
            {
                var intermediatePath = GetFilePath(_intermediateCertificatePath);
                if (File.Exists(intermediatePath))
                {
                    var intermediateCertificate = await File.ReadAllTextAsync(intermediatePath);

                    // Concatenate with intermediate certificate marker as per QZ Tray requirements
                    certificateChain += "\n--START INTERMEDIATE CERT--\n" + intermediateCertificate.Trim();

                    _logger.LogDebug("Successfully loaded certificate chain with intermediate certificate from {IntermediatePath}", intermediatePath);
                }
                else
                {
                    _logger.LogWarning("Intermediate certificate path configured but file not found: {IntermediatePath}", intermediatePath);
                }
            }

            _logger.LogDebug("Successfully loaded certificate chain from {CertificatePath}", certificatePath);
            return certificateChain;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading certificate chain from {CertificatePath}", _certificatePath);
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

    private string GenerateUid()
    {
        try
        {
            // Generate a new GUID and convert to a short base64 string (similar to QZ Tray demo)
            var guid = Guid.NewGuid();
            var guidBytes = guid.ToByteArray();

            // Take first 6 bytes and convert to base64, then remove padding and make lowercase
            var shortBytes = guidBytes.Take(6).ToArray();
            var base64 = Convert.ToBase64String(shortBytes)
                .Replace("+", "")
                .Replace("/", "")
                .Replace("=", "")
                .ToLowerInvariant();

            _logger.LogDebug("Generated uid: {Uid}", base64);
            return base64;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating uid");
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
    /// Gets the complete certificate chain for QZ Tray certificate endpoint
    /// </summary>
    /// <returns>Complete certificate chain as text</returns>
    public async Task<string> GetCertificateChainAsync()
    {
        try
        {
            // Check cache first
            if (_cachedCertificateChain != null && 
                DateTime.UtcNow - _cacheTimestamp < _cacheExpiry)
            {
                _logger.LogDebug("Returning cached certificate chain");
                return _cachedCertificateChain;
            }

            // Load fresh certificate chain
            var certificateChain = await LoadCertificateChainAsync();
            
            // Update cache
            _cachedCertificateChain = certificateChain;
            _cacheTimestamp = DateTime.UtcNow;
            
            _logger.LogDebug("Certificate chain loaded and cached");
            return certificateChain;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting certificate chain for QZ endpoint");
            throw new InvalidOperationException("Failed to load certificate chain for QZ Tray", ex);
        }
    }

    /// <summary>
    /// Signs a challenge string for QZ Tray signature endpoint
    /// </summary>
    /// <param name="challenge">Challenge string to sign</param>
    /// <returns>Base64-encoded signature</returns>
    public async Task<string> SignChallengeAsync(string challenge)
    {
        try
        {
            if (string.IsNullOrEmpty(challenge))
            {
                throw new ArgumentException("Challenge cannot be null or empty", nameof(challenge));
            }

            _logger.LogDebug("Signing challenge for QZ endpoint, length: {Length}", challenge.Length);

            // Load private key
            using var privateKey = await LoadPrivateKeyAsync();
            
            // Create signature
            var signature = CreateSignature(challenge, privateKey);
            var base64Signature = Convert.ToBase64String(signature);
            
            _logger.LogDebug("Challenge signed successfully, signature length: {Length}", base64Signature.Length);
            return base64Signature;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error signing challenge for QZ endpoint");
            throw new InvalidOperationException("Failed to sign challenge for QZ Tray", ex);
        }
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

            // Check intermediate certificate if configured
            if (!string.IsNullOrEmpty(_intermediateCertificatePath))
            {
                var intermediatePath = GetFilePath(_intermediateCertificatePath);
                if (!File.Exists(intermediatePath))
                {
                    _logger.LogWarning("Intermediate certificate file not found: {IntermediatePath}", intermediatePath);
                    // This is a warning, not a failure - intermediate cert is optional
                }
            }

            // Try to load both files to ensure they're valid
            using var privateKey = await LoadPrivateKeyAsync();
            var certificateChain = await LoadCertificateChainAsync();

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