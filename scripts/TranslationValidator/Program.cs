using System.CommandLine;
using System.Text.Json;

namespace TranslationValidator;

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
            description: "Base language file to compare against",
            getDefaultValue: () => "en");

        var outputOption = new Option<FileInfo?>(
            name: "--output",
            description: "Output file for missing keys report (JSON format)");

        var rootCommand = new RootCommand("Translation Validator - Ensures all translation files have the same keys")
        {
            directoryOption,
            baseLanguageOption,
            outputOption
        };

        rootCommand.SetHandler(
            (directory, baseLang, output) =>
            {
                var exitCode = ValidateTranslations(directory, baseLang, output);
                Environment.ExitCode = exitCode;
            },
            directoryOption,
            baseLanguageOption,
            outputOption);

        return await rootCommand.InvokeAsync(args);
    }

    static int ValidateTranslations(DirectoryInfo directory, string baseLanguage, FileInfo? outputFile)
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
            // Find all JSON files in the directory
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

            var baseKeys = LoadTranslationKeys(baseFile.FullName);
            var baseKeySet = baseKeys.ToHashSet();

            Console.WriteLine();
            Console.WriteLine("═══════════════════════════════════════════════════════");
            Console.WriteLine("  Translation Validation Report");
            Console.WriteLine("═══════════════════════════════════════════════════════");
            Console.WriteLine();
            Console.WriteLine($"  Base Language: {baseLanguage}");
            Console.WriteLine($"  Base Keys: {baseKeys.Count}");
            Console.WriteLine($"  Files Found: {translationFiles.Length}");
            Console.WriteLine();

            var allValid = true;
            var missingKeysReport = new Dictionary<string, MissingKeysData>();

            foreach (var file in translationFiles.OrderBy(f => f.Name))
            {
                var lang = Path.GetFileNameWithoutExtension(file.Name);
                var keys = LoadTranslationKeys(file.FullName);
                var keySet = keys.ToHashSet();

                var missingKeys = baseKeySet.Except(keySet).OrderBy(k => k).ToList();
                var extraKeys = keySet.Except(baseKeySet).OrderBy(k => k).ToList();

                Console.WriteLine($"  Language: {lang}");
                Console.WriteLine($"    Total Keys: {keys.Count}");

                if (missingKeys.Count > 0)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"    ✗ Missing Keys: {missingKeys.Count}");
                    Console.ResetColor();
                    allValid = false;

                    if (missingKeys.Count <= 10)
                    {
                        foreach (var key in missingKeys)
                        {
                            Console.WriteLine($"      - {key}");
                        }
                    }
                    else
                    {
                        foreach (var key in missingKeys.Take(10))
                        {
                            Console.WriteLine($"      - {key}");
                        }
                        Console.WriteLine($"      ... and {missingKeys.Count - 10} more");
                    }
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"    ✓ Missing Keys: 0");
                    Console.ResetColor();
                }

                if (extraKeys.Count > 0)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"    ⚠ Extra Keys: {extraKeys.Count}");
                    Console.ResetColor();
                    allValid = false;

                    if (extraKeys.Count <= 10)
                    {
                        foreach (var key in extraKeys)
                        {
                            Console.WriteLine($"      + {key}");
                        }
                    }
                    else
                    {
                        foreach (var key in extraKeys.Take(10))
                        {
                            Console.WriteLine($"      + {key}");
                        }
                        Console.WriteLine($"      ... and {extraKeys.Count - 10} more");
                    }
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"    ✓ Extra Keys: 0");
                    Console.ResetColor();
                }

                Console.WriteLine();

                // Add to report
                if (lang != baseLanguage && (missingKeys.Count > 0 || extraKeys.Count > 0))
                {
                    missingKeysReport[lang] = new MissingKeysData
                    {
                        Language = lang,
                        TotalKeys = keys.Count,
                        MissingKeys = missingKeys,
                        ExtraKeys = extraKeys
                    };
                }
            }

            Console.WriteLine("═══════════════════════════════════════════════════════");
            Console.WriteLine();

            // Write report if requested
            if (outputFile != null && missingKeysReport.Count > 0)
            {
                var reportJson = JsonSerializer.Serialize(missingKeysReport, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(outputFile.FullName, reportJson);
                Console.WriteLine($"Report written to: {outputFile.FullName}");
                Console.WriteLine();
            }

            if (allValid)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("✓ All translation files are complete and consistent!");
                Console.ResetColor();
                return 0;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("✗ Translation validation failed!");
                Console.WriteLine("  Some files have missing or extra keys.");
                Console.ResetColor();
                return 1;
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"❌ Error: {ex.Message}");
            Console.ResetColor();
            return 1;
        }
    }

    static List<string> LoadTranslationKeys(string filePath)
    {
        var jsonContent = File.ReadAllText(filePath);
        var jsonDoc = JsonDocument.Parse(jsonContent);
        var keys = new List<string>();
        ExtractKeys(jsonDoc.RootElement, "", keys);
        return keys;
    }

    static void ExtractKeys(JsonElement element, string prefix, List<string> keys)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                var newPrefix = string.IsNullOrEmpty(prefix) ? property.Name : $"{prefix}.{property.Name}";
                ExtractKeys(property.Value, newPrefix, keys);
            }
        }
        else
        {
            // This is a leaf node (actual translation value)
            if (!string.IsNullOrEmpty(prefix))
            {
                keys.Add(prefix);
            }
        }
    }
}

class MissingKeysData
{
    public string Language { get; set; } = "";
    public int TotalKeys { get; set; }
    public List<string> MissingKeys { get; set; } = new();
    public List<string> ExtraKeys { get; set; } = new();
}
