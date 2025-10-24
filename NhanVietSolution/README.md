# NhanViet Group - Labor Export Website

## Dự án Website Xuất Khẩu Lao Động NhanVietGroup.com

Đây là dự án website xuất khẩu lao động được xây dựng trên nền tảng **OrchardCore CMS** với .NET 8.0.

### Cấu trúc dự án

- **NhanViet.Website**: Ứng dụng web chính (OrchardCore CMS)
- **NhanViet.JobOrders**: Module quản lý đơn hàng việc làm
- **NhanViet.News**: Module quản lý tin tức
- **NhanViet.Companies**: Module quản lý công ty
- **NhanViet.Recruitment**: Module quản lý tuyển dụng
- **NhanViet.Consultation**: Module tư vấn
- **NhanViet.Countries**: Module quản lý quốc gia
- **NhanViet.Analytics**: Module phân tích và báo cáo
- **NhanViet.Frontend.Theme**: Theme giao diện người dùng
- **NhanViet.Admin.Theme**: Theme giao diện quản trị

### Yêu cầu hệ thống

- .NET 8.0 SDK
- OrchardCore 2.2.1
- SQLite (mặc định) hoặc SQL Server/MySQL/PostgreSQL

### Cài đặt và chạy

1. Clone repository:
```bash
git clone https://github.com/DinhHieu1984/duanxkld24-10.git
cd duanxkld24-10/NhanVietSolution
```

2. Restore packages:
```bash
dotnet restore
```

3. Build solution:
```bash
dotnet build
```

4. Chạy ứng dụng:
```bash
cd NhanViet.Website
dotnet run
```

5. Mở trình duyệt và truy cập: `https://localhost:5001`

### Cấu hình ban đầu

Khi chạy lần đầu, OrchardCore sẽ hiển thị trang Setup để cấu hình:
- Tên website
- Recipe (chọn "Software as a Service")
- Múi giờ
- Loại database
- Tài khoản Super User

### Phát triển

Dự án được tổ chức theo kiến trúc modular của OrchardCore:
- Mỗi module có thể phát triển độc lập
- Sử dụng Dependency Injection
- Hỗ trợ multi-tenancy
- Có thể mở rộng và tùy chỉnh dễ dàng

### Tài liệu tham khảo

- [OrchardCore Documentation](https://docs.orchardcore.net/)
- [OrchardCore GitHub](https://github.com/OrchardCMS/OrchardCore)