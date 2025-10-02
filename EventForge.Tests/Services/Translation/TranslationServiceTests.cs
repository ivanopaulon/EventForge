using System.Text.Json;

namespace EventForge.Tests.Services.Translation;

/// <summary>
/// Tests to verify translation files are valid and contain expected keys.
/// These tests ensure that translation files are properly formatted and contain all required keys.
/// </summary>
[Trait("Category", "Integration")]
public class TranslationFileTests
{
    private readonly string _clientWwwRootPath;

    public TranslationFileTests()
    {
        // Navigate to the Client/wwwroot directory from the Tests directory
        _clientWwwRootPath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "..", "..", "..", "..",
            "EventForge.Client", "wwwroot"
        );
    }

    [Theory]
    [InlineData("it.json")]
    [InlineData("en.json")]
    public void TranslationFile_ShouldBeValidJson(string fileName)
    {
        // Arrange
        var filePath = Path.Combine(_clientWwwRootPath, "i18n", fileName);

        // Act & Assert
        Assert.True(File.Exists(filePath), $"Translation file '{fileName}' does not exist at {filePath}");

        var jsonContent = File.ReadAllText(filePath);
        var exception = Record.Exception(() => JsonDocument.Parse(jsonContent));

        Assert.Null(exception);
    }

    [Theory]
    [InlineData("it.json", "health.openDialog")]
    [InlineData("it.json", "health.status")]
    [InlineData("it.json", "accessibility.skipToContent")]
    [InlineData("it.json", "navigation.openMenu")]
    [InlineData("it.json", "navigation.home")]
    [InlineData("it.json", "auth.login")]
    [InlineData("it.json", "auth.loginDescription")]
    public void ItalianTranslationFile_ShouldContainKey(string fileName, string key)
    {
        // Arrange
        var filePath = Path.Combine(_clientWwwRootPath, "i18n", fileName);
        var jsonContent = File.ReadAllText(filePath);
        using var document = JsonDocument.Parse(jsonContent);

        // Act
        var found = FindKeyInJson(document.RootElement, key);

        // Assert
        Assert.True(found, $"Key '{key}' not found in {fileName}");
    }

    [Theory]
    [InlineData("en.json", "health.openDialog")]
    [InlineData("en.json", "health.status")]
    [InlineData("en.json", "accessibility.skipToContent")]
    [InlineData("en.json", "navigation.openMenu")]
    [InlineData("en.json", "navigation.home")]
    [InlineData("en.json", "auth.login")]
    [InlineData("en.json", "auth.loginDescription")]
    public void EnglishTranslationFile_ShouldContainKey(string fileName, string key)
    {
        // Arrange
        var filePath = Path.Combine(_clientWwwRootPath, "i18n", fileName);
        var jsonContent = File.ReadAllText(filePath);
        using var document = JsonDocument.Parse(jsonContent);

        // Act
        var found = FindKeyInJson(document.RootElement, key);

        // Assert
        Assert.True(found, $"Key '{key}' not found in {fileName}");
    }

    [Theory]
    [InlineData("it.json", "health.openDialog", "Visualizza stato integrità del sistema")]
    [InlineData("it.json", "health.status", "Stato")]
    [InlineData("it.json", "accessibility.skipToContent", "Salta al contenuto principale")]
    [InlineData("it.json", "navigation.openMenu", "Apri menu di navigazione")]
    [InlineData("it.json", "auth.loginDescription", "Accedi al sistema")]
    public void ItalianTranslation_ShouldHaveCorrectValue(string fileName, string key, string expectedValue)
    {
        // Arrange
        var filePath = Path.Combine(_clientWwwRootPath, "i18n", fileName);
        var jsonContent = File.ReadAllText(filePath);
        using var document = JsonDocument.Parse(jsonContent);

        // Act
        var value = GetValueFromJson(document.RootElement, key);

        // Assert
        Assert.NotNull(value);
        Assert.Equal(expectedValue, value);
    }

    [Theory]
    [InlineData("en.json", "health.openDialog", "Open status dialog")]
    [InlineData("en.json", "health.status", "Status")]
    [InlineData("en.json", "accessibility.skipToContent", "Skip to main content")]
    [InlineData("en.json", "navigation.openMenu", "Open navigation menu")]
    [InlineData("en.json", "auth.loginDescription", "Login to system")]
    public void EnglishTranslation_ShouldHaveCorrectValue(string fileName, string key, string expectedValue)
    {
        // Arrange
        var filePath = Path.Combine(_clientWwwRootPath, "i18n", fileName);
        var jsonContent = File.ReadAllText(filePath);
        using var document = JsonDocument.Parse(jsonContent);

        // Act
        var value = GetValueFromJson(document.RootElement, key);

        // Assert
        Assert.NotNull(value);
        Assert.Equal(expectedValue, value);
    }

    [Theory]
    [InlineData("it.json")]
    [InlineData("en.json")]
    public void TranslationFile_ShouldHaveTopLevelSections(string fileName)
    {
        // Arrange
        var filePath = Path.Combine(_clientWwwRootPath, "i18n", fileName);
        var jsonContent = File.ReadAllText(filePath);
        using var document = JsonDocument.Parse(jsonContent);
        var root = document.RootElement;

        // Expected top-level sections
        var expectedSections = new[] { "common", "navigation", "auth", "health", "accessibility" };

        // Act & Assert
        foreach (var section in expectedSections)
        {
            Assert.True(root.TryGetProperty(section, out _), $"Section '{section}' not found in {fileName}");
        }
    }

    private bool FindKeyInJson(JsonElement element, string key)
    {
        var keys = key.Split('.');
        JsonElement current = element;

        foreach (var k in keys)
        {
            if (current.TryGetProperty(k, out var nextElement))
            {
                current = nextElement;
            }
            else
            {
                return false;
            }
        }

        return true;
    }

    private string? GetValueFromJson(JsonElement element, string key)
    {
        var keys = key.Split('.');
        JsonElement current = element;

        foreach (var k in keys)
        {
            if (current.TryGetProperty(k, out var nextElement))
            {
                current = nextElement;
            }
            else
            {
                return null;
            }
        }

        return current.ValueKind == JsonValueKind.String ? current.GetString() : null;
    }

    /// <summary>
    /// Tests to verify newly added navigation keys are present in translation files.
    /// These keys were added to fix missing translation warnings at client startup.
    /// </summary>
    [Theory]
    [InlineData("it.json", "nav.chat")]
    [InlineData("it.json", "nav.superAdmin")]
    [InlineData("it.json", "nav.administration")]
    [InlineData("it.json", "nav.help")]
    [InlineData("it.json", "nav.notifications")]
    [InlineData("it.json", "nav.profile")]
    [InlineData("it.json", "tour.chat")]
    [InlineData("it.json", "tour.notifications")]
    [InlineData("it.json", "tour.superadmin")]
    [InlineData("it.json", "help.helpCenter")]
    [InlineData("it.json", "help.generalHelpTitle")]
    [InlineData("en.json", "nav.chat")]
    [InlineData("en.json", "nav.superAdmin")]
    [InlineData("en.json", "nav.administration")]
    [InlineData("en.json", "nav.help")]
    [InlineData("en.json", "nav.notifications")]
    [InlineData("en.json", "nav.profile")]
    [InlineData("en.json", "tour.chat")]
    [InlineData("en.json", "tour.notifications")]
    [InlineData("en.json", "tour.superadmin")]
    [InlineData("en.json", "help.helpCenter")]
    [InlineData("en.json", "help.generalHelpTitle")]
    public void NewlyAddedKeys_ShouldExistInTranslationFiles(string fileName, string key)
    {
        // Arrange
        var filePath = Path.Combine(_clientWwwRootPath, "i18n", fileName);
        var jsonContent = File.ReadAllText(filePath);
        using var document = JsonDocument.Parse(jsonContent);

        // Act
        var found = FindKeyInJson(document.RootElement, key);

        // Assert
        Assert.True(found, $"Newly added key '{key}' not found in {fileName}. This key was added to fix missing translation warnings.");
    }

    /// <summary>
    /// Tests to verify StorageLocationDrawer translation keys are present.
    /// These keys are required for the StorageLocationDrawer component.
    /// </summary>
    [Theory]
    [InlineData("it.json", "messages.warehouseRequired")]
    [InlineData("it.json", "messages.codeRequired")]
    [InlineData("it.json", "messages.loadWarehousesError")]
    [InlineData("en.json", "messages.warehouseRequired")]
    [InlineData("en.json", "messages.codeRequired")]
    [InlineData("en.json", "messages.loadWarehousesError")]
    public void StorageLocationDrawer_TranslationKeys_ShouldExist(string fileName, string key)
    {
        // Arrange
        var filePath = Path.Combine(_clientWwwRootPath, "i18n", fileName);
        var jsonContent = File.ReadAllText(filePath);
        using var document = JsonDocument.Parse(jsonContent);

        // Act
        var found = FindKeyInJson(document.RootElement, key);

        // Assert
        Assert.True(found, $"StorageLocationDrawer key '{key}' not found in {fileName}");
    }

    /// <summary>
    /// Tests to verify drawer component translation keys are present.
    /// These keys are required for the EntityDrawer and specific drawer components.
    /// </summary>
    [Theory]
    [InlineData("it.json", "drawer.title.modificaUM")]
    [InlineData("it.json", "drawer.title.visualizzaUM")]
    [InlineData("it.json", "drawer.title.creaEntita")]
    [InlineData("it.json", "drawer.title.modificaEntita")]
    [InlineData("it.json", "drawer.title.visualizzaEntita")]
    [InlineData("it.json", "drawer.button.crea")]
    [InlineData("it.json", "drawer.button.salva")]
    [InlineData("it.json", "drawer.button.annulla")]
    [InlineData("it.json", "drawer.button.chiudi")]
    [InlineData("it.json", "drawer.button.modifica")]
    [InlineData("en.json", "drawer.title.modificaUM")]
    [InlineData("en.json", "drawer.title.visualizzaUM")]
    [InlineData("en.json", "drawer.title.creaEntita")]
    [InlineData("en.json", "drawer.title.modificaEntita")]
    [InlineData("en.json", "drawer.title.visualizzaEntita")]
    [InlineData("en.json", "drawer.button.crea")]
    [InlineData("en.json", "drawer.button.salva")]
    [InlineData("en.json", "drawer.button.annulla")]
    [InlineData("en.json", "drawer.button.chiudi")]
    [InlineData("en.json", "drawer.button.modifica")]
    public void DrawerComponent_TranslationKeys_ShouldExist(string fileName, string key)
    {
        // Arrange
        var filePath = Path.Combine(_clientWwwRootPath, "i18n", fileName);
        var jsonContent = File.ReadAllText(filePath);
        using var document = JsonDocument.Parse(jsonContent);

        // Act
        var found = FindKeyInJson(document.RootElement, key);

        // Assert
        Assert.True(found, $"Drawer component key '{key}' not found in {fileName}");
    }
}
