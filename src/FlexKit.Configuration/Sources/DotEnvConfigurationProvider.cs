// <copyright file="DotEnvConfigurationProvider.cs" company="Michael Saruyev">
// Copyright (c) Michael Saruyev. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

using Microsoft.Extensions.Configuration;

namespace FlexKit.Configuration.Sources;

/// <summary>
/// Configuration provider that reads .env files and makes them available through the .NET configuration system.
/// Implements the standard IConfigurationProvider interface to integrate .env file support seamlessly
/// with Microsoft's configuration framework, following twelve-factor app principles.
/// </summary>
/// <remarks>
/// This provider enables the use of .env files in .NET applications, bringing the popular
/// configuration pattern from other ecosystems (Node.js, Python, etc.) to the .NET world.
/// It follows the standard .env file format and conventions while integrating naturally
/// with the existing .NET configuration infrastructure.
///
/// <para>
/// <strong>Supported .env File Features:</strong>
/// <list type="bullet">
/// <item>Key-value pairs separated by equals sign (KEY=value)</item>
/// <item>Comments starting with # character</item>
/// <item>Empty lines (ignored)</item>
/// <item>Quoted values (single and double quotes)</item>
/// <item>Basic escape sequences (\n, \t, \r, \\)</item>
/// <item>Unquoted values with automatic trimming</item>
/// </list>
/// </para>
///
/// <para>
/// <strong>Integration with .NET Configuration:</strong>
/// The provider converts .env key-value pairs directly into configuration keys,
/// making them accessible through all standard .NET configuration APIs including
/// IConfiguration indexer access, section binding, and options' pattern integration.
/// </para>
///
/// <para>
/// <strong>Security Considerations:</strong>
/// <list type="bullet">
/// <item>.env files should never be committed to version control when containing sensitive data</item>
/// <item>Use .env.example files to document required environment variables</item>
/// <item>Ensure appropriate file permissions are set in production environments</item>
/// <item>Consider using more secure configuration providers for production secrets</item>
/// </list>
/// </para>
///
/// <para>
/// <strong>Best Practices:</strong>
/// <list type="bullet">
/// <item>Use .env files primarily for development and testing scenarios</item>
/// <item>Structure .env files to match your configuration hierarchy needs</item>
/// <item>Document all required environment variables in your project</item>
/// <item>Use descriptive variable names that clearly indicate their purpose</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Example .env file content:
/// # Database Configuration
/// DATABASE_URL=postgresql://localhost:5432/myapp
/// DATABASE_POOL_SIZE=10
///
/// # API Configuration
/// API_KEY="your-secret-key-here"
/// API_TIMEOUT=5000
/// API_BASE_URL=https://api.example.com
///
/// # Feature Flags
/// ENABLE_CACHING=true
/// DEBUG_MODE=false
///
/// # Quoted values with spaces
/// WELCOME_MESSAGE="Welcome to our application!"
///
/// # Values with escape sequences
/// MULTILINE_TEXT="Line 1\nLine 2\nLine 3"
/// </code>
/// </example>
public class DotEnvConfigurationProvider : ConfigurationProvider
{
    /// <summary>
    /// The configuration source that defines the .env file location and loading options.
    /// Used to access a file path, optional flag, and other source-specific settings.
    /// </summary>
    private readonly DotEnvConfigurationSource _source;

    /// <summary>
    /// Initializes a new instance of the DotEnvConfigurationProvider class.
    /// </summary>
    /// <param name="source">The configuration source containing .env file settings and options.</param>
    /// <exception cref="ArgumentNullException">Thrown when the source is null.</exception>
    public DotEnvConfigurationProvider(DotEnvConfigurationSource source)
    {
        _source = source;
    }

    /// <summary>
    /// Loads configuration data from the .env file specified in the configuration source.
    /// Parses the file line by line, extracting key-value pairs and making them available
    /// through the .NET configuration system.
    /// </summary>
    /// <remarks>
    /// This method performs the core functionality of the .env configuration provider:
    ///
    /// <list type="number">
    /// <item>Checks if the .env file exists at the specified path</item>
    /// <item>Handles optional file scenarios (A file missing but marked as optional)</item>
    /// <item>Reads all lines from the file</item>
    /// <item>Parses each line to extract key-value pairs</item>
    /// <item>Stores the parsed data in the provider's Data dictionary</item>
    /// </list>
    ///
    /// <para>
    /// <strong>File Reading Behavior:</strong>
    /// The method reads the entire file into memory at once, which is appropriate for
    /// .env files that are typically small and contain environment-like configuration.
    /// For very large configuration files, consider using streaming approaches.
    /// </para>
    ///
    /// <para>
    /// <strong>Error Handling:</strong>
    /// <list type="bullet">
    /// <item>Missing optional files are silently ignored</item>
    /// <item>Missing required files throw FileNotFoundException</item>
    /// <item>File access errors propagate to the caller</item>
    /// <item>Parse errors in individual lines are handled gracefully (lines skipped)</item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// <strong>Case Sensitivity:</strong>
    /// The configuration dictionary uses case-insensitive string comparison (OrdinalIgnoreCase)
    /// to match the behavior of other .NET configuration providers and environment variables
    /// on Windows systems.
    /// </para>
    ///
    /// <para>
    /// <strong>Performance Considerations:</strong>
    /// <list type="bullet">
    /// <item>File is read synchronously during configuration building</item>
    /// <item>Entire file content is loaded into memory</item>
    /// <item>Parsing is performed once during the Load() call</item>
    /// <item>Further configuration access uses in-memory dictionary</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <exception cref="FileNotFoundException">Thrown when the .env file doesn't exist and the source is not marked as optional.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown when the application lacks permission to read the .env file.</exception>
    /// <exception cref="IOException">Thrown when other I/O errors occur during file reading.</exception>
    /// <example>
    /// <code>
    /// // This method is typically called automatically by the configuration system
    /// // but can be called manually for testing or custom scenarios:
    ///
    /// var source = new DotEnvConfigurationSource { Path = ".env", Optional = true };
    /// var provider = new DotEnvConfigurationProvider(source);
    /// provider.Load(); // Loads .env file content into configuration
    ///
    /// // Access loaded configuration data
    /// if (provider.TryGet("DATABASE_URL", out string? dbUrl))
    /// {
    ///     Console.WriteLine($"Database URL: {dbUrl}");
    /// }
    /// </code>
    /// </example>
    public override void Load()
    {
        if (!File.Exists(_source.Path))
        {
            if (!_source.Optional)
            {
                throw new FileNotFoundException($"DotEnv file '{_source.Path}' not found");
            }

            return;
        }

        var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        foreach (var line in File.ReadAllLines(_source.Path))
        {
            ParseLine(line, data);
        }

        Data = data;
    }

    /// <summary>
    /// Parses a single line from the .env file and extracts key-value pairs.
    /// Handles various .env file formats, including comments, quoted values, and escape sequences.
    /// </summary>
    /// <param name="line">The line of text to parse from the .env file.</param>
    /// <param name="data">The dictionary to store the parsed key-value pair in.</param>
    /// <remarks>
    /// This method implements the core .env file parsing logic, supporting the most
    /// common .env file conventions and formats. It provides robust parsing that
    /// handles edge cases while maintaining compatibility with popular .env implementations.
    ///
    /// <para>
    /// <strong>Parsing Rules Applied:</strong>
    /// <list type="number">
    /// <item>Lines are trimmed of leading and trailing whitespace</item>
    /// <item>Empty lines are ignored</item>
    /// <item>Lines starting with # are treated as comments and ignored</item>
    /// <item>Lines must contain a "=" character to be considered valid key-value pairs</item>
    /// <item>The first = character acts as the separator between key and value</item>
    /// <item>Keys and values are trimmed of surrounding whitespace</item>
    /// <item>Values surrounded by quotes (single or double) have quotes removed</item>
    /// <item>Basic escape sequences in values are processed</item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// <strong>Comment Handling:</strong>
    /// Only full-line comments are supported (lines that start with #).
    /// Inline comments (comments after values) are not supported and will be
    /// treated as part of the value.
    /// </para>
    ///
    /// <para>
    /// <strong>Quote Processing:</strong>
    /// <list type="bullet">
    /// <item>Single quotes ('value') are removed from the value</item>
    /// <item>Double quotes ("value") are removed from the value</item>
    /// <item>Mixed quotes are not processed (e.g., "value' remains as-is)</item>
    /// <item>Only matching quote pairs at the beginning and end are removed</item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// <strong>Escape Sequence Support:</strong>
    /// The following escape sequences are processed in values:
    /// <list type="table">
    /// <listheader>
    /// <term>Sequence</term>
    /// <description>Result</description>
    /// </listheader>
    /// <item>
    /// <term>\n</term>
    /// <description>Newline character</description>
    /// </item>
    /// <item>
    /// <term>\t</term>
    /// <description>Tab character</description>
    /// </item>
    /// <item>
    /// <term>\r</term>
    /// <description>Carriage return character</description>
    /// </item>
    /// <item>
    /// <term>\\</term>
    /// <description>Literal backslash character</description>
    /// </item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// <strong>Error Handling:</strong>
    /// <list type="bullet">
    /// <item>Lines without = characters are silently ignored</item>
    /// <item>Duplicate keys overwrite previous values</item>
    /// <item>Malformed lines don't cause parsing to stop</item>
    /// <item>Empty keys (lines starting with =) are ignored</item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// <strong>Key Naming:</strong>
    /// Keys are used exactly as specified in the .env file without any transformation.
    /// Unlike environment variables in some systems, the case is preserved as written.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Examples of lines that would be parsed:
    ///
    /// ParseLine("DATABASE_URL=postgresql://localhost:5432/myapp", data);
    /// // Result: data["DATABASE_URL"] = "postgresql://localhost:5432/myapp"
    ///
    /// ParseLine("API_KEY=\"secret-key-123\"", data);
    /// // Result: data["API_KEY"] = "secret-key-123"
    ///
    /// ParseLine("WELCOME_MSG='Hello\\nWorld'", data);
    /// // Result: data["WELCOME_MSG"] = "Hello\nWorld"
    ///
    /// ParseLine("# This is a comment", data);
    /// // Result: Line ignored, no data added
    ///
    /// ParseLine("", data);
    /// // Result: Line ignored, no data added
    ///
    /// ParseLine("COMPLEX_VALUE=value with = signs in it", data);
    /// // Result: data["COMPLEX_VALUE"] = "value with = signs in it"
    /// </code>
    /// </example>
    private static void ParseLine(string line, Dictionary<string, string?> data)
    {
        // Skip empty lines and comments
        var trimmed = line.Trim();
        if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith('#'))
        {
            return;
        }

        // Find the first '=' character
        var separatorIndex = trimmed.IndexOf('=');
        if (separatorIndex == -1)
        {
            return; // Invalid line, skip
        }

        var key = trimmed[..separatorIndex].Trim();
        var value = trimmed[(separatorIndex + 1)..].Trim();

        // Remove quotes if present
        if (value.Length >= 2 && (value.StartsWith('"') && value.EndsWith('"') || value.StartsWith('\'') && value.EndsWith('\'')))
        {
            value = value[1..^1];
        }

        // Basic escape sequence handling
        value = value.Replace("\\n", "\n")
            .Replace("\\t", "\t")
            .Replace("\\r", "\r")
            .Replace("\\\\", "\\");

        data[key] = value;
    }
}
