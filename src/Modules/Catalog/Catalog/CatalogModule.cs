using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Behaviors;
using Shared.Data.Interceptors;

namespace Catalog;

public static class CatalogModule
{
    public static IServiceCollection AddCatalogModule(this IServiceCollection services, IConfiguration configuration)
    {
        // Add services to DI container one by one (by each layer)
        // 1.register Presentation (API / Endpoint) layer related services

        // 2.register Application / Use Case layer related services
        services.AddMediatR(config => // register MediatR
        {
            config.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            config.AddOpenBehavior(typeof(ValidationBehavior<,>));
            config.AddOpenBehavior(typeof(LoggingBehavior<,>));
        });
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly()); // registers all Fluent validations

        // 3.register Data / Infrastructure layer related services
        var connectionString = configuration.GetConnectionString("Database");

        services.AddScoped<ISaveChangesInterceptor, AuditableEntityInterceptor>();
        services.AddScoped<ISaveChangesInterceptor, DispatchDomainEventsInterceptor>();

        services.AddDbContext<CatalogDbContext>((sp, options) =>
        {
            options.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>());
            options.UseNpgsql(connectionString);
        });

        services.AddScoped<IDataSeeder, CatalogDataSeeder>();

        return services;
    }

    public static IApplicationBuilder UseCatalogModule(this IApplicationBuilder app)
    {
        // Configure the HTTP request pipeline (add your custom middlewares)
        // 1.Implement custom middleware related to Presentation (API / Endpoint) layer

        // 2.Implement custom middleware related to Application / Use Case layer

        // 3.Implement custom middleware related to Data / Infrastructure layer
        app.UseMigration<CatalogDbContext>(); // implement generic migration extension method to auto-migrate

        return app;
    }
}
