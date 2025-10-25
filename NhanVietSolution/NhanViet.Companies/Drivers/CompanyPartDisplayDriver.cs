using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using NhanViet.Companies.Models;
using NhanViet.Companies.ViewModels;
using OrchardCore.ContentManagement.Display.ContentDisplay;
using OrchardCore.ContentManagement.Display.Models;
using OrchardCore.DisplayManagement.Views;

namespace NhanViet.Companies.Drivers;

public sealed class CompanyPartDisplayDriver : ContentPartDisplayDriver<CompanyPart>
{
    private readonly IAuthorizationService _authorizationService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CompanyPartDisplayDriver(
        IAuthorizationService authorizationService,
        IHttpContextAccessor httpContextAccessor)
    {
        _authorizationService = authorizationService;
        _httpContextAccessor = httpContextAccessor;
    }

    public override async Task<IDisplayResult> DisplayAsync(CompanyPart companyPart, BuildPartDisplayContext context)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        
        // Kiểm tra permission ViewCompanies trước khi hiển thị
        if (user != null && !await _authorizationService.AuthorizeAsync(user, Permissions.ViewCompanies, companyPart.ContentItem))
        {
            // Trả về empty result nếu không có permission
            return Task.FromResult<IDisplayResult?>(null).Result!;
        }

        return Initialize<CompanyPartViewModel>(GetDisplayShapeType(context), m => BuildViewModel(m, companyPart))
            .Location("Detail", "Content:10")
            .Location("Summary", "Content:10");
    }

    public override async Task<IDisplayResult> EditAsync(CompanyPart companyPart, BuildPartEditorContext context)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        
        // Kiểm tra permission EditCompany trước khi hiển thị editor
        if (user != null && !await _authorizationService.AuthorizeAsync(user, Permissions.EditCompany, companyPart.ContentItem))
        {
            // Trả về empty result nếu không có permission
            return Task.FromResult<IDisplayResult?>(null).Result!;
        }

        return Initialize<CompanyPartViewModel>(GetEditorShapeType(context), m => BuildViewModel(m, companyPart));
    }

    public override async Task<IDisplayResult> UpdateAsync(CompanyPart model, UpdatePartEditorContext context)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        
        // Kiểm tra permission EditCompany trước khi update
        if (user != null && !await _authorizationService.AuthorizeAsync(user, Permissions.EditCompany, model.ContentItem))
        {
            // Không update nếu không có permission
            return await EditAsync(model, context);
        }

        await context.Updater.TryUpdateModelAsync(model, Prefix, 
            t => t.CompanyName, 
            t => t.Industry, 
            t => t.Location,
            t => t.Website,
            t => t.ContactEmail,
            t => t.ContactPhone,
            t => t.Description,
            t => t.EmployeeCount,
            t => t.EstablishedDate,
            t => t.LogoUrl,
            t => t.IsVerified);

        return await EditAsync(model, context);
    }

    private static void BuildViewModel(CompanyPartViewModel model, CompanyPart part)
    {
        model.CompanyName = part.CompanyName;
        model.Industry = part.Industry;
        model.Location = part.Location;
        model.Website = part.Website;
        model.ContactEmail = part.ContactEmail;
        model.ContactPhone = part.ContactPhone;
        model.Description = part.Description;
        model.EmployeeCount = part.EmployeeCount;
        model.EstablishedDate = part.EstablishedDate;
        model.LogoUrl = part.LogoUrl;
        model.IsVerified = part.IsVerified;
        model.CompanyPart = part;
        model.ContentItem = part.ContentItem;
    }
}