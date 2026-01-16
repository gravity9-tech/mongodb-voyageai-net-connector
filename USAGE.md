# Gravity9.MongoDb.VoyageAiEmbeddingGenerator - Usage Examples

This document provides comprehensive examples of using the VoyageAI Embedding Generator with MongoDB and Semantic Kernel.

## Table of Contents

1. [Basic Usage](#basic-usage)
2. [Integration with MongoDB Vector Store](#integration-with-mongodb-vector-store)
3. [Dependency Injection](#dependency-injection)
4. [Advanced Configuration](#advanced-configuration)

## Basic Usage

### Simple Embedding Generation

```csharp
using Gravity9.MongoDb.VoyageAiEmbeddingGenerator;

var generator = new VoyageAiEmbeddingGenerator("your-voyage-api-key");

var texts = new[]
{
    "The quick brown fox jumps over the lazy dog.",
    "Machine learning is a subset of artificial intelligence."
};

var embeddings = await generator.GenerateAsync(texts);

foreach (var embedding in embeddings)
{
    Console.WriteLine($"Generated embedding with {embedding.Vector.Length} dimensions");
}
```

### Custom Model Configuration

```csharp
var options = new VoyageAiEmbeddingGeneratorOptions
{
    ApiKey = "your-voyage-api-key",
    Model = "voyage-4",
    InputType = "document",
    OutputDimension = 512
};

var generator = new VoyageAiEmbeddingGenerator(options);
var embeddings = await generator.GenerateAsync(new[] { "Sample text" });
```

## Integration with MongoDB Vector Store

### Complete Example with Data Model

```csharp
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Connectors.MongoDB;
using Gravity9.MongoDb.VoyageAiEmbeddingGenerator;
using MongoDB.Driver;

// Define your data model
public class ProductRecord
{
    [VectorStoreRecordKey]
    public string Id { get; set; } = string.Empty;

    [VectorStoreRecordData]
    public string Name { get; set; } = string.Empty;

    [VectorStoreRecordData]
    public string Description { get; set; } = string.Empty;

    [VectorStoreRecordData]
    public decimal Price { get; set; }

    [VectorStoreRecordVector(1024, DistanceFunction.CosineSimilarity)]
    public ReadOnlyMemory<float>? DescriptionEmbedding { get; set; }
}

// Setup MongoDB and embedding generator
var mongoClient = new MongoClient("mongodb://localhost:27017");
var database = mongoClient.GetDatabase("ecommerce");

var embeddingGenerator = new VoyageAiEmbeddingGenerator(new VoyageAiEmbeddingGeneratorOptions
{
    ApiKey = Environment.GetEnvironmentVariable("VOYAGE_API_KEY")!,
    Model = "voyage-4-large",
    InputType = "document",
    OutputDimension = 1024
});

// Create collection with embedding generator
var collectionOptions = new MongoCollectionOptions
{
    EmbeddingGenerator = embeddingGenerator,
    VectorIndexName = "product_vector_index",
    Definition = VectorStoreRecordDefinition.FromType<ProductRecord>()
};

var collection = new MongoCollection<string, ProductRecord>(
    database,
    "products",
    collectionOptions);

// Ensure collection and indexes exist
await collection.EnsureCollectionExistsAsync();

// Add products - embeddings are generated automatically
var product = new ProductRecord
{
    Id = "prod-001",
    Name = "Wireless Headphones",
    Description = "High-quality Bluetooth wireless headphones with active noise cancellation",
    Price = 299.99m
};

await collection.UpsertAsync(product);

// Perform vector search
var searchQuery = "noise cancelling headphones";
var searchResults = collection.SearchAsync(
    searchQuery,
    top: 5,
    new VectorSearchOptions<ProductRecord>
    {
        VectorProperty = nameof(ProductRecord.DescriptionEmbedding)
    });

await foreach (var result in searchResults)
{
    Console.WriteLine($"Product: {result.Record.Name}");
    Console.WriteLine($"Description: {result.Record.Description}");
    Console.WriteLine($"Similarity Score: {result.Score:F4}");
    Console.WriteLine($"Price: ${result.Record.Price}");
    Console.WriteLine();
}
```

### Semantic Search with Filtering

```csharp
using System.Linq.Expressions;

// Search with price filter
Expression<Func<ProductRecord, bool>> filter = p => p.Price < 500;

var searchResults = collection.SearchAsync(
    "affordable headphones",
    top: 10,
    new VectorSearchOptions<ProductRecord>
    {
        VectorProperty = nameof(ProductRecord.DescriptionEmbedding),
        Filter = filter
    });

await foreach (var result in searchResults)
{
    Console.WriteLine($"{result.Record.Name} - ${result.Record.Price}");
}
```

## Dependency Injection

### ASP.NET Core Integration

```csharp
using Gravity9.MongoDb.VoyageAiEmbeddingGenerator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

// Register MongoDB
builder.Services.AddSingleton<IMongoClient>(sp =>
    new MongoClient(builder.Configuration.GetConnectionString("MongoDB")));

builder.Services.AddSingleton(sp =>
    sp.GetRequiredService<IMongoClient>().GetDatabase("myapp"));

// Register VoyageAI embedding generator
builder.Services.AddVoyageAiEmbeddingGenerator(
    apiKey: builder.Configuration["VoyageAI:ApiKey"]!,
    configureOptions: options =>
    {
        options.Model = "voyage-4-large";
        options.InputType = "document";
        options.OutputDimension = 1024;
        options.MaxRetries = 5;
    });

var app = builder.Build();

// Use in a minimal API endpoint
app.MapPost("/products/search", async (
    [FromBody] SearchRequest request,
    [FromServices] IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
    [FromServices] IMongoDatabase database) =>
{
    var collection = new MongoCollection<string, ProductRecord>(
        database,
        "products",
        new MongoCollectionOptions
        {
            EmbeddingGenerator = embeddingGenerator,
            VectorIndexName = "product_vector_index"
        });

    var results = new List<ProductSearchResult>();

    var searchResults = collection.SearchAsync(
        request.Query,
        top: request.Limit,
        new VectorSearchOptions<ProductRecord>
        {
            VectorProperty = nameof(ProductRecord.DescriptionEmbedding)
        });

    await foreach (var result in searchResults)
    {
        results.Add(new ProductSearchResult
        {
            Id = result.Record.Id,
            Name = result.Record.Name,
            Description = result.Record.Description,
            Price = result.Record.Price,
            RelevanceScore = result.Score
        });
    }

    return Results.Ok(results);
});

app.Run();

public record SearchRequest(string Query, int Limit = 10);

public record ProductSearchResult
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public float RelevanceScore { get; init; }
}
```

### Using in a Service Class

```csharp
public class ProductSearchService
{
    private readonly MongoCollection<string, ProductRecord> _collection;

    public ProductSearchService(
        IMongoDatabase database,
        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator)
    {
        var collectionOptions = new MongoCollectionOptions
        {
            EmbeddingGenerator = embeddingGenerator,
            VectorIndexName = "product_vector_index",
            Definition = VectorStoreRecordDefinition.FromType<ProductRecord>()
        };

        _collection = new MongoCollection<string, ProductRecord>(
            database,
            "products",
            collectionOptions);
    }

    public async Task<List<ProductRecord>> SearchProductsAsync(
        string query,
        int limit = 10,
        CancellationToken cancellationToken = default)
    {
        var results = new List<ProductRecord>();

        var searchResults = _collection.SearchAsync(
            query,
            top: limit,
            cancellationToken: cancellationToken);

        await foreach (var result in searchResults.WithCancellation(cancellationToken))
        {
            results.Add(result.Record);
        }

        return results;
    }

    public async Task AddProductAsync(
        ProductRecord product,
        CancellationToken cancellationToken = default)
    {
        await _collection.UpsertAsync(product, cancellationToken);
    }
}

// Register in DI
services.AddScoped<ProductSearchService>();
```

## Advanced Configuration

### Multiple Embedding Generators

```csharp
// For queries
services.AddKeyedSingleton<IEmbeddingGenerator<string, Embedding<float>>>(
    "query-embedder",
    (sp, key) => new VoyageAiEmbeddingGenerator(new VoyageAiEmbeddingGeneratorOptions
    {
        ApiKey = sp.GetRequiredService<IConfiguration>()["VoyageAI:ApiKey"]!,
        Model = "voyage-4-large",
        InputType = "query",
        OutputDimension = 1024
    }));

// For documents
services.AddKeyedSingleton<IEmbeddingGenerator<string, Embedding<float>>>(
    "document-embedder",
    (sp, key) => new VoyageAiEmbeddingGenerator(new VoyageAiEmbeddingGeneratorOptions
    {
        ApiKey = sp.GetRequiredService<IConfiguration>()["VoyageAI:ApiKey"]!,
        Model = "voyage-4-large",
        InputType = "document",
        OutputDimension = 1024
    }));

// Use in services
public class SmartSearchService
{
    private readonly IEmbeddingGenerator<string, Embedding<float>> _queryEmbedder;
    private readonly IEmbeddingGenerator<string, Embedding<float>> _documentEmbedder;

    public SmartSearchService(
        [FromKeyedServices("query-embedder")] IEmbeddingGenerator<string, Embedding<float>> queryEmbedder,
        [FromKeyedServices("document-embedder")] IEmbeddingGenerator<string, Embedding<float>> documentEmbedder)
    {
        _queryEmbedder = queryEmbedder;
        _documentEmbedder = documentEmbedder;
    }
}
```

### Error Handling

```csharp
try
{
    var embeddings = await generator.GenerateAsync(new[] { "Sample text" });
}
catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
{
    // Rate limit exceeded - already retried automatically
    Console.WriteLine("Rate limit exceeded. Please wait and try again.");
}
catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
{
    // Invalid API key
    Console.WriteLine("Invalid API key. Please check your configuration.");
}
catch (HttpRequestException ex)
{
    // Other API errors
    Console.WriteLine($"API error: {ex.Message}");
}
catch (Exception ex)
{
    Console.WriteLine($"Unexpected error: {ex.Message}");
}
```

### Batch Processing with Progress Tracking

```csharp
public async Task ProcessLargeDatasetAsync(
    List<ProductRecord> products,
    IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
    MongoCollection<string, ProductRecord> collection,
    IProgress<double> progress)
{
    var batchSize = 100;
    var totalBatches = (int)Math.Ceiling(products.Count / (double)batchSize);

    for (int i = 0; i < totalBatches; i++)
    {
        var batch = products.Skip(i * batchSize).Take(batchSize).ToList();

        // Process batch
        await collection.UpsertAsync(batch);

        // Report progress
        var progressPercentage = ((i + 1) / (double)totalBatches) * 100;
        progress.Report(progressPercentage);

        Console.WriteLine($"Processed {i + 1}/{totalBatches} batches ({progressPercentage:F1}%)");
    }
}
```

## Best Practices

1. **Reuse HttpClient**: When creating VoyageAiEmbeddingGenerator instances directly, reuse HttpClient instances to avoid socket exhaustion.

2. **Use Dependency Injection**: Register the embedding generator in your DI container for better lifecycle management.

3. **Configure Retry Logic**: Adjust `MaxRetries` and `RetryDelayMilliseconds` based on your application's tolerance for latency.

4. **Monitor Token Usage**: Track the `Usage` property in responses to monitor your API consumption.

5. **Choose Appropriate Models**: Use `voyage-4-lite` for faster responses or `voyage-4-large` for better accuracy.

6. **Set Input Type**: Always set `InputType` to "query" or "document" for better semantic search results.

7. **Handle Errors Gracefully**: Implement proper error handling for rate limits and transient failures.

8. **Batch Operations**: When processing multiple items, batch them together to reduce API calls.
