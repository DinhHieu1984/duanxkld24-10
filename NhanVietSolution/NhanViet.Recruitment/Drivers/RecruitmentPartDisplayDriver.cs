using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using NhanViet.Recruitment.Models;
using NhanViet.Recruitment.ViewModels;
using OrchardCore.ContentManagement.Display.ContentDisplay;
using OrchardCore.ContentManagement.Display.Models;
using OrchardCore.DisplayManagement.Views;

namespace NhanViet.Recruitment.Drivers;

public sealed class RecruitmentPartDisplayDriver : ContentPartDisplayDriver<RecruitmentPart>
{
    private readonly IAuthorizationService _authorizationService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public RecruitmentPartDisplayDriver(
        IAuthorizationService authorizationService,
        IHttpContextAccessor httpContextAccessor)
    {
        _authorizationService = authorizationService;
        _httpContextAccessor = httpContextAccessor;
    }

    public override async Task<IDisplayResult> DisplayAsync(RecruitmentPart recruitmentPart, BuildPartDisplayContext context)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        
        // Kiểm tra permission ViewRecruitment trước khi hiển thị
        if (user != null && !await _authorizationService.AuthorizeAsync(user, Permissions.ViewRecruitment, recruitmentPart.ContentItem))
        {
            // Trả về empty result nếu không có permission
            return Task.FromResult<IDisplayResult?>(null).Result!;
        }

        return Initialize<RecruitmentPartViewModel>(GetDisplayShapeType(context), m => BuildViewModel(m, recruitmentPart))
            .Location("Detail", "Content:10")
            .Location("Summary", "Content:10");
    }

    public override async Task<IDisplayResult> EditAsync(RecruitmentPart recruitmentPart, BuildPartEditorContext context)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        
        // Kiểm tra permission EditRecruitment trước khi hiển thị editor
        if (user != null && !await _authorizationService.AuthorizeAsync(user, Permissions.EditRecruitment, recruitmentPart.ContentItem))
        {
            // Trả về empty result nếu không có permission
            return Task.FromResult<IDisplayResult?>(null).Result!;
        }

        return Initialize<RecruitmentPartViewModel>(GetEditorShapeType(context), m => BuildViewModel(m, recruitmentPart));
    }

    public override async Task<IDisplayResult> UpdateAsync(RecruitmentPart model, UpdatePartEditorContext context)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        
        // Kiểm tra permission EditRecruitment trước khi update
        if (user != null && !await _authorizationService.AuthorizeAsync(user, Permissions.EditRecruitment, model.ContentItem))
        {
            // Không update nếu không có permission
            return await EditAsync(model, context);
        }

        await context.Updater.TryUpdateModelAsync(model, Prefix, 
            t => t.CandidateName, 
            t => t.Email, 
            t => t.Phone,
            t => t.Position,
            t => t.ResumeUrl,
            t => t.CoverLetter,
            t => t.Experience,
            t => t.Education,
            t => t.Skills,
            t => t.ApplicationDate,
            t => t.Status,
            t => t.Notes,
            t => t.YearsOfExperience,
            t => t.PreferredLocation,
            t => t.ExpectedSalary,
            t => t.IsAvailable,
            t => t.AvailableFrom,
            t => t.LinkedInProfile,
            t => t.PortfolioUrl,
            t => t.References);

        return await EditAsync(model, context);
    }

    private static void BuildViewModel(RecruitmentPartViewModel model, RecruitmentPart part)
    {
        model.CandidateName = part.CandidateName;
        model.Email = part.Email;
        model.Phone = part.Phone;
        model.Position = part.Position;
        model.ResumeUrl = part.ResumeUrl;
        model.CoverLetter = part.CoverLetter;
        model.Experience = part.Experience;
        model.Education = part.Education;
        model.Skills = part.Skills;
        model.ApplicationDate = part.ApplicationDate;
        model.Status = part.Status;
        model.Notes = part.Notes;
        model.YearsOfExperience = part.YearsOfExperience;
        model.PreferredLocation = part.PreferredLocation;
        model.ExpectedSalary = part.ExpectedSalary;
        model.IsAvailable = part.IsAvailable;
        model.AvailableFrom = part.AvailableFrom;
        model.LinkedInProfile = part.LinkedInProfile;
        model.PortfolioUrl = part.PortfolioUrl;
        model.References = part.References;
        model.RecruitmentPart = part;
        model.ContentItem = part.ContentItem;
    }
}