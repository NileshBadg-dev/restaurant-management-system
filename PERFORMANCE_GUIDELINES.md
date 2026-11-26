# PERFORMANCE_GUIDELINES.md – Performance Optimization Rules

This document outlines the performance best practices for developing and maintaining the **Restaurant Management System (Girana RMS)**.

---

## 1. Database & EF Core Performance

### 1. Avoid the N+1 Query Problem
- When querying entities with child collections (e.g., `Order` with `OrderItems`, `Table` with `Floor`), load related data in a single query using `.Include()` / `.ThenInclude()` or project directly into DTOs using `.Select()`.
- Never execute database queries inside `foreach` loops.

```csharp
// Prohibited: Executes 1 query for orders + N queries for items
var orders = await _unitOfWork.Orders.GetAllAsync();
foreach (var order in orders)
{
    var items = await _unitOfWork.OrderItems.FindAsync(i => i.OrderId == order.Id); // N+1 hazard!
}

// Recommended: Project directly in a single database round-trip
var orderSummaries = await _dbContext.Orders
    .Where(o => !o.IsDeleted)
    .Select(o => new OrderSummaryDto
    {
        Id = o.Id,
        OrderNumber = o.OrderNumber,
        TotalItems = o.Items.Count(i => !i.IsDeleted),
        GrandTotal = o.GrandTotal
    })
    .ToListAsync();
```

### 2. Read-Only Queries with `.AsNoTracking()`
- For queries that only read data to render a view or return a DTO, use `.AsNoTracking()`.
- Disabling change tracking reduces memory overhead by 40–60% and eliminates EF Core identity snapshot comparisons.

### 3. Retrieve Only Required Columns (Projection)
- Avoid loading complete entity graphs with heavy columns if only a few fields are needed.
- Project directly to DTOs in the query to minimize network transfer and memory allocations.

### 4. Database-Side Aggregation
- Perform counts, sums, and existence checks directly on the database:
  - Use `await query.AnyAsync()` instead of `(await query.ToListAsync()).Any()`.
  - Use `await query.CountAsync()` instead of `(await query.ToListAsync()).Count`.
  - Use `await query.SumAsync(...)` instead of loading collections into memory.

---

## 2. LINQ & Collection Efficiency

1. **Filter Before Sorting or Materializing**
   - Always place `.Where(...)` before `.OrderBy(...)` and `.ToList()` so the database filters data before sorting and transmission.
2. **Pre-Size Collections When Size Is Known**
   - When instantiating `List<T>` with a known item count, specify capacity: `new List<DailySalesSummaryDto>(7)`.
3. **Use Dictionaries for In-Memory Lookups**
   - When mapping relationships in memory across collections, convert lookup data to a `Dictionary<int, T>` instead of calling `.FirstOrDefault(...)` repeatedly inside loops:

```csharp
// Fast O(1) lookups
var tableDict = tables.ToDictionary(t => t.Id, t => t.TableNumber);
foreach (var order in activeOrders)
{
    string tableNum = order.TableId.HasValue && tableDict.TryGetValue(order.TableId.Value, out var num) ? num : "";
}
```

---

## 3. Caching Strategy

- **Master & Static Data**: Categories, floor layouts, restaurant settings, and taxes change infrequently. Use `IMemoryCache` with a sliding expiration (e.g., 30 minutes).
- **Cache Invalidation**: Always evict or update cached items immediately when an update, insert, or delete operation occurs in that domain.
- **Do Not Cache Dynamic Data**: Active table orders, KOT slips, and live kitchen states must never be cached with long expirations to prevent stale state conflicts between waiters and kitchen staff.

---

## 4. Memory & Resource Management

1. **Dispose Disposable Resources**
   - Use `using` statements for `DbContext`, streams, HTTP responses, and database transactions:
     ```csharp
     using var scope = app.Services.CreateScope();
     ```
2. **Avoid Large Object Heap (LOH) Allocations**
   - Allocations greater than 85,000 bytes go directly to the Large Object Heap (LOH) and trigger expensive Gen 2 garbage collections.
   - For string concatenation in loops or report generation, use `StringBuilder` with an estimated capacity.
3. **Pagination on Large Datasets**
   - All listing endpoints (Orders, Bills, Inventory History, Audit Logs) must support pagination (`pageNumber`, `pageSize`) using `.Skip((page - 1) * pageSize).Take(pageSize)`.

---

## 5. Build & Development Optimization

1. **NuGet Vulnerability Audit Settings**
   - Network vulnerability audits during `dotnet build` can hang for 50+ seconds in offline or restricted sandbox environments.
   - `Directory.Build.props` defines `<NuGetAudit>false</NuGetAudit>` at the solution root. Keep this enabled.
2. **Solution Specification**
   - Because the root folder contains both `.sln` and `.slnx`, always target the specific solution file:
     ```powershell
     dotnet build RestaurantManagementSystem.sln -maxcpucount:1
     ```
