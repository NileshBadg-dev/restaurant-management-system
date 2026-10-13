using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.Application.DTOs;
using RestaurantManagement.Application.Services;
using RestaurantManagement.Domain.Entities;
using RestaurantManagement.Domain.Enums;
using RestaurantManagement.Infrastructure.DbContext;
using RestaurantManagement.Infrastructure.Repositories;
using Xunit;

namespace RestaurantManagement.Tests;

public class ScenarioTests
{
    private (RestaurantDbContext context, UnitOfWork uow, OrderService orderService, BillingService billingService) CreateTestEnvironment(
        KitchenMode kitchenMode = KitchenMode.PrinterOnly,
        bool singlePersonMode = false,
        VoidApprovalSetting voidApproval = VoidApprovalSetting.ManagerPIN,
        BillingUnservedItemsSetting allowBillingUnserved = BillingUnservedItemsSetting.Ask)
    {
        var options = new DbContextOptionsBuilder<RestaurantDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new RestaurantDbContext(options);

        // Seed Setting
        context.RestaurantSettings.Add(new RestaurantSetting
        {
            RestaurantName = "Girana Garden",
            CGSTPercentage = 2.5m,
            SGSTPercentage = 2.5m,
            KitchenMode = kitchenMode,
            SinglePersonMode = singlePersonMode,
            VoidApproval = voidApproval,
            AllowBillingWithUnservedItems = allowBillingUnserved,
            ManagerPIN = "1234",
            DraftExpiryHours = 4
        });

        // Floors and Tables
        var floor = new Floor { Id = 1, Name = "Ground Floor" };
        var table1 = new Table { Id = 1, TableNumber = "01", Capacity = 4, FloorId = 1, Status = TableStatus.Available };
        var table2 = new Table { Id = 2, TableNumber = "02", Capacity = 2, FloorId = 1, Status = TableStatus.Available };
        var table3 = new Table { Id = 3, TableNumber = "03", Capacity = 6, FloorId = 1, Status = TableStatus.Available };
        context.Floors.Add(floor);
        context.Tables.AddRange(table1, table2, table3);

        // Users
        var waiter1 = new User { Id = 1, Username = "amit", FullName = "Amit Waiter", Role = UserRole.Waiter };
        var waiter2 = new User { Id = 2, Username = "priya", FullName = "Priya Waiter", Role = UserRole.Waiter };
        var manager = new User { Id = 3, Username = "vikram", FullName = "Vikram Manager", Role = UserRole.Manager };
        context.Users.AddRange(waiter1, waiter2, manager);

        // Categories & Menu Items
        var catStarters = new Category { Id = 1, Name = "Starters" };
        var catMains = new Category { Id = 2, Name = "Mains" };
        context.Categories.AddRange(catStarters, catMains);

        var itemTikka = new MenuItem { Id = 1, CategoryId = 1, Name = "Paneer Tikka", Price = 200m, IsAvailable = true };
        var itemNaan = new MenuItem { Id = 2, CategoryId = 2, Name = "Butter Naan", Price = 40m, IsAvailable = true };
        var itemSoldOut = new MenuItem { Id = 3, CategoryId = 1, Name = "Fish Fry", Price = 300m, IsAvailable = false };
        context.MenuItems.AddRange(itemTikka, itemNaan, itemSoldOut);

        context.SaveChanges();

        var uow = new UnitOfWork(context);
        var custService = new CustomerService(uow);
        var billingService = new BillingService(uow, custService);
        var orderService = new OrderService(uow, billingService);

        return (context, uow, orderService, billingService);
    }

    // =========================================================================
    // 5A: CREATING AN ORDER
    // =========================================================================

    [Fact]
    public async Task OM_A01_DineInOrder_OnAvailableTable_BecomesOccupied_AfterFirstKOT()
    {
        var (context, uow, orderService, _) = CreateTestEnvironment();

        var draft = await orderService.SaveDraftAsync(new SaveDraftOrderDto
        {
            TableId = 1,
            UserId = 1,
            UserName = "Amit",
            Covers = 2,
            Items = new List<CreateOrderItemDto> { new CreateOrderItemDto { MenuItemId = 1, Quantity = 1 } }
        });

        var tableBeforeKOT = await uow.Tables.GetByIdAsync(1);
        Assert.Equal(TableStatus.Available, tableBeforeKOT!.Status);

        var kot = await orderService.SendKOTAsync(draft.Id);
        Assert.NotNull(kot);

        var tableAfterKOT = await uow.Tables.GetByIdAsync(1);
        Assert.Equal(TableStatus.Occupied, tableAfterKOT!.Status);
        Assert.Equal(draft.Id, tableAfterKOT.CurrentOrderId);
    }

    [Fact]
    public async Task OM_A02_OpenOrderScreen_AddNothingAndLeave_NoOrderConsumed()
    {
        var (context, uow, orderService, _) = CreateTestEnvironment();

        // No draft created, or check existing drafts
        var draft = await orderService.GetDraftAsync(1, 1);
        Assert.Null(draft);

        var table = await uow.Tables.GetByIdAsync(1);
        Assert.Equal(TableStatus.Available, table!.Status);
    }

    [Fact]
    public async Task OM_A03_DraftAutoExpires_AfterConfiguredHours()
    {
        var (context, uow, orderService, _) = CreateTestEnvironment();

        var draft = await orderService.SaveDraftAsync(new SaveDraftOrderDto
        {
            TableId = 1,
            UserId = 1,
            UserName = "Amit",
            Covers = 2,
            Items = new List<CreateOrderItemDto> { new CreateOrderItemDto { MenuItemId = 1, Quantity = 1 } }
        });

        // Manually fast-forward DraftExpiresAt
        var order = await uow.Orders.GetByIdAsync(draft.Id);
        order!.DraftExpiresAt = DateTime.UtcNow.AddMinutes(-5);
        await uow.Orders.UpdateAsync(order);
        await uow.SaveChangesAsync();

        // Querying draft should detect expiry, mark deleted, and return null
        var expiredDraft = await orderService.GetDraftAsync(1, 1);
        Assert.Null(expiredDraft);

        var table = await uow.Tables.GetByIdAsync(1);
        Assert.False(table!.HasActiveDraft);
    }

    [Fact]
    public async Task OM_A04_DraftConflict_AnotherWaiterOpensTable_ShowsExistingDraftInfo()
    {
        var (context, uow, orderService, _) = CreateTestEnvironment();

        await orderService.SaveDraftAsync(new SaveDraftOrderDto
        {
            TableId = 1,
            UserId = 1,
            UserName = "Amit",
            Covers = 2,
            Items = new List<CreateOrderItemDto> { new CreateOrderItemDto { MenuItemId = 1, Quantity = 1 } }
        });

        var table = await uow.Tables.GetByIdAsync(1);
        Assert.True(table!.HasActiveDraft);
        Assert.Equal("Amit", table.ActiveDraftUserName);

        // Waiter 2 (Priya) fetches the draft for Table 1
        var draft = await orderService.GetDraftAsync(1);
        Assert.NotNull(draft);
        Assert.Equal("Amit", draft.DraftUserName);
    }

    [Fact]
    public async Task OM_A10_CoversExceedingCapacity_BlockedWithValidationError()
    {
        var (_, _, orderService, _) = CreateTestEnvironment();

        // Table 1 capacity is 4. Try covers = 6
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => orderService.SaveDraftAsync(new SaveDraftOrderDto
        {
            TableId = 1,
            Covers = 6, // Exceeds capacity of 4
            Items = new List<CreateOrderItemDto> { new CreateOrderItemDto { MenuItemId = 1, Quantity = 1 } }
        }));

        Assert.Contains("capacity", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task OM_A12_OutOfStockItem_BlockedFromAdding()
    {
        var (_, _, orderService, _) = CreateTestEnvironment();

        // Item 3 is sold out
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => orderService.SaveDraftAsync(new SaveDraftOrderDto
        {
            TableId = 1,
            Covers = 2,
            Items = new List<CreateOrderItemDto> { new CreateOrderItemDto { MenuItemId = 3, Quantity = 1 } }
        }));

        Assert.Contains("sold out or unavailable", ex.Message);
    }

    [Fact]
    public async Task OM_A16_SendKOT_DisabledWhenSectionBIsEmpty()
    {
        var (_, _, orderService, _) = CreateTestEnvironment();

        var draft = await orderService.SaveDraftAsync(new SaveDraftOrderDto
        {
            TableId = 1,
            Covers = 2,
            Items = new List<CreateOrderItemDto>() // Empty items
        });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => orderService.SendKOTAsync(draft.Id));
        Assert.Contains("disabled when Section B", ex.Message);
    }

    // =========================================================================
    // 5B: CHANGING AN ORDER AFTER KOT
    // =========================================================================

    [Fact]
    public async Task OM_B01_AddItemsToExistingOrder_GeneratesAdditionKOT_UntouchedInitialKOT()
    {
        var (context, uow, orderService, _) = CreateTestEnvironment();

        var draft = await orderService.SaveDraftAsync(new SaveDraftOrderDto
        {
            TableId = 1,
            Covers = 2,
            Items = new List<CreateOrderItemDto> { new CreateOrderItemDto { MenuItemId = 1, Quantity = 1 } }
        });
        var kot1 = await orderService.SendKOTAsync(draft.Id);
        Assert.Equal(KOTType.Initial, kot1.Type);

        // Customer adds 2 Naans
        var kot2 = await orderService.SendKOTAsync(draft.Id, new List<CreateOrderItemDto>
        {
            new CreateOrderItemDto { MenuItemId = 2, Quantity = 2 }
        });

        Assert.Equal(KOTType.Addition, kot2.Type);
        Assert.Single(kot2.Items);
        Assert.Equal(2, kot2.Items.First().Quantity);

        var kots = await uow.KOTs.FindAsync(k => k.OrderId == draft.Id && !k.IsDeleted);
        Assert.Equal(2, kots.Count());
    }

    [Fact]
    public async Task OM_B04_CancelOrReduceItem_InPreparingState_RequiresManagerPIN_AndMarksWastage()
    {
        var (context, uow, orderService, _) = CreateTestEnvironment(kitchenMode: KitchenMode.FullKDS);

        var draft = await orderService.SaveDraftAsync(new SaveDraftOrderDto
        {
            TableId = 1,
            Covers = 2,
            Items = new List<CreateOrderItemDto> { new CreateOrderItemDto { MenuItemId = 1, Quantity = 2 } }
        });
        await orderService.SendKOTAsync(draft.Id);

        var order = await orderService.GetOrderByIdAsync(draft.Id);
        var item = order!.Items.First();

        // Mark Preparing
        await orderService.UpdateItemStatusAsync(order.Id, item.Id, OrderItemStatus.Preparing);

        // Attempt reduce without PIN -> Unauthorized
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => orderService.ReduceItemQuantityAsync(new ReduceItemDto
        {
            OrderId = order.Id,
            OrderItemId = item.Id,
            ReduceByQuantity = 1,
            Reason = "Customer changed mind",
            ManagerPin = "wrong-pin"
        }));

        // Reduce with correct PIN -> Marks Wastage (OM-B04)
        var updated = await orderService.ReduceItemQuantityAsync(new ReduceItemDto
        {
            OrderId = order.Id,
            OrderItemId = item.Id,
            ReduceByQuantity = 1,
            Reason = "Burnt / Cancelled",
            ManagerPin = "1234"
        });

        var reducedItem = (await uow.OrderItems.GetByIdAsync(item.Id))!;
        Assert.True(reducedItem.IsWastage);
    }

    [Fact]
    public async Task OM_B06_CancelOrReduce_ServedItem_BlockedForRegularWaiter_AllowedOnlyAsBillAdjustment()
    {
        var (context, uow, orderService, _) = CreateTestEnvironment();

        var draft = await orderService.SaveDraftAsync(new SaveDraftOrderDto
        {
            TableId = 1,
            Covers = 2,
            Items = new List<CreateOrderItemDto> { new CreateOrderItemDto { MenuItemId = 1, Quantity = 1 } }
        });
        await orderService.SendKOTAsync(draft.Id);
        var order = await orderService.GetOrderByIdAsync(draft.Id);
        var item = order!.Items.First();

        // Mark item served
        await orderService.UpdateItemStatusAsync(order.Id, item.Id, OrderItemStatus.Served);

        // Attempt reduce without Manager PIN -> Unauthorized
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => orderService.ReduceItemQuantityAsync(new ReduceItemDto
        {
            OrderId = order.Id,
            OrderItemId = item.Id,
            ReduceByQuantity = 1,
            Reason = "Customer refused",
            ManagerPin = null
        }));

        // Reduce with Manager PIN -> Bill Adjustment
        var updatedOrder = await orderService.ReduceItemQuantityAsync(new ReduceItemDto
        {
            OrderId = order.Id,
            OrderItemId = item.Id,
            ReduceByQuantity = 1,
            Reason = "Cold food complaint - bill adjustment",
            ManagerPin = "1234"
        });

        var cancelledItem = (await uow.OrderItems.GetByIdAsync(item.Id))!;
        Assert.True(cancelledItem.IsBillAdjusted);
    }

    [Fact]
    public async Task OM_B07_ReplaceItem_CancelsOldAndAddsNew_CombinedKOT()
    {
        var (context, uow, orderService, _) = CreateTestEnvironment();

        var draft = await orderService.SaveDraftAsync(new SaveDraftOrderDto
        {
            TableId = 1,
            Covers = 2,
            Items = new List<CreateOrderItemDto> { new CreateOrderItemDto { MenuItemId = 1, Quantity = 1 } }
        });
        await orderService.SendKOTAsync(draft.Id);
        var order = await orderService.GetOrderByIdAsync(draft.Id);
        var oldItem = order!.Items.First();

        var updatedOrder = await orderService.ReplaceItemAsync(new ReplaceItemDto
        {
            OrderId = order.Id,
            OldOrderItemId = oldItem.Id,
            NewItem = new CreateOrderItemDto { MenuItemId = 2, Quantity = 2 },
            Reason = "Allergy to paneer"
        });

        var replacedOld = updatedOrder.Items.First(i => i.Id == oldItem.Id);
        Assert.Equal(OrderItemStatus.Cancelled, replacedOld.Status);

        var newItem = updatedOrder.Items.First(i => i.MenuItemId == 2);
        Assert.Equal(OrderItemStatus.Sent, newItem.Status);
        Assert.Equal(2, newItem.Quantity);
    }

    [Fact]
    public async Task OM_B09_B10_ChangeItemNote_GeneratesNoteChangeSlip_BlockedIfPreparing()
    {
        var (context, uow, orderService, _) = CreateTestEnvironment(kitchenMode: KitchenMode.FullKDS);

        var draft = await orderService.SaveDraftAsync(new SaveDraftOrderDto
        {
            TableId = 1,
            Covers = 2,
            Items = new List<CreateOrderItemDto> { new CreateOrderItemDto { MenuItemId = 1, Quantity = 1 } }
        });
        await orderService.SendKOTAsync(draft.Id);
        var order = await orderService.GetOrderByIdAsync(draft.Id);
        var item = order!.Items.First();

        // While Sent: Note change succeeds and prints NOTE CHANGE KOT (OM-B09)
        var updated = await orderService.ChangeItemNoteAsync(new ChangeItemNoteDto
        {
            OrderId = order.Id,
            OrderItemId = item.Id,
            NewNote = "Extra crispy please"
        });
        Assert.Equal("Extra crispy please", updated.Items.First().SpecialInstructions);

        var noteKot = (await uow.KOTs.FindAsync(k => k.OrderId == order.Id && k.Type == KOTType.NoteChange)).FirstOrDefault();
        Assert.NotNull(noteKot);
        Assert.Contains("NOTE CHANGE", noteKot.Notes);

        // When item enters Preparing: Note change blocked (OM-B10)
        await orderService.UpdateItemStatusAsync(order.Id, item.Id, OrderItemStatus.Preparing);
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => orderService.ChangeItemNoteAsync(new ChangeItemNoteDto
        {
            OrderId = order.Id,
            OrderItemId = item.Id,
            NewNote = "Make it mild"
        }));
        Assert.Contains("already started cooking", ex.Message);
    }

    [Fact]
    public async Task OM_B15_ModifySettledOrder_BlockedUnlessReopenedByManager()
    {
        var (context, uow, orderService, billingService) = CreateTestEnvironment();

        var draft = await orderService.SaveDraftAsync(new SaveDraftOrderDto
        {
            TableId = 1,
            Covers = 2,
            Items = new List<CreateOrderItemDto> { new CreateOrderItemDto { MenuItemId = 1, Quantity = 1 } }
        });
        await orderService.SendKOTAsync(draft.Id);
        await orderService.MarkAllServedAsync(draft.Id);
        var bill = await orderService.GenerateBillFromPOSAsync(draft.Id);

        // Settle bill
        await billingService.CollectPaymentAsync(new CollectPaymentDto
        {
            BillId = bill.Id,
            Amount = bill.GrandTotal,
            PaymentMethod = PaymentMethod.Cash
        });

        // Modifying settled order directly is blocked (OM-B15)
        await Assert.ThrowsAsync<InvalidOperationException>(() => orderService.AddItemToOrderAsync(draft.Id, new CreateOrderItemDto
        {
            MenuItemId = 2,
            Quantity = 1
        }));

        // Reopening order with Manager PIN restores ability to modify (OM-J12)
        var reopened = await orderService.ReopenOrderAsync(draft.Id, "Customer wanted more food after paying", managerPin: "1234");
        Assert.False(reopened.IsSettled);

        var orderAfterAdd = await orderService.AddItemToOrderAsync(draft.Id, new CreateOrderItemDto
        {
            MenuItemId = 2,
            Quantity = 1
        });
        Assert.Equal(2, orderAfterAdd.Items.Count);
    }

    // =========================================================================
    // 5C: SAME-PAGE ORDER SCREEN INTERACTIONS
    // =========================================================================

    [Fact]
    public async Task OM_C03_ItemNote_Max100CharactersEnforced()
    {
        var (_, _, orderService, _) = CreateTestEnvironment();

        var draft = await orderService.SaveDraftAsync(new SaveDraftOrderDto
        {
            TableId = 1,
            Covers = 2,
            Items = new List<CreateOrderItemDto> { new CreateOrderItemDto { MenuItemId = 1, Quantity = 1 } }
        });
        await orderService.SendKOTAsync(draft.Id);
        var order = await orderService.GetOrderByIdAsync(draft.Id);
        var item = order!.Items.First();

        string longNote = new string('A', 101);
        await Assert.ThrowsAsync<ArgumentException>(() => orderService.ChangeItemNoteAsync(new ChangeItemNoteDto
        {
            OrderId = order.Id,
            OrderItemId = item.Id,
            NewNote = longNote
        }));
    }

    [Fact]
    public async Task OM_C09_QuantityLimits_Between1And99Enforced()
    {
        var (_, _, orderService, _) = CreateTestEnvironment();

        // Quantity 0 rejected
        await Assert.ThrowsAsync<ArgumentException>(() => orderService.SaveDraftAsync(new SaveDraftOrderDto
        {
            TableId = 1,
            Covers = 2,
            Items = new List<CreateOrderItemDto> { new CreateOrderItemDto { MenuItemId = 1, Quantity = 0 } }
        }));

        // Quantity 100 rejected
        await Assert.ThrowsAsync<ArgumentException>(() => orderService.SaveDraftAsync(new SaveDraftOrderDto
        {
            TableId = 1,
            Covers = 2,
            Items = new List<CreateOrderItemDto> { new CreateOrderItemDto { MenuItemId = 1, Quantity = 100 } }
        }));
    }

    // =========================================================================
    // 5D: SMALL-RESTAURANT & KITCHEN MODE DIFFERENCES
    // =========================================================================

    [Fact]
    public async Task OM_D02_Mode2_PrinterOnly_SkipsPreparingStatus()
    {
        var (_, _, orderService, _) = CreateTestEnvironment(kitchenMode: KitchenMode.PrinterOnly);

        var draft = await orderService.SaveDraftAsync(new SaveDraftOrderDto
        {
            TableId = 1,
            Covers = 2,
            Items = new List<CreateOrderItemDto> { new CreateOrderItemDto { MenuItemId = 1, Quantity = 1 } }
        });
        await orderService.SendKOTAsync(draft.Id);
        var order = await orderService.GetOrderByIdAsync(draft.Id);
        var item = order!.Items.First();

        // Preparing status blocked in Mode 2
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => 
            orderService.UpdateItemStatusAsync(order.Id, item.Id, OrderItemStatus.Preparing));
        Assert.Contains("Mode 2 (Printer only) does not use Preparing status", ex.Message);

        // Ready and Served are allowed
        var updated = await orderService.UpdateItemStatusAsync(order.Id, item.Id, OrderItemStatus.Ready);
        Assert.Equal(OrderItemStatus.Ready, updated.Items.First().Status);
    }

    [Fact]
    public async Task OM_D03_Mode3_NoKitchenDevice_SkipsPreparingAndReady_GoesStraightToServed()
    {
        var (_, _, orderService, _) = CreateTestEnvironment(kitchenMode: KitchenMode.NoDevice);

        var draft = await orderService.SaveDraftAsync(new SaveDraftOrderDto
        {
            TableId = 1,
            Covers = 2,
            Items = new List<CreateOrderItemDto> { new CreateOrderItemDto { MenuItemId = 1, Quantity = 1 } }
        });
        await orderService.SendKOTAsync(draft.Id);
        var order = await orderService.GetOrderByIdAsync(draft.Id);
        var item = order!.Items.First();

        // Preparing and Ready blocked in Mode 3
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            orderService.UpdateItemStatusAsync(order.Id, item.Id, OrderItemStatus.Preparing));
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            orderService.UpdateItemStatusAsync(order.Id, item.Id, OrderItemStatus.Ready));

        // Directly served
        var served = await orderService.UpdateItemStatusAsync(order.Id, item.Id, OrderItemStatus.Served);
        Assert.Equal(OrderItemStatus.Served, served.Items.First().Status);
    }

    [Fact]
    public async Task OM_D04_AllowBillingWithUnservedItems_SettingEnforced()
    {
        // When setting is 'No': billing blocked if items are unserved
        var (_, _, orderServiceNo, _) = CreateTestEnvironment(allowBillingUnserved: BillingUnservedItemsSetting.No);
        var draft = await orderServiceNo.SaveDraftAsync(new SaveDraftOrderDto
        {
            TableId = 1,
            Covers = 2,
            Items = new List<CreateOrderItemDto> { new CreateOrderItemDto { MenuItemId = 1, Quantity = 1 } }
        });
        await orderServiceNo.SendKOTAsync(draft.Id);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => orderServiceNo.GenerateBillFromPOSAsync(draft.Id));
        Assert.Contains("Some items have not been marked Served", ex.Message);

        // When setting is 'Ask' / 'Yes': auto-marks unserved items as Served upon billing
        var (_, _, orderServiceAsk, _) = CreateTestEnvironment(allowBillingUnserved: BillingUnservedItemsSetting.Ask);
        var draftAsk = await orderServiceAsk.SaveDraftAsync(new SaveDraftOrderDto
        {
            TableId = 1,
            Covers = 2,
            Items = new List<CreateOrderItemDto> { new CreateOrderItemDto { MenuItemId = 1, Quantity = 1 } }
        });
        await orderServiceAsk.SendKOTAsync(draftAsk.Id);
        var bill = await orderServiceAsk.GenerateBillFromPOSAsync(draftAsk.Id);
        Assert.NotNull(bill);

        var billedOrder = await orderServiceAsk.GetOrderByIdAsync(draftAsk.Id);
        Assert.All(billedOrder!.Items, i => Assert.Equal(OrderItemStatus.Served, i.Status));
    }

    [Fact]
    public async Task OM_D05_Mode3_ReduceItem_RequiresInformedKitchenCheck()
    {
        var (_, _, orderService, _) = CreateTestEnvironment(kitchenMode: KitchenMode.NoDevice);

        var draft = await orderService.SaveDraftAsync(new SaveDraftOrderDto
        {
            TableId = 1,
            Covers = 2,
            Items = new List<CreateOrderItemDto> { new CreateOrderItemDto { MenuItemId = 1, Quantity = 2 } }
        });
        await orderService.SendKOTAsync(draft.Id);
        var order = await orderService.GetOrderByIdAsync(draft.Id);
        var item = order!.Items.First();

        // Not ticked -> rejected (FR-OM-05, OM-D05)
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => orderService.ReduceItemQuantityAsync(new ReduceItemDto
        {
            OrderId = order.Id,
            OrderItemId = item.Id,
            ReduceByQuantity = 1,
            Reason = "Customer mind changed",
            InformedKitchen = false
        }));
        Assert.Contains("I have informed the kitchen", ex.Message);

        // Ticked -> accepted
        var updated = await orderService.ReduceItemQuantityAsync(new ReduceItemDto
        {
            OrderId = order.Id,
            OrderItemId = item.Id,
            ReduceByQuantity = 1,
            Reason = "Customer mind changed",
            InformedKitchen = true
        });
        Assert.NotNull(updated);
    }

    [Fact]
    public async Task OM_D09_SinglePersonMode_AutoPassesManagerApprovals()
    {
        var (_, _, orderService, _) = CreateTestEnvironment(singlePersonMode: true, voidApproval: VoidApprovalSetting.ManagerPIN);

        var draft = await orderService.SaveDraftAsync(new SaveDraftOrderDto
        {
            TableId = 1,
            Covers = 2,
            Items = new List<CreateOrderItemDto> { new CreateOrderItemDto { MenuItemId = 1, Quantity = 1 } }
        });
        await orderService.SendKOTAsync(draft.Id);

        // Cancel order after KOT normally requires manager PIN, but single-person mode bypasses it
        bool cancelled = await orderService.CancelOrderAsync(draft.Id, "Guest left suddenly", managerPin: null);
        Assert.True(cancelled);
    }

    // =========================================================================
    // 5E: TABLE OPERATIONS
    // =========================================================================

    [Fact]
    public async Task OM_E01_TableTransfer_MovesOrderToAvailableTable_GeneratesNotification()
    {
        var (context, uow, orderService, _) = CreateTestEnvironment();

        var draft = await orderService.SaveDraftAsync(new SaveDraftOrderDto
        {
            TableId = 1,
            Covers = 2,
            Items = new List<CreateOrderItemDto> { new CreateOrderItemDto { MenuItemId = 1, Quantity = 1 } }
        });
        await orderService.SendKOTAsync(draft.Id);

        // Move Table 01 to Table 02
        bool success = await orderService.TransferTableAsync(1, 2);
        Assert.True(success);

        var t1 = await uow.Tables.GetByIdAsync(1);
        var t2 = await uow.Tables.GetByIdAsync(2);
        Assert.Equal(TableStatus.Available, t1!.Status);
        Assert.Null(t1.CurrentOrderId);

        Assert.Equal(TableStatus.Occupied, t2!.Status);
        Assert.Equal(draft.Id, t2.CurrentOrderId);

        // Verify KOT notification generated
        var transferKot = (await uow.KOTs.FindAsync(k => k.OrderId == draft.Id && k.TableNumber == "02")).LastOrDefault();
        Assert.NotNull(transferKot);
        Assert.Contains("Moved from Table 01 to Table 02", transferKot.Notes);
    }

    [Fact]
    public async Task OM_E02_MergeTables_CombinesItemsAndCovers_ReleasesSourceTable()
    {
        var (context, uow, orderService, _) = CreateTestEnvironment();

        // Order 1 on Table 1 (2 covers, Paneer Tikka ₹200)
        var draft1 = await orderService.SaveDraftAsync(new SaveDraftOrderDto
        {
            TableId = 1,
            Covers = 2,
            Items = new List<CreateOrderItemDto> { new CreateOrderItemDto { MenuItemId = 1, Quantity = 1 } }
        });
        await orderService.SendKOTAsync(draft1.Id);

        // Order 2 on Table 2 (1 cover, Butter Naan ₹40 x 2 = ₹80)
        var draft2 = await orderService.SaveDraftAsync(new SaveDraftOrderDto
        {
            TableId = 2,
            Covers = 1,
            Items = new List<CreateOrderItemDto> { new CreateOrderItemDto { MenuItemId = 2, Quantity = 2 } }
        });
        await orderService.SendKOTAsync(draft2.Id);

        // Merge Table 2 into Table 1
        bool success = await orderService.MergeTablesAsync(2, 1);
        Assert.True(success);

        var t1 = await uow.Tables.GetByIdAsync(1);
        var t2 = await uow.Tables.GetByIdAsync(2);
        Assert.Equal(TableStatus.Occupied, t1!.Status);
        Assert.Equal(TableStatus.Available, t2!.Status);

        var mergedOrder = await orderService.GetOrderByIdAsync(draft1.Id);
        Assert.Equal(3, mergedOrder!.Covers); // 2 + 1
        Assert.Equal(280m, mergedOrder.SubTotal); // 200 + 80
        Assert.Equal(2, mergedOrder.Items.Count);
    }

    // =========================================================================
    // 5F: CANCELLATIONS & WASTAGE
    // =========================================================================

    [Fact]
    public async Task OM_F01_BeforeKOT_DraftCancelledFreely_NoApprovalRequired()
    {
        var (context, uow, orderService, _) = CreateTestEnvironment();

        var draft = await orderService.SaveDraftAsync(new SaveDraftOrderDto
        {
            TableId = 1,
            Covers = 2,
            Items = new List<CreateOrderItemDto> { new CreateOrderItemDto { MenuItemId = 1, Quantity = 1 } }
        });

        bool cancelled = await orderService.CancelOrderAsync(draft.Id, "Guests left before order placed");
        Assert.True(cancelled);

        var table = await uow.Tables.GetByIdAsync(1);
        Assert.Equal(TableStatus.Available, table!.Status);
    }

    [Fact]
    public async Task OM_F04_CancelOrder_BlockedIfAnyItemIsServed()
    {
        var (context, uow, orderService, _) = CreateTestEnvironment();

        var draft = await orderService.SaveDraftAsync(new SaveDraftOrderDto
        {
            TableId = 1,
            Covers = 2,
            Items = new List<CreateOrderItemDto> { new CreateOrderItemDto { MenuItemId = 1, Quantity = 1 } }
        });
        await orderService.SendKOTAsync(draft.Id);
        await orderService.MarkAllServedAsync(draft.Id);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => 
            orderService.CancelOrderAsync(draft.Id, "Guests walked out after eating", managerPin: "1234"));
        Assert.Contains("Cannot cancel an order after items have been Served", ex.Message);
    }

    [Fact]
    public async Task OM_F05_CustomerReturnsItem_RemakeGeneratesNoChargeKOT()
    {
        var (context, uow, orderService, _) = CreateTestEnvironment();

        var draft = await orderService.SaveDraftAsync(new SaveDraftOrderDto
        {
            TableId = 1,
            Covers = 2,
            Items = new List<CreateOrderItemDto> { new CreateOrderItemDto { MenuItemId = 1, Quantity = 1 } }
        });
        await orderService.SendKOTAsync(draft.Id);
        var order = await orderService.GetOrderByIdAsync(draft.Id);
        var item = order!.Items.First();

        var updated = await orderService.ReturnItemAsync(new ReturnItemDto
        {
            OrderId = order.Id,
            OrderItemId = item.Id,
            IsRemake = true,
            Reason = "Food was too salty",
            ManagerPin = "1234"
        });

        // Subtotal untouched for remade dish (no extra charge)
        Assert.Equal(200m, updated.SubTotal);

        var remakeKot = (await uow.KOTs.FindAsync(k => k.OrderId == order.Id && k.Type == KOTType.Remake)).FirstOrDefault();
        Assert.NotNull(remakeKot);
        Assert.Contains("REMAKE", remakeKot.Notes);
    }

    // =========================================================================
    // 5H: BILLING HAND-OFF & SETTLEMENT
    // =========================================================================

    [Fact]
    public async Task OM_H04_CustomerGSTIN_ValidationEnforced()
    {
        var (_, _, orderService, _) = CreateTestEnvironment();

        var draft = await orderService.SaveDraftAsync(new SaveDraftOrderDto
        {
            TableId = 1,
            Covers = 2,
            Items = new List<CreateOrderItemDto> { new CreateOrderItemDto { MenuItemId = 1, Quantity = 1 } }
        });
        await orderService.SendKOTAsync(draft.Id);
        await orderService.MarkAllServedAsync(draft.Id);

        // Invalid GSTIN throws ArgumentException
        await Assert.ThrowsAsync<ArgumentException>(() => 
            orderService.GenerateBillFromPOSAsync(draft.Id, customerGstin: "INVALID-GSTIN-123"));

        // Valid 15-char GSTIN succeeds
        var bill = await orderService.GenerateBillFromPOSAsync(draft.Id, customerGstin: "27AAAAA0000A1Z5");
        Assert.NotNull(bill);
    }

    [Fact]
    public async Task OM_H05_SplitPayment_SettlesOrderToZeroBalance()
    {
        var (context, uow, orderService, billingService) = CreateTestEnvironment();

        var draft = await orderService.SaveDraftAsync(new SaveDraftOrderDto
        {
            TableId = 1,
            Covers = 2,
            Items = new List<CreateOrderItemDto> { new CreateOrderItemDto { MenuItemId = 1, Quantity = 2 } } // 2 x 200 = 400
        });
        await orderService.SendKOTAsync(draft.Id);
        await orderService.MarkAllServedAsync(draft.Id);

        // Subtotal ₹400, GST ₹20, Grand Total ₹420
        var bill = await orderService.GenerateBillFromPOSAsync(draft.Id);
        Assert.Equal(420m, bill.GrandTotal);

        // Split 1: ₹200 Cash
        var pay1 = await billingService.CollectPaymentAsync(new CollectPaymentDto
        {
            BillId = bill.Id,
            PaymentMethod = PaymentMethod.Cash,
            Amount = 200m
        });
        Assert.False(pay1.IsPaid);

        // Split 2: ₹220 UPI
        var pay2 = await billingService.CollectPaymentAsync(new CollectPaymentDto
        {
            BillId = bill.Id,
            PaymentMethod = PaymentMethod.UPI,
            Amount = 220m,
            TransactionId = "UPI-123456"
        });
        Assert.True(pay2.IsPaid);

        var table = await uow.Tables.GetByIdAsync(1);
        Assert.Equal(TableStatus.Available, table!.Status);
    }

    // =========================================================================
    // 5J: EDGE CASES & GUARD RAILS
    // =========================================================================

    [Fact]
    public async Task OM_J08_DiscountCannotExceedSubtotal()
    {
        var (_, _, orderService, _) = CreateTestEnvironment();

        var draft = await orderService.SaveDraftAsync(new SaveDraftOrderDto
        {
            TableId = 1,
            Covers = 2,
            Items = new List<CreateOrderItemDto> { new CreateOrderItemDto { MenuItemId = 1, Quantity = 1 } } // Subtotal 200
        });
        await orderService.SendKOTAsync(draft.Id);
        await orderService.MarkAllServedAsync(draft.Id);

        // Discount 250 > Subtotal 200
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => 
            orderService.GenerateBillFromPOSAsync(draft.Id, discountAmount: 250m));
        Assert.Contains("Discount cannot exceed order subtotal", ex.Message);
    }

    [Fact]
    public async Task OM_J14_WalkoutOrder_MarkedUnpaid_ReleasesTable()
    {
        var (context, uow, orderService, _) = CreateTestEnvironment();

        var draft = await orderService.SaveDraftAsync(new SaveDraftOrderDto
        {
            TableId = 1,
            Covers = 2,
            Items = new List<CreateOrderItemDto> { new CreateOrderItemDto { MenuItemId = 1, Quantity = 1 } }
        });
        await orderService.SendKOTAsync(draft.Id);

        bool walkout = await orderService.MarkWalkoutAsync(new WalkoutOrderDto
        {
            OrderId = draft.Id,
            Reason = "Guests slipped away without paying",
            ManagerPin = "1234"
        });
        Assert.True(walkout);

        var table = await uow.Tables.GetByIdAsync(1);
        Assert.Equal(TableStatus.Available, table!.Status);

        var order = await orderService.GetOrderByIdAsync(draft.Id);
        Assert.True(order!.IsWalkout);
        Assert.Equal(OrderStatus.Cancelled, order.Status);
    }

    [Fact]
    public async Task OM_UserScenario_Table04_Add2Items_ThenAdd2MoreItems_StayInSameOrder_UntilBilled()
    {
        var (context, uow, orderService, billingService) = CreateTestEnvironment();

        // Step 1: Initial order on Table 1 with 2 items
        var draft = await orderService.SaveDraftAsync(new SaveDraftOrderDto
        {
            TableId = 1,
            Covers = 4,
            Items = new List<CreateOrderItemDto>
            {
                new CreateOrderItemDto { MenuItemId = 1, Quantity = 1 }, // Paneer Tikka ₹200
                new CreateOrderItemDto { MenuItemId = 2, Quantity = 1 }  // Butter Naan ₹40
            }
        });

        // Initial Send KOT
        var kot1 = await orderService.SendKOTAsync(draft.Id);
        Assert.Equal(KOTType.Initial, kot1.Type);

        var tableAfterFirstKOT = await uow.Tables.GetByIdAsync(1);
        Assert.Equal(TableStatus.Occupied, tableAfterFirstKOT!.Status);
        Assert.Equal(draft.Id, tableAfterFirstKOT.CurrentOrderId);

        var firstOrder = await orderService.GetOrderByIdAsync(draft.Id);
        Assert.NotNull(firstOrder);
        string originalOrderNumber = firstOrder.OrderNumber;
        Assert.Equal(240m, firstOrder.SubTotal); // 200 + 40
        Assert.Equal(2, firstOrder.Items.Count);

        // Step 2: Customer wants 2 more items (added to the SAME table order)
        var additionalItems = new List<CreateOrderItemDto>
        {
            new CreateOrderItemDto { MenuItemId = 2, Quantity = 2 } // 2 more Butter Naans (₹80)
        };

        var kot2 = await orderService.SendKOTAsync(firstOrder.Id, additionalItems);
        Assert.Equal(KOTType.Addition, kot2.Type);
        Assert.Single(kot2.Items); // KOT contains only the 2 new items
        Assert.Equal(2, kot2.Items.First().Quantity);

        // Step 3: Verify it is in the SAME order, NOT a new order
        var orderAfterAdditions = await orderService.GetOrderByIdAsync(firstOrder.Id);
        Assert.NotNull(orderAfterAdditions);
        Assert.Equal(originalOrderNumber, orderAfterAdditions.OrderNumber);
        Assert.Equal(firstOrder.Id, orderAfterAdditions.Id);
        Assert.Equal(320m, orderAfterAdditions.SubTotal); // 240 + 80 = 320
        Assert.Equal(3, orderAfterAdditions.Items.Count); // Total 3 item rows (1 Tikka, 1 Naan, 2 Naan)
        Assert.Equal(4, orderAfterAdditions.Items.Sum(i => i.Quantity)); // Total 4 item quantity

        // Table still points to the same order
        var tableAfterAddition = await uow.Tables.GetByIdAsync(1);
        Assert.Equal(firstOrder.Id, tableAfterAddition!.CurrentOrderId);
        Assert.Equal(TableStatus.Occupied, tableAfterAddition.Status);

        // Step 4: Order remains editable until bill is generated
        // Can change note:
        var itemToUpdate = orderAfterAdditions.Items.First();
        var orderWithNote = await orderService.ChangeItemNoteAsync(new ChangeItemNoteDto
        {
            OrderId = firstOrder.Id,
            OrderItemId = itemToUpdate.Id,
            NewNote = "Extra crispy"
        });
        Assert.Equal("Extra crispy", orderWithNote.Items.First(i => i.Id == itemToUpdate.Id).SpecialInstructions);

        // Step 5: Bill generated and settled
        await orderService.MarkAllServedAsync(firstOrder.Id);
        var bill = await orderService.GenerateBillFromPOSAsync(firstOrder.Id);
        Assert.NotNull(bill);

        var payment = await billingService.CollectPaymentAsync(new CollectPaymentDto
        {
            BillId = bill.Id,
            PaymentMethod = PaymentMethod.Cash,
            Amount = bill.GrandTotal
        });
        Assert.True(payment.IsPaid);

        // After billing and settlement, modifying order is blocked
        await Assert.ThrowsAsync<InvalidOperationException>(() => orderService.AddItemToOrderAsync(firstOrder.Id, new CreateOrderItemDto
        {
            MenuItemId = 1,
            Quantity = 1
        }));
    }
}

