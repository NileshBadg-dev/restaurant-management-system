using Microsoft.EntityFrameworkCore;
using RestaurantManagement.Application.DTOs;
using RestaurantManagement.Application.Services;
using RestaurantManagement.Domain.Entities;
using RestaurantManagement.Domain.Enums;
using RestaurantManagement.Infrastructure.DbContext;
using RestaurantManagement.Infrastructure.Repositories;
using Xunit;

namespace RestaurantManagement.Tests;

public class WorkedExampleTable04Tests
{
    private (RestaurantDbContext context, UnitOfWork uow, OrderService orderService, BillingService billingService, Table table04) SetupTestEnvironment()
    {
        var options = new DbContextOptionsBuilder<RestaurantDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new RestaurantDbContext(options);

        // Seed Setting: Mode 2, 5% GST (2.5% + 2.5%)
        context.RestaurantSettings.Add(new RestaurantSetting
        {
            RestaurantName = "Girana Garden",
            CGSTPercentage = 2.5m,
            SGSTPercentage = 2.5m,
            KitchenMode = KitchenMode.PrinterOnly,
            TrackReadyStatus = false
        });

        // Floor & Table 04 (6 seats)
        var floor = new Floor { Id = 1, Name = "Ground Floor" };
        var table04 = new Table { Id = 4, TableNumber = "04", Capacity = 6, FloorId = 1, Status = TableStatus.Available };
        context.Floors.Add(floor);
        context.Tables.Add(table04);

        // Waiter
        var waiter = new User { Id = 4, Username = "amit", FullName = "Amit Kumar", Role = UserRole.Waiter };
        context.Users.Add(waiter);

        // Categories & Menu Items matching Section 4
        var catStarters = new Category { Id = 1, Name = "Starters" };
        var catMains = new Category { Id = 2, Name = "Main Course" };
        var catBeverages = new Category { Id = 3, Name = "Beverages" };
        context.Categories.AddRange(catStarters, catMains, catBeverages);

        var paneerTikka = new MenuItem { Id = 10, CategoryId = 1, Name = "Paneer Tikka", Price = 250m, IsAvailable = true };
        var variantHalf = new MenuItemVariant { Id = 1, MenuItemId = 10, VariantName = "Half", Price = 220m };
        var butterNaan = new MenuItem { Id = 11, CategoryId = 2, Name = "Butter Naan", Price = 45m, IsAvailable = true };
        var dalMakhani = new MenuItem { Id = 12, CategoryId = 2, Name = "Dal Makhani", Price = 260m, IsAvailable = true };
        var coldDrink = new MenuItem { Id = 13, CategoryId = 3, Name = "Cold Drink", Price = 60m, IsAvailable = true };

        context.MenuItems.AddRange(paneerTikka, butterNaan, dalMakhani, coldDrink);
        context.MenuItemVariants.Add(variantHalf);

        // Seed previous 6 KOTs so next is KOT-0007 as in Section 4
        for (int i = 1; i <= 6; i++)
        {
            context.KOTs.Add(new KOT { KOTNumber = $"KOT-{i:D4}", OrderId = 999, TableNumber = "99" });
        }

        context.SaveChanges();

        var uow = new UnitOfWork(context);
        var custService = new CustomerService(uow);
        var billingService = new BillingService(uow, custService);
        var orderService = new OrderService(uow, billingService);

        return (context, uow, orderService, billingService, table04);
    }

    [Fact]
    public async Task Section4_Table04_FromStartToFinish_ExactAmountsAndFlow()
    {
        var (context, uow, orderService, billingService, table04) = SetupTestEnvironment();

        // -------------------------------------------------------------
        // Step 1 & 2: Guests sit at Table 04 (6 seats). Covers = 4.
        // Table is still Available before Send KOT. Draft created.
        // -------------------------------------------------------------
        var draftDto = new SaveDraftOrderDto
        {
            TableId = table04.Id,
            UserId = 4,
            UserName = "Amit Kumar",
            Covers = 4,
            OrderType = OrderType.DineIn,
            Items = new List<CreateOrderItemDto>
            {
                new CreateOrderItemDto { MenuItemId = 10, SelectedVariantName = "Half", Quantity = 1 }, // ₹220 x 1
                new CreateOrderItemDto { MenuItemId = 11, Quantity = 4, SpecialInstructions = "Less butter" }, // ₹45 x 4 = ₹180
                new CreateOrderItemDto { MenuItemId = 12, Quantity = 1 } // ₹260 x 1
            }
        };

        var draft = await orderService.SaveDraftAsync(draftDto);
        Assert.NotNull(draft);
        Assert.True(draft.IsDraft);
        Assert.Equal(4, draft.Covers);

        // Verify Step 3 calculations in Section B:
        // Subtotal: 220 + 180 + 260 = 660.00
        // GST (5%): 33.00
        // Total: 693.00
        Assert.Equal(660.00m, draft.SubTotal);
        Assert.Equal(16.50m, draft.CGST);
        Assert.Equal(16.50m, draft.SGST);
        Assert.Equal(33.00m, draft.TotalTax);
        Assert.Equal(693.00m, draft.GrandTotal);

        // Table 04 is still Available because no KOT sent yet (Step 1 & OM-A01, OM-A02)
        var tableAfterDraft = await uow.Tables.GetByIdAsync(table04.Id);
        Assert.Equal(TableStatus.Available, tableAfterDraft!.Status);
        Assert.True(tableAfterDraft.HasActiveDraft);

        // -------------------------------------------------------------
        // Step 4: Taps Send KOT.
        // Table 04 becomes Occupied. ORD number final. KOT-0007 created.
        // Rows move to Section A as Sent.
        // -------------------------------------------------------------
        var kot1 = await orderService.SendKOTAsync(draft.Id);
        Assert.NotNull(kot1);
        Assert.Equal("KOT-0007", kot1.KOTNumber);
        Assert.Equal(KOTType.Initial, kot1.Type);

        var orderAfterKOT = await orderService.GetOrderByIdAsync(draft.Id);
        Assert.NotNull(orderAfterKOT);
        Assert.False(orderAfterKOT.IsDraft);
        Assert.Equal(OrderStatus.Confirmed, orderAfterKOT.Status);
        Assert.All(orderAfterKOT.Items, i => Assert.Equal(OrderItemStatus.Sent, i.Status));

        var tableAfterKOT = await uow.Tables.GetByIdAsync(table04.Id);
        Assert.Equal(TableStatus.Occupied, tableAfterKOT!.Status);

        // -------------------------------------------------------------
        // Step 5: Customer says: "Add 2 cold drinks." (₹60 x 2 = ₹120)
        // Subtotal ₹780.00, GST ₹39.00, total ₹819.00.
        // -------------------------------------------------------------
        var newColdDrinks = new List<CreateOrderItemDto>
        {
            new CreateOrderItemDto { MenuItemId = 13, Quantity = 2 }
        };

        // -------------------------------------------------------------
        // Step 6: Taps Update KOT.
        // KOT-0008 prints with label ADDITION, contains only 2 Cold Drinks. KOT-0007 untouched.
        // -------------------------------------------------------------
        var kot2 = await orderService.SendKOTAsync(orderAfterKOT.Id, newColdDrinks);
        Assert.NotNull(kot2);
        Assert.Equal("KOT-0008", kot2.KOTNumber);
        Assert.Equal(KOTType.Addition, kot2.Type);
        Assert.Single(kot2.Items);
        Assert.Equal(2, kot2.Items.First().Quantity);

        var orderAfterAddition = await orderService.GetOrderByIdAsync(orderAfterKOT.Id);
        Assert.Equal(780.00m, orderAfterAddition!.SubTotal);
        Assert.Equal(39.00m, orderAfterAddition.TotalTax);
        Assert.Equal(819.00m, orderAfterAddition.GrandTotal);

        // -------------------------------------------------------------
        // Step 7: Customer: "Make it 3 naan, not 4." Naan is still Sent.
        // Waiter selects ⋯ -> Reduce, quantity 3, reason "Customer changed mind",
        // ticks "I have informed the kitchen".
        // Cancel slip prints: "CANCELLED 1 × Butter Naan (Table 04)".
        // Subtotal ₹735.00, GST ₹36.75, total ₹771.75, round-off +₹0.25, payable ₹772.00.
        // -------------------------------------------------------------
        var naanItem = orderAfterAddition.Items.First(i => i.MenuItemId == 11);
        var reduceDto = new ReduceItemDto
        {
            OrderId = orderAfterAddition.Id,
            OrderItemId = naanItem.Id,
            ReduceByQuantity = 1,
            RemainingQuantity = 3,
            Reason = "Customer changed mind",
            InformedKitchen = true
        };

        var orderAfterReduce = await orderService.ReduceItemQuantityAsync(reduceDto);
        Assert.NotNull(orderAfterReduce);
        Assert.Equal(735.00m, orderAfterReduce.SubTotal);
        Assert.Equal(36.75m, orderAfterReduce.TotalTax);
        Assert.Equal(0.25m, orderAfterReduce.RoundOff);
        Assert.Equal(772.00m, orderAfterReduce.GrandTotal);

        // Verify cancel slip generated
        var cancelKot = (await uow.KOTs.FindAsync(k => k.OrderId == orderAfterReduce.Id && k.Type == KOTType.Cancellation)).FirstOrDefault();
        Assert.NotNull(cancelKot);
        Assert.Contains("CANCELLED 1 × Butter Naan", cancelKot.Notes);

        // -------------------------------------------------------------
        // Step 8: Kitchen finishes everything. Items are marked Served.
        // Table 04 stays Occupied until billing. Section A shows all rows Served.
        // -------------------------------------------------------------
        await orderService.MarkAllServedAsync(orderAfterReduce.Id);

        var orderAfterServed = await orderService.GetOrderByIdAsync(orderAfterReduce.Id);
        Assert.All(orderAfterServed!.Items.Where(i => i.Status != OrderItemStatus.Cancelled), i => Assert.Equal(OrderItemStatus.Served, i.Status));

        var tableAfterServed = await uow.Tables.GetByIdAsync(table04.Id);
        Assert.Equal(TableStatus.Occupied, tableAfterServed!.Status);

        // -------------------------------------------------------------
        // Step 9: Customer asks for the bill. Waiter taps Bill.
        // Invoice is generated, table turns Billing, bill prints.
        // -------------------------------------------------------------
        var bill = await orderService.GenerateBillFromPOSAsync(orderAfterServed.Id);
        Assert.NotNull(bill);
        Assert.False(string.IsNullOrEmpty(bill.InvoiceNumber));

        var tableAfterBill = await uow.Tables.GetByIdAsync(table04.Id);
        Assert.Equal(TableStatus.Billing, tableAfterBill!.Status);

        // -------------------------------------------------------------
        // Step 10: Cashier takes ₹500 cash and ₹272 UPI (reference entered).
        // Balance is ₹0.00. Order is Settled, table turns Cleaning or Available.
        // -------------------------------------------------------------
        // Payment 1: ₹500 Cash
        var payment1 = await billingService.CollectPaymentAsync(new CollectPaymentDto
        {
            BillId = bill.Id,
            PaymentMethod = PaymentMethod.Cash,
            Amount = 500.00m,
            Notes = "Split cash payment"
        });
        Assert.False(payment1.IsPaid); // Not fully paid yet

        // Payment 2: ₹272 UPI
        var payment2 = await billingService.CollectPaymentAsync(new CollectPaymentDto
        {
            BillId = bill.Id,
            PaymentMethod = PaymentMethod.UPI,
            Amount = 272.00m,
            TransactionId = "UPI-789012",
            Notes = "Split UPI payment"
        });
        Assert.True(payment2.IsPaid); // Now fully paid!
        Assert.Equal(772.00m, payment2.GrandTotal);

        var finalTable = await uow.Tables.GetByIdAsync(table04.Id);
        Assert.Equal(TableStatus.Available, finalTable!.Status);
    }
}
