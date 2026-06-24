using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Prym.DTOs.Store;
using Prym.Web.Services.Store;
using System.Net;
using System.Text;
using System.Text.Json;

namespace EventForge.Tests.Services;

[Trait("Category", "Unit")]
public class ShiftServiceTests : IDisposable
{
    private readonly Mock<ILogger<ShiftService>> _mockLogger;
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly HttpClient _httpClient;
    private readonly ShiftService _service;

    public ShiftServiceTests()
    {
        _mockLogger = new Mock<ILogger<ShiftService>>();
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri("https://localhost/")
        };
        _service = new ShiftService(_httpClient, _mockLogger.Object);
    }

    [Fact]
    public async Task GetShiftsAsync_ReturnsShifts()
    {
        var from = new DateOnly(2026, 1, 1);
        var to = new DateOnly(2026, 1, 31);
        var expected = new List<CashierShiftDto>
        {
            new() { Id = Guid.NewGuid(), StoreUserName = "Alice", Status = ShiftStatus.Scheduled },
            new() { Id = Guid.NewGuid(), StoreUserName = "Bob", Status = ShiftStatus.Completed }
        };

        SetupJsonResponse(HttpMethod.Get, "https://localhost/api/v1/shifts?from=2026-01-01&to=2026-01-31", expected);

        var result = await _service.GetShiftsAsync(from, to);

        Assert.Equal(2, result.Count);
        Assert.Equal("Alice", result[0].StoreUserName);
    }

    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsShift()
    {
        var shiftId = Guid.NewGuid();
        var expected = new CashierShiftDto { Id = shiftId, StoreUserName = "Alice", Status = ShiftStatus.InProgress };

        SetupJsonResponse(HttpMethod.Get, $"https://localhost/api/v1/shifts/{shiftId}", expected);

        var result = await _service.GetByIdAsync(shiftId);

        Assert.NotNull(result);
        Assert.Equal(shiftId, result!.Id);
        Assert.Equal(ShiftStatus.InProgress, result.Status);
    }

    [Fact]
    public async Task CreateAsync_WithValidDto_ReturnsCreatedShift()
    {
        var dto = new CreateCashierShiftDto
        {
            StoreUserId = Guid.NewGuid(),
            ShiftStart = DateTime.UtcNow,
            ShiftEnd = DateTime.UtcNow.AddHours(8),
            Notes = "Evening shift"
        };
        var expected = new CashierShiftDto { Id = Guid.NewGuid(), StoreUserId = dto.StoreUserId, Notes = dto.Notes };

        SetupJsonResponse(HttpMethod.Post, "https://localhost/api/v1/shifts", expected, HttpStatusCode.Created);

        var result = await _service.CreateAsync(dto);

        Assert.NotNull(result);
        Assert.Equal(dto.StoreUserId, result!.StoreUserId);
        Assert.Equal("Evening shift", result.Notes);
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }

    private void SetupJsonResponse(HttpMethod method, string url, object payload, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var response = new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(request =>
                    request.Method == method &&
                    request.RequestUri != null &&
                    request.RequestUri.ToString() == url),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);
    }
}
