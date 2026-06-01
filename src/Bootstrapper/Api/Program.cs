var builder = WebApplication.CreateBuilder(args);

// Register all staff to DI from the given assemblies that implements ICarterModule
builder.Services
    .AddCarterWithAssemblies(typeof(CatalogModule).Assembly);

// Add our module services to the container
builder.Services
    .AddCatalogModule(builder.Configuration)
    .AddBasketModule(builder.Configuration)
    .AddOrderingModule(builder.Configuration);

var app = builder.Build();

// Register all endpoints into the HTTP pipeline
app.MapCarter();

// Configure the HTTP request pipeline (middleware, routing, custom exception handling etc.)
app.UseCatalogModule()
   .UseBasketModule()
   .UseOrderingModule();

app.Run();
