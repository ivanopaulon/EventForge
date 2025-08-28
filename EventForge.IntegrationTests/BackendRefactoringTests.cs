using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;

namespace EventForge.RefactoringTests
{
    public class BackendRefactoringIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public BackendRefactoringIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Development_Environment_Should_Show_Swagger_At_Root()
        {
            // Arrange
            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.UseSetting("ASPNETCORE_ENVIRONMENT", "Development");
            }).CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

            // Act - Access Swagger UI at root in development
            var response = await client.GetAsync("/");

            // Assert - Should show Swagger UI (OK status)
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Production_Environment_Should_Redirect_To_Logs()
        {
            // Arrange
            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.UseSetting("ASPNETCORE_ENVIRONMENT", "Production");
            }).CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

            // Act - Access homepage
            var response = await client.GetAsync("/");

            // Assert - Should redirect to logs.html (MovedPermanently or Found are both acceptable)
            Assert.True(response.StatusCode == HttpStatusCode.Redirect || response.StatusCode == HttpStatusCode.MovedPermanently);
            Assert.Equal("/logs.html", response.Headers.Location?.ToString());
        }

        [Fact]
        public async Task Swagger_Should_Be_Available_In_All_Environments()
        {
            // Arrange - Development
            var devClient = _factory.WithWebHostBuilder(builder =>
            {
                builder.UseSetting("ASPNETCORE_ENVIRONMENT", "Development");
            }).CreateClient();

            // Act & Assert - Development should have Swagger at root
            var devSwaggerResponse = await devClient.GetAsync("/swagger/v1/swagger.json");
            Assert.Equal(HttpStatusCode.OK, devSwaggerResponse.StatusCode);

            // Arrange - Production
            var prodClient = _factory.WithWebHostBuilder(builder =>
            {
                builder.UseSetting("ASPNETCORE_ENVIRONMENT", "Production");
            }).CreateClient();

            // Act & Assert - Production should have Swagger at /swagger
            var prodSwaggerResponse = await prodClient.GetAsync("/swagger/v1/swagger.json");
            Assert.Equal(HttpStatusCode.OK, prodSwaggerResponse.StatusCode);
        }

        [Fact]
        public async Task Health_Checks_Should_Be_Available()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act & Assert - Main health check (might be unhealthy due to missing DB in test, but should respond)
            var healthResponse = await client.GetAsync("/health");
            Assert.True(healthResponse.StatusCode == HttpStatusCode.OK || healthResponse.StatusCode == HttpStatusCode.ServiceUnavailable);

            // Act & Assert - Readiness check (might be unhealthy due to missing DB in test, but should respond)
            var readyResponse = await client.GetAsync("/health/ready");
            Assert.True(readyResponse.StatusCode == HttpStatusCode.OK || readyResponse.StatusCode == HttpStatusCode.ServiceUnavailable);

            // Act & Assert - Liveness check (should always be OK)
            var liveResponse = await client.GetAsync("/health/live");
            Assert.Equal(HttpStatusCode.OK, liveResponse.StatusCode);
        }

        [Fact]
        public async Task FileUploadOperationFilter_Should_Be_Registered()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act - Get Swagger spec
            var response = await client.GetAsync("/swagger/v1/swagger.json");
            var content = await response.Content.ReadAsStringAsync();

            // Assert - Should contain multipart/form-data for file upload endpoints
            Assert.Contains("multipart/form-data", content);
        }
    }
}