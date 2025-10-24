# 02. 🏗️ CẤU TRÚC DỰ ÁN VÀ NGUYÊN TẮC ĐẶT TÊN ORCHARDCORE

## 📐 VÍ DỤ DỰ ÁN WEBSANPHAM - ÁP DỤNG NGUYÊN TẮC ORCHARDCORE

### 🎯 Cấu trúc Repository - WebSanPham

```
📁 WebSanPhamRepository/             ← Repository root
├── 📄 README.md
├── 📄 LICENSE
├── 📁 WebSanPham/                   ← Project folder (như OrchardCore folder)
│   ├── 📄 WebSanPham.sln                ← Solution file (28 projects)
│   ├── 📄 .gitignore
│   ├── 📄 README.md
│   ├── 📄 Directory.Build.props
│   ├── 📄 NuGet.config
│   ├── 📄 global.json
│   ├── 📄 package.json
│   ├── 📄 gulpfile.js
│   ├── 📁 build/                        ← Build scripts & configurations
│   ├── 📁 docs/                         ← Documentation files
│   ├── 📁 scripts/                      ← Automation scripts
│   ├── 📁 src/                          ← SOURCE CODE CHÍNH
│   │   ├── 📁 WebSanPham.Core/              ← Core Framework
│   │   ├── 📁 WebSanPham.Modules/           ← Modules container (12 modules)
│   │   │   ├── 📁 WebSanPham.Products/          ← Quản lý sản phẩm
│   │   │   ├── 📁 WebSanPham.Categories/        ← Quản lý danh mục
│   │   │   ├── 📁 WebSanPham.Orders/            ← Quản lý đơn hàng
│   │   │   ├── 📁 WebSanPham.Customers/         ← Quản lý khách hàng
│   │   │   ├── 📁 WebSanPham.Inventory/         ← Quản lý kho
│   │   │   ├── 📁 WebSanPham.Reviews/           ← Đánh giá sản phẩm
│   │   │   ├── 📁 WebSanPham.Promotions/        ← Khuyến mãi
│   │   │   ├── 📁 WebSanPham.Reports/           ← Báo cáo
│   │   │   ├── 📁 WebSanPham.Payment.Stripe/    ← Thanh toán Stripe
│   │   │   ├── 📁 WebSanPham.Shipping.Ghn/      ← Vận chuyển GHN
│   │   │   ├── 📁 WebSanPham.Email.SendGrid/    ← Email SendGrid
│   │   │   └── 📁 WebSanPham.Analytics/         ← Phân tích dữ liệu
│   │   ├── 📁 WebSanPham.Themes/            ← Themes container (4 themes)
│   │   │   ├── 📁 WebSanPham.Storefront.Theme/  ← Theme cửa hàng chính
│   │   │   ├── 📁 WebSanPham.Mobile.Theme/      ← Theme mobile
│   │   │   ├── 📁 WebSanPham.Admin.Theme/       ← Theme quản trị
│   │   │   └── 📁 WebSanPham.Minimal.Theme/     ← Theme tối giản
│   │   ├── 📁 WebSanPham.Services/          ← Business services
│   │   │   ├── 📁 WebSanPham.Services.Payment/
│   │   │   ├── 📁 WebSanPham.Services.Inventory/
│   │   │   └── 📁 WebSanPham.Services.Notification/
│   │   └── 📁 WebSanPham.Website/           ← Main Web Application
│   ├── 📁 Templates/                    ← Project templates
│   └── 📁 test/                         ← Test projects
│       ├── 📁 WebSanPham.Tests.Unit/
│       └── 📁 WebSanPham.Tests.Integration/
└── 📁 docs/                             ← Repository documentation
```

## 🧩 NGUYÊN TẮC ĐẶT TÊN MODULES (HỌC THEO ORCHARDCORE)

### 📋 1. MODULES CHUNG - Container Folder

#### **🔹 Pattern OrchardCore:**
```
📁 OrchardCore.Modules/          ← Container chứa tất cả modules
├── 📁 OrchardCore.Users/            ← Module quản lý user
├── 📁 OrchardCore.Media/            ← Module quản lý media
├── 📁 OrchardCore.Email/            ← Module email cơ bản
├── 📁 OrchardCore.Email.Smtp/       ← Module email SMTP
└── 📁 OrchardCore.Search.Lucene/    ← Module tìm kiếm Lucene
```

#### **🔹 Pattern WebSanPham (Áp dụng):**
```
📁 WebSanPham.Modules/           ← Container chứa tất cả modules
├── 📁 WebSanPham.Products/          ← Module quản lý sản phẩm
├── 📁 WebSanPham.Categories/        ← Module quản lý danh mục
├── 📁 WebSanPham.Orders/            ← Module quản lý đơn hàng
├── 📁 WebSanPham.Customers/         ← Module quản lý khách hàng
├── 📁 WebSanPham.Inventory/         ← Module quản lý kho
├── 📁 WebSanPham.Reviews/           ← Module đánh giá sản phẩm
├── 📁 WebSanPham.Promotions/        ← Module khuyến mãi
├── 📁 WebSanPham.Reports/           ← Module báo cáo
├── 📁 WebSanPham.Payment.Stripe/    ← Module thanh toán Stripe
├── 📁 WebSanPham.Shipping.Ghn/      ← Module vận chuyển GHN
├── 📁 WebSanPham.Email.SendGrid/    ← Module email SendGrid
└── 📁 WebSanPham.Analytics/         ← Module phân tích dữ liệu
```

### 📋 2. MODULES RIÊNG - Cấu trúc bên trong

#### **🔹 Cấu trúc Module WebSanPham.Products:**
```
📁 WebSanPham.Products/
├── 📁 Controllers/                  ← API Controllers
│   ├── 📄 ProductController.cs
│   └── 📄 ProductApiController.cs
├── 📁 Drivers/                      ← Display Drivers
│   ├── 📄 ProductDisplayDriver.cs
│   └── 📄 ProductPartDisplayDriver.cs
├── 📁 Models/                       ← Data Models
│   ├── 📄 Product.cs
│   ├── 📄 ProductPart.cs
│   └── 📄 ProductVariant.cs
├── 📁 Services/                     ← Business Services
│   ├── 📄 IProductService.cs
│   ├── 📄 ProductService.cs
│   └── 📄 ProductPriceService.cs
├── 📁 ViewModels/                   ← View Models
│   ├── 📄 ProductViewModel.cs
│   ├── 📄 ProductListViewModel.cs
│   └── 📄 ProductEditViewModel.cs
├── 📁 Views/                        ← Razor Views
│   ├── 📁 Product/
│   ├── 📁 ProductAdmin/
│   └── 📁 Items/
├── 📁 Migrations/                   ← Database Migrations
│   └── 📄 ProductMigrations.cs
├── 📄 Manifest.cs                   ← Module Manifest
├── 📄 Startup.cs                    ← Module Startup
├── 📄 Permissions.cs                ← Module Permissions
└── 📄 WebSanPham.Products.csproj    ← Project File
```

### 📋 3. NGUYÊN TẮC ĐẶT TÊN MODULES

#### **🔹 Modules cơ bản:**
```
{ProjectName}.{Function}
WebSanPham.Products          ← Quản lý sản phẩm
WebSanPham.Categories        ← Quản lý danh mục
WebSanPham.Orders            ← Quản lý đơn hàng
WebSanPham.Customers         ← Quản lý khách hàng
```

#### **🔹 Modules tích hợp công nghệ:**
```
{ProjectName}.{Function}.{Technology}
WebSanPham.Payment.Stripe    ← Thanh toán qua Stripe
WebSanPham.Payment.Momo      ← Thanh toán qua MoMo
WebSanPham.Shipping.Ghn      ← Vận chuyển qua GHN
WebSanPham.Email.SendGrid    ← Email qua SendGrid
```

#### **🔹 Modules mở rộng:**
```
{ProjectName}.{BaseFunction}.{Extension}
WebSanPham.Products.Advanced ← Sản phẩm nâng cao
WebSanPham.Orders.Tracking   ← Theo dõi đơn hàng
WebSanPham.Customers.Loyalty ← Khách hàng thân thiết
```

## 🎨 NGUYÊN TẮC ĐẶT TÊN THEMES (HỌC THEO ORCHARDCORE)

### 📋 1. THEMES CHUNG - Container Folder

#### **🔹 Pattern OrchardCore:**
```
📁 OrchardCore.Themes/           ← Container chứa tất cả themes
├── 📁 SafeMode/                     ← Theme an toàn
├── 📁 TheAdmin/                     ← Theme quản trị
├── 📁 TheAgencyTheme/               ← Theme doanh nghiệp
├── 📁 TheBlogTheme/                 ← Theme blog
├── 📁 TheComingSoonTheme/           ← Theme coming soon
└── 📁 TheTheme/                     ← Theme mặc định
```

#### **🔹 Pattern WebSanPham (Áp dụng):**
```
📁 WebSanPham.Themes/            ← Container chứa tất cả themes
├── 📁 WebSanPham.Storefront.Theme/  ← Theme cửa hàng chính
├── 📁 WebSanPham.Mobile.Theme/      ← Theme mobile
├── 📁 WebSanPham.Admin.Theme/       ← Theme quản trị
└── 📁 WebSanPham.Minimal.Theme/     ← Theme tối giản
```

### 📋 2. THEMES RIÊNG - Cấu trúc bên trong

#### **🔹 Cấu trúc Theme WebSanPham.Storefront.Theme:**
```
📁 WebSanPham.Storefront.Theme/
├── 📁 Assets/                       ← CSS, JS, images
│   ├── 📁 css/
│   │   ├── 📄 storefront.css
│   │   ├── 📄 products.css
│   │   └── 📄 responsive.css
│   ├── 📁 js/
│   │   ├── 📄 storefront.js
│   │   ├── 📄 cart.js
│   │   └── 📄 product-gallery.js
│   ├── 📁 images/
│   │   ├── 📄 logo.png
│   │   ├── 📄 banner.jpg
│   │   └── 📁 icons/
│   └── 📁 fonts/
├── 📁 Views/                        ← Razor views
│   ├── 📁 Layout/
│   │   ├── 📄 _Layout.cshtml
│   │   ├── 📄 _Header.cshtml
│   │   └── 📄 _Footer.cshtml
│   ├── 📁 Product/
│   │   ├── 📄 Display.cshtml
│   │   ├── 📄 List.cshtml
│   │   └── 📄 Detail.cshtml
│   ├── 📁 Category/
│   ├── 📁 Cart/
│   └── 📁 Shared/
├── 📁 Drivers/                      ← Display drivers (nếu cần)
├── 📁 Recipes/                      ← Setup recipes
│   └── 📄 storefront-setup.recipe.json
├── 📄 Assets.json                   ← Asset definitions
├── 📄 Manifest.cs                   ← Theme manifest
├── 📄 Startup.cs                    ← Theme startup
├── 📄 ResourceManagementOptionsConfiguration.cs
└── 📄 WebSanPham.Storefront.Theme.csproj ← Project file
```

### 📋 3. NGUYÊN TẮC ĐẶT TÊN THEMES

#### **🔹 Themes theo mục đích:**
```
{ProjectName}.{Purpose}.Theme
WebSanPham.Storefront.Theme  ← Theme cửa hàng
WebSanPham.Admin.Theme       ← Theme quản trị
WebSanPham.Mobile.Theme      ← Theme mobile
WebSanPham.Minimal.Theme     ← Theme tối giản
```

#### **🔹 Themes theo thiết bị:**
```
{ProjectName}.{Device}.Theme
WebSanPham.Desktop.Theme     ← Theme desktop
WebSanPham.Tablet.Theme      ← Theme tablet
WebSanPham.Mobile.Theme      ← Theme mobile
WebSanPham.Responsive.Theme  ← Theme responsive
```

#### **🔹 Themes theo framework:**
```
{ProjectName}.{Framework}.Theme
WebSanPham.Bootstrap.Theme   ← Theme Bootstrap
WebSanPham.Tailwind.Theme    ← Theme Tailwind CSS
WebSanPham.Material.Theme    ← Theme Material Design
WebSanPham.Bulma.Theme       ← Theme Bulma CSS
```

### 📋 4. MANIFEST THEMES

#### **🔹 WebSanPham.Storefront.Theme/Manifest.cs:**
```csharp
using OrchardCore.DisplayManagement.Manifest;
using OrchardCore.Modules.Manifest;

[assembly: Theme(
    Name = "WebSanPham Storefront Theme",
    Author = "WebSanPham Team",
    Website = "https://websanpham.com",
    Version = "1.0.0",
    Description = "Theme chính cho cửa hàng WebSanPham với thiết kế hiện đại và responsive.",
    Dependencies = [
        "OrchardCore.Themes"
    ],
    Tags = [
        "Ecommerce", 
        "Storefront", 
        "Responsive", 
        "Bootstrap"
    ]
)]
```

## 🎯 MANIFEST MODULES

### 📋 WebSanPham.Products/Manifest.cs:
```csharp
using OrchardCore.Modules.Manifest;

[assembly: Module(
    Name = "Products Management",
    Author = "WebSanPham Team",
    Website = "https://websanpham.com",
    Version = "1.0.0",
    Description = "Module quản lý sản phẩm cho hệ thống WebSanPham với đầy đủ tính năng CRUD, phân loại và tìm kiếm.",
    Dependencies = [
        "OrchardCore.Contents",
        "OrchardCore.ContentTypes",
        "OrchardCore.Media",
        "WebSanPham.Categories"
    ],
    Category = "Commerce"
)]

[assembly: Feature(
    Id = "WebSanPham.Products",
    Name = "Products Management",
    Description = "Quản lý sản phẩm cơ bản",
    Category = "Commerce"
)]

[assembly: Feature(
    Id = "WebSanPham.Products.Advanced",
    Name = "Advanced Products",
    Description = "Tính năng sản phẩm nâng cao: variants, bundles, digital products",
    Dependencies = [
        "WebSanPham.Products"
    ],
    Category = "Commerce"
)]
```

## 🎯 VALIDATION RULES - QUY TẮC KIỂM TRA

### ✅ Naming Validation

**🔹 Solution Names:**
```regex
^[A-Z][a-zA-Z0-9]*\.sln$
WebSanPham.sln ✅
websanpham.sln ❌
Web-SanPham.sln ❌
```

**🔹 Module Names:**
```regex
^[A-Z][a-zA-Z0-9]*(\.[A-Z][a-zA-Z0-9]*)*$
WebSanPham.Products ✅
WebSanPham.Payment.Stripe ✅
websanpham.products ❌
WebSanPham_Products ❌
```

**🔹 Theme Names:**
```regex
^[A-Z][a-zA-Z0-9]*(\.[A-Z][a-zA-Z0-9]*)*\.Theme$
WebSanPham.Storefront.Theme ✅
WebSanPham.Mobile.Theme ✅
WebSanPham.Storefront ❌
WebSanPham-Storefront.Theme ❌
```

## 🎯 CATEGORIES CHUẨN

### 📋 Module Categories cho WebSanPham:
```
Commerce        ← Products, Orders, Payments, Inventory
Customer        ← Customers, Reviews, Loyalty, Support
Marketing       ← Promotions, Analytics, Email, SEO
Content         ← CMS, Blog, Pages, Media
Integration     ← Third-party APIs, Shipping, Payment gateways
Security        ← Authentication, Authorization, Audit
Performance     ← Caching, Optimization, CDN
Development     ← APIs, Webhooks, Tools
Reporting       ← Analytics, Sales reports, Dashboard
Communication   ← Email, SMS, Notifications
```

## 🎯 BEST PRACTICES SUMMARY

### ✅ DO (Nên làm):
- ✅ **Consistent naming** - Đặt tên nhất quán theo pattern
- ✅ **Clear hierarchy** - Phân cấp rõ ràng: Solution → Modules/Themes → Projects
- ✅ **Meaningful names** - Tên có ý nghĩa, dễ hiểu
- ✅ **Proper categorization** - Phân loại đúng theo chức năng
- ✅ **Complete manifests** - Manifest đầy đủ thông tin
- ✅ **Dependency management** - Quản lý dependencies rõ ràng
- ✅ **Version control** - Semantic versioning
- ✅ **Container folders** - Sử dụng thư mục chung cho Modules/Themes

### ❌ DON'T (Không nên):
- ❌ **Generic names** - Tên chung chung như "Module1", "Theme1"
- ❌ **Inconsistent patterns** - Không theo pattern chuẩn
- ❌ **Missing dependencies** - Thiếu khai báo dependencies
- ❌ **Wrong categories** - Phân loại sai chức năng
- ❌ **Poor documentation** - Manifest thiếu thông tin
- ❌ **Mixed naming styles** - Trộn lẫn camelCase, snake_case
- ❌ **Special characters** - Sử dụng ký tự đặc biệt trong tên
- ❌ **Flat structure** - Không sử dụng container folders

## 🎯 KẾT LUẬN

**NGUYÊN TẮC VÀNG CHO DỰ ÁN WEBSANPHAM:**

### 📋 1. CẤU TRÚC SOLUTION
```
WebSanPham.sln
├── 📁 WebSanPham.Modules/     ← Container chứa 12 modules
├── 📁 WebSanPham.Themes/      ← Container chứa 4 themes  
├── 📁 WebSanPham.Services/    ← Business services
└── 📁 WebSanPham.Website/     ← Main application
```

### 📋 2. NAMING PATTERNS
- **Modules:** `WebSanPham.{Function}[.{Technology}]`
- **Themes:** `WebSanPham.{Purpose}.Theme`
- **Services:** `WebSanPham.Services.{Domain}`

### 📋 3. CONTAINER FOLDERS
- **✅ Modules chung:** Tất cả modules trong `WebSanPham.Modules/`
- **✅ Themes chung:** Tất cả themes trong `WebSanPham.Themes/`
- **✅ Cấu trúc rõ ràng:** Mỗi module/theme có cấu trúc chuẩn

### 📋 4. MANIFEST CHUẨN
- **Module:** Name, Author, Version, Description, Dependencies, Category
- **Theme:** Name, Author, Version, Description, Tags
- **Features:** Hỗ trợ multiple features trong 1 module

### 📋 5. CATEGORIES
- **Commerce, Customer, Marketing, Content, Integration**
- **Security, Performance, Development, Reporting, Communication**

**Áp dụng nguyên tắc này cho dự án WebSanPham sẽ có cấu trúc professional như OrchardCore!** 🚀

---
*Tài liệu dựa trên phân tích cấu trúc OrchardCore và áp dụng cho dự án WebSanPham*
*Cập nhật: 2024-10-24*