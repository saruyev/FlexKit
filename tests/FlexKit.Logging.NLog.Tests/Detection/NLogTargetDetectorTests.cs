using System.Diagnostics;
using System.Reflection;
using FlexKit.Logging.NLog.Detection;
using FluentAssertions;
using HarmonyLib;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NLog.Targets;
using Xunit;
// ReSharper disable RedundantAssignment

// ReSharper disable InconsistentNaming

namespace FlexKit.Logging.NLog.Tests.Detection;

/// <summary>
/// Unit tests for NLogTargetDetector class covering catch block scenarios.
/// Tests the exception handling in DetectAvailableTargets, ReferencesNLog, and DetectTargetsInAssembly methods.
/// </summary>
public class NLogTargetDetectorTests
{
    [Fact]
    public void DetectAvailableTargets_WhenAssemblyThrowsException_LogsWarningAndContinues()
    {
        // Arrange - Create a string writer to capture debug output
        using var debugOutput = new StringWriter();
        var listener = new TextWriterTraceListener(debugOutput);
        Trace.Listeners.Add(listener);

        var harmony = new Harmony("test.assembly.exception");
        
        try
        {
            // Patch DetectTargetsInAssembly to throw an exception
            var originalDetectTargets = typeof(NLogTargetDetector)
                .GetMethod("DetectTargetsInAssembly", BindingFlags.NonPublic | BindingFlags.Static);
            
            harmony.Patch(originalDetectTargets,
                prefix: new HarmonyMethod(typeof(NLogTargetDetectorTests), 
                    nameof(ThrowExceptionInDetectTargets)));

            // Act
            var result = NLogTargetDetector.DetectAvailableTargets();

            // Assert
            result.Should().NotBeNull("method should handle exceptions gracefully");
            
            var debugMessages = debugOutput.ToString();
            debugMessages.Should().Contain("Warning: Failed to scan assembly", 
                "should log warning when assembly scanning fails");
            debugMessages.Should().Contain("Simulated DetectTargetsInAssembly exception", 
                "should include the exception message");
        }
        finally
        {
            harmony.UnpatchAll("test.assembly.exception");
            Trace.Listeners.Remove(listener);
        }
    }

    [Fact]
    public void ReferencesNLog_WhenGetReferencedAssembliesThrowsException_ReturnsFalse()
    {
        // Arrange
        var mockAssembly = Substitute.For<Assembly>();
        mockAssembly.GetReferencedAssemblies().Throws(new Exception("Assembly reference error"));

        // Act
        var referencesNLogMethod = typeof(NLogTargetDetector)
            .GetMethod("ReferencesNLog", BindingFlags.NonPublic | BindingFlags.Static);
        
        referencesNLogMethod.Should().NotBeNull("ReferencesNLog method should exist");
        
        var result = (bool)referencesNLogMethod.Invoke(null, [mockAssembly])!;

        // Assert
        result.Should().BeFalse("should return false when GetReferencedAssemblies throws exception");
    }

    [Fact] 
    public void DetectTargetsInAssembly_WhenReflectionTypeLoadException_WithNoAvailableTypes_HandlesGracefully()
    {
        // Arrange
        var targets = new Dictionary<string, NLogTargetDetector.TargetInfo>(StringComparer.OrdinalIgnoreCase);
        var mockAssembly = Substitute.For<Assembly>();

        // Create ReflectionTypeLoadException with no valid types (all null)
        var ex = new ReflectionTypeLoadException(
            [null, null], // No valid types
            [new Exception("Type 1 load error"), new Exception("Type 2 load error")]
        );

        mockAssembly.GetTypes().Throws(ex);

        // Act
        var detectMethod = typeof(NLogTargetDetector)
            .GetMethod("DetectTargetsInAssembly", BindingFlags.NonPublic | BindingFlags.Static);
        
        detectMethod.Should().NotBeNull("DetectTargetsInAssembly method should exist");
        
        // Should not throw - method should handle exception gracefully
        Action act = () => detectMethod.Invoke(null, [mockAssembly, targets]);
        act.Should().NotThrow("should handle ReflectionTypeLoadException with no available types");

        // Assert
        targets.Should().BeEmpty("no targets should be added when no types are available");
    }

    [Fact]
    public void DetectTargetsInAssembly_WhenReflectionTypeLoadException_WithCreateTargetInfoReturningNull_SkipsTarget()
    {
        // Arrange
        var targets = new Dictionary<string, NLogTargetDetector.TargetInfo>(StringComparer.OrdinalIgnoreCase);
        var mockAssembly = Substitute.For<Assembly>();

        // Use a real NLog target type that will pass IsValidTargetType but may fail CreateTargetInfo
        var validTargetType = typeof(ConsoleTarget);
        
        var ex = new ReflectionTypeLoadException(
            [validTargetType, null], // One valid type, one null
            [null, new Exception("Type load error")]
        );

        mockAssembly.GetTypes().Throws(ex);

        var harmony = new Harmony("test.createtargetinfo.null");
        
        try
        {
            // Patch CreateTargetInfo to return null
            var originalCreateTargetInfo = typeof(NLogTargetDetector)
                .GetMethod("CreateTargetInfo", BindingFlags.NonPublic | BindingFlags.Static);
            
            harmony.Patch(originalCreateTargetInfo,
                prefix: new HarmonyMethod(typeof(NLogTargetDetectorTests), 
                    nameof(ReturnNullFromCreateTargetInfo)));

            // Act
            var detectMethod = typeof(NLogTargetDetector)
                .GetMethod("DetectTargetsInAssembly", BindingFlags.NonPublic | BindingFlags.Static);
            
            detectMethod.Should().NotBeNull("DetectTargetsInAssembly method should exist");
            
            Action act = () => detectMethod.Invoke(null, [mockAssembly, targets]);
            act.Should().NotThrow("should handle CreateTargetInfo returning null gracefully");

            // Assert
            targets.Should().BeEmpty("no targets should be added when CreateTargetInfo returns null");
        }
        finally
        {
            harmony.UnpatchAll("test.createtargetinfo.null");
        }
    }

    [Fact]
    public void DetectTargetsInAssembly_WhenReflectionTypeLoadException_WithValidTargetInfo_AddsTarget()
    {
        // Arrange
        var targets = new Dictionary<string, NLogTargetDetector.TargetInfo>(StringComparer.OrdinalIgnoreCase);
        var mockAssembly = Substitute.For<Assembly>();

        // Use a real NLog target type
        var validTargetType = typeof(ConsoleTarget);
        
        var ex = new ReflectionTypeLoadException(
            [validTargetType, null], // One valid type, one null
            [null, new Exception("Type load error")]
        );

        mockAssembly.GetTypes().Throws(ex);

        // Act - Let CreateTargetInfo work normally (no patching)
        var detectMethod = typeof(NLogTargetDetector)
            .GetMethod("DetectTargetsInAssembly", BindingFlags.NonPublic | BindingFlags.Static);
        
        detectMethod.Should().NotBeNull("DetectTargetsInAssembly method should exist");
        
        Action act = () => detectMethod.Invoke(null, [mockAssembly, targets]);
        act.Should().NotThrow("should handle ReflectionTypeLoadException with valid types");

        // Assert
        targets.Should().HaveCount(1, "should add one target for the valid ConsoleTarget type");
        targets.Should().ContainKey("Console", "should add ConsoleTarget with 'Console' name");
        var targetInfo = targets["Console"];
        targetInfo.TargetType.Should().Be(typeof(ConsoleTarget));
    }
    
    [Fact]
    public void CreateTargetInfo_WhenCreateTargetInfoFromTypeThrowsException_ReturnsNull()
    {
        // Arrange
        var validTargetType = typeof(ConsoleTarget);
        var harmony = new Harmony("test.createtargetinfo.exception");
    
        try
        {
            // Patch CreateTargetInfoFromType to throw an exception
            var originalCreateTargetInfoFromType = typeof(NLogTargetDetector)
                .GetMethod("CreateTargetInfoFromType", BindingFlags.NonPublic | BindingFlags.Static);
        
            harmony.Patch(originalCreateTargetInfoFromType,
                prefix: new HarmonyMethod(typeof(NLogTargetDetectorTests), 
                    nameof(ThrowExceptionInCreateTargetInfoFromType)));

            // Act
            var createTargetInfoMethod = typeof(NLogTargetDetector)
                .GetMethod("CreateTargetInfo", BindingFlags.NonPublic | BindingFlags.Static);
        
            createTargetInfoMethod.Should().NotBeNull("CreateTargetInfo method should exist");
        
            var result = createTargetInfoMethod.Invoke(null, [validTargetType]);

            // Assert
            result.Should().BeNull("should return null when CreateTargetInfoFromType throws exception");
        }
        finally
        {
            harmony.UnpatchAll("test.createtargetinfo.exception");
        }
    }
    
    [Fact]
    public void GetNLogAssemblies_WhenAssemblyLoadThrowsException_ContinuesWithOtherAssemblies()
    {
        // Arrange
        var harmony = new Harmony("test.assembly.load.exception");
    
        try
        {
            // Patch Assembly.Load to throw an exception
            var originalAssemblyLoad = typeof(Assembly)
                .GetMethod("Load", [typeof(AssemblyName)]);
        
            harmony.Patch(originalAssemblyLoad,
                prefix: new HarmonyMethod(typeof(NLogTargetDetectorTests), 
                    nameof(ThrowExceptionInAssemblyLoad)));

            // Act
            var getNLogAssembliesMethod = typeof(NLogTargetDetector)
                .GetMethod("GetNLogAssemblies", BindingFlags.NonPublic | BindingFlags.Static);
        
            getNLogAssembliesMethod.Should().NotBeNull("GetNLogAssemblies method should exist");
        
            var result = (Assembly[])getNLogAssembliesMethod.Invoke(null, null)!;

            // Assert
            result.Should().NotBeNull("should return non-null array even when Assembly.Load throws");
            // Should still contain assemblies from AppDomain.CurrentDomain.GetAssemblies() that reference NLog
            result.Should().Contain(a => a.GetReferencedAssemblies()
                    .Any(name => name.Name!.StartsWith("NLog", StringComparison.OrdinalIgnoreCase) == true),
                "should still include assemblies from current domain that reference NLog");
        }
        finally
        {
            harmony.UnpatchAll("test.assembly.load.exception");
        }
    }

    private static bool ThrowExceptionInAssemblyLoad()
    {
        throw new FileNotFoundException("Simulated Assembly.Load exception");
    }
    
    private static bool ThrowExceptionInCreateTargetInfoFromType()
    {
        throw new InvalidOperationException("Simulated CreateTargetInfoFromType exception");
    }

    // Harmony prefix method to throw exception in DetectTargetsInAssembly
    private static bool ThrowExceptionInDetectTargets()
    {
        throw new InvalidOperationException("Simulated DetectTargetsInAssembly exception");
    }

    // Harmony prefix method to return null from CreateTargetInfo
    private static bool ReturnNullFromCreateTargetInfo(ref NLogTargetDetector.TargetInfo __result)
    {
        __result = null!;
        return false; 
    }
}