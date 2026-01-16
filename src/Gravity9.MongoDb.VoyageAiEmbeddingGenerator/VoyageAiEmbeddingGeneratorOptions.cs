namespace Gravity9.MongoDb.VoyageAiEmbeddingGenerator;

/// <summary>
/// Configuration options for VoyageAI Embedding Generator.
/// </summary>
public sealed class VoyageAiEmbeddingGeneratorOptions
{
    /// <summary>
    /// The VoyageAI API key for authentication.
    /// Required for API calls.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// The base URL for the VoyageAI API.
    /// Default: https://ai.mongodb.com/v1/ (for MongoDB Atlas AI API)
    /// Alternative: https://api.voyageai.com/v1/ (for direct VoyageAI API keys)
    /// </summary>
    public string BaseUrl { get; set; } = "https://ai.mongodb.com/v1/";

    /// <summary>
    /// The embedding model to use.
    /// Default: voyage-4-large
    /// 
    /// Available models (via MongoDB Atlas):
    /// - voyage-4-large: Latest large model with best performance
    /// - voyage-4: Latest standard model
    /// - voyage-3.5: Previous generation standard model
    /// - voyage-4-lite: Faster, lightweight model
    /// - voyage-3-large: Previous generation large model
    /// - voyage-code-3: Optimized for code
    /// - voyage-finance-2: Optimized for financial documents
    /// - voyage-law-2: Optimized for legal documents
    /// 
    /// Available models (via direct VoyageAI API):
    /// - voyage-3, voyage-3-lite, voyage-code-3, voyage-finance-2, voyage-law-2
    /// </summary>
    public string Model { get; set; } = "voyage-4-large";

    /// <summary>
    /// The type of input text for optimization.
    /// Options: null (default), "query" (for search queries), "document" (for documents to be searched)
    /// </summary>
    public string? InputType { get; set; }

    /// <summary>
    /// Whether to truncate input texts that exceed the context length.
    /// Default: true
    /// </summary>
    public bool Truncation { get; set; } = true;

    /// <summary>
    /// The number of dimensions for the output embeddings.
    /// Supported by some models: 256, 512, 1024 (default), 2048
    /// Set to null to use the model's default dimension.
    /// </summary>
    public int? OutputDimension { get; set; }

    /// <summary>
    /// The data type for the returned embeddings.
    /// Options: "float" (default), "int8", "uint8", "binary", "ubinary"
    /// Default: float
    /// </summary>
    public string OutputDtype { get; set; } = "float";

    /// <summary>
    /// The format in which embeddings are encoded in the response.
    /// Options: null (default - arrays), "base64"
    /// </summary>
    public string? EncodingFormat { get; set; }

    /// <summary>
    /// Request timeout in seconds.
    /// Default: 30 seconds
    /// </summary>
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Maximum number of retries for transient failures.
    /// Default: 3
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Delay between retries in milliseconds.
    /// Default: 1000 (1 second)
    /// </summary>
    public int RetryDelayMilliseconds { get; set; } = 1000;
}
