using NhanViet.Countries.Models;
using OrchardCore.ContentManagement;

namespace NhanViet.Countries.ViewModels;

public class CountryPartViewModel
{
    public string CountryName { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;
    public string CountryCodeAlpha3 { get; set; } = string.Empty;
    public string Capital { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string SubRegion { get; set; } = string.Empty;
    public long Population { get; set; } = 0;
    public decimal Area { get; set; } = 0;
    public string Currency { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = string.Empty;
    public string Languages { get; set; } = string.Empty;
    public string TimeZone { get; set; } = string.Empty;
    public string FlagUrl { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public bool IsPopular { get; set; } = false;
    public string VisaRequirements { get; set; } = string.Empty;
    public string WorkPermitInfo { get; set; } = string.Empty;
    public string CostOfLiving { get; set; } = string.Empty;
    public string JobMarketInfo { get; set; } = string.Empty;
    
    public CountryPart CountryPart { get; set; } = new();
    public ContentItem ContentItem { get; set; } = new();
}