using OrchardCore.ContentManagement;
using OrchardCoreLearning.DemoBlogModule.Models;

namespace OrchardCoreLearning.DemoBlogModule.Services
{
    /// <summary>
    /// ✅ ĐÚNG: Interface definition tuân thủ naming conventions
    /// - Prefix: I
    /// - Suffix: Service
    /// - Clear method signatures
    /// - Async methods where appropriate
    /// </summary>
    public interface IDemoBlogService
    {
        // Initialization methods
        Task<bool> IsInitializedAsync();
        Task MarkAsInitializedAsync();
        Task InstallationSetupAsync();
        Task FullInitializationAsync();
        Task PartialInitializationAsync();
        Task MinimalInitializationAsync();
        Task UpdateConfigurationAsync();
        Task CleanupAsync();
        
        // Category management
        Task CreateDefaultCategoriesAsync();
        Task CreateCategoryAsync(string name, string description);
        Task<IEnumerable<ContentItem>> GetCategoriesAsync();
        
        // Blog post management
        Task CreateSamplePostAsync();
        Task<IEnumerable<ContentItem>> GetBlogPostsAsync(int page = 1, int pageSize = 10);
        Task<IEnumerable<ContentItem>> GetBlogPostsByCategoryAsync(string category, int page = 1, int pageSize = 10);
        Task<ContentItem> GetBlogPostAsync(string slug);
        Task<ContentItem> CreateBlogPostAsync(string title, string content, string category = null);
        
        // Settings management
        Task UpdateSettingsAsync(DemoBlogSettings settings);
        Task<DemoBlogSettings> GetSettingsAsync();
        
        // Data migration
        Task MigrateDataFromV1ToV2Async();
        
        // Statistics
        Task<int> GetTotalPostsCountAsync();
        Task<int> GetTotalCategoriesCountAsync();
        Task IncrementViewCountAsync(string contentItemId);
    }
}