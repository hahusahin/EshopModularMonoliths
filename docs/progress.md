# EshopModularMonoliths — Build Log

> Quick reference: what was built, in what order, and why.

---

## Step 0 — Repo Init
Created git repo with `.gitignore` and empty `README.md`.

---

## Step 1 — Solution & Projects
Created the VS solution at `src/` with 5 projects:

| Project | Path | Role |
|---|---|---|
| `Api` | `Bootstrapper/Api/` | Single entry point, hosts the web API |
| `Catalog` | `Modules/Catalog/Catalog/` | Products module |
| `Basket` | `Modules/Basket/Basket/` | Basket module |
| `Ordering` | `Modules/Ordering/Ordering/` | Ordering module |
| `Shared` | `Shared/Shared/` | Base classes and contracts shared across modules |

`Api` references all modules via `<ProjectReference>`. Modules reference `Shared`.

---

## Step 2 — Module Registration Pattern
Each module exposes two static extension methods called from `Program.cs`:
- `AddXxxModule(services, config)` — registers DI services
- `UseXxxModule(app)` — registers middleware/startup logic

NuGet added to `Shared`: `MediatR`, `Microsoft.AspNetCore.Http.Abstractions`, `Microsoft.Extensions.*`.

---

## Step 3 — DDD Base Classes (`Shared/DDD/`)
Built the building blocks all modules inherit from:
- `IEntity` / `Entity<TId>` — base entity with Id and audit fields
- `IAggregate` / `Aggregate<TId>` — extends Entity, adds `DomainEvents` list + `AddDomainEvent()` / `ClearDomainEvents()`
- `IDomainEvent` — marker interface (extends MediatR's `INotification`)

---

## Step 4 — Product Aggregate (`Catalog/Products/Models/`)
First real domain model:
- `Product : Aggregate<Guid>` — properties all have `private set`; state only changes through methods
- `Product.Create()` — factory method, validates input, raises `ProductCreatedEvent`
- `Product.Update()` — raises `ProductPriceChangedEvent` only if price actually changed
- `ProductCreatedEvent` / `ProductPriceChangedEvent` — immutable records carrying the product data

---

## Step 5 — Docker & PostgreSQL
Added Docker support via VS right-click → Add Container Orchestrator Support:
- `Dockerfile` — containerizes the API
- `docker-compose.yml` — declares `eshopdb` (postgres image)
- `docker-compose.override.yml` — dev credentials + port `5432:5432`

---

## Step 6 — EF Core Data Layer

**Shared infrastructure (`Shared/Data/`):**
- `AuditableEntityInterceptor` — SaveChanges interceptor; auto-sets `CreatedAt`, `LastModified`
- `DispatchDomainEventsInterceptor` — SaveChanges interceptor; collects domain events from all tracked aggregates, publishes each via `mediator.Publish()` before the DB write
- `IDataSeeder` — contract for seeding initial data on startup
- `Extensions.UseMigration<T>()` — applies pending migrations + runs seeders on startup

**Catalog data layer (`Catalog/Data/`):**
- `CatalogDbContext` — module-scoped DbContext (each module has its own)
- `ProductConfiguration` — Fluent API mapping for the `products` table
- `InitialCreate` migration — auto-generated, creates `catalog.Products`
- `CatalogDataSeeder` — inserts 4 seed products on first run

**`CatalogModule.cs` updated** to register MediatR, both interceptors, `CatalogDbContext`, and `CatalogDataSeeder`.

---

## Step 7 — CQRS Interfaces (`Shared/CQRS/`)
Thin wrapper interfaces over MediatR to enforce the Commands/Queries split:
- `ICommand` / `ICommand<TResponse>` — marks a class as a write operation
- `ICommandHandler<TCommand>` / `ICommandHandler<TCommand, TResponse>` — handles a command
- `IQuery<TResponse>` — marks a class as a read operation
- `IQueryHandler<TQuery, TResponse>` — handles a query

---

## Step 8 — Catalog Feature Handlers
One folder per feature under `Catalog/Products/Features/`. Each folder contains a single file with 3 things: result record, command/query record, handler class.

| Feature | Type | Returns |
|---|---|---|
| `CreateProduct` | Command | `CreateProductResult(Guid Id)` |
| `UpdateProduct` | Command | `Unit` (nothing) |
| `DeleteProduct` | Command | `Unit` (nothing) |
| `GetProducts` | Query | `GetProductsResult(IEnumerable<ProductDto>)` |
| `GetProductById` | Query | `GetProductByIdResult(ProductDto)` |
| `GetProductByCategory` | Query | `GetProductByCategoryResult(IEnumerable<ProductDto>)` |

Queries use `.AsNoTracking()` (read-only, no change tracking overhead). Mapster's `.Adapt<T>()` converts `Product` entities to `ProductDto` before returning.

---

## Step 9 — Domain Event Handlers (`Catalog/Products/EventHandlers/`)
- `ProductCreatedEventHandler` — handles `ProductCreatedEvent`; currently logs only
- `ProductPriceChangedEventHandler` — handles `ProductPriceChangedEvent`; currently logs; TODO: publish integration event to update Basket prices

Both implement `INotificationHandler<T>` (MediatR). Auto-discovered at startup by `AddMediatR(RegisterServicesFromAssembly(...))`.
