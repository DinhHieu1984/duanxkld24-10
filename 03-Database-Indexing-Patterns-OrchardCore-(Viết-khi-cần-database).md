# Database và Tables trong OrchardCore - Hướng Dẫn Chi Tiết

## 🔴 TRÁCH NHIỆM QUAN TRỌNG: OrchardCore KHÔNG dùng database riêng cho từng module!

### Cơ Chế Hoạt Động của OrchardCore

OrchardCore sử dụng một **database duy nhất** cho toàn bộ application và tất cả modules. Đây là những điểm quan trọng:

## 1. Shared Database Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    OrchardCore Database                     │
├─────────────────────────────────────────────────────────────┤
│  Core Tables:                                               │
│  - Document (YesSql document store)                         │
│  - ContentItemIndex                                         │
│  - ContentItemVersionIndex                                  │
│  - UserIndex                                                │
│  - RoleIndex                                                │
│                                                             │
│  Module-specific Index Tables:                              │
│  - PublishLaterPartIndex                                    │
│  - TaxonomyIndex                                            │
│  - AutoroutePartIndex                                       │
│  - LuceneQueryIndex                                         │
│  - NotificationIndex                                        │
│  - ... (mỗi module tạo index tables riêng)                 │
└─────────────────────────────────────────────────────────────┘
```

## 2. YesSql Document Store Pattern

OrchardCore sử dụng **YesSql** - một document database trên SQL:

### Core Concept:
- **Document Table**: Lưu trữ tất cả content dưới dạng JSON
- **Index Tables**: Tạo bởi từng module để query hiệu quả
- **No Schema Migration**: Content structure linh hoạt

```csharp
// ✅ ĐÚNG: Cách OrchardCore lưu trữ data
┌─────────────────┐    ┌──────────────────────┐    ┌─────────────────────┐
│   Document      │    │  ContentItemIndex    │    │ PublishLaterIndex   │
├─────────────────┤    ├──────────────────────┤    ├─────────────────────┤
│ Id: 1           │◄──►│ DocumentId: 1        │    │ DocumentId: 1       │
│ Type: BlogPost  │    │ ContentItemId: abc   │    │ ContentItemId: abc  │
│ Content: {...}  │    │ ContentType: BlogPost│    │ ScheduledDate: ...  │
│                 │    │ Published: true      │    │ Published: false    │
└─────────────────┘    └──────────────────────┘    └─────────────────────┘
```

## 3. 🔴 QUY ĐỊNH BẮT BUỘC: Index Tables

### Mỗi module TẠO INDEX TABLES, KHÔNG TẠO CONTENT TABLES

```csharp
// ✅ ĐÚNG: Index Table Definition
namespace MyCompany.MyModule.Indexes
{
    /// <summary>
    /// ✅ ĐÚNG: Index table để query hiệu quả
    /// - Kế thừa từ MapIndex hoặc ReduceIndex
    /// - Chỉ chứa fields cần thiết cho query
    /// - Không lưu trữ content chính
    /// </summary>
    public class MyModulePartIndex : MapIndex
    {
        // ✅ QUY ĐỊNH: Luôn có ContentItemId
        public string ContentItemId { get; set; }
        
        // ✅ QUY ĐỊNH: Các fields để query
        public string Title { get; set; }
        public string Category { get; set; }
        public DateTime? PublishedDate { get; set; }
        public bool IsActive { get; set; }
        public int Priority { get; set; }
        
        // ✅ QUY ĐỊNH: Standard fields cho versioning
        public bool Published { get; set; }
        public bool Latest { get; set; }
    }
}

// ❌ SAI: Không tạo content tables
public class MyModuleContent  // ❌ SAI: Không cần table riêng cho content
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Content { get; set; }  // ❌ SAI: Content được lưu trong Document table
}
```

### Index Provider - Cách tạo và cập nhật index

```csharp
// ✅ ĐÚNG: Index Provider chuẩn OrchardCore
namespace MyCompany.MyModule.Indexes
{
    /// <summary>
    /// ✅ ĐÚNG: Index Provider tuân thủ quy định
    /// - Implement IIndexProvider
    /// - Kế thừa ContentHandlerBase để handle content events
    /// - Map từ ContentItem sang Index
    /// - Validation và cleanup logic
    /// </summary>
    public class MyModulePartIndexProvider : ContentHandlerBase, IIndexProvider, IScopedIndexProvider
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly HashSet<string> _partRemoved = new();
        private IContentDefinitionManager _contentDefinitionManager;
        
        public MyModulePartIndexProvider(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }
        
        // ✅ QUY ĐỊNH: Handle content lifecycle events
        public override Task CreatedAsync(CreateContentContext context)
            => UpdateMyModulePartAsync(context.ContentItem);
            
        public override Task UpdatedAsync(UpdateContentContext context)
            => UpdateMyModulePartAsync(context.ContentItem);
        
        /// <summary>
        /// ✅ ĐÚNG: Validation logic để đảm bảo part còn tồn tại
        /// </summary>
        private async Task UpdateMyModulePartAsync(ContentItem contentItem)
        {
            var part = contentItem.As<MyModulePart>();
            
            if (part != null)
            {
                // ✅ QUY ĐỊNH: Lazy initialization để tránh cyclic dependency
                _contentDefinitionManager ??= _serviceProvider.GetRequiredService<IContentDefinitionManager>();
                
                // ✅ QUY ĐỊNH: Validate part definition exists
                var contentTypeDefinition = await _contentDefinitionManager.GetTypeDefinitionAsync(contentItem.ContentType);
                if (!contentTypeDefinition.Parts.Any(ctd => ctd.Name == nameof(MyModulePart)))
                {
                    // ✅ QUY ĐỊNH: Remove part nếu không còn trong definition
                    contentItem.Remove<MyModulePart>();
                    _partRemoved.Add(contentItem.ContentItemId);
                }
            }
        }
        
        // ✅ QUY ĐỊNH: IIndexProvider implementation
        public string CollectionName { get; set; }
        public Type ForType() => typeof(ContentItem);
        public void Describe(IDescriptor context) => Describe((DescribeContext<ContentItem>)context);
        
        /// <summary>
        /// ✅ ĐÚNG: Describe method - core logic của indexing
        /// </summary>
        public void Describe(DescribeContext<ContentItem> context)
        {
            context.For<MyModulePartIndex>()
                // ✅ QUY ĐỊNH: When condition - khi nào tạo index
                .When(contentItem => 
                    contentItem.Has<MyModulePart>() || 
                    _partRemoved.Contains(contentItem.ContentItemId))
                // ✅ QUY ĐỊNH: Map function - chuyển đổi ContentItem thành Index
                .Map(contentItem =>
                {
                    // ✅ QUY ĐỊNH: Skip published hoặc non-latest versions
                    if (contentItem.Published || !contentItem.Latest)
                    {
                        return null;
                    }
                    
                    var part = contentItem.As<MyModulePart>();
                    if (part == null)
                    {
                        return null;
                    }
                    
                    // ✅ QUY ĐỊNH: Map sang index object
                    return new MyModulePartIndex
                    {
                        ContentItemId = contentItem.ContentItemId,
                        Title = part.Title?.Text,
                        Category = part.Category?.Text,
                        PublishedDate = part.PublishedDate?.Value,
                        IsActive = part.IsActive?.Value ?? false,
                        Priority = (int)(part.Priority?.Value ?? 0),
                        Published = contentItem.Published,
                        Latest = contentItem.Latest
                    };
                });
        }
    }
}

// ❌ SAI: Index Provider không chuẩn
public class MyIndexProvider : IIndexProvider  // ❌ SAI: Không kế thừa ContentHandlerBase
{
    public void Describe(IDescriptor context)
    {
        // ❌ SAI: Không có validation
        // ❌ SAI: Không handle content lifecycle
        // ❌ SAI: Map logic không đầy đủ
        
        context.For<MyModulePartIndex>()
            .Map(contentItem => new MyModulePartIndex
            {
                // ❌ SAI: Không check null
                Title = contentItem.As<MyModulePart>().Title.Text,
                // ❌ SAI: Không có Published/Latest logic
            });
    }
}
```

## 4. 🔴 QUY ĐỊNH BẮT BUỘC: Data Migrations

### Migration Class - Tạo và cập nhật database schema

```csharp
// ✅ ĐÚNG: Data Migration chuẩn OrchardCore
namespace MyCompany.MyModule
{
    /// <summary>
    /// ✅ ĐÚNG: Data Migration tuân thủ quy định
    /// - Sealed class kế thừa DataMigration
    /// - CreateAsync method cho initial setup
    /// - UpdateFromXAsync methods cho version upgrades
    /// - Proper error handling và rollback support
    /// </summary>
    public sealed class Migrations : DataMigration
    {
        private readonly IContentDefinitionManager _contentDefinitionManager;
        
        public Migrations(IContentDefinitionManager contentDefinitionManager)
        {
            _contentDefinitionManager = contentDefinitionManager;
        }
        
        /// <summary>
        /// ✅ ĐÚNG: CreateAsync - Initial setup khi module được install lần đầu
        /// </summary>
        public async Task<int> CreateAsync()
        {
            // ✅ QUY ĐỊNH: 1. Tạo Content Part Definition
            await _contentDefinitionManager.AlterPartDefinitionAsync("MyModulePart", builder => builder
                .Attachable()
                .WithDescription("Provides custom functionality for My Module"));
            
            // ✅ QUY ĐỊNH: 2. Tạo Content Type Definition (nếu cần)
            await _contentDefinitionManager.AlterTypeDefinitionAsync("MyModuleItem", type => type
                .Draftable()
                .Versionable()
                .Creatable()
                .Listable()
                .Securable()
                .WithPart("TitlePart", part => part.WithPosition("1"))
                .WithPart("MyModulePart", part => part.WithPosition("2"))
                .WithPart("AutoroutePart", part => part
                    .WithPosition("3")
                    .WithSettings(new AutoroutePartSettings
                    {
                        Pattern = "{{ Model.ContentItem | display_text | slugify }}",
                        AllowCustomPath = true
                    })));
            
            // ✅ QUY ĐỊNH: 3. Tạo Index Table
            await SchemaBuilder.CreateMapIndexTableAsync<MyModulePartIndex>(table => table
                .Column<string>("ContentItemId", c => c.WithLength(26))
                .Column<string>("Title", c => c.WithLength(255))
                .Column<string>("Category", c => c.WithLength(100))
                .Column<DateTime>("PublishedDate")
                .Column<bool>("IsActive", c => c.WithDefault(true))
                .Column<int>("Priority", c => c.WithDefault(50))
                .Column<bool>("Published", c => c.WithDefault(false))
                .Column<bool>("Latest", c => c.WithDefault(false))
            );
            
            // ✅ QUY ĐỊNH: 4. Tạo Database Indexes cho performance
            await SchemaBuilder.AlterIndexTableAsync<MyModulePartIndex>(table => table
                .CreateIndex("IDX_MyModulePartIndex_DocumentId",
                    "Id",
                    "DocumentId", 
                    "ContentItemId",
                    "Published",
                    "Latest")
            );
            
            await SchemaBuilder.AlterIndexTableAsync<MyModulePartIndex>(table => table
                .CreateIndex("IDX_MyModulePartIndex_Category",
                    "DocumentId",
                    "Category",
                    "IsActive",
                    "Published")
            );
            
            await SchemaBuilder.AlterIndexTableAsync<MyModulePartIndex>(table => table
                .CreateIndex("IDX_MyModulePartIndex_PublishedDate",
                    "DocumentId",
                    "PublishedDate",
                    "IsActive")
            );
            
            // ✅ QUY ĐỊNH: Return version number
            return 3; // Current version after all initial setup
        }
        
        /// <summary>
        /// ✅ ĐÚNG: UpdateFrom1Async - Upgrade từ version 1 lên 2
        /// </summary>
        public async Task<int> UpdateFrom1Async()
        {
            // ✅ QUY ĐỊNH: Add new columns
            await SchemaBuilder.AlterIndexTableAsync<MyModulePartIndex>(table => table
                .AddColumn<DateTime>("PublishedDate")
            );
            
            // ✅ QUY ĐỊNH: Create new index
            await SchemaBuilder.AlterIndexTableAsync<MyModulePartIndex>(table => table
                .CreateIndex("IDX_MyModulePartIndex_PublishedDate",
                    "DocumentId",
                    "PublishedDate",
                    "IsActive")
            );
            
            return 2;
        }
        
        /// <summary>
        /// ✅ ĐÚNG: UpdateFrom2Async - Upgrade từ version 2 lên 3
        /// </summary>
        public async Task<int> UpdateFrom2Async()
        {
            // ✅ QUY ĐỊNH: Add multiple columns
            await SchemaBuilder.AlterIndexTableAsync<MyModulePartIndex>(table => table
                .AddColumn<bool>("Published", c => c.WithDefault(false))
                .AddColumn<bool>("Latest", c => c.WithDefault(false))
            );
            
            // ✅ QUY ĐỊNH: Drop old index
            await SchemaBuilder.AlterIndexTableAsync<MyModulePartIndex>(table => table
                .DropIndex("IDX_MyModulePartIndex_Old")
            );
            
            // ✅ QUY ĐỊNH: Create new comprehensive index
            await SchemaBuilder.AlterIndexTableAsync<MyModulePartIndex>(table => table
                .CreateIndex("IDX_MyModulePartIndex_DocumentId",
                    "Id",
                    "DocumentId",
                    "ContentItemId", 
                    "Published",
                    "Latest")
            );
            
            return 3;
        }
        
        /// <summary>
        /// ✅ ĐÚNG: Data migration với content updates
        /// </summary>
        public async Task<int> UpdateFrom3Async()
        {
            // ✅ QUY ĐỊNH: Update existing content definitions
            await _contentDefinitionManager.AlterPartDefinitionAsync("MyModulePart", builder => builder
                .WithDescription("Updated description for My Module Part with new features"));
            
            // ✅ QUY ĐỊNH: Add new field to existing part
            await _contentDefinitionManager.AlterPartDefinitionAsync("MyModulePart", builder => builder
                .WithField("NewField", field => field
                    .OfType("TextField")
                    .WithDisplayName("New Field")
                    .WithSettings(new TextFieldSettings
                    {
                        Hint = "This is a new field added in version 4"
                    })));
            
            return 4;
        }
    }
}

// ❌ SAI: Migration không chuẩn
public class MyMigration : DataMigration  // ❌ SAI: Không sealed
{
    // ❌ SAI: Không có dependencies injection
    
    public async Task<int> CreateAsync()
    {
        // ❌ SAI: Tạo table riêng thay vì index table
        await SchemaBuilder.CreateTableAsync("MyModuleData", table => table
            .Column<int>("Id", c => c.PrimaryKey().Identity())
            .Column<string>("Title")
            .Column<string>("Content")  // ❌ SAI: Duplicate với Document table
        );
        
        // ❌ SAI: Không return version number
        return 1;
    }
    
    // ❌ SAI: Không có update methods cho version management
}
```

## 5. 🔴 QUY ĐỊNH BẮT BUỘC: Query Patterns

### Cách query data từ Index Tables

```csharp
// ✅ ĐÚNG: Service với proper querying
namespace MyCompany.MyModule.Services
{
    public class MyModuleService : IMyModuleService
    {
        private readonly ISession _session;
        private readonly IContentManager _contentManager;
        private readonly ILogger<MyModuleService> _logger;
        
        public MyModuleService(
            ISession session,
            IContentManager contentManager,
            ILogger<MyModuleService> logger)
        {
            _session = session;
            _contentManager = contentManager;
            _logger = logger;
        }
        
        /// <summary>
        /// ✅ ĐÚNG: Query sử dụng Index Table
        /// </summary>
        public async Task<IEnumerable<ContentItem>> GetActiveItemsAsync(string category = null)
        {
            // ✅ QUY ĐỊNH: Query index table trước
            var query = _session.Query<ContentItem, MyModulePartIndex>(index => 
                index.IsActive == true && 
                index.Published == true && 
                index.Latest == true);
            
            // ✅ QUY ĐỊNH: Filter by category nếu có
            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(index => index.Category == category);
            }
            
            // ✅ QUY ĐỊNH: Order by priority
            query = query.OrderByDescending(index => index.Priority)
                         .ThenByDescending(index => index.PublishedDate);
            
            // ✅ QUY ĐỊNH: Execute query
            var contentItems = await query.ListAsync();
            
            _logger.LogDebug("Retrieved {Count} active items for category {Category}", 
                contentItems.Count(), category ?? "all");
            
            return contentItems;
        }
        
        /// <summary>
        /// ✅ ĐÚNG: Pagination với Index
        /// </summary>
        public async Task<(IEnumerable<ContentItem> Items, int TotalCount)> GetItemsPagedAsync(
            int page = 1, 
            int pageSize = 10, 
            string category = null,
            bool? isActive = null)
        {
            var query = _session.Query<ContentItem, MyModulePartIndex>(index => 
                index.Published == true && index.Latest == true);
            
            // ✅ QUY ĐỊNH: Dynamic filtering
            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(index => index.Category == category);
            }
            
            if (isActive.HasValue)
            {
                query = query.Where(index => index.IsActive == isActive.Value);
            }
            
            // ✅ QUY ĐỊNH: Get total count trước khi pagination
            var totalCount = await query.CountAsync();
            
            // ✅ QUY ĐỊNH: Apply pagination
            var items = await query
                .OrderByDescending(index => index.PublishedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ListAsync();
            
            return (items, totalCount);
        }
        
        /// <summary>
        /// ✅ ĐÚNG: Search với full-text capabilities
        /// </summary>
        public async Task<IEnumerable<ContentItem>> SearchItemsAsync(string searchTerm)
        {
            // ✅ QUY ĐỊNH: Search trong index fields
            var items = await _session.Query<ContentItem, MyModulePartIndex>(index => 
                index.Title.Contains(searchTerm) && 
                index.IsActive == true &&
                index.Published == true &&
                index.Latest == true)
                .OrderByDescending(index => index.PublishedDate)
                .Take(50) // ✅ QUY ĐỊNH: Limit search results
                .ListAsync();
            
            return items;
        }
        
        /// <summary>
        /// ✅ ĐÚNG: Complex query với joins
        /// </summary>
        public async Task<IEnumerable<ContentItem>> GetRelatedItemsAsync(string contentItemId)
        {
            // ✅ QUY ĐỊNH: Get current item category first
            var currentItem = await _session.Query<ContentItem, MyModulePartIndex>(index => 
                index.ContentItemId == contentItemId)
                .FirstOrDefaultAsync();
            
            if (currentItem == null)
                return Enumerable.Empty<ContentItem>();
            
            var currentIndex = await _session.Query<MyModulePartIndex>(index => 
                index.ContentItemId == contentItemId)
                .FirstOrDefaultAsync();
            
            if (currentIndex == null || string.IsNullOrEmpty(currentIndex.Category))
                return Enumerable.Empty<ContentItem>();
            
            // ✅ QUY ĐỊNH: Find related items in same category
            var relatedItems = await _session.Query<ContentItem, MyModulePartIndex>(index => 
                index.Category == currentIndex.Category &&
                index.ContentItemId != contentItemId &&
                index.IsActive == true &&
                index.Published == true &&
                index.Latest == true)
                .OrderByDescending(index => index.Priority)
                .Take(5)
                .ListAsync();
            
            return relatedItems;
        }
        
        /// <summary>
        /// ✅ ĐÚNG: Aggregation queries
        /// </summary>
        public async Task<Dictionary<string, int>> GetCategoryStatisticsAsync()
        {
            // ✅ QUY ĐỊNH: Group by category
            var stats = await _session.Query<MyModulePartIndex>(index => 
                index.IsActive == true &&
                index.Published == true &&
                index.Latest == true)
                .GroupBy(index => index.Category)
                .Select(group => new { Category = group.Key, Count = group.Count() })
                .ToDictionaryAsync(x => x.Category ?? "Uncategorized", x => x.Count);
            
            return stats;
        }
    }
}

// ❌ SAI: Query không hiệu quả
public class BadService
{
    private readonly ISession _session;
    
    public BadService(ISession session)
    {
        _session = session;
    }
    
    public async Task<IEnumerable<ContentItem>> GetItems()
    {
        // ❌ SAI: Query trực tiếp ContentItem không qua Index
        var allItems = await _session.Query<ContentItem>().ListAsync();
        
        // ❌ SAI: Filter trong memory thay vì database
        return allItems.Where(item => 
        {
            var part = item.As<MyModulePart>();
            return part?.IsActive?.Value == true;
        });
    }
    
    public async Task<IEnumerable<ContentItem>> SearchItems(string term)
    {
        // ❌ SAI: Load tất cả items rồi filter
        var allItems = await _session.Query<ContentItem>().ListAsync();
        
        return allItems.Where(item =>
        {
            var part = item.As<MyModulePart>();
            return part?.Title?.Text?.Contains(term) == true;
        });
    }
}
```

## 6. 🔴 QUY ĐỊNH BẮT BUỘC: Registration trong Startup

```csharp
// ✅ ĐÚNG: Startup class registration
namespace MyCompany.MyModule
{
    public class Startup : StartupBase
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            // ✅ QUY ĐỊNH: Register Index Provider
            services.AddSingleton<IIndexProvider, MyModulePartIndexProvider>();
            
            // ✅ QUY ĐỊNH: Register Data Migration
            services.AddScoped<IDataMigration, Migrations>();
            
            // ✅ QUY ĐỊNH: Register Content Part
            services.AddContentPart<MyModulePart>();
            
            // ✅ QUY ĐỊNH: Register services
            services.AddScoped<IMyModuleService, MyModuleService>();
        }
    }
}
```

## 7. Tóm Tắt Quy Trình

### Khi Module Được Enable:

1. **Data Migration chạy**: Tạo Index Tables và Content Definitions
2. **Index Provider đăng ký**: Bắt đầu index content
3. **Content được tạo**: Lưu trong Document table + Index tables
4. **Query thông qua Index**: Hiệu quả và nhanh chóng

### Khi Content Được Tạo/Cập Nhật:

1. **Content lưu trong Document table** (JSON format)
2. **Index Provider tự động tạo/cập nhật Index records**
3. **Query sử dụng Index tables** cho performance
4. **Content được retrieve từ Document table** khi cần

### ✅ ĐÚNG vs ❌ SAI - Tóm Tắt

| ✅ ĐÚNG | ❌ SAI |
|---------|--------|
| Sử dụng shared database | Tạo database riêng cho module |
| Tạo Index Tables | Tạo Content Tables |
| Content lưu trong Document table | Content lưu trong custom tables |
| Query qua Index Provider | Query trực tiếp content |
| Data Migration cho schema | Manual table creation |
| YesSql document store | Traditional ORM approach |

## Kết Luận

OrchardCore sử dụng một kiến trúc database thông minh:
- **Một database duy nhất** cho toàn bộ application
- **Document store** cho content flexibility  
- **Index tables** cho query performance
- **Automatic schema management** qua migrations
- **No manual table creation** - tất cả thông qua framework

Điều này đảm bảo:
- **Consistency** across modules
- **Performance** với proper indexing
- **Flexibility** với document storage
- **Maintainability** với automatic migrations