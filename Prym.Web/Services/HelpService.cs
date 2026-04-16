using Microsoft.JSInterop;

namespace Prym.Web.Services;

/// <summary>
/// Interface for help and tutorial service.
/// </summary>
public interface IHelpService
{
    /// <summary>
    /// Gets the user's onboarding progress.
    /// </summary>
    Task<Dictionary<string, bool>> GetOnboardingProgressAsync(CancellationToken ct = default);

    /// <summary>
    /// Sets a specific onboarding step as completed.
    /// </summary>
    Task SetOnboardingStepCompletedAsync(string stepId, CancellationToken ct = default);

    /// <summary>
    /// Checks if a specific onboarding step has been completed.
    /// </summary>
    Task<bool> IsOnboardingStepCompletedAsync(string stepId, CancellationToken ct = default);

    /// <summary>
    /// Starts an interactive walkthrough for a specific component.
    /// </summary>
    Task StartWalkthroughAsync(string componentId, CancellationToken ct = default);

    /// <summary>
    /// Resets onboarding progress (for testing or user preference).
    /// </summary>
    Task ResetOnboardingProgressAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets help content for a specific component.
    /// </summary>
    Task<string> GetHelpContentAsync(string componentId, string? section = null, CancellationToken ct = default);
}

/// <summary>
/// Service for managing help, tutorial, and onboarding features.
/// Provides functionality for interactive walkthroughs, onboarding progress tracking,
/// and accessibility-compliant help content delivery.
/// </summary>
public class HelpService(
    IJSRuntime jsRuntime,
    ITranslationService translationService,
    ILogger<HelpService> logger) : IHelpService
{
    private const string ONBOARDING_STORAGE_KEY = "eventforge_onboarding_progress";
    private const string HELP_PREFERENCE_KEY = "eventforge_help_preferences";

    /// <summary>
    /// Available onboarding steps for different components.
    /// </summary>
    private readonly Dictionary<string, List<string>> _onboardingSteps = new()
    {
        ["notifications"] = [
            "notifications_overview",
            "notifications_filter",
            "notifications_actions",
            "notifications_preferences"
        ],
        ["chat"] = [
            "chat_overview",
            "chat_messaging",
            "chat_channels",
            "chat_files"
        ],
        ["superadmin"] = [
            "superadmin_overview",
            "superadmin_users",
            "superadmin_configuration",
            "superadmin_audit"
        ],
        ["file_management"] = [
            "files_overview",
            "files_upload",
            "files_management",
            "files_sharing"
        ]
    };

    public async Task<Dictionary<string, bool>> GetOnboardingProgressAsync(CancellationToken ct = default)
    {
        try
        {
            var progressJson = await jsRuntime.InvokeAsync<string?>("localStorage.getItem", ONBOARDING_STORAGE_KEY);

            if (string.IsNullOrEmpty(progressJson))
            {
                return new Dictionary<string, bool>();
            }

            var progress = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, bool>>(progressJson);
            return progress ?? new Dictionary<string, bool>();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting onboarding progress");
            return new Dictionary<string, bool>();
        }
    }

    public async Task SetOnboardingStepCompletedAsync(string stepId, CancellationToken ct = default)
    {
        try
        {
            var progress = await GetOnboardingProgressAsync();
            progress[stepId] = true;

            var progressJson = System.Text.Json.JsonSerializer.Serialize(progress);
            await jsRuntime.InvokeVoidAsync("localStorage.setItem", ONBOARDING_STORAGE_KEY, progressJson);

            logger.LogDebug("Onboarding step {StepId} marked as completed", stepId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error setting onboarding step {StepId} as completed", stepId);
        }
    }

    public async Task<bool> IsOnboardingStepCompletedAsync(string stepId, CancellationToken ct = default)
    {
        try
        {
            var progress = await GetOnboardingProgressAsync();
            return progress.GetValueOrDefault(stepId, false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking onboarding step {StepId} completion", stepId);
            return false;
        }
    }

    public async Task StartWalkthroughAsync(string componentId, CancellationToken ct = default)
    {
        try
        {
            logger.LogDebug("Starting walkthrough for component {ComponentId}", componentId);

            // Check if we have steps for this component
            if (!_onboardingSteps.ContainsKey(componentId))
            {
                logger.LogWarning("No walkthrough steps defined for component {ComponentId}", componentId);
                return;
            }

            // Start the JavaScript-based walkthrough
            await jsRuntime.InvokeVoidAsync("startInteractiveWalkthrough", componentId, _onboardingSteps[componentId]);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error starting walkthrough for component {ComponentId}", componentId);
        }
    }

    public async Task ResetOnboardingProgressAsync(CancellationToken ct = default)
    {
        try
        {
            await jsRuntime.InvokeVoidAsync("localStorage.removeItem", ONBOARDING_STORAGE_KEY);
            logger.LogInformation("Onboarding progress reset successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error resetting onboarding progress");
        }
    }

    public async Task<string> GetHelpContentAsync(string componentId, string? section = null, CancellationToken ct = default)
    {
        try
        {
            var baseKey = $"help.{componentId}";
            var key = string.IsNullOrEmpty(section) ? $"{baseKey}.overview" : $"{baseKey}.{section}";

            var content = translationService.GetTranslation(key, $"Help content for {componentId}");

            // If no specific content found, return general help
            if (content.StartsWith("[") && content.EndsWith("]"))
            {
                var fallbackKey = $"help.general.{section ?? "overview"}";
                content = translationService.GetTranslation(fallbackKey, "Help content is being prepared for this section.");
            }

            return content;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting help content for {ComponentId}, section {Section}", componentId, section);
            return "Help content temporarily unavailable.";
        }
    }
}