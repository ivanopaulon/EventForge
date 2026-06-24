using EventForge.Server.Controllers;
using EventForge.Server.Services.Business;
using EventForge.Server.Services.Tenants;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Prym.DTOs.Business.Fidelity;
using System.Security.Claims;

namespace EventForge.Tests.Controllers;

[Trait("Category", "Unit")]
public class FidelityCardsControllerTests
{
    private readonly Mock<IFidelityCardService> _mockService;
    private readonly Mock<ITenantContext> _mockTenantContext;
    private readonly FidelityCardsController _controller;

    public FidelityCardsControllerTests()
    {
        _mockService = new Mock<IFidelityCardService>();
        _mockTenantContext = new Mock<ITenantContext>();

        var tenantId = Guid.NewGuid();
        _mockTenantContext.Setup(t => t.CurrentTenantId).Returns(tenantId);
        _mockTenantContext.Setup(t => t.CanAccessTenantAsync(It.IsAny<Guid>())).ReturnsAsync(true);

        _controller = new FidelityCardsController(_mockService.Object, _mockTenantContext.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = CreateHttpContext("/api/v1/fidelity-cards")
            }
        };
    }

    [Fact]
    public async Task GetCards_ReturnsOkWithCards()
    {
        var cards = new List<FidelityCardDto>
        {
            new() { Id = Guid.NewGuid(), CardNumber = "CARD-001" },
            new() { Id = Guid.NewGuid(), CardNumber = "CARD-002" }
        };
        _mockService.Setup(s => s.GetAllCardsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(cards);

        var result = await _controller.GetCards(null, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var value = Assert.IsAssignableFrom<IEnumerable<FidelityCardDto>>(okResult.Value);
        Assert.Equal(2, value.Count());
    }

    [Fact]
    public async Task GetCards_ByBusinessPartyId_ReturnsFilteredCards()
    {
        var businessPartyId = Guid.NewGuid();
        var cards = new List<FidelityCardDto>
        {
            new() { Id = Guid.NewGuid(), CardNumber = "CARD-010", BusinessPartyId = businessPartyId }
        };
        _mockService.Setup(s => s.GetCardsByBusinessPartyAsync(businessPartyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cards);

        var result = await _controller.GetCards(businessPartyId, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var value = Assert.IsAssignableFrom<IEnumerable<FidelityCardDto>>(okResult.Value);
        var card = Assert.Single(value);
        Assert.Equal(businessPartyId, card.BusinessPartyId);
    }

    [Fact]
    public async Task GetCard_WithValidId_ReturnsOk()
    {
        var cardId = Guid.NewGuid();
        var card = new FidelityCardDto { Id = cardId, CardNumber = "CARD-020" };
        _mockService.Setup(s => s.GetCardByIdAsync(cardId, It.IsAny<CancellationToken>())).ReturnsAsync(card);

        var result = await _controller.GetCard(cardId, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var value = Assert.IsType<FidelityCardDto>(okResult.Value);
        Assert.Equal(cardId, value.Id);
    }

    [Fact]
    public async Task GetCard_WithInvalidId_ReturnsNotFound()
    {
        var cardId = Guid.NewGuid();
        _mockService.Setup(s => s.GetCardByIdAsync(cardId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((FidelityCardDto?)null);

        var result = await _controller.GetCard(cardId, CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task CreateCard_WithValidDto_ReturnsCreatedAtAction()
    {
        var dto = new CreateFidelityCardDto { CardNumber = "CARD-030", DiscountPercentage = 5 };
        var created = new FidelityCardDto { Id = Guid.NewGuid(), CardNumber = dto.CardNumber };
        _mockService.Setup(s => s.CreateCardAsync(dto, "test-user", It.IsAny<CancellationToken>()))
            .ReturnsAsync(created);

        var result = await _controller.CreateCard(dto, CancellationToken.None);

        var createdAt = Assert.IsType<CreatedAtActionResult>(result.Result);
        var value = Assert.IsType<FidelityCardDto>(createdAt.Value);
        Assert.Equal(created.Id, value.Id);
        Assert.Equal(nameof(FidelityCardsController.GetCard), createdAt.ActionName);
    }

    [Fact]
    public async Task CreateCard_WithInvalidModel_ReturnsBadRequest()
    {
        var dto = new CreateFidelityCardDto();
        _controller.ModelState.AddModelError("CardNumber", "Required");

        var result = await _controller.CreateCard(dto, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        _mockService.Verify(s => s.CreateCardAsync(It.IsAny<CreateFidelityCardDto>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateCard_WithValidId_ReturnsOk()
    {
        var cardId = Guid.NewGuid();
        var dto = new UpdateFidelityCardDto { Type = FidelityCardType.Gold, DiscountPercentage = 15 };
        var updated = new FidelityCardDto { Id = cardId, Type = dto.Type, DiscountPercentage = dto.DiscountPercentage };
        _mockService.Setup(s => s.UpdateCardAsync(cardId, dto, "test-user", It.IsAny<CancellationToken>()))
            .ReturnsAsync(updated);

        var result = await _controller.UpdateCard(cardId, dto, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var value = Assert.IsType<FidelityCardDto>(okResult.Value);
        Assert.Equal(cardId, value.Id);
    }

    [Fact]
    public async Task RevokeCard_WithValidId_ReturnsNoContent()
    {
        var cardId = Guid.NewGuid();
        _mockService.Setup(s => s.RevokeCardAsync(cardId, "test-user", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _controller.RevokeCard(cardId, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task SuspendCard_WithValidId_ReturnsNoContent()
    {
        var cardId = Guid.NewGuid();
        _mockService.Setup(s => s.SuspendCardAsync(cardId, "test-user", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _controller.SuspendCard(cardId, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task ActivateCard_WithValidId_ReturnsNoContent()
    {
        var cardId = Guid.NewGuid();
        _mockService.Setup(s => s.ActivateCardAsync(cardId, "test-user", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _controller.ActivateCard(cardId, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task AddPoints_WithValidId_ReturnsOk()
    {
        var cardId = Guid.NewGuid();
        var dto = new ModifyFidelityPointsDto { Points = 20, Description = "Order" };
        var transaction = new FidelityPointsTransactionDto
        {
            Id = Guid.NewGuid(),
            FidelityCardId = cardId,
            TransactionType = FidelityTransactionType.Earned,
            Points = dto.Points
        };
        _mockService.Setup(s => s.AddPointsAsync(cardId, dto, "test-user", It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction);

        var result = await _controller.AddPoints(cardId, dto, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var value = Assert.IsType<FidelityPointsTransactionDto>(okResult.Value);
        Assert.Equal(cardId, value.FidelityCardId);
    }

    [Fact]
    public async Task RedeemPoints_WithValidId_ReturnsOk()
    {
        var cardId = Guid.NewGuid();
        var dto = new ModifyFidelityPointsDto { Points = 15, Description = "Reward" };
        var transaction = new FidelityPointsTransactionDto
        {
            Id = Guid.NewGuid(),
            FidelityCardId = cardId,
            TransactionType = FidelityTransactionType.Redeemed,
            Points = dto.Points
        };
        _mockService.Setup(s => s.RedeemPointsAsync(cardId, dto, "test-user", It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction);

        var result = await _controller.RedeemPoints(cardId, dto, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var value = Assert.IsType<FidelityPointsTransactionDto>(okResult.Value);
        Assert.Equal(FidelityTransactionType.Redeemed, value.TransactionType);
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
