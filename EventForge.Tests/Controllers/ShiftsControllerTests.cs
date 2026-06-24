using EventForge.Server.Controllers;
using EventForge.Server.Services.Store;
using EventForge.Server.Services.Tenants;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Prym.DTOs.Store;
using System.Security.Claims;

namespace EventForge.Tests.Controllers;

[Trait("Category", "Unit")]
public class ShiftsControllerTests
{
    private readonly Mock<IShiftService> _mockService;
    private readonly Mock<ITenantContext> _mockTenantContext;
    private readonly ShiftsController _controller;

    public ShiftsControllerTests()
    {
        _mockService = new Mock<IShiftService>();
        _mockTenantContext = new Mock<ITenantContext>();

        var tenantId = Guid.NewGuid();
        _mockTenantContext.Setup(t => t.CurrentTenantId).Returns(tenantId);
        _mockTenantContext.Setup(t => t.CanAccessTenantAsync(It.IsAny<Guid>())).ReturnsAsync(true);

        _controller = new ShiftsController(_mockService.Object, _mockTenantContext.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = CreateHttpContext("/api/v1/shifts")
            }
        };
    }

    [Fact]
    public async Task GetShifts_ReturnsOkWithShifts()
    {
        var from = new DateOnly(2026, 1, 1);
        var to = new DateOnly(2026, 1, 31);
        var shifts = new List<CashierShiftDto>
        {
            new() { Id = Guid.NewGuid(), StoreUserName = "Alice", Status = ShiftStatus.Scheduled },
            new() { Id = Guid.NewGuid(), StoreUserName = "Bob", Status = ShiftStatus.InProgress }
        };
        _mockService.Setup(s => s.GetShiftsAsync(from, to, It.IsAny<CancellationToken>())).ReturnsAsync(shifts);

        var result = await _controller.GetShifts("2026-01-01", "2026-01-31", CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var value = Assert.IsType<List<CashierShiftDto>>(okResult.Value);
        Assert.Equal(2, value.Count);
    }

    [Fact]
    public async Task GetShiftById_WithValidId_ReturnsOk()
    {
        var shiftId = Guid.NewGuid();
        var shift = new CashierShiftDto { Id = shiftId, StoreUserName = "Alice", Status = ShiftStatus.Scheduled };
        _mockService.Setup(s => s.GetShiftByIdAsync(shiftId, It.IsAny<CancellationToken>())).ReturnsAsync(shift);

        var result = await _controller.GetShift(shiftId, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var value = Assert.IsType<CashierShiftDto>(okResult.Value);
        Assert.Equal(shiftId, value.Id);
    }

    [Fact]
    public async Task GetShiftById_WithInvalidId_ReturnsNotFound()
    {
        var shiftId = Guid.NewGuid();
        _mockService.Setup(s => s.GetShiftByIdAsync(shiftId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CashierShiftDto?)null);

        var result = await _controller.GetShift(shiftId, CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task CreateShift_WithValidDto_ReturnsCreatedAtAction()
    {
        var dto = new CreateCashierShiftDto
        {
            StoreUserId = Guid.NewGuid(),
            ShiftStart = DateTime.UtcNow,
            ShiftEnd = DateTime.UtcNow.AddHours(8),
            Notes = "Morning shift"
        };
        var created = new CashierShiftDto { Id = Guid.NewGuid(), StoreUserId = dto.StoreUserId, Notes = dto.Notes };
        _mockService.Setup(s => s.CreateShiftAsync(dto, "test-user", It.IsAny<CancellationToken>())).ReturnsAsync(created);

        var result = await _controller.CreateShift(dto, CancellationToken.None);

        var createdAt = Assert.IsType<CreatedAtActionResult>(result.Result);
        var value = Assert.IsType<CashierShiftDto>(createdAt.Value);
        Assert.Equal(created.Id, value.Id);
        Assert.Equal(nameof(ShiftsController.GetShift), createdAt.ActionName);
    }

    [Fact]
    public async Task CreateShift_WithInvalidModel_ReturnsBadRequest()
    {
        _controller.ModelState.AddModelError("StoreUserId", "Required");

        var result = await _controller.CreateShift(new CreateCashierShiftDto(), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        _mockService.Verify(s => s.CreateShiftAsync(It.IsAny<CreateCashierShiftDto>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateShift_WithValidId_ReturnsOk()
    {
        var shiftId = Guid.NewGuid();
        var dto = new UpdateCashierShiftDto
        {
            ShiftStart = DateTime.UtcNow,
            ShiftEnd = DateTime.UtcNow.AddHours(8),
            Status = ShiftStatus.Completed,
            Notes = "Closed"
        };
        var updated = new CashierShiftDto { Id = shiftId, Status = dto.Status, Notes = dto.Notes };
        _mockService.Setup(s => s.UpdateShiftAsync(shiftId, dto, "test-user", It.IsAny<CancellationToken>())).ReturnsAsync(updated);

        var result = await _controller.UpdateShift(shiftId, dto, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var value = Assert.IsType<CashierShiftDto>(okResult.Value);
        Assert.Equal(ShiftStatus.Completed, value.Status);
    }

    [Fact]
    public async Task DeleteShift_WithValidId_ReturnsNoContent()
    {
        var shiftId = Guid.NewGuid();
        _mockService.Setup(s => s.DeleteShiftAsync(shiftId, "test-user", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await _controller.DeleteShift(shiftId, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
    }

    private static DefaultHttpContext CreateHttpContext(string path)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.Name, "test-user")
        ], "TestAuth"));
        return context;
    }
}
