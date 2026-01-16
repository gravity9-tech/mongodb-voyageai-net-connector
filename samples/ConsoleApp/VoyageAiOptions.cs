namespace ConsoleApp;

/// <summary>
/// Configuration options for VoyageAI settings.
/// </summary>
public sealed class VoyageAiOptions
{
    /// <summary>
    /// The VoyageAI API key for authentication.
    /// </summary>
    public required string ApiKey { get; set; }

    /// <summary>
    /// The base URL for the VoyageAI API.
    /// Default: https://ai.mongodb.com/v1 (for MongoDB Atlas AI API)
    /// Alternative: https://api.voyageai.com/v1 (for direct VoyageAI API keys)
    /// </summary>
    public required string BaseUrl { get; set; } = "https://ai.mongodb.com/v1/";

    /// <summary>
    /// The embedding model to use.
    /// Default: voyage-4-large
    /// </summary>
    public string Model { get; set; } = "voyage-4-large";

    /// <summary>
    /// The type of input text for optimization.
    /// Options: null (default), "query" (for search queries), "document" (for documents to be searched)
    /// </summary>
    public string? InputType { get; set; }

    /// <summary>
    /// The number of dimensions for the output embeddings.
    /// Supported by some models: 256, 512, 1024 (default), 2048
    /// </summary>
    public int? OutputDimension { get; set; } = 1024;

    /// <summary>
    /// Maximum number of retries for transient failures.
    /// Default: 5
    /// </summary>
    public int MaxRetries { get; set; } = 5;

    /// <summary>
    /// Delay between retries in milliseconds.
    /// Default: 1000 (1 second)
    /// </summary>
    public int RetryDelayMilliseconds { get; set; } = 1000;
}
