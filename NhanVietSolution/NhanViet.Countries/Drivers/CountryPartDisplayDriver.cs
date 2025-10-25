using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using NhanViet.Countries.Models;
using NhanViet.Countries.ViewModels;
using OrchardCore.ContentManagement.Display.ContentDisplay;
using OrchardCore.ContentManagement.Display.Models;
using OrchardCore.DisplayManagement.Views;

namespace NhanViet.Countries.Drivers;

public sealed class CountryPartDisplayDriver : ContentPartDisplayDriver<CountryPart>
{
    private readonly IAuthorizationService _authorizationService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CountryPartDisplayDriver(
        IAuthorizationService authorizationService,
        IHttpContextAccessor httpContextAccessor)
    {
        _authorizationService = authorizationService;
        _httpContextAccessor = httpContextAccessor;
    }

    public override async Task<IDisplayResult> DisplayAsync(CountryPart countryPart, BuildPartDisplayContext context)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        
        // Kiểm tra permission ViewCountries trước khi hiển thị
        if (user != null && !await _authorizationService.AuthorizeAsync(user, Permissions.ViewCountries, countryPart.ContentItem))
        {
            // Trả về empty result nếu không có permission
            return Task.FromResult<IDisplayResult?>(null).Result;
        }

        return Initialize<CountryPartViewModel>(GetDisplayShapeType(context), m => BuildViewModel(m, countryPart))
            .Location("Detail", "Content:10")
            .Location("Summary", "Content:10");
    }

    public override async Task<IDisplayResult> EditAsync(CountryPart countryPart, BuildPartEditorContext context)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        
        // Kiểm tra permission EditCountries trước khi hiển thị editor
        if (user != null && !await _authorizationService.AuthorizeAsync(user, Permissions.EditCountries, countryPart.ContentItem))
        {
            // Trả về empty result nếu không có permission
            return Task.FromResult<IDisplayResult?>(null).Result;
        }

        return Initialize<CountryPartViewModel>(GetEditorShapeType(context), m => BuildViewModel(m, countryPart));
    }

    public override async Task<IDisplayResult> UpdateAsync(CountryPart model, UpdatePartEditorContext context)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        
        // Kiểm tra permission EditCountries trước khi update
        if (user != null && !await _authorizationService.AuthorizeAsync(user, Permissions.EditCountries, model.ContentItem))
        {
            // Không update nếu không có permission
            return await EditAsync(model, context);
        }

        await context.Updater.TryUpdateModelAsync(model, Prefix, 
            t => t.CountryName, 
            t => t.CountryCode, 
            t => t.CountryCodeAlpha3,
            t => t.Capital,
            t => t.Region,
            t => t.SubRegion,
            t => t.Population,
            t => t.Area,
            t => t.Currency,
            t => t.CurrencyCode,
            t => t.Languages,
            t => t.TimeZone,
            t => t.FlagUrl,
            t => t.Description,
            t => t.IsActive,
            t => t.IsPopular,
            t => t.VisaRequirements,
            t => t.WorkPermitInfo,
            t => t.CostOfLiving,
            t => t.JobMarketInfo);

        return await EditAsync(model, context);
    }

    private static void BuildViewModel(CountryPartViewModel model, CountryPart part)
    {
        model.CountryName = part.CountryName;
        model.CountryCode = part.CountryCode;
        model.CountryCodeAlpha3 = part.CountryCodeAlpha3;
        model.Capital = part.Capital;
        model.Region = part.Region;
        model.SubRegion = part.SubRegion;
        model.Population = part.Population;
        model.Area = part.Area;
        model.Currency = part.Currency;
        model.CurrencyCode = part.CurrencyCode;
        model.Languages = part.Languages;
        model.TimeZone = part.TimeZone;
        model.FlagUrl = part.FlagUrl;
        model.Description = part.Description;
        model.IsActive = part.IsActive;
        model.IsPopular = part.IsPopular;
        model.VisaRequirements = part.VisaRequirements;
        model.WorkPermitInfo = part.WorkPermitInfo;
        model.CostOfLiving = part.CostOfLiving;
        model.JobMarketInfo = part.JobMarketInfo;
        model.CountryPart = part;
        model.ContentItem = part.ContentItem;
    }
}