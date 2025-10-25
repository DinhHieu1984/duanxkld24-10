using OrchardCore.ContentManagement;

namespace NhanViet.Countries.Models;

public class CountryPart : ContentPart
{
    public string CountryName { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty; // ISO 3166-1 alpha-2
    public string CountryCodeAlpha3 { get; set; } = string.Empty; // ISO 3166-1 alpha-3
    public string Capital { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty; // Asia, Europe, etc.
    public string SubRegion { get; set; } = string.Empty; // Southeast Asia, etc.
    public long Population { get; set; } = 0;
    public decimal Area { get; set; } = 0; // in square kilometers
    public string Currency { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = string.Empty; // USD, VND, etc.
    public string Languages { get; set; } = string.Empty; // comma-separated
    public string TimeZone { get; set; } = string.Empty;
    public string FlagUrl { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public bool IsPopular { get; set; } = false; // for featured countries
    public string VisaRequirements { get; set; } = string.Empty;
    public string WorkPermitInfo { get; set; } = string.Empty;
    public string CostOfLiving { get; set; } = string.Empty;
    public string JobMarketInfo { get; set; } = string.Empty;
}