namespace Gravity9.MongoDb.VoyageAiEmbeddingGenerator.Services;

using System.Net.Http.Json;
using System.Text.Json;

using Gravity9.MongoDb.VoyageAiEmbeddingGenerator.Models;

/// <summary>
/// Service for interacting with the VoyageAI API.
/// </summary>
public interface IVoyageAiApiClient
{
    /// <summary>
    /// Creates embeddings for the provided input texts.
    /// </summary>
    /// <param name="request">The embedding request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The embedding response containing the generated embeddings.</returns>
    Task<EmbeddingResponse> CreateEmbeddingsAsync(EmbeddingRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation of VoyageAI API client using HttpClient.
/// </summary>
internal sealed class VoyageAiApiClient : IVoyageAiApiClient
{
    private readonly HttpClient _httpClient;
    private readonly VoyageAiEmbeddingGeneratorOptions _options;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="VoyageAiApiClient"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client to use for API calls.</param>
    /// <param name="options">Configuration options.</param>
    public VoyageAiApiClient(HttpClient httpClient, VoyageAiEmbeddingGeneratorOptions options)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options ?? throw new ArgumentNullException(nameof(options));

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException("VoyageAI API key is required. Please set VoyageAiEmbeddingGeneratorOptions.ApiKey.");
        }

        // HttpClient is already configured during service registration
        // No need to set BaseAddress, Authorization header, or Timeout here

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
    }

    /// <inheritdoc/>
    public async Task<EmbeddingResponse> CreateEmbeddingsAsync(EmbeddingRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var retryCount = 0;
        Exception? lastException = null;

        while (retryCount <= _options.MaxRetries)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("embeddings", request, _jsonOptions, cancellationToken)
                    .ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<EmbeddingResponse>(_jsonOptions, cancellationToken)
                        .ConfigureAwait(false);

                    if (result == null)
                    {
                        throw new InvalidOperationException("Failed to deserialize embedding response.");
                    }

                    return result;
                }

                // Try to read error response
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                ErrorResponse? errorResponse = null;

                try
                {
                    errorResponse = JsonSerializer.Deserialize<ErrorResponse>(errorContent, _jsonOptions);
                }
                catch
                {
                    // Ignore deserialization errors for error response
                }

                var errorMessage = errorResponse?.Detail ?? errorContent ?? response.ReasonPhrase ?? "Unknown error";

                // Check if we should retry based on status code
                var shouldRetry = response.StatusCode switch
                {
                    System.Net.HttpStatusCode.TooManyRequests => true, // 429
                    System.Net.HttpStatusCode.InternalServerError => true, // 500
                    System.Net.HttpStatusCode.BadGateway => true, // 502
                    System.Net.HttpStatusCode.ServiceUnavailable => true, // 503
                    System.Net.HttpStatusCode.GatewayTimeout => true, // 504
                    _ => false
                };

                if (!shouldRetry || retryCount >= _options.MaxRetries)
                {
                    var requestUri = response.RequestMessage?.RequestUri?.ToString() ?? "Unknown URL";
                    throw new HttpRequestException(
                        $"VoyageAI API request failed with status code {response.StatusCode}: {errorMessage}. Request URL: {requestUri}",
                        null,
                        response.StatusCode);
                }

                lastException = new HttpRequestException(errorMessage, null, response.StatusCode);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                lastException = ex;

                if (retryCount >= _options.MaxRetries)
                {
                    throw;
                }
            }

            // Wait before retry
            retryCount++;
            if (retryCount <= _options.MaxRetries)
            {
                await Task.Delay(_options.RetryDelayMilliseconds * retryCount, cancellationToken).ConfigureAwait(false);
            }
        }

        // Should never reach here, but just in case
        throw lastException ?? new InvalidOperationException("Unexpected error in CreateEmbeddingsAsync");
    }
}
