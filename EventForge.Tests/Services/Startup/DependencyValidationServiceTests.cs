using EventForge.Server.Startup;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace EventForge.Tests.Services.Startup;

/// <summary>
/// Tests for DependencyValidationService to verify circular dependency detection
/// </summary>
[Trait("Category", "Unit")]
public class DependencyValidationServiceTests
{
    private readonly Mock<ILogger> _loggerMock;

    public DependencyValidationServiceTests()
    {
        _loggerMock = new Mock<ILogger>();
    }

    [Fact]
    public void ValidateDependencies_NoDependencies_DoesNotThrow()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<SimpleServiceA>();
        var provider = services.BuildServiceProvider();

        // Act & Assert
        var exception = Record.Exception(() =>
            DependencyValidationService.ValidateDependencies(provider, _loggerMock.Object));

        Assert.Null(exception);
    }

    [Fact]
    public void ValidateDependencies_SimpleDependency_DoesNotThrow()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<SimpleServiceA>();
        services.AddSingleton<ServiceDependsOnA>();
        var provider = services.BuildServiceProvider();

        // Act & Assert
        var exception = Record.Exception(() =>
            DependencyValidationService.ValidateDependencies(provider, _loggerMock.Object));

        Assert.Null(exception);
    }

    [Fact]
    public void ValidateDependencies_SimpleCircularDependency_ThrowsException()
    {
        // Arrange: A -> B -> A
        var services = new ServiceCollection();
        services.AddSingleton<ICircularServiceA, CircularServiceA>();
        services.AddSingleton<ICircularServiceB, CircularServiceB>();
        var provider = services.BuildServiceProvider();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            DependencyValidationService.ValidateDependencies(provider, _loggerMock.Object));

        Assert.Contains("Circular dependencies detected", exception.Message);
        Assert.Contains("ICircularServiceA", exception.Message);
        Assert.Contains("ICircularServiceB", exception.Message);
    }

    [Fact]
    public void ValidateDependencies_ComplexCircularDependency_ThrowsException()
    {
        // Arrange: A -> B -> C -> A
        var services = new ServiceCollection();
        services.AddSingleton<IComplexCircularA, ComplexCircularA>();
        services.AddSingleton<IComplexCircularB, ComplexCircularB>();
        services.AddSingleton<IComplexCircularC, ComplexCircularC>();
        var provider = services.BuildServiceProvider();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            DependencyValidationService.ValidateDependencies(provider, _loggerMock.Object));

        Assert.Contains("Circular dependencies detected", exception.Message);
        Assert.Contains("CYCLE", exception.Message);
    }

    [Fact]
    public void ValidateDependencies_ChainWithoutCycle_DoesNotThrow()
    {
        // Arrange: A -> B -> C (no cycle)
        var services = new ServiceCollection();
        services.AddSingleton<ChainServiceA>();
        services.AddSingleton<ChainServiceB>();
        services.AddSingleton<ChainServiceC>();
        var provider = services.BuildServiceProvider();

        // Act & Assert
        var exception = Record.Exception(() =>
            DependencyValidationService.ValidateDependencies(provider, _loggerMock.Object));

        Assert.Null(exception);
    }

    [Fact]
    public void ValidateDependencies_WithLogger_LogsProgress()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<SimpleServiceA>();
        var provider = services.BuildServiceProvider();

        var loggerMock = new Mock<ILogger>();

        // Act
        DependencyValidationService.ValidateDependencies(provider, loggerMock.Object);

        // Assert - Verify logging occurred
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Starting dependency validation")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public void ValidateDependencies_ErrorMessage_ContainsSolutionSuggestions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<ICircularServiceA, CircularServiceA>();
        services.AddSingleton<ICircularServiceB, CircularServiceB>();
        var provider = services.BuildServiceProvider();

        // Act
        var exception = Assert.Throws<InvalidOperationException>(() =>
            DependencyValidationService.ValidateDependencies(provider, _loggerMock.Object));

        // Assert - Error message contains helpful suggestions
        Assert.Contains("SOLUTION", exception.Message);
        Assert.Contains("Introduce an interface", exception.Message);
        Assert.Contains("Facade pattern", exception.Message);
    }

    #region Test Service Classes

    // Simple services without dependencies
    private class SimpleServiceA { }

    private class ServiceDependsOnA
    {
        public ServiceDependsOnA(SimpleServiceA serviceA) { }
    }

    // Circular dependency: A -> B -> A
    private interface ICircularServiceA { }
    private interface ICircularServiceB { }

    private class CircularServiceA : ICircularServiceA
    {
        public CircularServiceA(ICircularServiceB serviceB) { }
    }

    private class CircularServiceB : ICircularServiceB
    {
        public CircularServiceB(ICircularServiceA serviceA) { }
    }

    // Complex circular dependency: A -> B -> C -> A
    private interface IComplexCircularA { }
    private interface IComplexCircularB { }
    private interface IComplexCircularC { }

    private class ComplexCircularA : IComplexCircularA
    {
        public ComplexCircularA(IComplexCircularB serviceB) { }
    }

    private class ComplexCircularB : IComplexCircularB
    {
        public ComplexCircularB(IComplexCircularC serviceC) { }
    }

    private class ComplexCircularC : IComplexCircularC
    {
        public ComplexCircularC(IComplexCircularA serviceA) { }
    }

    // Chain without cycle: A -> B -> C
    private class ChainServiceA
    {
        public ChainServiceA(ChainServiceB serviceB) { }
    }

    private class ChainServiceB
    {
        public ChainServiceB(ChainServiceC serviceC) { }
    }

    private class ChainServiceC { }

    #endregion
}
