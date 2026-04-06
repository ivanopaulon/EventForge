using Microsoft.AspNetCore.Mvc.Testing;

namespace EventForge.Tests.Integration;

/// <summary>
/// Shared collection definition for all integration tests.
/// Groups all integration tests into a single xUnit collection so they share
/// one WebApplicationFactory instance and run sequentially, avoiding the
/// ObjectDisposedException that occurs when multiple factories run in parallel.
/// </summary>
[CollectionDefinition("Integration Tests")]
public class IntegrationTestsCollection : ICollectionFixture<WebApplicationFactory<Program>>
{
}
