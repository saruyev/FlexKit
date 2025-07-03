using BenchmarkDotNet.Attributes;
using FlexKit.Configuration.Providers.Yaml.Sources;
using Microsoft.Extensions.Configuration;

namespace FlexKit.Configuration.Providers.Yaml.PerformanceTests.Benchmarks;

/// <summary>
/// Benchmarks for different YAML parsing approaches and features.
/// Tests the performance impact of various YAML syntax features like anchors, 
/// multi-line strings, complex data types, and nested structures.
/// </summary>
[MemoryDiagnoser]
[SimpleJob]
public class YamlParsingBenchmarks
{
    private string _simpleYamlContent = null!;
    private string _anchorsYamlContent = null!;
    private string _multilineYamlContent = null!;
    private string _arrayYamlContent = null!;
    private string _deepNestingYamlContent = null!;
    private string _mixedTypesYamlContent = null!;
    private string _unicodeYamlContent = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Simple YAML with basic key-value pairs
        _simpleYamlContent = """
            app: "Simple App"
            version: "1.0.0"
            port: 8080
            enabled: true
            timeout: 30.5
            """;

        // YAML with anchors and aliases for configuration reuse
        _anchorsYamlContent = """
            # Define defaults anchor
            defaults: &defaults
              timeout: 5000
              retries: 3
              ssl: true
              format: json

            # Environment configurations using anchors
            development:
              <<: *defaults
              host: "dev.example.com"
              timeout: 2000  # Override default
              debug: true

            production:
              <<: *defaults
              host: "prod.example.com"
              timeout: 10000  # Override default
              ssl: true

            staging:
              <<: *defaults
              host: "staging.example.com"
              debug: false
            """;

        // YAML with multi-line strings (literal and folded)
        _multilineYamlContent = """
            documentation:
              installation: |
                Step 1: Download the package
                Step 2: Extract to desired location
                Step 3: Run the installer
                Step 4: Configure settings
                Step 5: Start the application

              description: >
                This is a long description that spans
                multiple lines but will be folded into
                a single line with spaces replacing
                the line breaks for better readability
                in configuration files.

              script: |
                #!/bin/bash
                echo "Starting application..."
                cd /app
                ./start.sh --config=/etc/app/config.yaml
                echo "Application started successfully"

            messages:
              welcome: >
                Welcome to our application! This message
                demonstrates the folded string style which
                removes line breaks and creates a single
                line of text.

              terms: |
                By using this software, you agree to the following terms:
                
                1. Use the software responsibly
                2. Do not distribute without permission
                3. Report bugs to our support team
                
                Thank you for using our product!
            """;

        // YAML with complex arrays and nested objects
        _arrayYamlContent = """
            servers:
              - name: web01
                host: 192.168.1.10
                port: 8080
                services: [http, https, websocket]
                metadata:
                  location: datacenter-1
                  environment: production
                  
              - name: web02
                host: 192.168.1.11
                port: 8080
                services: [http, https]
                metadata:
                  location: datacenter-1
                  environment: production

              - name: db01
                host: 192.168.2.10
                port: 5432
                services: [postgresql, monitoring]
                metadata:
                  location: datacenter-2
                  environment: production

            endpoints:
              - service: UserService
                methods: [GET, POST, PUT, DELETE]
                authentication: required
                rateLimit: { requests: 100, window: "1m" }
                
              - service: OrderService
                methods: [GET, POST]
                authentication: required
                rateLimit: { requests: 50, window: "1m" }

            features:
              - name: caching
                enabled: true
                config: { type: "redis", ttl: 3600 }
                
              - name: logging
                enabled: true
                config: { level: "info", format: "json" }
            """;

        // YAML with deep nesting (6+ levels)
        _deepNestingYamlContent = """
            application:
              services:
                database:
                  clusters:
                    primary:
                      instances:
                        instance1:
                          connection:
                            host: "db-primary-1.example.com"
                            port: 5432
                            credentials:
                              username: "app_user"
                              password: "secure_password"
                              
                        instance2:
                          connection:
                            host: "db-primary-2.example.com"
                            port: 5432
                            credentials:
                              username: "app_user"
                              password: "secure_password"
                              
                    secondary:
                      instances:
                        instance1:
                          connection:
                            host: "db-secondary-1.example.com"
                            port: 5432
                            credentials:
                              username: "readonly_user"
                              password: "readonly_password"
            """;

        // YAML with mixed data types and special values
        _mixedTypesYamlContent = """
            # String variations
            strings:
              simple: hello
              quoted: "hello world"
              singleQuoted: 'hello world'
              multiWord: hello world
              empty: ""
              
            # Numeric variations
            numbers:
              integer: 42
              float: 3.14159
              scientific: 1.23e-4
              hexadecimal: 0xFF
              octal: 0o755
              binary: 0b1010
              infinity: .inf
              notANumber: .nan
              
            # Boolean variations
            booleans:
              trueValue1: true
              trueValue2: True
              trueValue3: TRUE
              trueValue4: yes
              trueValue5: Yes
              trueValue6: on
              falseValue1: false
              falseValue2: False
              falseValue3: FALSE
              falseValue4: no
              falseValue5: No
              falseValue6: off
              
            # Null variations
            nulls:
              nullValue1: null
              nullValue2: Null
              nullValue3: NULL
              nullValue4: ~
              nullValue5:  # empty value
              
            # Date and timestamp
            dates:
              date: 2024-07-03
              datetime: 2024-07-03T15:30:00Z
              timestamp: 2024-07-03 15:30:00
            """;

        // YAML with Unicode and special characters
        _unicodeYamlContent = """
            internationalization:
              english: "Hello World"
              spanish: "Hola Mundo"
              chinese: "你好世界"
              japanese: "こんにちは世界"
              arabic: "مرحبا بالعالم"
              russian: "Привет мир"
              emoji: "Hello World! 🌍🚀✨"
              
            specialCharacters:
              quotes: 'Text with "double quotes" inside'
              backslashes: "C:\\Users\\Admin\\Documents"
              unicode: "Unicode: \\u0048\\u0065\\u006C\\u006C\\u006F"
              symbols: "Special symbols: !@#$%^&*()_+-=[]{}|;:,.<>?"
              
            paths:
              windows: "C:\\Program Files\\Application\\config.yaml"
              unix: "/etc/application/config.yaml"
              url: "https://example.com/api/v1/config?format=yaml&type=full"
            """;
    }

    // === Simple YAML Parsing (Baseline) ===

    [Benchmark(Baseline = true)]
    public IConfiguration ParseSimpleYaml()
    {
        var builder = new ConfigurationBuilder();
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(_simpleYamlContent));
        builder.Add(new YamlConfigurationSource { Path = "memory://simple.yaml" });
        
        // Simulate loading from content
        var source = new YamlConfigurationSource();
        _ = new YamlConfigurationProvider(source);
        
        // Use direct content parsing for memory-based testing
        return ParseYamlContent(_simpleYamlContent);
    }

    // === YAML Anchors and Aliases ===

    [Benchmark]
    public IConfiguration ParseYamlWithAnchors()
    {
        return ParseYamlContent(_anchorsYamlContent);
    }

    // === Multi-line Strings ===

    [Benchmark]
    public IConfiguration ParseYamlWithMultilineStrings()
    {
        return ParseYamlContent(_multilineYamlContent);
    }

    // === Complex Arrays and Objects ===

    [Benchmark]
    public IConfiguration ParseYamlWithArrays()
    {
        return ParseYamlContent(_arrayYamlContent);
    }

    // === Deep Nesting ===

    [Benchmark]
    public IConfiguration ParseYamlWithDeepNesting()
    {
        return ParseYamlContent(_deepNestingYamlContent);
    }

    // === Mixed Data Types ===

    [Benchmark]
    public IConfiguration ParseYamlWithMixedTypes()
    {
        return ParseYamlContent(_mixedTypesYamlContent);
    }

    // === Unicode and Special Characters ===

    [Benchmark]
    public IConfiguration ParseYamlWithUnicode()
    {
        return ParseYamlContent(_unicodeYamlContent);
    }

    // === File-based vs. Memory-based Parsing ===

    [Benchmark]
    public IConfiguration ParseYamlFromFile()
    {
        // Create a temporary file for testing file-based loading
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, _simpleYamlContent);
            
            var builder = new ConfigurationBuilder();
            builder.Add(new YamlConfigurationSource { Path = tempFile });
            return builder.Build();
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    // === Combined Complex Features ===

    [Benchmark]
    public IConfiguration ParseComplexYamlAllFeatures()
    {
        // Combine multiple YAML features in one document
        var combinedYaml = $"""
            # Combined YAML with multiple features
            {_anchorsYamlContent}
            
            # Multi-line content
            content:
              {_multilineYamlContent.Replace("\n", "\n  ")}
            
            # Array data  
            data:
              {_arrayYamlContent.Replace("\n", "\n  ")}
            """;

        return ParseYamlContent(combinedYaml);
    }

    // Helper method to parse YAML content consistently
    private static IConfiguration ParseYamlContent(string yamlContent)
    {
        // Create a temporary file since YamlConfigurationSource requires a file path
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, yamlContent);
            
            var builder = new ConfigurationBuilder();
            builder.Add(new YamlConfigurationSource { Path = tempFile });
            return builder.Build();
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }
}