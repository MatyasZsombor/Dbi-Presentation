namespace ElasticSearch.Models;

public class Comment
{
    public required string User { get; set; }
    public required string Text { get; set; }
    public int Likes { get; set; }
}