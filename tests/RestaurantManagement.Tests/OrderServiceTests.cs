using Microsoft.EntityFrameworkCore;
using RestaurantManagement.Application.DTOs;
using RestaurantManagement.Application.Services;
using RestaurantManagement.Domain.Entities;
using RestaurantManagement.Domain.Enums;
using RestaurantManagement.Infrastructure.DbContext;
using RestaurantManagement.Infrastructure.Repositories;
using Xunit;

namespace RestaurantManagement.Tests;

public class OrderServiceTests
{
    private RestaurantDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<RestaurantDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new RestaurantDbContext(options);

        // Seed basic category and menu item
        var cat = new Category { Id = 1, Name = "Starters" };
        var item1 = new MenuItem { Id = 1, CategoryId = 1, Name = "Paneer Tikka", Price = 200m, IsAvailable = true };
        var item2 = new MenuItem { Id = 2, CategoryId = 1, Name = "Sold Out Item", Price = 150m, IsAvailable = false };
        var table = new Table { Id = 1, TableNumber = "T-01", Status = TableStatus.Available };

        context.Categories.Add(cat);
        context.MenuItems.AddRange(item1, item2);
        context.Tables.Add(table);
        context.SaveChanges();

        return context;
    }

    [Fact]
    public async Task CreateOrder_ShouldCalculateSubTotalAndGSTCorrectly()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var uow = new UnitOfWork(context);
        var orderService = new OrderService(uow);

        var dto = new CreateOrderDto
        {
            OrderType = OrderType.DineIn,
            TableId = 1,
            CustomerName = "Rahul",
            Items = new List<CreateOrderItemDto>
            {
                new CreateOrderItemDto { MenuItemId = 1, Quantity = 2 } // 2 x ₹200 = ₹400
            }
        };

        // Act
        var result = await orderService.CreateOrderAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(400m, result.SubTotal);
        Assert.Equal(10m, result.CGST); // 2.5% of 400
        Assert.Equal(10m, result.SGST); // 2.5% of 400
        Assert.Equal(420m, result.GrandTotal); // 400 + 20
    }

    [Fact]
    public async Task CreateOrder_WithSoldOutItem_ShouldThrowException()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var uow = new UnitOfWork(context);
        var orderService = new OrderService(uow);

        var dto = new CreateOrderDto
        {
            OrderType = OrderType.Takeaway,
            Items = new List<CreateOrderItemDto>
            {
                new CreateOrderItemDto { MenuItemId = 2, Quantity = 1 } // Item 2 is sold out!
            }
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => orderService.CreateOrderAsync(dto));
    }
}
