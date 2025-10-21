# Performance & Caching Patterns trong OrchardCore

## 🎯 **MỤC TIÊU**
Tìm hiểu các patterns về Performance và Caching để **viết modules OrchardCore hiệu suất cao**.

---

## 🚀 **CACHING ARCHITECTURE TRONG ORCHARDCORE**

### **1. Multi-Level Caching Strategy**

#### **A. Memory Cache (L1)**
- **Scope**: In-process, fastest
- **Lifetime**: Application lifetime
- **Use case**: Frequently accessed data

#### **B. Distributed Cache (L2)**
- **Scope**: Cross-process, scalable
- **Providers**: Redis, SQL Server, In-Memory
- **Use case**: Multi-instance deployments

#### **C. Dynamic Cache (L3)**
- **Scope**: Content-aware caching
- **Features**: Tag-based invalidation
- **Use case**: Content rendering

---

## 🔧 **CORE CACHING PATTERNS**

### **1. IMemoryCache Pattern**
```csharp
public class MyService
{
    private readonly IMemoryCache _memoryCache;
    
    public MyService(IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache;
    }
    
    public async Task<MyData> GetDataAsync(string key)
    {
        return await _memoryCache.GetOrCreateAsync($"mydata_{key}", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30);
            entry.Priority = CacheItemPriority.High;
            
            return await LoadDataFromDatabaseAsync(key);
        });
    }
}
```

### **2. IDynamicCacheService Pattern**
```csharp
public class MyContentService
{
    private readonly IDynamicCacheService _dynamicCache;
    
    public async Task<string> RenderContentAsync(ContentItem contentItem)
    {
        var cacheKey = $"content_{contentItem.ContentItemId}";
        var tags = new[] { $"contentitem:{contentItem.ContentItemId}" };
        
        return await _dynamicCache.GetOrCreateAsync(cacheKey, tags, async ctx =>
        {
            ctx.AddExpirationToken(new CancellationChangeToken(_cts.Token));
            return await RenderContentInternalAsync(contentItem);
        });
    }
}
```

### **3. Cache Tag Invalidation Pattern**
```csharp
public class MyContentHandler : ContentHandlerBase
{
    private readonly ITagCache _tagCache;
    
    public override async Task UpdatedAsync(UpdateContentContext context)
    {
        // Invalidate related caches
        await _tagCache.RemoveTagAsync($"contentitem:{context.ContentItem.ContentItemId}");
        await _tagCache.RemoveTagAsync($"contenttype:{context.ContentItem.ContentType}");
    }
}
```

---

## 📊 **PERFORMANCE OPTIMIZATION PATTERNS**

### **1. Lazy Loading Pattern**
```csharp
public class MyPart : ContentPart
{
    private IList<MyRelatedItem> _relatedItems;
    
    public IList<MyRelatedItem> RelatedItems
    {
        get
        {
            if (_relatedItems == null)
            {
                _relatedItems = LoadRelatedItemsAsync().GetAwaiter().GetResult();
            }
            return _relatedItems;
        }
    }
}
```

### **2. Bulk Loading Pattern**
```csharp
public class MyService
{
    public async Task<IEnumerable<MyViewModel>> GetViewModelsAsync(IEnumerable<string> ids)
    {
        // Load all at once instead of N+1 queries
        var items = await _session.Query<MyRecord>()
            .Where(x => x.Id.IsIn(ids))
            .ListAsync();
            
        return items.Select(MapToViewModel);
    }
}
```

### **3. Async Enumerable Pattern**
```csharp
public class MyDataService
{
    public async IAsyncEnumerable<MyItem> GetItemsAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var batch in GetBatchesAsync(cancellationToken))
        {
            foreach (var item in batch)
            {
                yield return item;
            }
        }
    }
}
```

---

## 🎯 **BEST PRACTICES**

### **✅ ĐÚNG:**
- Cache expensive operations
- Use appropriate cache levels
- Implement cache invalidation
- Monitor cache hit rates
- Use async patterns consistently

### **❌ SAI:**
- Cache everything blindly
- Ignore cache invalidation
- Block async operations
- Cache sensitive data without encryption
- Ignore memory pressure

---

*Tài liệu này được tạo dựa trên phân tích source code OrchardCore và best practices.*