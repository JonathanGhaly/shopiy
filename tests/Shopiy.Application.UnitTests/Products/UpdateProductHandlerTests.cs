using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Moq;
using Shopiy.Application.Features.Products.Commands.UpdateProduct;
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

public class UpdateProductHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<ICacheService> _cacheMock;

    public UpdateProductHandlerTests()
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
    public async Task Handle_ShouldUpdateProduct_AndInvalidateListAndDetailCaches()
    {
        // Arrange: Pre-populate seed product
        var originalProduct = new Product
        {
            Name = "Original Screen",
            Description = "Standard monitor",
            Price = 12000, // stored in cents/piasters (120 USD)
            StockQuantity = 5,
            SKU = "MON-ORG-01",
            Currency = "USD",
            IsActive = true,
            Slug = "original-screen",
            Metadata = "{\"sizes\":[\"24 inch\"]}"
        };

        _context.Products.Add(originalProduct);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var productId = originalProduct.Id;

        var command = new UpdateProductCommand(
            Id: productId,
            Name: "Updated UltraWide Screen",
            Description: "Curved gaming monitor",
            Price: 349.99m,
            StockQuantity: 8,
            CategoryIds: new List<Guid>(),
            SKU: "MON-ORG-01",
            Currency: "USD",
            IsActive: true,
            Metadata: new Dictionary<string, object>
            {
                { "sizes", new List<string> { "34 inch" } }
            }
        );

        var handler = new UpdateProductHandler(_context, _mapperMock.Object, _cacheMock.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Updated UltraWide Screen", result.Name);
        Assert.True(result.IsActive);

        // Verify product was updated in the DB
        var savedProduct = await _context.Products.FirstOrDefaultAsync(p => p.Id == productId);
        Assert.NotNull(savedProduct);
        Assert.Equal("Updated UltraWide Screen", savedProduct.Name);
        Assert.Equal("Curved gaming monitor", savedProduct.Description);

        // Verify list cache invalidation was triggered
        _cacheMock.Verify(
            c => c.RemoveByPrefixAsync(
                "Shopiy.Application.Features.Products.Queries.GetProducts.GetProductsQuery", 
                It.IsAny<CancellationToken>()), 
            Times.Once);

        // Verify detail cache invalidation was triggered
        _cacheMock.Verify(
            c => c.RemoveByPrefixAsync(
                "Shopiy.Application.Features.Products.Queries.GetProduct.GetProductQuery", 
                It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
