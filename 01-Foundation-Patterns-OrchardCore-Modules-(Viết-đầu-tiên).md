# Hướng Dẫn Tạo Modules Chuẩn OrchardCore 2.2.1

## Mục Lục
1. [Giới Thiệu](#giới-thiệu)
2. [Cấu Trúc Cơ Bản](#cấu-trúc-cơ-bản)
3. [Các File Bắt Buộc](#các-file-bắt-buộc)
4. [Patterns Phổ Biến](#patterns-phổ-biến)
5. [Cách Tránh Xung Đột](#cách-tránh-xung-đột)
6. [Best Practices](#best-practices)
7. [Testing và Debugging](#testing-và-debugging)
8. [Deployment](#deployment)
9. [Checklist](#checklist)

## Giới Thiệu

OrchardCore là một framework CMS modular mạnh mẽ được xây dựng trên .NET Core. Việc tạo modules đúng chuẩn sẽ đảm bảo:
- Tính ổn định và bảo mật
- Khả năng tương thích với các modules khác
- Dễ dàng bảo trì và nâng cấp
- Hiệu suất tối ưu

## Cấu Trúc Cơ Bản

### Cấu trúc thư mục chuẩn:
```
YourModule/
├── Manifest.cs                 (BẮT BUỘC - Định nghĩa module)
├── Startup.cs                  (BẮT BUỘC - Đăng ký services)
├── YourModule.csproj          (BẮT BUỘC - Project configuration)
├── Models/                     (Tùy chọn - Data models)
│   ├── YourContentPart.cs
│   ├── YourSettings.cs
│   └── YourField.cs
├── Drivers/                    (Tùy chọn - Display drivers)
│   ├── YourContentPartDisplayDriver.cs
│   └── YourFieldDisplayDriver.cs
├── Handlers/                   (Tùy chọn - Business logic handlers)
│   ├── YourContentPartHandler.cs
│   └── YourEventHandler.cs
├── Controllers/                (Tùy chọn - MVC controllers)
│   ├── HomeController.cs
│   └── AdminController.cs
├── Views/                      (Tùy chọn - Razor views)
│   ├── YourContentPart.Edit.cshtml
│   ├── YourContentPart.cshtml
│   ├── YourContentPart.Summary.cshtml
│   └── _ViewImports.cshtml
├── ViewModels/                 (Tùy chọn - View models)
│   ├── YourViewModel.cs
│   └── YourSettingsViewModel.cs
├── Services/                   (Tùy chọn - Business services)
│   ├── IYourService.cs
│   └── YourService.cs
├── Migrations.cs              (Tùy chọn - Database migrations)
├── Permissions.cs             (Tùy chọn - Permission definitions)
├── AdminMenu.cs               (Tùy chọn - Admin menu items)
├── ResourceManifest.cs        (Tùy chọn - CSS/JS resources)
├── placement.json             (Tùy chọn - Shape placement)
└── wwwroot/                   (Tùy chọn - Static files)
    ├── Scripts/
    ├── Styles/
    └── Images/
```

## Các File Bắt Buộc

### 1. Manifest.cs - Định nghĩa Module

```csharp
using OrchardCore.Modules.Manifest;

[assembly: Module(
    Name = "Your Module Name",
    Author = "Your Name or Company",
    Website = "https://yourwebsite.com",
    Version = "1.0.0",
    Description = "Detailed description of what your module does",
    Category = "Content Management", // hoặc "Commerce", "Security", etc.
    Dependencies = new[] { 
        "OrchardCore.Contents",
        "OrchardCore.ContentFields" 
    },
    Tags = new[] { "content", "management", "custom" }
)]

// Định nghĩa feature chính (tự động tạo từ Module)
// Có thể định nghĩa thêm features phụ
[assembly: Feature(
    Id = "YourModule.AdvancedFeature",
    Name = "Advanced Feature",
    Description = "Advanced functionality for power users",
    Category = "Advanced",
    Dependencies = new[] { "YourModule" }, // Phụ thuộc vào feature chính
    Priority = "10" // Thứ tự load
)]

[assembly: Feature(
    Id = "YourModule.ApiIntegration",
    Name = "API Integration",
    Description = "REST API endpoints for external integration",
    Category = "API",
    Dependencies = new[] { 
        "YourModule",
        "OrchardCore.Apis.GraphQL" 
    }
)]
```

### 2. Startup.cs - Đăng ký Services

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using OrchardCore.Modules;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Display.ContentDisplay;
using OrchardCore.Data.Migration;
using OrchardCore.DisplayManagement.Handlers;
using OrchardCore.Navigation;
using OrchardCore.Security.Permissions;
using OrchardCore.Settings;
using Fluid;
using YourModule.Models;
using YourModule.Drivers;
using YourModule.Handlers;
using YourModule.Services;
using YourModule.ViewModels;

namespace YourModule;

public sealed class Startup : StartupBase
{
    public override void ConfigureServices(IServiceCollection services)
    {
        // 1. Đăng ký Content Parts
        services.AddContentPart<YourContentPart>()
            .UseDisplayDriver<YourContentPartDisplayDriver>()
            .AddHandler<YourContentPartHandler>();

        // 2. Đăng ký Content Fields
        services.AddContentField<YourField>()
            .UseDisplayDriver<YourFieldDisplayDriver>();

        // 3. Đăng ký Business Services
        services.AddScoped<IYourService, YourService>();
        services.AddSingleton<IYourCacheService, YourCacheService>();
        services.AddTransient<IYourHelperService, YourHelperService>();

        // 4. Đăng ký Background Services
        services.AddSingleton<IBackgroundTask, YourBackgroundTask>();

        // 5. Đăng ký Event Handlers
        services.AddScoped<IContentHandler, YourContentHandler>();
        services.AddScoped<IModularTenantEvents, YourTenantEventHandler>();

        // 6. Đăng ký Display Drivers cho Settings
        services.AddScoped<IDisplayDriver<ISite>, YourSiteSettingsDisplayDriver>();

        // 7. Đăng ký Navigation Provider
        services.AddScoped<INavigationProvider, YourAdminMenu>();

        // 8. Đăng ký Permission Provider
        services.AddScoped<IPermissionProvider, YourPermissions>();

        // 9. Đăng ký Data Migration
        services.AddDataMigration<YourMigrations>();

        // 10. Đăng ký Resource Manifest
        services.AddScoped<IResourceManifestProvider, YourResourceManifest>();

        // 11. Cấu hình Liquid Templates
        services.Configure<TemplateOptions>(o =>
        {
            o.MemberAccessStrategy.Register<YourViewModel>();
            o.MemberAccessStrategy.Register<YourContentPartViewModel>();
            o.MemberAccessStrategy.Register<YourSettingsViewModel>();
        });

        // 12. Cấu hình Settings
        services.Configure<YourModuleSettings>(configuration.GetSection("YourModule"));

        // 13. Đăng ký Command Handlers (nếu có CLI commands)
        services.AddScoped<ICommandHandler, YourCommandHandler>();
    }

    public override void Configure(IApplicationBuilder builder, IEndpointRouteBuilder routes, IServiceProvider serviceProvider)
    {
        // 1. Đăng ký Controller Routes
        routes.MapAreaControllerRoute(
            name: "YourModule.Home.Index",
            areaName: "YourModule",
            pattern: "your-module/{action=Index}/{id?}",
            defaults: new { controller = "Home" }
        );

        routes.MapAreaControllerRoute(
            name: "YourModule.Api",
            areaName: "YourModule", 
            pattern: "api/your-module/{action}",
            defaults: new { controller = "Api" }
        );

        // 2. Đăng ký Admin Routes (sử dụng Admin attribute thay vì route thủ công)
        // Xem ví dụ trong Controllers section

        // 3. Đăng ký Middleware (nếu cần)
        builder.UseMiddleware<YourCustomMiddleware>();
    }
}

// Startup class cho feature phụ
[RequireFeatures("YourModule.AdvancedFeature")]
public sealed class AdvancedFeatureStartup : StartupBase
{
    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<IAdvancedService, AdvancedService>();
        services.AddContentPart<AdvancedContentPart>()
            .UseDisplayDriver<AdvancedContentPartDisplayDriver>();
    }
}

// Startup class cho API feature
[RequireFeatures("YourModule.ApiIntegration")]
public sealed class ApiIntegrationStartup : StartupBase
{
    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<IApiService, ApiService>();
        // Đăng ký GraphQL types nếu cần
    }
}
```

### 3. YourModule.csproj - Project Configuration

```xml
<Project Sdk="Microsoft.NET.Sdk.Razor">

  <PropertyGroup>
    <!-- Bắt buộc cho Razor support -->
    <AddRazorSupportForMvc>true</AddRazorSupportForMvc>
    
    <!-- Target Framework -->
    <TargetFramework>net8.0</TargetFramework>
    
    <!-- Package Information -->
    <PackageId>YourCompany.OrchardCore.YourModule</PackageId>
    <Title>Your Module Title</Title>
    <Description>Detailed description of your module functionality and features</Description>
    <PackageVersion>1.0.0</PackageVersion>
    <Authors>Your Name or Company</Authors>
    <Company>Your Company</Company>
    <Product>Your Module</Product>
    <Copyright>Copyright © Your Company 2024</Copyright>
    
    <!-- Package Metadata -->
    <PackageTags>OrchardCore;Module;CMS;Content;Management</PackageTags>
    <PackageProjectUrl>https://github.com/yourcompany/yourmodule</PackageProjectUrl>
    <RepositoryUrl>https://github.com/yourcompany/yourmodule</RepositoryUrl>
    <RepositoryType>git</RepositoryType>
    <PackageLicenseExpression>MIT</PackageLicenseExpression>
    <PackageRequireLicenseAcceptance>false</PackageRequireLicenseAcceptance>
    
    <!-- Build Configuration -->
    <GeneratePackageOnBuild>false</GeneratePackageOnBuild>
    <IncludeSymbols>true</IncludeSymbols>
    <SymbolPackageFormat>snupkg</SymbolPackageFormat>
  </PropertyGroup>

  <!-- Framework References -->
  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
  </ItemGroup>

  <!-- OrchardCore Dependencies -->
  <ItemGroup>
    <!-- Core Dependencies -->
    <ProjectReference Include="..\..\OrchardCore\OrchardCore.Module.Targets\OrchardCore.Module.Targets.csproj" />
    <ProjectReference Include="..\..\OrchardCore\OrchardCore.ContentManagement\OrchardCore.ContentManagement.csproj" />
    <ProjectReference Include="..\..\OrchardCore\OrchardCore.ContentManagement.Display\OrchardCore.ContentManagement.Display.csproj" />
    <ProjectReference Include="..\..\OrchardCore\OrchardCore.DisplayManagement\OrchardCore.DisplayManagement.csproj" />
    
    <!-- Additional Dependencies (thêm theo nhu cầu) -->
    <ProjectReference Include="..\..\OrchardCore\OrchardCore.Data.Abstractions\OrchardCore.Data.Abstractions.csproj" />
    <ProjectReference Include="..\..\OrchardCore\OrchardCore.Navigation.Core\OrchardCore.Navigation.Core.csproj" />
    <ProjectReference Include="..\..\OrchardCore\OrchardCore.ResourceManagement\OrchardCore.ResourceManagement.csproj" />
    <ProjectReference Include="..\..\OrchardCore\OrchardCore.Settings.Core\OrchardCore.Settings.Core.csproj" />
    <ProjectReference Include="..\..\OrchardCore\OrchardCore.Admin.Abstractions\OrchardCore.Admin.Abstractions.csproj" />
  </ItemGroup>

  <!-- External NuGet Packages (nếu cần) -->
  <ItemGroup>
    <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
    <!-- Thêm các packages khác nếu cần -->
  </ItemGroup>

  <!-- Static Files -->
  <ItemGroup>
    <None Include="wwwroot\**" CopyToOutputDirectory="PreserveNewest" />
  </ItemGroup>

</Project>
```

## Patterns Phổ Biến

### 1. Content Part Pattern

#### A. Model Definition
```csharp
// Models/YourContentPart.cs
using OrchardCore.ContentManagement;
using System.ComponentModel.DataAnnotations;

namespace YourModule.Models
{
    public class YourContentPart : ContentPart
    {
        [Required]
        [StringLength(255)]
        public string Title { get; set; } = string.Empty;

        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;

        public bool IsEnabled { get; set; } = true;

        public DateTime? PublishDate { get; set; }

        public int Priority { get; set; } = 0;

        // Complex properties should use JSON serialization
        public List<string> Tags { get; set; } = new();

        public YourCustomSettings Settings { get; set; } = new();
    }

    public class YourCustomSettings
    {
        public string ApiEndpoint { get; set; } = string.Empty;
        public int TimeoutSeconds { get; set; } = 30;
        public bool EnableLogging { get; set; } = true;
    }
}

// Models/YourContentPartSettings.cs
namespace YourModule.Models
{
    public class YourContentPartSettings
    {
        public string DefaultTitle { get; set; } = "Default Title";
        public bool ShowDescription { get; set; } = true;
        public int MaxTags { get; set; } = 10;
        public string[] AllowedFileTypes { get; set; } = Array.Empty<string>();
    }
}
```

#### B. Display Driver
```csharp
// Drivers/YourContentPartDisplayDriver.cs
using OrchardCore.ContentManagement.Display.ContentDisplay;
using OrchardCore.ContentManagement.Display.Models;
using OrchardCore.ContentManagement.Metadata;
using OrchardCore.DisplayManagement.Views;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using YourModule.Models;
using YourModule.ViewModels;
using YourModule.Services;

namespace YourModule.Drivers
{
    public class YourContentPartDisplayDriver : ContentPartDisplayDriver<YourContentPart>
    {
        private readonly IContentDefinitionManager _contentDefinitionManager;
        private readonly IYourService _yourService;
        private readonly IStringLocalizer S;
        private readonly ILogger<YourContentPartDisplayDriver> _logger;

        public YourContentPartDisplayDriver(
            IContentDefinitionManager contentDefinitionManager,
            IYourService yourService,
            IStringLocalizer<YourContentPartDisplayDriver> stringLocalizer,
            ILogger<YourContentPartDisplayDriver> logger)
        {
            _contentDefinitionManager = contentDefinitionManager;
            _yourService = yourService;
            S = stringLocalizer;
            _logger = logger;
        }

        // Display shape for frontend
        public override IDisplayResult Display(YourContentPart part, BuildPartDisplayContext context)
        {
            return Initialize<YourContentPartViewModel>(GetDisplayShapeType(context), async viewModel =>
            {
                var settings = GetSettings(part, context);
                
                viewModel.Title = part.Title;
                viewModel.Description = settings.ShowDescription ? part.Description : string.Empty;
                viewModel.IsEnabled = part.IsEnabled;
                viewModel.PublishDate = part.PublishDate;
                viewModel.Priority = part.Priority;
                viewModel.Tags = part.Tags;
                viewModel.ContentItem = part.ContentItem;
                
                // Load additional data if needed
                viewModel.AdditionalData = await _yourService.GetAdditionalDataAsync(part.ContentItem.ContentItemId);
            })
            .Location("Detail", "Content:5")
            .Location("Summary", "Content:5");
        }

        // Edit shape for admin
        public override IDisplayResult Edit(YourContentPart part, BuildPartEditorContext context)
        {
            return Initialize<YourContentPartEditViewModel>(GetEditorShapeType(context), viewModel =>
            {
                var settings = GetSettings(part, context);
                
                viewModel.Title = part.Title;
                viewModel.Description = part.Description;
                viewModel.IsEnabled = part.IsEnabled;
                viewModel.PublishDate = part.PublishDate;
                viewModel.Priority = part.Priority;
                viewModel.Tags = string.Join(", ", part.Tags);
                viewModel.Settings = part.Settings;
                
                // Pass settings to view
                viewModel.PartSettings = settings;
            });
        }

        // Update logic
        public override async Task<IDisplayResult> UpdateAsync(YourContentPart part, IUpdateModel updater, UpdatePartEditorContext context)
        {
            var viewModel = new YourContentPartEditViewModel();
            
            if (await updater.TryUpdateModelAsync(viewModel, Prefix))
            {
                var settings = GetSettings(part, context);
                
                // Validation
                if (string.IsNullOrWhiteSpace(viewModel.Title))
                {
                    updater.ModelState.AddModelError(nameof(viewModel.Title), S["Title is required"]);
                }
                
                if (viewModel.Title?.Length > 255)
                {
                    updater.ModelState.AddModelError(nameof(viewModel.Title), S["Title cannot exceed 255 characters"]);
                }

                // Parse tags
                var tags = viewModel.Tags?.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(t => t.Trim())
                    .Where(t => !string.IsNullOrEmpty(t))
                    .Take(settings.MaxTags)
                    .ToList() ?? new List<string>();

                if (updater.ModelState.IsValid)
                {
                    part.Title = viewModel.Title?.Trim() ?? string.Empty;
                    part.Description = viewModel.Description?.Trim() ?? string.Empty;
                    part.IsEnabled = viewModel.IsEnabled;
                    part.PublishDate = viewModel.PublishDate;
                    part.Priority = viewModel.Priority;
                    part.Tags = tags;
                    part.Settings = viewModel.Settings ?? new YourCustomSettings();

                    _logger.LogInformation("Updated YourContentPart for content item {ContentItemId}", 
                        part.ContentItem.ContentItemId);
                }
            }

            return Edit(part, context);
        }

        private YourContentPartSettings GetSettings(YourContentPart part, BuildPartDisplayContext context)
        {
            var contentTypePartDefinition = _contentDefinitionManager
                .GetTypeDefinition(part.ContentItem.ContentType)?
                .Parts.FirstOrDefault(p => p.PartDefinition.Name == nameof(YourContentPart));

            return contentTypePartDefinition?.GetSettings<YourContentPartSettings>() ?? new YourContentPartSettings();
        }

        private YourContentPartSettings GetSettings(YourContentPart part, BuildPartEditorContext context)
        {
            var contentTypePartDefinition = _contentDefinitionManager
                .GetTypeDefinition(part.ContentItem.ContentType)?
                .Parts.FirstOrDefault(p => p.PartDefinition.Name == nameof(YourContentPart));

            return contentTypePartDefinition?.GetSettings<YourContentPartSettings>() ?? new YourContentPartSettings();
        }
    }
}
```

#### C. Content Part Handler
```csharp
// Handlers/YourContentPartHandler.cs
using OrchardCore.ContentManagement.Handlers;
using Microsoft.Extensions.Logging;
using YourModule.Models;
using YourModule.Services;

namespace YourModule.Handlers
{
    public class YourContentPartHandler : ContentPartHandler<YourContentPart>
    {
        private readonly IYourService _yourService;
        private readonly ILogger<YourContentPartHandler> _logger;

        public YourContentPartHandler(
            IYourService yourService,
            ILogger<YourContentPartHandler> logger)
        {
            _yourService = yourService;
            _logger = logger;
        }

        public override async Task CreatedAsync(CreateContentContext context, YourContentPart part)
        {
            // Logic khi content được tạo
            _logger.LogInformation("YourContentPart created for content item {ContentItemId}", 
                context.ContentItem.ContentItemId);
            
            await _yourService.OnContentCreatedAsync(part);
        }

        public override async Task UpdatedAsync(UpdateContentContext context, YourContentPart part)
        {
            // Logic khi content được cập nhật
            _logger.LogInformation("YourContentPart updated for content item {ContentItemId}", 
                context.ContentItem.ContentItemId);
            
            await _yourService.OnContentUpdatedAsync(part);
        }

        public override async Task PublishedAsync(PublishContentContext context, YourContentPart part)
        {
            // Logic khi content được publish
            if (part.IsEnabled)
            {
                _logger.LogInformation("YourContentPart published for content item {ContentItemId}", 
                    context.ContentItem.ContentItemId);
                
                await _yourService.OnContentPublishedAsync(part);
            }
        }

        public override async Task UnpublishedAsync(PublishContentContext context, YourContentPart part)
        {
            // Logic khi content được unpublish
            _logger.LogInformation("YourContentPart unpublished for content item {ContentItemId}", 
                context.ContentItem.ContentItemId);
            
            await _yourService.OnContentUnpublishedAsync(part);
        }

        public override async Task RemovedAsync(RemoveContentContext context, YourContentPart part)
        {
            // Logic khi content được xóa
            _logger.LogInformation("YourContentPart removed for content item {ContentItemId}", 
                context.ContentItem.ContentItemId);
            
            await _yourService.OnContentRemovedAsync(part);
        }

        public override Task LoadingAsync(LoadContentContext context, YourContentPart part)
        {
            // Logic khi content được load
            // Thường dùng để load related data
            return Task.CompletedTask;
        }

        public override Task LoadedAsync(LoadContentContext context, YourContentPart part)
        {
            // Logic sau khi content được load hoàn toàn
            return Task.CompletedTask;
        }
    }
}
```

### 2. Service Pattern

#### A. Service Interface
```csharp
// Services/IYourService.cs
using YourModule.Models;

namespace YourModule.Services
{
    public interface IYourService
    {
        Task<string> ProcessDataAsync(string input);
        Task<IEnumerable<YourModel>> GetDataAsync(int page = 1, int pageSize = 20);
        Task<YourModel?> GetByIdAsync(string id);
        Task<bool> CreateAsync(YourModel model);
        Task<bool> UpdateAsync(YourModel model);
        Task<bool> DeleteAsync(string id);
        
        // Content Part related methods
        Task OnContentCreatedAsync(YourContentPart part);
        Task OnContentUpdatedAsync(YourContentPart part);
        Task OnContentPublishedAsync(YourContentPart part);
        Task OnContentUnpublishedAsync(YourContentPart part);
        Task OnContentRemovedAsync(YourContentPart part);
        
        Task<object> GetAdditionalDataAsync(string contentItemId);
    }
}
```

#### B. Service Implementation
```csharp
// Services/YourService.cs
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OrchardCore.Environment.Shell;
using OrchardCore.ContentManagement;
using YourModule.Models;

namespace YourModule.Services
{
    public class YourService : IYourService
    {
        private readonly ILogger<YourService> _logger;
        private readonly ShellSettings _shellSettings;
        private readonly YourModuleSettings _settings;
        private readonly IContentManager _contentManager;

        public YourService(
            ILogger<YourService> logger,
            ShellSettings shellSettings,
            IOptions<YourModuleSettings> settings,
            IContentManager contentManager)
        {
            _logger = logger;
            _shellSettings = shellSettings;
            _settings = settings.Value;
            _contentManager = contentManager;
        }

        public async Task<string> ProcessDataAsync(string input)
        {
            try
            {
                _logger.LogInformation("Processing data for tenant: {TenantName}", _shellSettings.Name);
                
                if (string.IsNullOrWhiteSpace(input))
                {
                    throw new ArgumentException("Input cannot be null or empty", nameof(input));
                }

                // Your business logic here
                var result = $"Processed: {input} at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC";
                
                _logger.LogDebug("Data processed successfully: {Result}", result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing data: {Input}", input);
                throw;
            }
        }

        public async Task<IEnumerable<YourModel>> GetDataAsync(int page = 1, int pageSize = 20)
        {
            try
            {
                // Implement pagination logic
                var skip = (page - 1) * pageSize;
                
                // Example: Get content items with YourContentPart
                var contentItems = await _contentManager.Query()
                    .With<YourContentPart>()
                    .Skip(skip)
                    .Take(pageSize)
                    .ListAsync();

                return contentItems.Select(ci => new YourModel
                {
                    Id = ci.ContentItemId,
                    Title = ci.As<YourContentPart>()?.Title ?? string.Empty,
                    Description = ci.As<YourContentPart>()?.Description ?? string.Empty,
                    IsEnabled = ci.As<YourContentPart>()?.IsEnabled ?? false
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting data for page {Page}, pageSize {PageSize}", page, pageSize);
                return Enumerable.Empty<YourModel>();
            }
        }

        public async Task<YourModel?> GetByIdAsync(string id)
        {
            try
            {
                var contentItem = await _contentManager.GetAsync(id);
                if (contentItem == null)
                    return null;

                var part = contentItem.As<YourContentPart>();
                if (part == null)
                    return null;

                return new YourModel
                {
                    Id = contentItem.ContentItemId,
                    Title = part.Title,
                    Description = part.Description,
                    IsEnabled = part.IsEnabled
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting data by id: {Id}", id);
                return null;
            }
        }

        public async Task<bool> CreateAsync(YourModel model)
        {
            try
            {
                // Implementation for creating new content
                _logger.LogInformation("Creating new item: {Title}", model.Title);
                
                // Your creation logic here
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating item: {Title}", model.Title);
                return false;
            }
        }

        public async Task<bool> UpdateAsync(YourModel model)
        {
            try
            {
                _logger.LogInformation("Updating item: {Id}", model.Id);
                
                // Your update logic here
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating item: {Id}", model.Id);
                return false;
            }
        }

        public async Task<bool> DeleteAsync(string id)
        {
            try
            {
                _logger.LogInformation("Deleting item: {Id}", id);
                
                // Your deletion logic here
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting item: {Id}", id);
                return false;
            }
        }

        // Content Part Event Handlers
        public async Task OnContentCreatedAsync(YourContentPart part)
        {
            _logger.LogInformation("Content created event for part: {ContentItemId}", 
                part.ContentItem.ContentItemId);
            
            // Custom logic when content is created
            await Task.CompletedTask;
        }

        public async Task OnContentUpdatedAsync(YourContentPart part)
        {
            _logger.LogInformation("Content updated event for part: {ContentItemId}", 
                part.ContentItem.ContentItemId);
            
            // Custom logic when content is updated
            await Task.CompletedTask;
        }

        public async Task OnContentPublishedAsync(YourContentPart part)
        {
            _logger.LogInformation("Content published event for part: {ContentItemId}", 
                part.ContentItem.ContentItemId);
            
            // Custom logic when content is published
            // E.g., send notifications, update cache, etc.
            await Task.CompletedTask;
        }

        public async Task OnContentUnpublishedAsync(YourContentPart part)
        {
            _logger.LogInformation("Content unpublished event for part: {ContentItemId}", 
                part.ContentItem.ContentItemId);
            
            // Custom logic when content is unpublished
            await Task.CompletedTask;
        }

        public async Task OnContentRemovedAsync(YourContentPart part)
        {
            _logger.LogInformation("Content removed event for part: {ContentItemId}", 
                part.ContentItem.ContentItemId);
            
            // Custom logic when content is removed
            // E.g., cleanup related data
            await Task.CompletedTask;
        }

        public async Task<object> GetAdditionalDataAsync(string contentItemId)
        {
            try
            {
                // Load additional data for display
                return new
                {
                    LoadedAt = DateTime.UtcNow,
                    TenantName = _shellSettings.Name,
                    ContentItemId = contentItemId
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting additional data for content item: {ContentItemId}", contentItemId);
                return new { };
            }
        }
    }

    // Model class
    public class YourModel
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsEnabled { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
```

### 3. Migration Pattern

```csharp
// Migrations.cs
using OrchardCore.ContentManagement.Metadata;
using OrchardCore.ContentManagement.Metadata.Settings;
using OrchardCore.Data.Migration;
using OrchardCore.Recipes.Services;
using YourModule.Models;

namespace YourModule
{
    public class YourMigrations : DataMigration
    {
        private readonly IContentDefinitionManager _contentDefinitionManager;
        private readonly IRecipeMigrator _recipeMigrator;

        public YourMigrations(
            IContentDefinitionManager contentDefinitionManager,
            IRecipeMigrator recipeMigrator)
        {
            _contentDefinitionManager = contentDefinitionManager;
            _recipeMigrator = recipeMigrator;
        }

        // Initial migration - Version 1
        public async Task<int> CreateAsync()
        {
            // 1. Create Content Part Definition
            await _contentDefinitionManager.AlterPartDefinitionAsync(nameof(YourContentPart), part => part
                .Attachable()
                .WithDescription("Your content part description")
                .WithDisplayName("Your Content Part")
                .WithDefaultPosition("5"));

            // 2. Create Content Type using the part
            await _contentDefinitionManager.AlterTypeDefinitionAsync("YourContentType", type => type
                .WithPart(nameof(YourContentPart))
                .WithPart("TitlePart")
                .WithPart("CommonPart")
                .Creatable()
                .Listable()
                .Draftable()
                .Versionable()
                .Securable());

            // 3. Run recipe for initial data (optional)
            await _recipeMigrator.ExecuteAsync("initial-data.recipe.json", this);

            return 1;
        }

        // Migration from version 1 to 2
        public async Task<int> UpdateFrom1Async()
        {
            // Add new settings to existing part
            await _contentDefinitionManager.AlterPartDefinitionAsync(nameof(YourContentPart), part => part
                .WithSettings(new YourContentPartSettings
                {
                    DefaultTitle = "Updated Default Title",
                    ShowDescription = true,
                    MaxTags = 15
                }));

            // Create new content type
            await _contentDefinitionManager.AlterTypeDefinitionAsync("YourAdvancedContentType", type => type
                .WithPart(nameof(YourContentPart))
                .WithPart("TitlePart")
                .WithPart("CommonPart")
                .WithPart("FlowPart")
                .Creatable()
                .Listable()
                .Draftable()
                .Versionable()
                .Securable());

            return 2;
        }

        // Migration from version 2 to 3
        public async Task<int> UpdateFrom2Async()
        {
            // Add new field to existing content type
            await _contentDefinitionManager.AlterTypeDefinitionAsync("YourContentType", type => type
                .WithPart("YourContentType", part => part
                    .WithField("Category", field => field
                        .OfType("TextField")
                        .WithDisplayName("Category")
                        .WithPosition("10"))));

            // Update part settings
            await _contentDefinitionManager.AlterPartDefinitionAsync(nameof(YourContentPart), part => part
                .WithSettings(new YourContentPartSettings
                {
                    DefaultTitle = "Version 3 Default",
                    ShowDescription = true,
                    MaxTags = 20,
                    AllowedFileTypes = new[] { "jpg", "png", "pdf", "docx" }
                }));

            return 3;
        }

        // Migration from version 3 to 4 - Add indexes
        public async Task<int> UpdateFrom3Async()
        {
            // Note: For database schema changes, you might need to use SchemaBuilder
            // This is typically done in a separate DataMigration class that inherits from DataMigration
            // and uses ISchemaBuilder

            // For content definition changes:
            await _contentDefinitionManager.AlterPartDefinitionAsync(nameof(YourContentPart), part => part
                .WithDescription("Updated description for version 4")
                .WithSettings(new YourContentPartSettings
                {
                    DefaultTitle = "Version 4 Default",
                    ShowDescription = true,
                    MaxTags = 25,
                    AllowedFileTypes = new[] { "jpg", "png", "pdf", "docx", "xlsx" }
                }));

            return 4;
        }
    }

    // Separate migration class for database schema changes
    public class YourDatabaseMigrations : DataMigration
    {
        public int Create()
        {
            // Create custom database tables if needed
            SchemaBuilder.CreateMapIndexTable<YourContentPartIndex>(table => table
                .Column<string>("ContentItemId", column => column.WithLength(26))
                .Column<string>("Title", column => column.WithLength(255))
                .Column<bool>("IsEnabled")
                .Column<int>("Priority")
                .Column<DateTime>("PublishDate", column => column.Nullable())
            );

            SchemaBuilder.AlterIndexTable<YourContentPartIndex>(table => table
                .CreateIndex("IDX_YourContentPart_Title", "Title")
                .CreateIndex("IDX_YourContentPart_IsEnabled", "IsEnabled")
                .CreateIndex("IDX_YourContentPart_Priority", "Priority")
            );

            return 1;
        }

        public int UpdateFrom1()
        {
            // Add new columns to existing table
            SchemaBuilder.AlterTable<YourContentPartIndex>(table => table
                .AddColumn<string>("Category", column => column.WithLength(100).Nullable())
                .AddColumn<string>("Tags", column => column.WithLength(1000).Nullable())
            );

            return 2;
        }
    }

    // Index class for database queries
    public class YourContentPartIndex : MapIndex
    {
        public string ContentItemId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public bool IsEnabled { get; set; }
        public int Priority { get; set; }
        public DateTime? PublishDate { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Tags { get; set; } = string.Empty;
    }
}
```

### 4. Controller Pattern

```csharp
// Controllers/HomeController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using OrchardCore.Admin;
using OrchardCore.DisplayManagement;
using OrchardCore.DisplayManagement.Notify;
using OrchardCore.Navigation;
using YourModule.Services;
using YourModule.ViewModels;

namespace YourModule.Controllers
{
    public class HomeController : Controller
    {
        private readonly IYourService _yourService;
        private readonly INotifier _notifier;
        private readonly ILogger<HomeController> _logger;
        private readonly IShapeFactory _shapeFactory;

        public HomeController(
            IYourService yourService,
            INotifier notifier,
            ILogger<HomeController> logger,
            IShapeFactory shapeFactory)
        {
            _yourService = yourService;
            _notifier = notifier;
            _logger = logger;
            _shapeFactory = shapeFactory;
        }

        // Public action
        public async Task<IActionResult> Index(int page = 1, int pageSize = 10)
        {
            try
            {
                var data = await _yourService.GetDataAsync(page, pageSize);
                
                var viewModel = new YourIndexViewModel
                {
                    Items = data,
                    Pager = await _shapeFactory.PagerAsync(new PagerParameters
                    {
                        Page = page,
                        PageSize = pageSize,
                        // TotalItemCount = totalCount // You need to implement count method
                    })
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading index page");
                await _notifier.ErrorAsync(H["An error occurred while loading the page."]);
                return View(new YourIndexViewModel());
            }
        }

        // Public action with parameter
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            try
            {
                var item = await _yourService.GetByIdAsync(id);
                if (item == null)
                {
                    return NotFound();
                }

                var viewModel = new YourDetailsViewModel
                {
                    Item = item
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading details for id: {Id}", id);
                await _notifier.ErrorAsync(H["An error occurred while loading the details."]);
                return RedirectToAction(nameof(Index));
            }
        }

        // API endpoint
        [HttpGet]
        public async Task<IActionResult> Api(string action, string id = null)
        {
            try
            {
                return action.ToLowerInvariant() switch
                {
                    "list" => Json(await _yourService.GetDataAsync()),
                    "get" when !string.IsNullOrEmpty(id) => Json(await _yourService.GetByIdAsync(id)),
                    "process" when !string.IsNullOrEmpty(id) => Json(await _yourService.ProcessDataAsync(id)),
                    _ => BadRequest("Invalid action")
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API error for action: {Action}, id: {Id}", action, id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }
    }
}

// Controllers/AdminController.cs - Admin interface
[Admin("your-module/{action}/{id?}", "YourModule.{action}")]
public class AdminController : Controller
{
    private readonly IYourService _yourService;
    private readonly IAuthorizationService _authorizationService;
    private readonly INotifier _notifier;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        IYourService yourService,
        IAuthorizationService authorizationService,
        INotifier notifier,
        ILogger<AdminController> logger)
    {
        _yourService = yourService;
        _authorizationService = authorizationService;
        _notifier = notifier;
        _logger = logger;
    }

    // Admin dashboard
    public async Task<IActionResult> Index()
    {
        if (!await _authorizationService.AuthorizeAsync(User, YourPermissions.ManageYourModule))
        {
            return Forbid();
        }

        try
        {
            var data = await _yourService.GetDataAsync(1, 50);
            
            var viewModel = new YourAdminIndexViewModel
            {
                Items = data
            };

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading admin index");
            await _notifier.ErrorAsync(H["An error occurred while loading the admin page."]);
            return View(new YourAdminIndexViewModel());
        }
    }

    // Create new item
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        if (!await _authorizationService.AuthorizeAsync(User, YourPermissions.ManageYourModule))
        {
            return Forbid();
        }

        var viewModel = new YourCreateViewModel();
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(YourCreateViewModel viewModel)
    {
        if (!await _authorizationService.AuthorizeAsync(User, YourPermissions.ManageYourModule))
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            return View(viewModel);
        }

        try
        {
            var model = new YourModel
            {
                Title = viewModel.Title,
                Description = viewModel.Description,
                IsEnabled = viewModel.IsEnabled
            };

            var success = await _yourService.CreateAsync(model);
            
            if (success)
            {
                await _notifier.SuccessAsync(H["Item created successfully."]);
                return RedirectToAction(nameof(Index));
            }
            else
            {
                await _notifier.ErrorAsync(H["Failed to create item."]);
                return View(viewModel);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating item");
            await _notifier.ErrorAsync(H["An error occurred while creating the item."]);
            return View(viewModel);
        }
    }

    // Edit existing item
    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        if (!await _authorizationService.AuthorizeAsync(User, YourPermissions.ManageYourModule))
        {
            return Forbid();
        }

        if (string.IsNullOrEmpty(id))
        {
            return NotFound();
        }

        try
        {
            var item = await _yourService.GetByIdAsync(id);
            if (item == null)
            {
                return NotFound();
            }

            var viewModel = new YourEditViewModel
            {
                Id = item.Id,
                Title = item.Title,
                Description = item.Description,
                IsEnabled = item.IsEnabled
            };

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading edit form for id: {Id}", id);
            await _notifier.ErrorAsync(H["An error occurred while loading the edit form."]);
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(YourEditViewModel viewModel)
    {
        if (!await _authorizationService.AuthorizeAsync(User, YourPermissions.ManageYourModule))
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            return View(viewModel);
        }

        try
        {
            var model = new YourModel
            {
                Id = viewModel.Id,
                Title = viewModel.Title,
                Description = viewModel.Description,
                IsEnabled = viewModel.IsEnabled
            };

            var success = await _yourService.UpdateAsync(model);
            
            if (success)
            {
                await _notifier.SuccessAsync(H["Item updated successfully."]);
                return RedirectToAction(nameof(Index));
            }
            else
            {
                await _notifier.ErrorAsync(H["Failed to update item."]);
                return View(viewModel);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating item with id: {Id}", viewModel.Id);
            await _notifier.ErrorAsync(H["An error occurred while updating the item."]);
            return View(viewModel);
        }
    }

    // Delete item
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        if (!await _authorizationService.AuthorizeAsync(User, YourPermissions.ManageYourModule))
        {
            return Forbid();
        }

        if (string.IsNullOrEmpty(id))
        {
            return NotFound();
        }

        try
        {
            var success = await _yourService.DeleteAsync(id);
            
            if (success)
            {
                await _notifier.SuccessAsync(H["Item deleted successfully."]);
            }
            else
            {
                await _notifier.ErrorAsync(H["Failed to delete item."]);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting item with id: {Id}", id);
            await _notifier.ErrorAsync(H["An error occurred while deleting the item."]);
        }

        return RedirectToAction(nameof(Index));
    }
}
```

### 5. ViewModels

```csharp
// ViewModels/YourContentPartViewModel.cs
using OrchardCore.ContentManagement;
using System.ComponentModel.DataAnnotations;

namespace YourModule.ViewModels
{
    public class YourContentPartViewModel
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsEnabled { get; set; }
        public DateTime? PublishDate { get; set; }
        public int Priority { get; set; }
        public List<string> Tags { get; set; } = new();
        public ContentItem ContentItem { get; set; }
        public object AdditionalData { get; set; }
    }

    public class YourContentPartEditViewModel
    {
        [Required(ErrorMessage = "Title is required")]
        [StringLength(255, ErrorMessage = "Title cannot exceed 255 characters")]
        public string Title { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        public string Description { get; set; } = string.Empty;

        public bool IsEnabled { get; set; } = true;

        [DataType(DataType.DateTime)]
        public DateTime? PublishDate { get; set; }

        [Range(0, 100, ErrorMessage = "Priority must be between 0 and 100")]
        public int Priority { get; set; } = 0;

        public string Tags { get; set; } = string.Empty;

        public YourCustomSettings Settings { get; set; } = new();

        // Settings from content type definition
        public YourContentPartSettings PartSettings { get; set; } = new();
    }

    // Admin ViewModels
    public class YourIndexViewModel
    {
        public IEnumerable<YourModel> Items { get; set; } = Enumerable.Empty<YourModel>();
        public dynamic Pager { get; set; }
    }

    public class YourDetailsViewModel
    {
        public YourModel Item { get; set; }
    }

    public class YourAdminIndexViewModel
    {
        public IEnumerable<YourModel> Items { get; set; } = Enumerable.Empty<YourModel>();
    }

    public class YourCreateViewModel
    {
        [Required(ErrorMessage = "Title is required")]
        [StringLength(255, ErrorMessage = "Title cannot exceed 255 characters")]
        public string Title { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        public string Description { get; set; } = string.Empty;

        public bool IsEnabled { get; set; } = true;
    }

    public class YourEditViewModel : YourCreateViewModel
    {
        [Required]
        public string Id { get; set; } = string.Empty;
    }
}
```

## Cách Tránh Xung Đột

### 1. Naming Conventions

#### A. Namespace Strategy
```csharp
// ✅ ĐÚNG: Sử dụng namespace riêng biệt và có cấu trúc
namespace YourCompany.OrchardCore.YourModule.Models
{
    public class YourContentPart : ContentPart
    {
        // Implementation
    }
}

namespace YourCompany.OrchardCore.YourModule.Services
{
    public interface IYourService
    {
        // Interface definition
    }
}

// ❌ SAI: Namespace chung có thể gây xung đột
namespace OrchardCore.Models
{
    public class ContentPart // Có thể xung đột với core
    {
    }
}

namespace Models // Quá chung chung
{
    public class YourModel
    {
    }
}
```

#### B. Feature IDs và Dependencies
```csharp
// ✅ ĐÚNG: Feature ID duy nhất và rõ ràng
[assembly: Module(
    Name = "Your Company - Your Module",
    Author = "Your Company",
    Version = "1.0.0"
)]

[assembly: Feature(
    Id = "YourCompany.YourModule",
    Name = "Your Module",
    Description = "Main functionality of your module",
    Category = "Content Management"
)]

[assembly: Feature(
    Id = "YourCompany.YourModule.AdvancedFeature",
    Name = "Your Module - Advanced Feature",
    Description = "Advanced functionality",
    Dependencies = new[] { "YourCompany.YourModule" }
)]

// ❌ SAI: Feature ID có thể trùng lặp
[assembly: Feature(
    Id = "AdvancedFeature", // Có thể trùng với module khác
    Dependencies = new[] { "SomeModule" } // Dependency không rõ ràng
)]
```

#### C. Route Naming
```csharp
// ✅ ĐÚNG: Route name có prefix rõ ràng
routes.MapAreaControllerRoute(
    name: "YourCompany.YourModule.Home.Index",
    areaName: "YourCompany.YourModule",
    pattern: "your-company/your-module/{action=Index}/{id?}",
    defaults: new { controller = "Home" }
);

routes.MapAreaControllerRoute(
    name: "YourCompany.YourModule.Admin.Dashboard",
    areaName: "YourCompany.YourModule",
    pattern: "admin/your-company/your-module/{action=Index}/{id?}",
    defaults: new { controller = "Admin" }
);

// ❌ SAI: Route name chung chung
routes.MapAreaControllerRoute(
    name: "Home.Index", // Có thể trùng với route khác
    pattern: "home/{action=Index}"
);
```

#### D. Permission Names
```csharp
// ✅ ĐÚNG: Permission names có prefix
public class YourPermissions : IPermissionProvider
{
    public static readonly Permission ManageYourCompanyYourModule = 
        new("ManageYourCompanyYourModule", "Manage Your Company Your Module");
    
    public static readonly Permission ViewYourCompanyYourModule = 
        new("ViewYourCompanyYourModule", "View Your Company Your Module");
    
    public static readonly Permission EditYourCompanyYourModuleSettings = 
        new("EditYourCompanyYourModuleSettings", "Edit Your Company Your Module Settings");
}

// ❌ SAI: Permission names chung chung
public static readonly Permission Manage = new("Manage", "Manage"); // Quá chung
public static readonly Permission View = new("View", "View"); // Có thể trùng
```

### 2. Service Registration Strategy

```csharp
// ✅ ĐÚNG: Đăng ký services với interface rõ ràng
public override void ConfigureServices(IServiceCollection services)
{
    // Sử dụng interface để tránh xung đột
    services.AddScoped<IYourCompanyYourModuleService, YourCompanyYourModuleService>();
    services.AddSingleton<IYourCompanyYourModuleCacheService, YourCompanyYourModuleCacheService>();
    
    // Đăng ký với key nếu cần
    services.AddKeyedScoped<IYourGenericService, YourSpecificService>("YourCompany.YourModule");
    
    // Factory pattern cho complex scenarios
    services.AddScoped<Func<string, IYourCompanyYourModuleProcessor>>(serviceProvider => key =>
    {
        return key switch
        {
            "type1" => serviceProvider.GetService<YourCompanyYourModuleProcessor1>(),
            "type2" => serviceProvider.GetService<YourCompanyYourModuleProcessor2>(),
            _ => throw new ArgumentException($"Unknown processor type: {key}")
        };
    });
}

// ❌ SAI: Đăng ký trực tiếp class có thể gây xung đột
services.AddScoped<YourService>(); // Không có interface
services.AddScoped<IService, YourService>(); // Interface name quá chung
```

### 3. Database và Index Strategy

```csharp
// ✅ ĐÚNG: Table và index names có prefix
public class YourCompanyYourModuleMigrations : DataMigration
{
    public int Create()
    {
        SchemaBuilder.CreateMapIndexTable<YourCompanyYourModuleIndex>(table => table
            .Column<string>("ContentItemId", column => column.WithLength(26))
            .Column<string>("Title", column => column.WithLength(255))
            .Column<bool>("IsEnabled")
        );

        SchemaBuilder.AlterIndexTable<YourCompanyYourModuleIndex>(table => table
            .CreateIndex("IDX_YourCompany_YourModule_Title", "Title")
            .CreateIndex("IDX_YourCompany_YourModule_IsEnabled", "IsEnabled")
        );

        return 1;
    }
}

public class YourCompanyYourModuleIndex : MapIndex
{
    public string ContentItemId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
}

// ❌ SAI: Table names chung chung
public class ContentIndex : MapIndex // Có thể trùng với core hoặc module khác
{
}
```

### 4. Configuration Strategy

```csharp
// ✅ ĐÚNG: Configuration section có prefix
// appsettings.json
{
  "YourCompany": {
    "YourModule": {
      "ApiKey": "your-api-key",
      "MaxRetries": 3,
      "EnableLogging": true
    }
  }
}

// Configuration class
public class YourCompanyYourModuleSettings
{
    public const string SectionName = "YourCompany:YourModule";
    
    public string ApiKey { get; set; } = string.Empty;
    public int MaxRetries { get; set; } = 3;
    public bool EnableLogging { get; set; } = true;
}

// Registration
services.Configure<YourCompanyYourModuleSettings>(
    configuration.GetSection(YourCompanyYourModuleSettings.SectionName));

// ❌ SAI: Configuration section chung chung
{
  "Settings": { // Quá chung chung
    "ApiKey": "key"
  }
}
```

## Best Practices

### 1. Dependency Injection Best Practices

```csharp
// ✅ ĐÚNG: Proper service lifetimes
public override void ConfigureServices(IServiceCollection services)
{
    // Scoped: Per request (default cho web apps)
    services.AddScoped<IYourBusinessService, YourBusinessService>();
    services.AddScoped<IYourDataService, YourDataService>();
    
    // Singleton: Application lifetime (cho caching, configuration)
    services.AddSingleton<IYourCacheService, YourCacheService>();
    services.AddSingleton<IYourConfigurationService, YourConfigurationService>();
    
    // Transient: Per injection (cho lightweight services)
    services.AddTransient<IYourHelperService, YourHelperService>();
    services.AddTransient<IYourValidatorService, YourValidatorService>();
    
    // Factory pattern cho complex object creation
    services.AddScoped<IYourServiceFactory>(serviceProvider => 
        new YourServiceFactory(serviceProvider));
    
    // Conditional registration
    if (configuration.GetValue<bool>("YourModule:EnableAdvancedFeatures"))
    {
        services.AddScoped<IYourAdvancedService, YourAdvancedService>();
    }
    else
    {
        services.AddScoped<IYourAdvancedService, YourBasicService>();
    }
}

// Service Factory Pattern
public interface IYourServiceFactory
{
    IYourProcessor CreateProcessor(string type);
    IYourValidator CreateValidator(string validationType);
}

public class YourServiceFactory : IYourServiceFactory
{
    private readonly IServiceProvider _serviceProvider;

    public YourServiceFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IYourProcessor CreateProcessor(string type)
    {
        return type.ToLowerInvariant() switch
        {
            "json" => _serviceProvider.GetRequiredService<JsonProcessor>(),
            "xml" => _serviceProvider.GetRequiredService<XmlProcessor>(),
            "csv" => _serviceProvider.GetRequiredService<CsvProcessor>(),
            _ => throw new ArgumentException($"Unknown processor type: {type}")
        };
    }

    public IYourValidator CreateValidator(string validationType)
    {
        return validationType.ToLowerInvariant() switch
        {
            "email" => _serviceProvider.GetRequiredService<EmailValidator>(),
            "phone" => _serviceProvider.GetRequiredService<PhoneValidator>(),
            "url" => _serviceProvider.GetRequiredService<UrlValidator>(),
            _ => _serviceProvider.GetRequiredService<DefaultValidator>()
        };
    }
}
```

### 2. Configuration Management

```csharp
// Models/YourModuleSettings.cs
public class YourModuleSettings
{
    public const string SectionName = "YourCompany:YourModule";

    [Required]
    public string ApiKey { get; set; } = string.Empty;

    [Range(1, 10)]
    public int MaxRetries { get; set; } = 3;

    [Range(1000, 60000)]
    public int TimeoutMs { get; set; } = 30000;

    public bool EnableLogging { get; set; } = true;

    public bool EnableCaching { get; set; } = true;

    [Range(1, 3600)]
    public int CacheExpirationSeconds { get; set; } = 300;

    public List<string> AllowedDomains { get; set; } = new();

    public Dictionary<string, string> CustomHeaders { get; set; } = new();

    // Validation method
    public bool IsValid(out List<string> errors)
    {
        errors = new List<string>();

        if (string.IsNullOrWhiteSpace(ApiKey))
            errors.Add("ApiKey is required");

        if (MaxRetries < 1 || MaxRetries > 10)
            errors.Add("MaxRetries must be between 1 and 10");

        if (TimeoutMs < 1000 || TimeoutMs > 60000)
            errors.Add("TimeoutMs must be between 1000 and 60000");

        return errors.Count == 0;
    }
}

// Services/YourConfigurationService.cs
public interface IYourConfigurationService
{
    YourModuleSettings GetSettings();
    Task<bool> ValidateSettingsAsync();
    Task UpdateSettingsAsync(YourModuleSettings settings);
}

public class YourConfigurationService : IYourConfigurationService
{
    private readonly IOptionsMonitor<YourModuleSettings> _optionsMonitor;
    private readonly ILogger<YourConfigurationService> _logger;

    public YourConfigurationService(
        IOptionsMonitor<YourModuleSettings> optionsMonitor,
        ILogger<YourConfigurationService> logger)
    {
        _optionsMonitor = optionsMonitor;
        _logger = logger;
    }

    public YourModuleSettings GetSettings()
    {
        return _optionsMonitor.CurrentValue;
    }

    public async Task<bool> ValidateSettingsAsync()
    {
        var settings = GetSettings();
        var isValid = settings.IsValid(out var errors);

        if (!isValid)
        {
            _logger.LogWarning("Invalid settings detected: {Errors}", string.Join(", ", errors));
        }

        return isValid;
    }

    public async Task UpdateSettingsAsync(YourModuleSettings settings)
    {
        // Implementation for updating settings
        // This might involve updating configuration files or database
        await Task.CompletedTask;
    }
}

// Startup.cs registration
public override void ConfigureServices(IServiceCollection services)
{
    // Register configuration
    services.Configure<YourModuleSettings>(
        configuration.GetSection(YourModuleSettings.SectionName));

    // Add validation
    services.AddOptions<YourModuleSettings>()
        .Bind(configuration.GetSection(YourModuleSettings.SectionName))
        .ValidateDataAnnotations()
        .Validate(settings => settings.IsValid(out _), "Invalid YourModule settings");

    // Register configuration service
    services.AddSingleton<IYourConfigurationService, YourConfigurationService>();
}
```

### 3. Error Handling và Logging

```csharp
// Services/YourService.cs with comprehensive error handling
public class YourService : IYourService
{
    private readonly ILogger<YourService> _logger;
    private readonly INotifier _notifier;
    private readonly IStringLocalizer S;
    private readonly YourModuleSettings _settings;

    public YourService(
        ILogger<YourService> logger,
        INotifier notifier,
        IStringLocalizer<YourService> stringLocalizer,
        IOptions<YourModuleSettings> settings)
    {
        _logger = logger;
        _notifier = notifier;
        S = stringLocalizer;
        _settings = settings.Value;
    }

    public async Task<ServiceResult<string>> ProcessDataAsync(string input)
    {
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["Operation"] = nameof(ProcessDataAsync),
            ["InputLength"] = input?.Length ?? 0
        });

        try
        {
            _logger.LogInformation("Starting data processing");

            // Input validation
            if (string.IsNullOrWhiteSpace(input))
            {
                var error = "Input cannot be null or empty";
                _logger.LogWarning(error);
                return ServiceResult<string>.Failure(error);
            }

            if (input.Length > 1000)
            {
                var error = "Input exceeds maximum length of 1000 characters";
                _logger.LogWarning(error);
                await _notifier.WarningAsync(S["Input is too long and will be truncated"]);
                input = input[..1000];
            }

            // Business logic
            var result = await ProcessInternalAsync(input);

            _logger.LogInformation("Data processing completed successfully");
            return ServiceResult<string>.Success(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument provided");
            await _notifier.ErrorAsync(S["Invalid data provided"]);
            return ServiceResult<string>.Failure(ex.Message);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error during processing");
            await _notifier.ErrorAsync(S["Network error occurred. Please try again later."]);
            return ServiceResult<string>.Failure("Network error");
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, "Operation timed out");
            await _notifier.ErrorAsync(S["Operation timed out. Please try again."]);
            return ServiceResult<string>.Failure("Timeout");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during data processing");
            await _notifier.ErrorAsync(S["An unexpected error occurred"]);
            return ServiceResult<string>.Failure("Unexpected error");
        }
    }

    private async Task<string> ProcessInternalAsync(string input)
    {
        // Simulate processing with timeout
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(_settings.TimeoutMs));
        
        try
        {
            await Task.Delay(100, cts.Token); // Simulate work
            return $"Processed: {input} at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC";
        }
        catch (OperationCanceledException) when (cts.Token.IsCancellationRequested)
        {
            throw new TimeoutException($"Processing timed out after {_settings.TimeoutMs}ms");
        }
    }
}

// Result wrapper class
public class ServiceResult<T>
{
    public bool IsSuccess { get; private set; }
    public T Data { get; private set; }
    public string ErrorMessage { get; private set; } = string.Empty;
    public List<string> Errors { get; private set; } = new();

    private ServiceResult() { }

    public static ServiceResult<T> Success(T data)
    {
        return new ServiceResult<T>
        {
            IsSuccess = true,
            Data = data
        };
    }

    public static ServiceResult<T> Failure(string errorMessage)
    {
        return new ServiceResult<T>
        {
            IsSuccess = false,
            ErrorMessage = errorMessage,
            Errors = new List<string> { errorMessage }
        };
    }

    public static ServiceResult<T> Failure(List<string> errors)
    {
        return new ServiceResult<T>
        {
            IsSuccess = false,
            ErrorMessage = string.Join(", ", errors),
            Errors = errors
        };
    }
}
```

### 4. Caching Strategy

```csharp
// Services/YourCacheService.cs
public interface IYourCacheService
{
    Task<T?> GetAsync<T>(string key) where T : class;
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class;
    Task RemoveAsync(string key);
    Task RemoveByPatternAsync(string pattern);
    Task ClearAllAsync();
}

public class YourCacheService : IYourCacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly IDistributedCache _distributedCache;
    private readonly ILogger<YourCacheService> _logger;
    private readonly YourModuleSettings _settings;
    private readonly string _keyPrefix;

    public YourCacheService(
        IMemoryCache memoryCache,
        IDistributedCache distributedCache,
        ILogger<YourCacheService> logger,
        IOptions<YourModuleSettings> settings)
    {
        _memoryCache = memoryCache;
        _distributedCache = distributedCache;
        _logger = logger;
        _settings = settings.Value;
        _keyPrefix = "YourCompany.YourModule:";
    }

    public async Task<T?> GetAsync<T>(string key) where T : class
    {
        if (!_settings.EnableCaching)
            return null;

        var fullKey = _keyPrefix + key;

        try
        {
            // Try memory cache first
            if (_memoryCache.TryGetValue(fullKey, out T cachedValue))
            {
                _logger.LogDebug("Cache hit (memory): {Key}", key);
                return cachedValue;
            }

            // Try distributed cache
            var distributedValue = await _distributedCache.GetStringAsync(fullKey);
            if (!string.IsNullOrEmpty(distributedValue))
            {
                var deserializedValue = JsonSerializer.Deserialize<T>(distributedValue);
                
                // Store in memory cache for faster access
                _memoryCache.Set(fullKey, deserializedValue, TimeSpan.FromMinutes(5));
                
                _logger.LogDebug("Cache hit (distributed): {Key}", key);
                return deserializedValue;
            }

            _logger.LogDebug("Cache miss: {Key}", key);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error retrieving from cache: {Key}", key);
            return null;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class
    {
        if (!_settings.EnableCaching || value == null)
            return;

        var fullKey = _keyPrefix + key;
        var exp = expiration ?? TimeSpan.FromSeconds(_settings.CacheExpirationSeconds);

        try
        {
            // Set in memory cache
            _memoryCache.Set(fullKey, value, exp);

            // Set in distributed cache
            var serializedValue = JsonSerializer.Serialize(value);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = exp
            };
            
            await _distributedCache.SetStringAsync(fullKey, serializedValue, options);

            _logger.LogDebug("Cache set: {Key}, Expiration: {Expiration}", key, exp);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error setting cache: {Key}", key);
        }
    }

    public async Task RemoveAsync(string key)
    {
        var fullKey = _keyPrefix + key;

        try
        {
            _memoryCache.Remove(fullKey);
            await _distributedCache.RemoveAsync(fullKey);
            
            _logger.LogDebug("Cache removed: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error removing from cache: {Key}", key);
        }
    }

    public async Task RemoveByPatternAsync(string pattern)
    {
        // Implementation depends on your distributed cache provider
        // This is a simplified version
        _logger.LogInformation("Removing cache entries by pattern: {Pattern}", pattern);
        await Task.CompletedTask;
    }

    public async Task ClearAllAsync()
    {
        try
        {
            // Clear memory cache (if you have access to all keys)
            if (_memoryCache is MemoryCache mc)
            {
                mc.Compact(1.0);
            }

            // For distributed cache, you might need to track keys
            _logger.LogInformation("Cache cleared");
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error clearing cache");
        }
    }
}

// Usage in service
public class YourBusinessService : IYourBusinessService
{
    private readonly IYourCacheService _cacheService;
    private readonly IYourDataService _dataService;

    public async Task<YourModel?> GetByIdAsync(string id)
    {
        // Try cache first
        var cacheKey = $"model:{id}";
        var cachedModel = await _cacheService.GetAsync<YourModel>(cacheKey);
        
        if (cachedModel != null)
            return cachedModel;

        // Load from data source
        var model = await _dataService.GetByIdAsync(id);
        
        if (model != null)
        {
            // Cache for future requests
            await _cacheService.SetAsync(cacheKey, model, TimeSpan.FromMinutes(30));
        }

        return model;
    }
}
```

### 5. Background Tasks

```csharp
// Services/YourBackgroundTask.cs
using OrchardCore.BackgroundTasks;

[BackgroundTask(Schedule = "*/5 * * * *", Description = "Your background task description")]
public class YourBackgroundTask : IBackgroundTask
{
    private readonly IYourService _yourService;
    private readonly ILogger<YourBackgroundTask> _logger;
    private readonly YourModuleSettings _settings;

    public YourBackgroundTask(
        IYourService yourService,
        ILogger<YourBackgroundTask> logger,
        IOptions<YourModuleSettings> settings)
    {
        _yourService = yourService;
        _logger = logger;
        _settings = settings.Value;
    }

    public async Task DoWorkAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        if (!_settings.EnableBackgroundTasks)
        {
            _logger.LogDebug("Background tasks are disabled");
            return;
        }

        try
        {
            _logger.LogInformation("Starting background task execution");

            // Your background work here
            await ProcessPendingItemsAsync(cancellationToken);
            await CleanupExpiredDataAsync(cancellationToken);

            _logger.LogInformation("Background task completed successfully");
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Background task was cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in background task execution");
        }
    }

    private async Task ProcessPendingItemsAsync(CancellationToken cancellationToken)
    {
        // Process pending items
        var pendingItems = await _yourService.GetPendingItemsAsync();
        
        foreach (var item in pendingItems)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            try
            {
                await _yourService.ProcessItemAsync(item);
                _logger.LogDebug("Processed item: {ItemId}", item.Id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to process item: {ItemId}", item.Id);
            }
        }
    }

    private async Task CleanupExpiredDataAsync(CancellationToken cancellationToken)
    {
        // Cleanup expired data
        var expiredItems = await _yourService.GetExpiredItemsAsync();
        
        foreach (var item in expiredItems)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            try
            {
                await _yourService.DeleteItemAsync(item.Id);
                _logger.LogDebug("Cleaned up expired item: {ItemId}", item.Id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to cleanup item: {ItemId}", item.Id);
            }
        }
    }
}
```

### 6. Permissions và Security

```csharp
// Permissions.cs
using OrchardCore.Security.Permissions;

namespace YourModule
{
    public class YourPermissions : IPermissionProvider
    {
        // Define permissions with descriptive names
        public static readonly Permission ManageYourModule = 
            new("ManageYourCompanyYourModule", "Manage Your Company Your Module", new[] { ViewYourModule });
        
        public static readonly Permission ViewYourModule = 
            new("ViewYourCompanyYourModule", "View Your Company Your Module");
        
        public static readonly Permission EditYourModuleSettings = 
            new("EditYourCompanyYourModuleSettings", "Edit Your Company Your Module Settings", new[] { ViewYourModule });
        
        public static readonly Permission DeleteYourModuleItems = 
            new("DeleteYourCompanyYourModuleItems", "Delete Your Company Your Module Items", new[] { ManageYourModule });

        private readonly IEnumerable<Permission> _allPermissions = new[]
        {
            ManageYourModule,
            ViewYourModule,
            EditYourModuleSettings,
            DeleteYourModuleItems,
        };

        public Task<IEnumerable<Permission>> GetPermissionsAsync()
            => Task.FromResult(_allPermissions);

        public IEnumerable<PermissionStereotype> GetDefaultStereotypes() => new[]
        {
            new PermissionStereotype
            {
                Name = OrchardCoreConstants.Roles.Administrator,
                Permissions = _allPermissions,
            },
            new PermissionStereotype
            {
                Name = OrchardCoreConstants.Roles.Editor,
                Permissions = new[] { ViewYourModule, ManageYourModule },
            },
            new PermissionStereotype
            {
                Name = OrchardCoreConstants.Roles.Moderator,
                Permissions = new[] { ViewYourModule },
            },
            new PermissionStereotype
            {
                Name = OrchardCoreConstants.Roles.Contributor,
                Permissions = new[] { ViewYourModule },
            },
        };
    }
}

// Usage in controllers
[Admin("your-module/{action}/{id?}", "YourModule.{action}")]
public class AdminController : Controller
{
    private readonly IAuthorizationService _authorizationService;

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        if (!await _authorizationService.AuthorizeAsync(User, YourPermissions.ViewYourModule))
        {
            return Forbid();
        }

        // Controller logic
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Create(YourCreateViewModel model)
    {
        if (!await _authorizationService.AuthorizeAsync(User, YourPermissions.ManageYourModule))
        {
            return Forbid();
        }

        // Create logic
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(string id)
    {
        if (!await _authorizationService.AuthorizeAsync(User, YourPermissions.DeleteYourModuleItems))
        {
            return Forbid();
        }

        // Delete logic
        return RedirectToAction(nameof(Index));
    }
}

// Usage in services
public class YourService : IYourService
{
    private readonly IAuthorizationService _authorizationService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public async Task<bool> CanUserManageAsync()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user == null) return false;

        var result = await _authorizationService.AuthorizeAsync(user, YourPermissions.ManageYourModule);
        return result.Succeeded;
    }

    public async Task<ServiceResult<YourModel>> CreateAsync(YourModel model)
    {
        if (!await CanUserManageAsync())
        {
            return ServiceResult<YourModel>.Failure("Insufficient permissions");
        }

        // Create logic
        return ServiceResult<YourModel>.Success(model);
    }
}
```

## Testing và Debugging

### 1. Unit Testing Setup

```csharp
// Tests/YourServiceTests.cs
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using YourModule.Services;
using YourModule.Models;

namespace YourModule.Tests.Services
{
    public class YourServiceTests
    {
        private readonly Mock<ILogger<YourService>> _mockLogger;
        private readonly Mock<IOptions<YourModuleSettings>> _mockOptions;
        private readonly YourModuleSettings _settings;
        private readonly YourService _service;

        public YourServiceTests()
        {
            _mockLogger = new Mock<ILogger<YourService>>();
            _mockOptions = new Mock<IOptions<YourModuleSettings>>();
            _settings = new YourModuleSettings
            {
                ApiKey = "test-key",
                MaxRetries = 3,
                TimeoutMs = 5000,
                EnableLogging = true
            };
            
            _mockOptions.Setup(x => x.Value).Returns(_settings);
            _service = new YourService(_mockLogger.Object, _mockOptions.Object);
        }

        [Fact]
        public async Task ProcessDataAsync_ValidInput_ReturnsProcessedData()
        {
            // Arrange
            var input = "test data";

            // Act
            var result = await _service.ProcessDataAsync(input);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Contains("Processed: test data", result.Data);
            
            // Verify logging
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Starting data processing")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task ProcessDataAsync_NullInput_ReturnsFailure()
        {
            // Arrange
            string input = null;

            // Act
            var result = await _service.ProcessDataAsync(input);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal("Input cannot be null or empty", result.ErrorMessage);
        }

        [Fact]
        public async Task ProcessDataAsync_EmptyInput_ReturnsFailure()
        {
            // Arrange
            var input = string.Empty;

            // Act
            var result = await _service.ProcessDataAsync(input);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal("Input cannot be null or empty", result.ErrorMessage);
        }

        [Theory]
        [InlineData("short")]
        [InlineData("medium length input")]
        [InlineData("this is a much longer input string that should still work fine")]
        public async Task ProcessDataAsync_VariousInputLengths_ReturnsSuccess(string input)
        {
            // Act
            var result = await _service.ProcessDataAsync(input);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Contains($"Processed: {input}", result.Data);
        }

        [Fact]
        public async Task ProcessDataAsync_VeryLongInput_TruncatesInput()
        {
            // Arrange
            var input = new string('a', 1500); // Longer than 1000 chars

            // Act
            var result = await _service.ProcessDataAsync(input);

            // Assert
            Assert.True(result.IsSuccess);
            // Should be truncated to 1000 chars
            Assert.Contains($"Processed: {input[..1000]}", result.Data);
        }
    }
}

// Tests/YourContentPartDisplayDriverTests.cs
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Moq;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Display.Models;
using OrchardCore.ContentManagement.Metadata;
using OrchardCore.DisplayManagement.Views;
using Xunit;
using YourModule.Drivers;
using YourModule.Models;
using YourModule.Services;
using YourModule.ViewModels;

namespace YourModule.Tests.Drivers
{
    public class YourContentPartDisplayDriverTests
    {
        private readonly Mock<IContentDefinitionManager> _mockContentDefinitionManager;
        private readonly Mock<IYourService> _mockYourService;
        private readonly Mock<IStringLocalizer<YourContentPartDisplayDriver>> _mockLocalizer;
        private readonly Mock<ILogger<YourContentPartDisplayDriver>> _mockLogger;
        private readonly YourContentPartDisplayDriver _driver;

        public YourContentPartDisplayDriverTests()
        {
            _mockContentDefinitionManager = new Mock<IContentDefinitionManager>();
            _mockYourService = new Mock<IYourService>();
            _mockLocalizer = new Mock<IStringLocalizer<YourContentPartDisplayDriver>>();
            _mockLogger = new Mock<ILogger<YourContentPartDisplayDriver>>();

            _driver = new YourContentPartDisplayDriver(
                _mockContentDefinitionManager.Object,
                _mockYourService.Object,
                _mockLocalizer.Object,
                _mockLogger.Object);
        }

        [Fact]
        public void Display_ValidPart_ReturnsDisplayResult()
        {
            // Arrange
            var contentItem = new ContentItem { ContentItemId = "test-id", ContentType = "TestType" };
            var part = new YourContentPart
            {
                ContentItem = contentItem,
                Title = "Test Title",
                Description = "Test Description",
                IsEnabled = true
            };

            var context = new BuildPartDisplayContext(part, "", "", null, null);

            // Act
            var result = _driver.Display(part, context);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<ShapeResult>(result);
        }

        [Fact]
        public void Edit_ValidPart_ReturnsEditResult()
        {
            // Arrange
            var contentItem = new ContentItem { ContentItemId = "test-id", ContentType = "TestType" };
            var part = new YourContentPart
            {
                ContentItem = contentItem,
                Title = "Test Title",
                Description = "Test Description",
                IsEnabled = true
            };

            var context = new BuildPartEditorContext(part, "", false, "", null, null);

            // Act
            var result = _driver.Edit(part, context);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<ShapeResult>(result);
        }
    }
}
```

### 2. Integration Testing

```csharp
// Tests/YourModuleIntegrationTests.cs
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OrchardCore;
using System.Net.Http;
using Xunit;
using YourModule.Services;

namespace YourModule.Tests.Integration
{
    public class YourModuleIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public YourModuleIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Override services for testing
                    services.AddScoped<IYourService, MockYourService>();
                });
                
                builder.UseEnvironment("Testing");
            });
            
            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task Module_ShouldLoadCorrectly()
        {
            // Arrange & Act
            using var scope = _factory.Services.CreateScope();
            var yourService = scope.ServiceProvider.GetService<IYourService>();

            // Assert
            Assert.NotNull(yourService);
        }

        [Fact]
        public async Task HomePage_ShouldReturnSuccessStatusCode()
        {
            // Act
            var response = await _client.GetAsync("/your-module");

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Equal("text/html; charset=utf-8", response.Content.Headers.ContentType?.ToString());
        }

        [Fact]
        public async Task ApiEndpoint_ShouldReturnJsonResponse()
        {
            // Act
            var response = await _client.GetAsync("/api/your-module/list");

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Equal("application/json; charset=utf-8", response.Content.Headers.ContentType?.ToString());
        }
    }

    // Mock service for testing
    public class MockYourService : IYourService
    {
        public Task<ServiceResult<string>> ProcessDataAsync(string input)
        {
            return Task.FromResult(ServiceResult<string>.Success($"Mock processed: {input}"));
        }

        public Task<IEnumerable<YourModel>> GetDataAsync(int page = 1, int pageSize = 20)
        {
            var mockData = new[]
            {
                new YourModel { Id = "1", Title = "Mock Item 1", IsEnabled = true },
                new YourModel { Id = "2", Title = "Mock Item 2", IsEnabled = false }
            };
            
            return Task.FromResult<IEnumerable<YourModel>>(mockData);
        }

        // Implement other interface methods...
    }
}
```

### 3. Performance Testing

```csharp
// Tests/YourServicePerformanceTests.cs
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using YourModule.Services;
using YourModule.Models;

namespace YourModule.Tests.Performance
{
    [MemoryDiagnoser]
    [SimpleJob(BenchmarkDotNet.Jobs.RuntimeMoniker.Net80)]
    public class YourServicePerformanceTests
    {
        private YourService _service;
        private string _testData;

        [GlobalSetup]
        public void Setup()
        {
            var settings = new YourModuleSettings
            {
                ApiKey = "test-key",
                MaxRetries = 3,
                TimeoutMs = 5000
            };
            
            var options = Options.Create(settings);
            _service = new YourService(NullLogger<YourService>.Instance, options);
            _testData = "Test data for performance testing";
        }

        [Benchmark]
        public async Task ProcessDataAsync_Benchmark()
        {
            await _service.ProcessDataAsync(_testData);
        }

        [Benchmark]
        [Arguments(10)]
        [Arguments(100)]
        [Arguments(1000)]
        public async Task ProcessMultipleItems_Benchmark(int itemCount)
        {
            var tasks = new List<Task>();
            
            for (int i = 0; i < itemCount; i++)
            {
                tasks.Add(_service.ProcessDataAsync($"{_testData} {i}"));
            }
            
            await Task.WhenAll(tasks);
        }
    }

    // Run performance tests
    public class PerformanceTestRunner
    {
        public static void Main(string[] args)
        {
            BenchmarkRunner.Run<YourServicePerformanceTests>();
        }
    }
}
```

## Deployment

### 1. NuGet Package Configuration

```xml
<!-- YourModule.csproj - Complete package configuration -->
<Project Sdk="Microsoft.NET.Sdk.Razor">

  <PropertyGroup>
    <AddRazorSupportForMvc>true</AddRazorSupportForMvc>
    <TargetFramework>net8.0</TargetFramework>
    
    <!-- Package Information -->
    <PackageId>YourCompany.OrchardCore.YourModule</PackageId>
    <Title>Your Company - Your Module</Title>
    <Description>Comprehensive module for OrchardCore providing advanced functionality for content management and business operations.</Description>
    <PackageVersion>1.0.0</PackageVersion>
    <AssemblyVersion>1.0.0.0</AssemblyVersion>
    <FileVersion>1.0.0.0</FileVersion>
    
    <!-- Authors and Company -->
    <Authors>Your Name, Your Team</Authors>
    <Company>Your Company</Company>
    <Product>Your Module</Product>
    <Copyright>Copyright © Your Company 2024</Copyright>
    
    <!-- Package Metadata -->
    <PackageTags>OrchardCore;Module;CMS;Content;Management;Business</PackageTags>
    <PackageProjectUrl>https://github.com/yourcompany/yourmodule</PackageProjectUrl>
    <RepositoryUrl>https://github.com/yourcompany/yourmodule</RepositoryUrl>
    <RepositoryType>git</RepositoryType>
    <PackageLicenseExpression>MIT</PackageLicenseExpression>
    <PackageRequireLicenseAcceptance>false</PackageRequireLicenseAcceptance>
    <PackageIcon>icon.png</PackageIcon>
    <PackageReadmeFile>README.md</PackageReadmeFile>
    <PackageReleaseNotes>
      Version 1.0.0:
      - Initial release
      - Content part functionality
      - Admin interface
      - API endpoints
      - Background tasks
    </PackageReleaseNotes>
    
    <!-- Build Configuration -->
    <GeneratePackageOnBuild>false</GeneratePackageOnBuild>
    <IncludeSymbols>true</IncludeSymbols>
    <SymbolPackageFormat>snupkg</SymbolPackageFormat>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <NoWarn>$(NoWarn);1591</NoWarn> <!-- Disable XML documentation warnings -->
    
    <!-- Source Link -->
    <PublishRepositoryUrl>true</PublishRepositoryUrl>
    <EmbedUntrackedSources>true</EmbedUntrackedSources>
    <IncludeSourceRevisionInInformationalVersion>false</IncludeSourceRevisionInInformationalVersion>
  </PropertyGroup>

  <!-- Package Files -->
  <ItemGroup>
    <None Include="icon.png" Pack="true" PackagePath="\" />
    <None Include="README.md" Pack="true" PackagePath="\" />
    <None Include="LICENSE" Pack="true" PackagePath="\" />
  </ItemGroup>

  <!-- Source Link -->
  <ItemGroup>
    <PackageReference Include="Microsoft.SourceLink.GitHub" Version="1.1.1" PrivateAssets="All"/>
  </ItemGroup>

</Project>
```

### 2. CI/CD Pipeline (GitHub Actions)

```yaml
# .github/workflows/ci-cd.yml
name: CI/CD Pipeline

on:
  push:
    branches: [ main, develop ]
    tags: [ 'v*' ]
  pull_request:
    branches: [ main ]

env:
  DOTNET_VERSION: '8.0.x'
  PROJECT_PATH: 'src/YourModule/YourModule.csproj'
  TEST_PROJECT_PATH: 'tests/YourModule.Tests/YourModule.Tests.csproj'

jobs:
  test:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v4
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: ${{ env.DOTNET_VERSION }}
    
    - name: Restore dependencies
      run: dotnet restore
    
    - name: Build
      run: dotnet build --no-restore --configuration Release
    
    - name: Test
      run: dotnet test --no-build --configuration Release --verbosity normal --collect:"XPlat Code Coverage"
    
    - name: Upload coverage reports
      uses: codecov/codecov-action@v3
      with:
        file: ./coverage.cobertura.xml
        flags: unittests
        name: codecov-umbrella

  build-and-pack:
    needs: test
    runs-on: ubuntu-latest
    if: github.event_name == 'push' && (github.ref == 'refs/heads/main' || startsWith(github.ref, 'refs/tags/v'))
    
    steps:
    - uses: actions/checkout@v4
      with:
        fetch-depth: 0 # Required for GitVersion
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: ${{ env.DOTNET_VERSION }}
    
    - name: Install GitVersion
      uses: gittools/actions/gitversion/setup@v0.9.15
      with:
        versionSpec: '5.x'
    
    - name: Determine Version
      uses: gittools/actions/gitversion/execute@v0.9.15
      id: gitversion
    
    - name: Restore dependencies
      run: dotnet restore
    
    - name: Build
      run: dotnet build --no-restore --configuration Release -p:Version=${{ steps.gitversion.outputs.nuGetVersionV2 }}
    
    - name: Pack
      run: dotnet pack ${{ env.PROJECT_PATH }} --no-build --configuration Release -p:PackageVersion=${{ steps.gitversion.outputs.nuGetVersionV2 }} --output ./artifacts
    
    - name: Upload artifacts
      uses: actions/upload-artifact@v3
      with:
        name: nuget-packages
        path: ./artifacts/*.nupkg

  publish:
    needs: build-and-pack
    runs-on: ubuntu-latest
    if: startsWith(github.ref, 'refs/tags/v')
    
    steps:
    - name: Download artifacts
      uses: actions/download-artifact@v3
      with:
        name: nuget-packages
        path: ./artifacts
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: ${{ env.DOTNET_VERSION }}
    
    - name: Publish to NuGet
      run: dotnet nuget push ./artifacts/*.nupkg --api-key ${{ secrets.NUGET_API_KEY }} --source https://api.nuget.org/v3/index.json --skip-duplicate
```

### 3. Docker Support

```dockerfile
# Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY ["src/YourModule/YourModule.csproj", "src/YourModule/"]
COPY ["tests/YourModule.Tests/YourModule.Tests.csproj", "tests/YourModule.Tests/"]

# Restore dependencies
RUN dotnet restore "src/YourModule/YourModule.csproj"

# Copy source code
COPY . .

# Build and test
WORKDIR "/src/src/YourModule"
RUN dotnet build "YourModule.csproj" -c Release -o /app/build

# Run tests
WORKDIR "/src/tests/YourModule.Tests"
RUN dotnet test "YourModule.Tests.csproj" -c Release --no-build

# Publish
WORKDIR "/src/src/YourModule"
RUN dotnet publish "YourModule.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "YourModule.dll"]
```

```yaml
# docker-compose.yml
version: '3.8'

services:
  yourmodule:
    build:
      context: .
      dockerfile: Dockerfile
    ports:
      - "8080:80"
      - "8443:443"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__DefaultConnection=Server=db;Database=OrchardCore;User=sa;Password=YourPassword123!;TrustServerCertificate=true
      - YourCompany__YourModule__ApiKey=your-production-api-key
    depends_on:
      - db
    volumes:
      - ./App_Data:/app/App_Data
      - ./wwwroot/Media:/app/wwwroot/Media

  db:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      - ACCEPT_EULA=Y
      - SA_PASSWORD=YourPassword123!
    ports:
      - "1433:1433"
    volumes:
      - sqlserver_data:/var/opt/mssql

volumes:
  sqlserver_data:
```

## Checklist

### ✅ Pre-Development Checklist

- [ ] **Planning và Design**
  - [ ] Định nghĩa rõ ràng chức năng module
  - [ ] Thiết kế database schema (nếu cần)
  - [ ] Xác định dependencies
  - [ ] Lập kế hoạch testing strategy

- [ ] **Environment Setup**
  - [ ] Cài đặt OrchardCore development environment
  - [ ] Setup IDE với OrchardCore templates
  - [ ] Cấu hình debugging tools
  - [ ] Setup version control (Git)

### ✅ Development Checklist

- [ ] **Core Files**
  - [ ] Tạo `Manifest.cs` với thông tin đầy đủ
  - [ ] Tạo `Startup.cs` với service registration
  - [ ] Cấu hình `.csproj` file đúng chuẩn
  - [ ] Tạo folder structure theo convention

- [ ] **Naming Conventions**
  - [ ] Sử dụng namespace riêng biệt (YourCompany.YourModule)
  - [ ] Feature IDs là duy nhất
  - [ ] Route names có prefix rõ ràng
  - [ ] Permission names có prefix
  - [ ] Database table/index names có prefix

- [ ] **Content Management**
  - [ ] Content Parts được định nghĩa đúng
  - [ ] Display Drivers hoạt động chính xác
  - [ ] Content Handlers xử lý events đúng
  - [ ] ViewModels có validation đầy đủ
  - [ ] Views/Templates được tạo đúng

- [ ] **Services và Business Logic**
  - [ ] Interfaces được định nghĩa rõ ràng
  - [ ] Dependency injection được cấu hình đúng
  - [ ] Error handling và logging đầy đủ
  - [ ] Caching strategy được implement (nếu cần)
  - [ ] Background tasks hoạt động đúng (nếu có)

- [ ] **Security**
  - [ ] Permissions được định nghĩa đầy đủ
  - [ ] Authorization checks trong controllers
  - [ ] Input validation và sanitization
  - [ ] CSRF protection
  - [ ] SQL injection prevention

- [ ] **Database**
  - [ ] Migrations được viết đúng
  - [ ] Index strategy được tối ưu
  - [ ] Foreign key constraints đúng
  - [ ] Data seeding (nếu cần)

### ✅ Quality Assurance Checklist

- [ ] **Code Quality**
  - [ ] Code follows C# coding standards
  - [ ] No hard-coded values
  - [ ] Proper exception handling
  - [ ] Logging levels appropriate
  - [ ] Comments và documentation đầy đủ

- [ ] **Testing**
  - [ ] Unit tests cho services
  - [ ] Integration tests cho controllers
  - [ ] Display driver tests
  - [ ] Migration tests
  - [ ] Performance tests (nếu cần)

- [ ] **Performance**
  - [ ] Database queries được tối ưu
  - [ ] Caching được implement đúng
  - [ ] Memory leaks được kiểm tra
  - [ ] Large data handling

- [ ] **Compatibility**
  - [ ] Tương thích với OrchardCore version target
  - [ ] Không xung đột với core modules
  - [ ] Không xung đột với popular modules
  - [ ] Multi-tenant support (nếu cần)

### ✅ Pre-Release Checklist

- [ ] **Documentation**
  - [ ] README.md đầy đủ
  - [ ] API documentation
  - [ ] Installation guide
  - [ ] Configuration guide
  - [ ] Troubleshooting guide

- [ ] **Package Configuration**
  - [ ] NuGet package metadata đầy đủ
  - [ ] Version numbering đúng
  - [ ] Dependencies được khai báo đúng
  - [ ] License file
  - [ ] Icon và assets

- [ ] **CI/CD**
  - [ ] Build pipeline hoạt động
  - [ ] Test pipeline pass
  - [ ] Package generation thành công
  - [ ] Deployment scripts

### ✅ Release Checklist

- [ ] **Final Testing**
  - [ ] Fresh installation test
  - [ ] Upgrade test từ version cũ
  - [ ] Multi-tenant test
  - [ ] Performance test với production data
  - [ ] Security scan

- [ ] **Release Preparation**
  - [ ] Release notes được viết
  - [ ] Version tags được tạo
  - [ ] Backup strategies documented
  - [ ] Rollback procedures documented

- [ ] **Publication**
  - [ ] NuGet package published
  - [ ] GitHub release created
  - [ ] Documentation website updated
  - [ ] Community announcement

### ✅ Post-Release Checklist

- [ ] **Monitoring**
  - [ ] Error tracking setup
  - [ ] Performance monitoring
  - [ ] Usage analytics
  - [ ] User feedback collection

- [ ] **Support**
  - [ ] Issue tracking setup
  - [ ] Support documentation
  - [ ] Community forum monitoring
  - [ ] Update roadmap

---

## Kết Luận

Việc tạo modules OrchardCore đúng chuẩn đòi hỏi:

1. **Hiểu rõ architecture**: OrchardCore sử dụng modular architecture với dependency injection
2. **Tuân thủ conventions**: Naming, folder structure, và patterns
3. **Tránh xung đột**: Sử dụng namespaces riêng, unique identifiers
4. **Quality assurance**: Testing, documentation, và performance optimization
5. **Security first**: Proper authorization, input validation, và secure coding practices

Bằng cách tuân thủ hướng dẫn này, bạn sẽ tạo ra được modules OrchardCore chất lượng cao, dễ bảo trì, và tương thích tốt với hệ sinh thái OrchardCore.

**Lưu ý quan trọng**: Luôn test module trên môi trường development trước khi deploy lên production, và maintain backward compatibility khi release updates.