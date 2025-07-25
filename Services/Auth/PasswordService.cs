using Konscious.Security.Cryptography;
using System.Security.Cryptography;
using System.Text;

namespace EventForge.Services.Auth;

/// <summary>
/// Service for password hashing and verification using Argon2.
/// </summary>
public interface IPasswordService
{
    /// <summary>
    /// Hashes a password using Argon2.
    /// </summary>
    /// <param name="password">Plain text password</param>
    /// <returns>Hash and salt</returns>
    (string Hash, string Salt) HashPassword(string password);

    /// <summary>
    /// Verifies a password against a hash.
    /// </summary>
    /// <param name="password">Plain text password</param>
    /// <param name="hash">Stored hash</param>
    /// <param name="salt">Stored salt</param>
    /// <returns>True if password matches</returns>
    bool VerifyPassword(string password, string hash, string salt);

    /// <summary>
    /// Validates password against security policy.
    /// </summary>
    /// <param name="password">Password to validate</param>
    /// <returns>Validation result</returns>
    PasswordValidationResult ValidatePassword(string password);
}

/// <summary>
/// Password validation result.
/// </summary>
public class PasswordValidationResult
{
    /// <summary>
    /// Indicates if the password is valid.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// List of validation errors.
    /// </summary>
    public List<string> Errors { get; set; } = new();
}

/// <summary>
/// Password policy configuration.
/// </summary>
public class PasswordPolicy
{
    /// <summary>
    /// Minimum password length.
    /// </summary>
    public int MinimumLength { get; set; } = 8;

    /// <summary>
    /// Maximum password length.
    /// </summary>
    public int MaximumLength { get; set; } = 128;

    /// <summary>
    /// Require uppercase letters.
    /// </summary>
    public bool RequireUppercase { get; set; } = true;

    /// <summary>
    /// Require lowercase letters.
    /// </summary>
    public bool RequireLowercase { get; set; } = true;

    /// <summary>
    /// Require digits.
    /// </summary>
    public bool RequireDigits { get; set; } = true;

    /// <summary>
    /// Require special characters.
    /// </summary>
    public bool RequireSpecialCharacters { get; set; } = true;

    /// <summary>
    /// Special characters to require.
    /// </summary>
    public string SpecialCharacters { get; set; } = "!@#$%^&*()_+-=[]{}|;:,.<>?";

    /// <summary>
    /// Maximum password age in days.
    /// </summary>
    public int? MaxPasswordAge { get; set; } = 90;

    /// <summary>
    /// Number of previous passwords to remember.
    /// </summary>
    public int PasswordHistory { get; set; } = 5;
}

/// <summary>
/// Implementation of password service using Argon2.
/// </summary>
public class PasswordService : IPasswordService
{
    private readonly PasswordPolicy _passwordPolicy;
    private readonly ILogger<PasswordService> _logger;

    public PasswordService(IConfiguration configuration, ILogger<PasswordService> logger)
    {
        _logger = logger;
        _passwordPolicy = configuration.GetSection("Authentication:PasswordPolicy").Get<PasswordPolicy>() ?? new PasswordPolicy();
    }

    public (string Hash, string Salt) HashPassword(string password)
    {
        if (string.IsNullOrEmpty(password))
            throw new ArgumentException("Password cannot be null or empty", nameof(password));

        // Generate a random salt
        var salt = GenerateSalt();

        // Hash the password with Argon2
        using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
        {
            Salt = Convert.FromBase64String(salt),
            DegreeOfParallelism = 8, // Number of threads
            Iterations = 4,          // Number of iterations
            MemorySize = 1024 * 128  // 128 MB
        };

        var hash = Convert.ToBase64String(argon2.GetBytes(64)); // 64 byte hash

        _logger.LogDebug("Password hashed successfully");

        return (hash, salt);
    }

    public bool VerifyPassword(string password, string hash, string salt)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hash) || string.IsNullOrEmpty(salt))
            return false;

        try
        {
            // Hash the input password with the stored salt
            using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
            {
                Salt = Convert.FromBase64String(salt),
                DegreeOfParallelism = 8,
                Iterations = 4,
                MemorySize = 1024 * 128
            };

            var computedHash = Convert.ToBase64String(argon2.GetBytes(64));

            // Compare the hashes
            return hash == computedHash;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying password");
            return false;
        }
    }

    public PasswordValidationResult ValidatePassword(string password)
    {
        var result = new PasswordValidationResult();

        if (string.IsNullOrEmpty(password))
        {
            result.Errors.Add("Password is required.");
            return result;
        }

        // Length validation
        if (password.Length < _passwordPolicy.MinimumLength)
        {
            result.Errors.Add($"Password must be at least {_passwordPolicy.MinimumLength} characters long.");
        }

        if (password.Length > _passwordPolicy.MaximumLength)
        {
            result.Errors.Add($"Password cannot exceed {_passwordPolicy.MaximumLength} characters.");
        }

        // Character requirements
        if (_passwordPolicy.RequireUppercase && !password.Any(char.IsUpper))
        {
            result.Errors.Add("Password must contain at least one uppercase letter.");
        }

        if (_passwordPolicy.RequireLowercase && !password.Any(char.IsLower))
        {
            result.Errors.Add("Password must contain at least one lowercase letter.");
        }

        if (_passwordPolicy.RequireDigits && !password.Any(char.IsDigit))
        {
            result.Errors.Add("Password must contain at least one digit.");
        }

        if (_passwordPolicy.RequireSpecialCharacters && 
            !password.Any(c => _passwordPolicy.SpecialCharacters.Contains(c)))
        {
            result.Errors.Add($"Password must contain at least one special character: {_passwordPolicy.SpecialCharacters}");
        }

        result.IsValid = result.Errors.Count == 0;
        return result;
    }

    private static string GenerateSalt()
    {
        var salt = new byte[32]; // 256-bit salt
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(salt);
        return Convert.ToBase64String(salt);
    }
}