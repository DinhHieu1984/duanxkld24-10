using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OrchardCore.Environment.Extensions.Features;
using OrchardCore.Environment.Shell;
using OrchardCore.Environment.Shell.Scope;

namespace OrchardCoreLearning.DemoBlogModule.Services
{
    /// <summary>
    /// ✅ ĐÚNG: Feature Event Handler tuân thủ tất cả quy định OrchardCore
    /// - Kế thừa từ FeatureEventHandler (abstract class)
    /// - Naming convention: {ModuleName}FeatureEventHandler
    /// - Namespace: {Company}.{Module}.Services
    /// - Feature ID validation
    /// - Deferred tasks cho heavy operations
    /// - Proper error handling
    /// - Service resolution từ scope
    /// </summary>
    public class DemoBlogModuleFeatureEventHandler : FeatureEventHandler
    {
        private readonly ILogger<DemoBlogModuleFeatureEventHandler> _logger;
        
        /// <summary>
        /// ✅ ĐÚNG: Constructor injection cho dependencies
        /// </summary>
        public DemoBlogModuleFeatureEventHandler(ILogger<DemoBlogModuleFeatureEventHandler> logger)
        {
            _logger = logger;
        }
        
        /// <summary>
        /// ✅ ĐÚNG: EnabledAsync method với tất cả best practices
        /// </summary>
        public override Task EnabledAsync(IFeatureInfo feature)
        {
            // ✅ QUY ĐỊNH BẮT BUỘC: LUÔN kiểm tra feature ID
            if (feature.Id != "OrchardCoreLearning.DemoBlogModule")
                return Task.CompletedTask;
                
            _logger.LogInformation("DemoBlogModule feature enabled, starting initialization...");
            
            // ✅ QUY ĐỊNH BẮT BUỘC: Heavy operations PHẢI dùng deferred tasks
            ShellScope.AddDeferredTask(async scope =>
            {
                try
                {
                    // ✅ QUY ĐỊNH: LUÔN resolve services từ scope.ServiceProvider
                    var demoBlogService = scope.ServiceProvider.GetRequiredService<IDemoBlogService>();
                    var featuresManager = scope.ServiceProvider.GetRequiredService<IShellFeaturesManager>();
                    var logger = scope.ServiceProvider.GetRequiredService<ILogger<DemoBlogModuleFeatureEventHandler>>();
                    
                    // ✅ QUY ĐỊNH: Kiểm tra dependencies trước khi thực hiện
                    var hasContentTypes = await featuresManager.IsFeatureEnabledAsync("OrchardCore.ContentTypes");
                    if (!hasContentTypes)
                    {
                        logger.LogWarning("ContentTypes feature not enabled, skipping blog setup");
                        return;
                    }
                    
                    var hasAutoroute = await featuresManager.IsFeatureEnabledAsync("OrchardCore.Autoroute");
                    var hasHtml = await featuresManager.IsFeatureEnabledAsync("OrchardCore.Html");
                    
                    // ✅ QUY ĐỊNH: Idempotent operations - có thể chạy nhiều lần an toàn
                    if (!await demoBlogService.IsInitializedAsync())
                    {
                        logger.LogInformation("Initializing DemoBlogModule...");
                        
                        // Conditional initialization based on available features
                        if (hasContentTypes && hasAutoroute && hasHtml)
                        {
                            await demoBlogService.FullInitializationAsync();
                            logger.LogInformation("DemoBlogModule full initialization completed");
                        }
                        else if (hasContentTypes)
                        {
                            await demoBlogService.PartialInitializationAsync();
                            logger.LogInformation("DemoBlogModule partial initialization completed");
                        }
                        else
                        {
                            await demoBlogService.MinimalInitializationAsync();
                            logger.LogInformation("DemoBlogModule minimal initialization completed");
                        }
                        
                        await demoBlogService.MarkAsInitializedAsync();
                    }
                    else
                    {
                        logger.LogInformation("DemoBlogModule already initialized, updating configuration...");
                        
                        // ✅ QUY ĐỊNH: Cập nhật configuration mỗi lần enable
                        await demoBlogService.UpdateConfigurationAsync();
                    }
                    
                    logger.LogInformation("DemoBlogModule feature initialization completed successfully");
                }
                catch (Exception ex)
                {
                    var logger = scope.ServiceProvider.GetRequiredService<ILogger<DemoBlogModuleFeatureEventHandler>>();
                    
                    // ✅ QUY ĐỊNH: Log error nhưng KHÔNG throw exception
                    logger.LogError(ex, "Failed to initialize DemoBlogModule after enabling feature");
                    
                    // ✅ QUY ĐỊNH: Có thể thử fallback hoặc partial initialization
                    try
                    {
                        var demoBlogService = scope.ServiceProvider.GetRequiredService<IDemoBlogService>();
                        await demoBlogService.MinimalInitializationAsync();
                        logger.LogWarning("DemoBlogModule initialized with minimal configuration due to error");
                    }
                    catch (Exception fallbackEx)
                    {
                        logger.LogError(fallbackEx, "Failed to initialize DemoBlogModule even with minimal configuration");
                        // ✅ QUY ĐỊNH: KHÔNG throw - để application tiếp tục hoạt động
                    }
                }
            });
            
            return Task.CompletedTask;
        }
        
        /// <summary>
        /// ✅ ĐÚNG: DisabledAsync method với cleanup logic
        /// </summary>
        public override Task DisabledAsync(IFeatureInfo feature)
        {
            // ✅ QUY ĐỊNH: Kiểm tra feature ID
            if (feature.Id != "OrchardCoreLearning.DemoBlogModule")
                return Task.CompletedTask;
                
            _logger.LogInformation("DemoBlogModule feature disabled, starting cleanup...");
            
            // ✅ QUY ĐỊNH: Cleanup operations trong deferred tasks
            ShellScope.AddDeferredTask(async scope =>
            {
                try
                {
                    var demoBlogService = scope.ServiceProvider.GetRequiredService<IDemoBlogService>();
                    var logger = scope.ServiceProvider.GetRequiredService<ILogger<DemoBlogModuleFeatureEventHandler>>();
                    
                    await demoBlogService.CleanupAsync();
                    logger.LogInformation("DemoBlogModule cleanup completed successfully");
                }
                catch (Exception ex)
                {
                    var logger = scope.ServiceProvider.GetRequiredService<ILogger<DemoBlogModuleFeatureEventHandler>>();
                    logger.LogError(ex, "Failed to cleanup DemoBlogModule");
                    // ✅ QUY ĐỊNH: KHÔNG throw exception
                }
            });
            
            return Task.CompletedTask;
        }
        
        /// <summary>
        /// ✅ ĐÚNG: InstalledAsync method cho first-time installation
        /// </summary>
        public override Task InstalledAsync(IFeatureInfo feature)
        {
            if (feature.Id != "OrchardCoreLearning.DemoBlogModule")
                return Task.CompletedTask;
                
            _logger.LogInformation("DemoBlogModule feature installed");
            
            // ✅ QUY ĐỊNH: Installation-specific logic
            ShellScope.AddDeferredTask(async scope =>
            {
                try
                {
                    var demoBlogService = scope.ServiceProvider.GetRequiredService<IDemoBlogService>();
                    var logger = scope.ServiceProvider.GetRequiredService<ILogger<DemoBlogModuleFeatureEventHandler>>();
                    
                    // Perform any installation-specific tasks
                    await demoBlogService.InstallationSetupAsync();
                    logger.LogInformation("DemoBlogModule installation setup completed");
                }
                catch (Exception ex)
                {
                    var logger = scope.ServiceProvider.GetRequiredService<ILogger<DemoBlogModuleFeatureEventHandler>>();
                    logger.LogError(ex, "Failed to complete DemoBlogModule installation setup");
                }
            });
            
            return Task.CompletedTask;
        }
    }
}