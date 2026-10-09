using Microsoft.AspNetCore.Mvc;
using RestaurantManagement.Application.DTOs;
using RestaurantManagement.Application.Interfaces;
using RestaurantManagement.Domain.Enums;

namespace RestaurantManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] OrderStatus? status, [FromQuery] OrderType? type)
    {
        var orders = await _orderService.GetOrdersAsync(status, type);
        return Ok(orders);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var order = await _orderService.GetOrderByIdAsync(id);
        if (order == null) return NotFound();
        return Ok(order);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrderDto dto)
    {
        var order = await _orderService.CreateOrderAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
    }

    [HttpPost("{id}/items")]
    public async Task<IActionResult> AddItem(int id, [FromBody] CreateOrderItemDto dto)
    {
        var order = await _orderService.AddItemToOrderAsync(id, dto);
        return Ok(order);
    }

    [HttpDelete("{id}/items/{orderItemId}")]
    public async Task<IActionResult> RemoveItem(int id, int orderItemId)
    {
        var order = await _orderService.RemoveItemFromOrderAsync(id, orderItemId);
        return Ok(order);
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] OrderStatus status)
    {
        var order = await _orderService.UpdateOrderStatusAsync(id, status);
        return Ok(order);
    }

    [HttpPost("{id}/kot")]
    public async Task<IActionResult> GenerateKOT(int id, [FromQuery] string notes = "")
    {
        var kot = await _orderService.GenerateKOTAsync(id, notes);
        return Ok(kot);
    }

    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> Cancel(int id, [FromQuery] string reason = "")
    {
        var result = await _orderService.CancelOrderAsync(id, reason);
        if (!result) return NotFound();
        return Ok(new { Message = "Order cancelled" });
    }
}

[ApiController]
[Route("api/[controller]")]
public class KOTsController : ControllerBase
{
    private readonly IKOTService _kotService;

    public KOTsController(IKOTService kotService)
    {
        _kotService = kotService;
    }

    [HttpGet]
    public async Task<IActionResult> GetActiveKOTs()
    {
        var kots = await _kotService.GetActiveKOTsAsync();
        return Ok(kots);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var kot = await _kotService.GetKOTByIdAsync(id);
        if (kot == null) return NotFound();
        return Ok(kot);
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] KOTStatus status)
    {
        var kot = await _kotService.UpdateKOTStatusAsync(id, status);
        return Ok(kot);
    }
}

[ApiController]
[Route("api/[controller]")]
public class BillsController : ControllerBase
{
    private readonly IBillingService _billingService;

    public BillsController(IBillingService billingService)
    {
        _billingService = billingService;
    }

    [HttpGet]
    public async Task<IActionResult> GetRecentBills()
    {
        var bills = await _billingService.GetRecentBillsAsync();
        return Ok(bills);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var bill = await _billingService.GetBillByIdAsync(id);
        if (bill == null) return NotFound();
        return Ok(bill);
    }

    [HttpPost]
    public async Task<IActionResult> GenerateBill([FromBody] CreateBillDto dto)
    {
        var bill = await _billingService.GenerateBillAsync(dto);
        return Ok(bill);
    }

    [HttpPost("payment")]
    public async Task<IActionResult> CollectPayment([FromBody] CollectPaymentDto dto)
    {
        var bill = await _billingService.CollectPaymentAsync(dto);
        return Ok(bill);
    }
}

[ApiController]
[Route("api/[controller]")]
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _customerService;

    public CustomersController(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var customers = await _customerService.GetAllCustomersAsync();
        return Ok(customers);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var customer = await _customerService.GetCustomerByIdAsync(id);
        if (customer == null) return NotFound();
        return Ok(customer);
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrUpdate([FromBody] CustomerCreateUpdateDto dto)
    {
        var customer = await _customerService.CreateOrUpdateCustomerAsync(dto);
        return Ok(customer);
    }
}

[ApiController]
[Route("api/[controller]")]
public class ReservationsController : ControllerBase
{
    private readonly IReservationService _reservationService;

    public ReservationsController(IReservationService reservationService)
    {
        _reservationService = reservationService;
    }

    [HttpGet]
    public async Task<IActionResult> GetReservations([FromQuery] DateTime? date, [FromQuery] ReservationStatus? status)
    {
        var res = await _reservationService.GetReservationsAsync(date, status);
        return Ok(res);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateReservationDto dto)
    {
        var res = await _reservationService.CreateReservationAsync(dto);
        return Ok(res);
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] ReservationStatus status)
    {
        var res = await _reservationService.UpdateReservationStatusAsync(id, status);
        return Ok(res);
    }
}

[ApiController]
[Route("api/[controller]")]
public class InventoryController : ControllerBase
{
    private readonly IInventoryService _inventoryService;

    public InventoryController(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var items = await _inventoryService.GetInventoryItemsAsync();
        return Ok(items);
    }

    [HttpGet("low-stock")]
    public async Task<IActionResult> GetLowStock()
    {
        var items = await _inventoryService.GetLowStockItemsAsync();
        return Ok(items);
    }

    [HttpPost("adjust")]
    public async Task<IActionResult> AdjustStock([FromBody] StockAdjustmentDto dto)
    {
        var item = await _inventoryService.AdjustStockAsync(dto);
        return Ok(item);
    }
}

[ApiController]
[Route("api/[controller]")]
public class StaffController : ControllerBase
{
    private readonly IStaffService _staffService;

    public StaffController(IStaffService staffService)
    {
        _staffService = staffService;
    }

    [HttpGet("employees")]
    public async Task<IActionResult> GetEmployees()
    {
        var employees = await _staffService.GetEmployeesAsync();
        return Ok(employees);
    }

    [HttpPost("employees")]
    public async Task<IActionResult> CreateEmployee([FromBody] EmployeeDto dto)
    {
        var employee = await _staffService.CreateEmployeeAsync(dto);
        return Ok(employee);
    }

    [HttpGet("expenses")]
    public async Task<IActionResult> GetExpenses([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        var expenses = await _staffService.GetExpensesAsync(startDate, endDate);
        return Ok(expenses);
    }

    [HttpPost("expenses")]
    public async Task<IActionResult> AddExpense([FromBody] ExpenseDto dto)
    {
        var exp = await _staffService.AddExpenseAsync(dto);
        return Ok(exp);
    }
}

[ApiController]
[Route("api/[controller]")]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpGet("sales")]
    public async Task<IActionResult> GetSalesReport([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        var start = startDate ?? DateTime.UtcNow.AddDays(-30);
        var end = endDate ?? DateTime.UtcNow;

        var report = await _reportService.GetSalesReportAsync(start, end);
        return Ok(report);
    }
}
