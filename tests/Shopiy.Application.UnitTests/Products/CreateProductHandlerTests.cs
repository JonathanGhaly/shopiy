using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Moq;
using Shopiy.Application.Features.Products.Commands.CreateProduct;
using Shopiy.Domain.Entities;
using Shopiy.Domain.Interfaces;
using Shopiy.Application.DTOs.Products;
using Shopiy.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Shopiy.Application.UnitTests.Products;

public class CreateProductHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<ICacheService> _cacheMock;

    public CreateProductHandlerTests()
    {
        // 1. Setup InMemory Database Options
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _context.Database.EnsureCreated();

        // 2. Mock IMapper
        _mapperMock = new Mock<IMapper>();
        _mapperMock.Setup(m => m.Map<ProductDto>(It.IsAny<Product>()))
            .Returns((Product src) => new ProductDto
            {
                Id = src.Id,
                Name = src.Name,
                SKU = src.SKU,
                IsActive = src.IsActive
            });

        // 3. Mock Caching Service
        _cacheMock = new Mock<ICacheService>();
    }

    [Fact]
    public async Task Handle_ShouldCreateProduct_AndInvalidateCachedProducts()
    {
        // Arrange
        var command = new CreateProductCommand(
            Name: "Gamer Elite Keyboard",
            Description: "Premium RGB mechanical keyboard",
            Price: 99.99m,
            StockQuantity: 15,
            CategoryIds: new List<Guid>(),
            SKU: "RGB-KBD-01",
            Currency: "USD",
            IsActive: true,
            Metadata: new Dictionary<string, object>
            {
                { "sizes", new List<string> { "Standard", "Tenkeyless" } },
                { "colors", new List<string> { "Midnight Black", "Carbon Gray" } }
            }
        );

        var handler = new CreateProductHandler(_context, _mapperMock.Object, _cacheMock.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Gamer Elite Keyboard", result.Name);
        Assert.Equal("RGB-KBD-01", result.SKU);
        Assert.True(result.IsActive);

        // Verify product was actually saved in the DB
        var savedProduct = await _context.Products.FirstOrDefaultAsync(p => p.SKU == "RGB-KBD-01");
        Assert.NotNull(savedProduct);
        Assert.Equal("Gamer Elite Keyboard", savedProduct.Name);

        // Verify caching prefix invalidation was triggered for listings
        _cacheMock.Verify(
            c => c.RemoveByPrefixAsync(
                "Shopiy.Application.Features.Products.Queries.GetProducts.GetProductsQuery", 
                It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
