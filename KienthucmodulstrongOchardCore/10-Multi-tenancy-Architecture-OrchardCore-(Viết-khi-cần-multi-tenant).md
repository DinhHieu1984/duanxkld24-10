# Multi-tenancy Architecture trong OrchardCore

## 🎯 **MỤC TIÊU**
Tìm hiểu **Multi-tenancy Architecture** để **viết modules OrchardCore hỗ trợ multi-tenant**.

---

## 🏗️ **MULTI-TENANCY ARCHITECTURE OVERVIEW**

### **1. Core Concepts**

#### **A. Tenant (Shell)**
- **Định nghĩa**: Một instance độc lập của ứng dụng
- **Isolation**: Database, configuration, modules riêng biệt
- **Routing**: Host-based hoặc path-based routing
- **State Management**: Uninitialized → Initializing → Running → Disabled

#### **B. Shell Context**
- **Container**: Mỗi tenant có DI container riêng
- **Pipeline**: Request pipeline độc lập
- **Lifecycle**: Lazy loading và automatic disposal
- **Scope**: Per-request shell scope management

---

## 🔧 **CORE MULTI-TENANCY PATTERNS**

### **1. Shell Settings Pattern**
```csharp
// ShellSettings - Tenant configuration
public class ShellSettings : IDisposable
{
    public const string DefaultShellName = "Default";
    
    public string Name { get; set; }
    public string TenantId { get; set; }
    public string RequestUrlHost { get; set; }
    public string RequestUrlPrefix { get; set; }
    public TenantState State { get; set; }
    
    // Multi-host support
    public string[] RequestUrlHosts => RequestUrlHost
        ?.Split(HostSeparators, StringSplitOptions.RemoveEmptyEntries)
        ?? [];
    
    // Configuration access
    public IShellConfiguration ShellConfiguration { get; }
    public string this[string key] { get; set; }
}

// Tenant states
public enum TenantState
{
    Uninitialized,
    Initializing,
    Running,
    Disabled,
    Invalid
}
```

### **2. Shell Host Pattern**
```csharp
public class ShellHost : IShellHost
{
    private readonly ConcurrentDictionary<string, ShellContext> _shellContexts;
    private readonly ConcurrentDictionary<string, ShellSettings> _shellSettings;
    private readonly IRunningShellTable _runningShellTable;
    
    // Initialize all tenants on startup
    public async Task InitializeAsync()
    {
        await PreCreateAndRegisterShellsAsync();
    }
    
    // Get or create shell context for tenant
    public async Task<ShellContext> GetOrCreateShellContextAsync(ShellSettings settings)
    {
        if (!_shellContexts.TryGetValue(settings.Name, out var shell))
        {
            shell = await CreateShellContextAsync(settings);
            AddAndRegisterShell(shell);
        }
        return shell;
    }
    
    // Create shell scope for request
    public async Task<ShellScope> GetScopeAsync(ShellSettings settings)
    {
        var shellContext = await GetOrCreateShellContextAsync(settings);
        return await shellContext.CreateScopeAsync();
    }
}
```

### **3. Running Shell Table Pattern**
```csharp
public class RunningShellTable : IRunningShellTable
{
    private ImmutableDictionary<string, ShellSettings> _shellsByHostAndPrefix;
    private ShellSettings _default;
    private bool _hasStarMapping;
    
    // Register tenant routing
    public void Add(ShellSettings settings)
    {
        if (settings.IsDefaultShell())
        {
            _default = settings;
        }
        
        var allHostsAndPrefix = GetAllHostsAndPrefix(settings);
        // Register all host/prefix combinations
        foreach (var hostAndPrefix in allHostsAndPrefix)
        {
            _shellsByHostAndPrefix = _shellsByHostAndPrefix.SetItem(hostAndPrefix, settings);
        }
    }
    
    // Match incoming request to tenant
    public ShellSettings Match(HostString host, PathString path, bool fallbackToDefault = true)
    {
        // 1. Exact host:port + prefix match
        // 2. Host + prefix match  
        // 3. Host only match
        // 4. Prefix only match
        // 5. Wildcard matching (*.domain.com)
        // 6. Default tenant fallback
        
        if (TryMatchInternal(host.Value, host.Host, path.Value, out var result))
        {
            return result;
        }
        
        // Star mapping support
        if (_hasStarMapping && TryMatchStarMapping(host.Value, host.Host, path.Value, out result))
        {
            return result;
        }
        
        return fallbackToDefault ? _default : null;
    }
}
```

### **4. Tenant Container Middleware Pattern**
```csharp
public class ModularTenantContainerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IShellHost _shellHost;
    private readonly IRunningShellTable _runningShellTable;
    
    public async Task Invoke(HttpContext httpContext)
    {
        // Initialize all shells
        await _shellHost.InitializeAsync();
        
        // Match request to tenant
        var shellSettings = _runningShellTable.Match(httpContext);
        
        if (shellSettings != null)
        {
            // Handle initializing tenant
            if (shellSettings.IsInitializing())
            {
                httpContext.Response.Headers.Append(HeaderNames.RetryAfter, "10");
                httpContext.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                await httpContext.Response.WriteAsync("The requested tenant is currently initializing.");
                return;
            }
            
            // Set up tenant-specific services
            httpContext.UseShellScopeServices();
            
            // Create shell scope for request
            var shellScope = await _shellHost.GetScopeAsync(shellSettings);
            
            // Store shell context in request features
            httpContext.Features.Set(new ShellContextFeature
            {
                ShellContext = shellScope.ShellContext,
                OriginalPath = httpContext.Request.Path,
                OriginalPathBase = httpContext.Request.PathBase
            });
            
            // Execute request in tenant context
            await shellScope.UsingAsync(async scope =>
            {
                await _next.Invoke(httpContext);
            });
        }
    }
}
```

### **5. Shell Context Pattern**
```csharp
public class ShellContext : IDisposable, IAsyncDisposable
{
    private volatile int _refCount;
    private bool _released;
    private readonly SemaphoreSlim _semaphore = new(1);
    
    public ShellSettings Settings { get; set; }
    public ShellBlueprint Blueprint { get; set; }
    public IServiceProvider ServiceProvider { get; set; }
    public IShellPipeline Pipeline { get; set; }
    public bool IsActivated { get; set; }
    
    // Create request scope
    public async Task<ShellScope> CreateScopeAsync()
    {
        if (_released)
        {
            return null;
        }
        
        var scope = new ShellScope(this);
        
        if (_released)
        {
            await scope.TerminateShellAsync();
            return null;
        }
        
        return scope;
    }
    
    // Reference counting for proper disposal
    internal void AddRef()
    {
        if (ServiceProvider == null)
        {
            throw new InvalidOperationException(
                $"Can't resolve a scope on tenant '{Settings.Name}' as it is disabled or disposed");
        }
        
        Interlocked.Increment(ref _refCount);
    }
    
    internal bool Release()
    {
        return Interlocked.Decrement(ref _refCount) == 0;
    }
}
```

---

## 🗄️ **TENANT CONFIGURATION PATTERNS**

### **1. File-based Configuration**
```csharp
// tenants.json - Global tenant settings
{
  "Default": {
    "State": "Running"
  },
  "Tenant1": {
    "RequestUrlHost": "tenant1.example.com",
    "State": "Running"
  },
  "Tenant2": {
    "RequestUrlPrefix": "tenant2",
    "State": "Running"
  }
}

// App_Data/Sites/{TenantName}/appsettings.json - Per-tenant configuration
{
  "ConnectionStrings": {
    "Default": "Data Source=tenant1.db"
  },
  "OrchardCore": {
    "OrchardCore_Media": {
      "AssetsPath": "Media/Tenant1"
    }
  }
}
```

### **2. Azure Blob Configuration Pattern**
```csharp
public class BlobShellsSettingsSources : IShellsSettingsSources
{
    private readonly BlobShellStorageOptions _options;
    private readonly BlobServiceClient _blobServiceClient;
    
    public async Task AddSourcesAsync(IConfigurationBuilder builder)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(_options.ContainerName);
        
        // Load tenants.json from blob
        var blobClient = containerClient.GetBlobClient($"{_options.BasePath}/tenants.json");
        
        if (await blobClient.ExistsAsync())
        {
            var response = await blobClient.DownloadStreamingAsync();
            builder.AddJsonStream(response.Value.Content);
        }
    }
    
    public async Task SaveAsync(string tenant, IDictionary<string, string> data)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(_options.ContainerName);
        var blobClient = containerClient.GetBlobClient($"{_options.BasePath}/tenants.json");
        
        // Save tenant settings to blob
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        await blobClient.UploadAsync(new BinaryData(json), overwrite: true);
    }
}
```

### **3. Database Configuration Pattern**
```csharp
public class DatabaseShellsSettingsSources : IShellsSettingsSources
{
    private readonly DatabaseShellsStorageOptions _options;
    private readonly IDbConnectionAccessor _dbConnectionAccessor;
    
    public async Task AddSourcesAsync(IConfigurationBuilder builder)
    {
        using var connection = _dbConnectionAccessor.CreateConnection();
        await connection.OpenAsync();
        
        var command = connection.CreateCommand();
        command.CommandText = $"SELECT [Name], [Settings] FROM [{_options.TablePrefix}ShellSettings]";
        
        var settings = new Dictionary<string, Dictionary<string, string>>();
        
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var name = reader.GetString("Name");
            var settingsJson = reader.GetString("Settings");
            var tenantSettings = JsonSerializer.Deserialize<Dictionary<string, string>>(settingsJson);
            settings[name] = tenantSettings;
        }
        
        builder.AddInMemoryCollection(settings.SelectMany(kvp => 
            kvp.Value.Select(setting => 
                new KeyValuePair<string, string>($"{kvp.Key}:{setting.Key}", setting.Value))));
    }
}
```

---

## 🔒 **TENANT ISOLATION PATTERNS**

### **1. Database Isolation**
```csharp
// Per-tenant database connections
public class TenantDbContextFactory<TDbContext> : IDbContextFactory<TDbContext>
    where TDbContext : DbContext
{
    private readonly IShellHost _shellHost;
    private readonly IHttpContextAccessor _httpContextAccessor;
    
    public TDbContext CreateDbContext()
    {
        var shellSettings = GetCurrentTenantSettings();
        var connectionString = shellSettings.ShellConfiguration.GetConnectionString("Default");
        
        var optionsBuilder = new DbContextOptionsBuilder<TDbContext>();
        optionsBuilder.UseSqlServer(connectionString);
        
        return (TDbContext)Activator.CreateInstance(typeof(TDbContext), optionsBuilder.Options);
    }
    
    private ShellSettings GetCurrentTenantSettings()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var shellContext = httpContext.Features.Get<ShellContextFeature>()?.ShellContext;
        return shellContext?.Settings;
    }
}
```

### **2. File System Isolation**
```csharp
public class TenantFileProvider : IFileProvider
{
    private readonly IFileProvider _baseProvider;
    private readonly string _tenantPath;
    
    public TenantFileProvider(IFileProvider baseProvider, string tenantName)
    {
        _baseProvider = baseProvider;
        _tenantPath = $"App_Data/Sites/{tenantName}";
    }
    
    public IFileInfo GetFileInfo(string subpath)
    {
        var tenantPath = Path.Combine(_tenantPath, subpath.TrimStart('/'));
        return _baseProvider.GetFileInfo(tenantPath);
    }
    
    public IDirectoryContents GetDirectoryContents(string subpath)
    {
        var tenantPath = Path.Combine(_tenantPath, subpath.TrimStart('/'));
        return _baseProvider.GetDirectoryContents(tenantPath);
    }
}
```

### **3. Cache Isolation**
```csharp
public class TenantMemoryCache : IMemoryCache
{
    private readonly IMemoryCache _baseCache;
    private readonly string _tenantPrefix;
    
    public TenantMemoryCache(IMemoryCache baseCache, string tenantName)
    {
        _baseCache = baseCache;
        _tenantPrefix = $"tenant:{tenantName}:";
    }
    
    public bool TryGetValue(object key, out object value)
    {
        var tenantKey = _tenantPrefix + key.ToString();
        return _baseCache.TryGetValue(tenantKey, out value);
    }
    
    public ICacheEntry CreateEntry(object key)
    {
        var tenantKey = _tenantPrefix + key.ToString();
        return _baseCache.CreateEntry(tenantKey);
    }
}
```

---

## 🚀 **ADVANCED MULTI-TENANCY PATTERNS**

### **1. Tenant-Aware Services**
```csharp
public interface ITenantService
{
    string GetCurrentTenant();
    Task<T> ExecuteForTenantAsync<T>(string tenantName, Func<Task<T>> operation);
}

public class TenantService : ITenantService
{
    private readonly IShellHost _shellHost;
    private readonly IHttpContextAccessor _httpContextAccessor;
    
    public string GetCurrentTenant()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var shellContext = httpContext?.Features.Get<ShellContextFeature>()?.ShellContext;
        return shellContext?.Settings.Name ?? ShellSettings.DefaultShellName;
    }
    
    public async Task<T> ExecuteForTenantAsync<T>(string tenantName, Func<Task<T>> operation)
    {
        var shellSettings = await _shellHost.GetSettingsAsync(tenantName);
        var shellScope = await _shellHost.GetScopeAsync(shellSettings);
        
        return await shellScope.UsingAsync(async scope =>
        {
            return await operation();
        });
    }
}
```

### **2. Cross-Tenant Communication**
```csharp
public interface ICrossTenantService
{
    Task<TResult> CallTenantServiceAsync<TService, TResult>(
        string tenantName, 
        Func<TService, Task<TResult>> serviceCall)
        where TService : class;
}

public class CrossTenantService : ICrossTenantService
{
    private readonly IShellHost _shellHost;
    
    public async Task<TResult> CallTenantServiceAsync<TService, TResult>(
        string tenantName, 
        Func<TService, Task<TResult>> serviceCall)
        where TService : class
    {
        var shellSettings = await _shellHost.GetSettingsAsync(tenantName);
        var shellScope = await _shellHost.GetScopeAsync(shellSettings);
        
        return await shellScope.UsingAsync(async scope =>
        {
            var service = scope.ServiceProvider.GetRequiredService<TService>();
            return await serviceCall(service);
        });
    }
}
```

### **3. Tenant Auto-Setup Pattern**
```csharp
public class TenantAutoSetupService
{
    private readonly IShellHost _shellHost;
    private readonly IShellSettingsManager _shellSettingsManager;
    
    public async Task<ShellContext> SetupTenantAsync(TenantSetupOptions options)
    {
        // Create shell settings
        var shellSettings = new ShellSettings
        {
            Name = options.TenantName,
            RequestUrlHost = options.Host,
            RequestUrlPrefix = options.Prefix,
            State = TenantState.Uninitialized
        };
        
        // Configure database
        shellSettings["ConnectionString"] = options.ConnectionString;
        shellSettings["DatabaseProvider"] = options.DatabaseProvider;
        shellSettings["TablePrefix"] = options.TablePrefix;
        
        // Save settings
        await _shellSettingsManager.SaveSettingsAsync(shellSettings);
        
        // Create and initialize shell context
        var shellContext = await _shellHost.GetOrCreateShellContextAsync(shellSettings);
        
        // Run setup recipe
        if (!string.IsNullOrEmpty(options.RecipeName))
        {
            await ExecuteSetupRecipeAsync(shellContext, options.RecipeName);
        }
        
        // Mark as running
        shellSettings.State = TenantState.Running;
        await _shellSettingsManager.SaveSettingsAsync(shellSettings);
        
        return shellContext;
    }
}
```

### **4. Tenant Health Monitoring**
```csharp
public class TenantHealthService : IHostedService
{
    private readonly IShellHost _shellHost;
    private readonly ILogger<TenantHealthService> _logger;
    private Timer _timer;
    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _timer = new Timer(CheckTenantHealth, null, TimeSpan.Zero, TimeSpan.FromMinutes(5));
        return Task.CompletedTask;
    }
    
    private async void CheckTenantHealth(object state)
    {
        var shellSettings = await _shellHost.ListShellSettingsAsync();
        
        foreach (var settings in shellSettings.Where(s => s.State == TenantState.Running))
        {
            try
            {
                var shellScope = await _shellHost.GetScopeAsync(settings);
                await shellScope.UsingAsync(async scope =>
                {
                    // Perform health checks
                    var dbContext = scope.ServiceProvider.GetService<DbContext>();
                    if (dbContext != null)
                    {
                        await dbContext.Database.CanConnectAsync();
                    }
                });
                
                _logger.LogInformation("Tenant {TenantName} is healthy", settings.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Tenant {TenantName} health check failed", settings.Name);
                
                // Optionally disable unhealthy tenant
                settings.State = TenantState.Disabled;
                await _shellHost.UpdateShellSettingsAsync(settings);
            }
        }
    }
}
```

---

## 🎯 **BEST PRACTICES**

### **✅ ĐÚNG:**
- Use proper tenant isolation (database, files, cache)
- Implement graceful tenant initialization
- Handle cross-tenant communication carefully
- Monitor tenant health and performance
- Use tenant-aware logging and metrics
- Implement proper tenant lifecycle management
- Support multiple routing strategies (host/path)
- Use configuration providers for flexibility

### **❌ SAI:**
- Share sensitive data between tenants
- Hard-code tenant-specific logic
- Skip tenant state validation
- Ignore tenant disposal and cleanup
- Mix tenant contexts in services
- Use global caching without tenant isolation
- Skip error handling in tenant operations
- Forget to validate tenant permissions

---

## 🔧 **IMPLEMENTATION CHECKLIST**

### **Multi-Tenant Setup:**
- [ ] Configure tenant routing (host/path-based)
- [ ] Set up tenant isolation (database, files, cache)
- [ ] Implement tenant-aware services
- [ ] Configure tenant-specific settings
- [ ] Set up tenant lifecycle management
- [ ] Implement health monitoring
- [ ] Add cross-tenant communication if needed
- [ ] Test tenant isolation thoroughly

### **Configuration Options:**
- [ ] File-based configuration (tenants.json)
- [ ] Azure Blob configuration
- [ ] Database configuration
- [ ] Environment-specific settings
- [ ] Auto-setup capabilities
- [ ] Migration support
- [ ] Backup and restore procedures

---

## 🏢 **ỨNG DỤNG THỰC TẾ TRONG CÁC DỰ ÁN**

### **1. 🏪 SaaS E-commerce Platform**

#### **Tình huống:**
Xây dựng **nền tảng thương mại điện tử SaaS** cho phép nhiều doanh nghiệp tạo cửa hàng online riêng.

#### **Ứng dụng Multi-tenancy:**
```csharp
// Mỗi cửa hàng = 1 tenant
// shop1.myplatform.com -> Tenant "Shop1"  
// shop2.myplatform.com -> Tenant "Shop2"
// myplatform.com/shop3 -> Tenant "Shop3"

// Tenant Settings
{
  "Shop1": {
    "RequestUrlHost": "shop1.myplatform.com",
    "State": "Running",
    "DatabaseProvider": "SqlServer",
    "ConnectionString": "Server=...;Database=Shop1_DB;",
    "Theme": "EcommerceTheme",
    "Currency": "VND",
    "Language": "vi-VN"
  },
  "Shop2": {
    "RequestUrlHost": "shop2.myplatform.com", 
    "State": "Running",
    "DatabaseProvider": "SqlServer",
    "ConnectionString": "Server=...;Database=Shop2_DB;",
    "Theme": "FashionTheme",
    "Currency": "USD",
    "Language": "en-US"
  }
}
```

#### **Lợi ích:**
- ✅ **Isolation hoàn toàn**: Dữ liệu Shop1 không bao giờ lẫn với Shop2
- ✅ **Custom branding**: Mỗi shop có theme, logo, domain riêng
- ✅ **Scalability**: Thêm shop mới chỉ cần tạo tenant mới
- ✅ **Cost-effective**: Dùng chung infrastructure nhưng tách biệt dữ liệu

### **2. 🏫 Multi-School Management System**

#### **Tình huống:**
Phát triển **hệ thống quản lý trường học** cho nhiều trường sử dụng.

#### **Ứng dụng Multi-tenancy:**
```csharp
// Mỗi trường học = 1 tenant
// thpt-nguyen-hue.edu.vn -> Tenant "NguyenHue"
// thpt-le-quy-don.edu.vn -> Tenant "LeQuyDon"  
// schoolsystem.vn/marie-curie -> Tenant "MarieCurie"

// Custom Module cho từng trường
public class SchoolTenantService : ITenantService
{
    public async Task SetupSchoolAsync(string schoolName, SchoolConfig config)
    {
        var shellSettings = new ShellSettings
        {
            Name = schoolName,
            RequestUrlHost = config.Domain,
            State = TenantState.Uninitialized
        };
        
        // Cấu hình riêng cho từng trường
        shellSettings["SchoolCode"] = config.SchoolCode;
        shellSettings["AcademicYear"] = config.AcademicYear;
        shellSettings["MaxStudents"] = config.MaxStudents.ToString();
        shellSettings["Modules"] = string.Join(",", config.EnabledModules);
        
        await _shellSettingsManager.SaveSettingsAsync(shellSettings);
    }
}
```

#### **Lợi ích:**
- ✅ **Data Privacy**: Dữ liệu học sinh trường A không thể truy cập từ trường B
- ✅ **Custom Features**: Trường A có module "Internat", trường B không cần
- ✅ **Independent Updates**: Update trường A không ảnh hưởng trường B
- ✅ **Compliance**: Đáp ứng yêu cầu bảo mật giáo dục

### **3. 🏥 Multi-Hospital System**

#### **Tình huống:**
Xây dựng **hệ thống quản lý bệnh viện** cho chuỗi bệnh viện.

#### **Ứng dụng Multi-tenancy:**
```csharp
// Mỗi bệnh viện = 1 tenant với cấu hình riêng
public class HospitalTenantSetup
{
    public async Task SetupHospitalAsync(HospitalInfo hospital)
    {
        var shellSettings = new ShellSettings
        {
            Name = hospital.Code,
            RequestUrlHost = hospital.Domain,
            State = TenantState.Uninitialized
        };
        
        // Cấu hình đặc thù bệnh viện
        shellSettings["HospitalType"] = hospital.Type; // "General", "Specialized", "Emergency"
        shellSettings["Departments"] = JsonSerializer.Serialize(hospital.Departments);
        shellSettings["MedicalEquipment"] = JsonSerializer.Serialize(hospital.Equipment);
        shellSettings["ComplianceLevel"] = hospital.ComplianceLevel; // "Basic", "Advanced", "International"
        
        // Database riêng cho mỗi bệnh viện (HIPAA compliance)
        shellSettings["ConnectionString"] = $"Server=...;Database=Hospital_{hospital.Code}_DB;";
        
        await CreateHospitalTenantAsync(shellSettings);
    }
}
```

#### **Lợi ích:**
- ✅ **HIPAA Compliance**: Dữ liệu bệnh nhân được tách biệt hoàn toàn
- ✅ **Specialized Modules**: Bệnh viện tim mạch có module khác bệnh viện nhi
- ✅ **Regional Compliance**: Bệnh viện ở các tỉnh có quy định khác nhau
- ✅ **Disaster Recovery**: Sự cố ở bệnh viện A không ảnh hưởng bệnh viện B

### **4. 🏢 Multi-Company CRM/ERP**

#### **Tình huống:**
Phát triển **hệ thống CRM/ERP** cho nhiều công ty thuê sử dụng.

#### **Ứng dụng Multi-tenancy:**
```csharp
// Mỗi công ty = 1 tenant
public class CompanyTenantManager
{
    public async Task OnboardCompanyAsync(CompanyOnboardingRequest request)
    {
        // Tạo tenant cho công ty mới
        var shellSettings = new ShellSettings
        {
            Name = request.CompanyCode,
            RequestUrlHost = $"{request.CompanyCode}.mycrm.com",
            State = TenantState.Initializing
        };
        
        // Cấu hình business-specific
        shellSettings["Industry"] = request.Industry;
        shellSettings["CompanySize"] = request.EmployeeCount.ToString();
        shellSettings["Timezone"] = request.Timezone;
        shellSettings["Currency"] = request.Currency;
        shellSettings["FiscalYearStart"] = request.FiscalYearStart.ToString();
        
        // Modules theo gói dịch vụ
        var modules = GetModulesForPlan(request.SubscriptionPlan);
        shellSettings["EnabledModules"] = string.Join(",", modules);
        
        await SetupCompanyTenantAsync(shellSettings, request);
    }
    
    private string[] GetModulesForPlan(string plan)
    {
        return plan switch
        {
            "Basic" => new[] { "CRM", "Contacts", "Tasks" },
            "Professional" => new[] { "CRM", "Contacts", "Tasks", "Sales", "Marketing" },
            "Enterprise" => new[] { "CRM", "Contacts", "Tasks", "Sales", "Marketing", "ERP", "Accounting", "HR" },
            _ => new[] { "CRM" }
        };
    }
}
```

#### **Lợi ích:**
- ✅ **Business Isolation**: Dữ liệu công ty A không bao giờ lộ cho công ty B
- ✅ **Flexible Pricing**: Mỗi công ty trả tiền theo modules sử dụng
- ✅ **Custom Workflows**: Công ty A có quy trình khác công ty B
- ✅ **Compliance**: Đáp ứng yêu cầu bảo mật doanh nghiệp

---

## 🎯 **KHI NÀO CẦN SỬ DỤNG MULTI-TENANCY?**

### **✅ NÊN DÙNG KHI:**
1. **Nhiều khách hàng độc lập** cần hệ thống tương tự
2. **Dữ liệu phải tách biệt hoàn toàn** (bảo mật, pháp lý)
3. **Mỗi khách hàng cần customization** khác nhau
4. **Muốn tiết kiệm chi phí infrastructure** (shared resources)
5. **Cần scale nhanh** với nhiều khách hàng mới
6. **Compliance requirements** khác nhau theo khu vực/ngành

### **❌ KHÔNG NÊN DÙNG KHI:**
1. **Chỉ có 1-2 khách hàng** (overhead không đáng)
2. **Dữ liệu cần share** giữa các đơn vị
3. **Logic business hoàn toàn giống nhau** (không cần tách)
4. **Team nhỏ, kinh nghiệm ít** (phức tạp quản lý)
5. **Performance critical** (single-tenant sẽ nhanh hơn)

---

## ❌ **HIỂU LẦM PHỔ BIẾN VỀ MULTI-TENANCY**

### **Multi-tenancy KHÔNG PHẢI để:**
- ❌ Tách biệt dữ liệu theo **từng user/tài khoản**
- ❌ Mỗi user có "giao diện riêng" 
- ❌ User A không thấy file của User B

### **Multi-tenancy DÙNG ĐỂ:**
- ✅ Tách biệt theo **tổ chức/công ty/khách hàng**
- ✅ Mỗi **tenant** (công ty) có hệ thống riêng
- ✅ Công ty A không thấy dữ liệu của Công ty B

---

## 🔍 **GIẢI PHÁP CHO USER-LEVEL SECURITY**

### **Nếu cần tách biệt dữ liệu theo từng user, sử dụng USER-LEVEL AUTHORIZATION:**

#### **1. 👤 Content Ownership Pattern**
```csharp
// Mỗi Content Item có Owner
public class MyCalculationPart : ContentPart
{
    public string Title { get; set; }
    public string CalculationData { get; set; }
    public string ResultFile { get; set; }
    
    // Quan trọng: Lưu owner của content
    public string OwnerId { get; set; }
    public DateTime CreatedDate { get; set; }
}

// Service để quản lý ownership
public class UserContentService
{
    private readonly IContentManager _contentManager;
    private readonly IHttpContextAccessor _httpContextAccessor;
    
    // Chỉ lấy content của user hiện tại
    public async Task<IEnumerable<ContentItem>> GetUserCalculationsAsync()
    {
        var currentUserId = GetCurrentUserId();
        
        return await _contentManager
            .Query<MyCalculationPart>()
            .Where(x => x.OwnerId == currentUserId)
            .ListAsync();
    }
    
    // Tạo content mới với owner
    public async Task<ContentItem> CreateCalculationAsync(string title, string data)
    {
        var contentItem = await _contentManager.NewAsync("MyCalculation");
        var part = contentItem.As<MyCalculationPart>();
        
        part.Title = title;
        part.CalculationData = data;
        part.OwnerId = GetCurrentUserId(); // Quan trọng!
        part.CreatedDate = DateTime.UtcNow;
        
        await _contentManager.CreateAsync(contentItem);
        return contentItem;
    }
}
```

#### **2. 🔒 Authorization Handler Pattern**
```csharp
// Custom Authorization Requirement
public class OwnershipRequirement : IAuthorizationRequirement
{
    public string Permission { get; }
    
    public OwnershipRequirement(string permission)
    {
        Permission = permission;
    }
}

// Authorization Handler
public class OwnershipAuthorizationHandler : AuthorizationHandler<OwnershipRequirement, ContentItem>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OwnershipRequirement requirement,
        ContentItem resource)
    {
        var currentUserId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var contentOwner = resource.As<MyCalculationPart>()?.OwnerId;
        
        // Chỉ owner mới được truy cập
        if (currentUserId == contentOwner)
        {
            context.Succeed(requirement);
        }
        // Hoặc Admin có thể truy cập tất cả
        else if (context.User.IsInRole("Administrator"))
        {
            context.Succeed(requirement);
        }
        
        return Task.CompletedTask;
    }
}

// Sử dụng trong Controller
[HttpGet("{id}")]
public async Task<IActionResult> GetCalculation(string id)
{
    var contentItem = await _contentManager.GetAsync(id);
    
    // Kiểm tra quyền truy cập
    var authResult = await _authorizationService.AuthorizeAsync(
        User, contentItem, new OwnershipRequirement("ViewOwnContent"));
        
    if (!authResult.Succeeded)
    {
        return Forbid(); // 403 - Không có quyền
    }
    
    return Ok(contentItem);
}
```

#### **3. 📁 User-Scoped File Storage**
```csharp
public class UserFileService
{
    private readonly IMediaFileStore _mediaFileStore;
    private readonly IHttpContextAccessor _httpContextAccessor;
    
    // Lưu file vào thư mục riêng của user
    public async Task<string> SaveUserFileAsync(IFormFile file, string fileName)
    {
        var userId = GetCurrentUserId();
        var userFolder = $"users/{userId}/calculations";
        
        // Tạo đường dẫn file: /Media/users/user123/calculations/file.xlsx
        var filePath = _mediaFileStore.Combine(userFolder, fileName);
        
        using var stream = file.OpenReadStream();
        await _mediaFileStore.CreateFileFromStreamAsync(filePath, stream);
        
        return filePath;
    }
    
    // Chỉ lấy file của user hiện tại
    public async Task<IFileInfo> GetUserFileAsync(string fileName)
    {
        var userId = GetCurrentUserId();
        var filePath = $"users/{userId}/calculations/{fileName}";
        
        var fileInfo = await _mediaFileStore.GetFileInfoAsync(filePath);
        
        // Kiểm tra file có tồn tại và thuộc về user không
        if (fileInfo == null || !filePath.Contains($"users/{userId}/"))
        {
            throw new UnauthorizedAccessException("File not found or access denied");
        }
        
        return fileInfo;
    }
}
```

### **🔍 SO SÁNH: MULTI-TENANCY vs USER-LEVEL SECURITY**

#### **🏢 Multi-tenancy (Tenant-level)**
```
Công ty A (tenant-a.myapp.com)
├── User A1 ──┐
├── User A2   ├── Cùng thấy dữ liệu công ty A
└── User A3 ──┘

Công ty B (tenant-b.myapp.com)  
├── User B1 ──┐
├── User B2   ├── Cùng thấy dữ liệu công ty B
└── User B3 ──┘

❌ Công ty A KHÔNG thấy dữ liệu công ty B
```

#### **👤 User-level Security (User-scoped)**
```
Cùng 1 ứng dụng (myapp.com)
├── User 1 ── Chỉ thấy dữ liệu của mình
├── User 2 ── Chỉ thấy dữ liệu của mình  
├── User 3 ── Chỉ thấy dữ liệu của mình
└── Admin  ── Thấy tất cả (nếu có quyền)
```

### **💡 KẾT LUẬN**

#### **Use case tách biệt dữ liệu theo user cần:**
- ✅ **User-level Authorization** (không phải Multi-tenancy)
- ✅ **Content Ownership** pattern
- ✅ **User-scoped File Storage**
- ✅ **Role-based Access Control**

#### **Kết quả:**
- 🎯 Mỗi user chỉ thấy dữ liệu của mình
- 🔒 File tính toán được bảo vệ theo user
- 🎨 Giao diện cá nhân hóa theo user
- 📊 Dashboard riêng cho từng user

**Multi-tenancy dành cho tách biệt theo tổ chức/công ty, còn tách biệt theo user cần User-level Security!**

---

*Tài liệu này được tạo dựa trên phân tích source code OrchardCore và best practices.*