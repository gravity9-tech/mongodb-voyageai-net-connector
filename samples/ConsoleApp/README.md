# Console App Sample - VoyageAI with MongoDB Vector Store

This sample demonstrates how to use the `Gravity9.MongoDb.VoyageAiEmbeddingGenerator` with Semantic Kernel's MongoDB vector store connector.

## Prerequisites

1. **MongoDB** - Running instance (local or cloud)
   - Default: `mongodb://localhost:27017`
   - Or use MongoDB Atlas (cloud)

2. **VoyageAI API Key**
   - Get it from [MongoDB Atlas](https://www.mongodb.com/cloud/atlas)
   - Navigate to Model API Keys section

## Setup

### 1. Configure Settings

Edit `appsettings.json`:

```json
{
  "MongoDB": {
    "ConnectionString": "mongodb://localhost:27017",
    "DatabaseName": "vectordb"
  },
  "VoyageAI": {
    "ApiKey": "your-mongodb-atlas-api-key-here",
    "Model": "voyage-4-large",
    "InputType": "document",
    "OutputDimension": 1024
  }
}
```

**Note:** This sample uses MongoDB Atlas AI API (`https://ai.mongodb.com/v1`) by default. If you're using a direct VoyageAI API key instead, add `"BaseUrl": "https://api.voyageai.com/v1"` and use models like `voyage-3`, `voyage-3-lite`, etc.

Or set environment variable:

```bash
# Windows (PowerShell)
$env:VOYAGE_API_KEY="your-api-key"

# Windows (CMD)
set VOYAGE_API_KEY=your-api-key

# Linux/Mac
export VOYAGE_API_KEY=your-api-key
```

### 2. Install MongoDB (if needed)

**Docker:**

```bash
docker run -d -p 27017:27017 --name mongodb mongo:latest
```

**Or use MongoDB Atlas:**

- Sign up at https://www.mongodb.com/cloud/atlas
- Create a free cluster
- Get connection string
- Update `appsettings.json`

### 3. Build and Run

```bash
cd samples/ConsoleApp
dotnet build
dotnet run
```

## What This Sample Demonstrates

### 1. **Automatic Embedding Generation**

```csharp
// Define your data model with vector property
public class ProductRecord
{
    [VectorStoreKey]
    public string Id { get; set; }

    [VectorStoreData]
    public string Description { get; set; }

    // Vector property must be a string that returns the source text
    // This enables automatic embedding generation during upsert
    [VectorStoreVector(1024, DistanceFunction.CosineSimilarity)]
    public string? DescriptionEmbedding => Description;
}

// Configure collection with embedding generator
var collectionOptions = new MongoCollectionOptions
{
    EmbeddingGenerator = embeddingGenerator, // Will automatically generate embeddings during upsert
    VectorIndexName = "product_vector_index"
};

var collection = new MongoCollection<string, ProductRecord>(database, "products", collectionOptions);

// Embeddings are generated automatically during upsert!
await collection.UpsertAsync(product);
```

**Important:** The vector property must be of type `string` (or `string?`) and return the source text to enable automatic embedding generation. The MongoDB connector will automatically call the `IEmbeddingGenerator` to create the vector embedding from this string value.

### 2. **Semantic Search**

```csharp
var results = collection.SearchAsync(
    "noise cancelling headphones for travel",
    top: 5);

await foreach (var result in results)
{
    Console.WriteLine($"{result.Record.Name} - Score: {result.Score}");
}
```

### 3. **Filtered Semantic Search**

```csharp
var results = collection.SearchAsync(
    "professional work setup",
    top: 5,
    new VectorSearchOptions<ProductRecord>
    {
        Filter = p => p.Price <= 300
    });
```

### 4. **Dependency Injection**

```csharp
services.AddVoyageAiEmbeddingGenerator(
    apiKey: "your-api-key",
    configureOptions: options =>
    {
        // BaseUrl defaults to https://ai.mongodb.com/v1 for MongoDB Atlas AI
        // For direct VoyageAI API, set: options.BaseUrl = "https://api.voyageai.com/v1";
        options.Model = "voyage-4-large";
        options.InputType = "document";
        options.OutputDimension = 1024;
    });
```

## Expected Output

```
=== VoyageAI Embedding Generator with MongoDB Vector Store Demo ===

Connected to MongoDB: vectordb
Using VoyageAI Model: voyage-4-large

Ensuring collection and indexes exist...
✓ Collection and indexes ready

Inserting 5 products with automatic embedding generation...
✓ Inserted: Sony WH-1000XM5
✓ Inserted: Apple AirPods Pro
✓ Inserted: Mechanical Gaming Keyboard
✓ Inserted: 4K Webcam
✓ Inserted: Ergonomic Office Chair

=== Semantic Search Examples ===

Query: "noise cancelling headphones for travel"
Top 3 Results:
--------------------------------------------------------------------------------
1. Sony WH-1000XM5 (Score: 0.8543)
   Category: Electronics | Price: $399.99
   Description: Premium wireless noise-cancelling headphones...

2. Apple AirPods Pro (Score: 0.7821)
   Category: Electronics | Price: $249.99
   Description: True wireless earbuds with active noise cancellation...

...
```

## Key Features Demonstrated

- ✅ Integration with Semantic Kernel MongoDB connector
- ✅ Automatic embedding generation using VoyageAI
- ✅ Vector similarity search
- ✅ Filtered vector search
- ✅ Dependency injection setup
- ✅ Configuration management
- ✅ Error handling

## Customization

### Change the Model

**MongoDB Atlas AI Models:**
```csharp
options.Model = "voyage-4-large"; // Best performance (default)
options.Model = "voyage-4"; // Standard latest model
options.Model = "voyage-4-lite"; // Faster, lower cost
options.Model = "voyage-3-large"; // Previous generation large
options.Model = "voyage-code-3"; // Optimized for code
options.Model = "voyage-finance-2"; // Optimized for finance
options.Model = "voyage-law-2"; // Optimized for legal documents
```

**Direct VoyageAI API Models:**
```csharp
options.BaseUrl = "https://api.voyageai.com/v1";
options.Model = "voyage-3"; // Latest standard
options.Model = "voyage-3-lite"; // Faster, lower cost
options.Model = "voyage-code-3"; // Optimized for code
```

### Adjust Output Dimensions

```csharp
options.OutputDimension = 512;  // Smaller, faster
options.OutputDimension = 2048; // Larger, more precise
```

### Optimize for Queries vs Documents

```csharp
// For indexing documents
options.InputType = "document";

// For search queries (if you generate embeddings for queries separately)
options.InputType = "query";
```

## Troubleshooting

### "VoyageAI API key not found"

- Set API key in `appsettings.json` or as environment variable
- Ensure environment variable is set in the current shell session

### "Connection refused to MongoDB"

- Ensure MongoDB is running: `docker ps` (if using Docker)
- Check connection string in `appsettings.json`
- Verify port 27017 is accessible

### "Rate limit exceeded"

- The library automatically retries
- Reduce batch size or add delays between operations
- Check your VoyageAI API quota

## Next Steps

1. **Modify the data model** - Add your own fields and properties
2. **Try different search queries** - Test semantic understanding
3. **Experiment with filters** - Combine semantic and structured search
4. **Scale up** - Add more documents and test performance
5. **Integrate into your app** - Use this as a template for your application

## Resources

- [VoyageAI Documentation](http://dochub.mongodb.org/core/voyage-landing)
- [Semantic Kernel Docs](https://learn.microsoft.com/en-us/semantic-kernel/)
- [MongoDB Vector Search](https://www.mongodb.com/docs/atlas/atlas-vector-search/vector-search-overview/)
