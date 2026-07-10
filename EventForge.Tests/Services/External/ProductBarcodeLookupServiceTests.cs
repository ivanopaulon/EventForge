using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Prym.Web.Services.External;
using System.Net;

namespace EventForge.Tests.Services.External;

[Trait("Category", "Unit")]
public class ProductBarcodeLookupServiceTests
{
    private readonly Mock<ILogger<ProductBarcodeLookupService>> _loggerMock = new();

    [Fact]
    public async Task LookupAsync_WithEmptyCode_ReturnsInvalidResult()
    {
        var service = new ProductBarcodeLookupService(Mock.Of<IHttpClientFactory>(), _loggerMock.Object);

        var result = await service.LookupAsync(string.Empty);

        Assert.NotNull(result);
        Assert.False(result.IsFound);
        Assert.Equal("Product code is required", result.ErrorMessage);
    }

    [Fact]
    public async Task LookupAsync_WhenFirstProviderMatches_ReturnsMappedData()
    {
        var factory = CreateFactory((_, _) => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""
                                        {"status":1,"product":{"product_name":"Trapano XR","brands":"DeWalt","generic_name":"Trapano avvitatore","image_front_url":"https://img.example/trapano.jpg"}}
                                        """)
        });

        var service = new ProductBarcodeLookupService(factory.Object, _loggerMock.Object);

        var result = await service.LookupAsync("8011391023884");

        Assert.NotNull(result);
        Assert.True(result.IsFound);
        Assert.Equal("Trapano XR", result.Name);
        Assert.Equal("DeWalt", result.Brand);
        Assert.Equal("Trapano avvitatore", result.ShortDescription);
        Assert.Equal("https://img.example/trapano.jpg", result.ImageUrl);
        Assert.Equal("Open Products Facts", result.Source);
    }

    [Fact]
    public async Task LookupAsync_WhenFirstProviderMisses_FallsBackToNextProvider()
    {
        var callCount = 0;
        var factory = CreateFactory((request, _) =>
        {
            callCount++;
            if (callCount == 1)
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("""{"status":0}""")
                };
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""
                                            {"status":1,"product":{"product_name":"Shampoo Ultra","brands":"Acme","generic_name":"Shampoo professionale"}}
                                            """)
            };
        });

        var service = new ProductBarcodeLookupService(factory.Object, _loggerMock.Object);

        var result = await service.LookupAsync("8011391023884");

        Assert.NotNull(result);
        Assert.True(result.IsFound);
        Assert.Equal("Shampoo Ultra", result.Name);
        Assert.Equal("Open Beauty Facts", result.Source);
        Assert.True(callCount >= 2);
    }

    [Fact]
    public async Task LookupAsync_WhenNoProviderMatches_ReturnsNotFound()
    {
        var factory = CreateFactory((_, _) => new HttpResponseMessage(HttpStatusCode.NotFound));
        var service = new ProductBarcodeLookupService(factory.Object, _loggerMock.Object);

        var result = await service.LookupAsync("8011391023884");

        Assert.NotNull(result);
        Assert.False(result.IsFound);
        Assert.Equal("No product data found from public providers", result.ErrorMessage);
    }

    private static Mock<IHttpClientFactory> CreateFactory(Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> responder)
    {
        var handler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage request, CancellationToken ct) => responder(request, ct));

        var client = new HttpClient(handler.Object);
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(x => x.CreateClient("ProductBarcodeLookupClient")).Returns(client);
        return factory;
    }
}
