# 📋 **KẾ HOẠCH TRIỂN KHAI NHANVIETGROUP.COM**
## Dự án xây dựng website xuất khẩu lao động bằng OrchardCore

---

## 🎯 **PHÂN TÍCH WEBSITE HIỆN TẠI**

**Website**: https://nhanvietgroup.com/  
**Loại hình**: Công ty xuất khẩu lao động  

### **Các dịch vụ chính**:
- ✅ **Xuất khẩu lao động**: Châu Âu (Đức, Hungary, Ba Lan, Pháp), Đài Loan, Nhật Bản
- ✅ **Quản lý đơn hàng**: Nông nghiệp, khách sạn, chế biến thực phẩm, xây dựng, điều dưỡng
- ✅ **Tin tức theo thị trường**: Nhật Bản, Đài Loan, Châu Âu
- ✅ **Hoạt động công ty**: Sự kiện, đào tạo, hội thảo  
- ✅ **Tuyển dụng nhân viên**: Tuyển dụng nội bộ
- ✅ **Form tư vấn khách hàng**: Đăng ký tư vấn trực tuyến

---

## 🏗️ **KIẾN TRÚC ORCHARDCORE THEO CHUẨN**

### **1. NAMING CONVENTION**
Áp dụng 100% quy tắc từ tài liệu hướng dẫn:

#### **Modules**: `NhanViet.{Function}[.{Technology}]`
- `NhanViet.JobOrders` ← Quản lý đơn hàng XKLĐ
- `NhanViet.News` ← Tin tức theo thị trường
- `NhanViet.Companies` ← Hoạt động công ty
- `NhanViet.Recruitment` ← Tuyển dụng nhân viên
- `NhanViet.Consultation` ← Tư vấn khách hàng
- `NhanViet.Countries` ← Quản lý quốc gia
- `NhanViet.Analytics` ← Thống kê báo cáo

#### **Themes**: `NhanViet.{Purpose}.Theme`
- `NhanViet.Frontend.Theme` ← Theme website chính
- `NhanViet.Admin.Theme` ← Theme quản trị admin

#### **Solution**: `NhanVietSolution`

### **2. CẤU TRÚC DỰ ÁN CHUẨN**
```
📁 NhanVietSolution/
├── 📁 src/
│   ├── 📁 NhanViet/                     ← Core framework
│   ├── 📁 NhanViet.Modules/             ← Container chứa modules
│   │   ├── 📁 NhanViet.JobOrders/           ← Quản lý đơn hàng XKLĐ
│   │   ├── 📁 NhanViet.News/                ← Tin tức
│   │   ├── 📁 NhanViet.Companies/           ← Hoạt động công ty
│   │   ├── 📁 NhanViet.Recruitment/         ← Tuyển dụng
│   │   ├── 📁 NhanViet.Consultation/        ← Tư vấn khách hàng
│   │   ├── 📁 NhanViet.Countries/           ← Quản lý quốc gia
│   │   └── 📁 NhanViet.Analytics/           ← Thống kê báo cáo
│   ├── 📁 NhanViet.Themes/              ← Container chứa themes
│   │   ├── 📁 NhanViet.Frontend.Theme/      ← Theme chính
│   │   └── 📁 NhanViet.Admin.Theme/         ← Theme admin
│   ├── 📁 NhanViet.Services/            ← Business services
│   └── 📁 NhanViet.Website/             ← Main application
├── 📁 test/
│   ├── 📁 NhanViet.Tests.Unit/
│   └── 📁 NhanViet.Tests.Integration/
├── 📁 build/
├── 📁 docs/
└── 📁 scripts/
```

---

## 📦 **KẾ HOẠCH MODULES CHI TIẾT**

### **PHÂN LOẠI ĐỘ PHỨC TẠP**
Dự án thuộc loại **TRUNG BÌNH-PHỨC TẠP** (12-14/16 bước)

### **Module 1: NhanViet.JobOrders (Đơn hàng XKLĐ)**
**Độ phức tạp**: 14/16 bước - **Module chính, phức tạp nhất**

#### **16 Bước Development:**
- ✅ **Bước 1**: Foundation Patterns (Manifest.cs, Startup.cs, .csproj)
- ✅ **Bước 2**: Content Management (JobOrder, JobCategory, JobApplication)
- ✅ **Bước 3**: Database & Indexing (Complex queries, reporting)
- ✅ **Bước 4**: Security & Permissions (Role-based access)
- ✅ **Bước 5**: Background Processing (Application processing, notifications)
- ✅ **Bước 6**: Performance & Caching (Job listings cache)
- ✅ **Bước 7**: Localization (Multi-language job posts)
- ✅ **Bước 8**: Testing (Business logic testing)
- ✅ **Bước 9**: API & GraphQL (Mobile app integration)
- ❌ **Bước 10**: Multi-tenancy (Không cần cho dự án này)
- ✅ **Bước 11**: Display Management (Job listing UI)
- ✅ **Bước 12**: Workflow (Application approval process)
- ✅ **Bước 13**: Media Management (Job images, documents)
- ✅ **Bước 14**: Search & Indexing (Job search, filtering)
- ✅ **Bước 15**: Deployment
- ❌ **Bước 16**: Advanced Patterns (Không cần cho MVP)

#### **Features chính:**
- Quản lý đơn hàng XKLĐ theo quốc gia
- Phân loại theo ngành nghề
- Workflow phê duyệt đơn hàng
- Tìm kiếm và lọc đơn hàng
- Báo cáo thống kê

### **Module 2: NhanViet.News (Tin tức)**
**Độ phức tạp**: 11/16 bước - **Module trung bình**

#### **Features chính:**
- Tin tức theo thị trường (Nhật Bản, Đài Loan, Châu Âu)
- Phân loại tin tức
- Đa ngôn ngữ (Việt, Anh, Nhật)
- SEO optimization
- Social media integration

### **Module 3: NhanViet.Companies (Hoạt động công ty)**
**Độ phức tạp**: 9/16 bước - **Module đơn giản**

#### **Features chính:**
- Quản lý sự kiện công ty
- Hội thảo, đào tạo
- Gallery hình ảnh
- Lịch sự kiện

### **Module 4: NhanViet.Recruitment (Tuyển dụng)**
**Độ phức tạp**: 12/16 bước - **Module trung bình**

#### **Features chính:**
- Đăng tin tuyển dụng
- Quản lý CV ứng viên
- Workflow phỏng vấn
- Báo cáo tuyển dụng

### **Module 5: NhanViet.Consultation (Tư vấn)**
**Độ phức tạp**: 10/16 bước - **Module trung bình**

#### **Features chính:**
- Form đăng ký tư vấn
- Quản lý yêu cầu tư vấn
- Email notifications
- CRM integration

---

## 🎨 **KẾ HOẠCH THEMES**

### **NhanViet.Frontend.Theme**
- **Framework**: Bootstrap 5
- **Responsive**: Desktop, Tablet, Mobile
- **Features**:
  - Homepage với slider
  - Danh sách đơn hàng XKLĐ
  - Trang tin tức
  - Form tư vấn
  - Trang hoạt động công ty
  - SEO optimized

### **NhanViet.Admin.Theme**
- **Framework**: AdminLTE hoặc tương tự
- **Features**:
  - Dashboard tổng quan
  - Quản lý đơn hàng
  - Quản lý tin tức
  - Quản lý users
  - Báo cáo thống kê

---

## 🗄️ **THIẾT KẾ DATABASE**

### **Content Types chính:**

#### **1. JobOrder (Đơn hàng XKLĐ)**
```csharp
- Title: string (Tên đơn hàng)
- Country: ContentPickerField (Quốc gia)
- Category: ContentPickerField (Ngành nghề)
- Quantity: int (Số lượng tuyển)
- Salary: decimal (Mức lương)
- Requirements: HtmlField (Yêu cầu)
- Benefits: HtmlField (Quyền lợi)
- Status: string (Trạng thái)
- ExpiryDate: DateTime (Hạn nộp hồ sơ)
- ContactInfo: TextField (Thông tin liên hệ)
```

#### **2. NewsArticle (Bài viết tin tức)**
```csharp
- Title: string
- Content: HtmlField
- Summary: TextField
- Category: ContentPickerField (Thị trường)
- FeaturedImage: MediaField
- PublishedDate: DateTime
- Author: string
- Tags: TagsField
```

#### **3. CompanyActivity (Hoạt động công ty)**
```csharp
- Title: string
- Description: HtmlField
- EventDate: DateTime
- Location: TextField
- Images: MediaField
- Category: string (Sự kiện, Đào tạo, Hội thảo)
```

#### **4. JobPosting (Tin tuyển dụng)**
```csharp
- Title: string
- Position: string
- Department: string
- Requirements: HtmlField
- Benefits: HtmlField
- Salary: string
- Location: string
- ExpiryDate: DateTime
```

#### **5. ConsultationRequest (Yêu cầu tư vấn)**
```csharp
- FullName: string
- Phone: string
- Email: string
- Content: TextField
- Status: string
- AssignedTo: string
- CreatedDate: DateTime
- ResponseDate: DateTime
```

### **Indexes tối ưu:**
- **JobOrder**: Country, Category, Status, CreatedDate, ExpiryDate
- **NewsArticle**: Category, PublishedDate, Featured, Tags
- **CompanyActivity**: EventDate, Category
- **JobPosting**: Department, Status, PostedDate, ExpiryDate
- **ConsultationRequest**: Status, CreatedDate, AssignedTo

---

## 🔒 **HỆ THỐNG BẢO MẬT**

### **Roles & Permissions:**

#### **SuperAdmin**
- Full system access
- User management
- System configuration

#### **Admin**
- Content management
- User management (limited)
- Reports access

#### **Editor**
- Create/edit content
- Manage media
- Publish content

#### **HR Manager**
- Manage recruitment
- View applications
- Generate HR reports

#### **Consultant**
- Manage consultation requests
- Customer communication
- View customer data

#### **User (Public)**
- View public content
- Submit consultation requests
- Apply for jobs

### **Security Features:**
- Role-based authorization
- Content permissions
- API authentication
- Data validation
- XSS/CSRF protection
- Audit logging

---

## 📊 **TRÌNH TỰ TRIỂN KHAI CHI TIẾT**

## 🎯 **PHASE 1: FOUNDATION SETUP (2 tuần)**

### **Tuần 1: Cấu trúc Solution**
#### **Ngày 1-2: Tạo Solution Structure**
```bash
# Tạo solution
dotnet new sln -n NhanVietSolution

# Tạo cấu trúc thư mục
mkdir -p src/NhanViet.Modules
mkdir -p src/NhanViet.Themes  
mkdir -p src/NhanViet.Services
mkdir -p test/NhanViet.Tests.Unit
mkdir -p test/NhanViet.Tests.Integration
mkdir -p build
mkdir -p docs
mkdir -p scripts
```

#### **Ngày 3-4: Setup Main Application**
```bash
# Tạo OrchardCore application
cd src
dotnet new orchardcore -n NhanViet.Website
dotnet sln ../NhanVietSolution.sln add NhanViet.Website
```

#### **Ngày 5: Configuration Files**
- `Directory.Build.props`
- `NuGet.config`
- `global.json`
- `.gitignore`
- `README.md`

### **Tuần 2: CI/CD Setup**
#### **Ngày 6-7: DevOps Setup**
- GitHub Actions workflow
- Docker configuration
- Database setup scripts

#### **Ngày 8-10: Development Environment**
- Local development setup
- Database migrations
- Initial testing

---

## 🧩 **PHASE 2: CORE MODULES DEVELOPMENT (6 tuần)**

### **Tuần 3-4: NhanViet.JobOrders Module (2 tuần)**

#### **Tuần 3: Foundation + Core Features**
**Ngày 1-2: Foundation Patterns**
```csharp
// Manifest.cs
[assembly: Module(
    Name = "NhanViet Job Orders",
    Author = "NhanViet Team",
    Website = "https://nhanvietgroup.com",
    Version = "1.0.0",
    Description = "Quản lý đơn hàng xuất khẩu lao động",
    Category = "Content Management"
)]

// Startup.cs - Đăng ký services
public class Startup : StartupBase
{
    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<IJobOrderService, JobOrderService>();
        services.AddContentPart<JobOrderPart>();
        services.AddContentType<JobOrder>();
    }
}
```

**Ngày 3-4: Content Management**
- JobOrder Content Type
- JobCategory Content Type  
- JobApplication Content Type
- Content Part Drivers

**Ngày 5: Database & Indexing**
- Migration scripts
- Index providers
- Query optimization

#### **Tuần 4: Advanced Features**
**Ngày 1-2: Security & Permissions**
```csharp
public class Permissions : IPermissionProvider
{
    public static readonly Permission ManageJobOrders = new Permission("ManageJobOrders", "Manage Job Orders");
    public static readonly Permission ViewJobOrders = new Permission("ViewJobOrders", "View Job Orders");
    public static readonly Permission ApplyJobOrders = new Permission("ApplyJobOrders", "Apply Job Orders");
}
```

**Ngày 3: Background Processing**
- Job notification services
- Email templates
- Queue processing

**Ngày 4: Performance & Caching**
- Cache strategies
- Performance optimization

**Ngày 5: Testing**
- Unit tests
- Integration tests

### **Tuần 5: NhanViet.News Module (1 tuần)**
**Ngày 1-2: Foundation + Content Management**
**Ngày 3: Security + Display Management**  
**Ngày 4: Localization + API**
**Ngày 5: Testing + Deployment**

### **Tuần 6: NhanViet.Companies Module (1 tuần)**
**Ngày 1-2: Foundation + Content Management**
**Ngày 3: Security + Display**
**Ngày 4: Media + Search**
**Ngày 5: Testing + Deployment**

### **Tuần 7: NhanViet.Recruitment Module (1 tuần)**
**Ngày 1-2: Foundation + Content + Database**
**Ngày 3: Security + Background Processing**
**Ngày 4: Workflow + API**
**Ngày 5: Testing + Deployment**

### **Tuần 8: NhanViet.Consultation Module (1 tuần)**
**Ngày 1-2: Foundation + Content Management**
**Ngày 3: Background Processing + Workflow**
**Ngày 4: API + Display**
**Ngày 5: Testing + Deployment**

---

## 🎨 **PHASE 3: THEMES DEVELOPMENT (3 tuần)**

### **Tuần 9-10: NhanViet.Frontend.Theme (2 tuần)**

#### **Tuần 9: Core Layout**
**Ngày 1-2: Base Layout**
```html
<!-- Layout.liquid -->
<!DOCTYPE html>
<html lang="{{ Culture.Name }}">
<head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1">
    <title>{% if Model.Title %}{{ Model.Title }} - {% endif %}{{ Site.SiteName }}</title>
    {% resources type: "Meta" %}
    {% resources type: "HeadLink" %}
    {% resources type: "Stylesheet" %}
</head>
<body>
    {% include "Header" %}
    <main>
        {{ Model.Content | shape_render }}
    </main>
    {% include "Footer" %}
    {% resources type: "FootScript" %}
</body>
</html>
```

**Ngày 3-4: Homepage Design**
- Hero slider
- Job listings section
- News section
- Company activities
- Consultation form

**Ngày 5: Responsive Design**
- Mobile optimization
- Tablet optimization

#### **Tuần 10: Pages & Components**
**Ngày 1-2: Job Pages**
- Job listing page
- Job detail page
- Job application form

**Ngày 3: News Pages**
- News listing
- News detail
- News categories

**Ngày 4: Company Pages**
- Activities listing
- Activity detail
- About page

**Ngày 5: Forms & Interactions**
- Consultation form
- Contact forms
- Search functionality

### **Tuần 11: NhanViet.Admin.Theme (1 tuần)**
**Ngày 1-2: Admin Layout & Dashboard**
**Ngày 3: Content Management UI**
**Ngày 4: User Management UI**
**Ngày 5: Reports & Analytics UI**

---

## 🔧 **PHASE 4: INTEGRATION & OPTIMIZATION (2 tuần)**

### **Tuần 12: Module Integration**
**Ngày 1-2: Cross-module Dependencies**
- Shared services integration
- Data consistency
- Event handling

**Ngày 3-4: Workflow Integration**
- Job application workflow
- Consultation workflow
- Approval processes

**Ngày 5: API Integration**
- REST API endpoints
- GraphQL schema
- API documentation

### **Tuần 13: Performance Optimization**
**Ngày 1-2: Database Optimization**
- Query optimization
- Index tuning
- Connection pooling

**Ngày 3-4: Caching Implementation**
- Output caching
- Data caching
- CDN integration

**Ngày 5: Load Testing**
- Performance testing
- Stress testing
- Optimization

---

## 🔒 **PHASE 5: SECURITY & TESTING (1 tuần)**

### **Tuần 14: Security & Testing**
**Ngày 1-2: Security Implementation**
- Authentication setup
- Authorization policies
- Data validation
- Security audit

**Ngày 3-4: Comprehensive Testing**
- Unit test completion
- Integration testing
- UI testing
- Performance testing

**Ngày 5: Security Testing**
- Penetration testing
- Vulnerability assessment
- Security fixes

---

## 🚀 **PHASE 6: DEPLOYMENT & GO-LIVE (1 tuần)**

### **Tuần 15: Production Deployment**
**Ngày 1-2: Production Environment**
- Server setup
- Database setup
- SSL certificates
- Domain configuration

**Ngày 3-4: Deployment & Migration**
- Application deployment
- Database migration
- Content migration
- Final testing

**Ngày 5: Go-Live**
- Production launch
- Monitoring setup
- Support documentation
- User training

---

## 📊 **TỔNG KẾT DỰ ÁN**

### **Timeline Summary:**
| Phase | Thời gian | Nội dung chính | Deliverables |
|-------|-----------|----------------|--------------|
| **Phase 1** | 2 tuần | Foundation Setup | Solution structure, CI/CD |
| **Phase 2** | 6 tuần | Core Modules | 5 modules hoàn chỉnh |
| **Phase 3** | 3 tuần | Themes | Frontend + Admin themes |
| **Phase 4** | 2 tuần | Integration & Optimization | Integrated system |
| **Phase 5** | 1 tuần | Security & Testing | Tested & secured system |
| **Phase 6** | 1 tuần | Deployment & Go-Live | Production website |
| **TỔNG** | **15 tuần** | **Complete System** | **Enterprise Website** |

### **Team Requirements:**
- **1 Solution Architect** (Part-time)
- **2 Senior .NET Developers** (Full-time)
- **1 Frontend Developer** (Full-time)
- **1 UI/UX Designer** (Part-time)
- **1 DevOps Engineer** (Part-time)
- **1 QA Tester** (Part-time)

### **Technology Stack:**
- **Backend**: OrchardCore CMS, .NET 8, C#
- **Frontend**: Bootstrap 5, jQuery, Liquid Templates
- **Database**: SQL Server / PostgreSQL
- **Caching**: Redis
- **Search**: Elasticsearch (optional)
- **CI/CD**: GitHub Actions
- **Hosting**: Azure / AWS

### **Key Features Delivered:**
- ✅ **5 Core Modules** theo chuẩn OrchardCore
- ✅ **2 Professional Themes** (Frontend + Admin)
- ✅ **Multi-language Support** (Việt, Anh, Nhật)
- ✅ **Mobile Responsive Design**
- ✅ **SEO Optimized**
- ✅ **Role-based Security**
- ✅ **Performance Optimized**
- ✅ **API Ready**
- ✅ **Comprehensive Testing**
- ✅ **Production Ready**

### **Success Metrics:**
- **Performance**: Page load < 3 seconds
- **Availability**: 99.9% uptime
- **Security**: Zero critical vulnerabilities
- **SEO**: Google PageSpeed > 90
- **Mobile**: 100% responsive
- **Testing**: 90%+ code coverage

---

## 🎯 **KẾT LUẬN**

Dự án **NhanVietGroup.com** sẽ được xây dựng **100% theo chuẩn OrchardCore professional** với:

### **✅ Áp dụng hoàn toàn tài liệu hướng dẫn:**
1. **Quy tắc đặt tên**: NhanViet.{Function}[.{Technology}]
2. **Cấu trúc dự án**: Container folders chuẩn
3. **16 bước phát triển**: Áp dụng đúng cho từng module
4. **Foundation patterns**: 3 files bắt buộc cho mỗi module
5. **Best practices**: Security, Performance, Testing, Deployment

### **✅ Kết quả mong đợi:**
- **Website enterprise-grade** cho công ty xuất khẩu lao động
- **Scalable architecture** có thể mở rộng
- **Professional codebase** dễ maintain
- **High performance** và security
- **Mobile-first responsive design**

### **🚀 READY TO IMPLEMENT!**

**Dự án đã sẵn sàng triển khai với kế hoạch chi tiết 15 tuần, đảm bảo chất lượng enterprise và tuân thủ 100% chuẩn OrchardCore!**

---

*Tài liệu được tạo ngày: {{ "now" | date: "%d/%m/%Y" }}*  
*Phiên bản: 1.0*  
*Tác giả: NhanViet Development Team*