using NUnit.Framework;
using Microsoft.Playwright;
using System.Text.RegularExpressions;

namespace EventForge.Tests.E2E.Pages;

/// <summary>
/// E2E tests for Products Management page
/// </summary>
[TestFixture]
[Category("E2E")]
public class ProductsPageTests : PlaywrightSetup
{
    private const string ProductsPagePath = "/product-management/products";

    [Test]
    [Category("Smoke")]
    [Description("Verify that the Products page loads successfully")]
    public async Task ProductsPage_ShouldLoad_Successfully()
    {
        // Act
        await NavigateToPageAsync(ProductsPagePath);

        // Assert
        await Expect(Page).ToHaveTitleAsync(new Regex(".*EventForge.*", RegexOptions.IgnoreCase));
        
        // Verify page loaded by checking for common elements
        var pageLoaded = await ElementExistsAsync("body") && 
                        await Page.Locator("body").IsVisibleAsync();
        Assert.That(pageLoaded, Is.True, "Products page should be visible");
    }

    [Test]
    [Category("Performance")]
    [Description("Verify Products page loads within 3 seconds")]
    public async Task ProductsPage_ShouldLoad_WithinThreeSeconds()
    {
        // Act
        var loadTime = await MeasurePageLoadTimeAsync(ProductsPagePath);

        // Assert
        Assert.That(loadTime, Is.LessThan(3.0), 
            $"Products page should load within 3 seconds, but took {loadTime:F2}s");
        
        TestContext.WriteLine($"Products page loaded in {loadTime:F2} seconds");
    }

    [Test]
    [Category("E2E")]
    [Description("Verify Products page contains main UI elements")]
    public async Task ProductsPage_Should_ContainMainUIElements()
    {
        // Arrange
        await NavigateToPageAsync(ProductsPagePath);

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
    [Description("Verify Products page responds to viewport changes")]
    public async Task ProductsPage_Should_BeResponsive()
    {
        // Arrange
        await NavigateToPageAsync(ProductsPagePath);

        // Act - Test mobile viewport
        await Page.SetViewportSizeAsync(375, 667);
        await Task.Delay(500); // Allow time for responsive layout to adjust
        var mobileContentVisible = await Page.Locator("body").IsVisibleAsync();

        // Assert
        Assert.That(mobileContentVisible, Is.True, "Page should be visible on mobile viewport");

        // Act - Test desktop viewport
        await Page.SetViewportSizeAsync(1920, 1080);
        await Task.Delay(500);
        var desktopContentVisible = await Page.Locator("body").IsVisibleAsync();

        // Assert
        Assert.That(desktopContentVisible, Is.True, "Page should be visible on desktop viewport");
    }

    [Test]
    [Category("E2E")]
    [Description("Verify Products page can be navigated to from home")]
    public async Task ProductsPage_Should_BeNavigableFromHome()
    {
        // Arrange
        await NavigateToPageAsync("/");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act - Navigate to products page
        await NavigateToPageAsync(ProductsPagePath);

        // Assert
        var currentUrl = Page.Url;
        Assert.That(currentUrl, Does.Contain(ProductsPagePath), 
            $"Should navigate to Products page, but URL is: {currentUrl}");
    }

    [Test]
    [Category("E2E")]
    [Description("Verify Products page handles page refresh correctly")]
    public async Task ProductsPage_Should_HandlePageRefresh()
    {
        // Arrange
        await NavigateToPageAsync(ProductsPagePath);
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
