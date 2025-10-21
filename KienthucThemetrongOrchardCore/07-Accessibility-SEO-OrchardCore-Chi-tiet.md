# ♿ Accessibility & SEO trong OrchardCore - Phân tích Chi tiết từ Source Code

## 🎯 Tổng quan Accessibility & SEO System

OrchardCore cung cấp hệ thống toàn diện cho Accessibility (WCAG compliance) và SEO optimization thông qua themes và modules:

## ♿ Accessibility Patterns

### 🏷️ ARIA Attributes Implementation

```html
<!-- TheTheme/Views/Layout.cshtml - Navigation với ARIA -->
<nav class="navbar navbar-expand-md fixed-top">
    <div class="container">
        <shape type="Branding" />
        <button type="button" class="navbar-toggler" 
                data-bs-toggle="collapse" 
                data-bs-target="#navbar" 
                aria-expanded="false" 
                aria-controls="navbar" 
                aria-label="Toggle navigation">
            <span class="navbar-toggler-icon"></span>
        </button>
        <div class="collapse navbar-collapse" id="navbar">
            <!-- Navigation content -->
        </div>
    </div>
</nav>
```

### 🔄 Theme Toggle với ARIA Support

```html
<!-- OrchardCore.Themes/Views/ToggleTheme.cshtml -->
<li class="nav-item">
    <div class="dropdown">
        <button class="btn btn-link nav-link dropdown-toggle" 
                id="bd-theme" 
                type="button" 
                aria-expanded="false" 
                data-bs-toggle="dropdown" 
                data-bs-display="static" 
                aria-label="@T["Toggle theme"]">
            <span class="theme-icon-active">
                <i class="fa-solid fa-circle-half-stroke"></i>
            </span>
            <span class="d-none" id="bd-theme-text">@T["Toggle theme"]</span>
        </button>
        
        <ul class="dropdown-menu dropdown-menu-end position-absolute" 
            aria-labelledby="bd-theme-text">
            <li>
                <button type="button" 
                        class="dropdown-item" 
                        data-bs-theme-value="auto" 
                        aria-pressed="false">
                    <span class="theme-icon">
                        <i class="fa-solid fa-circle-half-stroke"></i>
                    </span>
                    <span class="ps-2">@T["Auto"]</span>
                </button>
            </li>
            <li>
                <button type="button" 
                        class="dropdown-item active" 
                        data-bs-theme-value="light" 
                        aria-pressed="true">
                    <span class="theme-icon">
                        <i class="fa-solid fa-sun"></i>
                    </span>
                    <span class="ps-2">@T["Light"]</span>
                </button>
            </li>
            <li>
                <button type="button" 
                        class="dropdown-item" 
                        data-bs-theme-value="dark" 
                        aria-pressed="false">
                    <span class="theme-icon">
                        <i class="fa-solid fa-moon"></i>
                    </span>
                    <span class="ps-2">@T["Dark"]</span>
                </button>
            </li>
        </ul>
    </div>
</li>
```

### 🚨 Alert Messages với Role Support

```html
<!-- TheTheme/Views/Message.cshtml -->
@{
    string type = Model.Type.ToString().ToLowerInvariant();
    var bsClassName = type switch
    {
        "information" => "alert-info",
        "error" => "alert-danger",
        _ => $"alert-{type}",
    };
}

<div class="alert alert-dismissible @bsClassName fade show message-@type" 
     role="alert">
    @Model.Message
    <button type="button" 
            class="btn-close" 
            data-bs-dismiss="alert" 
            aria-label="Close"></button>
</div>
```

### 🧭 Pagination với Semantic Navigation

```html
<!-- TheTheme/Views/Pager.cshtml -->
@{
    Model.Metadata.Alternates.Clear();
    Model.Metadata.Type = "Pager_Links";
}

<nav aria-label="@T["Listing pages"]">
    @await DisplayAsync(Model)
</nav>
```

### 🔔 User Notifications với ARIA

```html
<!-- TheTheme/Views/UserNotificationNavbar.cshtml -->
<li class="nav-item dropdown text-end">
    <a type="button" 
       class="nav-link dropdown-toggle" 
       href="#" 
       role="button" 
       data-bs-toggle="dropdown" 
       data-bs-auto-close="outside" 
       aria-expanded="false" 
       aria-label="@T["Notifications"]">
        @if (Model.TotalUnread > 0)
        {
            <!-- Notification badge với proper labeling -->
            <span class="badge bg-danger">@Model.TotalUnread</span>
        }
    </a>
    <!-- Dropdown menu content -->
</li>
```

### 🌐 Internationalization & RTL Support

```html
<!-- Layout.cshtml - Language và Direction support -->
<!DOCTYPE html>
<html lang="@Orchard.CultureName()" 
      dir="@Orchard.CultureDir()" 
      data-bs-theme="@await ThemeTogglerService.CurrentTheme()" 
      data-tenant="@ThemeTogglerService.CurrentTenant">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>@RenderTitleSegments(Site.SiteName, "before")</title>
    
    @if (Orchard.IsRightToLeft())
    {
        <style asp-name="bootstrap-rtl" version="5" at="Head"></style>
        <style asp-name="TheTheme" depends-on="bootstrap-rtl" 
               asp-src="~/TheTheme/styles/theme.min.css" 
               debug-src="~/TheTheme/styles/theme.css" at="Foot"></style>
    }
    else
    {
        <style asp-name="bootstrap" version="5" at="Head"></style>
        <style asp-name="TheTheme" 
               asp-src="~/TheTheme/styles/theme.min.css" 
               debug-src="~/TheTheme/styles/theme.css" at="Foot"></style>
    }
</head>
```

### 🎨 Decorative Icons với aria-hidden

```html
<!-- ThemeEntry-Attributes.SummaryAdmin.cshtml -->
@if (!string.IsNullOrWhiteSpace(Model.Value.Extension.Manifest.Author))
{
    <li class="list-group-item">
        <small>
            <i class="fa-solid fa-user fa-width-auto" aria-hidden="true"></i>
            @Model.Value.Extension.Manifest.Author
        </small>
    </li>
}

@if (!string.IsNullOrWhiteSpace(Model.Value.Extension.Manifest.Website))
{
    <li class="list-group-item">
        <small>
            <i class="fa-solid fa-external-link fa-width-auto" aria-hidden="true"></i>
            <a href="@Model.Value.Extension.Manifest.Website" target="_blank">
                @Model.Value.Extension.Manifest.Website
            </a>
        </small>
    </li>
}

@if (Model.Value.Extension.Manifest.Tags.Any())
{
    <li class="list-group-item">
        <small>
            <i class="fa-solid fa-tags fa-width-auto" aria-hidden="true"></i> 
            @string.Join(", ", Model.Value.Extension.Manifest.Tags.ToArray())
        </small>
    </li>
}

<li class="list-group-item">
    <small>
        <i class="fa-solid fa-code-branch fa-width-auto" aria-hidden="true"></i> 
        @Model.Value.Extension.Manifest.Version
    </small>
</li>
```

### 🏗️ Semantic HTML Structure

```html
<!-- TheTheme/Views/Layout.cshtml - Semantic structure -->
<body>
    <nav class="navbar navbar-expand-md fixed-top">
        <!-- Navigation content -->
    </nav>
    
    @await RenderSectionAsync("Header", required: false)
    
    <main class="container">
        @await RenderSectionAsync("Messages", required: false)
        @await RenderBodyAsync()
    </main>
    
    @if (IsSectionDefined("Footer"))
    {
        <footer>
            <div class="container">
                @await RenderSectionAsync("Footer", required: false)
            </div>
        </footer>
    }
</body>
```

## 🔍 SEO Optimization System

### 📄 SeoMetaPart - Core SEO Model

```csharp
// OrchardCore.Seo/Models/SeoMetaPart.cs
public class SeoMetaPart : ContentPart
{
    public static readonly char[] InvalidCharactersForCanoncial = "?#[]@!$&'()*+,;=<>\\|%".ToCharArray();
    
    // Basic SEO Fields
    public string PageTitle { get; set; }
    
    [DefaultValue(true)]
    public bool Render { get; set; } = true;
    
    public string MetaDescription { get; set; }
    public string MetaKeywords { get; set; }
    public string Canonical { get; set; }
    public string MetaRobots { get; set; }
    public MetaEntry[] CustomMetaTags { get; set; } = [];
    
    // Social Media Images
    public MediaField DefaultSocialImage { get; set; }
    
    // Open Graph Properties
    public MediaField OpenGraphImage { get; set; }
    public string OpenGraphType { get; set; }
    public string OpenGraphTitle { get; set; }
    public string OpenGraphDescription { get; set; }
    
    // Twitter Card Properties
    public MediaField TwitterImage { get; set; }
    public string TwitterTitle { get; set; }
    public string TwitterDescription { get; set; }
    public string TwitterCard { get; set; }
    public string TwitterCreator { get; set; }
    public string TwitterSite { get; set; }
    
    // Structured Data
    public string GoogleSchema { get; set; }
}
```

### 🏷️ SEO Meta Handler

```csharp
// OrchardCore.Seo/Handlers/SeoMetaPartHandler.cs
public class SeoMetaPartHandler : ContentPartHandler<SeoMetaPart>
{
    private readonly IResourceManager _resourceManager;
    private readonly IContentManager _contentManager;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<SeoMetaPartHandler> _logger;

    public SeoMetaPartHandler(
        IResourceManager resourceManager,
        IContentManager contentManager,
        IHttpContextAccessor httpContextAccessor,
        ILogger<SeoMetaPartHandler> logger)
    {
        _resourceManager = resourceManager;
        _contentManager = contentManager;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public override Task PublishedAsync(PublishContentContext context, SeoMetaPart part)
    {
        return ProcessSeoMetaPartAsync(part);
    }

    public override Task UpdatedAsync(UpdateContentContext context, SeoMetaPart part)
    {
        return ProcessSeoMetaPartAsync(part);
    }

    private async Task ProcessSeoMetaPartAsync(SeoMetaPart part)
    {
        if (!part.Render)
        {
            return;
        }

        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            return;
        }

        // Page Title
        if (!string.IsNullOrWhiteSpace(part.PageTitle))
        {
            _resourceManager.SetMeta(new MetaEntry
            {
                Name = "title",
                Content = part.PageTitle
            });
        }

        // Meta Description
        if (!string.IsNullOrWhiteSpace(part.MetaDescription))
        {
            _resourceManager.SetMeta(new MetaEntry
            {
                Name = "description",
                Content = part.MetaDescription
            });
        }

        // Meta Keywords
        if (!string.IsNullOrWhiteSpace(part.MetaKeywords))
        {
            _resourceManager.SetMeta(new MetaEntry
            {
                Name = "keywords",
                Content = part.MetaKeywords
            });
        }

        // Canonical URL
        if (!string.IsNullOrWhiteSpace(part.Canonical))
        {
            _resourceManager.RegisterLink(new LinkEntry
            {
                Rel = "canonical",
                Href = part.Canonical
            });
        }

        // Robots Meta
        if (!string.IsNullOrWhiteSpace(part.MetaRobots))
        {
            _resourceManager.SetMeta(new MetaEntry
            {
                Name = "robots",
                Content = part.MetaRobots
            });
        }

        // Custom Meta Tags
        if (part.CustomMetaTags?.Length > 0)
        {
            foreach (var customMeta in part.CustomMetaTags)
            {
                if (!string.IsNullOrWhiteSpace(customMeta.Name) && 
                    !string.IsNullOrWhiteSpace(customMeta.Content))
                {
                    _resourceManager.SetMeta(customMeta);
                }
            }
        }

        // Open Graph Tags
        await ProcessOpenGraphAsync(part);

        // Twitter Card Tags
        await ProcessTwitterCardAsync(part);

        // Google Schema
        if (!string.IsNullOrWhiteSpace(part.GoogleSchema))
        {
            _resourceManager.RegisterHeadScript(new ScriptEntry
            {
                Type = "application/ld+json",
                Content = part.GoogleSchema
            });
        }
    }

    private async Task ProcessOpenGraphAsync(SeoMetaPart part)
    {
        if (!string.IsNullOrWhiteSpace(part.OpenGraphTitle))
        {
            _resourceManager.SetMeta(new MetaEntry
            {
                Property = "og:title",
                Content = part.OpenGraphTitle
            });
        }

        if (!string.IsNullOrWhiteSpace(part.OpenGraphDescription))
        {
            _resourceManager.SetMeta(new MetaEntry
            {
                Property = "og:description",
                Content = part.OpenGraphDescription
            });
        }

        if (!string.IsNullOrWhiteSpace(part.OpenGraphType))
        {
            _resourceManager.SetMeta(new MetaEntry
            {
                Property = "og:type",
                Content = part.OpenGraphType
            });
        }

        // Open Graph Image
        if (part.OpenGraphImage?.Paths?.Length > 0)
        {
            var imageUrl = part.OpenGraphImage.Paths[0];
            _resourceManager.SetMeta(new MetaEntry
            {
                Property = "og:image",
                Content = imageUrl
            });
        }
    }

    private async Task ProcessTwitterCardAsync(SeoMetaPart part)
    {
        if (!string.IsNullOrWhiteSpace(part.TwitterCard))
        {
            _resourceManager.SetMeta(new MetaEntry
            {
                Name = "twitter:card",
                Content = part.TwitterCard
            });
        }

        if (!string.IsNullOrWhiteSpace(part.TwitterTitle))
        {
            _resourceManager.SetMeta(new MetaEntry
            {
                Name = "twitter:title",
                Content = part.TwitterTitle
            });
        }

        if (!string.IsNullOrWhiteSpace(part.TwitterDescription))
        {
            _resourceManager.SetMeta(new MetaEntry
            {
                Name = "twitter:description",
                Content = part.TwitterDescription
            });
        }

        if (!string.IsNullOrWhiteSpace(part.TwitterSite))
        {
            _resourceManager.SetMeta(new MetaEntry
            {
                Name = "twitter:site",
                Content = part.TwitterSite
            });
        }

        if (!string.IsNullOrWhiteSpace(part.TwitterCreator))
        {
            _resourceManager.SetMeta(new MetaEntry
            {
                Name = "twitter:creator",
                Content = part.TwitterCreator
            });
        }

        // Twitter Image
        if (part.TwitterImage?.Paths?.Length > 0)
        {
            var imageUrl = part.TwitterImage.Paths[0];
            _resourceManager.SetMeta(new MetaEntry
            {
                Name = "twitter:image",
                Content = imageUrl
            });
        }
    }
}
```

### 🗺️ Sitemap Generation System

```csharp
// OrchardCore.Sitemaps/Services/SitemapManager.cs
public class SitemapManager : ISitemapManager
{
    private readonly ISitemapIdGenerator _sitemapIdGenerator;
    private readonly IEnumerable<ISitemapSourceBuilder> _sitemapSourceBuilders;
    private readonly IEnumerable<ISitemapSourceModifiedDateProvider> _sitemapSourceModifiedDateProviders;
    private readonly ILogger<SitemapManager> _logger;

    public SitemapManager(
        ISitemapIdGenerator sitemapIdGenerator,
        IEnumerable<ISitemapSourceBuilder> sitemapSourceBuilders,
        IEnumerable<ISitemapSourceModifiedDateProvider> sitemapSourceModifiedDateProviders,
        ILogger<SitemapManager> logger)
    {
        _sitemapIdGenerator = sitemapIdGenerator;
        _sitemapSourceBuilders = sitemapSourceBuilders;
        _sitemapSourceModifiedDateProviders = sitemapSourceModifiedDateProviders;
        _logger = logger;
    }

    public async Task<XDocument> BuildSitemapAsync(Sitemap sitemap, SitemapBuilderContext context)
    {
        var xmlNamespace = XNamespace.Get("http://www.sitemaps.org/schemas/sitemap/0.9");
        var root = new XElement(xmlNamespace + "urlset");

        foreach (var source in sitemap.SitemapSources)
        {
            var sourceBuilder = _sitemapSourceBuilders
                .FirstOrDefault(x => x.Name == source.GetType().Name);

            if (sourceBuilder != null)
            {
                try
                {
                    await sourceBuilder.BuildSitemapAsync(source, context);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error building sitemap source {SourceType}", source.GetType().Name);
                }
            }
        }

        // Add sitemap entries to XML
        foreach (var sitemapEntry in context.Response.ResponseElements)
        {
            var url = new XElement(xmlNamespace + "url");
            
            url.Add(new XElement(xmlNamespace + "loc", sitemapEntry.Location));
            
            if (sitemapEntry.LastModified.HasValue)
            {
                url.Add(new XElement(xmlNamespace + "lastmod", 
                    sitemapEntry.LastModified.Value.ToString("yyyy-MM-ddTHH:mm:sszzz")));
            }
            
            if (sitemapEntry.ChangeFrequency.HasValue)
            {
                url.Add(new XElement(xmlNamespace + "changefreq", 
                    sitemapEntry.ChangeFrequency.Value.ToString().ToLowerInvariant()));
            }
            
            if (sitemapEntry.Priority.HasValue)
            {
                url.Add(new XElement(xmlNamespace + "priority", 
                    sitemapEntry.Priority.Value.ToString("F1", CultureInfo.InvariantCulture)));
            }

            root.Add(url);
        }

        var document = new XDocument(new XDeclaration("1.0", "UTF-8", null), root);
        return document;
    }
}
```

### 🤖 Robots.txt Generation

```csharp
// OrchardCore.Seo/Services/RobotsMiddleware.cs
public class RobotsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RobotsMiddleware> _logger;

    public RobotsMiddleware(RequestDelegate next, ILogger<RobotsMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/robots.txt", StringComparison.OrdinalIgnoreCase))
        {
            var robotsProviders = context.RequestServices.GetServices<IRobotsProvider>();
            var stringBuilder = new StringBuilder();

            foreach (var provider in robotsProviders)
            {
                try
                {
                    var robotsContent = await provider.GetRobotsContentAsync();
                    if (!string.IsNullOrWhiteSpace(robotsContent))
                    {
                        stringBuilder.AppendLine(robotsContent);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting robots content from provider {ProviderType}", 
                        provider.GetType().Name);
                }
            }

            var robots = stringBuilder.ToString();
            
            context.Response.ContentType = "text/plain";
            await context.Response.WriteAsync(robots);
            return;
        }

        await _next(context);
    }
}

// OrchardCore.Seo/Services/SiteSettingsRobotsProvider.cs
public class SiteSettingsRobotsProvider : IRobotsProvider
{
    private readonly ISiteService _siteService;

    public SiteSettingsRobotsProvider(ISiteService siteService)
    {
        _siteService = siteService;
    }

    public async Task<string> GetRobotsContentAsync()
    {
        var robotsSettings = await _siteService.GetSettingsAsync<RobotsSettings>();
        return robotsSettings?.Content ?? string.Empty;
    }
}
```

### 📋 HeadMeta Zone System

```html
<!-- TheTheme/Views/Widget.Wrapper-Zone-HeadMeta.cshtml -->
@model dynamic
@* 
The HeadMeta zone is a convention for placement in the <head> section of a layout.
The <head> section should not contain html, so by convention widgets are rendered with an empty wrapper.    
*@
@await DisplayAsync(Model.Content)
```

```html
<!-- Layout.cshtml - HeadMeta section integration -->
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>@RenderTitleSegments(Site.SiteName, "before")</title>
    
    <!-- Resource management -->
    <resources type="Header" />
    
    <!-- HeadMeta zone for widgets and custom meta -->
    @await RenderSectionAsync("HeadMeta", required: false)
</head>
```

### 🏷️ Title Part Integration

```csharp
// OrchardCore.Title/Models/TitlePart.cs
public class TitlePart : ContentPart
{
    public string Title { get; set; }
}

// OrchardCore.Title/Handlers/TitlePartHandler.cs
public class TitlePartHandler : ContentPartHandler<TitlePart>
{
    public override Task GetContentItemAspectAsync(ContentItemAspectContext context, TitlePart part)
    {
        return context.ForAsync<TitleAspect>(aspect =>
        {
            aspect.Title = part.Title;
            return Task.CompletedTask;
        });
    }
}
```

```html
<!-- OrchardCore.Title/Views/TitlePart.cshtml -->
@model OrchardCore.Title.ViewModels.TitlePartViewModel

<h1>@Model.Title</h1>
```

## 🎯 Best Practices & Implementation

### ✅ Accessibility Best Practices

```html
<!-- ✅ GOOD: Comprehensive accessibility implementation -->
<nav class="main-navigation" aria-label="@T["Main navigation"]">
    <ul class="nav-list" role="menubar">
        <li class="nav-item" role="none">
            <a href="/" class="nav-link" role="menuitem" aria-current="page">
                @T["Home"]
            </a>
        </li>
        <li class="nav-item dropdown" role="none">
            <button class="nav-link dropdown-toggle" 
                    role="menuitem" 
                    aria-expanded="false" 
                    aria-haspopup="true"
                    data-bs-toggle="dropdown">
                @T["Products"]
            </button>
            <ul class="dropdown-menu" role="menu" aria-labelledby="products-menu">
                <li role="none">
                    <a href="/products/software" class="dropdown-item" role="menuitem">
                        @T["Software"]
                    </a>
                </li>
                <li role="none">
                    <a href="/products/hardware" class="dropdown-item" role="menuitem">
                        @T["Hardware"]
                    </a>
                </li>
            </ul>
        </li>
    </ul>
</nav>

<!-- Skip to content link -->
<a class="skip-link sr-only sr-only-focusable" href="#main-content">
    @T["Skip to main content"]
</a>

<main id="main-content" tabindex="-1">
    <!-- Main content -->
</main>
```

### 🔍 SEO Implementation Pattern

```csharp
// ✅ GOOD: Custom SEO service
public interface ICustomSeoService
{
    Task SetPageSeoAsync(string title, string description, string keywords = null);
    Task SetOpenGraphAsync(string title, string description, string imageUrl, string type = "website");
    Task SetTwitterCardAsync(string title, string description, string imageUrl, string cardType = "summary_large_image");
    Task AddStructuredDataAsync(object schemaData);
}

public class CustomSeoService : ICustomSeoService
{
    private readonly IResourceManager _resourceManager;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<CustomSeoService> _logger;

    public CustomSeoService(
        IResourceManager resourceManager,
        IHttpContextAccessor httpContextAccessor,
        ILogger<CustomSeoService> logger)
    {
        _resourceManager = resourceManager;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task SetPageSeoAsync(string title, string description, string keywords = null)
    {
        try
        {
            // Set page title
            if (!string.IsNullOrWhiteSpace(title))
            {
                _resourceManager.SetMeta(new MetaEntry
                {
                    Name = "title",
                    Content = title
                });
                
                // Also set as document title
                _resourceManager.AppendMeta(new MetaEntry
                {
                    Name = "title",
                    Content = title
                }, ",");
            }

            // Set meta description
            if (!string.IsNullOrWhiteSpace(description))
            {
                _resourceManager.SetMeta(new MetaEntry
                {
                    Name = "description",
                    Content = description
                });
            }

            // Set meta keywords
            if (!string.IsNullOrWhiteSpace(keywords))
            {
                _resourceManager.SetMeta(new MetaEntry
                {
                    Name = "keywords",
                    Content = keywords
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting page SEO");
        }
    }

    public async Task SetOpenGraphAsync(string title, string description, string imageUrl, string type = "website")
    {
        try
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var currentUrl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}{httpContext.Request.Path}";

            _resourceManager.SetMeta(new MetaEntry { Property = "og:title", Content = title });
            _resourceManager.SetMeta(new MetaEntry { Property = "og:description", Content = description });
            _resourceManager.SetMeta(new MetaEntry { Property = "og:type", Content = type });
            _resourceManager.SetMeta(new MetaEntry { Property = "og:url", Content = currentUrl });
            
            if (!string.IsNullOrWhiteSpace(imageUrl))
            {
                _resourceManager.SetMeta(new MetaEntry { Property = "og:image", Content = imageUrl });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting Open Graph data");
        }
    }

    public async Task SetTwitterCardAsync(string title, string description, string imageUrl, string cardType = "summary_large_image")
    {
        try
        {
            _resourceManager.SetMeta(new MetaEntry { Name = "twitter:card", Content = cardType });
            _resourceManager.SetMeta(new MetaEntry { Name = "twitter:title", Content = title });
            _resourceManager.SetMeta(new MetaEntry { Name = "twitter:description", Content = description });
            
            if (!string.IsNullOrWhiteSpace(imageUrl))
            {
                _resourceManager.SetMeta(new MetaEntry { Name = "twitter:image", Content = imageUrl });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting Twitter Card data");
        }
    }

    public async Task AddStructuredDataAsync(object schemaData)
    {
        try
        {
            var json = JsonSerializer.Serialize(schemaData, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            });

            _resourceManager.RegisterHeadScript(new ScriptEntry
            {
                Type = "application/ld+json",
                Content = json
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding structured data");
        }
    }
}
```

### 🏗️ Custom Display Driver với SEO

```csharp
// ✅ GOOD: Display driver với SEO integration
public class ProductDisplayDriver : ContentPartDisplayDriver<ProductPart>
{
    private readonly ICustomSeoService _seoService;
    private readonly IMediaFileStore _mediaFileStore;

    public ProductDisplayDriver(
        ICustomSeoService seoService,
        IMediaFileStore mediaFileStore)
    {
        _seoService = seoService;
        _mediaFileStore = mediaFileStore;
    }

    public override async Task<IDisplayResult> DisplayAsync(ProductPart model, BuildDisplayContext context)
    {
        // Set SEO data for product page
        if (context.DisplayType == "Detail")
        {
            await SetProductSeoAsync(model);
        }

        return Initialize<ProductPartViewModel>("ProductPart", async viewModel =>
        {
            viewModel.Product = model;
            viewModel.ImageUrl = await GetProductImageUrlAsync(model);
        }).Location("Detail", "Content:1");
    }

    private async Task SetProductSeoAsync(ProductPart product)
    {
        // Basic SEO
        var title = $"{product.Name} - {product.Category}";
        var description = !string.IsNullOrWhiteSpace(product.Description) 
            ? product.Description.Substring(0, Math.Min(160, product.Description.Length))
            : $"Buy {product.Name} online. High quality {product.Category} products.";
        var keywords = $"{product.Name}, {product.Category}, {string.Join(", ", product.Tags)}";

        await _seoService.SetPageSeoAsync(title, description, keywords);

        // Open Graph
        var imageUrl = await GetProductImageUrlAsync(product);
        await _seoService.SetOpenGraphAsync(title, description, imageUrl, "product");

        // Twitter Card
        await _seoService.SetTwitterCardAsync(title, description, imageUrl);

        // Structured Data (Product Schema)
        var structuredData = new
        {
            Context = "https://schema.org/",
            Type = "Product",
            Name = product.Name,
            Description = product.Description,
            Image = imageUrl,
            Brand = new
            {
                Type = "Brand",
                Name = product.Brand
            },
            Offers = new
            {
                Type = "Offer",
                Price = product.Price.ToString("F2"),
                PriceCurrency = "USD",
                Availability = product.InStock ? "https://schema.org/InStock" : "https://schema.org/OutOfStock"
            }
        };

        await _seoService.AddStructuredDataAsync(structuredData);
    }

    private async Task<string> GetProductImageUrlAsync(ProductPart product)
    {
        if (product.Images?.Paths?.Length > 0)
        {
            var imagePath = product.Images.Paths[0];
            return _mediaFileStore.MapPathToPublicUrl(imagePath);
        }
        return "/images/default-product.jpg";
    }
}
```

### 🚫 Common Anti-patterns

```html
<!-- ❌ BAD: Poor accessibility -->
<div onclick="toggleMenu()">Menu</div> <!-- Should be button -->
<img src="logo.jpg"> <!-- Missing alt attribute -->
<div class="alert">Error occurred</div> <!-- Missing role="alert" -->

<!-- ❌ BAD: Poor SEO -->
<title>Page</title> <!-- Generic title -->
<!-- Missing meta description -->
<!-- No structured data -->

<!-- ✅ GOOD: Proper accessibility -->
<button type="button" onclick="toggleMenu()" aria-expanded="false" aria-controls="menu">
    Menu
</button>
<img src="logo.jpg" alt="Company Logo">
<div class="alert" role="alert">Error occurred</div>

<!-- ✅ GOOD: Proper SEO -->
<title>Product Name - Category | Company Name</title>
<meta name="description" content="Detailed product description for SEO">
<script type="application/ld+json">
{
  "@context": "https://schema.org/",
  "@type": "Product",
  "name": "Product Name"
}
</script>
```

## 🎯 Kết luận

Accessibility & SEO trong OrchardCore themes cung cấp:

1. **ARIA Attributes** cho screen readers và assistive technologies
2. **Semantic HTML** với proper heading hierarchy và landmarks
3. **Internationalization** với language và direction support
4. **SEO Meta System** với SeoMetaPart và comprehensive meta tag management
5. **Open Graph & Twitter Cards** cho social media optimization
6. **Structured Data** với JSON-LD schema support
7. **Sitemap Generation** tự động với caching và optimization
8. **Robots.txt** dynamic generation với provider pattern
9. **HeadMeta Zone** cho custom meta tag injection
10. **Resource Management** integration cho SEO assets

Hệ thống này đảm bảo themes có thể đạt WCAG compliance và SEO optimization tốt nhất, cung cấp trải nghiệm tốt cho tất cả người dùng và search engines.