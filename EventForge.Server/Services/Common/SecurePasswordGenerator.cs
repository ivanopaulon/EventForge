namespace EventForge.Server.Services.Common;

/// <summary>
/// Shared helper for generating cryptographically secure random passwords.
/// Centralizes the logic previously duplicated across services that need to
/// generate initial passwords (e.g. tenant admin bootstrap, tenant user creation).
/// </summary>
public static class SecurePasswordGenerator
{
    /// <summary>
    /// Generates a 12-character cryptographically secure random password containing
    /// at least one lowercase letter, one uppercase letter, one digit and one special character.
    /// </summary>
    /// <returns>Random password string.</returns>
    public static string GenerateRandomPassword()
    {
        const string lowercase = "abcdefghijklmnopqrstuvwxyz";
        const string uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string digits = "0123456789";
        const string special = "!@#$%^&*";

        var password = new char[12];

        // Ensure at least one of each required character type using cryptographically secure random
        password[0] = lowercase[System.Security.Cryptography.RandomNumberGenerator.GetInt32(lowercase.Length)];
        password[1] = uppercase[System.Security.Cryptography.RandomNumberGenerator.GetInt32(uppercase.Length)];
        password[2] = digits[System.Security.Cryptography.RandomNumberGenerator.GetInt32(digits.Length)];
        password[3] = special[System.Security.Cryptography.RandomNumberGenerator.GetInt32(special.Length)];

        // Fill the rest randomly
        var allChars = lowercase + uppercase + digits + special;
        for (int i = 4; i < password.Length; i++)
        {
            password[i] = allChars[System.Security.Cryptography.RandomNumberGenerator.GetInt32(allChars.Length)];
        }

        // Shuffle the password using Fisher-Yates algorithm with cryptographically secure random
        for (int i = password.Length - 1; i > 0; i--)
        {
            int j = System.Security.Cryptography.RandomNumberGenerator.GetInt32(i + 1);
            (password[i], password[j]) = (password[j], password[i]);
        }

        return new string(password);
    }
}
