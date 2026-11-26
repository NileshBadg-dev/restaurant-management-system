# BUILD_AND_RUN.md – Building, Running & Testing Instructions

This document provides instructions for opening, building, running, debugging, and testing the **Restaurant Management System (Girana RMS)** using Visual Studio 2022 and the .NET CLI.

---

## 1. Visual Studio 2022 Compatibility

The project maintains two synchronized solution formats in the workspace root:

1. **`RestaurantManagementSystem.sln`** (Standard Visual Studio Format)
   - Fully compatible with Visual Studio 2022 (v17.0+).
   - Recommended default for opening the project.
2. **`RestaurantManagementSystem.slnx`** (Modern Solution Format)
   - Supported in newer Visual Studio 2022 preview/release versions.
3. **Multi-Project Launch Profile** (`RestaurantManagementSystem.slnLaunch.user`)
   - Pre-configured to automatically start `RestaurantManagement.Web` upon pressing `F5`.

### Opening in Visual Studio 2022
1. Open Visual Studio 2022.
2. Select **Open a project or solution**.
3. Choose [`RestaurantManagementSystem.sln`](RestaurantManagementSystem.sln).
4. Verify all 6 projects load without missing dependencies or warnings.
5. Set `RestaurantManagement.Web` as the Startup Project (right-click -> **Set as Startup Project**).
6. Press `Ctrl + F5` (Start Without Debugging) or `F5` (Start Debugging).

---

## 2. Command Line Instructions (.NET CLI)

### Build the Solution
Always specify the `.sln` file to avoid MSBuild file ambiguity:
```powershell
dotnet build RestaurantManagementSystem.sln -maxcpucount:1
```

### Run the Web MVC Application (POS & Management)
```powershell
dotnet run --project src/RestaurantManagement.Web/RestaurantManagement.Web.csproj
```
- **Access URLs**:
  - `https://localhost:7198`
  - `http://localhost:5288`
- Opens automatically to the Login page (`/Account/Login`). After login, redirects to `/Dashboard`.

### Run the Web API (REST Endpoints & Swagger)
```powershell
dotnet run --project src/RestaurantManagement.API/RestaurantManagement.API.csproj
```
- **Access URLs**:
  - `https://localhost:7082/swagger`
  - `http://localhost:5188/swagger`

### Run Unit and Scenario Tests
```powershell
dotnet test tests/RestaurantManagement.Tests/RestaurantManagement.Tests.csproj --no-build
```

---

## 3. Seeded Accounts & Default Credentials

On startup, `DbInitializer.SeedData()` automatically seeds the database with initial users:

| Username | Password | Role | Permissions / Accessible Pages |
| :--- | :--- | :--- | :--- |
| **`admin`** | `admin123` | **Admin** | Full access to all modules, masters, staff, reports |
| **`manager`** | `manager123` | **Manager** | Operations, approvals, discounts, billing, inventory |
| **`waiter`** | `waiter123` | **Waiter** | Table layout, POS ordering, KOT generation |
| **`cashier`** | `cashier123` | **Cashier** | Billing, payments, settlements, cash drawer |
| **`cook`** | `cook123` | **Cook / Kitchen** | Kitchen Display System (KDS), KOT order prep |

---

## 4. Database Setup & Persistence Modes

### Default: In-Memory Database
- Pre-configured by default for immediate, zero-friction local execution.
- Requires no external SQL Server installation.
- Automatically populated with sample tables, floors, categories, recipes, dishes, and inventory on each application startup.

### Optional: SQL Server Database
To switch from In-Memory to a persistent SQL Server instance:
1. Open `src/RestaurantManagement.Web/appsettings.json` (or `API/appsettings.json`).
2. Update the `ConnectionStrings:DefaultConnection`:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=localhost;Database=RestaurantDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
   }
   ```
3. In `Program.cs`, switch `options.UseInMemoryDatabase(...)` to `options.UseSqlServer(connectionString)`.

---

## 5. Troubleshooting & FAQ

### Issue: MSBuild Error MSB1011 (Multiple solutions detected)
- **Cause**: Both `.sln` and `.slnx` files exist in the root folder.
- **Fix**: Always specify the solution file explicitly: `dotnet build RestaurantManagementSystem.sln`.

### Issue: Error MSB3026 / MSB3021 (Cannot access DLL because it is used by another process)
- **Cause**: An active instance of `RestaurantManagement.Web.exe`, `RestaurantManagement.API.exe`, or `iisexpress.exe` is running in the background.
- **Fix**: Stop the running process before building:
  ```powershell
  Stop-Process -Name RestaurantManagement.Web, RestaurantManagement.API, iisexpress -ErrorAction SilentlyContinue
  ```

### Issue: NuGet audit hangs during build
- **Fix**: Ensure `<NuGetAudit>false</NuGetAudit>` is maintained in `Directory.Build.props`.
