# 🔍 **Search & Indexing Patterns trong OrchardCore**

## 🎯 **TỔNG QUAN**

**Search & Indexing** trong OrchardCore cung cấp hệ thống tìm kiếm mạnh mẽ với khả năng:
- **Multiple Search Engines**: Lucene.NET, Elasticsearch support
- **Full-Text Search**: Advanced text analysis, stemming, synonyms
- **Real-time Indexing**: Background tasks, incremental updates
- **Custom Search Providers**: Extensible architecture cho third-party engines
- **Advanced Queries**: Boolean, fuzzy, range, geospatial search

---

## 🏗️ **KIẾN TRÚC CORE COMPONENTS**

### **1. 📊 IIndexManager - Index Management Abstraction**

```csharp
// Core interface cho index management
public interface IIndexManager
{
    Task<bool> ExistsAsync(string indexName);
    Task CreateIndexAsync(string indexName);
    Task DeleteIndexAsync(string indexName);
    Task<IEnumerable<string>> GetIndexNamesAsync();
}

// Lucene implementation
public sealed class LuceneIndexManager : IIndexManager, IDocumentIndexManager
{
    private readonly ILuceneIndexStore _indexStore;
    private readonly ILuceneIndexingState _indexingState;
    private readonly ILogger _logger;
    private readonly IEnumerable<IIndexEvents> _indexEvents;

    public async Task CreateIndexAsync(string indexName)
    {
        if (await ExistsAsync(indexName))
        {
            return;
        }

        var directory = _indexStore.GetDirectory(indexName);
        var analyzer = _analyzerManager.CreateAnalyzer(indexName);
        
        var config = new IndexWriterConfig(LuceneVersion.LUCENE_48, analyzer)
        {
            OpenMode = OpenMode.CREATE
        };

        using var writer = new IndexWriter(directory, config);
        writer.Commit();

        // Notify index events
        foreach (var indexEvent in _indexEvents)
        {
            await indexEvent.IndexCreatedAsync(indexName);
        }

        _logger.LogInformation("Created Lucene index: {IndexName}", indexName);
    }

    public async Task<bool> ExistsAsync(string indexName)
    {
        var directory = _indexStore.GetDirectory(indexName);
        return DirectoryReader.IndexExists(directory);
    }
}
```

### **2. 🔍 ISearchService - Search Abstraction**

```csharp
// Core search service interface
public interface ISearchService
{
    Task<SearchResult> SearchAsync(SearchRequest request);
    Task<IEnumerable<string>> GetSuggestionsAsync(string query, string indexName);
}

// Lucene search service implementation
public class LuceneSearchService : ISearchService
{
    private readonly ILuceneIndexStore _indexStore;
    private readonly ILuceneQueryService _queryService;
    private readonly ILogger<LuceneSearchService> _logger;

    public async Task<SearchResult> SearchAsync(SearchRequest request)
    {
        var searchResult = new SearchResult();
        
        await _indexStore.SearchAsync(request.IndexName, async searcher =>
        {
            // Build Lucene query
            var query = await _queryService.BuildQueryAsync(request);
            
            // Apply filters
            var filter = BuildFilters(request.Filters);
            
            // Execute search
            var topDocs = searcher.Search(query, filter, request.Size, request.Sort);
            
            searchResult.TotalHits = topDocs.TotalHits;
            searchResult.Documents = new List<SearchDocument>();

            foreach (var scoreDoc in topDocs.ScoreDocs)
            {
                var doc = searcher.Doc(scoreDoc.Doc);
                var searchDoc = ConvertToSearchDocument(doc, scoreDoc.Score);
                searchResult.Documents.Add(searchDoc);
            }

            // Add facets if requested
            if (request.Facets?.Any() == true)
            {
                searchResult.Facets = await BuildFacetsAsync(searcher, query, request.Facets);
            }

            // Add highlighting if requested
            if (request.Highlight != null)
            {
                await AddHighlightingAsync(searchResult, searcher, query, request.Highlight);
            }
        });

        return searchResult;
    }
}
```

### **3. 📝 Content Indexing System**

```csharp
// Background task cho content indexing
[BackgroundTask(
    Title = "Content Indexing Task",
    Schedule = "* * * * *", // Every minute
    Description = "Indexes content items for search",
    LockTimeout = 1000,
    LockExpiration = 300000)]
public sealed class ContentIndexingBackgroundTask : IBackgroundTask
{
    public Task DoWorkAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var indexingService = serviceProvider.GetRequiredService<ContentIndexingService>();
        return indexingService.ProcessRecordsForAllIndexesAsync();
    }
}

// Content indexing service
public class ContentIndexingService
{
    private readonly IContentManager _contentManager;
    private readonly IIndexManager _indexManager;
    private readonly IEnumerable<IContentItemIndexHandler> _indexHandlers;

    public async Task IndexContentItemAsync(ContentItem contentItem, string indexName)
    {
        var indexDocument = new DocumentIndex();
        
        // Add basic content item fields
        indexDocument.Set("ContentItemId", contentItem.ContentItemId, DocumentIndexOptions.Store);
        indexDocument.Set("ContentType", contentItem.ContentType, DocumentIndexOptions.Store);
        indexDocument.Set("Published", contentItem.Published.ToString().ToLowerInvariant(), DocumentIndexOptions.Store);
        indexDocument.Set("Latest", contentItem.Latest.ToString().ToLowerInvariant(), DocumentIndexOptions.Store);
        indexDocument.Set("CreatedUtc", contentItem.CreatedUtc, DocumentIndexOptions.Store);
        indexDocument.Set("ModifiedUtc", contentItem.ModifiedUtc, DocumentIndexOptions.Store);
        indexDocument.Set("PublishedUtc", contentItem.PublishedUtc, DocumentIndexOptions.Store);

        // Process through content handlers
        var context = new BuildIndexContext(indexDocument, contentItem, new string[] { indexName });
        
        foreach (var handler in _indexHandlers)
        {
            await handler.BuildIndexAsync(context);
        }

        // Add to index
        await _indexManager.AddDocumentAsync(indexName, indexDocument);
    }

    public async Task ProcessRecordsForAllIndexesAsync()
    {
        var indexProfiles = await GetActiveIndexProfilesAsync();
        
        foreach (var profile in indexProfiles)
        {
            await ProcessRecordsForIndexAsync(profile);
        }
    }
}
```

### **4. 🔧 Query Providers System**

```csharp
// Base query provider
public abstract class LuceneQueryProvider : ILuceneQueryProvider
{
    public abstract string Name { get; }
    public abstract Query CreateQuery(ILuceneQueryService builder, LuceneQueryContext context, string type, JObject query);
}

// Match query provider
public class MatchQueryProvider : LuceneQueryProvider
{
    public override string Name => "match";

    public override Query CreateQuery(ILuceneQueryService builder, LuceneQueryContext context, string type, JObject query)
    {
        var field = query["field"]?.Value<string>();
        var value = query["value"]?.Value<string>();
        var boost = query["boost"]?.Value<float>() ?? 1.0f;

        if (string.IsNullOrEmpty(field) || string.IsNullOrEmpty(value))
        {
            return new MatchNoDocsQuery();
        }

        // Analyze the query text
        var analyzer = context.DefaultAnalyzer;
        var terms = AnalyzeText(analyzer, field, value);

        if (terms.Count == 0)
        {
            return new MatchNoDocsQuery();
        }

        if (terms.Count == 1)
        {
            var termQuery = new TermQuery(new Term(field, terms[0]));
            termQuery.Boost = boost;
            return termQuery;
        }

        // Multiple terms - create phrase query
        var phraseQuery = new PhraseQuery();
        foreach (var term in terms)
        {
            phraseQuery.Add(new Term(field, term));
        }
        phraseQuery.Boost = boost;

        return phraseQuery;
    }
}

// Boolean query provider
public class BooleanQueryProvider : LuceneQueryProvider
{
    public override string Name => "bool";

    public override Query CreateQuery(ILuceneQueryService builder, LuceneQueryContext context, string type, JObject query)
    {
        var boolQuery = new BooleanQuery();

        // Must clauses
        if (query["must"] is JArray mustClauses)
        {
            foreach (var clause in mustClauses)
            {
                var subQuery = builder.CreateQuery(context, clause as JObject);
                boolQuery.Add(subQuery, Occur.MUST);
            }
        }

        // Should clauses
        if (query["should"] is JArray shouldClauses)
        {
            foreach (var clause in shouldClauses)
            {
                var subQuery = builder.CreateQuery(context, clause as JObject);
                boolQuery.Add(subQuery, Occur.SHOULD);
            }
        }

        // Must not clauses
        if (query["must_not"] is JArray mustNotClauses)
        {
            foreach (var clause in mustNotClauses)
            {
                var subQuery = builder.CreateQuery(context, clause as JObject);
                boolQuery.Add(subQuery, Occur.MUST_NOT);
            }
        }

        return boolQuery;
    }
}
```

---

## 🛠️ **CUSTOM SEARCH PROVIDER PATTERNS**

### **1. 🔌 Custom Search Engine Integration**

```csharp
// Custom search provider interface
public interface ICustomSearchProvider
{
    string Name { get; }
    Task<SearchResult> SearchAsync(SearchRequest request);
    Task IndexDocumentAsync(string indexName, SearchDocument document);
    Task DeleteDocumentAsync(string indexName, string documentId);
    Task<bool> IndexExistsAsync(string indexName);
    Task CreateIndexAsync(string indexName, IndexSettings settings);
}

// Elasticsearch provider implementation
public class ElasticsearchProvider : ICustomSearchProvider
{
    private readonly IElasticClient _elasticClient;
    private readonly ILogger<ElasticsearchProvider> _logger;

    public string Name => "Elasticsearch";

    public async Task<SearchResult> SearchAsync(SearchRequest request)
    {
        var searchDescriptor = new SearchDescriptor<dynamic>()
            .Index(request.IndexName)
            .Size(request.Size)
            .From(request.From);

        // Build Elasticsearch query
        var query = BuildElasticsearchQuery(request.Query);
        searchDescriptor = searchDescriptor.Query(q => query);

        // Add filters
        if (request.Filters?.Any() == true)
        {
            var filters = BuildElasticsearchFilters(request.Filters);
            searchDescriptor = searchDescriptor.PostFilter(f => filters);
        }

        // Add sorting
        if (request.Sort?.Any() == true)
        {
            searchDescriptor = searchDescriptor.Sort(s => BuildElasticsearchSort(request.Sort, s));
        }

        // Add aggregations for facets
        if (request.Facets?.Any() == true)
        {
            searchDescriptor = searchDescriptor.Aggregations(a => BuildElasticsearchAggregations(request.Facets, a));
        }

        // Add highlighting
        if (request.Highlight != null)
        {
            searchDescriptor = searchDescriptor.Highlight(h => BuildElasticsearchHighlight(request.Highlight, h));
        }

        var response = await _elasticClient.SearchAsync<dynamic>(searchDescriptor);

        if (!response.IsValid)
        {
            _logger.LogError("Elasticsearch search failed: {Error}", response.OriginalException?.Message);
            throw new SearchException($"Search failed: {response.OriginalException?.Message}");
        }

        return ConvertElasticsearchResponse(response);
    }

    public async Task IndexDocumentAsync(string indexName, SearchDocument document)
    {
        var indexResponse = await _elasticClient.IndexAsync(document, i => i
            .Index(indexName)
            .Id(document.Id)
            .Refresh(Refresh.WaitFor));

        if (!indexResponse.IsValid)
        {
            _logger.LogError("Failed to index document {DocumentId}: {Error}", 
                document.Id, indexResponse.OriginalException?.Message);
            throw new IndexingException($"Failed to index document: {indexResponse.OriginalException?.Message}");
        }
    }

    private QueryContainer BuildElasticsearchQuery(SearchQuery query)
    {
        return query.Type switch
        {
            "match" => Query<dynamic>.Match(m => m
                .Field(query.Field)
                .Query(query.Value)
                .Boost(query.Boost)),
            
            "match_phrase" => Query<dynamic>.MatchPhrase(m => m
                .Field(query.Field)
                .Query(query.Value)
                .Boost(query.Boost)),
            
            "term" => Query<dynamic>.Term(t => t
                .Field(query.Field)
                .Value(query.Value)
                .Boost(query.Boost)),
            
            "range" => Query<dynamic>.Range(r => r
                .Field(query.Field)
                .GreaterThanOrEquals(query.From)
                .LessThanOrEquals(query.To)
                .Boost(query.Boost)),
            
            "bool" => BuildBooleanQuery(query.BoolQuery),
            
            _ => Query<dynamic>.MatchAll()
        };
    }
}
```

### **2. 🔍 Advanced Search Features**

```csharp
// Advanced search service với multiple features
public class AdvancedSearchService : IAdvancedSearchService
{
    private readonly ISearchService _searchService;
    private readonly ISearchAnalyticsService _analyticsService;
    private readonly ISearchSuggestionService _suggestionService;
    private readonly IMemoryCache _cache;

    public async Task<AdvancedSearchResult> SearchAsync(AdvancedSearchRequest request)
    {
        // Track search analytics
        await _analyticsService.TrackSearchAsync(new SearchAnalyticsEvent
        {
            Query = request.Query,
            UserId = request.UserId,
            Timestamp = DateTime.UtcNow,
            Filters = request.Filters,
            ResultCount = 0 // Will be updated later
        });

        // Check cache first
        var cacheKey = GenerateSearchCacheKey(request);
        if (_cache.TryGetValue(cacheKey, out AdvancedSearchResult cachedResult))
        {
            await _analyticsService.TrackCacheHitAsync(cacheKey);
            return cachedResult;
        }

        var result = new AdvancedSearchResult();

        // Execute main search
        var searchResult = await _searchService.SearchAsync(new SearchRequest
        {
            Query = request.Query,
            IndexName = request.IndexName,
            Size = request.Size,
            From = request.From,
            Filters = request.Filters,
            Sort = request.Sort,
            Facets = request.Facets,
            Highlight = request.Highlight
        });

        result.Documents = searchResult.Documents;
        result.TotalHits = searchResult.TotalHits;
        result.Facets = searchResult.Facets;

        // Add search suggestions if no results or few results
        if (searchResult.TotalHits < 5)
        {
            result.Suggestions = await _suggestionService.GetSuggestionsAsync(
                request.Query, request.IndexName, 5);
        }

        // Add related searches
        result.RelatedSearches = await GetRelatedSearchesAsync(request.Query, request.UserId);

        // Add auto-complete suggestions
        if (request.IncludeAutoComplete)
        {
            result.AutoCompleteSuggestions = await GetAutoCompleteSuggestionsAsync(
                request.Query, request.IndexName, 10);
        }

        // Add spell check
        if (request.IncludeSpellCheck && searchResult.TotalHits == 0)
        {
            result.SpellCheckSuggestions = await GetSpellCheckSuggestionsAsync(
                request.Query, request.IndexName);
        }

        // Cache result
        var cacheOptions = new MemoryCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromMinutes(15),
            Size = EstimateResultSize(result)
        };
        _cache.Set(cacheKey, result, cacheOptions);

        // Update analytics with result count
        await _analyticsService.UpdateSearchResultCountAsync(request.Query, searchResult.TotalHits);

        return result;
    }

    private async Task<IEnumerable<string>> GetAutoCompleteSuggestionsAsync(
        string query, 
        string indexName, 
        int maxSuggestions)
    {
        // Use prefix search for auto-complete
        var prefixRequest = new SearchRequest
        {
            Query = new SearchQuery
            {
                Type = "prefix",
                Field = "title.autocomplete",
                Value = query
            },
            IndexName = indexName,
            Size = maxSuggestions,
            Sort = new[] { new SearchSort { Field = "_score", Direction = "desc" } }
        };

        var result = await _searchService.SearchAsync(prefixRequest);
        
        return result.Documents
            .Select(d => d.GetValue("title")?.ToString())
            .Where(title => !string.IsNullOrEmpty(title))
            .Distinct()
            .Take(maxSuggestions);
    }
}
```

### **3. 📊 Search Analytics & Monitoring**

```csharp
// Search analytics service
public class SearchAnalyticsService : ISearchAnalyticsService
{
    private readonly ISearchAnalyticsStore _analyticsStore;
    private readonly IMetricsCollector _metricsCollector;
    private readonly ILogger<SearchAnalyticsService> _logger;

    public async Task TrackSearchAsync(SearchAnalyticsEvent searchEvent)
    {
        // Store search event
        await _analyticsStore.StoreSearchEventAsync(searchEvent);

        // Collect metrics
        _metricsCollector.Increment("search.queries.total", new Dictionary<string, string>
        {
            ["index"] = searchEvent.IndexName,
            ["has_results"] = (searchEvent.ResultCount > 0).ToString().ToLower()
        });

        _metricsCollector.Histogram("search.result_count", searchEvent.ResultCount);

        // Track popular queries
        await UpdatePopularQueriesAsync(searchEvent.Query, searchEvent.IndexName);

        // Track zero-result queries for improvement
        if (searchEvent.ResultCount == 0)
        {
            await TrackZeroResultQueryAsync(searchEvent);
        }
    }

    public async Task<SearchAnalyticsReport> GenerateReportAsync(
        DateTime startDate, 
        DateTime endDate, 
        string indexName = null)
    {
        var report = new SearchAnalyticsReport
        {
            StartDate = startDate,
            EndDate = endDate,
            IndexName = indexName
        };

        // Total searches
        report.TotalSearches = await _analyticsStore.GetSearchCountAsync(startDate, endDate, indexName);

        // Unique users
        report.UniqueUsers = await _analyticsStore.GetUniqueUserCountAsync(startDate, endDate, indexName);

        // Popular queries
        report.PopularQueries = await _analyticsStore.GetPopularQueriesAsync(
            startDate, endDate, indexName, 20);

        // Zero result queries
        report.ZeroResultQueries = await _analyticsStore.GetZeroResultQueriesAsync(
            startDate, endDate, indexName, 20);

        // Search trends
        report.SearchTrends = await _analyticsStore.GetSearchTrendsAsync(
            startDate, endDate, indexName);

        // Performance metrics
        report.AverageResponseTime = await _analyticsStore.GetAverageResponseTimeAsync(
            startDate, endDate, indexName);

        // Click-through rates
        report.ClickThroughRates = await _analyticsStore.GetClickThroughRatesAsync(
            startDate, endDate, indexName);

        return report;
    }

    private async Task TrackZeroResultQueryAsync(SearchAnalyticsEvent searchEvent)
    {
        // Log for manual review
        _logger.LogInformation("Zero result query: {Query} in index {Index}", 
            searchEvent.Query, searchEvent.IndexName);

        // Store for analysis
        await _analyticsStore.StoreZeroResultQueryAsync(new ZeroResultQuery
        {
            Query = searchEvent.Query,
            IndexName = searchEvent.IndexName,
            UserId = searchEvent.UserId,
            Timestamp = searchEvent.Timestamp,
            Filters = searchEvent.Filters
        });

        // Trigger improvement suggestions
        await TriggerQueryImprovementAsync(searchEvent.Query, searchEvent.IndexName);
    }
}
```

---

## 🔧 **INDEXING STRATEGIES**

### **1. 📝 Content-Specific Indexing**

```csharp
// Custom content index handler
public class ProductContentIndexHandler : IContentItemIndexHandler
{
    public async Task BuildIndexAsync(BuildIndexContext context)
    {
        var contentItem = context.ContentItem;
        
        if (contentItem.ContentType != "Product")
        {
            return;
        }

        var product = contentItem.As<ProductPart>();
        if (product == null)
        {
            return;
        }

        var document = context.DocumentIndex;

        // Index product-specific fields
        document.Set("product_name", product.Name, DocumentIndexOptions.Analyze | DocumentIndexOptions.Store);
        document.Set("product_sku", product.SKU, DocumentIndexOptions.Store);
        document.Set("product_price", product.Price, DocumentIndexOptions.Store);
        document.Set("product_category", product.Category, DocumentIndexOptions.Store);
        document.Set("product_brand", product.Brand, DocumentIndexOptions.Analyze | DocumentIndexOptions.Store);
        document.Set("product_description", product.Description, DocumentIndexOptions.Analyze);
        document.Set("product_tags", string.Join(" ", product.Tags), DocumentIndexOptions.Analyze);
        
        // Index availability
        document.Set("product_in_stock", product.InStock.ToString().ToLower(), DocumentIndexOptions.Store);
        document.Set("product_stock_quantity", product.StockQuantity, DocumentIndexOptions.Store);

        // Index pricing for range queries
        document.Set("product_price_range", GetPriceRange(product.Price), DocumentIndexOptions.Store);

        // Index product ratings
        if (product.AverageRating.HasValue)
        {
            document.Set("product_rating", product.AverageRating.Value, DocumentIndexOptions.Store);
            document.Set("product_rating_range", GetRatingRange(product.AverageRating.Value), DocumentIndexOptions.Store);
        }

        // Index product attributes
        foreach (var attribute in product.Attributes)
        {
            var fieldName = $"product_attr_{attribute.Name.ToLower()}";
            document.Set(fieldName, attribute.Value, DocumentIndexOptions.Analyze | DocumentIndexOptions.Store);
        }

        // Index related categories for faceting
        var categoryHierarchy = await GetCategoryHierarchyAsync(product.CategoryId);
        foreach (var category in categoryHierarchy)
        {
            document.Set("product_categories", category.Name, DocumentIndexOptions.Store);
        }

        // Index for geospatial search if product has location
        if (product.Location != null)
        {
            document.Set("product_location", $"{product.Location.Latitude},{product.Location.Longitude}", 
                DocumentIndexOptions.Store);
        }

        // Boost popular products
        var popularityBoost = await GetProductPopularityBoostAsync(product.Id);
        document.Set("_boost", popularityBoost, DocumentIndexOptions.Store);
    }

    private string GetPriceRange(decimal price)
    {
        return price switch
        {
            < 100 => "under_100",
            < 500 => "100_to_500",
            < 1000 => "500_to_1000",
            < 5000 => "1000_to_5000",
            _ => "over_5000"
        };
    }
}
```

### **2. 🔄 Real-time Indexing**

```csharp
// Real-time indexing service
public class RealTimeIndexingService : IRealTimeIndexingService
{
    private readonly IIndexManager _indexManager;
    private readonly IBackgroundTaskQueue _taskQueue;
    private readonly IMemoryCache _indexingQueue;
    private readonly ILogger<RealTimeIndexingService> _logger;

    public async Task QueueIndexingAsync(string contentItemId, IndexingAction action)
    {
        var indexingTask = new IndexingTask
        {
            ContentItemId = contentItemId,
            Action = action,
            QueuedAt = DateTime.UtcNow,
            Priority = GetIndexingPriority(action)
        };

        // Add to in-memory queue for immediate processing
        var queueKey = $"indexing_queue_{DateTime.UtcNow:yyyyMMddHH}";
        var queue = _indexingQueue.GetOrCreate(queueKey, factory =>
        {
            factory.SlidingExpiration = TimeSpan.FromHours(1);
            return new ConcurrentQueue<IndexingTask>();
        }) as ConcurrentQueue<IndexingTask>;

        queue.Enqueue(indexingTask);

        // Queue background processing
        await _taskQueue.QueueBackgroundWorkItemAsync(async token =>
        {
            await ProcessIndexingTaskAsync(indexingTask, token);
        });
    }

    private async Task ProcessIndexingTaskAsync(IndexingTask task, CancellationToken cancellationToken)
    {
        try
        {
            switch (task.Action)
            {
                case IndexingAction.Create:
                case IndexingAction.Update:
                    await IndexContentItemAsync(task.ContentItemId);
                    break;
                
                case IndexingAction.Delete:
                    await DeleteFromIndexAsync(task.ContentItemId);
                    break;
                
                case IndexingAction.Rebuild:
                    await RebuildIndexAsync(task.IndexName);
                    break;
            }

            _logger.LogDebug("Processed indexing task for {ContentItemId}: {Action}", 
                task.ContentItemId, task.Action);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process indexing task for {ContentItemId}: {Action}", 
                task.ContentItemId, task.Action);
            
            // Retry logic
            if (task.RetryCount < 3)
            {
                task.RetryCount++;
                task.NextRetryAt = DateTime.UtcNow.AddMinutes(Math.Pow(2, task.RetryCount));
                await QueueRetryAsync(task);
            }
        }
    }

    // Batch processing for efficiency
    public async Task ProcessBatchIndexingAsync()
    {
        var currentHour = DateTime.UtcNow.ToString("yyyyMMddHH");
        var queueKey = $"indexing_queue_{currentHour}";
        
        if (!_indexingQueue.TryGetValue(queueKey, out var queueObj) || 
            queueObj is not ConcurrentQueue<IndexingTask> queue)
        {
            return;
        }

        var batch = new List<IndexingTask>();
        
        // Dequeue up to 100 items for batch processing
        while (batch.Count < 100 && queue.TryDequeue(out var task))
        {
            batch.Add(task);
        }

        if (batch.Count == 0)
        {
            return;
        }

        // Group by action and index
        var groupedTasks = batch.GroupBy(t => new { t.Action, t.IndexName });

        foreach (var group in groupedTasks)
        {
            await ProcessBatchAsync(group.Key.IndexName, group.Key.Action, group.ToList());
        }
    }
}
```

### **3. 🎯 Smart Indexing Optimization**

```csharp
// Intelligent indexing optimizer
public class IndexingOptimizer : IIndexingOptimizer
{
    private readonly IIndexManager _indexManager;
    private readonly ISearchAnalyticsService _analyticsService;
    private readonly ILogger<IndexingOptimizer> _logger;

    public async Task OptimizeIndexingAsync(string indexName)
    {
        // Analyze search patterns
        var searchPatterns = await _analyticsService.GetSearchPatternsAsync(indexName);
        
        // Optimize field indexing based on usage
        var fieldUsage = await AnalyzeFieldUsageAsync(indexName, searchPatterns);
        await OptimizeFieldIndexingAsync(indexName, fieldUsage);

        // Optimize index structure
        await OptimizeIndexStructureAsync(indexName, searchPatterns);

        // Update index settings
        await UpdateIndexSettingsAsync(indexName, searchPatterns);

        _logger.LogInformation("Completed index optimization for {IndexName}", indexName);
    }

    private async Task<FieldUsageAnalysis> AnalyzeFieldUsageAsync(
        string indexName, 
        SearchPatternAnalysis patterns)
    {
        var analysis = new FieldUsageAnalysis();

        // Analyze which fields are searched most frequently
        foreach (var pattern in patterns.QueryPatterns)
        {
            foreach (var field in pattern.Fields)
            {
                if (!analysis.FieldFrequency.ContainsKey(field))
                {
                    analysis.FieldFrequency[field] = 0;
                }
                analysis.FieldFrequency[field] += pattern.Frequency;
            }
        }

        // Analyze which fields are used in filters
        foreach (var filter in patterns.FilterPatterns)
        {
            if (!analysis.FilterFrequency.ContainsKey(filter.Field))
            {
                analysis.FilterFrequency[filter.Field] = 0;
            }
            analysis.FilterFrequency[filter.Field] += filter.Frequency;
        }

        // Analyze which fields are used in sorting
        foreach (var sort in patterns.SortPatterns)
        {
            if (!analysis.SortFrequency.ContainsKey(sort.Field))
            {
                analysis.SortFrequency[sort.Field] = 0;
            }
            analysis.SortFrequency[sort.Field] += sort.Frequency;
        }

        return analysis;
    }

    private async Task OptimizeFieldIndexingAsync(string indexName, FieldUsageAnalysis usage)
    {
        var indexSettings = await GetIndexSettingsAsync(indexName);
        var optimizations = new List<IndexOptimization>();

        // Optimize frequently searched fields
        foreach (var field in usage.FieldFrequency.OrderByDescending(f => f.Value).Take(10))
        {
            if (field.Value > 100) // Threshold for high-frequency fields
            {
                optimizations.Add(new IndexOptimization
                {
                    Field = field.Key,
                    OptimizationType = "boost_analysis",
                    Reason = $"High search frequency: {field.Value} queries"
                });
            }
        }

        // Optimize frequently filtered fields
        foreach (var field in usage.FilterFrequency.OrderByDescending(f => f.Value).Take(5))
        {
            optimizations.Add(new IndexOptimization
            {
                Field = field.Key,
                OptimizationType = "add_keyword_field",
                Reason = $"High filter frequency: {field.Value} filters"
            });
        }

        // Optimize frequently sorted fields
        foreach (var field in usage.SortFrequency.OrderByDescending(f => f.Value).Take(5))
        {
            optimizations.Add(new IndexOptimization
            {
                Field = field.Key,
                OptimizationType = "disable_analysis_for_sort",
                Reason = $"High sort frequency: {field.Value} sorts"
            });
        }

        // Apply optimizations
        await ApplyIndexOptimizationsAsync(indexName, optimizations);
    }
}
```

---

## 🔍 **ADVANCED SEARCH FEATURES**

### **1. 🎯 Faceted Search**

```csharp
// Faceted search implementation
public class FacetedSearchService : IFacetedSearchService
{
    private readonly ISearchService _searchService;
    private readonly IFacetConfigurationService _facetConfigService;

    public async Task<FacetedSearchResult> SearchWithFacetsAsync(FacetedSearchRequest request)
    {
        // Get facet configuration
        var facetConfig = await _facetConfigService.GetFacetConfigurationAsync(request.IndexName);
        
        var searchRequest = new SearchRequest
        {
            Query = request.Query,
            IndexName = request.IndexName,
            Size = request.Size,
            From = request.From,
            Filters = request.Filters,
            Sort = request.Sort,
            Facets = BuildFacetRequests(facetConfig, request.SelectedFacets)
        };

        var searchResult = await _searchService.SearchAsync(searchRequest);
        
        return new FacetedSearchResult
        {
            Documents = searchResult.Documents,
            TotalHits = searchResult.TotalHits,
            Facets = ProcessFacetResults(searchResult.Facets, facetConfig, request.SelectedFacets),
            SearchTime = searchResult.SearchTime
        };
    }

    private IEnumerable<FacetRequest> BuildFacetRequests(
        FacetConfiguration config, 
        Dictionary<string, string[]> selectedFacets)
    {
        var facetRequests = new List<FacetRequest>();

        foreach (var facetDef in config.Facets)
        {
            var facetRequest = new FacetRequest
            {
                Name = facetDef.Name,
                Field = facetDef.Field,
                Type = facetDef.Type,
                Size = facetDef.MaxValues
            };

            // Add range facets
            if (facetDef.Type == FacetType.Range && facetDef.Ranges?.Any() == true)
            {
                facetRequest.Ranges = facetDef.Ranges.Select(r => new FacetRange
                {
                    Key = r.Label,
                    From = r.From,
                    To = r.To
                }).ToList();
            }

            // Add date histogram facets
            if (facetDef.Type == FacetType.DateHistogram)
            {
                facetRequest.DateHistogram = new DateHistogramFacet
                {
                    Interval = facetDef.DateInterval ?? "month",
                    Format = facetDef.DateFormat ?? "yyyy-MM"
                };
            }

            facetRequests.Add(facetRequest);
        }

        return facetRequests;
    }

    private IEnumerable<ProcessedFacet> ProcessFacetResults(
        IEnumerable<FacetResult> facetResults,
        FacetConfiguration config,
        Dictionary<string, string[]> selectedFacets)
    {
        var processedFacets = new List<ProcessedFacet>();

        foreach (var facetResult in facetResults)
        {
            var facetDef = config.Facets.FirstOrDefault(f => f.Name == facetResult.Name);
            if (facetDef == null) continue;

            var processedFacet = new ProcessedFacet
            {
                Name = facetResult.Name,
                DisplayName = facetDef.DisplayName,
                Type = facetDef.Type,
                Values = new List<ProcessedFacetValue>()
            };

            // Process facet values
            foreach (var value in facetResult.Values)
            {
                var isSelected = selectedFacets.ContainsKey(facetResult.Name) &&
                               selectedFacets[facetResult.Name].Contains(value.Value);

                processedFacet.Values.Add(new ProcessedFacetValue
                {
                    Value = value.Value,
                    DisplayValue = GetFacetDisplayValue(facetDef, value.Value),
                    Count = value.Count,
                    IsSelected = isSelected,
                    FilterUrl = BuildFacetFilterUrl(facetResult.Name, value.Value, selectedFacets)
                });
            }

            // Sort facet values
            processedFacet.Values = SortFacetValues(processedFacet.Values, facetDef.SortOrder);

            processedFacets.Add(processedFacet);
        }

        return processedFacets;
    }
}
```

### **2. 🌍 Geospatial Search**

```csharp
// Geospatial search service
public class GeospatialSearchService : IGeospatialSearchService
{
    private readonly ISearchService _searchService;
    private readonly IGeocodeService _geocodeService;

    public async Task<GeospatialSearchResult> SearchNearLocationAsync(GeospatialSearchRequest request)
    {
        // Geocode address if provided instead of coordinates
        if (!string.IsNullOrEmpty(request.Address) && 
            (!request.Latitude.HasValue || !request.Longitude.HasValue))
        {
            var geocodeResult = await _geocodeService.GeocodeAsync(request.Address);
            if (geocodeResult.Success)
            {
                request.Latitude = geocodeResult.Latitude;
                request.Longitude = geocodeResult.Longitude;
            }
        }

        if (!request.Latitude.HasValue || !request.Longitude.HasValue)
        {
            throw new ArgumentException("Either coordinates or a valid address must be provided");
        }

        var searchRequest = new SearchRequest
        {
            IndexName = request.IndexName,
            Size = request.Size,
            From = request.From,
            Query = request.Query,
            Filters = BuildGeospatialFilters(request),
            Sort = BuildGeospatialSort(request)
        };

        var searchResult = await _searchService.SearchAsync(searchRequest);

        return new GeospatialSearchResult
        {
            Documents = ProcessGeospatialDocuments(searchResult.Documents, request),
            TotalHits = searchResult.TotalHits,
            CenterPoint = new GeoPoint(request.Latitude.Value, request.Longitude.Value),
            SearchRadius = request.RadiusKm,
            SearchTime = searchResult.SearchTime
        };
    }

    private IEnumerable<SearchFilter> BuildGeospatialFilters(GeospatialSearchRequest request)
    {
        var filters = new List<SearchFilter>(request.Filters ?? []);

        // Add distance filter
        filters.Add(new SearchFilter
        {
            Type = "geo_distance",
            Field = request.LocationField ?? "location",
            Value = new
            {
                distance = $"{request.RadiusKm}km",
                location = new
                {
                    lat = request.Latitude.Value,
                    lon = request.Longitude.Value
                }
            }
        });

        // Add bounding box filter if specified
        if (request.BoundingBox != null)
        {
            filters.Add(new SearchFilter
            {
                Type = "geo_bounding_box",
                Field = request.LocationField ?? "location",
                Value = new
                {
                    top_left = new
                    {
                        lat = request.BoundingBox.TopLeft.Latitude,
                        lon = request.BoundingBox.TopLeft.Longitude
                    },
                    bottom_right = new
                    {
                        lat = request.BoundingBox.BottomRight.Latitude,
                        lon = request.BoundingBox.BottomRight.Longitude
                    }
                }
            });
        }

        return filters;
    }

    private IEnumerable<SearchSort> BuildGeospatialSort(GeospatialSearchRequest request)
    {
        var sorts = new List<SearchSort>();

        // Add distance sort
        sorts.Add(new SearchSort
        {
            Type = "geo_distance",
            Field = request.LocationField ?? "location",
            Direction = "asc",
            GeoPoint = new GeoPoint(request.Latitude.Value, request.Longitude.Value),
            Unit = "km"
        });

        // Add additional sorts
        if (request.Sort?.Any() == true)
        {
            sorts.AddRange(request.Sort);
        }

        return sorts;
    }

    private IEnumerable<GeospatialDocument> ProcessGeospatialDocuments(
        IEnumerable<SearchDocument> documents,
        GeospatialSearchRequest request)
    {
        var centerPoint = new GeoPoint(request.Latitude.Value, request.Longitude.Value);

        return documents.Select(doc =>
        {
            var geoDoc = new GeospatialDocument(doc);
            
            // Calculate distance if location is available
            if (doc.TryGetValue(request.LocationField ?? "location", out var locationValue))
            {
                if (TryParseGeoPoint(locationValue.ToString(), out var docLocation))
                {
                    geoDoc.DistanceKm = CalculateDistance(centerPoint, docLocation);
                    geoDoc.Location = docLocation;
                }
            }

            return geoDoc;
        });
    }

    private double CalculateDistance(GeoPoint point1, GeoPoint point2)
    {
        // Haversine formula for calculating distance between two points
        const double R = 6371; // Earth's radius in kilometers

        var lat1Rad = ToRadians(point1.Latitude);
        var lat2Rad = ToRadians(point2.Latitude);
        var deltaLatRad = ToRadians(point2.Latitude - point1.Latitude);
        var deltaLonRad = ToRadians(point2.Longitude - point1.Longitude);

        var a = Math.Sin(deltaLatRad / 2) * Math.Sin(deltaLatRad / 2) +
                Math.Cos(lat1Rad) * Math.Cos(lat2Rad) *
                Math.Sin(deltaLonRad / 2) * Math.Sin(deltaLonRad / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return R * c;
    }
}
```

---

## 🚀 **PERFORMANCE OPTIMIZATION**

### **1. ⚡ Search Performance Tuning**

```csharp
// Search performance optimizer
public class SearchPerformanceOptimizer : ISearchPerformanceOptimizer
{
    private readonly ISearchService _searchService;
    private readonly IMemoryCache _cache;
    private readonly IDistributedCache _distributedCache;
    private readonly IMetricsCollector _metricsCollector;

    public async Task<SearchResult> OptimizedSearchAsync(SearchRequest request)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            // Check cache first
            var cacheKey = GenerateSearchCacheKey(request);
            var cachedResult = await GetCachedResultAsync(cacheKey);
            if (cachedResult != null)
            {
                _metricsCollector.Histogram("search.cache_hit_duration", stopwatch.ElapsedMilliseconds);
                return cachedResult;
            }

            // Optimize query
            var optimizedRequest = await OptimizeSearchRequestAsync(request);

            // Execute search with timeout
            var searchTask = _searchService.SearchAsync(optimizedRequest);
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(30));
            
            var completedTask = await Task.WhenAny(searchTask, timeoutTask);
            
            if (completedTask == timeoutTask)
            {
                _metricsCollector.Increment("search.timeouts");
                throw new SearchTimeoutException("Search request timed out");
            }

            var result = await searchTask;

            // Cache result if appropriate
            if (ShouldCacheResult(request, result))
            {
                await CacheResultAsync(cacheKey, result);
            }

            // Collect performance metrics
            _metricsCollector.Histogram("search.duration", stopwatch.ElapsedMilliseconds);
            _metricsCollector.Histogram("search.result_count", result.TotalHits);

            return result;
        }
        catch (Exception ex)
        {
            _metricsCollector.Increment("search.errors");
            throw;
        }
        finally
        {
            stopwatch.Stop();
        }
    }

    private async Task<SearchRequest> OptimizeSearchRequestAsync(SearchRequest request)
    {
        var optimized = request.Clone();

        // Optimize query complexity
        if (IsComplexQuery(request.Query))
        {
            optimized.Query = await SimplifyQueryAsync(request.Query);
        }

        // Limit result size for performance
        if (optimized.Size > 1000)
        {
            optimized.Size = 1000;
        }

        // Optimize sorting
        if (optimized.Sort?.Any() == true)
        {
            optimized.Sort = OptimizeSorting(optimized.Sort);
        }

        // Optimize facets
        if (optimized.Facets?.Any() == true)
        {
            optimized.Facets = OptimizeFacets(optimized.Facets);
        }

        return optimized;
    }

    private bool ShouldCacheResult(SearchRequest request, SearchResult result)
    {
        // Cache criteria
        return result.TotalHits > 0 &&
               result.TotalHits < 10000 && // Don't cache very large result sets
               string.IsNullOrEmpty(request.UserId) && // Don't cache personalized results
               request.Filters?.Count() <= 3 && // Simple filters only
               EstimateResultSize(result) < 1024 * 1024; // Max 1MB cache size
    }

    private async Task CacheResultAsync(string cacheKey, SearchResult result)
    {
        var cacheOptions = new DistributedCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromMinutes(15),
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
        };

        var serializedResult = JsonSerializer.SerializeToUtf8Bytes(result);
        await _distributedCache.SetAsync(cacheKey, serializedResult, cacheOptions);
    }
}
```

### **2. 📊 Index Performance Monitoring**

```csharp
// Index performance monitor
public class IndexPerformanceMonitor : IIndexPerformanceMonitor
{
    private readonly IIndexManager _indexManager;
    private readonly IMetricsCollector _metricsCollector;
    private readonly ILogger<IndexPerformanceMonitor> _logger;

    public async Task MonitorIndexPerformanceAsync()
    {
        var indices = await _indexManager.GetIndexNamesAsync();

        foreach (var indexName in indices)
        {
            await MonitorSingleIndexAsync(indexName);
        }
    }

    private async Task MonitorSingleIndexAsync(string indexName)
    {
        try
        {
            var stats = await GetIndexStatsAsync(indexName);
            
            // Collect size metrics
            _metricsCollector.Gauge("index.size_bytes", stats.SizeInBytes, 
                new Dictionary<string, string> { ["index"] = indexName });
            
            _metricsCollector.Gauge("index.document_count", stats.DocumentCount,
                new Dictionary<string, string> { ["index"] = indexName });

            // Monitor search performance
            var searchPerf = await MeasureSearchPerformanceAsync(indexName);
            _metricsCollector.Histogram("index.search_latency", searchPerf.AverageLatencyMs,
                new Dictionary<string, string> { ["index"] = indexName });

            // Monitor indexing performance
            var indexingPerf = await MeasureIndexingPerformanceAsync(indexName);
            _metricsCollector.Histogram("index.indexing_latency", indexingPerf.AverageLatencyMs,
                new Dictionary<string, string> { ["index"] = indexName });

            // Check for performance issues
            await CheckPerformanceIssuesAsync(indexName, stats, searchPerf, indexingPerf);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to monitor performance for index {IndexName}", indexName);
        }
    }

    private async Task CheckPerformanceIssuesAsync(
        string indexName,
        IndexStats stats,
        SearchPerformance searchPerf,
        IndexingPerformance indexingPerf)
    {
        var issues = new List<PerformanceIssue>();

        // Check index size
        if (stats.SizeInBytes > 10L * 1024 * 1024 * 1024) // 10GB
        {
            issues.Add(new PerformanceIssue
            {
                Type = "LargeIndexSize",
                Severity = "Warning",
                Message = $"Index {indexName} is very large ({stats.SizeInBytes / (1024 * 1024 * 1024)}GB)",
                Recommendation = "Consider index partitioning or archiving old data"
            });
        }

        // Check search latency
        if (searchPerf.AverageLatencyMs > 1000)
        {
            issues.Add(new PerformanceIssue
            {
                Type = "SlowSearch",
                Severity = "Warning",
                Message = $"Search latency is high ({searchPerf.AverageLatencyMs}ms) for index {indexName}",
                Recommendation = "Review query complexity and consider index optimization"
            });
        }

        // Check indexing latency
        if (indexingPerf.AverageLatencyMs > 5000)
        {
            issues.Add(new PerformanceIssue
            {
                Type = "SlowIndexing",
                Severity = "Warning",
                Message = $"Indexing latency is high ({indexingPerf.AverageLatencyMs}ms) for index {indexName}",
                Recommendation = "Consider batch indexing or index optimization"
            });
        }

        // Report issues
        foreach (var issue in issues)
        {
            await ReportPerformanceIssueAsync(issue);
        }
    }
}
```

---

## 🔧 **MODULE REGISTRATION & STARTUP**

### **1. 📦 Search Module Startup**

```csharp
// Startup class cho custom search module
public class CustomSearchStartup : StartupBase
{
    public override void ConfigureServices(IServiceCollection services)
    {
        // Core search services
        services.AddScoped<IAdvancedSearchService, AdvancedSearchService>();
        services.AddScoped<IFacetedSearchService, FacetedSearchService>();
        services.AddScoped<IGeospatialSearchService, GeospatialSearchService>();
        
        // Analytics and monitoring
        services.AddScoped<ISearchAnalyticsService, SearchAnalyticsService>();
        services.AddScoped<ISearchPerformanceOptimizer, SearchPerformanceOptimizer>();
        services.AddScoped<IIndexPerformanceMonitor, IndexPerformanceMonitor>();
        
        // Indexing services
        services.AddScoped<IRealTimeIndexingService, RealTimeIndexingService>();
        services.AddScoped<IIndexingOptimizer, IndexingOptimizer>();
        
        // Custom providers
        services.AddScoped<ICustomSearchProvider, ElasticsearchProvider>();
        services.AddScoped<ICustomSearchProvider, SolrProvider>();
        
        // Background services
        services.AddHostedService<IndexOptimizationBackgroundService>();
        services.AddHostedService<SearchAnalyticsBackgroundService>();
        
        // Configuration
        services.Configure<SearchOptions>(options =>
        {
            options.DefaultPageSize = 20;
            options.MaxPageSize = 1000;
            options.EnableCaching = true;
            options.CacheExpirationMinutes = 15;
            options.EnableAnalytics = true;
            options.EnablePerformanceMonitoring = true;
        });

        // Elasticsearch configuration
        services.Configure<ElasticsearchOptions>(options =>
        {
            options.ConnectionString = "http://localhost:9200";
            options.DefaultIndex = "orchardcore";
            options.EnableSniffing = true;
            options.RequestTimeout = TimeSpan.FromSeconds(30);
        });
    }

    public override void Configure(IApplicationBuilder app, IEndpointRouteBuilder routes, IServiceProvider serviceProvider)
    {
        // Search API routes
        routes.MapControllerRoute(
            name: "SearchApi",
            pattern: "api/search/{action=Search}",
            defaults: new { controller = "SearchApi" });

        // Faceted search routes
        routes.MapControllerRoute(
            name: "FacetedSearch",
            pattern: "search/faceted/{action=Search}",
            defaults: new { controller = "FacetedSearch" });

        // Geospatial search routes
        routes.MapControllerRoute(
            name: "GeospatialSearch",
            pattern: "search/geo/{action=Search}",
            defaults: new { controller = "GeospatialSearch" });
    }
}

// Module manifest
[assembly: Module(
    Name = "Advanced Search & Indexing",
    Author = "Your Name",
    Website = "https://yourwebsite.com",
    Version = "1.0.0",
    Description = "Advanced search capabilities with multiple engines, analytics, and optimization",
    Category = "Search",
    Dependencies = new[] { "OrchardCore.Indexing", "OrchardCore.Search.Lucene" }
)]
```

### **2. ⚙️ Configuration Options**

```csharp
// Search configuration options
public class SearchOptions
{
    public int DefaultPageSize { get; set; } = 20;
    public int MaxPageSize { get; set; } = 1000;
    public bool EnableCaching { get; set; } = true;
    public int CacheExpirationMinutes { get; set; } = 15;
    public bool EnableAnalytics { get; set; } = true;
    public bool EnablePerformanceMonitoring { get; set; } = true;
    public bool EnableAutoComplete { get; set; } = true;
    public bool EnableSpellCheck { get; set; } = true;
    public bool EnableSuggestions { get; set; } = true;
    public SearchEngineOptions DefaultEngine { get; set; } = new();
    public List<SearchEngineOptions> Engines { get; set; } = new();
}

public class SearchEngineOptions
{
    public string Name { get; set; } = "Lucene";
    public string ConnectionString { get; set; }
    public Dictionary<string, object> Settings { get; set; } = new();
    public bool IsDefault { get; set; } = false;
    public bool Enabled { get; set; } = true;
}

public class ElasticsearchOptions
{
    public string ConnectionString { get; set; } = "http://localhost:9200";
    public string DefaultIndex { get; set; } = "orchardcore";
    public string Username { get; set; }
    public string Password { get; set; }
    public bool EnableSniffing { get; set; } = true;
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public int MaxRetries { get; set; } = 3;
    public bool EnableDebugMode { get; set; } = false;
}
```

---

## 🎯 **KẾT LUẬN**

**Search & Indexing** trong OrchardCore cung cấp foundation mạnh mẽ cho:

- **🔍 Multiple Search Engines**: Lucene.NET, Elasticsearch, custom providers
- **📊 Advanced Features**: Faceted search, geospatial, auto-complete, spell check
- **⚡ Performance Optimization**: Caching, query optimization, index tuning
- **📈 Analytics & Monitoring**: Search patterns, performance metrics, optimization suggestions
- **🔄 Real-time Indexing**: Background processing, batch updates, smart optimization

**Search & Indexing là component quan trọng cho mọi ứng dụng cần tìm kiếm thông tin hiệu quả và chính xác! 🚀**

---

## 🎯 **KHI NÀO CẦN SEARCH & INDEXING - VÍ DỤ THỰC TẾ**

### **1. 🛒 E-commerce Marketplace - Tiki/Shopee Clone**

#### **Tình huống:**
Anh xây dựng **marketplace online** với hàng triệu sản phẩm từ nhiều seller. Users cần tìm kiếm sản phẩm nhanh, chính xác với filters, facets, và suggestions.

#### **❌ TRƯỚC KHI DÙNG SEARCH & INDEXING:**
```csharp
// Tìm kiếm thủ công bằng SQL - chậm và không linh hoạt
public async Task<IActionResult> SearchProducts(string query, decimal? minPrice, decimal? maxPrice)
{
    var products = await _dbContext.Products
        .Where(p => p.Name.Contains(query) || p.Description.Contains(query))
        .Where(p => !minPrice.HasValue || p.Price >= minPrice)
        .Where(p => !maxPrice.HasValue || p.Price <= maxPrice)
        .Take(20)
        .ToListAsync();
    
    // Không có:
    // - Fuzzy search (tìm "iphone" khi gõ "iphone")
    // - Faceted search (filter theo brand, category, rating)
    // - Auto-complete suggestions
    // - Search analytics
    // - Relevance scoring
    // - Performance optimization
    
    return View(products);
}
```

#### **✅ SAU KHI DÙNG SEARCH & INDEXING:**
```csharp
// Professional E-commerce Search Service
public class EcommerceSearchService : IEcommerceSearchService
{
    private readonly IAdvancedSearchService _searchService;
    private readonly IFacetedSearchService _facetedSearchService;
    private readonly ISearchAnalyticsService _analyticsService;
    private readonly IAutoCompleteService _autoCompleteService;

    public async Task<EcommerceSearchResult> SearchProductsAsync(EcommerceSearchRequest request)
    {
        // 1. Track search analytics
        await _analyticsService.TrackSearchAsync(new SearchAnalyticsEvent
        {
            Query = request.Query,
            UserId = request.UserId,
            IndexName = "products",
            Filters = request.Filters,
            Timestamp = DateTime.UtcNow
        });

        // 2. Build advanced search với multiple strategies
        var searchRequest = new AdvancedSearchRequest
        {
            Query = BuildSmartQuery(request.Query),
            IndexName = "products",
            Size = request.PageSize,
            From = request.PageIndex * request.PageSize,
            Filters = BuildEcommerceFilters(request),
            Facets = BuildEcommerceFacets(),
            Sort = BuildEcommerceSorting(request.SortBy),
            Highlight = new HighlightOptions
            {
                Fields = new[] { "name", "description", "brand" },
                PreTag = "<mark>",
                PostTag = "</mark>"
            },
            IncludeAutoComplete = true,
            IncludeSpellCheck = true
        };

        var searchResult = await _searchService.SearchAsync(searchRequest);

        // 3. Process results cho e-commerce context
        var ecommerceResult = new EcommerceSearchResult
        {
            Products = await ProcessProductSearchResults(searchResult.Documents),
            TotalProducts = searchResult.TotalHits,
            Facets = ProcessEcommerceFacets(searchResult.Facets),
            Suggestions = searchResult.Suggestions,
            AutoComplete = searchResult.AutoCompleteSuggestions,
            SpellCheck = searchResult.SpellCheckSuggestions,
            SearchTime = searchResult.SearchTime,
            Query = request.Query
        };

        // 4. Add personalized recommendations
        if (!string.IsNullOrEmpty(request.UserId))
        {
            ecommerceResult.PersonalizedSuggestions = await GetPersonalizedSuggestionsAsync(
                request.UserId, request.Query, searchResult.Documents.Take(5));
        }

        // 5. Add trending products if few results
        if (searchResult.TotalHits < 10)
        {
            ecommerceResult.TrendingProducts = await GetTrendingProductsAsync(request.CategoryId);
        }

        return ecommerceResult;
    }

    private SearchQuery BuildSmartQuery(string userQuery)
    {
        if (string.IsNullOrWhiteSpace(userQuery))
        {
            return new SearchQuery { Type = "match_all" };
        }

        // Multi-field search với different weights
        return new SearchQuery
        {
            Type = "multi_match",
            Value = userQuery,
            Fields = new Dictionary<string, float>
            {
                ["name^3"] = 3.0f,           // Product name có weight cao nhất
                ["brand^2"] = 2.0f,          // Brand có weight cao
                ["category"] = 1.5f,         // Category có weight trung bình
                ["description"] = 1.0f,      // Description có weight thấp
                ["tags"] = 1.2f,             // Tags có weight trung bình
                ["sku"] = 2.5f               // SKU có weight cao (exact match)
            },
            Operator = "or",
            MinimumShouldMatch = "75%",      // Ít nhất 75% terms phải match
            Fuzziness = "AUTO",              // Auto fuzzy search
            PrefixLength = 2                 // Prefix length cho fuzzy
        };
    }

    private IEnumerable<SearchFilter> BuildEcommerceFilters(EcommerceSearchRequest request)
    {
        var filters = new List<SearchFilter>();

        // Price range filter
        if (request.MinPrice.HasValue || request.MaxPrice.HasValue)
        {
            filters.Add(new SearchFilter
            {
                Type = "range",
                Field = "price",
                From = request.MinPrice,
                To = request.MaxPrice
            });
        }

        // Brand filter
        if (request.Brands?.Any() == true)
        {
            filters.Add(new SearchFilter
            {
                Type = "terms",
                Field = "brand.keyword",
                Values = request.Brands
            });
        }

        // Category filter
        if (request.Categories?.Any() == true)
        {
            filters.Add(new SearchFilter
            {
                Type = "terms",
                Field = "category_path",
                Values = request.Categories
            });
        }

        // Rating filter
        if (request.MinRating.HasValue)
        {
            filters.Add(new SearchFilter
            {
                Type = "range",
                Field = "average_rating",
                From = request.MinRating.Value
            });
        }

        // Availability filter
        if (request.InStockOnly)
        {
            filters.Add(new SearchFilter
            {
                Type = "term",
                Field = "in_stock",
                Value = true
            });
        }

        // Seller filter
        if (request.SellerIds?.Any() == true)
        {
            filters.Add(new SearchFilter
            {
                Type = "terms",
                Field = "seller_id",
                Values = request.SellerIds.Select(id => id.ToString())
            });
        }

        // Free shipping filter
        if (request.FreeShippingOnly)
        {
            filters.Add(new SearchFilter
            {
                Type = "term",
                Field = "free_shipping",
                Value = true
            });
        }

        return filters;
    }

    private IEnumerable<FacetRequest> BuildEcommerceFacets()
    {
        return new[]
        {
            // Brand facet
            new FacetRequest
            {
                Name = "brands",
                Field = "brand.keyword",
                Type = FacetType.Terms,
                Size = 20
            },
            
            // Category facet
            new FacetRequest
            {
                Name = "categories",
                Field = "category.keyword",
                Type = FacetType.Terms,
                Size = 15
            },
            
            // Price range facet
            new FacetRequest
            {
                Name = "price_ranges",
                Field = "price",
                Type = FacetType.Range,
                Ranges = new[]
                {
                    new FacetRange { Key = "Under 100K", To = 100000 },
                    new FacetRange { Key = "100K - 500K", From = 100000, To = 500000 },
                    new FacetRange { Key = "500K - 1M", From = 500000, To = 1000000 },
                    new FacetRange { Key = "1M - 5M", From = 1000000, To = 5000000 },
                    new FacetRange { Key = "Over 5M", From = 5000000 }
                }
            },
            
            // Rating facet
            new FacetRequest
            {
                Name = "ratings",
                Field = "average_rating",
                Type = FacetType.Range,
                Ranges = new[]
                {
                    new FacetRange { Key = "4+ stars", From = 4.0 },
                    new FacetRange { Key = "3+ stars", From = 3.0 },
                    new FacetRange { Key = "2+ stars", From = 2.0 }
                }
            },
            
            // Seller facet
            new FacetRequest
            {
                Name = "sellers",
                Field = "seller_name.keyword",
                Type = FacetType.Terms,
                Size = 10
            },
            
            // Features facet
            new FacetRequest
            {
                Name = "features",
                Field = "features.keyword",
                Type = FacetType.Terms,
                Size = 15
            }
        };
    }

    private async Task<IEnumerable<EcommerceProduct>> ProcessProductSearchResults(
        IEnumerable<SearchDocument> documents)
    {
        var products = new List<EcommerceProduct>();

        foreach (var doc in documents)
        {
            var product = new EcommerceProduct
            {
                Id = doc.GetValue("id")?.ToString(),
                Name = doc.GetValue("name")?.ToString(),
                Description = doc.GetValue("description")?.ToString(),
                Price = Convert.ToDecimal(doc.GetValue("price")),
                OriginalPrice = Convert.ToDecimal(doc.GetValue("original_price")),
                Brand = doc.GetValue("brand")?.ToString(),
                Category = doc.GetValue("category")?.ToString(),
                ImageUrl = doc.GetValue("image_url")?.ToString(),
                AverageRating = Convert.ToDouble(doc.GetValue("average_rating")),
                ReviewCount = Convert.ToInt32(doc.GetValue("review_count")),
                SoldCount = Convert.ToInt32(doc.GetValue("sold_count")),
                InStock = Convert.ToBoolean(doc.GetValue("in_stock")),
                FreeShipping = Convert.ToBoolean(doc.GetValue("free_shipping")),
                SellerName = doc.GetValue("seller_name")?.ToString(),
                
                // Highlight information
                HighlightedName = doc.GetHighlight("name"),
                HighlightedDescription = doc.GetHighlight("description"),
                
                // Search relevance score
                RelevanceScore = doc.Score,
                
                // Discount calculation
                DiscountPercentage = CalculateDiscountPercentage(
                    Convert.ToDecimal(doc.GetValue("original_price")),
                    Convert.ToDecimal(doc.GetValue("price")))
            };

            products.Add(product);
        }

        return products;
    }
}
```

#### **🎯 Kết quả so sánh:**

| **Trước Search & Indexing** | **Sau Search & Indexing** |
|------------------------------|---------------------------|
| ❌ SQL LIKE queries - chậm với millions records | ✅ Elasticsearch/Lucene - sub-second search |
| ❌ Exact match only | ✅ Fuzzy search, synonyms, auto-correct |
| ❌ Không có faceted search | ✅ Dynamic facets: brand, price, rating, category |
| ❌ Không có auto-complete | ✅ Real-time suggestions khi typing |
| ❌ Không track user behavior | ✅ Search analytics, popular queries, zero-results |
| ❌ Không có relevance scoring | ✅ Smart ranking: popularity, ratings, sales |

---

### **2. 📚 Digital Library - Google Scholar Clone**

#### **Tình huống:**
Anh xây dựng **thư viện số** với hàng triệu tài liệu khoa học, sách, báo cáo. Researchers cần tìm kiếm chính xác theo tác giả, chủ đề, năm xuất bản.

#### **❌ TRƯỚC KHI DÙNG SEARCH & INDEXING:**
```csharp
// Tìm kiếm tài liệu đơn giản - không hiệu quả
public async Task<IActionResult> SearchDocuments(string query, string author, int? year)
{
    var documents = await _dbContext.Documents
        .Where(d => string.IsNullOrEmpty(query) || 
                   d.Title.Contains(query) || 
                   d.Abstract.Contains(query))
        .Where(d => string.IsNullOrEmpty(author) || 
                   d.Authors.Any(a => a.Name.Contains(author)))
        .Where(d => !year.HasValue || d.PublicationYear == year)
        .OrderByDescending(d => d.PublicationYear)
        .Take(50)
        .ToListAsync();
    
    // Thiếu:
    // - Full-text search trong PDF content
    // - Citation analysis
    // - Related documents
    // - Advanced academic filters
    // - Research trend analysis
    
    return View(documents);
}
```

#### **✅ SAU KHI DÙNG SEARCH & INDEXING:**
```csharp
// Academic Search Service - Chuyên nghiệp
public class AcademicSearchService : IAcademicSearchService
{
    private readonly IAdvancedSearchService _searchService;
    private readonly ICitationAnalysisService _citationService;
    private readonly IDocumentProcessor _documentProcessor;
    private readonly IResearchTrendService _trendService;

    public async Task<AcademicSearchResult> SearchDocumentsAsync(AcademicSearchRequest request)
    {
        // 1. Build academic search query
        var searchRequest = new AdvancedSearchRequest
        {
            Query = BuildAcademicQuery(request),
            IndexName = "academic_documents",
            Size = request.PageSize,
            From = request.PageIndex * request.PageSize,
            Filters = BuildAcademicFilters(request),
            Facets = BuildAcademicFacets(),
            Sort = BuildAcademicSorting(request.SortBy),
            Highlight = new HighlightOptions
            {
                Fields = new[] { "title", "abstract", "full_text", "authors.name" },
                FragmentSize = 150,
                NumberOfFragments = 3
            }
        };

        var searchResult = await _searchService.SearchAsync(searchRequest);

        // 2. Process academic results
        var academicResult = new AcademicSearchResult
        {
            Documents = await ProcessAcademicDocuments(searchResult.Documents),
            TotalDocuments = searchResult.TotalHits,
            Facets = ProcessAcademicFacets(searchResult.Facets),
            SearchTime = searchResult.SearchTime
        };

        // 3. Add citation analysis
        foreach (var doc in academicResult.Documents)
        {
            doc.CitationMetrics = await _citationService.GetCitationMetricsAsync(doc.Id);
            doc.RelatedDocuments = await GetRelatedDocumentsAsync(doc.Id, 5);
        }

        // 4. Add research trends
        if (!string.IsNullOrEmpty(request.Query))
        {
            academicResult.ResearchTrends = await _trendService.GetResearchTrendsAsync(
                request.Query, DateTime.Now.AddYears(-10), DateTime.Now);
        }

        // 5. Add author collaboration network
        if (!string.IsNullOrEmpty(request.Author))
        {
            academicResult.AuthorNetwork = await GetAuthorCollaborationNetworkAsync(request.Author);
        }

        return academicResult;
    }

    private SearchQuery BuildAcademicQuery(AcademicSearchRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
        {
            return new SearchQuery { Type = "match_all" };
        }

        // Academic-specific multi-field search
        return new SearchQuery
        {
            Type = "bool",
            BoolQuery = new BooleanQuery
            {
                Should = new[]
                {
                    // Title search - highest weight
                    new SearchQuery
                    {
                        Type = "match",
                        Field = "title",
                        Value = request.Query,
                        Boost = 5.0f
                    },
                    
                    // Abstract search - high weight
                    new SearchQuery
                    {
                        Type = "match",
                        Field = "abstract",
                        Value = request.Query,
                        Boost = 3.0f
                    },
                    
                    // Keywords search - high weight
                    new SearchQuery
                    {
                        Type = "match",
                        Field = "keywords",
                        Value = request.Query,
                        Boost = 4.0f
                    },
                    
                    // Full text search - medium weight
                    new SearchQuery
                    {
                        Type = "match",
                        Field = "full_text",
                        Value = request.Query,
                        Boost = 1.0f
                    },
                    
                    // Author search
                    new SearchQuery
                    {
                        Type = "nested",
                        Path = "authors",
                        Query = new SearchQuery
                        {
                            Type = "match",
                            Field = "authors.name",
                            Value = request.Query,
                            Boost = 2.0f
                        }
                    },
                    
                    // Subject classification search
                    new SearchQuery
                    {
                        Type = "match",
                        Field = "subject_classifications",
                        Value = request.Query,
                        Boost = 2.5f
                    }
                },
                MinimumShouldMatch = 1
            }
        };
    }

    private IEnumerable<SearchFilter> BuildAcademicFilters(AcademicSearchRequest request)
    {
        var filters = new List<SearchFilter>();

        // Publication year range
        if (request.FromYear.HasValue || request.ToYear.HasValue)
        {
            filters.Add(new SearchFilter
            {
                Type = "range",
                Field = "publication_year",
                From = request.FromYear,
                To = request.ToYear
            });
        }

        // Document type filter
        if (request.DocumentTypes?.Any() == true)
        {
            filters.Add(new SearchFilter
            {
                Type = "terms",
                Field = "document_type",
                Values = request.DocumentTypes
            });
        }

        // Subject area filter
        if (request.SubjectAreas?.Any() == true)
        {
            filters.Add(new SearchFilter
            {
                Type = "terms",
                Field = "subject_area",
                Values = request.SubjectAreas
            });
        }

        // Language filter
        if (request.Languages?.Any() == true)
        {
            filters.Add(new SearchFilter
            {
                Type = "terms",
                Field = "language",
                Values = request.Languages
            });
        }

        // Publisher filter
        if (request.Publishers?.Any() == true)
        {
            filters.Add(new SearchFilter
            {
                Type = "terms",
                Field = "publisher.keyword",
                Values = request.Publishers
            });
        }

        // Citation count filter
        if (request.MinCitations.HasValue)
        {
            filters.Add(new SearchFilter
            {
                Type = "range",
                Field = "citation_count",
                From = request.MinCitations.Value
            });
        }

        // Open access filter
        if (request.OpenAccessOnly)
        {
            filters.Add(new SearchFilter
            {
                Type = "term",
                Field = "is_open_access",
                Value = true
            });
        }

        // Peer reviewed filter
        if (request.PeerReviewedOnly)
        {
            filters.Add(new SearchFilter
            {
                Type = "term",
                Field = "is_peer_reviewed",
                Value = true
            });
        }

        return filters;
    }

    private async Task<IEnumerable<AcademicDocument>> ProcessAcademicDocuments(
        IEnumerable<SearchDocument> documents)
    {
        var academicDocs = new List<AcademicDocument>();

        foreach (var doc in documents)
        {
            var academicDoc = new AcademicDocument
            {
                Id = doc.GetValue("id")?.ToString(),
                Title = doc.GetValue("title")?.ToString(),
                Abstract = doc.GetValue("abstract")?.ToString(),
                Authors = ParseAuthors(doc.GetValue("authors")),
                PublicationYear = Convert.ToInt32(doc.GetValue("publication_year")),
                Publisher = doc.GetValue("publisher")?.ToString(),
                Journal = doc.GetValue("journal")?.ToString(),
                Volume = doc.GetValue("volume")?.ToString(),
                Issue = doc.GetValue("issue")?.ToString(),
                Pages = doc.GetValue("pages")?.ToString(),
                DOI = doc.GetValue("doi")?.ToString(),
                Keywords = ParseKeywords(doc.GetValue("keywords")),
                SubjectAreas = ParseSubjectAreas(doc.GetValue("subject_areas")),
                DocumentType = doc.GetValue("document_type")?.ToString(),
                Language = doc.GetValue("language")?.ToString(),
                IsOpenAccess = Convert.ToBoolean(doc.GetValue("is_open_access")),
                IsPeerReviewed = Convert.ToBoolean(doc.GetValue("is_peer_reviewed")),
                
                // Highlight information
                HighlightedTitle = doc.GetHighlight("title"),
                HighlightedAbstract = doc.GetHighlight("abstract"),
                HighlightedFullText = doc.GetHighlight("full_text"),
                
                // Search relevance
                RelevanceScore = doc.Score
            };

            academicDocs.Add(academicDoc);
        }

        return academicDocs;
    }

    // Advanced citation analysis
    private async Task<CitationMetrics> GetCitationMetricsAsync(string documentId)
    {
        var citationData = await _citationService.AnalyzeCitationsAsync(documentId);
        
        return new CitationMetrics
        {
            TotalCitations = citationData.TotalCitations,
            CitationsPerYear = citationData.CitationsPerYear,
            HIndex = citationData.HIndex,
            ImpactFactor = citationData.ImpactFactor,
            RecentCitations = citationData.RecentCitations,
            CitingAuthors = citationData.CitingAuthors,
            CitationTrend = citationData.CitationTrend
        };
    }
}
```

#### **🎯 Kết quả so sánh:**

| **Trước Search & Indexing** | **Sau Search & Indexing** |
|------------------------------|---------------------------|
| ❌ Chỉ search title/abstract | ✅ Full-text search trong PDF content |
| ❌ Không có citation analysis | ✅ Citation metrics, H-index, impact factor |
| ❌ Không có related documents | ✅ AI-powered document recommendations |
| ❌ Basic filters | ✅ Advanced academic filters: peer-reviewed, open access |
| ❌ Không có trend analysis | ✅ Research trend visualization |
| ❌ Không có author network | ✅ Author collaboration network |

---

### **3. 🏥 Hospital Management System - Patient Records Search**

#### **Tình huống:**
Anh quản lý **hệ thống bệnh viện** với hàng triệu hồ sơ bệnh nhân. Doctors cần tìm kiếm nhanh theo triệu chứng, chẩn đoán, thuốc, lịch sử điều trị.

#### **❌ TRƯỚC KHI DÙNG SEARCH & INDEXING:**
```csharp
// Tìm kiếm hồ sơ y tế cơ bản - không hiệu quả
public async Task<IActionResult> SearchPatients(string patientName, string diagnosis)
{
    var patients = await _dbContext.Patients
        .Where(p => string.IsNullOrEmpty(patientName) || 
                   p.FullName.Contains(patientName))
        .Where(p => string.IsNullOrEmpty(diagnosis) || 
                   p.MedicalRecords.Any(r => r.Diagnosis.Contains(diagnosis)))
        .Include(p => p.MedicalRecords)
        .Take(50)
        .ToListAsync();
    
    // Thiếu:
    // - Medical terminology search
    // - Symptom-based search
    // - Drug interaction search
    // - Similar case finding
    // - Medical image search
    // - HIPAA compliance logging
    
    return View(patients);
}
```

#### **✅ SAU KHI DÙNG SEARCH & INDEXING:**
```csharp
// Medical Search Service - HIPAA Compliant
public class MedicalSearchService : IMedicalSearchService
{
    private readonly IAdvancedSearchService _searchService;
    private readonly IMedicalTerminologyService _terminologyService;
    private readonly IHipaaAuditService _auditService;
    private readonly IMedicalImageSearchService _imageSearchService;

    public async Task<MedicalSearchResult> SearchPatientsAsync(
        MedicalSearchRequest request, 
        string doctorId)
    {
        // 1. HIPAA compliance check
        await ValidateSearchPermissionsAsync(doctorId, request);
        
        // 2. Log medical search for audit
        await _auditService.LogMedicalSearchAsync(new MedicalSearchAuditEvent
        {
            DoctorId = doctorId,
            SearchQuery = request.Query,
            SearchType = request.SearchType,
            PatientId = request.PatientId,
            Timestamp = DateTime.UtcNow,
            IpAddress = GetClientIpAddress(),
            UserAgent = GetUserAgent()
        });

        // 3. Build medical search query
        var searchRequest = new AdvancedSearchRequest
        {
            Query = await BuildMedicalQueryAsync(request),
            IndexName = "medical_records",
            Size = request.PageSize,
            From = request.PageIndex * request.PageSize,
            Filters = BuildMedicalFilters(request, doctorId),
            Facets = BuildMedicalFacets(),
            Sort = BuildMedicalSorting(request.SortBy),
            Highlight = new HighlightOptions
            {
                Fields = new[] { "symptoms", "diagnosis", "treatment_notes", "medications" },
                FragmentSize = 100,
                NumberOfFragments = 2
            }
        };

        var searchResult = await _searchService.SearchAsync(searchRequest);

        // 4. Process medical results với privacy protection
        var medicalResult = new MedicalSearchResult
        {
            PatientRecords = await ProcessMedicalRecords(searchResult.Documents, doctorId),
            TotalRecords = searchResult.TotalHits,
            Facets = ProcessMedicalFacets(searchResult.Facets),
            SearchTime = searchResult.SearchTime
        };

        // 5. Add similar cases
        if (request.IncludeSimilarCases)
        {
            medicalResult.SimilarCases = await FindSimilarCasesAsync(request.Query, doctorId);
        }

        // 6. Add drug interaction warnings
        if (request.Medications?.Any() == true)
        {
            medicalResult.DrugInteractions = await CheckDrugInteractionsAsync(request.Medications);
        }

        // 7. Add medical image search results
        if (request.IncludeMedicalImages)
        {
            medicalResult.RelatedImages = await _imageSearchService.SearchMedicalImagesAsync(
                request.Query, doctorId);
        }

        return medicalResult;
    }

    private async Task<SearchQuery> BuildMedicalQueryAsync(MedicalSearchRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
        {
            return new SearchQuery { Type = "match_all" };
        }

        // Expand medical terminology
        var expandedTerms = await _terminologyService.ExpandMedicalTermsAsync(request.Query);
        
        return new SearchQuery
        {
            Type = "bool",
            BoolQuery = new BooleanQuery
            {
                Should = new[]
                {
                    // Patient demographics
                    new SearchQuery
                    {
                        Type = "multi_match",
                        Value = request.Query,
                        Fields = new Dictionary<string, float>
                        {
                            ["patient_name^2"] = 2.0f,
                            ["patient_id^3"] = 3.0f,
                            ["medical_record_number^3"] = 3.0f
                        }
                    },
                    
                    // Symptoms search với medical synonyms
                    new SearchQuery
                    {
                        Type = "bool",
                        BoolQuery = new BooleanQuery
                        {
                            Should = expandedTerms.Symptoms.Select(symptom => new SearchQuery
                            {
                                Type = "match",
                                Field = "symptoms",
                                Value = symptom,
                                Boost = 2.5f
                            }).ToArray()
                        }
                    },
                    
                    // Diagnosis search với ICD-10 codes
                    new SearchQuery
                    {
                        Type = "bool",
                        BoolQuery = new BooleanQuery
                        {
                            Should = expandedTerms.Diagnoses.Select(diagnosis => new SearchQuery
                            {
                                Type = "match",
                                Field = "diagnosis",
                                Value = diagnosis,
                                Boost = 3.0f
                            }).ToArray()
                        }
                    },
                    
                    // Medication search
                    new SearchQuery
                    {
                        Type = "bool",
                        BoolQuery = new BooleanQuery
                        {
                            Should = expandedTerms.Medications.Select(med => new SearchQuery
                            {
                                Type = "match",
                                Field = "medications.name",
                                Value = med,
                                Boost = 2.0f
                            }).ToArray()
                        }
                    },
                    
                    // Treatment notes search
                    new SearchQuery
                    {
                        Type = "match",
                        Field = "treatment_notes",
                        Value = request.Query,
                        Boost = 1.5f
                    },
                    
                    // Lab results search
                    new SearchQuery
                    {
                        Type = "nested",
                        Path = "lab_results",
                        Query = new SearchQuery
                        {
                            Type = "multi_match",
                            Value = request.Query,
                            Fields = new Dictionary<string, float>
                            {
                                ["lab_results.test_name"] = 1.5f,
                                ["lab_results.result_value"] = 1.0f,
                                ["lab_results.interpretation"] = 1.2f
                            }
                        }
                    }
                },
                MinimumShouldMatch = 1
            }
        };
    }

    private IEnumerable<SearchFilter> BuildMedicalFilters(MedicalSearchRequest request, string doctorId)
    {
        var filters = new List<SearchFilter>();

        // Doctor access control - chỉ xem patients được assign
        filters.Add(new SearchFilter
        {
            Type = "bool",
            BoolQuery = new BooleanQuery
            {
                Should = new[]
                {
                    new SearchQuery
                    {
                        Type = "term",
                        Field = "attending_doctor_id",
                        Value = doctorId
                    },
                    new SearchQuery
                    {
                        Type = "term",
                        Field = "consulting_doctors",
                        Value = doctorId
                    },
                    new SearchQuery
                    {
                        Type = "term",
                        Field = "department",
                        Value = await GetDoctorDepartmentAsync(doctorId)
                    }
                }
            }
        });

        // Date range filter
        if (request.FromDate.HasValue || request.ToDate.HasValue)
        {
            filters.Add(new SearchFilter
            {
                Type = "range",
                Field = "visit_date",
                From = request.FromDate,
                To = request.ToDate
            });
        }

        // Age range filter
        if (request.MinAge.HasValue || request.MaxAge.HasValue)
        {
            filters.Add(new SearchFilter
            {
                Type = "range",
                Field = "patient_age",
                From = request.MinAge,
                To = request.MaxAge
            });
        }

        // Gender filter
        if (!string.IsNullOrEmpty(request.Gender))
        {
            filters.Add(new SearchFilter
            {
                Type = "term",
                Field = "patient_gender",
                Value = request.Gender
            });
        }

        // Department filter
        if (request.Departments?.Any() == true)
        {
            filters.Add(new SearchFilter
            {
                Type = "terms",
                Field = "department",
                Values = request.Departments
            });
        }

        // Urgency level filter
        if (request.UrgencyLevels?.Any() == true)
        {
            filters.Add(new SearchFilter
            {
                Type = "terms",
                Field = "urgency_level",
                Values = request.UrgencyLevels
            });
        }

        return filters;
    }

    private async Task<IEnumerable<MedicalRecord>> ProcessMedicalRecords(
        IEnumerable<SearchDocument> documents, 
        string doctorId)
    {
        var records = new List<MedicalRecord>();

        foreach (var doc in documents)
        {
            // Verify doctor has access to this record
            if (!await VerifyRecordAccessAsync(doc.GetValue("patient_id")?.ToString(), doctorId))
            {
                continue;
            }

            var record = new MedicalRecord
            {
                PatientId = doc.GetValue("patient_id")?.ToString(),
                PatientName = MaskPatientName(doc.GetValue("patient_name")?.ToString()),
                MedicalRecordNumber = doc.GetValue("medical_record_number")?.ToString(),
                VisitDate = Convert.ToDateTime(doc.GetValue("visit_date")),
                Department = doc.GetValue("department")?.ToString(),
                AttendingDoctor = doc.GetValue("attending_doctor_name")?.ToString(),
                
                // Medical information
                Symptoms = ParseSymptoms(doc.GetValue("symptoms")),
                Diagnosis = doc.GetValue("diagnosis")?.ToString(),
                TreatmentNotes = doc.GetValue("treatment_notes")?.ToString(),
                Medications = ParseMedications(doc.GetValue("medications")),
                LabResults = ParseLabResults(doc.GetValue("lab_results")),
                
                // Patient demographics (masked for privacy)
                PatientAge = Convert.ToInt32(doc.GetValue("patient_age")),
                PatientGender = doc.GetValue("patient_gender")?.ToString(),
                
                // Highlight information
                HighlightedSymptoms = doc.GetHighlight("symptoms"),
                HighlightedDiagnosis = doc.GetHighlight("diagnosis"),
                HighlightedTreatment = doc.GetHighlight("treatment_notes"),
                
                // Search relevance
                RelevanceScore = doc.Score
            };

            records.Add(record);
        }

        return records;
    }

    // HIPAA compliance methods
    private string MaskPatientName(string fullName)
    {
        if (string.IsNullOrEmpty(fullName)) return "";
        
        var parts = fullName.Split(' ');
        if (parts.Length == 1) return $"{parts[0][0]}***";
        
        return $"{parts[0]} {parts[1][0]}***";
    }

    private async Task ValidateSearchPermissionsAsync(string doctorId, MedicalSearchRequest request)
    {
        // Check if doctor has permission to search medical records
        var permissions = await GetDoctorPermissionsAsync(doctorId);
        
        if (!permissions.CanSearchMedicalRecords)
        {
            throw new UnauthorizedAccessException("Doctor does not have permission to search medical records");
        }

        // Check specific patient access if patient ID is provided
        if (!string.IsNullOrEmpty(request.PatientId))
        {
            var hasAccess = await VerifyPatientAccessAsync(doctorId, request.PatientId);
            if (!hasAccess)
            {
                throw new UnauthorizedAccessException($"Doctor does not have access to patient {request.PatientId}");
            }
        }
    }
}
```

#### **🎯 Kết quả so sánh:**

| **Trước Search & Indexing** | **Sau Search & Indexing** |
|------------------------------|---------------------------|
| ❌ Basic text search | ✅ Medical terminology expansion, ICD-10 codes |
| ❌ Không có access control | ✅ HIPAA compliant với doctor permissions |
| ❌ Không có audit trail | ✅ Đầy đủ audit log cho compliance |
| ❌ Không tìm similar cases | ✅ AI-powered similar case recommendations |
| ❌ Không check drug interactions | ✅ Real-time drug interaction warnings |
| ❌ Không search medical images | ✅ Medical image search integration |

---

## 💡 **TÓM TẮT KHI NÀO CẦN SEARCH & INDEXING**

### **✅ CẦN DÙNG KHI:**

#### **1. 🔍 Large Dataset với Complex Queries**
- **Ví dụ**: E-commerce với millions products, digital library, medical records
- **Lợi ích**: Sub-second search, faceted filtering, relevance scoring

#### **2. 📊 Cần Advanced Search Features**
- **Ví dụ**: Auto-complete, spell check, fuzzy search, synonyms
- **Lợi ích**: Better user experience, higher conversion rates

#### **3. 📈 Cần Search Analytics**
- **Ví dụ**: Track popular queries, zero-result queries, user behavior
- **Lợi ích**: Business insights, search optimization, personalization

#### **4. 🎯 Domain-Specific Search**
- **Ví dụ**: Medical terminology, academic papers, legal documents
- **Lợi ích**: Specialized search logic, compliance requirements

#### **5. 🌍 Multi-language và Geospatial Search**
- **Ví dụ**: Global platforms, location-based services
- **Lợi ích**: Localized search, distance-based results

### **❌ KHÔNG CẦN DÙNG KHI:**

#### **1. 📄 Small Dataset (< 10,000 records)**
- **Ví dụ**: Company directory, small product catalog
- **Lý do**: Database queries đủ nhanh rồi

#### **2. 🔒 Simple Exact Match Search**
- **Ví dụ**: User lookup by ID, order tracking
- **Lý do**: Không cần fuzzy search hay relevance scoring

#### **3. 💰 Limited Resources**
- **Ví dụ**: Small projects, prototype applications
- **Lý do**: Setup và maintenance cost cao

### **🎯 KẾT LUẬN:**
**Search & Indexing phù hợp nhất cho các ứng dụng có large datasets, complex search requirements, và cần advanced features như faceting, analytics, và domain-specific search!**

---

*Tài liệu này được tạo dựa trên phân tích source code OrchardCore và best practices.*