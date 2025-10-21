# Tự Động Hóa Modules OrchardCore - Quy Định Và Standards Chi Tiết

## Mục Lục
1. [Quy Định Chung OrchardCore](#quy-định-chung-orchardcore)
2. [Feature Event Handlers - Quy Định Chi Tiết](#feature-event-handlers)
3. [Data Migrations - Standards Và Patterns](#data-migrations)
4. [Setup Event Handlers - Quy Định Cụ Thể](#setup-event-handlers)
5. [Recipe System - Coding Standards](#recipe-system)
6. [Deferred Tasks - Quy Định Sử Dụng](#deferred-tasks)
7. [So Sánh Cách Viết Đúng vs Sai](#so-sánh-cách-viết-đúng-vs-sai)
8. [Ví Dụ Test Thực Tế](#ví-dụ-test-thực-tế)
9. [Checklist Tuân Thủ Standards](#checklist-tuân-thủ-standards)

---

## Quy Định Chung OrchardCore

### 🔴 QUY ĐỊNH BẮT BUỘC

#### 1. Naming Conventions (Quy Định Đặt Tên)
```csharp
// ✅ ĐÚNG: Tuân thủ naming conventions của OrchardCore
namespace MyCompany.MyModule.Services
{
    public class MyModuleFeatureEventHandler : FeatureEventHandler  // Suffix: FeatureEventHandler
    public class MyModuleMigrations : DataMigration                 // Suffix: Migrations hoặc DataMigration
    public class MyModuleSetupEventHandler : ISetupEventHandler     // Suffix: SetupEventHandler
    public class MyModuleRecipeStep : NamedRecipeStepHandler        // Suffix: RecipeStep hoặc Step
}

// ❌ SAI: Không tuân thủ naming conventions
namespace MyModule
{
    public class Handler : FeatureEventHandler          // Tên quá chung
    public class Migration : DataMigration              // Không có prefix module
    public class Setup : ISetupEventHandler             // Tên không rõ ràng
}
```

#### 2. Namespace Structure (Cấu Trúc Namespace)
```csharp
// ✅ ĐÚNG: Cấu trúc namespace chuẩn OrchardCore
MyCompany.MyModule                          // Root namespace
MyCompany.MyModule.Services                 // Services
MyCompany.MyModule.Models                   // Models
MyCompany.MyModule.ViewModels               // ViewModels
MyCompany.MyModule.Controllers              // Controllers
MyCompany.MyModule.Drivers                  // Display drivers
MyCompany.MyModule.Handlers                 // Content handlers
MyCompany.MyModule.Migrations               // Data migrations
MyCompany.MyModule.Recipes                  // Recipe steps
MyCompany.MyModule.Indexes                  // YesSql indexes

// ❌ SAI: Cấu trúc không chuẩn
MyModule.Stuff                              // Tên folder không rõ ràng
MyModule.Things                             // Không theo convention
MyModule.Utilities                          // Nên dùng Services
```

#### 3. Dependency Injection Registration (Đăng Ký DI)
```csharp
// ✅ ĐÚNG: Đăng ký services theo đúng lifetime
public override void ConfigureServices(IServiceCollection services)
{
    // Scoped cho services có state per request
    services.AddScoped<IMyModuleService, MyModuleService>();
    services.AddScoped<IFeatureEventHandler, MyModuleFeatureEventHandler>();
    services.AddScoped<ISetupEventHandler, MyModuleSetupEventHandler>();
    
    // Transient cho lightweight services
    services.AddTransient<IRecipeStepHandler, MyModuleRecipeStep>();
    
    // Singleton cho stateless services
    services.AddSingleton<IMyModuleConfiguration, MyModuleConfiguration>();
}

// ❌ SAI: Lifetime không phù hợp
public override void ConfigureServices(IServiceCollection services)
{
    // Singleton cho service có state - NGUY HIỂM
    services.AddSingleton<IMyModuleService, MyModuleService>();
    
    // Transient cho heavy services - KHÔNG HIỆU QUẢ
    services.AddTransient<IHeavyProcessingService, HeavyProcessingService>();
}
```

---

## Feature Event Handlers - Quy Định Chi Tiết

### 🔴 QUY ĐỊNH BẮT BUỘC CHO FEATURE EVENT HANDLERS

#### 1. Class Declaration Standards
```csharp
// ✅ ĐÚNG: Kế thừa từ FeatureEventHandler (abstract class)
public class MyModuleFeatureEventHandler : FeatureEventHandler
{
    private readonly IMyModuleService _myModuleService;
    private readonly ILogger<MyModuleFeatureEventHandler> _logger;
    
    // Constructor PHẢI inject các dependencies cần thiết
    public MyModuleFeatureEventHandler(
        IMyModuleService myModuleService,
        ILogger<MyModuleFeatureEventHandler> logger)
    {
        _myModuleService = myModuleService;
        _logger = logger;
    }
}

// ❌ SAI: Implement interface trực tiếp
public class MyModuleFeatureEventHandler : IFeatureEventHandler
{
    // Phải implement tất cả methods, kể cả không dùng
    public Task InstallingAsync(IFeatureInfo feature) => Task.CompletedTask;
    public Task InstalledAsync(IFeatureInfo feature) => Task.CompletedTask;
    // ... 6 methods khác
}
```

#### 2. Feature ID Validation (BẮT BUỘC)
```csharp
// ✅ ĐÚNG: LUÔN kiểm tra feature ID trước khi xử lý
public override Task EnabledAsync(IFeatureInfo feature)
{
    // QUY ĐỊNH: PHẢI kiểm tra feature.Id hoặc feature.Extension.Id
    if (feature.Id != "MyModule")
        return Task.CompletedTask;
        
    // Hoặc kiểm tra theo Extension
    if (feature.Extension.Id != GetType().Assembly.GetName().Name)
        return Task.CompletedTask;
        
    // Xử lý logic...
    return Task.CompletedTask;
}

// ❌ SAI: Không kiểm tra feature ID
public override Task EnabledAsync(IFeatureInfo feature)
{
    // NGUY HIỂM: Sẽ chạy cho TẤT CẢ features
    ShellScope.AddDeferredTask(async scope =>
    {
        // Logic này sẽ chạy khi BẤT KỲ feature nào được enable
    });
    
    return Task.CompletedTask;
}
```

#### 3. Deferred Tasks Usage (QUY ĐỊNH NGHIÊM NGẶT)
```csharp
// ✅ ĐÚNG: Sử dụng ShellScope.AddDeferredTask cho heavy operations
public override Task EnabledAsync(IFeatureInfo feature)
{
    if (feature.Id != "MyModule")
        return Task.CompletedTask;
        
    // QUY ĐỊNH: Heavy operations PHẢI dùng deferred tasks
    ShellScope.AddDeferredTask(async scope =>
    {
        try
        {
            var myService = scope.ServiceProvider.GetRequiredService<IMyModuleService>();
            
            // Database operations
            await myService.CreateDefaultDataAsync();
            
            // File system operations
            await myService.CreateDirectoriesAsync();
            
            // External API calls
            await myService.RegisterWithExternalServiceAsync();
        }
        catch (Exception ex)
        {
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<MyModuleFeatureEventHandler>>();
            logger.LogError(ex, "Failed to initialize MyModule");
            // QUY ĐỊNH: KHÔNG throw exception trong deferred tasks
        }
    });
    
    return Task.CompletedTask;
}

// ❌ SAI: Thực hiện heavy operations trực tiếp
public override async Task EnabledAsync(IFeatureInfo feature)
{
    if (feature.Id != "MyModule")
        return;
        
    // NGUY HIỂM: Blocking main thread
    await _myModuleService.CreateDefaultDataAsync();
    await _myModuleService.CreateDirectoriesAsync();
    
    // CÓ THỂ GÂY TIMEOUT hoặc DEADLOCK
}
```

#### 4. Error Handling Standards
```csharp
// ✅ ĐÚNG: Error handling chuẩn OrchardCore
public override Task EnabledAsync(IFeatureInfo feature)
{
    if (feature.Id != "MyModule")
        return Task.CompletedTask;
        
    ShellScope.AddDeferredTask(async scope =>
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<MyModuleFeatureEventHandler>>();
        
        try
        {
            var myService = scope.ServiceProvider.GetRequiredService<IMyModuleService>();
            await myService.InitializeAsync();
            
            logger.LogInformation("MyModule initialized successfully");
        }
        catch (Exception ex)
        {
            // QUY ĐỊNH: Log error nhưng KHÔNG throw
            logger.LogError(ex, "Failed to initialize MyModule after enabling feature");
            
            // QUY ĐỊNH: Có thể thử fallback hoặc partial initialization
            try
            {
                await myService.MinimalInitializeAsync();
                logger.LogWarning("MyModule initialized with minimal configuration due to error");
            }
            catch (Exception fallbackEx)
            {
                logger.LogError(fallbackEx, "Failed to initialize MyModule even with minimal configuration");
            }
        }
    });
    
    return Task.CompletedTask;
}

// ❌ SAI: Throw exception trong deferred task
ShellScope.AddDeferredTask(async scope =>
{
    var myService = scope.ServiceProvider.GetRequiredService<IMyModuleService>();
    await myService.InitializeAsync(); // Có thể throw exception
    
    // NGUY HIỂM: Exception sẽ crash application
});
```

#### 5. Service Resolution Pattern
```csharp
// ✅ ĐÚNG: Resolve services từ scope trong deferred task
ShellScope.AddDeferredTask(async scope =>
{
    // QUY ĐỊNH: LUÔN resolve từ scope.ServiceProvider
    var myService = scope.ServiceProvider.GetRequiredService<IMyModuleService>();
    var contentManager = scope.ServiceProvider.GetRequiredService<IContentManager>();
    var siteService = scope.ServiceProvider.GetRequiredService<ISiteService>();
    
    // Sử dụng services...
});

// ❌ SAI: Sử dụng injected services trong deferred task
private readonly IMyModuleService _myModuleService; // Injected

ShellScope.AddDeferredTask(async scope =>
{
    // NGUY HIỂM: Service có thể đã disposed
    await _myModuleService.InitializeAsync();
});
```

#### 6. Conditional Logic Standards
```csharp
// ✅ ĐÚNG: Kiểm tra dependencies và conditions
public override Task EnabledAsync(IFeatureInfo feature)
{
    if (feature.Id != "MyModule")
        return Task.CompletedTask;
        
    ShellScope.AddDeferredTask(async scope =>
    {
        var featuresManager = scope.ServiceProvider.GetRequiredService<IShellFeaturesManager>();
        var siteService = scope.ServiceProvider.GetRequiredService<ISiteService>();
        
        // QUY ĐỊNH: Kiểm tra dependencies trước khi thực hiện
        var hasContentTypes = await featuresManager.IsFeatureEnabledAsync("OrchardCore.ContentTypes");
        var hasMedia = await featuresManager.IsFeatureEnabledAsync("OrchardCore.Media");
        
        if (hasContentTypes && hasMedia)
        {
            await FullInitializationAsync(scope);
        }
        else if (hasContentTypes)
        {
            await PartialInitializationAsync(scope);
        }
        else
        {
            await MinimalInitializationAsync(scope);
        }
        
        // QUY ĐỊNH: Kiểm tra settings
        var site = await siteService.LoadSiteSettingsAsync();
        var settings = site.As<MyModuleSettings>();
        
        if (settings.AutoCreateContent)
        {
            await CreateDefaultContentAsync(scope);
        }
    });
    
    return Task.CompletedTask;
}
```

### 🟡 PATTERNS ĐƯỢC KHUYẾN NGHỊ

#### 1. Multi-Feature Handler Pattern
```csharp
// ✅ KHUYẾN NGHỊ: Handler xử lý multiple related features
public class MyModuleSuiteFeatureEventHandler : FeatureEventHandler
{
    private static readonly string[] RelatedFeatures = 
    {
        "MyModule.Core",
        "MyModule.Admin",
        "MyModule.Api"
    };
    
    public override Task EnabledAsync(IFeatureInfo feature)
    {
        if (!RelatedFeatures.Contains(feature.Id))
            return Task.CompletedTask;
            
        ShellScope.AddDeferredTask(async scope =>
        {
            await HandleFeatureEnabled(scope, feature.Id);
        });
        
        return Task.CompletedTask;
    }
    
    private async Task HandleFeatureEnabled(IServiceScope scope, string featureId)
    {
        switch (featureId)
        {
            case "MyModule.Core":
                await InitializeCoreAsync(scope);
                break;
            case "MyModule.Admin":
                await InitializeAdminAsync(scope);
                break;
            case "MyModule.Api":
                await InitializeApiAsync(scope);
                break;
        }
    }
}
```

#### 2. Idempotent Operations Pattern
```csharp
// ✅ KHUYẾN NGHỊ: Operations có thể chạy nhiều lần an toàn
public override Task EnabledAsync(IFeatureInfo feature)
{
    if (feature.Id != "MyModule")
        return Task.CompletedTask;
        
    ShellScope.AddDeferredTask(async scope =>
    {
        var myService = scope.ServiceProvider.GetRequiredService<IMyModuleService>();
        
        // QUY ĐỊNH: Kiểm tra trạng thái trước khi thực hiện
        if (!await myService.IsInitializedAsync())
        {
            await myService.InitializeAsync();
            await myService.MarkAsInitializedAsync();
        }
        
        // QUY ĐỊNH: Cập nhật configuration mỗi lần enable
        await myService.UpdateConfigurationAsync();
    });
    
    return Task.CompletedTask;
}
```

---

## Data Migrations - Standards Và Patterns

### 🔴 QUY ĐỊNH BẮT BUỘC CHO DATA MIGRATIONS

#### 1. Class Declaration và Naming
```csharp
// ✅ ĐÚNG: Naming và structure chuẩn
namespace MyCompany.MyModule.Migrations
{
    public sealed class MyModuleMigrations : DataMigration  // sealed class
    {
        private readonly IContentDefinitionManager _contentDefinitionManager;
        private readonly ILogger<MyModuleMigrations> _logger;
        
        // QUY ĐỊNH: Constructor injection cho dependencies
        public MyModuleMigrations(
            IContentDefinitionManager contentDefinitionManager,
            ILogger<MyModuleMigrations> logger)
        {
            _contentDefinitionManager = contentDefinitionManager;
            _logger = logger;
        }
    }
}

// ❌ SAI: Naming và structure không chuẩn
namespace MyModule
{
    public class Migration : DataMigration  // Tên không rõ ràng
    {
        // Không có dependencies injection
    }
}
```

#### 2. Create Method Standards
```csharp
// ✅ ĐÚNG: Create method chuẩn OrchardCore
public async Task<int> CreateAsync()  // PHẢI là async và return int
{
    // QUY ĐỊNH: Tạo database schema trước
    await SchemaBuilder.CreateMapIndexTableAsync<MyModuleIndex>(table => table
        .Column<string>("Name", column => column.WithLength(255))  // Specify length
        .Column<DateTime>("CreatedUtc")
        .Column<bool>("IsActive")
        .Column<string>("ContentItemId", column => column.WithLength(26))  // ContentItemId length
    );
    
    // QUY ĐỊNH: Tạo indexes sau
    await SchemaBuilder.AlterIndexTableAsync<MyModuleIndex>(table => table
        .CreateIndex("IDX_MyModule_Name", "Name")
        .CreateIndex("IDX_MyModule_CreatedUtc", "CreatedUtc")
        .CreateIndex("IDX_MyModule_ContentItemId", "ContentItemId")
    );
    
    // QUY ĐỊNH: Content definitions
    await _contentDefinitionManager.AlterPartDefinitionAsync("MyModulePart", part => part
        .Attachable()
        .WithDescription("Provides MyModule functionality"));
    
    // QUY ĐỊNH: Deferred tasks cho heavy operations
    ShellScope.AddDeferredTask(async scope =>
    {
        var myService = scope.ServiceProvider.GetRequiredService<IMyModuleService>();
        await myService.CreateDefaultDataAsync();
    });
    
    // QUY ĐỊNH: Return version number (thường là 1 cho Create)
    return 1;
}

// ❌ SAI: Create method không chuẩn
public int Create()  // Không async
{
    // Không có error handling
    SchemaBuilder.CreateMapIndexTable<MyModuleIndex>(table => table
        .Column<string>("Name")  // Không specify length
    );
    
    return 1;
}
```

#### 3. Update Methods Standards
```csharp
// ✅ ĐÚNG: Update methods với version management
public async Task<int> UpdateFrom1Async()  // Pattern: UpdateFrom{PreviousVersion}Async
{
    // QUY ĐỊNH: Log migration steps
    _logger.LogInformation("Updating MyModule from version 1 to 2");
    
    try
    {
        // QUY ĐỊNH: Schema changes trước
        await SchemaBuilder.AlterIndexTableAsync<MyModuleIndex>(table => table
            .AddColumn<string>("Description", column => column.WithLength(1000))
        );
        
        // QUY ĐỊNH: Data migration sau
        ShellScope.AddDeferredTask(async scope =>
        {
            var myService = scope.ServiceProvider.GetRequiredService<IMyModuleService>();
            await myService.MigrateDataFromV1ToV2Async();
        });
        
        _logger.LogInformation("Successfully updated MyModule to version 2");
        return 2;  // Return new version
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to update MyModule from version 1 to 2");
        throw;  // QUY ĐỊNH: Re-throw để migration framework xử lý
    }
}

public async Task<int> UpdateFrom2Async()
{
    // Migration từ version 2 lên 3
    await SchemaBuilder.AlterIndexTableAsync<MyModuleIndex>(table => table
        .DropIndex("IDX_MyModule_OldColumn")
        .DropColumn("OldColumn")
    );
    
    return 3;
}

// ❌ SAI: Update method không chuẩn
public int UpdateFrom1()  // Không async
{
    // Không có error handling
    // Không có logging
    return 2;
}
```

#### 4. Deferred Tasks trong Migrations
```csharp
// ✅ ĐÚNG: Sử dụng deferred tasks đúng cách
public async Task<int> CreateAsync()
{
    // Schema operations ngay lập tức
    await SchemaBuilder.CreateMapIndexTableAsync<MyModuleIndex>(table => table
        .Column<string>("Name", column => column.WithLength(255))
    );
    
    // QUY ĐỊNH: Heavy operations trong deferred tasks
    ShellScope.AddDeferredTask(async scope =>
    {
        try
        {
            var contentManager = scope.ServiceProvider.GetRequiredService<IContentManager>();
            var myService = scope.ServiceProvider.GetRequiredService<IMyModuleService>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<MyModuleMigrations>>();
            
            // Tạo default content
            await myService.CreateDefaultContentAsync();
            
            // Import recipes
            var recipeExecutor = scope.ServiceProvider.GetRequiredService<IRecipeExecutor>();
            await recipeExecutor.ExecuteAsync("mymodule-setup", new Dictionary<string, object>());
            
            // Cấu hình settings
            var siteService = scope.ServiceProvider.GetRequiredService<ISiteService>();
            var site = await siteService.LoadSiteSettingsAsync();
            site.Put(new MyModuleSettings { IsEnabled = true });
            await siteService.UpdateSiteSettingsAsync(site);
            
            logger.LogInformation("MyModule default data created successfully");
        }
        catch (Exception ex)
        {
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<MyModuleMigrations>>();
            logger.LogError(ex, "Failed to create MyModule default data");
            // Không throw - để migration tiếp tục
        }
    });
    
    return 1;
}

// ❌ SAI: Heavy operations trong main thread
public async Task<int> CreateAsync()
{
    await SchemaBuilder.CreateMapIndexTableAsync<MyModuleIndex>(table => table
        .Column<string>("Name", column => column.WithLength(255))
    );
    
    // NGUY HIỂM: Blocking operations
    var contentManager = GetService<IContentManager>();  // Service locator anti-pattern
    await CreateDefaultContentAsync();  // Heavy operation
    
    return 1;
}
```

#### 5. Recipe Integration trong Migrations
```csharp
// ✅ ĐÚNG: Tích hợp recipes trong migrations
public class MyModuleMigrations : DataMigration
{
    private readonly IRecipeMigrator _recipeMigrator;
    
    public MyModuleMigrations(IRecipeMigrator recipeMigrator)
    {
        _recipeMigrator = recipeMigrator;
    }
    
    public async Task<int> CreateAsync()
    {
        // Schema operations
        await CreateSchemaAsync();
        
        // QUY ĐỊNH: Execute recipe thông qua IRecipeMigrator
        await _recipeMigrator.ExecuteAsync($"mymodule-setup{RecipesConstants.RecipeExtension}", this);
        
        return 1;
    }
    
    public async Task<int> UpdateFrom1Async()
    {
        // Schema updates
        await UpdateSchemaAsync();
        
        // Execute update recipe
        await _recipeMigrator.ExecuteAsync($"mymodule-update-v2{RecipesConstants.RecipeExtension}", this);
        
        return 2;
    }
}

// ❌ SAI: Không sử dụng IRecipeMigrator
public async Task<int> CreateAsync()
{
    // Manual recipe execution - KHÔNG KHUYẾN NGHỊ
    var recipeExecutor = GetService<IRecipeExecutor>();
    await recipeExecutor.ExecuteAsync("mymodule-setup", new Dictionary<string, object>());
    
    return 1;
}
```

---

## Setup Event Handlers - Quy Định Cụ Thể

### 🔴 QUY ĐỊNH BẮT BUỘC CHO SETUP EVENT HANDLERS

#### 1. Interface Implementation
```csharp
// ✅ ĐÚNG: Implement ISetupEventHandler
namespace MyCompany.MyModule.Services
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
            // QUY ĐỊNH: Kiểm tra xem module có được enable không
            if (!IsModuleInRecipe(context))
                return;
                
            try
            {
                _logger.LogInformation("Setting up MyModule...");
                
                // Setup logic here
                await SetupModuleAsync(context);
                
                _logger.LogInformation("MyModule setup completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting up MyModule");
                // QUY ĐỊNH: Thêm error vào context
                context.Errors["MyModule"] = ex.Message;
            }
        }
        
        private bool IsModuleInRecipe(SetupContext context)
        {
            // Kiểm tra recipe name hoặc enabled features
            return context.Recipe?.Name?.Contains("MyModule") == true ||
                   context.EnabledFeatures?.Contains("MyModule") == true;
        }
    }
}

// ❌ SAI: Không implement interface hoặc sai cách
public class MyModuleSetup  // Không implement interface
{
    public void Setup()  // Sai signature
    {
        // Setup logic
    }
}
```

#### 2. SetupContext Usage Standards
```csharp
// ✅ ĐÚNG: Sử dụng SetupContext đúng cách
public async Task SetupAsync(SetupContext context)
{
    // QUY ĐỊNH: Đọc properties từ context
    var siteName = GetPropertyValue(context, SetupConstants.SiteName);
    var adminEmail = GetPropertyValue(context, SetupConstants.AdminEmail);
    var adminUsername = GetPropertyValue(context, SetupConstants.AdminUsername);
    
    // QUY ĐỊNH: Đọc custom properties
    var myModuleApiKey = GetPropertyValue(context, "MyModuleApiKey");
    var myModuleEnabled = bool.Parse(GetPropertyValue(context, "MyModuleEnabled", "true"));
    
    // Setup với properties
    var settings = new MyModuleSettings
    {
        ApiKey = myModuleApiKey,
        IsEnabled = myModuleEnabled,
        AdminEmail = adminEmail
    };
    
    await _myModuleService.SaveSettingsAsync(settings);
    
    // QUY ĐỊNH: Tạo admin user nếu cần
    if (!string.IsNullOrEmpty(adminUsername))
    {
        await _myModuleService.CreateAdminUserAsync(adminUsername, adminEmail);
    }
}

private string GetPropertyValue(SetupContext context, string key, string defaultValue = "")
{
    return context.Properties.TryGetValue(key, out var value) 
        ? value?.ToString() ?? defaultValue 
        : defaultValue;
}

// ❌ SAI: Không sử dụng SetupContext
public async Task SetupAsync(SetupContext context)
{
    // Hard-coded values - KHÔNG LINH HOẠT
    var settings = new MyModuleSettings
    {
        ApiKey = "default-key",
        IsEnabled = true
    };
    
    await _myModuleService.SaveSettingsAsync(settings);
}
```

#### 3. Error Handling trong Setup
```csharp
// ✅ ĐÚNG: Error handling chuẩn
public async Task SetupAsync(SetupContext context)
{
    if (!IsModuleInRecipe(context))
        return;
        
    try
    {
        // Validate required properties
        ValidateRequiredProperties(context);
        
        // Setup database
        await SetupDatabaseAsync(context);
        
        // Setup configuration
        await SetupConfigurationAsync(context);
        
        // Setup default data
        await SetupDefaultDataAsync(context);
        
        _logger.LogInformation("MyModule setup completed successfully");
    }
    catch (ArgumentException ex)
    {
        _logger.LogError(ex, "Invalid setup parameters for MyModule");
        context.Errors["MyModule.Validation"] = ex.Message;
    }
    catch (InvalidOperationException ex)
    {
        _logger.LogError(ex, "MyModule setup failed due to invalid operation");
        context.Errors["MyModule.Operation"] = ex.Message;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Unexpected error during MyModule setup");
        context.Errors["MyModule.Unexpected"] = ex.Message;
    }
}

private void ValidateRequiredProperties(SetupContext context)
{
    var requiredProperties = new[] { "MyModuleApiKey", "MyModuleDatabase" };
    
    foreach (var property in requiredProperties)
    {
        if (!context.Properties.ContainsKey(property) || 
            string.IsNullOrWhiteSpace(context.Properties[property]?.ToString()))
        {
            throw new ArgumentException($"Required property '{property}' is missing or empty");
        }
    }
}

// ❌ SAI: Không có error handling
public async Task SetupAsync(SetupContext context)
{
    // Có thể throw exception và crash setup process
    await SetupDatabaseAsync(context);
    await SetupConfigurationAsync(context);
}
```

#### 4. Integration với Site Settings
```csharp
// ✅ ĐÚNG: Tích hợp với site settings
public async Task SetupAsync(SetupContext context)
{
    if (!IsModuleInRecipe(context))
        return;
        
    try
    {
        // QUY ĐỊNH: Sử dụng ISiteService để lưu settings
        var siteService = GetRequiredService<ISiteService>(context);
        var site = await siteService.LoadSiteSettingsAsync();
        
        // Tạo settings từ setup context
        var myModuleSettings = new MyModuleSettings
        {
            ApiKey = GetPropertyValue(context, "MyModuleApiKey"),
            DatabaseConnection = GetPropertyValue(context, "MyModuleDatabaseConnection"),
            IsEnabled = bool.Parse(GetPropertyValue(context, "MyModuleEnabled", "true")),
            AdminEmail = GetPropertyValue(context, SetupConstants.AdminEmail)
        };
        
        // QUY ĐỊNH: Sử dụng Put method để lưu settings
        site.Put(myModuleSettings);
        await siteService.UpdateSiteSettingsAsync(site);
        
        _logger.LogInformation("MyModule settings saved successfully");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to save MyModule settings");
        context.Errors["MyModule.Settings"] = ex.Message;
    }
}

// Helper method để resolve services
private T GetRequiredService<T>(SetupContext context)
{
    // QUY ĐỊNH: Resolve từ service provider trong context
    return context.ServiceProvider?.GetRequiredService<T>() 
        ?? throw new InvalidOperationException($"Service {typeof(T).Name} not available");
}

// ❌ SAI: Không tích hợp với site settings
public async Task SetupAsync(SetupContext context)
{
    // Lưu settings riêng biệt - KHÔNG NHẤT QUÁN
    var settings = new MyModuleSettings();
    await SaveToFileAsync(settings);  // Không theo pattern OrchardCore
}
```

---

## Recipe System - Coding Standards

### 🔴 QUY ĐỊNH BẮT BUỘC CHO RECIPE SYSTEM

#### 1. Recipe File Structure Standards
```json
// ✅ ĐÚNG: Cấu trúc recipe file chuẩn
{
  "name": "MyModuleSetup",                    // REQUIRED: Unique name
  "displayName": "My Module Setup",          // REQUIRED: Display name
  "description": "Sets up My Module with default configuration", // REQUIRED
  "author": "My Company",                    // REQUIRED
  "website": "https://mycompany.com",        // OPTIONAL but recommended
  "version": "1.0.0",                        // REQUIRED: Semantic versioning
  "issetuprecipe": false,                    // REQUIRED: true for setup recipes only
  "categories": ["module", "automation"],    // REQUIRED: Array of categories
  "tags": ["mymodule", "setup", "automation"], // REQUIRED: Array of tags
  
  // OPTIONAL: Variables for reuse
  "variables": {
    "contentItemId": "[js:uuid()]",
    "currentDate": "[js: new Date().toISOString()]",
    "siteName": "[js: variables('SiteName') || 'Default Site']"
  },
  
  // REQUIRED: Steps array
  "steps": [
    {
      "name": "feature",                     // REQUIRED: Step name
      "enable": [                           // Enable features first
        "OrchardCore.Contents",
        "OrchardCore.ContentTypes",
        "MyModule"
      ]
    }
    // More steps...
  ]
}

// ❌ SAI: Cấu trúc không chuẩn
{
  "name": "setup",                          // Tên quá chung
  "description": "Setup",                   // Mô tả không rõ ràng
  "version": "1",                           // Không theo semantic versioning
  "issetuprecipe": "false",                 // String thay vì boolean
  "steps": {                                // Object thay vì array
    "feature": ["MyModule"]                 // Cấu trúc sai
  }
}
```

#### 2. Recipe Step Handler Implementation
```csharp
// ✅ ĐÚNG: Recipe step handler chuẩn OrchardCore
namespace MyCompany.MyModule.Recipes
{
    public sealed class MyModuleRecipeStep : NamedRecipeStepHandler
    {
        private readonly IMyModuleService _myModuleService;
        private readonly ILogger<MyModuleRecipeStep> _logger;
        
        // QUY ĐỊNH: Constructor với step name
        public MyModuleRecipeStep(
            IMyModuleService myModuleService,
            ILogger<MyModuleRecipeStep> logger) 
            : base("MyModuleStep")  // REQUIRED: Step name
        {
            _myModuleService = myModuleService;
            _logger = logger;
        }
        
        // QUY ĐỊNH: Override HandleAsync method
        protected override async Task HandleAsync(RecipeExecutionContext context)
        {
            try
            {
                // QUY ĐỊNH: Deserialize step data với proper error handling
                var stepModel = context.Step.ToObject<MyModuleStepModel>();
                
                if (stepModel == null)
                {
                    _logger.LogWarning("MyModuleStep data is null or invalid");
                    return;
                }
                
                // QUY ĐỊNH: Validate step data
                if (!ValidateStepModel(stepModel, context))
                    return;
                
                // Execute step logic
                await ExecuteStepAsync(stepModel, context);
                
                _logger.LogInformation("MyModuleStep executed successfully");
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to deserialize MyModuleStep data");
                context.Errors.Add("MyModuleStep.Deserialization", ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing MyModuleStep");
                context.Errors.Add("MyModuleStep.Execution", ex.Message);
            }
        }
        
        private bool ValidateStepModel(MyModuleStepModel model, RecipeExecutionContext context)
        {
            var errors = new List<string>();
            
            if (string.IsNullOrWhiteSpace(model.Name))
                errors.Add("Name is required");
                
            if (model.Settings == null)
                errors.Add("Settings are required");
            
            if (errors.Any())
            {
                var errorMessage = string.Join(", ", errors);
                _logger.LogError("MyModuleStep validation failed: {Errors}", errorMessage);
                context.Errors.Add("MyModuleStep.Validation", errorMessage);
                return false;
            }
            
            return true;
        }
        
        private async Task ExecuteStepAsync(MyModuleStepModel model, RecipeExecutionContext context)
        {
            // Step execution logic
            await _myModuleService.ConfigureAsync(model.Settings);
            
            if (model.CreateDefaultData)
            {
                await _myModuleService.CreateDefaultDataAsync();
            }
        }
    }
    
    // QUY ĐỊNH: Step model class
    public class MyModuleStepModel
    {
        public string Name { get; set; }
        public Dictionary<string, object> Settings { get; set; }
        public bool CreateDefaultData { get; set; }
    }
}

// ❌ SAI: Recipe step handler không chuẩn
public class MyRecipeHandler : IRecipeStepHandler  // Implement interface trực tiếp
{
    public string Name => "MyStep";  // Hard-coded name
    
    public async Task ExecuteAsync(RecipeExecutionContext context)
    {
        // Không có error handling
        var data = context.Step.ToObject<object>();
        // Process data...
    }
}
```

#### 3. Recipe Step Registration
```csharp
// ✅ ĐÚNG: Đăng ký recipe step handler
public override void ConfigureServices(IServiceCollection services)
{
    // QUY ĐỊNH: Đăng ký với Transient lifetime
    services.AddTransient<IRecipeStepHandler, MyModuleRecipeStep>();
    
    // QUY ĐỊNH: Có thể đăng ký multiple step handlers
    services.AddTransient<IRecipeStepHandler, MyModuleContentStep>();
    services.AddTransient<IRecipeStepHandler, MyModuleSettingsStep>();
}

// ❌ SAI: Đăng ký sai lifetime
public override void ConfigureServices(IServiceCollection services)
{
    // Singleton - KHÔNG ĐÚNG cho recipe steps
    services.AddSingleton<IRecipeStepHandler, MyModuleRecipeStep>();
}
```

#### 4. Complex Recipe Steps với Validation
```csharp
// ✅ ĐÚNG: Complex recipe step với full validation
public sealed class MyModuleContentRecipeStep : NamedRecipeStepHandler
{
    private readonly IContentManager _contentManager;
    private readonly IContentDefinitionManager _contentDefinitionManager;
    private readonly ILogger<MyModuleContentRecipeStep> _logger;
    
    public MyModuleContentRecipeStep(
        IContentManager contentManager,
        IContentDefinitionManager contentDefinitionManager,
        ILogger<MyModuleContentRecipeStep> logger) 
        : base("MyModuleContent")
    {
        _contentManager = contentManager;
        _contentDefinitionManager = contentDefinitionManager;
        _logger = logger;
    }
    
    protected override async Task HandleAsync(RecipeExecutionContext context)
    {
        try
        {
            var stepModel = context.Step.ToObject<MyModuleContentStepModel>();
            
            if (!await ValidateStepModelAsync(stepModel, context))
                return;
            
            // Process content types
            if (stepModel.ContentTypes?.Any() == true)
            {
                await ProcessContentTypesAsync(stepModel.ContentTypes, context);
            }
            
            // Process content items
            if (stepModel.ContentItems?.Any() == true)
            {
                await ProcessContentItemsAsync(stepModel.ContentItems, context);
            }
            
            _logger.LogInformation("MyModuleContent step completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing MyModuleContent step");
            context.Errors.Add("MyModuleContent.Execution", ex.Message);
        }
    }
    
    private async Task<bool> ValidateStepModelAsync(MyModuleContentStepModel model, RecipeExecutionContext context)
    {
        var errors = new List<string>();
        
        // Validate content types
        if (model.ContentTypes?.Any() == true)
        {
            foreach (var contentType in model.ContentTypes)
            {
                if (string.IsNullOrWhiteSpace(contentType.Name))
                {
                    errors.Add($"Content type name is required");
                }
                
                // Check if content type already exists
                var existingType = await _contentDefinitionManager.GetTypeDefinitionAsync(contentType.Name);
                if (existingType != null && !contentType.OverwriteExisting)
                {
                    errors.Add($"Content type '{contentType.Name}' already exists and OverwriteExisting is false");
                }
            }
        }
        
        // Validate content items
        if (model.ContentItems?.Any() == true)
        {
            foreach (var contentItem in model.ContentItems)
            {
                if (string.IsNullOrWhiteSpace(contentItem.ContentType))
                {
                    errors.Add("Content item ContentType is required");
                }
                
                // Validate content type exists
                var contentTypeDefinition = await _contentDefinitionManager.GetTypeDefinitionAsync(contentItem.ContentType);
                if (contentTypeDefinition == null)
                {
                    errors.Add($"Content type '{contentItem.ContentType}' does not exist");
                }
            }
        }
        
        if (errors.Any())
        {
            var errorMessage = string.Join("; ", errors);
            _logger.LogError("MyModuleContent step validation failed: {Errors}", errorMessage);
            context.Errors.Add("MyModuleContent.Validation", errorMessage);
            return false;
        }
        
        return true;
    }
    
    private async Task ProcessContentTypesAsync(IEnumerable<ContentTypeModel> contentTypes, RecipeExecutionContext context)
    {
        foreach (var contentType in contentTypes)
        {
            try
            {
                await _contentDefinitionManager.AlterTypeDefinitionAsync(contentType.Name, type =>
                {
                    type.DisplayedAs(contentType.DisplayName ?? contentType.Name);
                    
                    if (contentType.Settings?.Creatable == true)
                        type.Creatable();
                    if (contentType.Settings?.Draftable == true)
                        type.Draftable();
                    if (contentType.Settings?.Versionable == true)
                        type.Versionable();
                    if (contentType.Settings?.Listable == true)
                        type.Listable();
                    if (contentType.Settings?.Securable == true)
                        type.Securable();
                    
                    // Add parts
                    if (contentType.Parts?.Any() == true)
                    {
                        foreach (var part in contentType.Parts)
                        {
                            type.WithPart(part.Name, part.Settings);
                        }
                    }
                });
                
                _logger.LogInformation("Content type '{ContentType}' processed successfully", contentType.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process content type '{ContentType}'", contentType.Name);
                context.Errors.Add($"MyModuleContent.ContentType.{contentType.Name}", ex.Message);
            }
        }
    }
    
    private async Task ProcessContentItemsAsync(IEnumerable<ContentItemModel> contentItems, RecipeExecutionContext context)
    {
        foreach (var contentItemModel in contentItems)
        {
            try
            {
                var contentItem = await _contentManager.NewAsync(contentItemModel.ContentType);
                
                // Set properties from model
                if (!string.IsNullOrWhiteSpace(contentItemModel.DisplayText))
                {
                    contentItem.DisplayText = contentItemModel.DisplayText;
                }
                
                // Apply parts data
                if (contentItemModel.Parts != null)
                {
                    foreach (var partData in contentItemModel.Parts)
                    {
                        contentItem.Apply(partData.Key, partData.Value);
                    }
                }
                
                await _contentManager.CreateAsync(contentItem);
                
                if (contentItemModel.Published)
                {
                    await _contentManager.PublishAsync(contentItem);
                }
                
                _logger.LogInformation("Content item '{DisplayText}' created successfully", contentItem.DisplayText);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create content item '{DisplayText}'", contentItemModel.DisplayText);
                context.Errors.Add($"MyModuleContent.ContentItem.{contentItemModel.DisplayText}", ex.Message);
            }
        }
    }
}

// Step model classes
public class MyModuleContentStepModel
{
    public IEnumerable<ContentTypeModel> ContentTypes { get; set; }
    public IEnumerable<ContentItemModel> ContentItems { get; set; }
}

public class ContentTypeModel
{
    public string Name { get; set; }
    public string DisplayName { get; set; }
    public bool OverwriteExisting { get; set; }
    public ContentTypeSettings Settings { get; set; }
    public IEnumerable<PartModel> Parts { get; set; }
}

public class ContentTypeSettings
{
    public bool Creatable { get; set; }
    public bool Draftable { get; set; }
    public bool Versionable { get; set; }
    public bool Listable { get; set; }
    public bool Securable { get; set; }
}

public class PartModel
{
    public string Name { get; set; }
    public object Settings { get; set; }
}

public class ContentItemModel
{
    public string ContentType { get; set; }
    public string DisplayText { get; set; }
    public bool Published { get; set; }
    public Dictionary<string, object> Parts { get; set; }
}
```

---

## Deferred Tasks - Quy Định Sử Dụng

### 🔴 QUY ĐỊNH BẮT BUỘC CHO DEFERRED TASKS

#### 1. Khi Nào PHẢI Sử Dụng Deferred Tasks
```csharp
// ✅ ĐÚNG: Các trường hợp BẮT BUỘC dùng deferred tasks

// 1. Database operations trong Feature Event Handlers
public override Task EnabledAsync(IFeatureInfo feature)
{
    if (feature.Id != "MyModule")
        return Task.CompletedTask;
        
    // QUY ĐỊNH: Database operations PHẢI dùng deferred tasks
    ShellScope.AddDeferredTask(async scope =>
    {
        var contentManager = scope.ServiceProvider.GetRequiredService<IContentManager>();
        await contentManager.CreateAsync(defaultContent);
    });
    
    return Task.CompletedTask;
}

// 2. File system operations
ShellScope.AddDeferredTask(async scope =>
{
    var mediaFileStore = scope.ServiceProvider.GetRequiredService<IMediaFileStore>();
    await mediaFileStore.CreateDirectoryAsync("MyModule");
});

// 3. External API calls
ShellScope.AddDeferredTask(async scope =>
{
    var httpClient = scope.ServiceProvider.GetRequiredService<HttpClient>();
    await httpClient.PostAsync("https://api.example.com/register", content);
});

// 4. Heavy computational tasks
ShellScope.AddDeferredTask(async scope =>
{
    var indexingService = scope.ServiceProvider.GetRequiredService<IIndexingService>();
    await indexingService.RebuildIndexAsync("MyModuleIndex");
});

// 5. Email sending
ShellScope.AddDeferredTask(async scope =>
{
    var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
    await emailService.SendWelcomeEmailAsync(user);
});

// ❌ SAI: Thực hiện trực tiếp trong main thread
public override async Task EnabledAsync(IFeatureInfo feature)
{
    // NGUY HIỂM: Blocking main thread
    var contentManager = GetService<IContentManager>();
    await contentManager.CreateAsync(defaultContent);  // Có thể gây timeout
}
```

#### 2. Service Resolution trong Deferred Tasks
```csharp
// ✅ ĐÚNG: Resolve services từ scope
ShellScope.AddDeferredTask(async scope =>
{
    // QUY ĐỊNH: LUÔN resolve từ scope.ServiceProvider
    var contentManager = scope.ServiceProvider.GetRequiredService<IContentManager>();
    var siteService = scope.ServiceProvider.GetRequiredService<ISiteService>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<MyClass>>();
    
    try
    {
        // Use services...
        await contentManager.CreateAsync(content);
        
        var site = await siteService.LoadSiteSettingsAsync();
        // Process site...
        
        logger.LogInformation("Deferred task completed successfully");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Deferred task failed");
        // QUY ĐỊNH: KHÔNG throw exception
    }
});

// ❌ SAI: Sử dụng injected services
private readonly IContentManager _contentManager;  // Injected service

ShellScope.AddDeferredTask(async scope =>
{
    // NGUY HIỂM: Service có thể đã disposed
    await _contentManager.CreateAsync(content);
});

// ❌ SAI: Service locator pattern
ShellScope.AddDeferredTask(async scope =>
{
    // ANTI-PATTERN: Không nên dùng static service locator
    var contentManager = ServiceLocator.GetService<IContentManager>();
    await contentManager.CreateAsync(content);
});
```

#### 3. Error Handling trong Deferred Tasks
```csharp
// ✅ ĐÚNG: Error handling chuẩn
ShellScope.AddDeferredTask(async scope =>
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<MyClass>>();
    
    try
    {
        var myService = scope.ServiceProvider.GetRequiredService<IMyModuleService>();
        await myService.ProcessDataAsync();
        
        logger.LogInformation("Deferred task completed successfully");
    }
    catch (InvalidOperationException ex)
    {
        // QUY ĐỊNH: Log specific exceptions
        logger.LogWarning(ex, "Deferred task failed due to invalid operation, will retry later");
        
        // QUY ĐỊNH: Có thể schedule retry
        var backgroundTaskService = scope.ServiceProvider.GetRequiredService<IBackgroundTaskService>();
        backgroundTaskService.Schedule("RetryMyModuleProcess", TimeSpan.FromMinutes(5));
    }
    catch (Exception ex)
    {
        // QUY ĐỊNH: Log error nhưng KHÔNG throw
        logger.LogError(ex, "Unexpected error in deferred task");
        
        // QUY ĐỊNH: Có thể thử fallback operation
        try
        {
            await myService.FallbackProcessAsync();
            logger.LogInformation("Fallback operation completed successfully");
        }
        catch (Exception fallbackEx)
        {
            logger.LogError(fallbackEx, "Fallback operation also failed");
        }
    }
});

// ❌ SAI: Throw exception trong deferred task
ShellScope.AddDeferredTask(async scope =>
{
    var myService = scope.ServiceProvider.GetRequiredService<IMyModuleService>();
    await myService.ProcessDataAsync();  // Có thể throw
    
    // NGUY HIỂM: Exception sẽ crash application
});

// ❌ SAI: Không có error handling
ShellScope.AddDeferredTask(async scope =>
{
    var myService = scope.ServiceProvider.GetRequiredService<IMyModuleService>();
    await myService.ProcessDataAsync();  // Không có try-catch
});
```

#### 4. Conditional Logic trong Deferred Tasks
```csharp
// ✅ ĐÚNG: Conditional logic với proper checks
ShellScope.AddDeferredTask(async scope =>
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<MyClass>>();
    
    try
    {
        // QUY ĐỊNH: Kiểm tra feature dependencies
        var featuresManager = scope.ServiceProvider.GetRequiredService<IShellFeaturesManager>();
        
        var hasContentTypes = await featuresManager.IsFeatureEnabledAsync("OrchardCore.ContentTypes");
        var hasMedia = await featuresManager.IsFeatureEnabledAsync("OrchardCore.Media");
        
        if (!hasContentTypes)
        {
            logger.LogWarning("ContentTypes feature not enabled, skipping content creation");
            return;
        }
        
        // QUY ĐỊNH: Kiểm tra settings
        var siteService = scope.ServiceProvider.GetRequiredService<ISiteService>();
        var site = await siteService.LoadSiteSettingsAsync();
        var settings = site.As<MyModuleSettings>();
        
        if (!settings.AutoCreateContent)
        {
            logger.LogInformation("Auto create content is disabled, skipping");
            return;
        }
        
        // QUY ĐỊNH: Kiểm tra existing data
        var myService = scope.ServiceProvider.GetRequiredService<IMyModuleService>();
        if (await myService.HasDefaultDataAsync())
        {
            logger.LogInformation("Default data already exists, skipping creation");
            return;
        }
        
        // Proceed with operations
        if (hasMedia)
        {
            await CreateContentWithMediaAsync(scope);
        }
        else
        {
            await CreateBasicContentAsync(scope);
        }
        
        logger.LogInformation("Conditional deferred task completed successfully");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error in conditional deferred task");
    }
});

// ❌ SAI: Không có conditional checks
ShellScope.AddDeferredTask(async scope =>
{
    // Không kiểm tra dependencies hoặc conditions
    var contentManager = scope.ServiceProvider.GetRequiredService<IContentManager>();
    await contentManager.CreateAsync(content);  // Có thể fail nếu feature không enable
});
```

#### 5. Performance Optimization trong Deferred Tasks
```csharp
// ✅ ĐÚNG: Optimized deferred tasks
ShellScope.AddDeferredTask(async scope =>
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<MyClass>>();
    
    try
    {
        // QUY ĐỊNH: Batch operations để tăng performance
        var contentManager = scope.ServiceProvider.GetRequiredService<IContentManager>();
        var myService = scope.ServiceProvider.GetRequiredService<IMyModuleService>();
        
        // Parallel operations cho independent tasks
        var tasks = new List<Task>
        {
            CreateContentTypesAsync(scope),
            CreateMediaFoldersAsync(scope),
            SetupIndexesAsync(scope)
        };
        
        await Task.WhenAll(tasks);
        
        // Sequential operations cho dependent tasks
        await CreateDefaultContentAsync(scope);
        await ConfigureSettingsAsync(scope);
        await WarmupCacheAsync(scope);
        
        logger.LogInformation("Optimized deferred task completed in {ElapsedTime}ms", stopwatch.ElapsedMilliseconds);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error in optimized deferred task");
    }
});

// QUY ĐỊNH: Separate methods cho clarity
private async Task CreateContentTypesAsync(IServiceScope scope)
{
    var contentDefinitionManager = scope.ServiceProvider.GetRequiredService<IContentDefinitionManager>();
    
    // Batch create content types
    var contentTypes = GetDefaultContentTypes();
    foreach (var contentType in contentTypes)
    {
        await contentDefinitionManager.AlterTypeDefinitionAsync(contentType.Name, type =>
        {
            // Configure content type
        });
    }
}

private async Task CreateMediaFoldersAsync(IServiceScope scope)
{
    var mediaFileStore = scope.ServiceProvider.GetRequiredService<IMediaFileStore>();
    
    var folders = new[] { "MyModule", "MyModule/Images", "MyModule/Documents" };
    foreach (var folder in folders)
    {
        if (!await mediaFileStore.GetDirectoryInfoAsync(folder))
        {
            await mediaFileStore.CreateDirectoryAsync(folder);
        }
    }
}

// ❌ SAI: Không optimize performance
ShellScope.AddDeferredTask(async scope =>
{
    // Sequential execution cho tất cả operations
    await CreateContentType1Async(scope);
    await CreateContentType2Async(scope);  // Có thể chạy parallel
    await CreateContentType3Async(scope);  // Có thể chạy parallel
    
    // Không batch operations
    for (int i = 0; i < 100; i++)
    {
        await CreateSingleContentItemAsync(scope, i);  // Nên batch
    }
});
```

#### 6. Background Jobs Integration
```csharp
// ✅ ĐÚNG: Tích hợp với background jobs cho long-running tasks
ShellScope.AddDeferredTask(async scope =>
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<MyClass>>();
    
    try
    {
        // QUY ĐỊNH: Quick setup trong deferred task
        var myService = scope.ServiceProvider.GetRequiredService<IMyModuleService>();
        await myService.QuickSetupAsync();
        
        // QUY ĐỊNH: Long-running tasks qua background jobs
        var backgroundJobService = scope.ServiceProvider.GetRequiredService<IBackgroundJobService>();
        
        // Schedule heavy operations
        backgroundJobService.Schedule("MyModule.IndexRebuild", TimeSpan.FromMinutes(1));
        backgroundJobService.Schedule("MyModule.DataMigration", TimeSpan.FromMinutes(2));
        backgroundJobService.Schedule("MyModule.CacheWarmup", TimeSpan.FromMinutes(5));
        
        logger.LogInformation("Quick setup completed, background jobs scheduled");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error in deferred task with background jobs");
    }
});

// Background job implementations
[BackgroundJob("MyModule.IndexRebuild")]
public class IndexRebuildJob : IBackgroundJob
{
    public async Task ExecuteAsync(BackgroundJobContext context)
    {
        var indexingService = context.ServiceProvider.GetRequiredService<IIndexingService>();
        await indexingService.RebuildAllIndexesAsync();
    }
}

// ❌ SAI: Long-running tasks trong deferred task
ShellScope.AddDeferredTask(async scope =>
{
    // NGUY HIỂM: Long-running operations có thể timeout
    var indexingService = scope.ServiceProvider.GetRequiredService<IIndexingService>();
    await indexingService.RebuildAllIndexesAsync();  // Có thể mất hàng giờ
    
    var migrationService = scope.ServiceProvider.GetRequiredService<IMigrationService>();
    await migrationService.MigrateAllDataAsync();  // Có thể mất hàng giờ
});
```

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