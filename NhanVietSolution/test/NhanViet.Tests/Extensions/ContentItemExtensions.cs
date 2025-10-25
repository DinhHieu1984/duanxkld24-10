using OrchardCore.ContentManagement;
using NhanViet.Companies.Models;
using NhanViet.JobOrders.Models;
using NhanViet.News.Models;

namespace NhanViet.Tests.Extensions;

public static class ContentItemExtensions
{
    public static void Apply(this ContentItem contentItem, CompanyPart companyPart)
    {
        var existingPart = contentItem.As<CompanyPart>();
        if (existingPart == null)
        {
            contentItem.Weld(companyPart);
        }
        else
        {
            existingPart.Apply(companyPart);
        }
    }
    
    public static void Apply(this ContentItem contentItem, JobOrderPart jobOrderPart)
    {
        var existingPart = contentItem.As<JobOrderPart>();
        if (existingPart == null)
        {
            contentItem.Weld(jobOrderPart);
        }
        else
        {
            existingPart.Apply(jobOrderPart);
        }
    }
    
    public static void Apply(this ContentItem contentItem, NewsPart newsPart)
    {
        var existingPart = contentItem.As<NewsPart>();
        if (existingPart == null)
        {
            contentItem.Weld(newsPart);
        }
        else
        {
            existingPart.Apply(newsPart);
        }
    }
}