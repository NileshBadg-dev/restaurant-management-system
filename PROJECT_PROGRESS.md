# PROJECT_PROGRESS.md – Project Status & Progress Tracker

This document tracks completed features, current architecture milestones, pending enhancements, and key technical decisions for the **Restaurant Management System (Girana RMS)**.

---

## 1. Project Snapshot

| Metric | Status | Details |
| :--- | :--- | :--- |
| **Framework Version** | .NET 9.0 | Target framework across all projects |
| **Solution Build** | ✅ **Succeeded** | 0 Warnings, 0 Errors across 6 projects |
| **Visual Studio 2022** | ✅ Compatible | Both `.sln` and `.slnx` formats supported |
| **Test Project** | ✅ Active | xUnit unit and scenario integration tests |
| **Primary Architecture** | Monolithic Layered | Clean Architecture (Domain, Application, Infrastructure, Web, API) |

---

## 2. Completed Modules & Features

### Core Modules
- [x] **Authentication & Role-Based Access Control (RBAC)**: Cookie authentication (`GiranaRMS.Auth`) for Web MVC, JWT Bearer authentication for Web API. Roles: `Admin`, `Manager`, `Waiter`, `Cashier`, `Cook`.
- [x] **Table & Floor Management**: Floor layout, table capacities, dynamic table status transitions (`Available`, `Occupied`, `Billing`, `Reserved`), table transfers, and table merging.
- [x] **Menu & Category Management**: Hierarchical categories, item availability toggles, Veg/Non-Veg indicators, item variants, and add-on modifiers.
- [x] **Order Management & POS**: Two-section layout (**Section A: Sent to Kitchen**, **Section B: Draft**). Dine-In, Takeaway, and Delivery order workflows. Draft auto-expiration and conflict warnings.
- [x] **Kitchen Operating Modes**:
  - Mode 1: Full KDS (Kitchen Display Screen + Printer).
  - Mode 2: Thermal Printer only (bypasses Preparing status).
  - Mode 3: No kitchen device / counter verbal communication.
- [x] **KOT Generation & Management**: Initial KOTs, Addition KOTs (only newly added items), Cancel/Reduce KOT slips with reason tracking and Manager PIN verification.
- [x] **Billing & Payments**: Invoice generation (`INV-XXXXX`), tax computation (CGST 2.5% + SGST 2.5%), round-off adjustments, split tender payments (Cash, UPI, Card, Loyalty Points).
- [x] **Customer & Loyalty System**: Customer directory, tier progression (Silver, Gold, Platinum), points earning (1 pt per ₹100 spent), and redemption.
- [x] **Inventory & Recipe Auto-Deduction**: Ingredient stock tracking, recipe-driven auto-deduction upon placing orders, low-stock alerts, and manual stock adjustment (IN/OUT/WASTAGE).
- [x] **Staff Management & Daily Operations**: Staff directory, role-based shifts, daily attendance recording (Check-in / Check-out), and expense tracking.
- [x] **Reports & Dashboard Analytics**: Daily/weekly sales trends, top-selling items, order type breakdown (Dine-in vs Takeaway vs Delivery), and live floor occupancy stats.

### Code Organization & Quality Improvements (Completed)
- [x] **One Class per File**: Extracted bundled controllers into separate files:
  - `TablesController.cs`, `FloorsController.cs`, `CategoriesController.cs`, `MenuItemsController.cs` (from `MenuAndTablesControllers.cs`).
  - `OrdersController.cs`, `KOTsController.cs`, `BillsController.cs`, `CustomersController.cs`, `ReservationsController.cs`, `InventoryController.cs`, `StaffController.cs`, `ReportsController.cs` (from `OrdersAndOperationsControllers.cs`).
- [x] **Interface Separation**: Split `IOperationsServices.cs` and `ICustomerService.cs` into individual dedicated interface files:
  - `IInventoryService.cs`, `IStaffService.cs`, `IDashboardService.cs`, `IReportService.cs`, `IReservationService.cs`.
- [x] **Service Separation**: Split multi-service files into distinct single-responsibility service classes:
  - `CustomerService.cs`, `ReservationService.cs`, `InventoryService.cs`, `StaffService.cs`, `DashboardService.cs`, `ReportService.cs`.
- [x] **Cleaned Boilerplate**: Removed empty template files (`Class1.cs` in Application, Domain, and Infrastructure, and `UnitTest1.cs` in Tests).

### UI/UX & Localization Enhancements (Completed)
- [x] **Hotel Kumbharwada Bilingual Branding & Earthy Theme (`Masters -> Restaurant Settings`)**: Updated default branding to **Hotel Kumbharwada / हॉटेल कुंभारवाडा** with the custom clay-pot circular logo (`/assets/hotel-logo.jpg`), bilingual identity fields (`RestaurantNameMr`, `TaglineMr`, `AddressMr`), logo upload (PNG/JPG/SVG/WebP <= 1 MB with live preview, Remove button, and initial-letter fallback), structured address/phone fields, and complete earthy palette (`#4A2314`, `#7A3B1D`, `#A94E23`, `#F6E4D3`, `#FFF8EE`, `#6B7F3A`).
- [x] **Simplified Login & Header/Sidebar Icon Rail**: Simplified `Login.cshtml` to a cream backdrop and single centered card (`English | मराठी` toggle inside card top-right, 96px logo, bilingual title, `User ID` & password with eye toggle, `Remember me` + `Forgot password?`, `Log in` button); removed role chips, Operations Mode banners, and `Switch User`. Updated sidebar to collapse cleanly on desktop (`>=1024px`) into a `72px` icon rail with 40px logo, tooltips, Masters flyout, thin dividers, and transparent `panel-left` toggle row below the header; mobile (`<1024px`) uses an overlay drawer. Kept standalone `English | मराठी` toggle always visible immediately left of the Role dropdown.
- [x] **Kitchen Mode Removal**: Removed `KitchenMode`, `TrackReadyStatus`, `KitchenLiteScreen`, `SinglePersonMode`, mode claims/columns/selectors, and POS/KDS mode branches cleanly while retaining tax, surcharge, PIN, and billing policy settings.
- [x] **Full Marathi Conversion & Devanagari Digits**: Added `USE_DEVANAGARI_DIGITS = true` (`mr-IN-u-nu-deva`), localized `NameMr`/`FullNameMr` on Floors, Tables, Categories, Menu Items, and Users, SQL migration `20261126_HotelKumbharwadaThemeAndMarathi.sql`, localhost missing-key console warnings, and `LocalizationConsistencyTests` xUnit verification.

---

## 3. Pending Tasks & Future Roadmap

| Task | Priority | Description | Target Component |
| :--- | :--- | :--- | :--- |
| **Login Localization & Simplification** | Done | Simplified card, `User ID` auth, instant `English \| मराठी` toggle & toast notifications. | `Views/Account/Login.cshtml`, `wwwroot/i18n/` |
| **72px Icon Rail & Panel Toggle** | Done | 200ms desktop rail with `Masters` flyout, tooltips, `localStorage` persistence, and `<1024px` overlay drawer. | `Views/Shared/_Layout.cshtml`, `wwwroot/css/site.css` |
| **Earthy Clay & Cream Theme** | Done | Replaced all green tokens with logo-matched `#4A2314`/`#A94E23`/`#FFF8EE`/`#6B7F3A` palette. | `wwwroot/css/site.css`, Razor Views |
| **SQL Server Migration Scripts** | Done | Added `20261126_AddRestaurantSettingsAndNameMr.sql` and `20261126_HotelKumbharwadaThemeAndMarathi.sql`. | `Infrastructure/Data/Migrations` |

---

## 4. Key Architectural Decisions (ADR)

1. **Dual Solution Support (`.sln` and `.slnx`)**
   - *Decision*: Maintain standard `.sln` as the primary solution for universal Visual Studio 2022 compatibility while retaining `.slnx` for modern IDE versions.
   - *Rationale*: Guarantees that team members on varying Visual Studio 2022 updates can build and run without migration friction.

2. **In-Memory Default with SQL Server Capability**
   - *Decision*: Default database configuration uses EF Core InMemory database seeded via `DbInitializer.cs`, with connection string toggle for SQL Server in `appsettings.json`.
   - *Rationale*: Allows instant out-of-the-box development, testing, and UI review without requiring a local SQL Server installation or administrative DB creation.

3. **Strict Interface & Service Segregation**
   - *Decision*: Each domain concern has its own dedicated interface (`IOrderService`, `IBillingService`, `ITableService`, `IInventoryService`, etc.) and implementation file.
   - *Rationale*: Eliminates tight coupling, allows focused unit testing, and prevents massive multi-thousand-line files.
