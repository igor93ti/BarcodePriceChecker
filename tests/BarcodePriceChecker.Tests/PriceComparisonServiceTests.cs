using BarcodePriceChecker.Application.Interfaces;
using BarcodePriceChecker.Application.Services;
using BarcodePriceChecker.Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace BarcodePriceChecker.Tests;

public class PriceComparisonServiceTests
{
    private readonly Mock<IBarcodeProductResolver> _resolverMock = new();
    private readonly Mock<IPriceSearchService> _searchServiceMock = new();

    private PriceComparisonService CreateService() =>
        new(_resolverMock.Object,
            new[] { _searchServiceMock.Object },
            NullLogger<PriceComparisonService>.Instance);

    [Fact]
    public async Task CompareAsync_WhenProductFound_ReturnsComparisonWithProduct()
    {
        // Arrange
        var barcode = "7891000315507";
        var product = new Product { Barcode = barcode, Name = "Leite Integral", Brand = "Nestlé" };

        _resolverMock.Setup(r => r.ResolveAsync(barcode, default))
                     .ReturnsAsync(product);

        _searchServiceMock.Setup(s => s.SearchAsync(It.IsAny<string>(), barcode, default))
                          .ReturnsAsync(new List<PriceOffer>
                          {
                              new() { Source = "Mercado Livre", Price = 5.99m, ProductName = "Leite Nestlé" },
                              new() { Source = "Mercado Livre", Price = 6.50m, ProductName = "Leite Nestlé 1L" }
                          });

        _searchServiceMock.Setup(s => s.SourceName).Returns("Mercado Livre");

        var service = CreateService();

        // Act
        var result = await service.CompareAsync(barcode);

        // Assert
        result.Should().NotBeNull();
        result.Product.Name.Should().Be("Leite Integral");
        result.Offers.Should().HaveCount(2);
        result.LowestPrice.Should().Be(5.99m);
        result.AveragePrice.Should().Be(6.245m);
    }

    [Fact]
    public async Task CompareAsync_WhenProductNotFound_UsesBarcodeAsName()
    {
        // Arrange
        var barcode = "9999999999999";
        _resolverMock.Setup(r => r.ResolveAsync(barcode, default)).ReturnsAsync((Product?)null);
        _searchServiceMock.Setup(s => s.SearchAsync(It.IsAny<string>(), barcode, default))
                          .ReturnsAsync(Enumerable.Empty<PriceOffer>());
        _searchServiceMock.Setup(s => s.SourceName).Returns("Test");

        var service = CreateService();

        // Act
        var result = await service.CompareAsync(barcode);

        // Assert
        result.Product.Barcode.Should().Be(barcode);
        result.Offers.Should().BeEmpty();
        result.AveragePrice.Should().BeNull();
    }

    [Theory]
    [InlineData(4.00, PriceEvaluation.Cheap)]      // 20% abaixo da média
    [InlineData(5.00, PriceEvaluation.Average)]    // na média
    [InlineData(6.00, PriceEvaluation.Expensive)]  // 20% acima da média
    public void EvaluatePrice_ReturnsCorrectEvaluation(decimal userPrice, PriceEvaluation expected)
    {
        // Arrange
        var comparison = new PriceComparison
        {
            Offers = new List<PriceOffer>
            {
                new() { Price = 4.50m },
                new() { Price = 5.50m }
            }
        };

        // Act
        var evaluation = comparison.EvaluatePrice(userPrice);

        // Assert
        evaluation.Should().Be(expected);
    }

    [Fact]
    public async Task CompareAsync_WhenSearchServiceThrows_ReturnsEmptyOffers()
    {
        // Arrange
        var barcode = "1234567890";
        _resolverMock.Setup(r => r.ResolveAsync(barcode, default))
                     .ReturnsAsync(new Product { Name = "Produto Teste" });

        _searchServiceMock.Setup(s => s.SearchAsync(It.IsAny<string>(), barcode, default))
                          .ThrowsAsync(new HttpRequestException("Timeout"));
        _searchServiceMock.Setup(s => s.SourceName).Returns("TestSource");

        var service = CreateService();

        // Act
        var result = await service.CompareAsync(barcode);

        // Assert
        result.Offers.Should().BeEmpty(); // não deve lançar exceção
    }
}
