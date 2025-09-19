namespace FlexKit.Logging.Detection;

/// <summary>
/// Provides the ability to determine the type associated with a filter provider.
/// It enables the retrieval of the fully qualified type name for filtering logging behavior.
/// </summary>
public interface IFilterTypeProvider
{
    /// <summary>
    /// Retrieves the filter type associated with the provider.
    /// </summary>
    /// <returns>
    /// The fully qualified <see cref="Type"/> of the filter logging builder extension,
    /// or null if the type cannot be found.
    /// </returns>
    Type? GetFilterType() => Type.GetType(MelNames.FilterType);

    /// <summary>
    /// Creates a default implementation of the <see cref="IFilterTypeProvider"/>.
    /// </summary>
    /// <returns>
    /// An instance of the default implementation of <see cref="IFilterTypeProvider"/>.
    /// </returns>
    static IFilterTypeProvider CreateDefault() => new DefaultFilterTypeProvider();

    /// <inheritdoc />
    private sealed class DefaultFilterTypeProvider : IFilterTypeProvider;
}
