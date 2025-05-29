using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;
using Microsoft.Extensions.Logging;

namespace DynamoDBProcessor.Services;

public interface IMetricsService
{
    Task RecordCountAsync(string metricName, double value);
    Task RecordLatencyAsync(string metricName, TimeSpan duration);
}

public class MetricsService : IMetricsService
{
    private readonly IAmazonCloudWatch _cloudWatch;
    private readonly ILogger<MetricsService> _logger;
    private readonly string _namespace;

    public MetricsService(
        IAmazonCloudWatch cloudWatch,
        ILogger<MetricsService> logger)
    {
        _cloudWatch = cloudWatch;
        _logger = logger;
        _namespace = "DynamoDBProcessor";
    }

    public async Task RecordCountAsync(string metricName, double value)
    {
        try
        {
            var request = new PutMetricDataRequest
            {
                Namespace = _namespace,
                MetricData = new List<MetricDatum>
                {
                    new()
                    {
                        MetricName = metricName,
                        Value = value,
                        Unit = StandardUnit.Count,
                        Timestamp = DateTime.UtcNow
                    }
                }
            };

            await _cloudWatch.PutMetricDataAsync(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording metric {MetricName}", metricName);
        }
    }

    public async Task RecordLatencyAsync(string metricName, TimeSpan duration)
    {
        try
        {
            var request = new PutMetricDataRequest
            {
                Namespace = _namespace,
                MetricData = new List<MetricDatum>
                {
                    new()
                    {
                        MetricName = metricName,
                        Value = duration.TotalMilliseconds,
                        Unit = StandardUnit.Milliseconds,
                        Timestamp = DateTime.UtcNow
                    }
                }
            };

            await _cloudWatch.PutMetricDataAsync(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording latency metric {MetricName}", metricName);
        }
    }
} 