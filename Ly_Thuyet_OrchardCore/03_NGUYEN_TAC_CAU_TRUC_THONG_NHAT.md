# 03. 🏗️ NGUYÊN TẮC CẤU TRÚC THỐNG NHẤT - ORCHARDCORE STANDARD

## 🎯 TRIẾT LÝ: "MỘT NGUYÊN TẮC CHO TẤT CẢ"

### **📋 Tại sao cần thống nhất?**
- ✅ **Consistency** - Nhất quán trong tất cả dự án
- ✅ **Scalability** - Dễ mở rộng từ nhỏ đến lớn
- ✅ **Team onboarding** - Developers quen thuộc cấu trúc
- ✅ **Maintainability** - Dễ bảo trì và phát triển
- ✅ **Professional** - Cấu trúc enterprise-grade

## 🏗️ TEMPLATE CHUẨN - ÁP DỤNG CHO MỌI DỰ ÁN

### 📁 Cấu trúc Repository Standard

```
📁 {ProjectName}Repository/
├── 📄 README.md
├── 📄 LICENSE
├── 📄 .gitignore
├── 📁 {ProjectName}/                    ← Main project folder
│   ├── 📄 {ProjectName}.sln                 ← Solution file
│   ├── 📄 .gitignore
│   ├── 📄 README.md
│   ├── 📄 Directory.Build.props
│   ├── 📄 Directory.Packages.props
│   ├── 📄 NuGet.config
│   ├── 📄 global.json
│   ├── 📄 package.json
│   ├── 📄 gulpfile.js
│   ├── 📄 tsconfig.json
│   ├── 📁 build/                            ← Build scripts & CI/CD
│   │   ├── 📄 build.ps1
│   │   ├── 📄 build.sh
│   │   └── 📄 azure-pipelines.yml
│   ├── 📁 docs/                             ← Documentation
│   │   ├── 📄 getting-started.md
│   │   ├── 📄 deployment.md
│   │   └── 📄 api-reference.md
│   ├── 📁 scripts/                          ← Automation scripts
│   │   ├── 📄 setup.ps1
│   │   ├── 📄 deploy.ps1
│   │   └── 📄 cleanup.ps1
│   ├── 📁 src/                              ← SOURCE CODE
│   │   ├── 📁 {ProjectName}/                    ← Core framework
│   │   ├── 📁 {ProjectName}.Modules/            ← Modules container
│   │   │   ├── 📁 {ProjectName}.{Module1}/          ← Individual modules
│   │   │   ├── 📁 {ProjectName}.{Module2}/
│   │   │   └── 📁 {ProjectName}.{ModuleN}/
│   │   ├── 📁 {ProjectName}.Themes/             ← Themes container
│   │   │   ├── 📁 {ProjectName}.{Theme1}.Theme/     ← Individual themes
│   │   │   ├── 📁 {ProjectName}.{Theme2}.Theme/
│   │   │   └── 📁 {ProjectName}.{ThemeN}.Theme/
│   │   ├── 📁 {ProjectName}.Services/           ← Business services
│   │   │   ├── 📁 {ProjectName}.Services.{Domain1}/
│   │   │   └── 📁 {ProjectName}.Services.{Domain2}/
│   │   └── 📁 {ProjectName}.Website/            ← Main web application
│   ├── 📁 Templates/                        ← Project templates
│   │   ├── 📁 ModuleTemplate/
│   │   └── 📁 ThemeTemplate/
│   └── 📁 test/                             ← Test projects
│       ├── 📁 {ProjectName}.Tests.Unit/
│       ├── 📁 {ProjectName}.Tests.Integration/
│       └── 📁 {ProjectName}.Tests.Performance/
└── 📁 .github/                              ← GitHub workflows
    └── 📁 workflows/
        ├── 📄 ci.yml
        └── 📄 cd.yml
```

## 🎯 ÁP DỤNG CHO CÁC QUY MÔ KHÁC NHAU

### 🏢 1. DỰ ÁN LỚN - WebSanPham Enterprise (Full Structure)

```
📁 WebSanPhamRepository/
├── 📁 WebSanPham/
│   ├── 📄 WebSanPham.sln (45 projects)
│   ├── 📁 build/
│   ├── 📁 docs/
│   ├── 📁 scripts/
│   ├── 📁 src/
│   │   ├── 📁 WebSanPham/                   ← Core framework
│   │   ├── 📁 WebSanPham.Modules/           ← 15 modules
│   │   │   ├── 📁 WebSanPham.Products/
│   │   │   ├── 📁 WebSanPham.Categories/
│   │   │   ├── 📁 WebSanPham.Orders/
│   │   │   ├── 📁 WebSanPham.Customers/
│   │   │   ├── 📁 WebSanPham.Inventory/
│   │   │   ├── 📁 WebSanPham.Reviews/
│   │   │   ├── 📁 WebSanPham.Promotions/
│   │   │   ├── 📁 WebSanPham.Reports/
│   │   │   ├── 📁 WebSanPham.Analytics/
│   │   │   ├── 📁 WebSanPham.Payment.Stripe/
│   │   │   ├── 📁 WebSanPham.Payment.Momo/
│   │   │   ├── 📁 WebSanPham.Shipping.Ghn/
│   │   │   ├── 📁 WebSanPham.Shipping.Viettel/
│   │   │   ├── 📁 WebSanPham.Email.SendGrid/
│   │   │   └── 📁 WebSanPham.Sms.Twilio/
│   │   ├── 📁 WebSanPham.Themes/            ← 5 themes
│   │   │   ├── 📁 WebSanPham.Storefront.Theme/
│   │   │   ├── 📁 WebSanPham.Mobile.Theme/
│   │   │   ├── 📁 WebSanPham.Admin.Theme/
│   │   │   ├── 📁 WebSanPham.Minimal.Theme/
│   │   │   └── 📁 WebSanPham.B2B.Theme/
│   │   ├── 📁 WebSanPham.Services/          ← 8 services
│   │   │   ├── 📁 WebSanPham.Services.Payment/
│   │   │   ├── 📁 WebSanPham.Services.Inventory/
│   │   │   ├── 📁 WebSanPham.Services.Notification/
│   │   │   ├── 📁 WebSanPham.Services.Analytics/
│   │   │   ├── 📁 WebSanPham.Services.Search/
│   │   │   ├── 📁 WebSanPham.Services.Cache/
│   │   │   ├── 📁 WebSanPham.Services.Security/
│   │   │   └── 📁 WebSanPham.Services.Integration/
│   │   └── 📁 WebSanPham.Website/           ← Main application
│   ├── 📁 Templates/
│   └── 📁 test/
│       ├── 📁 WebSanPham.Tests.Unit/
│       ├── 📁 WebSanPham.Tests.Integration/
│       └── 📁 WebSanPham.Tests.Performance/
```

### 🏠 2. DỰ ÁN VỪA - WebSanPham Standard (Bỏ bớt modules/themes)

```
📁 WebSanPhamRepository/
├── 📁 WebSanPham/
│   ├── 📄 WebSanPham.sln (18 projects)
│   ├── 📁 build/                            ← Giữ nguyên
│   ├── 📁 docs/                             ← Giữ nguyên
│   ├── 📁 scripts/                          ← Giữ nguyên
│   ├── 📁 src/
│   │   ├── 📁 WebSanPham/                   ← Giữ nguyên
│   │   ├── 📁 WebSanPham.Modules/           ← 8 modules (bỏ bớt)
│   │   │   ├── 📁 WebSanPham.Products/
│   │   │   ├── 📁 WebSanPham.Categories/
│   │   │   ├── 📁 WebSanPham.Orders/
│   │   │   ├── 📁 WebSanPham.Customers/
│   │   │   ├── 📁 WebSanPham.Inventory/
│   │   │   ├── 📁 WebSanPham.Reviews/
│   │   │   ├── 📁 WebSanPham.Payment.Stripe/
│   │   │   └── 📁 WebSanPham.Email.SendGrid/
│   │   ├── 📁 WebSanPham.Themes/            ← 2 themes (bỏ bớt)
│   │   │   ├── 📁 WebSanPham.Storefront.Theme/
│   │   │   └── 📁 WebSanPham.Admin.Theme/
│   │   ├── 📁 WebSanPham.Services/          ← 3 services (bỏ bớt)
│   │   │   ├── 📁 WebSanPham.Services.Payment/
│   │   │   ├── 📁 WebSanPham.Services.Inventory/
│   │   │   └── 📁 WebSanPham.Services.Notification/
│   │   └── 📁 WebSanPham.Website/           ← Giữ nguyên
│   ├── 📁 Templates/                        ← Giữ nguyên
│   └── 📁 test/                             ← Giữ nguyên
│       ├── 📁 WebSanPham.Tests.Unit/
│       └── 📁 WebSanPham.Tests.Integration/
```

### 🏘️ 3. DỰ ÁN NHỎ - WebSanPham Simple (Bỏ bớt nhiều hơn)

```
📁 WebSanPhamRepository/
├── 📁 WebSanPham/
│   ├── 📄 WebSanPham.sln (8 projects)
│   ├── 📁 build/                            ← Giữ nguyên
│   ├── 📁 docs/                             ← Giữ nguyên  
│   ├── 📁 scripts/                          ← Giữ nguyên
│   ├── 📁 src/
│   │   ├── 📁 WebSanPham/                   ← Giữ nguyên
│   │   ├── 📁 WebSanPham.Modules/           ← 3 modules (bỏ nhiều)
│   │   │   ├── 📁 WebSanPham.Products/
│   │   │   ├── 📁 WebSanPham.Orders/
│   │   │   └── 📁 WebSanPham.Customers/
│   │   ├── 📁 WebSanPham.Themes/            ← 1 theme (bỏ nhiều)
│   │   │   └── 📁 WebSanPham.Storefront.Theme/
│   │   ├── 📁 WebSanPham.Services/          ← 1 service (bỏ nhiều)
│   │   │   └── 📁 WebSanPham.Services.Core/
│   │   └── 📁 WebSanPham.Website/           ← Giữ nguyên
│   ├── 📁 Templates/                        ← Giữ nguyên
│   └── 📁 test/                             ← Giữ nguyên
│       └── 📁 WebSanPham.Tests.Unit/
```

## 🎯 NGUYÊN TẮC "BỎ BỚT" THAY VÌ "THAY ĐỔI"

### ✅ LUÔN GIỮ NGUYÊN:
- **📁 Cấu trúc thư mục chính** (src, build, docs, scripts, test)
- **📁 Container folders** (Modules, Themes, Services)
- **📄 Global files** (.sln, Directory.Build.props, NuGet.config)
- **🎯 Naming conventions** 
- **📋 Solution folders** trong Visual Studio

### 📉 CHỈ BỎ BỚT:
- **🧩 Số lượng modules** (từ 15 → 8 → 3)
- **🎨 Số lượng themes** (từ 5 → 2 → 1)  
- **📚 Số lượng services** (từ 8 → 3 → 1)
- **🧪 Số lượng test projects** (từ 3 → 2 → 1)

### ❌ KHÔNG BAO GIỜ:
- **Thay đổi naming convention**
- **Bỏ container folders**
- **Thay đổi cấu trúc solution**
- **Dùng flat structure**

## 🎯 LỢI ÍCH CỦA NGUYÊN TẮC THỐNG NHẤT

### 👥 1. TEAM DEVELOPMENT
```
Developer A: "Tôi quen với cấu trúc này rồi!"
Developer B: "Module mới ở đâu?" → "Trong WebSanPham.Modules/"
Developer C: "Theme ở đâu?" → "Trong WebSanPham.Themes/"
```

### 📈 2. SCALABILITY
```
Dự án nhỏ (3 modules) → Dự án lớn (15 modules)
✅ Chỉ cần thêm modules vào WebSanPham.Modules/
✅ Không cần thay đổi cấu trúc
✅ Không cần học lại
```

### 🔧 3. TOOLING & AUTOMATION
```
Build script: "Build tất cả projects trong src/"
Deploy script: "Deploy WebSanPham.Website + dependencies"
Test script: "Run tất cả tests trong test/"
```

### 📚 4. DOCUMENTATION & TRAINING
```
Một bộ tài liệu cho tất cả dự án
Một training cho tất cả developers
Một set of best practices
```

## 🎯 TEMPLATE FILES CHUẨN

### 📄 Directory.Build.props (Áp dụng cho tất cả projects)
```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <Company>YourCompany</Company>
    <Product>$(MSBuildProjectName)</Product>
    <Version>1.0.0</Version>
  </PropertyGroup>
</Project>
```

### 📄 Directory.Packages.props (Central Package Management)
```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
  
  <ItemGroup>
    <PackageVersion Include="OrchardCore.Application.Cms.Targets" Version="2.2.1" />
    <PackageVersion Include="Microsoft.AspNetCore.Mvc" Version="8.0.0" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore" Version="8.0.0" />
  </ItemGroup>
</Project>
```

### 📄 global.json (SDK Version)
```json
{
  "sdk": {
    "version": "8.0.100",
    "rollForward": "latestMinor"
  }
}
```

## 🎯 VISUAL STUDIO SOLUTION STRUCTURE

### 📋 Solution Folders (Luôn giống nhau)
```
📁 Solution '{ProjectName}' (X projects)
├── 📁 External Sources
├── 📁 build
├── 📁 docs
├── 📁 scripts
├── 📁 Solution Items
│   ├── 📄 .gitignore
│   ├── 📄 README.md
│   ├── 📄 Directory.Build.props
│   ├── 📄 Directory.Packages.props
│   ├── 📄 NuGet.config
│   └── 📄 global.json
├── 📁 src
│   ├── 📁 {ProjectName}
│   ├── 📁 {ProjectName}.Modules
│   ├── 📁 {ProjectName}.Themes
│   ├── 📁 {ProjectName}.Services
│   └── 🌐 {ProjectName}.Website
├── 📁 Templates
└── 📁 test
```

## 🎯 KẾT LUẬN

### **🏆 NGUYÊN TẮC VÀNG:**

1. **📏 MỘT CHUẨN CHO TẤT CẢ** - Từ dự án nhỏ đến lớn
2. **📉 BỎ BỚT THAY VÌ THAY ĐỔI** - Giữ nguyên cấu trúc, chỉ giảm số lượng
3. **🎯 CONSISTENCY IS KING** - Nhất quán trong mọi dự án
4. **📈 THINK BIG, START SMALL** - Chuẩn bị cho tương lai từ đầu
5. **👥 TEAM-FRIENDLY** - Dễ onboarding và collaboration

### **✅ KHUYẾN NGHỊ CUỐI CÙNG:**

**LUÔN SỬ DỤNG CẤU TRÚC ORCHARDCORE STANDARD**
- Dự án nhỏ: Bỏ bớt modules/themes
- Dự án lớn: Thêm modules/themes
- Cấu trúc: Không đổi
- Naming: Không đổi
- Solution folders: Không đổi

**"Một lần thiết kế, sử dụng mãi mãi!"** 🚀

---
*Nguyên tắc này đảm bảo consistency và scalability cho mọi dự án OrchardCore*
*Cập nhật: 2024-10-24*