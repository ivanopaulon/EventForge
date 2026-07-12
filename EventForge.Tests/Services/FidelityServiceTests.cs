using Microsoft.Extensions.Logging;
using Moq;
using Prym.DTOs.Business.Fidelity;
using Prym.Web.Services;

namespace EventForge.Tests.Services;

[Trait("Category", "Unit")]
public class FidelityServiceTests
{
    private readonly Mock<IHttpClientService> _mockHttpClient;
    private readonly Mock<ILogger<FidelityService>> _mockLogger;
    private readonly IFidelityService _service;

    public FidelityServiceTests()
    {
        _mockHttpClient = new Mock<IHttpClientService>();
        _mockLogger = new Mock<ILogger<FidelityService>>();
        _service = new FidelityService(_mockHttpClient.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetAllCardsAsync_ReturnsAllCards()
    {
        var expected = new List<FidelityCardDto>
        {
            new() { Id = Guid.NewGuid(), CardNumber = "CARD-001", Status = FidelityCardStatus.Active },
            new() { Id = Guid.NewGuid(), CardNumber = "CARD-002", Status = FidelityCardStatus.Suspended }
        };

        _mockHttpClient.Setup(x => x.GetAsync<IEnumerable<FidelityCardDto>>(
            "api/v1/fidelity-cards",
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = (await _service.GetAllCardsAsync()).ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal("CARD-001", result[0].CardNumber);
        Assert.Equal("CARD-002", result[1].CardNumber);
    }

    [Fact]
    public async Task GetCardsByBusinessPartyAsync_WithBusinessPartyId_ReturnsFilteredCards()
    {
        var businessPartyId = Guid.NewGuid();
        var expected = new List<FidelityCardDto>
        {
            new() { Id = Guid.NewGuid(), CardNumber = "CARD-100", BusinessPartyId = businessPartyId },
            new() { Id = Guid.NewGuid(), CardNumber = "CARD-101", BusinessPartyId = businessPartyId }
        };

        _mockHttpClient.Setup(x => x.GetAsync<IEnumerable<FidelityCardDto>>(
            It.Is<string>(url => url == $"api/v1/fidelity-cards?businessPartyId={businessPartyId}"),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = (await _service.GetCardsByBusinessPartyAsync(businessPartyId)).ToList();

        Assert.Equal(2, result.Count);
        Assert.All(result, card => Assert.Equal(businessPartyId, card.BusinessPartyId));
    }

    [Fact]
    public async Task GetCardByIdAsync_WithValidId_ReturnsCard()
    {
        var cardId = Guid.NewGuid();
        var tierId = Guid.NewGuid();
        var expected = new FidelityCardDto
        {
            Id = cardId,
            CardNumber = "CARD-200",
            TierId = tierId,
            TierName = "Gold",
            Status = FidelityCardStatus.Active
        };

        _mockHttpClient.Setup(x => x.GetAsync<FidelityCardDto>(
            $"api/v1/fidelity-cards/{cardId}",
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _service.GetCardByIdAsync(cardId);

        Assert.NotNull(result);
        Assert.Equal(cardId, result!.Id);
        Assert.Equal(tierId, result.TierId);
        Assert.Equal("Gold", result.TierName);
    }

    [Fact]
    public async Task GetCardByIdAsync_WhenNotFound_ReturnsNull()
    {
        var cardId = Guid.NewGuid();

        _mockHttpClient.Setup(x => x.GetAsync<FidelityCardDto>(
            $"api/v1/fidelity-cards/{cardId}",
            It.IsAny<CancellationToken>()))
            .ReturnsAsync((FidelityCardDto?)null);

        var result = await _service.GetCardByIdAsync(cardId);

        Assert.Null(result);
    }

    [Fact]
    public async Task CreateCardAsync_WithValidDto_ReturnsCreatedCard()
    {
        var dto = new CreateFidelityCardDto
        {
            CardNumber = "CARD-300",
            TierId = Guid.NewGuid(),
            DiscountPercentage = 10,
            BusinessPartyId = Guid.NewGuid()
        };
        var expected = new FidelityCardDto
        {
            Id = Guid.NewGuid(),
            CardNumber = dto.CardNumber,
            TierId = dto.TierId,
            DiscountPercentage = dto.DiscountPercentage
        };

        _mockHttpClient.Setup(x => x.PostAsync<CreateFidelityCardDto, FidelityCardDto>(
            "api/v1/fidelity-cards",
            dto,
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _service.CreateCardAsync(dto);

        Assert.NotNull(result);
        Assert.Equal(dto.CardNumber, result.CardNumber);
        Assert.Equal(dto.DiscountPercentage, result.DiscountPercentage);
    }

    [Fact]
    public async Task UpdateCardAsync_WithValidId_ReturnsUpdatedCard()
    {
        var cardId = Guid.NewGuid();
        var dto = new UpdateFidelityCardDto
        {
            TierId = Guid.NewGuid(),
            DiscountPercentage = 20,
            Notes = "Updated"
        };
        var expected = new FidelityCardDto
        {
            Id = cardId,
            TierId = dto.TierId,
            DiscountPercentage = dto.DiscountPercentage,
            Notes = dto.Notes
        };

        _mockHttpClient.Setup(x => x.PutAsync<UpdateFidelityCardDto, FidelityCardDto>(
            $"api/v1/fidelity-cards/{cardId}",
            dto,
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _service.UpdateCardAsync(cardId, dto);

        Assert.NotNull(result);
        Assert.Equal(cardId, result!.Id);
        Assert.Equal("Updated", result.Notes);
    }

    [Fact]
    public async Task AddPointsAsync_WithValidId_ReturnsTransaction()
    {
        var cardId = Guid.NewGuid();
        var dto = new ModifyFidelityPointsDto { Points = 25, Description = "Purchase" };
        var expected = new FidelityPointsTransactionDto
        {
            Id = Guid.NewGuid(),
            FidelityCardId = cardId,
            TransactionType = FidelityTransactionType.Earned,
            Points = dto.Points,
            Description = dto.Description
        };

        _mockHttpClient.Setup(x => x.PostAsync<ModifyFidelityPointsDto, FidelityPointsTransactionDto>(
            $"api/v1/fidelity-cards/{cardId}/points/add",
            dto,
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _service.AddPointsAsync(cardId, dto);

        Assert.NotNull(result);
        Assert.Equal(cardId, result!.FidelityCardId);
        Assert.Equal(FidelityTransactionType.Earned, result.TransactionType);
    }

    [Fact]
    public async Task RedeemPointsAsync_WithValidId_ReturnsTransaction()
    {
        var cardId = Guid.NewGuid();
        var dto = new ModifyFidelityPointsDto { Points = 10, Description = "Reward" };
        var expected = new FidelityPointsTransactionDto
        {
            Id = Guid.NewGuid(),
            FidelityCardId = cardId,
            TransactionType = FidelityTransactionType.Redeemed,
            Points = dto.Points,
            Description = dto.Description
        };

        _mockHttpClient.Setup(x => x.PostAsync<ModifyFidelityPointsDto, FidelityPointsTransactionDto>(
            $"api/v1/fidelity-cards/{cardId}/points/redeem",
            dto,
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _service.RedeemPointsAsync(cardId, dto);

        Assert.NotNull(result);
        Assert.Equal(cardId, result!.FidelityCardId);
        Assert.Equal(FidelityTransactionType.Redeemed, result.TransactionType);
    }

    [Fact]
    public async Task GetTransactionHistoryAsync_WithValidCardId_ReturnsTransactions()
    {
        var cardId = Guid.NewGuid();
        var expected = new List<FidelityPointsTransactionDto>
        {
            new() { Id = Guid.NewGuid(), FidelityCardId = cardId, TransactionType = FidelityTransactionType.Earned, Points = 10 },
            new() { Id = Guid.NewGuid(), FidelityCardId = cardId, TransactionType = FidelityTransactionType.Redeemed, Points = 5 }
        };

        _mockHttpClient.Setup(x => x.GetAsync<IEnumerable<FidelityPointsTransactionDto>>(
            $"api/v1/fidelity-cards/{cardId}/transactions",
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = (await _service.GetTransactionHistoryAsync(cardId)).ToList();

        Assert.Equal(2, result.Count);
        Assert.All(result, transaction => Assert.Equal(cardId, transaction.FidelityCardId));
    }

    [Fact]
    public async Task RevokeCardAsync_CallsCorrectEndpoint()
    {
        var cardId = Guid.NewGuid();

        _mockHttpClient.Setup(x => x.PostAsync<It.IsAnyType>(
            It.Is<string>(url => url == $"api/v1/fidelity-cards/{cardId}/revoke"),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _service.RevokeCardAsync(cardId);

        _mockHttpClient.Verify(x => x.PostAsync<It.IsAnyType>(
            It.Is<string>(url => url == $"api/v1/fidelity-cards/{cardId}/revoke"),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SuspendCardAsync_CallsCorrectEndpoint()
    {
        var cardId = Guid.NewGuid();

        _mockHttpClient.Setup(x => x.PostAsync<It.IsAnyType>(
            It.Is<string>(url => url == $"api/v1/fidelity-cards/{cardId}/suspend"),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _service.SuspendCardAsync(cardId);

        _mockHttpClient.Verify(x => x.PostAsync<It.IsAnyType>(
            It.Is<string>(url => url == $"api/v1/fidelity-cards/{cardId}/suspend"),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ActivateCardAsync_CallsCorrectEndpoint()
    {
        var cardId = Guid.NewGuid();

        _mockHttpClient.Setup(x => x.PostAsync<It.IsAnyType>(
            It.Is<string>(url => url == $"api/v1/fidelity-cards/{cardId}/activate"),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _service.ActivateCardAsync(cardId);

        _mockHttpClient.Verify(x => x.PostAsync<It.IsAnyType>(
            It.Is<string>(url => url == $"api/v1/fidelity-cards/{cardId}/activate"),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteCardAsync_CallsCorrectEndpoint()
    {
        var cardId = Guid.NewGuid();

        _mockHttpClient.Setup(x => x.DeleteAsync(
            $"api/v1/fidelity-cards/{cardId}",
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _service.DeleteCardAsync(cardId);

        _mockHttpClient.Verify(x => x.DeleteAsync(
            $"api/v1/fidelity-cards/{cardId}",
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
