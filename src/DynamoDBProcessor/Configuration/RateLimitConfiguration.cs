using AspNetCoreRateLimit;
using Microsoft.Extensions.DependencyInjection;

namespace DynamoDBProcessor.Configuration;

public static class RateLimitConfiguration
{
    public static void ConfigureRateLimiting(this IServiceCollection services)
    {
        // Add rate limiting services
        services.AddMemoryCache();
        services.Configure<IpRateLimitOptions>(options =>
        {
            options.EnableEndpointRateLimiting = true;
            options.StackBlockedRequests = false;
            options.GeneralRules = new List<RateLimitRule>
            {
                new RateLimitRule
                {
                    Endpoint = "*",
                    Period = "1m",
                    Limit = 100
                },
                new RateLimitRule
                {
                    Endpoint = "*",
                    Period = "1h",
                    Limit = 1000
                }
            };
        });

        services.Configure<IpRateLimitPolicies>(options =>
        {
            options.IpRules = new List<IpRateLimitPolicy>
            {
                new IpRateLimitPolicy
                {
                    Ip = "127.0.0.1",
                    Rules = new List<RateLimitRule>
                    {
                        new RateLimitRule
                        {
                            Endpoint = "*",
                            Period = "1m",
                            Limit = 1000
                        }
                    }
                }
            };
        });

        services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
        services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
        services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
        services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();
        services.AddInMemoryRateLimiting();
    }
} 