# CODING_STANDARDS.md – Code Quality, Conventions & Best Practices

This document sets the mandatory coding and architectural standards for the **Restaurant Management System (Girana RMS)**. All code additions and refactoring must adhere to these rules.

---

## 1. Class & File Organization

1. **One Primary Class per `.cs` File**
   - Every class, interface, enum, and record must reside in its own dedicated file.
   - The file name must match the type name exactly (e.g., `TablesController.cs` defines `public class TablesController`).
   - Grouping multiple unrelated classes or controllers in a single file is strictly prohibited.

2. **Folder Structure by Layer & Responsibility**
   - **Controllers**: `src/RestaurantManagement.API/Controllers/` or `src/RestaurantManagement.Web/Controllers/`
   - **Services**: `src/RestaurantManagement.Application/Services/`
   - **Interfaces**: `src/RestaurantManagement.Application/Interfaces/`
   - **DTOs**: `src/RestaurantManagement.Application/DTOs/`
   - **Entities**: `src/RestaurantManagement.Domain/Entities/`
   - **Enums**: `src/RestaurantManagement.Domain/Enums/`
   - **ViewModels**: `src/RestaurantManagement.Web/Models/`
   - **Repositories**: `src/RestaurantManagement.Infrastructure/Repositories/`

3. **No Unused Boilerplate**
   - Remove auto-generated template files (e.g., `Class1.cs`, empty `UnitTest1.cs`).
   - Remove dead code, unused using directives, and unreferenced local variables.

---

## 2. Naming Conventions

| Element | Convention | Example |
| :--- | :--- | :--- |
| **Classes & Records** | `PascalCase` | `OrderService`, `BillPaymentDto` |
| **Interfaces** | `IPascalCase` (`I` prefix) | `IOrderService`, `IUnitOfWork` |
| **Methods & Properties** | `PascalCase` | `GetActiveOrdersAsync()`, `GrandTotal` |
| **Private Fields** | `_camelCase` | `_unitOfWork`, `_tableService` |
| **Parameters & Locals** | `camelCase` | `orderId`, `customerPhone` |
| **Constants** | `PascalCase` | `DefaultTaxRate`, `MaxOrderItems` |
| **DTO Suffix** | Must end in `Dto` | `CreateOrderDto`, `TableTransferDto` |
| **ViewModel Suffix** | Must end in `ViewModel` | `MastersViewModel`, `StaffViewModel` |
| **Controller Suffix** | Must end in `Controller` | `OrdersController`, `KitchenController` |

---

## 3. SOLID & Clean Architecture Principles

- **Single Responsibility Principle (SRP)**: Each class should have one reason to change. Separate business calculations from database persistence and presentation logic.
- **Open / Closed Principle (OCP)**: Write code extensible through interfaces and strategy patterns rather than deeply nested conditionals.
- **Liskov Substitution Principle (LSP)**: Derived classes or interface implementations must fully honor the contracts of their base abstractions.
- **Interface Segregation Principle (ISP)**: Favor focused, role-specific interfaces (`IInventoryService`, `IStaffService`, `IReservationService`) over bloated "god" interfaces.
- **Dependency Inversion Principle (DIP)**: Depend on abstractions (`IUnitOfWork`, `ITableService`), not concrete implementations.

---

## 4. Dependency Injection Best Practices

- Always use **Constructor Injection** for dependencies.
- Mark injected private fields as `private readonly`.
- Register services with appropriate lifetimes in `Program.cs`:
  - `AddScoped`: For services, repositories, and `DbContext` (per HTTP request lifetime).
  - `AddSingleton`: For stateful caches or stateless thread-safe utilities.
  - `AddTransient`: For lightweight, stateless transient operations.
- Avoid the Service Locator anti-pattern (`IServiceProvider.GetService` in business code).

---

## 5. Asynchronous Programming Guidelines

1. **Async All the Way**
   - Any I/O-bound method (database queries, network calls, file reading) must be `async Task` or `async Task<T>`.
   - Method names must end with the `Async` suffix (e.g., `GetOrderByIdAsync`).
2. **No Blocking Calls**
   - **Never call `.Result` or `.Wait()`**. These cause thread pool starvation and deadlocks in ASP.NET Core request pipelines.
   - **Never use `async void`** (exceptions cannot be caught).
3. **Cancellation Tokens**
   - Pass `CancellationToken cancellationToken = default` down through controllers, services, and EF Core calls where applicable.

```csharp
// Correct
public async Task<OrderDto?> GetOrderByIdAsync(int id, CancellationToken ct = default)
{
    var order = await _unitOfWork.Orders.GetByIdAsync(id, ct);
    return order != null ? MapToDto(order) : null;
}

// Prohibited
public OrderDto? GetOrderById(int id)
{
    var order = _unitOfWork.Orders.GetByIdAsync(id).Result; // Thread stall hazard!
    return order != null ? MapToDto(order) : null;
}
```

---

## 6. Nullable Reference Types & Safety

- All projects have `<Nullable>enable</Nullable>` active.
- Explicitly annotate nullable types (`string?`, `Table?`).
- Use pattern matching (`if (order is null)`, `if (user is { IsActive: true })`).
- Use null-coalescing operators (`??`, `??=`) and null-conditional operators (`?.`).
- Avoid suppressing compiler warnings with `!` unless guaranteed non-null with justification.

---

## 7. Error Handling & Defensive Programming

- **Never use empty `catch` blocks**. Suppressing errors hides critical bugs.
- Throw strongly-typed exceptions:
  - `KeyNotFoundException` for missing entities.
  - `InvalidOperationException` for violated domain invariants (e.g. reserving an occupied table).
  - `ArgumentException` for invalid parameters.
- Web & API layers should catch expected exceptions and return standardized HTTP responses:
  - `NotFound()` or `NotFound(new { Message = "..." })`
  - `BadRequest(new { Message = "..." })`
- Do not expose internal database error details or raw stack traces to client browsers or API consumers.

---

## 8. Security Guidelines

1. **Zero Secrets in Code**
   - Never hardcode credentials, connection strings, API tokens, or JWT signing keys.
   - Use `appsettings.json`, environment variables, or Secret Manager.
2. **Prevent Injection Attacks**
   - Use parameterized queries or EF Core LINQ expressions exclusively. Never concatenate user input into raw SQL queries.
3. **Input Validation**
   - Validate request DTOs before executing business logic. Check range bounds, required fields, and string length limits.
4. **Authentication & Authorization**
   - Web application uses secure HTTP-only cookies (`GiranaRMS.Auth`) with `SameSiteMode.Lax`.
   - API endpoints use JWT Bearer authentication.
   - Apply role-based authorization attributes (`[Authorize(Roles = "Admin,Manager")]`) to protect sensitive administrative functions.
