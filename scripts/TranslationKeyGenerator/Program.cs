using System.CommandLine;
using System.Text.Json;

namespace TranslationKeyGenerator;

class Program
{
    static async Task<int> Main(string[] args)
    {
        var directoryOption = new Option<DirectoryInfo>(
            name: "--directory",
            description: "Directory containing translation JSON files",
            getDefaultValue: () => new DirectoryInfo("EventForge.Client/wwwroot/i18n"));

        var baseLanguageOption = new Option<string>(
            name: "--base-language",
            description: "Base language to use as source for keys",
            getDefaultValue: () => "en");

        var targetLanguageOption = new Option<string?>(
            name: "--target-language",
            description: "Specific target language to update (if not specified, updates all)");

        var dryRunOption = new Option<bool>(
            name: "--dry-run",
            description: "Show what would be changed without modifying files",
            getDefaultValue: () => false);

        var placeholderOption = new Option<string>(
            name: "--placeholder",
            description: "Placeholder text for missing translations",
            getDefaultValue: () => "[NEEDS TRANSLATION]");

        var rootCommand = new RootCommand("Translation Key Generator - Generates missing translation keys with placeholders")
        {
            directoryOption,
            baseLanguageOption,
            targetLanguageOption,
            dryRunOption,
            placeholderOption
        };

        rootCommand.SetHandler(
            (directory, baseLang, targetLang, dryRun, placeholder) =>
            {
                var exitCode = GenerateMissingKeys(directory, baseLang, targetLang, dryRun, placeholder);
                Environment.ExitCode = exitCode;
            },
            directoryOption,
            baseLanguageOption,
            targetLanguageOption,
            dryRunOption,
            placeholderOption);

        return await rootCommand.InvokeAsync(args);
    }

    static int GenerateMissingKeys(DirectoryInfo directory, string baseLanguage, string? targetLanguage, bool dryRun, string placeholder)
    {
        if (!directory.Exists)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"❌ Error: Directory not found: {directory.FullName}");
            Console.ResetColor();
            return 1;
        }

        try
        {
            // Find all JSON files
            var translationFiles = directory.GetFiles("*.json");

            if (translationFiles.Length == 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ Error: No JSON files found in {directory.FullName}");
                Console.ResetColor();
                return 1;
            }

            // Load base language
            var baseFile = translationFiles.FirstOrDefault(f => Path.GetFileNameWithoutExtension(f.Name) == baseLanguage);
            if (baseFile == null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ Error: Base language file '{baseLanguage}.json' not found");
                Console.ResetColor();
                return 1;
            }

            var baseTranslations = LoadTranslations(baseFile.FullName);
            var baseKeys = GetAllKeys(baseTranslations);

            Console.WriteLine();
            Console.WriteLine("═══════════════════════════════════════════════════════");
            Console.WriteLine($"  Translation Key Generator {(dryRun ? "(DRY RUN)" : "")}");
            Console.WriteLine("═══════════════════════════════════════════════════════");
            Console.WriteLine();
            Console.WriteLine($"  Base Language: {baseLanguage}");
            Console.WriteLine($"  Base Keys: {baseKeys.Count}");
            Console.WriteLine($"  Placeholder: {placeholder}");
            Console.WriteLine();

            var totalGenerated = 0;

            foreach (var file in translationFiles.OrderBy(f => f.Name))
            {
                var lang = Path.GetFileNameWithoutExtension(file.Name);

                // Skip base language
                if (lang == baseLanguage)
                    continue;

                // Skip if target language specified and doesn't match
                if (targetLanguage != null && lang != targetLanguage)
                    continue;

                var translations = LoadTranslations(file.FullName);
                var existingKeys = GetAllKeys(translations);
                var missingKeys = baseKeys.Except(existingKeys).OrderBy(k => k).ToList();

                if (missingKeys.Count > 0)
                {
                    Console.WriteLine($"  Language: {lang}");
                    Console.WriteLine($"    Missing Keys: {missingKeys.Count}");

                    if (dryRun)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"    [DRY RUN] Would add {missingKeys.Count} keys");
                        Console.ResetColor();

                        if (missingKeys.Count <= 10)
                        {
                            foreach (var key in missingKeys)
                            {
                                var baseValue = GetValueByKey(baseTranslations, key);
                                Console.WriteLine($"      + {key}: {placeholder} (base: {TruncateString(baseValue, 50)})");
                            }
                        }
                        else
                        {
                            foreach (var key in missingKeys.Take(10))
                            {
                                var baseValue = GetValueByKey(baseTranslations, key);
                                Console.WriteLine($"      + {key}: {placeholder} (base: {TruncateString(baseValue, 50)})");
                            }
                            Console.WriteLine($"      ... and {missingKeys.Count - 10} more");
                        }
                    }
                    else
                    {
                        // Add missing keys
                        foreach (var key in missingKeys)
                        {
                            var baseValue = GetValueByKey(baseTranslations, key);
                            SetValueByKey(translations, key, $"{placeholder} {baseValue}");
                        }

                        // Save updated file
                        SaveTranslations(file.FullName, translations);

                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"    ✓ Added {missingKeys.Count} keys");
                        Console.ResetColor();

                        totalGenerated += missingKeys.Count;
                    }

                    Console.WriteLine();
                }
                else
                {
                    Console.WriteLine($"  Language: {lang}");
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"    ✓ No missing keys");
                    Console.ResetColor();
                    Console.WriteLine();
                }
            }

            Console.WriteLine("═══════════════════════════════════════════════════════");
            Console.WriteLine();

            if (dryRun)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("✓ Dry run completed. No files were modified.");
                Console.ResetColor();
            }
            else if (totalGenerated > 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"✓ Successfully generated {totalGenerated} missing translation keys!");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("✓ All translation files are already complete!");
                Console.ResetColor();
            }

            return 0;
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"❌ Error: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            Console.ResetColor();
            return 1;
        }
    }

    static Dictionary<string, object> LoadTranslations(string filePath)
    {
        var jsonContent = File.ReadAllText(filePath);
        var result = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonContent);
        return result ?? new Dictionary<string, object>();
    }

    static void SaveTranslations(string filePath, Dictionary<string, object> translations)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
        var jsonContent = JsonSerializer.Serialize(translations, options);
        File.WriteAllText(filePath, jsonContent);
    }

    static HashSet<string> GetAllKeys(Dictionary<string, object> translations, string prefix = "")
    {
        var keys = new HashSet<string>();

        foreach (var kvp in translations)
        {
            var currentKey = string.IsNullOrEmpty(prefix) ? kvp.Key : $"{prefix}.{kvp.Key}";

            if (kvp.Value is JsonElement element)
            {
                if (element.ValueKind == JsonValueKind.Object)
                {
                    var nested = JsonSerializer.Deserialize<Dictionary<string, object>>(element.GetRawText());
                    if (nested != null)
                    {
                        var nestedKeys = GetAllKeys(nested, currentKey);
                        keys.UnionWith(nestedKeys);
                    }
                }
                else
                {
                    keys.Add(currentKey);
                }
            }
            else if (kvp.Value is Dictionary<string, object> dict)
            {
                var nestedKeys = GetAllKeys(dict, currentKey);
                keys.UnionWith(nestedKeys);
            }
            else
            {
                keys.Add(currentKey);
            }
        }

        return keys;
    }

    static string GetValueByKey(Dictionary<string, object> translations, string key)
    {
        var parts = key.Split('.');
        object? current = translations;

        foreach (var part in parts)
        {
            if (current is Dictionary<string, object> dict)
            {
                if (!dict.TryGetValue(part, out current))
                    return "";
            }
            else if (current is JsonElement element && element.ValueKind == JsonValueKind.Object)
            {
                if (element.TryGetProperty(part, out var property))
                {
                    current = property;
                }
                else
                {
                    return "";
                }
            }
            else
            {
                return "";
            }
        }

        if (current is JsonElement jsonElement)
        {
            return jsonElement.ValueKind == JsonValueKind.String ? jsonElement.GetString() ?? "" : jsonElement.GetRawText();
        }

        return current?.ToString() ?? "";
    }

    static void SetValueByKey(Dictionary<string, object> translations, string key, string value)
    {
        var parts = key.Split('.');
        Dictionary<string, object> current = translations;

        for (int i = 0; i < parts.Length - 1; i++)
        {
            var part = parts[i];

            if (!current.ContainsKey(part))
            {
                current[part] = new Dictionary<string, object>();
            }

            if (current[part] is JsonElement element && element.ValueKind == JsonValueKind.Object)
            {
                var nested = JsonSerializer.Deserialize<Dictionary<string, object>>(element.GetRawText());
                current[part] = nested ?? new Dictionary<string, object>();
            }

            current = (Dictionary<string, object>)current[part];
        }

        current[parts[^1]] = value;
    }

    static string TruncateString(string str, int maxLength)
    {
        if (string.IsNullOrEmpty(str))
            return "";

        if (str.Length <= maxLength)
            return str;

        return str.Substring(0, maxLength - 3) + "...";
    }
}
