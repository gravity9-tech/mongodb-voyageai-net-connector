# Gravity9.MongoDb.VoyageAiEmbeddingGenerator

A .NET library that implements the `IEmbeddingGenerator<string, Embedding<float>>` interface from [Microsoft.Extensions.AI](https://learn.microsoft.com/en-us/dotnet/ai/microsoft-extensions-ai), using the [MongoDB Atlas VoyageAI](https://www.mongodb.com/docs/api/doc/atlas-embedding-and-reranking-api/) embedding models.

[![NuGet](https://img.shields.io/nuget/v/Gravity9.MongoDb.VoyageAiEmbeddingGenerator.svg)](https://www.nuget.org/packages/Gravity9.MongoDb.VoyageAiEmbeddingGenerator/)
[![License: Apache 2.0](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](https://opensource.org/licenses/Apache-2.0)

## Features

- ✅ **MongoDB Atlas AI Integration** - Uses VoyageAI models through MongoDB Atlas AI API
- ✅ **Microsoft.Extensions.AI Compatible** - Implements standard `IEmbeddingGenerator` interface
- ✅ **Microsoft Agent Framework Ready** - Works with Microsoft's AI agent building framework
- ✅ **Semantic Kernel Ready** - Works seamlessly with Semantic Kernel's MongoDB vector store connector
- ✅ **Dependency Injection Support** - Easy integration with Microsoft.Extensions.DependencyInjection
- ✅ **Automatic Retry Logic** - Handles transient failures with configurable retry policies
- ✅ **Multiple Model Support** - voyage-4-large, voyage-4-lite, voyage-code-3, and more

## Installation

```bash
dotnet add package Gravity9.MongoDb.VoyageAiEmbeddingGenerator
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
var generator = serviceProvider.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();

// Generate embeddings
var embeddings = await generator.GenerateAsync(new[] { "Sample text" });
```

### Direct Usage

```csharp
using Gravity9.MongoDb.VoyageAiEmbeddingGenerator;

var generator = new VoyageAiEmbeddingGenerator("your-mongodb-atlas-api-key");
var result = await generator.GenerateAsync(new[] { "Sample text" });
var embedding = result[0].Vector;
```

### With Semantic Kernel MongoDB Vector Store

```csharp
using Gravity9.MongoDb.VoyageAiEmbeddingGenerator;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Connectors.MongoDB;
using MongoDB.Driver;

// Setup MongoDB
var mongoClient = new MongoClient("mongodb://localhost:27017");
var database = mongoClient.GetDatabase("vectordb");

// Setup embedding generator
var embeddingGenerator = new VoyageAiEmbeddingGenerator(
    new VoyageAiEmbeddingGeneratorOptions
    {
        ApiKey = "your-mongodb-atlas-api-key",
        Model = "voyage-4-large",
        OutputDimension = 1024
    });

// Create vector store
var vectorStore = new MongoDBVectorStore(database);
var collection = vectorStore.GetCollection<Guid, MyRecord>("products");

// Create record and embed
await collection.CreateCollectionIfNotExistsAsync();
```

## Configuration Options

| Property                 | Description                      | Default          |
| ------------------------ | -------------------------------- | ---------------- |
| `ApiKey`                 | MongoDB Atlas API key (required) | -                |
| `Model`                  | VoyageAI model name              | `voyage-4-large` |
| `InputType`              | `document` or `query`            | `document`       |
| `OutputDimension`        | 256, 512, 1024, or 2048          | Model default    |
| `EncodingFormat`         | `float` or `base64`              | `float`          |
| `MaxRetries`             | Maximum retry attempts           | 3                |
| `RetryDelayMilliseconds` | Delay between retries            | 1000             |

## Supported Models

- **voyage-4-large** - High performance, 2048 dimensions (default)
- **voyage-4-lite** - Lightweight, faster, 1024 dimensions
- **voyage-code-3** - Optimized for code, 1024 dimensions
- **voyage-3** - General purpose, 1024 dimensions
- **voyage-3-lite** - Lighter version, 512 dimensions

## Documentation

For detailed documentation, visit:

- [GitHub Repository](https://github.com/gravity9-tech/mongodb-voyageai-net-connector)
- [Usage Guide](https://github.com/gravity9-tech/mongodb-voyageai-net-connector/blob/main/USAGE.md)

## License

Apache 2.0 - See [LICENSE](https://github.com/gravity9-tech/mongodb-voyageai-net-connector/blob/main/LICENSE) for details.

## Support

- Report issues: [GitHub Issues](https://github.com/gravity9-tech/mongodb-voyageai-net-connector/issues)
- Author: Gravity9
