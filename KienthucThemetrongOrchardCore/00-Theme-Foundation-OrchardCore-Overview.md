# 🎨 **THEME FOUNDATION ORCHARDCORE - TỔNG QUAN & OVERVIEW**

## 🎯 **TỔNG QUAN**

**Theme Foundation** là nền tảng cơ bản nhất của OrchardCore theme development. Đây là bước đầu tiên và bắt buộc cho mọi theme project, định nghĩa cấu trúc, metadata và các thành phần cốt lõi của theme.

---

## ⏰ **KHI NÀO VIẾT**

### **🚀 TIMING: ĐẦU TIÊN - FOUNDATION LAYER**
- **Viết đầu tiên**: Là nền tảng cho tất cả theme patterns khác
- **Không thể bỏ qua**: Mọi theme đều cần foundation này
- **Thời gian**: 2-4 giờ cho basic setup, 1-2 ngày cho advanced setup

### **📋 PREREQUISITES**
- ✅ OrchardCore project đã setup
- ✅ Visual Studio hoặc VS Code
- ✅ .NET SDK 8.0+
- ✅ Node.js (nếu cần asset compilation)

---

## 🏗️ **CẤU TRÚC THEME ORCHARDCORE**

### **📁 FOLDER STRUCTURE CHUẨN**
```
MyTheme/
├── 📄 Manifest.cs                    # Theme metadata & dependencies
├── 📄 MyTheme.csproj                 # Project file
├── 📄 Startup.cs                     # Services & DI registration (optional)
├── 📄 Assets.json                    # Asset build configuration (optional)
├── 📂 Views/                         # Razor templates
│   ├── 📄 Layout.cshtml              # Main layout template
│   ├── 📄 _ViewImports.cshtml        # View imports
│   └── 📂 Home/                      # Controller-specific views
│       └── 📄 Index.cshtml
├── 📂 wwwroot/                       # Static assets
│   ├── 📂 styles/                    # CSS files
│   ├── 📂 scripts/                   # JavaScript files
│   └── 📂 images/                    # Images & icons
├── 📂 Assets/                        # Source assets (SCSS, TypeScript)
│   ├── 📂 scss/
│   └── 📂 ts/
└── 📂 Drivers/                       # Display drivers (optional)
    └── 📄 CustomDisplayDriver.cs
```

---

## 📝 **1. MANIFEST.CS - THEME METADATA**

### **🔧 BASIC MANIFEST**
```csharp
using OrchardCore.DisplayManagement.Manifest;
using OrchardCore.Modules.Manifest;

[assembly: Theme(
    Name = "My Custom Theme",
    Author = "Your Name",
    Website = "https://yourwebsite.com",
    Version = "1.0.0",
    Description = "A custom theme for OrchardCore.",
    Dependencies = new[]
    {
        "OrchardCore.Themes"
    },
    Tags = new[]
    {
        "Bootstrap",
        "Responsive",
        "Custom"
    }
)]
```

### **🚀 ADVANCED MANIFEST WITH FEATURES**
```csharp
using OrchardCore.DisplayManagement.Manifest;
using OrchardCore.Modules.Manifest;

[assembly: Theme(
    Name = "Enterprise Theme",
    Author = "Enterprise Team",
    Website = "https://enterprise.com",
    Version = "2.1.0",
    Description = "Professional enterprise theme with advanced features.",
    Dependencies = new[]
    {
        "OrchardCore.Themes",
        "OrchardCore.Resources",
        "OrchardCore.Liquid"
    },
    Tags = new[]
    {
        "Bootstrap",
        "Enterprise",
        "Responsive",
        "PWA",
        "Accessibility"
    },
    Category = "Theme"
)]

[assembly: Feature(
    Id = "EnterpriseTheme.DarkMode",
    Name = "Dark Mode Support",
    Description = "Adds dark mode toggle functionality.",
    Dependencies = new[] { "EnterpriseTheme" },
    Category = "Theme"
)]

[assembly: Feature(
    Id = "EnterpriseTheme.PWA",
    Name = "Progressive Web App",
    Description = "PWA features for mobile app experience.",
    Dependencies = new[] { "EnterpriseTheme" },
    Category = "Theme"
)]
```

---

## 🎨 **2. PROJECT FILE (.CSPROJ)**

### **📦 BASIC PROJECT FILE**
```xml
<Project Sdk="Microsoft.NET.Sdk.Razor">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <AddRazorSupportForMvc>true</AddRazorSupportForMvc>
    <GenerateEmbeddedFilesManifest>true</GenerateEmbeddedFilesManifest>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="OrchardCore.Theme.Targets" Version="2.0.0" />
    <PackageReference Include="OrchardCore.DisplayManagement" Version="2.0.0" />
  </ItemGroup>

  <ItemGroup>
    <EmbeddedResource Include="wwwroot\**\*" />
  </ItemGroup>

</Project>
```

### **🚀 ADVANCED PROJECT FILE WITH ASSET COMPILATION**
```xml
<Project Sdk="Microsoft.NET.Sdk.Razor">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <AddRazorSupportForMvc>true</AddRazorSupportForMvc>
    <GenerateEmbeddedFilesManifest>true</GenerateEmbeddedFilesManifest>
    <PreserveCompilationContext>true</PreserveCompilationContext>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="OrchardCore.Theme.Targets" Version="2.0.0" />
    <PackageReference Include="OrchardCore.DisplayManagement" Version="2.0.0" />
    <PackageReference Include="OrchardCore.ResourceManagement" Version="2.0.0" />
  </ItemGroup>

  <ItemGroup>
    <EmbeddedResource Include="wwwroot\**\*" />
    <EmbeddedResource Remove="Assets\**\*" />
  </ItemGroup>

  <!-- Asset compilation targets -->
  <Target Name="CompileAssets" BeforeTargets="Build">
    <Exec Command="npm run build" WorkingDirectory="Assets" Condition="Exists('Assets/package.json')" />
  </Target>

</Project>
```

---

## 🖼️ **3. LAYOUT.CSHTML - MAIN LAYOUT**

### **🔧 BASIC LAYOUT TEMPLATE**
```html
@using OrchardCore.DisplayManagement
@using OrchardCore.DisplayManagement.ModelBinding
@using OrchardCore.Admin.Models

@inject IDisplayManager<Navbar> DisplayManager
@inject IUpdateModelAccessor UpdateModelAccessor

@{
    // Pre-render navbar for resource injection
    var navbar = await DisplayAsync(await DisplayManager.BuildDisplayAsync(UpdateModelAccessor.ModelUpdater));
}

<!DOCTYPE html>
<html lang="@Orchard.CultureName()" dir="@Orchard.CultureDir()">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>@RenderTitleSegments(Site.SiteName, "before")</title>
    
    <!-- Favicon -->
    <link type="image/x-icon" rel="shortcut icon" href="~/MyTheme/images/favicon.ico">
    
    <!-- CSS Resources -->
    <style asp-name="bootstrap" version="5" at="Head"></style>
    <style asp-name="MyTheme" asp-src="~/MyTheme/styles/theme.min.css" 
           debug-src="~/MyTheme/styles/theme.css" at="Head"></style>
    
    <!-- JavaScript Resources -->
    <script asp-name="bootstrap" version="5" at="Foot"></script>
    
    <!-- Head Resources -->
    <resources type="Header" />
    @await RenderSectionAsync("HeadMeta", required: false)
</head>
<body>
    <!-- Navigation -->
    <nav class="navbar navbar-expand-lg navbar-light bg-light">
        <div class="container">
            <shape type="Branding" />
            <button class="navbar-toggler" type="button" data-bs-toggle="collapse" 
                    data-bs-target="#navbarNav" aria-controls="navbarNav" 
                    aria-expanded="false" aria-label="Toggle navigation">
                <span class="navbar-toggler-icon"></span>
            </button>
            <div class="collapse navbar-collapse" id="navbarNav">
                <menu alias="alias:main-menu" cache-id="main-menu" 
                      cache-fixed-duration="00:05:00" cache-tag="alias:main-menu" />
                @navbar
            </div>
        </div>
    </nav>

    <!-- Header Section -->
    @await RenderSectionAsync("Header", required: false)

    <!-- Main Content -->
    <main class="container my-4">
        @await RenderSectionAsync("Messages", required: false)
        @await RenderBodyAsync()
    </main>

    <!-- Footer -->
    @if (IsSectionDefined("Footer"))
    {
        <footer class="bg-light py-4 mt-5">
            <div class="container">
                @await RenderSectionAsync("Footer", required: false)
            </div>
        </footer>
    }

    <!-- Footer Scripts -->
    <resources type="FootScript" />
</body>
</html>
```

### **🚀 ADVANCED LAYOUT WITH RTL & THEME TOGGLE**
```html
@using OrchardCore.DisplayManagement
@using OrchardCore.DisplayManagement.ModelBinding
@using OrchardCore.Admin.Models
@using OrchardCore.Themes.Services

@inject IDisplayManager<Navbar> DisplayManager
@inject IUpdateModelAccessor UpdateModelAccessor
@inject ThemeTogglerService ThemeTogglerService

@{
    var navbar = await DisplayAsync(await DisplayManager.BuildDisplayAsync(UpdateModelAccessor.ModelUpdater));
}

<!DOCTYPE html>
<html lang="@Orchard.CultureName()" dir="@Orchard.CultureDir()" 
      data-bs-theme="@await ThemeTogglerService.CurrentTheme()" 
      data-tenant="@ThemeTogglerService.CurrentTenant">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <meta name="theme-color" content="#0d6efd" />
    <title>@RenderTitleSegments(Site.SiteName, "before")</title>
    
    <!-- Favicon & App Icons -->
    <link type="image/x-icon" rel="shortcut icon" href="~/MyTheme/images/favicon.ico">
    <link rel="apple-touch-icon" href="~/MyTheme/images/apple-touch-icon.png">
    <link rel="manifest" href="~/MyTheme/manifest.json">
    
    <!-- Critical CSS -->
    <script asp-name="theme-head" version="1" at="Head"></script>
    
    <!-- RTL Support -->
    @if (Orchard.IsRightToLeft())
    {
        <style asp-name="bootstrap-rtl" version="5" at="Head"></style>
        <style asp-name="MyTheme" depends-on="bootstrap-rtl" 
               asp-src="~/MyTheme/styles/theme-rtl.min.css" 
               debug-src="~/MyTheme/styles/theme-rtl.css" at="Head"></style>
    }
    else
    {
        <style asp-name="bootstrap" version="5" at="Head"></style>
        <style asp-name="MyTheme" 
               asp-src="~/MyTheme/styles/theme.min.css" 
               debug-src="~/MyTheme/styles/theme.css" at="Head"></style>
    }
    
    <!-- JavaScript -->
    <script asp-name="bootstrap" version="5" at="Foot"></script>
    <script asp-name="theme-manager" at="Foot"></script>
    
    <!-- Font Awesome -->
    <script asp-name="font-awesome" at="Foot" version="7"></script>
    
    <!-- Head Resources -->
    <resources type="Header" />
    @await RenderSectionAsync("HeadMeta", required: false)
</head>
<body class="@ViewBag.BodyClass">
    <!-- Skip to content for accessibility -->
    <a class="visually-hidden-focusable" href="#main-content">Skip to main content</a>
    
    <!-- Navigation -->
    <nav class="navbar navbar-expand-lg fixed-top" role="navigation" aria-label="Main navigation">
        <div class="container">
            <shape type="Branding" />
            <button class="navbar-toggler" type="button" data-bs-toggle="collapse" 
                    data-bs-target="#navbar" aria-expanded="false" 
                    aria-controls="navbar" aria-label="Toggle navigation">
                <span class="navbar-toggler-icon"></span>
            </button>
            <div class="collapse navbar-collapse" id="navbar">
                <div class="d-flex w-100 align-items-end justify-content-end justify-content-md-between flex-column flex-md-row">
                    <menu alias="alias:main-menu" cache-id="main-menu" 
                          cache-fixed-duration="00:05:00" cache-tag="alias:main-menu" 
                          cache-context="user.roles" />
                    @navbar
                </div>
            </div>
        </div>
    </nav>

    <!-- Header Section -->
    @await RenderSectionAsync("Header", required: false)

    <!-- Main Content -->
    <main id="main-content" class="container" role="main">
        @await RenderSectionAsync("Messages", required: false)
        @await RenderBodyAsync()
    </main>

    <!-- Footer -->
    @if (IsSectionDefined("Footer"))
    {
        <footer role="contentinfo">
            <div class="container">
                @await RenderSectionAsync("Footer", required: false)
            </div>
        </footer>
    }

    <!-- Footer Scripts -->
    <resources type="FootScript" />
    
    <!-- Service Worker Registration -->
    <script>
        if ('serviceWorker' in navigator) {
            navigator.serviceWorker.register('/sw.js');
        }
    </script>
</body>
</html>
```

---

## 📦 **4. _VIEWIMPORTS.CSHTML**

```csharp
@using OrchardCore.DisplayManagement
@using OrchardCore.DisplayManagement.Shapes
@using OrchardCore.DisplayManagement.Title
@using OrchardCore.Settings
@using OrchardCore.ContentManagement
@using Microsoft.AspNetCore.Identity
@using Microsoft.AspNetCore.Authorization
@using OrchardCore.Users.Models
@using OrchardCore.Entities

@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers
@addTagHelper *, OrchardCore.DisplayManagement
@addTagHelper *, OrchardCore.ResourceManagement
```

---

## 🎯 **5. STARTUP.CS - SERVICES REGISTRATION**

### **🔧 BASIC STARTUP**
```csharp
using Microsoft.Extensions.DependencyInjection;
using OrchardCore.Modules;

namespace MyTheme;

public sealed class Startup : StartupBase
{
    public override void ConfigureServices(IServiceCollection services)
    {
        // Register theme-specific services here
    }
}
```

### **🚀 ADVANCED STARTUP WITH DISPLAY DRIVERS**
```csharp
using Microsoft.Extensions.DependencyInjection;
using OrchardCore.Admin.Models;
using OrchardCore.DisplayManagement.Handlers;
using OrchardCore.Modules;
using MyTheme.Drivers;
using MyTheme.Services;

namespace MyTheme;

public sealed class Startup : StartupBase
{
    public override void ConfigureServices(IServiceCollection services)
    {
        // Register display drivers
        services.AddDisplayDriver<Navbar, CustomNavbarDisplayDriver>();
        
        // Register theme services
        services.AddScoped<IThemeCustomizationService, ThemeCustomizationService>();
        
        // Register resource management
        services.AddTransient<IConfigureOptions<ResourceManagementOptions>, ResourceManagementOptionsConfiguration>();
    }
}
```

---

## 🎨 **6. ASSETS.JSON - ASSET COMPILATION**

```json
[
  {
    "action": "sass",
    "name": "theme-mytheme",
    "source": "Assets/scss/theme.scss",
    "dest": "wwwroot/styles/",
    "tags": ["theme", "css"]
  },
  {
    "action": "typescript",
    "name": "theme-scripts",
    "source": "Assets/ts/theme.ts",
    "dest": "wwwroot/scripts/",
    "tags": ["theme", "js"]
  }
]
```

---

## 🎯 **7. BASIC SHAPES & TEMPLATES**

### **📄 BRANDING.CSHTML**
```html
<a class="navbar-brand" href="@Url.Action("Index", "Home", new { area = "" })">
    @if (!string.IsNullOrEmpty(Site.SiteName))
    {
        <span>@Site.SiteName</span>
    }
    else
    {
        <span>OrchardCore</span>
    }
</a>
```

### **📄 MENU.CSHTML**
```html
@{
    var menuItems = Model.Items as IEnumerable<dynamic>;
}

@if (menuItems != null && menuItems.Any())
{
    <ul class="navbar-nav me-auto">
        @foreach (var menuItem in menuItems)
        {
            <li class="nav-item">
                <shape type="MenuItem" model="menuItem" />
            </li>
        }
    </ul>
}
```

---

## 🚀 **8. RESOURCE MANAGEMENT**

### **📄 RESOURCEMANAGEMENTOPTIONSCONFIGURATION.CS**
```csharp
using Microsoft.Extensions.Options;
using OrchardCore.ResourceManagement;

namespace MyTheme;

public sealed class ResourceManagementOptionsConfiguration : IConfigureOptions<ResourceManagementOptions>
{
    public void Configure(ResourceManagementOptions options)
    {
        options.ResourceManifests.Add(new ResourceManifest());
    }
}

public class ResourceManifest : IResourceManifestProvider
{
    public void BuildManifests(ResourceManifestBuilder builder)
    {
        var manifest = builder.Add();

        // Theme CSS
        manifest
            .DefineStyle("MyTheme")
            .SetUrl("~/MyTheme/styles/theme.min.css", "~/MyTheme/styles/theme.css")
            .SetVersion("1.0.0");

        // Theme JavaScript
        manifest
            .DefineScript("MyTheme")
            .SetUrl("~/MyTheme/scripts/theme.min.js", "~/MyTheme/scripts/theme.js")
            .SetVersion("1.0.0")
            .SetDependencies("bootstrap");
    }
}
```

---

## 🎯 **REAL-WORLD APPLICATIONS**

### **🏥 1. HOSPITAL MANAGEMENT SYSTEM THEME**
```csharp
[assembly: Theme(
    Name = "MedicalPro Theme",
    Author = "Healthcare Solutions",
    Version = "1.0.0",
    Description = "Professional medical theme with accessibility focus.",
    Dependencies = new[]
    {
        "OrchardCore.Themes",
        "OrchardCore.Resources",
        "OrchardCore.Liquid"
    },
    Tags = new[]
    {
        "Medical",
        "Accessibility",
        "HIPAA",
        "Professional"
    }
)]
```

**Features:**
- High contrast colors for medical professionals
- Large touch targets for tablet use
- WCAG 2.1 AAA compliance
- Medical iconography integration
- Emergency alert styling

### **🎓 2. E-LEARNING PLATFORM THEME**
```csharp
[assembly: Theme(
    Name = "EduCore Theme",
    Author = "Education Team",
    Version = "2.0.0",
    Description = "Interactive educational theme with gamification elements.",
    Dependencies = new[]
    {
        "OrchardCore.Themes",
        "OrchardCore.Resources",
        "OrchardCore.Media"
    },
    Tags = new[]
    {
        "Education",
        "Interactive",
        "Gamification",
        "Mobile-First"
    }
)]
```

**Features:**
- Progress indicators and badges
- Interactive course navigation
- Video player integration
- Mobile-optimized layouts
- Dark mode for extended study sessions

### **🛒 3. E-COMMERCE PLATFORM THEME**
```csharp
[assembly: Theme(
    Name = "CommerceMax Theme",
    Author = "E-commerce Solutions",
    Version = "3.1.0",
    Description = "High-conversion e-commerce theme with PWA support.",
    Dependencies = new[]
    {
        "OrchardCore.Themes",
        "OrchardCore.Commerce",
        "OrchardCore.Media"
    },
    Tags = new[]
    {
        "E-commerce",
        "PWA",
        "High-Performance",
        "Conversion-Optimized"
    }
)]
```

**Features:**
- Product showcase layouts
- Shopping cart integration
- Payment form styling
- Performance-optimized images
- PWA capabilities for mobile shopping

---

## ✅ **CHECKLIST HOÀN THÀNH**

### **🔧 BASIC SETUP**
- [ ] ✅ Tạo Manifest.cs với theme metadata
- [ ] ✅ Setup project file (.csproj)
- [ ] ✅ Tạo Layout.cshtml cơ bản
- [ ] ✅ Thêm _ViewImports.cshtml
- [ ] ✅ Tạo folder structure (Views, wwwroot)
- [ ] ✅ Test theme activation trong admin

### **🚀 ADVANCED SETUP**
- [ ] ✅ Implement Startup.cs với services
- [ ] ✅ Setup Assets.json cho compilation
- [ ] ✅ Tạo ResourceManagement configuration
- [ ] ✅ Implement RTL support
- [ ] ✅ Add theme toggle functionality
- [ ] ✅ Setup accessibility features
- [ ] ✅ Configure PWA manifest

### **🎯 PRODUCTION READY**
- [ ] ✅ Optimize asset loading
- [ ] ✅ Implement caching strategies
- [ ] ✅ Add error handling
- [ ] ✅ Setup monitoring và analytics
- [ ] ✅ Performance testing
- [ ] ✅ Cross-browser testing
- [ ] ✅ Accessibility audit

---

## 📈 **PERFORMANCE METRICS**

### **⚡ TARGET METRICS**
- **First Contentful Paint**: < 1.5s
- **Largest Contentful Paint**: < 2.5s
- **Cumulative Layout Shift**: < 0.1
- **Time to Interactive**: < 3s
- **Bundle Size**: < 50KB (CSS + JS)

### **🎯 QUALITY METRICS**
- **Lighthouse Performance**: 90+
- **Lighthouse Accessibility**: 100
- **Lighthouse Best Practices**: 90+
- **Lighthouse SEO**: 90+

---

## 🔗 **NEXT STEPS**

1. **🖼️ Layout & Template Patterns** - Tạo advanced layouts và templates
2. **🎭 Shape System & Display Management** - Custom shapes và display logic
3. **🎯 Responsive Design & CSS Framework** - Mobile-first responsive design
4. **🎪 Asset Management & Optimization** - Advanced asset pipeline

---

**🎉 Theme Foundation là bước đầu tiên quan trọng nhất! Với foundation vững chắc, anh có thể xây dựng theme professional và scalable! 🚀🎨**

---

*Timing: 2-4 giờ cho basic setup, 1-2 ngày cho advanced setup với tất cả features.*