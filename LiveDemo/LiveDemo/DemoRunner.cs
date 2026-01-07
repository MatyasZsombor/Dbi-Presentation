using System.Text.Json;
using ElasticSearch.Models;
using Elasticsearch.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LiveDemo;

public class DemoRunner(IServiceScopeFactory scopeFactory) : IHostedService
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        WriteIndented = true
    };
    
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using IServiceScope scope = scopeFactory.CreateScope();
        ElasticsearchService elasticsearchService = scope.ServiceProvider.GetRequiredService<ElasticsearchService>();
        
        await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
        await RunDemo(elasticsearchService);
    }
    
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private static async Task RunDemo(ElasticsearchService elasticsearchService)
    {
        await RunExample(() => elasticsearchService.SearchContent("Kibana"), "Example one");
        await RunExample(() => elasticsearchService.SearchTag("search"), "Example two");
        await RunExample(() => elasticsearchService.SearchTitleAndContentWithBoost("Kibana"), "Example three");
        await RunExample(() => elasticsearchService.SearchContentAndTitleWithHighlight("AI"), "Example four");
        await RunExample(() => elasticsearchService.SearchContentAndFilterTag("Elasticsearch", "cluster"), "Example five");
        await RunExample(() => elasticsearchService.SearchContentSortedByViews("Kibana"), "Example six");
        await RunExample(() => elasticsearchService.GetAverageViewsOfTag("tutorial"), "Example seven");
        await RunExample(() => elasticsearchService.GetMostCommonTags(5), "Example eight");
        await RunExample(() => elasticsearchService.FindPagesWithHighlyLikedComments(), "Example nine");
    }

    private static async Task RunExample<T>(Func<Task<T>> asyncFunction, string exampleName = "")
    {
        try
        {
            var result = await asyncFunction();
            var json = ConvertToJson(result);
            Console.WriteLine($"{exampleName}: {json}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in {exampleName}: {ex.Message}");
            throw;
        }
    }
    
    private static string ConvertToJson<T>(T content)
    {
        return  JsonSerializer.Serialize(content, JsonSerializerOptions);
    }
}