namespace FlexKit.Logging.Core;

/// <summary>
/// Collects items in batches and triggers processing when size or timeout thresholds are reached.
/// </summary>
/// <typeparam name="T">The type of items to collect in batches.</typeparam>
/// <remarks>
/// Initializes a new instance of the <see cref="BatchCollector{T}"/> class.
/// </remarks>
/// <param name="maxSize">The maximum number of items in a batch before triggering a flush.</param>
/// <param name="timeout">The maximum time to wait before triggering a flush, regardless of batch size.</param>
internal class BatchCollector<T>(
    int maxSize,
    in TimeSpan timeout)
{
    /// <summary>
    /// Holds the collection of items being accumulated for the current batch in the <see cref="BatchCollector{T}"/>.
    /// </summary>
    /// <remarks>
    /// Items are added to this variable until the maximum batch size or timeout is reached,
    /// at which point the batch is either processed or flushed.
    /// </remarks>
    private readonly List<T> _batch = new(maxSize);

    /// <summary>
    /// Represents the maximum duration to wait before forcing a batch flush,
    /// regardless of the current number of items in the batch.
    /// </summary>
    /// <remarks>
    /// This value acts as a timeout threshold to ensure timely processing of batches
    /// even if the maximum batch size is not reached.
    /// </remarks>
    private readonly TimeSpan _timeout = timeout;

    /// <summary>
    /// Stores the timestamp of the last flush operation performed by the <see cref="BatchCollector{T}"/>.
    /// </summary>
    /// <remarks>
    /// This variable is updated each time a batch is flushed, either automatically when the
    /// size or timeout thresholds are reached, or manually through the <see cref="Flush"/> method.
    /// It is used to determine if the timeout threshold for flushing has been exceeded.
    /// </remarks>
    private DateTime _lastFlush = DateTime.UtcNow;

    /// <summary>
    /// Attempts to add an item to the current batch.
    /// </summary>
    /// <param name="item">The item to add to the batch.</param>
    /// <param name="batchToProcess">
    /// When this method returns <c>true</c>, contains the batch that should be processed.
    /// When this method returns <c>false</c>, this parameter is <c>null</c>.
    /// </param>
    /// <returns>
    /// <c>true</c> if the batch should be processed (either size or timeout threshold reached);
    /// otherwise, <c>false</c>.
    /// </returns>
    public bool TryAdd(
        T item,
        out IReadOnlyList<T>? batchToProcess)
    {
        _batch.Add(item);

        if (ShouldFlush())
        {
            batchToProcess = _batch.ToList();
            _batch.Clear();
            _lastFlush = DateTime.UtcNow;
            return true;
        }

        batchToProcess = null;
        return false;
    }

    /// <summary>
    /// Forces a flush of the current batch, returning all items regardless of size or timeout.
    /// </summary>
    /// <returns>A read-only list containing all items in the current batch.</returns>
    public IReadOnlyList<T> Flush()
    {
        var result = _batch.ToList();
        _batch.Clear();
        _lastFlush = DateTime.UtcNow;
        return result;
    }

    /// <summary>
    /// Determines whether the current batch should be flushed based on size or timeout criteria.
    /// </summary>
    /// <returns>
    /// <c>true</c> if the batch has reached the maximum size or the timeout has elapsed;
    /// otherwise, <c>false</c>.
    /// </returns>
    private bool ShouldFlush() =>
        _batch.Count >= maxSize ||
        DateTime.UtcNow - _lastFlush >= _timeout;
}
