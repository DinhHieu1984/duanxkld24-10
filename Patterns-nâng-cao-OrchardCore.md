# Patterns Nâng Cao OrchardCore - Quy Định Chi Tiết

## Mục Lục
1. [Content Management Patterns](#content-management-patterns)
2. [Security và Permission Patterns](#security-và-permission-patterns)
3. [Background Tasks và Jobs](#background-tasks-và-jobs)
4. [Caching và Performance](#caching-và-performance)
5. [Localization và Multi-tenancy](#localization-và-multi-tenancy)
6. [API và GraphQL Patterns](#api-và-graphql-patterns)
7. [Display Management](#display-management)
8. [Event Handling](#event-handling)

---

## Content Management Patterns

### 🔴 QUY ĐỊNH BẮT BUỘC CHO CONTENT PARTS

#### 1. Content Part Definition
```csharp
// ✅ ĐÚNG: Content Part chuẩn OrchardCore
namespace MyCompany.MyModule.Models
{
    /// <summary>
    /// ✅ ĐÚNG: Content Part với proper documentation
    /// - Kế thừa từ ContentPart
    /// - Sử dụng ContentFields
    /// - Properties có getter/setter
    /// - Naming convention: {Purpose}Part
    /// </summary>
    public class MyCustomPart : ContentPart
    {
        /// <summary>
        /// ✅ ĐÚNG: Sử dụng ContentFields thay vì primitive types
        /// </summary>
        public TextField Title { get; set; } = new();
        public HtmlField Description { get; set; } = new();
        public BooleanField IsActive { get; set; } = new();
        public DateTimeField CreatedDate { get; set; } = new();
        public NumericField Priority { get; set; } = new();
        
        /// <summary>
        /// ✅ ĐÚNG: Complex fields với proper initialization
        /// </summary>
        public ContentPickerField RelatedItems { get; set; } = new();
        public MediaField FeaturedImage { get; set; } = new();
        public LinkField ExternalLink { get; set; } = new();
    }
}

// ❌ SAI: Content Part không chuẩn
public class MyPart : ContentPart  // Tên không rõ ràng
{
    // ❌ SAI: Sử dụng primitive types thay vì ContentFields
    public string Title { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedDate { get; set; }
    
    // ❌ SAI: Không có initialization
    public ContentPickerField RelatedItems { get; set; }
}
```

#### 2. Content Part Display Driver
```csharp
// ✅ ĐÚNG: Display Driver chuẩn OrchardCore
namespace MyCompany.MyModule.Drivers
{
    /// <summary>
    /// ✅ ĐÚNG: Display Driver tuân thủ tất cả quy định
    /// - Kế thừa từ ContentPartDisplayDriver<T>
    /// - Naming convention: {PartName}DisplayDriver
    /// - Proper dependency injection
    /// - Location specifications
    /// - Authorization checks
    /// </summary>
    public sealed class MyCustomPartDisplayDriver : ContentPartDisplayDriver<MyCustomPart>
    {
        private readonly IAuthorizationService _authorizationService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IStringLocalizer<MyCustomPartDisplayDriver> _stringLocalizer;
        private readonly ILogger<MyCustomPartDisplayDriver> _logger;
        
        public MyCustomPartDisplayDriver(
            IAuthorizationService authorizationService,
            IHttpContextAccessor httpContextAccessor,
            IStringLocalizer<MyCustomPartDisplayDriver> stringLocalizer,
            ILogger<MyCustomPartDisplayDriver> logger)
        {
            _authorizationService = authorizationService;
            _httpContextAccessor = httpContextAccessor;
            _stringLocalizer = stringLocalizer;
            _logger = logger;
        }
        
        /// <summary>
        /// ✅ ĐÚNG: Display method với proper shape naming và location
        /// </summary>
        public override IDisplayResult Display(MyCustomPart part, BuildPartDisplayContext context)
        {
            return Initialize<MyCustomPartViewModel>($"{nameof(MyCustomPart)}_Display", 
                viewModel => PopulateDisplayViewModel(part, viewModel))
                .Location("Detail", "Content:10")
                .Location("Summary", "Meta:5");
        }
        
        /// <summary>
        /// ✅ ĐÚNG: Edit method với authorization check
        /// </summary>
        public override async Task<IDisplayResult> EditAsync(MyCustomPart part, BuildPartEditorContext context)
        {
            var user = _httpContextAccessor.HttpContext?.User;
            
            // ✅ QUY ĐỊNH: Authorization check trước khi hiển thị editor
            if (!await _authorizationService.AuthorizeAsync(user, CommonPermissions.EditContent, part.ContentItem))
            {
                return null;
            }
            
            return Initialize<MyCustomPartEditViewModel>(GetEditorShapeType(context),
                viewModel => PopulateEditViewModel(part, viewModel))
                .Location("Parts", "Content:10");
        }
        
        /// <summary>
        /// ✅ ĐÚNG: Update method với validation và error handling
        /// </summary>
        public override async Task<IDisplayResult> UpdateAsync(MyCustomPart part, UpdatePartEditorContext context)
        {
            var user = _httpContextAccessor.HttpContext?.User;
            
            // ✅ QUY ĐỊNH: Authorization check
            if (!await _authorizationService.AuthorizeAsync(user, CommonPermissions.EditContent, part.ContentItem))
            {
                return null;
            }
            
            var viewModel = new MyCustomPartEditViewModel();
            
            try
            {
                // ✅ QUY ĐỊNH: Model binding với error handling
                await context.Updater.TryUpdateModelAsync(viewModel, Prefix);
                
                // ✅ QUY ĐỊNH: Custom validation
                if (!ValidateViewModel(viewModel, context))
                {
                    return await EditAsync(part, context);
                }
                
                // ✅ QUY ĐỊNH: Update part properties
                UpdatePartFromViewModel(part, viewModel);
                
                _logger.LogInformation("MyCustomPart updated successfully for content item {ContentItemId}", 
                    part.ContentItem.ContentItemId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating MyCustomPart for content item {ContentItemId}", 
                    part.ContentItem.ContentItemId);
                    
                context.Updater.ModelState.AddModelError(Prefix, 
                    _stringLocalizer["An error occurred while updating the part."]);
            }
            
            return await EditAsync(part, context);
        }
        
        /// <summary>
        /// ✅ ĐÚNG: Helper methods với proper separation of concerns
        /// </summary>
        private void PopulateDisplayViewModel(MyCustomPart part, MyCustomPartViewModel viewModel)
        {
            viewModel.Title = part.Title.Text;
            viewModel.Description = part.Description.Html;
            viewModel.IsActive = part.IsActive.Value;
            viewModel.CreatedDate = part.CreatedDate.Value;
            viewModel.Priority = (int)(part.Priority.Value ?? 0);
            viewModel.ContentItem = part.ContentItem;
        }
        
        private void PopulateEditViewModel(MyCustomPart part, MyCustomPartEditViewModel viewModel)
        {
            viewModel.Title = part.Title.Text;
            viewModel.Description = part.Description.Html;
            viewModel.IsActive = part.IsActive.Value;
            viewModel.CreatedDate = part.CreatedDate.Value;
            viewModel.Priority = (int)(part.Priority.Value ?? 0);
        }
        
        private bool ValidateViewModel(MyCustomPartEditViewModel viewModel, UpdatePartEditorContext context)
        {
            var isValid = true;
            
            if (string.IsNullOrWhiteSpace(viewModel.Title))
            {
                context.Updater.ModelState.AddModelError(
                    Prefix, nameof(viewModel.Title), 
                    _stringLocalizer["Title is required."]);
                isValid = false;
            }
            
            if (viewModel.Priority < 0 || viewModel.Priority > 100)
            {
                context.Updater.ModelState.AddModelError(
                    Prefix, nameof(viewModel.Priority), 
                    _stringLocalizer["Priority must be between 0 and 100."]);
                isValid = false;
            }
            
            return isValid;
        }
        
        private void UpdatePartFromViewModel(MyCustomPart part, MyCustomPartEditViewModel viewModel)
        {
            part.Title.Text = viewModel.Title?.Trim();
            part.Description.Html = viewModel.Description;
            part.IsActive.Value = viewModel.IsActive;
            part.CreatedDate.Value = viewModel.CreatedDate;
            part.Priority.Value = viewModel.Priority;
        }
    }
}

// ❌ SAI: Display Driver không chuẩn
public class MyDriver : ContentPartDisplayDriver<MyCustomPart>  // Tên không rõ ràng
{
    // ❌ SAI: Không có dependencies injection
    
    public override IDisplayResult Display(MyCustomPart part, BuildPartDisplayContext context)
    {
        // ❌ SAI: Không có shape naming convention
        return Initialize<MyCustomPartViewModel>("MyPart", viewModel => { });
        // ❌ SAI: Không có location specification
    }
    
    public override async Task<IDisplayResult> UpdateAsync(MyCustomPart part, UpdatePartEditorContext context)
    {
        // ❌ SAI: Không có authorization check
        // ❌ SAI: Không có error handling
        var viewModel = new MyCustomPartEditViewModel();
        await context.Updater.TryUpdateModelAsync(viewModel, Prefix);
        
        // ❌ SAI: Direct assignment without validation
        part.Title.Text = viewModel.Title;
        
        return Edit(part, context);
    }
}
```

#### 3. Content Part Handler
```csharp
// ✅ ĐÚNG: Content Part Handler chuẩn OrchardCore
namespace MyCompany.MyModule.Handlers
{
    /// <summary>
    /// ✅ ĐÚNG: Content Part Handler tuân thủ quy định
    /// - Kế thừa từ ContentPartHandler<T>
    /// - Proper lifecycle event handling
    /// - Indexing support
    /// - Validation logic
    /// </summary>
    public class MyCustomPartHandler : ContentPartHandler<MyCustomPart>
    {
        private readonly ILogger<MyCustomPartHandler> _logger;
        private readonly IMyCustomService _myCustomService;
        
        public MyCustomPartHandler(
            ILogger<MyCustomPartHandler> logger,
            IMyCustomService myCustomService)
        {
            _logger = logger;
            _myCustomService = myCustomService;
        }
        
        /// <summary>
        /// ✅ ĐÚNG: Activating event với initialization logic
        /// </summary>
        public override Task ActivatingAsync(ActivatingContentContext context, MyCustomPart part)
        {
            // ✅ QUY ĐỊNH: Initialize default values
            part.IsActive.Value ??= true;
            part.CreatedDate.Value ??= DateTime.UtcNow;
            part.Priority.Value ??= 50;
            
            return Task.CompletedTask;
        }
        
        /// <summary>
        /// ✅ ĐÚNG: Validating event với business logic validation
        /// </summary>
        public override async Task ValidatingAsync(ValidateContentContext context, MyCustomPart part)
        {
            // ✅ QUY ĐỊNH: Business logic validation
            if (part.IsActive.Value == true && string.IsNullOrWhiteSpace(part.Title.Text))
            {
                context.Fail(_stringLocalizer["Active items must have a title."], nameof(part.Title));
            }
            
            // ✅ QUY ĐỊNH: Async validation với external services
            if (!string.IsNullOrWhiteSpace(part.Title.Text))
            {
                var isDuplicate = await _myCustomService.IsTitleDuplicateAsync(
                    part.Title.Text, part.ContentItem.ContentItemId);
                    
                if (isDuplicate)
                {
                    context.Fail(_stringLocalizer["Title must be unique."], nameof(part.Title));
                }
            }
        }
        
        /// <summary>
        /// ✅ ĐÚNG: Creating event với pre-creation logic
        /// </summary>
        public override async Task CreatingAsync(CreateContentContext context, MyCustomPart part)
        {
            // ✅ QUY ĐỊNH: Pre-creation business logic
            if (part.CreatedDate.Value == null)
            {
                part.CreatedDate.Value = DateTime.UtcNow;
            }
            
            // ✅ QUY ĐỊNH: Generate default values
            if (string.IsNullOrWhiteSpace(part.Title.Text))
            {
                part.Title.Text = await _myCustomService.GenerateDefaultTitleAsync();
            }
            
            _logger.LogInformation("Creating MyCustomPart for content item {ContentItemId}", 
                context.ContentItem.ContentItemId);
        }
        
        /// <summary>
        /// ✅ ĐÚNG: Created event với post-creation logic
        /// </summary>
        public override async Task CreatedAsync(CreateContentContext context, MyCustomPart part)
        {
            // ✅ QUY ĐỊNH: Post-creation side effects
            await _myCustomService.NotifyCreationAsync(part);
            
            _logger.LogInformation("MyCustomPart created for content item {ContentItemId}", 
                context.ContentItem.ContentItemId);
        }
        
        /// <summary>
        /// ✅ ĐÚNG: Publishing event với pre-publish logic
        /// </summary>
        public override async Task PublishingAsync(PublishContentContext context, MyCustomPart part)
        {
            // ✅ QUY ĐỊNH: Pre-publish validation
            if (part.IsActive.Value == false)
            {
                _logger.LogWarning("Publishing inactive MyCustomPart for content item {ContentItemId}", 
                    context.ContentItem.ContentItemId);
            }
            
            // ✅ QUY ĐỊNH: Update timestamps
            part.CreatedDate.Value = DateTime.UtcNow;
        }
        
        /// <summary>
        /// ✅ ĐÚNG: Published event với post-publish logic
        /// </summary>
        public override async Task PublishedAsync(PublishContentContext context, MyCustomPart part)
        {
            // ✅ QUY ĐỊNH: Post-publish side effects
            await _myCustomService.NotifyPublishedAsync(part);
            
            // ✅ QUY ĐỊNH: Update search indexes
            await _myCustomService.UpdateSearchIndexAsync(part);
            
            _logger.LogInformation("MyCustomPart published for content item {ContentItemId}", 
                context.ContentItem.ContentItemId);
        }
        
        /// <summary>
        /// ✅ ĐÚNG: Removing event với cleanup logic
        /// </summary>
        public override async Task RemovingAsync(RemoveContentContext context, MyCustomPart part)
        {
            // ✅ QUY ĐỊNH: Cleanup related data
            await _myCustomService.CleanupRelatedDataAsync(part);
            
            _logger.LogInformation("Removing MyCustomPart for content item {ContentItemId}", 
                context.ContentItem.ContentItemId);
        }
        
        /// <summary>
        /// ✅ ĐÚNG: GetContentItemAspect cho custom aspects
        /// </summary>
        public override Task GetContentItemAspectAsync(ContentItemAspectContext context, MyCustomPart part)
        {
            return context.ForAsync<MyCustomAspect>(aspect =>
            {
                aspect.Title = part.Title.Text;
                aspect.IsActive = part.IsActive.Value ?? false;
                aspect.Priority = (int)(part.Priority.Value ?? 0);
                return Task.CompletedTask;
            });
        }
    }
    
    /// <summary>
    /// ✅ ĐÚNG: Custom aspect definition
    /// </summary>
    public class MyCustomAspect
    {
        public string Title { get; set; }
        public bool IsActive { get; set; }
        public int Priority { get; set; }
    }
}

// ❌ SAI: Content Part Handler không chuẩn
public class MyHandler : ContentPartHandler<MyCustomPart>  // Tên không rõ ràng
{
    // ❌ SAI: Không có dependencies injection
    
    public override Task CreatingAsync(CreateContentContext context, MyCustomPart part)
    {
        // ❌ SAI: Hard-coded values
        part.Title.Text = "Default Title";
        part.IsActive.Value = true;
        
        // ❌ SAI: Không có logging
        // ❌ SAI: Không có error handling
        
        return Task.CompletedTask;
    }
    
    public override Task ValidatingAsync(ValidateContentContext context, MyCustomPart part)
    {
        // ❌ SAI: Validation logic không đầy đủ
        if (string.IsNullOrEmpty(part.Title.Text))
        {
            context.Fail("Title is required");  // Không localized
        }
        
        return Task.CompletedTask;
    }
}
```

---

## Security và Permission Patterns

### 🔴 QUY ĐỊNH BẮT BUỘC CHO PERMISSIONS

#### 1. Permission Provider
```csharp
// ✅ ĐÚNG: Permission Provider chuẩn OrchardCore
namespace MyCompany.MyModule
{
    /// <summary>
    /// ✅ ĐÚNG: Permission Provider tuân thủ quy định
    /// - Implement IPermissionProvider
    /// - Static readonly permissions
    /// - Proper naming conventions
    /// - Default stereotypes
    /// - Localized descriptions
    /// </summary>
    public sealed class Permissions : IPermissionProvider
    {
        // ✅ QUY ĐỊNH: Static readonly permissions với descriptive names
        public static readonly Permission ManageMyModule = new(
            "ManageMyModule", 
            "Manage My Module",
            new[] { CommonPermissions.EditContent });
            
        public static readonly Permission ViewMyModule = new(
            "ViewMyModule", 
            "View My Module");
            
        public static readonly Permission CreateMyModuleContent = new(
            "CreateMyModuleContent", 
            "Create My Module Content",
            new[] { ManageMyModule });
            
        public static readonly Permission EditMyModuleContent = new(
            "EditMyModuleContent", 
            "Edit My Module Content",
            new[] { ManageMyModule });
            
        public static readonly Permission DeleteMyModuleContent = new(
            "DeleteMyModuleContent", 
            "Delete My Module Content",
            new[] { ManageMyModule });
            
        public static readonly Permission PublishMyModuleContent = new(
            "PublishMyModuleContent", 
            "Publish My Module Content",
            new[] { EditMyModuleContent });
        
        // ✅ QUY ĐỊNH: All permissions collection
        private readonly IEnumerable<Permission> _allPermissions = new[]
        {
            ManageMyModule,
            ViewMyModule,
            CreateMyModuleContent,
            EditMyModuleContent,
            DeleteMyModuleContent,
            PublishMyModuleContent
        };
        
        /// <summary>
        /// ✅ ĐÚNG: GetPermissionsAsync implementation
        /// </summary>
        public Task<IEnumerable<Permission>> GetPermissionsAsync()
            => Task.FromResult(_allPermissions);
        
        /// <summary>
        /// ✅ ĐÚNG: Default stereotypes với proper role assignments
        /// </summary>
        public IEnumerable<PermissionStereotype> GetDefaultStereotypes() => new[]
        {
            new PermissionStereotype
            {
                Name = OrchardCoreConstants.Roles.Administrator,
                Permissions = _allPermissions
            },
            new PermissionStereotype
            {
                Name = OrchardCoreConstants.Roles.Editor,
                Permissions = new[]
                {
                    ViewMyModule,
                    CreateMyModuleContent,
                    EditMyModuleContent,
                    PublishMyModuleContent
                }
            },
            new PermissionStereotype
            {
                Name = OrchardCoreConstants.Roles.Author,
                Permissions = new[]
                {
                    ViewMyModule,
                    CreateMyModuleContent,
                    EditMyModuleContent
                }
            },
            new PermissionStereotype
            {
                Name = OrchardCoreConstants.Roles.Contributor,
                Permissions = new[]
                {
                    ViewMyModule,
                    CreateMyModuleContent
                }
            }
        };
    }
}

// ❌ SAI: Permission Provider không chuẩn
public class MyPermissions : IPermissionProvider  // Tên không theo convention
{
    // ❌ SAI: Không static readonly
    public Permission Manage = new("Manage", "Manage");  // Tên quá chung
    public Permission View = new("View", "View");        // Tên quá chung
    
    // ❌ SAI: Không có hierarchy
    // ❌ SAI: Không có implied permissions
    
    public Task<IEnumerable<Permission>> GetPermissionsAsync()
    {
        // ❌ SAI: Hard-coded array
        return Task.FromResult(new[] { Manage, View }.AsEnumerable());
    }
    
    public IEnumerable<PermissionStereotype> GetDefaultStereotypes()
    {
        // ❌ SAI: Không có default stereotypes
        return Enumerable.Empty<PermissionStereotype>();
    }
}
```

#### 2. Authorization trong Controllers
```csharp
// ✅ ĐÚNG: Controller với proper authorization
namespace MyCompany.MyModule.Controllers
{
    /// <summary>
    /// ✅ ĐÚNG: Controller với comprehensive authorization
    /// </summary>
    [Route("api/mymodule")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "Api")]
    public sealed class MyModuleApiController : ControllerBase
    {
        private readonly IAuthorizationService _authorizationService;
        private readonly IMyModuleService _myModuleService;
        private readonly ILogger<MyModuleApiController> _logger;
        private readonly IStringLocalizer<MyModuleApiController> _stringLocalizer;
        
        public MyModuleApiController(
            IAuthorizationService authorizationService,
            IMyModuleService myModuleService,
            ILogger<MyModuleApiController> logger,
            IStringLocalizer<MyModuleApiController> stringLocalizer)
        {
            _authorizationService = authorizationService;
            _myModuleService = myModuleService;
            _logger = logger;
            _stringLocalizer = stringLocalizer;
        }
        
        /// <summary>
        /// ✅ ĐÚNG: GET endpoint với proper authorization
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            // ✅ QUY ĐỊNH: Authorization check trước khi xử lý
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ViewMyModule))
            {
                return this.ChallengeOrForbid("Api");
            }
            
            try
            {
                var items = await _myModuleService.GetAllAsync();
                return Ok(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving MyModule items");
                return StatusCode(500, _stringLocalizer["An error occurred while retrieving items."]);
            }
        }
        
        /// <summary>
        /// ✅ ĐÚNG: POST endpoint với validation và authorization
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateMyModuleItemRequest request)
        {
            // ✅ QUY ĐỊNH: Authorization check
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.CreateMyModuleContent))
            {
                return this.ChallengeOrForbid("Api");
            }
            
            // ✅ QUY ĐỊNH: Model validation
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            
            try
            {
                // ✅ QUY ĐỊNH: Additional business validation
                var validationResult = await _myModuleService.ValidateCreateRequestAsync(request);
                if (!validationResult.IsValid)
                {
                    foreach (var error in validationResult.Errors)
                    {
                        ModelState.AddModelError(error.Key, error.Value);
                    }
                    return BadRequest(ModelState);
                }
                
                var item = await _myModuleService.CreateAsync(request);
                
                _logger.LogInformation("MyModule item created with ID {ItemId} by user {UserId}", 
                    item.Id, User.Identity.Name);
                
                return CreatedAtAction(nameof(GetById), new { id = item.Id }, item);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating MyModule item");
                return StatusCode(500, _stringLocalizer["An error occurred while creating the item."]);
            }
        }
        
        /// <summary>
        /// ✅ ĐÚNG: PUT endpoint với resource-specific authorization
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateMyModuleItemRequest request)
        {
            try
            {
                // ✅ QUY ĐỊNH: Get resource first
                var existingItem = await _myModuleService.GetByIdAsync(id);
                if (existingItem == null)
                {
                    return NotFound();
                }
                
                // ✅ QUY ĐỊNH: Resource-specific authorization
                if (!await _authorizationService.AuthorizeAsync(User, Permissions.EditMyModuleContent, existingItem))
                {
                    return this.ChallengeOrForbid("Api");
                }
                
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }
                
                var updatedItem = await _myModuleService.UpdateAsync(id, request);
                
                _logger.LogInformation("MyModule item {ItemId} updated by user {UserId}", 
                    id, User.Identity.Name);
                
                return Ok(updatedItem);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating MyModule item {ItemId}", id);
                return StatusCode(500, _stringLocalizer["An error occurred while updating the item."]);
            }
        }
        
        /// <summary>
        /// ✅ ĐÚNG: DELETE endpoint với confirmation
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id, [FromQuery] bool confirm = false)
        {
            try
            {
                var existingItem = await _myModuleService.GetByIdAsync(id);
                if (existingItem == null)
                {
                    return NotFound();
                }
                
                // ✅ QUY ĐỊNH: Resource-specific authorization
                if (!await _authorizationService.AuthorizeAsync(User, Permissions.DeleteMyModuleContent, existingItem))
                {
                    return this.ChallengeOrForbid("Api");
                }
                
                // ✅ QUY ĐỊNH: Confirmation for destructive operations
                if (!confirm)
                {
                    return BadRequest(_stringLocalizer["Confirmation required for delete operation."]);
                }
                
                await _myModuleService.DeleteAsync(id);
                
                _logger.LogWarning("MyModule item {ItemId} deleted by user {UserId}", 
                    id, User.Identity.Name);
                
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting MyModule item {ItemId}", id);
                return StatusCode(500, _stringLocalizer["An error occurred while deleting the item."]);
            }
        }
        
        /// <summary>
        /// ✅ ĐÚNG: Bulk operations với proper authorization
        /// </summary>
        [HttpPost("bulk-delete")]
        public async Task<IActionResult> BulkDelete([FromBody] BulkDeleteRequest request)
        {
            // ✅ QUY ĐỊNH: General permission check
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.DeleteMyModuleContent))
            {
                return this.ChallengeOrForbid("Api");
            }
            
            if (!ModelState.IsValid || !request.ItemIds?.Any() == true)
            {
                return BadRequest(ModelState);
            }
            
            try
            {
                var results = new List<BulkOperationResult>();
                
                foreach (var itemId in request.ItemIds)
                {
                    try
                    {
                        var item = await _myModuleService.GetByIdAsync(itemId);
                        if (item == null)
                        {
                            results.Add(new BulkOperationResult 
                            { 
                                ItemId = itemId, 
                                Success = false, 
                                Error = "Item not found" 
                            });
                            continue;
                        }
                        
                        // ✅ QUY ĐỊNH: Individual authorization check
                        if (!await _authorizationService.AuthorizeAsync(User, Permissions.DeleteMyModuleContent, item))
                        {
                            results.Add(new BulkOperationResult 
                            { 
                                ItemId = itemId, 
                                Success = false, 
                                Error = "Access denied" 
                            });
                            continue;
                        }
                        
                        await _myModuleService.DeleteAsync(itemId);
                        results.Add(new BulkOperationResult { ItemId = itemId, Success = true });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error deleting item {ItemId} in bulk operation", itemId);
                        results.Add(new BulkOperationResult 
                        { 
                            ItemId = itemId, 
                            Success = false, 
                            Error = "Internal error" 
                        });
                    }
                }
                
                var successCount = results.Count(r => r.Success);
                _logger.LogInformation("Bulk delete completed: {SuccessCount}/{TotalCount} items deleted by user {UserId}", 
                    successCount, results.Count, User.Identity.Name);
                
                return Ok(new BulkDeleteResponse { Results = results });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in bulk delete operation");
                return StatusCode(500, _stringLocalizer["An error occurred during bulk delete operation."]);
            }
        }
    }
    
    // ✅ ĐÚNG: Request/Response models
    public class CreateMyModuleItemRequest
    {
        [Required]
        [StringLength(255)]
        public string Title { get; set; }
        
        [StringLength(1000)]
        public string Description { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        [Range(0, 100)]
        public int Priority { get; set; } = 50;
    }
    
    public class UpdateMyModuleItemRequest
    {
        [StringLength(255)]
        public string Title { get; set; }
        
        [StringLength(1000)]
        public string Description { get; set; }
        
        public bool? IsActive { get; set; }
        
        [Range(0, 100)]
        public int? Priority { get; set; }
    }
    
    public class BulkDeleteRequest
    {
        [Required]
        public string[] ItemIds { get; set; }
    }
    
    public class BulkDeleteResponse
    {
        public List<BulkOperationResult> Results { get; set; }
    }
    
    public class BulkOperationResult
    {
        public string ItemId { get; set; }
        public bool Success { get; set; }
        public string Error { get; set; }
    }
}

// ❌ SAI: Controller không có authorization
[Route("api/mymodule")]
[ApiController]
public class MyController : ControllerBase  // Tên không rõ ràng
{
    private readonly IMyModuleService _service;
    
    public MyController(IMyModuleService service)
    {
        _service = service;
    }
    
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        // ❌ SAI: Không có authorization check
        var items = await _service.GetAllAsync();
        return Ok(items);  // ❌ SAI: Không có error handling
    }
    
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] object request)  // ❌ SAI: object type
    {
        // ❌ SAI: Không có authorization
        // ❌ SAI: Không có validation
        // ❌ SAI: Không có error handling
        // ❌ SAI: Không có logging
        
        var item = await _service.CreateAsync(request);
        return Ok(item);
    }
    
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        // ❌ SAI: Không có authorization
        // ❌ SAI: Không có confirmation cho destructive operation
        // ❌ SAI: Không kiểm tra resource existence
        
        await _service.DeleteAsync(id);
        return Ok();
    }
}
```

---

## Background Tasks và Jobs

### 🔴 QUY ĐỊNH BẮT BUỘC CHO BACKGROUND TASKS

#### 1. Background Task Implementation
```csharp
// ✅ ĐÚNG: Background Task chuẩn OrchardCore
namespace MyCompany.MyModule.Services
{
    /// <summary>
    /// ✅ ĐÚNG: Background Task tuân thủ quy định
    /// - BackgroundTask attribute với proper configuration
    /// - Implement IBackgroundTask
    /// - Proper error handling và logging
    /// - Cancellation token support
    /// - Service resolution từ IServiceProvider
    /// </summary>
    [BackgroundTask(
        Title = "My Module Data Processor",
        Schedule = "*/5 * * * *",  // Every 5 minutes
        Description = "Processes pending data items and performs cleanup operations for My Module.")]
    public sealed class MyModuleBackgroundTask : IBackgroundTask
    {
        private readonly ILogger<MyModuleBackgroundTask> _logger;
        private readonly IClock _clock;
        
        public MyModuleBackgroundTask(
            ILogger<MyModuleBackgroundTask> logger,
            IClock clock)
        {
            _logger = logger;
            _clock = clock;
        }
        
        /// <summary>
        /// ✅ ĐÚNG: DoWorkAsync implementation với comprehensive error handling
        /// </summary>
        public async Task DoWorkAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("MyModule background task started at {StartTime}", _clock.UtcNow);
                
                // ✅ QUY ĐỊNH: Resolve services từ serviceProvider
                var myModuleService = serviceProvider.GetRequiredService<IMyModuleService>();
                var session = serviceProvider.GetRequiredService<ISession>();
                
                // ✅ QUY ĐỊNH: Check cancellation token
                cancellationToken.ThrowIfCancellationRequested();
                
                // ✅ QUY ĐỊNH: Process pending items
                await ProcessPendingItemsAsync(myModuleService, session, cancellationToken);
                
                // ✅ QUY ĐỊNH: Check cancellation token between operations
                cancellationToken.ThrowIfCancellationRequested();
                
                // ✅ QUY ĐỊNH: Cleanup expired items
                await CleanupExpiredItemsAsync(myModuleService, session, cancellationToken);
                
                // ✅ QUY ĐỊNH: Update statistics
                await UpdateStatisticsAsync(myModuleService, cancellationToken);
                
                _logger.LogInformation("MyModule background task completed successfully at {EndTime}", _clock.UtcNow);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("MyModule background task was cancelled");
                throw; // ✅ QUY ĐỊNH: Re-throw cancellation exceptions
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in MyModule background task");
                // ✅ QUY ĐỊNH: KHÔNG throw exception để không crash background service
            }
        }
        
        /// <summary>
        /// ✅ ĐÚNG: Helper method với proper error handling
        /// </summary>
        private async Task ProcessPendingItemsAsync(
            IMyModuleService myModuleService, 
            ISession session, 
            CancellationToken cancellationToken)
        {
            try
            {
                // ✅ QUY ĐỊNH: Query với proper filtering
                var pendingItems = await session
                    .Query<ContentItem, MyModuleIndex>(index => 
                        index.Status == "Pending" && 
                        index.ScheduledProcessingDate <= _clock.UtcNow)
                    .Take(100) // ✅ QUY ĐỊNH: Limit batch size
                    .ListAsync(cancellationToken);
                
                if (!pendingItems.Any())
                {
                    _logger.LogDebug("No pending items found for processing");
                    return;
                }
                
                _logger.LogInformation("Processing {ItemCount} pending items", pendingItems.Count());
                
                var processedCount = 0;
                var errorCount = 0;
                
                foreach (var item in pendingItems)
                {
                    try
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        
                        await myModuleService.ProcessItemAsync(item.ContentItemId);
                        processedCount++;
                        
                        _logger.LogDebug("Processed item {ContentItemId}", item.ContentItemId);
                    }
                    catch (OperationCanceledException)
                    {
                        throw; // ✅ QUY ĐỊNH: Re-throw cancellation
                    }
                    catch (Exception ex)
                    {
                        errorCount++;
                        _logger.LogError(ex, "Error processing item {ContentItemId}", item.ContentItemId);
                        
                        // ✅ QUY ĐỊNH: Continue processing other items
                        continue;
                    }
                }
                
                _logger.LogInformation("Processed {ProcessedCount} items successfully, {ErrorCount} errors", 
                    processedCount, errorCount);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ProcessPendingItemsAsync");
                throw; // ✅ QUY ĐỊNH: Re-throw để caller xử lý
            }
        }
        
        /// <summary>
        /// ✅ ĐÚNG: Cleanup method với proper date handling
        /// </summary>
        private async Task CleanupExpiredItemsAsync(
            IMyModuleService myModuleService, 
            ISession session, 
            CancellationToken cancellationToken)
        {
            try
            {
                // ✅ QUY ĐỊNH: Calculate expiry date
                var expiryDate = _clock.UtcNow.AddDays(-30); // 30 days old
                
                var expiredItems = await session
                    .Query<ContentItem, MyModuleIndex>(index => 
                        index.Status == "Processed" && 
                        index.ProcessedDate < expiryDate)
                    .Take(50) // ✅ QUY ĐỊNH: Smaller batch for cleanup
                    .ListAsync(cancellationToken);
                
                if (!expiredItems.Any())
                {
                    _logger.LogDebug("No expired items found for cleanup");
                    return;
                }
                
                _logger.LogInformation("Cleaning up {ItemCount} expired items", expiredItems.Count());
                
                var cleanedCount = 0;
                
                foreach (var item in expiredItems)
                {
                    try
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        
                        await myModuleService.CleanupItemAsync(item.ContentItemId);
                        cleanedCount++;
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error cleaning up item {ContentItemId}", item.ContentItemId);
                        continue;
                    }
                }
                
                _logger.LogInformation("Cleaned up {CleanedCount} expired items", cleanedCount);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CleanupExpiredItemsAsync");
                throw;
            }
        }
        
        /// <summary>
        /// ✅ ĐÚNG: Statistics update method
        /// </summary>
        private async Task UpdateStatisticsAsync(IMyModuleService myModuleService, CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                await myModuleService.UpdateStatisticsAsync();
                _logger.LogDebug("Statistics updated successfully");
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating statistics");
                // ✅ QUY ĐỊNH: Không throw - statistics update không critical
            }
        }
    }
}

// ❌ SAI: Background Task không chuẩn
[BackgroundTask(Title = "Task", Schedule = "* * * * *")]  // Tên và mô tả không rõ ràng
public class MyTask : IBackgroundTask  // Tên không theo convention
{
    // ❌ SAI: Không có dependencies injection
    
    public async Task DoWorkAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        // ❌ SAI: Không có error handling
        // ❌ SAI: Không check cancellation token
        // ❌ SAI: Không có logging
        
        var service = serviceProvider.GetRequiredService<IMyModuleService>();
        
        // ❌ SAI: Có thể throw exception và crash background service
        await service.ProcessAllItemsAsync();
        
        // ❌ SAI: Không có batch processing - có thể xử lý quá nhiều items
        // ❌ SAI: Không có progress tracking
    }
}
```

#### 2. Scheduled Background Task
```csharp
// ✅ ĐÚNG: Scheduled Background Task với flexible scheduling
namespace MyCompany.MyModule.Services
{
    /// <summary>
    /// ✅ ĐÚNG: Scheduled task với configurable schedule
    /// </summary>
    [BackgroundTask(
        Title = "My Module Report Generator",
        Schedule = "0 2 * * *",  // Daily at 2 AM
        Description = "Generates daily reports and sends notifications for My Module.")]
    public sealed class MyModuleReportGeneratorTask : IBackgroundTask
    {
        private readonly ILogger<MyModuleReportGeneratorTask> _logger;
        private readonly IClock _clock;
        
        public MyModuleReportGeneratorTask(
            ILogger<MyModuleReportGeneratorTask> logger,
            IClock clock)
        {
            _logger = logger;
            _clock = clock;
        }
        
        public async Task DoWorkAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Starting daily report generation at {StartTime}", _clock.UtcNow);
                
                // ✅ QUY ĐỊNH: Resolve services
                var reportService = serviceProvider.GetRequiredService<IMyModuleReportService>();
                var notificationService = serviceProvider.GetRequiredService<INotificationService>();
                var siteService = serviceProvider.GetRequiredService<ISiteService>();
                
                // ✅ QUY ĐỊNH: Check if reports are enabled
                var settings = await siteService.GetSettingsAsync<MyModuleSettings>();
                if (!settings.EnableDailyReports)
                {
                    _logger.LogInformation("Daily reports are disabled, skipping report generation");
                    return;
                }
                
                cancellationToken.ThrowIfCancellationRequested();
                
                // ✅ QUY ĐỊNH: Generate report for previous day
                var reportDate = _clock.UtcNow.Date.AddDays(-1);
                var report = await reportService.GenerateDailyReportAsync(reportDate, cancellationToken);
                
                cancellationToken.ThrowIfCancellationRequested();
                
                // ✅ QUY ĐỊNH: Save report
                await reportService.SaveReportAsync(report, cancellationToken);
                
                // ✅ QUY ĐỊNH: Send notifications if configured
                if (settings.SendReportNotifications)
                {
                    await SendReportNotificationsAsync(notificationService, report, cancellationToken);
                }
                
                _logger.LogInformation("Daily report generation completed successfully for {ReportDate}", reportDate);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Daily report generation was cancelled");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during daily report generation");
            }
        }
        
        private async Task SendReportNotificationsAsync(
            INotificationService notificationService, 
            DailyReport report, 
            CancellationToken cancellationToken)
        {
            try
            {
                var recipients = await GetReportRecipientsAsync(notificationService);
                
                foreach (var recipient in recipients)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    try
                    {
                        await notificationService.SendReportNotificationAsync(recipient, report);
                        _logger.LogDebug("Report notification sent to {Recipient}", recipient);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send report notification to {Recipient}", recipient);
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending report notifications");
            }
        }
        
        private async Task<IEnumerable<string>> GetReportRecipientsAsync(INotificationService notificationService)
        {
            // Implementation to get report recipients
            return await notificationService.GetReportRecipientsAsync();
        }
    }
}
```

---

## Caching và Performance

### 🔴 QUY ĐỊNH BẮT BUỘC CHO CACHING

#### 1. Service với Caching
```csharp
// ✅ ĐÚNG: Service với comprehensive caching strategy
namespace MyCompany.MyModule.Services
{
    /// <summary>
    /// ✅ ĐÚNG: Service với proper caching implementation
    /// - Multiple cache layers (Memory + Distributed)
    /// - Cache invalidation strategies
    /// - Performance monitoring
    /// - Fallback mechanisms
    /// </summary>
    public class MyModuleService : IMyModuleService
    {
        private readonly IMemoryCache _memoryCache;
        private readonly IDistributedCache _distributedCache;
        private readonly ITagCache _tagCache;
        private readonly ISession _session;
        private readonly ILogger<MyModuleService> _logger;
        private readonly IClock _clock;
        
        // ✅ QUY ĐỊNH: Cache key constants
        private const string CACHE_KEY_PREFIX = "MyModule:";
        private const string CACHE_KEY_ALL_ITEMS = CACHE_KEY_PREFIX + "AllItems";
        private const string CACHE_KEY_ITEM = CACHE_KEY_PREFIX + "Item:{0}";
        private const string CACHE_KEY_CATEGORY = CACHE_KEY_PREFIX + "Category:{0}";
        private const string CACHE_KEY_STATISTICS = CACHE_KEY_PREFIX + "Statistics";
        
        // ✅ QUY ĐỊNH: Cache tags for invalidation
        private const string CACHE_TAG_ITEMS = "MyModule_Items";
        private const string CACHE_TAG_CATEGORIES = "MyModule_Categories";
        private const string CACHE_TAG_STATISTICS = "MyModule_Statistics";
        
        // ✅ QUY ĐỊNH: Cache durations
        private static readonly TimeSpan ShortCacheDuration = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan MediumCacheDuration = TimeSpan.FromMinutes(30);
        private static readonly TimeSpan LongCacheDuration = TimeSpan.FromHours(2);
        
        public MyModuleService(
            IMemoryCache memoryCache,
            IDistributedCache distributedCache,
            ITagCache tagCache,
            ISession session,
            ILogger<MyModuleService> logger,
            IClock clock)
        {
            _memoryCache = memoryCache;
            _distributedCache = distributedCache;
            _tagCache = tagCache;
            _session = session;
            _logger = logger;
            _clock = clock;
        }
        
        /// <summary>
        /// ✅ ĐÚNG: Get method với multi-layer caching
        /// </summary>
        public async Task<MyModuleItem> GetByIdAsync(string id)
        {
            var cacheKey = string.Format(CACHE_KEY_ITEM, id);
            
            // ✅ QUY ĐỊNH: Try memory cache first (fastest)
            if (_memoryCache.TryGetValue(cacheKey, out MyModuleItem cachedItem))
            {
                _logger.LogDebug("Item {ItemId} retrieved from memory cache", id);
                return cachedItem;
            }
            
            // ✅ QUY ĐỊNH: Try distributed cache second
            try
            {
                var distributedCachedData = await _distributedCache.GetStringAsync(cacheKey);
                if (!string.IsNullOrEmpty(distributedCachedData))
                {
                    var distributedCachedItem = JsonSerializer.Deserialize<MyModuleItem>(distributedCachedData);
                    
                    // ✅ QUY ĐỊNH: Store in memory cache for faster subsequent access
                    _memoryCache.Set(cacheKey, distributedCachedItem, ShortCacheDuration);
                    
                    _logger.LogDebug("Item {ItemId} retrieved from distributed cache", id);
                    return distributedCachedItem;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to retrieve item {ItemId} from distributed cache", id);
                // ✅ QUY ĐỊNH: Continue to database fallback
            }
            
            // ✅ QUY ĐỊNH: Fallback to database
            var item = await GetItemFromDatabaseAsync(id);
            if (item != null)
            {
                // ✅ QUY ĐỊNH: Cache the result in both layers
                await CacheItemAsync(cacheKey, item, MediumCacheDuration);
            }
            
            return item;
        }
        
        /// <summary>
        /// ✅ ĐÚNG: Get all method với caching và pagination
        /// </summary>
        public async Task<IEnumerable<MyModuleItem>> GetAllAsync(int page = 1, int pageSize = 20)
        {
            var cacheKey = $"{CACHE_KEY_ALL_ITEMS}:Page{page}:Size{pageSize}";
            
            // ✅ QUY ĐỊNH: Check memory cache first
            if (_memoryCache.TryGetValue(cacheKey, out IEnumerable<MyModuleItem> cachedItems))
            {
                _logger.LogDebug("Items page {Page} retrieved from memory cache", page);
                return cachedItems;
            }
            
            // ✅ QUY ĐỊNH: Fallback to database with caching
            var items = await GetItemsFromDatabaseAsync(page, pageSize);
            
            // ✅ QUY ĐỊNH: Cache with tags for invalidation
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = MediumCacheDuration,
                Priority = CacheItemPriority.Normal
            };
            
            _memoryCache.Set(cacheKey, items, cacheOptions);
            
            // ✅ QUY ĐỊNH: Tag cache for invalidation
            await _tagCache.TagAsync(cacheKey, CACHE_TAG_ITEMS);
            
            _logger.LogDebug("Items page {Page} cached for {Duration}", page, MediumCacheDuration);
            
            return items;
        }
        
        /// <summary>
        /// ✅ ĐÚNG: Create method với cache invalidation
        /// </summary>
        public async Task<MyModuleItem> CreateAsync(CreateMyModuleItemRequest request)
        {
            try
            {
                var item = await CreateItemInDatabaseAsync(request);
                
                // ✅ QUY ĐỊNH: Invalidate related caches
                await InvalidateItemCachesAsync();
                
                // ✅ QUY ĐỊNH: Cache the new item
                var cacheKey = string.Format(CACHE_KEY_ITEM, item.Id);
                await CacheItemAsync(cacheKey, item, MediumCacheDuration);
                
                _logger.LogInformation("Item {ItemId} created and cached", item.Id);
                
                return item;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating item");
                throw;
            }
        }
        
        /// <summary>
        /// ✅ ĐÚNG: Update method với selective cache invalidation
        /// </summary>
        public async Task<MyModuleItem> UpdateAsync(string id, UpdateMyModuleItemRequest request)
        {
            try
            {
                var item = await UpdateItemInDatabaseAsync(id, request);
                
                // ✅ QUY ĐỊNH: Invalidate specific item cache
                var cacheKey = string.Format(CACHE_KEY_ITEM, id);
                _memoryCache.Remove(cacheKey);
                await _distributedCache.RemoveAsync(cacheKey);
                
                // ✅ QUY ĐỊNH: Invalidate list caches
                await InvalidateItemCachesAsync();
                
                // ✅ QUY ĐỊNH: Cache updated item
                await CacheItemAsync(cacheKey, item, MediumCacheDuration);
                
                _logger.LogInformation("Item {ItemId} updated and cache refreshed", id);
                
                return item;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating item {ItemId}", id);
                throw;
            }
        }
        
        /// <summary>
        /// ✅ ĐÚNG: Delete method với comprehensive cache cleanup
        /// </summary>
        public async Task DeleteAsync(string id)
        {
            try
            {
                await DeleteItemFromDatabaseAsync(id);
                
                // ✅ QUY ĐỊNH: Remove specific item cache
                var cacheKey = string.Format(CACHE_KEY_ITEM, id);
                _memoryCache.Remove(cacheKey);
                await _distributedCache.RemoveAsync(cacheKey);
                
                // ✅ QUY ĐỊNH: Invalidate all related caches
                await InvalidateItemCachesAsync();
                
                _logger.LogInformation("Item {ItemId} deleted and cache cleaned", id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting item {ItemId}", id);
                throw;
            }
        }
        
        /// <summary>
        /// ✅ ĐÚNG: Statistics method với long-term caching
        /// </summary>
        public async Task<MyModuleStatistics> GetStatisticsAsync()
        {
            const string cacheKey = CACHE_KEY_STATISTICS;
            
            // ✅ QUY ĐỊNH: Check cache first
            if (_memoryCache.TryGetValue(cacheKey, out MyModuleStatistics cachedStats))
            {
                return cachedStats;
            }
            
            // ✅ QUY ĐỊNH: Calculate statistics (expensive operation)
            var stats = await CalculateStatisticsAsync();
            
            // ✅ QUY ĐỊNH: Cache with longer duration for expensive operations
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = LongCacheDuration,
                Priority = CacheItemPriority.High,
                Size = 1
            };
            
            _memoryCache.Set(cacheKey, stats, cacheOptions);
            await _tagCache.TagAsync(cacheKey, CACHE_TAG_STATISTICS);
            
            _logger.LogDebug("Statistics calculated and cached for {Duration}", LongCacheDuration);
            
            return stats;
        }
        
        /// <summary>
        /// ✅ ĐÚNG: Cache warming method
        /// </summary>
        public async Task WarmupCacheAsync()
        {
            try
            {
                _logger.LogInformation("Starting cache warmup");
                
                // ✅ QUY ĐỊNH: Warm up frequently accessed data
                var tasks = new List<Task>
                {
                    GetAllAsync(1, 20),  // First page
                    GetStatisticsAsync(),
                    WarmupPopularItemsAsync()
                };
                
                await Task.WhenAll(tasks);
                
                _logger.LogInformation("Cache warmup completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during cache warmup");
            }
        }
        
        /// <summary>
        /// ✅ ĐÚNG: Helper methods
        /// </summary>
        private async Task CacheItemAsync(string cacheKey, MyModuleItem item, TimeSpan duration)
        {
            try
            {
                // ✅ QUY ĐỊNH: Memory cache
                _memoryCache.Set(cacheKey, item, duration);
                
                // ✅ QUY ĐỊNH: Distributed cache
                var serializedItem = JsonSerializer.Serialize(item);
                var distributedCacheOptions = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = duration
                };
                
                await _distributedCache.SetStringAsync(cacheKey, serializedItem, distributedCacheOptions);
                
                // ✅ QUY ĐỊNH: Tag for invalidation
                await _tagCache.TagAsync(cacheKey, CACHE_TAG_ITEMS);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to cache item with key {CacheKey}", cacheKey);
            }
        }
        
        private async Task InvalidateItemCachesAsync()
        {
            try
            {
                // ✅ QUY ĐỊNH: Invalidate by tags
                await _tagCache.RemoveTagAsync(CACHE_TAG_ITEMS);
                
                _logger.LogDebug("Item caches invalidated");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to invalidate item caches");
            }
        }
        
        private async Task WarmupPopularItemsAsync()
        {
            try
            {
                var popularItemIds = await GetPopularItemIdsAsync();
                
                var tasks = popularItemIds.Select(id => GetByIdAsync(id));
                await Task.WhenAll(tasks);
                
                _logger.LogDebug("Warmed up {Count} popular items", popularItemIds.Count());
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to warmup popular items");
            }
        }
        
        // Database methods (implementation details)
        private async Task<MyModuleItem> GetItemFromDatabaseAsync(string id) { /* Implementation */ return null; }
        private async Task<IEnumerable<MyModuleItem>> GetItemsFromDatabaseAsync(int page, int pageSize) { /* Implementation */ return null; }
        private async Task<MyModuleItem> CreateItemInDatabaseAsync(CreateMyModuleItemRequest request) { /* Implementation */ return null; }
        private async Task<MyModuleItem> UpdateItemInDatabaseAsync(string id, UpdateMyModuleItemRequest request) { /* Implementation */ return null; }
        private async Task DeleteItemFromDatabaseAsync(string id) { /* Implementation */ }
        private async Task<MyModuleStatistics> CalculateStatisticsAsync() { /* Implementation */ return null; }
        private async Task<IEnumerable<string>> GetPopularItemIdsAsync() { /* Implementation */ return null; }
    }
}

// ❌ SAI: Service không có caching strategy
public class MyService : IMyModuleService
{
    private readonly ISession _session;
    
    public MyService(ISession session)
    {
        _session = session;
    }
    
    public async Task<MyModuleItem> GetByIdAsync(string id)
    {
        // ❌ SAI: Luôn query database, không cache
        return await _session.Query<MyModuleItem>()
            .Where(x => x.Id == id)
            .FirstOrDefaultAsync();
    }
    
    public async Task<IEnumerable<MyModuleItem>> GetAllAsync()
    {
        // ❌ SAI: Expensive query không cache
        return await _session.Query<MyModuleItem>()
            .ListAsync();
    }
    
    public async Task<MyModuleItem> UpdateAsync(string id, UpdateMyModuleItemRequest request)
    {
        var item = await GetByIdAsync(id);
        // Update item...
        await _session.SaveAsync(item);
        
        // ❌ SAI: Không invalidate cache
        return item;
    }
}
```

Tôi sẽ tiếp tục với các patterns còn lại trong phần tiếp theo...