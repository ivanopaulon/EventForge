using System.Text;
using System.Text.RegularExpressions;

namespace RouteConflictAnalyzer;

/// <summary>
/// Console application per analizzare i controller di EventForge e rilevare conflitti di route HTTP.
/// Genera un report dettagliato con mapping delle route e potenziali conflitti.
/// </summary>
class Program
{
    private static readonly List<RouteInfo> Routes = new();
    private static readonly List<RouteConflict> Conflicts = new();

    static async Task Main(string[] args)
    {
        Console.WriteLine("EventForge Route Conflict Analyzer");
        Console.WriteLine("===================================");
        Console.WriteLine();

        string controllersPath = args.Length > 0 ? args[0] : "../EventForge.Server/Controllers";
        string outputPath = args.Length > 1 ? args[1] : "route_analysis_report.txt";

        if (!Directory.Exists(controllersPath))
        {
            Console.WriteLine($"❌ Cartella Controllers non trovata: {Path.GetFullPath(controllersPath)}");
            Console.WriteLine("Utilizzo: RouteConflictAnalyzer [percorso-controllers] [file-output]");
            Environment.Exit(1);
        }

        Console.WriteLine($"📂 Scansione cartella: {Path.GetFullPath(controllersPath)}");
        Console.WriteLine($"📄 Report generato in: {Path.GetFullPath(outputPath)}");
        Console.WriteLine();

        // Analizza tutti i file controller
        await AnalyzeControllersAsync(controllersPath);

        // Genera il report
        await GenerateReportAsync(outputPath);

        Console.WriteLine("✅ Analisi completata!");
        Console.WriteLine($"📊 Controller analizzati: {Routes.GroupBy(r => r.Controller).Count()}");
        Console.WriteLine($"🔍 Route totali trovate: {Routes.Count}");
        Console.WriteLine($"⚠️  Conflitti rilevati: {Conflicts.Count}");

        if (Conflicts.Any())
        {
            Console.WriteLine();
            Console.WriteLine("⚠️  ATTENZIONE: Conflitti di route rilevati!");
            Console.WriteLine("Consulta il report per i dettagli e le soluzioni suggerite.");
            Environment.Exit(1);
        }
    }

    private static async Task AnalyzeControllersAsync(string controllersPath)
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

    private static async Task AnalyzeControllerFileAsync(string filePath)
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

            Routes.Add(new RouteInfo
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

    private static void DetectConflicts()
    {
        var routeGroups = Routes.GroupBy(r => new { r.HttpMethod, Route = NormalizeRoute(r.RouteTemplate) });

        foreach (var group in routeGroups.Where(g => g.Count() > 1))
        {
            var conflictingRoutes = group.ToList();
            Conflicts.Add(new RouteConflict
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

    private static async Task GenerateReportAsync(string outputPath)
    {
        var report = new StringBuilder();

        // Header del report
        report.AppendLine("EVENTFORGE - ROUTE CONFLICT ANALYSIS REPORT");
        report.AppendLine("==========================================");
        report.AppendLine($"Data Generazione: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        report.AppendLine($"Controller Analizzati: {Routes.GroupBy(r => r.Controller).Count()}");
        report.AppendLine($"Route Totali: {Routes.Count}");
        report.AppendLine($"Conflitti Rilevati: {Conflicts.Count}");
        report.AppendLine();

        // Sezione mapping completo delle route
        report.AppendLine("📋 MAPPING COMPLETO DELLE ROUTE");
        report.AppendLine("==============================");
        report.AppendLine();

        var groupedByController = Routes.GroupBy(r => r.Controller).OrderBy(g => g.Key);

        foreach (var controllerGroup in groupedByController)
        {
            report.AppendLine($"Controller: {controllerGroup.Key}");
            report.AppendLine(new string('-', 50));

            foreach (var route in controllerGroup.OrderBy(r => r.HttpMethod).ThenBy(r => r.RouteTemplate))
            {
                report.AppendLine($"  {route.HttpMethod,-7} {route.RouteTemplate,-40} -> {route.Method}()");
            }
            report.AppendLine();
        }

        // Sezione conflitti
        if (Conflicts.Any())
        {
            report.AppendLine("⚠️  CONFLITTI RILEVATI");
            report.AppendLine("=====================");
            report.AppendLine();

            foreach (var conflict in Conflicts.OrderBy(c => c.HttpMethod).ThenBy(c => c.RoutePattern))
            {
                report.AppendLine($"🚨 CONFLITTO: {conflict.HttpMethod} {conflict.RoutePattern}");
                report.AppendLine(new string('-', 60));

                foreach (var route in conflict.ConflictingRoutes)
                {
                    report.AppendLine($"   Controller: {route.Controller}");
                    report.AppendLine($"   Metodo: {route.Method}()");
                    report.AppendLine($"   File: {Path.GetFileName(route.FilePath)}");
                    report.AppendLine();
                }

                report.AppendLine("💡 SOLUZIONI SUGGERITE:");
                report.AppendLine("   1. Rinominare uno dei metodi con un percorso più specifico");
                report.AppendLine("   2. Aggiungere un prefisso alla route (es. 'details', 'summary')");
                report.AppendLine("   3. Utilizzare parametri di route più specifici");
                report.AppendLine("   4. Considerare l'uso di query parameters invece di route parameters");
                report.AppendLine();
                report.AppendLine(new string('=', 70));
                report.AppendLine();
            }
        }
        else
        {
            report.AppendLine("✅ NESSUN CONFLITTO RILEVATO");
            report.AppendLine("============================");
            report.AppendLine("Tutte le route sono uniche e non presentano conflitti.");
            report.AppendLine();
        }

        // Sezione statistiche
        report.AppendLine("📊 STATISTICHE");
        report.AppendLine("===============");
        report.AppendLine();

        var methodStats = Routes.GroupBy(r => r.HttpMethod)
            .Select(g => new { Method = g.Key, Count = g.Count() })
            .OrderByDescending(s => s.Count);

        report.AppendLine("Distribuzione per HTTP Method:");
        foreach (var stat in methodStats)
        {
            report.AppendLine($"  {stat.Method}: {stat.Count} route");
        }
        report.AppendLine();

        var controllerStats = Routes.GroupBy(r => r.Controller)
            .Select(g => new { Controller = g.Key, Count = g.Count() })
            .OrderByDescending(s => s.Count);

        report.AppendLine("Route per Controller:");
        foreach (var stat in controllerStats.Take(10))
        {
            report.AppendLine($"  {stat.Controller}: {stat.Count} route");
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
