# Usage Guide

This guide provides detailed instructions and best practices for using the Gravity9.MongoDb.VoyageAiEmbeddingGenerator library.

## Table of Contents

- [Installation](#installation)
- [Basic Usage](#basic-usage)
- [Dependency Injection](#dependency-injection)
- [MongoDB Vector Store Integration](#mongodb-vector-store-integration)
- [Configuration](#configuration)
- [Advanced Scenarios](#advanced-scenarios)
- [Best Practices](#best-practices)

## Installation

```bash
dotnet add package Gravity9.MongoDb.VoyageAiEmbeddingGenerator
```

## Basic Usage

### Simple Embedding Generation

```csharp
using Gravity9.MongoDb.VoyageAiEmbeddingGenerator;

// Create generator with just an API key (uses defaults)
var generator = new VoyageAiEmbeddingGenerator("your-mongodb-atlas-api-key");

// Generate embedding for a single text
var result = await generator.GenerateAsync(new[] { "Sample text" });
var embedding = result[0].Vector; // ReadOnlyMemory<float>

// Generate embeddings for multiple texts
var texts = new[] { "First text", "Second text", "Third text" };
var results = await generator.GenerateAsync(texts);

foreach (var item in results)
{
    Console.WriteLine($"Embedding dimensions: {item.Vector.Length}");
}
```

### With Custom Options

```csharp
var options = new VoyageAiEmbeddingGeneratorOptions
{
    ApiKey = "your-mongodb-atlas-api-key",
    Model = "voyage-4-large",
    InputType = "document",      // Optimize for documents (use "query" for search queries)
    OutputDimension = 1024,      // 256, 512, 1024, or 2048
    MaxRetries = 5,
    RetryDelayMilliseconds = 2000
};

var generator = new VoyageAiEmbeddingGenerator(options);
var embeddings = await generator.GenerateAsync(new[] { "Sample text" });
```

## Dependency Injection

### Basic Registration

```csharp
using Gravity9.MongoDb.VoyageAiEmbeddingGenerator;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

// Register with API key
services.AddVoyageAiEmbeddingGenerator(
    apiKey: "your-mongodb-atlas-api-key");

// Or with configuration
services.AddVoyageAiEmbeddingGenerator(
    apiKey: "your-mongodb-atlas-api-key",
    configureOptions: options =>
    {
        options.Model = "voyage-4-large";
        options.InputType = "document";
        options.OutputDimension = 1024;
    });

var serviceProvider = services.BuildServiceProvider();
var generator = serviceProvider.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();
```

### With Configuration File

**appsettings.json:**
```json
{
  "VoyageAI": {
    "ApiKey": "your-mongodb-atlas-api-key",
    "Model": "voyage-4-large",
    "InputType": "document",
    "OutputDimension": 1024,
    "MaxRetries": 3,
    "RetryDelayMilliseconds": 1000
  }
}
```

**Program.cs:**
```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();

var services = new ServiceCollection();

// Bind configuration to options
var voyageAiConfig = configuration.GetSection("VoyageAI");

services.AddVoyageAiEmbeddingGenerator(
    apiKey: voyageAiConfig["ApiKey"]!,
    configureOptions: options =>
    {
        options.Model = voyageAiConfig["Model"] ?? "voyage-4-large";
        options.InputType = voyageAiConfig["InputType"];
        options.OutputDimension = int.Parse(voyageAiConfig["OutputDimension"] ?? "1024");
    });
```

## MongoDB Vector Store Integration

### Complete Example with Semantic Kernel

```csharp
using Gravity9.MongoDb.VoyageAiEmbeddingGenerator;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Connectors.MongoDB;
using MongoDB.Driver;

// Setup MongoDB
var mongoClient = new MongoClient("mongodb://localhost:27017");
var database = mongoClient.GetDatabase("vectordb");

// Create embedding generator
var embeddingGenerator = new VoyageAiEmbeddingGenerator(new VoyageAiEmbeddingGeneratorOptions
{
    ApiKey = "your-mongodb-atlas-api-key",
    Model = "voyage-4-large",
    InputType = "document",
    OutputDimension = 1024
});

// Create MongoDB vector store collection
var collectionOptions = new MongoCollectionOptions
{
    EmbeddingGenerator = embeddingGenerator, // Used for search-time query embedding only
    VectorIndexName = "product_vector_index"
};

var collection = new MongoCollection<string, ProductRecord>(
    database,
    "products",
    collectionOptions);

// Ensure collection and indexes exist
await collection.EnsureCollectionExistsAsync();

// Create a product
var product = new ProductRecord
{
    Id = "prod-001",
    Name = "Sony WH-1000XM5",
    Description = "Premium wireless noise-cancelling headphones"
};

// IMPORTANT: Generate embedding BEFORE upserting
var embeddingResult = await embeddingGenerator.GenerateAsync([product.Description]);
product.DescriptionEmbedding = embeddingResult[0].Vector;

// Now upsert
await collection.UpsertAsync(product);

// Perform semantic search (EmbeddingGenerator automatically embeds the query)
var searchResults = collection.SearchAsync("noise cancelling headphones", top: 5);

await foreach (var result in searchResults)
{
    Console.WriteLine($"{result.Record.Name} - Score: {result.Score:F4}");
}
```

### Product Record Model

```csharp
using Microsoft.Extensions.VectorData;

public class ProductRecord
{
    [VectorStoreKey]
    public string Id { get; set; } = string.Empty;

    [VectorStoreData(IsIndexed = true)]
    public string Name { get; set; } = string.Empty;

    [VectorStoreData]
    public string Description { get; set; } = string.Empty;

    [VectorStoreData(IsIndexed = true)]
    public string Category { get; set; } = string.Empty;

    [VectorStoreData(IsIndexed = true)]
    public decimal Price { get; set; }

    // Vector property must be a string that returns the source text
    // This enables automatic embedding generation during upsert
    [VectorStoreVector(Dimensions: 1024, DistanceFunction = DistanceFunction.CosineSimilarity, IndexKind = IndexKind.Hnsw)]
    public string? DescriptionEmbedding => Description;
}
```

### Important: Automatic Embedding Generation

? **The MongoDB vector store connector CAN automatically generate embeddings during `UpsertAsync` when configured correctly!**

The key requirement is that the vector property must be of type `string` (or `string?`) and return the source text:

```csharp
public class ProductRecord
{
    [VectorStoreData]
    public string Description { get; set; } = string.Empty;

    // ? CORRECT: String property returning source text - enables automatic embedding
    [VectorStoreVector(Dimensions: 1024, DistanceFunction = DistanceFunction.CosineSimilarity)]
    public string? DescriptionEmbedding => Description;
}

// Configure collection with embedding generator
var collectionOptions = new MongoCollectionOptions
{
    EmbeddingGenerator = embeddingGenerator // This enables automatic embedding generation
};

// Embeddings are automatically generated during upsert!
await collection.UpsertAsync(product);
```

#### How Automatic Embedding Works

1. You configure `EmbeddingGenerator` in `MongoCollectionOptions`
2. The vector property is declared as `string` type that returns the source text
3. During `UpsertAsync`, the MongoDB connector:
   - Reads the string value from the vector property
   - Passes it to the `IEmbeddingGenerator`
   - Stores the generated vector embedding in MongoDB

#### Alternative: Manual Embedding Generation

If you use `ReadOnlyMemory<float>?` as the property type, you must generate embeddings manually:

```csharp
// Property type is ReadOnlyMemory<float>?
[VectorStoreVector(Dimensions: 1024, DistanceFunction = DistanceFunction.CosineSimilarity)]
public ReadOnlyMemory<float>? DescriptionEmbedding { get; set; }

// Must generate embedding before upserting
var embeddingResult = await embeddingGenerator.GenerateAsync([product.Description]);
product.DescriptionEmbedding = embeddingResult[0].Vector;
await collection.UpsertAsync(product);
```

**Recommendation:** Use the `string` property pattern for simpler code and automatic embedding generation.

### Important: Embedding Generation During Upsert

?? **The MongoDB vector store connector does NOT automatically generate embeddings during `UpsertAsync`.** 

You must explicitly generate embeddings before saving documents:

```csharp
// ? CORRECT
var embeddingResult = await embeddingGenerator.GenerateAsync([product.Description]);
product.DescriptionEmbedding = embeddingResult[0].Vector;
await collection.UpsertAsync(product);

// ? INCORRECT - DescriptionEmbedding will be null!
await collection.UpsertAsync(product);
```

The `EmbeddingGenerator` in `MongoCollectionOptions` is used for **search-time query embedding only** (converting search query text to vectors), not for document insertion.

### Batch Processing Documents

```csharp
public async Task ProcessBatchAsync(
    List<ProductRecord> products,
    MongoCollection<string, ProductRecord> collection)
{
    // With automatic embedding generation, simply upsert all products
    // The EmbeddingGenerator will create embeddings automatically
    foreach (var product in products)
    {
        await collection.UpsertAsync(product);
    }
}

// Alternative: Manual batch embedding generation (if using ReadOnlyMemory<float>? property type)
public async Task ProcessBatchManuallyAsync(
    List<ProductRecord> products,
    IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
    MongoCollection<string, ProductRecord> collection)
{
    // Generate all embeddings in one API call (more efficient)
    var descriptions = products.Select(p => p.Description).ToArray();
    var embeddingResults = await embeddingGenerator.GenerateAsync(descriptions);

    // Assign embeddings to products
    for (int i = 0; i < products.Count; i++)
    {
        products[i].DescriptionEmbedding = embeddingResults[i].Vector;
    }

    // Upsert all products
    foreach (var product in products)
    {
        await collection.UpsertAsync(product);
    }
}
```

## Configuration

### API Endpoints

#### MongoDB Atlas AI (Default)
```csharp
var options = new VoyageAiEmbeddingGeneratorOptions
{
    ApiKey = "your-mongodb-atlas-api-key",
    BaseUrl = "https://ai.mongodb.com/v1", // Default
    Model = "voyage-4-large"
};
```

#### Direct VoyageAI API
```csharp
var options = new VoyageAiEmbeddingGeneratorOptions
{
    ApiKey = "your-voyageai-api-key",
    BaseUrl = "https://api.voyageai.com/v1",
    Model = "voyage-3"
};
```

### Model Selection

```csharp
// MongoDB Atlas AI models
options.Model = "voyage-4-large";    // Best performance (default)
options.Model = "voyage-4";          // Standard latest
options.Model = "voyage-4-lite";     // Faster, lower cost
options.Model = "voyage-3-large";    // Previous generation
options.Model = "voyage-code-3";     // Optimized for code
options.Model = "voyage-finance-2";  // Financial documents
options.Model = "voyage-law-2";      // Legal documents
```

### Input Type Optimization

```csharp
// For indexing documents
options.InputType = "document";

// For search queries
options.InputType = "query";

// Default (no optimization)
options.InputType = null;
```

### Output Dimensions

```csharp
options.OutputDimension = 256;   // Smaller, faster
options.OutputDimension = 512;   // Balanced
options.OutputDimension = 1024;  // Default, good balance
options.OutputDimension = 2048;  // Maximum, slower but more precise
```

### Retry Configuration

```csharp
options.MaxRetries = 3;                   // Default
options.RetryDelayMilliseconds = 1000;    // 1 second between retries
options.RequestTimeout = TimeSpan.FromSeconds(30);  // Default
```

## Advanced Scenarios

### Multiple Embedding Generators

Use keyed services for different configurations:

```csharp
// Register multiple generators for different purposes
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

        // Generate embeddings for batch
        var descriptions = batch.Select(p => p.Description).ToArray();
        var embeddingResults = await embeddingGenerator.GenerateAsync(descriptions);

        // Assign embeddings
        for (int j = 0; j < batch.Count; j++)
        {
            batch[j].DescriptionEmbedding = embeddingResults[j].Vector;
        }

        // Upsert batch
        foreach (var product in batch)
        {
            await collection.UpsertAsync(product);
        }

        // Report progress
        var progressPercentage = ((i + 1) / (double)totalBatches) * 100;
        progress.Report(progressPercentage);

        Console.WriteLine($"Processed {i + 1}/{totalBatches} batches ({progressPercentage:F1}%)");
    }
}
```

## Best Practices

1. **Use String Property for Automatic Embedding**: Define vector properties as `string` type that return the source text to enable automatic embedding generation.

   ```csharp
   // ? BEST: Automatic embedding generation
   [VectorStoreVector(Dimensions: 1024, DistanceFunction = DistanceFunction.CosineSimilarity)]
   public string? DescriptionEmbedding => Description;

   // Works automatically with EmbeddingGenerator in MongoCollectionOptions
   await collection.UpsertAsync(product);
   ```

2. **Batch for Manual Embedding**: If you need manual control, generate embeddings for multiple documents in a single API call.

   ```csharp
   // Generate embeddings in batch (more efficient than individual calls)
   var descriptions = products.Select(p => p.Description).ToArray();
   var embeddings = await generator.GenerateAsync(descriptions);
   ```

3. **Use Dependency Injection**: Register the embedding generator in your DI container for better lifecycle management and testability.

4. **Configure Retry Logic**: Adjust `MaxRetries` and `RetryDelayMilliseconds` based on your application's tolerance for latency.

5. **Monitor Token Usage**: Track the `Usage` property in responses to monitor your API consumption.

6. **Choose Appropriate Models**: 
   - Use `voyage-4-lite` for faster responses when accuracy can be slightly lower
   - Use `voyage-4-large` for best accuracy
   - Use specialized models like `voyage-code-3` for code embeddings

7. **Set Input Type**: Always set `InputType` to "query" or "document" for better semantic search results.

8. **Handle Errors Gracefully**: Implement proper error handling for rate limits and transient failures.

9. **Reuse HttpClient**: When creating `VoyageAiEmbeddingGenerator` instances directly (not via DI), be mindful of HttpClient lifecycle. The DI registration handles this automatically.

10. **Use Correct Base URL**: 
    - For MongoDB Atlas Model API keys: `https://ai.mongodb.com/v1` (default)
    - For direct VoyageAI API keys: `https://api.voyageai.com/v1`
