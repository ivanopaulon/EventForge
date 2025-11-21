using NUnit.Framework;
using Microsoft.Playwright;
using System.Text.RegularExpressions;

namespace EventForge.Tests.E2E.Pages;

/// <summary>
/// E2E tests for Business Party (Suppliers) Management page
/// </summary>
[TestFixture]
[Category("E2E")]
public class BusinessPartyPageTests : PlaywrightSetup
{
    private const string BusinessPartyPagePath = "/business/suppliers";

    [Test]
    [Category("Smoke")]
    [Description("Verify that the Business Party page loads successfully")]
    public async Task BusinessPartyPage_ShouldLoad_Successfully()
    {
        // Act
        await NavigateToPageAsync(BusinessPartyPagePath);

        // Assert
        await Expect(Page).ToHaveTitleAsync(new Regex(".*EventForge.*", RegexOptions.IgnoreCase));
        
        // Verify page loaded by checking for common elements
        var pageLoaded = await ElementExistsAsync("body") && 
                        await Page.Locator("body").IsVisibleAsync();
        Assert.That(pageLoaded, Is.True, "Business Party page should be visible");
    }

    [Test]
    [Category("Performance")]
    [Description("Verify Business Party page loads within 3 seconds")]
    public async Task BusinessPartyPage_ShouldLoad_WithinThreeSeconds()
    {
        // Act
        var loadTime = await MeasurePageLoadTimeAsync(BusinessPartyPagePath);

        // Assert
        Assert.That(loadTime, Is.LessThan(3.0), 
            $"Business Party page should load within 3 seconds, but took {loadTime:F2}s");
        
        TestContext.WriteLine($"Business Party page loaded in {loadTime:F2} seconds");
    }

    [Test]
    [Category("E2E")]
    [Description("Verify Business Party page contains main UI elements")]
    public async Task BusinessPartyPage_Should_ContainMainUIElements()
    {
        // Arrange
        await NavigateToPageAsync(BusinessPartyPagePath);

        // Act & Assert - Check for common Blazor/MudBlazor UI elements
        var mainContentExists = await ElementExistsAsync("main") || 
                               await ElementExistsAsync(".mud-main-content") ||
                               await ElementExistsAsync("[role='main']");
        
        Assert.That(mainContentExists, Is.True, "Main content area should exist");

        // Check that the page has rendered content
        var bodyText = await Page.Locator("body").InnerTextAsync();
        Assert.That(bodyText, Is.Not.Empty, "Page should have rendered content");
    }

    [Test]
    [Category("E2E")]
    [Description("Verify Business Party page handles page refresh correctly")]
    public async Task BusinessPartyPage_Should_HandlePageRefresh()
    {
        // Arrange
        await NavigateToPageAsync(BusinessPartyPagePath);
        var initialUrl = Page.Url;

        // Act
        await Page.ReloadAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert
        var reloadedUrl = Page.Url;
        Assert.That(reloadedUrl, Is.EqualTo(initialUrl), 
            "URL should remain the same after refresh");
        
        var pageStillVisible = await Page.Locator("body").IsVisibleAsync();
        Assert.That(pageStillVisible, Is.True, "Page should still be visible after refresh");
    }
}
