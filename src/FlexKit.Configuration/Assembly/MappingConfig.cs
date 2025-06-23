// <copyright file="MappingConfig.cs" company="Michael Saruyev">
// Copyright (c) Michael Saruyev. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

using JetBrains.Annotations;

namespace FlexKit.Configuration.Assembly;

/// <summary>
/// Assembly mapping settings holder for controlling which assemblies are scanned during module discovery.
/// Provides configuration options to filter assemblies by naming patterns, enabling precise control
/// over which assemblies are included in the Autofac module registration process.
/// </summary>
/// <remarks>
/// This record defines the configuration structure used by the FlexKit Configuration library
/// to determine which assemblies should be scanned for Autofac modules. It supports two
/// filtering strategies that can be used independently:
///
/// <list type="number">
/// <item>
/// <term>Prefix-based filtering</term>
/// <description>All assemblies starting with a specific prefix are included</description>
/// </item>
/// <item>
/// <term>Name-based filtering</term>
/// <description>Only assemblies starting with explicitly listed prefixes are included</description>
/// </item>
/// </list>
///
/// <para>
/// <strong>Configuration Priority:</strong>
/// If both <see cref="Prefix"/> and <see cref="Names"/> are specified, the <see cref="Prefix"/>
/// takes precedence and <see cref="Names"/> is ignored.
/// </para>
///
/// <para>
/// <strong>Configuration Examples:</strong>
/// <code>
/// // Example 1: Prefix-based configuration
/// {
///     "Application": {
///         "Mapping": {
///             "Prefix": "MyCompany"
///         }
///     }
/// }
/// // This will include: MyCompany.Services.dll, MyCompany.Data.dll, MyCompany.Core.dll, etc.
///
/// // Example 2: Name-based configuration
/// {
///     "Application": {
///         "Mapping": {
///             "Names": ["Acme.Services", "Acme.Data", "ThirdParty.Extensions"]
///         }
///     }
/// }
/// // This will include: Acme.Services.dll, Acme.Data.Models.dll, ThirdParty.Extensions.dll, etc.
///
/// // Example 3: No configuration (uses defaults)
/// {
///     "Application": {
///         "Mapping": {}
///     }
/// }
/// // This will include FlexKit.Configuration assemblies and any assembly containing "Module" in the name
/// </code>
/// </para>
///
/// <para>
/// <strong>Best Practices:</strong>
/// <list type="bullet">
/// <item>Use <see cref="Prefix"/> when all your assemblies follow a consistent naming convention</item>
/// <item>Use <see cref="Names"/> when you need fine-grained control over which assemblies to include</item>
/// <item>Prefer specific configurations over defaults to improve startup performance</item>
/// <item>Ensure the configuration matches your actual assembly naming patterns</item>
/// </list>
/// </para>
/// </remarks>
public sealed record MappingConfig
{
    /// <summary>
    /// Gets the assembly prefix pattern for filtering assemblies during module discovery.
    /// When specified, only assemblies whose names start with this prefix will be scanned for Autofac modules.
    /// </summary>
    /// <value>
    /// A string representing the prefix that assembly names must start with to be included in scanning.
    /// If <c>null</c> or empty, prefix-based filtering is not applied.
    /// </value>
    /// <remarks>
    /// This property provides a simple way to include all assemblies that follow a consistent
    /// naming convention. It's particularly useful in enterprise environments where all
    /// application assemblies share a common prefix (e.g., company name or product name).
    ///
    /// <para>
    /// <strong>Comparison is Case-Invariant:</strong>
    /// The prefix matching is performed using <see cref="StringComparison.InvariantCulture"/>,
    /// making the comparison case-sensitive but culture-invariant.
    /// </para>
    ///
    /// <para>
    /// <strong>Example Usage:</strong>
    /// If <c>Prefix</c> is set to "MyCompany", the following assemblies would be included:
    /// <list type="bullet">
    /// <item>MyCompany.Services.dll</item>
    /// <item>MyCompany.Data.dll</item>
    /// <item>MyCompany.Core.Utilities.dll</item>
    /// </list>
    /// But these would be excluded:
    /// <list type="bullet">
    /// <item>ThirdParty.Library.dll</item>
    /// <item>System.Core.dll</item>
    /// <item>Microsoft.Extensions.dll</item>
    /// </list>
    /// </para>
    /// </remarks>
    public string? Prefix { get; [UsedImplicitly] init; }

    /// <summary>
    /// Gets the collection of assembly name prefixes for fine-grained filtering during module discovery.
    /// When specified, only assemblies whose names start with one of these prefixes will be scanned for Autofac modules.
    /// </summary>
    /// <value>
    /// An array of strings representing the prefixes that assembly names must start with to be included in scanning.
    /// If <c>null</c> or empty, name-based filtering is not applied.
    /// </value>
    /// <remarks>
    /// This property enables precise control over which assemblies are included in module scanning.
    /// It's particularly useful when you need to include assemblies from multiple sources or
    /// when your assemblies don't follow a single consistent naming pattern.
    ///
    /// <para>
    /// <strong>Priority Note:</strong>
    /// If both <see cref="Prefix"/> and <see cref="Names"/> are specified, <see cref="Prefix"/>
    /// takes precedence and this property is ignored. To use name-based filtering, ensure
    /// <see cref="Prefix"/> is <c>null</c> or empty.
    /// </para>
    ///
    /// <para>
    /// <strong>Matching Logic:</strong>
    /// An assembly is included if its name starts with ANY of the specified prefixes.
    /// The matching uses the <see cref="string.StartsWith(string)"/> method, which is
    /// case-sensitive and culture-specific.
    /// </para>
    ///
    /// <para>
    /// <strong>Example Usage:</strong>
    /// If <c>Names</c> is set to <c>["Acme.Services", "Acme.Data", "ThirdParty.Extensions"]</c>,
    /// the following assemblies would be included:
    /// <list type="bullet">
    /// <item>Acme.Services.dll</item>
    /// <item>Acme.Services.WebApi.dll</item>
    /// <item>Acme.Data.dll</item>
    /// <item>Acme.Data.EntityFramework.dll</item>
    /// <item>ThirdParty.Extensions.dll</item>
    /// <item>ThirdParty.Extensions.Logging.dll</item>
    /// </list>
    /// But these would be excluded:
    /// <list type="bullet">
    /// <item>Acme.Core.dll (doesn't match any prefix)</item>
    /// <item>OtherVendor.Services.dll (doesn't match any prefix)</item>
    /// <item>System.dll (doesn't match any prefix)</item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// <strong>Performance Consideration:</strong>
    /// Using a longer list of names may slightly impact startup performance as each assembly
    /// name is checked against all specified prefixes. For large numbers of prefixes,
    /// consider using a single <see cref="Prefix"/> if possible.
    /// </para>
    /// </remarks>
    public IReadOnlyList<string>? Names { get; [UsedImplicitly] init; }

    /// <summary>
    /// Determines whether the specified <see cref="MappingConfig"/> is equal to the current instance.
    /// Compares both the <see cref="Prefix"/> and <see cref="Names"/> properties using value equality.
    /// For the <see cref="Names"/> collection, performs element-wise comparison to ensure structural equality.
    /// </summary>
    /// <param name="other">The <see cref="MappingConfig"/> to compare with the current instance.</param>
    /// <returns>
    /// <c>true</c> if the specified <see cref="MappingConfig"/> is equal to the current instance; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// This method provides proper value-based equality comparison for the record type, ensuring that:
    /// <list type="bullet">
    /// <item>Null references are handled correctly</item>
    /// <item>Reference equality is checked for performance optimization</item>
    /// <item>String properties are compared using default string equality</item>
    /// <item>Collection properties are compared element-wise using <see cref="Enumerable.SequenceEqual{TSource}(IEnumerable{TSource}, IEnumerable{TSource})"/></item>
    /// <item>Null collections are treated as equivalent to each other but not to non-null collections</item>
    /// </list>
    ///
    /// <para>
    /// <strong>Example Usage:</strong>
    /// <code>
    /// var config1 = new MappingConfig { Prefix = "Test", Names = new[] { "Assembly1", "Assembly2" } };
    /// var config2 = new MappingConfig { Prefix = "Test", Names = new[] { "Assembly1", "Assembly2" } };
    /// bool areEqual = config1.Equals(config2); // Returns true
    /// </code>
    /// </para>
    /// </remarks>
    public bool Equals(MappingConfig? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Prefix == other.Prefix &&
               (Names == null && other.Names == null ||
                Names != null && other.Names != null && Names.SequenceEqual(other.Names));
    }

    /// <summary>
    /// Returns a hash code for the current <see cref="MappingConfig"/> instance.
    /// Combines hash codes from both the <see cref="Prefix"/> and <see cref="Names"/> properties
    /// to ensure consistent hash code generation for value equality scenarios.
    /// </summary>
    /// <returns>A 32-bit signed integer hash code.</returns>
    /// <remarks>
    /// This method ensures that instances with equal values produce the same hash code,
    /// which is essential for proper behavior in hash-based collections like <see cref="Dictionary{TKey,TValue}"/>
    /// and <see cref="HashSet{T}"/>.
    ///
    /// <para>
    /// <strong>Hash Code Calculation:</strong>
    /// <list type="number">
    /// <item>Starts with the hash code of the <see cref="Prefix"/> property</item>
    /// <item>For non-null <see cref="Names"/> collections, incorporates the hash code of each element</item>
    /// <item>Uses <see cref="HashCode"/> struct for efficient and collision-resistant hash combination</item>
    /// <item>Handles null collections appropriately to maintain consistency with <see cref="Equals(MappingConfig?)"/></item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// <strong>Performance Considerations:</strong>
    /// The hash code calculation has O(n) complexity where n is the number of elements in the <see cref="Names"/> collection.
    /// For large collections, consider caching the hash code if the instance is immutable and used frequently in hash-based operations.
    /// </para>
    ///
    /// <para>
    /// <strong>Consistency Guarantee:</strong>
    /// This implementation guarantees that if <c>config1.Equals(config2)</c> returns <c>true</c>,
    /// then <c>config1.GetHashCode() == config2.GetHashCode()</c> will also be <c>true</c>.
    /// </para>
    /// </remarks>
    public override int GetHashCode()
    {
        var hashCode = new HashCode();
        hashCode.Add(Prefix);

        if (Names != null)
        {
            foreach (var name in Names)
            {
                hashCode.Add(name);
            }
        }

        return hashCode.ToHashCode();
    }
}
