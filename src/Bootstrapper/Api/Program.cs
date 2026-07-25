
var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, config) =>
    config.ReadFrom.Configuration(context.Configuration));

// 1. Add COMMON services (carter, mediatr, fluentvalidation, masstransit) to the container
var catalogAssembly = typeof(CatalogModule).Assembly;
var basketAssembly = typeof(BasketModule).Assembly;
var orderingAssembly = typeof(OrderingModule).Assembly;

// 1.1 Register all staff to DI from the given assemblies that implements ICarterModule
builder.Services.AddCarterWithAssemblies(catalogAssembly, basketAssembly, orderingAssembly);

// 1.2 Register both MediatR and FluentValidation (from extension method)
builder.Services.AddMediatRWithAssemblies(catalogAssembly, basketAssembly, orderingAssembly);

// 1.3 Register Redis
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
});

// 1.4 Register MassTransit
builder.Services.AddMassTransitWithAssemblies(builder.Configuration, catalogAssembly, basketAssembly, orderingAssembly);

// 1.5 Register Keycloak authentication / authorization
builder.Services.AddKeycloakWebApiAuthentication(builder.Configuration);
builder.Services.AddAuthorization();

// 2. Add MODULE related services (catalog, basket, ordering) to the container
builder.Services
    .AddCatalogModule(builder.Configuration)
    .AddBasketModule(builder.Configuration)
    .AddOrderingModule(builder.Configuration);

// Register our custom exception handler
builder.Services.AddExceptionHandler<CustomExceptionHandler>();

var app = builder.Build();

// Register all endpoints into the HTTP pipeline
app.MapCarter();

// Configure the HTTP request pipeline (middleware, custom exception handling, logging etc.)
app.UseSerilogRequestLogging();
app.UseExceptionHandler(options => { });

app.UseAuthentication();
app.UseAuthorization();

app.UseCatalogModule()
   .UseBasketModule()
   .UseOrderingModule();

app.Run();
