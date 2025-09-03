using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace EventForge.Server.Services;

/// <summary>
/// Service for signing QZ Tray requests with SHA512withRSA digital signatures
/// </summary>
public class QzSigner
{
    private readonly ILogger<QzSigner> _logger;
    private readonly string _privateKeyPath;

    public QzSigner(ILogger<QzSigner> logger, IConfiguration configuration)
    {
        _logger = logger;
        _privateKeyPath = Environment.GetEnvironmentVariable("QZ_PRIVATE_KEY_PATH") 
            ?? "private-key.pem";
    }

    /// <summary>
    /// Signs a QZ Tray call with the specified parameters and timestamp
    /// </summary>
    /// <param name="callName">The QZ Tray function name to call</param>
    /// <param name="params">Parameters for the call</param>
    /// <param name="timestamp">Unix timestamp in milliseconds</param>
    /// <returns>Base64-encoded signature using SHA512withRSA</returns>
    public async Task<string> Sign(string callName, object[] @params, long timestamp)
    {
        if (callName == null)
            throw new ArgumentNullException(nameof(callName));
        
        try
        {
            // Create JSON payload with properties in the specified order: call, params, timestamp
            var payload = new
            {
                call = callName,
                @params = @params,
                timestamp = timestamp
            };

            // Serialize to compact JSON
            var options = new JsonSerializerOptions
            {
                WriteIndented = false
            };
            var jsonData = JsonSerializer.Serialize(payload, options);

            _logger.LogDebug("Signing payload: {Payload}", jsonData);

            // Load private key and create signature
            using var rsa = await LoadPrivateKeyAsync();
            var dataBytes = Encoding.UTF8.GetBytes(jsonData);
            var signature = rsa.SignData(dataBytes, HashAlgorithmName.SHA512, RSASignaturePadding.Pkcs1);

            var base64Signature = Convert.ToBase64String(signature);
            _logger.LogDebug("Successfully created SHA512withRSA signature");
            
            return base64Signature;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error signing QZ Tray request");
            throw new InvalidOperationException("Failed to sign QZ Tray request", ex);
        }
    }

    private async Task<RSA> LoadPrivateKeyAsync()
    {
        try
        {
            var resolvedPath = Path.IsPathRooted(_privateKeyPath) 
                ? _privateKeyPath 
                : Path.Combine(AppContext.BaseDirectory, _privateKeyPath);

            if (!File.Exists(resolvedPath))
            {
                throw new FileNotFoundException($"Private key file not found: {resolvedPath}");
            }

            var privateKeyPem = await File.ReadAllTextAsync(resolvedPath);
            var rsa = RSA.Create();
            
            try
            {
                rsa.ImportFromPem(privateKeyPem);
                _logger.LogDebug("Successfully loaded private key from {Path}", resolvedPath);
                return rsa;
            }
            catch (CryptographicException ex)
            {
                rsa.Dispose();
                throw new InvalidOperationException(
                    $"Unsupported private key format. Only PKCS#8 and PKCS#1 PEM formats are supported. Path: {resolvedPath}", 
                    ex);
            }
        }
        catch (Exception ex) when (!(ex is InvalidOperationException))
        {
            _logger.LogError(ex, "Error loading private key from {Path}", _privateKeyPath);
            throw;
        }
    }
}