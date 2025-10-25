# So Sánh Data Flow Testing với Cấu Trúc OrchardCore Chuẩn

## 📊 **TỔNG QUAN SO SÁNH**

### ✅ **ĐIỂM GIỐNG VỚI CHUẨN ORCHARDCORE**

#### 1. Framework và Tools
- **xUnit Framework**: ✅ Đúng chuẩn OrchardCore
- **Moq Library**: ✅ Đúng chuẩn cho mocking
- **Test Naming Convention**: ✅ Descriptive names theo pattern `ClassName_Scenario_ExpectedResult`
- **.NET 8.0 Target**: ✅ Phù hợp với OrchardCore hiện tại

#### 2. Test Structure Cơ Bản
- **Unit Tests Folder**: ✅ Có cấu trúc `/Unit/` folder
- **Test Categories**: ✅ Sử dụng `[Trait("Category", "Unit")]`
- **Fact và Theory Tests**: ✅ Sử dụng đúng xUnit attributes

#### 3. Content Part Testing
- **Property Testing**: ✅ Test setters/getters của ContentPart properties
- **Business Logic Testing**: ✅ Test increment operations, validation logic
- **Data Validation**: ✅ Test email validation, required fields

---

## ❌ **ĐIỂM KHÁC BIỆT VỚI CHUẨN ORCHARDCORE**

### 1. **Thiếu OrchardTestFixture Pattern**

**Chuẩn OrchardCore:**
```csharp
public class OrchardTestFixture<TStartup> : WebApplicationFactory<TStartup>
    where TStartup : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Clean slate cho mỗi test
        var shellsApplicationDataPath = Path.Combine(
            Directory.GetCurrentDirectory(), "App_Data");
        if (Directory.Exists(shellsApplicationDataPath))
        {
            Directory.Delete(shellsApplicationDataPath, true);
        }
        builder.UseContentRoot(Directory.GetCurrentDirectory());
    }
}
```

**Implementation hiện tại:**
```csharp
// ❌ THIẾU: Không có WebApplicationFactory pattern
// Chỉ có simple unit tests không có OrchardCore context
```

### 2. **Thiếu SiteContext Pattern (Tenant Isolation)**

**Chuẩn OrchardCore:**
```csharp
public class SiteContext : IDisposable
{
    public async Task UsingTenantScopeAsync(Func<IServiceScope, Task> execute)
    {
        var shellScope = await ShellHost.GetScopeAsync(TenantName);
        await using (shellScope)
        {
            await execute(shellScope);
        }
    }
}
```

**Implementation hiện tại:**
```csharp
// ❌ THIẾU: Không có tenant isolation
// Không test trong OrchardCore context thực tế
```

### 3. **Thiếu Content Testing Pattern Chuẩn**

**Chuẩn OrchardCore:**
```csharp
[Fact]
public void ContentPart_Serialization_PreservesTypeInformation()
{
    // Arrange - Create content with parts
    var contentItem = new ContentItem();
    var titlePart = new TitlePart { Title = "Test Title" };
    contentItem.Weld(titlePart);
    
    // Act - Test serialization/deserialization
    var json = JConvert.SerializeObject(contentItem);
    var deserializedItem = JConvert.DeserializeObject<ContentItem>(json);
    
    // Assert - Verify casting behavior
    var basePart = deserializedItem.Get<ContentPart>(nameof(TitlePart));
    var concretePart = deserializedItem.Get<TitlePart>(nameof(TitlePart));
    
    Assert.NotNull(basePart);
    Assert.NotNull(concretePart);
    Assert.NotSame(basePart, concretePart);
}
```

**Implementation hiện tại:**
```csharp
// ❌ THIẾU: Không test ContentItem.Weld()
// ❌ THIẾU: Không test serialization/deserialization
// ❌ THIẾU: Không test ContentItem.Get<T>() methods
```

### 4. **Thiếu Test Project Structure Chuẩn**

**Chuẩn OrchardCore:**
```
MyModule.Tests/
├── MyModule.Tests.csproj
├── Unit/
│   ├── Parts/           ← ContentPart tests
│   ├── Drivers/         ← DisplayDriver tests  
│   ├── Handlers/        ← ContentHandler tests
│   └── Services/        ← Service tests
├── Integration/         ← Integration tests
│   ├── StartupTests.cs
│   ├── MigrationTests.cs
│   └── PermissionTests.cs
├── Functional/          ← E2E tests
└── Stubs/              ← Mock objects
    ├── StubMyRepository.cs
    └── StubMyService.cs
```

**Implementation hiện tại:**
```
NhanViet.Tests/
├── NhanViet.Tests.Simple.csproj
└── Unit/
    └── JobOrders/       ← Chỉ có JobOrder tests
        └── JobOrderPartCleanTests.cs
        
❌ THIẾU: Drivers/, Handlers/, Services/ folders
❌ THIẾU: Integration/ folder  
❌ THIẾU: Functional/ folder
❌ THIẾU: Stubs/ folder
❌ THIẾU: Tests cho Companies, News modules
```

### 5. **Thiếu Display Driver Testing**

**Chuẩn OrchardCore:**
```csharp
[Fact]
public async Task MyPartDisplayDriver_Display_ReturnsCorrectShape()
{
    // Arrange - Mock dependencies
    var mockLocalizer = new Mock<IStringLocalizer<MyPartDisplayDriver>>();
    var driver = new MyPartDisplayDriver(mockLocalizer.Object);
    var part = new MyContentPart { MyProperty = "test" };
    var context = new BuildPartDisplayContext(part, "", "", new ShapeFactory());
    
    // Act
    var result = driver.Display(part, context);
    
    // Assert
    Assert.NotNull(result);
}
```

**Implementation hiện tại:**
```csharp
// ❌ THIẾU: Không có DisplayDriver tests
// ❌ THIẾU: Không test shape rendering
// ❌ THIẾU: Không test editor contexts
```

### 6. **Thiếu Handler Testing**

**Chuẩn OrchardCore:**
```csharp
[Fact]
public async Task MyContentHandler_Creating_SetsDefaultValues()
{
    // Arrange
    var handler = new MyContentHandler();
    var context = new CreateContentContext(new ContentItem { ContentType = "MyType" });
    
    // Act
    await handler.CreatingAsync(context);
    
    // Assert
    var part = context.ContentItem.As<MyContentPart>();
    Assert.NotNull(part);
    Assert.Equal("default", part.MyProperty);
}
```

**Implementation hiện tại:**
```csharp
// ❌ THIẾU: Không có ContentHandler tests
// ❌ THIẾU: Không test lifecycle events (Creating, Created, etc.)
```

### 7. **Thiếu Integration Tests**

**Chuẩn OrchardCore:**
```csharp
[Fact]
public async Task MyModule_Startup_RegistersServicesCorrectly()
{
    var context = await GetSiteContextAsync();
    
    await context.UsingTenantScopeAsync(async scope =>
    {
        // Verify service registration
        var myService = scope.ServiceProvider.GetService<IMyService>();
        Assert.NotNull(myService);
        
        // Verify feature is enabled
        var shellFeaturesManager = scope.ServiceProvider
            .GetRequiredService<IShellFeaturesManager>();
        var features = await shellFeaturesManager.GetEnabledFeaturesAsync();
        
        Assert.Contains(features, f => f.Id == "MyModule");
    });
}
```

**Implementation hiện tại:**
```csharp
// ❌ THIẾU: Không có integration tests
// ❌ THIẾU: Không test service registration
// ❌ THIẾU: Không test module startup
// ❌ THIẾU: Không test database migrations
```

### 8. **Thiếu Test Project Configuration Chuẩn**

**Chuẩn OrchardCore:**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <!-- Core testing packages -->
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit.v3" />
    <PackageReference Include="Moq" />
    
    <!-- ASP.NET Core testing -->
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" />
    
    <!-- HTML parsing for UI tests -->
    <PackageReference Include="AngleSharp" />
  </ItemGroup>

  <ItemGroup>
    <!-- Reference to OrchardCore test infrastructure -->
    <ProjectReference Include="..\..\OrchardCore\test\OrchardCore.Tests\OrchardCore.Tests.csproj" />
  </ItemGroup>
</Project>
```

**Implementation hiện tại:**
```xml
<!-- ❌ THIẾU: Microsoft.AspNetCore.Mvc.Testing -->
<!-- ❌ THIẾU: AngleSharp for HTML parsing -->
<!-- ❌ THIẾU: OrchardCore.Tests project reference -->
<!-- ❌ THIẾU: Test configuration files (xunit.runner.json, appsettings.json) -->
```

---

## 🎯 **ĐỀ XUẤT CẢI THIỆN**

### **Phase 1: Cải Thiện Unit Tests**

#### 1. **Thêm Content Testing Pattern Chuẩn**
```csharp
[Fact]
public void JobOrderPart_ContentItemWeld_WorksCorrectly()
{
    // Arrange
    var contentItem = new ContentItem();
    var jobOrderPart = new JobOrderPart 
    { 
        JobTitle = "Software Engineer",
        CompanyName = "Tech Corp"
    };
    
    // Act
    contentItem.Weld(jobOrderPart);
    
    // Assert
    var retrievedPart = contentItem.As<JobOrderPart>();
    Assert.NotNull(retrievedPart);
    Assert.Equal("Software Engineer", retrievedPart.JobTitle);
    Assert.Equal("Tech Corp", retrievedPart.CompanyName);
}

[Fact]
public void JobOrderPart_Serialization_PreservesData()
{
    // Arrange
    var contentItem = new ContentItem();
    var jobOrderPart = new JobOrderPart { JobTitle = "Test Job" };
    contentItem.Weld(jobOrderPart);
    
    // Act
    var json = JConvert.SerializeObject(contentItem);
    var deserializedItem = JConvert.DeserializeObject<ContentItem>(json);
    
    // Assert
    var deserializedPart = deserializedItem.As<JobOrderPart>();
    Assert.NotNull(deserializedPart);
    Assert.Equal("Test Job", deserializedPart.JobTitle);
}
```

#### 2. **Thêm Tests cho Tất Cả Modules**
```csharp
// CompanyPartTests.cs
[Fact]
public void CompanyPart_SetProperties_UpdatesCorrectly()
{
    var part = new CompanyPart();
    part.CompanyName = "Tech Corp";
    part.Industry = "Technology";
    
    Assert.Equal("Tech Corp", part.CompanyName);
    Assert.Equal("Technology", part.Industry);
}

// NewsPartTests.cs  
[Fact]
public void NewsPart_SetProperties_UpdatesCorrectly()
{
    var part = new NewsPart();
    part.Title = "Breaking News";
    part.Content = "Important announcement";
    
    Assert.Equal("Breaking News", part.Title);
    Assert.Equal("Important announcement", part.Content);
}
```

#### 3. **Thêm Display Driver Tests**
```csharp
[Fact]
public async Task JobOrderPartDisplayDriver_Display_ReturnsCorrectShape()
{
    // Arrange
    var mockLocalizer = new Mock<IStringLocalizer<JobOrderPartDisplayDriver>>();
    var driver = new JobOrderPartDisplayDriver(mockLocalizer.Object);
    var part = new JobOrderPart { JobTitle = "Test Job" };
    var context = new BuildPartDisplayContext(part, "", "", new ShapeFactory());
    
    // Act
    var result = await driver.DisplayAsync(part, context);
    
    // Assert
    Assert.NotNull(result);
    // Verify shape properties
}
```

### **Phase 2: Thêm Integration Tests**

#### 1. **Setup OrchardTestFixture**
```csharp
public class NhanVietTestFixture : OrchardTestFixture<SiteStartup>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        
        // Configure test-specific settings
        builder.ConfigureServices(services =>
        {
            // Add test services
        });
    }
}
```

#### 2. **Module Startup Tests**
```csharp
[Fact]
public async Task JobOrdersModule_Startup_RegistersServicesCorrectly()
{
    var context = await GetSiteContextAsync();
    
    await context.UsingTenantScopeAsync(async scope =>
    {
        // Verify JobOrders services are registered
        var contentDefinitionManager = scope.ServiceProvider
            .GetRequiredService<IContentDefinitionManager>();
        
        var jobOrderType = await contentDefinitionManager
            .GetTypeDefinitionAsync("JobOrder");
        
        Assert.NotNull(jobOrderType);
        Assert.Contains(jobOrderType.Parts, p => p.Name == "JobOrderPart");
    });
}
```

### **Phase 3: Cải Thiện Test Project Structure**

#### 1. **Tạo Cấu Trúc Folder Chuẩn**
```
NhanViet.Tests/
├── NhanViet.Tests.csproj
├── Unit/
│   ├── JobOrders/
│   │   ├── JobOrderPartTests.cs
│   │   ├── JobOrderPartDisplayDriverTests.cs
│   │   └── JobOrderHandlerTests.cs
│   ├── Companies/
│   │   ├── CompanyPartTests.cs
│   │   └── CompanyPartDisplayDriverTests.cs
│   └── News/
│       ├── NewsPartTests.cs
│       └── NewsPartDisplayDriverTests.cs
├── Integration/
│   ├── JobOrdersModuleTests.cs
│   ├── CompaniesModuleTests.cs
│   └── NewsModuleTests.cs
├── Functional/
│   └── cypress/
│       └── integration/
└── Stubs/
    ├── StubContentDefinitionManager.cs
    └── StubExtensionManager.cs
```

#### 2. **Cập Nhật Project Configuration**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <OutputType>Exe</OutputType>
  </PropertyGroup>

  <ItemGroup>
    <!-- Core testing packages -->
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
    <PackageReference Include="xunit" Version="2.6.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.5.3" />
    <PackageReference Include="Moq" Version="4.20.69" />
    
    <!-- ASP.NET Core testing -->
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="8.0.0" />
    
    <!-- HTML parsing -->
    <PackageReference Include="AngleSharp" Version="0.17.1" />
    
    <!-- OrchardCore testing -->
    <PackageReference Include="OrchardCore.ContentManagement" Version="1.8.2" />
    <PackageReference Include="OrchardCore.ContentTypes" Version="1.8.2" />
  </ItemGroup>

  <ItemGroup>
    <!-- Module references -->
    <ProjectReference Include="..\..\NhanViet.JobOrders\NhanViet.JobOrders.csproj" />
    <ProjectReference Include="..\..\NhanViet.Companies\NhanViet.Companies.csproj" />
    <ProjectReference Include="..\..\NhanViet.News\NhanViet.News.csproj" />
  </ItemGroup>

  <ItemGroup>
    <!-- Test configuration -->
    <None Include="xunit.runner.json" CopyToOutputDirectory="PreserveNewest" />
    <None Include="appsettings.json" CopyToOutputDirectory="PreserveNewest" />
  </ItemGroup>
</Project>
```

---

## 📈 **ROADMAP CẢI THIỆN**

### **Immediate (1-2 days)**
1. ✅ **COMPLETED**: Basic Unit Tests cho JobOrderPart
2. 🔄 **IN PROGRESS**: Thêm Content Testing Pattern chuẩn
3. 📋 **TODO**: Thêm Unit Tests cho CompanyPart và NewsPart

### **Short Term (1 week)**
4. 📋 **TODO**: Thêm Display Driver Tests
5. 📋 **TODO**: Thêm Handler Tests  
6. 📋 **TODO**: Setup Integration Test infrastructure

### **Medium Term (2 weeks)**
7. 📋 **TODO**: Implement OrchardTestFixture pattern
8. 📋 **TODO**: Add SiteContext pattern
9. 📋 **TODO**: Module startup và service registration tests

### **Long Term (1 month)**
10. 📋 **TODO**: Database migration tests
11. 📋 **TODO**: Permission và authorization tests
12. 📋 **TODO**: Functional/E2E tests với Cypress

---

## 🎯 **KẾT LUẬN**

### **Tình Trạng Hiện Tại: 30% Chuẩn OrchardCore**

**Điểm Mạnh:**
- ✅ Framework và tools đúng chuẩn (xUnit, Moq)
- ✅ Test naming convention tốt
- ✅ Basic unit tests hoạt động
- ✅ Property validation tests comprehensive

**Điểm Cần Cải Thiện:**
- ❌ Thiếu OrchardCore context testing (70% functionality)
- ❌ Thiếu integration tests
- ❌ Thiếu display driver và handler tests
- ❌ Thiếu content serialization tests
- ❌ Test coverage chỉ 1/3 modules

### **Khuyến Nghị:**
1. **Ưu tiên cao**: Implement Content Testing Pattern chuẩn
2. **Ưu tiên trung bình**: Thêm Integration Tests với OrchardTestFixture
3. **Ưu tiên thấp**: Functional Tests với Cypress

Để đạt chuẩn OrchardCore 100%, cần implement thêm 70% functionality còn lại theo roadmap trên.