# Testing Strategies cho OrchardCore Modules

## 🎯 **QUY TRÌNH TESTING TRONG PHÁT TRIỂN MODULES**

### **KHÔNG PHẢI** - Testing sau khi hoàn thiện module ❌
### **MÀ LÀ** - Testing song song với phát triển module ✅

## 🔄 **QUY TRÌNH PHÁT TRIỂN MODULE CHUẨN**

### **Bước 1: TDD (Test-Driven Development) - KHUYẾN KHÍCH**
```
1. Viết Test trước → 2. Viết Code để pass test → 3. Refactor
```

### **Bước 2: Development với Testing song song**
```
Viết Feature → Viết Test → Chạy Test → Fix bugs → Repeat
```

### **Bước 3: Testing trước khi commit**
```
Code Complete → Run All Tests → Fix failing tests → Commit
```

## 🎯 **MỤC TIÊU**
Tìm hiểu các chiến lược testing để **viết modules OrchardCore chuẩn, không bị lỗi** khi triển khai và sử dụng.

---

## 🏗️ **KIẾN TRÚC TESTING TRONG ORCHARDCORE**

### **1. Ba Loại Testing Chính**

#### **A. Unit Tests** ⭐ **CƠ BẢN NHẤT**
```
📁 /test/OrchardCore.Tests/
├── ContentManagement/
├── Modules/
│   ├── OrchardCore.Users/
│   ├── OrchardCore.Media/
│   └── OrchardCore.Localization/
└── Stubs/ (Mock objects)
```

**Đặc điểm:**
- Framework: **xUnit + Moq**
- Scope: Test từng component riêng lẻ
- Dependencies: Được mock hoàn toàn
- Execution: Nhanh, isolated

#### **B. Integration Tests** ⭐ **QUAN TRỌNG CHO MODULES**
```
📁 /test/OrchardCore.Tests/Apis/Context/
├── SiteContext.cs          ← Tenant management
├── OrchardTestFixture.cs   ← WebApplicationFactory
└── Extensions/             ← Helper methods
```

**Đặc điểm:**
- Framework: **WebApplicationFactory + TestHost**
- Scope: Test modules trong OrchardCore context
- Dependencies: Real database, real services
- Execution: Chậm hơn, realistic

#### **C. Functional/E2E Tests** ⭐ **UI TESTING**
```
📁 /test/OrchardCore.Tests.Functional/
├── cms-tests/cypress/      ← CMS UI tests
├── mvc-tests/cypress/      ← MVC tests
└── cypress-commands/       ← Custom OrchardCore commands
```

**Đặc điểm:**
- Framework: **Cypress**
- Scope: End-to-end user workflows
- Dependencies: Full application stack
- Execution: Chậm nhất, most realistic

---

## 🔧 **CORE TESTING PATTERNS**

### **1. OrchardTestFixture Pattern**

```csharp
public class OrchardTestFixture<TStartup> : WebApplicationFactory<TStartup>
    where TStartup : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // ✅ PATTERN: Clean slate cho mỗi test
        var shellsApplicationDataPath = Path.Combine(
            Directory.GetCurrentDirectory(), "App_Data");
            
        if (Directory.Exists(shellsApplicationDataPath))
        {
            Directory.Delete(shellsApplicationDataPath, true);
        }
        
        builder.UseContentRoot(Directory.GetCurrentDirectory());
    }
    
    protected override IWebHostBuilder CreateWebHostBuilder()
    {
        return WebHostBuilderFactory.CreateFromAssemblyEntryPoint(
            typeof(Program).Assembly, []);
    }
}
```

**Ứng dụng trong module:**
```csharp
public class MyModuleIntegrationTests : IClassFixture<OrchardTestFixture<SiteStartup>>
{
    private readonly OrchardTestFixture<SiteStartup> _fixture;
    
    public MyModuleIntegrationTests(OrchardTestFixture<SiteStartup> fixture)
    {
        _fixture = fixture;
    }
}
```

### **2. SiteContext Pattern (Tenant Isolation)**

```csharp
public class SiteContext : IDisposable
{
    private static readonly TablePrefixGenerator _tablePrefixGenerator = new();
    
    public virtual async Task InitializeAsync()
    {
        // ✅ PATTERN: Unique tenant per test
        var tenantName = Guid.NewGuid().ToString("n");
        var tablePrefix = await _tablePrefixGenerator.GeneratePrefixAsync();
        
        // ✅ PATTERN: API-based tenant creation
        var createModel = new TenantApiModel
        {
            DatabaseProvider = DatabaseProvider,
            TablePrefix = tablePrefix,
            ConnectionString = ConnectionString,
            RecipeName = RecipeName,
            Name = tenantName,
            RequestUrlPrefix = tenantName,
        };
        
        var createResult = await DefaultTenantClient
            .PostAsJsonAsync("api/tenants/create", createModel);
        createResult.EnsureSuccessStatusCode();
        
        // ✅ PATTERN: Setup tenant với recipe
        var setupModel = new SetupApiViewModel
        {
            SiteName = "Test Site",
            DatabaseProvider = DatabaseProvider,
            TablePrefix = tablePrefix,
            RecipeName = RecipeName,
            UserName = "admin",
            Password = "Password01_",
            Name = tenantName,
        };
        
        var setupResult = await DefaultTenantClient
            .PostAsJsonAsync("api/tenants/setup", setupModel);
        setupResult.EnsureSuccessStatusCode();
    }
    
    // ✅ PATTERN: Scoped service access
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

### **3. Content Testing Pattern**

```csharp
[Fact]
public void ContentPart_Serialization_PreservesTypeInformation()
{
    // ✅ PATTERN: Arrange - Create content with parts
    var contentItem = new ContentItem();
    var titlePart = new TitlePart { Title = "Test Title" };
    contentItem.Weld(titlePart);
    
    // ✅ PATTERN: Act - Test serialization/deserialization
    var json = JConvert.SerializeObject(contentItem);
    var deserializedItem = JConvert.DeserializeObject<ContentItem>(json);
    
    // ✅ PATTERN: Assert - Verify casting behavior
    var basePart = deserializedItem.Get<ContentPart>(nameof(TitlePart));
    var concretePart = deserializedItem.Get<TitlePart>(nameof(TitlePart));
    
    Assert.NotNull(basePart);
    Assert.NotNull(concretePart);
    Assert.NotSame(basePart, concretePart); // Different instances
}
```

### **4. Stub Pattern (Mocking Dependencies)**

```csharp
public class StubExtensionManager : IExtensionManager
{
    public IEnumerable<IExtensionInfo> GetExtensions()
    {
        // ✅ PATTERN: Return empty for testing
        return [];
    }
    
    public IEnumerable<IFeatureInfo> GetFeatures()
    {
        // ✅ PATTERN: Return minimal test feature
        return [new FeatureInfo(
            GetType().Assembly.GetName().Name, 
            new ExtensionInfo(GetType().Assembly.GetName().Name))];
    }
    
    public IExtensionInfo GetExtension(string extensionId)
    {
        throw new NotImplementedException(); // ✅ Explicit not implemented
    }
}
```

---

## 📋 **MODULE TESTING CHECKLIST**

### **A. Unit Tests** ⭐ **BẮT BUỘC**

#### **1. Content Parts Testing:**
```csharp
[Fact]
public void MyContentPart_SetProperty_UpdatesCorrectly()
{
    // ✅ Test part properties
    var part = new MyContentPart();
    part.MyProperty = "test value";
    
    Assert.Equal("test value", part.MyProperty);
}

[Fact]
public void MyContentPart_Apply_MergesDataCorrectly()
{
    // ✅ Test Apply method
    var contentItem = new ContentItem();
    contentItem.Apply(new MyContentPart { MyProperty = "initial" });
    contentItem.Apply(new MyContentPart { MyProperty = null });
    
    var property = (string)contentItem.Content.MyContentPart.MyProperty;
    Assert.Null(property);
}
```

#### **2. Display Drivers Testing:**
```csharp
[Fact]
public async Task MyPartDisplayDriver_Display_ReturnsCorrectShape()
{
    // ✅ Arrange - Mock dependencies
    var mockLocalizer = new Mock<IStringLocalizer<MyPartDisplayDriver>>();
    var mockService = new Mock<IMyService>();
    
    var driver = new MyPartDisplayDriver(mockLocalizer.Object, mockService.Object);
    var part = new MyContentPart { MyProperty = "test" };
    var context = new BuildPartDisplayContext(part, "", "", new ShapeFactory());
    
    // ✅ Act
    var result = driver.Display(part, context);
    
    // ✅ Assert
    Assert.NotNull(result);
    // Verify shape properties
}

[Fact]
public async Task MyPartDisplayDriver_Update_ValidatesInput()
{
    // ✅ Test validation logic
    var driver = new MyPartDisplayDriver();
    var part = new MyContentPart();
    var context = new UpdatePartEditorContext(part, new DefaultModelUpdater());
    
    // ✅ Simulate invalid input
    context.Updater.ModelState.AddModelError("MyProperty", "Required");
    
    var result = await driver.UpdateAsync(part, context);
    
    Assert.True(context.Updater.ModelState.ErrorCount > 0);
}
```

#### **3. Handlers Testing:**
```csharp
[Fact]
public async Task MyContentHandler_Creating_SetsDefaultValues()
{
    // ✅ Arrange
    var handler = new MyContentHandler();
    var context = new CreateContentContext(new ContentItem { ContentType = "MyType" });
    
    // ✅ Act
    await handler.CreatingAsync(context);
    
    // ✅ Assert
    var part = context.ContentItem.As<MyContentPart>();
    Assert.NotNull(part);
    Assert.Equal("default", part.MyProperty);
}
```

#### **4. Services Testing:**
```csharp
[Fact]
public async Task MyService_ProcessData_ReturnsExpectedResult()
{
    // ✅ Arrange - Mock dependencies
    var mockRepository = new Mock<IMyRepository>();
    mockRepository.Setup(x => x.GetDataAsync(It.IsAny<string>()))
              .ReturnsAsync("mock data");
    
    var service = new MyService(mockRepository.Object);
    
    // ✅ Act
    var result = await service.ProcessDataAsync("input");
    
    // ✅ Assert
    Assert.Equal("processed: mock data", result);
    mockRepository.Verify(x => x.GetDataAsync("input"), Times.Once);
}
```

### **B. Integration Tests** ⭐ **KHUYẾN KHÍCH**

#### **1. Module Startup Testing:**
```csharp
[Fact]
public async Task MyModule_Startup_RegistersServicesCorrectly()
{
    var context = await GetSiteContextAsync();
    
    await context.UsingTenantScopeAsync(async scope =>
    {
        // ✅ Verify service registration
        var myService = scope.ServiceProvider.GetService<IMyService>();
        Assert.NotNull(myService);
        
        // ✅ Verify feature is enabled
        var shellFeaturesManager = scope.ServiceProvider
            .GetRequiredService<IShellFeaturesManager>();
        var features = await shellFeaturesManager.GetEnabledFeaturesAsync();
        
        Assert.Contains(features, f => f.Id == "MyModule");
    });
}
```

#### **2. Database Migration Testing:**
```csharp
[Fact]
public async Task MyModule_Migration_CreatesTablesCorrectly()
{
    var context = await GetSiteContextAsync();
    
    await context.UsingTenantScopeAsync(async scope =>
    {
        var session = scope.ServiceProvider.GetRequiredService<ISession>();
        
        // ✅ Verify table exists
        var schemaBuilder = new SchemaBuilder(session.Store.Configuration, session);
        var tableExists = await schemaBuilder.TableExistsAsync("MyModuleIndex");
        
        Assert.True(tableExists);
        
        // ✅ Verify table structure
        // Test columns, indexes, etc.
    });
}
```

#### **3. Content Type Definition Testing:**
```csharp
[Fact]
public async Task MyModule_ContentTypeDefinition_CreatedCorrectly()
{
    var context = await GetSiteContextAsync();
    
    await context.UsingTenantScopeAsync(async scope =>
    {
        var contentDefinitionManager = scope.ServiceProvider
            .GetRequiredService<IContentDefinitionManager>();
        
        // ✅ Verify content type exists
        var contentType = await contentDefinitionManager
            .GetTypeDefinitionAsync("MyContentType");
        
        Assert.NotNull(contentType);
        
        // ✅ Verify parts are attached
        Assert.Contains(contentType.Parts, p => p.Name == "MyContentPart");
        
        // ✅ Verify settings
        var settings = contentType.GetSettings<MyContentTypeSettings>();
        Assert.NotNull(settings);
    });
}
```

#### **4. Permissions Testing:**
```csharp
[Fact]
public async Task MyModule_Permissions_WorkCorrectly()
{
    var context = await GetSiteContextAsync();
    
    await context.UsingTenantScopeAsync(async scope =>
    {
        var authorizationService = scope.ServiceProvider
            .GetRequiredService<IAuthorizationService>();
        var userManager = scope.ServiceProvider
            .GetRequiredService<UserManager<IUser>>();
        
        // ✅ Create test user with specific role
        var user = await userManager.FindByNameAsync("testuser");
        var principal = await userManager.CreatePrincipalAsync(user);
        
        // ✅ Test permission
        var result = await authorizationService
            .AuthorizeAsync(principal, MyPermissions.ManageMyContent);
        
        Assert.True(result.Succeeded);
    });
}
```

### **C. Functional Tests** ⭐ **TÙY CHỌN**

#### **1. Cypress Custom Commands:**
```javascript
// cypress/support/commands.js
Cypress.Commands.add('loginAsAdmin', () => {
    cy.visit('/login');
    cy.get('[name="UserName"]').type('admin');
    cy.get('[name="Password"]').type('Password01_');
    cy.get('[type="submit"]').click();
});

Cypress.Commands.add('createMyContent', (title, content) => {
    cy.visit('/Admin/Contents/Create/MyContentType');
    cy.get('[name="TitlePart.Title"]').type(title);
    cy.get('[name="MyContentPart.MyProperty"]').type(content);
    cy.get('[name="submit.Publish"]').click();
});
```

#### **2. Admin UI Testing:**
```javascript
describe('MyModule Admin UI', () => {
    beforeEach(() => {
        cy.loginAsAdmin();
    });
    
    it('should create content successfully', () => {
        // ✅ Test content creation workflow
        cy.createMyContent('Test Title', 'Test Content');
        
        // ✅ Verify success message
        cy.contains('Your content has been created').should('be.visible');
        
        // ✅ Verify content appears in list
        cy.visit('/Admin/Contents/ContentItems');
        cy.contains('Test Title').should('be.visible');
    });
    
    it('should validate required fields', () => {
        // ✅ Test validation
        cy.visit('/Admin/Contents/Create/MyContentType');
        cy.get('[name="submit.Publish"]').click();
        
        // ✅ Verify validation messages
        cy.contains('The Title field is required').should('be.visible');
    });
});
```

---

## 🛠️ **TEST PROJECT SETUP**

### **1. Test Project Structure:**
```
MyModule.Tests/
├── MyModule.Tests.csproj
├── Unit/
│   ├── Parts/
│   │   └── MyContentPartTests.cs
│   ├── Drivers/
│   │   └── MyPartDisplayDriverTests.cs
│   ├── Handlers/
│   │   └── MyContentHandlerTests.cs
│   └── Services/
│       └── MyServiceTests.cs
├── Integration/
│   ├── StartupTests.cs
│   ├── MigrationTests.cs
│   └── PermissionTests.cs
├── Functional/
│   └── cypress/
│       └── integration/
│           └── my-module.spec.js
└── Stubs/
    ├── StubMyRepository.cs
    └── StubMyService.cs
```

### **2. Test Project Configuration:**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFrameworks>$(CommonTargetFrameworks)</TargetFrameworks>
    <OutputType>Exe</OutputType>
  </PropertyGroup>

  <ItemGroup>
    <!-- ✅ Core testing packages -->
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit.v3" />
    <PackageReference Include="xunit.runner.visualstudio" />
    <PackageReference Include="xunit.analyzers" />
    
    <!-- ✅ Mocking framework -->
    <PackageReference Include="Moq" />
    
    <!-- ✅ ASP.NET Core testing -->
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" />
    
    <!-- ✅ HTML parsing for UI tests -->
    <PackageReference Include="AngleSharp" />
  </ItemGroup>

  <ItemGroup>
    <!-- ✅ Reference to OrchardCore test infrastructure -->
    <ProjectReference Include="..\..\OrchardCore\test\OrchardCore.Tests\OrchardCore.Tests.csproj" />
    
    <!-- ✅ Reference to your module -->
    <ProjectReference Include="..\MyModule\MyModule.csproj" />
  </ItemGroup>

  <ItemGroup>
    <!-- ✅ Test configuration files -->
    <None Include="xunit.runner.json" CopyToOutputDirectory="PreserveNewest" />
    <None Include="appsettings.json" CopyToOutputDirectory="PreserveNewest" />
  </ItemGroup>
</Project>
```

### **3. Test Configuration:**
```json
// xunit.runner.json
{
  "methodDisplay": "method",
  "methodDisplayOptions": "all",
  "diagnosticMessages": true,
  "preEnumerateTheories": false,
  "maxParallelThreads": 1
}
```

```json
// appsettings.json
{
  "OrchardCore": {
    "OrchardCore_Default": {
      "TablePrefix": "oc_test_",
      "ConnectionString": "Data Source=:memory:"
    }
  }
}
```

---

## ✅ **TESTING BEST PRACTICES**

### **1. Test Organization:**
```csharp
// ✅ ĐÚNG: Descriptive test names
[Fact]
public void MyContentPart_WhenTitleIsEmpty_ShouldUseDefaultTitle()

// ❌ SAI: Vague test names  
[Fact]
public void TestMyContentPart()
```

### **2. Test Data Management:**
```csharp
// ✅ ĐÚNG: Clean test data
public void Dispose()
{
    // Clean up database
    // Remove test files
    // Reset static state
}

// ✅ ĐÚNG: Isolated test data
[Fact]
public async Task EachTest_ShouldHaveUniqueData()
{
    var uniqueId = Guid.NewGuid().ToString();
    var testData = CreateTestData(uniqueId);
    // ...
}
```

### **3. Async Testing:**
```csharp
// ✅ ĐÚNG: Proper async testing
[Fact]
public async Task MyService_ProcessAsync_ShouldCompleteSuccessfully()
{
    var result = await myService.ProcessAsync();
    Assert.NotNull(result);
}

// ❌ SAI: Blocking async calls
[Fact]
public void MyService_ProcessAsync_ShouldCompleteSuccessfully()
{
    var result = myService.ProcessAsync().Result; // ❌ Deadlock risk
}
```

### **4. Mock Verification:**
```csharp
// ✅ ĐÚNG: Verify mock interactions
[Fact]
public async Task MyService_ShouldCallRepository()
{
    var mockRepo = new Mock<IMyRepository>();
    var service = new MyService(mockRepo.Object);
    
    await service.ProcessAsync();
    
    mockRepo.Verify(x => x.SaveAsync(It.IsAny<MyEntity>()), Times.Once);
}
```

---

## 🚀 **CONTINUOUS INTEGRATION SETUP**

### **1. GitHub Actions Workflow:**
```yaml
name: Tests

on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '8.0.x'
    
    - name: Restore dependencies
      run: dotnet restore
    
    - name: Build
      run: dotnet build --no-restore
    
    - name: Run Unit Tests
      run: dotnet test --no-build --verbosity normal --filter Category=Unit
    
    - name: Run Integration Tests
      run: dotnet test --no-build --verbosity normal --filter Category=Integration
    
    - name: Setup Node.js
      uses: actions/setup-node@v3
      with:
        node-version: '18'
    
    - name: Install Cypress dependencies
      run: npm install
      working-directory: ./tests/Functional
    
    - name: Run Functional Tests
      run: npm run test:headless
      working-directory: ./tests/Functional
```

### **2. Test Categories:**
```csharp
[Fact]
[Trait("Category", "Unit")]
public void UnitTest_ShouldRunFast() { }

[Fact]
[Trait("Category", "Integration")]
public async Task IntegrationTest_ShouldRunWithDatabase() { }

[Fact]
[Trait("Category", "Functional")]
public void FunctionalTest_ShouldRunWithUI() { }
```

---

## 📊 **TEST COVERAGE & METRICS**

### **1. Coverage Configuration:**
```xml
<PropertyGroup>
  <CollectCoverage>true</CollectCoverage>
  <CoverletOutputFormat>opencover</CoverletOutputFormat>
  <CoverletOutput>./coverage/</CoverletOutput>
  <Exclude>[*]*.Migrations.*,[*]*.Stubs.*</Exclude>
</PropertyGroup>
```

### **2. Quality Gates:**
```yaml
# Minimum coverage thresholds
- name: Check Coverage
  run: |
    dotnet test --collect:"XPlat Code Coverage"
    # Fail if coverage < 80%
    if [ $(grep -o 'line-rate="[^"]*"' coverage.xml | cut -d'"' -f2 | awk '{print $1*100}') -lt 80 ]; then
      echo "Coverage below 80%"
      exit 1
    fi
```

---

## 🎯 **TÓM TẮT - TESTING STRATEGY CHO MODULES**

### **Minimum Required (để modules không lỗi):**
1. ✅ **Unit Tests** cho tất cả business logic
2. ✅ **Integration Tests** cho database operations  
3. ✅ **Mock dependencies** đúng cách
4. ✅ **Clean test data** sau mỗi test
5. ✅ **Async testing** patterns

### **Recommended (để modules chất lượng cao):**
1. ✅ **Functional Tests** cho user workflows
2. ✅ **Performance Tests** cho heavy operations
3. ✅ **Multi-tenant Tests** nếu support multi-tenancy
4. ✅ **API Tests** nếu có REST/GraphQL endpoints
5. ✅ **CI/CD integration** với automated testing

### **Advanced (cho enterprise modules):**
1. ✅ **Load Testing** cho scalability
2. ✅ **Security Testing** cho vulnerabilities
3. ✅ **Accessibility Testing** cho UI compliance
4. ✅ **Cross-browser Testing** cho compatibility

**Testing không chỉ để tìm lỗi, mà còn đảm bảo modules hoạt động đúng trong OrchardCore ecosystem và maintainable trong tương lai!** 🎯

---

*Tài liệu này được tạo dựa trên phân tích source code OrchardCore và best practices từ cộng đồng.*