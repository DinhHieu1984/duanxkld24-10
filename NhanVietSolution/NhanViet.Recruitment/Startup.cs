using Microsoft.Extensions.DependencyInjection;
using NhanViet.Recruitment.Drivers;
using NhanViet.Recruitment.Models;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Display.ContentDisplay;
using OrchardCore.Data.Migration;
using OrchardCore.Modules;
using OrchardCore.Security.Permissions;

namespace NhanViet.Recruitment;

public sealed class Startup : StartupBase
{
    public override void ConfigureServices(IServiceCollection services)
    {
        // Recruitment Content Part
        services.AddContentPart<RecruitmentPart>()
            .UseDisplayDriver<RecruitmentPartDisplayDriver>();
            
        services.AddDataMigration<Migrations>();

        // Register Permission Provider
        services.AddScoped<IPermissionProvider, Permissions>();

        // Register Authorization Handler
    }
}

