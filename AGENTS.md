# AGENTS.md – Antigravity AI Assistant Guidelines

Welcome to the **Restaurant Management System (Girana RMS)** codebase. This document contains mandatory instructions for Antigravity and any AI agent working on this solution.

The primary objective is to **minimize execution time while delivering secure, clean, high-performance, and maintainable .NET 9 code** without breaking existing functionality or UI styling.

---

## 1. Core Operating Principles

1. **Read Relevant Instructions First**
   - Before modifying any code, consult the relevant project documentation:
     - [`PROJECT_ARCHITECTURE.md`](PROJECT_ARCHITECTURE.md) – System design, layer dependencies, entity relationships, and communication flow.
     - [`CODING_STANDARDS.md`](CODING_STANDARDS.md) – Class/file organization, naming standards, SOLID, async/await, and error handling.
     - [`PERFORMANCE_GUIDELINES.md`](PERFORMANCE_GUIDELINES.md) – EF Core query optimization, caching, pagination, and memory efficiency.
     - [`BUILD_AND_RUN.md`](BUILD_AND_RUN.md) – Build commands, test instructions, Visual Studio 2022 compatibility, and default credentials.
     - [`PROJECT_PROGRESS.md`](PROJECT_PROGRESS.md) – Current feature status, completed work, pending tasks, and architectural decisions.

2. **Understand Before Modifying**
   - Inspect existing implementations and understand the exact data and control flow before touching any file.
   - Do not make assumptions about data structures or business logic. Check entities, repository methods, and DTO contracts.

3. **Reuse Existing Infrastructure**
   - Always reuse existing services, repositories, `IUnitOfWork`, DTOs, ViewModels, and utility methods.
   - Do not create duplicate repositories, duplicate service interfaces, or redundant helper classes.

4. **Small, Focused, Incremental Changes**
   - Make targeted, minimal edits rather than rewriting large blocks of code.
   - Avoid modifying unrelated files, formatting entire files unnecessarily, or regenerating files that only need a one-line change.
   - Never rewrite working business logic without an explicit requirement.

5. **Targeted Code Inspection (Faster Development)**
   - **Do not repeatedly scan or list the entire repository** for small changes.
   - Navigate directly to the relevant project and class using the architecture documentation.
   - Inspect only the classes directly involved in the task and their immediate dependencies.

6. **Preserve Compatibility**
   - Maintain strict backward compatibility for API endpoints, DTO contracts, database schema, and Razor views.
   - Keep both [`RestaurantManagementSystem.sln`](RestaurantManagementSystem.sln) and [`RestaurantManagementSystem.slnx`](RestaurantManagementSystem.slnx) valid and synchronized.
   - Ensure seamless Visual Studio 2022 compatibility.

7. **No Unnecessary External Dependencies**
   - Do not install new NuGet packages unless explicitly requested or strictly required after evaluating existing framework capabilities.
   - Target `.NET 9.0` features and built-in ASP.NET Core libraries first.

8. **Documentation Maintenance**
   - Update [`PROJECT_PROGRESS.md`](PROJECT_PROGRESS.md) whenever a feature is implemented or modified.
   - Update [`PROJECT_ARCHITECTURE.md`](PROJECT_ARCHITECTURE.md) or [`CODING_STANDARDS.md`](CODING_STANDARDS.md) if architectural patterns or conventions are added or altered.

---

## 2. Solution Structure Quick Reference

```
RestaurantManagementSystem/
├── src/
│   ├── RestaurantManagement.Domain/          # Core Domain Entities, Enums, BaseEntity (No external dependencies)
│   ├── RestaurantManagement.Application/     # Interfaces, Services, DTOs, Business Rules, Validation
│   ├── RestaurantManagement.Infrastructure/  # EF Core DbContext, Repositories, UnitOfWork, DbInitializer
│   ├── RestaurantManagement.Web/             # ASP.NET Core MVC, Razor Views, Cookie Auth, POS UI
│   └── RestaurantManagement.API/             # ASP.NET Core Web API, Swagger, JWT Bearer Auth
├── tests/
│   └── RestaurantManagement.Tests/           # xUnit Unit & Scenario Integration Tests
├── RestaurantManagementSystem.sln            # Standard Visual Studio 2022 solution
├── RestaurantManagementSystem.slnx           # Modern XML-based solution format
├── Directory.Build.props                     # Global build settings (<NuGetAudit>false</NuGetAudit>)
└── scenario-tests.md                         # Detailed Order Management business scenarios & rules
```

---

## 3. Fast Development Workflow for Agents

- **Building**: Always specify the solution file explicitly to avoid multi-solution MSBuild conflicts:
  ```powershell
  dotnet build RestaurantManagementSystem.sln -maxcpucount:1
  ```
- **Testing**: Target the specific test project to bypass solution-level overhead:
  ```powershell
  dotnet test tests/RestaurantManagement.Tests/RestaurantManagement.Tests.csproj --no-build
  ```
- **Running Web App**:
  ```powershell
  dotnet run --project src/RestaurantManagement.Web/RestaurantManagement.Web.csproj
  ```
- **Avoid Package Hangs**: `Directory.Build.props` has `<NuGetAudit>false</NuGetAudit>` enabled to prevent 50+ second external network hangs during restore on airgapped or sandboxed environments. Do not remove this property.

---

## 4. Final Task Summary Format

After completing any task, provide a concise summary with the following 7 points:
1. **Changes Implemented**: High-level summary of what was accomplished.
2. **Files Created / Modified**: List of exact file paths.
3. **Refactoring Performed**: Code reorganization or cleanup applied.
4. **Performance & Security Notes**: Optimizations, query improvements, and safety measures.
5. **Build & Test Results**: Status of MSBuild and test execution.
6. **Verification Results**: Verification details (runtime / browser / API probe).
7. **Remaining Issues / Limitations**: Any pending configuration, credentials, or environmental limits.
