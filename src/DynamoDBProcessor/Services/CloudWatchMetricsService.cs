using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;
using Microsoft.Extensions.Logging;

namespace DynamoDBProcessor.Services;

/// <summary>
/// Implementation of IMetricsService using AWS CloudWatch for metrics collection.
/// </summary>
public class CloudWatchMetricsService : IMetricsService
{
    private readonly IAmazonCloudWatch _cloudWatchClient;
    private readonly ILogger<CloudWatchMetricsService> _logger;
    private readonly string _namespace;

    public CloudWatchMetricsService(
        IAmazonCloudWatch cloudWatchClient,
        ILogger<CloudWatchMetricsService> logger,
        string @namespace = "DynamoDBProcessor")
    {
        _cloudWatchClient = cloudWatchClient;
        _logger = logger;
        _namespace = @namespace;
    }

    public async Task RecordCountAsync(string name, double value, Dictionary<string, string>? dimensions = null)
    {
        try
        {
            var metricData = new MetricDatum
            {
                MetricName = name,
                Value = value,
                Unit = StandardUnit.Count,
                Timestamp = DateTime.UtcNow,
                Dimensions = ConvertDimensions(dimensions)
            };

            var request = new PutMetricDataRequest
            {
                Namespace = _namespace,
                MetricData = new List<MetricDatum> { metricData }
            };

            await _cloudWatchClient.PutMetricDataAsync(request);
            _logger.LogDebug("Recorded count metric: {Name} = {Value}", name, value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording count metric: {Name}", name);
        }
    }

    public async Task RecordTimingAsync(string name, double milliseconds, Dictionary<string, string>? dimensions = null)
    {
        try
        {
            var metricData = new MetricDatum
            {
                MetricName = name,
                Value = milliseconds,
                Unit = StandardUnit.Milliseconds,
                Timestamp = DateTime.UtcNow,
                Dimensions = ConvertDimensions(dimensions)
            };

            var request = new PutMetricDataRequest
            {
                Namespace = _namespace,
                MetricData = new List<MetricDatum> { metricData }
            };

            await _cloudWatchClient.PutMetricDataAsync(request);
            _logger.LogDebug("Recorded timing metric: {Name} = {Value}ms", name, milliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording timing metric: {Name}", name);
        }
    }

    public async Task RecordGaugeAsync(string name, double value, Dictionary<string, string>? dimensions = null)
    {
        try
        {
            var metricData = new MetricDatum
            {
                MetricName = name,
                Value = value,
                Unit = StandardUnit.None,
                Timestamp = DateTime.UtcNow,
                Dimensions = ConvertDimensions(dimensions)
            };

            var request = new PutMetricDataRequest
            {
                Namespace = _namespace,
                MetricData = new List<MetricDatum> { metricData }
            };

            await _cloudWatchClient.PutMetricDataAsync(request);
            _logger.LogDebug("Recorded gauge metric: {Name} = {Value}", name, value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording gauge metric: {Name}", name);
        }
    }

    private List<Dimension> ConvertDimensions(Dictionary<string, string>? dimensions)
    {
        if (dimensions == null)
        {
            return new List<Dimension>();
        }

        return dimensions.Select(d => new Dimension
        {
            Name = d.Key,
            Value = d.Value
        }).ToList();
    }
} 