namespace ElasticSearch.Models;

public class WebpageWithHighlight : WebPage
{
    public required IReadOnlyDictionary<string, IReadOnlyCollection<string>> Highlights { get; set; }
}