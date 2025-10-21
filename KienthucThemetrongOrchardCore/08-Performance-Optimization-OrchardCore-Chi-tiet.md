# ⚡ Performance & Optimization trong OrchardCore - Phân tích Chi tiết từ Source Code

## 🎯 Tổng quan Performance & Optimization System

OrchardCore cung cấp hệ thống performance optimization toàn diện thông qua caching, resource management, background tasks và lazy loading patterns:

## 🚀 Dynamic Cache System

### 💾 Core Cache Interfaces

```csharp
// OrchardCore.DynamicCache.Abstractions/IDynamicCache.cs
public interface IDynamicCache
{
    Task<byte[]> GetAsync(string key);
    Task RemoveAsync(string key);
    Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options);
}

// OrchardCore.DynamicCache.Abstractions/IDynamicCacheService.cs
public interface IDynamicCacheService : ITagRemovedEventHandler
{
    Task<string> GetCachedValueAsync(CacheContext context);
    Task SetCachedValueAsync(CacheContext context, string value);
}
```

### 🏗️ DefaultDynamicCacheService Implementation

```csharp
// OrchardCore.DynamicCache/Services/DefaultDynamicCacheService.cs
public class DefaultDynamicCacheService : IDynamicCacheService
{
    public const string FailoverKey = "OrchardCore_DynamicCache_FailoverKey";
    public static readonly TimeSpan DefaultFailoverRetryLatency = TimeSpan.FromSeconds(30);

    private readonly ICacheContextManager _cacheContextManager;
    private readonly IDynamicCache _dynamicCache;
    private readonly IMemoryCache _memoryCache;
    private readonly IServiceProvider _serviceProvider;
    private readonly DynamicCacheOptions _dynamicCacheOptions;
    private readonly CacheOptions _cacheOptions;
    private readonly ILogger _logger;

    private readonly Dictionary<string, string> _localCache = [];
    private ITagCache _tagcache;

    public async Task<string> GetCachedValueAsync(CacheContext context)
    {
        if (!_cacheOptions.Enabled)
        {
            return null;
        }

        var cacheKey = await GetCacheKey(context);

        context = await GetCachedContextAsync(cacheKey);
        if (context == null)
        {
            // We don't know the context, so we must treat this as a cache miss
            return null;
        }

        var content = await GetCachedStringAsync(cacheKey);
        return content;
    }

    public async Task SetCachedValueAsync(CacheContext context, string value)
    {
        if (!_cacheOptions.Enabled)
        {
            return;
        }

        var cacheKey = await GetCacheKey(context);

        _localCache[cacheKey] = value;
        var esi = JConvert.SerializeObject(CacheContextModel.FromCacheContext(context));

        await Task.WhenAll(
            SetCachedValueAsync(cacheKey, value, context),
            SetCachedValueAsync(GetCacheContextCacheKey(cacheKey), esi, context)
        );
    }

    private async Task SetCachedValueAsync(string cacheKey, string value, CacheContext context)
    {
        var failover = _memoryCache.Get<bool>(FailoverKey);
        if (failover)
        {
            return;
        }

        var bytes = Encoding.UTF8.GetBytes(value);

        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpiration = context.ExpiresOn,
            SlidingExpiration = context.ExpiresSliding,
            AbsoluteExpirationRelativeToNow = context.ExpiresAfter,
        };

        // Default duration is sliding expiration (permanent as long as it's used)
        if (!options.AbsoluteExpiration.HasValue && 
            !options.SlidingExpiration.HasValue && 
            !options.AbsoluteExpirationRelativeToNow.HasValue)
        {
            options.SlidingExpiration = new TimeSpan(0, 1, 0);
        }

        try
        {
            await _dynamicCache.SetAsync(cacheKey, bytes, options);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to write the '{CacheKey}' to the dynamic cache", cacheKey);

            _memoryCache.Set(FailoverKey, true, new MemoryCacheEntryOptions()
            {
                AbsoluteExpirationRelativeToNow = _dynamicCacheOptions.FailoverRetryLatency,
            });

            return;
        }

        // Lazy load to prevent cyclic dependency
        _tagcache ??= _serviceProvider.GetRequiredService<ITagCache>();
        await _tagcache.TagAsync(cacheKey, context.Tags.ToArray());
    }

    private async Task<string> GetCachedStringAsync(string cacheKey)
    {
        if (_localCache.TryGetValue(cacheKey, out var content))
        {
            return content;
        }

        var failover = _memoryCache.Get<bool>(FailoverKey);
        if (failover)
        {
            return null;
        }

        try
        {
            var bytes = await _dynamicCache.GetAsync(cacheKey);
            if (bytes == null)
            {
                return null;
            }

            return Encoding.UTF8.GetString(bytes);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to read the '{CacheKey}' from the dynamic cache", cacheKey);

            _memoryCache.Set(FailoverKey, true, new MemoryCacheEntryOptions()
            {
                AbsoluteExpirationRelativeToNow = _dynamicCacheOptions.FailoverRetryLatency,
            });
        }

        return null;
    }
}
```

### 🏷️ DynamicCacheTagHelper

```csharp
// OrchardCore.DynamicCache/TagHelpers/DynamicCacheTagHelper.cs
[HtmlTargetElement("dynamic-cache", Attributes = CacheIdAttributeName)]
public class DynamicCacheTagHelper : TagHelper
{
    private const string CacheIdAttributeName = "cache-id";
    private const string VaryByAttributeName = "vary-by";
    private const string DependenciesAttributeNAme = "dependencies";
    private const string ExpiresOnAttributeName = "expires-on";
    private const string ExpiresAfterAttributeName = "expires-after";
    private const string ExpiresSlidingAttributeName = "expires-sliding";
    private const string EnabledAttributeName = "enabled";

    /// <summary>
    /// The default duration, from the time the cache entry was added, when it should be evicted.
    /// This default duration will only be used if no other expiration criteria is specified.
    /// The default expiration time is a sliding expiration of 30 seconds.
    /// </summary>
    public static readonly TimeSpan DefaultExpiration = TimeSpan.FromSeconds(30);

    [HtmlAttributeName(CacheIdAttributeName)]
    public string CacheId { get; set; }

    [HtmlAttributeName(VaryByAttributeName)]
    public string VaryBy { get; set; }

    [HtmlAttributeName(DependenciesAttributeNAme)]
    public string Dependencies { get; set; }

    [HtmlAttributeName(ExpiresOnAttributeName)]
    public DateTimeOffset? ExpiresOn { get; set; }

    [HtmlAttributeName(ExpiresAfterAttributeName)]
    public TimeSpan? ExpiresAfter { get; set; }

    [HtmlAttributeName(ExpiresSlidingAttributeName)]
    public TimeSpan? ExpiresSliding { get; set; }

    [HtmlAttributeName(EnabledAttributeName)]
    public bool Enabled { get; set; } = true;

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        IHtmlContent content;

        if (Enabled)
        {
            var cacheContext = new CacheContext(CacheId);

            if (!string.IsNullOrEmpty(VaryBy))
            {
                cacheContext.AddContext(VaryBy.Split(_splitChars, StringSplitOptions.RemoveEmptyEntries));
            }

            if (!string.IsNullOrEmpty(Dependencies))
            {
                cacheContext.AddTag(Dependencies.Split(_splitChars, StringSplitOptions.RemoveEmptyEntries));
            }

            var hasEvictionCriteria = false;

            if (ExpiresOn.HasValue)
            {
                hasEvictionCriteria = true;
                cacheContext.WithExpiryOn(ExpiresOn.Value);
            }

            if (ExpiresAfter.HasValue)
            {
                hasEvictionCriteria = true;
                cacheContext.WithExpiryAfter(ExpiresAfter.Value);
            }

            if (ExpiresSliding.HasValue)
            {
                hasEvictionCriteria = true;
                cacheContext.WithExpirySliding(ExpiresSliding.Value);
            }

            if (!hasEvictionCriteria)
            {
                cacheContext.WithExpirySliding(DefaultExpiration);
            }

            _cacheScopeManager.EnterScope(cacheContext);

            try
            {
                content = await ProcessContentAsync(output, cacheContext);
            }
            finally
            {
                _cacheScopeManager.ExitScope();
            }
        }
        else
        {
            content = await output.GetChildContentAsync();
        }

        // Clear the contents of the "cache" element since we don't want to render it.
        output.SuppressOutput();
        output.Content.SetHtmlContent(content);
    }

    public async Task<IHtmlContent> ProcessContentAsync(TagHelperOutput output, CacheContext cacheContext)
    {
        IHtmlContent content = null;

        while (content == null)
        {
            Task<IHtmlContent> result;

            // Is there any request already processing the value?
            if (!_dynamicCacheTagHelperService.Workers.TryGetValue(CacheId, out result))
            {
                var tcs = new TaskCompletionSource<IHtmlContent>();
                _dynamicCacheTagHelperService.Workers.TryAdd(CacheId, tcs.Task);

                try
                {
                    var value = await _dynamicCacheService.GetCachedValueAsync(cacheContext);

                    if (value == null)
                    {
                        // The value is not cached, we need to render the tag helper output
                        var processedContent = await output.GetChildContentAsync();

                        using var writer = new ZStringWriter();
                        
                        // Write cache debug information
                        if (_cacheOptions.DebugMode)
                        {
                            writer.WriteLine();
                            writer.WriteLine($"<!-- CACHE BLOCK: {cacheContext.CacheId} ({Guid.NewGuid()})");
                            writer.WriteLine($"         VARY BY: {string.Join(", ", cacheContext.Contexts)}");
                            writer.WriteLine($"    DEPENDENCIES: {string.Join(", ", cacheContext.Tags)}");
                            writer.WriteLine($"      EXPIRES ON: {cacheContext.ExpiresOn}");
                            writer.WriteLine($"   EXPIRES AFTER: {cacheContext.ExpiresAfter}");
                            writer.WriteLine($" EXPIRES SLIDING: {cacheContext.ExpiresSliding}");
                            writer.WriteLine("-->");
                        }

                        processedContent.WriteTo(writer, HtmlEncoder);

                        if (_cacheOptions.DebugMode)
                        {
                            writer.WriteLine();
                            writer.WriteLine($"<!-- END CACHE BLOCK: {cacheContext.CacheId} -->");
                        }

                        await writer.FlushAsync();
                        var html = writer.ToString();

                        await _dynamicCacheService.SetCachedValueAsync(cacheContext, html);
                        content = new HtmlString(html);
                    }
                    else
                    {
                        content = new HtmlString(value);
                    }
                }
                finally
                {
                    _dynamicCacheTagHelperService.Workers.TryRemove(CacheId, out _);
                    tcs.TrySetResult(content);
                }
            }
            else
            {
                content = await result;
            }
        }

        return content;
    }
}
```

### 🏷️ Tag-based Cache Invalidation

```csharp
// OrchardCore.Infrastructure/Cache/DefaultTagCache.cs
public class DefaultTagCache : ITagCache
{
    private const string CacheKey = nameof(DefaultTagCache);

    private readonly ConcurrentDictionary<string, HashSet<string>> _dictionary;
    private readonly IEnumerable<ITagRemovedEventHandler> _tagRemovedEventHandlers;
    private readonly ILogger _logger;

    public DefaultTagCache(
        IEnumerable<ITagRemovedEventHandler> tagRemovedEventHandlers,
        IMemoryCache memoryCache,
        ILogger<DefaultTagCache> logger)
    {
        // We use the memory cache as the state holder and keep this class transient
        if (!memoryCache.TryGetValue(CacheKey, out _dictionary))
        {
            _dictionary = new ConcurrentDictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            memoryCache.Set(CacheKey, _dictionary);
        }

        _tagRemovedEventHandlers = tagRemovedEventHandlers;
        _logger = logger;
    }

    public Task TagAsync(string key, params string[] tags)
    {
        foreach (var tag in tags)
        {
            var set = _dictionary.GetOrAdd(tag, x => new HashSet<string>());

            lock (set)
            {
                set.Add(key);
            }
        }

        return Task.CompletedTask;
    }

    public Task<IEnumerable<string>> GetTaggedItemsAsync(string tag)
    {
        if (_dictionary.TryGetValue(tag, out var set))
        {
            lock (set)
            {
                return Task.FromResult(set.AsEnumerable());
            }
        }

        return Task.FromResult(Enumerable.Empty<string>());
    }

    public Task RemoveTagAsync(string tag)
    {
        if (_dictionary.TryRemove(tag, out var set))
        {
            return _tagRemovedEventHandlers.InvokeAsync(
                (handler, tag, set) => handler.TagRemovedAsync(tag, set), 
                tag, set, _logger);
        }

        return Task.CompletedTask;
    }
}
```

### 🎯 Cache Context Management

```csharp
// OrchardCore.Infrastructure/Cache/CacheContextManager.cs
public class CacheContextManager : ICacheContextManager
{
    private readonly IEnumerable<ICacheContextProvider> _cacheContextProviders;

    public CacheContextManager(IEnumerable<ICacheContextProvider> cacheContextProviders)
    {
        _cacheContextProviders = cacheContextProviders;
    }

    public async Task<IEnumerable<CacheContextEntry>> GetDiscriminatorsAsync(IEnumerable<string> contexts)
    {
        var entries = new List<CacheContextEntry>();

        foreach (var provider in _cacheContextProviders.Reverse())
        {
            await provider.PopulateContextEntriesAsync(contexts, entries);
        }

        return entries;
    }
}

// OrchardCore.Infrastructure/Cache/CacheContextProviders/UserCacheContextProvider.cs
public class UserCacheContextProvider : ICacheContextProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserCacheContextProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Task PopulateContextEntriesAsync(IEnumerable<string> contexts, List<CacheContextEntry> entries)
    {
        if (contexts.Any(ctx => string.Equals(ctx, "user", StringComparison.OrdinalIgnoreCase)))
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.User?.Identity?.IsAuthenticated == true)
            {
                entries.Add(new CacheContextEntry("user", httpContext.User.Identity.Name));
            }
            else
            {
                entries.Add(new CacheContextEntry("user", "anonymous"));
            }
        }

        return Task.CompletedTask;
    }
}

// OrchardCore.Infrastructure/Cache/CacheContextProviders/RolesCacheContextProvider.cs
public class RolesCacheContextProvider : ICacheContextProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public RolesCacheContextProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Task PopulateContextEntriesAsync(IEnumerable<string> contexts, List<CacheContextEntry> entries)
    {
        if (contexts.Any(ctx => string.Equals(ctx, "user.roles", StringComparison.OrdinalIgnoreCase)))
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.User?.Identity?.IsAuthenticated == true)
            {
                var roles = httpContext.User.Claims
                    .Where(c => c.Type == ClaimTypes.Role)
                    .Select(c => c.Value)
                    .OrderBy(r => r);

                entries.Add(new CacheContextEntry("user.roles", string.Join(",", roles)));
            }
            else
            {
                entries.Add(new CacheContextEntry("user.roles", "anonymous"));
            }
        }

        return Task.CompletedTask;
    }
}
```

## 📦 Resource Management & Optimization

### 🏗️ Resource Manifest System

```csharp
// OrchardCore.Resources/ResourceManagementOptionsConfiguration.cs
public sealed class ResourceManagementOptionsConfiguration : IConfigureOptions<ResourceManagementOptions>
{
    private readonly ResourceOptions _resourceOptions;
    private readonly IHostEnvironment _env;
    private readonly PathString _pathBase;

    // Versions
    private const string CodeMirrorVersion = "5.65.7";
    private const string MonacoEditorVersion = "0.46.0";

    // URLs
    private const string CloudflareUrl = "https://cdnjs.cloudflare.com/ajax/libs/";
    private const string CodeMirrorUrl = CloudflareUrl + "codemirror/" + CodeMirrorVersion + "/";

    private ResourceManifest BuildManifest()
    {
        var manifest = new ResourceManifest();

        // jQuery with CDN fallback and integrity hashes
        manifest
            .DefineScript("jQuery")
            .SetUrl(
                "~/OrchardCore.Resources/Scripts/jquery.min.js",
                "~/OrchardCore.Resources/Scripts/jquery.js"
            )
            .SetCdn(
                "https://code.jquery.com/jquery-3.7.1.min.js",
                "https://code.jquery.com/jquery-3.7.1.js"
            )
            .SetCdnIntegrity(
                "sha384-1H217gwSVyLSIfaLxHbE7dRb3v4mYCKbpQvzx0cegeju1MVsGrX5xXxAvs/HgeFs",
                "sha384-wsqsSADZR1YRBEZ4/kKHNSmU+aX8ojbnKUMN4RyD3jDkxw5mHtoe2z/T/n4l56U/"
            )
            .SetVersion("3.7.1");

        // Bootstrap with version management
        manifest
            .DefineScript("bootstrap")
            .SetDependencies("popperjs")
            .SetUrl(
                "~/OrchardCore.Resources/Scripts/bootstrap.min.js",
                "~/OrchardCore.Resources/Scripts/bootstrap.js"
            )
            .SetCdn(
                "https://cdn.jsdelivr.net/npm/bootstrap@5.3.8/dist/js/bootstrap.min.js",
                "https://cdn.jsdelivr.net/npm/bootstrap@5.3.8/dist/js/bootstrap.js"
            )
            .SetCdnIntegrity(
                "sha384-G/EV+4j2dNv+tEPo3++6LCgdCROaejBqfUeNjuKAiuXbjrxilcCdDz6ZAVfHWe1Y",
                "sha384-C8GZT7abfugBh6OJBafyOVkzQPOyZNorS6QrwxpTzdwP/Osl/1MlCLq1D0enn8bH"
            )
            .SetVersion("5.3.8");

        manifest
            .DefineStyle("bootstrap")
            .SetUrl(
                "~/OrchardCore.Resources/Styles/bootstrap.min.css",
                "~/OrchardCore.Resources/Styles/bootstrap.css"
            )
            .SetCdn(
                "https://cdn.jsdelivr.net/npm/bootstrap@5.3.8/dist/css/bootstrap.min.css",
                "https://cdn.jsdelivr.net/npm/bootstrap@5.3.8/dist/css/bootstrap.css"
            )
            .SetCdnIntegrity(
                "sha384-sRIl4kxILFvY47J16cr9ZwB07vP4J8+LH7qKQnuqkuIAvNWLzeN8tE5YBujZqJLB",
                "sha384-6qOMjEs/dk1B8DWuMdvpXhSoFK8G0LAZAgA0WCuiPYo4zOpviuNw5/7W4qLc2EdE"
            )
            .SetVersion("5.3.8");

        return manifest;
    }
}

// OrchardCore.ResourceManagement.Abstractions/ResourceOptions.cs
public class ResourceOptions
{
    public ResourceDebugMode ResourceDebugMode { get; set; }
    public bool UseCdn { get; set; }
    public string CdnBaseUrl { get; set; }
    public bool AppendVersion { get; set; } = true;
}
```

## 🔄 Background Tasks & Optimization

### ⏰ Background Task System

```csharp
// OrchardCore.Sitemaps/Cache/SitemapCacheBackgroundTask.cs
[BackgroundTask(
    Title = "Sitemap Cache Cleaner",
    Schedule = "*/5 * * * *",
    Description = "Cleans up sitemap cache files.")]
public sealed class SitemapCacheBackgroundTask : IBackgroundTask
{
    public async Task DoWorkAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var sitemapManager = serviceProvider.GetRequiredService<ISitemapManager>();
        var sitemapCacheProvider = serviceProvider.GetRequiredService<ISitemapCacheProvider>();

        var sitemaps = await sitemapManager.GetSitemapsAsync();
        await sitemapCacheProvider.CleanSitemapCacheAsync(sitemaps.Select(s => s.CacheFileName));
    }
}

// OrchardCore.PublishLater/Services/ScheduledPublishingBackgroundTask.cs
[BackgroundTask(
    Title = "Scheduled Publishing",
    Schedule = "*/1 * * * *",
    Description = "Publishes scheduled content items.")]
public sealed class ScheduledPublishingBackgroundTask : IBackgroundTask
{
    public async Task DoWorkAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var contentManager = serviceProvider.GetRequiredService<IContentManager>();
        var session = serviceProvider.GetRequiredService<ISession>();
        var logger = serviceProvider.GetRequiredService<ILogger<ScheduledPublishingBackgroundTask>>();

        var itemsToPublish = await session.Query<ContentItem, PublishLaterPartIndex>()
            .Where(x => x.ScheduledPublishUtc <= DateTime.UtcNow && x.Published == false)
            .ListAsync();

        foreach (var item in itemsToPublish)
        {
            try
            {
                await contentManager.PublishAsync(item);
                logger.LogInformation("Published scheduled content item {ContentItemId}", item.ContentItemId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error publishing scheduled content item {ContentItemId}", item.ContentItemId);
            }
        }
    }
}
```

## 💾 Template & Liquid Caching

### 🧪 Liquid Template Manager

```csharp
// OrchardCore.Liquid/Services/LiquidTemplateManager.cs
public class LiquidTemplateManager : ILiquidTemplateManager
{
    private readonly IMemoryCache _memoryCache;
    private readonly LiquidViewParser _liquidViewParser;
    private readonly TemplateOptions _templateOptions;
    private readonly IServiceProvider _serviceProvider;

    public async Task<string> RenderStringAsync(string source, TextEncoder encoder, object model = null, 
        IEnumerable<KeyValuePair<string, FluidValue>> properties = null)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return null;
        }

        var result = GetCachedTemplate(source);
        var context = new LiquidTemplateContext(_serviceProvider, _templateOptions);

        if (properties != null)
        {
            foreach (var property in properties)
            {
                context.SetValue(property.Key, property.Value);
            }
        }

        return await result.RenderAsync(encoder, context, model);
    }

    private IFluidTemplate GetCachedTemplate(string source)
    {
        return _memoryCache.GetOrCreate(source, entry =>
        {
            entry.SetSlidingExpiration(TimeSpan.FromHours(1));
            entry.SetPriority(CacheItemPriority.NeverRemove);
            
            if (_liquidViewParser.TryParse(source, out var template, out var errors))
            {
                return template;
            }
            
            throw new InvalidOperationException($"Liquid template parsing failed: {string.Join(", ", errors)}");
        });
    }
}
```

## 🎯 View & Menu Caching Implementation

### 🧭 Menu Caching trong Themes

```html
<!-- TheTheme/Views/Layout.cshtml - Menu caching với context -->
<menu alias="alias:main-menu" 
      cache-id="main-menu" 
      cache-fixed-duration="00:05:00" 
      cache-tag="alias:main-menu" 
      cache-context="user.roles" />
```

### 🏷️ Dynamic Cache TagHelper Usage

```html
<!-- ✅ GOOD: Comprehensive caching implementation -->
<dynamic-cache cache-id="product-list" 
               vary-by="category,page" 
               dependencies="products,categories" 
               expires-sliding="00:15:00"
               enabled="true">
    @foreach (var product in Model.Products)
    {
        <div class="product-card">
            <h3>@product.Name</h3>
            <p>@product.Description</p>
            <span class="price">@product.Price.ToString("C")</span>
        </div>
    }
</dynamic-cache>

<!-- Widget caching với user context -->
<dynamic-cache cache-id="user-dashboard-@Model.UserId" 
               vary-by="user" 
               dependencies="user-data" 
               expires-after="01:00:00">
    <div class="dashboard">
        <h2>Welcome, @Model.UserName!</h2>
        <div class="stats">
            <span>Orders: @Model.OrderCount</span>
            <span>Points: @Model.Points</span>
        </div>
    </div>
</dynamic-cache>

<!-- Content caching với multiple contexts -->
<dynamic-cache cache-id="article-@Model.ContentItemId" 
               vary-by="user.roles,route" 
               dependencies="content:@Model.ContentItemId" 
               expires-on="@Model.PublishDate.AddDays(1)">
    <article>
        <h1>@Model.Title</h1>
        <div class="content">@Html.Raw(Model.Content)</div>
        <div class="meta">
            <span>Published: @Model.PublishDate</span>
            <span>Author: @Model.Author</span>
        </div>
    </article>
</dynamic-cache>
```

## 🚀 Advanced Performance Patterns

### 🏗️ Custom Cache Service Implementation

```csharp
// ✅ GOOD: Advanced caching service
public interface IAdvancedCacheService
{
    Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null, params string[] tags);
    Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, CacheContext context);
    Task InvalidateByTagAsync(string tag);
    Task InvalidateByPatternAsync(string pattern);
    Task WarmupCacheAsync(IEnumerable<string> keys);
}

public class AdvancedCacheService : IAdvancedCacheService
{
    private readonly IDynamicCacheService _dynamicCacheService;
    private readonly ITagCache _tagCache;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<AdvancedCacheService> _logger;

    public AdvancedCacheService(
        IDynamicCacheService dynamicCacheService,
        ITagCache tagCache,
        IMemoryCache memoryCache,
        ILogger<AdvancedCacheService> logger)
    {
        _dynamicCacheService = dynamicCacheService;
        _tagCache = tagCache;
        _memoryCache = memoryCache;
        _logger = logger;
    }

    public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null, params string[] tags)
    {
        var context = new CacheContext(key);
        
        if (expiration.HasValue)
        {
            context.WithExpiryAfter(expiration.Value);
        }
        
        if (tags?.Length > 0)
        {
            context.AddTag(tags);
        }

        return await GetOrSetAsync(key, factory, context);
    }

    public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, CacheContext context)
    {
        try
        {
            var cachedValue = await _dynamicCacheService.GetCachedValueAsync(context);
            
            if (cachedValue != null)
            {
                return JsonSerializer.Deserialize<T>(cachedValue);
            }

            var value = await factory();
            var serializedValue = JsonSerializer.Serialize(value);
            
            await _dynamicCacheService.SetCachedValueAsync(context, serializedValue);
            
            return value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in cache operation for key {Key}", key);
            return await factory(); // Fallback to direct execution
        }
    }

    public async Task InvalidateByTagAsync(string tag)
    {
        try
        {
            await _tagCache.RemoveTagAsync(tag);
            _logger.LogInformation("Invalidated cache entries with tag {Tag}", tag);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating cache by tag {Tag}", tag);
        }
    }

    public async Task InvalidateByPatternAsync(string pattern)
    {
        try
        {
            // Implementation would depend on cache provider capabilities
            // This is a simplified example
            var keys = await GetKeysByPatternAsync(pattern);
            
            foreach (var key in keys)
            {
                await _dynamicCacheService.GetCachedValueAsync(new CacheContext(key));
            }
            
            _logger.LogInformation("Invalidated cache entries matching pattern {Pattern}", pattern);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating cache by pattern {Pattern}", pattern);
        }
    }

    public async Task WarmupCacheAsync(IEnumerable<string> keys)
    {
        var tasks = keys.Select(async key =>
        {
            try
            {
                await _dynamicCacheService.GetCachedValueAsync(new CacheContext(key));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to warmup cache for key {Key}", key);
            }
        });

        await Task.WhenAll(tasks);
        _logger.LogInformation("Cache warmup completed for {Count} keys", keys.Count());
    }

    private async Task<IEnumerable<string>> GetKeysByPatternAsync(string pattern)
    {
        // This would need to be implemented based on your cache provider
        // Redis supports pattern matching, in-memory cache would need custom implementation
        return Enumerable.Empty<string>();
    }
}
```

### 🎯 Performance-Optimized Display Driver

```csharp
// ✅ GOOD: Display driver với comprehensive caching
public class OptimizedProductDisplayDriver : ContentPartDisplayDriver<ProductPart>
{
    private readonly IAdvancedCacheService _cacheService;
    private readonly IContentManager _contentManager;
    private readonly ILogger<OptimizedProductDisplayDriver> _logger;

    public OptimizedProductDisplayDriver(
        IAdvancedCacheService cacheService,
        IContentManager contentManager,
        ILogger<OptimizedProductDisplayDriver> logger)
    {
        _cacheService = cacheService;
        _contentManager = contentManager;
        _logger = logger;
    }

    public override async Task<IDisplayResult> DisplayAsync(ProductPart model, BuildDisplayContext context)
    {
        var cacheKey = $"product-display-{model.ContentItem.ContentItemId}-{context.DisplayType}";
        
        var viewModel = await _cacheService.GetOrSetAsync(
            cacheKey,
            async () => await BuildViewModelAsync(model, context),
            TimeSpan.FromMinutes(30),
            $"content:{model.ContentItem.ContentItemId}",
            "products",
            $"product-category:{model.Category}"
        );

        return Initialize<ProductPartViewModel>("ProductPart", vm =>
        {
            vm.Product = viewModel.Product;
            vm.RelatedProducts = viewModel.RelatedProducts;
            vm.Reviews = viewModel.Reviews;
            vm.ImageUrls = viewModel.ImageUrls;
        }).Location("Detail", "Content:1");
    }

    private async Task<ProductPartViewModel> BuildViewModelAsync(ProductPart model, BuildDisplayContext context)
    {
        var viewModel = new ProductPartViewModel
        {
            Product = model
        };

        // Parallel loading of related data
        var tasks = new[]
        {
            LoadRelatedProductsAsync(model).ContinueWith(t => viewModel.RelatedProducts = t.Result),
            LoadProductReviewsAsync(model).ContinueWith(t => viewModel.Reviews = t.Result),
            LoadProductImagesAsync(model).ContinueWith(t => viewModel.ImageUrls = t.Result)
        };

        await Task.WhenAll(tasks);

        return viewModel;
    }

    private async Task<IEnumerable<ProductPart>> LoadRelatedProductsAsync(ProductPart product)
    {
        var cacheKey = $"related-products-{product.Category}-{product.ContentItem.ContentItemId}";
        
        return await _cacheService.GetOrSetAsync(
            cacheKey,
            async () =>
            {
                var query = await _contentManager.Query<ProductPart>()
                    .Where(p => p.Category == product.Category && 
                               p.ContentItem.ContentItemId != product.ContentItem.ContentItemId)
                    .Take(5)
                    .ListAsync();
                
                return query.ToList();
            },
            TimeSpan.FromHours(1),
            $"product-category:{product.Category}",
            "products"
        );
    }

    private async Task<IEnumerable<ReviewPart>> LoadProductReviewsAsync(ProductPart product)
    {
        var cacheKey = $"product-reviews-{product.ContentItem.ContentItemId}";
        
        return await _cacheService.GetOrSetAsync(
            cacheKey,
            async () =>
            {
                var reviews = await _contentManager.Query<ReviewPart>()
                    .Where(r => r.ProductId == product.ContentItem.ContentItemId && r.IsApproved)
                    .OrderByDescending(r => r.CreatedUtc)
                    .Take(10)
                    .ListAsync();
                
                return reviews.ToList();
            },
            TimeSpan.FromMinutes(15),
            $"product-reviews:{product.ContentItem.ContentItemId}",
            "reviews"
        );
    }

    private async Task<IEnumerable<string>> LoadProductImagesAsync(ProductPart product)
    {
        var cacheKey = $"product-images-{product.ContentItem.ContentItemId}";
        
        return await _cacheService.GetOrSetAsync(
            cacheKey,
            async () =>
            {
                if (product.Images?.Paths?.Length > 0)
                {
                    return product.Images.Paths.Select(path => 
                        _mediaFileStore.MapPathToPublicUrl(path)).ToList();
                }
                
                return new[] { "/images/default-product.jpg" };
            },
            TimeSpan.FromHours(2),
            $"content:{product.ContentItem.ContentItemId}",
            "product-images"
        );
    }
}
```

### 🔧 Cache Invalidation Strategies

```csharp
// ✅ GOOD: Content handler với cache invalidation
public class ProductPartHandler : ContentPartHandler<ProductPart>
{
    private readonly IAdvancedCacheService _cacheService;
    private readonly ILogger<ProductPartHandler> _logger;

    public ProductPartHandler(
        IAdvancedCacheService cacheService,
        ILogger<ProductPartHandler> logger)
    {
        _cacheService = cacheService;
        _logger = logger;
    }

    public override async Task PublishedAsync(PublishContentContext context, ProductPart part)
    {
        await InvalidateProductCacheAsync(part);
    }

    public override async Task UpdatedAsync(UpdateContentContext context, ProductPart part)
    {
        await InvalidateProductCacheAsync(part);
    }

    public override async Task RemovedAsync(RemoveContentContext context, ProductPart part)
    {
        await InvalidateProductCacheAsync(part);
    }

    private async Task InvalidateProductCacheAsync(ProductPart part)
    {
        try
        {
            // Invalidate specific product cache
            await _cacheService.InvalidateByTagAsync($"content:{part.ContentItem.ContentItemId}");
            
            // Invalidate category-related cache
            await _cacheService.InvalidateByTagAsync($"product-category:{part.Category}");
            
            // Invalidate general products cache
            await _cacheService.InvalidateByTagAsync("products");
            
            _logger.LogInformation("Invalidated cache for product {ProductId}", part.ContentItem.ContentItemId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating cache for product {ProductId}", part.ContentItem.ContentItemId);
        }
    }
}
```

### 🚫 Common Anti-patterns

```csharp
// ❌ BAD: No caching strategy
public class SlowProductDisplayDriver : ContentPartDisplayDriver<ProductPart>
{
    public override async Task<IDisplayResult> DisplayAsync(ProductPart model, BuildDisplayContext context)
    {
        // Always hits database - no caching
        var relatedProducts = await LoadRelatedProductsAsync(model);
        var reviews = await LoadProductReviewsAsync(model);
        
        return Initialize<ProductPartViewModel>("ProductPart", vm =>
        {
            vm.Product = model;
            vm.RelatedProducts = relatedProducts;
            vm.Reviews = reviews;
        });
    }
}

// ❌ BAD: Cache without invalidation
public class BadCacheService
{
    private readonly IMemoryCache _cache;
    
    public async Task<T> GetAsync<T>(string key, Func<Task<T>> factory)
    {
        if (_cache.TryGetValue(key, out T value))
        {
            return value;
        }
        
        value = await factory();
        _cache.Set(key, value, TimeSpan.FromHours(24)); // Too long, no invalidation strategy
        return value;
    }
}

// ✅ GOOD: Proper caching with invalidation
public class GoodCacheService
{
    private readonly IAdvancedCacheService _cacheService;
    
    public async Task<T> GetAsync<T>(string key, Func<Task<T>> factory, params string[] tags)
    {
        return await _cacheService.GetOrSetAsync(
            key, 
            factory, 
            TimeSpan.FromMinutes(30), // Reasonable expiration
            tags // Proper invalidation tags
        );
    }
}
```

## 🎯 Kết luận

Performance & Optimization trong OrchardCore themes cung cấp:

1. **Dynamic Cache System** với IDynamicCache, IDynamicCacheService và tag-based invalidation
2. **DynamicCacheTagHelper** cho view-level caching với comprehensive options
3. **Resource Management** với CDN support, versioning và integrity hashes
4. **Background Tasks** cho scheduled operations và cache maintenance
5. **Template Caching** với Liquid template compilation caching
6. **Cache Context Management** với user, role, route-based discrimination
7. **Tag-based Invalidation** với DefaultTagCache và event handlers
8. **Memory Cache Fallback** cho distributed cache failures
9. **Advanced Caching Patterns** với custom cache services và strategies
10. **Performance Monitoring** với debug mode và cache statistics

Hệ thống này đảm bảo themes có thể đạt performance tối ưu với caching strategies toàn diện, resource optimization và background task processing hiệu quả.