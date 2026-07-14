using System.Text;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

// FluentValidationGenerator
// One-off / repeatable tool that reads a manifest of DTO type -> source file (relative to repo root)
// and generates skeleton FluentValidation validator classes under EventForge.Server/Validators/,
// translating common DataAnnotations attributes into FluentValidation rules.
//
// Usage:
//   dotnet run --project scripts/FluentValidationGenerator -- <repoRoot> <manifestJsonPath>
//
// See scripts/FluentValidationGenerator/README.md for details.

// Attributes that are purely metadata/documentation/serialization/EF-concurrency related and
// carry no validation semantics: skip them silently instead of emitting a TODO-REVIEW comment.
var IgnoredNonValidationAttributes = new HashSet<string>(StringComparer.Ordinal)
{
    "Display", "DisplayAttribute",
    "Timestamp", "TimestampAttribute",
    "Obsolete", "ObsoleteAttribute",
    "JsonPropertyName", "JsonPropertyNameAttribute",
    "JsonIgnore", "JsonIgnoreAttribute",
    "JsonConverter", "JsonConverterAttribute",
    "DataType", "DataTypeAttribute",
    "Key", "KeyAttribute",
    "ScaffoldColumn", "ScaffoldColumnAttribute",
    "Editable", "EditableAttribute",
    "Column", "ColumnAttribute",
    "NotMapped", "NotMappedAttribute",
    "DefaultValue", "DefaultValueAttribute",
    "Description", "DescriptionAttribute",
};

if (args.Length < 2)
{
    Console.Error.WriteLine("Usage: dotnet run --project scripts/FluentValidationGenerator -- <repoRoot> <manifestJsonPath>");
    return 1;
}

var repoRoot = Path.GetFullPath(args[0]);
var manifestPath = Path.GetFullPath(args[1]);

if (!File.Exists(manifestPath))
{
    Console.Error.WriteLine($"Manifest file not found: {manifestPath}");
    return 1;
}

var manifestJson = File.ReadAllText(manifestPath);
var manifest = JsonSerializer.Deserialize<Dictionary<string, string>>(manifestJson)
    ?? new Dictionary<string, string>();

var byFile = manifest
    .GroupBy(kv => kv.Value)
    .ToDictionary(g => g.Key, g => g.Select(kv => kv.Key).ToList());

// Collect every enum type name declared under Prym.DTOs so [Required] on an enum property
// can be recognized and mapped to .IsInEnum() instead of being silently skipped.
var enumTypeNames = new HashSet<string>(StringComparer.Ordinal);
var dtosRoot = Path.Combine(repoRoot, "Prym.DTOs");
if (Directory.Exists(dtosRoot))
{
    foreach (var enumFile in Directory.EnumerateFiles(dtosRoot, "*.cs", SearchOption.AllDirectories))
    {
        var enumTree = CSharpSyntaxTree.ParseText(File.ReadAllText(enumFile));
        foreach (var enumDecl in enumTree.GetCompilationUnitRoot().DescendantNodes().OfType<EnumDeclarationSyntax>())
        {
            enumTypeNames.Add(enumDecl.Identifier.Text);
        }
    }
}

int generatedCount = 0;
var skippedNoRules = new List<string>();
var todoReviewLog = new List<string>();
var errors = new List<string>();

foreach (var (relativeFilePath, typeNames) in byFile)
{
    var fullPath = Path.Combine(repoRoot, relativeFilePath);
    if (!File.Exists(fullPath))
    {
        errors.Add($"File non trovato: {relativeFilePath}");
        continue;
    }

    var text = File.ReadAllText(fullPath);
    var tree = CSharpSyntaxTree.ParseText(text);
    var root = tree.GetCompilationUnitRoot();

    foreach (var typeName in typeNames)
    {
        var typeDecl = root.DescendantNodes()
            .OfType<TypeDeclarationSyntax>()
            .FirstOrDefault(t => t.Identifier.Text == typeName &&
                                  (t is ClassDeclarationSyntax || t is RecordDeclarationSyntax));

        if (typeDecl is null)
        {
            errors.Add($"Tipo '{typeName}' non trovato in {relativeFilePath}");
            continue;
        }

        var dtoNamespace = GetNamespace(typeDecl);
        var subFolder = GetSubFolderFromDtoNamespace(dtoNamespace);

        var properties = typeDecl.Members.OfType<PropertyDeclarationSyntax>()
            .Where(p => p.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword)))
            .ToList();

        var ruleBlocks = new List<string>();
        var todoLines = new List<string>();
        bool anyMappedRule = false;

        foreach (var prop in properties)
        {
            var propName = prop.Identifier.Text;
            var propTypeText = prop.Type.ToString();
            var attributes = prop.AttributeLists.SelectMany(al => al.Attributes).ToList();

            var rules = new List<(string Rule, string Message)>();
            var unmapped = new List<string>();
            var extraChainCalls = new List<string>();

            foreach (var attr in attributes)
            {
                var name = attr.Name.ToString();
                var args2 = attr.ArgumentList?.Arguments ?? default;

                switch (name)
                {
                    case "Required":
                        if (IsStringType(propTypeText))
                        {
                            rules.Add((".NotEmpty()", $"Il campo {propName} è obbligatorio."));
                        }
                        else if (IsNullableType(propTypeText))
                        {
                            rules.Add((".NotEmpty()", $"Il campo {propName} è obbligatorio."));
                        }
                        else if (enumTypeNames.Contains(propTypeText.Trim()))
                        {
                            // [Required] on an enum property validates nothing on its own (a non-nullable
                            // enum can never be null); .IsInEnum() is the real, useful check here: it
                            // catches an out-of-range integer silently cast to the enum by the JSON binder.
                            rules.Add((".IsInEnum()", $"Il campo {propName} deve essere un valore valido."));
                        }
                        else if (!IsCollectionOrDictionaryType(propTypeText) && !IsKnownScalarValueType(propTypeText))
                        {
                            // Non-scalar, non-collection reference type (a nested complex DTO/class):
                            // a real null check is possible and useful here, unlike for a plain enum.
                            rules.Add((".NotNull()", $"Il campo {propName} è obbligatorio."));
                            var nestedTypeName = propTypeText.Trim().TrimEnd('?');
                            if (manifest.ContainsKey(nestedTypeName))
                            {
                                // Compose validation with the nested type's own generated validator
                                // instead of duplicating its rules here.
                                extraChainCalls.Add($".SetValidator(new {nestedTypeName}Validator())");
                            }
                        }
                        // Non-nullable scalar value types already guarantee presence: no rule needed.
                        break;

                    case "MaxLength":
                    case "StringLength":
                    {
                        var positional = args2.Where(a => a.NameEquals is null).ToList();
                        var minNamed = args2.FirstOrDefault(a => a.NameEquals?.Name.Identifier.Text == "MinimumLength");
                        if (positional.Count > 0 && int.TryParse(positional[0].Expression.ToString(), out var max))
                        {
                            if (IsCollectionType(propTypeText))
                            {
                                rules.Add(($".Must(v => v == null || v.Count <= {max})", $"Il campo {propName} non può contenere più di {max} elemento/i."));
                                if (minNamed is not null && int.TryParse(minNamed.Expression.ToString(), out var minCol))
                                {
                                    rules.Add(($".Must(v => v != null && v.Count >= {minCol})", $"Il campo {propName} deve contenere almeno {minCol} elemento/i."));
                                }
                            }
                            else if (minNamed is not null && int.TryParse(minNamed.Expression.ToString(), out var min))
                            {
                                rules.Add(($".MinimumLength({min})", $"Il campo {propName} deve contenere almeno {min} caratteri."));
                                rules.Add(($".MaximumLength({max})", $"Il campo {propName} non può superare {max} caratteri."));
                            }
                            else
                            {
                                rules.Add(($".MaximumLength({max})", $"Il campo {propName} non può superare {max} caratteri."));
                            }
                        }
                        else
                        {
                            unmapped.Add(name);
                        }
                        break;
                    }

                    case "MinLength":
                    {
                        var positional = args2.Where(a => a.NameEquals is null).ToList();
                        if (positional.Count > 0 && int.TryParse(positional[0].Expression.ToString(), out var min))
                        {
                            if (IsCollectionType(propTypeText))
                            {
                                rules.Add(($".Must(v => v != null && v.Count >= {min})", $"Il campo {propName} deve contenere almeno {min} elemento/i."));
                            }
                            else
                            {
                                rules.Add(($".MinimumLength({min})", $"Il campo {propName} deve contenere almeno {min} caratteri."));
                            }
                        }
                        else
                        {
                            unmapped.Add(name);
                        }
                        break;
                    }

                    case "Range":
                    {
                        var positional = args2.Where(a => a.NameEquals is null).ToList();
                        if (positional.Count >= 2)
                        {
                            var min = positional[0].Expression.ToString();
                            var max = positional[1].Expression.ToString();
                            var numericType = GetUnderlyingNumericTypeName(propTypeText);
                            if (numericType is not null)
                            {
                                min = AdaptNumericLiteral(min, numericType);
                                max = AdaptNumericLiteral(max, numericType);
                                rules.Add(($".InclusiveBetween(({numericType}){min}, ({numericType}){max})", $"Il campo {propName} deve essere compreso tra {min} e {max}."));
                            }
                            else
                            {
                                rules.Add(($".InclusiveBetween({min}, {max})", $"Il campo {propName} deve essere compreso tra {min} e {max}."));
                            }
                        }
                        else
                        {
                            unmapped.Add(name);
                        }
                        break;
                    }

                    case "EmailAddress":
                        rules.Add((".EmailAddress()", $"Il campo {propName} deve contenere un indirizzo email valido."));
                        break;

                    case "RegularExpression":
                    {
                        var positional = args2.Where(a => a.NameEquals is null).ToList();
                        if (positional.Count > 0)
                        {
                            var pattern = positional[0].Expression.ToString();
                            if (!pattern.TrimStart().StartsWith("\"", StringComparison.Ordinal) &&
                                !pattern.TrimStart().StartsWith("@\"", StringComparison.Ordinal))
                            {
                                // Reference to a const field/property declared on the DTO itself (e.g. a shared pattern constant).
                                pattern = $"{dtoNamespace}.{typeName}.{pattern}";
                            }
                            rules.Add(($".Matches({pattern})", $"Il campo {propName} non è nel formato corretto."));
                        }
                        else
                        {
                            unmapped.Add(name);
                        }
                        break;
                    }

                    case "Compare":
                    {
                        var positional = args2.Where(a => a.NameEquals is null).ToList();
                        if (positional.Count > 0)
                        {
                            var otherExpr = positional[0].Expression.ToString();
                            var otherName = ExtractNameOfArgument(otherExpr);
                            if (otherName is not null)
                            {
                                rules.Add(($".Equal(x => x.{otherName})", $"Il campo {propName} deve corrispondere a {otherName}."));
                            }
                            else
                            {
                                unmapped.Add(name);
                            }
                        }
                        else
                        {
                            unmapped.Add(name);
                        }
                        break;
                    }

                    default:
                        if (!IgnoredNonValidationAttributes.Contains(name))
                        {
                            unmapped.Add(name);
                        }
                        break;
                }
            }

            if (rules.Count > 0)
            {
                anyMappedRule = true;
                var sb = new StringBuilder();
                sb.Append($"        RuleFor(x => x.{propName})");
                foreach (var (rule, message) in rules)
                {
                    sb.Append('\n').Append($"            {rule}");
                    sb.Append('\n').Append($"            .WithMessage(\"{message}\")");
                }
                foreach (var extraChainCall in extraChainCalls)
                {
                    sb.Append('\n').Append($"            {extraChainCall}");
                }
                sb.Append(';');
                if (unmapped.Count > 0)
                {
                    var todoComment = $"        // TODO-REVIEW: attributo/i non mappato/i automaticamente: [{string.Join("], [", unmapped)}]";
                    ruleBlocks.Add(todoComment + "\n" + sb);
                    foreach (var u in unmapped)
                    {
                        todoLines.Add($"{typeName}.{propName}: [{u}]");
                    }
                }
                else
                {
                    ruleBlocks.Add(sb.ToString());
                }
            }
            else if (unmapped.Count > 0)
            {
                var todoComment = $"        // TODO-REVIEW: attributo/i non mappato/i automaticamente: [{string.Join("], [", unmapped)}] (proprietà: {propName})";
                ruleBlocks.Add(todoComment);
                foreach (var u in unmapped)
                {
                    todoLines.Add($"{typeName}.{propName}: [{u}]");
                }
            }
        }

        if (!anyMappedRule)
        {
            skippedNoRules.Add(typeName);
            continue;
        }

        todoReviewLog.AddRange(todoLines);

        var outputDir = Path.Combine(repoRoot, "EventForge.Server", "Validators", subFolder.Replace('.', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(outputDir);
        var outputPath = Path.Combine(outputDir, $"{typeName}Validator.cs");

        var validatorNamespace = string.IsNullOrEmpty(subFolder)
            ? "EventForge.Server.Validators"
            : $"EventForge.Server.Validators.{subFolder}";

        var fileContent = new StringBuilder();
        fileContent.AppendLine("using FluentValidation;");
        fileContent.AppendLine();
        fileContent.AppendLine($"namespace {validatorNamespace};");
        fileContent.AppendLine();
        fileContent.AppendLine("/// <summary>");
        fileContent.AppendLine($"/// FluentValidation validator for {typeName}.");
        fileContent.AppendLine("/// Auto-generated by scripts/FluentValidationGenerator; TODO-REVIEW comments require manual follow-up.");
        fileContent.AppendLine("/// </summary>");
        fileContent.AppendLine($"public class {typeName}Validator : AbstractValidator<{dtoNamespace}.{typeName}>");
        fileContent.AppendLine("{");
        fileContent.AppendLine($"    public {typeName}Validator()");
        fileContent.AppendLine("    {");
        fileContent.AppendLine(string.Join("\n\n", ruleBlocks));
        fileContent.AppendLine("    }");
        fileContent.AppendLine("}");

        File.WriteAllText(outputPath, fileContent.ToString());
        generatedCount++;
        Console.WriteLine($"Generato: {outputPath}");
    }
}

Console.WriteLine();
Console.WriteLine("=== Riepilogo generazione ===");
Console.WriteLine($"Validator generati: {generatedCount}");
Console.WriteLine($"DTO senza regole di validazione rilevate (saltati): {skippedNoRules.Count}");
foreach (var s in skippedNoRules)
{
    Console.WriteLine($"  - DTO senza regole di validazione rilevate, verificare se necessita validazione manuale: {s}");
}
Console.WriteLine($"TODO-REVIEW inseriti: {todoReviewLog.Count}");
foreach (var t in todoReviewLog)
{
    Console.WriteLine($"  - {t}");
}
if (errors.Count > 0)
{
    Console.WriteLine($"Errori: {errors.Count}");
    foreach (var e in errors)
    {
        Console.WriteLine($"  - {e}");
    }
}

return 0;

static string GetNamespace(SyntaxNode node)
{
    var ns = node.Ancestors().OfType<BaseNamespaceDeclarationSyntax>().FirstOrDefault();
    return ns?.Name.ToString() ?? string.Empty;
}

static string GetSubFolderFromDtoNamespace(string dtoNamespace)
{
    const string prefix = "Prym.DTOs";
    if (dtoNamespace.StartsWith(prefix + ".", StringComparison.Ordinal))
    {
        return dtoNamespace.Substring(prefix.Length + 1);
    }
    if (dtoNamespace == prefix)
    {
        return string.Empty;
    }
    return dtoNamespace;
}

static bool IsStringType(string typeText)
{
    var t = typeText.Trim();
    return t == "string" || t == "string?" || t == "String" || t == "String?";
}

static bool IsCollectionType(string typeText)
{
    var t = typeText.Trim().TrimEnd('?');
    return t.EndsWith("[]", StringComparison.Ordinal)
        || t.StartsWith("List<", StringComparison.Ordinal)
        || t.StartsWith("IList<", StringComparison.Ordinal)
        || t.StartsWith("ICollection<", StringComparison.Ordinal)
        || t.StartsWith("IEnumerable<", StringComparison.Ordinal)
        || t.StartsWith("HashSet<", StringComparison.Ordinal)
        || t.StartsWith("IReadOnlyList<", StringComparison.Ordinal)
        || t.StartsWith("IReadOnlyCollection<", StringComparison.Ordinal);
}

static bool IsCollectionOrDictionaryType(string typeText)
{
    if (IsCollectionType(typeText))
    {
        return true;
    }
    var t = typeText.Trim().TrimEnd('?');
    return t.StartsWith("Dictionary<", StringComparison.Ordinal)
        || t.StartsWith("IDictionary<", StringComparison.Ordinal)
        || t.StartsWith("IReadOnlyDictionary<", StringComparison.Ordinal);
}

static bool IsKnownScalarValueType(string typeText)
{
    // Well-known non-nullable BCL value types: [Required] on these is already a no-op (the type
    // itself guarantees non-null), so they must not be misclassified as a "complex nested class".
    var t = typeText.Trim().TrimEnd('?');
    return t switch
    {
        "bool" or "Boolean" => true,
        "byte" or "Byte" => true,
        "sbyte" or "SByte" => true,
        "short" or "Int16" => true,
        "ushort" or "UInt16" => true,
        "int" or "Int32" => true,
        "uint" or "UInt32" => true,
        "long" or "Int64" => true,
        "ulong" or "UInt64" => true,
        "float" or "Single" => true,
        "double" or "Double" => true,
        "decimal" or "Decimal" => true,
        "char" or "Char" => true,
        "DateTime" => true,
        "DateTimeOffset" => true,
        "TimeSpan" => true,
        "DateOnly" => true,
        "TimeOnly" => true,
        "Guid" => true,
        _ => false,
    };
}

static string? GetUnderlyingNumericTypeName(string typeText)
{
    var t = typeText.Trim().TrimEnd('?');
    return t switch
    {
        "decimal" or "Decimal" => "decimal",
        "double" or "Double" => "double",
        "float" or "Single" => "float",
        "int" or "Int32" => "int",
        "long" or "Int64" => "long",
        "short" or "Int16" => "short",
        "byte" or "Byte" => "byte",
        _ => null,
    };
}

static string AdaptNumericLiteral(string literal, string targetType)
{
    // DataAnnotations' [Range] often uses double bounds (e.g. double.MaxValue / int.MaxValue)
    // that do not convert to decimal/other narrower types. Map well-known sentinel constants
    // to the equivalent constant of the target numeric type.
    var trimmed = literal.Trim();
    return (trimmed, targetType) switch
    {
        ("double.MaxValue", "decimal") => "decimal.MaxValue",
        ("double.MinValue", "decimal") => "decimal.MinValue",
        ("double.MaxValue", not "double") => $"{targetType}.MaxValue",
        ("double.MinValue", not "double") => $"{targetType}.MinValue",
        ("int.MaxValue", "decimal") => "decimal.MaxValue",
        ("int.MinValue", "decimal") => "decimal.MinValue",
        _ => trimmed,
    };
}

static bool IsNullableType(string typeText)
{
    return typeText.Trim().EndsWith("?", StringComparison.Ordinal);
}

static string? ExtractNameOfArgument(string expr)
{
    // Handles nameof(Other) or "Other"
    var trimmed = expr.Trim();
    if (trimmed.StartsWith("nameof(", StringComparison.Ordinal) && trimmed.EndsWith(")", StringComparison.Ordinal))
    {
        return trimmed.Substring("nameof(".Length, trimmed.Length - "nameof(".Length - 1).Trim();
    }
    if (trimmed.StartsWith("\"", StringComparison.Ordinal) && trimmed.EndsWith("\"", StringComparison.Ordinal))
    {
        return trimmed.Trim('"');
    }
    return null;
}
