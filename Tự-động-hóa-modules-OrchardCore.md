# Tự Động Hóa Modules OrchardCore - Hướng Dẫn Toàn Diện

## Mục Lục
1. [Tổng Quan Về Tự Động Hóa](#tổng-quan-về-tự-động-hóa)
2. [Recipes System - Hệ Thống Công Thức](#recipes-system)
3. [Feature Event Handlers](#feature-event-handlers)
4. [Setup Event Handlers](#setup-event-handlers)
5. [Data Migrations](#data-migrations)
6. [Auto Setup Module](#auto-setup-module)
7. [Deferred Tasks](#deferred-tasks)
8. [Ví Dụ Thực Tế](#ví-dụ-thực-tế)
9. [Best Practices](#best-practices)

---

## Tổng Quan Về Tự Động Hóa

OrchardCore cung cấp nhiều cơ chế để modules có thể tự động cấu hình và hoạt động ngay sau khi được cài đặt:

### Các Cơ Chế Chính:
- **Recipes**: Tự động cấu hình content types, settings, data
- **Feature Event Handlers**: Xử lý khi enable/disable features
- **Setup Event Handlers**: Xử lý trong quá trình setup site
- **Data Migrations**: Tự động cập nhật database và cấu hình
- **Deferred Tasks**: Thực thi các tác vụ sau khi request hoàn thành

---

## Recipes System

### 1. Cấu Trúc Recipe File

```json
{
  "name": "MyModuleSetup",
  "displayName": "My Module Auto Setup",
  "description": "Automatically configures My Module",
  "author": "Your Name",
  "website": "https://yoursite.com",
  "version": "1.0.0",
  "issetuprecipe": false,
  "categories": ["module"],
  "tags": ["automation", "setup"],
  
  "variables": {
    "contentItemId": "[js:uuid()]",
    "currentDate": "[js: new Date().toISOString()]"
  },
  
  "steps": [
    {
      "name": "feature",
      "enable": [
        "OrchardCore.Contents",
        "OrchardCore.ContentTypes",
        "MyModule"
      ]
    },
    {
      "name": "ContentDefinition",
      "ContentTypes": [
        {
          "Name": "MyContentType",
          "DisplayName": "My Content Type",
          "Settings": {
            "ContentTypeSettings": {
              "Creatable": true,
              "Draftable": true,
              "Versionable": true,
              "Listable": true
            }
          },
          "ContentTypePartDefinitionRecords": [
            {
              "PartName": "TitlePart",
              "Name": "TitlePart",
              "Settings": {
                "ContentTypePartSettings": {
                  "Position": "0"
                }
              }
            }
          ]
        }
      ]
    },
    {
      "name": "settings",
      "MyModuleSettings": {
        "ApiKey": "default-api-key",
        "EnableFeature": true,
        "MaxItems": 100
      }
    },
    {
      "name": "content",
      "Data": [
        {
          "ContentItemId": "[js: variables('contentItemId')]",
          "ContentType": "MyContentType",
          "DisplayText": "Welcome Content",
          "Latest": true,
          "Published": true,
          "TitlePart": {
            "Title": "Welcome to My Module"
          }
        }
      ]
    }
  ]
}
```

### 2. Recipe Executor

```csharp
// Services/MyModuleRecipeExecutor.cs
using OrchardCore.Recipes.Models;
using OrchardCore.Recipes.Services;
using System.Text.Json.Nodes;

namespace MyModule.Services
{
    public class MyModuleRecipeExecutor : NamedRecipeStepHandler
    {
        private readonly IMyModuleService _myModuleService;
        
        public MyModuleRecipeExecutor(IMyModuleService myModuleService) 
            : base("MyModuleStep")
        {
            _myModuleService = myModuleService;
        }
        
        protected override async Task HandleAsync(RecipeExecutionContext context)
        {
            var step = context.Step.ToObject<MyModuleStepModel>();
            
            // Thực hiện cấu hình tự động
            await _myModuleService.ConfigureAsync(step.Settings);
            
            // Tạo dữ liệu mặc định
            if (step.CreateDefaultData)
            {
                await _myModuleService.CreateDefaultDataAsync();
            }
        }
        
        private class MyModuleStepModel
        {
            public Dictionary<string, object> Settings { get; set; }
            public bool CreateDefaultData { get; set; }
        }
    }
}
```

### 3. Đăng Ký Recipe Executor

```csharp
// Startup.cs
public override void ConfigureServices(IServiceCollection services)
{
    services.AddScoped<IRecipeStepHandler, MyModuleRecipeExecutor>();
}
```

---

## Feature Event Handlers

### 1. Implementing IFeatureEventHandler

```csharp
// Services/MyModuleFeatureEventHandler.cs
using OrchardCore.Environment.Extensions.Features;
using OrchardCore.Environment.Shell;
using OrchardCore.Environment.Shell.Scope;
using Microsoft.Extensions.DependencyInjection;

namespace MyModule.Services
{
    public class MyModuleFeatureEventHandler : FeatureEventHandler
    {
        public override Task EnabledAsync(IFeatureInfo feature)
        {
            // Chỉ xử lý khi feature của module này được enable
            if (feature.Id != "MyModule")
                return Task.CompletedTask;
                
            // Sử dụng deferred task để thực hiện sau khi request hoàn thành
            ShellScope.AddDeferredTask(async scope =>
            {
                var myService = scope.ServiceProvider.GetRequiredService<IMyModuleService>();
                await myService.InitializeAsync();
                
                // Tạo dữ liệu mặc định
                await myService.CreateDefaultDataAsync();
                
                // Cấu hình settings
                await myService.ConfigureDefaultSettingsAsync();
            });
            
            return Task.CompletedTask;
        }
        
        public override Task DisabledAsync(IFeatureInfo feature)
        {
            if (feature.Id != "MyModule")
                return Task.CompletedTask;
                
            ShellScope.AddDeferredTask(async scope =>
            {
                var myService = scope.ServiceProvider.GetRequiredService<IMyModuleService>();
                await myService.CleanupAsync();
            });
            
            return Task.CompletedTask;
        }
        
        public override Task InstalledAsync(IFeatureInfo feature)
        {
            if (feature.Id != "MyModule")
                return Task.CompletedTask;
                
            ShellScope.AddDeferredTask(async scope =>
            {
                // Thực hiện migration hoặc setup ban đầu
                var migrationService = scope.ServiceProvider.GetRequiredService<IDataMigrationManager>();
                await migrationService.UpdateFeatureAsync("MyModule");
            });
            
            return Task.CompletedTask;
        }
    }
}
```

### 2. Đăng Ký Feature Event Handler

```csharp
// Startup.cs
public override void ConfigureServices(IServiceCollection services)
{
    services.AddScoped<IFeatureEventHandler, MyModuleFeatureEventHandler>();
}
```

---

## Setup Event Handlers

### 1. Implementing ISetupEventHandler

```csharp
// Services/MyModuleSetupEventHandler.cs
using OrchardCore.Abstractions.Setup;
using OrchardCore.Setup.Events;

namespace MyModule.Services
{
    public class MyModuleSetupEventHandler : ISetupEventHandler
    {
        private readonly IMyModuleService _myModuleService;
        private readonly ILogger<MyModuleSetupEventHandler> _logger;
        
        public MyModuleSetupEventHandler(
            IMyModuleService myModuleService,
            ILogger<MyModuleSetupEventHandler> logger)
        {
            _myModuleService = myModuleService;
            _logger = logger;
        }
        
        public async Task SetupAsync(SetupContext context)
        {
            // Chỉ chạy nếu module được enable trong setup
            if (!context.Recipe?.Name.Contains("MyModule") == true)
                return;
                
            try
            {
                _logger.LogInformation("Setting up MyModule...");
                
                // Tạo admin user cho module nếu cần
                if (context.Properties.ContainsKey("MyModuleAdminUser"))
                {
                    await _myModuleService.CreateAdminUserAsync(
                        context.Properties["MyModuleAdminUser"].ToString());
                }
                
                // Cấu hình settings từ setup context
                var settings = new MyModuleSettings
                {
                    ApiKey = context.Properties.GetValueOrDefault("MyModuleApiKey")?.ToString(),
                    EnableFeature = bool.Parse(context.Properties.GetValueOrDefault("MyModuleEnabled", "true").ToString())
                };
                
                await _myModuleService.SaveSettingsAsync(settings);
                
                _logger.LogInformation("MyModule setup completed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting up MyModule");
                context.Errors["MyModule"] = ex.Message;
            }
        }
    }
}
```

### 2. Đăng Ký Setup Event Handler

```csharp
// Startup.cs
public override void ConfigureServices(IServiceCollection services)
{
    services.AddScoped<ISetupEventHandler, MyModuleSetupEventHandler>();
}
```

---

## Data Migrations

### 1. Basic Migration

```csharp
// Migrations.cs
using OrchardCore.ContentManagement.Metadata;
using OrchardCore.ContentManagement.Metadata.Settings;
using OrchardCore.Data.Migration;
using OrchardCore.Environment.Shell.Scope;
using Microsoft.Extensions.DependencyInjection;

namespace MyModule
{
    public class Migrations : DataMigration
    {
        private readonly IContentDefinitionManager _contentDefinitionManager;
        
        public Migrations(IContentDefinitionManager contentDefinitionManager)
        {
            _contentDefinitionManager = contentDefinitionManager;
        }
        
        public async Task<int> CreateAsync()
        {
            // Tạo database tables
            await SchemaBuilder.CreateMapIndexTableAsync<MyModuleIndex>(table => table
                .Column<string>("Name", column => column.WithLength(255))
                .Column<DateTime>("CreatedUtc")
                .Column<bool>("IsActive")
            );
            
            // Tạo indexes
            await SchemaBuilder.AlterIndexTableAsync<MyModuleIndex>(table => table
                .CreateIndex("IDX_MyModule_Name", "Name")
                .CreateIndex("IDX_MyModule_CreatedUtc", "CreatedUtc")
            );
            
            // Tạo content types
            await _contentDefinitionManager.AlterTypeDefinitionAsync("MyContentType", type => type
                .DisplayedAs("My Content Type")
                .Creatable()
                .Draftable()
                .Versionable()
                .Listable()
                .WithPart("TitlePart")
                .WithPart("MyCustomPart")
            );
            
            // Sử dụng deferred task để thực hiện sau khi migration hoàn thành
            ShellScope.AddDeferredTask(async scope =>
            {
                var myService = scope.ServiceProvider.GetRequiredService<IMyModuleService>();
                
                // Tạo dữ liệu mặc định
                await myService.CreateDefaultDataAsync();
                
                // Cấu hình settings
                await myService.ConfigureDefaultSettingsAsync();
                
                // Import recipe nếu cần
                var recipeExecutor = scope.ServiceProvider.GetRequiredService<IRecipeExecutor>();
                await recipeExecutor.ExecuteAsync("mymodule-setup", new Dictionary<string, object>());
            });
            
            return 1;
        }
        
        public async Task<int> UpdateFrom1Async()
        {
            // Migration từ version 1 lên 2
            await SchemaBuilder.AlterIndexTableAsync<MyModuleIndex>(table => table
                .AddColumn<string>("Description", column => column.WithLength(1000))
            );
            
            return 2;
        }
        
        public async Task<int> UpdateFrom2Async()
        {
            // Migration từ version 2 lên 3
            ShellScope.AddDeferredTask(async scope =>
            {
                var myService = scope.ServiceProvider.GetRequiredService<IMyModuleService>();
                await myService.MigrateDataAsync();
            });
            
            return 3;
        }
    }
}
```

### 2. Advanced Migration với Recipe

```csharp
// Migrations.cs (Advanced)
using OrchardCore.Recipes;
using OrchardCore.Recipes.Services;

public class Migrations : DataMigration
{
    private readonly IRecipeMigrator _recipeMigrator;
    
    public Migrations(IRecipeMigrator recipeMigrator)
    {
        _recipeMigrator = recipeMigrator;
    }
    
    public async Task<int> CreateAsync()
    {
        // Thực thi recipe tự động
        await _recipeMigrator.ExecuteAsync($"mymodule-setup{RecipesConstants.RecipeExtension}", this);
        
        return 1;
    }
}
```

---

## Auto Setup Module

### 1. Auto Setup Configuration

```json
// appsettings.json
{
  "OrchardCore": {
    "OrchardCore_AutoSetup": {
      "AutoSetupPath": "/MyModule/AutoSetup",
      "Tenants": [
        {
          "ShellName": "Default",
          "SiteName": "My Site with MyModule",
          "RecipeName": "MyModuleSetup",
          "DatabaseProvider": "Sqlite",
          "DatabaseConnectionString": "Data Source=App_Data/Sites/Default/mysite.db;Cache=Shared",
          "AdminUsername": "admin",
          "AdminEmail": "admin@mysite.com",
          "AdminPassword": "Password123!",
          "FeatureProfile": "MyModule"
        }
      ]
    }
  }
}
```

### 2. Custom Auto Setup Service

```csharp
// Services/MyModuleAutoSetupService.cs
using OrchardCore.AutoSetup.Options;
using OrchardCore.AutoSetup.Services;

namespace MyModule.Services
{
    public class MyModuleAutoSetupService : IAutoSetupService
    {
        private readonly IAutoSetupService _baseAutoSetupService;
        private readonly IMyModuleService _myModuleService;
        
        public MyModuleAutoSetupService(
            IAutoSetupService baseAutoSetupService,
            IMyModuleService myModuleService)
        {
            _baseAutoSetupService = baseAutoSetupService;
            _myModuleService = myModuleService;
        }
        
        public async Task<(SetupContext, bool)> SetupTenantAsync(
            TenantSetupOptions setupOptions, 
            ShellSettings shellSettings)
        {
            // Thực hiện setup cơ bản
            var (context, success) = await _baseAutoSetupService.SetupTenantAsync(setupOptions, shellSettings);
            
            if (success)
            {
                // Thực hiện setup bổ sung cho module
                await _myModuleService.PostSetupAsync(context);
            }
            
            return (context, success);
        }
        
        public Task<ShellSettings> CreateTenantSettingsAsync(TenantSetupOptions setupOptions)
        {
            return _baseAutoSetupService.CreateTenantSettingsAsync(setupOptions);
        }
        
        public Task<SetupContext> GetSetupContextAsync(TenantSetupOptions options, ShellSettings shellSettings)
        {
            return _baseAutoSetupService.GetSetupContextAsync(options, shellSettings);
        }
    }
}
```

---

## Deferred Tasks

### 1. Sử dụng Deferred Tasks

```csharp
// Services/MyModuleService.cs
using OrchardCore.Environment.Shell.Scope;

namespace MyModule.Services
{
    public class MyModuleService : IMyModuleService
    {
        public async Task ProcessDataAsync()
        {
            // Thực hiện xử lý ngay lập tức
            await DoImmediateProcessingAsync();
            
            // Thêm task để thực hiện sau khi request hoàn thành
            ShellScope.AddDeferredTask(async scope =>
            {
                var heavyService = scope.ServiceProvider.GetRequiredService<IHeavyProcessingService>();
                await heavyService.ProcessAsync();
                
                var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
                await notificationService.SendNotificationAsync("Processing completed");
            });
        }
        
        public async Task InitializeModuleAsync()
        {
            ShellScope.AddDeferredTask(async scope =>
            {
                // Tạo indexes
                var indexingService = scope.ServiceProvider.GetRequiredService<IIndexingService>();
                await indexingService.RebuildIndexAsync("MyModuleIndex");
                
                // Cấu hình cache
                var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();
                await cacheService.WarmupCacheAsync();
                
                // Gửi email thông báo
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
                await emailService.SendWelcomeEmailAsync();
            });
        }
    }
}
```

### 2. Conditional Deferred Tasks

```csharp
public async Task ConditionalSetupAsync()
{
    ShellScope.AddDeferredTask(async scope =>
    {
        var featuresManager = scope.ServiceProvider.GetRequiredService<IShellFeaturesManager>();
        
        // Chỉ thực hiện nếu feature khác được enable
        if (await featuresManager.IsFeatureEnabledAsync("OrchardCore.Media"))
        {
            var mediaService = scope.ServiceProvider.GetRequiredService<IMediaService>();
            await mediaService.CreateDefaultFoldersAsync();
        }
        
        // Kiểm tra settings trước khi thực hiện
        var siteService = scope.ServiceProvider.GetRequiredService<ISiteService>();
        var site = await siteService.LoadSiteSettingsAsync();
        var settings = site.As<MyModuleSettings>();
        
        if (settings.AutoCreateContent)
        {
            var contentManager = scope.ServiceProvider.GetRequiredService<IContentManager>();
            await CreateDefaultContentAsync(contentManager);
        }
    });
}
```

---

## Ví Dụ Thực Tế

### 1. Module Blog Tự Động

```csharp
// BlogModule/Startup.cs
public class Startup : StartupBase
{
    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<IBlogService, BlogService>();
        services.AddScoped<IFeatureEventHandler, BlogFeatureEventHandler>();
        services.AddScoped<ISetupEventHandler, BlogSetupEventHandler>();
        services.AddScoped<IRecipeStepHandler, BlogRecipeExecutor>();
    }
}

// BlogModule/Services/BlogFeatureEventHandler.cs
public class BlogFeatureEventHandler : FeatureEventHandler
{
    public override Task EnabledAsync(IFeatureInfo feature)
    {
        if (feature.Id != "BlogModule")
            return Task.CompletedTask;
            
        ShellScope.AddDeferredTask(async scope =>
        {
            var blogService = scope.ServiceProvider.GetRequiredService<IBlogService>();
            
            // Tạo blog categories mặc định
            await blogService.CreateDefaultCategoriesAsync();
            
            // Tạo blog post mẫu
            await blogService.CreateSamplePostsAsync();
            
            // Cấu hình menu
            await blogService.SetupNavigationAsync();
            
            // Cấu hình widgets
            await blogService.SetupWidgetsAsync();
        });
        
        return Task.CompletedTask;
    }
}

// BlogModule/Services/BlogService.cs
public class BlogService : IBlogService
{
    private readonly IContentManager _contentManager;
    private readonly IContentDefinitionManager _contentDefinitionManager;
    
    public async Task CreateDefaultCategoriesAsync()
    {
        var categories = new[] { "Technology", "Lifestyle", "Business" };
        
        foreach (var categoryName in categories)
        {
            var category = await _contentManager.NewAsync("BlogCategory");
            category.Alter<TitlePart>(part => part.Title = categoryName);
            category.Alter<AutoroutePart>(part => part.Path = $"/category/{categoryName.ToLower()}");
            
            await _contentManager.CreateAsync(category);
            await _contentManager.PublishAsync(category);
        }
    }
    
    public async Task CreateSamplePostsAsync()
    {
        var samplePost = await _contentManager.NewAsync("BlogPost");
        samplePost.Alter<TitlePart>(part => part.Title = "Welcome to Your New Blog");
        samplePost.Alter<HtmlBodyPart>(part => part.Html = "<p>This is your first blog post. You can edit or delete it.</p>");
        samplePost.Alter<AutoroutePart>(part => part.Path = "/welcome-to-your-new-blog");
        
        await _contentManager.CreateAsync(samplePost);
        await _contentManager.PublishAsync(samplePost);
    }
}
```

### 2. Recipe File cho Blog Module

```json
// BlogModule/Recipes/blog-setup.recipe.json
{
  "name": "BlogSetup",
  "displayName": "Blog Module Setup",
  "description": "Automatically sets up a complete blog system",
  "author": "Blog Module Team",
  "version": "1.0.0",
  "issetuprecipe": false,
  "categories": ["blog"],
  "tags": ["blog", "content", "automation"],
  
  "variables": {
    "blogHomeId": "[js:uuid()]",
    "categoryListId": "[js:uuid()]"
  },
  
  "steps": [
    {
      "name": "feature",
      "enable": [
        "OrchardCore.Contents",
        "OrchardCore.ContentTypes",
        "OrchardCore.Autoroute",
        "OrchardCore.Html",
        "OrchardCore.Title",
        "OrchardCore.Taxonomies",
        "OrchardCore.Lists",
        "BlogModule"
      ]
    },
    {
      "name": "ContentDefinition",
      "ContentTypes": [
        {
          "Name": "BlogPost",
          "DisplayName": "Blog Post",
          "Settings": {
            "ContentTypeSettings": {
              "Creatable": true,
              "Draftable": true,
              "Versionable": true,
              "Listable": true,
              "Securable": true
            }
          },
          "ContentTypePartDefinitionRecords": [
            {
              "PartName": "TitlePart",
              "Name": "TitlePart",
              "Settings": {
                "ContentTypePartSettings": {
                  "Position": "0"
                }
              }
            },
            {
              "PartName": "AutoroutePart",
              "Name": "AutoroutePart",
              "Settings": {
                "ContentTypePartSettings": {
                  "Position": "1"
                },
                "AutoroutePartSettings": {
                  "Pattern": "/blog/{{ Model.ContentItem | display_text | slugify }}",
                  "AllowCustomPath": true
                }
              }
            },
            {
              "PartName": "HtmlBodyPart",
              "Name": "HtmlBodyPart",
              "Settings": {
                "ContentTypePartSettings": {
                  "Position": "2"
                }
              }
            },
            {
              "PartName": "TaxonomyPart",
              "Name": "Categories",
              "Settings": {
                "ContentTypePartSettings": {
                  "DisplayName": "Categories",
                  "Position": "3"
                },
                "TaxonomyPartSettings": {
                  "TaxonomyContentItemId": "[js: variables('categoryListId')]"
                }
              }
            }
          ]
        },
        {
          "Name": "BlogCategory",
          "DisplayName": "Blog Category",
          "Settings": {
            "ContentTypeSettings": {
              "Creatable": true,
              "Listable": true,
              "Stereotype": "Taxonomy"
            }
          },
          "ContentTypePartDefinitionRecords": [
            {
              "PartName": "TitlePart",
              "Name": "TitlePart",
              "Settings": {
                "ContentTypePartSettings": {
                  "Position": "0"
                }
              }
            },
            {
              "PartName": "AutoroutePart",
              "Name": "AutoroutePart",
              "Settings": {
                "ContentTypePartSettings": {
                  "Position": "1"
                },
                "AutoroutePartSettings": {
                  "Pattern": "/category/{{ Model.ContentItem | display_text | slugify }}"
                }
              }
            },
            {
              "PartName": "TaxonomyPart",
              "Name": "TaxonomyPart",
              "Settings": {
                "ContentTypePartSettings": {
                  "Position": "2"
                }
              }
            }
          ]
        }
      ]
    },
    {
      "name": "content",
      "Data": [
        {
          "ContentItemId": "[js: variables('categoryListId')]",
          "ContentType": "Taxonomy",
          "DisplayText": "Blog Categories",
          "Latest": true,
          "Published": true,
          "TitlePart": {
            "Title": "Blog Categories"
          },
          "AutoroutePart": {
            "Path": "/blog-categories"
          },
          "TaxonomyPart": {
            "TermContentType": "BlogCategory"
          }
        },
        {
          "ContentItemId": "[js: variables('blogHomeId')]",
          "ContentType": "BlogPost",
          "DisplayText": "Welcome to Your Blog",
          "Latest": true,
          "Published": true,
          "TitlePart": {
            "Title": "Welcome to Your Blog"
          },
          "AutoroutePart": {
            "Path": "/blog/welcome"
          },
          "HtmlBodyPart": {
            "Html": "<h2>Welcome to your new blog!</h2><p>This is your first blog post. You can edit or delete this post and start creating your own content.</p><p>Your blog is now ready to use with the following features:</p><ul><li>Create and manage blog posts</li><li>Organize posts with categories</li><li>SEO-friendly URLs</li><li>Rich text editing</li></ul>"
          }
        }
      ]
    },
    {
      "name": "settings",
      "BlogSettings": {
        "PostsPerPage": 10,
        "AllowComments": true,
        "RequireApproval": false,
        "ShowAuthor": true,
        "ShowDate": true,
        "ShowCategories": true
      }
    }
  ]
}
```

---

## Best Practices

### 1. Nguyên Tắc Thiết Kế

```csharp
// ✅ ĐÚNG: Sử dụng deferred tasks cho heavy operations
public async Task EnableFeatureAsync()
{
    // Thực hiện ngay lập tức những gì cần thiết
    await ValidateConfigurationAsync();
    
    // Defer heavy operations
    ShellScope.AddDeferredTask(async scope =>
    {
        await CreateIndexesAsync();
        await WarmupCacheAsync();
        await SendNotificationsAsync();
    });
}

// ❌ SAI: Thực hiện heavy operations trong main thread
public async Task EnableFeatureAsync()
{
    await ValidateConfigurationAsync();
    await CreateIndexesAsync(); // Có thể làm chậm request
    await WarmupCacheAsync(); // Có thể làm chậm request
}
```

### 2. Error Handling

```csharp
public class SafeFeatureEventHandler : FeatureEventHandler
{
    private readonly ILogger<SafeFeatureEventHandler> _logger;
    
    public override Task EnabledAsync(IFeatureInfo feature)
    {
        if (feature.Id != "MyModule")
            return Task.CompletedTask;
            
        ShellScope.AddDeferredTask(async scope =>
        {
            try
            {
                var myService = scope.ServiceProvider.GetRequiredService<IMyModuleService>();
                await myService.InitializeAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize MyModule after enabling feature");
                // Không throw exception để không làm crash application
            }
        });
        
        return Task.CompletedTask;
    }
}
```

### 3. Conditional Setup

```csharp
public class ConditionalSetupHandler : FeatureEventHandler
{
    public override Task EnabledAsync(IFeatureInfo feature)
    {
        if (feature.Id != "MyModule")
            return Task.CompletedTask;
            
        ShellScope.AddDeferredTask(async scope =>
        {
            var featuresManager = scope.ServiceProvider.GetRequiredService<IShellFeaturesManager>();
            var siteService = scope.ServiceProvider.GetRequiredService<ISiteService>();
            
            // Chỉ setup nếu các dependencies có sẵn
            var hasMedia = await featuresManager.IsFeatureEnabledAsync("OrchardCore.Media");
            var hasContentTypes = await featuresManager.IsFeatureEnabledAsync("OrchardCore.ContentTypes");
            
            if (hasMedia && hasContentTypes)
            {
                var myService = scope.ServiceProvider.GetRequiredService<IMyModuleService>();
                await myService.FullSetupAsync();
            }
            else
            {
                await myService.MinimalSetupAsync();
            }
        });
        
        return Task.CompletedTask;
    }
}
```

### 4. Idempotent Operations

```csharp
public class IdempotentMigration : DataMigration
{
    public async Task<int> CreateAsync()
    {
        // Kiểm tra xem đã setup chưa
        ShellScope.AddDeferredTask(async scope =>
        {
            var myService = scope.ServiceProvider.GetRequiredService<IMyModuleService>();
            
            // Chỉ tạo nếu chưa tồn tại
            if (!await myService.IsInitializedAsync())
            {
                await myService.CreateDefaultDataAsync();
                await myService.MarkAsInitializedAsync();
            }
        });
        
        return 1;
    }
}
```

### 5. Performance Considerations

```csharp
public class PerformantSetupHandler : FeatureEventHandler
{
    public override Task EnabledAsync(IFeatureInfo feature)
    {
        if (feature.Id != "MyModule")
            return Task.CompletedTask;
            
        ShellScope.AddDeferredTask(async scope =>
        {
            // Batch operations để tăng performance
            var tasks = new List<Task>
            {
                CreateContentTypesAsync(scope),
                CreateDefaultDataAsync(scope),
                SetupIndexesAsync(scope)
            };
            
            // Chạy parallel những task không phụ thuộc nhau
            await Task.WhenAll(tasks);
            
            // Chạy sequential những task có dependency
            await ConfigureSettingsAsync(scope);
            await WarmupCacheAsync(scope);
        });
        
        return Task.CompletedTask;
    }
}
```

---

## Kết Luận

Việc tự động hóa modules OrchardCore giúp:

1. **Giảm thiểu can thiệp thủ công**: Module hoạt động ngay sau khi cài đặt
2. **Tăng trải nghiệm người dùng**: Không cần cấu hình phức tạp
3. **Đảm bảo tính nhất quán**: Cùng một cấu hình trên mọi môi trường
4. **Giảm lỗi**: Tự động hóa giảm thiểu lỗi do con người

### Checklist Tự Động Hóa:

- [ ] Tạo Recipe files cho cấu hình tự động
- [ ] Implement Feature Event Handlers
- [ ] Tạo Data Migrations với deferred tasks
- [ ] Xử lý Setup Events nếu cần
- [ ] Test trên môi trường clean
- [ ] Đảm bảo idempotent operations
- [ ] Implement proper error handling
- [ ] Document auto-setup process

Với những patterns này, modules OrchardCore của bạn sẽ có thể tự động cấu hình và hoạt động ngay sau khi được cài đặt, mang lại trải nghiệm tốt nhất cho người dùng cuối.