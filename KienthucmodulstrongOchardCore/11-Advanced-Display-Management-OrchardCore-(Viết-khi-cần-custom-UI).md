# 11. Advanced Display Management trong OrchardCore
## (Viết khi cần custom UI, themes, widgets)

> **Khi nào sử dụng**: Viết **KHI CẦN** custom UI components, theme integration, widget systems, shape templates

---

## 🎯 **TỔNG QUAN ADVANCED DISPLAY MANAGEMENT**

Advanced Display Management trong OrchardCore là hệ thống mạnh mẽ cho phép:
- **Shape System**: Tạo và quản lý UI components động
- **Theme Integration**: Tích hợp themes và templates
- **Widget Management**: Quản lý widgets và zones
- **Placement System**: Điều khiển vị trí hiển thị components
- **Template Overrides**: Override templates theo context

---

## 🏗️ **CORE CONCEPTS**

### **1. Shape System Architecture**

#### **Shape Definition Pattern**
```csharp
// Shape cơ bản
public class Shape : IShape, IPositioned
{
    public ShapeMetadata Metadata { get; } = new ShapeMetadata();
    public string Id { get; set; }
    public string TagName { get; set; }
    public IList<string> Classes { get; }
    public IDictionary<string, string> Attributes { get; }
    public IReadOnlyList<IPositioned> Items { get; }
    public string Position { get; set; }
    
    public virtual ValueTask<IShape> AddAsync(object item, string position)
    {
        // Logic thêm item vào shape
        return ValueTask.FromResult<IShape>(this);
    }
}

// Custom Shape cho module
public class ProductCardShape : Shape
{
    public string ProductName { get; set; }
    public decimal Price { get; set; }
    public string ImageUrl { get; set; }
    public string Description { get; set; }
    public bool IsOnSale { get; set; }
}
```

#### **Shape Factory Pattern**
```csharp
public class ProductShapeProvider : IShapeTableProvider
{
    public void Discover(ShapeTableBuilder builder)
    {
        builder.Describe("ProductCard")
            .OnCreating(creating => creating.Create = () => new ProductCardShape())
            .OnDisplaying(displaying =>
            {
                var shape = displaying.Shape as ProductCardShape;
                
                // Thêm CSS classes động
                shape.Classes.Add("product-card");
                if (shape.IsOnSale)
                {
                    shape.Classes.Add("on-sale");
                }
                
                // Thêm attributes
                shape.Attributes["data-product-id"] = shape.Id;
                shape.Attributes["data-price"] = shape.Price.ToString();
            });
            
        builder.Describe("ProductList")
            .OnCreating(creating => creating.Create = () => new Shape())
            .OnDisplaying(displaying =>
            {
                displaying.Shape.Metadata.Alternates.Add("ProductList__Grid");
                displaying.Shape.Metadata.Alternates.Add("ProductList__List");
            });
    }
}
```

### **2. Display Driver Pattern**

#### **Content Part Display Driver**
```csharp
public class ProductPartDisplayDriver : ContentPartDisplayDriver<ProductPart>
{
    private readonly IProductService _productService;
    private readonly IMediaFileStore _mediaFileStore;
    
    public ProductPartDisplayDriver(
        IProductService productService,
        IMediaFileStore mediaFileStore)
    {
        _productService = productService;
        _mediaFileStore = mediaFileStore;
    }
    
    // Display cho frontend
    public override async Task<IDisplayResult> DisplayAsync(
        ProductPart part, 
        BuildPartDisplayContext context)
    {
        return Initialize<ProductPartViewModel>(GetDisplayShapeType(context), async model =>
        {
            model.ProductPart = part;
            model.Product = await _productService.GetProductAsync(part.ProductId);
            model.ImageUrl = await GetProductImageUrlAsync(part.ImagePath);
            model.RelatedProducts = await _productService.GetRelatedProductsAsync(part.ProductId);
            
            // Thêm metadata cho SEO
            model.MetaTitle = part.MetaTitle ?? model.Product.Name;
            model.MetaDescription = part.MetaDescription ?? model.Product.Description;
        })
        .Location("Detail", "Content:5")
        .Location("Summary", "Content:1");
    }
    
    // Editor cho admin
    public override IDisplayResult Edit(ProductPart part, BuildPartEditorContext context)
    {
        return Initialize<ProductPartEditViewModel>(GetEditorShapeType(context), model =>
        {
            model.ProductPart = part;
            model.ProductId = part.ProductId;
            model.Price = part.Price;
            model.IsOnSale = part.IsOnSale;
            model.SalePrice = part.SalePrice;
            model.ImagePath = part.ImagePath;
            model.MetaTitle = part.MetaTitle;
            model.MetaDescription = part.MetaDescription;
        });
    }
    
    // Update từ editor
    public override async Task<IDisplayResult> UpdateAsync(
        ProductPart part, 
        UpdatePartEditorContext context)
    {
        var model = new ProductPartEditViewModel();
        
        if (await context.Updater.TryUpdateModelAsync(model, Prefix))
        {
            part.ProductId = model.ProductId;
            part.Price = model.Price;
            part.IsOnSale = model.IsOnSale;
            part.SalePrice = model.SalePrice;
            part.ImagePath = model.ImagePath;
            part.MetaTitle = model.MetaTitle;
            part.MetaDescription = model.MetaDescription;
            
            // Validation
            if (part.IsOnSale && part.SalePrice >= part.Price)
            {
                context.Updater.ModelState.AddModelError(
                    nameof(model.SalePrice), 
                    "Sale price must be less than regular price");
            }
        }
        
        return Edit(part, context);
    }
    
    private async Task<string> GetProductImageUrlAsync(string imagePath)
    {
        if (string.IsNullOrEmpty(imagePath))
            return "/images/no-image.jpg";
            
        var fileInfo = await _mediaFileStore.GetFileInfoAsync(imagePath);
        return fileInfo?.Exists == true ? _mediaFileStore.MapPathToPublicUrl(imagePath) : "/images/no-image.jpg";
    }
}
```

### **3. Placement System**

#### **Placement.json Configuration**
```json
{
  "ProductPart": [
    {
      "place": "Content:5",
      "displayType": "Detail"
    },
    {
      "place": "Content:1", 
      "displayType": "Summary"
    },
    {
      "place": "-",
      "displayType": "SummaryAdmin"
    }
  ],
  "ProductPart_Edit": [
    {
      "place": "Content:1"
    }
  ],
  "ProductCard": [
    {
      "place": "Content:0",
      "contentType": ["ProductPage"],
      "displayType": "Detail"
    },
    {
      "place": "Content:0",
      "contentType": ["ProductListing"],
      "displayType": "Summary"
    }
  ],
  "ProductList": [
    {
      "place": "Content:0",
      "path": "/products*",
      "alternates": ["ProductList__Grid"]
    },
    {
      "place": "Content:0", 
      "path": "/shop*",
      "alternates": ["ProductList__List"]
    }
  ]
}
```

#### **Dynamic Placement Provider**
```csharp
public class ProductPlacementProvider : IPlacementNodeFilterProvider
{
    private readonly IProductService _productService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    
    public ProductPlacementProvider(
        IProductService productService,
        IHttpContextAccessor httpContextAccessor)
    {
        _productService = productService;
        _httpContextAccessor = httpContextAccessor;
    }
    
    public string Key => "product";
    
    public bool IsMatch(ShapePlacementContext context, JToken expression)
    {
        if (context.Content?.ContentItem?.ContentType != "Product")
            return false;
            
        var productPart = context.Content.As<ProductPart>();
        if (productPart == null)
            return false;
            
        var condition = expression.Value<string>();
        
        return condition switch
        {
            "featured" => productPart.IsFeatured,
            "on-sale" => productPart.IsOnSale,
            "new" => productPart.CreatedUtc > DateTime.UtcNow.AddDays(-30),
            "category" => CheckCategoryCondition(productPart, expression),
            _ => false
        };
    }
    
    private bool CheckCategoryCondition(ProductPart productPart, JToken expression)
    {
        var categoryName = expression["category"]?.Value<string>();
        return productPart.Categories?.Contains(categoryName) == true;
    }
}
```

---

## 🎨 **THEME INTEGRATION PATTERNS**

### **1. Theme Shape Provider**
```csharp
public class MyThemeShapeProvider : IShapeTableProvider
{
    public void Discover(ShapeTableBuilder builder)
    {
        // Hero Section Shape
        builder.Describe("HeroSection")
            .OnCreating(creating => creating.Create = () => new Shape())
            .OnDisplaying(displaying =>
            {
                var shape = displaying.Shape;
                shape.Classes.Add("hero-section");
                shape.Metadata.Alternates.Add("HeroSection__Home");
                shape.Metadata.Alternates.Add("HeroSection__Landing");
            });
            
        // Card Component Shape
        builder.Describe("Card")
            .OnCreating(creating => creating.Create = () => new Shape())
            .OnDisplaying(displaying =>
            {
                var shape = displaying.Shape;
                var cardType = shape.Properties.ContainsKey("CardType") 
                    ? shape.Properties["CardType"].ToString() 
                    : "default";
                    
                shape.Classes.Add("card");
                shape.Classes.Add($"card--{cardType}");
                shape.Metadata.Alternates.Add($"Card__{cardType}");
            });
            
        // Navigation Shape
        builder.Describe("Navigation")
            .OnCreating(creating => creating.Create = () => new Shape())
            .OnDisplaying(displaying =>
            {
                var shape = displaying.Shape;
                var httpContext = displaying.ServiceProvider.GetService<IHttpContextAccessor>()?.HttpContext;
                
                if (httpContext != null)
                {
                    var isHomePage = httpContext.Request.Path == "/";
                    if (isHomePage)
                    {
                        shape.Metadata.Alternates.Add("Navigation__Home");
                    }
                }
            });
    }
}
```

### **2. Theme Layout Management**
```csharp
public class ThemeLayoutProvider : ILayoutAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IShapeFactory _shapeFactory;
    
    public ThemeLayoutProvider(
        IHttpContextAccessor httpContextAccessor,
        IShapeFactory shapeFactory)
    {
        _httpContextAccessor = httpContextAccessor;
        _shapeFactory = shapeFactory;
    }
    
    public async Task<IShape> GetLayoutAsync()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var layout = await _shapeFactory.CreateAsync("Layout");
        
        // Determine layout based on route
        var routeData = httpContext.GetRouteData();
        var controller = routeData.Values["controller"]?.ToString();
        var action = routeData.Values["action"]?.ToString();
        
        // Add layout alternates
        if (controller == "Home")
        {
            layout.Metadata.Alternates.Add("Layout__Home");
        }
        
        if (controller == "Product")
        {
            layout.Metadata.Alternates.Add("Layout__Product");
            if (action == "Details")
            {
                layout.Metadata.Alternates.Add("Layout__ProductDetails");
            }
        }
        
        // Add responsive classes
        layout.Classes.Add("layout");
        layout.Classes.Add("layout--responsive");
        
        return layout;
    }
}
```

---

## 🧩 **WIDGET SYSTEM PATTERNS**

### **1. Custom Widget Implementation**
```csharp
// Widget Model
public class ProductSliderWidget : ContentPart
{
    public string Title { get; set; }
    public int MaxProducts { get; set; } = 6;
    public string CategoryFilter { get; set; }
    public bool ShowOnSaleOnly { get; set; }
    public string SliderSpeed { get; set; } = "3000";
    public bool AutoPlay { get; set; } = true;
}

// Widget Display Driver
public class ProductSliderWidgetDisplayDriver : ContentPartDisplayDriver<ProductSliderWidget>
{
    private readonly IProductService _productService;
    private readonly IContentManager _contentManager;
    
    public ProductSliderWidgetDisplayDriver(
        IProductService productService,
        IContentManager contentManager)
    {
        _productService = productService;
        _contentManager = contentManager;
    }
    
    public override async Task<IDisplayResult> DisplayAsync(
        ProductSliderWidget part, 
        BuildPartDisplayContext context)
    {
        return Initialize<ProductSliderWidgetViewModel>(
            GetDisplayShapeType(context), 
            async model =>
            {
                model.Widget = part;
                
                // Get products based on widget settings
                var products = await _productService.GetProductsAsync(new ProductQuery
                {
                    MaxResults = part.MaxProducts,
                    Category = part.CategoryFilter,
                    OnSaleOnly = part.ShowOnSaleOnly,
                    OrderBy = "Featured"
                });
                
                model.Products = products;
                model.SliderSettings = new SliderSettings
                {
                    Speed = int.Parse(part.SliderSpeed),
                    AutoPlay = part.AutoPlay,
                    ShowDots = true,
                    ShowArrows = true
                };
            })
            .Location("Detail", "Content:1");
    }
    
    public override IDisplayResult Edit(ProductSliderWidget part, BuildPartEditorContext context)
    {
        return Initialize<ProductSliderWidgetEditViewModel>(
            GetEditorShapeType(context), 
            model =>
            {
                model.Title = part.Title;
                model.MaxProducts = part.MaxProducts;
                model.CategoryFilter = part.CategoryFilter;
                model.ShowOnSaleOnly = part.ShowOnSaleOnly;
                model.SliderSpeed = part.SliderSpeed;
                model.AutoPlay = part.AutoPlay;
                
                // Load available categories
                model.AvailableCategories = _productService.GetCategoriesAsync().Result;
            });
    }
    
    public override async Task<IDisplayResult> UpdateAsync(
        ProductSliderWidget part, 
        UpdatePartEditorContext context)
    {
        var model = new ProductSliderWidgetEditViewModel();
        
        if (await context.Updater.TryUpdateModelAsync(model, Prefix))
        {
            part.Title = model.Title;
            part.MaxProducts = Math.Max(1, Math.Min(20, model.MaxProducts)); // Limit 1-20
            part.CategoryFilter = model.CategoryFilter;
            part.ShowOnSaleOnly = model.ShowOnSaleOnly;
            part.SliderSpeed = model.SliderSpeed;
            part.AutoPlay = model.AutoPlay;
        }
        
        return Edit(part, context);
    }
}
```

### **2. Widget Zone Management**
```csharp
public class CustomWidgetZoneProvider : IShapeTableProvider
{
    public void Discover(ShapeTableBuilder builder)
    {
        // Define custom zones for widgets
        builder.Describe("Zone")
            .OnDisplaying(displaying =>
            {
                var zoneName = displaying.Shape.ZoneName?.ToString();
                
                if (!string.IsNullOrEmpty(zoneName))
                {
                    displaying.Shape.Classes.Add($"zone-{zoneName.ToLower()}");
                    displaying.Shape.Metadata.Alternates.Add($"Zone__{zoneName}");
                    
                    // Add responsive classes based on zone
                    switch (zoneName.ToLower())
                    {
                        case "sidebar":
                            displaying.Shape.Classes.Add("col-md-3");
                            break;
                        case "content":
                            displaying.Shape.Classes.Add("col-md-9");
                            break;
                        case "footer":
                            displaying.Shape.Classes.Add("footer-zone");
                            break;
                    }
                }
            });
    }
}

// Widget Placement Service
public class WidgetPlacementService
{
    private readonly IContentManager _contentManager;
    private readonly IContentItemDisplayManager _displayManager;
    
    public WidgetPlacementService(
        IContentManager contentManager,
        IContentItemDisplayManager displayManager)
    {
        _contentManager = contentManager;
        _displayManager = displayManager;
    }
    
    public async Task<IEnumerable<IShape>> GetWidgetsForZoneAsync(
        string zoneName, 
        string contentType = null)
    {
        var widgets = await _contentManager
            .Query<WidgetMetadata>()
            .Where(w => w.Zone == zoneName)
            .OrderBy(w => w.Position)
            .ListAsync();
            
        var shapes = new List<IShape>();
        
        foreach (var widget in widgets)
        {
            // Check if widget should display for current content type
            if (!string.IsNullOrEmpty(contentType) && 
                !IsWidgetValidForContentType(widget, contentType))
            {
                continue;
            }
            
            var shape = await _displayManager.BuildDisplayAsync(widget, null, "Detail");
            shapes.Add(shape);
        }
        
        return shapes;
    }
    
    private bool IsWidgetValidForContentType(ContentItem widget, string contentType)
    {
        var metadata = widget.As<WidgetMetadata>();
        
        // If no content type restrictions, show everywhere
        if (metadata.ContentTypes == null || !metadata.ContentTypes.Any())
            return true;
            
        return metadata.ContentTypes.Contains(contentType);
    }
}
```

---

## 📋 **TEMPLATE OVERRIDE PATTERNS**

### **1. Shape Template Alternates**
```csharp
public class ProductTemplateProvider : IShapeTableProvider
{
    public void Discover(ShapeTableBuilder builder)
    {
        builder.Describe("ProductPart")
            .OnDisplaying(displaying =>
            {
                var contentItem = displaying.Shape.ContentItem as ContentItem;
                var productPart = contentItem?.As<ProductPart>();
                
                if (productPart != null)
                {
                    // Add alternates based on product properties
                    if (productPart.IsFeatured)
                    {
                        displaying.Shape.Metadata.Alternates.Add("ProductPart__Featured");
                    }
                    
                    if (productPart.IsOnSale)
                    {
                        displaying.Shape.Metadata.Alternates.Add("ProductPart__OnSale");
                    }
                    
                    // Add category-based alternates
                    foreach (var category in productPart.Categories ?? [])
                    {
                        var categoryClass = category.Replace(" ", "-").ToLower();
                        displaying.Shape.Metadata.Alternates.Add($"ProductPart__Category__{categoryClass}");
                    }
                    
                    // Add price range alternates
                    var priceRange = GetPriceRange(productPart.Price);
                    displaying.Shape.Metadata.Alternates.Add($"ProductPart__PriceRange__{priceRange}");
                }
            });
    }
    
    private string GetPriceRange(decimal price)
    {
        return price switch
        {
            < 100 => "Budget",
            >= 100 and < 500 => "Mid",
            >= 500 and < 1000 => "Premium",
            _ => "Luxury"
        };
    }
}
```

### **2. Conditional Template Rendering**
```csharp
public class ConditionalRenderingProvider : IShapeTableProvider
{
    public void Discover(ShapeTableBuilder builder)
    {
        builder.Describe("ProductCard")
            .OnDisplaying(displaying =>
            {
                var httpContext = displaying.ServiceProvider
                    .GetService<IHttpContextAccessor>()?.HttpContext;
                    
                if (httpContext != null)
                {
                    var userAgent = httpContext.Request.Headers["User-Agent"].ToString();
                    var isMobile = IsMobileDevice(userAgent);
                    
                    if (isMobile)
                    {
                        displaying.Shape.Metadata.Alternates.Add("ProductCard__Mobile");
                    }
                    else
                    {
                        displaying.Shape.Metadata.Alternates.Add("ProductCard__Desktop");
                    }
                    
                    // Add time-based alternates
                    var hour = DateTime.Now.Hour;
                    var timeOfDay = hour switch
                    {
                        >= 6 and < 12 => "Morning",
                        >= 12 and < 18 => "Afternoon", 
                        >= 18 and < 22 => "Evening",
                        _ => "Night"
                    };
                    
                    displaying.Shape.Metadata.Alternates.Add($"ProductCard__Time__{timeOfDay}");
                }
            });
    }
    
    private bool IsMobileDevice(string userAgent)
    {
        var mobileKeywords = new[] { "Mobile", "Android", "iPhone", "iPad", "Windows Phone" };
        return mobileKeywords.Any(keyword => userAgent.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }
}
```

---

## 🔧 **ADVANCED SHAPE MANIPULATION**

### **1. Dynamic Shape Building**
```csharp
public class DynamicShapeBuilder
{
    private readonly IShapeFactory _shapeFactory;
    private readonly IServiceProvider _serviceProvider;
    
    public DynamicShapeBuilder(
        IShapeFactory shapeFactory,
        IServiceProvider serviceProvider)
    {
        _shapeFactory = shapeFactory;
        _serviceProvider = serviceProvider;
    }
    
    public async Task<IShape> BuildProductGridAsync(
        IEnumerable<ProductViewModel> products,
        GridOptions options)
    {
        var grid = await _shapeFactory.CreateAsync("ProductGrid");
        
        // Configure grid properties
        grid.Properties["Columns"] = options.Columns;
        grid.Properties["ShowPagination"] = options.ShowPagination;
        grid.Properties["ItemsPerPage"] = options.ItemsPerPage;
        
        // Add CSS classes based on options
        grid.Classes.Add("product-grid");
        grid.Classes.Add($"grid-cols-{options.Columns}");
        
        if (options.ResponsiveBreakpoints)
        {
            grid.Classes.Add("grid-responsive");
        }
        
        // Build product cards
        foreach (var product in products)
        {
            var card = await BuildProductCardAsync(product, options.CardStyle);
            await grid.AddAsync(card);
        }
        
        // Add pagination if needed
        if (options.ShowPagination && options.TotalItems > options.ItemsPerPage)
        {
            var pagination = await BuildPaginationAsync(options);
            await grid.AddAsync(pagination, "after");
        }
        
        return grid;
    }
    
    private async Task<IShape> BuildProductCardAsync(ProductViewModel product, CardStyle style)
    {
        var card = await _shapeFactory.CreateAsync("ProductCard");
        
        // Set product data
        card.Properties["Product"] = product;
        card.Properties["Style"] = style.ToString();
        
        // Add style-specific classes
        card.Classes.Add("product-card");
        card.Classes.Add($"card-style-{style.ToString().ToLower()}");
        
        // Add conditional elements based on style
        switch (style)
        {
            case CardStyle.Minimal:
                card.Properties["ShowDescription"] = false;
                card.Properties["ShowRating"] = false;
                break;
                
            case CardStyle.Detailed:
                card.Properties["ShowDescription"] = true;
                card.Properties["ShowRating"] = true;
                card.Properties["ShowSpecs"] = true;
                break;
                
            case CardStyle.Compact:
                card.Properties["ShowImage"] = true;
                card.Properties["ImageSize"] = "small";
                break;
        }
        
        return card;
    }
    
    private async Task<IShape> BuildPaginationAsync(GridOptions options)
    {
        var pagination = await _shapeFactory.CreateAsync("Pagination");
        
        var totalPages = (int)Math.Ceiling((double)options.TotalItems / options.ItemsPerPage);
        
        pagination.Properties["CurrentPage"] = options.CurrentPage;
        pagination.Properties["TotalPages"] = totalPages;
        pagination.Properties["ItemsPerPage"] = options.ItemsPerPage;
        pagination.Properties["TotalItems"] = options.TotalItems;
        
        return pagination;
    }
}

public class GridOptions
{
    public int Columns { get; set; } = 3;
    public bool ShowPagination { get; set; } = true;
    public int ItemsPerPage { get; set; } = 12;
    public int CurrentPage { get; set; } = 1;
    public int TotalItems { get; set; }
    public bool ResponsiveBreakpoints { get; set; } = true;
    public CardStyle CardStyle { get; set; } = CardStyle.Standard;
}

public enum CardStyle
{
    Minimal,
    Standard,
    Detailed,
    Compact
}
```

### **2. Shape Composition Patterns**
```csharp
public class CompositeShapeBuilder
{
    private readonly IShapeFactory _shapeFactory;
    
    public CompositeShapeBuilder(IShapeFactory shapeFactory)
    {
        _shapeFactory = shapeFactory;
    }
    
    public async Task<IShape> BuildDashboardAsync(DashboardConfig config)
    {
        var dashboard = await _shapeFactory.CreateAsync("Dashboard");
        
        // Build header section
        var header = await BuildHeaderSectionAsync(config.HeaderConfig);
        await dashboard.AddAsync(header, "header");
        
        // Build main content area with widgets
        var mainContent = await _shapeFactory.CreateAsync("DashboardContent");
        
        foreach (var widgetConfig in config.Widgets)
        {
            var widget = await BuildWidgetAsync(widgetConfig);
            await mainContent.AddAsync(widget, widgetConfig.Position);
        }
        
        await dashboard.AddAsync(mainContent, "content");
        
        // Build sidebar if configured
        if (config.ShowSidebar)
        {
            var sidebar = await BuildSidebarAsync(config.SidebarConfig);
            await dashboard.AddAsync(sidebar, "sidebar");
        }
        
        // Build footer
        var footer = await BuildFooterSectionAsync(config.FooterConfig);
        await dashboard.AddAsync(footer, "footer");
        
        return dashboard;
    }
    
    private async Task<IShape> BuildHeaderSectionAsync(HeaderConfig config)
    {
        var header = await _shapeFactory.CreateAsync("DashboardHeader");
        
        // Add navigation
        if (config.ShowNavigation)
        {
            var nav = await _shapeFactory.CreateAsync("Navigation");
            nav.Properties["Items"] = config.NavigationItems;
            await header.AddAsync(nav, "navigation");
        }
        
        // Add user info
        if (config.ShowUserInfo)
        {
            var userInfo = await _shapeFactory.CreateAsync("UserInfo");
            await header.AddAsync(userInfo, "user-info");
        }
        
        // Add breadcrumbs
        if (config.ShowBreadcrumbs)
        {
            var breadcrumbs = await _shapeFactory.CreateAsync("Breadcrumbs");
            await header.AddAsync(breadcrumbs, "breadcrumbs");
        }
        
        return header;
    }
    
    private async Task<IShape> BuildWidgetAsync(WidgetConfig config)
    {
        var widget = await _shapeFactory.CreateAsync(config.WidgetType);
        
        // Set common widget properties
        widget.Properties["Title"] = config.Title;
        widget.Properties["Config"] = config.Settings;
        widget.Classes.Add("dashboard-widget");
        widget.Classes.Add($"widget-{config.WidgetType.ToLower()}");
        
        // Add size classes
        widget.Classes.Add($"widget-size-{config.Size}");
        
        // Add conditional features
        if (config.IsCollapsible)
        {
            widget.Classes.Add("widget-collapsible");
            widget.Properties["IsCollapsed"] = config.IsCollapsed;
        }
        
        if (config.IsRefreshable)
        {
            widget.Classes.Add("widget-refreshable");
            widget.Properties["RefreshInterval"] = config.RefreshInterval;
        }
        
        return widget;
    }
}
```

---

## 📊 **PERFORMANCE OPTIMIZATION**

### **1. Shape Caching Strategies**
```csharp
public class CachedShapeProvider : IShapeTableProvider
{
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<CachedShapeProvider> _logger;
    
    public CachedShapeProvider(
        IMemoryCache memoryCache,
        ILogger<CachedShapeProvider> logger)
    {
        _memoryCache = memoryCache;
        _logger = logger;
    }
    
    public void Discover(ShapeTableBuilder builder)
    {
        builder.Describe("ProductList")
            .OnDisplaying(displaying =>
            {
                var cacheKey = GenerateCacheKey(displaying.Shape);
                
                if (!_memoryCache.TryGetValue(cacheKey, out var cachedShape))
                {
                    // Build shape if not cached
                    BuildProductListShape(displaying.Shape);
                    
                    // Cache for 5 minutes
                    var cacheOptions = new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
                        SlidingExpiration = TimeSpan.FromMinutes(2),
                        Priority = CacheItemPriority.Normal
                    };
                    
                    _memoryCache.Set(cacheKey, displaying.Shape, cacheOptions);
                    _logger.LogDebug("Cached shape with key: {CacheKey}", cacheKey);
                }
                else
                {
                    _logger.LogDebug("Retrieved cached shape with key: {CacheKey}", cacheKey);
                }
            });
    }
    
    private string GenerateCacheKey(dynamic shape)
    {
        var keyParts = new List<string>
        {
            shape.GetType().Name,
            shape.Properties?.ContainsKey("Category") == true ? shape.Properties["Category"].ToString() : "all",
            shape.Properties?.ContainsKey("PageSize") == true ? shape.Properties["PageSize"].ToString() : "10",
            shape.Properties?.ContainsKey("SortBy") == true ? shape.Properties["SortBy"].ToString() : "default"
        };
        
        return string.Join(":", keyParts);
    }
    
    private void BuildProductListShape(dynamic shape)
    {
        // Expensive shape building logic here
        shape.Classes.Add("product-list");
        shape.Properties["BuildTime"] = DateTime.UtcNow;
    }
}
```

### **2. Lazy Loading Shapes**
```csharp
public class LazyShapeProvider : IShapeTableProvider
{
    public void Discover(ShapeTableBuilder builder)
    {
        builder.Describe("LazyProductGrid")
            .OnCreating(creating => creating.Create = () => new LazyShape())
            .OnDisplaying(displaying =>
            {
                var lazyShape = displaying.Shape as LazyShape;
                lazyShape?.SetLazyLoader(async () =>
                {
                    var productService = displaying.ServiceProvider.GetService<IProductService>();
                    var products = await productService.GetProductsAsync();
                    
                    var grid = displaying.ServiceProvider.GetService<IShapeFactory>()
                        .CreateAsync("ProductGrid").Result;
                        
                    foreach (var product in products)
                    {
                        var card = displaying.ServiceProvider.GetService<IShapeFactory>()
                            .CreateAsync("ProductCard").Result;
                        card.Properties["Product"] = product;
                        await grid.AddAsync(card);
                    }
                    
                    return grid;
                });
            });
    }
}

public class LazyShape : Shape
{
    private Func<Task<IShape>> _lazyLoader;
    private IShape _loadedShape;
    private bool _isLoaded;
    
    public void SetLazyLoader(Func<Task<IShape>> loader)
    {
        _lazyLoader = loader;
    }
    
    public async Task<IShape> GetLoadedShapeAsync()
    {
        if (!_isLoaded && _lazyLoader != null)
        {
            _loadedShape = await _lazyLoader();
            _isLoaded = true;
        }
        
        return _loadedShape ?? this;
    }
}
```

---

## 🎯 **BEST PRACTICES**

### **1. Shape Organization**
- **Tách biệt concerns**: Display logic, business logic, data access
- **Sử dụng ViewModels**: Không pass domain objects trực tiếp vào views
- **Consistent naming**: Follow OrchardCore naming conventions
- **Performance**: Cache expensive shape operations

### **2. Template Management**
- **Logical alternates**: Tạo alternates dựa trên business logic
- **Responsive design**: Support multiple device types
- **Accessibility**: Follow WCAG guidelines
- **SEO optimization**: Proper meta tags và structured data

### **3. Widget Development**
- **Configurable**: Widgets nên có settings để customize
- **Reusable**: Design widgets để reuse across different contexts
- **Performance**: Lazy load widget content khi cần
- **User-friendly**: Intuitive admin interface

### **4. Testing Strategies**
- **Unit tests**: Test display drivers và shape providers
- **Integration tests**: Test shape rendering pipeline
- **Visual regression**: Test UI changes
- **Performance tests**: Monitor shape rendering performance

---

## 📚 **MIGRATION & DEPLOYMENT**

### **1. Shape Migration Pattern**
```csharp
public class ShapeMigration : DataMigration
{
    public int Create()
    {
        // Create shape-related content types
        ContentDefinitionManager.AlterTypeDefinition("ProductWidget", type => type
            .WithPart("ProductSliderWidget")
            .Stereotype("Widget"));
            
        return 1;
    }
    
    public int UpdateFrom1()
    {
        // Update shape templates
        ContentDefinitionManager.AlterPartDefinition("ProductSliderWidget", part => part
            .WithField("MaxProducts", field => field
                .OfType("NumericField")
                .WithDisplayName("Maximum Products")
                .WithSettings(new NumericFieldSettings
                {
                    Minimum = 1,
                    Maximum = 20,
                    DefaultValue = "6"
                })));
                
        return 2;
    }
}
```

### **2. Theme Deployment**
```csharp
public class ThemeDeploymentService
{
    private readonly IShapeTableManager _shapeTableManager;
    private readonly IThemeManager _themeManager;
    
    public async Task DeployThemeAsync(string themeName)
    {
        // Validate theme structure
        await ValidateThemeStructureAsync(themeName);
        
        // Register theme shapes
        await RegisterThemeShapesAsync(themeName);
        
        // Update shape table
        await _shapeTableManager.RebuildAsync();
        
        // Set as current theme if specified
        await _themeManager.SetCurrentThemeAsync(themeName);
    }
    
    private async Task ValidateThemeStructureAsync(string themeName)
    {
        // Validate required files and structure
        var requiredFiles = new[] { "Theme.txt", "Views/Layout.cshtml" };
        
        foreach (var file in requiredFiles)
        {
            var filePath = Path.Combine("Themes", themeName, file);
            if (!File.Exists(filePath))
            {
                throw new InvalidOperationException($"Required file missing: {file}");
            }
        }
    }
    
    private async Task RegisterThemeShapesAsync(string themeName)
    {
        // Register theme-specific shapes
        // Implementation depends on theme structure
        await Task.CompletedTask;
    }
}
```

---

## 🔄 **TODO & FUTURE ENHANCEMENTS**

### **Planned Features:**
- [ ] Advanced shape composition patterns
- [ ] Real-time shape updates
- [ ] Shape versioning system
- [ ] Advanced caching strategies
- [ ] Performance monitoring tools
- [ ] Visual shape editor
- [ ] Shape testing framework
- [ ] Theme marketplace integration

---

## 🏢 **ỨNG DỤNG THỰC TẾ TRONG CÁC DỰ ÁN**

### **1. 🛒 E-commerce Website - Product Display System**

#### **Tình huống:**
Xây dựng **website bán hàng** cần hiển thị sản phẩm theo nhiều cách khác nhau: grid, list, card, slider.

#### **Ứng dụng Advanced Display Management:**

```csharp
// 1. Tạo ProductPart Display Driver
public class ProductPartDisplayDriver : ContentPartDisplayDriver<ProductPart>
{
    public override async Task<IDisplayResult> DisplayAsync(ProductPart part, BuildPartDisplayContext context)
    {
        return Initialize<ProductPartViewModel>("ProductPart", async model =>
        {
            model.ProductPart = part;
            model.Price = part.Price;
            model.SalePrice = part.SalePrice;
            model.IsOnSale = part.IsOnSale;
            model.Images = part.Images;
            model.Rating = await GetAverageRatingAsync(part.ContentItem.ContentItemId);
        })
        // Hiển thị khác nhau theo context
        .Location("Detail", "Content:5")      // Trang chi tiết sản phẩm
        .Location("Summary", "Content:1")     // Trang danh sách sản phẩm  
        .Location("Card", "Content:0");       // Widget card
    }
}

// 2. Shape Provider cho các layout khác nhau
public class ProductShapeProvider : IShapeTableProvider
{
    public void Discover(ShapeTableBuilder builder)
    {
        builder.Describe("ProductPart")
            .OnDisplaying(displaying =>
            {
                var productPart = displaying.Shape.ContentItem.As<ProductPart>();
                
                // Thêm CSS classes động
                if (productPart.IsOnSale)
                {
                    displaying.Shape.Classes.Add("product-on-sale");
                    displaying.Shape.Metadata.Alternates.Add("ProductPart__OnSale");
                }
                
                if (productPart.IsFeatured)
                {
                    displaying.Shape.Classes.Add("product-featured");
                    displaying.Shape.Metadata.Alternates.Add("ProductPart__Featured");
                }
                
                // Alternates theo giá
                var priceRange = GetPriceRange(productPart.Price);
                displaying.Shape.Metadata.Alternates.Add($"ProductPart__Price__{priceRange}");
            });
    }
}
```

#### **Placement.json - Điều khiển vị trí hiển thị:**
```json
{
  "ProductPart": [
    {
      "place": "Content:5",
      "displayType": "Detail"
    },
    {
      "place": "Content:1", 
      "displayType": "Summary"
    },
    {
      "place": "Content:0",
      "displayType": "Card",
      "contentType": ["Product"]
    }
  ],
  "ProductPart__OnSale": [
    {
      "place": "Content:0",
      "displayType": "Summary",
      "alternates": ["ProductPart__OnSale__Badge"]
    }
  ]
}
```

#### **Templates tương ứng:**
```html
<!-- Views/ProductPart.cshtml - Layout cơ bản -->
@model ProductPartViewModel
<div class="product-item">
    <h3>@Model.ProductPart.Title</h3>
    <div class="price">
        @if (Model.IsOnSale)
        {
            <span class="sale-price">@Model.SalePrice.ToString("C")</span>
            <span class="original-price">@Model.Price.ToString("C")</span>
        }
        else
        {
            <span class="price">@Model.Price.ToString("C")</span>
        }
    </div>
</div>

<!-- Views/ProductPart__OnSale.cshtml - Template cho sản phẩm sale -->
@model ProductPartViewModel
<div class="product-item product-on-sale">
    <div class="sale-badge">SALE!</div>
    <h3>@Model.ProductPart.Title</h3>
    <div class="price-section">
        <span class="sale-price">@Model.SalePrice.ToString("C")</span>
        <span class="original-price strikethrough">@Model.Price.ToString("C")</span>
        <span class="discount">-@(((Model.Price - Model.SalePrice) / Model.Price * 100):F0)%</span>
    </div>
</div>

<!-- Views/ProductPart.Summary.cshtml - Template cho danh sách -->
@model ProductPartViewModel
<div class="product-card">
    <img src="@Model.Images.FirstOrDefault()" alt="@Model.ProductPart.Title" />
    <div class="product-info">
        <h4>@Model.ProductPart.Title</h4>
        <div class="rating">⭐ @Model.Rating</div>
        <div class="price">@Model.Price.ToString("C")</div>
        <button class="btn-add-cart">Add to Cart</button>
    </div>
</div>
```

#### **Kết quả:**
- ✅ **Trang chi tiết**: Hiển thị đầy đủ thông tin sản phẩm
- ✅ **Trang danh sách**: Hiển thị dạng card compact
- ✅ **Sản phẩm sale**: Tự động thêm badge và styling đặc biệt
- ✅ **Responsive**: Tự động adapt theo device

### **2. 📰 News Portal - Dynamic Content Layout**

#### **Tình huống:**
Xây dựng **website tin tức** cần hiển thị bài viết theo nhiều layout: featured, grid, list, timeline.

#### **Ứng dụng Advanced Display Management:**

```csharp
// News Article Display Driver
public class NewsArticleDisplayDriver : ContentPartDisplayDriver<NewsArticlePart>
{
    public override async Task<IDisplayResult> DisplayAsync(NewsArticlePart part, BuildPartDisplayContext context)
    {
        return Initialize<NewsArticleViewModel>("NewsArticlePart", async model =>
        {
            model.Article = part;
            model.Author = await GetAuthorAsync(part.AuthorId);
            model.Category = await GetCategoryAsync(part.CategoryId);
            model.ReadTime = CalculateReadTime(part.Content);
            model.RelatedArticles = await GetRelatedArticlesAsync(part.CategoryId, part.ContentItem.ContentItemId);
        })
        .Location("Detail", "Content:1")
        .Location("Summary", "Content:0")
        .Location("Featured", "Content:0")
        .Location("Timeline", "Content:0");
    }
}

// Dynamic Layout Provider
public class NewsLayoutProvider : IShapeTableProvider
{
    public void Discover(ShapeTableBuilder builder)
    {
        builder.Describe("NewsArticlePart")
            .OnDisplaying(displaying =>
            {
                var article = displaying.Shape.ContentItem.As<NewsArticlePart>();
                var httpContext = displaying.ServiceProvider.GetService<IHttpContextAccessor>()?.HttpContext;
                
                // Layout theo thời gian
                var publishedHours = (DateTime.UtcNow - article.PublishedUtc).TotalHours;
                if (publishedHours < 24)
                {
                    displaying.Shape.Metadata.Alternates.Add("NewsArticlePart__Breaking");
                }
                else if (publishedHours < 168) // 1 week
                {
                    displaying.Shape.Metadata.Alternates.Add("NewsArticlePart__Recent");
                }
                
                // Layout theo category
                var category = article.Category?.ToLower().Replace(" ", "-");
                displaying.Shape.Metadata.Alternates.Add($"NewsArticlePart__Category__{category}");
                
                // Layout theo device
                var userAgent = httpContext?.Request.Headers["User-Agent"].ToString() ?? "";
                if (IsMobile(userAgent))
                {
                    displaying.Shape.Metadata.Alternates.Add("NewsArticlePart__Mobile");
                }
                
                // Layout theo priority
                if (article.IsFeatured)
                {
                    displaying.Shape.Metadata.Alternates.Add("NewsArticlePart__Featured");
                }
            });
    }
}
```

#### **Widget System cho Homepage:**
```csharp
// Breaking News Widget
public class BreakingNewsWidget : ContentPart
{
    public int MaxArticles { get; set; } = 5;
    public string CategoryFilter { get; set; }
    public bool AutoRefresh { get; set; } = true;
    public int RefreshInterval { get; set; } = 300; // 5 minutes
}

public class BreakingNewsWidgetDriver : ContentPartDisplayDriver<BreakingNewsWidget>
{
    public override async Task<IDisplayResult> DisplayAsync(BreakingNewsWidget part, BuildPartDisplayContext context)
    {
        return Initialize<BreakingNewsViewModel>("BreakingNewsWidget", async model =>
        {
            model.Widget = part;
            
            // Lấy tin breaking news (24h gần nhất)
            var breakingNews = await _newsService.GetBreakingNewsAsync(new NewsQuery
            {
                MaxResults = part.MaxArticles,
                Category = part.CategoryFilter,
                PublishedWithin = TimeSpan.FromHours(24),
                OrderBy = "PublishedUtc DESC"
            });
            
            model.Articles = breakingNews;
            model.LastUpdated = DateTime.UtcNow;
        })
        .Location("Detail", "Content:0");
    }
}
```

#### **Templates cho các layout:**
```html
<!-- Views/NewsArticlePart__Breaking.cshtml -->
@model NewsArticleViewModel
<article class="news-article breaking-news">
    <div class="breaking-badge">🔴 BREAKING</div>
    <h2 class="headline">@Model.Article.Title</h2>
    <div class="meta">
        <span class="time">@Model.Article.PublishedUtc.ToString("HH:mm")</span>
        <span class="author">@Model.Author.Name</span>
    </div>
    <div class="summary">@Model.Article.Summary</div>
    <a href="@Url.Action("Details", new { id = Model.Article.ContentItemId })" class="read-more">Read More</a>
</article>

<!-- Views/NewsArticlePart__Category__sports.cshtml -->
@model NewsArticleViewModel
<article class="news-article sports-article">
    <div class="sports-icon">⚽</div>
    <div class="content">
        <div class="category-tag">SPORTS</div>
        <h3>@Model.Article.Title</h3>
        <div class="match-info">
            @if (Model.Article.MatchDate.HasValue)
            {
                <span class="match-date">📅 @Model.Article.MatchDate.Value.ToString("MMM dd")</span>
            }
        </div>
    </div>
</article>

<!-- Views/BreakingNewsWidget.cshtml -->
@model BreakingNewsViewModel
<div class="breaking-news-widget" data-auto-refresh="@Model.Widget.AutoRefresh" data-interval="@Model.Widget.RefreshInterval">
    <h3 class="widget-title">🔴 Breaking News</h3>
    <div class="news-ticker">
        @foreach (var article in Model.Articles)
        {
            <div class="ticker-item">
                <span class="time">@article.PublishedUtc.ToString("HH:mm")</span>
                <a href="@Url.Action("Details", new { id = article.ContentItemId })">@article.Title</a>
            </div>
        }
    </div>
    <div class="last-updated">Last updated: @Model.LastUpdated.ToString("HH:mm:ss")</div>
</div>
```

### **3. 🏢 Corporate Dashboard - Multi-Widget System**

#### **Tình huống:**
Xây dựng **dashboard doanh nghiệp** với nhiều widgets: charts, KPIs, notifications, reports.

#### **Ứng dụng Advanced Display Management:**

```csharp
// Dashboard Layout Builder
public class DashboardLayoutBuilder
{
    private readonly IShapeFactory _shapeFactory;
    private readonly IWidgetService _widgetService;
    
    public async Task<IShape> BuildDashboardAsync(string userId, DashboardConfig config)
    {
        var dashboard = await _shapeFactory.CreateAsync("Dashboard");
        
        // Build header với user info
        var header = await BuildDashboardHeaderAsync(userId);
        await dashboard.AddAsync(header, "header");
        
        // Build main content với widgets
        var mainContent = await _shapeFactory.CreateAsync("DashboardContent");
        mainContent.Classes.Add("dashboard-grid");
        mainContent.Classes.Add($"grid-cols-{config.Columns}");
        
        foreach (var widgetConfig in config.Widgets.OrderBy(w => w.Order))
        {
            var widget = await BuildWidgetAsync(widgetConfig, userId);
            await mainContent.AddAsync(widget, $"position-{widgetConfig.Order}");
        }
        
        await dashboard.AddAsync(mainContent, "content");
        
        return dashboard;
    }
    
    private async Task<IShape> BuildWidgetAsync(WidgetConfig config, string userId)
    {
        var widget = await _shapeFactory.CreateAsync(config.WidgetType);
        
        // Common widget properties
        widget.Properties["Title"] = config.Title;
        widget.Properties["UserId"] = userId;
        widget.Properties["Config"] = config.Settings;
        
        // Widget styling
        widget.Classes.Add("dashboard-widget");
        widget.Classes.Add($"widget-{config.WidgetType.ToLower()}");
        widget.Classes.Add($"widget-size-{config.Size}"); // small, medium, large
        
        // Widget features
        if (config.IsCollapsible)
        {
            widget.Classes.Add("widget-collapsible");
            widget.Attributes["data-collapsible"] = "true";
        }
        
        if (config.AutoRefresh)
        {
            widget.Classes.Add("widget-auto-refresh");
            widget.Attributes["data-refresh-interval"] = config.RefreshInterval.ToString();
        }
        
        return widget;
    }
}

// KPI Widget Example
public class KPIWidget : ContentPart
{
    public string MetricName { get; set; }
    public string DataSource { get; set; }
    public string ChartType { get; set; } = "line";
    public int TimeRange { get; set; } = 30; // days
    public bool ShowTrend { get; set; } = true;
    public string TargetValue { get; set; }
}

public class KPIWidgetDriver : ContentPartDisplayDriver<KPIWidget>
{
    private readonly IMetricsService _metricsService;
    
    public override async Task<IDisplayResult> DisplayAsync(KPIWidget part, BuildPartDisplayContext context)
    {
        return Initialize<KPIWidgetViewModel>("KPIWidget", async model =>
        {
            model.Widget = part;
            
            // Lấy dữ liệu metrics
            var metrics = await _metricsService.GetMetricsAsync(new MetricsQuery
            {
                MetricName = part.MetricName,
                DataSource = part.DataSource,
                TimeRange = TimeSpan.FromDays(part.TimeRange),
                IncludeTrend = part.ShowTrend
            });
            
            model.CurrentValue = metrics.CurrentValue;
            model.PreviousValue = metrics.PreviousValue;
            model.TrendPercentage = metrics.TrendPercentage;
            model.ChartData = metrics.ChartData;
            model.IsTargetMet = !string.IsNullOrEmpty(part.TargetValue) && 
                               metrics.CurrentValue >= decimal.Parse(part.TargetValue);
        });
    }
}
```

#### **Widget Templates:**
```html
<!-- Views/KPIWidget.cshtml -->
@model KPIWidgetViewModel
<div class="kpi-widget @(Model.IsTargetMet ? "target-met" : "target-missed")">
    <div class="widget-header">
        <h4 class="widget-title">@Model.Widget.MetricName</h4>
        <div class="widget-actions">
            <button class="btn-refresh" data-widget-refresh>🔄</button>
            <button class="btn-settings" data-widget-settings>⚙️</button>
        </div>
    </div>
    
    <div class="widget-content">
        <div class="kpi-value">
            <span class="current-value">@Model.CurrentValue.ToString("N0")</span>
            @if (Model.Widget.ShowTrend)
            {
                <span class="trend @(Model.TrendPercentage >= 0 ? "trend-up" : "trend-down")">
                    @(Model.TrendPercentage >= 0 ? "↗️" : "↘️") @Math.Abs(Model.TrendPercentage).ToString("F1")%
                </span>
            }
        </div>
        
        @if (!string.IsNullOrEmpty(Model.Widget.TargetValue))
        {
            <div class="target-info">
                Target: @Model.Widget.TargetValue
                @if (Model.IsTargetMet)
                {
                    <span class="target-status achieved">✅ Achieved</span>
                }
                else
                {
                    <span class="target-status missed">❌ Missed</span>
                }
            </div>
        }
        
        <div class="mini-chart" data-chart-type="@Model.Widget.ChartType" data-chart-data="@Json.Serialize(Model.ChartData)">
            <!-- Chart sẽ được render bằng JavaScript -->
        </div>
    </div>
</div>

<!-- Views/Dashboard.cshtml -->
@model DashboardViewModel
<div class="dashboard-container">
    <header class="dashboard-header">
        @await DisplayAsync(Model.Header)
    </header>
    
    <main class="dashboard-content">
        @await DisplayAsync(Model.Content)
    </main>
    
    <aside class="dashboard-sidebar" style="@(Model.ShowSidebar ? "" : "display: none;")">
        @if (Model.ShowSidebar)
        {
            @await DisplayAsync(Model.Sidebar)
        }
    </aside>
</div>
```

### **4. 🎓 E-learning Platform - Adaptive Content Display**

#### **Tình huống:**
Xây dựng **nền tảng học online** cần hiển thị nội dung khác nhau theo: progress, device, user role.

#### **Ứng dụng Advanced Display Management:**

```csharp
// Course Content Display Driver
public class CourseContentDisplayDriver : ContentPartDisplayDriver<CourseContentPart>
{
    private readonly IProgressService _progressService;
    private readonly IUserService _userService;
    
    public override async Task<IDisplayResult> DisplayAsync(CourseContentPart part, BuildPartDisplayContext context)
    {
        return Initialize<CourseContentViewModel>("CourseContentPart", async model =>
        {
            var userId = GetCurrentUserId();
            var userProgress = await _progressService.GetUserProgressAsync(userId, part.CourseId);
            var userRole = await _userService.GetUserRoleAsync(userId);
            
            model.Content = part;
            model.UserProgress = userProgress;
            model.UserRole = userRole;
            model.IsCompleted = userProgress.CompletedLessons.Contains(part.LessonId);
            model.IsLocked = !userProgress.UnlockedLessons.Contains(part.LessonId);
            model.NextLesson = await GetNextLessonAsync(part.CourseId, part.LessonId);
        });
    }
}

// Adaptive Layout Provider
public class LearningLayoutProvider : IShapeTableProvider
{
    public void Discover(ShapeTableBuilder builder)
    {
        builder.Describe("CourseContentPart")
            .OnDisplaying(displaying =>
            {
                var content = displaying.Shape.ContentItem.As<CourseContentPart>();
                var httpContext = displaying.ServiceProvider.GetService<IHttpContextAccessor>()?.HttpContext;
                var userId = GetCurrentUserId(httpContext);
                
                // Layout theo progress
                var progressService = displaying.ServiceProvider.GetService<IProgressService>();
                var progress = progressService.GetUserProgressAsync(userId, content.CourseId).Result;
                
                if (progress.CompletionPercentage < 25)
                {
                    displaying.Shape.Metadata.Alternates.Add("CourseContentPart__Beginner");
                }
                else if (progress.CompletionPercentage < 75)
                {
                    displaying.Shape.Metadata.Alternates.Add("CourseContentPart__Intermediate");
                }
                else
                {
                    displaying.Shape.Metadata.Alternates.Add("CourseContentPart__Advanced");
                }
                
                // Layout theo content type
                switch (content.ContentType.ToLower())
                {
                    case "video":
                        displaying.Shape.Metadata.Alternates.Add("CourseContentPart__Video");
                        break;
                    case "quiz":
                        displaying.Shape.Metadata.Alternates.Add("CourseContentPart__Quiz");
                        break;
                    case "assignment":
                        displaying.Shape.Metadata.Alternates.Add("CourseContentPart__Assignment");
                        break;
                }
                
                // Layout theo device
                if (IsMobileDevice(httpContext))
                {
                    displaying.Shape.Metadata.Alternates.Add("CourseContentPart__Mobile");
                }
            });
    }
}
```

#### **Templates theo progress:**
```html
<!-- Views/CourseContentPart__Beginner.cshtml -->
@model CourseContentViewModel
<div class="course-content beginner-mode">
    <div class="beginner-guide">
        <h4>👋 Welcome to your learning journey!</h4>
        <div class="tips">
            <p>💡 <strong>Tip:</strong> Take your time and don't rush through the content.</p>
            <p>📝 <strong>Note:</strong> You can replay videos as many times as you need.</p>
        </div>
    </div>
    
    <div class="content-wrapper">
        <div class="progress-indicator">
            <div class="progress-bar">
                <div class="progress-fill" style="width: @Model.UserProgress.CompletionPercentage%"></div>
            </div>
            <span class="progress-text">@Model.UserProgress.CompletionPercentage% Complete</span>
        </div>
        
        @await DisplayAsync(Model.Content)
        
        <div class="beginner-navigation">
            <button class="btn-previous" @(Model.UserProgress.CurrentLessonIndex == 0 ? "disabled" : "")>
                ← Previous Lesson
            </button>
            <button class="btn-next" onclick="markAsCompleted('@Model.Content.LessonId')">
                Mark Complete & Continue →
            </button>
        </div>
    </div>
</div>

<!-- Views/CourseContentPart__Video.cshtml -->
@model CourseContentViewModel
<div class="video-lesson">
    <div class="video-container">
        <video controls data-lesson-id="@Model.Content.LessonId" data-track-progress="true">
            <source src="@Model.Content.VideoUrl" type="video/mp4">
        </video>
        
        <div class="video-controls">
            <button class="btn-speed" data-speed="0.5x">0.5x</button>
            <button class="btn-speed active" data-speed="1x">1x</button>
            <button class="btn-speed" data-speed="1.25x">1.25x</button>
            <button class="btn-speed" data-speed="1.5x">1.5x</button>
        </div>
    </div>
    
    <div class="video-transcript" style="@(Model.Content.ShowTranscript ? "" : "display: none;")">
        <h5>📝 Transcript</h5>
        <div class="transcript-content">@Model.Content.Transcript</div>
    </div>
    
    <div class="video-notes">
        <h5>📚 Your Notes</h5>
        <textarea class="notes-editor" data-lesson-id="@Model.Content.LessonId" 
                  placeholder="Take notes while watching...">@Model.UserProgress.Notes</textarea>
    </div>
</div>
```

---

## 🎯 **KHI NÀO CẦN SỬ DỤNG ADVANCED DISPLAY MANAGEMENT?**

### **✅ NÊN DÙNG KHI:**

#### **1. 🎨 Custom UI Requirements**
- **Multiple layouts** cho cùng một content type
- **Responsive design** với layout khác nhau theo device
- **Dynamic styling** based on content properties
- **Theme customization** với nhiều variants

#### **2. 🧩 Widget-based Architecture**
- **Dashboard systems** với draggable widgets
- **Homepage builders** với configurable sections
- **Sidebar management** với dynamic content
- **Multi-zone layouts** với flexible placement

#### **3. 📱 Context-aware Display**
- **User role-based** UI differences
- **Time-sensitive** content display
- **Location-based** content adaptation
- **Device-specific** optimizations

#### **4. 🔄 Dynamic Content Rendering**
- **A/B testing** với different templates
- **Personalization** based on user behavior
- **Progressive enhancement** theo user progress
- **Conditional features** based on subscriptions

### **❌ KHÔNG CẦN DÙNG KHI:**

#### **1. 📄 Simple Static Content**
- **Basic blog** với uniform layout
- **Simple company website** không cần customization
- **Documentation sites** với consistent formatting
- **Landing pages** với fixed design

#### **2. 🚀 Performance-critical Applications**
- **High-traffic sites** cần maximum performance
- **Real-time applications** không thể afford overhead
- **Simple APIs** chỉ return JSON
- **Microservices** không cần UI complexity

---

## 💡 **KINH NGHIỆM THỰC TẾ**

### **🔥 Best Practices:**

#### **1. Performance Optimization**
```csharp
// Cache expensive shape operations
public class CachedProductDisplayDriver : ContentPartDisplayDriver<ProductPart>
{
    private readonly IMemoryCache _cache;
    
    public override async Task<IDisplayResult> DisplayAsync(ProductPart part, BuildPartDisplayContext context)
    {
        var cacheKey = $"product_display_{part.ContentItem.ContentItemId}_{context.DisplayType}";
        
        if (!_cache.TryGetValue(cacheKey, out var cachedResult))
        {
            cachedResult = await BuildDisplayResultAsync(part, context);
            _cache.Set(cacheKey, cachedResult, TimeSpan.FromMinutes(5));
        }
        
        return (IDisplayResult)cachedResult;
    }
}
```

#### **2. Maintainable Template Structure**
```
Views/
├── Shared/
│   ├── _Layout.cshtml
│   └── Components/
├── ProductPart.cshtml              # Base template
├── ProductPart.Summary.cshtml      # List view
├── ProductPart.Detail.cshtml       # Detail view
├── ProductPart__OnSale.cshtml      # Sale variant
├── ProductPart__Featured.cshtml    # Featured variant
└── ProductPart__Mobile.cshtml      # Mobile variant
```

#### **3. Testing Strategy**
```csharp
[Test]
public async Task ProductDisplayDriver_Should_Show_Sale_Badge_When_OnSale()
{
    // Arrange
    var product = new ProductPart { IsOnSale = true, Price = 100, SalePrice = 80 };
    var context = CreateBuildDisplayContext();
    
    // Act
    var result = await _driver.DisplayAsync(product, context);
    
    // Assert
    var shape = result.Shape;
    Assert.That(shape.Classes, Contains.Item("product-on-sale"));
    Assert.That(shape.Metadata.Alternates, Contains.Item("ProductPart__OnSale"));
}
```

### **🚀 Tips Triển Khai:**

#### **1. Start Simple, Scale Up**
```csharp
// Phase 1: Basic display driver
public class ProductDisplayDriver : ContentPartDisplayDriver<ProductPart>
{
    public override IDisplayResult Display(ProductPart part, BuildPartDisplayContext context)
    {
        return View("ProductPart", part);
    }
}

// Phase 2: Add alternates
// Phase 3: Add caching
// Phase 4: Add advanced features
```

#### **2. Monitor Performance**
```csharp
public class PerformanceMonitoringShapeProvider : IShapeTableProvider
{
    private readonly ILogger _logger;
    
    public void Discover(ShapeTableBuilder builder)
    {
        builder.Describe("ProductPart")
            .OnDisplaying(displaying =>
            {
                var stopwatch = Stopwatch.StartNew();
                
                displaying.Shape.Metadata.OnDisplayed(() =>
                {
                    stopwatch.Stop();
                    if (stopwatch.ElapsedMilliseconds > 100) // Log slow renders
                    {
                        _logger.LogWarning("Slow shape render: {ShapeType} took {ElapsedMs}ms", 
                            displaying.Shape.Metadata.Type, stopwatch.ElapsedMilliseconds);
                    }
                });
            });
    }
}
```

### **💡 KẾT LUẬN**

#### **Use cases chính cần Advanced Display Management:**
- ✅ **E-commerce**: Product displays với multiple variants
- ✅ **News portals**: Dynamic content layouts
- ✅ **Dashboards**: Multi-widget systems
- ✅ **E-learning**: Adaptive content display
- ✅ **Corporate sites**: Theme customization

#### **Lợi ích:**
- 🎯 **Flexibility**: Multiple layouts cho same content
- 🎨 **Customization**: Dynamic styling và alternates
- 📱 **Responsive**: Device-specific optimizations
- ⚡ **Performance**: Caching và lazy loading
- 🔧 **Maintainable**: Clean separation of concerns

**Advanced Display Management là core strength của OrchardCore cho các dự án cần UI linh hoạt và có nhiều variants hiển thị!**

---

*Tài liệu này được tạo dựa trên phân tích source code OrchardCore và best practices.*