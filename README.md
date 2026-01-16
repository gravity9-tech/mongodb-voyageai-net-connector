# Gravity9.MongoDb.VoyageAiEmbeddingGenerator

A .NET 10 library that implements the `IEmbeddingGenerator` abstraction from [Microsoft.Extensions.AI](https://www.nuget.org/packages/Microsoft.Extensions.AI.Abstractions/) to generate embeddings using the VoyageAI/MongoDB Atlas Embedding API.

This library is designed to work seamlessly with [Semantic Kernel](https://learn.microsoft.com/en-us/semantic-kernel/) MongoDB vector store implementations, providing easy integration with VoyageAI's state-of-the-art embedding models.

## Features

- ✅ Full implementation of `IEmbeddingGenerator<string, Embedding<float>>`
- ✅ Support for all VoyageAI embedding models (voyage-4-large, voyage-4, voyage-4-lite, voyage-code-3, etc.)
- ✅ Automatic retry logic with configurable delays
- ✅ Comprehensive error handling
- ✅ Dependency injection support via `IServiceCollection` extensions
- ✅ Compatible with Semantic Kernel MongoDB vector store
- ✅ Configurable model parameters (dimensions, truncation, input type)
- ✅ Thread-safe and async-first design

## Installation

```bash
dotnet add package Gravity9.MongoDb.VoyageAiEmbeddingGenerator
```

## Quick Start

### Basic Usage

```csharp
using Gravity9.MongoDb.VoyageAiEmbeddingGenerator;
using Microsoft.Extensions.AI;

// Create the embedding generator
var generator = new VoyageAiEmbeddingGenerator("your-voyage-api-key");

// Generate embeddings for text
var embeddings = await generator.GenerateAsync(new[] { "Hello, world!", "Semantic Kernel is awesome!" });

foreach (var embedding in embeddings)
{
    Console.WriteLine($"Generated embedding with {embedding.Vector.Length} dimensions");
}
```

### Using with Dependency Injection

```csharp
using Gravity9.MongoDb.VoyageAiEmbeddingGenerator;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

// Register the embedding generator
services.AddVoyageAiEmbeddingGenerator(
    apiKey: "your-voyage-api-key",
    configureOptions: options =>
    {
        options.Model = "voyage-4-large";
        options.InputType = "document";
        options.OutputDimension = 1024;
    });

var serviceProvider = services.BuildServiceProvider();

// Resolve and use the generator
var generator = serviceProvider.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();
var embeddings = await generator.GenerateAsync(new[] { "Document text to embed" });
```

### Integration with Semantic Kernel MongoDB Vector Store

```csharp
using Microsoft.SemanticKernel.Connectors.MongoDB;
using Microsoft.Extensions.VectorData;
using Gravity9.MongoDb.VoyageAiEmbeddingGenerator;
using MongoDB.Driver;

// Create the embedding generator
var embeddingGenerator = new VoyageAiEmbeddingGenerator(new VoyageAiEmbeddingGeneratorOptions
{
    ApiKey = "your-voyage-api-key",
    Model = "voyage-4-large",
    InputType = "document"
});

// Create MongoDB client and database
var mongoClient = new MongoClient("mongodb://localhost:27017");
var database = mongoClient.GetDatabase("vectordb");

// Create the collection with embedding generator
var collectionOptions = new MongoCollectionOptions
{
    EmbeddingGenerator = embeddingGenerator,
    VectorIndexName = "vector_index",
    Definition = VectorStoreRecordDefinition.FromType<MyDataModel>()
};

var collection = new MongoCollection<string, MyDataModel>(
    database,
    "my_collection",
    collectionOptions);

// Ensure collection and indexes exist
await collection.EnsureCollectionExistsAsync();

// Add records - embeddings will be generated automatically
var record = new MyDataModel
{
    Id = "1",
    Text = "This text will be automatically embedded",
    Description = "Additional metadata"
};

await collection.UpsertAsync(record);

// Perform vector search
var searchResults = collection.SearchAsync(
    "search query",
    top: 10,
    new VectorSearchOptions<MyDataModel>
    {
        VectorProperty = nameof(MyDataModel.Embedding)
    });

await foreach (var result in searchResults)
{
    Console.WriteLine($"Found: {result.Record.Text}, Score: {result.Score}");
}
```

## Configuration Options

The `VoyageAiEmbeddingGeneratorOptions` class provides comprehensive configuration:

```csharp
var options = new VoyageAiEmbeddingGeneratorOptions
{
    // Required: Your VoyageAI API key
    ApiKey = "your-voyage-api-key",

    // Optional: Base URL for the API (default: https://ai.mongodb.com/v1)
    BaseUrl = "https://ai.mongodb.com/v1",

    // Optional: Model to use (default: voyage-4-large)
    Model = "voyage-4-large",

    // Optional: Input type for optimization (null, "query", or "document")
    InputType = "document",

    // Optional: Enable truncation for long texts (default: true)
    Truncation = true,

    // Optional: Output dimensions (256, 512, 1024, or 2048)
    OutputDimension = 1024,

    // Optional: Output data type (default: "float")
    OutputDtype = "float",

    // Optional: Encoding format (null or "base64")
    EncodingFormat = null,

    // Optional: Request timeout (default: 30 seconds)
    RequestTimeout = TimeSpan.FromSeconds(30),

    // Optional: Max retries for transient failures (default: 3)
    MaxRetries = 3,

    // Optional: Delay between retries in milliseconds (default: 1000)
    RetryDelayMilliseconds = 1000
};

var generator = new VoyageAiEmbeddingGenerator(options);
```

## Supported VoyageAI Models

The library supports all VoyageAI embedding models available through MongoDB Atlas:

- **voyage-4-large** (Recommended) - Highest quality, 1024 dimensions default
- **voyage-4** - Balanced quality and performance
- **voyage-4-lite** - Fast and efficient
- **voyage-3-large** - Previous generation, high quality
- **voyage-3.5** - Enhanced version of v3
- **voyage-3.5-lite** - Lightweight v3.5
- **voyage-code-3** - Specialized for code embeddings
- **voyage-finance-2** - Optimized for financial documents
- **voyage-law-2** - Optimized for legal documents
- **voyage-2** - Legacy model
- **voyage-multimodal-3** - Multimodal support (images + text)

## Advanced Features

### Batch Processing

The library automatically handles batch processing and respects API limits:

```csharp
var generator = new VoyageAiEmbeddingGenerator("your-api-key");

var largeBatch = Enumerable.Range(0, 500)
    .Select(i => $"Document {i}")
    .ToList();

var embeddings = await generator.GenerateAsync(largeBatch);
// Returns 500 embeddings
```

### Error Handling

The library includes comprehensive error handling with automatic retries for transient failures:

```csharp
try
{
    var embeddings = await generator.GenerateAsync(new[] { "Text to embed" });
}
catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
{
    // Rate limit exceeded - already retried automatically
    Console.WriteLine("Rate limit exceeded after retries");
}
catch (HttpRequestException ex)
{
    // Other API errors
    Console.WriteLine($"API error: {ex.Message}");
}
```

### Usage Tracking

Access token usage information from the response:

```csharp
var embeddings = await generator.GenerateAsync(new[] { "Sample text" });

if (embeddings.Usage != null)
{
    Console.WriteLine($"Tokens used: {embeddings.Usage.InputTokenCount}");
    Console.WriteLine($"Total tokens: {embeddings.Usage.TotalTokenCount}");
}
```

## API Reference

### VoyageAiEmbeddingGenerator

Main class implementing `IEmbeddingGenerator<string, Embedding<float>>`.

#### Constructors

- `VoyageAiEmbeddingGenerator(string apiKey)` - Simple constructor with API key
- `VoyageAiEmbeddingGenerator(VoyageAiEmbeddingGeneratorOptions options)` - Constructor with full options
- `VoyageAiEmbeddingGenerator(IVoyageAiApiClient apiClient, VoyageAiEmbeddingGeneratorOptions options)` - For advanced scenarios with custom API client

#### Methods

- `Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(IEnumerable<string> values, EmbeddingGenerationOptions? options = null, CancellationToken cancellationToken = default)` - Generate embeddings for the provided text inputs

### ServiceCollectionExtensions

Extension methods for dependency injection.

- `AddVoyageAiEmbeddingGenerator(string apiKey, Action<VoyageAiEmbeddingGeneratorOptions>? configureOptions = null)` - Register with DI container
- `AddVoyageAiEmbeddingGenerator(VoyageAiEmbeddingGeneratorOptions options)` - Register with pre-configured options

## Requirements

- .NET 10.0 or later
- MongoDB.Driver (for MongoDB integration)
- Microsoft.Extensions.AI.Abstractions
- Valid VoyageAI API key (obtain from MongoDB Atlas)

## Getting a VoyageAI API Key

1. Sign in to [MongoDB Atlas](https://www.mongodb.com/cloud/atlas)
2. Navigate to the **Model API Keys** section
3. Create a new API key for VoyageAI embedding services
4. Copy the API key and use it in your application

For more information, see [MongoDB's documentation on Model API Keys](http://dochub.mongodb.org/core/voyage-api-keys).

## License

MIT

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## Support

For issues, questions, or suggestions, please open an issue on GitHub.

## Related Projects

- [Semantic Kernel](https://github.com/microsoft/semantic-kernel) - Microsoft's AI orchestration framework
- [Microsoft.Extensions.AI](https://www.nuget.org/packages/Microsoft.Extensions.AI.Abstractions/) - AI abstraction interfaces
- [MongoDB .NET Driver](https://github.com/mongodb/mongo-csharp-driver) - Official MongoDB driver for .NET

## Acknowledgments

This library is built on top of the VoyageAI embedding models provided through MongoDB Atlas and follows the abstractions defined by Microsoft.Extensions.AI.
