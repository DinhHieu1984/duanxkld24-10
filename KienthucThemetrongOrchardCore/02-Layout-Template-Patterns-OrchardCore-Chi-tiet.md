# 🖼️ **LAYOUT & TEMPLATE PATTERNS ORCHARDCORE - CHI TIẾT TỪNG BƯỚC**

## 🎯 **TỔNG QUAN**

**Layout & Template Patterns** là bước thứ 2 trong thiết kế theme OrchardCore. Dựa trên phân tích chi tiết source code của TheTheme, TheAdmin, và SafeMode, đây là hệ thống template và layout patterns chuẩn OrchardCore.

---

## ⏰ **KHI NÀO VIẾT**

### **🚀 TIMING: SAU THEME FOUNDATION - LAYOUT LAYER**
- **Viết sau**: Theme Foundation đã hoàn thành
- **Quan trọng**: Định nghĩa cấu trúc hiển thị của theme
- **Thời gian**: 4-6 giờ cho basic layout, 1-2 ngày cho advanced patterns

---

## 🔍 **PHÂN TÍCH SOURCE CODE ORCHARDCORE**

### **📁 LAYOUTS ĐƯỢC PHÂN TÍCH:**
- **TheTheme/Layout.cshtml**: Frontend theme với Bootstrap và theme toggle
- **TheAdmin/Layout.cshtml**: Admin theme với sidebar navigation
- **SafeMode/Layout.cshtml**: Minimal fail-safe layout

### **📁 TEMPLATES ĐƯỢC PHÂN TÍCH:**
- **Shape Templates**: Branding, Menu, MenuItem, MenuItemLink variations
- **Advanced Templates**: ToggleTheme, NavbarUserMenu, UserNotificationNavbar
- **Widget Templates**: Widget-Form, Widget-Input, Widget-Select
- **Utility Templates**: Message, Pager, FlowPart

---

## 📄 **BƯỚC 1: LAYOUT.CSHTML PATTERNS**

### **🔧 1.1. BASIC LAYOUT (THEO SAFEMODE)**

```html
<!DOCTYPE html>
<html lang="@Orchard.CultureName()">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>@ViewBag.Title - Orchard Core</title>
</head>
<body>
    <h1>@T["SAFE MODE"]</h1>
    @await RenderSectionAsync("Header", required: false)
    @await RenderBodyAsync()

    <hr />
    <footer>
        @await RenderSectionAsync("Footer", required: false)
    </footer>
</body>
</html>
```

**📋 Key Features:**
- ✅ `@Orchard.CultureName()` cho language support
- ✅ `@T["Text"]` cho localization
- ✅ `@await RenderSectionAsync()` cho section-based architecture
- ✅ `@await RenderBodyAsync()` cho main content

### **🚀 1.2. FRONTEND LAYOUT (THEO THETHEME)**

```html
@using OrchardCore.Admin.Models
@using OrchardCore.DisplayManagement
@using OrchardCore.DisplayManagement.ModelBinding
@using OrchardCore.Entities
@using OrchardCore.Themes.Services
@using OrchardCore.Users.Models

@inject IDisplayManager<Navbar> DisplayManager
@inject IUpdateModelAccessor UpdateModelAccessor
@inject ThemeTogglerService ThemeTogglerService
@{
    // Navbar is pre-rendered to allow resource injection.
    var navbar = await DisplayAsync(await DisplayManager.BuildDisplayAsync(UpdateModelAccessor.ModelUpdater));
}
<!DOCTYPE html>
<html lang="@Orchard.CultureName()" dir="@Orchard.CultureDir()" 
      data-bs-theme="@await ThemeTogglerService.CurrentTheme()" 
      data-tenant="@ThemeTogglerService.CurrentTenant">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>@RenderTitleSegments(Site.SiteName, "before")</title>
    <link type="image/x-icon" rel="shortcut icon" href="~/TheTheme/images/favicon.ico">

    <!-- This script can't wait till the footer -->
    <script asp-name="theme-head" version="1" at="Head"></script>

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

    <script asp-name="bootstrap" version="5" at="Foot"></script>
    <script asp-name="theme-manager" at="Foot"></script>
    <script asp-name="font-awesome" at="Foot" version="7"></script>
    
    <resources type="Header" />
    @await RenderSectionAsync("HeadMeta", required: false)
</head>
<body>
    <nav class="navbar navbar-expand-md fixed-top">
        <div class="container">
            <shape type="Branding" />
            <button type="button" class="navbar-toggler" data-bs-toggle="collapse" 
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
    <resources type="FootScript" />
</body>
</html>
```

**📋 Advanced Features:**
- ✅ **Pre-render navbar**: `var navbar = await DisplayAsync()` để inject resources
- ✅ **Theme toggle**: `ThemeTogglerService.CurrentTheme()` cho dark/light mode
- ✅ **RTL support**: `@Orchard.IsRightToLeft()` với bootstrap-rtl
- ✅ **Resource management**: `asp-name`, `depends-on`, `at="Head|Foot"`
- ✅ **Caching**: `cache-id`, `cache-fixed-duration`, `cache-context`
- ✅ **Shape system**: `<shape type="Branding" />`
- ✅ **Menu system**: `<menu alias="alias:main-menu" />`

### **🏢 1.3. ADMIN LAYOUT (THEO THEADMIN)**

```html
@using OrchardCore.DisplayManagement
@using OrchardCore.DisplayManagement.ModelBinding
@using OrchardCore.Environment.Shell
@using OrchardCore.Themes.Services
@using OrchardCore.Users.Models

@inject ThemeTogglerService ThemeTogglerService
@inject IDisplayManager<Navbar> DisplayManager
@inject IUpdateModelAccessor UpdateModelAccessor
@{
    var adminSettings = Site.As<AdminSettings>();

    // Branding and Navbar are pre-rendered to allow resource injection.
    var brandingHtml = await DisplayAsync(await New.AdminBranding());
    var navbar = await DisplayAsync(await DisplayManager.BuildDisplayAsync(UpdateModelAccessor.ModelUpdater, "DetailAdmin"));
}
<!DOCTYPE html>
<html lang="@Orchard.CultureName()" dir="@Orchard.CultureDir()" 
      data-bs-theme="@await ThemeTogglerService.CurrentTheme()" 
      data-tenant="@ThemeTogglerService.CurrentTenant">
<head>
    <title>@RenderTitleSegments(Site.SiteName, "before")</title>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1, shrink-to-fit=no">
    <meta http-equiv="x-ua-compatible" content="ie=edge">

    <!-- This script can't wait till the footer -->
    <script asp-name="admin-main" version="1" at="Head"></script>

    <style asp-name="TheAdminLayout" 
           asp-src="~/TheAdmin/css/admin-layout.min.css" 
           debug-src="~/TheAdmin/css/admin-layout.css" at="Head"></style>

    @if (Orchard.IsRightToLeft())
    {
        <style asp-name="bootstrap-rtl" version="5" at="Head"></style>
        <style media="all" asp-name="the-admin" version="1" 
               depends-on="bootstrap-rtl,TheAdminLayout" at="Head"></style>
    }
    else
    {
        <style asp-name="bootstrap" version="5" at="Head"></style>
        <style media="all" asp-name="the-admin" version="1" 
               depends-on="bootstrap,TheAdminLayout" at="Head"></style>
    }
    
    <script asp-name="font-awesome" at="Foot" version="7"></script>
    <script asp-name="the-admin" version="1" at="Foot"></script>

    <resources type="Header" />
    @await RenderSectionAsync("HeadMeta", required: false)
</head>
<body class="preload">
    <div class="ta-wrapper">
        <nav class="ta-navbar-top navbar text-bg-theme fixed-top">
            <div class="container-fluid">
                <div class="d-flex w-100 justify-content-between align-items-center">
                    <div class="brand-wrapper">
                        <div class="d-flex w-100 justify-content-between align-items-center">
                            @brandingHtml

                            @if (adminSettings.DisplayTitlesInTopbar)
                            {
                                <div class="brand-wrapper-title">
                                    @await RenderSectionAsync("Title", required: false)
                                </div>
                            }
                        </div>
                    </div>
                    @navbar
                </div>
            </div>
        </nav>

        <div id="ta-left-sidebar" class="d-flex flex-column justify-content-between align-items-stretch">
            @await RenderSectionAsync("Navigation", required: false)
            <div class="footer">
                <button type="button" class="leftbar-compactor" title="@T["Collapse / expand menu"]">
                    <span class="d-none">@T["Collapse / expand menu"]</span>
                </button>
            </div>
        </div>

        <div class="ta-content">
            @await RenderSectionAsync("Header", required: false)
            @await RenderSectionAsync("Messages", required: false)
            @await RenderSectionAsync("Breadcrumbs", required: false)
            @if (!adminSettings.DisplayTitlesInTopbar)
            {
                @await RenderSectionAsync("Title", required: false)
            }
            @await RenderBodyAsync()
        </div>

        @await RenderSectionAsync("Footer", required: false)

    </div>
    <div id="confirmRemoveModalMetadata" 
         data-title="@T["Delete"]" 
         data-message="@T["Are you sure you want to remove this element?"]" 
         data-ok-text="@T["Ok"]" 
         data-cancel-text="@T["Cancel"]" 
         data-ok-class="btn-danger" 
         data-cancel-class="btn-secondary"></div>
    <resources type="Footer" />
</body>
</html>
```

**📋 Admin-specific Features:**
- ✅ **AdminSettings integration**: `Site.As<AdminSettings>()`
- ✅ **Admin branding**: `await New.AdminBranding()`
- ✅ **Sidebar navigation**: `ta-left-sidebar` với collapsible menu
- ✅ **Admin sections**: Navigation, Breadcrumbs, Title positioning
- ✅ **Modal metadata**: Confirmation dialogs với localized text

---

## 📄 **BƯỚC 2: _VIEWIMPORTS.CSHTML PATTERNS**

### **🔧 2.1. BASIC VIEWIMPORTS (THEO SAFEMODE)**

```csharp
@inherits OrchardCore.DisplayManagement.Razor.RazorPage<TModel>
@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers
@addTagHelper *, OrchardCore.DisplayManagement
@addTagHelper *, OrchardCore.ResourceManagement
```

### **🚀 2.2. ADVANCED VIEWIMPORTS (THEO THETHEME)**

```csharp
@inherits OrchardCore.DisplayManagement.Razor.RazorPage<TModel>
@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers
@addTagHelper *, OrchardCore.DisplayManagement
@addTagHelper *, OrchardCore.ResourceManagement
@addTagHelper *, OrchardCore.Menu
@using Microsoft.AspNetCore.Routing
@using Microsoft.Extensions.Options
@using OrchardCore.ContentManagement
@using OrchardCore.ContentManagement.Routing
@using OrchardCore.Menu.Models
```

**📋 Key Components:**
- ✅ **Base class**: `OrchardCore.DisplayManagement.Razor.RazorPage<TModel>`
- ✅ **TagHelpers**: MVC, DisplayManagement, ResourceManagement, Menu
- ✅ **Using statements**: OrchardCore namespaces cho content và routing

---

## 🎭 **BƯỚC 3: SHAPE TEMPLATES**

### **🔧 3.1. BRANDING.CSHTML (SIMPLE)**

```html
<a class="navbar-brand" href="~/#">@Site.SiteName</a>
```

### **🚀 3.2. MENU.CSHTML (TAGBUILDER PATTERN)**

```csharp
@{
    // By using a tag builder, sub items can alter the
    // menu element. Note how Display is called on the menu
    // items before the tag is actually rendered.

    TagBuilder tag = Tag(Model, "ul");
    tag.AddCssClass("navbar-nav");

    foreach (var item in Model.Items)
    {
        tag.InnerHtml.AppendHtml(await DisplayAsync(item));
    }
}

@tag
```

**📋 TagBuilder Pattern:**
- ✅ `TagBuilder tag = Tag(Model, "ul")` tạo HTML element
- ✅ `tag.AddCssClass()` thêm CSS classes
- ✅ `tag.InnerHtml.AppendHtml()` thêm content
- ✅ `@tag` render final HTML

### **🎯 3.3. MENUITEM.CSHTML (COMPLEX DROPDOWN)**

```csharp
@{
    int level = (int)Model.Level;
    TagBuilder tag = Tag(Model, "li");
    tag.AddCssClass("nav-item");

    TagBuilder parentTag = null;
    if ((bool)Model.HasItems)
    {
        tag.AddCssClass("dropdown");
        parentTag = Tag(Model, "ul");
        parentTag.AddCssClass("dropdown-menu");
    }

    // Morphing the shape to keep Model untouched
    Model.Metadata.Alternates.Clear();
    Model.Classes.Clear();
    Model.Metadata.Type = "MenuItemLink";

    @if ((int)Model.Level > 0)
    {
        Model.Classes.Add("dropdown-item");
    }
    else
    {
        Model.Classes.Add("nav-link");
        if (Model.HasItems)
        {
            Model.Classes.Add("dropdown-toggle");
            Model.Attributes.Add("data-bs-toggle", "dropdown");
        }
    }

    tag.InnerHtml.AppendHtml(await DisplayAsync(Model));

    if (parentTag != null)
    {
        foreach (var item in Model.Items)
        {
            item.Level = level + 1;
            item.ParentTag = parentTag;
            parentTag.InnerHtml.AppendHtml(await DisplayAsync(item));
        }

        tag.InnerHtml.AppendHtml(parentTag);
    }
}

@tag
```

**📋 Advanced Features:**
- ✅ **Level-based styling**: Different classes cho different levels
- ✅ **Dropdown logic**: Bootstrap dropdown classes và attributes
- ✅ **Shape morphing**: `Model.Metadata.Type = "MenuItemLink"`
- ✅ **Recursive rendering**: Nested menu items với level tracking

### **🎯 3.4. MENUITEMLINK VARIATIONS**

#### **CONTENTMENUITEM.CSHTML**
```csharp
@using Microsoft.AspNetCore.Authorization
@using OrchardCore.Contents

@inject IContentManager ContentManager
@inject IAuthorizationService AuthorizationService
@inject IOptions<AutorouteOptions> AutorouteOptions

@{
    ContentItem contentItem = Model.ContentItem;
    var menuItemPart = contentItem.As<ContentMenuItemPart>();
    string contentItemId = menuItemPart.ContentItem.Content.ContentMenuItemPart.SelectedContentItem.ContentItemIds[0];

    var routeValues = new RouteValueDictionary(AutorouteOptions.Value.GlobalRouteValues);
    routeValues[AutorouteOptions.Value.ContentItemIdKey] = menuItemPart.ContentItem.Content.ContentMenuItemPart.SelectedContentItem.ContentItemIds[0];

    var linkUrl = Url.RouteUrl(routeValues);

    TagBuilder tag = Tag(Model, "a");
    tag.Attributes["href"] = linkUrl;
    tag.InnerHtml.Append(contentItem.DisplayText);

    if (Model.Level == 0 && Model.HasItems)
    {
        tag.InnerHtml.AppendHtml("<b class=\"caret\"></b>");
    }
}

@tag
```

#### **LINKMENUITEM.CSHTML**
```csharp
@using OrchardCore.ContentManagement
@{
    ContentItem contentItem = Model.ContentItem;
    var linkMenuItemPart = contentItem.As<LinkMenuItemPart>();

    TagBuilder tag = Tag(Model, "a");

    var url = linkMenuItemPart.Url;
    if (url != null)
    {
        if (url.StartsWith('/'))
        {
            url = '~' + url;
        }

        if (url.StartsWith("~/", StringComparison.Ordinal))
        {
            url = Url.Content(linkMenuItemPart.Url);
        }
    }

    tag.Attributes["href"] = url;
    tag.InnerHtml.Append(contentItem.DisplayText);

    if (Model.Level == 0 && Model.HasItems)
    {
        tag.InnerHtml.AppendHtml("<b class=\"caret\"></b>");
    }
}
@tag
```

---

## 🎨 **BƯỚC 4: ADVANCED TEMPLATES**

### **🌓 4.1. TOGGLETHEME.CSHTML (DARK/LIGHT MODE)**

```html
<li class="nav-item dropdown text-end">
    <a role="button" class="nav-link dropdown-toggle" id="bd-theme" 
       aria-expanded="false" data-bs-toggle="dropdown" data-bs-display="static" 
       aria-label="@T["Toggle theme"]">
        <span class="theme-icon-active">
            <i class="fa-solid fa-circle-half-stroke"></i>
        </span>
        <span class="d-none" id="bd-theme-text">@T["Toggle theme"]</span>
    </a>
    <ul class="dropdown-menu dropdown-menu-end" aria-labelledby="bd-theme-text">
        <li>
            <button type="button" class="dropdown-item" data-bs-theme-value="auto" 
                    aria-pressed="false">
                <span class="theme-icon">
                    <i class="fa-solid fa-circle-half-stroke"></i>
                </span>
                <span class="ps-2">@T["Auto"]</span>
            </button>
        </li>
        <li>
            <button type="button" class="dropdown-item active" data-bs-theme-value="light" 
                    aria-pressed="true">
                <span class="theme-icon">
                    <i class="fa-solid fa-sun"></i>
                </span>
                <span class="ps-2">@T["Light"]</span>
            </button>
        </li>
        <li>
            <button type="button" class="dropdown-item" data-bs-theme-value="dark" 
                    aria-pressed="false">
                <span class="theme-icon">
                    <i class="fa-solid fa-moon"></i>
                </span>
                <span class="ps-2">@T["Dark"]</span>
            </button>
        </li>
        @await RenderSectionAsync("ToggleThemeItems", required: false)
    </ul>
</li>
```

### **🔔 4.2. USERNOTIFICATIONNAVBAR.CSHTML (NOTIFICATION SYSTEM)**

```html
@using Microsoft.AspNetCore.Authorization
@using OrchardCore.Notifications
@using OrchardCore.Notifications.ViewModels

@inject IDisplayManager<Notification> NotificationDisplayDriver
@inject IUpdateModelAccessor UpdateModelAccessor
@inject IAuthorizationService AuthorizationService

@model UserNotificationNavbarViewModel

<li class="nav-item dropdown text-end">
    <a type="button" class="nav-link dropdown-toggle" href="#" role="button" 
       data-bs-toggle="dropdown" data-bs-auto-close="outside" 
       aria-expanded="false" aria-label="@T["Notifications"]">
        @if (Model.TotalUnread > 0)
        {
            LocalizedHtmlString title;
            if (Model.TotalUnread > Model.MaxVisibleNotifications)
            {
                title = T["You have more than {0} unread notifications", Model.MaxVisibleNotifications];
            }
            else
            {
                title = T.Plural(Model.TotalUnread, "You have {1} unread notification", "You have {0} unread notifications", Model.TotalUnread);
            }

            <i class="fa-solid fa-bell" aria-hidden="true" 
               data-bs-toggle="tooltip" data-bs-original-title="@title"></i>

            <span class="position-absolute start-50 translate-middle badge rounded-pill text-bg-danger" 
                  id="UnreadNotificationBadge">@Model.TotalUnread</span>
        }
        else
        {
            <i class="fa-regular fa-bell" aria-hidden="true" 
               data-bs-toggle="tooltip" data-bs-original-title="@T["You have no unread notifications"]"></i>
        }
    </a>
    <ul class="dropdown-menu dropdown-menu-end notification-navbar scrollable position-absolute">
        @if (Model.Notifications.Count > 0)
        {
            var maxCount = Math.Min(Model.MaxVisibleNotifications, Model.Notifications.Count);
            <li>
                @for (int i = 0; i < maxCount; i++)
                {
                    var notification = Model.Notifications[i];
                    var shape = await NotificationDisplayDriver.BuildDisplayAsync(notification, UpdateModelAccessor.ModelUpdater, "Header");
                    shape.Properties[nameof(Notification)] = notification;
                    <div class="@(i == maxCount - 1 ? string.Empty : "border-bottom")">
                        @await DisplayAsync(shape)
                    </div>
                }
            </li>
        }
        else
        {
            <li class="text-center">@T["You have no unread notifications."]</li>
        }

        <li><hr class="dropdown-divider"></li>

        @if (await AuthorizationService.AuthorizeAsync(ViewContext.HttpContext.User, AdminPermissions.AccessAdminPanel))
        {
            <li>
                <a class="dropdown-item fw-bold" asp-action="List" asp-controller="Admin" 
                   asp-area="@NotificationConstants.Features.Notifications">
                   @T["Notification Center"]
                </a>
            </li>
        }
    </ul>
</li>

<script at="Foot" asp-name="notification-manager-initializes" depends-on="notification-manager">
    (function () {
        notificationManager.initializeContainer('@Url.RouteUrl(MarkAsReadEndpoints.RouteName, new { area = NotificationConstants.Features.Notifications })', '#UnreadNotificationBadge')
    })();
</script>

<style at="Head">
    #UnreadNotificationBadge {
        top:5px;
    }
    .notification-navbar {
        max-width: 20rem;
    }
    .notification-navbar .notification-is-unread {
        font-weight: 600 !important;
    }
    .dropdown-menu.scrollable {
        overflow-y: auto;
        max-height: var(--oc-height-dropdown, 250px);
        scrollbar-width: thin;
    }
</style>
```

**📋 Advanced Features:**
- ✅ **Badge system**: Unread count với Bootstrap badges
- ✅ **Pluralization**: `T.Plural()` cho multiple languages
- ✅ **Authorization checks**: `AuthorizationService.AuthorizeAsync()`
- ✅ **Dynamic styling**: Conditional CSS classes
- ✅ **Inline scripts**: `<script at="Foot">` với dependencies
- ✅ **Inline styles**: `<style at="Head">` cho component-specific CSS

---

## 📝 **BƯỚC 5: UTILITY TEMPLATES**

### **💬 5.1. MESSAGE.CSHTML (ALERT SYSTEM)**

```csharp
@{
    string type = Model.Type.ToString().ToLowerInvariant();
    var bsClassName = type switch
    {
        "information" => "alert-info",
        "error" => "alert-danger",
        _ => $"alert-{type}",
    };
}

<div class="alert alert-dismissible @bsClassName fade show message-@type" role="alert">
    @Model.Message
    <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
</div>
```

### **📄 5.2. PAGER.CSHTML (PAGINATION)**

```csharp
@{
    Model.Metadata.Alternates.Clear();
    Model.Metadata.Type = "Pager_Links";
}

<nav aria-label="@T["Listing pages"]">
    @await DisplayAsync(Model)
</nav>
```

### **🎛️ 5.3. WIDGET-FORM.CSHTML (FORM WRAPPER)**

```csharp
@{
    Model.Classes.Add("widget-body mb-3");
}

<div class="@string.Join(' ', Model.Classes.ToArray())">
    @await DisplayAsync(Model.Content)
</div>
```

### **🌊 5.4. FLOWPART.CSHTML (WIDGET FLOW SYSTEM)**

```csharp
@using Microsoft.AspNetCore.Authorization
@using OrchardCore.ContentManagement
@using OrchardCore.Flows.Models
@using OrchardCore.Flows.ViewModels

@model FlowPartViewModel

@inject OrchardCore.ContentManagement.Display.IContentItemDisplayManager ContentItemDisplayManager
@inject IAuthorizationService AuthorizationService
@inject IContentDefinitionManager ContentDefinitionManager

@{
    var widgetDefinitions = (await ContentDefinitionManager.ListWidgetTypeDefinitionsAsync())
    .ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);
}
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

**📋 Flow System Features:**
- ✅ **Widget definitions**: `ContentDefinitionManager.ListWidgetTypeDefinitionsAsync()`
- ✅ **Security checks**: `AuthorizationService.AuthorizeAsync()`
- ✅ **Dynamic CSS classes**: Widget type, alignment, size classes
- ✅ **Content display**: `ContentItemDisplayManager.BuildDisplayAsync()`

---

## 🎯 **LAYOUT PATTERNS SUMMARY**

### **🔧 BASIC PATTERNS**
1. **SafeMode Pattern**: Minimal HTML, no Bootstrap, basic sections
2. **Section-based Architecture**: Header, Messages, Body, Footer
3. **Localization**: `@T["Text"]` cho multi-language
4. **Culture Support**: `@Orchard.CultureName()`, `@Orchard.CultureDir()`

### **🚀 ADVANCED PATTERNS**
1. **Pre-render Strategy**: Navbar pre-render để inject resources
2. **Theme Toggle**: Dark/light mode với ThemeTogglerService
3. **RTL Support**: Conditional bootstrap-rtl loading
4. **Resource Management**: `asp-name`, `depends-on`, `at="Head|Foot"`
5. **Caching Strategy**: Menu caching với context và duration
6. **Shape System**: `<shape type="Name" />` cho reusable components

### **🏢 ADMIN PATTERNS**
1. **Admin Layout**: Sidebar navigation với collapsible menu
2. **Admin Settings**: `Site.As<AdminSettings>()` integration
3. **Admin Branding**: `await New.AdminBranding()` cho custom branding
4. **Modal Integration**: Confirmation dialogs với metadata

---

## 🎭 **SHAPE PATTERNS SUMMARY**

### **🔧 BASIC SHAPES**
1. **Branding**: Simple navbar brand link
2. **Menu**: TagBuilder pattern với navbar-nav
3. **MenuItem**: Complex dropdown logic với Bootstrap

### **🚀 ADVANCED SHAPES**
1. **TagBuilder Pattern**: Dynamic HTML element creation
2. **Shape Morphing**: `Model.Metadata.Type` changes
3. **CSS Class Management**: `Model.Classes.Add()`
4. **Attribute Management**: `Model.Attributes.Add()`
5. **Recursive Rendering**: Nested menu items với level tracking

### **🎨 SPECIALIZED SHAPES**
1. **Theme Toggle**: Dark/light mode với FontAwesome icons
2. **Notification System**: Badge system với pluralization
3. **User Menu**: Authorization-based menu items
4. **Flow System**: Widget layout với alignment và sizing

---

## ✅ **CHECKLIST LAYOUT & TEMPLATES**

### **🔧 BASIC SETUP (BẮT BUỘC)**
- [ ] ✅ Tạo `Layout.cshtml` với OrchardCore patterns
- [ ] ✅ Setup `_ViewImports.cshtml` với proper inheritance
- [ ] ✅ Implement basic shapes: Branding, Menu, MenuItem
- [ ] ✅ Add culture support: `@Orchard.CultureName()`, `@Orchard.CultureDir()`
- [ ] ✅ Add localization: `@T["Text"]` cho all user-facing text
- [ ] ✅ Setup section architecture: Header, Messages, Footer
- [ ] ✅ Add resource management: `<resources type="Header|FootScript" />`

### **🚀 ADVANCED SETUP (KHUYẾN NGHỊ)**
- [ ] ✅ Implement pre-render strategy cho navbar
- [ ] ✅ Add theme toggle functionality
- [ ] ✅ Setup RTL support với conditional loading
- [ ] ✅ Implement menu caching với proper context
- [ ] ✅ Add TagBuilder patterns cho dynamic HTML
- [ ] ✅ Setup shape morphing cho flexible templates
- [ ] ✅ Add notification system với badge support
- [ ] ✅ Implement widget flow system

### **🏢 ADMIN SETUP (NẾU CẦN)**
- [ ] ✅ Create admin-specific layout
- [ ] ✅ Setup sidebar navigation
- [ ] ✅ Add admin settings integration
- [ ] ✅ Implement admin branding
- [ ] ✅ Add modal confirmation system

---

## 🚫 **NHỮNG LỖI THƯỜNG GẶP**

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

<!-- ✅ ĐÚNG: Đầy đủ OrchardCore patterns -->
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

### **❌ SHAPE ERRORS**
```html
<!-- ❌ SAI: Hard-code menu -->
<ul class="navbar-nav">
    <li><a href="/home">Home</a></li>
    <li><a href="/about">About</a></li>
</ul>

<!-- ✅ ĐÚNG: Sử dụng Shape system -->
<menu alias="alias:main-menu" cache-id="main-menu" 
      cache-fixed-duration="00:05:00" cache-tag="alias:main-menu" />
```

### **❌ RESOURCE ERRORS**
```html
<!-- ❌ SAI: Hard-code CSS/JS -->
<link rel="stylesheet" href="/css/bootstrap.min.css">
<script src="/js/bootstrap.min.js"></script>

<!-- ✅ ĐÚNG: Resource management -->
<style asp-name="bootstrap" version="5" at="Head"></style>
<script asp-name="bootstrap" version="5" at="Foot"></script>
```

---

## 📊 **PERFORMANCE METRICS**

### **⚡ RENDERING TIME**
- **Basic Layout**: < 50ms
- **Advanced Layout**: 50-100ms
- **Admin Layout**: 100-200ms

### **📦 TEMPLATE SIZE**
- **Minimal Templates**: < 5KB
- **Standard Templates**: 5-20KB
- **Complex Templates**: 20-50KB

### **🚀 CACHING EFFECTIVENESS**
- **Menu Caching**: 90%+ cache hit rate
- **Shape Caching**: 80%+ cache hit rate
- **Resource Bundling**: 50%+ size reduction

---

## 🎯 **NEXT STEPS**

Sau khi hoàn thành Layout & Template Patterns, anh có thể tiếp tục với:

1. **🎭 Shape System & Display Management** - Advanced shapes và display logic
2. **🎯 Responsive Design & CSS Framework** - Mobile-first responsive design
3. **🎪 Asset Management & Optimization** - Advanced asset pipeline
4. **🔧 Services & Startup Configuration** - Custom services và DI

---

## 🔗 **REFERENCES TỪ SOURCE CODE**

- **TheTheme/Layout.cshtml**: `/src/OrchardCore.Themes/TheTheme/Views/Layout.cshtml`
- **TheAdmin/Layout.cshtml**: `/src/OrchardCore.Themes/TheAdmin/Views/Layout.cshtml`
- **SafeMode/Layout.cshtml**: `/src/OrchardCore.Themes/SafeMode/Views/Layout.cshtml`
- **Shape Templates**: `/src/OrchardCore.Themes/TheTheme/Views/`
- **TagBuilder Documentation**: Microsoft.AspNetCore.Html.TagBuilder

---

**🎉 Layout & Template Patterns là nền tảng hiển thị của theme! Với patterns chuẩn OrchardCore, anh có thể tạo layouts professional và maintainable! 🚀🖼️**

---

*Timing: 4-6 giờ cho basic layout, 1-2 ngày cho advanced patterns với tất cả features.*