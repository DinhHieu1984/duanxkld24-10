# 📦 Asset Management & Optimization trong OrchardCore - Phân tích Chi tiết từ Source Code

## 🎯 Tổng quan Asset Management System

OrchardCore sử dụng hệ thống quản lý tài nguyên phức tạp với các thành phần chính:

### 🏗️ Kiến trúc Resource Management

```csharp
// IResourceManager - Interface chính
public interface IResourceManager
{
    ResourceManifest InlineManifest { get; }
    ResourceDefinition FindResource(RequireSettings settings);
    RequireSettings RegisterResource(string resourceType, string resourceName);
    RequireSettings RegisterUrl(string resourceType, string resourcePath, string resourceDebugPath);
    
    // Script Management
    void RegisterHeadScript(IHtmlContent script);
    void RegisterFootScript(IHtmlContent script);
    
    // Style Management  
    void RegisterStyle(IHtmlContent style);
    
    // Meta & Link Management
    void RegisterLink(LinkEntry link);
    void RegisterMeta(MetaEntry meta);
    
    // Rendering Methods
    void RenderStylesheet(TextWriter writer);
    void RenderHeadScript(TextWriter writer);
    void RenderFootScript(TextWriter writer);
}
```

### 📋 ResourceManifest - Định nghĩa Tài nguyên

```csharp
// ResourceManifest.cs - Quản lý danh sách resources
public class ResourceManifest
{
    private readonly Dictionary<string, IDictionary<string, IList<ResourceDefinition>>> _resources;

    public ResourceDefinition DefineScript(string name)
    {
        return DefineResource("script", name);
    }

    public ResourceDefinition DefineStyle(string name)
    {
        return DefineResource("stylesheet", name);
    }

    public virtual ResourceDefinition DefineResource(string resourceType, string resourceName)
    {
        var definition = new ResourceDefinition(this, resourceType, resourceName);
        var resources = GetResources(resourceType);
        if (!resources.TryGetValue(resourceName, out var value))
        {
            value = new List<ResourceDefinition>();
            resources[resourceName] = value;
        }
        value.Add(definition);
        return definition;
    }
}
```

### 🔧 ResourceDefinition - Chi tiết Tài nguyên

```csharp
// ResourceDefinition.cs - Định nghĩa chi tiết từng resource
public class ResourceDefinition
{
    public string Name { get; private set; }
    public string Type { get; private set; }
    public string Version { get; private set; }
    public bool? AppendVersion { get; private set; }
    
    // URL Management
    public string Url { get; private set; }
    public string UrlDebug { get; private set; }
    public string UrlCdn { get; private set; }
    public string UrlCdnDebug { get; private set; }
    
    // Security & Integrity
    public string CdnIntegrity { get; private set; }
    public string CdnDebugIntegrity { get; private set; }
    public bool CdnSupportsSsl { get; private set; }
    
    // Dependencies & Culture
    public List<string> Dependencies { get; private set; }
    public string[] Cultures { get; private set; }
    public ResourcePosition Position { get; private set; }

    // Fluent API Methods
    public ResourceDefinition SetUrl(string url, string urlDebug = null)
    {
        Url = url;
        if (urlDebug != null) UrlDebug = urlDebug;
        return this;
    }

    public ResourceDefinition SetCdn(string cdnUrl, string cdnUrlDebug = null, bool? cdnSupportsSsl = null)
    {
        UrlCdn = cdnUrl;
        if (cdnUrlDebug != null) UrlCdnDebug = cdnUrlDebug;
        if (cdnSupportsSsl.HasValue) CdnSupportsSsl = cdnSupportsSsl.Value;
        return this;
    }

    public ResourceDefinition SetCdnIntegrity(string cdnIntegrity, string cdnDebugIntegrity = null)
    {
        CdnIntegrity = cdnIntegrity;
        if (cdnDebugIntegrity != null) CdnDebugIntegrity = cdnDebugIntegrity;
        return this;
    }

    public ResourceDefinition SetDependencies(params string[] dependencies)
    {
        Dependencies ??= [];
        Dependencies.AddRange(dependencies);
        return this;
    }

    public ResourceDefinition SetVersion(string version)
    {
        if (!System.Version.TryParse(version, out _))
        {
            throw new FormatException("The resource version should be in the form of major.minor[.build[.revision]].");
        }
        Version = version;
        return this;
    }
}
```

## 🏭 Asset Pipeline System

### 📄 Assets.json Configuration

```json
// Assets.json - Cấu hình build pipeline
[
  {
    "action": "sass",
    "name": "themes-admin",
    "source": "Assets/scss/themes.admin.scss",
    "tags": ["themes", "css"]
  },
  {
    "action": "parcel",
    "name": "themes-theme-manager",
    "source": "Assets/ts/theme-manager.ts",
    "dest": "wwwroot/Scripts/theme-manager/",
    "tags": ["themes", "js"]
  },
  {
    "action": "min",
    "name": "contentfields",
    "source": "Assets/js/vue-multiselect-userpicker.js",
    "tags": ["admin", "dashboard", "js"]
  },
  {
    "action": "copy",
    "name": "contentfields",
    "source": "Assets/js/iconpicker-custom.js",
    "tags": ["admin", "dashboard", "js"]
  }
]
```

### 🔄 Asset Actions

1. **SASS Compilation**
   - Biên dịch SCSS thành CSS
   - Hỗ trợ imports và variables
   - Tự động minification

2. **Parcel Bundling**
   - Bundle TypeScript/JavaScript
   - Tree shaking
   - Code splitting
   - Hot module replacement

3. **Minification**
   - Nén JavaScript files
   - Loại bỏ whitespace và comments
   - Obfuscation tên biến

4. **Copy Action**
   - Copy files không cần xử lý
   - Giữ nguyên structure

## 🚀 File Versioning & Caching

### 🔐 ShellFileVersionProvider

```csharp
// ShellFileVersionProvider.cs - Tự động versioning
public class ShellFileVersionProvider : IFileVersionProvider
{
    private const string VersionKey = "v";
    private static readonly MemoryCache _sharedCache = new(new MemoryCacheOptions());

    public string AddFileVersionToPath(PathString requestPathBase, string path)
    {
        // Kiểm tra cache trước
        if (_cache.TryGetValue(resolvedPath, out string value))
        {
            if (value.Length > 0)
            {
                return QueryHelpers.AddQueryString(path, VersionKey, value);
            }
            return path;
        }

        // Tính hash cho file
        foreach (var fileProvider in _fileProviders)
        {
            var fileInfo = fileProvider.GetFileInfo(resolvedPath);
            if (fileInfo.Exists)
            {
                value = GetHashForFile(fileInfo);
                
                // Cache kết quả
                if (fileProvider is IModuleStaticFileProvider)
                {
                    _sharedCache.Set(resolvedPath, value, cacheEntryOptions);
                }
                else
                {
                    _cache.Set(cacheKey, value, cacheEntryOptions);
                }

                return QueryHelpers.AddQueryString(path, VersionKey, value);
            }
        }
        
        return path;
    }

    private static string GetHashForFile(IFileInfo fileInfo)
    {
        using var sha256 = SHA256.Create();
        using var readStream = fileInfo.CreateReadStream();
        var hash = sha256.ComputeHash(readStream);
        return WebEncoders.Base64UrlEncode(hash);
    }
}
```

### 💾 Caching Strategy

1. **Tenant-level Cache**: Cache riêng cho từng tenant
2. **Shared Cache**: Cache chung cho module static files
3. **File Watching**: Tự động invalidate khi file thay đổi
4. **Memory Optimization**: Giới hạn size cache entries

## 🌐 CDN Support & Optimization

### ⚙️ ResourceManagementOptions

```csharp
// ResourceManagementOptions.cs - Cấu hình global
public class ResourceManagementOptions
{
    public bool UseCdn { get; set; }
    public string CdnBaseUrl { get; set; }
    public bool DebugMode { get; set; }
    public string Culture { get; set; }
    public bool AppendVersion { get; set; } = true;
    public string ContentBasePath { get; set; } = string.Empty;
    public HashSet<ResourceManifest> ResourceManifests { get; init; } = [];
}
```

### 🎯 RequireSettings - Runtime Configuration

```csharp
// RequireSettings.cs - Cấu hình runtime cho resource
public class RequireSettings
{
    public string Type { get; set; }
    public string Name { get; set; }
    public string Culture { get; set; }
    public bool DebugMode { get; set; }
    public bool CdnMode { get; set; }
    public string CdnBaseUrl { get; set; }
    public ResourceLocation Location { get; set; } // Head, Foot, Inline
    public string Version { get; set; }
    public bool? AppendVersion { get; set; }
    public List<string> Dependencies { get; set; }

    // Fluent API
    public RequireSettings AtHead() => AtLocation(ResourceLocation.Head);
    public RequireSettings AtFoot() => AtLocation(ResourceLocation.Foot);
    public RequireSettings UseCdn(bool useCdn) { CdnMode = useCdn; return this; }
    public RequireSettings UseDebugMode(bool debug) { DebugMode = debug; return this; }
    public RequireSettings UseCulture(string cultureName) { Culture = cultureName; return this; }
    public RequireSettings UseVersion(string version) { Version = version; return this; }
    public RequireSettings SetDependencies(params string[] dependencies)
    {
        Dependencies ??= [];
        Dependencies.AddRange(dependencies);
        return this;
    }
}
```

### 🔄 URL Resolution Logic

```csharp
// ResourceDefinition.GetTagBuilder() - Logic chọn URL
public TagBuilder GetTagBuilder(RequireSettings settings, string applicationPath, IFileVersionProvider fileVersionProvider)
{
    string url;
    
    // URL priority logic
    if (settings.DebugMode)
    {
        url = settings.CdnMode
            ? Coalesce(UrlCdnDebug, UrlDebug, UrlCdn, Url)      // CDN Debug mode
            : Coalesce(UrlDebug, Url, UrlCdnDebug, UrlCdn);     // Local Debug mode
    }
    else
    {
        url = settings.CdnMode
            ? Coalesce(UrlCdn, Url, UrlCdnDebug, UrlDebug)      // CDN Production mode
            : Coalesce(Url, UrlDebug, UrlCdn, UrlCdnDebug);     // Local Production mode
    }

    // Culture-specific URL
    if (!string.IsNullOrEmpty(settings.Culture))
    {
        var nearestCulture = FindNearestCulture(settings.Culture);
        if (!string.IsNullOrEmpty(nearestCulture))
        {
            url = Path.ChangeExtension(url, nearestCulture + Path.GetExtension(url));
        }
    }

    // Convert relative URL to absolute
    if (url != null && url.StartsWith("~/", StringComparison.Ordinal))
    {
        url = !string.IsNullOrEmpty(BasePath) 
            ? string.Concat(BasePath, url.AsSpan(1))
            : string.Concat(applicationPath, url.AsSpan(1));
    }

    // Add file version
    if (url != null && ((settings.AppendVersion.HasValue && settings.AppendVersion == true) ||
        (!settings.AppendVersion.HasValue && AppendVersion == true)))
    {
        url = fileVersionProvider.AddFileVersionToPath(applicationPath, url);
    }

    // Add CDN prefix
    if (url != null && !settings.DebugMode && !string.IsNullOrEmpty(settings.CdnBaseUrl) &&
        !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase) &&
        !url.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
    {
        url = settings.CdnBaseUrl + url;
    }

    return CreateTagBuilder(url);
}
```

## 🏷️ TagHelper System

### 📜 ScriptTagHelper

```csharp
// ScriptTagHelper.cs - Quản lý script tags
[HtmlTargetElement("script", Attributes = "asp-name")]
[HtmlTargetElement("script", Attributes = "asp-src")]
[HtmlTargetElement("script", Attributes = "at")]
public class ScriptTagHelper : TagHelper
{
    [HtmlAttributeName("asp-name")]
    public string Name { get; set; }

    [HtmlAttributeName("asp-src")]
    public string Src { get; set; }

    [HtmlAttributeName("asp-append-version")]
    public bool? AppendVersion { get; set; }

    public string CdnSrc { get; set; }
    public string DebugSrc { get; set; }
    public string DebugCdnSrc { get; set; }
    public bool? UseCdn { get; set; }
    public string DependsOn { get; set; }
    public string Version { get; set; }

    [HtmlAttributeName("at")]
    public ResourceLocation At { get; set; }

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        output.SuppressOutput();

        var hasName = !string.IsNullOrEmpty(Name);
        var hasSource = !string.IsNullOrEmpty(Src);

        if (!hasName && hasSource)
        {
            // Custom script URL
            var setting = _resourceManager.RegisterUrl("script", Src, DebugSrc);
            PopulateRequireSettings(setting, output, hasName: false);
            
            if (At == ResourceLocation.Inline)
            {
                RenderScript(output, setting);
            }
        }
        else if (hasName && !hasSource)
        {
            // Named resource
            var setting = _resourceManager.RegisterResource("script", Name);
            PopulateRequireSettings(setting, output, hasName: true);
            
            if (!string.IsNullOrEmpty(DependsOn))
            {
                setting.SetDependencies(DependsOn.Split(',', StringSplitOptions.TrimEntries));
            }
            
            if (At == ResourceLocation.Inline)
            {
                RenderScript(output, setting);
            }
        }
        else if (hasName && hasSource)
        {
            // Inline declaration
            PopulateResourceDefinition(_resourceManager.InlineManifest.DefineScript(Name));
            
            if (At != ResourceLocation.Unspecified)
            {
                var setting = _resourceManager.RegisterResource("script", Name);
                if (At == ResourceLocation.Inline)
                {
                    RenderScript(output, setting);
                }
            }
        }
    }
}
```

### 🎨 StyleTagHelper

```csharp
// StyleTagHelper.cs - Quản lý style tags
[HtmlTargetElement("style", Attributes = "asp-name")]
[HtmlTargetElement("style", Attributes = "asp-src")]
[HtmlTargetElement("style", Attributes = "at")]
public class StyleTagHelper : TagHelper
{
    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        output.SuppressOutput();

        var hasName = !string.IsNullOrEmpty(Name);
        var hasSource = !string.IsNullOrEmpty(Src);

        if (!hasName && hasSource)
        {
            // Custom style URL
            var setting = _resourceManager.RegisterUrl("stylesheet", Src, DebugSrc);
            
            // Default to Head location for styles
            if (At != ResourceLocation.Unspecified)
            {
                setting.AtLocation(At);
            }
            else
            {
                setting.AtLocation(ResourceLocation.Head);
            }

            if (At == ResourceLocation.Inline)
            {
                RenderStyle(output, setting);
            }
        }
        else if (hasName && !hasSource)
        {
            // Named style resource
            var setting = _resourceManager.RegisterResource("stylesheet", Name);
            
            if (At == ResourceLocation.Inline)
            {
                RenderStyle(output, setting);
            }
        }
        else if (hasName && hasSource)
        {
            // Inline style declaration
            PopulateResourceDefinition(_resourceManager.InlineManifest.DefineStyle(Name));
        }
        else
        {
            // Custom inline style content
            var childContent = await output.GetChildContentAsync();
            var builder = new TagBuilder("style");
            builder.InnerHtml.AppendHtml(childContent);
            builder.Attributes["type"] = "text/css";
            
            _resourceManager.RegisterStyle(builder);
        }
    }
}
```

### 🔗 ResourcesTagHelper

```csharp
// ResourcesTagHelper.cs - Render resources theo type
[HtmlTargetElement("resources", Attributes = nameof(Type))]
public class ResourcesTagHelper : TagHelper
{
    public ResourceTagType Type { get; set; } // Meta, HeadLink, Stylesheet, HeadScript, FootScript

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        try
        {
            await using var writer = new ZStringWriter();
            var processorContext = new ResourcesTagHelperProcessorContext(Type, writer);

            foreach (var processor in _processors)
            {
                await processor.ProcessAsync(processorContext);
            }

            output.Content.AppendHtml(writer.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while rendering {Type} resource.", Type);
        }
        finally
        {
            output.TagName = null;
        }
    }
}
```

## 🏗️ Theme Resource Configuration

### 📋 ResourceManagementOptionsConfiguration

```csharp
// TheAdmin/ResourceManagementOptionsConfiguration.cs
public sealed class ResourceManagementOptionsConfiguration : IConfigureOptions<ResourceManagementOptions>
{
    private static readonly ResourceManifest _manifest;

    static ResourceManagementOptionsConfiguration()
    {
        _manifest = new ResourceManifest();

        // Define admin script with dependencies
        _manifest
            .DefineScript("the-admin")
            .SetDependencies("bootstrap", "admin-main", "theme-manager", "jQuery", "Sortable")
            .SetUrl("~/TheAdmin/js/theadmin/TheAdmin.min.js", "~/TheAdmin/js/theadmin/TheAdmin.js")
            .SetVersion("1.0.0");

        // Define main admin script
        _manifest
            .DefineScript("admin-main")
            .SetUrl("~/TheAdmin/js/theadmin-main/TheAdmin-main.min.js", "~/TheAdmin/js/theadmin-main/TheAdmin-main.js")
            .SetDependencies("bootstrap", "theme-head", "js-cookie")
            .SetVersion("1.0.0");

        // Define admin styles
        _manifest
            .DefineStyle("the-admin")
            .SetUrl("~/TheAdmin/css/TheAdmin.min.css", "~/TheAdmin/css/TheAdmin.css")
            .SetVersion("1.0.0");
    }

    public void Configure(ResourceManagementOptions options)
    {
        options.ResourceManifests.Add(_manifest);
    }
}
```

### 🎯 TheBlogTheme Resource Configuration

```csharp
// TheBlogTheme/ResourceManagementOptionsConfiguration.cs
public sealed class ResourceManagementOptionsConfiguration : IConfigureOptions<ResourceManagementOptions>
{
    static ResourceManagementOptionsConfiguration()
    {
        _manifest = new ResourceManifest();

        // Bootstrap with CDN and integrity
        _manifest
            .DefineScript("TheBlogTheme-bootstrap-bundle")
            .SetCdn("https://cdn.jsdelivr.net/npm/bootstrap@5.1.3/dist/js/bootstrap.bundle.min.js", 
                   "https://cdn.jsdelivr.net/npm/bootstrap@5.1.3/dist/js/bootstrap.bundle.js")
            .SetCdnIntegrity("sha384-ka7Sk0Gln4gmtz2MlQnikT1wXgYsOg+OMhuP+IlRH9sENBO0LRn5q+8nbTov4+1p", 
                           "sha384-8fq7CZc5BnER+jVlJI2Jafpbn4A9320TKhNJfYP33nneHep7sUg/OD30x7fK09pS")
            .SetVersion("5.1.3");

        // Custom Bootstrap styles
        _manifest
            .DefineStyle("TheBlogTheme-bootstrap-oc")
            .SetUrl("~/TheBlogTheme/css/bootstrap-oc.min.css", "~/TheBlogTheme/css/bootstrap-oc.css")
            .SetVersion("1.0.0");

        // Theme scripts with dependencies
        _manifest
            .DefineScript("TheBlogTheme")
            .SetDependencies("TheBlogTheme-bootstrap-bundle")
            .SetUrl("~/TheBlogTheme/js/scripts.min.js", "~/TheBlogTheme/js/scripts.js")
            .SetVersion("6.0.7");

        // Theme styles
        _manifest
            .DefineStyle("TheBlogTheme")
            .SetUrl("~/TheBlogTheme/css/styles.min.css", "~/TheBlogTheme/css/styles.css")
            .SetVersion("6.0.7");
    }
}
```

## 🔗 Meta & Link Management

### 🏷️ MetaEntry

```csharp
// MetaEntry.cs - Quản lý meta tags
public class MetaEntry
{
    private readonly TagBuilder _builder = new("meta");

    public MetaEntry(string name = null, string property = null, string content = null, 
                    string httpEquiv = null, string charset = null)
    {
        _builder.TagRenderMode = TagRenderMode.SelfClosing;
        
        if (!string.IsNullOrEmpty(name)) Name = name;
        if (!string.IsNullOrEmpty(property)) Property = property;
        if (!string.IsNullOrEmpty(content)) Content = content;
        if (!string.IsNullOrEmpty(httpEquiv)) HttpEquiv = httpEquiv;
        if (!string.IsNullOrEmpty(charset)) Charset = charset;
    }

    // Properties with automatic attribute management
    public string Name
    {
        get => _builder.Attributes.TryGetValue("name", out var value) ? value : null;
        set => SetAttribute("name", value);
    }

    public string Property
    {
        get => _builder.Attributes.TryGetValue("property", out var value) ? value : null;
        set => SetAttribute("property", value);
    }

    public string Content
    {
        get => _builder.Attributes.TryGetValue("content", out var value) ? value : null;
        set => SetAttribute("content", value);
    }

    // Combine multiple meta entries
    public static MetaEntry Combine(MetaEntry meta1, MetaEntry meta2, string contentSeparator)
    {
        var newMeta = new MetaEntry();
        Merge(newMeta._builder.Attributes, meta1._builder.Attributes, meta2._builder.Attributes);
        
        if (!string.IsNullOrEmpty(meta1.Content) && !string.IsNullOrEmpty(meta2.Content))
        {
            newMeta.Content = meta1.Content + contentSeparator + meta2.Content;
        }
        
        return newMeta;
    }
}
```

### 🔗 LinkEntry

```csharp
// LinkEntry.cs - Quản lý link tags
public class LinkEntry
{
    private readonly TagBuilder _builder = new("link");

    public LinkEntry()
    {
        _builder.TagRenderMode = TagRenderMode.SelfClosing;
    }

    // Properties with automatic attribute management
    public string Rel
    {
        get => _builder.Attributes.TryGetValue("rel", out var value) ? value : null;
        set => SetAttribute("rel", value);
    }

    public string Href
    {
        get => _builder.Attributes.TryGetValue("href", out var value) ? value : null;
        set => SetAttribute("href", value);
    }

    public string Type
    {
        get => _builder.Attributes.TryGetValue("type", out var value) ? value : null;
        set => SetAttribute("type", value);
    }

    public bool AppendVersion { get; set; }
    public string Condition { get; set; }

    public IHtmlContent GetTag()
    {
        if (!string.IsNullOrEmpty(Condition))
        {
            var htmlBuilder = new HtmlContentBuilder();
            htmlBuilder.AppendHtml("<!--[if " + Condition + "]>");
            htmlBuilder.AppendHtml(_builder);
            htmlBuilder.AppendHtml("<![endif]-->");
            return htmlBuilder;
        }

        return _builder;
    }
}
```

## 🎯 Best Practices & Patterns

### ✅ Resource Definition Best Practices

```csharp
// ✅ GOOD: Comprehensive resource definition
_manifest
    .DefineScript("my-theme-script")
    .SetUrl("~/MyTheme/js/script.min.js", "~/MyTheme/js/script.js")           // Production & Debug URLs
    .SetCdn("https://cdn.example.com/script.min.js", "https://cdn.example.com/script.js")  // CDN URLs
    .SetCdnIntegrity("sha384-hash", "sha384-debug-hash")                      // Integrity hashes
    .SetDependencies("jquery", "bootstrap")                                   // Dependencies
    .SetVersion("1.2.3")                                                      // Version
    .SetCultures("en", "vi")                                                  // Supported cultures
    .ShouldAppendVersion(true);                                               // File versioning

// ✅ GOOD: Proper dependency chain
_manifest.DefineScript("base-script").SetUrl("~/js/base.js");
_manifest.DefineScript("feature-script").SetDependencies("base-script").SetUrl("~/js/feature.js");
_manifest.DefineScript("app-script").SetDependencies("feature-script").SetUrl("~/js/app.js");
```

### 🏷️ TagHelper Usage Patterns

```html
<!-- ✅ GOOD: Named resource with location -->
<script asp-name="jquery" at="Head"></script>
<style asp-name="bootstrap" at="Head"></style>

<!-- ✅ GOOD: Custom resource with versioning -->
<script asp-src="~/js/custom.js" asp-append-version="true" at="Foot"></script>
<style asp-src="~/css/custom.css" asp-append-version="true"></style>

<!-- ✅ GOOD: Resource with dependencies -->
<script asp-name="my-script" depends-on="jquery,bootstrap" at="Foot"></script>

<!-- ✅ GOOD: CDN with fallback -->
<script asp-name="jquery" cdn-src="https://code.jquery.com/jquery-3.6.0.min.js" 
        debug-cdn-src="https://code.jquery.com/jquery-3.6.0.js" use-cdn="true"></script>

<!-- ✅ GOOD: Inline script with dependencies -->
<script depends-on="jquery" at="Foot">
    $(document).ready(function() {
        console.log('Page loaded');
    });
</script>

<!-- ✅ GOOD: Resource rendering -->
<resources type="Meta" />
<resources type="HeadLink" />
<resources type="Stylesheet" />
<resources type="HeadScript" />
<resources type="FootScript" />
```

### 🔧 Assets.json Configuration

```json
[
  {
    "action": "sass",
    "name": "theme-styles",
    "source": "Assets/scss/main.scss",
    "dest": "wwwroot/css/",
    "tags": ["theme", "css"]
  },
  {
    "action": "parcel",
    "name": "theme-scripts",
    "source": "Assets/ts/main.ts",
    "dest": "wwwroot/js/",
    "tags": ["theme", "js"]
  },
  {
    "action": "min",
    "name": "vendor-scripts",
    "source": "Assets/js/vendor/*.js",
    "dest": "wwwroot/js/vendor.min.js",
    "tags": ["vendor", "js"]
  }
]
```

### 🚫 Common Anti-patterns

```csharp
// ❌ BAD: Missing debug URL
.SetUrl("~/js/script.min.js")  // Không có debug version

// ❌ BAD: Không set version
.SetUrl("~/js/script.js")      // Không có version để cache busting

// ❌ BAD: Circular dependencies
_manifest.DefineScript("a").SetDependencies("b");
_manifest.DefineScript("b").SetDependencies("a");  // Circular!

// ❌ BAD: Hardcode CDN trong code
.SetUrl("https://cdn.example.com/script.js")  // Nên dùng CDN riêng

// ❌ BAD: Không handle culture
.SetUrl("~/js/script.js")  // Không hỗ trợ đa ngôn ngữ
```

## 🎯 Kết luận

Asset Management & Optimization trong OrchardCore cung cấp:

1. **Resource Management System** hoàn chỉnh với manifest, definition, và manager
2. **Asset Pipeline** mạnh mẽ với SASS, Parcel, minification
3. **File Versioning** tự động với SHA256 hashing
4. **CDN Support** với fallback và integrity checking
5. **TagHelper System** linh hoạt cho script, style, meta, link
6. **Caching Strategy** thông minh với multi-level cache
7. **Culture Support** cho internationalization
8. **Dependency Management** với resolution tự động

Hệ thống này đảm bảo performance tối ưu, security cao, và developer experience tốt cho theme development trong OrchardCore.