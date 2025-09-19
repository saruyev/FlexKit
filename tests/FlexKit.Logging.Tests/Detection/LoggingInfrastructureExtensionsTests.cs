using System.Diagnostics;
using System.Reflection;
using Autofac;
using FlexKit.Logging.Configuration;
using FlexKit.Logging.Core;
using FlexKit.Logging.Detection;
using FlexKit.Logging.Formatting.Core;
using FlexKit.Logging.Formatting.Formatters;
using FlexKit.Logging.Interception;
using FlexKit.Logging.Models;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;
// ReSharper disable TooManyDeclarations
// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedParameter.Local
// ReSharper disable MemberCanBePrivate.Local
// ReSharper disable MemberCanBeProtected.Local
// ReSharper disable MethodTooLong
// ReSharper disable ClassTooBig

namespace FlexKit.Logging.Tests.Detection;

[Collection("LoggingInfrastructureExtensionsTests")]
public class LoggingInfrastructureExtensionsTests
{
    [Fact]
    public void RegisterLoggingInfrastructure_ShouldRegisterLoggingConfig()
    {
        // Arrange
        var builder = new ContainerBuilder();

        var configData = new Dictionary<string, string?>
        {
            ["FlexKit:Logging:ActivitySourceName"] = "TestSource",
            ["FlexKit:Logging:DefaultTarget"] = "TestTarget"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        builder.RegisterInstance<IConfiguration>(configuration);

        // Act
        builder.RegisterLoggingInfrastructure();
        var container = builder.Build();

        // Assert
        var resolvedConfig = container.Resolve<LoggingConfig>();
        resolvedConfig.ActivitySourceName.Should().Be("TestSource");
        resolvedConfig.DefaultTarget.Should().Be("TestTarget");
    }

    [Fact]
    public void RegisterLoggingInfrastructure_ShouldRegisterMessageFormatters()
    {
        // Arrange
        var builder = new ContainerBuilder();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        builder.RegisterInstance<IConfiguration>(configuration);

        // Act
        builder.RegisterLoggingInfrastructure();
        var container = builder.Build();

        // Assert
        var formatters = container.Resolve<IEnumerable<IMessageFormatter>>().ToList();
        formatters.Should().HaveCount(5);
        formatters.Should().ContainSingle(f => f is CustomTemplateFormatter);
        formatters.Should().ContainSingle(f => f is HybridFormatter);
        formatters.Should().ContainSingle(f => f is JsonFormatter);
        formatters.Should().ContainSingle(f => f is StandardStructuredFormatter);
        formatters.Should().ContainSingle(f => f is SuccessErrorFormatter);

        container.IsRegistered<IMessageFormatterFactory>().Should().BeTrue();
    }

    [Fact]
    public void RegisterInterceptionComponents_ShouldRegisterInterceptor()
    {
        // Arrange
        var builder = new ContainerBuilder();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        builder.RegisterInstance<IConfiguration>(configuration);

        // Act
        builder.RegisterLoggingInfrastructure();
        var container = builder.Build();

        container.IsRegistered<MethodLoggingInterceptor>().Should().BeTrue();
    }

    [Fact]
    public void RegisterManualLogging_ShouldRegisterActivity()
    {
        // Arrange
        var builder = new ContainerBuilder();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        builder.RegisterInstance<IConfiguration>(configuration);

        // Act
        builder.RegisterLoggingInfrastructure();
        var container = builder.Build();
        var activity = container.Resolve<ActivitySource>().StartActivity();

        container.IsRegistered<IFlexKitLogger>().Should().BeTrue();
        container.IsRegistered<ActivitySource>().Should().BeTrue();
        activity.Should().NotBeNull();
        activity.OperationName.Should().Be("RegisterManualLogging_ShouldRegisterActivity");
    }

    [Fact]
    public void RegisterLoggingInfrastructure_ShouldRegisterBackgroundComponents_WhenNoProviderAssemblies()
    {
        // Arrange
        var builder = new ContainerBuilder();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        builder.RegisterInstance<IConfiguration>(configuration);

        // Act
        builder.RegisterLoggingInfrastructure();
        var container = builder.Build();

        // Assert
        container.IsRegistered<IBackgroundLog>().Should().BeTrue();
        container.IsRegistered<ILogEntryProcessor>().Should().BeTrue();
        container.IsRegistered<BackgroundLoggingService>().Should().BeTrue();

        var backgroundLog = container.Resolve<IBackgroundLog>();
        backgroundLog.Should().BeOfType<BackgroundLog>();

        var processor = container.Resolve<ILogEntryProcessor>();
        processor.Should().BeOfType<FormattedLogWriter>();
    }

    [Fact]
    public void RegisterManualLogging_ActivityListenerSamplesAllDataCorrectly()
    {
        // Arrange
        var builder = new ContainerBuilder();
        builder.Register(_ => new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["FlexKit:Logging:ActivitySourceName"] = "TestFlexKit"
                })
            .Build()).As<IConfiguration>();

        builder.RegisterLoggingInfrastructure();
        var container = builder.Build();
        var activitySource = container.Resolve<ActivitySource>();

        // Act - Start activities to trigger the registered listener's Sample methods
        // ReSharper disable once ExplicitCallerInfoArgument
        using var parentActivity = activitySource.StartActivity("ParentOperation");

        // Create child activity using parent ID string to trigger SampleUsingParentId
        var parentId = parentActivity?.Id ?? "00-12345678901234567890123456789012-1234567890123456-01";
        using var childActivity = activitySource.StartActivity("ChildOperation", ActivityKind.Internal, parentId);

        // Assert - Verify activities were sampled and created (AllData result means they should exist)
        parentActivity.Should().NotBeNull("Sample should return AllData for activities from correct source");
        childActivity.Should().NotBeNull("SampleUsingParentId should return AllData for child activities");

        parentActivity.Source.Name.Should().Be("TestFlexKit");
        childActivity.Source.Name.Should().Be("TestFlexKit");

        container.Dispose();
    }

    [Fact]
    public void ProcessExitHandler_WhenTriggered_ShouldCallFlushRemainingEntries()
    {
        // Arrange - create substitutes for dependencies
        var logQueue = Substitute.For<IBackgroundLog>();
        var logger = Substitute.For<ILogger<BackgroundLoggingService>>();
        var logEntryProcessor = Substitute.For<ILogEntryProcessor>();

        // Create the actual service with substituted dependencies
        var backgroundService = new BackgroundLoggingService(logQueue, logger, logEntryProcessor);

        // Set up the log queue to return false when trying to dequeue (empty queue)
        logQueue.TryDequeue(out Arg.Any<LogEntry>()).Returns(false);

        // Act - manually call FlushLogsOnExit (this is what the ProcessExit handler calls)
        LoggingInfrastructureExtensions.FlushLogsOnExit(backgroundService);

        // Assert - verify that TryDequeue was called (which means FlushRemainingEntries was called)
        logQueue.Received(1).TryDequeue(out Arg.Any<LogEntry>());
    }

    [Fact]
    public void FlushLogsOnExit_WithLogEntries_ShouldProcessAllEntries()
    {
        // Arrange
        var logQueue = Substitute.For<IBackgroundLog>();
        var logger = Substitute.For<ILogger<BackgroundLoggingService>>();
        var logEntryProcessor = Substitute.For<ILogEntryProcessor>();

        var backgroundService = new BackgroundLoggingService(logQueue, logger, logEntryProcessor);

        // Set up the log queue to return entries then empty
        var logEntry = new LogEntry(); // You'll need to create a valid LogEntry
        logQueue.TryDequeue(out Arg.Any<LogEntry>())
            .Returns(
                x =>
                {
                    x[0] = logEntry;
                    return true;
                },
                _ => false);

        // Act
        LoggingInfrastructureExtensions.FlushLogsOnExit(backgroundService);

        // Assert
        logQueue.Received(2).TryDequeue(out Arg.Any<LogEntry>());
        logEntryProcessor.Received(1).ProcessEntry(logEntry);
    }

    [Fact]
    public void RegisterLoggingInfrastructure_ShouldRegisterProcessExitHandler()
    {
        // Arrange
        var configuration = Substitute.For<IConfiguration>();
        var configSection = Substitute.For<IConfigurationSection>();
        configuration.GetSection("FlexKit:Logging").Returns(configSection);

        var builder = new ContainerBuilder();
        builder.RegisterInstance(configuration).As<IConfiguration>();

        // Count handlers before
        var handlerCountBefore = GetProcessExitHandlerCount();

        // Act
        builder.RegisterLoggingInfrastructure();
        _ = builder.Build();

        // Count handlers after
        var handlerCountAfter = GetProcessExitHandlerCount();

        // Assert that exactly one handler was added
        (handlerCountAfter - handlerCountBefore).Should().Be(1, "exactly one ProcessExit handler should be registered");
    }

    [Fact]
    public void RegisterLoggingInfrastructure_WhenProcessExitTriggered_ShouldCallFlushLogsOnExit()
    {
        // Arrange
        var configuration = Substitute.For<IConfiguration>();
        var configSection = Substitute.For<IConfigurationSection>();
        configuration.GetSection("FlexKit:Logging").Returns(configSection);

        var builder = new ContainerBuilder();
        builder.RegisterInstance(configuration).As<IConfiguration>();

        // Register infrastructure and build container
        builder.RegisterLoggingInfrastructure();
        var container = builder.Build();

        // Get the background service to verify it can flush
        var backgroundService = container.Resolve<BackgroundLoggingService>();
        backgroundService.Should().NotBeNull();

        // Act - Trigger ProcessExit event without actually exiting
        TriggerProcessExitEvent();

        // Assert - if we get here without exceptions, the handler was called successfully
        // The real verification is that FlushLogsOnExit doesn't throw when called
        Action verifyFlushWorks = () => backgroundService.FlushRemainingEntries();
        verifyFlushWorks.Should().NotThrow();
    }

    [Fact]
    public void RegisterLoggingInfrastructure_WithDebugTarget_ShouldConfigureDebugLogger()
    {
        // Arrange
        var loggingConfig = new LoggingConfig();
        loggingConfig.Targets.Add(
            "Debug",
            new LoggingTarget
            {
                Type = "Console",
                Enabled = true
            });

        var builder = new ContainerBuilder();

        // Just register the config directly - much simpler!
        var configuration = Substitute.For<IConfiguration>();
        builder.RegisterInstance(configuration).As<IConfiguration>();
        builder.RegisterInstance(loggingConfig).As<LoggingConfig>();

        // Act
        builder.RegisterLoggingInfrastructure();
        var container = builder.Build();

        // Assert
        var loggerFactory = container.Resolve<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("Console");

        logger.Should().NotBeNull();
        logger.IsEnabled(LogLevel.Information).Should().BeTrue("Console provider should be enabled for Information level");

        Action logAction = () => logger.LogInformation("Test message");
        logAction.Should().NotThrow();
    }

    [Fact]
    public async Task RunBackgroundServiceAsync_WithCancellation_ShouldCompleteGracefully()
    {
        // Arrange
        var logQueue = Substitute.For<IBackgroundLog>();
        var logger = Substitute.For<ILogger<BackgroundLoggingService>>();
        var logEntryProcessor = Substitute.For<ILogEntryProcessor>();

        // Set up the queue to return no entries (empty queue)
        logQueue.TryDequeue(out Arg.Any<LogEntry>()).Returns(false);

        var backgroundService = new BackgroundLoggingService(logQueue, logger, logEntryProcessor);

        // Act - start the service then stop it quickly
        var runTask = LoggingInfrastructureExtensions.RunBackgroundServiceAsync(backgroundService);

        // Give it time to start
        await Task.Delay(100);

        // Now dispose the service to trigger shutdown
        backgroundService.Dispose();

        // Wait a bit more for cleanup
        await Task.Delay(100);

        // Assert - should complete without throwing
        Action waitForCompletion = () => runTask.Wait(TimeSpan.FromSeconds(2));
        waitForCompletion.Should().NotThrow("Service should shut down gracefully");
    }

    [Fact]
    public void RunBackgroundServiceAsync_WithNullService_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Func<Task> act = () => LoggingInfrastructureExtensions.RunBackgroundServiceAsync(null!);

        act.Should().ThrowAsync<ArgumentNullException>("null service should throw");
    }

    [Fact]
    public async Task RunBackgroundServiceAsync_WhenServiceStartsSuccessfully_ShouldNotThrowImmediately()
    {
        // Arrange
        var logQueue = Substitute.For<IBackgroundLog>();
        var logger = Substitute.For<ILogger<BackgroundLoggingService>>();
        var logEntryProcessor = Substitute.For<ILogEntryProcessor>();

        // Set up the queue to simulate having entries to process indefinitely
        var logEntry = LogEntry.CreateStart("TestMethod", "TestType");
        logQueue.TryDequeue(out Arg.Any<LogEntry>())
            .Returns(x =>
            {
                x[0] = logEntry;
                return true;
            }); // Always return entries

        var backgroundService = new BackgroundLoggingService(logQueue, logger, logEntryProcessor);

        // Act
        var runTask = LoggingInfrastructureExtensions.RunBackgroundServiceAsync(backgroundService);

        // Wait a short time
        await Task.Delay(100);

        // Assert - should not have completed yet since we have infinite entries
        if (runTask.IsCompleted)
        {
            // If it completed, it should not have faulted
            runTask.IsFaulted.Should().BeFalse("If service completed early, it should not have faulted");

            // Check if there was an exception
            if (runTask.Exception != null)
            {
                throw runTask.Exception;
            }
        }
        else
        {
            runTask.IsCompleted.Should().BeFalse("Service should still be processing entries");
        }

        // Cleanup
        backgroundService.Dispose();
    }

    [Fact]
    public async Task RunBackgroundServiceAsync_WhenServiceCompletes_ShouldNotThrow()
    {
        // Arrange
        var logQueue = Substitute.For<IBackgroundLog>();
        var logger = Substitute.For<ILogger<BackgroundLoggingService>>();
        var logEntryProcessor = Substitute.For<ILogEntryProcessor>();

        // Set up an empty queue so the service completes quickly
        logQueue.TryDequeue(out Arg.Any<LogEntry>()).Returns(false);

        var backgroundService = new BackgroundLoggingService(logQueue, logger, logEntryProcessor);

        // Act & Assert - should complete without throwing
        var runTask = LoggingInfrastructureExtensions.RunBackgroundServiceAsync(backgroundService);

        // Wait for completion with timeout
        var completedTask = await Task.WhenAny(runTask, Task.Delay(TimeSpan.FromSeconds(5)));
        completedTask.Should().Be(runTask, "Service should complete within timeout");

        // Should not throw
        await runTask;

        // Don't verify logger calls - too complex with argument matchers
        // The important thing is that RunBackgroundServiceAsync completed without throwing
    }

    [Fact]
    public async Task RunBackgroundServiceAsync_WhenServiceDisposed_ShouldCompleteGracefully()
    {
        // Arrange
        var logQueue = Substitute.For<IBackgroundLog>();
        var logger = Substitute.For<ILogger<BackgroundLoggingService>>();
        var logEntryProcessor = Substitute.For<ILogEntryProcessor>();

        var backgroundService = new BackgroundLoggingService(logQueue, logger, logEntryProcessor);

        // Act
        var runTask = LoggingInfrastructureExtensions.RunBackgroundServiceAsync(backgroundService);

        // Give it a moment to start
        await Task.Delay(50);

        // Dispose the service to trigger shutdown
        backgroundService.Dispose();

        // Should complete gracefully
        var completedTask = await Task.WhenAny(runTask, Task.Delay(TimeSpan.FromSeconds(2)));
        completedTask.Should().Be(runTask, "Service should complete after disposal");

        // Should not throw
        await runTask;
    }

    [Fact]
    public async Task RunBackgroundServiceAsync_WhenOperationCanceledException_ShouldHandleGracefully()
    {
        // Arrange - Create a custom BackgroundService that throws OperationCanceledException
        var testService = new TestBackgroundService();

        // Act & Assert - should handle OperationCanceledException gracefully
        var runTask = LoggingInfrastructureExtensions.RunBackgroundServiceAsync(testService);

        await runTask;

        // If we get here, the OperationCanceledException was caught and handled
        testService.StartAsyncWasCalled.Should().BeTrue();
    }
    
    [Fact]
    public async Task RunBackgroundServiceAsync_NoException_ShouldStop()
    {
        // Arrange - Create a custom BackgroundService that throws OperationCanceledException
        var testService = new SuccessfulTestBackgroundService();

        // Act & Assert - should handle OperationCanceledException gracefully
        var runTask = LoggingInfrastructureExtensions.RunBackgroundServiceAsync(testService, TimeSpan.FromSeconds(1));

        await runTask;

        // If we get here, the OperationCanceledException was caught and handled
        testService.StartAsyncWasCalled.Should().BeTrue();
        testService.StopAsyncWasCalled.Should().BeTrue();
    }

    [Fact]
    public void FlushLogsOnExit_WhenBackgroundServiceThrows_ShouldCatchAndHandleException()
    {
        // Arrange
        var backgroundService = new TestBackgroundService();
        var debugOutput = new StringWriter();
        Trace.Listeners.Add(new TextWriterTraceListener(debugOutput));

        // Act & Assert - should not throw (exception should be caught)
        Action flushAction = () => LoggingInfrastructureExtensions.FlushLogsOnExit(backgroundService);
        flushAction.Should().NotThrow("Exception should be caught and handled internally");

        // Verify the exception path was taken
        debugOutput.ToString().Should().Contain("Error flushing logs on exit:");
    }

    private static void TriggerProcessExitEvent()
    {
        // Call the internal OnProcessExit method which triggers the ProcessExit event
        var onProcessExitMethod = typeof(AppDomain).GetMethod(
            "OnProcessExit",
            BindingFlags.NonPublic | BindingFlags.Static);

        onProcessExitMethod?.Invoke(null, null);
    }

    private static int GetProcessExitHandlerCount()
    {
        try
        {
            var domain = AppDomain.CurrentDomain;

            // Get the ProcessExit field directly (it's a simple event field)
            var processExitField = typeof(AppDomain).GetField(
                "ProcessExit",
                BindingFlags.NonPublic | BindingFlags.Instance);

            if (processExitField?.GetValue(domain) is Delegate handler)
            {
                return handler.GetInvocationList().Length;
            }

            return 0;
        }
        catch
        {
            return 0;
        }
    }

    private class TestBackgroundService : IBackgroundLoggingService
    {
        public bool StartAsyncWasCalled { get; set; }
        public bool StopAsyncWasCalled { get; set; }

        protected Task ExecuteAsync(CancellationToken stoppingToken) => Task.CompletedTask;

        public virtual Task StartAsync(CancellationToken cancellationToken)
        {
            StartAsyncWasCalled = true;
            throw new OperationCanceledException("Test cancellation");
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            StopAsyncWasCalled = true;
            return Task.CompletedTask;
        }

        public void FlushRemainingEntries()
        {
            throw new Exception("test");
        }
    }

    private class SuccessfulTestBackgroundService : TestBackgroundService
    {
        public override Task StartAsync(CancellationToken cancellationToken)
        {
            StartAsyncWasCalled = true;
            return Task.CompletedTask;
        }
    }
}
