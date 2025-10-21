# ⚙️ Services & Startup Configuration trong OrchardCore - Phân tích Chi tiết từ Source Code

## 🎯 Tổng quan Services & Startup System

OrchardCore sử dụng hệ thống Dependency Injection mạnh mẽ với startup configuration pattern cho themes và modules:

### 🏗️ Startup Architecture

```csharp
// IStartup.cs - Interface chính cho startup
public interface IStartup
{
    /// <summary>
    /// Thứ tự khởi tạo services (default: 0)
    /// </summary>
    int Order { get; }

    /// <summary>
    /// Thứ tự configure pipeline (default: Order value)
    /// </summary>
    int ConfigureOrder { get; }

    /// <summary>
    /// Đăng ký services vào DI container
    /// </summary>
    void ConfigureServices(IServiceCollection services);

    /// <summary>
    /// Cấu hình HTTP request pipeline
    /// </summary>
    void Configure(IApplicationBuilder builder, IEndpointRouteBuilder routes, IServiceProvider serviceProvider);
}
```

### 📋 StartupBase - Base Class

```csharp
// StartupBase.cs - Base class cho tất cả startup classes
public abstract class StartupBase : IStartup, IAsyncStartup
{
    /// <summary>
    /// Thứ tự mặc định cho service configuration
    /// </summary>
    public virtual int Order { get; } = OrchardCoreConstants.ConfigureOrder.Default;

    /// <summary>
    /// Thứ tự configure pipeline (mặc định = Order)
    /// </summary>
    public virtual int ConfigureOrder => Order;

    /// <summary>
    /// Override để đăng ký services
    /// </summary>
    public virtual void ConfigureServices(IServiceCollection services)
    {
        // Implementation trong derived classes
    }

    /// <summary>
    /// Override để configure pipeline
    /// </summary>
    public virtual void Configure(IApplicationBuilder app, IEndpointRouteBuilder routes, IServiceProvider serviceProvider)
    {
        // Implementation trong derived classes
    }

    /// <summary>
    /// Async configuration support
    /// </summary>
    public virtual ValueTask ConfigureAsync(IApplicationBuilder app, IEndpointRouteBuilder routes, IServiceProvider serviceProvider) => default;
}
```

## 🎨 Theme Startup Implementations

### 🔧 TheAdmin Theme Startup

```csharp
// TheAdmin/Startup.cs - Admin theme startup
public sealed class Startup : StartupBase
{
    private readonly IShellConfiguration _configuration;

    public Startup(IShellConfiguration configuration)
    {
        _configuration = configuration;
    }

    public override void ConfigureServices(IServiceCollection services)
    {
        // Đăng ký Display Driver cho Navbar
        services.AddDisplayDriver<Navbar, ToggleThemeNavbarDisplayDriver>();
        
        // Đăng ký Resource Configuration
        services.AddResourceConfiguration<ResourceManagementOptionsConfiguration>();
        
        // Cấu hình Theme Options từ configuration
        services.Configure<TheAdminThemeOptions>(_configuration.GetSection("TheAdminTheme:StyleSettings"));
    }
}
```

### 🌐 TheBlogTheme Startup

```csharp
// TheBlogTheme/Startup.cs - Blog theme startup (minimal)
public sealed class Startup : StartupBase
{
    public override void ConfigureServices(IServiceCollection serviceCollection)
    {
        // Chỉ đăng ký resource configuration
        serviceCollection.AddResourceConfiguration<ResourceManagementOptionsConfiguration>();
    }
}
```

### 🎯 TheTheme Startup

```csharp
// TheTheme/Startup.cs - General theme startup
public sealed class Startup : StartupBase
{
    public override void ConfigureServices(IServiceCollection services)
    {
        // Đăng ký Display Driver cho theme toggle
        services.AddDisplayDriver<Navbar, ToggleThemeNavbarDisplayDriver>();
    }
}
```

### 🏢 OrchardCore.Themes Module Startup

```csharp
// OrchardCore.Themes/Startup.cs - Core themes module startup
public sealed class Startup : StartupBase
{
    public override void ConfigureServices(IServiceCollection services)
    {
        // Resource Management
        services.AddResourceConfiguration<ResourceManagementOptionsConfiguration>();
        
        // Recipe Support
        services.AddRecipeExecutionStep<ThemesStep>();
        
        // Permissions
        services.AddPermissionProvider<Permissions>();
        
        // Theme Services
        services.AddScoped<IThemeSelector, SiteThemeSelector>();
        services.AddScoped<ISiteThemeService, SiteThemeService>();
        services.AddScoped<IThemeService, ThemeService>();
        services.AddScoped<ThemeTogglerService>();
        
        // Navigation
        services.AddNavigationProvider<AdminMenu>();
        
        // Deployment
        services.AddDeployment<ThemesDeploymentSource, ThemesDeploymentStep, ThemesDeploymentStepDriver>();
        
        // Display Drivers
        services.AddDisplayDriver<ThemeEntry, ThemeEntryDisplayDriver>();
    }
}
```

## 🛠️ Theme Services Architecture

### 🎨 IThemeService & ThemeService

```csharp
// IThemeService.cs - Interface cho theme management
public interface IThemeService
{
    Task DisableThemeFeaturesAsync(string themeName);
    Task EnableThemeFeaturesAsync(string themeName);
}

// ThemeService.cs - Implementation
public class ThemeService : IThemeService
{
    private readonly IExtensionManager _extensionManager;
    private readonly IShellFeaturesManager _shellFeaturesManager;
    private readonly INotifier _notifier;
    private readonly ISiteThemeService _siteThemeService;
    protected readonly IHtmlLocalizer H;

    public ThemeService(
        IExtensionManager extensionManager,
        IShellFeaturesManager shellFeaturesManager,
        ISiteThemeService siteThemeService,
        IHtmlLocalizer<ThemeService> htmlLocalizer,
        INotifier notifier)
    {
        _extensionManager = extensionManager;
        _shellFeaturesManager = shellFeaturesManager;
        _siteThemeService = siteThemeService;
        _notifier = notifier;
        H = htmlLocalizer;
    }

    public async Task EnableThemeFeaturesAsync(string themeName)
    {
        var themes = new Stack<string>();
        
        // Build theme hierarchy (base themes first)
        while (themeName != null)
        {
            if (themes.Contains(themeName))
            {
                throw new InvalidOperationException(H["The theme \"{0}\" is already in the stack of themes that need features enabled.", themeName].ToString());
            }

            themes.Push(themeName);

            var extensionInfo = _extensionManager.GetExtension(themeName);
            var theme = new ThemeExtensionInfo(extensionInfo);
            themeName = theme.BaseTheme;
        }

        // Enable features from base to derived
        while (themes.Count > 0)
        {
            var themeId = themes.Pop();
            await EnableFeaturesAsync(new[] { themeId }, true);
        }
    }

    public async Task DisableThemeFeaturesAsync(string themeName)
    {
        var themes = new Queue<string>();
        
        // Build theme hierarchy (derived themes first)
        while (themeName != null)
        {
            if (themes.Contains(themeName))
            {
                throw new InvalidOperationException(H["The theme \"{0}\" is already in the stack of themes that need features disabled.", themeName].ToString());
            }

            themes.Enqueue(themeName);

            var theme = _extensionManager.GetExtension(themeName);
            themeName = !string.IsNullOrWhiteSpace(theme.Manifest.Name)
                ? theme.Manifest.Name
                : null;
        }

        var currentTheme = await _siteThemeService.GetSiteThemeNameAsync();

        // Disable features from derived to base (skip current theme)
        while (themes.Count > 0)
        {
            var themeId = themes.Dequeue();
            
            if (themeId != currentTheme)
            {
                await DisableFeaturesAsync(new[] { themeId }, true);
            }
        }
    }
}
```

### 🌐 ISiteThemeService & SiteThemeService

```csharp
// ISiteThemeService.cs - Interface cho site theme management
public interface ISiteThemeService
{
    Task<IExtensionInfo> GetSiteThemeAsync();
    Task SetSiteThemeAsync(string themeName);
    Task<string> GetSiteThemeNameAsync();
}

// SiteThemeService.cs - Implementation
public class SiteThemeService : ISiteThemeService
{
    private readonly ISiteService _siteService;
    private readonly IExtensionManager _extensionManager;

    public SiteThemeService(
        ISiteService siteService,
        IExtensionManager extensionManager)
    {
        _siteService = siteService;
        _extensionManager = extensionManager;
    }

    public async Task<IExtensionInfo> GetSiteThemeAsync()
    {
        var currentThemeName = await GetSiteThemeNameAsync();
        if (string.IsNullOrEmpty(currentThemeName))
        {
            return null;
        }

        return _extensionManager.GetExtension(currentThemeName);
    }

    public async Task SetSiteThemeAsync(string themeName)
    {
        var site = await _siteService.LoadSiteSettingsAsync();
        site.Properties["CurrentThemeName"] = themeName;
        await _siteService.UpdateSiteSettingsAsync(site);
    }

    public async Task<string> GetSiteThemeNameAsync()
    {
        var site = await _siteService.GetSiteSettingsAsync();
        return (string)site.Properties["CurrentThemeName"];
    }
}
```

### 🎯 IThemeSelector & SiteThemeSelector

```csharp
// SiteThemeSelector.cs - Theme selection logic
public class SiteThemeSelector : IThemeSelector
{
    private readonly ISiteThemeService _siteThemeService;

    public SiteThemeSelector(ISiteThemeService siteThemeService)
    {
        _siteThemeService = siteThemeService;
    }

    public async Task<ThemeSelectorResult> GetThemeAsync()
    {
        var currentThemeName = await _siteThemeService.GetSiteThemeNameAsync();
        if (string.IsNullOrEmpty(currentThemeName))
        {
            return null;
        }

        return new ThemeSelectorResult
        {
            Priority = 0,
            ThemeName = currentThemeName,
        };
    }
}
```

### 🔄 ThemeTogglerService

```csharp
// ThemeTogglerService.cs - Theme toggle functionality
public class ThemeTogglerService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ISiteService _siteService;

    public ThemeTogglerService(
        IHttpContextAccessor httpContextAccessor,
        ISiteService siteService,
        ShellSettings shellSettings)
    {
        _httpContextAccessor = httpContextAccessor;
        _siteService = siteService;
        CurrentTenant = shellSettings.Name;
    }

    public string CurrentTenant { get; }

    public async Task<string> CurrentTheme()
    {
        var adminSettings = await _siteService.GetSettingsAsync<AdminSettings>();

        if (adminSettings.DisplayThemeToggler)
        {
            var cookieName = $"{CurrentTenant}-admintheme";

            if (_httpContextAccessor.HttpContext.Request.Cookies.TryGetValue(cookieName, out var value)
                && !string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            return "auto";
        }

        return "lite";
    }
}
```

## 🎨 Display Drivers

### 🔄 ToggleThemeNavbarDisplayDriver

```csharp
// ToggleThemeNavbarDisplayDriver.cs - Display driver cho theme toggle
public sealed class ToggleThemeNavbarDisplayDriver : DisplayDriver<Navbar>
{
    private readonly ISiteService _siteService;

    public ToggleThemeNavbarDisplayDriver(ISiteService siteService)
    {
        _siteService = siteService;
    }

    public override IDisplayResult Display(Navbar model, BuildDisplayContext context)
    {
        return View("ToggleTheme", model)
            .RenderWhen(async () => (await _siteService.GetSettingsAsync<AdminSettings>()).DisplayThemeToggler)
            .Location(OrchardCoreConstants.DisplayType.DetailAdmin, "Content:10");
    }
}
```

## ⚙️ Configuration Options

### 🎨 TheAdminThemeOptions

```csharp
// TheAdminThemeOptions.cs - Admin theme styling options
public class TheAdminThemeOptions
{
    /// <summary>
    /// CSS classes cho wrapper elements
    /// </summary>
    public string WrapperClasses { get; set; } = "mb-3";

    /// <summary>
    /// CSS classes cho limited width wrappers
    /// </summary>
    public string LimitedWidthWrapperClasses { get; set; } = "row";

    /// <summary>
    /// CSS classes cho limited width elements
    /// </summary>
    public string LimitedWidthClasses { get; set; } = "col-md-6 col-lg-4 col-xxl-3";

    /// <summary>
    /// CSS classes cho leading elements
    /// </summary>
    public string StartClasses { get; set; }

    /// <summary>
    /// CSS classes cho trailing elements
    /// </summary>
    public string EndClasses { get; set; }

    /// <summary>
    /// CSS classes cho labels
    /// </summary>
    public string LabelClasses { get; set; } = "form-label";

    /// <summary>
    /// CSS classes cho offset elements
    /// </summary>
    public string OffsetClasses { get; set; }

    /// <summary>
    /// CSS classes cho required labels
    /// </summary>
    public string LabelRequiredClasses { get; set; } = "input-required";
}
```

## 🔧 Service Registration Extensions

### 📦 AddResourceConfiguration Extension

```csharp
// ServiceCollectionExtensions.cs - Resource configuration extension
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Đăng ký service implementing IConfigureOptions<ResourceManagementOptions>
    /// </summary>
    public static IServiceCollection AddResourceConfiguration<T>(this IServiceCollection services)
        where T : class, IConfigureOptions<ResourceManagementOptions>
    {
        services.TryAddEnumerable(ServiceDescriptor.Transient<IConfigureOptions<ResourceManagementOptions>, T>());
        return services;
    }
}
```

### 🎨 AddDisplayDriver Extension

```csharp
// ServiceCollectionExtensions.cs - Display driver extension
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSiteDisplayDriver<TDriver>(this IServiceCollection services)
        where TDriver : class, IDisplayDriver<ISite>
        => services.AddDisplayDriver<ISite, TDriver>();

    public static IServiceCollection AddDisplayDriver<TModel, TDriver>(this IServiceCollection services)
        where TDriver : class, IDisplayDriver<TModel>
    {
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IDisplayDriver<TModel>, TDriver>());
        return services;
    }
}
```

## 🎯 Best Practices & Patterns

### ✅ Service Registration Best Practices

```csharp
// ✅ GOOD: Proper startup class structure
public sealed class MyThemeStartup : StartupBase
{
    private readonly IShellConfiguration _configuration;

    public MyThemeStartup(IShellConfiguration configuration)
    {
        _configuration = configuration;
    }

    public override void ConfigureServices(IServiceCollection services)
    {
        // 1. Resource Configuration
        services.AddResourceConfiguration<MyThemeResourceConfiguration>();
        
        // 2. Display Drivers
        services.AddDisplayDriver<Navbar, MyThemeNavbarDisplayDriver>();
        services.AddDisplayDriver<ISite, MyThemeSiteDisplayDriver>();
        
        // 3. Custom Services
        services.AddScoped<IMyThemeService, MyThemeService>();
        services.AddTransient<MyThemeHelper>();
        
        // 4. Options Configuration
        services.Configure<MyThemeOptions>(_configuration.GetSection("MyTheme"));
        services.Configure<MyThemeOptions>(options =>
        {
            options.DefaultColor = "blue";
            options.EnableAnimations = true;
        });
        
        // 5. Conditional Registration
        if (_configuration.GetValue<bool>("MyTheme:EnableAdvancedFeatures"))
        {
            services.AddScoped<IAdvancedFeatureService, AdvancedFeatureService>();
        }
    }

    public override void Configure(IApplicationBuilder app, IEndpointRouteBuilder routes, IServiceProvider serviceProvider)
    {
        // Configure middleware if needed
        // Usually not needed for themes
    }
}
```

### 🏗️ Custom Service Implementation

```csharp
// ✅ GOOD: Custom theme service
public interface IMyThemeService
{
    Task<string> GetThemeColorAsync();
    Task SetThemeColorAsync(string color);
    Task<bool> IsFeatureEnabledAsync(string featureName);
}

public class MyThemeService : IMyThemeService
{
    private readonly ISiteService _siteService;
    private readonly ILogger<MyThemeService> _logger;
    private readonly IOptions<MyThemeOptions> _options;

    public MyThemeService(
        ISiteService siteService,
        ILogger<MyThemeService> logger,
        IOptions<MyThemeOptions> options)
    {
        _siteService = siteService;
        _logger = logger;
        _options = options;
    }

    public async Task<string> GetThemeColorAsync()
    {
        try
        {
            var site = await _siteService.GetSiteSettingsAsync();
            var color = (string)site.Properties["MyTheme_Color"];
            return color ?? _options.Value.DefaultColor;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting theme color");
            return _options.Value.DefaultColor;
        }
    }

    public async Task SetThemeColorAsync(string color)
    {
        try
        {
            var site = await _siteService.LoadSiteSettingsAsync();
            site.Properties["MyTheme_Color"] = color;
            await _siteService.UpdateSiteSettingsAsync(site);
            
            _logger.LogInformation("Theme color changed to {Color}", color);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting theme color to {Color}", color);
            throw;
        }
    }

    public async Task<bool> IsFeatureEnabledAsync(string featureName)
    {
        var site = await _siteService.GetSiteSettingsAsync();
        var features = (string[])site.Properties["MyTheme_EnabledFeatures"] ?? [];
        return features.Contains(featureName);
    }
}
```

### 🎨 Advanced Display Driver

```csharp
// ✅ GOOD: Advanced display driver với multiple displays
public class MyThemeNavbarDisplayDriver : DisplayDriver<Navbar>
{
    private readonly IMyThemeService _themeService;
    private readonly IAuthorizationService _authorizationService;
    private readonly IHtmlLocalizer<MyThemeNavbarDisplayDriver> H;

    public MyThemeNavbarDisplayDriver(
        IMyThemeService themeService,
        IAuthorizationService authorizationService,
        IHtmlLocalizer<MyThemeNavbarDisplayDriver> htmlLocalizer)
    {
        _themeService = themeService;
        _authorizationService = authorizationService;
        H = htmlLocalizer;
    }

    public override async Task<IDisplayResult> DisplayAsync(Navbar model, BuildDisplayContext context)
    {
        var user = context.ServiceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext?.User;
        
        if (user == null || !await _authorizationService.AuthorizeAsync(user, Permissions.ManageThemes))
        {
            return null;
        }

        var results = new List<IDisplayResult>();

        // Theme color picker
        results.Add(View("MyTheme_ColorPicker", model)
            .Location("Content:5")
            .RenderWhen(async () => await _themeService.IsFeatureEnabledAsync("ColorPicker")));

        // Theme settings
        results.Add(View("MyTheme_Settings", model)
            .Location("Content:10")
            .RenderWhen(async () => await _themeService.IsFeatureEnabledAsync("Settings")));

        return Combine(results.ToArray());
    }

    public override async Task<IDisplayResult> EditAsync(Navbar model, BuildEditContext context)
    {
        return Initialize<MyThemeNavbarViewModel>("MyTheme_NavbarEdit", async viewModel =>
        {
            viewModel.CurrentColor = await _themeService.GetThemeColorAsync();
            viewModel.AvailableColors = new[] { "blue", "green", "red", "purple" };
        }).Location("Content:5");
    }

    public override async Task<IDisplayResult> UpdateAsync(Navbar model, UpdateDisplayContext context)
    {
        var viewModel = new MyThemeNavbarViewModel();
        
        if (await context.Updater.TryUpdateModelAsync(viewModel, Prefix))
        {
            await _themeService.SetThemeColorAsync(viewModel.CurrentColor);
            context.Updater.ModelState.Clear();
        }

        return await EditAsync(model, context);
    }
}
```

### ⚙️ Configuration Pattern

```csharp
// ✅ GOOD: Comprehensive options configuration
public class MyThemeOptions
{
    public string DefaultColor { get; set; } = "blue";
    public bool EnableAnimations { get; set; } = true;
    public int AnimationDuration { get; set; } = 300;
    public string[] EnabledFeatures { get; set; } = [];
    public Dictionary<string, object> CustomSettings { get; set; } = new();
}

// Configuration trong appsettings.json
{
  "MyTheme": {
    "DefaultColor": "purple",
    "EnableAnimations": true,
    "AnimationDuration": 500,
    "EnabledFeatures": ["ColorPicker", "Settings", "Analytics"],
    "CustomSettings": {
      "HeaderHeight": 60,
      "SidebarWidth": 250,
      "EnableDarkMode": true
    }
  }
}

// Sử dụng trong service
public class MyThemeConfigurationService
{
    private readonly IOptionsMonitor<MyThemeOptions> _options;

    public MyThemeConfigurationService(IOptionsMonitor<MyThemeOptions> options)
    {
        _options = options;
        
        // Listen for configuration changes
        _options.OnChange(OnOptionsChanged);
    }

    private void OnOptionsChanged(MyThemeOptions options)
    {
        // Handle configuration changes
        Console.WriteLine($"Theme configuration changed: Color = {options.DefaultColor}");
    }

    public MyThemeOptions GetCurrentOptions() => _options.CurrentValue;
}
```

### 🚫 Common Anti-patterns

```csharp
// ❌ BAD: Không sử dụng DI properly
public class BadThemeService
{
    public BadThemeService()
    {
        // Tạo dependencies manually thay vì inject
        var siteService = new SiteService(); // BAD!
    }
}

// ❌ BAD: Không handle exceptions
public async Task<string> GetThemeColorAsync()
{
    var site = await _siteService.GetSiteSettingsAsync(); // Có thể throw exception
    return (string)site.Properties["Color"]; // Có thể null
}

// ❌ BAD: Hardcode configuration
public class BadStartup : StartupBase
{
    public override void ConfigureServices(IServiceCollection services)
    {
        services.Configure<MyThemeOptions>(options =>
        {
            options.DefaultColor = "blue"; // Hardcoded!
            options.ApiUrl = "https://api.example.com"; // Hardcoded!
        });
    }
}

// ❌ BAD: Không sử dụng proper service lifetimes
services.AddSingleton<IMyThemeService, MyThemeService>(); // BAD: Singleton cho service có state
services.AddTransient<IExpensiveService, ExpensiveService>(); // BAD: Transient cho expensive service
```

## 🎯 Kết luận

Services & Startup Configuration trong OrchardCore themes cung cấp:

1. **Startup Architecture** với StartupBase và IStartup interface
2. **Theme Services** cho theme management, selection, và toggling
3. **Display Drivers** cho UI components và theme-specific displays
4. **Configuration Options** với Options pattern và IShellConfiguration
5. **Service Registration Extensions** cho resource configuration và display drivers
6. **Dependency Injection** patterns với proper service lifetimes
7. **Configuration Management** với appsettings.json và runtime updates

Hệ thống này đảm bảo theme có thể tích hợp sâu vào OrchardCore architecture, cung cấp services tùy chỉnh, và quản lý configuration một cách hiệu quả.