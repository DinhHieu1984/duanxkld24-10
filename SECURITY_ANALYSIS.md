# 🔒 **ORCHARDCORE SECURITY & PERMISSIONS ANALYSIS**

## 📋 **OrchardCore Security Pattern Analysis**

### **1. IPermissionProvider Interface Pattern**
```csharp
public class Permissions : IPermissionProvider
{
    // Define static permissions
    public static readonly Permission ManageJobOrders = new Permission("ManageJobOrders", "Manage Job Orders");
    public static readonly Permission ViewJobOrders = new Permission("ViewJobOrders", "View Job Orders");
    
    // Return all permissions
    public Task<IEnumerable<Permission>> GetPermissionsAsync()
    {
        return Task.FromResult(new[] { ManageJobOrders, ViewJobOrders }.AsEnumerable());
    }
    
    // Define default stereotypes (roles)
    public IEnumerable<PermissionStereotype> GetDefaultStereotypes()
    {
        return new[]
        {
            new PermissionStereotype
            {
                Name = "Administrator",
                Permissions = new[] { ManageJobOrders, ViewJobOrders }
            },
            new PermissionStereotype
            {
                Name = "Editor",
                Permissions = new[] { ViewJobOrders }
            }
        };
    }
}
```

### **2. Permission Naming Convention**
- **Format**: `{Action}{ContentType}` hoặc `{Action}{Module}`
- **Examples**:
  - `ManageJobOrders` - Quản lý đơn hàng
  - `ViewJobOrders` - Xem đơn hàng
  - `ApplyJobOrders` - Ứng tuyển đơn hàng
  - `EditNews` - Chỉnh sửa tin tức
  - `PublishNews` - Xuất bản tin tức

### **3. Security Critical Permissions**
Các permissions có thể nâng cao quyền hạn user:
- `ManageUsers` - Quản lý users
- `ManageRoles` - Quản lý roles
- `ManageRecipes` - Quản lý recipes
- `EditContentTypes` - Chỉnh sửa content types

### **4. Role-based Authorization**
```csharp
// Trong Controller hoặc DisplayDriver
if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageJobOrders))
{
    return Forbid();
}

// Trong Liquid templates
{% if has_permission: "ManageJobOrders" %}
    <a href="/admin/joborders">Manage Job Orders</a>
{% endif %}
```

---

## 🎯 **SECURITY IMPLEMENTATION PLAN CHO 7 MODULES**

### **Module 1: NhanViet.JobOrders**
**Permissions cần tạo:**
- `ManageJobOrders` - Quản lý đơn hàng (Admin, HR Manager)
- `ViewJobOrders` - Xem đơn hàng (Editor, Consultant)
- `ApplyJobOrders` - Ứng tuyển đơn hàng (User, Authenticated)
- `EditJobOrders` - Chỉnh sửa đơn hàng (Admin, HR Manager)
- `PublishJobOrders` - Xuất bản đơn hàng (Admin, Editor)

### **Module 2: NhanViet.News**
**Permissions cần tạo:**
- `ManageNews` - Quản lý tin tức (Admin, Editor)
- `ViewNews` - Xem tin tức (All users)
- `EditNews` - Chỉnh sửa tin tức (Admin, Editor)
- `PublishNews` - Xuất bản tin tức (Admin, Editor)
- `DeleteNews` - Xóa tin tức (Admin)

### **Module 3: NhanViet.Companies**
**Permissions cần tạo:**
- `ManageCompanies` - Quản lý công ty (Admin)
- `ViewCompanies` - Xem thông tin công ty (All users)
- `EditCompanies` - Chỉnh sửa thông tin công ty (Admin, Editor)
- `ManageCompanyEvents` - Quản lý sự kiện công ty (Admin, HR Manager)

### **Module 4: NhanViet.Recruitment**
**Permissions cần tạo:**
- `ManageRecruitment` - Quản lý tuyển dụng (Admin, HR Manager)
- `ViewRecruitment` - Xem tin tuyển dụng (All users)
- `ApplyRecruitment` - Ứng tuyển (User, Authenticated)
- `ReviewApplications` - Xem xét hồ sơ (Admin, HR Manager)
- `ManageInterviews` - Quản lý phỏng vấn (Admin, HR Manager)

### **Module 5: NhanViet.Consultation**
**Permissions cần tạo:**
- `ManageConsultation` - Quản lý tư vấn (Admin, Consultant)
- `ViewConsultation` - Xem yêu cầu tư vấn (Consultant)
- `CreateConsultationRequest` - Tạo yêu cầu tư vấn (User, Authenticated)
- `AssignConsultation` - Phân công tư vấn (Admin)

### **Module 6: NhanViet.Countries**
**Permissions cần tạo:**
- `ManageCountries` - Quản lý quốc gia (Admin)
- `ViewCountries` - Xem thông tin quốc gia (All users)
- `EditCountries` - Chỉnh sửa thông tin quốc gia (Admin, Editor)

### **Module 7: NhanViet.Analytics**
**Permissions cần tạo:**
- `ManageAnalytics` - Quản lý thống kê (Admin)
- `ViewAnalytics` - Xem báo cáo (Admin, HR Manager, Consultant)
- `ExportAnalytics` - Xuất báo cáo (Admin, HR Manager)

---

## 🔐 **ROLE DEFINITIONS**

### **Administrator**
- Full system access
- All permissions across all modules
- User management, system configuration

### **HR Manager**
- ManageJobOrders, ManageRecruitment, ManageCompanyEvents
- ViewAnalytics, ExportAnalytics
- ReviewApplications, ManageInterviews

### **Editor**
- ManageNews, EditNews, PublishNews
- ViewJobOrders, EditCompanies
- EditCountries

### **Consultant**
- ManageConsultation, ViewConsultation
- ViewJobOrders, ViewAnalytics
- AssignConsultation (if senior consultant)

### **Authenticated User**
- ApplyJobOrders, ApplyRecruitment
- CreateConsultationRequest
- ViewNews, ViewCompanies, ViewCountries

### **Anonymous User**
- ViewNews, ViewCompanies, ViewCountries (public content only)

---

## 🛡️ **SECURITY BEST PRACTICES**

### **1. Principle of Least Privilege**
- Chỉ cấp quyền tối thiểu cần thiết
- Phân quyền theo chức năng cụ thể

### **2. Permission Granularity**
- Tách biệt View/Edit/Manage permissions
- Specific permissions cho từng action

### **3. Content-based Authorization**
```csharp
// Check ownership
if (jobOrder.CreatedBy != User.Identity.Name && 
    !await _authorizationService.AuthorizeAsync(User, Permissions.ManageJobOrders))
{
    return Forbid();
}
```

### **4. Audit Trail**
- Log tất cả security-critical actions
- Track permission changes
- Monitor failed authorization attempts

### **5. Input Validation**
- Validate tất cả user inputs
- Sanitize HTML content
- Prevent XSS/CSRF attacks

---

## 📁 **FILES CẦN TẠO**

### **Cho mỗi module (7 modules):**
1. `Permissions.cs` - Permission definitions
2. Update `Startup.cs` - Register IPermissionProvider
3. `Handlers/SecurityHandler.cs` - Authorization logic
4. Update `DisplayDrivers` - Add authorization checks

### **Shared Security:**
1. `Services/IAuthorizationService.cs` - Custom authorization logic
2. `Handlers/AuditTrailHandler.cs` - Security logging
3. `Middleware/SecurityMiddleware.cs` - Request validation

**Tổng files cần tạo**: ~28 files (4 per module + 3 shared)