using Microsoft.Extensions.DependencyInjection;
using NhanViet.Companies.Drivers;
using NhanViet.Companies.Models;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Display.ContentDisplay;
using OrchardCore.Data.Migration;
using OrchardCore.Modules;
using OrchardCore.Security.Permissions;

namespace NhanViet.Companies;

public sealed class Startup : StartupBase
{
    public override void ConfigureServices(IServiceCollection services)
    {
        // Company Content Part
        services.AddContentPart<CompanyPart>()
            .UseDisplayDriver<CompanyPartDisplayDriver>();
            
        services.AddDataMigration<Migrations>();
        
        // Register Permission Provider
        services.AddScoped<IPermissionProvider, Permissions>();

        // Register Authorization Handler
    }
}

