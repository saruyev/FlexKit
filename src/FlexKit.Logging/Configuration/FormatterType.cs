namespace FlexKit.Logging.Configuration;

/// <summary>
/// Enumeration of available message formatter types.
/// Defines the different formatting strategies supported by FlexKit.Logging.
/// </summary>
public enum FormatterType
{
    /// <summary>
    /// Standard structured formatting using template placeholders.
    /// Example: "Method ProcessPayment completed in 450 ms"
    /// </summary>
    StandardStructured,

    /// <summary>
    /// Full JSON serialization of method execution data.
    /// Example: {"method_name": "ProcessPayment", "duration": 450, "success": true}
    /// </summary>
    Json,

    /// <summary>
    /// Custom template-based formatting with per-service templates.
    /// Example: "Processing payment of {Amount} for {Customer}"
    /// </summary>
    CustomTemplate,

    /// <summary>
    /// Combination of a structured message with JSON metadata.
    /// Example: "Method ProcessPayment completed | {"duration": 450, "thread_id": 12}"
    /// </summary>
    Hybrid,

    /// <summary>
    /// Different templates for successful vs. failed method executions.
    /// Example Success: "Method ProcessPayment completed in 450 ms"
    /// Example Error: "Method ProcessPayment failed: InvalidOperationException after 450 ms"
    /// </summary>
    SuccessError
}
