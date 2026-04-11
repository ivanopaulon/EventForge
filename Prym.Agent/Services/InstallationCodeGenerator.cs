using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Prym.Agent.Configuration;

namespace Prym.Agent.Services;

/// <summary>
/// Generates and persists a unique InstallationCode on first startup.
/// Format: EF-{hostname8}-{yyyyMMddHHmmss}-{32hexrandom}
/// Example: EF-MYSERVER-20260406093045-a1b2c3d4e5f6789012345678901234ab
///
/// Properties:
/// - Human-readable prefix EF identifies EventForge installations at a glance
/// - Hostname fragment helps locate the machine during debugging
/// - Timestamp records when the agent was first activated
/// - 32 random hex chars (128 bits) ensure global uniqueness
/// Total length: ~58 chars — longer than a standard UUID (36), easier to trace
/// </summary>
public class InstallationCodeGenerator(
    AgentOptions options,
    ILogger<InstallationCodeGenerator> logger)
{
    private static readonly string AppSettingsPath =
        Path.Combine(AppContext.BaseDirectory, "appsettings.json");

    /// <summary>
    /// Ensures that <see cref="AgentOptions.InstallationCode"/> is populated.
    /// If not set (empty or null), generates a new code and persists it to appsettings.json.
    /// </summary>
    public void EnsureInstallationCode()
    {
        if (!string.IsNullOrWhiteSpace(options.InstallationCode))
            return;

        var code = GenerateCode();
        options.InstallationCode = code;
        PersistCode(code);

        logger.LogInformation("Generated new InstallationCode: {Code}", code);
    }

    private static string GenerateCode()
    {
        // Hostname: max 8 chars, uppercase, only alphanumeric
        var rawHost = Environment.MachineName ?? "UNKNOWN";
        var host = new string(rawHost
            .ToUpperInvariant()
            .Where(char.IsLetterOrDigit)
            .Take(8)
            .ToArray());
        if (host.Length == 0) host = "AGENT";

        // Timestamp: yyyyMMddHHmmss (UTC)
        var ts = DateTime.UtcNow.ToString("yyyyMMddHHmmss");

        // 16 random bytes → 32 hex chars (128 bits of entropy)
        var randomBytes = RandomNumberGenerator.GetBytes(16);
        var randomHex = Convert.ToHexString(randomBytes).ToLower();

        return $"EF-{host}-{ts}-{randomHex}";
    }

    private void PersistCode(string code)
    {
        try
        {
            JsonNode root;
            if (File.Exists(AppSettingsPath))
            {
                var text = File.ReadAllText(AppSettingsPath);
                root = JsonNode.Parse(text) ?? new JsonObject();
            }
            else
            {
                root = new JsonObject();
            }

            var section = root[AgentOptions.SectionName] as JsonObject ?? new JsonObject();
            section["InstallationCode"] = code;
            root[AgentOptions.SectionName] = section;

            var writeOptions = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(AppSettingsPath, root.ToJsonString(writeOptions));
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not persist InstallationCode to appsettings.json. Code is active in-memory.");
        }
    }
}
