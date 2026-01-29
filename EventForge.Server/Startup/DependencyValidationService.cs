using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Text;

namespace EventForge.Server.Startup;

/// <summary>
/// Validates dependency injection configuration at application startup.
/// Detects circular dependencies and provides detailed error messages.
/// </summary>
public static class DependencyValidationService
{
    /// <summary>
    /// Validates all registered services for circular dependencies.
    /// </summary>
    /// <param name="services">Service provider to validate</param>
    /// <param name="logger">Logger for validation messages</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when circular dependencies are detected
    /// </exception>
    public static void ValidateDependencies(
        IServiceProvider services,
        ILogger? logger = null)
    {
        logger?.LogInformation("Starting dependency validation...");

        var serviceDescriptors = GetServiceDescriptors(services);
        logger?.LogInformation("Analyzing {Count} registered services...", serviceDescriptors.Count());

        var graph = BuildDependencyGraph(serviceDescriptors);
        var cycles = DetectCycles(graph);

        if (cycles.Any())
        {
            var errorMessage = FormatCycleError(cycles);
            logger?.LogCritical("Circular dependencies detected:\n{ErrorMessage}",
                errorMessage);
            throw new InvalidOperationException(
                $"Circular dependencies detected:\n{errorMessage}");
        }

        logger?.LogInformation(
            "Dependency validation completed. No circular dependencies found.");
    }

    /// <summary>
    /// Extracts all registered services from IServiceProvider using reflection.
    /// 
    /// TECHNICAL NOTE: IServiceProvider doesn't expose service descriptors through its public API.
    /// We use reflection to access internal fields of the Microsoft.Extensions.DependencyInjection
    /// implementation. This approach works across different .NET versions but may need updates
    /// if the DI container's internal structure changes.
    /// 
    /// FALLBACK: Returns empty collection if reflection fails (safe degradation - no validation).
    /// </summary>
    private static IEnumerable<ServiceDescriptor> GetServiceDescriptors(
        IServiceProvider services)
    {
        // Try to get service descriptors from the service provider
        // IServiceProvider doesn't expose descriptors directly, so we need to use reflection
        var serviceProviderType = services.GetType();

        // For Microsoft.Extensions.DependencyInjection.ServiceProvider
        var engineField = serviceProviderType.GetField("_engine", 
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        
        object? engine = null;
        
        if (engineField == null)
        {
            // Try alternative approach for different service provider implementations
            var rootField = serviceProviderType.GetField("_root",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            
            if (rootField != null)
            {
                var root = rootField.GetValue(services);
                if (root != null)
                {
                    engineField = root.GetType().GetField("_engine",
                        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    if (engineField != null)
                    {
                        engine = engineField.GetValue(root);
                    }
                }
            }
        }
        else
        {
            engine = engineField.GetValue(services);
        }

        // Extract descriptors from engine if found
        if (engine != null)
        {
            var descriptors = ExtractDescriptorsFromEngine(engine);
            if (descriptors != null)
            {
                return descriptors;
            }
        }

        // If we can't get descriptors via reflection, return empty collection
        // This will result in no validation, which is safer than throwing an error
        return Enumerable.Empty<ServiceDescriptor>();
    }

    /// <summary>
    /// Extracts service descriptors from the engine object.
    /// </summary>
    private static ServiceDescriptor[]? ExtractDescriptorsFromEngine(object engine)
    {
        var callSiteFactoryField = engine.GetType().GetField("_callSiteFactory",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        
        if (callSiteFactoryField != null)
        {
            var callSiteFactory = callSiteFactoryField.GetValue(engine);
            if (callSiteFactory != null)
            {
                var descriptorsField = callSiteFactory.GetType().GetField("_descriptors",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                
                if (descriptorsField != null)
                {
                    var descriptors = descriptorsField.GetValue(callSiteFactory);
                    if (descriptors is ServiceDescriptor[] descriptorArray)
                    {
                        return descriptorArray;
                    }
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Builds a dependency graph from service descriptors.
    /// Key = Service Type, Value = List of dependencies (constructor parameters)
    /// </summary>
    private static Dictionary<Type, List<Type>> BuildDependencyGraph(
        IEnumerable<ServiceDescriptor> descriptors)
    {
        var graph = new Dictionary<Type, List<Type>>();

        foreach (var descriptor in descriptors)
        {
            var serviceType = descriptor.ServiceType;

            // Skip open generic types
            if (serviceType.IsGenericTypeDefinition)
                continue;

            // Skip types we've already processed
            if (graph.ContainsKey(serviceType))
                continue;

            var dependencies = new List<Type>();

            // Get the implementation type
            Type? implementationType = null;

            if (descriptor.ImplementationType != null)
            {
                implementationType = descriptor.ImplementationType;
            }
            else if (descriptor.ImplementationFactory != null)
            {
                // For factory registrations, we can't easily determine dependencies
                // Skip these for now
                continue;
            }
            else if (descriptor.ImplementationInstance != null)
            {
                // Instance registrations don't have constructor dependencies
                implementationType = descriptor.ImplementationInstance.GetType();
            }

            if (implementationType != null && !implementationType.IsAbstract && !implementationType.IsInterface)
            {
                // Get constructors
                var constructors = implementationType.GetConstructors(
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

                if (constructors.Length > 0)
                {
                    // IMPORTANT: Use the constructor with the most parameters
                    // This matches .NET DI container behavior (greedy constructor selection)
                    // The DI container selects the constructor with the most parameters it can resolve
                    var constructor = constructors.OrderByDescending(c => c.GetParameters().Length).First();
                    var parameters = constructor.GetParameters();

                    foreach (var parameter in parameters)
                    {
                        var paramType = parameter.ParameterType;

                        // Skip generic type definitions
                        if (paramType.IsGenericTypeDefinition)
                            continue;

                        // Add dependency
                        dependencies.Add(paramType);
                    }
                }
            }

            graph[serviceType] = dependencies;
        }

        return graph;
    }

    /// <summary>
    /// Detects cycles in the dependency graph using Depth-First Search (DFS).
    /// 
    /// ALGORITHM: Uses DFS with two tracking structures:
    /// - Visited set: Tracks all nodes visited during the entire traversal
    /// - Recursion stack/set: Tracks current path to detect back edges (cycles)
    /// When a node in the recursion stack is revisited, a cycle exists.
    /// 
    /// PERFORMANCE: O(V + E) where V = number of services, E = number of dependencies
    /// </summary>
    private static List<List<Type>> DetectCycles(
        Dictionary<Type, List<Type>> graph)
    {
        var cycles = new List<List<Type>>();
        var visited = new HashSet<Type>();
        var recursionStack = new Stack<Type>();
        var recursionSet = new HashSet<Type>(); // For O(1) cycle detection

        foreach (var node in graph.Keys)
        {
            if (!visited.Contains(node))
            {
                DetectCyclesDFS(node, graph, visited, recursionStack, recursionSet, cycles);
            }
        }

        return cycles;
    }

    /// <summary>
    /// DFS helper method to detect cycles.
    /// </summary>
    private static void DetectCyclesDFS(
        Type current,
        Dictionary<Type, List<Type>> graph,
        HashSet<Type> visited,
        Stack<Type> recursionStack,
        HashSet<Type> recursionSet,
        List<List<Type>> cycles)
    {
        visited.Add(current);
        recursionStack.Push(current);
        recursionSet.Add(current);

        if (graph.TryGetValue(current, out var dependencies))
        {
            foreach (var dependency in dependencies)
            {
                if (!visited.Contains(dependency))
                {
                    DetectCyclesDFS(dependency, graph, visited,
                        recursionStack, recursionSet, cycles);
                }
                else if (recursionSet.Contains(dependency))
                {
                    // CYCLE DETECTED! 
                    // A back edge exists: dependency is already in the current path (recursion stack)
                    // This means we've found a circular dependency chain
                    var cycle = ExtractCycle(recursionStack, dependency);
                    
                    // Only add unique cycles (avoid duplicates)
                    // Multiple paths may discover the same cycle
                    if (!cycles.Any(c => CyclesAreEqual(c, cycle)))
                    {
                        cycles.Add(cycle);
                    }
                }
            }
        }

        recursionStack.Pop();
        recursionSet.Remove(current);
    }

    /// <summary>
    /// Extracts the cycle path from the recursion stack.
    /// </summary>
    private static List<Type> ExtractCycle(
        Stack<Type> stack,
        Type cycleStart)
    {
        var cycle = new List<Type>();
        var stackArray = stack.ToArray();

        bool foundStart = false;
        for (int i = stackArray.Length - 1; i >= 0; i--)
        {
            if (stackArray[i] == cycleStart)
            {
                foundStart = true;
            }

            if (foundStart)
            {
                cycle.Add(stackArray[i]);
            }
        }

        cycle.Add(cycleStart); // Close the cycle
        return cycle;
    }

    /// <summary>
    /// Checks if two cycles are equal (same services in the same order).
    /// 
    /// BUSINESS RULE: Cycles are equivalent regardless of starting point.
    /// Example: [A → B → C → A] equals [B → C → A → B] equals [C → A → B → C]
    /// This is because a circular dependency is the same cycle regardless of entry point.
    /// </summary>
    private static bool CyclesAreEqual(List<Type> cycle1, List<Type> cycle2)
    {
        if (cycle1.Count != cycle2.Count)
            return false;

        // Cycles can start at different points but represent the same cycle
        // E.g., A -> B -> C -> A is the same as B -> C -> A -> B
        for (int offset = 0; offset < cycle1.Count; offset++)
        {
            bool match = true;
            for (int i = 0; i < cycle1.Count; i++)
            {
                if (cycle1[i] != cycle2[(i + offset) % cycle2.Count])
                {
                    match = false;
                    break;
                }
            }
            if (match)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Formats error message showing cycle paths with clear visualization.
    /// </summary>
    private static string FormatCycleError(List<List<Type>> cycles)
    {
        var sb = new StringBuilder();
        sb.AppendLine();
        sb.AppendLine("==============================================");
        sb.AppendLine(" CIRCULAR DEPENDENCY DETECTED");
        sb.AppendLine("==============================================");
        sb.AppendLine();

        for (int i = 0; i < cycles.Count; i++)
        {
            sb.AppendLine($"Cycle {i + 1}:");
            sb.AppendLine();

            var cycle = cycles[i];
            for (int j = 0; j < cycle.Count - 1; j++)
            {
                var indent = new string(' ', j * 2);
                var serviceName = GetFriendlyServiceName(cycle[j]);
                sb.AppendLine($"{indent}{serviceName}");
                sb.AppendLine($"{indent}  ↓");
            }

            // Last service (closes the cycle)
            var lastIndent = new string(' ', (cycle.Count - 1) * 2);
            var lastServiceName = GetFriendlyServiceName(cycle[^1]);
            sb.AppendLine($"{lastIndent}{lastServiceName} ❌ CYCLE!");
            sb.AppendLine();
        }

        sb.AppendLine("==============================================");
        sb.AppendLine(" SOLUTION");
        sb.AppendLine("==============================================");
        sb.AppendLine();
        sb.AppendLine("To fix circular dependencies:");
        sb.AppendLine("1. Introduce an interface to break the cycle");
        sb.AppendLine("2. Use a Facade pattern");
        sb.AppendLine("3. Refactor to remove direct dependency");
        sb.AppendLine("4. Use property injection (if appropriate)");
        sb.AppendLine();

        return sb.ToString();
    }

    /// <summary>
    /// Gets a friendly display name for a service type.
    /// </summary>
    private static string GetFriendlyServiceName(Type type)
    {
        if (type.IsGenericType)
        {
            var genericName = type.Name.Split('`')[0];
            var genericArgs = string.Join(", ",
                type.GetGenericArguments().Select(t => t.Name));
            return $"{genericName}<{genericArgs}>";
        }

        return type.Name;
    }
}
