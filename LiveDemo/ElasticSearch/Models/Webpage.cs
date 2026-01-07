namespace ElasticSearch.Models;

public class WebPage
{
    public required string Url { get; set; }
    public required string Title { get; set; }
    public required string Content { get; set; }
    public required List<string> Tags { get; set; }
    public int Views { get; set; }
    public DateTime PublishedDate { get; set; }
    public required List<Comment> Comments { get; set; }
}