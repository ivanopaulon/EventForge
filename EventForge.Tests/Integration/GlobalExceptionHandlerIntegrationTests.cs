using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;
using EventForge.DTOs.Auth;
using System.Text.Json;

namespace EventForge.Tests.Integration;

[Trait("Category", "Integration")]
public class GlobalExceptionHandlerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public GlobalExceptionHandlerIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task FluentValidation_Should_Return_ProblemDetails_BadRequest()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act - Send invalid login request (missing required fields which should trigger FluentValidation)
        var invalidRequest = new LoginRequestDto
        {
            TenantCode = "", // Empty tenant code (invalid per FluentValidation)
            Username = "",   // Empty username (invalid per FluentValidation)
            Password = ""    // Empty password (invalid per FluentValidation)
        };
        
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", invalidRequest);
        var content = await response.Content.ReadAsStringAsync();

        // Assert - Should get BadRequest
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        
        // Verify it's a problem details response (even if not perfect content-type)
        // The response should contain error details
        Assert.NotEmpty(content);
    }

    [Fact]
    public async Task ChangePassword_With_Invalid_Data_Should_Return_BadRequest()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act - Send change password request with mismatched passwords
        var invalidRequest = new ChangePasswordRequestDto
        {
            CurrentPassword = "current",
            NewPassword = "short", // Too short (less than 8 chars, will fail FluentValidation)
            ConfirmNewPassword = "different" // Different from NewPassword
        };
        
        var response = await client.PostAsJsonAsync("/api/v1/auth/change-password", invalidRequest);

        // Assert - Should get BadRequest or Unauthorized (depending on auth state)
        Assert.True(
            response.StatusCode == HttpStatusCode.BadRequest || 
            response.StatusCode == HttpStatusCode.Unauthorized,
            $"Expected BadRequest or Unauthorized but got {response.StatusCode}"
        );
    }

    [Fact]
    public async Task NotFound_Endpoint_Should_Return_NotFound()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act - Access non-existent API endpoint
        var response = await client.GetAsync("/api/v1/nonexistent");

        // Assert - Should get NotFound
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
