# Monitoring Guide

## Overview

This document outlines the monitoring strategy for the DynamoDB Processor application, including metrics collection, alerting, and observability practices.

## Monitoring Architecture

### 1. Metrics Collection

1. **CloudWatch Metrics**
   ```csharp
   public class MetricsService
   {
       private readonly IAmazonCloudWatch _cloudWatch;
       private readonly ILogger<MetricsService> _logger;
       private readonly string _namespace;

       public MetricsService(
           IAmazonCloudWatch cloudWatch,
           ILogger<MetricsService> logger,
           string @namespace)
       {
           _cloudWatch = cloudWatch;
           _logger = logger;
           _namespace = @namespace;
       }

       public async Task RecordMetric(
           string name,
           double value,
           Dictionary<string, string> dimensions)
       {
           try
           {
               var metricData = new MetricDatum
               {
                   MetricName = name,
                   Value = value,
                   Unit = StandardUnit.Count,
                   Timestamp = DateTime.UtcNow,
                   Dimensions = dimensions.Select(d => new Dimension
                   {
                       Name = d.Key,
                       Value = d.Value
                   }).ToList()
               };

               await _cloudWatch.PutMetricDataAsync(new PutMetricDataRequest
               {
                   Namespace = _namespace,
                   MetricData = new List<MetricDatum> { metricData }
               });

               _logger.LogDebug("Recorded metric {MetricName} with value {Value}", name, value);
           }
           catch (Exception ex)
           {
               _logger.LogError(ex, "Failed to record metric {MetricName}", name);
               throw;
           }
       }
   }
   ```

2. **Custom Metrics**
   ```csharp
   public class QueryMetrics
   {
       public async Task RecordQueryMetrics(
           string userId,
           int recordCount,
           double duration,
           bool isCacheHit)
       {
           var dimensions = new Dictionary<string, string>
           {
               { "UserId", userId },
               { "Environment", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") }
           };

           await _metricsService.RecordMetric("QueryCount", 1, dimensions);
           await _metricsService.RecordMetric("RecordCount", recordCount, dimensions);
           await _metricsService.RecordMetric("QueryDuration", duration, dimensions);
           await _metricsService.RecordMetric(
               isCacheHit ? "CacheHit" : "CacheMiss",
               1,
               dimensions);
       }
   }
   ```

### 2. Logging

1. **Structured Logging**
   ```csharp
   public class LoggingService
   {
       private readonly ILogger<LoggingService> _logger;

       public LoggingService(ILogger<LoggingService> logger)
       {
           _logger = logger;
       }

       public void LogQuery(
           string userId,
           string queryType,
           Dictionary<string, object> parameters)
       {
           _logger.LogInformation(
               "Query executed by {UserId}: {QueryType} with parameters {@Parameters}",
               userId,
               queryType,
               parameters);
       }

       public void LogError(
           string userId,
           string operation,
           Exception ex)
       {
           _logger.LogError(
               ex,
               "Error in {Operation} by {UserId}",
               operation,
               userId);
       }
   }
   ```

2. **Log Configuration**
   ```csharp
   services.AddLogging(builder =>
   {
       builder.AddSerilog(new LoggerConfiguration()
           .WriteTo.CloudWatch(new CloudWatchSinkOptions
           {
               LogGroupName = "/aws/lambda/dynamodb-processor",
               LogStreamName = $"{DateTime.UtcNow:yyyy/MM/dd}"
           })
           .Enrich.FromLogContext()
           .Enrich.WithProperty("Application", "DynamoDBProcessor")
           .CreateLogger());
   });
   ```

### 3. Tracing

1. **X-Ray Integration**
   ```csharp
   public class TracingService
   {
       private readonly IAmazonXRay _xray;
       private readonly ILogger<TracingService> _logger;

       public async Task TraceOperation(
           string operationName,
           Func<Task> operation)
       {
           var segment = new Segment("DynamoDBProcessor");
           try
           {
               segment.AddAnnotation("Operation", operationName);
               await operation();
           }
           catch (Exception ex)
           {
               segment.AddException(ex);
               throw;
           }
           finally
           {
               await segment.CloseAsync();
           }
       }
   }
   ```

2. **Trace Configuration**
   ```csharp
   services.AddXRay(options =>
   {
       options.ServiceName = "DynamoDBProcessor";
       options.CollectSqlQueries = true;
       options.TraceHttpRequests = true;
   });
   ```

## Alerting

### 1. CloudWatch Alarms

1. **Error Rate Alarm**
   ```csharp
   public class AlarmService
   {
       private readonly IAmazonCloudWatch _cloudWatch;

       public async Task CreateErrorRateAlarm()
       {
           await _cloudWatch.PutMetricAlarmAsync(new PutMetricAlarmRequest
           {
               AlarmName = "DynamoDBProcessor-ErrorRate",
               MetricName = "ErrorCount",
               Namespace = "DynamoDBProcessor",
               Statistic = Statistic.Sum,
               Period = 300,
               EvaluationPeriods = 2,
               Threshold = 5,
               ComparisonOperator = ComparisonOperator.GreaterThanThreshold,
               AlarmActions = new List<string> { "arn:aws:sns:region:account:alert-topic" }
           });
       }
   }
   ```

2. **Latency Alarm**
   ```csharp
   public async Task CreateLatencyAlarm()
   {
       await _cloudWatch.PutMetricAlarmAsync(new PutMetricAlarmRequest
       {
           AlarmName = "DynamoDBProcessor-Latency",
           MetricName = "QueryDuration",
           Namespace = "DynamoDBProcessor",
           Statistic = Statistic.Average,
           Period = 300,
           EvaluationPeriods = 3,
           Threshold = 1000,
           ComparisonOperator = ComparisonOperator.GreaterThanThreshold,
           AlarmActions = new List<string> { "arn:aws:sns:region:account:alert-topic" }
       });
   }
   ```

### 2. Health Checks

1. **Application Health**
   ```csharp
   public class HealthCheckService : IHealthCheck
   {
       private readonly IAmazonDynamoDB _dynamoDB;
       private readonly ILogger<HealthCheckService> _logger;

       public async Task<HealthCheckResult> CheckHealthAsync(
           HealthCheckContext context,
           CancellationToken cancellationToken = default)
       {
           try
           {
               await _dynamoDB.DescribeTableAsync("audit-records");
               return HealthCheckResult.Healthy("DynamoDB connection is healthy");
           }
           catch (Exception ex)
           {
               _logger.LogError(ex, "Health check failed");
               return HealthCheckResult.Unhealthy("DynamoDB connection failed", ex);
           }
       }
   }
   ```

2. **Dependency Health**
   ```csharp
   services.AddHealthChecks()
       .AddCheck<DynamoDBHealthCheck>("DynamoDB")
       .AddCheck<CacheHealthCheck>("Cache")
       .AddCheck<ApiGatewayHealthCheck>("API Gateway");
   ```

## Dashboards

### 1. CloudWatch Dashboard

1. **Query Metrics**
   ```json
   {
     "widgets": [
       {
         "type": "metric",
         "properties": {
           "metrics": [
             ["DynamoDBProcessor", "QueryCount"],
             ["DynamoDBProcessor", "RecordCount"],
             ["DynamoDBProcessor", "QueryDuration"]
           ],
           "period": 300,
           "stat": "Sum",
           "region": "us-east-1",
           "title": "Query Metrics"
         }
       }
     ]
   }
   ```

2. **Cache Metrics**
   ```json
   {
     "widgets": [
       {
         "type": "metric",
         "properties": {
           "metrics": [
             ["DynamoDBProcessor", "CacheHit"],
             ["DynamoDBProcessor", "CacheMiss"]
           ],
           "period": 300,
           "stat": "Sum",
           "region": "us-east-1",
           "title": "Cache Performance"
         }
       }
     ]
   }
   ```

### 2. Custom Dashboards

1. **Performance Dashboard**
   ```csharp
   public class DashboardService
   {
       private readonly IAmazonCloudWatch _cloudWatch;

       public async Task CreatePerformanceDashboard()
       {
           var dashboard = new Dashboard
           {
               DashboardName = "DynamoDBProcessor-Performance",
               DashboardBody = JsonSerializer.Serialize(new
               {
                   widgets = new[]
                   {
                       new
                       {
                           type = "metric",
                           properties = new
                           {
                               metrics = new[]
                               {
                                   new[] { "DynamoDBProcessor", "QueryDuration" },
                                   new[] { "DynamoDBProcessor", "RecordCount" }
                               },
                               period = 300,
                               stat = "Average",
                               region = "us-east-1",
                               title = "Query Performance"
                           }
                       }
                   }
               })
           };

           await _cloudWatch.PutDashboardAsync(new PutDashboardRequest
           {
               DashboardName = dashboard.DashboardName,
               DashboardBody = dashboard.DashboardBody
           });
       }
   }
   ```

2. **Error Dashboard**
   ```csharp
   public async Task CreateErrorDashboard()
   {
       var dashboard = new Dashboard
       {
           DashboardName = "DynamoDBProcessor-Errors",
           DashboardBody = JsonSerializer.Serialize(new
           {
               widgets = new[]
               {
                   new
                   {
                       type = "metric",
                       properties = new
                       {
                           metrics = new[]
                           {
                               new[] { "DynamoDBProcessor", "ErrorCount" },
                               new[] { "DynamoDBProcessor", "ErrorRate" }
                           },
                           period = 300,
                           stat = "Sum",
                           region = "us-east-1",
                           title = "Error Metrics"
                       }
                   }
               }
           })
       };

       await _cloudWatch.PutDashboardAsync(new PutDashboardRequest
       {
           DashboardName = dashboard.DashboardName,
           DashboardBody = dashboard.DashboardBody
       });
   }
   ```

## Monitoring Best Practices

### 1. Metric Collection

1. **Key Metrics**
   - Query success/error rates
   - Query duration
   - Cache hit/miss rates
   - Records retrieved
   - Lambda execution metrics
   - DynamoDB capacity units

2. **Metric Dimensions**
   - User ID
   - Environment
   - Query type
   - Table name
   - Region

### 2. Logging Best Practices

1. **Log Levels**
   - Debug: Detailed information
   - Info: General information
   - Warning: Potential issues
   - Error: Error conditions
   - Critical: System failures

2. **Log Structure**
   - Timestamp
   - Log level
   - Message
   - Context
   - Exception details
   - Correlation ID

### 3. Alerting Best Practices

1. **Alert Thresholds**
   - Error rate > 1%
   - Latency > 500ms
   - Cache hit rate < 80%
   - DynamoDB throttling
   - Lambda errors

2. **Alert Actions**
   - SNS notifications
   - Slack integration
   - PagerDuty integration
   - Email notifications

## Monitoring Tools

### 1. AWS Services

- [CloudWatch](https://aws.amazon.com/cloudwatch/)
- [X-Ray](https://aws.amazon.com/xray/)
- [CloudTrail](https://aws.amazon.com/cloudtrail/)

### 2. Third-Party Tools

- [Datadog](https://www.datadoghq.com/)
- [New Relic](https://newrelic.com/)
- [Splunk](https://www.splunk.com/)

### 3. Custom Tools

- [Grafana](https://grafana.com/)
- [Prometheus](https://prometheus.io/)
- [ELK Stack](https://www.elastic.co/elk-stack)

## Support

### 1. Documentation

- [AWS CloudWatch Documentation](https://docs.aws.amazon.com/cloudwatch/)
- [AWS X-Ray Documentation](https://docs.aws.amazon.com/xray/)
- [Monitoring Best Practices](https://aws.amazon.com/architecture/well-architected/)

### 2. Tools

- [AWS CloudWatch Console](https://console.aws.amazon.com/cloudwatch/)
- [AWS X-Ray Console](https://console.aws.amazon.com/xray/)
- [AWS CloudTrail Console](https://console.aws.amazon.com/cloudtrail/)

### 3. Support

- [AWS Support](https://aws.amazon.com/support/)
- [Monitoring Support](https://aws.amazon.com/cloudwatch/support/)
- [X-Ray Support](https://aws.amazon.com/xray/support/) 