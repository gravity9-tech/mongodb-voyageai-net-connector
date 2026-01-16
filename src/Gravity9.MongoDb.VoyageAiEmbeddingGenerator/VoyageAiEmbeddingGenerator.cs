namespace Gravity9.MongoDb.VoyageAiEmbeddingGenerator;

using System.Text.Json;

using Gravity9.MongoDb.VoyageAiEmbeddingGenerator.Models;
using Gravity9.MongoDb.VoyageAiEmbeddingGenerator.Services;

using Microsoft.Extensions.AI;

/// <summary>
/// Implementation of <see cref="IEmbeddingGenerator{TInput, TEmbedding}"/> using the VoyageAI/MongoDB Atlas Embedding API.
/// This class generates embeddings for text inputs using the VoyageAI models accessible via MongoDB Atlas.
/// </summary>
public sealed class VoyageAiEmbeddingGenerator : IEmbeddingGenerator<string, Embedding<float>>
{
    private readonly IVoyageAiApiClient _apiClient;
    private readonly VoyageAiEmbeddingGeneratorOptions _options;
    private readonly EmbeddingGeneratorMetadata _metadata;

    /// <summary>
    /// Initializes a new instance of the <see cref="VoyageAiEmbeddingGenerator"/> class.
    /// </summary>
    /// <param name="apiClient">The VoyageAI API client.</param>
    /// <param name="options">Configuration options.</param>
    public VoyageAiEmbeddingGenerator(IVoyageAiApiClient apiClient, VoyageAiEmbeddingGeneratorOptions options)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _options = options ?? throw new ArgumentNullException(nameof(options));

        _metadata = new EmbeddingGeneratorMetadata();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="VoyageAiEmbeddingGenerator"/> class.
    /// Convenience constructor that creates the API client internally.
    /// </summary>
    /// <param name="options">Configuration options.</param>
    public VoyageAiEmbeddingGenerator(VoyageAiEmbeddingGeneratorOptions options)
        : this(CreateApiClient(options), options)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="VoyageAiEmbeddingGenerator"/> class.
    /// Convenience constructor using default options with only the API key.
    /// </summary>
    /// <param name="apiKey">The VoyageAI API key.</param>
    public VoyageAiEmbeddingGenerator(string apiKey)
        : this(new VoyageAiEmbeddingGeneratorOptions { ApiKey = apiKey })
    {
    }

    /// <inheritdoc/>
    public EmbeddingGeneratorMetadata Metadata => _metadata;

    /// <inheritdoc/>
    public async Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
        IEnumerable<string> values,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(values);

        var inputList = values.ToList();
        if (inputList.Count == 0)
        {
            return new GeneratedEmbeddings<Embedding<float>>([]);
        }

        // Validate inputs
        foreach (var value in inputList)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentException("Input values cannot be null or empty.", nameof(values));
            }
        }

        // Create the API request
        var request = new EmbeddingRequest
        {
            Input = inputList.Count == 1 ? inputList[0] : inputList,
            Model = _options.Model,
            InputType = _options.InputType,
            Truncation = _options.Truncation,
            OutputDimension = _options.OutputDimension,
            OutputDtype = _options.OutputDtype,
            EncodingFormat = _options.EncodingFormat
        };

        // Call the API
        var response = await _apiClient.CreateEmbeddingsAsync(request, cancellationToken).ConfigureAwait(false);

        // Convert response to Embedding<float>[]
        var embeddings = new List<Embedding<float>>(response.Data.Count);

        foreach (var embeddingData in response.Data.OrderBy(e => e.Index))
        {
            var vector = ConvertToFloatArray(embeddingData.Embedding);
            embeddings.Add(new Embedding<float>(vector));
        }

        // Create usage information
        var usage = new UsageDetails
        {
            InputTokenCount = response.Usage.TotalTokens,
            TotalTokenCount = response.Usage.TotalTokens
        };

        return new GeneratedEmbeddings<Embedding<float>>(embeddings)
        {
            Usage = usage
        };
    }

    /// <inheritdoc/>
    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        return serviceKey is null && serviceType == typeof(VoyageAiEmbeddingGeneratorOptions)
            ? _options
            : null;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        // No resources to dispose
    }

    /// <summary>
    /// Converts the embedding data from the API response to a float array.
    /// </summary>
    private static float[] ConvertToFloatArray(object embeddingData)
    {
        // Handle different response formats based on encoding_format
        if (embeddingData is JsonElement jsonElement)
        {
            if (jsonElement.ValueKind == JsonValueKind.Array)
            {
                var list = new List<float>();
                foreach (var element in jsonElement.EnumerateArray())
                {
                    if (element.ValueKind == JsonValueKind.Number)
                    {
                        list.Add(element.GetSingle());
                    }
                    else
                    {
                        throw new InvalidOperationException($"Unexpected element type in embedding array: {element.ValueKind}");
                    }
                }
                return list.ToArray();
            }
            else if (jsonElement.ValueKind == JsonValueKind.String)
            {
                // Base64 encoded format
                var base64 = jsonElement.GetString();
                if (string.IsNullOrEmpty(base64))
                {
                    throw new InvalidOperationException("Base64 embedding string is null or empty.");
                }

                // Decode base64 to float array
                var bytes = Convert.FromBase64String(base64);
                var floats = new float[bytes.Length / sizeof(float)];
                Buffer.BlockCopy(bytes, 0, floats, 0, bytes.Length);
                return floats;
            }
        }

        throw new InvalidOperationException($"Unsupported embedding data type: {embeddingData.GetType()}");
    }

    /// <summary>
    /// Creates an API client with the given options.
    /// </summary>
    private static IVoyageAiApiClient CreateApiClient(VoyageAiEmbeddingGeneratorOptions options)
    {
        var httpClient = new HttpClient();
        return new VoyageAiApiClient(httpClient, options);
    }
}
