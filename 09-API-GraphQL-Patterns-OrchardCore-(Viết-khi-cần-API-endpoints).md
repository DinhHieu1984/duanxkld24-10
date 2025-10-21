# API & GraphQL Patterns trong OrchardCore

## 🎯 **MỤC TIÊU**
Tìm hiểu các patterns về **API và GraphQL** để **viết modules OrchardCore có khả năng expose APIs**.

---

## 🚀 **API ARCHITECTURE TRONG ORCHARDCORE**

### **1. Hai Loại API Chính**

#### **A. REST APIs (Traditional)**
- **Controllers**: MVC Controllers với API attributes
- **Authentication**: JWT, API Keys, Cookie
- **Serialization**: JSON.NET / System.Text.Json
- **Routing**: Attribute routing hoặc conventional

#### **B. GraphQL APIs (Modern)**
- **Module**: `OrchardCore.Apis.GraphQL`
- **Schema**: Dynamic schema building
- **Queries**: Type-safe queries với filtering
- **Mutations**: Content creation/updates
- **Subscriptions**: Real-time updates

---

## 🔧 **CORE GRAPHQL PATTERNS**

### **1. GraphQL Module Setup**
```csharp
// Startup.cs
[RequireFeatures("OrchardCore.Apis.GraphQL")]
public sealed class Startup : StartupBase
{
    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddContentGraphQL();
        services.AddSingleton<ISchemaBuilder, MyCustomQuery>();
        services.AddTransient<MyCustomObjectType>();
    }
}
```

### **2. Schema Builder Pattern**
```csharp
public class MyCustomQuery : ISchemaBuilder
{
    private readonly IStringLocalizer S;
    
    public MyCustomQuery(IStringLocalizer<MyCustomQuery> localizer)
    {
        S = localizer;
    }
    
    public Task<string> GetIdentifierAsync()
        => Task.FromResult(string.Empty);
    
    public Task BuildAsync(ISchema schema)
    {
        // Add Query field
        var field = new FieldType
        {
            Name = "myCustomData",
            Description = S["Custom data query"],
            Type = typeof(ListGraphType<MyCustomObjectType>),
            Arguments = new QueryArguments(
                new QueryArgument<StringGraphType>
                {
                    Name = "filter",
                    Description = S["Filter criteria"]
                }
            ),
            Resolver = new FuncFieldResolver<IEnumerable<MyData>>(ResolveAsync)
        };
        
        schema.Query.AddField(field);
        return Task.CompletedTask;
    }
    
    private async ValueTask<IEnumerable<MyData>> ResolveAsync(IResolveFieldContext context)
    {
        var filter = context.GetArgument<string>("filter");
        // Implementation logic
        return await GetDataAsync(filter);
    }
}
```

### **3. Object Type Definition Pattern**
```csharp
public class MyCustomObjectType : ObjectGraphType<MyData>
{
    public MyCustomObjectType()
    {
        Name = "MyData";
        Description = "Custom data object";
        
        Field(x => x.Id)
            .Description("Unique identifier");
            
        Field(x => x.Title)
            .Description("Display title");
            
        Field(x => x.CreatedUtc)
            .Description("Creation date");
            
        // Complex field with resolver
        Field<StringGraphType>("formattedDate")
            .Description("Formatted creation date")
            .Resolve(context => 
            {
                var data = context.Source;
                return data.CreatedUtc.ToString("yyyy-MM-dd");
            });
    }
}
```

### **4. Content Type GraphQL Integration**
```csharp
// Automatic GraphQL exposure for Content Types
public class MyContentPart : ContentPart
{
    public string Title { get; set; }
    public string Description { get; set; }
}

// GraphQL settings in Content Type definition
public class MyContentPartGraphQLSettings
{
    public bool Queryable { get; set; } = true;
    public bool Hidden { get; set; } = false;
}
```

### **5. GraphQL Middleware Configuration**
```csharp
// In GraphQLMiddleware
public async Task InvokeAsync(HttpContext context, RequestDelegate next)
{
    if (!IsGraphQLRequest(context))
    {
        await next(context);
        return;
    }
    
    // Authentication
    var authResult = await authenticationService.AuthenticateAsync(context, "Api");
    if (authResult.Succeeded)
    {
        context.User = authResult.Principal;
    }
    
    // Authorization
    var authorized = await authorizationService.AuthorizeAsync(
        context.User, GraphQLPermissions.ExecuteGraphQL);
        
    if (authorized)
    {
        await ExecuteAsync(context);
    }
    else
    {
        await context.ChallengeAsync("Api");
    }
}
```

---

## 🔴 **REST API PATTERNS**

### **1. API Controller Pattern**
```csharp
[Route("api/[controller]")]
[ApiController]
[Authorize(AuthenticationSchemes = "Api")]
public class MyApiController : ControllerBase
{
    private readonly IContentManager _contentManager;
    private readonly IAuthorizationService _authorizationService;
    
    public MyApiController(
        IContentManager contentManager,
        IAuthorizationService authorizationService)
    {
        _contentManager = contentManager;
        _authorizationService = authorizationService;
    }
    
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string contentType = null)
    {
        if (!await _authorizationService.AuthorizeAsync(User, Permissions.AccessApi))
        {
            return Forbid();
        }
        
        var query = _contentManager.Query();
        
        if (!string.IsNullOrEmpty(contentType))
        {
            query = query.ForType(contentType);
        }
        
        var items = await query.ListAsync();
        return Ok(items.Select(MapToApiModel));
    }
    
    [HttpPost]
    public async Task<IActionResult> Post([FromBody] CreateContentRequest request)
    {
        if (!await _authorizationService.AuthorizeAsync(User, CommonPermissions.PublishContent))
        {
            return Forbid();
        }
        
        var contentItem = await _contentManager.NewAsync(request.ContentType);
        
        // Map request to content item
        MapRequestToContentItem(request, contentItem);
        
        await _contentManager.CreateAsync(contentItem);
        
        return CreatedAtAction(nameof(GetById), 
            new { id = contentItem.ContentItemId }, 
            MapToApiModel(contentItem));
    }
}
```

### **2. API Model Mapping Pattern**
```csharp
public class ContentApiModel
{
    public string ContentItemId { get; set; }
    public string ContentType { get; set; }
    public string DisplayText { get; set; }
    public DateTime? CreatedUtc { get; set; }
    public DateTime? ModifiedUtc { get; set; }
    public Dictionary<string, object> Parts { get; set; } = new();
}

public static class ContentItemExtensions
{
    public static ContentApiModel ToApiModel(this ContentItem contentItem)
    {
        return new ContentApiModel
        {
            ContentItemId = contentItem.ContentItemId,
            ContentType = contentItem.ContentType,
            DisplayText = contentItem.DisplayText,
            CreatedUtc = contentItem.CreatedUtc,
            ModifiedUtc = contentItem.ModifiedUtc,
            Parts = contentItem.Content.ToDictionary()
        };
    }
}
```

### **3. API Authentication Pattern**
```csharp
// JWT Authentication setup
public void ConfigureServices(IServiceCollection services)
{
    services.AddAuthentication("Api")
        .AddJwtBearer("Api", options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = Configuration["Jwt:Issuer"],
                ValidAudience = Configuration["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(Configuration["Jwt:Key"]))
            };
        });
}
```

---

## 📊 **ADVANCED PATTERNS**

### **1. GraphQL Filtering Pattern**
```csharp
public class ContentItemFilters : GraphQLFilter<ContentItem>
{
    public override void Configure(IInputObjectGraphType<ContentItem> filter)
    {
        filter.Field<StringGraphType>("contentType")
            .Description("Filter by content type");
            
        filter.Field<DateTimeGraphType>("createdFrom")
            .Description("Filter by creation date from");
            
        filter.Field<BooleanGraphType>("published")
            .Description("Filter by publication status");
    }
    
    public override IQuery<ContentItem> PreQuery(
        IQuery<ContentItem> query, 
        IResolveFieldContext context)
    {
        var contentType = context.GetArgument<string>("contentType");
        if (!string.IsNullOrEmpty(contentType))
        {
            query = query.Where(x => x.ContentType == contentType);
        }
        
        var createdFrom = context.GetArgument<DateTime?>("createdFrom");
        if (createdFrom.HasValue)
        {
            query = query.Where(x => x.CreatedUtc >= createdFrom.Value);
        }
        
        return query;
    }
}
```

### **2. GraphQL Mutation Pattern**
```csharp
public class MyMutations : ISchemaBuilder
{
    public Task BuildAsync(ISchema schema)
    {
        var field = new FieldType
        {
            Name = "createMyContent",
            Type = typeof(MyContentObjectType),
            Arguments = new QueryArguments(
                new QueryArgument<NonNullGraphType<MyContentInputType>>
                {
                    Name = "input",
                    Description = "Content input data"
                }
            ),
            Resolver = new FuncFieldResolver<ContentItem>(CreateContentAsync)
        };
        
        schema.Mutation.AddField(field);
        return Task.CompletedTask;
    }
    
    private async ValueTask<ContentItem> CreateContentAsync(IResolveFieldContext context)
    {
        var input = context.GetArgument<MyContentInput>("input");
        var contentManager = context.RequestServices.GetService<IContentManager>();
        
        var contentItem = await contentManager.NewAsync("MyContentType");
        
        // Map input to content item
        var part = contentItem.As<MyContentPart>();
        part.Title = input.Title;
        part.Description = input.Description;
        
        await contentManager.CreateAsync(contentItem);
        return contentItem;
    }
}
```

### **3. API Versioning Pattern**
```csharp
[ApiVersion("1.0")]
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class MyApiController : ControllerBase
{
    [HttpGet]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> GetV1()
    {
        // Version 1.0 implementation
        return Ok(await GetDataV1Async());
    }
    
    [HttpGet]
    [MapToApiVersion("2.0")]
    public async Task<IActionResult> GetV2()
    {
        // Version 2.0 implementation with enhanced features
        return Ok(await GetDataV2Async());
    }
}
```

### **4. API Rate Limiting Pattern**
```csharp
public class ApiRateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IMemoryCache _cache;
    
    public async Task InvokeAsync(HttpContext context)
    {
        var clientId = GetClientId(context);
        var key = $"rate_limit_{clientId}";
        
        if (_cache.TryGetValue(key, out int requestCount))
        {
            if (requestCount >= 100) // 100 requests per hour
            {
                context.Response.StatusCode = 429; // Too Many Requests
                return;
            }
            
            _cache.Set(key, requestCount + 1, TimeSpan.FromHours(1));
        }
        else
        {
            _cache.Set(key, 1, TimeSpan.FromHours(1));
        }
        
        await _next(context);
    }
}
```

---

## 🎯 **BEST PRACTICES**

### **✅ ĐÚNG:**
- Use GraphQL for complex queries
- Use REST for simple CRUD operations
- Implement proper authentication/authorization
- Version your APIs
- Use DTOs for API models
- Implement rate limiting
- Add comprehensive error handling
- Document APIs (Swagger/GraphQL Playground)

### **❌ SAI:**
- Expose internal models directly
- Skip authentication/authorization
- No API versioning strategy
- Missing error handling
- No rate limiting
- Inconsistent naming conventions
- Poor documentation

---

## 🔧 **IMPLEMENTATION CHECKLIST**

### **GraphQL APIs:**
- [ ] Install OrchardCore.Apis.GraphQL
- [ ] Create ISchemaBuilder implementations
- [ ] Define ObjectGraphType classes
- [ ] Implement proper resolvers
- [ ] Add filtering capabilities
- [ ] Configure permissions
- [ ] Test with GraphQL Playground

### **REST APIs:**
- [ ] Create API controllers
- [ ] Implement authentication
- [ ] Add authorization checks
- [ ] Create API models/DTOs
- [ ] Implement error handling
- [ ] Add API versioning
- [ ] Configure Swagger documentation
- [ ] Implement rate limiting

---

*Tài liệu này được tạo dựa trên phân tích source code OrchardCore và best practices.*