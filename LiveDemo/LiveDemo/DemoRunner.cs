using System.Text.Json;
using Elasticsearch.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LiveDemo;

public class DemoRunner(IServiceScopeFactory scopeFactory, IHostApplicationLifetime appLifetime) : IHostedService
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        WriteIndented = true
    };
    
    private const int MinBoxWidth = 80;
    private const int Padding = 2;
    private const int BorderChars = 2; 
    
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using IServiceScope scope = scopeFactory.CreateScope();
        ElasticsearchService elasticsearchService = scope.ServiceProvider.GetRequiredService<ElasticsearchService>();
        
        await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
        
        PrintHeader();
        await RunDemo(elasticsearchService);
        appLifetime.StopApplication();
    }
    
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private static void PrintHeader()
    {
        Console.WriteLine();
        Console.WriteLine("".PadRight(MinBoxWidth, '='));
        Console.WriteLine("ELASTICSEARCH DEMO RUNNER".PadLeft((MinBoxWidth + 28) / 2).PadRight(MinBoxWidth));
        Console.WriteLine("".PadRight(MinBoxWidth, '='));
        Console.WriteLine();
    }

    private static void PrintFooter()
    {
        Console.WriteLine();
        Console.WriteLine("".PadRight(MinBoxWidth, '='));
        Console.WriteLine("DEMO COMPLETE".PadLeft((MinBoxWidth + 13) / 2).PadRight(MinBoxWidth));
        Console.WriteLine("".PadRight(MinBoxWidth, '='));
        Console.WriteLine();
    }

    private static async Task RunDemo(ElasticsearchService elasticsearchService)
    {
        // Basic Search Examples
        await RunExample(
            () => elasticsearchService.SearchContent("Kibana"),
            "Basic Content Search",
            "Search for documents containing the term 'Kibana' in the Content field"
        );
        
        await RunExample(
            () => elasticsearchService.SearchTag("search"),
            "Tag-Based Search",
            "Find all documents tagged with 'search' using exact matching on Tags field"
        );
        
        // Advanced Search with Boosting
        await RunExample(
            () => elasticsearchService.SearchTitleAndContentWithBoost("Kibana"),
            "Title-Weighted Search",
            "Search for 'Kibana' in both Title and Content fields, but give Title matches 2x higher importance (boost)"
        );
        
        // Search with Highlighting
        await RunExample(
            () => elasticsearchService.SearchContentAndTitleWithHighlight("AI"),
            "Search with Highlighting",
            "Search for 'AI' in Title and Content fields, returning highlighted snippets showing where matches occur"
        );
        
        // Filtered Search
        await RunExample(
            () => elasticsearchService.SearchContentAndFilterTag("Elasticsearch", "cluster"),
            "Filtered Search",
            "Find documents containing 'Elasticsearch' in Content field, filtered to only include those with 'cluster' tag"
        );
        
        // Sorting Examples
        await RunExample(
            () => elasticsearchService.SearchContentSortedByViews("Kibana"),
            "Sorted Search Results",
            "Search for 'Kibana' in Content field, returning results sorted by Views (descending)"
        );
        
        // Aggregation Examples
        await RunExample(
            () => elasticsearchService.GetAverageViewsOfTag("tutorial"),
            "Aggregation - Average Views",
            "Calculate the average Views count for all documents tagged with 'tutorial'"
        );
        
        await RunExample(
            () => elasticsearchService.GetMostCommonTags(5),
            "Aggregation - Top Tags",
            "Find the 5 most frequently used tags across all documents (tag frequency analysis)"
        );
        
        // Complex Query Examples
        await RunExample(
            () => elasticsearchService.FindPagesWithHighlyLikedComments(),
            "Nested Document Query",
            "Find webpages that contain comments with 4 or more likes, demonstrating nested document queries"
        );
        
        await RunExample(
            () => elasticsearchService.GetRecentContent(TimeSpan.FromDays(1)),
            "Time-Range Query",
            "Retrieve all documents published within the last 24 hours (relative time window search)"
        );
        
        PrintFooter();
    }

    private static async Task RunExample<T>(
        Func<Task<T>> asyncFunction, 
        string exampleName,
        string explanation
    )
    {
        try
        {
            // Calculate box width based on the longest text
            int boxWidth = CalculateBoxWidth(exampleName, explanation);
            
            Console.WriteLine();
            PrintTopBorder(boxWidth);
            PrintBoxLine($"EXAMPLE: {exampleName}", boxWidth);
            PrintMiddleBorder(boxWidth);
            PrintBoxLine(explanation, boxWidth);
            PrintMiddleBorder(boxWidth);
            
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var result = await asyncFunction();
            stopwatch.Stop();
            
            PrintBoxLine($"Execution time: {stopwatch.ElapsedMilliseconds} ms", boxWidth);
            PrintBottomBorder(boxWidth);
            Console.WriteLine();
            
            var json = ConvertToJson(result);
            Console.WriteLine("Result:");
            PrintHorizontalLine(boxWidth);
            Console.WriteLine(json);
            PrintHorizontalLine(boxWidth);
        }
        catch (Exception ex)
        {
            int errorBoxWidth = CalculateErrorBoxWidth(exampleName, ex.Message);
            
            Console.WriteLine();
            PrintErrorTopBorder(errorBoxWidth);
            PrintErrorBoxLine($"ERROR in '{exampleName}'", errorBoxWidth);
            PrintErrorBottomBorder(errorBoxWidth);
            Console.WriteLine();
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine();
            Console.WriteLine("Stack Trace:");
            PrintHorizontalLine(errorBoxWidth);
            Console.WriteLine(ex.StackTrace);
            PrintHorizontalLine(errorBoxWidth);
            Console.WriteLine();
            throw;
        }
    }
    
    private static int CalculateBoxWidth(string exampleName, string explanation)
    {
        int exampleLength = $"EXAMPLE: {exampleName}".Length + Padding + BorderChars;
        int explanationLength = explanation.Length + Padding + BorderChars;
        int timeTextLength = "Execution time: 9999 ms".Length + Padding + BorderChars;
        
        int maxLength = Math.Max(exampleLength, Math.Max(explanationLength, timeTextLength));
        return Math.Max(maxLength, MinBoxWidth);
    }
    
    private static int CalculateErrorBoxWidth(string exampleName, string errorMessage)
    {
        int titleLength = $"ERROR in '{exampleName}'".Length + Padding + BorderChars;
        int messageLength = $"Error: {errorMessage}".Length + Padding + BorderChars;
        
        int maxLength = Math.Max(titleLength, messageLength);
        return Math.Max(maxLength, MinBoxWidth);
    }
    
    private static void PrintTopBorder(int width)
    {
        Console.WriteLine("┌" + new string('─', width - 2) + "┐");
    }
    
    private static void PrintMiddleBorder(int width)
    {
        Console.WriteLine("├" + new string('─', width - 2) + "┤");
    }
    
    private static void PrintBottomBorder(int width)
    {
        Console.WriteLine("└" + new string('─', width - 2) + "┘");
    }
    
    private static void PrintBoxLine(string text, int width)
    {
        string paddedText = $" {text.PadRight(width - 3)}│";
        Console.WriteLine($"│{paddedText}");
    }
    
    private static void PrintErrorTopBorder(int width)
    {
        Console.WriteLine("╔" + new string('═', width - 2) + "╗");
    }
    
    private static void PrintErrorBottomBorder(int width)
    {
        Console.WriteLine("╚" + new string('═', width - 2) + "╝");
    }
    
    private static void PrintErrorBoxLine(string text, int width)
    {
        string paddedText = $" {text.PadRight(width - 3)}║";
        Console.WriteLine($"║{paddedText}");
    }
    
    private static void PrintHorizontalLine(int width)
    {
        Console.WriteLine(new string('─', width));
    }
    
    private static string ConvertToJson<T>(T content)
    {
        return JsonSerializer.Serialize(content, JsonSerializerOptions);
    }
}