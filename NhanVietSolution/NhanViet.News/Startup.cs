using Microsoft.Extensions.DependencyInjection;
using NhanViet.News.Drivers;
using NhanViet.News.Models;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Display.ContentDisplay;
using OrchardCore.Data.Migration;
using OrchardCore.Modules;
using OrchardCore.Security.Permissions;

namespace NhanViet.News;

public sealed class Startup : StartupBase
{
    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<IDataMigration, Migrations>();
        
        // Register News Content Part
        services.AddContentPart<NewsPart>()
            .UseDisplayDriver<NewsPartDisplayDriver>();
            
        // Register Permission Provider
        services.AddScoped<IPermissionProvider, Permissions>();

        // Register Authorization Handler
    }
}

