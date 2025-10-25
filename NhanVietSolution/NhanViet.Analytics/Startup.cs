using Microsoft.Extensions.DependencyInjection;
using NhanViet.Analytics.Drivers;
using NhanViet.Analytics.Models;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Display.ContentDisplay;
using OrchardCore.Data.Migration;
using OrchardCore.Modules;
using OrchardCore.Security.Permissions;

namespace NhanViet.Analytics;

public sealed class Startup : StartupBase
{
    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<IDataMigration, Migrations>();
        
        // Register Analytics Content Part
        services.AddContentPart<AnalyticsPart>()
            .UseDisplayDriver<AnalyticsPartDisplayDriver>();
            
        // Register Permission Provider
        services.AddScoped<IPermissionProvider, Permissions>();

        // Register Authorization Handler
    }
}

