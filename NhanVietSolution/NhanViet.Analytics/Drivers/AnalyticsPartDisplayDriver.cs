using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using NhanViet.Analytics.Models;
using NhanViet.Analytics.ViewModels;
using OrchardCore.ContentManagement.Display.ContentDisplay;
using OrchardCore.ContentManagement.Display.Models;
using OrchardCore.DisplayManagement.Views;

namespace NhanViet.Analytics.Drivers;

public sealed class AnalyticsPartDisplayDriver : ContentPartDisplayDriver<AnalyticsPart>
{
    private readonly IAuthorizationService _authorizationService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AnalyticsPartDisplayDriver(
        IAuthorizationService authorizationService,
        IHttpContextAccessor httpContextAccessor)
    {
        _authorizationService = authorizationService;
        _httpContextAccessor = httpContextAccessor;
    }

    public override async Task<IDisplayResult> DisplayAsync(AnalyticsPart analyticsPart, BuildPartDisplayContext context)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        
        // Kiểm tra permission ViewAnalyticsDashboard trước khi hiển thị
        if (user != null && !await _authorizationService.AuthorizeAsync(user, Permissions.ViewAnalyticsDashboard, analyticsPart.ContentItem))
        {
            // Trả về empty result nếu không có permission
            return Task.FromResult<IDisplayResult?>(null).Result;
        }

        return Initialize<AnalyticsPartViewModel>(GetDisplayShapeType(context), m => BuildViewModel(m, analyticsPart))
            .Location("Detail", "Content:10")
            .Location("Summary", "Content:10");
    }

    public override async Task<IDisplayResult> EditAsync(AnalyticsPart analyticsPart, BuildPartEditorContext context)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        
        // Kiểm tra permission ManageAnalytics trước khi hiển thị editor
        if (user != null && !await _authorizationService.AuthorizeAsync(user, Permissions.ManageAnalytics, analyticsPart.ContentItem))
        {
            // Trả về empty result nếu không có permission
            return Task.FromResult<IDisplayResult?>(null).Result;
        }

        return Initialize<AnalyticsPartViewModel>(GetEditorShapeType(context), m => BuildViewModel(m, analyticsPart));
    }

    public override async Task<IDisplayResult> UpdateAsync(AnalyticsPart model, UpdatePartEditorContext context)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        
        // Kiểm tra permission ManageAnalytics trước khi update
        if (user != null && !await _authorizationService.AuthorizeAsync(user, Permissions.ManageAnalytics, model.ContentItem))
        {
            // Không update nếu không có permission
            return await EditAsync(model, context);
        }

        await context.Updater.TryUpdateModelAsync(model, Prefix, 
            t => t.TrackingCode, 
            t => t.EnableTracking, 
            t => t.AnalyticsProvider);

        return await EditAsync(model, context);
    }

    private static void BuildViewModel(AnalyticsPartViewModel model, AnalyticsPart part)
    {
        model.TrackingCode = part.TrackingCode;
        model.EnableTracking = part.EnableTracking;
        model.AnalyticsProvider = part.AnalyticsProvider;
        model.AnalyticsPart = part;
        model.ContentItem = part.ContentItem;
    }
}