namespace ConsoleApp;

/// <summary>
/// Configuration options for MongoDB connection.
/// </summary>
public sealed class MongoDbOptions
{
    /// <summary>
    /// MongoDB connection string.
    /// Default: mongodb://localhost:27017
    /// </summary>
    public string ConnectionString { get; set; } = "mongodb://localhost:27017";

    /// <summary>
    /// The name of the MongoDB database to use.
    /// Default: vectordb
    /// </summary>
    public string DatabaseName { get; set; } = "vectordb";
}
