using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;
using Microsoft.Extensions.Logging;

namespace DynamoDBProcessor.Services;

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

    public async Task RecordCountAsync(string metricName, double value, Dictionary<string, string>? dimensions = null)
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
                        TimestampUtc = DateTime.UtcNow,
                        Dimensions = dimensions?.Select(d => new Dimension
                        {
                            Name = d.Key,
                            Value = d.Value
                        }).ToList()
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

    public async Task RecordTimingAsync(string metricName, double value, Dictionary<string, string>? dimensions = null)
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
                        Unit = StandardUnit.Milliseconds,
                        TimestampUtc = DateTime.UtcNow,
                        Dimensions = dimensions?.Select(d => new Dimension
                        {
                            Name = d.Key,
                            Value = d.Value
                        }).ToList()
                    }
                }
            };

            await _cloudWatch.PutMetricDataAsync(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording timing metric {MetricName}", metricName);
        }
    }

    public async Task RecordGaugeAsync(string metricName, double value, Dictionary<string, string>? dimensions = null)
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
                        Unit = StandardUnit.None,
                        TimestampUtc = DateTime.UtcNow,
                        Dimensions = dimensions?.Select(d => new Dimension
                        {
                            Name = d.Key,
                            Value = d.Value
                        }).ToList()
                    }
                }
            };

            await _cloudWatch.PutMetricDataAsync(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording gauge metric {MetricName}", metricName);
        }
    }

    public async Task RecordLatencyAsync(string metricName, TimeSpan duration)
    {
        await RecordTimingAsync(metricName, duration.TotalMilliseconds);
    }
} 