using BenchmarkDotNet.Attributes;
using System.Text.Json;

namespace EventForge.Benchmarks;

/// <summary>
/// Benchmarks for SignalR operations - Onda 4 Performance Tracking
/// Tests event processing, health checks, and serialization performance
/// </summary>
[MemoryDiagnoser]
public class SignalRBenchmarks
{
    private List<EventData> _events = null!;
    private EventData _singleEvent = null!;
    private string _serializedEvent = null!;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    [GlobalSetup]
    public void Setup()
    {
        // Setup test data for benchmarks
        _singleEvent = new EventData
        {
            Id = Guid.NewGuid(),
            Type = "Notification",
            Timestamp = DateTime.UtcNow,
            UserId = 123,
            TenantId = 1,
            Payload = new Dictionary<string, object>
            {
                ["message"] = "Test notification",
                ["priority"] = "High",
                ["read"] = false
            }
        };

        _serializedEvent = JsonSerializer.Serialize(_singleEvent, _jsonOptions);

        // Generate batch of 100 events for batch processing tests
        _events = new List<EventData>();
        for (int i = 0; i < 100; i++)
        {
            _events.Add(new EventData
            {
                Id = Guid.NewGuid(),
                Type = i % 3 == 0 ? "Notification" : i % 3 == 1 ? "Audit" : "Collaboration",
                Timestamp = DateTime.UtcNow.AddSeconds(-i),
                UserId = 100 + (i % 10),
                TenantId = 1 + (i % 3),
                Payload = new Dictionary<string, object>
                {
                    ["index"] = i,
                    ["data"] = $"Event data {i}"
                }
            });
        }
    }

    /// <summary>
    /// Benchmark: Process batch of 100 events
    /// Simulates real-world scenario of multiple concurrent events
    /// </summary>
    [Benchmark]
    public async Task ProcessEventBatch()
    {
        var tasks = new List<Task>();
        
        foreach (var evt in _events)
        {
            // Simulate event processing
            tasks.Add(Task.Run(() =>
            {
                var serialized = JsonSerializer.Serialize(evt, _jsonOptions);
                var deserialized = JsonSerializer.Deserialize<EventData>(serialized, _jsonOptions);
                
                // Simulate event validation and routing
                _ = evt.Type switch
                {
                    "Notification" => ProcessNotificationEvent(evt),
                    "Audit" => ProcessAuditEvent(evt),
                    "Collaboration" => ProcessCollaborationEvent(evt),
                    _ => false
                };
            }));
        }

        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Benchmark: Connection health check simulation
    /// Tests performance of health check operations
    /// </summary>
    [Benchmark]
    public async Task ConnectionHealthCheck()
    {
        var healthChecks = new List<Task<HealthCheckResult>>();

        // Simulate 10 concurrent health checks
        for (int i = 0; i < 10; i++)
        {
            healthChecks.Add(Task.Run(() =>
            {
                var result = new HealthCheckResult
                {
                    IsHealthy = true,
                    ConnectionId = Guid.NewGuid().ToString(),
                    Timestamp = DateTime.UtcNow,
                    Latency = TimeSpan.FromMilliseconds(Random.Shared.Next(10, 100)),
                    Status = "Connected"
                };

                return Task.FromResult(result);
            }));
        }

        var results = await Task.WhenAll(healthChecks);
        
        // Aggregate results
        var healthyCount = results.Count(r => r.IsHealthy);
        var avgLatency = results.Average(r => r.Latency.TotalMilliseconds);
    }

    /// <summary>
    /// Benchmark: Event serialization performance
    /// Tests JSON serialization/deserialization speed
    /// </summary>
    [Benchmark]
    public void EventSerialization()
    {
        // Serialize
        var serialized = JsonSerializer.Serialize(_singleEvent, _jsonOptions);
        
        // Deserialize
        var deserialized = JsonSerializer.Deserialize<EventData>(serialized, _jsonOptions);
        
        // Validate
        if (deserialized?.Id != _singleEvent.Id)
        {
            throw new InvalidOperationException("Deserialization failed");
        }
    }

    /// <summary>
    /// Benchmark: Event deserialization from pre-serialized data
    /// Tests deserialization performance in isolation
    /// </summary>
    [Benchmark]
    public void EventDeserialization()
    {
        var deserialized = JsonSerializer.Deserialize<EventData>(_serializedEvent, _jsonOptions);
        
        if (deserialized?.Id == Guid.Empty)
        {
            throw new InvalidOperationException("Deserialization produced invalid data");
        }
    }

    /// <summary>
    /// Benchmark: Batch serialization of multiple events
    /// Tests performance when serializing event collections
    /// </summary>
    [Benchmark]
    public void BatchSerialization()
    {
        var serialized = JsonSerializer.Serialize(_events, _jsonOptions);
        var deserialized = JsonSerializer.Deserialize<List<EventData>>(serialized, _jsonOptions);
        
        if (deserialized?.Count != _events.Count)
        {
            throw new InvalidOperationException("Batch serialization failed");
        }
    }

    #region Helper Methods

    private bool ProcessNotificationEvent(EventData evt)
    {
        // Simulate notification processing logic
        return evt.Payload.ContainsKey("message") && 
               evt.UserId > 0 && 
               evt.TenantId > 0;
    }

    private bool ProcessAuditEvent(EventData evt)
    {
        // Simulate audit event processing logic
        return evt.Type == "Audit" && 
               evt.Timestamp < DateTime.UtcNow;
    }

    private bool ProcessCollaborationEvent(EventData evt)
    {
        // Simulate collaboration event processing logic
        return evt.Type == "Collaboration" && 
               evt.Payload.Count > 0;
    }

    #endregion
}

#region Data Models

public class EventData
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public int UserId { get; set; }
    public int TenantId { get; set; }
    public Dictionary<string, object> Payload { get; set; } = new();
}

public class HealthCheckResult
{
    public bool IsHealthy { get; set; }
    public string ConnectionId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public TimeSpan Latency { get; set; }
    public string Status { get; set; } = string.Empty;
}

#endregion
