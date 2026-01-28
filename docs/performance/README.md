# Performance Monitoring & Optimization Documentation

## Overview

This directory contains documentation for FASE 6 - Performance Optimization initiative, which aims to establish performance monitoring infrastructure and optimize the EventForge application.

## Wave 1: Foundation & Monitoring Setup (Current)

**Status**: ✅ Completed

### Components Implemented

1. **MiniProfiler Integration**
   - Real-time SQL query profiling
   - Request timing analysis
   - Available at `/profiler/results-index`
   - EF Core integration for query tracking

2. **Performance Telemetry Middleware**
   - Response time headers (`X-Response-Time-Ms`)
   - Slow request logging (threshold: 200ms)
   - Very slow request alerts (threshold: 400ms)

3. **BenchmarkDotNet Performance Tests**
   - Baseline benchmarks for paginated endpoints
   - Located in `EventForge.PerformanceTests` project
   - Run with: `dotnet run -c Release --project EventForge.PerformanceTests`

### How to Use MiniProfiler

1. **Start the application** in development mode
2. **Navigate to any API endpoint** via browser or Postman
3. **View profiling results** at `https://localhost:5001/profiler/results-index`
4. **Inspect SQL queries** and execution times

### How to Run Performance Benchmarks

```bash
# Navigate to the root directory
cd EventForge

# Run all benchmarks
dotnet run -c Release --project EventForge.PerformanceTests

# Run specific category
dotnet run -c Release --project EventForge.PerformanceTests --filter *Products*
```

### How to Monitor Slow Requests

Slow requests are automatically logged by the Performance Telemetry Middleware:

- **Warning** logs for requests > 200ms
- **Error** logs for requests > 400ms
- Check application logs for performance insights

### Configuration

Performance settings are configured in `appsettings.Development.json`:

```json
{
  "Performance": {
    "SlowRequestThresholdMs": 200
  }
}
```

## Wave 2: AsNoTracking() Quick Wins (Upcoming)

**Status**: 🔜 Planned

- Target: 20-30% response time reduction
- Scope: Read-only queries optimization
- Implementation: Add `.AsNoTracking()` to GET endpoints

## Wave 3: N+1 Query Fixes (Upcoming)

**Status**: 🔜 Planned

- Target: 50-70% response time reduction
- Scope: Fix eager loading issues
- Implementation: Optimize Include() statements

## Wave 4: Caching & Indexes (Upcoming)

**Status**: 🔜 Planned

- Target: 80-90% response time reduction for cached data
- Scope: Response caching and database indexes
- Implementation: Distributed caching + SQL indexes

## Related Documents

- [BASELINE_REPORT.md](./BASELINE_REPORT.md) - Pre-optimization performance baseline
- [SERVER_STARTUP_OPTIMIZATION.md](./SERVER_STARTUP_OPTIMIZATION.md) - Server startup performance improvements

## Tools & Technologies

- **MiniProfiler 4.5.4** - Profiling and diagnostics
- **BenchmarkDotNet 0.14.0** - Performance benchmarking
- **.NET 10.0** - Application framework
- **Entity Framework Core 10.0** - ORM
- **SQL Server** - Database

## Best Practices

1. **Always profile before optimizing** - Use MiniProfiler to identify bottlenecks
2. **Measure twice, cut once** - Run benchmarks before and after changes
3. **Monitor in production** - Keep track of performance metrics over time
4. **Don't guess, measure** - Use data to drive optimization decisions

## Support

For questions or issues related to performance monitoring:
1. Check the [BASELINE_REPORT.md](./BASELINE_REPORT.md) for current metrics
2. Review MiniProfiler results at `/profiler/results-index`
3. Consult application logs for slow request patterns
