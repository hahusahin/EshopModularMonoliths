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

---

## Step 10 — Catalog API Endpoints (Presentation Layer) with Carter & REPR Pattern

**Pattern:** REPR (Request → Endpoint → Response). Each feature folder under `Catalog/Products/Features/` now contains an endpoint class alongside its handler.

**Carter discovery extension (`Shared/Extensions/CarterExtensions.cs`):**
- `AddCarterWithAssemblies(params Assembly[] assemblies)` — scans given assemblies via Reflection, finds all `ICarterModule` implementations, registers them in DI. Needed because Carter's default only scans the entry assembly (`Api`), but endpoints live in module assemblies.

**`Program.cs` wiring:**
- `builder.Services.AddCarterWithAssemblies(typeof(CatalogModule).Assembly)` — Phase 1: discovers and registers all Catalog endpoints
- `app.MapCarter()` — Phase 2: calls `AddRoutes()` on each registered module, activating all routes

6 endpoints added covering full CRUD + category/id queries. Each follows the same flow: Carter routes the request → Mapster maps Request DTO → MediatR Command/Query → Handler → Response DTO → HTTP result.

---

## Step 11 — Validation Pipeline (`Shared/Behaviors/ValidationBehavior.cs`)
MediatR `IPipelineBehavior` that runs before the handler — only for commands, queries skip it. Throws `ValidationException` so the handler never runs on bad input.

Validators sit next to their command in the feature file (`AbstractValidator<T>`), auto-registered from the assembly.

NuGet to `Shared`: `FluentValidation`.

---

## Step 12 — Logging Pipeline (`Shared/Behaviors/LoggingBehavior.cs`)
Second `IPipelineBehavior`, applied to **all** requests. Logs start/end of every request, times it, warns if it took > 3 seconds.

Nesting follows registration order: `Logging → Validation → Handler`.

---

## Step 13 — Global Exception Handling (`Shared/Exceptions/`)
Handlers used to throw plain `Exception` → API returned 500 for what is really a 404.

- `NotFoundException`, `BadRequestException`, `InternalServerException` — typed exceptions
- `ProductNotFoundException : NotFoundException` — thrown by the Catalog handlers
- `CustomExceptionHandler` — maps exception type → status code, returns an RFC-7807 `ProblemDetails` body

---

## Step 14 — Structured Logging (Serilog + Seq)
Replaced the default logger with Serilog, configured from `appsettings.json` (Console + Seq sinks).

Added `seq` to `docker-compose` as the log server — UI on `:9091`.

Point of Seq: logs become queryable data, not text.

---

## Step 15 — Pagination (`Shared/Pagination/`)
`GetProducts` was returning every row in the table.

- `PaginatedRequest(PageIndex, PageSize)` — bound from the query string
- `PaginatedResult<TEntity>` — page + total count

---

## Step 16 — Basket Domain Models (`Basket/Basket/Models/`)
Start of the Basket module:
- `ShoppingCart : Aggregate<Guid>` — the aggregate root; items exposed as `IReadOnlyList`, so the only way in is a domain method
- `ShoppingCart.Create()` / `AddItem()` / `RemoveItem()` — factory + rich domain methods
- `TotalPrice` — computed from the items, never stored
- `ShoppingCartItem : Entity<Guid>` — child entity with an `internal` constructor; only `ShoppingCart` can create one
- `Price` / `ProductName` — a snapshot copied from Catalog when the item is added

Both become tables (one-to-many).

---

## Step 17 — Basket Data Layer (`Basket/Data/`)
Same shape as Catalog's:
- `BasketDbContext` — own schema (`basket`), applies configurations from the assembly
- `ShoppingCartConfiguration` — PK, unique index on `UserName`, one-to-many to `Items` (`WithOne()` has no inverse nav — child never points back to the root)
- `ShoppingCartItemConfiguration` — PK + required columns
- `InitialCreate` migration

**`BasketModule.cs` updated** — registers both interceptors + `BasketDbContext`, and `UseMigration<BasketDbContext>()` on startup. No seeder (a basket starts empty).

---

## Step 18 — Basket Feature Handlers (`Basket/Basket/Features/`)
One folder per use case, one file each: command/query record + result record + validator + handler.

| Feature | Type | Returns |
|---|---|---|
| `CreateBasket` | Command | `CreateBasketResult(Guid Id)` |
| `AddItemIntoBasket` | Command | `AddItemIntoBasketResult(Guid Id)` |
| `RemoveItemFromBasket` | Command | `RemoveItemFromBasketResult(Guid Id)` |
| `DeleteBasket` | Command | `DeleteBasketResult(bool IsSuccess)` |
| `GetBasket` | Query | `GetBasketResult(ShoppingCartDto)` |

Every handler follows the same script: **load the aggregate → call a domain method → `SaveChangesAsync` → return**. No business rules in the handlers — `AddItem` / `RemoveItem` live on `ShoppingCart`.
