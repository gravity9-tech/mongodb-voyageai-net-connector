namespace Gravity9.MongoDb.VoyageAiEmbeddingGenerator.Models;

using System.Text.Json.Serialization;

/// <summary>
/// Error response from VoyageAI API.
/// </summary>
public sealed class ErrorResponse
{
    /// <summary>
    /// The error detail message.
    /// </summary>
    [JsonPropertyName("detail")]
    public string? Detail { get; init; }
}
