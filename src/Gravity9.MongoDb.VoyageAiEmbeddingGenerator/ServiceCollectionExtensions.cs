namespace Gravity9.MongoDb.VoyageAiEmbeddingGenerator;

using Gravity9.MongoDb.VoyageAiEmbeddingGenerator.Services;

using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

/// <summary>
/// Extension methods for registering VoyageAI embedding generator with the dependency injection container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the VoyageAI embedding generator to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="apiKey">The VoyageAI API key.</param>
    /// <param name="configureOptions">Optional action to configure additional options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddVoyageAiEmbeddingGenerator(
        this IServiceCollection services,
        string apiKey,
        Action<VoyageAiEmbeddingGeneratorOptions>? configureOptions = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);

        var options = new VoyageAiEmbeddingGeneratorOptions
        {
            ApiKey = apiKey
        };

        configureOptions?.Invoke(options);

        return AddVoyageAiEmbeddingGenerator(services, options);
    }

    /// <summary>
    /// Adds the VoyageAI embedding generator to the service collection with pre-configured options.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="options">The configured options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddVoyageAiEmbeddingGenerator(
        this IServiceCollection services,
        VoyageAiEmbeddingGeneratorOptions options)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            throw new ArgumentException("API key is required.", nameof(options));
        }

        // Register the options as a singleton
        services.TryAddSingleton(options);

        // Register HttpClient for VoyageAI API
        services.AddHttpClient<IVoyageAiApiClient, VoyageAiApiClient>()
            .ConfigureHttpClient((serviceProvider, client) =>
            {
                var opts = serviceProvider.GetRequiredService<VoyageAiEmbeddingGeneratorOptions>();
                client.BaseAddress = new Uri(opts.BaseUrl);
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {opts.ApiKey}");
                client.Timeout = opts.RequestTimeout;
            });

        // Register the embedding generator
        services.TryAddSingleton<IEmbeddingGenerator<string, Embedding<float>>, VoyageAiEmbeddingGenerator>();

        return services;
    }
}
