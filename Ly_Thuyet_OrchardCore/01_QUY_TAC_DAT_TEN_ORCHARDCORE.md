# 01. 🎯 QUY TẮC ĐẶT TÊN TRONG ORCHARDCORE

## 📐 CÔNG THỨC TỔNG QUÁT

```
{CompanyPrefix}.{FunctionalName}.{ComponentType}
```

## 🧩 1. CÔNG THỨC MODULES

### 📋 Cơ bản:
```
{CompanyPrefix}.{ModuleName}
```

**Ví dụ:**
```
✅ OrchardCore.Users          ← OrchardCore + Users
✅ OrchardCore.Media          ← OrchardCore + Media
✅ MyCompany.Ecommerce        ← MyCompany + Ecommerce
✅ MyCompany.Blog             ← MyCompany + Blog
```

### 📋 Phức hợp (2 từ):
```
{CompanyPrefix}.{MainFunction}{SubFunction}
```

**Ví dụ:**
```
✅ OrchardCore.ContentTypes    ← Content + Types
✅ OrchardCore.AdminMenu       ← Admin + Menu
✅ OrchardCore.BackgroundTasks ← Background + Tasks
✅ MyCompany.UserProfiles      ← User + Profiles
```

### 📋 Tích hợp công nghệ:
```
{CompanyPrefix}.{MainFunction}.{Technology}
```

**Ví dụ:**
```
✅ OrchardCore.Email.Smtp      ← Email + Smtp
✅ OrchardCore.Media.Azure     ← Media + Azure
✅ OrchardCore.Search.Lucene   ← Search + Lucene
✅ MyCompany.Payment.Stripe    ← Payment + Stripe
```

### 📋 Mở rộng module có sẵn:
```
{CompanyPrefix}.{BaseModule}.{Extension}
```

**Ví dụ:**
```
✅ MyCompany.Users.Extended    ← Extend Users module
✅ MyCompany.Media.Gallery     ← Extend Media module
✅ MyCompany.Content.Workflow  ← Extend Content module
```

## 🎨 2. CÔNG THỨC THEMES

### 📋 Cơ bản:
```
{CompanyPrefix}.{ThemeName}.Theme
```

**Ví dụ:**
```
✅ MyCompany.Corporate.Theme
✅ MyCompany.Bootstrap.Theme
✅ MyCompany.Mobile.Theme
```

### 📋 OrchardCore pattern (dùng "The"):
```
The{ThemeName}Theme
```

**Ví dụ:**
```
✅ TheAdmin              ← The + Admin
✅ TheAgencyTheme        ← The + Agency + Theme
✅ TheBlogTheme          ← The + Blog + Theme
✅ TheComingSoonTheme    ← The + ComingSoon + Theme
```

### 📋 Theo mục đích:
```
{CompanyPrefix}.{Purpose}.Theme
```

**Ví dụ:**
```
✅ MyCompany.Admin.Theme      ← Admin theme
✅ MyCompany.Frontend.Theme   ← Frontend theme
✅ MyCompany.Mobile.Theme     ← Mobile theme
```

## 🌐 3. CÔNG THỨC WEB APPLICATIONS

### 📋 Cơ bản:
```
{CompanyPrefix}.{ApplicationType}.Web
```

**Ví dụ:**
```
✅ OrchardCore.Cms.Web        ← CMS Web App
✅ OrchardCore.Mvc.Web        ← MVC Web App
✅ MyCompany.Website          ← Main website (rút gọn)
✅ MyCompany.Admin.Web        ← Admin portal
✅ MyCompany.Api.Web          ← API application
```

### 📋 Theo chức năng:
```
{CompanyPrefix}.{Function}
```

**Ví dụ:**
```
✅ MyCompany.Website          ← Main website
✅ MyCompany.Portal           ← Customer portal
✅ MyCompany.Dashboard        ← Analytics dashboard
```

## 📚 4. CÔNG THỨC LIBRARIES

### 📋 Core libraries:
```
{CompanyPrefix}.{Purpose}
```

**Ví dụ:**
```
✅ OrchardCore.Abstractions   ← Interfaces
✅ OrchardCore.Data           ← Data access
✅ MyCompany.Core             ← Core utilities
✅ MyCompany.Services         ← Business services
```

### 📋 Specialized libraries:
```
{CompanyPrefix}.{MainArea}.{SubArea}
```

**Ví dụ:**
```
✅ OrchardCore.ContentManagement.Abstractions
✅ OrchardCore.DisplayManagement.Liquid
✅ MyCompany.Data.EntityFramework
✅ MyCompany.Services.Payment
```

## 🧪 5. CÔNG THỨC TEST PROJECTS

### 📋 Unit tests:
```
{ProjectName}.Tests
```

**Ví dụ:**
```
✅ MyCompany.Ecommerce.Tests
✅ MyCompany.Core.Tests
```

### 📋 Integration tests:
```
{ProjectName}.Integration.Tests
```

**Ví dụ:**
```
✅ MyCompany.Website.Integration.Tests
✅ MyCompany.Api.Integration.Tests
```

## 📁 6. CÔNG THỨC SOLUTION & FOLDERS

### 📋 Solution name:
```
{CompanyName}.{ProjectName}
```

**Ví dụ:**
```
✅ MyCompany.EcommerceProject
✅ MyCompany.CorporateWebsite
✅ MyCompany.CustomerPortal
```

### 📋 Solution folders:
```
📁 {SolutionName}
├── 📁 build           ← Build scripts
├── 📁 docs            ← Documentation  
├── 📁 scripts         ← Automation scripts
├── 📁 Solution Items  ← Global files
├── 📁 src             ← Source code
├── 📁 Templates       ← Custom templates
└── 📁 test            ← Test projects
```

## 🎯 7. CÔNG THỨC MANIFEST NAMES

### 📋 Module manifest:
```csharp
[assembly: Module(
    Name = "{Friendly Display Name}",           ← Không prefix
    Author = "{CompanyName}",
    Version = "{Major}.{Minor}.{Patch}",
    Description = "{Clear description}",
    Category = "{Functional Category}"
)]
```

**Ví dụ:**
```csharp
[assembly: Module(
    Name = "E-commerce",                        ← Không có "MyCompany"
    Author = "MyCompany",
    Version = "1.0.0",
    Description = "E-commerce functionality",
    Category = "Commerce"
)]
```

### 📋 Theme manifest:
```csharp
[assembly: Theme(
    Name = "{CompanyName} {Theme Purpose} Theme",
    Author = "{CompanyName}",
    Version = "{Major}.{Minor}.{Patch}",
    Description = "{Theme description}",
    Tags = ["{Tag1}", "{Tag2}", "{Tag3}"]
)]
```

## 🎯 8. CÔNG THỨC CATEGORIES

### 📋 Module categories:
```
Security        ← Users, Roles, Authentication
Content         ← Content management, Fields, Types
Commerce        ← E-commerce, Payment, Inventory
Communication   ← Email, SMS, Notifications
Search          ← Search engines, Indexing
Media           ← Files, Images, Storage
Navigation      ← Menus, Routes, URLs
Development     ← APIs, Scripting, Templates
Performance     ← Caching, Optimization
Integration     ← Third-party services
```

## 🎯 9. CÔNG THỨC VALIDATION

### ✅ Kiểm tra tên hợp lệ:

**🔹 Regex pattern cho modules:**
```regex
^[A-Z][a-zA-Z0-9]*(\.[A-Z][a-zA-Z0-9]*)*$
```

**🔹 Regex pattern cho themes:**
```regex
^[A-Z][a-zA-Z0-9]*(\.[A-Z][a-zA-Z0-9]*)*\.Theme$
```

**🔹 Validation rules:**
```csharp
// ✅ Valid names
MyCompany.Ecommerce          ← PascalCase, meaningful
MyCompany.Users.Extended     ← Clear hierarchy
MyCompany.Payment.Stripe     ← Technology integration

// ❌ Invalid names  
mycompany.ecommerce          ← lowercase
MyCompany_Ecommerce          ← underscore
MyCompany.EC                 ← abbreviation
MyCompany.Ecommerce.Module   ← redundant suffix
```

## 🎯 10. CÔNG THỨC ĐẦY ĐỦ - TEMPLATE

### 📋 Complete naming template:

```
📁 {Company}.{Project}Solution/
├── 📁 src/
│   ├── 🌐 {Company}.{Project}.Web
│   ├── 📦 {Company}.{Function1}
│   ├── 📦 {Company}.{Function2}.{Technology}
│   ├── 🎨 {Company}.{ThemeName}.Theme
│   ├── 📚 {Company}.Core
│   └── 📚 {Company}.Services
├── 📁 test/
│   ├── 🧪 {Company}.{Project}.Tests
│   └── 🧪 {Company}.{Project}.Integration.Tests
└── 📁 build/
```

### 📋 Ví dụ thực tế:

```
📁 TechCorp.EcommerceSolution/
├── 📁 src/
│   ├── 🌐 TechCorp.Website
│   ├── 📦 TechCorp.Ecommerce
│   ├── 📦 TechCorp.Payment.Stripe
│   ├── 📦 TechCorp.Inventory
│   ├── 🎨 TechCorp.Corporate.Theme
│   ├── 🎨 TechCorp.Mobile.Theme
│   ├── 📚 TechCorp.Core
│   └── 📚 TechCorp.Services
├── 📁 test/
│   ├── 🧪 TechCorp.Ecommerce.Tests
│   └── 🧪 TechCorp.Website.Integration.Tests
└── 📁 build/
```

## 🎯 CÔNG THỨC MASTER - TÓM TẮT

```
MODULES:     {Company}.{Function}[.{Technology}]
THEMES:      {Company}.{Name}.Theme
WEB APPS:    {Company}.{Type}[.Web]
LIBRARIES:   {Company}.{Purpose}
TESTS:       {ProjectName}[.Integration].Tests
SOLUTION:    {Company}.{Project}Solution
```

## 📋 NAMING CONVENTIONS SUMMARY

### ✅ DO (Nên làm):
- ✅ **PascalCase** cho tất cả tên
- ✅ **Meaningful names** - Tên có ý nghĩa
- ✅ **Consistent patterns** - Theo pattern chuẩn
- ✅ **Company prefix** - Prefix tên công ty
- ✅ **Clear separation** - Phân biệt rõ module/theme/web

### ❌ DON'T (Không nên):
- ❌ **camelCase** hoặc **snake_case**
- ❌ **Abbreviations** - Viết tắt khó hiểu
- ❌ **Generic names** - Tên chung chung
- ❌ **Special characters** - Ký tự đặc biệt
- ❌ **Spaces in folder names** - Khoảng trắng

## 🎯 KẾT LUẬN

**QUY TẮC VÀNG ORCHARDCORE:**

1. **📁 Solution Structure**: Theo cấu trúc OrchardCore gốc
2. **🧩 Modules**: `{Company}.{Function}` 
3. **🎨 Themes**: `{Company}.{Name}.Theme`
4. **🌐 Web Apps**: `{Company}.{Type}.Web`
5. **📦 Libraries**: `{Company}.{Purpose}`
6. **📋 Manifests**: Thông tin đầy đủ, category rõ ràng

**Tuân thủ quy tắc này sẽ giúp dự án của bạn professional và dễ maintain!** 🚀

---
*Tài liệu này được tạo dựa trên phân tích source code OrchardCore chính thức*
*Cập nhật: 2024-10-24*