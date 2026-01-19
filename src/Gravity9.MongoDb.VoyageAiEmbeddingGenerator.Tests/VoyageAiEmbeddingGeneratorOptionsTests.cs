namespace Gravity9.MongoDb.VoyageAiEmbeddingGenerator.Tests;

using AwesomeAssertions;

using Xunit;

public class VoyageAiEmbeddingGeneratorOptionsTests
{
    [Fact]
    public void DefaultOptions_ShouldHaveExpectedDefaults()
    {
        // Arrange & Act
        var options = new VoyageAiEmbeddingGeneratorOptions();

        // Assert
        options.BaseUrl.Should().Be("https://ai.mongodb.com/v1/");
        options.Model.Should().Be("voyage-4-large");
        options.Truncation.Should().BeTrue();
        options.OutputDtype.Should().Be("float");
        options.RequestTimeout.Should().Be(TimeSpan.FromSeconds(30));
        options.MaxRetries.Should().Be(3);
        options.RetryDelayMilliseconds.Should().Be(1000);
    }

    [Fact]
    public void Options_ShouldAllowCustomConfiguration()
    {
        // Arrange & Act
        var options = new VoyageAiEmbeddingGeneratorOptions
        {
            ApiKey = "test-api-key",
            Model = "voyage-4",
            InputType = "query",
            OutputDimension = 512,
            Truncation = false
        };

        // Assert
        options.ApiKey.Should().Be("test-api-key");
        options.Model.Should().Be("voyage-4");
        options.InputType.Should().Be("query");
        options.OutputDimension.Should().Be(512);
        options.Truncation.Should().BeFalse();
    }
}
