namespace Gravity9.MongoDb.VoyageAiEmbeddingGenerator.Models;

using System.Text.Json.Serialization;

/// <summary>
/// Response model for embedding creation from VoyageAI API.
/// </summary>
public sealed class EmbeddingResponse
{
    /// <summary>
    /// The object type. Always returns "list".
    /// </summary>
    [JsonPropertyName("object")]
    public required string Object { get; init; }

    /// <summary>
    /// An array of embedding objects, one for each input text.
    /// </summary>
    [JsonPropertyName("data")]
    public required List<EmbeddingData> Data { get; init; }

    /// <summary>
    /// The name of the model used to generate the embeddings.
    /// </summary>
    [JsonPropertyName("model")]
    public required string Model { get; init; }

    /// <summary>
    /// Usage information for the request.
    /// </summary>
    [JsonPropertyName("usage")]
    public required UsageInfo Usage { get; init; }
}

/// <summary>
/// Represents a single embedding result.
/// </summary>
public sealed class EmbeddingData
{
    /// <summary>
    /// The object type. Always returns "embedding".
    /// </summary>
    [JsonPropertyName("object")]
    public required string Object { get; init; }

    /// <summary>
    /// The embedding vector as an array of floats or a base64-encoded string.
    /// </summary>
    [JsonPropertyName("embedding")]
    public required object Embedding { get; init; }

    /// <summary>
    /// The index of this embedding in the input list.
    /// </summary>
    [JsonPropertyName("index")]
    public required int Index { get; init; }
}

/// <summary>
/// Token usage information for the embedding request.
/// </summary>
public sealed class UsageInfo
{
    /// <summary>
    /// The total number of tokens processed across all input texts.
    /// </summary>
    [JsonPropertyName("total_tokens")]
    public required int TotalTokens { get; init; }
}
