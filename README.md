# Gravity9.MongoDb.VoyageAiEmbeddingGenerator

A .NET library that implements the `IEmbeddingGenerator<string, Embedding<float>>` interface from [Microsoft.Extensions.AI](https://learn.microsoft.com/en-us/dotnet/ai/microsoft-extensions-ai), using the [MongoDB Atlas VoyageAI](https://www.mongodb.com/docs/api/doc/atlas-embedding-and-reranking-api/) embedding models.
This makes it seamlessly compatible with Microsoft Agent Framework and Semantic Kernel's MongoDB vector store connector.

[![NuGet](https://img.shields.io/nuget/v/Gravity9.MongoDb.VoyageAiEmbeddingGenerator.svg)](https://www.nuget.org/packages/Gravity9.MongoDb.VoyageAiEmbeddingGenerator/)
[![License: Apache 2.0](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](https://opensource.org/licenses/Apache-2.0)

## Overview

This library provides MongoDB Atlas VoyageAI embedding generation capabilities through the Microsoft.Extensions.AI abstractions, making it compatible with:
- **Microsoft Agent Framework** - Build AI agents that leverage VoyageAI embeddings
- **Semantic Kernel** - Integrate with Semantic Kernel's MongoDB vector store connector
- **Any framework using Microsoft.Extensions.AI** - Works with any library that consumes the standard `IEmbeddingGenerator` interface

## Features

- ✅ **MongoDB Atlas AI Integration** - Uses VoyageAI models through MongoDB Atlas AI API (`https://ai.mongodb.com/v1`)
- ✅ **Microsoft.Extensions.AI Compatible** - Implements standard `IEmbeddingGenerator<string, Embedding<float>>` interface
- ✅ **Microsoft Agent Framework Ready** - Works with Microsoft's AI agent building framework
- ✅ **Semantic Kernel Ready** - Works seamlessly with Semantic Kernel's MongoDB vector store connector
- ✅ **Dependency Injection Support** - Easy integration with Microsoft.Extensions.DependencyInjection
- ✅ **Automatic Retry Logic** - Handles transient failures with configurable retry policies
- ✅ **Multiple Model Support** - Access to voyage-4-large, voyage-4-lite, voyage-code-3, and more
- ✅ **Flexible Configuration** - Support for different input types, output dimensions, and encoding formats
- ✅ **Token Usage Tracking** - Monitor API usage through response metadata

## Installation

Install via NuGet Package Manager:

```bash
dotnet add package Gravity9.MongoDb.VoyageAiEmbeddingGenerator
```

Or via Package Manager Console:

```powershell
Install-Package Gravity9.MongoDb.VoyageAiEmbeddingGenerator
```

## Quick Start

### With Dependency Injection

```csharp
using Gravity9.MongoDb.VoyageAiEmbeddingGenerator;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

// Register the embedding generator
services.AddVoyageAiEmbeddingGenerator(
    apiKey: "your-mongodb-atlas-api-key",
    configureOptions: options =>
    {
        options.Model = "voyage-4-large";
        options.InputType = "document";
        options.OutputDimension = 1024;
    });

var serviceProvider = services.BuildServiceProvider();

// Resolve and use
var generator = serviceProvider.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();
var embeddings = await generator.GenerateAsync(new[] { "Sample text" });
```

### With Semantic Kernel MongoDB Vector Store

```csharp
using ConsoleApp;
using Gravity9.MongoDb.VoyageAiEmbeddingGenerator;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Connectors.MongoDB;
using MongoDB.Driver;

// Setup services
var services = new ServiceCollection();

// Register MongoDB
services.AddSingleton<IMongoClient>(new MongoClient("mongodb://localhost:27017"));
services.AddSingleton(sp => sp.GetRequiredService<IMongoClient>().GetDatabase("vectordb"));

// Register VoyageAI Embedding Generator
services.AddVoyageAiEmbeddingGenerator(
    apiKey: "your-mongodb-atlas-api-key",
    configureOptions: options =>
    {
        options.Model = "voyage-4-large";
        options.InputType = "document";
        options.OutputDimension = 1024;
    });

var serviceProvider = services.BuildServiceProvider();

// Get services
var database = serviceProvider.GetRequiredService<IMongoDatabase>();
var embeddingGenerator = serviceProvider.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();

// Create vector store collection
var collectionOptions = new MongoCollectionOptions
{
    EmbeddingGenerator = embeddingGenerator, // Enables automatic embedding generation
    VectorIndexName = "product_vector_index"
};

var collection = new MongoCollection<string, ProductRecord>(
    database,
    "products",
    collectionOptions);

// Ensure collection exists
await collection.EnsureCollectionExistsAsync();

// Create and insert a product
var product = new ProductRecord
{
    Id = "prod-001",
    Name = "Sony WH-1000XM5",
    Description = "Premium wireless noise-cancelling headphones with exceptional sound quality."
};

// Embeddings are generated automatically during upsert!
// (because DescriptionEmbedding property returns Description text)
await collection.UpsertAsync(product);

// Perform semantic search (EmbeddingGenerator automatically converts query to vector)
var searchResults = collection.SearchAsync("noise cancelling headphones", top: 5);

await foreach (var result in searchResults)
{
    Console.WriteLine($"{result.Record.Name} - Score: {result.Score}");
}
```

**ProductRecord Model:**

```csharp
public class ProductRecord
{
    [VectorStoreKey]
    public string Id { get; set; } = string.Empty;

    [VectorStoreData]
    public string Description { get; set; } = string.Empty;

    // Vector property must be a string that returns the source text
    // This enables automatic embedding generation during upsert
    [VectorStoreVector(Dimensions: 1024, DistanceFunction = DistanceFunction.CosineSimilarity, IndexKind = IndexKind.Hnsw)]
    public string? DescriptionEmbedding => Description;
}
```

## Configuration Options

### VoyageAiEmbeddingGeneratorOptions

```csharp
public class VoyageAiEmbeddingGeneratorOptions
{
    // API Configuration
    public string? ApiKey { get; set; }                    // Required: Your MongoDB Atlas API key
    public string BaseUrl { get; set; }                    // Default: https://ai.mongodb.com/v1
    
    // Model Configuration
    public string Model { get; set; }                      // Default: voyage-4-large
    public string? InputType { get; set; }                 // "query" or "document" for optimization
    
    // Output Configuration
    public int? OutputDimension { get; set; }              // 256, 512, 1024, or 2048
    public string OutputDtype { get; set; }                // Default: float
    public string? EncodingFormat { get; set; }            // null (arrays) or "base64"
    public bool Truncation { get; set; }                   // Default: true
    
    // HTTP Configuration
    public TimeSpan RequestTimeout { get; set; }           // Default: 30 seconds
    public int MaxRetries { get; set; }                    // Default: 3
    public int RetryDelayMilliseconds { get; set; }        // Default: 1000
}
```

## Available Models

### MongoDB Atlas AI Models

- **voyage-4-large** - Best performance, highest accuracy (default)
- **voyage-4** - Standard latest model
- **voyage-4-lite** - Faster, lower cost
- **voyage-3-large** - Previous generation large model
- **voyage-3.5** - Previous generation standard model
- **voyage-code-3** - Optimized for code embeddings
- **voyage-finance-2** - Optimized for financial documents
- **voyage-law-2** - Optimized for legal documents
- **voyage-multimodal-3** - Multimodal support (images + text)

### Direct VoyageAI API Models

If using direct VoyageAI API (`https://api.voyageai.com/v1`):
- **voyage-3** - Latest standard
- **voyage-3-lite** - Faster, lower cost
- **voyage-code-3** - Optimized for code

## API Endpoints

### MongoDB Atlas AI (Default)
- **Base URL**: `https://ai.mongodb.com/v1`
- **Authentication**: Bearer token (Model API Key from MongoDB Atlas)
- **Get API Key**: MongoDB Atlas → Model API Keys section
- **Models**: voyage-4-large, voyage-4, voyage-4-lite, voyage-3-large, etc.

### Direct VoyageAI API (Alternative)
- **Base URL**: `https://api.voyageai.com/v1`
- **Authentication**: Bearer token (VoyageAI API key)
- **Get API Key**: VoyageAI website
- **Models**: voyage-3, voyage-3-lite, voyage-code-3

To use direct VoyageAI API:
```csharp
options.BaseUrl = "https://api.voyageai.com/v1";
options.Model = "voyage-3";
```

## Important Notes

### Automatic Embedding Generation

**The Semantic Kernel MongoDB connector CAN automatically generate embeddings during `UpsertAsync` when configured correctly!**

The key is that the vector property must be of type `string` (or `string?`) and return the source text to embed:

```csharp
public class ProductRecord
{
    [VectorStoreData]
    public string Description { get; set; } = string.Empty;

    // ✅ CORRECT: String property that returns source text - enables automatic embedding
    [VectorStoreVector(Dimensions: 1024, DistanceFunction: DistanceFunction.CosineSimilarity)]
    public string? DescriptionEmbedding => Description;
}

// With EmbeddingGenerator configured, this will automatically generate embeddings
await collection.UpsertAsync(product);
```

How it works:
1. The MongoDB connector reads the string value from `DescriptionEmbedding` (which returns `Description`)
2. It passes this string to the configured `IEmbeddingGenerator`
3. The generator creates the vector embedding
4. The vector is stored in MongoDB

**Note:** If you use `ReadOnlyMemory<float>?` as the property type, you must manually generate embeddings before upserting:

```csharp
// ❌ With ReadOnlyMemory<float>? - requires manual embedding generation
[VectorStoreVector(Dimensions: 1024, DistanceFunction: DistanceFunction.CosineSimilarity)]
public ReadOnlyMemory<float>? DescriptionEmbedding { get; set; }

// Must generate manually
var embeddingResult = await embeddingGenerator.GenerateAsync([product.Description]);
product.DescriptionEmbedding = embeddingResult[0].Vector;
await collection.UpsertAsync(product);
```

The `EmbeddingGenerator` property in `MongoCollectionOptions` is used for:
- **Automatic embedding during upsert** (when vector property is string type)
- **Search-time query embedding** (converting search queries to vectors)

## Complete Example

See the [samples/ConsoleApp](./samples/ConsoleApp) directory for a complete working example that demonstrates:

- ✅ Dependency injection setup
- ✅ Configuration management with appsettings.json
- ✅ MongoDB connection
- ✅ Automatic embedding generation during document insertion
- ✅ Vector store collection creation with vector indexes
- ✅ Document upserting with embeddings
- ✅ Semantic search with vector similarity
- ✅ Filtered semantic search (combining vector search with property filters)
- ✅ Error handling and logging
- ✅ Product catalog search demonstration

**To run the sample:**

```bash
cd samples/ConsoleApp

# Configure your API key in appsettings.Development.json
# {
#   "VoyageAI": {
#     "ApiKey": "your-mongodb-atlas-api-key"
#   }
# }

dotnet run
```

The sample demonstrates a product catalog with semantic search capabilities, showing how to:
- Insert products with automatic embedding generation
- Search for products using natural language queries
- Filter results by price while maintaining semantic relevance
- Retrieve individual products with their embeddings

## About Atlas Embedding and Reranking API

The [Atlas Embedding and Reranking API](https://www.mongodb.com/docs/api/doc/atlas-embedding-and-reranking-api/) provides programmatic access to the latest Voyage AI embedding and reranking models through a RESTful interface. This API allows you to generate high-quality embeddings for text data, enabling powerful semantic search and similarity matching capabilities within MongoDB Atlas.

**Key Benefits:**
- Access to state-of-the-art Voyage AI models (voyage-4-large, voyage-4, voyage-4-lite, and more)
- Seamless integration with MongoDB Atlas Vector Search
- Support for different input types (documents vs queries) for optimized results
- Flexible output dimensions and encoding formats
- Built-in rate limiting and authentication

Learn more: [MongoDB Atlas Embedding and Reranking API Documentation](https://www.mongodb.com/docs/atlas/atlas-vector-search/ai-integrations/voyage/)

## About Microsoft.Extensions.AI.Abstractions

[Microsoft.Extensions.AI.Abstractions](https://devblogs.microsoft.com/dotnet/introducing-microsoft-extensions-ai-preview/) is a unified set of APIs for integrating AI services into .NET applications. It provides standardized interfaces that enable developers to write AI-powered code that works across different AI providers without vendor lock-in.

**Key Interfaces:**
- `IEmbeddingGenerator<TInput, TEmbedding>` - Generate embeddings from text or other inputs
- `IChatClient` - Interact with chat-based AI models
- `ITextGenerator` - Generate text completions

**Benefits:**
- **Provider Agnostic** - Write code once, swap providers easily
- **Dependency Injection Ready** - Built for modern .NET DI patterns
- **Extensible** - Add middleware, logging, caching, and more
- **Type Safe** - Strongly typed interfaces for better developer experience

This library implements the `IEmbeddingGenerator<string, Embedding<float>>` interface, making it compatible with any .NET application that uses Microsoft.Extensions.AI abstractions.

Learn more: 
- [Microsoft.Extensions.AI Announcement](https://devblogs.microsoft.com/dotnet/introducing-microsoft-extensions-ai-preview/)
- [Microsoft.Extensions.AI on NuGet](https://www.nuget.org/packages/Microsoft.Extensions.AI.Abstractions/)
- [Microsoft.Extensions.AI GitHub](https://github.com/dotnet/extensions/tree/main/src/Libraries/Microsoft.Extensions.AI.Abstractions)
