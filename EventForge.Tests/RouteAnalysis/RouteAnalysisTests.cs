using System.Text;
using System.Text.RegularExpressions;

namespace EventForge.Tests.RouteAnalysis;

/// <summary>
/// Route analysis functionality ported from RouteConflictAnalyzer.
/// Tests for detecting conflicts in HTTP route definitions across controllers.
/// </summary>
[Trait("Category", "RouteAnalysis")]
public class RouteAnalysisTests
{
    private readonly List<RouteInfo> _routes = new();
    private readonly List<RouteConflict> _conflicts = new();

    [Fact]
    public async Task AnalyzeRoutes_ShouldDetectConflictsAndGenerateReport()
    {
        // Arrange
        var controllersPath = Environment.GetEnvironmentVariable("CONTROLLERS_PATH") ?? "EventForge.Server/Controllers";
        var outputFile = Environment.GetEnvironmentVariable("OUTPUT_FILE") ?? "route_analysis_report.txt";

        // Find the solution root by walking up from current directory
        var currentDir = Directory.GetCurrentDirectory();
        var solutionRoot = currentDir;

        // Walk up until we find EventForge.sln or reach a reasonable limit
        for (int i = 0; i < 10; i++)
        {
            if (File.Exists(Path.Combine(solutionRoot, "EventForge.sln")))
                break;
            var parent = Directory.GetParent(solutionRoot);
            if (parent == null) break;
            solutionRoot = parent.FullName;
        }

        // Make paths absolute relative to solution root
        if (!Path.IsPathRooted(controllersPath))
        {
            controllersPath = Path.Combine(solutionRoot, controllersPath);
        }
        if (!Path.IsPathRooted(outputFile))
        {
            outputFile = Path.Combine(solutionRoot, outputFile);
        }

        // Act - Perform route analysis
        if (Directory.Exists(controllersPath))
        {
            Console.WriteLine("EventForge Route Conflict Analyzer");
            Console.WriteLine("===================================");
            Console.WriteLine();
            Console.WriteLine($"📂 Scansione cartella: {controllersPath}");
            Console.WriteLine($"📄 Report generato in: {outputFile}");
            Console.WriteLine();

            await AnalyzeControllersAsync(controllersPath);
            await GenerateReportAsync(outputFile);

            Console.WriteLine("✅ Analisi completata!");
            Console.WriteLine($"📊 Controller analizzati: {_routes.GroupBy(r => r.Controller).Count()}");
            Console.WriteLine($"🔍 Route totali trovate: {_routes.Count}");
            Console.WriteLine($"⚠️  Conflitti rilevati: {_conflicts.Count}");

            if (_conflicts.Any())
            {
                Console.WriteLine();
                Console.WriteLine("⚠️  ATTENZIONE: Conflitti di route rilevati!");
                Console.WriteLine("Consulta il report per i dettagli e le soluzioni suggerite.");
            }
        }
        else
        {
            Console.WriteLine($"❌ Cartella Controllers non trovata: {controllersPath}");
            Console.WriteLine("Utilizzare variabili d'ambiente CONTROLLERS_PATH e OUTPUT_FILE per specificare percorsi personalizzati.");
        }

        // Assert - Fail test if conflicts are found (so dotnet test exits non-zero)
        Assert.True(!_conflicts.Any(),
            $"Conflitti di route rilevati: {_conflicts.Count}. Consulta il file {outputFile} per i dettagli.");
    }

    private async Task AnalyzeControllersAsync(string controllersPath)
    {
        var controllerFiles = Directory.GetFiles(controllersPath, "*.cs");

        foreach (var file in controllerFiles)
        {
            Console.WriteLine($"   🔍 Analizzando: {Path.GetFileName(file)}");
            await AnalyzeControllerFileAsync(file);
        }

        Console.WriteLine();
        Console.WriteLine("🔍 Rilevamento conflitti...");
        DetectConflicts();
    }

    private async Task AnalyzeControllerFileAsync(string filePath)
    {
        var content = await File.ReadAllTextAsync(filePath);
        var fileName = Path.GetFileNameWithoutExtension(filePath);

        // Estrae il nome del controller
        var controllerMatch = Regex.Match(content, @"class\s+(\w+)\s*:\s*\w*Controller");
        var controllerName = controllerMatch.Success ? controllerMatch.Groups[1].Value : fileName;

        // Estrae la route base del controller
        var baseRouteMatch = Regex.Match(content, @"\[Route\s*\(\s*""([^""]+)""\s*\)\]");
        var baseRoute = baseRouteMatch.Success ? baseRouteMatch.Groups[1].Value : "";

        // Sostituisce [controller] con il nome effettivo
        if (baseRoute.Contains("[controller]"))
        {
            var controllerRouteValue = controllerName.EndsWith("Controller")
                ? controllerName[..^10] // Rimuove "Controller"
                : controllerName;
            baseRoute = baseRoute.Replace("[controller]", controllerRouteValue);
        }

        // Trova tutti i metodi HTTP con pattern migliorato ma sicuro
        var httpMethodPattern = @"\[Http(Get|Post|Put|Delete|Patch)(?:\s*\(\s*""([^""]*)""\s*\))?\][\s\S]*?public\s+[^{]*?\s+(\w+)\s*\(";
        var matches = Regex.Matches(content, httpMethodPattern, RegexOptions.IgnoreCase);

        foreach (Match match in matches)
        {
            var httpMethod = match.Groups[1].Value.ToUpper();
            var routeSuffix = match.Groups[2].Value;
            var methodName = match.Groups[3].Value;

            var fullRoute = CombineRoutes(baseRoute, routeSuffix);

            _routes.Add(new RouteInfo
            {
                Controller = controllerName,
                Method = methodName,
                HttpMethod = httpMethod,
                RouteTemplate = fullRoute,
                FilePath = filePath
            });
        }
    }

    private static string CombineRoutes(string baseRoute, string suffix)
    {
        if (string.IsNullOrEmpty(baseRoute) && string.IsNullOrEmpty(suffix))
            return "/";

        if (string.IsNullOrEmpty(suffix))
        {
            return baseRoute.StartsWith('/') ? baseRoute : "/" + baseRoute;
        }

        if (string.IsNullOrEmpty(baseRoute))
        {
            return suffix.StartsWith('/') ? suffix : "/" + suffix;
        }

        var combined = baseRoute.TrimEnd('/') + "/" + suffix.TrimStart('/');
        return combined.StartsWith('/') ? combined : "/" + combined;
    }

    private void DetectConflicts()
    {
        var routeGroups = _routes.GroupBy(r => new { r.HttpMethod, Route = NormalizeRoute(r.RouteTemplate) });

        foreach (var group in routeGroups.Where(g => g.Count() > 1))
        {
            var conflictingRoutes = group.ToList();
            _conflicts.Add(new RouteConflict
            {
                HttpMethod = group.Key.HttpMethod,
                RoutePattern = group.Key.Route,
                ConflictingRoutes = conflictingRoutes
            });
        }
    }

    private static string NormalizeRoute(string route)
    {
        // Normalizza i parametri di route per confronto
        // {id:guid} -> {id}
        // {id:int} -> {id}
        var normalized = Regex.Replace(route, @"\{([^:}]+):[^}]+\}", "{$1}");

        // Assicura che il percorso inizi sempre con /
        if (!normalized.StartsWith('/'))
        {
            normalized = "/" + normalized;
        }

        return normalized.ToLowerInvariant();
    }

    private async Task GenerateReportAsync(string outputPath)
    {
        var report = new StringBuilder();

        // Header del report
        _ = report.AppendLine("EVENTFORGE - ROUTE CONFLICT ANALYSIS REPORT");
        _ = report.AppendLine("==========================================");
        _ = report.AppendLine($"Data Generazione: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        _ = report.AppendLine($"Controller Analizzati: {_routes.GroupBy(r => r.Controller).Count()}");
        _ = report.AppendLine($"Route Totali: {_routes.Count}");
        _ = report.AppendLine($"Conflitti Rilevati: {_conflicts.Count}");
        _ = report.AppendLine();

        // Sezione mapping completo delle route
        _ = report.AppendLine("📋 MAPPING COMPLETO DELLE ROUTE");
        _ = report.AppendLine("==============================");
        _ = report.AppendLine();

        var groupedByController = _routes.GroupBy(r => r.Controller).OrderBy(g => g.Key);

        foreach (var controllerGroup in groupedByController)
        {
            _ = report.AppendLine($"Controller: {controllerGroup.Key}");
            _ = report.AppendLine(new string('-', 50));

            foreach (var route in controllerGroup.OrderBy(r => r.HttpMethod).ThenBy(r => r.RouteTemplate))
            {
                _ = report.AppendLine($"  {route.HttpMethod,-7} {route.RouteTemplate,-40} -> {route.Method}()");
            }
            _ = report.AppendLine();
        }

        // Sezione conflitti
        if (_conflicts.Any())
        {
            _ = report.AppendLine("⚠️  CONFLITTI RILEVATI");
            _ = report.AppendLine("=====================");
            _ = report.AppendLine();

            foreach (var conflict in _conflicts.OrderBy(c => c.HttpMethod).ThenBy(c => c.RoutePattern))
            {
                _ = report.AppendLine($"🚨 CONFLITTO: {conflict.HttpMethod} {conflict.RoutePattern}");
                _ = report.AppendLine(new string('-', 60));

                foreach (var route in conflict.ConflictingRoutes)
                {
                    _ = report.AppendLine($"   Controller: {route.Controller}");
                    _ = report.AppendLine($"   Metodo: {route.Method}()");
                    _ = report.AppendLine($"   File: {Path.GetFileName(route.FilePath)}");
                    _ = report.AppendLine();
                }

                _ = report.AppendLine("💡 SOLUZIONI SUGGERITE:");
                _ = report.AppendLine("   1. Rinominare uno dei metodi con un percorso più specifico");
                _ = report.AppendLine("   2. Aggiungere un prefisso alla route (es. 'details', 'summary')");
                _ = report.AppendLine("   3. Utilizzare parametri di route più specifici");
                _ = report.AppendLine("   4. Considerare l'uso di query parameters invece di route parameters");
                _ = report.AppendLine();
                _ = report.AppendLine(new string('=', 70));
                _ = report.AppendLine();
            }
        }
        else
        {
            _ = report.AppendLine("✅ NESSUN CONFLITTO RILEVATO");
            _ = report.AppendLine("============================");
            _ = report.AppendLine("Tutte le route sono uniche e non presentano conflitti.");
            _ = report.AppendLine();
        }

        // Sezione statistiche
        _ = report.AppendLine("📊 STATISTICHE");
        _ = report.AppendLine("===============");
        _ = report.AppendLine();

        var methodStats = _routes.GroupBy(r => r.HttpMethod)
            .Select(g => new { Method = g.Key, Count = g.Count() })
            .OrderByDescending(s => s.Count);

        _ = report.AppendLine("Distribuzione per HTTP Method:");
        foreach (var stat in methodStats)
        {
            _ = report.AppendLine($"  {stat.Method}: {stat.Count} route");
        }
        _ = report.AppendLine();

        var controllerStats = _routes.GroupBy(r => r.Controller)
            .Select(g => new { Controller = g.Key, Count = g.Count() })
            .OrderByDescending(s => s.Count);

        _ = report.AppendLine("Route per Controller:");
        foreach (var stat in controllerStats.Take(10))
        {
            _ = report.AppendLine($"  {stat.Controller}: {stat.Count} route");
        }

        await File.WriteAllTextAsync(outputPath, report.ToString());
    }
}

public class RouteInfo
{
    public string Controller { get; set; } = "";
    public string Method { get; set; } = "";
    public string HttpMethod { get; set; } = "";
    public string RouteTemplate { get; set; } = "";
    public string FilePath { get; set; } = "";
}

public class RouteConflict
{
    public string HttpMethod { get; set; } = "";
    public string RoutePattern { get; set; } = "";
    public List<RouteInfo> ConflictingRoutes { get; set; } = new();
}