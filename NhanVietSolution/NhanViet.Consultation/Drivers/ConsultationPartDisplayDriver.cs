using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using NhanViet.Consultation.Models;
using NhanViet.Consultation.ViewModels;
using OrchardCore.ContentManagement.Display.ContentDisplay;
using OrchardCore.ContentManagement.Display.Models;
using OrchardCore.DisplayManagement.Views;

namespace NhanViet.Consultation.Drivers;

public sealed class ConsultationPartDisplayDriver : ContentPartDisplayDriver<ConsultationPart>
{
    private readonly IAuthorizationService _authorizationService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ConsultationPartDisplayDriver(
        IAuthorizationService authorizationService,
        IHttpContextAccessor httpContextAccessor)
    {
        _authorizationService = authorizationService;
        _httpContextAccessor = httpContextAccessor;
    }

    public override async Task<IDisplayResult> DisplayAsync(ConsultationPart consultationPart, BuildPartDisplayContext context)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        
        // Kiểm tra permission ViewConsultation trước khi hiển thị
        if (user != null && !await _authorizationService.AuthorizeAsync(user, Permissions.ViewConsultation, consultationPart.ContentItem))
        {
            // Trả về empty result nếu không có permission
            return null;
        }

        return Initialize<ConsultationPartViewModel>(GetDisplayShapeType(context), m => BuildViewModel(m, consultationPart))
            .Location("Detail", "Content:10")
            .Location("Summary", "Content:10");
    }

    public override async Task<IDisplayResult> EditAsync(ConsultationPart consultationPart, BuildPartEditorContext context)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        
        // Kiểm tra permission EditConsultation trước khi hiển thị editor
        if (user != null && !await _authorizationService.AuthorizeAsync(user, Permissions.EditConsultation, consultationPart.ContentItem))
        {
            // Trả về empty result nếu không có permission
            return null;
        }

        return Initialize<ConsultationPartViewModel>(GetEditorShapeType(context), m => BuildViewModel(m, consultationPart));
    }

    public override async Task<IDisplayResult> UpdateAsync(ConsultationPart model, UpdatePartEditorContext context)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        
        // Kiểm tra permission EditConsultation trước khi update
        if (user != null && !await _authorizationService.AuthorizeAsync(user, Permissions.EditConsultation, model.ContentItem))
        {
            // Không update nếu không có permission
            return await EditAsync(model, context);
        }

        await context.Updater.TryUpdateModelAsync(model, Prefix, 
            t => t.ClientName, 
            t => t.Email, 
            t => t.Phone,
            t => t.Company,
            t => t.ServiceType,
            t => t.Subject,
            t => t.Message,
            t => t.RequestDate,
            t => t.Status,
            t => t.Priority,
            t => t.AssignedTo,
            t => t.Notes,
            t => t.FollowUpDate,
            t => t.PreferredContactMethod,
            t => t.Country,
            t => t.Industry,
            t => t.EstimatedBudget,
            t => t.Requirements,
            t => t.CompletedDate,
            t => t.Rating,
            t => t.Feedback);

        return await EditAsync(model, context);
    }

    private static void BuildViewModel(ConsultationPartViewModel model, ConsultationPart part)
    {
        model.ClientName = part.ClientName;
        model.Email = part.Email;
        model.Phone = part.Phone;
        model.Company = part.Company;
        model.ServiceType = part.ServiceType;
        model.Subject = part.Subject;
        model.Message = part.Message;
        model.RequestDate = part.RequestDate;
        model.Status = part.Status;
        model.Priority = part.Priority;
        model.AssignedTo = part.AssignedTo;
        model.Notes = part.Notes;
        model.FollowUpDate = part.FollowUpDate;
        model.PreferredContactMethod = part.PreferredContactMethod;
        model.Country = part.Country;
        model.Industry = part.Industry;
        model.EstimatedBudget = part.EstimatedBudget;
        model.Requirements = part.Requirements;
        model.CompletedDate = part.CompletedDate;
        model.Rating = part.Rating;
        model.Feedback = part.Feedback;
        model.ConsultationPart = part;
        model.ContentItem = part.ContentItem;
    }
}