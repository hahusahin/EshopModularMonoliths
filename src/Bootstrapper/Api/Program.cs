
var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, config) =>
    config.ReadFrom.Configuration(context.Configuration));

// Register all staff to DI from the given assemblies that implements ICarterModule
builder.Services
    .AddCarterWithAssemblies(typeof(CatalogModule).Assembly);

// Add our module services to the container
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

app.UseCatalogModule()
   .UseBasketModule()
   .UseOrderingModule();

app.Run();
