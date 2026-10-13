using Microsoft.AspNetCore.Mvc;
using RestaurantManagement.Application.DTOs;
using RestaurantManagement.Application.Interfaces;
using RestaurantManagement.Domain.Entities;
using RestaurantManagement.Domain.Enums;

namespace RestaurantManagement.Web.Controllers;

public class OrdersController : Controller
{
    private readonly IOrderService _orderService;
    private readonly IMenuService _menuService;
    private readonly ITableService _tableService;
    private readonly IStaffService _staffService;
    private readonly IUnitOfWork _unitOfWork;

    public OrdersController(
        IOrderService orderService, 
        IMenuService menuService, 
        ITableService tableService,
        IStaffService staffService,
        IUnitOfWork unitOfWork)
    {
        _orderService = orderService;
        _menuService = menuService;
        _tableService = tableService;
        _staffService = staffService;
        _unitOfWork = unitOfWork;
    }

    public async Task<IActionResult> Index(OrderStatus? status, OrderType? type)
    {
        var orders = await _orderService.GetOrdersAsync(status, type);
        var settings = await _unitOfWork.RestaurantSettings.GetAllAsync();
        ViewBag.Setting = settings.FirstOrDefault() ?? new RestaurantSetting();
        return View(orders);
    }

    public async Task<IActionResult> POS(int? tableId, int? orderId = null, string? returnUrl = null)
    {
        ViewBag.Categories = await _menuService.GetCategoriesAsync();
        ViewBag.MenuItems = await _menuService.GetMenuItemsAsync();
        ViewBag.Tables = await _tableService.GetAllTablesAsync();
        ViewBag.SelectedTableId = tableId;
        ViewBag.ReturnUrl = string.IsNullOrWhiteSpace(returnUrl) ? "/Dashboard" : returnUrl;

        var employees = await _staffService.GetEmployeesAsync();
        ViewBag.Waiters = employees.Where(e => e.Role == UserRole.Waiter || e.Role == UserRole.Manager || e.Role == UserRole.Cashier).ToList();

        var settings = await _unitOfWork.RestaurantSettings.GetAllAsync();
        var setting = settings.FirstOrDefault() ?? new RestaurantSetting();
        ViewBag.Setting = setting;

        TableDto? selectedTable = null;
        if (tableId.HasValue)
        {
            selectedTable = await _tableService.GetTableByIdAsync(tableId.Value);
        }
        ViewBag.SelectedTable = selectedTable;

        OrderDto? activeOrder = null;
        if (orderId.HasValue)
        {
            activeOrder = await _orderService.GetOrderByIdAsync(orderId.Value);
        }
        else if (tableId.HasValue)
        {
            if (selectedTable?.CurrentOrderId.HasValue == true)
            {
                activeOrder = await _orderService.GetOrderByIdAsync(selectedTable.CurrentOrderId.Value);
            }
            if (activeOrder == null || activeOrder.IsSettled || activeOrder.Status == OrderStatus.Completed || activeOrder.Status == OrderStatus.Cancelled)
            {
                var allOrders = await _orderService.GetOrdersAsync();
                activeOrder = allOrders.FirstOrDefault(o => o.TableId == tableId.Value && !o.IsSettled && o.Status != OrderStatus.Completed && o.Status != OrderStatus.Cancelled);
            }
        }
        ViewBag.ActiveOrder = activeOrder;

        OrderDto? activeDraft = null;
        if (activeOrder == null && tableId.HasValue)
        {
            activeDraft = await _orderService.GetDraftAsync(tableId);
        }
        ViewBag.ActiveDraft = activeDraft;

        return View();
    }

    [HttpGet]
    public async Task<IActionResult> GetOrderDetails(int id)
    {
        var order = await _orderService.GetOrderByIdAsync(id);
        if (order == null) return NotFound(new { success = false, message = "Order not found" });
        return Json(new { success = true, order });
    }

    [HttpPost]
    public async Task<IActionResult> CreatePOSOrder([FromBody] CreateOrderDto dto)
    {
        try
        {
            var order = await _orderService.CreateOrderAsync(dto);
            return Json(new { success = true, orderId = order.Id, orderNumber = order.OrderNumber, order });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> SaveDraft([FromBody] SaveDraftOrderDto dto)
    {
        try
        {
            var draft = await _orderService.SaveDraftAsync(dto);
            return Json(new { success = true, draftId = draft.Id, draft });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetDraft(int? tableId, int? userId)
    {
        var draft = await _orderService.GetDraftAsync(tableId, userId);
        return Json(new { success = draft != null, draft });
    }

    [HttpPost]
    public async Task<IActionResult> DiscardDraft(int? tableId, int? userId, string? managerPin)
    {
        bool discarded = await _orderService.DiscardDraftAsync(tableId, userId, managerPin);
        return Json(new { success = discarded });
    }

    [HttpPost]
    public async Task<IActionResult> SendKOT([FromBody] SendKOTRequestDto dto)
    {
        try
        {
            var itemsToSend = dto.NewItems ?? dto.Items ?? new List<CreateOrderItemDto>();
            int orderId = dto.OrderId;

            // If orderId is not provided or <= 0, check if this table ALREADY has an active open order
            if (orderId <= 0 && dto.TableId.HasValue)
            {
                var table = await _tableService.GetTableByIdAsync(dto.TableId.Value);
                if (table?.CurrentOrderId.HasValue == true)
                {
                    var existing = await _orderService.GetOrderByIdAsync(table.CurrentOrderId.Value);
                    if (existing != null && !existing.IsSettled && existing.Status != OrderStatus.Completed && existing.Status != OrderStatus.Cancelled)
                    {
                        orderId = existing.Id;
                    }
                }
                
                if (orderId <= 0)
                {
                    var allOrders = await _orderService.GetOrdersAsync();
                    var existingOpenOrder = allOrders.FirstOrDefault(o => o.TableId == dto.TableId.Value && !o.IsSettled && o.Status != OrderStatus.Completed && o.Status != OrderStatus.Cancelled);
                    if (existingOpenOrder != null)
                    {
                        orderId = existingOpenOrder.Id;
                    }
                }
            }

            // If still no order exists for this table, check for draft or create initial order
            if (orderId <= 0 && dto.TableId.HasValue)
            {
                var draft = await _orderService.GetDraftAsync(dto.TableId.Value);
                if (draft != null)
                {
                    orderId = draft.Id;
                }
                else
                {
                    var newDraft = await _orderService.SaveDraftAsync(new SaveDraftOrderDto
                    {
                        TableId = dto.TableId.Value,
                        Covers = dto.Covers > 0 ? dto.Covers : 2,
                        OrderNote = dto.OrderNote ?? dto.Notes,
                        UserName = dto.WaiterName ?? "Staff",
                        Items = itemsToSend
                    });
                    orderId = newDraft.Id;
                    itemsToSend = null; // Already added as draft items
                }
            }

            var kot = await _orderService.SendKOTAsync(orderId, itemsToSend, dto.Notes, dto.IsRush);
            var updatedOrder = await _orderService.GetOrderByIdAsync(orderId);
            return Json(new { success = true, kotNumber = kot.KOTNumber, kot, order = updatedOrder, orderId = orderId });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> ReduceItem([FromBody] ReduceItemDto dto)
    {
        try
        {
            var order = await _orderService.ReduceItemQuantityAsync(dto);
            return Json(new { success = true, order });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> ReplaceItem([FromBody] ReplaceItemDto dto)
    {
        try
        {
            var order = await _orderService.ReplaceItemAsync(dto);
            return Json(new { success = true, order });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> ChangeItemNote([FromBody] ChangeItemNoteDto dto)
    {
        try
        {
            var order = await _orderService.ChangeItemNoteAsync(dto);
            return Json(new { success = true, order });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> UpdateItemStatus([FromBody] MarkItemStatusDto dto)
    {
        try
        {
            var order = await _orderService.UpdateItemStatusAsync(dto.OrderId, dto.OrderItemId, dto.Status);
            return Json(new { success = true, order });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> MarkAllServed(int orderId)
    {
        try
        {
            var order = await _orderService.MarkAllServedAsync(orderId);
            return Json(new { success = true, order });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> MakeItemComplimentary([FromBody] ComplimentaryItemDto dto)
    {
        try
        {
            var order = await _orderService.MakeItemComplimentaryAsync(dto);
            return Json(new { success = true, order });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> ReturnItem([FromBody] ReturnItemDto dto)
    {
        try
        {
            var order = await _orderService.ReturnItemAsync(dto);
            return Json(new { success = true, order });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> TransferOrMergeTable([FromBody] TableMoveOrMergeDto dto)
    {
        try
        {
            bool success = dto.IsMerge 
                ? await _orderService.MergeTablesAsync(dto.SourceTableId, dto.TargetTableId)
                : await _orderService.TransferTableAsync(dto.SourceTableId, dto.TargetTableId);

            return Json(new { success });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> CancelOrder(int orderId, string reason = "", string? managerPin = null)
    {
        try
        {
            bool success = await _orderService.CancelOrderAsync(orderId, reason, managerPin);
            return Json(new { success });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> ReopenOrder(int orderId, string reason, string? managerPin = null)
    {
        try
        {
            var order = await _orderService.ReopenOrderAsync(orderId, reason, managerPin);
            return Json(new { success = true, order });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> GenerateBillFromPOS([FromBody] BillFromPOSRequestDto dto)
    {
        try
        {
            var bill = await _orderService.GenerateBillFromPOSAsync(dto.OrderId, dto.DiscountAmount, dto.DiscountReason, dto.CustomerGstin);
            return Json(new { success = true, bill });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }
}
