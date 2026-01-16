using ConsoleApp;

using Gravity9.MongoDb.VoyageAiEmbeddingGenerator;

using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Connectors.MongoDB;

using MongoDB.Driver;

Console.WriteLine("=== VoyageAI Embedding Generator with MongoDB Vector Store Demo ===\n");

// Build configuration
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false)
    .AddJsonFile("appsettings.Development.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

// Setup dependency injection
var services = new ServiceCollection();

// Register configuration
services.AddSingleton<IConfiguration>(configuration);

// Configure options using the Options pattern
services.Configure<MongoDbOptions>(configuration.GetSection("MongoDB"));
services.Configure<VoyageAiOptions>(configuration.GetSection("VoyageAI"));


var mongoOptions = configuration.GetSection("MongoDB").Get<MongoDbOptions>()!;
var voyageAiOptions = configuration.GetSection("VoyageAI").Get<VoyageAiOptions>()!;

// Register MongoDB
services.AddSingleton<IMongoClient>(sp =>
{
    return new MongoClient(mongoOptions.ConnectionString);
});
services.AddSingleton(sp => sp.GetRequiredService<IMongoClient>().GetDatabase(mongoOptions.DatabaseName));

// Register VoyageAI Embedding Generator implementation for Microsoft.Extensions.AI.Abstractions specifically IEmbeddingGenerator<string, Embedding<float>>
services.AddVoyageAiEmbeddingGenerator(
    apiKey: voyageAiOptions.ApiKey!,
    configureOptions: options =>
    {
        options.BaseUrl = voyageAiOptions.BaseUrl;
        options.Model = voyageAiOptions.Model;
        options.InputType = voyageAiOptions.InputType;
        options.OutputDimension = voyageAiOptions.OutputDimension;
        options.MaxRetries = voyageAiOptions.MaxRetries;
        options.RetryDelayMilliseconds = voyageAiOptions.RetryDelayMilliseconds;
    });

// Build service provider
var serviceProvider = services.BuildServiceProvider();

try
{
    // Get services
    var database = serviceProvider.GetRequiredService<IMongoDatabase>();

    // Get the embedding generator abstracted by Microsoft.Extensions.AI
    var embeddingGenerator = serviceProvider.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();

    Console.WriteLine($"Connected to MongoDB: {database.DatabaseNamespace.DatabaseName}");
    Console.WriteLine($"Using VoyageAI Model: {voyageAiOptions.Model}\n");

    // Create MongoDB vector store collection with automatic embedding generation
    var collectionOptions = new MongoCollectionOptions
    {
        EmbeddingGenerator = embeddingGenerator,
        VectorIndexName = "product_vector_index",
        FullTextSearchIndexName = "product_fulltext_index"
    };

    // Get the Microsoft.Extensions.VectorData.Abstractions
    var collection = new MongoCollection<string, ProductRecord>(
        database,
        "products",
        collectionOptions);

    // Ensure collection and indexes exist
    Console.WriteLine("Ensuring collection and indexes exist...");
    await collection.EnsureCollectionExistsAsync();
    Console.WriteLine("✓ Collection and indexes ready\n");

    // Sample products
    var products = new List<ProductRecord>
    {
        new()
        {
            Id = "prod-001",
            Name = "Sony WH-1000XM5",
            Description = "Premium wireless noise-cancelling headphones with industry-leading ANC technology, exceptional sound quality, and 30-hour battery life. Perfect for travel and work.",
            Category = "Electronics",
            Price = 399.99m
        },
        new()
        {
            Id = "prod-002",
            Name = "Apple AirPods Pro",
            Description = "True wireless earbuds with active noise cancellation, transparency mode, and spatial audio. Comfortable in-ear design with multiple tip sizes.",
            Category = "Electronics",
            Price = 249.99m
        },
        new()
        {
            Id = "prod-003",
            Name = "Mechanical Gaming Keyboard",
            Description = "RGB backlit mechanical keyboard with Cherry MX switches, programmable keys, and dedicated media controls. Ideal for gaming and typing enthusiasts.",
            Category = "Computers",
            Price = 129.99m
        },
        new()
        {
            Id = "prod-004",
            Name = "4K Webcam",
            Description = "Professional 4K webcam with auto-focus, HDR, and dual microphones. Perfect for video conferencing, streaming, and content creation.",
            Category = "Computers",
            Price = 199.99m
        },
        new()
        {
            Id = "prod-005",
            Name = "Ergonomic Office Chair",
            Description = "Premium ergonomic office chair with lumbar support, adjustable armrests, and breathable mesh back. Designed for all-day comfort during long work sessions.",
            Category = "Furniture",
            Price = 449.99m
        }
    };

    // Insert products (embeddings must be generated before upserting)
    Console.WriteLine($"Inserting {products.Count} products with embedding generation...");
    foreach (var product in products)
    {
        // Note: Embeddings will be automatically calculated from the Description property using the IEmbeddingGenerator registered earlier
        await collection.UpsertAsync(product);

        Console.WriteLine($"✓ Inserted: {product.Name}");
    }
    Console.WriteLine();

    // Perform semantic search
    Console.WriteLine("=== Semantic Search Examples ===\n");

    // Search 1: Find noise-cancelling products
    await PerformSearch(collection, "noise cancelling headphones for travel", 3);

    // Search 2: Find products for home office setup
    await PerformSearch(collection, "comfortable home office equipment", 3);

    // Search 3: Find wireless audio devices
    await PerformSearch(collection, "wireless earbuds with good battery life", 3);

    // Demonstrate filtered search
    Console.WriteLine("\n=== Filtered Search Example ===\n");
    await PerformFilteredSearch(collection, "professional work from home setup", maxPrice: 300m);

    // Demonstrate retrieval of specific product
    Console.WriteLine("\n=== Retrieve Specific Product ===\n");
    var specificProduct = await collection.GetAsync("prod-001");
    if (specificProduct != null)
    {
        Console.WriteLine($"Retrieved Product: {specificProduct.Name}");
        Console.WriteLine($"Description: {specificProduct.Description}");
        Console.WriteLine($"Price: ${specificProduct.Price}");
    }

    Console.WriteLine("\n=== Demo Complete ===");
    Console.WriteLine("\nNote: To clean up, you can drop the collection:");
    Console.WriteLine("  await collection.EnsureCollectionDeletedAsync();");
}
catch (Exception ex)
{
    Console.WriteLine($"\n❌ ERROR: {ex.Message}");
    Console.WriteLine($"Stack Trace: {ex.StackTrace}");
}
finally
{
    await serviceProvider.DisposeAsync();
}

// Helper method to perform semantic search
static async Task PerformSearch(
    VectorStoreCollection<string, ProductRecord> collection,
    string query,
    int topResults)
{
    Console.WriteLine($"Query: \"{query}\"");
    Console.WriteLine($"Top {topResults} Results:");
    Console.WriteLine(new string('-', 80));

    var searchResults = collection.SearchAsync(
        query,
        top: topResults,
        new Microsoft.Extensions.VectorData.VectorSearchOptions<ProductRecord>());

    var resultCount = 0;
    await foreach (var result in searchResults)
    {
        resultCount++;
        Console.WriteLine($"{resultCount}. {result.Record.Name} (Score: {result.Score:F4})");
        Console.WriteLine($"   Category: {result.Record.Category} | Price: ${result.Record.Price}");
        Console.WriteLine($"   Description: {result.Record.Description}");
        Console.WriteLine();
    }

    if (resultCount == 0)
    {
        Console.WriteLine("   No results found.\n");
    }
}

// Helper method to perform filtered semantic search
static async Task PerformFilteredSearch(
    VectorStoreCollection<string, ProductRecord> collection,
    string query,
    decimal maxPrice)
{
    Console.WriteLine($"Query: \"{query}\"");
    Console.WriteLine($"Filter: Price <= ${maxPrice}");
    Console.WriteLine($"Results:");
    Console.WriteLine(new string('-', 80));

    var searchResults = collection.SearchAsync(
        query,
        top: 5,
        new Microsoft.Extensions.VectorData.VectorSearchOptions<ProductRecord>
        {
            Filter = p => p.Price <= maxPrice
        });

    var resultCount = 0;
    await foreach (var result in searchResults)
    {
        resultCount++;
        Console.WriteLine($"{resultCount}. {result.Record.Name} (Score: {result.Score:F4})");
        Console.WriteLine($"   Category: {result.Record.Category} | Price: ${result.Record.Price}");
        Console.WriteLine($"   Description: {result.Record.Description}");
        Console.WriteLine();
    }

    if (resultCount == 0)
    {
        Console.WriteLine("   No results found matching the criteria.\n");
    }
}
