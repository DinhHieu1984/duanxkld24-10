using Microsoft.Extensions.DependencyInjection;
using NhanViet.JobOrders.Drivers;
using NhanViet.JobOrders.Models;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Display.ContentDisplay;
using OrchardCore.Data.Migration;
using OrchardCore.Modules;
using OrchardCore.Security.Permissions;

namespace NhanViet.JobOrders;

public sealed class Startup : StartupBase
{
    public override void ConfigureServices(IServiceCollection services)
    {
        // JobOrder Content Part
        services.AddContentPart<JobOrderPart>()
            .UseDisplayDriver<JobOrderPartDisplayDriver>();
            
        services.AddDataMigration<Migrations>();
        
        // Register Permission Provider
        services.AddScoped<IPermissionProvider, Permissions>();
        
        // Register Authorization Handler
    }
}

