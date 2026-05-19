var builder = WebApplication.CreateBuilder(args);

// Add our module services to the container
builder.Services
    .AddCatalogModule(builder.Configuration)
    .AddBasketModule(builder.Configuration)
    .AddOrderingModule(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline (middleware, routing, custom exception handling etc.)
app.UseCatalogModule()
   .UseBasketModule()
   .UseOrderingModule();

app.Run(); 
