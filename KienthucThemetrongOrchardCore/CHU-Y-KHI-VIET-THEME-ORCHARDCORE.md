# ⚠️ CHÚ Ý KHI VIẾT THEME ORCHARDCORE

## ✅ CHECKLIST CHUẨN ORCHARDCORE

### 🚫 KHÔNG ĐƯỢC LÀM:

#### ❌ **Thiếu `[assembly: Theme]` trong Manifest.cs**
```csharp
// ❌ SAI: Không có assembly attribute
namespace MyTheme
{
    public class Manifest : IThemeAttributeProvider
    {
        // Missing [assembly: Theme] attribute
    }
}

// ✅ ĐÚNG: Phải có assembly attribute
using OrchardCore.DisplayManagement.Manifest;

[assembly: Theme(
    Name = "My Theme",
    Author = "Your Name",
    Website = "https://yourwebsite.com",
    Version = "1.0.0",
    Description = "My custom OrchardCore theme"
)]
```

#### ❌ **Hard-code menu items thay vì Shape system**
```html
<!-- ❌ SAI: Hard-code menu -->
<nav class="navbar">
    <ul>
        <li><a href="/home">Home</a></li>
        <li><a href="/about">About</a></li>
        <li><a href="/contact">Contact</a></li>
    </ul>
</nav>

<!-- ✅ ĐÚNG: Sử dụng Shape system -->
<nav class="navbar">
    <menu alias="alias:main-menu" cache-id="main-menu" cache-fixed-duration="00:05:00" />
</nav>
```

#### ❌ **Không sử dụng `asp-name` cho resources**
```html
<!-- ❌ SAI: Hard-code resource paths -->
<link rel="stylesheet" href="/css/bootstrap.min.css" />
<script src="/js/jquery.min.js"></script>

<!-- ✅ ĐÚNG: Sử dụng asp-name -->
<style asp-name="bootstrap" version="5"></style>
<script asp-name="jQuery" at="Foot"></script>
```

#### ❌ **Thiếu `@RenderTitleSegments()` và `<resources type="Header" />`**
```html
<!-- ❌ SAI: Thiếu title segments và resources -->
<head>
    <title>My Site</title>
    <!-- Missing RenderTitleSegments and resources -->
</head>

<!-- ✅ ĐÚNG: Đầy đủ head elements -->
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>@RenderTitleSegments(ViewBag.Title, Site.SiteName, " - ")</title>
    <resources type="Meta" />
    <resources type="HeadLink" />
    <resources type="Stylesheet" />
    <resources type="Header" />
</head>
```

#### ❌ **Không support RTL với `@Orchard.IsRightToLeft()`**
```html
<!-- ❌ SAI: Không support RTL -->
<html lang="en">
<body>
    <div class="container">
        <!-- No RTL support -->
    </div>
</body>
</html>

<!-- ✅ ĐÚNG: Support RTL -->
<html lang="@Orchard.CultureName()" dir="@Orchard.CultureDir()">
<body>
    <div class="container@(Orchard.IsRightToLeft() ? " rtl" : "")">
        <!-- RTL-aware content -->
    </div>
</body>
</html>
```

---

## ✅ PHẢI LÀM:

### ✅ **Sử dụng Shape system**
```html
<!-- ✅ ĐÚNG: Shape-based rendering -->
<shape type="Branding" />
<shape type="Navigation" />
<shape type="Content" />
```

### ✅ **Resource management**
```html
<!-- ✅ ĐÚNG: Proper resource management -->
<style asp-name="bootstrap" version="5" at="Head"></style>
<style asp-name="theme-styles" at="Head"></style>
<script asp-name="jQuery" at="Foot"></script>
<script asp-name="bootstrap" at="Foot"></script>
```

### ✅ **Caching**
```html
<!-- ✅ ĐÚNG: Menu caching -->
<menu cache-id="main-menu" cache-fixed-duration="00:05:00">
```

### ✅ **Accessibility: Skip links, ARIA labels, semantic HTML**
```html
<!-- ✅ ĐÚNG: Accessibility features -->
<a href="#main-content" class="skip-link">Skip to main content</a>

<nav aria-label="Main navigation">
    <menu alias="alias:main-menu" />
</nav>

<main id="main-content" role="main">
    @RenderBody()
</main>

<footer role="contentinfo">
    <shape type="Footer" />
</footer>
```

### ✅ **Performance: Load CSS ở Head, JS ở Foot**
```html
<!-- ✅ ĐÚNG: Performance optimization -->
<head>
    <!-- CSS in head for faster rendering -->
    <resources type="Stylesheet" />
    <resources type="Header" />
</head>
<body>
    <!-- Content -->
    
    <!-- JS at foot for better performance -->
    <resources type="Footer" />
</body>
```

---

## 🎯 NGUYÊN TẮC VÀNG

### 1. 🎭 **LUÔN** sử dụng Shape system thay vì hard-code
```csharp
// ✅ ĐÚNG: Shape-based approach
public override IDisplayResult Display(MyPart part, BuildDisplayContext context)
{
    return Initialize<MyPartViewModel>("MyPart", model =>
    {
        model.Title = part.Title;
        model.Content = part.Content;
    }).Location("Detail", "Content:1");
}
```

### 2. 📦 **LUÔN** sử dụng Resource management với `asp-name`
```html
<!-- ✅ ĐÚNG: Resource management -->
<style asp-name="bootstrap" version="5"></style>
<style asp-name="font-awesome" version="6"></style>
<script asp-name="jQuery" at="Foot"></script>
```

### 3. 🌐 **LUÔN** support RTL và multi-language
```html
<!-- ✅ ĐÚNG: Multi-language support -->
<html lang="@Orchard.CultureName()" dir="@Orchard.CultureDir()">
<body class="@Orchard.CultureName().Replace("-", "_")@(Orchard.IsRightToLeft() ? " rtl" : "")">
```

### 4. 📱 **LUÔN** responsive và mobile-first
```css
/* ✅ ĐÚNG: Mobile-first approach */
.container {
    width: 100%;
    padding: 0 15px;
}

@media (min-width: 768px) {
    .container {
        max-width: 750px;
        margin: 0 auto;
    }
}

@media (min-width: 1200px) {
    .container {
        max-width: 1140px;
    }
}
```

### 5. ♿ **LUÔN** accessibility-compliant
```html
<!-- ✅ ĐÚNG: Accessibility features -->
<nav aria-label="Main navigation">
    <ul role="menubar">
        <li role="none">
            <a href="/" role="menuitem" aria-current="page">Home</a>
        </li>
        <li role="none">
            <a href="/about" role="menuitem">About</a>
        </li>
    </ul>
</nav>

<main id="main-content" role="main" tabindex="-1">
    <h1>@ViewBag.Title</h1>
    @RenderBody()
</main>
```

### 6. ⚡ **LUÔN** optimize performance
```html
<!-- ✅ ĐÚNG: Performance optimization -->
<head>
    <!-- Critical CSS inline -->
    <style>
        /* Critical above-the-fold styles */
        body { font-family: system-ui, sans-serif; }
        .header { background: #fff; }
    </style>
    
    <!-- Non-critical CSS deferred -->
    <link rel="preload" href="~/css/theme.css" as="style" onload="this.onload=null;this.rel='stylesheet'">
    <noscript><link rel="stylesheet" href="~/css/theme.css"></noscript>
</head>

<body>
    <!-- Content -->
    
    <!-- JS at bottom -->
    <script asp-name="jQuery" at="Foot"></script>
    <script asp-name="theme-scripts" at="Foot"></script>
</body>
```

---

## 🔍 CHECKLIST CUỐI CÙNG

### ✅ **Trước khi deploy theme:**

- [ ] **Manifest.cs** có `[assembly: Theme]` attribute
- [ ] **Layout.cshtml** có đầy đủ `@RenderTitleSegments()` và `<resources>`
- [ ] **Menu** sử dụng Shape system, không hard-code
- [ ] **Resources** sử dụng `asp-name`, không hard-code paths
- [ ] **RTL support** với `@Orchard.IsRightToLeft()`
- [ ] **Accessibility** với skip links, ARIA labels, semantic HTML
- [ ] **Performance** với CSS ở Head, JS ở Foot
- [ ] **Responsive** design với mobile-first approach
- [ ] **Caching** cho menu và content blocks
- [ ] **Multi-language** support với culture-aware rendering

### 🎯 **Test checklist:**
- [ ] Theme hoạt động trên desktop/mobile
- [ ] Menu render đúng từ OrchardCore admin
- [ ] Resources load đúng (không 404)
- [ ] RTL languages hiển thị đúng
- [ ] Accessibility tools (screen readers) hoạt động
- [ ] Performance tốt (PageSpeed Insights)
- [ ] Cache invalidation hoạt động đúng

---

## 🚨 **LỖI THƯỜNG GẶP VÀ CÁCH SỬA**

### ❌ **Lỗi: Theme không xuất hiện trong admin**
```csharp
// ✅ SỬA: Đảm bảo có [assembly: Theme] trong Manifest.cs
[assembly: Theme(
    Name = "My Theme",
    Author = "Your Name",
    Version = "1.0.0",
    Description = "My custom theme"
)]
```

### ❌ **Lỗi: Menu không hiển thị**
```html
<!-- ✅ SỬA: Sử dụng đúng menu shape -->
<menu alias="alias:main-menu" cache-id="main-menu"></menu>
```

### ❌ **Lỗi: CSS/JS không load**
```html
<!-- ✅ SỬA: Sử dụng resource management -->
<style asp-name="bootstrap"></style>
<script asp-name="jQuery" at="Foot"></script>
```

### ❌ **Lỗi: Title không hiển thị đúng**
```html
<!-- ✅ SỬA: Sử dụng RenderTitleSegments -->
<title>@RenderTitleSegments(ViewBag.Title, Site.SiteName, " - ")</title>
```

---

*⚠️ **Lưu ý:** Luôn test theme trên nhiều devices và browsers khác nhau trước khi deploy production!*