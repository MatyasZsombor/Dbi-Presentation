using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Core.Bulk;
using Elastic.Clients.Elasticsearch.Core.Search;
using Elastic.Clients.Elasticsearch.IndexManagement;
using Elastic.Clients.Elasticsearch.Mapping;
using Elastic.Clients.Elasticsearch.QueryDsl;
using ElasticSearch;
using ElasticSearch.Models;
using Microsoft.Extensions.Logging;
using ExistsResponse = Elastic.Clients.Elasticsearch.IndexManagement.ExistsResponse;

namespace Elasticsearch.Services;

public class ElasticsearchService(
    ILogger<ElasticsearchService> logger,
    AdminElasticsearchClient adminClient,
    UserElasticsearchClient userClient)
{
    private static readonly string[] HighlightPreTags = ["<em>"];
    private static readonly string[] HighlightPostTags = ["</em>"];

    private static readonly Action<ILogger, Exception?> LogNoDocumentsFound =
        LoggerMessage.Define(
            LogLevel.Information,
            new EventId(1, nameof(SyncIndex)),
            "No document found for indexing");

    private static readonly Action<ILogger, string, string, Exception?> LogSyncIndexFailed =
        LoggerMessage.Define<string, string>(
            LogLevel.Error,
            new EventId(2, nameof(SyncIndex)),
            "Failed to sync index '{IndexName}': {Error}");

    private static readonly Action<ILogger, string, string, Exception?> LogDocumentIndexError =
        LoggerMessage.Define<string, string>(
            LogLevel.Error,
            new EventId(3, nameof(SyncIndex)),
            "Error indexing document {DocumentId}: {Error}");

    private static readonly Action<ILogger, string, int, Exception?> LogSyncIndexSuccess =
        LoggerMessage.Define<string, int>(
            LogLevel.Information,
            new EventId(4, nameof(SyncIndex)),
            "Successfully synced index '{IndexName}' with {Count} document(s)");

    private static readonly Action<ILogger, string, Exception?> LogCreatingIndex =
        LoggerMessage.Define<string>(
            LogLevel.Information,
            new EventId(5, nameof(CreateIndex)),
            "Creating '{IndexName}' index");

    private static readonly Action<ILogger, string, string, Exception?> LogCreateIndexFailed =
        LoggerMessage.Define<string, string>(
            LogLevel.Error,
            new EventId(6, nameof(CreateIndex)),
            "Failed to create '{IndexName}' index: {Error}");

    private static readonly Action<ILogger, string, Exception?> LogCreateIndexSuccess =
        LoggerMessage.Define<string>(
            LogLevel.Information,
            new EventId(7, nameof(CreateIndex)),
            "Successfully created '{IndexName}' index");

    private static readonly Action<ILogger, string, string, Exception?> LogDeleteIndexFailed =
        LoggerMessage.Define<string, string>(
            LogLevel.Error,
            new EventId(8, nameof(DeleteIndex)),
            "Failed to delete index '{IndexName}': {Error}");

    private static readonly Action<ILogger, string, Exception?> LogDeleteIndexSuccess =
        LoggerMessage.Define<string>(
            LogLevel.Information,
            new EventId(9, nameof(DeleteIndex)),
            "Successfully deleted index '{IndexName}'");

    private static readonly Action<ILogger, Exception> LogHealthCheckFailed =
        LoggerMessage.Define(
            LogLevel.Error,
            new EventId(10, nameof(IsAlive)),
            "Elasticsearch health check failed");

    private static readonly Action<ILogger, string, Exception?> LogElasticsearchQuery =
        LoggerMessage.Define<string>(
            LogLevel.Information,
            new EventId(11, "ElasticsearchQuery"),
            "Elasticsearch Query: {Query}");

    public string DefaultIndex => adminClient.Client.ElasticsearchClientSettings.DefaultIndex;

    public async Task<List<WebPage>> SearchContent(string query)
    {
        var searchResponse = await userClient.Client.SearchAsync<WebPage>(s => s
            .Indices(DefaultIndex)
            .Query(q => q
                .Match(m => m
                    .Field(f => f.Content)
                    .Query(query)
                )
            ).Size(1000)
        );

        LogElasticsearchQuery(logger, searchResponse.DebugInformation, null);
        return searchResponse.Documents.ToList();
    }
    
    public async Task<List<WebPage>> SearchTag(string query)
    {
        var searchResponse = await userClient.Client.SearchAsync<WebPage>(s => s
            .Indices(DefaultIndex)
            .Query(q => q
                .Term(m => m
                    .Field(f => f.Tags)
                    .Value(query)
                )
            ).Size(1000)
        );

        LogElasticsearchQuery(logger, searchResponse.DebugInformation, null);
        return searchResponse.Documents.ToList();
    }

    public async Task<List<WebPage>> SearchTitleAndContentWithBoost(string query)
    {
        var searchResponse = await userClient.Client.SearchAsync<WebPage>(s => s
            .Indices(DefaultIndex)
            .Query(q => q
                .MultiMatch(mm => mm
                    .Query(query)
                    .Fields(
                        new[]
                        {
                            Infer.Field<WebPage>(f => f.Title, 3.0),
                            Infer.Field<WebPage>(f => f.Content)
                        }
                    )
                )
            ).Size(1000)
        );

        LogElasticsearchQuery(logger, searchResponse.DebugInformation, null);
        return searchResponse.Documents.ToList();
    }

    public async Task<List<WebpageWithHighlight>> SearchContentAndTitleWithHighlight(string query)
    {
        var searchResponse = await userClient.Client.SearchAsync<WebPage>(s => s
            .Indices(DefaultIndex)
            .Query(q => q
                .Match(m => m
                    .Field(f => f.Content)
                    .Query(query)
                )
            )
            .Highlight(h => h
                .Fields(new Dictionary<Field, HighlightField>()
                {
                    {
                        Infer.Field<WebPage>(f => f.Content),
                        new HighlightField()
                    },
                    {
                        Infer.Field<WebPage>(f => f.Title),
                        new HighlightField()
                    }
                })
                .PreTags(HighlightPreTags)
                .PostTags(HighlightPostTags)
            )
            .Size(1000)
        );

        LogElasticsearchQuery(logger, searchResponse.DebugInformation, null);

        return searchResponse.Hits
            .Where(hit => hit.Source != null)
            .Select(hit => new WebpageWithHighlight
            {
                Url = hit.Source!.Url,
                Title = hit.Source!.Title,
                Content = hit.Source!.Content,
                Tags = hit.Source!.Tags,
                Views = hit.Source!.Views,
                PublishedDate = hit.Source!.PublishedDate,
                Comments = hit.Source!.Comments,
                Highlights =  hit.Highlight ?? new Dictionary<string, IReadOnlyCollection<string>>()
            }).ToList();
    }

    public async Task<List<WebPage>> SearchContentAndFilterTag(string searchTerm, string tagFilter)
    {
        var searchResponse = await userClient.Client.SearchAsync<WebPage>(s => s
            .Indices(DefaultIndex)
            .Query(q => q
                .Bool(b => b
                    .Must(mq => mq
                        .Match(m => m
                            .Field(f => f.Content)
                            .Query(searchTerm)
                        )
                    )
                    .Filter(fq => fq
                        .Term(t => t
                            .Field(f => f.Tags)
                            .Value(tagFilter)
                        )
                    )
                )
            )
            .Size(1000)
        );

        LogElasticsearchQuery(logger, searchResponse.DebugInformation, null);
        return searchResponse.Documents.ToList();
    }

    public async Task<List<WebPage>> SearchContentSortedByViews(string query, bool descending = true)
    {
        var searchResponse = await userClient.Client.SearchAsync<WebPage>(s => s
            .Indices(DefaultIndex)
            .Query(q => q
                .Match(m => m
                    .Field(f => f.Content)
                    .Query(query)
                )
            )
            .Sort(so => so
                .Field(f => f.Views, 
                    descending ? SortOrder.Desc : SortOrder.Asc
                )
            )
        );

        LogElasticsearchQuery(logger, searchResponse.DebugInformation, null);
        return searchResponse.Documents.ToList();
    }

    public async Task<double> GetAverageViewsOfTag(string tag)
    {
        var search = await userClient.Client.SearchAsync<WebPage>(s => s
            .Indices(DefaultIndex)
            .Query(q => q
                .Term(t => t
                    .Field(f => f.Tags)
                    .Value(tag)
                )
            )
            .Size(0)
            .Aggregations(a => a.Add(
                    "average_views", 
                    descriptor => descriptor
                        .Avg(avg => avg
                            .Field(f => f.Views)
                        )
                    )
            )
        );

        LogElasticsearchQuery(logger, search.DebugInformation, null);
        var avgViewsAgg = search.Aggregations?.GetAverage("average_views")?.Value ?? 0.0;
        return avgViewsAgg;
    }

    public async Task<Dictionary<string, long>> GetMostCommonTags(int size = 10)
    {
        var searchResponse = await userClient.Client.SearchAsync<WebPage>(s => s
            .Indices(DefaultIndex)
            .Size(0)
            .Aggregations(a =>
                a.Add("pages_per_tag", descriptor => descriptor
                    .Terms(t => t
                        .Field(f => f.Tags)
                        .Size(size)
                        .AddOrder(new Field("_count"), SortOrder.Desc)
                    )
                )
            )
        );

        LogElasticsearchQuery(logger, searchResponse.DebugInformation, null);
        var termsAgg = searchResponse.Aggregations?.GetStringTerms("pages_per_tag");
        if (termsAgg == null)
        {
            return new Dictionary<string, long>();
        }
        return termsAgg.Buckets.ToDictionary(
            bucket => bucket.Key.ToString(),
            bucket => bucket.DocCount);
    }

    public async Task<List<WebPage>> FindPagesWithHighlyLikedComments(int minLikes = 5)
    {
        var searchResponse = await userClient.Client.SearchAsync<WebPage>(s => s
            .Indices(DefaultIndex)
            .Query(q => q
                .Nested(n => n
                    .Path(p => p.Comments)
                    .Query(nq => nq
                        .Range(r => r
                            .Number(c => c
                                .Field(f => f.Comments.First().Likes).Gte(minLikes)
                            )
                        )
                    )
                )
            )
            .Size(1000)
        );

        LogElasticsearchQuery(logger, searchResponse.DebugInformation, null);
        return searchResponse.Documents.ToList();
    }

    public async Task<List<WebPage>> GetRecentContent(TimeSpan timeSpan)
    {
        var searchResponse = await userClient.Client.SearchAsync<WebPage>(s => s
            .Indices(DefaultIndex)
            .Query(q => q
                .Range(new DateRangeQuery
                {
                    Field = Infer.Field<WebPage>(f => f.PublishedDate),
                    Gte = DateMath.Now.Subtract(timeSpan)
                })
            )
            .Sort(so => so
                .Field(f => f.PublishedDate, SortOrder.Desc)
            )
            .Size(1000)
        );

        LogElasticsearchQuery(logger, searchResponse.DebugInformation, null);
        return searchResponse.Documents.ToList();
    }
    
    public async Task<bool> IsAlive()
    {
        try
        {
            PingResponse pingResponse = await adminClient.Client.PingAsync();
            return pingResponse.IsValidResponse;
        }
        catch (Exception ex)
        {
            LogHealthCheckFailed(logger, ex);
            return false;
        }
    }
    
    public async Task<bool> RecreateIndex(List<WebPage> webPages)
    {
        if (await IndexExists())
        {
            return await DeleteIndex() && await CreateIndex() && await SyncIndex(webPages);
        }

        return await CreateIndex() && await SyncIndex(webPages);
    }
    
    public async Task<bool> CreateIndexAndSync(List<WebPage> webPages)
    {
        if (!await IndexExists())
        {
            return await CreateIndex() && await SyncIndex(webPages);
        }

        return true;
    }
    
    public async Task<bool> SyncIndex(List<WebPage> webPages)
    {
        if (webPages.Count == 0)
        {
            LogNoDocumentsFound(logger, null);
            return true;
        }

        BulkResponse bulkResponse = await userClient.Client.BulkAsync(b => b
            .Index(DefaultIndex)
            .IndexMany(webPages));

        if (!bulkResponse.IsValidResponse || bulkResponse.Errors)
        {
            LogSyncIndexFailed(
                logger,
                DefaultIndex,
                bulkResponse.DebugInformation,
                null);

            if (!bulkResponse.ItemsWithErrors.Any())
            {
                return false;
            }

            foreach (ResponseItem item in bulkResponse.ItemsWithErrors)
            {
                LogDocumentIndexError(
                    logger,
                    item.Id ?? "Unknown",
                    item.Error?.Reason ?? "Unknown error",
                    null);
            }

            return false;
        }

        LogSyncIndexSuccess(
            logger,
            DefaultIndex,
            webPages.Count,
            null);

        return true;
    }

    private async Task<bool> IndexExists()
    {
        ExistsResponse existsResponse = await adminClient.Client.Indices.ExistsAsync(DefaultIndex);
        return existsResponse.Exists;
    }

    private async Task<bool> CreateIndex()
    {
        LogCreatingIndex(logger, DefaultIndex, null);
        var createIndexResponse = await adminClient.Client.Indices.CreateAsync<WebPage>(DefaultIndex, c => c
            .Settings(s => s
                .Analysis(a => a
                    .Analyzers(an => an
                        .Custom("content_analyzer", ca => ca
                            .Tokenizer("standard")
                            .Filter("lowercase", "stop")
                        )
                    )
                )
            )
            .Mappings(m => m.Dynamic(DynamicMapping.Strict)
                .Properties(new Properties
                {
                    { "title", new TextProperty
                        {
                            Analyzer = "content_analyzer",
                            Fields = new Properties
                            {
                                { "raw", new KeywordProperty() }
                            }
                        }
                    },
                    { "url", new TextProperty() },
                    { "content", new TextProperty { Analyzer = "content_analyzer" } },
                    { "tags", new KeywordProperty() },
                    { "views", new IntegerNumberProperty() },
                    { "publishedDate", new DateProperty() },
                    { "comments", new NestedProperty
                        {
                            Properties = new Properties
                            {
                                { "user", new KeywordProperty() },
                                { "text", new TextProperty { Analyzer = "content_analyzer" } },
                                { "likes", new IntegerNumberProperty() }
                            }
                        }
                    }
                })
            )
        );
        
        if (!createIndexResponse.IsValidResponse)
        {
            LogCreateIndexFailed(
                logger,
                DefaultIndex,
                createIndexResponse.DebugInformation,
                null);
            return false;
        }

        LogCreateIndexSuccess(logger, DefaultIndex, null);
        return true;
    }

    private async Task<bool> DeleteIndex()
    {
        if (!await IndexExists())
        {
            return true;
        }

        DeleteIndexResponse deleteIndexResponse = await adminClient.Client.Indices.DeleteAsync(DefaultIndex);

        if (!deleteIndexResponse.IsValidResponse)
        {
            LogDeleteIndexFailed(
                logger,
                DefaultIndex,
                deleteIndexResponse.DebugInformation,
                null);
            return false;
        }

        LogDeleteIndexSuccess(logger, DefaultIndex, null);
        return true;
    }
}