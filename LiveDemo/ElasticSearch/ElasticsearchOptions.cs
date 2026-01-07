namespace ElasticSearch;

public class ElasticsearchOptions
{
    public string ConnectionString { get; set; } = string.Empty;
    public string OnStartUp { get; set; } = string.Empty;
    public string IndexName { get; set; } = string.Empty;
    public bool VerboseLogging { get; set; }

    public ElasticsearchUser Admin { get; set; } = new();
    public ElasticsearchUser User { get; set; } = new();
}

public class ElasticsearchUser
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}