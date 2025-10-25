using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using NhanViet.News.Models;
using NhanViet.News.ViewModels;
using OrchardCore.ContentManagement.Display.ContentDisplay;
using OrchardCore.ContentManagement.Display.Models;
using OrchardCore.DisplayManagement.Views;

namespace NhanViet.News.Drivers;

public sealed class NewsPartDisplayDriver : ContentPartDisplayDriver<NewsPart>
{
    private readonly IAuthorizationService _authorizationService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public NewsPartDisplayDriver(
        IAuthorizationService authorizationService,
        IHttpContextAccessor httpContextAccessor)
    {
        _authorizationService = authorizationService;
        _httpContextAccessor = httpContextAccessor;
    }

    public override async Task<IDisplayResult> DisplayAsync(NewsPart newsPart, BuildPartDisplayContext context)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        
        // Kiểm tra permission ViewNews trước khi hiển thị
        if (user != null && !await _authorizationService.AuthorizeAsync(user, Permissions.ViewNews, newsPart.ContentItem))
        {
            // Trả về empty result nếu không có permission
            return Task.FromResult<IDisplayResult?>(null).Result!;
        }

        return Initialize<NewsPartViewModel>(GetDisplayShapeType(context), m => BuildViewModel(m, newsPart))
            .Location("Detail", "Content:10")
            .Location("Summary", "Content:10");
    }

    public override async Task<IDisplayResult> EditAsync(NewsPart newsPart, BuildPartEditorContext context)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        
        // Kiểm tra permission EditNews trước khi hiển thị editor
        if (user != null && !await _authorizationService.AuthorizeAsync(user, Permissions.EditNews, newsPart.ContentItem))
        {
            // Trả về empty result nếu không có permission
            return Task.FromResult<IDisplayResult?>(null).Result!;
        }

        return Initialize<NewsPartViewModel>(GetEditorShapeType(context), m => BuildViewModel(m, newsPart));
    }

    public override async Task<IDisplayResult> UpdateAsync(NewsPart model, UpdatePartEditorContext context)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        
        // Kiểm tra permission EditNews trước khi update
        if (user != null && !await _authorizationService.AuthorizeAsync(user, Permissions.EditNews, model.ContentItem))
        {
            // Không update nếu không có permission
            return await EditAsync(model, context);
        }

        await context.Updater.TryUpdateModelAsync(model, Prefix, 
            t => t.Summary, 
            t => t.Category, 
            t => t.Tags,
            t => t.PublishedDate,
            t => t.IsFeatured,
            t => t.Author,
            t => t.ImageUrl);

        return await EditAsync(model, context);
    }

    private static void BuildViewModel(NewsPartViewModel model, NewsPart part)
    {
        model.Summary = part.Summary;
        model.Category = part.Category;
        model.Tags = part.Tags;
        model.PublishedDate = part.PublishedDate;
        model.IsFeatured = part.IsFeatured;
        model.Author = part.Author;
        model.ImageUrl = part.ImageUrl;
        model.NewsPart = part;
        model.ContentItem = part.ContentItem;
    }
}