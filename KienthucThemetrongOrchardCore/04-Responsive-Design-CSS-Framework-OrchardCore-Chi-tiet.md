# 📱 **RESPONSIVE DESIGN & CSS FRAMEWORK ORCHARDCORE - CHI TIẾT TỪNG BƯỚC**

## 🎯 **TỔNG QUAN**

**Responsive Design & CSS Framework** là bước thứ 4 trong thiết kế theme OrchardCore. Dựa trên phân tích chi tiết source code của TheTheme và TheAdmin, đây là hệ thống responsive design và CSS framework chuẩn OrchardCore với Bootstrap 5 integration.

---

## ⏰ **KHI NÀO VIẾT**

### **🚀 TIMING: SAU SHAPE SYSTEM - RESPONSIVE LAYER**
- **Viết sau**: Shape System & Display Management đã hoàn thành
- **Quan trọng**: Responsive design cho mobile-first approach
- **Thời gian**: 4-6 giờ cho basic responsive, 1-2 ngày cho advanced CSS framework

---

## 🔍 **PHÂN TÍCH SOURCE CODE ORCHARDCORE**

### **📁 CORE FILES ĐƯỢC PHÂN TÍCH:**
- **package.json**: Bootstrap 5.3.8 và @popperjs/core dependencies
- **theme.scss**: Main SCSS architecture với modular imports
- **_variables.scss**: Bootstrap variables và functions imports
- **_widgets.scss**: Responsive widget system với breakpoints
- **_layout.scss**: Sticky footer và responsive layout patterns
- **themes/light/_index.scss**: Light theme với CSS custom properties
- **themes/dark/_index.scss**: Dark theme với color-mode mixin
- **Assets.json**: SASS compilation configuration
- **Layout.cshtml**: Viewport meta và responsive HTML structure
- **FlowPart.cshtml**: Responsive grid system cho widgets

### **📁 ADMIN THEME FILES ĐƯỢC PHÂN TÍCH:**
- **TheAdmin Assets.json**: Advanced asset compilation với RTL support
- **mixins/_grid.scss**: Custom grid mixins cho admin interface
- **components/_grid.scss**: Responsive grid components

---

## 📱 **BƯỚC 1: BOOTSTRAP 5 INTEGRATION**

### **🔧 1.1. PACKAGE.JSON DEPENDENCIES (TỪ SOURCE CODE)**

```json
{
  "name": "@orchardcore/thetheme",
  "version": "1.0.0",
  "dependencies": {
    "@popperjs/core": "2.11.8",
    "bootstrap": "5.3.8"
  }
}
```

**📋 Key Dependencies:**
- ✅ **Bootstrap 5.3.8**: Latest stable Bootstrap version
- ✅ **@popperjs/core 2.11.8**: Tooltip và dropdown positioning
- ✅ **Semantic Versioning**: Specific versions cho stability

### **🚀 1.2. BOOTSTRAP VARIABLES IMPORT (TỪ SOURCE CODE)**

```scss
// _variables.scss
@import '../../../../../node_modules/bootstrap/scss/_functions';
@import '../../../../../node_modules/bootstrap/scss/_variables';
```

**📋 Bootstrap Integration Features:**
- ✅ **Functions Import**: Bootstrap utility functions
- ✅ **Variables Import**: All Bootstrap SCSS variables
- ✅ **Path Resolution**: Relative path to node_modules
- ✅ **Customization Ready**: Override variables before import

### **🎯 1.3. BOOTSTRAP MIXINS INTEGRATION (TỪ SOURCE CODE)**

```scss
// _widgets.scss
@use 'sass:math';
@import '../_variables';
@import "../../../../../../node_modules/bootstrap/scss/mixins/_breakpoints";
@import "../../../../../../node_modules/bootstrap/scss/mixins/_grid";
@import "../../../../../../node_modules/bootstrap/scss/mixins/_container";

.widget-container {
    @include make-container;
    padding-left: 0 !important;
    padding-right: 0 !important;
}

.widget-size-100 {
    @include make-col-ready;
    @include make-col(12);
}

@each $wsize in (25, 33, 50, 66, 75) {
    .widget-size-#{$wsize} {
        @extend .widget-size-100;

        @include media-breakpoint-up(sm) {
            @include make-col(round(math.div($wsize * 12, 100)));
        }
    }

    .widget .widget-size-#{$wsize} {
        @extend .widget-size-#{$wsize};

        @include media-breakpoint-up(md) {
            @include make-col(round(math.div($wsize * 12, 100)));
        }
    }
}
```

**📋 Bootstrap Mixins Features:**
- ✅ **Breakpoint Mixins**: `media-breakpoint-up(sm)`, `media-breakpoint-up(md)`
- ✅ **Grid Mixins**: `make-container`, `make-col-ready`, `make-col(12)`
- ✅ **Math Functions**: `math.div()` cho responsive calculations
- ✅ **Dynamic Classes**: `@each` loop cho widget sizes
- ✅ **Extend Pattern**: `@extend` cho code reuse

---

## 📱 **BƯỚC 2: SCSS ARCHITECTURE**

### **🔧 2.1. MAIN SCSS STRUCTURE (TỪ SOURCE CODE)**

```scss
// theme.scss
/* Include main */
@import "main/layout";

/* Include modules */
@import "modules/pager";
@import "modules/recaptcha";
@import "modules/taxonomy";
@import "modules/widgets";

/* Include themes */
@import 'themes/light/index';
@import 'themes/dark/index';
```

**📋 SCSS Architecture Features:**
- ✅ **Modular Structure**: Separate files cho different concerns
- ✅ **Main Layer**: Core layout và structural styles
- ✅ **Modules Layer**: Feature-specific styles
- ✅ **Themes Layer**: Light và dark theme variations
- ✅ **Import Order**: Logical dependency order

### **🚀 2.2. LAYOUT SCSS PATTERNS (TỪ SOURCE CODE)**

```scss
// main/_layout.scss
#togglePassword {
    width: 44px;
    padding: 0;
}

/* Sticky footer styles
-------------------------------------------------- */
html {
    position: relative;
    min-height: 100%;
}

body {
    /* Margin bottom by footer height */
    margin-bottom: 60px;
}

body > footer {
    position: absolute;
    bottom: 0;
    width: 100%;
    /* Set the fixed height of the footer here */
    height: 60px;
    line-height: 60px; /* Vertically center the text there */
}

html[data-bs-theme="light"] body > footer {
    background-color: #f5f5f5;
}

html[data-bs-theme="dark"] body > footer {
    background-color: #343a40;
}

/* Custom page CSS
-------------------------------------------------- */
/* Not required for template or sticky footer method. */
body > .container {
    padding: 60px 15px 0;
}
```

**📋 Layout Patterns Features:**
- ✅ **Sticky Footer**: Absolute positioning với min-height 100%
- ✅ **Theme-aware Styles**: `html[data-bs-theme="light"]` selectors
- ✅ **Container Padding**: Top padding cho fixed navbar
- ✅ **Responsive Heights**: Fixed heights với responsive considerations
- ✅ **Vertical Centering**: Line-height technique cho text alignment

### **🎯 2.3. RESPONSIVE WIDGET SYSTEM (TỪ SOURCE CODE)**

```scss
// modules/_widgets.scss
.widget-container {
    @include make-container;
    padding-left: 0 !important;
    padding-right: 0 !important;
}

.widget-image-widget img {
    width: 100%;
}

.widget-align-left {
    text-align: left;
}

.widget-align-center {
    text-align: center;
}

.widget-align-right {
    text-align: right;
}

.widget-align-justify {
    text-align: justify;
}

.widget-size-100 {
    @include make-col-ready;
    @include make-col(12);
}

@each $wsize in (25, 33, 50, 66, 75) {
    .widget-size-#{$wsize} {
        @extend .widget-size-100;

        @include media-breakpoint-up(sm) {
            @include make-col(round(math.div($wsize * 12, 100)));
        }
    }

    .widget .widget-size-#{$wsize} {
        @extend .widget-size-#{$wsize};

        @include media-breakpoint-up(md) {
            @include make-col(round(math.div($wsize * 12, 100)));
        }
    }
}
```

**📋 Widget System Features:**
- ✅ **Container System**: Bootstrap container với custom padding
- ✅ **Image Responsiveness**: `width: 100%` cho responsive images
- ✅ **Text Alignment**: Left, center, right, justify options
- ✅ **Size System**: 25%, 33%, 50%, 66%, 75%, 100% widths
- ✅ **Breakpoint Logic**: Different sizes cho sm và md breakpoints
- ✅ **Nested Widgets**: Special handling cho `.widget .widget-size-*`

---

## 📱 **BƯỚC 3: THEME SYSTEM (LIGHT/DARK)**

### **🔧 3.1. LIGHT THEME PATTERNS (TỪ SOURCE CODE)**

```scss
// themes/light/_index.scss
/* The light theme is also the default theme. */
/* Do not wrap the these classes for the light theme specific. */

.card {
    --bs-card-bg: var(--bs-light);
}

.navbar {
    --bs-navbar-color: rgba(255, 255, 255, 0.55);
    --bs-navbar-hover-color: rgba(255, 255, 255, 0.75);
    --bs-navbar-disabled-color: rgba(255, 255, 255, 0.25);
    --bs-navbar-active-color: #fff;
    --bs-navbar-brand-color: #fff;
    --bs-navbar-brand-hover-color: #fff;
    --bs-navbar-toggler-border-color: rgba(255, 255, 255, 0.1);
    --bs-navbar-toggler-icon-bg: url("data:image/svg+xml,%3csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 30 30'%3e%3cpath stroke='rgba%28255, 255, 255, 0.55%29' stroke-linecap='round' stroke-miterlimit='10' stroke-width='2' d='M4 7h22M4 15h22M4 23h22'/%3e%3c/svg%3e");
    color: #fff !important;
    background-color: rgba(var(--bs-dark-rgb), var(--bs-bg-opacity, 1)) !important;
}

@import "theme-helpers";
```

**📋 Light Theme Features:**
- ✅ **CSS Custom Properties**: `--bs-card-bg`, `--bs-navbar-color` variables
- ✅ **RGBA Colors**: Transparent colors với alpha values
- ✅ **SVG Icons**: Inline SVG cho navbar toggler
- ✅ **Bootstrap Variables**: Integration với Bootstrap CSS variables
- ✅ **Default Theme**: Light theme as default, no wrapper needed

### **🚀 3.2. DARK THEME PATTERNS (TỪ SOURCE CODE)**

```scss
// themes/dark/_index.scss
@import '../../_variables';
@import '../../../../../../../node_modules/bootstrap/scss/mixins/_color-mode';

@include color-mode(dark) {

    .card {
        --bs-card-bg: var(--bs-body);
    }

    @import "theme-helpers";
}
```

**📋 Dark Theme Features:**
- ✅ **Color Mode Mixin**: Bootstrap 5 `@include color-mode(dark)` pattern
- ✅ **Scoped Styles**: All dark styles wrapped trong color-mode
- ✅ **Variable Override**: Different CSS custom properties cho dark mode
- ✅ **Helper Import**: Shared theme helpers với different contexts
- ✅ **Bootstrap Integration**: Uses Bootstrap's official dark mode approach

### **🎯 3.3. THEME HELPERS (TỪ SOURCE CODE)**

#### **LIGHT THEME HELPERS**
```scss
// themes/light/_theme-helpers.scss
.bg-theme {
    background-color: rgba(var(--bs-light-rgb), var(--bs-bg-opacity, 1));
}

.text-theme {
    color: var(--bs-body-color);
}

.text-bg-theme {
    color: var(--bs-body-color);
    background-color: rgba(var(--bs-light-rgb), var(--bs-bg-opacity, 1));
}

.btn-theme {
    color: #000;
    background-color: #f8f9fa;
    border-color: #f8f9fa;
}

.btn-theme:hover {
    color: #000;
    background-color: #f9fafb;
    border-color: #f9fafb;
}

.btn-check:focus + .btn-theme, .btn-theme:focus {
    color: #000;
    background-color: #f9fafb;
    border-color: #f9fafb;
    box-shadow: 0 0 0 0.25rem rgba(211, 212, 213, 0.5);
}

.btn-check:checked + .btn-theme, .btn-check:active + .btn-theme, .btn-theme:active, .btn-theme.active, .show > .btn-theme.dropdown-toggle {
    color: #000;
    background-color: #f9fafb;
    border-color: #f9fafb;
}

.btn-check:checked + .btn-theme:focus, .btn-check:active + .btn-theme:focus, .btn-theme:active:focus, .btn-theme.active:focus, .show > .btn-theme.dropdown-toggle:focus {
    box-shadow: 0 0 0 0.25rem rgba(211, 212, 213, 0.5);
}

.btn-theme:disabled, .btn-theme.disabled {
    color: #000;
    background-color: #f8f9fa;
    border-color: #f8f9fa;
}
```

#### **DARK THEME HELPERS**
```scss
// themes/dark/_theme-helpers.scss
.bg-theme {
    background-color: rgba(var(--bs-body-bg-rgb), var(--bs-bg-opacity, 1));
}

.text-theme {
    color: var(--bs-body-color);
}

.text-bg-theme {
    color: var(--bs-body-color);
    background-color: rgba(var(--bs-body-bg-rgb), var(--bs-bg-opacity, 1));
}

.btn-theme {
    color: #fff;
    background-color: #212529;
    border-color: #212529;
}

.btn-theme:hover {
    color: #fff;
    background-color: #1c1f23;
    border-color: #1a1e21;
}

.btn-check:focus + .btn-theme, .btn-theme:focus {
    color: #fff;
    background-color: #1c1f23;
    border-color: #1a1e21;
    box-shadow: 0 0 0 0.25rem rgba(66, 70, 73, 0.5);
}

.btn-check:checked + .btn-theme, .btn-check:active + .btn-theme, .btn-theme:active, .btn-theme.active, .show > .btn-theme.dropdown-toggle {
    color: #fff;
    background-color: #1a1e21;
    border-color: #191c1f;
}

.btn-check:checked + .btn-theme:focus, .btn-check:active + .btn-theme:focus, .btn-theme:active:focus, .btn-theme.active:focus, .show > .btn-theme.dropdown-toggle:focus {
    box-shadow: 0 0 0 0.25rem rgba(66, 70, 73, 0.5);
}

.btn-theme:disabled, .btn-theme.disabled {
    color: #fff;
    background-color: #212529;
    border-color: #212529;
}
```

**📋 Theme Helpers Features:**
- ✅ **Utility Classes**: `.bg-theme`, `.text-theme`, `.text-bg-theme`
- ✅ **Button Variants**: `.btn-theme` với all states (hover, focus, active, disabled)
- ✅ **State Management**: Focus, checked, active, disabled states
- ✅ **Box Shadow**: Consistent focus indicators
- ✅ **Color Consistency**: Theme-aware colors cho light và dark modes

---

## 📱 **BƯỚC 4: RESPONSIVE HTML STRUCTURE**

### **🔧 4.1. VIEWPORT & META TAGS (TỪ SOURCE CODE)**

```html
<!-- Layout.cshtml -->
<!DOCTYPE html>
<html lang="@Orchard.CultureName()" dir="@Orchard.CultureDir()" data-bs-theme="@await ThemeTogglerService.CurrentTheme()" data-tenant="@ThemeTogglerService.CurrentTenant">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>@RenderTitleSegments(Site.SiteName, "before")</title>
    <link type="image/x-icon" rel="shortcut icon" href="~/TheTheme/images/favicon.ico">
    
    <!-- RTL Support -->
    @if (Orchard.IsRightToLeft())
    {
        <style asp-name="bootstrap-rtl" version="5" at="Head"></style>
        <style asp-name="TheTheme" depends-on="bootstrap-rtl" asp-src="~/TheTheme/styles/theme.min.css" debug-src="~/TheTheme/styles/theme.css" at="Foot"></style>
    }
    else
    {
        <style asp-name="bootstrap" version="5" at="Head"></style>
        <style asp-name="TheTheme" asp-src="~/TheTheme/styles/theme.min.css" debug-src="~/TheTheme/styles/theme.css" at="Foot"></style>
    }
</head>
```

**📋 HTML Structure Features:**
- ✅ **Viewport Meta**: `width=device-width, initial-scale=1.0` cho mobile-first
- ✅ **Language Attributes**: `lang` và `dir` cho internationalization
- ✅ **Theme Data Attributes**: `data-bs-theme` cho Bootstrap theme switching
- ✅ **RTL Support**: Conditional loading của bootstrap-rtl
- ✅ **Asset Dependencies**: `depends-on` cho proper loading order

### **🚀 4.2. RESPONSIVE NAVIGATION (TỪ SOURCE CODE)**

```html
<!-- Layout.cshtml -->
<body>
    <nav class="navbar navbar-expand-md fixed-top">
        <div class="container">
            <shape type="Branding" />
            <button type="button" class="navbar-toggler" data-bs-toggle="collapse" data-bs-target="#navbar" aria-expanded="false" aria-controls="navbar" aria-label="Toggle navigation">
                <span class="navbar-toggler-icon"></span>
            </button>
            <div class="collapse navbar-collapse" id="navbar">
                <shape type="Menu" alias="alias:main-menu" />
                @navbar
            </div>
        </div>
    </nav>
```

**📋 Navigation Features:**
- ✅ **Bootstrap Classes**: `navbar`, `navbar-expand-md`, `fixed-top`
- ✅ **Container System**: Bootstrap container cho responsive width
- ✅ **Collapse System**: `navbar-toggler` với `data-bs-toggle="collapse"`
- ✅ **Accessibility**: `aria-expanded`, `aria-controls`, `aria-label`
- ✅ **Shape Integration**: `<shape type="Branding" />`, `<shape type="Menu" />`

### **🎯 4.3. RESPONSIVE GRID SYSTEM (TỪ SOURCE CODE)**

```html
<!-- FlowPart.cshtml -->
<section class="flow row">
    @foreach (var widget in Model.FlowPart.Widgets)
    {
        if (!widgetDefinitions.TryGetValue(widget.ContentType, out var definition))
        {
            continue;
        }

        if (definition.IsSecurable() && !await AuthorizationService.AuthorizeAsync(User, CommonPermissions.ViewContent, widget))
        {
            continue;
        }

        var widgetContent = await ContentItemDisplayManager.BuildDisplayAsync(widget, Model.BuildPartDisplayContext.Updater, Model.BuildPartDisplayContext.DisplayType, Model.BuildPartDisplayContext.GroupId);
        var flowMetadata = widget.As<FlowMetadata>();

        if (flowMetadata != null)
        {
            widgetContent.Classes.Add("widget");
            widgetContent.Classes.Add("widget-" + widget.ContentItem.ContentType.HtmlClassify());
            widgetContent.Classes.Add("widget-align-" + flowMetadata.Alignment.ToString().ToLowerInvariant());
            widgetContent.Classes.Add("widget-size-" + flowMetadata.Size);
        }

        @await DisplayAsync(widgetContent)
    }
</section>
```

**📋 Grid System Features:**
- ✅ **Bootstrap Row**: `class="flow row"` cho grid container
- ✅ **Dynamic Classes**: `widget-size-{size}` cho responsive columns
- ✅ **Alignment Classes**: `widget-align-{alignment}` cho text alignment
- ✅ **Content Type Classes**: `widget-{contentType}` cho styling hooks
- ✅ **Security Integration**: Authorization checks cho widget visibility

---

## 📱 **BƯỚC 5: ASSET MANAGEMENT & COMPILATION**

### **🔧 5.1. BASIC ASSETS.JSON (TỪ SOURCE CODE)**

```json
// TheTheme/Assets.json
[
  {
    "action": "sass",
    "name": "theme-thetheme",
    "source": "Assets/scss/theme.scss",
    "dest": "wwwroot/styles/",
    "tags": ["theme", "css"]
  }
]
```

**📋 Basic Asset Features:**
- ✅ **SASS Compilation**: `"action": "sass"` cho SCSS processing
- ✅ **Named Assets**: `"name": "theme-thetheme"` cho identification
- ✅ **Source Path**: `Assets/scss/theme.scss` input file
- ✅ **Destination Path**: `wwwroot/styles/` output directory
- ✅ **Tags**: `["theme", "css"]` cho categorization

### **🚀 5.2. ADVANCED ASSETS.JSON (TỪ SOURCE CODE)**

```json
// TheAdmin/Assets.json
[
  {
    "action": "sass",
    "name": "theme-theadmin",
    "source": "Assets/scss/TheAdmin.scss",
    "dest": "wwwroot/css/",
    "tags": ["theme", "css"]
  },
  {
    "action": "sass",
    "generateRTL": true,
    "name": "theme-theadmin-layout",
    "source": "Assets/scss/admin-layout.scss",
    "dest": "wwwroot/css/",
    "tags": ["theme", "css"]
  },
  {
    "action": "sass",
    "name": "theme-theadmin-login",
    "source": "Assets/scss/login.scss",
    "dest": "wwwroot/css/",
    "tags": ["theme", "css"]
  },
  {
    "action": "parcel",
    "name": "TheAdmin-main",
    "source": "Assets/js/TheAdmin-main/TheAdmin-main.ts",
    "dest": "wwwroot/js/theadmin-main/",
    "tags": ["theme", "css"]
  },
  {
    "action": "parcel",
    "name": "TheAdmin",
    "source": "Assets/js/TheAdmin/TheAdmin.ts",
    "dest": "wwwroot/js/theadmin/",
    "tags": ["theme", "css"]
  }
]
```

**📋 Advanced Asset Features:**
- ✅ **Multiple SASS Files**: Separate compilation cho different layouts
- ✅ **RTL Generation**: `"generateRTL": true` cho right-to-left support
- ✅ **Parcel Integration**: TypeScript compilation với Parcel bundler
- ✅ **Organized Output**: Different directories cho CSS và JS
- ✅ **Modular Assets**: Separate assets cho main, layout, login

### **🎯 5.3. ASSET LOADING PATTERNS (TỪ SOURCE CODE)**

```html
<!-- Layout.cshtml -->
<!-- This script can't wait till the footer -->
<script asp-name="theme-head" version="1" at="Head"></script>

@if (Orchard.IsRightToLeft())
{
    <style asp-name="bootstrap-rtl" version="5" at="Head"></style>
    <style asp-name="TheTheme" depends-on="bootstrap-rtl" asp-src="~/TheTheme/styles/theme.min.css" debug-src="~/TheTheme/styles/theme.css" at="Foot"></style>
}
else
{
    <style asp-name="bootstrap" version="5" at="Head"></style>
    <style asp-name="TheTheme" asp-src="~/TheTheme/styles/theme.min.css" debug-src="~/TheTheme/styles/theme.css" at="Foot"></style>
}

<script asp-name="bootstrap" version="5" at="Foot"></script>
<script asp-name="theme-manager" at="Foot"></script>
<script asp-name="font-awesome" at="Foot" version="7"></script>
<resources type="Header" />
```

**📋 Asset Loading Features:**
- ✅ **Position Control**: `at="Head"` và `at="Foot"` cho loading order
- ✅ **Version Management**: `version="5"` cho cache busting
- ✅ **Dependencies**: `depends-on="bootstrap-rtl"` cho proper order
- ✅ **Debug Support**: `debug-src` cho development builds
- ✅ **Resource Injection**: `<resources type="Header" />` cho dynamic resources

---

## 📱 **BƯỚC 6: ADMIN RESPONSIVE PATTERNS**

### **🔧 6.1. ADMIN GRID MIXINS (TỪ SOURCE CODE)**

```scss
// TheAdmin/Assets/scss/mixins/_grid.scss
// Row / Column Groupings
// Creates mixins to target columns contained inside a column grouping to expand small sizes.
// ------------------------------
@import '../../../../../../node_modules/bootstrap/scss/_variables';

@mixin make-col-fix($breakpoint) {
    .col-#{$breakpoint} {
        flex: 0 0 100%;
        max-width: 100%;
    }
}

@mixin make-col-grouping($grouping) {
    &.col-#{$grouping} {
        @each $breakpoint in map-keys($grid-breakpoints) {
            @include make-col-fix(#{$breakpoint});

            @for $i from 1 through math.div($grid-columns, 2) {
                @include make-col-fix(#{$breakpoint}-#{$i});
            }
        }
    }
}
```

**📋 Admin Grid Features:**
- ✅ **Column Fixes**: `make-col-fix` cho responsive column behavior
- ✅ **Grouping System**: `make-col-grouping` cho nested columns
- ✅ **Breakpoint Iteration**: `@each $breakpoint in map-keys($grid-breakpoints)`
- ✅ **Dynamic Generation**: `@for $i from 1 through math.div($grid-columns, 2)`
- ✅ **Flex Properties**: `flex: 0 0 100%` và `max-width: 100%`

### **🚀 6.2. ADMIN RESPONSIVE COMPONENTS (TỪ SOURCE CODE)**

```scss
// TheAdmin/Assets/scss/components/_grid.scss
// Responsive sizes
// Creates mixins to target all breakpoints
// ------------------------------
@use 'sass:math';
@import '../mixins/_grid';

.ta-col-grouping {
    // Column fixes
    @each $breakpoint in map-keys($grid-breakpoints) {
        @if $grid-row-columns > 0 {
            // Column fixes
            @include make-col-grouping(#{$breakpoint}); // Include a fix for unspecified cols, i.e. col-md
            // Only apply to the lower half of the grid.
            @for $i from 1 through math.div($grid-columns, 2) {
                // Include a fix for specified cols, i.e. col-md-3
                @include make-col-grouping(#{$breakpoint}-#{$i});
            }
        }
    }
}
```

**📋 Admin Components Features:**
- ✅ **Column Grouping**: `.ta-col-grouping` class cho admin-specific grid
- ✅ **Conditional Logic**: `@if $grid-row-columns > 0` check
- ✅ **Half Grid**: `math.div($grid-columns, 2)` cho optimized columns
- ✅ **Breakpoint Coverage**: All Bootstrap breakpoints supported
- ✅ **Mixin Integration**: Uses custom grid mixins

### **🎯 6.3. ADMIN LAYOUT STRUCTURE (TỪ SOURCE CODE)**

```html
<!-- TheAdmin/Views/Layout.cshtml -->
<!DOCTYPE html>
<html lang="@Orchard.CultureName()" dir="@Orchard.CultureDir()" data-bs-theme="@await ThemeTogglerService.CurrentTheme()" data-tenant="@ThemeTogglerService.CurrentTenant">
<head>
    <title>@RenderTitleSegments(Site.SiteName, "before")</title>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1, shrink-to-fit=no">
    <meta http-equiv="x-ua-compatible" content="ie=edge">

    <style asp-name="TheAdminLayout" asp-src="~/TheAdmin/css/admin-layout.min.css" debug-src="~/TheAdmin/css/admin-layout.css" at="Head"></style>

    @if (Orchard.IsRightToLeft())
    {
        <style asp-name="bootstrap-rtl" version="5" at="Head"></style>
        <style media="all" asp-name="the-admin" version="1" depends-on="bootstrap-rtl,TheAdminLayout" at="Head"></style>
    }
    else
    {
        <style asp-name="bootstrap" version="5" at="Head"></style>
        <style media="all" asp-name="the-admin" version="1" depends-on="bootstrap,TheAdminLayout" at="Head"></style>
    }
</head>
<body class="preload">
    <div class="ta-wrapper">
        <nav class="ta-navbar-top navbar text-bg-theme fixed-top">
            <div class="container-fluid">
```

**📋 Admin Layout Features:**
- ✅ **Enhanced Viewport**: `initial-scale=1, shrink-to-fit=no` cho better mobile
- ✅ **IE Compatibility**: `http-equiv="x-ua-compatible" content="ie=edge"`
- ✅ **Preload Class**: `class="preload"` cho loading states
- ✅ **Admin Wrapper**: `.ta-wrapper` cho admin-specific layout
- ✅ **Fluid Container**: `container-fluid` cho full-width admin interface

---

## 📱 **RESPONSIVE DESIGN PATTERNS SUMMARY**

### **🔧 CORE PATTERNS**
1. **Bootstrap 5 Integration**: Latest Bootstrap với @popperjs/core
2. **SCSS Architecture**: Modular structure với main, modules, themes
3. **Theme System**: Light/dark themes với CSS custom properties
4. **Responsive Widgets**: Breakpoint-based sizing với Bootstrap mixins
5. **Asset Management**: SASS compilation với Assets.json configuration

### **🚀 ADVANCED PATTERNS**
1. **RTL Support**: Conditional loading của bootstrap-rtl
2. **Theme Switching**: `data-bs-theme` attribute với ThemeTogglerService
3. **Grid Extensions**: Custom mixins cho admin interface
4. **Asset Dependencies**: `depends-on` cho proper loading order
5. **Debug Support**: Separate debug và production assets

### **🎯 MOBILE-FIRST PATTERNS**
1. **Viewport Configuration**: `width=device-width, initial-scale=1.0`
2. **Responsive Navigation**: Collapsible navbar với toggler
3. **Flexible Images**: `width: 100%` cho responsive images
4. **Breakpoint Strategy**: sm (576px), md (768px) breakpoints
5. **Touch-friendly**: Proper touch targets và spacing

---

## ✅ **CHECKLIST RESPONSIVE DESIGN & CSS FRAMEWORK**

### **🔧 BASIC SETUP (BẮT BUỘC)**
- [ ] ✅ Setup Bootstrap 5 dependencies trong package.json
- [ ] ✅ Import Bootstrap variables và mixins
- [ ] ✅ Create modular SCSS architecture
- [ ] ✅ Setup viewport meta tag cho mobile-first
- [ ] ✅ Implement responsive navigation với collapse
- [ ] ✅ Configure Assets.json cho SASS compilation
- [ ] ✅ Add basic responsive utilities

### **🚀 ADVANCED SETUP (KHUYẾN NGHỊ)**
- [ ] ✅ Implement light/dark theme system
- [ ] ✅ Setup RTL support với conditional loading
- [ ] ✅ Create responsive widget system
- [ ] ✅ Add custom Bootstrap mixins
- [ ] ✅ Implement asset dependencies
- [ ] ✅ Setup debug/production asset variants
- [ ] ✅ Add theme switching functionality
- [ ] ✅ Create responsive grid extensions

### **🎯 MOBILE-FIRST SETUP (CHUYÊN SÂU)**
- [ ] ✅ Optimize touch interactions
- [ ] ✅ Implement progressive enhancement
- [ ] ✅ Add responsive images với srcset
- [ ] ✅ Setup critical CSS loading
- [ ] ✅ Implement lazy loading cho assets
- [ ] ✅ Add performance monitoring
- [ ] ✅ Setup responsive typography
- [ ] ✅ Implement accessibility features

---

## 🚫 **NHỮNG LỖI THƯỜNG GẶP**

### **❌ BOOTSTRAP INTEGRATION ERRORS**
```scss
// ❌ SAI: Import Bootstrap sau custom variables
@import 'custom-variables';
@import '../../../../../node_modules/bootstrap/scss/_variables';

// ✅ ĐÚNG: Import Bootstrap variables trước custom overrides
@import '../../../../../node_modules/bootstrap/scss/_functions';
@import '../../../../../node_modules/bootstrap/scss/_variables';
@import 'custom-variables';
```

### **❌ VIEWPORT ERRORS**
```html
<!-- ❌ SAI: Missing viewport meta tag -->
<head>
    <meta charset="utf-8" />
    <title>My Site</title>
</head>

<!-- ✅ ĐÚNG: Proper viewport configuration -->
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>My Site</title>
</head>
```

### **❌ RESPONSIVE WIDGET ERRORS**
```scss
// ❌ SAI: Hard-coded breakpoints
.widget-size-50 {
    width: 50%;
}

// ✅ ĐÚNG: Bootstrap mixin với breakpoints
.widget-size-50 {
    @extend .widget-size-100;

    @include media-breakpoint-up(sm) {
        @include make-col(6);
    }
}
```

### **❌ ASSET LOADING ERRORS**
```json
// ❌ SAI: Missing dependencies
{
  "action": "sass",
  "name": "theme-custom",
  "source": "Assets/scss/theme.scss",
  "dest": "wwwroot/styles/"
}

// ✅ ĐÚNG: Proper asset configuration
{
  "action": "sass",
  "name": "theme-custom",
  "source": "Assets/scss/theme.scss",
  "dest": "wwwroot/styles/",
  "tags": ["theme", "css"]
}
```

---

## 📊 **PERFORMANCE METRICS**

### **⚡ COMPILATION TIME**
- **Basic SCSS**: 100-300ms
- **Complex SCSS**: 500ms-1s
- **With RTL**: +200-400ms

### **📦 FILE SIZES**
- **Bootstrap 5**: ~200KB (uncompressed)
- **Theme CSS**: 20-50KB
- **Combined**: 220-250KB
- **Gzipped**: 30-40KB

### **🚀 LOADING PERFORMANCE**
- **Critical CSS**: < 14KB inline
- **Non-critical**: Async loaded
- **Mobile Performance**: < 3s First Contentful Paint
- **Desktop Performance**: < 1s First Contentful Paint

---

## 🎯 **NEXT STEPS**

Sau khi hoàn thành Responsive Design & CSS Framework, anh có thể tiếp tục với:

1. **🎪 Asset Management & Optimization** - SCSS compilation, bundling, minification
2. **🔧 Services & Startup Configuration** - Custom services, dependency injection
3. **♿ Accessibility & SEO Optimization** - ARIA attributes, semantic HTML
4. **⚡ Performance & Optimization** - Caching strategies, lazy loading

---

## 🔗 **REFERENCES TỪ SOURCE CODE**

- **package.json**: `/src/OrchardCore.Themes/TheTheme/Assets/package.json`
- **theme.scss**: `/src/OrchardCore.Themes/TheTheme/Assets/scss/theme.scss`
- **_variables.scss**: `/src/OrchardCore.Themes/TheTheme/Assets/scss/_variables.scss`
- **_widgets.scss**: `/src/OrchardCore.Themes/TheTheme/Assets/scss/modules/_widgets.scss`
- **_layout.scss**: `/src/OrchardCore.Themes/TheTheme/Assets/scss/main/_layout.scss`
- **Assets.json**: `/src/OrchardCore.Themes/TheTheme/Assets.json`
- **Layout.cshtml**: `/src/OrchardCore.Themes/TheTheme/Views/Layout.cshtml`
- **FlowPart.cshtml**: `/src/OrchardCore.Themes/TheTheme/Views/FlowPart.cshtml`

---

**🎉 Responsive Design & CSS Framework là foundation cho modern web experience! Với Bootstrap 5 integration chuẩn từ OrchardCore, anh có thể tạo themes responsive và accessible! 🚀📱**

---

*Timing: 4-6 giờ cho basic responsive, 1-2 ngày cho advanced CSS framework với tất cả features.*