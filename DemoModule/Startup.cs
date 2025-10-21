using Microsoft.Extensions.DependencyInjection;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Display.ContentDisplay;
using OrchardCore.ContentManagement.Handlers;
using OrchardCore.Data.Migration;
using OrchardCore.Environment.Extensions.Features;
using OrchardCore.Modules;
using OrchardCore.Recipes;
using OrchardCore.Setup.Events;
using OrchardCoreLearning.DemoBlogModule.Indexes;
using OrchardCoreLearning.DemoBlogModule.Models;
using OrchardCoreLearning.DemoBlogModule.Recipes;
using OrchardCoreLearning.DemoBlogModule.Services;
using YesSql.Indexes;

namespace OrchardCoreLearning.DemoBlogModule
{
    /// <summary>
    /// ✅ ĐÚNG: Startup class tuân thủ naming conventions và structure chuẩn OrchardCore
    /// </summary>
    public class Startup : StartupBase
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            // ✅ ĐÚNG: Đăng ký services với đúng lifetime
            
            // Core services - Scoped cho services có state per request
            services.AddScoped<IDemoBlogService, DemoBlogService>();
            
            // Content part
            services.AddContentPart<DemoBlogPostPart>();
            
            // Index provider - Singleton cho stateless services
            services.AddSingleton<IIndexProvider, DemoBlogPostIndexProvider>();
            
            // Data migration - Scoped
            services.AddScoped<IDataMigration, DemoBlogModuleMigrations>();
            
            // Feature event handler - Scoped
            services.AddScoped<IFeatureEventHandler, DemoBlogModuleFeatureEventHandler>();
            
            // Recipe step handler - Transient cho lightweight services
            services.AddTransient<IRecipeStepHandler, DemoBlogSetupRecipeStep>();
        }
    }
    
    /// <summary>
    /// ✅ ĐÚNG: Separate startup class cho admin feature
    /// </summary>
    [Feature("OrchardCoreLearning.DemoBlogModule.Admin")]
    public class AdminStartup : StartupBase
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            // ✅ ĐÚNG: Admin-specific services
            services.AddScoped<ISetupEventHandler, DemoBlogModuleSetupEventHandler>();
        }
    }
}