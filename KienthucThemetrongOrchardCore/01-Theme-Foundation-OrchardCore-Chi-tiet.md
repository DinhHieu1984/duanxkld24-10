# 🎨 **THEME FOUNDATION ORCHARDCORE - CHI TIẾT TỪNG BƯỚC**

## 🎯 **TỔNG QUAN**

**Theme Foundation** là nền tảng cơ bản nhất của OrchardCore theme development. Dựa trên phân tích chi tiết OrchardCore source code, đây là bước đầu tiên và bắt buộc cho mọi theme project.

---

## ⏰ **KHI NÀO VIẾT**

### **🚀 TIMING: ĐẦU TIÊN - FOUNDATION LAYER**
- **Viết đầu tiên**: Là nền tảng cho tất cả theme patterns khác
- **Không thể bỏ qua**: Mọi theme đều cần foundation này
- **Thời gian**: 2-4 giờ cho basic setup, 1-2 ngày cho advanced setup

---

## 🔍 **PHÂN TÍCH SOURCE CODE ORCHARDCORE**

### **📁 THEMES ĐƯỢC PHÂN TÍCH:**
- **TheTheme**: Default theme với full dependencies
- **TheAgencyTheme**: Agency/business theme
- **TheBlogTheme**: Blog-specific theme với Liquid
- **TheComingSoonTheme**: Landing page theme
- **SafeMode**: Minimal fail-safe theme
- **TheAdmin**: Admin interface theme

### **🏗️ CẤU TRÚC THEME THỰC TẾ:**
```
MyTheme/
├── 📄 Manifest.cs                    # [assembly: Theme] attribute
├── 📄 MyTheme.csproj                 # Project với OrchardCore.Theme.Targets
├── 📂 Views/                         # Razor templates (.cshtml)
├── 📂 wwwroot/                       # Static assets
└── 📂 Assets/                        # Source files (optional)
```

---

## 📝 **BƯỚC 1: TẠO MANIFEST.CS**

### **🔧 1.1. BASIC MANIFEST (THEO CHUẨN ORCHARDCORE)**

```csharp
using OrchardCore.DisplayManagement.Manifest;
using OrchardCore.Modules.Manifest;

[assembly: Theme(
    Name = "My Custom Theme",
    Author = ManifestConstants.OrchardCoreTeam,
    Website = ManifestConstants.OrchardCoreWebsite,
    Version = ManifestConstants.OrchardCoreVersion,
    Description = "A custom theme for OrchardCore.",
    Tags = new[]
    {
        "Bootstrap",
        "Responsive",
        "Custom"
    }
)]
```

**📋 Giải thích từ source code:**
- `ManifestConstants.OrchardCoreTeam` = "The Orchard Core Team"
- `ManifestConstants.OrchardCoreVersion` = "3.0.0"
- `ManifestConstants.OrchardCoreWebsite` = "https://orchardcore.net"
- `Tags` thường dùng: "Bootstrap", "Landing page", "Liquid", "Admin"

### **🚀 1.2. ADVANCED MANIFEST VỚI BASE THEME**

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
        "OrchardCore.Themes"
    },
    Tags = new[]
    {
        "Bootstrap",
        "Enterprise",
        "Responsive",
        "PWA"
    }
)]

// Feature cho Dark Mode (theo pattern TheAdmin)
[assembly: Feature(
    Id = "EnterpriseTheme.DarkMode",
    Name = "Dark Mode Support",
    Description = "Adds dark mode toggle functionality.",
    Dependencies = new[] { "EnterpriseTheme" },
    Category = "Theme"
)]
```

### **🎯 1.3. THEME VỚI BASE THEME (KẾ THỪA)**

```csharp
using OrchardCore.DisplayManagement.Manifest;
using OrchardCore.Modules.Manifest;

[assembly: Theme(
    Name = "Custom Agency Theme",
    Author = "Custom Developer",
    Website = "https://customdev.com",
    Version = "1.0.0",
    Description = "Custom theme based on Agency Theme.",
    BaseTheme = "TheAgencyTheme",  // Kế thừa từ TheAgencyTheme
    Tags = new[]
    {
        "Bootstrap",
        "Agency",
        "Custom"
    }
)]
```

**📋 Từ ThemeAttribute.cs:**
- `BaseTheme` cho phép kế thừa từ theme khác
- `DefaultBaseTheme = ""` khi không kế thừa
- ThemeAttribute kế thừa từ ModuleAttribute

---

## 📦 **BƯỚC 2: TẠO PROJECT FILE (.CSPROJ)**

### **🔧 2.1. BASIC PROJECT FILE (THEO THEAGENCYTHEME)**

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <!-- NuGet properties-->
    <Title>My Custom Theme</Title>
    <Description>A custom theme for OrchardCore CMS.</Description>
    <PackageTags>OrchardCoreCMS Theme</PackageTags>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="OrchardCore.ResourceManagement.Abstractions" />
    <ProjectReference Include="OrchardCore.Theme.Targets" />
    <ProjectReference Include="OrchardCore.DisplayManagement" />
  </ItemGroup>

</Project>
```

### **🚀 2.2. ADVANCED PROJECT FILE VỚI RAZOR (THEO THETHEME)**

```xml
<Project Sdk="Microsoft.NET.Sdk.Razor">

  <PropertyGroup>
    <AddRazorSupportForMvc>true</AddRazorSupportForMvc>
    
    <!-- NuGet properties-->
    <Title>My Advanced Theme</Title>
    <Description>Advanced theme with Razor support.</Description>
    <PackageTags>OrchardCoreCMS Theme</PackageTags>
  </PropertyGroup>

  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="OrchardCore.Flows" />
    <ProjectReference Include="OrchardCore.Notifications" />
    <ProjectReference Include="OrchardCore.Themes" />
    <ProjectReference Include="OrchardCore.Theme.Targets" />
    <ProjectReference Include="OrchardCore.Admin.Abstractions" />
    <ProjectReference Include="OrchardCore.ContentManagement" />
    <ProjectReference Include="OrchardCore.ResourceManagement" />
    <ProjectReference Include="OrchardCore.DisplayManagement" />
    <ProjectReference Include="OrchardCore.Users.Abstractions" />
    <ProjectReference Include="OrchardCore.Users" />
    <ProjectReference Include="OrchardCore.Menu" />
  </ItemGroup>

</Project>
```

### **📦 2.3. TEMPLATE PROJECT FILE (THEO ORCHARDCORE.TEMPLATES.THEME)**

```xml
<Project Sdk="Microsoft.NET.Sdk.Razor">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <AddRazorSupportForMvc>true</AddRazorSupportForMvc>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="OrchardCore.Theme.Targets" Version="2.0.0" />
    <PackageReference Include="OrchardCore.ContentManagement" Version="2.0.0" />
    <PackageReference Include="OrchardCore.DisplayManagement" Version="2.0.0" />
    <PackageReference Include="OrchardCore.ResourceManagement" Version="2.0.0" />
  </ItemGroup>

</Project>
```

**📋 Giải thích từ source code:**
- `Microsoft.NET.Sdk.Razor`: Cho themes có Razor templates
- `Microsoft.NET.Sdk`: Cho themes đơn giản chỉ có static assets
- `OrchardCore.Theme.Targets`: **BẮT BUỘC** - Convert project thành theme
- `AddRazorSupportForMvc=true`: Enable Razor support

---

## 🏗️ **BƯỚC 3: HIỂU ORCHARDCORE.THEME.TARGETS**

### **🔍 3.1. ORCHARDCORE.THEME.TARGETS.PROPS**

```xml
<Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <PropertyGroup>
    <ModuleType>Theme</ModuleType>
  </PropertyGroup>
</Project>
```

**📋 Chức năng:**
- Set `ModuleType=Theme` để MSBuild biết đây là theme
- Import vào tất cả theme projects

### **🔍 3.2. VALIDATION LOGIC (TỪ TARGETS FILE)**

```xml
<!-- Fail ASAP when one+ THEME declaration has been detected. -->
<Target Name="OrchardCoreErrorsThemeAtMostOne">
  <Error
    Code="OC3001"
    Condition="@(OrchardCoreThemes->Count()) > 1"
    Text="'$(MSBuildProjectName)' cannot declare itself to be more than one 'Theme'." />
</Target>

<!-- Fail ASAP when MODULE+THEME declaration has been detected -->
<Target Name="OrchardCoreErrorsThemeAndModuleRedundant">
  <Error
    Code="OC3002"
    Condition="@(OrchardCoreModules->Count()) > 0 And @(OrchardCoreThemes->Count()) > 0"
    Text="'$(MSBuildProjectName)' cannot declare itself to be both a 'Module' and a 'Theme'." />
</Target>
```

**📋 Validation Rules:**
- ❌ Không thể có nhiều hơn 1 Theme declaration
- ❌ Không thể vừa là Module vừa là Theme
- ✅ Chỉ được có 1 Theme declaration duy nhất

### **🔍 3.3. AUTO-GENERATE THEMEATTRIBUTE**

```xml
<Target Name="OrchardCoreEmbedThemes">
  <ItemGroup>
    <AssemblyAttribute Include="OrchardCore.DisplayManagement.Manifest.ThemeAttribute">
      <_Parameter1>@(OrchardCoreThemes)</_Parameter1>
      <_Parameter2>%(OrchardCoreThemes.Name)</_Parameter2>
      <_Parameter3>%(OrchardCoreThemes.Base)</_Parameter3>
      <!-- ... more parameters ... -->
    </AssemblyAttribute>
  </ItemGroup>
</Target>
```

**📋 Chức năng:**
- Auto-generate ThemeAttribute trong assembly
- Embed theme metadata vào compiled assembly
- Cho phép OrchardCore discover theme at runtime

---

## 📁 **BƯỚC 4: TẠO FOLDER STRUCTURE**

### **🏗️ 4.1. BASIC FOLDER STRUCTURE**

```
MyTheme/
├── 📄 Manifest.cs
├── 📄 MyTheme.csproj
├── 📂 Views/
│   └── 📄 Layout.cshtml
└── 📂 wwwroot/
    ├── 📂 styles/
    ├── 📂 scripts/
    └── 📂 images/
        └── 📄 favicon.ico
```

### **🚀 4.2. ADVANCED FOLDER STRUCTURE (THEO THETHEME)**

```
MyTheme/
├── 📄 Manifest.cs
├── 📄 MyTheme.csproj
├── 📄 Startup.cs
├── 📄 Assets.json
├── 📂 Views/
│   ├── 📄 Layout.cshtml
│   ├── 📄 _ViewImports.cshtml
│   ├── 📄 Branding.cshtml
│   ├── 📄 Menu.cshtml
│   ├── 📄 MenuItem.cshtml
│   └── 📂 Home/
│       └── 📄 Index.cshtml
├── 📂 wwwroot/
│   ├── 📂 styles/
│   │   ├── 📄 theme.css
│   │   └── 📄 theme.min.css
│   ├── 📂 scripts/
│   └── 📂 images/
│       ├── 📄 favicon.ico
│       └── 📄 Theme.png
├── 📂 Assets/
│   ├── 📂 scss/
│   │   └── 📄 theme.scss
│   └── 📂 ts/
└── 📂 Drivers/
    └── 📄 CustomDisplayDriver.cs
```

---

## 🎯 **BƯỚC 5: TẠO BASIC VIEWS**

### **📄 5.1. VIEWS/_VIEWIMPORTS.CSHTML**

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

### **📄 5.2. VIEWS/LAYOUT.CSHTML (BASIC)**

```html
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
    
    <!-- Head Resources -->
    <resources type="Header" />
</head>
<body>
    <!-- Navigation -->
    <nav class="navbar navbar-expand-lg navbar-light bg-light">
        <div class="container">
            <shape type="Branding" />
            <menu alias="alias:main-menu" />
        </div>
    </nav>

    <!-- Main Content -->
    <main class="container my-4">
        @await RenderBodyAsync()
    </main>

    <!-- Footer Scripts -->
    <resources type="FootScript" />
</body>
</html>
```

---

## 🎨 **THEME TYPES DỰA TRÊN SOURCE CODE**

### **🟢 1. SIMPLE THEME (THEO THEAGENCYTHEME)**

```csharp
[assembly: Theme(
    Name = "Simple Theme",
    Author = ManifestConstants.OrchardCoreTeam,
    Website = ManifestConstants.OrchardCoreWebsite,
    Version = ManifestConstants.OrchardCoreVersion,
    Description = "A simple theme for basic websites.",
    Tags = ["Bootstrap", "Simple"]
)]
```

**📦 Project Dependencies:**
- OrchardCore.ResourceManagement.Abstractions
- OrchardCore.Theme.Targets
- OrchardCore.DisplayManagement

### **🟡 2. INTERMEDIATE THEME (THEO THEBLOGTHEME)**

```csharp
[assembly: Theme(
    Name = "Blog Theme",
    Author = ManifestConstants.OrchardCoreTeam,
    Website = ManifestConstants.OrchardCoreWebsite,
    Version = ManifestConstants.OrchardCoreVersion,
    Description = "A theme adapted for blogs.",
    Tags = ["Blog", "Bootstrap", "Liquid"]
)]
```

**📦 Project Dependencies:**
- Basic dependencies + Liquid support
- Content management features
- Blog-specific layouts

### **🔴 3. COMPLEX THEME (THEO THETHEME)**

```csharp
[assembly: Theme(
    Name = "The Default Theme",
    Author = ManifestConstants.OrchardCoreTeam,
    Website = ManifestConstants.OrchardCoreWebsite,
    Version = ManifestConstants.OrchardCoreVersion,
    Description = "The default Theme.",
    Dependencies = ["OrchardCore.Themes"],
    Tags = ["Bootstrap", "Default"]
)]
```

**📦 Project Dependencies:**
- OrchardCore.Flows
- OrchardCore.Notifications
- OrchardCore.Users
- OrchardCore.Menu
- OrchardCore.ContentManagement
- Full feature set

### **🛡️ 4. SAFE MODE THEME**

```csharp
[assembly: Theme(
    Name = "Safe Mode Theme",
    Author = ManifestConstants.OrchardCoreTeam,
    Website = ManifestConstants.OrchardCoreWebsite,
    Version = ManifestConstants.OrchardCoreVersion,
    Description = "A fail-safe theme when no other themes are available.",
    Tags = ["Safe"]
)]
```

**📦 Minimal Dependencies:**
- Chỉ basic OrchardCore dependencies
- Fail-safe khi themes khác lỗi

---

## ✅ **CHECKLIST THEME FOUNDATION**

### **🔧 BASIC SETUP (BẮT BUỘC)**
- [ ] ✅ Tạo `Manifest.cs` với `[assembly: Theme]` attribute
- [ ] ✅ Sử dụng `ManifestConstants` cho Author, Website, Version
- [ ] ✅ Setup `.csproj` với `OrchardCore.Theme.Targets` reference
- [ ] ✅ Chọn đúng SDK: `Microsoft.NET.Sdk` hoặc `Microsoft.NET.Sdk.Razor`
- [ ] ✅ Tạo folder structure: `Views/`, `wwwroot/`
- [ ] ✅ Tạo basic `Layout.cshtml`
- [ ] ✅ Tạo `_ViewImports.cshtml` với OrchardCore imports

### **🚀 ADVANCED SETUP (KHUYẾN NGHỊ)**
- [ ] ✅ Add `Dependencies` array nếu cần
- [ ] ✅ Setup `BaseTheme` nếu kế thừa theme khác
- [ ] ✅ Add multiple `[assembly: Feature]` cho sub-features
- [ ] ✅ Setup `Assets.json` cho asset compilation
- [ ] ✅ Add `Startup.cs` cho custom services
- [ ] ✅ Create `Drivers/` folder cho display drivers

### **🎯 VALIDATION (KIỂM TRA)**
- [ ] ✅ Build project thành công (no OC3001, OC3002 errors)
- [ ] ✅ Theme xuất hiện trong Admin > Design > Themes
- [ ] ✅ Có thể activate theme thành công
- [ ] ✅ Layout.cshtml render đúng
- [ ] ✅ Resources load đúng (CSS, JS)

---

## 🚫 **NHỮNG LỖI THƯỜNG GẶP**

### **❌ MANIFEST ERRORS**
```csharp
// ❌ SAI: Thiếu using statements
[assembly: Theme(Name = "My Theme")]

// ✅ ĐÚNG: Đầy đủ using statements
using OrchardCore.DisplayManagement.Manifest;
using OrchardCore.Modules.Manifest;

[assembly: Theme(
    Name = "My Theme",
    Author = ManifestConstants.OrchardCoreTeam,
    Website = ManifestConstants.OrchardCoreWebsite,
    Version = ManifestConstants.OrchardCoreVersion
)]
```

### **❌ PROJECT FILE ERRORS**
```xml
<!-- ❌ SAI: Thiếu OrchardCore.Theme.Targets -->
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <ProjectReference Include="OrchardCore.DisplayManagement" />
  </ItemGroup>
</Project>

<!-- ✅ ĐÚNG: Có OrchardCore.Theme.Targets -->
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <ProjectReference Include="OrchardCore.Theme.Targets" />
    <ProjectReference Include="OrchardCore.DisplayManagement" />
  </ItemGroup>
</Project>
```

### **❌ LAYOUT ERRORS**
```html
<!-- ❌ SAI: Thiếu OrchardCore-specific elements -->
<!DOCTYPE html>
<html>
<head>
    <title>My Site</title>
</head>
<body>
    @RenderBody()
</body>
</html>

<!-- ✅ ĐÚNG: Đầy đủ OrchardCore elements -->
<!DOCTYPE html>
<html lang="@Orchard.CultureName()" dir="@Orchard.CultureDir()">
<head>
    <title>@RenderTitleSegments(Site.SiteName, "before")</title>
    <resources type="Header" />
</head>
<body>
    @await RenderBodyAsync()
    <resources type="FootScript" />
</body>
</html>
```

---

## 📊 **PERFORMANCE METRICS**

### **⚡ BUILD TIME**
- **Simple Theme**: < 10 seconds
- **Intermediate Theme**: 10-30 seconds  
- **Complex Theme**: 30-60 seconds

### **📦 PACKAGE SIZE**
- **Minimal Theme**: < 50KB
- **Standard Theme**: 50-200KB
- **Full-featured Theme**: 200KB-1MB

### **🚀 RUNTIME PERFORMANCE**
- **Theme Discovery**: < 100ms
- **Theme Activation**: < 500ms
- **First Page Load**: < 2 seconds

---

## 🎯 **NEXT STEPS**

Sau khi hoàn thành Theme Foundation, anh có thể tiếp tục với:

1. **🖼️ Layout & Template Patterns** - Advanced layouts và templates
2. **🎭 Shape System & Display Management** - Custom shapes và display logic
3. **🎯 Responsive Design & CSS Framework** - Mobile-first responsive design
4. **🎪 Asset Management & Optimization** - Advanced asset pipeline

---

## 🔗 **REFERENCES TỪ SOURCE CODE**

- **ThemeAttribute.cs**: `/src/OrchardCore/OrchardCore.DisplayManagement/Manifest/ThemeAttribute.cs`
- **ManifestConstants.cs**: `/src/OrchardCore/OrchardCore.Abstractions/Modules/Manifest/ManifestConstants.cs`
- **OrchardCore.Theme.Targets**: `/src/OrchardCore/OrchardCore.Theme.Targets/`
- **TheTheme**: `/src/OrchardCore.Themes/TheTheme/`
- **TheAgencyTheme**: `/src/OrchardCore.Themes/TheAgencyTheme/`

---

**🎉 Theme Foundation là bước đầu tiên quan trọng nhất! Với foundation vững chắc dựa trên OrchardCore source code, anh có thể xây dựng theme professional và scalable! 🚀🎨**

---

*Timing: 2-4 giờ cho basic setup, 1-2 ngày cho advanced setup với tất cả features.*