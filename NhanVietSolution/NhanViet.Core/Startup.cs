using Microsoft.Extensions.DependencyInjection;
using NhanViet.Core.Navigation;
using NhanViet.Core.Services;
using NhanViet.Core.BackgroundServices;
using OrchardCore.Modules;
using OrchardCore.Navigation;

namespace NhanViet.Core;

public class Startup : StartupBase
{
    public override void ConfigureServices(IServiceCollection services)
    {
        // Navigation Providers
        services.AddScoped<INavigationProvider, MainNavigationProvider>();
        services.AddScoped<INavigationProvider, NhanViet.Core.Navigation.AdminNavigationProvider>();
        
        // Core Services
        services.AddScoped<INotificationService, NotificationService>();
        
        // Background Services
        services.AddSingleton<EmailNotificationService>();
        services.AddHostedService<EmailNotificationService>(provider => 
            provider.GetRequiredService<EmailNotificationService>());
        services.AddHostedService<JobProcessingService>();
    }
}