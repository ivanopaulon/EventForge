using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace EventForge.Tests.E2E;

/// <summary>
/// Base test class for E2E tests with Playwright utilities
/// </summary>
[Parallelizable(ParallelScope.Self)]
public class PlaywrightSetup : PageTest
{
    protected string BaseUrl { get; private set; } = null!;
    protected string ApiUrl { get; private set; } = null!;

    [SetUp]
    public async Task Setup()
    {
        // Read base URL from environment variable or use default
        BaseUrl = Environment.GetEnvironmentVariable("E2E_BASE_URL") ?? "http://localhost:5050";
        ApiUrl = Environment.GetEnvironmentVariable("E2E_API_URL") ?? "http://localhost:5000";

        // Set default timeout
        Page.SetDefaultTimeout(30000); // 30 seconds
        Page.SetDefaultNavigationTimeout(30000);

        // Navigate to the base URL to ensure the app is loaded
        await Page.GotoAsync(BaseUrl);

        // Wait for initial page load
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    /// <summary>
    /// Navigate to a specific page and wait for it to load
    /// </summary>
    protected async Task NavigateToPageAsync(string path)
    {
        await Page.GotoAsync($"{BaseUrl}{path}");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    /// <summary>
    /// Measure page load performance
    /// </summary>
    protected async Task<double> MeasurePageLoadTimeAsync(string path)
    {
        var startTime = DateTime.UtcNow;
        await NavigateToPageAsync(path);
        var endTime = DateTime.UtcNow;
        return (endTime - startTime).TotalSeconds;
    }

    /// <summary>
    /// Wait for a selector to be visible with custom timeout
    /// </summary>
    protected async Task WaitForSelectorAsync(string selector, int timeoutMs = 10000)
    {
        await Page.WaitForSelectorAsync(selector, new PageWaitForSelectorOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = timeoutMs
        });
    }

    /// <summary>
    /// Take a screenshot on test failure
    /// </summary>
    [TearDown]
    public async Task TearDown()
    {
        if (TestContext.CurrentContext.Result.Outcome.Status == NUnit.Framework.Interfaces.TestStatus.Failed)
        {
            var screenshotPath = Path.Combine(
                TestContext.CurrentContext.WorkDirectory,
                "screenshots",
                $"{TestContext.CurrentContext.Test.Name}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.png"
            );

            Directory.CreateDirectory(Path.GetDirectoryName(screenshotPath)!);
            await Page.ScreenshotAsync(new PageScreenshotOptions { Path = screenshotPath, FullPage = true });
            TestContext.WriteLine($"Screenshot saved: {screenshotPath}");
        }
    }

    /// <summary>
    /// Check if an element exists on the page
    /// </summary>
    protected async Task<bool> ElementExistsAsync(string selector)
    {
        return await Page.Locator(selector).CountAsync() > 0;
    }

    /// <summary>
    /// Wait for API response
    /// </summary>
    protected async Task<IResponse> WaitForApiResponseAsync(string urlPattern)
    {
        return await Page.WaitForResponseAsync(urlPattern);
    }
}
