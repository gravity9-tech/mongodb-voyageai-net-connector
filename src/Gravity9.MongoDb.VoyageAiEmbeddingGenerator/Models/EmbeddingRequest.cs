namespace Gravity9.MongoDb.VoyageAiEmbeddingGenerator.Models;

using System.Text.Json.Serialization;

/// <summary>
/// Request model for creating embeddings via VoyageAI API.
/// </summary>
public sealed class EmbeddingRequest
{
    /// <summary>
    /// A single text string or a list of text strings to be embedded.
    /// </summary>
    [JsonPropertyName("input")]
    public required object Input { get; init; }

    /// <summary>
    /// The embedding model to use.
    /// Recommended models: voyage-4-large, voyage-4, voyage-4-lite, voyage-code-3, voyage-finance-2, voyage-law-2
    /// </summary>
    [JsonPropertyName("model")]
    public required string Model { get; init; }

    /// <summary>
    /// The type of input text. Use this parameter to optimize embeddings for semantic search and retrieval tasks.
    /// Options: null (default), "query", "document"
    /// </summary>
    [JsonPropertyName("input_type")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? InputType { get; init; }

    /// <summary>
    /// Whether to truncate input texts that exceed the context length.
    /// Default: true
    /// </summary>
    [JsonPropertyName("truncation")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool Truncation { get; init; } = true;

    /// <summary>
    /// The number of dimensions for the output embeddings.
    /// Supported by voyage-4-large, voyage-4, voyage-4-lite, voyage-3-large, voyage-3.5, voyage-3.5-lite, voyage-code-3.
    /// Values: 256, 512, 1024 (default), 2048
    /// </summary>
    [JsonPropertyName("output_dimension")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? OutputDimension { get; init; }

    /// <summary>
    /// The data type for the returned embeddings.
    /// Options: "float" (default), "int8", "uint8", "binary", "ubinary"
    /// </summary>
    [JsonPropertyName("output_dtype")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? OutputDtype { get; init; }

    /// <summary>
    /// The format in which embeddings are encoded in the response.
    /// Options: null (default - arrays), "base64"
    /// </summary>
    [JsonPropertyName("encoding_format")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? EncodingFormat { get; init; }
}
