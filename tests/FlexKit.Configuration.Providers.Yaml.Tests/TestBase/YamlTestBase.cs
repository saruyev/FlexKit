using System.Diagnostics.CodeAnalysis;
using AutoFixture;
using JetBrains.Annotations;

namespace FlexKit.Configuration.Providers.Yaml.Tests.TestBase;

/// <summary>
/// Base class for YAML provider unit tests providing common test infrastructure.
/// Follows the same patterns as the main FlexKit.Configuration.Tests project.
/// </summary>
public abstract class YamlTestBase : IDisposable
{
    /// <summary>
    /// AutoFixture instance for generating test data.
    /// </summary>
    protected Fixture Fixture { [UsedImplicitly] get; }

    /// <summary>
    /// List of temporary files created during tests for cleanup.
    /// </summary>
    private readonly List<string> _tempFiles = new();

    /// <summary>
    /// Tracks disposal state to prevent double disposal.
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// Initializes the test base with configured AutoFixture.
    /// </summary>
    protected YamlTestBase()
    {
        Fixture = new Fixture();
    }

    /// <summary>
    /// Creates a temporary YAML file with the specified content for testing.
    /// File will be automatically cleaned up when the test completes.
    /// </summary>
    /// <param name="yamlContent">The YAML content to write to the file.</param>
    /// <param name="extension">The file extension (default: .yaml).</param>
    /// <returns>The path to the created temporary file.</returns>
    protected string CreateTempYamlFile(string yamlContent, string extension = ".yaml")
    {
        var tempFile = Path.GetTempFileName();
        var yamlFile = Path.ChangeExtension(tempFile, extension);
        
        // Delete the original temp file and create the YAML one
        File.Delete(tempFile);
        File.WriteAllText(yamlFile, yamlContent);
        
        _tempFiles.Add(yamlFile);
        return yamlFile;
    }

    /// <summary>
    /// Creates a temporary file path without creating the actual file.
    /// Useful for testing missing file scenarios.
    /// </summary>
    /// <param name="extension">The file extension (default: .yaml).</param>
    /// <returns>A path to a non-existent file.</returns>
    protected string GetTempYamlFilePath(string extension = ".yaml")
    {
        var tempFile = Path.GetTempFileName();
        var yamlFile = Path.ChangeExtension(tempFile, extension);
        
        // Delete the temp file so it doesn't exist
        File.Delete(tempFile);
        
        _tempFiles.Add(yamlFile); // Track for a cleanup attempt
        return yamlFile;
    }

    /// <summary>
    /// Disposes the test base and cleans up temporary files.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Protected dispose method for cleanup of managed resources.
    /// </summary>
    /// <param name="disposing">True if disposing managed resources.</param>
    [SuppressMessage("ReSharper", "FlagArgument")]
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            // Clean up temporary test files
            foreach (var file in _tempFiles.Where(File.Exists))
            {
                try
                {
                    File.Delete(file);
                }
                catch
                {
                    // Ignore cleanup errors in tests
                }
            }

            _tempFiles.Clear();
            _disposed = true;
        }
    }
}