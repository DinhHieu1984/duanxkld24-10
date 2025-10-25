using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using NhanViet.JobOrders.Models;
using NhanViet.JobOrders.ViewModels;
using OrchardCore.ContentManagement.Display.ContentDisplay;
using OrchardCore.ContentManagement.Display.Models;
using OrchardCore.DisplayManagement.Views;

namespace NhanViet.JobOrders.Drivers;

public sealed class JobOrderPartDisplayDriver : ContentPartDisplayDriver<JobOrderPart>
{
    private readonly IAuthorizationService _authorizationService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public JobOrderPartDisplayDriver(
        IAuthorizationService authorizationService,
        IHttpContextAccessor httpContextAccessor)
    {
        _authorizationService = authorizationService;
        _httpContextAccessor = httpContextAccessor;
    }

    public override async Task<IDisplayResult> DisplayAsync(JobOrderPart jobOrderPart, BuildPartDisplayContext context)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        
        // Kiểm tra permission ViewJobOrders trước khi hiển thị
        if (user != null && !await _authorizationService.AuthorizeAsync(user, Permissions.ViewJobOrders, jobOrderPart.ContentItem))
        {
            // Trả về empty result nếu không có permission
            return null;
        }

        return Initialize<JobOrderPartViewModel>(GetDisplayShapeType(context), m => BuildViewModel(m, jobOrderPart))
            .Location("Detail", "Content:10")
            .Location("Summary", "Content:10");
    }

    public override async Task<IDisplayResult> EditAsync(JobOrderPart jobOrderPart, BuildPartEditorContext context)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        
        // Kiểm tra permission EditJobOrders trước khi hiển thị editor
        if (user != null && !await _authorizationService.AuthorizeAsync(user, Permissions.EditJobOrders, jobOrderPart.ContentItem))
        {
            // Trả về empty result nếu không có permission
            return null;
        }

        return Initialize<JobOrderPartViewModel>(GetEditorShapeType(context), m => BuildViewModel(m, jobOrderPart));
    }

    public override async Task<IDisplayResult> UpdateAsync(JobOrderPart model, UpdatePartEditorContext context)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        
        // Kiểm tra permission EditJobOrders trước khi update
        if (user != null && !await _authorizationService.AuthorizeAsync(user, Permissions.EditJobOrders, model.ContentItem))
        {
            // Không update nếu không có permission
            return await EditAsync(model, context);
        }

        await context.Updater.TryUpdateModelAsync(model, Prefix, 
            t => t.JobTitle, 
            t => t.JobDescription, 
            t => t.Requirements,
            t => t.Benefits,
            t => t.Location,
            t => t.SalaryRange,
            t => t.JobType,
            t => t.ExperienceLevel,
            t => t.PostedDate,
            t => t.ExpiryDate,
            t => t.ContactEmail,
            t => t.ContactPhone,
            t => t.CompanyName,
            t => t.IsActive,
            t => t.IsFeatured,
            t => t.ApplicationCount);

        return await EditAsync(model, context);
    }

    private static void BuildViewModel(JobOrderPartViewModel model, JobOrderPart part)
    {
        model.JobTitle = part.JobTitle;
        model.JobDescription = part.JobDescription;
        model.Requirements = part.Requirements;
        model.Benefits = part.Benefits;
        model.Location = part.Location;
        model.SalaryRange = part.SalaryRange;
        model.JobType = part.JobType;
        model.ExperienceLevel = part.ExperienceLevel;
        model.PostedDate = part.PostedDate;
        model.ExpiryDate = part.ExpiryDate;
        model.ContactEmail = part.ContactEmail;
        model.ContactPhone = part.ContactPhone;
        model.CompanyName = part.CompanyName;
        model.IsActive = part.IsActive;
        model.IsFeatured = part.IsFeatured;
        model.ApplicationCount = part.ApplicationCount;
        model.JobOrderPart = part;
        model.ContentItem = part.ContentItem;
    }
}