# EshopModularMonoliths — Step-by-Step Build Log

> This document tracks every development step from project creation to the current state.
> It is meant to serve as a future reference: "if I start this from scratch, what do I do and in what order?"
>
> **How to maintain:** After finishing each logical development step, add a new section at the bottom.

---

## Step 0 — Repository Initialization

**What happened:** Created the git repository with just the essentials.

**Files created:**
- `.gitignore` — tells git which files/folders to ignore (e.g. `bin/`, `obj/`, `.vs/`)
- `README.md` — empty placeholder

---

## Step 1 — Solution & Project Setup

**What happened:** Created the Visual Studio solution and all projects inside `src/`. The architecture is a **Modular Monolith**: one deployable application (`Bootstrapper/Api`) that internally contains separate, isolated modules.

**Solution file:**
- `src/eshop-modular-monoliths.sln` — the Visual Studio solution that references all projects

**Projects created (each is a separate  `.csproj`):**

| Project | Path | Purpose |
|---|---|---|
| `Api` | `src/Bootstrapper/Api/` | The single entry point — hosts the ASP.NET Web API, composes all modules | Empty API project
| `Catalog` | `src/Modules/Catalog/Catalog/` | Catalog module (products) | Class Library
| `Basket` | `src/Modules/Basket/Basket/` | Basket module | Class Library
| `Ordering` | `src/Modules/Ordering/Ordering/` | Ordering module | Class Library
| `Shared` | `src/Shared/Shared/` | Shared library — base classes and contracts reused across all modules | Class Library

**Key structural decisions:**
- `Bootstrapper/Api` references all module projects via `<ProjectReference>` in its `.csproj`
- Module projects reference `Shared` (added later when needed)
- Each module started with just a placeholder `Class1.cs` — to be replaced with real code
- `Api` targets `net8.0` with `Microsoft.NET.Sdk.Web`; modules use the plain `Microsoft.NET.Sdk`

**Files added to `Api`:**
- `Program.cs` — minimal web app boilerplate (`WebApplication.CreateBuilder` + `app.Run()`)
- `appsettings.json` + `appsettings.Development.json` — default config files
- `Properties/launchSettings.json` — local dev launch profiles (HTTP/HTTPS URLs)

---

## Step 2 — Module Registration Pattern (Wiring Up Dependencies)

**What happened:** Removed the placeholder `Class1.cs` from every module and established the **module registration pattern**: each module exposes two static extension methods that the `Api` bootstrapper calls.

**The pattern (same structure in every module):**

```csharp
// Example: CatalogModule.cs
public static class CatalogModule
{
    // Called in Program.cs → builder.Services.AddCatalogModule(config)
    // Registers all DI services this module needs (DbContext, handlers, etc.)
    public static IServiceCollection AddCatalogModule(this IServiceCollection services, IConfiguration configuration)
    {
        // ... register services
        return services;
    }

    // Called in Program.cs → app.UseCatalogModule()
    // Adds any middleware or startup logic this module needs
    public static IApplicationBuilder UseCatalogModule(this IApplicationBuilder app)
    {
        // ... configure pipeline
        return app;
    }
}
```

**Files created:**
- `src/Modules/Catalog/Catalog/CatalogModule.cs`
- `src/Modules/Basket/Basket/BasketModule.cs`
- `src/Modules/Ordering/Ordering/OrderingModule.cs`

**`Program.cs` updated** to call all modules in the two-phase pattern:
```csharp
// Phase 1 — register services
builder.Services
    .AddCatalogModule(builder.Configuration)
    .AddBasketModule(builder.Configuration)
    .AddOrderingModule(builder.Configuration);

// Phase 2 — configure HTTP pipeline
app.UseCatalogModule()
   .UseBasketModule()
   .UseOrderingModule();
```

**NuGet packages added to `Shared.csproj`:**
- `MediatR` — for CQRS (Commands/Queries/Events dispatching)
- `Microsoft.AspNetCore.Http.Abstractions` — gives access to `IApplicationBuilder`
- `Microsoft.Extensions.Configuration.Abstractions` — gives access to `IConfiguration`
- `Microsoft.Extensions.DependencyInjection.Abstractions` — gives access to `IServiceCollection`

**`GlobalUsing.cs` added to `Api`** — project-wide using statements so you don't repeat `using` directives in every file.

---

## Step 3 — DDD Base Classes in Shared Module

**What happened:** Removed the placeholder `Class1.cs` from `Shared` and built the **Domain-Driven Design (DDD) building blocks** that all modules will use. These are abstract base classes and interfaces that encode rules like "every entity has an Id and audit timestamps" and "aggregates can raise domain events."

**Files created in `src/Shared/Shared/DDD/`:**

### `IDomainEvent.cs`
### `IEntity.cs`
### `IAggregate.cs`
### `Entity.cs`
### `Aggregate.cs`

## Step 4 — Product Aggregate & Domain Events (Catalog Module)

**What happened:** Created the first real domain model — the `Product` aggregate in the Catalog module.

**Files created:**

### `src/Modules/Catalog/Catalog/Products/Models/Product.cs`
The `Product` class is the **Aggregate Root** of the Catalog module.
- Inherits `Aggregate<Guid>` (which gives it an Id and domain event management)
- Properties: `Name`, `Category` (list of strings), `Description`, `ImageFile`, `Price`
- All setters are `private` — the only way to change the product is through its methods (this is the DDD rule: aggregates protect their own state)
- **Factory method `Create()`** — static method to construct a new Product; validates inputs and raises `ProductCreatedEvent`
- **`Update()` method** — modifies the product; raises `ProductPriceChangedEvent` only if the price actually changed

### `src/Modules/Catalog/Catalog/Products/Events/ProductCreatedEvent.cs`
A C# `record` — immutable value object carrying the product that was just created.

### `src/Modules/Catalog/Catalog/Products/Events/ProductPriceChangedEvent.cs`

## Step 5 — Docker & PostgreSQL Setup

**What happened:** Added Docker support so the PostgreSQL database can be run locally as a container — no need to install PostgreSQL directly on the machine. 

**How:** By built-in right click on the project: Add - Container Orchestrator Support

**Files created:**

### `src/Bootstrapper/Api/Dockerfile`

### `src/docker-compose.yml`
Declares the services. Currently just `eshopdb` (the PostgreSQL container):
```yaml
services:
  eshopdb:
    image: postgres
```

### `src/docker-compose.override.yml`
Development-specific overrides (credentials, port mapping):
```yaml
services:
  eshopdb:
    environment:
      - POSTGRES_USER=postgres
      - POSTGRES_PASSWORD=postgres
      - POSTGRES_DB=EShopDb
    ports:
      - "5432:5432"   # host:container
```

**`Api.csproj` updated:**
- Added `Microsoft.EntityFrameworkCore.Design` — needed at build time for EF Core tooling (migrations)
- Added `Microsoft.VisualStudio.Azure.Containers.Tools.Targets` — enables VS Docker integration

**`launchSettings.json` updated** — new "Docker" launch profile added so you can run the app in a container directly from Visual Studio.

---

## Step 6 — EF Core Data Layer (Catalog Module + Shared Infrastructure)

**What happened:** Wired up the full data persistence layer using **Entity Framework Core** with a PostgreSQL database. This step spans both the `Shared` project (reusable infrastructure) and the `Catalog` module (concrete implementation).

### 6a — Shared Data Infrastructure (`src/Shared/Shared/Data/`)

**NuGet packages added to `Shared.csproj`:**
- `Npgsql.EntityFrameworkCore.PostgreSQL` — PostgreSQL driver for EF Core
- `Microsoft.EntityFrameworkCore` — the ORM itself
- `Microsoft.EntityFrameworkCore.Tools` — CLI tools for migrations (`dotnet ef migrations add ...`)

#### `Seed/IDataSeeder.cs`
Contract for "seed initial data into the database on startup." Each module implements this to populate its own tables.

#### `Interceptors/AuditableEntityInterceptor.cs`
An EF Core **SaveChanges interceptor** — runs automatically every time EF Core saves changes to the database. It sets the audit fields (`CreatedAt`, `CreatedBy`, `LastModified`, `LastModifiedBy`) on any entity implementing `IEntity`.

#### `Interceptors/DispatchDomainEventsInterceptor.cs`
Another SaveChanges interceptor. Right before saving, it:
1. Finds all tracked aggregates (`IAggregate`) that have pending domain events
2. Clears those events from the aggregate
3. Publishes each event via **MediatR** (`mediator.Publish(domainEvent)`)

This is the mechanism that connects "something changed in the database" to "domain event handlers run."

#### `Extensions.cs` — `UseMigration<TContext>()` extension method

A reusable extension that modules call on startup. It:
1. Applies any pending EF Core migrations (`MigrateAsync`) — creates/updates the DB schema automatically
2. Runs all registered `IDataSeeder` implementations

### 6b — Catalog Data Layer (`src/Modules/Catalog/Catalog/Data/`)

#### `CatalogDbContext.cs`
The EF Core `DbContext` for the Catalog module. Each module has its own `DbContext` — this is the Modular Monolith pattern for data isolation.

#### `Configurations/ProductConfiguration.cs`
Fluent API configuration — maps the `Product` C# class to a SQL table, defining column types, constraints, max lengths:

#### `Migrations/20260521122554_InitialCreate.cs`
Auto-generated by EF Core after running `dotnet ef migrations add InitialCreate`. Creates the `catalog.Products` table with the correct columns.

#### `Seed/InitialData.cs` + `Seed/CatalogDataSeeder.cs`
Seed data: 4 products are inserted the first time the app starts if the table is empty.

### 6c — Connecting Everything in `CatalogModule.cs`

The `AddCatalogModule()` method was updated to register all the new services:
```csharp
services.AddMediatR(...);                                    // register CQRS handlers
services.AddScoped<ISaveChangesInterceptor, AuditableEntityInterceptor>();
services.AddScoped<ISaveChangesInterceptor, DispatchDomainEventsInterceptor>();
services.AddDbContext<CatalogDbContext>((sp, options) => {
    options.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>());
    options.UseNpgsql(connectionString);
});
services.AddScoped<IDataSeeder, CatalogDataSeeder>();
```

And `UseCatalogModule()` now calls:
```csharp
app.UseMigration<CatalogDbContext>(); // auto-migrate + seed on startup
```

**`appsettings.json` updated** with the PostgreSQL connection string:
```json
"ConnectionStrings": {
  "Database": "Server=localhost;Port=5432;Database=EShopDb;User Id=postgres;Password=postgres;Include Error Detail=true"
}
```
