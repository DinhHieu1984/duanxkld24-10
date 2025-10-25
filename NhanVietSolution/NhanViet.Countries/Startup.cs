using Microsoft.Extensions.DependencyInjection;
using NhanViet.Countries.Drivers;
using NhanViet.Countries.Models;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Display.ContentDisplay;
using OrchardCore.Data.Migration;
using OrchardCore.Modules;
using OrchardCore.Security.Permissions;

namespace NhanViet.Countries;

public sealed class Startup : StartupBase
{
    public override void ConfigureServices(IServiceCollection services)
    {
        // Country Content Part
        services.AddContentPart<CountryPart>()
            .UseDisplayDriver<CountryPartDisplayDriver>();
            
        services.AddDataMigration<Migrations>();

        // Register Permission Provider
        services.AddScoped<IPermissionProvider, Permissions>();

        // Register Authorization Handler
    }
}

