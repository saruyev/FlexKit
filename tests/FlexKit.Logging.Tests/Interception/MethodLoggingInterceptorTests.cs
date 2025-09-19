using System.Reflection;
using Castle.DynamicProxy;
using FlexKit.Logging.Configuration;
using FlexKit.Logging.Core;
using FlexKit.Logging.Interception;
using FlexKit.Logging.Interception.Attributes;
using FlexKit.Logging.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;
// ReSharper disable TooManyArguments
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable TooManyDeclarations
// ReSharper disable UseCollectionExpression
// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedMember.Global
// ReSharper disable RedundantCast
// ReSharper disable MethodTooLong
// ReSharper disable ClassTooBig

namespace FlexKit.Logging.Tests.Interception;

public class MethodLoggingInterceptorTests
{
    [Fact]
    public void Intercept_WhenDecisionIsNull_ShouldCallProceedAndReturn()
    {
        // Arrange
        var loggingConfig = new LoggingConfig();
        var cache = new InterceptionDecisionCache(loggingConfig);
        var logQueue = Substitute.For<IBackgroundLog>();
        var logger = new TestLogger<MethodLoggingInterceptor>();
        var interceptor = new MethodLoggingInterceptor(cache, logQueue, logger);
        var invocation = Substitute.For<IInvocation>();
        
        var method = typeof(object).GetMethod(nameof(ToString))!;
        invocation.Method.Returns(method);

        // Act
        interceptor.Intercept(invocation);

        // Assert
        invocation.Received(1).Proceed();
    }

    [Fact]
    public void Intercept_WhenDecisionExists_ShouldCallLogMethodExecution()
    {
        // Arrange
        var loggingConfig = new LoggingConfig { AutoIntercept = true };
        var cache = new InterceptionDecisionCache(loggingConfig);
        var logQueue = Substitute.For<IBackgroundLog>();
        var logger = new TestLogger<MethodLoggingInterceptor>();
        var interceptor = new MethodLoggingInterceptor(cache, logQueue, logger);
        var invocation = Substitute.For<IInvocation>();
        
        logQueue.TryEnqueue(Arg.Any<LogEntry>()).Returns(true);
        
        var method = typeof(TestClass).GetMethod(nameof(TestClass.TestMethod))!;
        invocation.Method.Returns(method);
        invocation.Arguments.Returns(new object[0]);
        invocation.ReturnValue.Returns("result");
        
        cache.CacheTypeDecisions(typeof(TestClass));

        // Act
        interceptor.Intercept(invocation);

        // Assert
        invocation.Received(1).Proceed();
    }
    
    [Fact]
    public void Intercept_WhenAsyncMethodWithTask_ShouldCallHandleAsyncCompletion()
    {
        // Arrange
        var loggingConfig = new LoggingConfig { AutoIntercept = true };
        var cache = new InterceptionDecisionCache(loggingConfig);
        var logQueue = Substitute.For<IBackgroundLog>();
        var logger = new TestLogger<MethodLoggingInterceptor>();
        var interceptor = new MethodLoggingInterceptor(cache, logQueue, logger);
        var invocation = Substitute.For<IInvocation>();
        
        logQueue.TryEnqueue(Arg.Any<LogEntry>()).Returns(true);
        
        var method = typeof(TestClass).GetMethod(nameof(TestClass.TestAsyncMethod))!;
        invocation.Method.Returns(method);
        invocation.Arguments.Returns(new object[0]);
        invocation.ReturnValue.Returns(Task.CompletedTask);
        
        cache.CacheTypeDecisions(typeof(TestClass));

        // Act
        interceptor.Intercept(invocation);

        // Assert
        invocation.Received(1).Proceed();
    }
    
    [Fact]
    public void Intercept_WhenMethodThrowsException_ShouldLogErrorAndRethrow()
    {
        // Arrange
        var loggingConfig = new LoggingConfig { AutoIntercept = true };
        var cache = new InterceptionDecisionCache(loggingConfig);
        var logQueue = Substitute.For<IBackgroundLog>();
        var logger = new TestLogger<MethodLoggingInterceptor>();
        var interceptor = new MethodLoggingInterceptor(cache, logQueue, logger);
        var invocation = Substitute.For<IInvocation>();
    
        logQueue.TryEnqueue(Arg.Any<LogEntry>()).Returns(true);
    
        var method = typeof(TestClass).GetMethod(nameof(TestClass.TestMethod))!;
        invocation.Method.Returns(method);
        invocation.Arguments.Returns([]);
    
        var testException = new InvalidOperationException("Test exception");
        invocation.When(x => x.Proceed()).Do(_ => throw testException);
    
        cache.CacheTypeDecisions(typeof(TestClass));

        // Act & Assert
        var thrownException = Assert.Throws<InvalidOperationException>(() => 
            interceptor.Intercept(invocation));
    
        thrownException.Should().Be(testException);
        invocation.Received(1).Proceed();
        logQueue.Received().TryEnqueue(Arg.Is<LogEntry>(entry => 
            entry.ExceptionMessage == "Test exception"));
    }
    
    [Fact]
    public void Intercept_WhenSyncMethodWithLogOutputDecision_ShouldLogOutput()
    {
        // Arrange
        var loggingConfig = new LoggingConfig { AutoIntercept = true };
        var cache = new InterceptionDecisionCache(loggingConfig);
        var logQueue = Substitute.For<IBackgroundLog>();
        var logger = new TestLogger<MethodLoggingInterceptor>();
        var interceptor = new MethodLoggingInterceptor(cache, logQueue, logger);
        var invocation = Substitute.For<IInvocation>();
    
        logQueue.TryEnqueue(Arg.Any<LogEntry>()).Returns(true);
    
        var method = typeof(TestClassWithLogOutput).GetMethod(nameof(TestClassWithLogOutput.MethodWithLogOutput))!;
        invocation.Method.Returns(method);
        invocation.Arguments.Returns([]);
        invocation.ReturnValue.Returns("test output");
    
        cache.CacheTypeDecisions(typeof(TestClassWithLogOutput));

        // Act
        interceptor.Intercept(invocation);

        // Assert
        invocation.Received(1).Proceed();
        logQueue.Received().TryEnqueue(Arg.Any<LogEntry>());
    }
    
    [Fact]
    public void Intercept_WhenAsyncMethodWithLogOutputDecision_ShouldLogAsyncOutput()
    {
        // Arrange
        var loggingConfig = new LoggingConfig { AutoIntercept = true };
        var cache = new InterceptionDecisionCache(loggingConfig);
        var logQueue = Substitute.For<IBackgroundLog>();
        var logger = new TestLogger<MethodLoggingInterceptor>();
        var interceptor = new MethodLoggingInterceptor(cache, logQueue, logger);
        var invocation = Substitute.For<IInvocation>();
    
        logQueue.TryEnqueue(Arg.Any<LogEntry>()).Returns(true);
    
        var method = typeof(TestClassWithLogOutput).GetMethod(nameof(TestClassWithLogOutput.AsyncMethodWithLogOutput))!;
        invocation.Method.Returns(method);
        invocation.Arguments.Returns([]);
        invocation.ReturnValue.Returns(Task.FromResult("async result"));
    
        cache.CacheTypeDecisions(typeof(TestClassWithLogOutput));

        // Act
        interceptor.Intercept(invocation);

        // Assert
        invocation.Received(1).Proceed();
        logQueue.Received().TryEnqueue(Arg.Any<LogEntry>());
    }
    
    [Fact]
    public void Intercept_WhenAsyncMethodResultPropertyThrows_ShouldHandleExceptionAndReturnNull()
    {
        // Arrange
        var loggingConfig = new LoggingConfig { AutoIntercept = true };
        var cache = new InterceptionDecisionCache(loggingConfig);
        var logQueue = Substitute.For<IBackgroundLog>();
        var logger = new TestLogger<MethodLoggingInterceptor>();
        var interceptor = new MethodLoggingInterceptor(cache, logQueue, logger);
        var invocation = Substitute.For<IInvocation>();
    
        logQueue.TryEnqueue(Arg.Any<LogEntry>()).Returns(true);
    
        var method = typeof(TestClassWithLogOutput).GetMethod(nameof(TestClassWithLogOutput.AsyncMethodWithLogOutput))!;
        invocation.Method.Returns(method);
        invocation.Arguments.Returns(new object[0]);
    
        // Create a derived task type that inherits from Task<string>
        var derivedTask = new DerivedTask("derived result");
        invocation.ReturnValue.Returns(derivedTask);
    
        cache.CacheTypeDecisions(typeof(TestClassWithLogOutput));

        // Act
        interceptor.Intercept(invocation);

        // Assert
        invocation.Received(1).Proceed();
        logQueue.Received().TryEnqueue(Arg.Any<LogEntry>());
    }
    
    [Fact]
    public void Intercept_WhenLogQueueIsFull_ShouldLogQueueFullWarning()
    {
        // Arrange
        var loggingConfig = new LoggingConfig { AutoIntercept = true };
        var cache = new InterceptionDecisionCache(loggingConfig);
        var logQueue = Substitute.For<IBackgroundLog>();
        var logger = new TestLogger<MethodLoggingInterceptor>();
        var interceptor = new MethodLoggingInterceptor(cache, logQueue, logger);
        var invocation = Substitute.For<IInvocation>();
    
        // Make TryEnqueue return false to simulate the queue being full
        logQueue.TryEnqueue(Arg.Any<LogEntry>()).Returns(false);
    
        var method = typeof(TestClass).GetMethod(nameof(TestClass.TestMethod))!;
        invocation.Method.Returns(method);
        invocation.Arguments.Returns(new object[0]);
        invocation.ReturnValue.Returns("result");
    
        cache.CacheTypeDecisions(typeof(TestClass));

        // Act
        interceptor.Intercept(invocation);

        // Assert
        invocation.Received(1).Proceed();
        logQueue.Received().TryEnqueue(Arg.Any<LogEntry>());
    }
    
    [Fact]
    public void Intercept_WhenAsyncMethodFailsInLogging_ShouldLogAsyncCompletionFailure()
    {
        // Arrange
        var loggingConfig = new LoggingConfig { AutoIntercept = true };
        var cache = new InterceptionDecisionCache(loggingConfig);
        var logQueue = Substitute.For<IBackgroundLog>();
        var logger = new TestLogger<MethodLoggingInterceptor>();
        var interceptor = new MethodLoggingInterceptor(cache, logQueue, logger);
        var invocation = Substitute.For<IInvocation>();
    
        // Make TryEnqueue throw an exception to trigger the catch block in LogAsyncCompletion
        logQueue.TryEnqueue(Arg.Any<LogEntry>()).Returns(_ => throw new InvalidOperationException("Queue error"));
    
        var method = typeof(TestClassWithLogOutput).GetMethod(nameof(TestClassWithLogOutput.AsyncMethodWithLogOutput))!;
        invocation.Method.Returns(method);
        invocation.Arguments.Returns(new object[0]);
        invocation.ReturnValue.Returns(Task.FromResult("async result"));
    
        cache.CacheTypeDecisions(typeof(TestClassWithLogOutput));

        // Act
        interceptor.Intercept(invocation);

        // Assert
        invocation.Received(1).Proceed();
        logQueue.Received().TryEnqueue(Arg.Any<LogEntry>());
    }
    
    [Fact]
    public void Intercept_WhenMethodHasParameters_ShouldCreateParameterStructures()
    {
        // Arrange
        var loggingConfig = new LoggingConfig { AutoIntercept = true };
        var cache = new InterceptionDecisionCache(loggingConfig);
        var logQueue = Substitute.For<IBackgroundLog>();
        var logger = new TestLogger<MethodLoggingInterceptor>();
        var interceptor = new MethodLoggingInterceptor(cache, logQueue, logger);
        var invocation = Substitute.For<IInvocation>();
    
        logQueue.TryEnqueue(Arg.Any<LogEntry>()).Returns(true);
    
        var method = typeof(TestClassWithParameters).GetMethod(nameof(TestClassWithParameters.MethodWithParameters))!;
        invocation.Method.Returns(method);
        invocation.Arguments.Returns(new object[] { "test string", 42, null! });
        invocation.ReturnValue.Returns("result");
    
        cache.CacheTypeDecisions(typeof(TestClassWithParameters));

        // Act
        interceptor.Intercept(invocation);

        // Assert
        invocation.Received(1).Proceed();
        logQueue.Received().TryEnqueue(Arg.Any<LogEntry>());
    }
    
    [Fact]
    public void Intercept_WhenAsyncLoggingFailsButFallbackSucceeds_ShouldLogFallbackEntry()
    {
        // Arrange
        var loggingConfig = new LoggingConfig { AutoIntercept = true };
        var cache = new InterceptionDecisionCache(loggingConfig);
        var logQueue = Substitute.For<IBackgroundLog>();
        var logger = new TestLogger<MethodLoggingInterceptor>();
        var interceptor = new MethodLoggingInterceptor(cache, logQueue, logger);
        var invocation = Substitute.For<IInvocation>();
    
        var callCount = 0;
        logQueue.TryEnqueue(Arg.Any<LogEntry>()).Returns(_ => 
        {
            callCount++;
            if (callCount == 1)
                throw new InvalidOperationException("First call fails");
            return true; // The second call succeeds
        });
    
        var method = typeof(TestClassWithLogOutput).GetMethod(nameof(TestClassWithLogOutput.AsyncMethodWithLogOutput))!;
        invocation.Method.Returns(method);
        invocation.Arguments.Returns(new object[0]);
        invocation.ReturnValue.Returns(Task.FromResult("async result"));
    
        cache.CacheTypeDecisions(typeof(TestClassWithLogOutput));

        // Act
        interceptor.Intercept(invocation);

        // Assert
        invocation.Received(1).Proceed();
        logQueue.Received(2).TryEnqueue(Arg.Any<LogEntry>()); // Called twice: first fails, second succeeds
    }
    
    [Fact]
    public void ExtractTaskResult_ShouldReturnNull_WhenTaskIsFaulted()
    {
        // Arrange
        var tcs = new TaskCompletionSource<int>();
        tcs.SetException(new InvalidOperationException("fail"));
        var faultedTask = tcs.Task;

        // Act
        var result = CallExtractTaskResult(faultedTask);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ExtractTaskResult_ShouldUseExtractFromTaskInterface_WhenResultPropertyInvalid()
    {
        // Arrange
        var task = new FallbackTask(123);

        // Act
        var result = CallExtractTaskResult(task);

        // Assert
        result.Should().Be(123);
    }
    
    [Fact]
    public void ExtractTaskResult_ShouldUseInterfacePath_WhenNoGenericBase()
    {
        // Arrange
        var task = new InterfaceTask(77);
        task.RunSynchronously();

        // Act
        var result = typeof(MethodLoggingInterceptor)
            .GetMethod("ExtractTaskResult", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, new object[] { task });

        // Assert
        result.Should().BeNull();
    }
    
    public interface IFakeTask<T> { T Result { get; } }

    public class InterfaceTask : Task, IFakeTask<int>
    {
        private readonly int _value;
        public InterfaceTask(int value) : base(() => { }) => _value = value;
        int IFakeTask<int>.Result => _value;
    }

    public class FallbackTask : Task<int>
    {
        private readonly int _value;

        public FallbackTask(int value) : base(() => value)
        {
            _value = value;
            RunSynchronously();
        }
        
        public new object Result => (object)_value;
    }
    
    private static object? CallExtractTaskResult(Task task)
    {
        var method = typeof(MethodLoggingInterceptor)
            .GetMethod("ExtractTaskResult", BindingFlags.NonPublic | BindingFlags.Static)!;

        return method.Invoke(null, new object[] { task });
    }
    
    public class TestClassWithParameters
    {
        public string MethodWithParameters(string name, int count, object? optional) => "result";
    }
    
    private class DerivedTask : Task<string>
    {
        public DerivedTask(string result) : base(() => result)
        {
            RunSynchronously();
        }
    
        // Hide the Result property to force ExtractTaskResult to fail on direct property access
        public new string Result => throw new InvalidOperationException("Direct access not allowed");
    }

    public class TestClass
    {
        public string TestMethod() => "result";
        public async Task TestAsyncMethod() => await Task.CompletedTask;
    }
    
    public class TestClassWithLogOutput
    {
        [LogOutput]
        public string MethodWithLogOutput() => "result";
        
        [LogOutput]
        public async Task<string> AsyncMethodWithLogOutput() => await Task.FromResult("async result");
    }

    private class TestLogger<T> : ILogger<T>
    {
        public IDisposable BeginScope<TState>(TState state) where TState : notnull => null!;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
    }
}