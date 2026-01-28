using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http.Json;

namespace EventForge.PerformanceTests;

/// <summary>
/// Baseline performance benchmarks for paginated endpoints
/// Run with: dotnet run -c Release --project EventForge.PerformanceTests
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class PaginationPerformanceBenchmark
{
    private HttpClient _client = null!;
#pragma warning disable CS0436 // Type conflicts with imported type
    private WebApplicationFactory<global::Program> _factory = null!;
#pragma warning restore CS0436

    [GlobalSetup]
    public void Setup()
    {
#pragma warning disable CS0436 // Type conflicts with imported type
        _factory = new WebApplicationFactory<global::Program>();
#pragma warning restore CS0436
        _client = _factory.CreateClient();
    }

    [Benchmark]
    [BenchmarkCategory("Products")]
    public async Task GetProducts_Page1_PageSize20()
    {
        var response = await _client.GetAsync("/api/v1/product-management/products?page=1&pageSize=20");
        response.EnsureSuccessStatusCode();
    }

    [Benchmark]
    [BenchmarkCategory("Products")]
    public async Task GetProducts_Page1_PageSize100()
    {
        var response = await _client.GetAsync("/api/v1/product-management/products?page=1&pageSize=100");
        response.EnsureSuccessStatusCode();
    }

    [Benchmark]
    [BenchmarkCategory("Orders")]
    public async Task GetOrders_Page1_PageSize20()
    {
        var response = await _client.GetAsync("/api/v1/sales/orders?page=1&pageSize=20");
        response.EnsureSuccessStatusCode();
    }

    [Benchmark]
    [BenchmarkCategory("Orders")]
    public async Task GetOrders_Page1_PageSize50()
    {
        var response = await _client.GetAsync("/api/v1/sales/orders?page=1&pageSize=50");
        response.EnsureSuccessStatusCode();
    }

    [Benchmark]
    [BenchmarkCategory("Invoices")]
    public async Task GetInvoices_Page1_PageSize20()
    {
        var response = await _client.GetAsync("/api/v1/sales/invoices?page=1&pageSize=20");
        response.EnsureSuccessStatusCode();
    }

    [Benchmark]
    [BenchmarkCategory("POS")]
    public async Task GetOpenPOSSessions_Page1_PageSize20()
    {
        var response = await _client.GetAsync("/api/v1/pos/sessions/open?page=1&pageSize=20");
        response.EnsureSuccessStatusCode();
    }

    [Benchmark]
    [BenchmarkCategory("Tables")]
    public async Task GetAvailableTables_Page1_PageSize20()
    {
        var response = await _client.GetAsync("/api/v1/tables/available?page=1&pageSize=20");
        response.EnsureSuccessStatusCode();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }
}
