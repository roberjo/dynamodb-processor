namespace DynamoDBProcessor.Services;

/// <summary>
/// Interface for metrics service that provides methods for recording application metrics.
/// </summary>
public interface IMetricsService
{
    /// <summary>
    /// Records a count metric.
    /// </summary>
    /// <param name="name">The metric name</param>
    /// <param name="value">The metric value</param>
    /// <param name="dimensions">Optional dimensions for the metric</param>
    Task RecordCountAsync(string name, double value, Dictionary<string, string>? dimensions = null);

    /// <summary>
    /// Records a timing metric.
    /// </summary>
    /// <param name="name">The metric name</param>
    /// <param name="milliseconds">The duration in milliseconds</param>
    /// <param name="dimensions">Optional dimensions for the metric</param>
    Task RecordTimingAsync(string name, double milliseconds, Dictionary<string, string>? dimensions = null);

    /// <summary>
    /// Records a gauge metric.
    /// </summary>
    /// <param name="name">The metric name</param>
    /// <param name="value">The metric value</param>
    /// <param name="dimensions">Optional dimensions for the metric</param>
    Task RecordGaugeAsync(string name, double value, Dictionary<string, string>? dimensions = null);
} 