# Changelog

All notable changes to the FlexKit project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Planned
- Additional FlexKit modules (Logging, Security, Data, etc.)
- Advanced configuration providers (Azure Key Vault, AWS Parameter Store)
- Performance optimizations and benchmarks
- Comprehensive documentation site

## [1.0.0] - 2025-01-20

### Added

#### FlexKit.Configuration
- **Dynamic Configuration Access**: Access configuration values using natural C# syntax with runtime member resolution
- **Autofac Integration**: Seamless integration with Autofac dependency injection container
- **Assembly Scanning**: Intelligent assembly discovery and registration with configurable filtering through `MappingConfig`
- **Type Conversion System**: Built-in support for converting configuration values to various types with culture-invariant parsing
- **Multiple Configuration Sources**: Support for JSON files, environment variables, and .env files
- **FlexConfigurationBuilder**: Fluent API for building configuration with multiple sources
- **Property Injection**: Automatic injection of `IFlexConfig` into services through `ConfigurationModule`
- **Comprehensive Documentation**: Full XML documentation with examples and best practices

#### Core Components
- `IFlexConfig` interface for dynamic configuration access
- `FlexConfiguration` class implementing dynamic object with configuration capabilities
- `FlexConfigurationBuilder` for fluent configuration setup
- `ConfigurationModule` for Autofac integration and property injection

#### Assembly Management
- `AssemblyExtensions` for automatic module discovery and registration
- `MappingConfig` for controlling which assemblies are scanned
- Support for prefix-based and name-based assembly filtering

#### Type Conversion
- `TypeConversionExtensions` with support for primitives, enums, collections, and dictionaries
- Culture-invariant parsing for consistent behavior across environments
- Collection support with customizable separators
- Dictionary conversion from configuration sections

#### .env File Support
- `DotEnvConfigurationProvider` for reading .env files
- `DotEnvConfigurationSource` for .env file configuration
- Support for comments, quoted values, and escape sequences
- Integration with standard .NET configuration system

#### Development Standards
- **Modern C# 13**: File-scoped namespaces, primary constructors, and latest language features
- **Null Safety**: Full nullable reference types support with comprehensive annotations
- **FlexKit Style Guide**: Enforced coding standards with analyzers and build targets
- **Comprehensive Testing**: Unit and integration testing support with examples

### Technical Requirements
- .NET 9.0 or later
- C# 13.0 or later
- Autofac 8.3.0 or later
- Microsoft.Extensions.Configuration 9.0 or later

### Documentation
- Complete README with usage examples and best practices
- Comprehensive XML documentation for all public APIs
- Migration guides from legacy systems (StructureMap, standard IConfiguration)
- Performance considerations and optimization guidelines
- Integration examples for ASP.NET Core applications

### License
- MIT License for open-source usage
- Compatible with commercial and private use

---

## Release Notes

### v1.0.0 - "Foundation"

This is the initial release of FlexKit.Configuration, establishing the foundation for the FlexKit ecosystem. This release focuses on providing a robust, flexible configuration system that bridges the gap between Microsoft's structured IConfiguration and developer-friendly dynamic access patterns.

**Key Highlights:**
- 🚀 **Dynamic Access**: `config.Database.ConnectionString` syntax
- 📦 **Autofac Ready**: Seamless DI integration with module scanning
- 🔧 **Type Safe**: Built-in conversion with `config["Port"].ToType<int>()`
- 🌍 **Multi-Source**: JSON, environment variables, and .env files
- ⚡ **Modern**: Built for .NET 9 with latest C# features

**Perfect for:**
- New .NET 9 applications requiring flexible configuration
- Legacy application modernization
- Teams wanting dynamic configuration access with type safety
- Applications with complex configuration hierarchies

**Next Steps:**
The FlexKit.Configuration module serves as the foundation for the broader FlexKit ecosystem. Future releases will build upon this configuration system to provide integrated logging, security, data access, and other enterprise application concerns.

---

## Version History

- **v1.0.0**: Initial release with core configuration functionality
- **Future**: Additional FlexKit modules and enhanced features

---

*For more information, visit the [FlexKit Repository](https://github.com/msaruyev/FlexKit)*
