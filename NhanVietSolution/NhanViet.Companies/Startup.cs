using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using OrchardCore.Modules;
using OrchardCore.Data.Migration;

namespace NhanViet.Companies;

public sealed class Startup : StartupBase
{
    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<IDataMigration, Migrations>();
    }

    public override void Configure(IApplicationBuilder builder, IEndpointRouteBuilder routes, IServiceProvider serviceProvider)
    {
        routes.MapAreaControllerRoute(
            name: "CompaniesHome",
            areaName: "NhanViet.Companies", 
            pattern: "companies",
            defaults: new { controller = "Home", action = "Index" }
        );
        
        routes.MapAreaControllerRoute(
            name: "Companies",
            areaName: "NhanViet.Companies",
            pattern: "companies/{action=Index}/{id?}",
            defaults: new { controller = "Home", action = "Index" }
        );
    }
}

