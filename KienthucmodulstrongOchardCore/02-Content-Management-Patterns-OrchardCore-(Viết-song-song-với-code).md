# So Sánh Cách Viết Đúng vs Sai Và Ví Dụ Test Thực Tế

## Mục Lục
1. [Bảng So Sánh Tổng Quan](#bảng-so-sánh-tổng-quan)
2. [So Sánh Chi Tiết Từng Pattern](#so-sánh-chi-tiết-từng-pattern)
3. [Ví Dụ Module Test Thực Tế](#ví-dụ-module-test-thực-tế)
4. [Test Cases Và Validation](#test-cases-và-validation)
5. [Deployment Guide](#deployment-guide)

---

## Bảng So Sánh Tổng Quan

| Aspect | ✅ ĐÚNG (OrchardCore Standards) | ❌ SAI (Anti-patterns) | 🔥 Hậu Quả |
|--------|--------------------------------|------------------------|-------------|
| **Naming** | `MyCompanyMyModuleFeatureEventHandler` | `Handler`, `EventHandler` | Conflict, không rõ ràng |
| **Namespace** | `MyCompany.MyModule.Services` | `MyModule.Stuff` | Không professional |
| **Inheritance** | `FeatureEventHandler` (abstract) | `IFeatureEventHandler` (interface) | Phải implement tất cả methods |
| **Feature Check** | `if (feature.Id != "MyModule")` | Không check | Chạy cho tất cả features |
| **Heavy Operations** | `ShellScope.AddDeferredTask` | Direct execution | Timeout, deadlock |
| **Service Resolution** | `scope.ServiceProvider.GetRequiredService` | Injected services | Disposed services |
| **Error Handling** | Try-catch, log, không throw | Throw exceptions | Crash application |
| **Migration Methods** | `CreateAsync()`, `UpdateFrom1Async()` | `Create()`, `Update()` | Không async |
| **Recipe Steps** | `NamedRecipeStepHandler` | `IRecipeStepHandler` | Phức tạp hơn |
| **DI Registration** | Đúng lifetime (Scoped/Transient) | Sai lifetime | Memory leaks |

---

## So Sánh Chi Tiết Từng Pattern

### 1. Feature Event Handlers

#### ✅ CÁCH VIẾT ĐÚNG
```csharp
namespace MyCompany.BlogModule.Services
{
    public class BlogModuleFeatureEventHandler : FeatureEventHandler
    {
        private readonly ILogger<BlogModuleFeatureEventHandler> _logger;
        
        public BlogModuleFeatureEventHandler(ILogger<BlogModuleFeatureEventHandler> logger)
        {
            _logger = logger;
        }
        
        public override Task EnabledAsync(IFeatureInfo feature)
        {
            // ✅ LUÔN kiểm tra feature ID
            if (feature.Id != "MyCompany.BlogModule")
                return Task.CompletedTask;
                
            _logger.LogInformation("BlogModule feature enabled, starting initialization...");
            
            // ✅ Heavy operations trong deferred tasks
            ShellScope.AddDeferredTask(async scope =>
            {
                try
                {
                    var blogService = scope.ServiceProvider.GetRequiredService<IBlogService>();
                    var featuresManager = scope.ServiceProvider.GetRequiredService<IShellFeaturesManager>();
                    var logger = scope.ServiceProvider.GetRequiredService<ILogger<BlogModuleFeatureEventHandler>>();
                    
                    // ✅ Kiểm tra dependencies
                    var hasContentTypes = await featuresManager.IsFeatureEnabledAsync("OrchardCore.ContentTypes");
                    if (!hasContentTypes)
                    {
                        logger.LogWarning("ContentTypes feature not enabled, skipping blog setup");
                        return;
                    }
                    
                    // ✅ Idempotent operations
                    if (!await blogService.IsInitializedAsync())
                    {
                        await blogService.CreateDefaultCategoriesAsync();
                        await blogService.CreateSamplePostAsync();
                        await blogService.MarkAsInitializedAsync();
                        
                        logger.LogInformation("BlogModule initialized successfully");
                    }
                    else
                    {
                        logger.LogInformation("BlogModule already initialized, skipping setup");
                    }
                }
                catch (Exception ex)
                {
                    var logger = scope.ServiceProvider.GetRequiredService<ILogger<BlogModuleFeatureEventHandler>>();
                    logger.LogError(ex, "Failed to initialize BlogModule");
                    // ✅ KHÔNG throw exception
                }
            });
            
            return Task.CompletedTask;
        }
        
        public override Task DisabledAsync(IFeatureInfo feature)
        {
            if (feature.Id != "MyCompany.BlogModule")
                return Task.CompletedTask;
                
            _logger.LogInformation("BlogModule feature disabled, cleaning up...");
            
            ShellScope.AddDeferredTask(async scope =>
            {
                try
                {
                    var blogService = scope.ServiceProvider.GetRequiredService<IBlogService>();
                    await blogService.CleanupAsync();
                    
                    var logger = scope.ServiceProvider.GetRequiredService<ILogger<BlogModuleFeatureEventHandler>>();
                    logger.LogInformation("BlogModule cleanup completed");
                }
                catch (Exception ex)
                {
                    var logger = scope.ServiceProvider.GetRequiredService<ILogger<BlogModuleFeatureEventHandler>>();
                    logger.LogError(ex, "Failed to cleanup BlogModule");
                }
            });
            
            return Task.CompletedTask;
        }
    }
}
```

#### ❌ CÁCH VIẾT SAI
```csharp
namespace BlogModule
{
    // ❌ Tên class không rõ ràng
    public class Handler : IFeatureEventHandler
    {
        private readonly IBlogService _blogService; // ❌ Injected service
        
        public Handler(IBlogService blogService)
        {
            _blogService = blogService;
        }
        
        // ❌ Phải implement tất cả methods
        public Task InstallingAsync(IFeatureInfo feature) => Task.CompletedTask;
        public Task InstalledAsync(IFeatureInfo feature) => Task.CompletedTask;
        public Task EnablingAsync(IFeatureInfo feature) => Task.CompletedTask;
        public Task DisablingAsync(IFeatureInfo feature) => Task.CompletedTask;
        public Task UninstallingAsync(IFeatureInfo feature) => Task.CompletedTask;
        public Task UninstalledAsync(IFeatureInfo feature) => Task.CompletedTask;
        
        public async Task EnabledAsync(IFeatureInfo feature)
        {
            // ❌ KHÔNG kiểm tra feature ID - chạy cho tất cả features
            
            // ❌ Heavy operations trực tiếp - blocking main thread
            await _blogService.CreateDefaultCategoriesAsync();
            await _blogService.CreateSamplePostAsync();
            
            // ❌ Có thể throw exception và crash application
        }
        
        public Task DisabledAsync(IFeatureInfo feature)
        {
            // ❌ Sử dụng injected service trong deferred task
            ShellScope.AddDeferredTask(async scope =>
            {
                await _blogService.CleanupAsync(); // Service có thể đã disposed
            });
            
            return Task.CompletedTask;
        }
    }
}
```

### 2. Data Migrations

#### ✅ CÁCH VIẾT ĐÚNG
```csharp
namespace MyCompany.BlogModule.Migrations
{
    public sealed class BlogModuleMigrations : DataMigration
    {
        private readonly IContentDefinitionManager _contentDefinitionManager;
        private readonly IRecipeMigrator _recipeMigrator;
        private readonly ILogger<BlogModuleMigrations> _logger;
        
        public BlogModuleMigrations(
            IContentDefinitionManager contentDefinitionManager,
            IRecipeMigrator recipeMigrator,
            ILogger<BlogModuleMigrations> logger)
        {
            _contentDefinitionManager = contentDefinitionManager;
            _recipeMigrator = recipeMigrator;
            _logger = logger;
        }
        
        public async Task<int> CreateAsync()
        {
            _logger.LogInformation("Creating BlogModule database schema...");
            
            try
            {
                // ✅ Tạo database schema với proper column lengths
                await SchemaBuilder.CreateMapIndexTableAsync<BlogPostIndex>(table => table
                    .Column<string>("Title", column => column.WithLength(255))
                    .Column<string>("ContentItemId", column => column.WithLength(26))
                    .Column<DateTime>("PublishedUtc")
                    .Column<bool>("IsPublished")
                    .Column<string>("Category", column => column.WithLength(100))
                );
                
                // ✅ Tạo indexes cho performance
                await SchemaBuilder.AlterIndexTableAsync<BlogPostIndex>(table => table
                    .CreateIndex("IDX_BlogPost_Title", "Title")
                    .CreateIndex("IDX_BlogPost_Published", "PublishedUtc", "IsPublished")
                    .CreateIndex("IDX_BlogPost_Category", "Category")
                    .CreateIndex("IDX_BlogPost_ContentItemId", "ContentItemId")
                );
                
                // ✅ Content type definitions
                await _contentDefinitionManager.AlterTypeDefinitionAsync("BlogPost", type => type
                    .DisplayedAs("Blog Post")
                    .Creatable()
                    .Draftable()
                    .Versionable()
                    .Listable()
                    .Securable()
                    .WithPart("TitlePart")
                    .WithPart("AutoroutePart", part => part
                        .WithSettings(new AutoroutePartSettings
                        {
                            Pattern = "/blog/{{ Model.ContentItem | display_text | slugify }}",
                            AllowCustomPath = true
                        }))
                    .WithPart("HtmlBodyPart")
                    .WithPart("BlogPostPart")
                );
                
                await _contentDefinitionManager.AlterPartDefinitionAsync("BlogPostPart", part => part
                    .Attachable()
                    .WithDescription("Provides blog post specific functionality")
                    .WithField("Category", field => field
                        .OfType("TextField")
                        .WithDisplayName("Category")
                        .WithSettings(new TextFieldSettings { Hint = "Blog post category" }))
                    .WithField("Tags", field => field
                        .OfType("TextField")
                        .WithDisplayName("Tags")
                        .WithSettings(new TextFieldSettings { Hint = "Comma-separated tags" }))
                );
                
                // ✅ Execute setup recipe
                await _recipeMigrator.ExecuteAsync($"blog-setup{RecipesConstants.RecipeExtension}", this);
                
                // ✅ Heavy operations trong deferred tasks
                ShellScope.AddDeferredTask(async scope =>
                {
                    try
                    {
                        var blogService = scope.ServiceProvider.GetRequiredService<IBlogService>();
                        var logger = scope.ServiceProvider.GetRequiredService<ILogger<BlogModuleMigrations>>();
                        
                        // Tạo default categories
                        await blogService.CreateDefaultCategoriesAsync();
                        
                        // Tạo sample blog post
                        await blogService.CreateSamplePostAsync();
                        
                        // Cấu hình settings
                        var siteService = scope.ServiceProvider.GetRequiredService<ISiteService>();
                        var site = await siteService.LoadSiteSettingsAsync();
                        site.Put(new BlogSettings 
                        { 
                            PostsPerPage = 10,
                            AllowComments = true,
                            RequireApproval = false
                        });
                        await siteService.UpdateSiteSettingsAsync(site);
                        
                        logger.LogInformation("BlogModule default data created successfully");
                    }
                    catch (Exception ex)
                    {
                        var logger = scope.ServiceProvider.GetRequiredService<ILogger<BlogModuleMigrations>>();
                        logger.LogError(ex, "Failed to create BlogModule default data");
                        // ✅ Không throw - để migration tiếp tục
                    }
                });
                
                _logger.LogInformation("BlogModule database schema created successfully");
                return 1;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create BlogModule database schema");
                throw; // ✅ Re-throw để migration framework xử lý
            }
        }
        
        public async Task<int> UpdateFrom1Async()
        {
            _logger.LogInformation("Updating BlogModule from version 1 to 2...");
            
            try
            {
                // ✅ Schema changes
                await SchemaBuilder.AlterIndexTableAsync<BlogPostIndex>(table => table
                    .AddColumn<string>("Excerpt", column => column.WithLength(500))
                    .AddColumn<int>("ViewCount", column => column.WithDefault(0))
                );
                
                // ✅ Update content type definition
                await _contentDefinitionManager.AlterPartDefinitionAsync("BlogPostPart", part => part
                    .WithField("Excerpt", field => field
                        .OfType("TextField")
                        .WithDisplayName("Excerpt")
                        .WithSettings(new TextFieldSettings 
                        { 
                            Hint = "Short description of the blog post",
                            Required = false
                        }))
                );
                
                // ✅ Data migration trong deferred task
                ShellScope.AddDeferredTask(async scope =>
                {
                    try
                    {
                        var blogService = scope.ServiceProvider.GetRequiredService<IBlogService>();
                        await blogService.MigrateDataFromV1ToV2Async();
                        
                        var logger = scope.ServiceProvider.GetRequiredService<ILogger<BlogModuleMigrations>>();
                        logger.LogInformation("BlogModule data migration from v1 to v2 completed");
                    }
                    catch (Exception ex)
                    {
                        var logger = scope.ServiceProvider.GetRequiredService<ILogger<BlogModuleMigrations>>();
                        logger.LogError(ex, "Failed to migrate BlogModule data from v1 to v2");
                    }
                });
                
                _logger.LogInformation("BlogModule updated to version 2 successfully");
                return 2;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update BlogModule from version 1 to 2");
                throw;
            }
        }
    }
}
```

#### ❌ CÁCH VIẾT SAI
```csharp
namespace BlogModule
{
    // ❌ Không sealed, tên không rõ ràng
    public class Migration : DataMigration
    {
        // ❌ Không có dependencies injection
        
        // ❌ Không async, không error handling
        public int Create()
        {
            // ❌ Không specify column lengths
            SchemaBuilder.CreateMapIndexTable<BlogPostIndex>(table => table
                .Column<string>("Title")  // Có thể gây lỗi với long titles
                .Column<DateTime>("PublishedUtc")
            );
            
            // ❌ Không tạo indexes - performance kém
            
            // ❌ Heavy operations trong main thread
            var contentManager = GetService<IContentManager>(); // Service locator anti-pattern
            var defaultPost = contentManager.NewAsync("BlogPost").Result;
            contentManager.CreateAsync(defaultPost).Wait(); // Blocking calls
            
            return 1;
        }
        
        // ❌ Không có update methods - không thể upgrade
    }
}
```

### 3. Recipe Steps

#### ✅ CÁCH VIẾT ĐÚNG
```csharp
namespace MyCompany.BlogModule.Recipes
{
    public sealed class BlogSetupRecipeStep : NamedRecipeStepHandler
    {
        private readonly IBlogService _blogService;
        private readonly IContentManager _contentManager;
        private readonly ILogger<BlogSetupRecipeStep> _logger;
        
        public BlogSetupRecipeStep(
            IBlogService blogService,
            IContentManager contentManager,
            ILogger<BlogSetupRecipeStep> logger) 
            : base("BlogSetup") // ✅ Explicit step name
        {
            _blogService = blogService;
            _contentManager = contentManager;
            _logger = logger;
        }
        
        protected override async Task HandleAsync(RecipeExecutionContext context)
        {
            try
            {
                _logger.LogInformation("Executing BlogSetup recipe step...");
                
                // ✅ Proper deserialization với error handling
                var stepModel = context.Step.ToObject<BlogSetupStepModel>();
                
                if (stepModel == null)
                {
                    _logger.LogWarning("BlogSetup step data is null or invalid");
                    return;
                }
                
                // ✅ Validation
                if (!ValidateStepModel(stepModel, context))
                    return;
                
                // ✅ Execute step logic
                await ExecuteStepAsync(stepModel, context);
                
                _logger.LogInformation("BlogSetup recipe step completed successfully");
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to deserialize BlogSetup step data");
                context.Errors.Add("BlogSetup.Deserialization", ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing BlogSetup step");
                context.Errors.Add("BlogSetup.Execution", ex.Message);
            }
        }
        
        private bool ValidateStepModel(BlogSetupStepModel model, RecipeExecutionContext context)
        {
            var errors = new List<string>();
            
            if (model.Categories == null || !model.Categories.Any())
                errors.Add("At least one category is required");
                
            if (model.SamplePosts != null)
            {
                foreach (var post in model.SamplePosts)
                {
                    if (string.IsNullOrWhiteSpace(post.Title))
                        errors.Add($"Sample post title is required");
                        
                    if (string.IsNullOrWhiteSpace(post.Content))
                        errors.Add($"Sample post content is required for '{post.Title}'");
                }
            }
            
            if (errors.Any())
            {
                var errorMessage = string.Join("; ", errors);
                _logger.LogError("BlogSetup step validation failed: {Errors}", errorMessage);
                context.Errors.Add("BlogSetup.Validation", errorMessage);
                return false;
            }
            
            return true;
        }
        
        private async Task ExecuteStepAsync(BlogSetupStepModel model, RecipeExecutionContext context)
        {
            // Create categories
            foreach (var category in model.Categories)
            {
                await _blogService.CreateCategoryAsync(category.Name, category.Description);
                _logger.LogInformation("Created blog category: {CategoryName}", category.Name);
            }
            
            // Create sample posts
            if (model.SamplePosts?.Any() == true)
            {
                foreach (var samplePost in model.SamplePosts)
                {
                    var blogPost = await _contentManager.NewAsync("BlogPost");
                    
                    blogPost.Alter<TitlePart>(part => part.Title = samplePost.Title);
                    blogPost.Alter<HtmlBodyPart>(part => part.Html = samplePost.Content);
                    blogPost.Alter<AutoroutePart>(part => part.Path = $"/blog/{samplePost.Title.ToSlug()}");
                    
                    if (!string.IsNullOrWhiteSpace(samplePost.Category))
                    {
                        blogPost.Alter<BlogPostPart>(part => part.Category.Text = samplePost.Category);
                    }
                    
                    await _contentManager.CreateAsync(blogPost);
                    
                    if (samplePost.Published)
                    {
                        await _contentManager.PublishAsync(blogPost);
                    }
                    
                    _logger.LogInformation("Created sample blog post: {PostTitle}", samplePost.Title);
                }
            }
            
            // Configure settings
            if (model.Settings != null)
            {
                await _blogService.UpdateSettingsAsync(model.Settings);
                _logger.LogInformation("Updated blog settings");
            }
        }
    }
    
    // ✅ Strongly typed step model
    public class BlogSetupStepModel
    {
        public IEnumerable<CategoryModel> Categories { get; set; } = new List<CategoryModel>();
        public IEnumerable<SamplePostModel> SamplePosts { get; set; } = new List<SamplePostModel>();
        public BlogSettingsModel Settings { get; set; }
    }
    
    public class CategoryModel
    {
        public string Name { get; set; }
        public string Description { get; set; }
    }
    
    public class SamplePostModel
    {
        public string Title { get; set; }
        public string Content { get; set; }
        public string Category { get; set; }
        public bool Published { get; set; } = true;
    }
    
    public class BlogSettingsModel
    {
        public int PostsPerPage { get; set; } = 10;
        public bool AllowComments { get; set; } = true;
        public bool RequireApproval { get; set; } = false;
    }
}
```

#### ❌ CÁCH VIẾT SAI
```csharp
namespace BlogModule
{
    // ❌ Implement interface trực tiếp
    public class BlogRecipe : IRecipeStepHandler
    {
        public string Name => "BlogSetup"; // ❌ Hard-coded name
        
        private readonly IBlogService _blogService;
        
        public BlogRecipe(IBlogService blogService)
        {
            _blogService = blogService;
        }
        
        public async Task ExecuteAsync(RecipeExecutionContext context)
        {
            // ❌ Không có error handling
            var data = context.Step.ToObject<object>(); // ❌ Không strongly typed
            
            // ❌ Không validation
            
            // ❌ Hard-coded logic
            await _blogService.CreateCategoryAsync("Technology", "Tech posts");
            await _blogService.CreateCategoryAsync("Lifestyle", "Life posts");
            
            // ❌ Có thể throw exception và crash recipe execution
        }
    }
}
```

---

## Ví Dụ Module Test Thực Tế

### Cấu Trúc Project Hoàn Chỉnh

```
MyCompany.BlogModule/
├── Manifest.cs
├── Startup.cs
├── MyCompany.BlogModule.csproj
├── Services/
│   ├── IBlogService.cs
│   ├── BlogService.cs
│   └── BlogModuleFeatureEventHandler.cs
├── Models/
│   ├── BlogPost.cs
│   ├── BlogSettings.cs
│   └── BlogPostPart.cs
├── Migrations/
│   └── BlogModuleMigrations.cs
├── Recipes/
│   ├── BlogSetupRecipeStep.cs
│   └── blog-setup.recipe.json
├── Indexes/
│   └── BlogPostIndex.cs
├── Drivers/
│   └── BlogPostPartDisplayDriver.cs
├── Handlers/
│   └── BlogPostPartHandler.cs
├── Controllers/
│   └── BlogController.cs
├── ViewModels/
│   ├── BlogPostViewModel.cs
│   └── BlogSettingsViewModel.cs
├── Views/
│   ├── Blog/
│   │   ├── Index.cshtml
│   │   └── Details.cshtml
│   └── Parts/
│       └── BlogPost.cshtml
└── wwwroot/
    ├── css/
    │   └── blog.css
    └── js/
        └── blog.js
```

### 1. Manifest.cs
```csharp
using OrchardCore.Modules.Manifest;

[assembly: Module(
    Name = "Blog Module",
    Author = "My Company",
    Website = "https://mycompany.com",
    Version = "1.0.0",
    Description = "A comprehensive blog module for OrchardCore",
    Category = "Content Management",
    Dependencies = new[] 
    { 
        "OrchardCore.Contents",
        "OrchardCore.ContentTypes",
        "OrchardCore.Autoroute",
        "OrchardCore.Html",
        "OrchardCore.Title"
    }
)]

[assembly: Feature(
    Id = "MyCompany.BlogModule",
    Name = "Blog Module",
    Description = "Core blog functionality",
    Category = "Content Management"
)]

[assembly: Feature(
    Id = "MyCompany.BlogModule.Admin",
    Name = "Blog Module Admin",
    Description = "Admin interface for blog management",
    Category = "Content Management",
    Dependencies = new[] { "MyCompany.BlogModule", "OrchardCore.Admin" }
)]
```

### 2. Startup.cs
```csharp
using Microsoft.Extensions.DependencyInjection;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Display.ContentDisplay;
using OrchardCore.ContentManagement.Handlers;
using OrchardCore.Data.Migration;
using OrchardCore.Environment.Shell;
using OrchardCore.Modules;
using OrchardCore.Recipes;
using OrchardCore.Setup.Events;
using MyCompany.BlogModule.Drivers;
using MyCompany.BlogModule.Handlers;
using MyCompany.BlogModule.Indexes;
using MyCompany.BlogModule.Models;
using MyCompany.BlogModule.Recipes;
using MyCompany.BlogModule.Services;
using YesSql.Indexes;

namespace MyCompany.BlogModule
{
    public class Startup : StartupBase
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            // ✅ Core services
            services.AddScoped<IBlogService, BlogService>();
            
            // ✅ Content part
            services.AddContentPart<BlogPostPart>();
            
            // ✅ Display driver
            services.AddScoped<IContentPartDisplayDriver, BlogPostPartDisplayDriver>();
            
            // ✅ Content handler
            services.AddScoped<IContentPartHandler, BlogPostPartHandler>();
            
            // ✅ Index provider
            services.AddSingleton<IIndexProvider, BlogPostIndexProvider>();
            
            // ✅ Data migration
            services.AddScoped<IDataMigration, BlogModuleMigrations>();
            
            // ✅ Feature event handler
            services.AddScoped<IFeatureEventHandler, BlogModuleFeatureEventHandler>();
            
            // ✅ Recipe step handler
            services.AddTransient<IRecipeStepHandler, BlogSetupRecipeStep>();
        }
    }
    
    [Feature("MyCompany.BlogModule.Admin")]
    public class AdminStartup : StartupBase
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            // ✅ Admin-specific services
            services.AddScoped<ISetupEventHandler, BlogModuleSetupEventHandler>();
        }
    }
}
```

### 3. Models/BlogPostPart.cs
```csharp
using OrchardCore.ContentFields.Fields;
using OrchardCore.ContentManagement;

namespace MyCompany.BlogModule.Models
{
    public class BlogPostPart : ContentPart
    {
        public TextField Category { get; set; } = new();
        public TextField Tags { get; set; } = new();
        public TextField Excerpt { get; set; } = new();
        public BooleanField IsFeatured { get; set; } = new();
        public DateTimeField PublishedDate { get; set; } = new();
        public NumericField ViewCount { get; set; } = new();
    }
}
```

### 4. Services/IBlogService.cs
```csharp
using MyCompany.BlogModule.Models;
using OrchardCore.ContentManagement;

namespace MyCompany.BlogModule.Services
{
    public interface IBlogService
    {
        Task<bool> IsInitializedAsync();
        Task MarkAsInitializedAsync();
        Task CreateDefaultCategoriesAsync();
        Task CreateSamplePostAsync();
        Task CreateCategoryAsync(string name, string description);
        Task<IEnumerable<ContentItem>> GetBlogPostsAsync(int page = 1, int pageSize = 10);
        Task<IEnumerable<ContentItem>> GetBlogPostsByCategoryAsync(string category, int page = 1, int pageSize = 10);
        Task<ContentItem> GetBlogPostAsync(string slug);
        Task UpdateSettingsAsync(BlogSettings settings);
        Task<BlogSettings> GetSettingsAsync();
        Task CleanupAsync();
        Task MigrateDataFromV1ToV2Async();
    }
}
```

### 5. Services/BlogService.cs
```csharp
using Microsoft.Extensions.Logging;
using MyCompany.BlogModule.Indexes;
using MyCompany.BlogModule.Models;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Records;
using OrchardCore.Entities;
using OrchardCore.Settings;
using YesSql;

namespace MyCompany.BlogModule.Services
{
    public class BlogService : IBlogService
    {
        private readonly IContentManager _contentManager;
        private readonly ISiteService _siteService;
        private readonly ISession _session;
        private readonly ILogger<BlogService> _logger;
        
        public BlogService(
            IContentManager contentManager,
            ISiteService siteService,
            ISession session,
            ILogger<BlogService> logger)
        {
            _contentManager = contentManager;
            _siteService = siteService;
            _session = session;
            _logger = logger;
        }
        
        public async Task<bool> IsInitializedAsync()
        {
            var site = await _siteService.LoadSiteSettingsAsync();
            var settings = site.As<BlogSettings>();
            return settings.IsInitialized;
        }
        
        public async Task MarkAsInitializedAsync()
        {
            var site = await _siteService.LoadSiteSettingsAsync();
            var settings = site.As<BlogSettings>();
            settings.IsInitialized = true;
            site.Put(settings);
            await _siteService.UpdateSiteSettingsAsync(site);
        }
        
        public async Task CreateDefaultCategoriesAsync()
        {
            var categories = new[]
            {
                new { Name = "Technology", Description = "Posts about technology and programming" },
                new { Name = "Lifestyle", Description = "Posts about lifestyle and personal experiences" },
                new { Name = "Business", Description = "Posts about business and entrepreneurship" }
            };
            
            foreach (var category in categories)
            {
                await CreateCategoryAsync(category.Name, category.Description);
            }
            
            _logger.LogInformation("Created {CategoryCount} default blog categories", categories.Length);
        }
        
        public async Task CreateCategoryAsync(string name, string description)
        {
            // Check if category already exists
            var existingCategory = await _session.Query<ContentItem, ContentItemIndex>()
                .Where(x => x.ContentType == "BlogCategory" && x.DisplayText == name)
                .FirstOrDefaultAsync();
                
            if (existingCategory != null)
            {
                _logger.LogInformation("Blog category '{CategoryName}' already exists", name);
                return;
            }
            
            var category = await _contentManager.NewAsync("BlogCategory");
            category.DisplayText = name;
            category.Alter<BlogCategoryPart>(part => part.Description.Text = description);
            
            await _contentManager.CreateAsync(category);
            await _contentManager.PublishAsync(category);
            
            _logger.LogInformation("Created blog category: {CategoryName}", name);
        }
        
        public async Task CreateSamplePostAsync()
        {
            // Check if sample post already exists
            var existingSample = await _session.Query<ContentItem, ContentItemIndex>()
                .Where(x => x.ContentType == "BlogPost" && x.DisplayText.Contains("Welcome"))
                .FirstOrDefaultAsync();
                
            if (existingSample != null)
            {
                _logger.LogInformation("Sample blog post already exists");
                return;
            }
            
            var samplePost = await _contentManager.NewAsync("BlogPost");
            
            samplePost.Alter<TitlePart>(part => part.Title = "Welcome to Your New Blog");
            samplePost.Alter<HtmlBodyPart>(part => part.Html = @"
                <h2>Welcome to your new blog!</h2>
                <p>This is your first blog post. You can edit or delete this post and start creating your own content.</p>
                <p>Your blog is now ready to use with the following features:</p>
                <ul>
                    <li>Create and manage blog posts</li>
                    <li>Organize posts with categories</li>
                    <li>SEO-friendly URLs</li>
                    <li>Rich text editing</li>
                    <li>Responsive design</li>
                </ul>
                <p>Happy blogging!</p>
            ");
            
            samplePost.Alter<AutoroutePart>(part => part.Path = "/blog/welcome-to-your-new-blog");
            samplePost.Alter<BlogPostPart>(part => 
            {
                part.Category.Text = "Technology";
                part.Tags.Text = "welcome, blog, getting-started";
                part.Excerpt.Text = "Welcome to your new blog! This is your first blog post with all the features you need.";
                part.IsFeatured.Value = true;
                part.PublishedDate.Value = DateTime.UtcNow;
                part.ViewCount.Value = 0;
            });
            
            await _contentManager.CreateAsync(samplePost);
            await _contentManager.PublishAsync(samplePost);
            
            _logger.LogInformation("Created sample blog post: Welcome to Your New Blog");
        }
        
        public async Task<IEnumerable<ContentItem>> GetBlogPostsAsync(int page = 1, int pageSize = 10)
        {
            var blogPosts = await _session.Query<ContentItem, BlogPostIndex>()
                .Where(x => x.IsPublished)
                .OrderByDescending(x => x.PublishedUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ListAsync();
                
            return blogPosts;
        }
        
        public async Task<IEnumerable<ContentItem>> GetBlogPostsByCategoryAsync(string category, int page = 1, int pageSize = 10)
        {
            var blogPosts = await _session.Query<ContentItem, BlogPostIndex>()
                .Where(x => x.IsPublished && x.Category == category)
                .OrderByDescending(x => x.PublishedUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ListAsync();
                
            return blogPosts;
        }
        
        public async Task<ContentItem> GetBlogPostAsync(string slug)
        {
            var blogPost = await _session.Query<ContentItem, AutoroutePartIndex>()
                .Where(x => x.Path == $"/blog/{slug}")
                .FirstOrDefaultAsync();
                
            return blogPost;
        }
        
        public async Task UpdateSettingsAsync(BlogSettings settings)
        {
            var site = await _siteService.LoadSiteSettingsAsync();
            site.Put(settings);
            await _siteService.UpdateSiteSettingsAsync(site);
            
            _logger.LogInformation("Updated blog settings");
        }
        
        public async Task<BlogSettings> GetSettingsAsync()
        {
            var site = await _siteService.LoadSiteSettingsAsync();
            return site.As<BlogSettings>();
        }
        
        public async Task CleanupAsync()
        {
            // Cleanup logic when feature is disabled
            _logger.LogInformation("BlogModule cleanup completed");
        }
        
        public async Task MigrateDataFromV1ToV2Async()
        {
            // Data migration logic from version 1 to 2
            var blogPosts = await _session.Query<ContentItem, ContentItemIndex>()
                .Where(x => x.ContentType == "BlogPost")
                .ListAsync();
                
            foreach (var post in blogPosts)
            {
                // Migrate data structure
                post.Alter<BlogPostPart>(part =>
                {
                    if (part.ViewCount.Value == null)
                        part.ViewCount.Value = 0;
                        
                    if (string.IsNullOrEmpty(part.Excerpt.Text) && post.Has<HtmlBodyPart>())
                    {
                        var body = post.As<HtmlBodyPart>().Html;
                        part.Excerpt.Text = body?.Length > 200 ? body.Substring(0, 200) + "..." : body;
                    }
                });
                
                await _contentManager.UpdateAsync(post);
            }
            
            _logger.LogInformation("Migrated {PostCount} blog posts from v1 to v2", blogPosts.Count());
        }
    }
}
```

### 6. Indexes/BlogPostIndex.cs
```csharp
using MyCompany.BlogModule.Models;
using OrchardCore.ContentManagement;
using YesSql.Indexes;

namespace MyCompany.BlogModule.Indexes
{
    public class BlogPostIndex : MapIndex
    {
        public string Title { get; set; }
        public string ContentItemId { get; set; }
        public DateTime? PublishedUtc { get; set; }
        public bool IsPublished { get; set; }
        public string Category { get; set; }
        public string Tags { get; set; }
        public string Excerpt { get; set; }
        public bool IsFeatured { get; set; }
        public int ViewCount { get; set; }
    }
    
    public class BlogPostIndexProvider : IndexProvider<ContentItem>
    {
        public override void Describe(DescribeContext<ContentItem> context)
        {
            context.For<BlogPostIndex>()
                .Map(contentItem =>
                {
                    if (contentItem.ContentType != "BlogPost")
                        return null;
                        
                    var blogPostPart = contentItem.As<BlogPostPart>();
                    var titlePart = contentItem.As<TitlePart>();
                    
                    return new BlogPostIndex
                    {
                        Title = titlePart?.Title,
                        ContentItemId = contentItem.ContentItemId,
                        PublishedUtc = contentItem.PublishedUtc,
                        IsPublished = contentItem.Published,
                        Category = blogPostPart?.Category?.Text,
                        Tags = blogPostPart?.Tags?.Text,
                        Excerpt = blogPostPart?.Excerpt?.Text,
                        IsFeatured = blogPostPart?.IsFeatured?.Value ?? false,
                        ViewCount = (int)(blogPostPart?.ViewCount?.Value ?? 0)
                    };
                });
        }
    }
}
```

### 7. Recipe File: blog-setup.recipe.json
```json
{
  "name": "BlogModuleSetup",
  "displayName": "Blog Module Setup",
  "description": "Sets up the blog module with default configuration",
  "author": "My Company",
  "website": "https://mycompany.com",
  "version": "1.0.0",
  "issetuprecipe": false,
  "categories": ["blog", "content"],
  "tags": ["blog", "content", "automation"],
  
  "variables": {
    "welcomePostId": "[js:uuid()]",
    "categoryTaxonomyId": "[js:uuid()]"
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
        "MyCompany.BlogModule"
      ]
    },
    {
      "name": "ContentDefinition",
      "ContentTypes": [
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
              "PartName": "BlogCategoryPart",
              "Name": "BlogCategoryPart",
              "Settings": {
                "ContentTypePartSettings": {
                  "Position": "1"
                }
              }
            }
          ]
        }
      ],
      "ContentParts": [
        {
          "Name": "BlogCategoryPart",
          "Settings": {
            "ContentPartSettings": {
              "Attachable": true,
              "Description": "Provides blog category functionality"
            }
          },
          "ContentPartFieldDefinitionRecords": [
            {
              "FieldName": "TextField",
              "Name": "Description",
              "Settings": {
                "ContentPartFieldSettings": {
                  "DisplayName": "Description",
                  "Position": "0"
                },
                "TextFieldSettings": {
                  "Hint": "Category description"
                }
              }
            }
          ]
        }
      ]
    },
    {
      "name": "BlogSetup",
      "Categories": [
        {
          "Name": "Technology",
          "Description": "Posts about technology and programming"
        },
        {
          "Name": "Lifestyle", 
          "Description": "Posts about lifestyle and personal experiences"
        },
        {
          "Name": "Business",
          "Description": "Posts about business and entrepreneurship"
        }
      ],
      "SamplePosts": [
        {
          "Title": "Welcome to Your New Blog",
          "Content": "<h2>Welcome to your new blog!</h2><p>This is your first blog post. You can edit or delete this post and start creating your own content.</p>",
          "Category": "Technology",
          "Published": true
        }
      ],
      "Settings": {
        "PostsPerPage": 10,
        "AllowComments": true,
        "RequireApproval": false
      }
    }
  ]
}
```

---

## Test Cases Và Validation

### 1. Unit Tests
```csharp
using Microsoft.Extensions.Logging;
using Moq;
using MyCompany.BlogModule.Services;
using OrchardCore.ContentManagement;
using OrchardCore.Settings;
using Xunit;
using YesSql;

namespace MyCompany.BlogModule.Tests.Services
{
    public class BlogServiceTests
    {
        private readonly Mock<IContentManager> _contentManagerMock;
        private readonly Mock<ISiteService> _siteServiceMock;
        private readonly Mock<ISession> _sessionMock;
        private readonly Mock<ILogger<BlogService>> _loggerMock;
        private readonly BlogService _blogService;
        
        public BlogServiceTests()
        {
            _contentManagerMock = new Mock<IContentManager>();
            _siteServiceMock = new Mock<ISiteService>();
            _sessionMock = new Mock<ISession>();
            _loggerMock = new Mock<ILogger<BlogService>>();
            
            _blogService = new BlogService(
                _contentManagerMock.Object,
                _siteServiceMock.Object,
                _sessionMock.Object,
                _loggerMock.Object);
        }
        
        [Fact]
        public async Task IsInitializedAsync_WhenNotInitialized_ReturnsFalse()
        {
            // Arrange
            var site = new SiteSettings();
            var settings = new BlogSettings { IsInitialized = false };
            site.Put(settings);
            
            _siteServiceMock.Setup(x => x.LoadSiteSettingsAsync())
                .ReturnsAsync(site);
            
            // Act
            var result = await _blogService.IsInitializedAsync();
            
            // Assert
            Assert.False(result);
        }
        
        [Fact]
        public async Task MarkAsInitializedAsync_SetsInitializedToTrue()
        {
            // Arrange
            var site = new SiteSettings();
            var settings = new BlogSettings { IsInitialized = false };
            site.Put(settings);
            
            _siteServiceMock.Setup(x => x.LoadSiteSettingsAsync())
                .ReturnsAsync(site);
            
            // Act
            await _blogService.MarkAsInitializedAsync();
            
            // Assert
            var updatedSettings = site.As<BlogSettings>();
            Assert.True(updatedSettings.IsInitialized);
            
            _siteServiceMock.Verify(x => x.UpdateSiteSettingsAsync(site), Times.Once);
        }
        
        [Fact]
        public async Task CreateCategoryAsync_WhenCategoryExists_DoesNotCreateDuplicate()
        {
            // Arrange
            var existingCategory = new ContentItem { ContentType = "BlogCategory", DisplayText = "Technology" };
            
            _sessionMock.Setup(x => x.Query<ContentItem, ContentItemIndex>())
                .Returns(new Mock<IQuery<ContentItem, ContentItemIndex>>().Object);
                
            // Mock the query chain
            var queryMock = new Mock<IQuery<ContentItem, ContentItemIndex>>();
            queryMock.Setup(x => x.Where(It.IsAny<Expression<Func<ContentItemIndex, bool>>>()))
                .Returns(queryMock.Object);
            queryMock.Setup(x => x.FirstOrDefaultAsync())
                .ReturnsAsync(existingCategory);
                
            _sessionMock.Setup(x => x.Query<ContentItem, ContentItemIndex>())
                .Returns(queryMock.Object);
            
            // Act
            await _blogService.CreateCategoryAsync("Technology", "Tech posts");
            
            // Assert
            _contentManagerMock.Verify(x => x.NewAsync("BlogCategory"), Times.Never);
            _contentManagerMock.Verify(x => x.CreateAsync(It.IsAny<ContentItem>()), Times.Never);
        }
    }
}
```

### 2. Integration Tests
```csharp
using Microsoft.Extensions.DependencyInjection;
using MyCompany.BlogModule.Services;
using OrchardCore.Environment.Shell.Scope;
using OrchardCore.Tests;
using Xunit;

namespace MyCompany.BlogModule.Tests.Integration
{
    public class BlogModuleIntegrationTests : OrchardCoreTestBase
    {
        [Fact]
        public async Task BlogModule_WhenEnabled_CreatesDefaultData()
        {
            // Arrange
            await using var context = await CreateShellContextAsync();
            await using var scope = await context.CreateScopeAsync();
            
            var blogService = scope.ServiceProvider.GetRequiredService<IBlogService>();
            
            // Act
            await blogService.CreateDefaultCategoriesAsync();
            await blogService.CreateSamplePostAsync();
            await blogService.MarkAsInitializedAsync();
            
            // Assert
            var isInitialized = await blogService.IsInitializedAsync();
            Assert.True(isInitialized);
            
            var blogPosts = await blogService.GetBlogPostsAsync();
            Assert.NotEmpty(blogPosts);
        }
        
        [Fact]
        public async Task BlogModule_Recipe_ExecutesSuccessfully()
        {
            // Arrange
            await using var context = await CreateShellContextAsync();
            await using var scope = await context.CreateScopeAsync();
            
            var recipeExecutor = scope.ServiceProvider.GetRequiredService<IRecipeExecutor>();
            
            // Act
            var result = await recipeExecutor.ExecuteAsync("blog-setup", new Dictionary<string, object>());
            
            // Assert
            Assert.True(result.IsSuccessful);
            Assert.Empty(result.Errors);
        }
    }
}
```

### 3. Feature Event Handler Tests
```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using MyCompany.BlogModule.Services;
using OrchardCore.Environment.Extensions.Features;
using OrchardCore.Environment.Shell.Scope;
using Xunit;

namespace MyCompany.BlogModule.Tests.Services
{
    public class BlogModuleFeatureEventHandlerTests
    {
        [Fact]
        public async Task EnabledAsync_WithCorrectFeature_InitializesBlog()
        {
            // Arrange
            var loggerMock = new Mock<ILogger<BlogModuleFeatureEventHandler>>();
            var handler = new BlogModuleFeatureEventHandler(loggerMock.Object);
            
            var featureMock = new Mock<IFeatureInfo>();
            featureMock.Setup(x => x.Id).Returns("MyCompany.BlogModule");
            
            var blogServiceMock = new Mock<IBlogService>();
            blogServiceMock.Setup(x => x.IsInitializedAsync()).ReturnsAsync(false);
            
            var serviceProviderMock = new Mock<IServiceProvider>();
            serviceProviderMock.Setup(x => x.GetRequiredService<IBlogService>())
                .Returns(blogServiceMock.Object);
            serviceProviderMock.Setup(x => x.GetRequiredService<ILogger<BlogModuleFeatureEventHandler>>())
                .Returns(loggerMock.Object);
            
            var scopeMock = new Mock<IServiceScope>();
            scopeMock.Setup(x => x.ServiceProvider).Returns(serviceProviderMock.Object);
            
            // Mock ShellScope.AddDeferredTask
            var deferredTasks = new List<Func<IServiceScope, Task>>();
            
            // Act
            await handler.EnabledAsync(featureMock.Object);
            
            // Assert
            // Verify that deferred task was added (this would require mocking ShellScope)
            // In real implementation, you'd need to make ShellScope testable
        }
        
        [Fact]
        public async Task EnabledAsync_WithWrongFeature_DoesNothing()
        {
            // Arrange
            var loggerMock = new Mock<ILogger<BlogModuleFeatureEventHandler>>();
            var handler = new BlogModuleFeatureEventHandler(loggerMock.Object);
            
            var featureMock = new Mock<IFeatureInfo>();
            featureMock.Setup(x => x.Id).Returns("SomeOtherModule");
            
            // Act
            var result = await handler.EnabledAsync(featureMock.Object);
            
            // Assert
            Assert.Equal(Task.CompletedTask, result);
        }
    }
}
```

---

## Deployment Guide

### 1. Development Environment Setup
```bash
# Clone OrchardCore
git clone https://github.com/OrchardCMS/OrchardCore.git
cd OrchardCore

# Create module directory
mkdir src/OrchardCore.Modules/MyCompany.BlogModule
cd src/OrchardCore.Modules/MyCompany.BlogModule

# Initialize module structure
dotnet new classlib -n MyCompany.BlogModule
```

### 2. Project File Configuration
```xml
<!-- MyCompany.BlogModule.csproj -->
<Project Sdk="Microsoft.NET.Sdk.Razor">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <AddRazorSupportForMvc>true</AddRazorSupportForMvc>
    <GenerateEmbeddedFilesManifest>true</GenerateEmbeddedFilesManifest>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\OrchardCore\OrchardCore.Module.Targets\OrchardCore.Module.Targets.csproj" />
    <ProjectReference Include="..\..\OrchardCore\OrchardCore.ContentManagement\OrchardCore.ContentManagement.csproj" />
    <ProjectReference Include="..\..\OrchardCore\OrchardCore.ContentTypes.Abstractions\OrchardCore.ContentTypes.Abstractions.csproj" />
    <ProjectReference Include="..\..\OrchardCore\OrchardCore.Data\OrchardCore.Data.csproj" />
    <ProjectReference Include="..\..\OrchardCore\OrchardCore.DisplayManagement\OrchardCore.DisplayManagement.csproj" />
    <ProjectReference Include="..\..\OrchardCore\OrchardCore.Modules.Abstractions\OrchardCore.Modules.Abstractions.csproj" />
    <ProjectReference Include="..\..\OrchardCore\OrchardCore.Navigation.Core\OrchardCore.Navigation.Core.csproj" />
    <ProjectReference Include="..\..\OrchardCore\OrchardCore.Recipes.Abstractions\OrchardCore.Recipes.Abstractions.csproj" />
    <ProjectReference Include="..\..\OrchardCore\OrchardCore.Settings.Abstractions\OrchardCore.Settings.Abstractions.csproj" />
  </ItemGroup>

  <ItemGroup>
    <EmbeddedResource Include="Recipes\**\*.json" />
    <EmbeddedResource Include="wwwroot\**\*" />
  </ItemGroup>

</Project>
```

### 3. Testing Commands
```bash
# Build module
dotnet build

# Run tests
dotnet test

# Run OrchardCore with module
cd ../../../
dotnet run --project src/OrchardCore.Cms.Web

# Access admin
# Navigate to: https://localhost:5001/admin
# Enable "Blog Module" feature
```

### 4. Validation Checklist

#### ✅ Pre-Deployment Checklist
- [ ] All naming conventions followed
- [ ] Feature ID validation implemented
- [ ] Deferred tasks used for heavy operations
- [ ] Error handling implemented (no throwing in deferred tasks)
- [ ] Services resolved from scope in deferred tasks
- [ ] Migration methods are async
- [ ] Recipe steps have proper validation
- [ ] Unit tests pass
- [ ] Integration tests pass
- [ ] Manual testing completed

#### ✅ Runtime Validation
```csharp
// Add to your module for runtime validation
public class BlogModuleValidator
{
    public static ValidationResult ValidateModule()
    {
        var result = new ValidationResult();
        
        // Check naming conventions
        var handlerType = typeof(BlogModuleFeatureEventHandler);
        if (!handlerType.Name.EndsWith("FeatureEventHandler"))
        {
            result.Errors.Add("Feature event handler naming convention violated");
        }
        
        // Check namespace structure
        if (!handlerType.Namespace.Contains("Services"))
        {
            result.Errors.Add("Feature event handler should be in Services namespace");
        }
        
        // Check migration class
        var migrationType = typeof(BlogModuleMigrations);
        if (!migrationType.IsSealed)
        {
            result.Errors.Add("Migration class should be sealed");
        }
        
        return result;
    }
}

public class ValidationResult
{
    public List<string> Errors { get; set; } = new();
    public bool IsValid => !Errors.Any();
}
```

### 5. Performance Testing
```csharp
// Performance test for deferred tasks
[Fact]
public async Task DeferredTask_Performance_CompletesWithinTimeout()
{
    var stopwatch = Stopwatch.StartNew();
    
    // Simulate deferred task execution
    await SimulateDeferredTaskAsync();
    
    stopwatch.Stop();
    
    // Should complete within reasonable time
    Assert.True(stopwatch.ElapsedMilliseconds < 5000, 
        $"Deferred task took {stopwatch.ElapsedMilliseconds}ms, expected < 5000ms");
}
```

### 6. Production Deployment
```bash
# Build for production
dotnet publish -c Release

# Copy module to production OrchardCore
cp -r bin/Release/net8.0/publish/* /path/to/production/orchardcore/

# Restart application
systemctl restart orchardcore

# Enable feature via admin or recipe
```

Với hướng dẫn chi tiết này, anh có thể tạo ra các modules OrchardCore tuân thủ đầy đủ các quy định và standards, đảm bảo chất lượng code cao và hoạt động ổn định trong production.