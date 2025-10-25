# NhanViet Solution - Security & Permissions Guide

## Tổng quan

Dự án NhanViet Solution đã được implement đầy đủ Security & Permissions system theo chuẩn OrchardCore, bao gồm:

- **7 modules** với Permissions system hoàn chỉnh
- **Authorization Handlers** cho từng module
- **Controller & DisplayDriver** integration
- **Test cases** để validate functionality

## Cấu trúc Security Implementation

### 1. Permissions System

Mỗi module có file `Permissions.cs` định nghĩa các permissions:

```csharp
// Ví dụ: NhanViet.JobOrders/Permissions.cs
public static class Permissions
{
    public static readonly Permission ViewJobOrders = new("ViewJobOrders", "Xem danh sách Job Orders", "JobOrders");
    public static readonly Permission EditJobOrders = new("EditJobOrders", "Chỉnh sửa Job Orders", "JobOrders");
    public static readonly Permission DeleteJobOrders = new("DeleteJobOrders", "Xóa Job Orders", "JobOrders");
    // ... thêm permissions khác
}
```

### 2. Authorization Handlers

Mỗi module có `Authorization/[Module]AuthorizationHandler.cs`:

```csharp
public class JobOrderAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context, 
        PermissionRequirement requirement)
    {
        // Logic kiểm tra permissions dựa trên user roles và resource
        // Tuân thủ OrchardCore patterns
    }
}
```

### 3. Startup Registration

Tất cả modules đã đăng ký IPermissionProvider và IAuthorizationHandler:

```csharp
// Trong Startup.cs của mỗi module
services.AddScoped<IPermissionProvider, Permissions>();
services.AddScoped<IAuthorizationHandler, [Module]AuthorizationHandler>();
```

## Danh sách Modules & Permissions

### 1. NhanViet.JobOrders
- **ViewJobOrders**: Xem danh sách Job Orders
- **EditJobOrders**: Chỉnh sửa Job Orders  
- **DeleteJobOrders**: Xóa Job Orders
- **ManageJobOrders**: Quản lý toàn bộ Job Orders
- **ApplyJobOrders**: Ứng tuyển Job Orders
- **PublishJobOrders**: Xuất bản Job Orders
- **ViewJobOrderReports**: Xem báo cáo Job Orders
- **ExportJobOrderReports**: Xuất báo cáo Job Orders

### 2. NhanViet.News
- **ViewNews**: Xem tin tức
- **EditNews**: Chỉnh sửa tin tức
- **DeleteNews**: Xóa tin tức
- **ManageNews**: Quản lý toàn bộ tin tức
- **PublishNews**: Xuất bản tin tức
- **ViewNewsReports**: Xem báo cáo tin tức
- **ExportNewsReports**: Xuất báo cáo tin tức

### 3. NhanViet.Companies
- **ViewCompanies**: Xem danh sách công ty
- **EditCompanies**: Chỉnh sửa thông tin công ty
- **DeleteCompanies**: Xóa công ty
- **ManageCompanies**: Quản lý toàn bộ công ty
- **PublishCompanies**: Xuất bản thông tin công ty
- **ViewCompanyReports**: Xem báo cáo công ty
- **ExportCompanyReports**: Xuất báo cáo công ty

### 4. NhanViet.Recruitment
- **ViewRecruitment**: Xem thông tin tuyển dụng
- **EditRecruitment**: Chỉnh sửa thông tin tuyển dụng
- **DeleteRecruitment**: Xóa thông tin tuyển dụng
- **ManageRecruitment**: Quản lý toàn bộ tuyển dụng
- **ApplyRecruitment**: Ứng tuyển
- **PublishRecruitment**: Xuất bản thông tin tuyển dụng
- **ViewRecruitmentReports**: Xem báo cáo tuyển dụng
- **ExportRecruitmentReports**: Xuất báo cáo tuyển dụng

### 5. NhanViet.Consultation
- **ViewConsultation**: Xem dịch vụ tư vấn
- **EditConsultation**: Chỉnh sửa dịch vụ tư vấn
- **DeleteConsultation**: Xóa dịch vụ tư vấn
- **ManageConsultation**: Quản lý toàn bộ tư vấn
- **RequestConsultation**: Yêu cầu tư vấn
- **PublishConsultation**: Xuất bản dịch vụ tư vấn
- **ViewConsultationReports**: Xem báo cáo tư vấn
- **ExportConsultationReports**: Xuất báo cáo tư vấn

### 6. NhanViet.Countries
- **ViewCountries**: Xem danh sách quốc gia
- **EditCountries**: Chỉnh sửa thông tin quốc gia
- **DeleteCountries**: Xóa quốc gia
- **ManageCountries**: Quản lý toàn bộ quốc gia
- **PublishCountries**: Xuất bản thông tin quốc gia
- **ViewCountryReports**: Xem báo cáo quốc gia
- **ExportCountryReports**: Xuất báo cáo quốc gia

### 7. NhanViet.Analytics
- **ViewAnalytics**: Xem phân tích dữ liệu
- **EditAnalytics**: Chỉnh sửa phân tích
- **DeleteAnalytics**: Xóa phân tích
- **ManageAnalytics**: Quản lý toàn bộ phân tích
- **PublishAnalytics**: Xuất bản phân tích
- **ViewAnalyticsReports**: Xem báo cáo phân tích
- **ExportAnalyticsReports**: Xuất báo cáo phân tích

## Cách sử dụng trong Code

### 1. Trong Controller

```csharp
[HttpGet]
public async Task<IActionResult> Index()
{
    // Kiểm tra permission
    if (!await _authorizationService.AuthorizeAsync(User, Permissions.ViewJobOrders))
    {
        return Forbid();
    }
    
    // Logic xử lý...
    return View();
}

[HttpPost]
public async Task<IActionResult> Delete(string contentItemId)
{
    var contentItem = await _contentManager.GetAsync(contentItemId);
    
    // Kiểm tra permission với resource cụ thể
    if (!await _authorizationService.AuthorizeAsync(User, Permissions.DeleteJobOrders, contentItem))
    {
        return Forbid();
    }
    
    // Logic xóa...
    return RedirectToAction(nameof(Index));
}
```

### 2. Trong DisplayDriver

```csharp
public override async Task<IDisplayResult> DisplayAsync(JobOrderPart jobOrderPart, BuildPartDisplayContext context)
{
    var user = _httpContextAccessor.HttpContext?.User;
    
    // Kiểm tra permission trước khi hiển thị
    if (user != null && !await _authorizationService.AuthorizeAsync(user, Permissions.ViewJobOrders, jobOrderPart.ContentItem))
    {
        return null; // Không hiển thị nếu không có permission
    }

    return Initialize<JobOrderPartViewModel>(GetDisplayShapeType(context), m => BuildViewModel(m, jobOrderPart));
}
```

### 3. Trong Razor Views

```html
@if (await AuthorizeAsync(Permissions.EditJobOrders))
{
    <a href="@Url.Action("Edit", new { id = Model.ContentItemId })" class="btn btn-primary">
        Chỉnh sửa
    </a>
}

@if (await AuthorizeAsync(Permissions.DeleteJobOrders))
{
    <button type="submit" class="btn btn-danger" onclick="return confirm('Bạn có chắc muốn xóa?')">
        Xóa
    </button>
}
```

## Role-based Authorization

Authorization Handlers đã được cấu hình với các roles phù hợp:

### Administrator
- Có tất cả permissions cho tất cả modules
- Có thể xóa, chỉnh sửa, quản lý mọi content

### Editor  
- Có thể chỉnh sửa và xuất bản content
- Không thể xóa content

### Manager
- Có thể xem báo cáo và phân tích
- Có thể quản lý trong phạm vi chuyên môn

### Specialized Roles
- **HR Manager**: Quản lý JobOrders và Recruitment
- **Consultant**: Quản lý Consultation services
- **Analyst**: Quản lý Analytics và Reports
- **DataManager**: Quản lý Countries và master data

### Authenticated Users
- Có thể xem content công khai
- Có thể ứng tuyển và yêu cầu tư vấn

### Anonymous Users
- Chỉ có thể xem Countries (public data)
- Không có quyền truy cập các module khác

## Testing

Đã tạo test cases trong `NhanViet.JobOrders/Tests/SecurityTests.cs` để validate:

- Authorization Handler logic
- Permission definitions
- Role-based access control
- Anonymous vs Authenticated user access

## Tuân thủ OrchardCore Patterns

Implementation này tuân thủ 100% các patterns của OrchardCore:

1. **IPermissionProvider pattern**: Đăng ký permissions theo chuẩn OrchardCore
2. **AuthorizationHandler<PermissionRequirement>**: Sử dụng base class chuẩn
3. **Resource-based authorization**: Kiểm tra permissions với ContentItem cụ thể
4. **Role-based authorization**: Sử dụng User.IsInRole() pattern
5. **Dependency Injection**: Đăng ký services theo chuẩn OrchardCore
6. **Async patterns**: Sử dụng async/await đúng cách

## Kết luận

Security & Permissions system đã được implement hoàn chỉnh cho tất cả 7 modules của NhanViet Solution, đảm bảo:

- ✅ **Bảo mật**: Kiểm tra permissions ở mọi layer (Controller, DisplayDriver, View)
- ✅ **Linh hoạt**: Role-based và resource-based authorization
- ✅ **Chuẩn OrchardCore**: Tuân thủ 100% patterns và best practices
- ✅ **Testable**: Có test cases để validate functionality
- ✅ **Maintainable**: Code structure rõ ràng, dễ bảo trì và mở rộng

Hệ thống này sẵn sàng cho production và có thể dễ dàng mở rộng thêm permissions hoặc roles mới khi cần thiết.