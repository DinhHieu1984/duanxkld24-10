# 🎨 **CÁCH THIẾT KẾ THEME ĐÚNG CHUẨN ORCHARDCORE**

## 🎯 **TÓM TẮT: 8 BƯỚC THIẾT KẾ THEME CHUẨN**

Dựa trên phân tích OrchardCore source code (TheTheme, TheBlogTheme, TheAgencyTheme, SafeMode, TheAdmin), có **8 bước cốt lõi** để thiết kế theme đúng chuẩn OrchardCore:

---

## 📋 **8 BƯỚC THIẾT KẾ THEME CHUẨN ORCHARDCORE**

### **🏗️ BƯỚC 1: THEME FOUNDATION (BẮT BUỘC)**
**⏰ Timing**: Đầu tiên - 2-4 giờ

#### **✅ PHẢI LÀM GÌ:**
```csharp
// 1.1. Tạo Manifest.cs
[assembly: Theme(
    Name = "Theme Name",
    Author = "Author Name",
    Version = "1.0.0",
    Description = "Theme description",
    Dependencies = new[] { "OrchardCore.Themes" },
    Tags = new[] { "Bootstrap", "Responsive" }
)]

// 1.2. Tạo .csproj file
<PackageReference Include="OrchardCore.Theme.Targets" Version="2.0.0" />
<PackageReference Include="OrchardCore.DisplayManagement" Version="2.0.0" />

// 1.3. Folder structure
MyTheme/
├── Manifest.cs
├── MyTheme.csproj  
├── Views/Layout.cshtml
├── Views/_ViewImports.cshtml
└── wwwroot/
```

#### **🚫 KHÔNG ĐƯỢC:**
- Thiếu Manifest.cs
- Không kế thừa OrchardCore.Theme.Targets
- Folder structure sai chuẩn

---

### **🖼️ BƯỚC 2: LAYOUT & TEMPLATES (BẮT BUỘC)**
**⏰ Timing**: Sau Foundation - 4-6 giờ

#### **✅ PHẢI LÀM GÌ:**
```html
<!-- 2.1. Layout.cshtml chuẩn -->
@using OrchardCore.DisplayManagement
@using OrchardCore.DisplayManagement.ModelBinding
@inject IDisplayManager<Navbar> DisplayManager

<!DOCTYPE html>
<html lang="@Orchard.CultureName()" dir="@Orchard.CultureDir()">
<head>
    <title>@RenderTitleSegments(Site.SiteName, "before")</title>
    <style asp-name="bootstrap" version="5" at="Head"></style>
    <resources type="Header" />
</head>
<body>
    <nav class="navbar">
        <shape type="Branding" />
        <menu alias="alias:main-menu" cache-id="main-menu" />
    </nav>
    <main>@await RenderBodyAsync()</main>
    <resources type="FootScript" />
</body>
</html>

<!-- 2.2. _ViewImports.cshtml -->
@using OrchardCore.DisplayManagement
@using OrchardCore.DisplayManagement.Shapes
@addTagHelper *, OrchardCore.DisplayManagement
@addTagHelper *, OrchardCore.ResourceManagement
```

#### **🚫 KHÔNG ĐƯỢC:**
- Không sử dụng Shape system (`<shape type="Branding" />`)
- Thiếu `@RenderTitleSegments(Site.SiteName, "before")`
- Không có `<resources type="Header" />` và `<resources type="FootScript" />`
- Thiếu `@Orchard.CultureName()` và `@Orchard.CultureDir()`

---

### **🎭 BƯỚC 3: SHAPE SYSTEM (QUAN TRỌNG)**
**⏰ Timing**: Song song với Layout - 2-3 giờ

#### **✅ PHẢI LÀM GÌ:**
```html
<!-- 3.1. Branding.cshtml -->
<a class="navbar-brand" href="@Url.Action("Index", "Home", new { area = "" })">
    @if (!string.IsNullOrEmpty(Site.SiteName))
    {
        <span>@Site.SiteName</span>
    }
</a>

<!-- 3.2. Menu.cshtml -->
@{
    var menuItems = Model.Items as IEnumerable<dynamic>;
}
@if (menuItems != null && menuItems.Any())
{
    <ul class="navbar-nav">
        @foreach (var menuItem in menuItems)
        {
            <li class="nav-item">
                <shape type="MenuItem" model="menuItem" />
            </li>
        }
    </ul>
}

<!-- 3.3. MenuItem.cshtml -->
<a class="nav-link" href="@Model.Href">@Model.Text</a>
```

#### **🚫 KHÔNG ĐƯỢC:**
- Hard-code menu items thay vì dùng Shape system
- Không sử dụng `<shape type="MenuItem" model="menuItem" />`
- Thiếu template cho các shapes cơ bản

---

### **🎯 BƯỚC 4: RESPONSIVE & CSS FRAMEWORK (BẮT BUỘC)**
**⏰ Timing**: Song song với Layout - 3-4 giờ

#### **✅ PHẢI LÀM GÌ:**
```html
<!-- 4.1. Bootstrap Integration -->
<meta name="viewport" content="width=device-width, initial-scale=1.0" />
<style asp-name="bootstrap" version="5" at="Head"></style>
<script asp-name="bootstrap" version="5" at="Foot"></script>

<!-- 4.2. RTL Support -->
@if (Orchard.IsRightToLeft())
{
    <style asp-name="bootstrap-rtl" version="5" at="Head"></style>
}

<!-- 4.3. Responsive Navigation -->
<button class="navbar-toggler" type="button" data-bs-toggle="collapse" 
        data-bs-target="#navbar" aria-controls="navbar">
    <span class="navbar-toggler-icon"></span>
</button>
<div class="collapse navbar-collapse" id="navbar">
    <!-- Menu content -->
</div>
```

#### **🚫 KHÔNG ĐƯỢC:**
- Không responsive design
- Thiếu RTL support
- Không sử dụng Bootstrap classes chuẩn

---

### **🎪 BƯỚC 5: ASSET MANAGEMENT (QUAN TRỌNG)**
**⏰ Timing**: Khi cần custom CSS/JS - 2-3 giờ

#### **✅ PHẢI LÀM GÌ:**
```json
// 5.1. Assets.json
[
  {
    "action": "sass",
    "name": "theme-mytheme",
    "source": "Assets/scss/theme.scss",
    "dest": "wwwroot/styles/",
    "tags": ["theme", "css"]
  }
]
```

```csharp
// 5.2. ResourceManagement
public class ResourceManifest : IResourceManifestProvider
{
    public void BuildManifests(ResourceManifestBuilder builder)
    {
        var manifest = builder.Add();
        
        manifest
            .DefineStyle("MyTheme")
            .SetUrl("~/MyTheme/styles/theme.min.css", "~/MyTheme/styles/theme.css")
            .SetVersion("1.0.0");
    }
}
```

```html
<!-- 5.3. Asset Loading -->
<style asp-name="MyTheme" asp-src="~/MyTheme/styles/theme.min.css" 
       debug-src="~/MyTheme/styles/theme.css" at="Head"></style>
```

#### **🚫 KHÔNG ĐƯỢC:**
- Hard-code CSS/JS paths
- Không sử dụng `asp-name` attributes
- Thiếu versioning cho assets

---

### **🔧 BƯỚC 6: SERVICES & STARTUP (TÙY CHỌN)**
**⏰ Timing**: Khi cần custom logic - 1-2 giờ

#### **✅ PHẢI LÀM GÌ:**
```csharp
// 6.1. Startup.cs
public sealed class Startup : StartupBase
{
    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddDisplayDriver<Navbar, CustomNavbarDisplayDriver>();
        services.AddScoped<IThemeService, ThemeService>();
    }
}

// 6.2. Display Driver
public sealed class CustomNavbarDisplayDriver : DisplayDriver<Navbar>
{
    public override IDisplayResult Display(Navbar model, BuildDisplayContext context)
    {
        return View("CustomNavbar", model)
            .Location("Content:10");
    }
}
```

#### **🚫 KHÔNG ĐƯỢC:**
- Đăng ký services không cần thiết
- Không follow DisplayDriver pattern

---

### **♿ BƯỚC 7: ACCESSIBILITY & SEO (PRODUCTION)**
**⏰ Timing**: Trước production - 2-3 giờ

#### **✅ PHẢI LÀM GÌ:**
```html
<!-- 7.1. Accessibility -->
<a class="visually-hidden-focusable" href="#main-content">Skip to main content</a>
<nav role="navigation" aria-label="Main navigation">
<main id="main-content" role="main">
<footer role="contentinfo">

<!-- 7.2. SEO Meta Tags -->
<meta name="viewport" content="width=device-width, initial-scale=1.0" />
<meta name="theme-color" content="#0d6efd" />
<title>@RenderTitleSegments(Site.SiteName, "before")</title>

<!-- 7.3. ARIA Labels -->
<button aria-expanded="false" aria-controls="navbar" aria-label="Toggle navigation">
```

#### **🚫 KHÔNG ĐƯỢC:**
- Thiếu semantic HTML
- Không có skip links
- Thiếu ARIA labels

---

### **🚀 BƯỚC 8: PERFORMANCE & OPTIMIZATION (PRODUCTION)**
**⏰ Timing**: Cuối cùng - 1-2 giờ

#### **✅ PHẢI LÀM GÌ:**
```html
<!-- 8.1. Resource Optimization -->
<style asp-name="bootstrap" version="5" at="Head"></style>
<script asp-name="bootstrap" version="5" at="Foot"></script>

<!-- 8.2. Caching -->
<menu alias="alias:main-menu" cache-id="main-menu" 
      cache-fixed-duration="00:05:00" cache-tag="alias:main-menu" />

<!-- 8.3. Lazy Loading -->
<img loading="lazy" src="image.jpg" alt="Description" />
```

#### **🚫 KHÔNG ĐƯỢC:**
- Load tất cả resources ở Head
- Không sử dụng caching
- Thiếu image optimization

---

## 🎯 **CHECKLIST THEME CHUẨN ORCHARDCORE**

### **✅ BẮT BUỘC (KHÔNG THỂ THIẾU)**
- [ ] **Manifest.cs** với `[assembly: Theme]` attribute
- [ ] **Layout.cshtml** với Shape system và Resource management
- [ ] **_ViewImports.cshtml** với OrchardCore imports
- [ ] **Bootstrap integration** với responsive design
- [ ] **Shape templates** (Branding, Menu, MenuItem)
- [ ] **RTL support** với `@Orchard.IsRightToLeft()`
- [ ] **Resource management** với `asp-name` attributes

### **🚀 QUAN TRỌNG (NÊN CÓ)**
- [ ] **Assets.json** cho SCSS/TypeScript compilation
- [ ] **ResourceManagement** configuration
- [ ] **Display Drivers** cho custom logic
- [ ] **Accessibility** features (skip links, ARIA)
- [ ] **SEO optimization** (meta tags, semantic HTML)
- [ ] **Caching** cho menu và resources

### **🌟 ADVANCED (TÙY CHỌN)**
- [ ] **PWA support** (manifest, service worker)
- [ ] **Theme toggle** (dark/light mode)
- [ ] **Multi-language** support
- [ ] **Performance optimization** (lazy loading, CDN)

---

## 🚫 **NHỮNG LỖI THƯỜNG GẶP**

### **❌ LỖI FOUNDATION**
- Thiếu `OrchardCore.Theme.Targets` package
- Manifest.cs không đúng format
- Folder structure không chuẩn

### **❌ LỖI LAYOUT**
- Không sử dụng `@RenderTitleSegments()`
- Thiếu `<resources type="Header" />` và `<resources type="FootScript" />`
- Hard-code menu thay vì dùng Shape system

### **❌ LỖI ASSETS**
- Hard-code CSS/JS paths
- Không sử dụng `asp-name` attributes
- Thiếu versioning

### **❌ LỖI ACCESSIBILITY**
- Thiếu semantic HTML
- Không có skip links
- Thiếu ARIA labels

---

## 📊 **THEME COMPLEXITY LEVELS**

### **🟢 SIMPLE THEME (4-5 bước)**
**Ví dụ**: Blog theme, Portfolio theme
- Foundation + Layout + Shapes + Responsive + Assets

### **🟡 INTERMEDIATE THEME (6-7 bước)**  
**Ví dụ**: Corporate theme, E-commerce theme
- Simple + Services + Accessibility + Performance

### **🔴 COMPLEX THEME (8 bước)**
**Ví dụ**: Enterprise theme, Multi-purpose theme
- Intermediate + Advanced features + Full optimization

---

## ⏰ **TIMELINE DEVELOPMENT**

### **🚀 WEEK 1: FOUNDATION**
- **Day 1-2**: Foundation + Layout setup
- **Day 3-4**: Shape system + Templates
- **Day 5**: Responsive + CSS framework

### **🎨 WEEK 2: FEATURES**
- **Day 1-2**: Asset management + Optimization
- **Day 3**: Services + Custom logic
- **Day 4-5**: Accessibility + SEO + Performance

### **🏆 TOTAL TIME**
- **Simple Theme**: 1 tuần
- **Intermediate Theme**: 2 tuần  
- **Complex Theme**: 3-4 tuần

---

## 🎯 **KẾT LUẬN**

**Để thiết kế theme đúng chuẩn OrchardCore:**

1. **BẮT BUỘC**: 4 bước đầu (Foundation, Layout, Shapes, Responsive)
2. **QUAN TRỌNG**: Thêm 2 bước (Assets, Accessibility) 
3. **PROFESSIONAL**: Đủ 8 bước với Performance optimization

**Nguyên tắc vàng**: 
- ✅ **LUÔN** sử dụng Shape system
- ✅ **LUÔN** sử dụng Resource management với `asp-name`
- ✅ **LUÔN** support RTL và responsive
- ✅ **LUÔN** follow OrchardCore conventions

**🎉 Với 8 bước này, anh có thể tạo theme professional và maintainable cho OrchardCore! 🚀🎨**