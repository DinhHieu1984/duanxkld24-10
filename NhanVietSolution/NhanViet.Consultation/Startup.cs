using Microsoft.Extensions.DependencyInjection;
using NhanViet.Consultation.Drivers;
using NhanViet.Consultation.Models;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Display.ContentDisplay;
using OrchardCore.Data.Migration;
using OrchardCore.Modules;
using OrchardCore.Security.Permissions;

namespace NhanViet.Consultation;

public sealed class Startup : StartupBase
{
    public override void ConfigureServices(IServiceCollection services)
    {
        // Consultation Content Part
        services.AddContentPart<ConsultationPart>()
            .UseDisplayDriver<ConsultationPartDisplayDriver>();
            
        services.AddDataMigration<Migrations>();

        // Register Permission Provider
        services.AddScoped<IPermissionProvider, Permissions>();

        // Register Authorization Handler
    }
}

