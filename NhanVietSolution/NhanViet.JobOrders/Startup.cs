using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using OrchardCore.Modules;
using OrchardCore.Data.Migration;

namespace NhanViet.JobOrders;

public sealed class Startup : StartupBase
{
    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<IDataMigration, Migrations>();
    }

    public override void Configure(IApplicationBuilder builder, IEndpointRouteBuilder routes, IServiceProvider serviceProvider)
    {
        routes.MapAreaControllerRoute(
            name: "Jobs",
            areaName: "NhanViet.JobOrders",
            pattern: "jobs/{action=Index}/{id?}",
            defaults: new { controller = "Home", action = "Index" }
        );
        
        routes.MapAreaControllerRoute(
            name: "JobsHome",
            areaName: "NhanViet.JobOrders", 
            pattern: "jobs",
            defaults: new { controller = "Home", action = "Index" }
        );
    }
}

