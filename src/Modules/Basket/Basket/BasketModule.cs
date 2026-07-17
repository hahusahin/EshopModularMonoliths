using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Data;
using Shared.Data.Interceptors;

namespace Basket;

public static class BasketModule
{
    public static IServiceCollection AddBasketModule(this IServiceCollection services, IConfiguration configuration)
    {
        //////////// Add services to DI container one by one (by each layer) ////////////

        // 1.register Presentation (API / Endpoint) layer related services

        // 2.register Application / Use Case layer related services
        services.AddScoped<IBasketRepository, BasketRepository>();
        services.Decorate<IBasketRepository, CachedBasketRepository>();

        // 3.register Data / Infrastructure layer related services
        var connectionString = configuration.GetConnectionString("Database");

        services.AddScoped<ISaveChangesInterceptor, AuditableEntityInterceptor>();
        services.AddScoped<ISaveChangesInterceptor, DispatchDomainEventsInterceptor>();

        services.AddDbContext<BasketDbContext>((sp, options) =>
        {
            options.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>());
            options.UseNpgsql(connectionString);
        });

        return services;
    }

    public static IApplicationBuilder UseBasketModule(this IApplicationBuilder app)
    {
        //////////// Configure the HTTP request pipeline ////////////

        // 1.Implement custom middleware related to Presentation (API / Endpoint) layer

        // 2.Implement custom middleware related to Application / Use Case layer

        // 3.Implement custom middleware related to Data / Infrastructure layer
        app.UseMigration<BasketDbContext>();

        return app;
    }
}
