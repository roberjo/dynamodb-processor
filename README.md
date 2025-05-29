# DynamoDB Query Processor

A production-ready AWS Lambda function for efficiently querying audit records from DynamoDB with advanced features like caching, rate limiting, and monitoring.

## Features

- **Efficient Querying**: Optimized DynamoDB queries using GSI (Global Secondary Index)
- **Caching**: In-memory caching for frequently accessed queries
- **Rate Limiting**: IP-based rate limiting to prevent abuse
- **Security**: API key authentication and security headers
- **Monitoring**: Comprehensive CloudWatch metrics and logging
- **High Availability**: Multi-AZ deployment with cross-region replication
- **Documentation**: Swagger/OpenAPI documentation
- **Testing**: Unit and integration tests with high coverage

## Architecture

```
┌─────────────┐     ┌─────────────┐     ┌─────────────┐
│  API Gateway│────▶│   Lambda    │────▶│  DynamoDB   │
└─────────────┘     └─────────────┘     └─────────────┘
                           │
                    ┌──────┴──────┐
                    │   CloudWatch│
                    └─────────────┘
```

## Prerequisites

- .NET 8.0 SDK
- AWS CLI configured with appropriate credentials
- DynamoDB table with the following schema:
  - Partition Key: `PK` (String)
  - Sort Key: `SK` (String)
  - GSI1: `GS1_PK` (String), `GS1_SK` (String)
  - GSI2: `GSI2_PK` (String), `GSI2_SK` (String)

## Getting Started

1. Clone the repository:
   ```powershell
   git clone https://github.com/yourusername/dynamodb-processor.git
   cd dynamodb-processor
   ```

2. Install dependencies:
   ```powershell
   dotnet restore
   ```

3. Configure environment variables:
   ```powershell
   $env:DYNAMODB_TABLE_NAME = "your-table-name"
   $env:AWS_REGION = "us-east-1"
   ```

4. Run tests:
   ```powershell
   dotnet test
   ```

5. Deploy to AWS:
   ```powershell
   dotnet publish -c Release
   aws lambda update-function-code --function-name dynamodb-processor --zip-file fileb://publish.zip
   ```

## API Documentation

The API documentation is available via Swagger UI when running in development mode:
```
https://your-api-gateway-url/swagger
```

### Endpoints

- `POST /api/query`: Query audit records
  - Request body:
    ```json
    {
      "userId": "string",
      "startDate": "2024-01-01T00:00:00Z",
      "endDate": "2024-01-02T00:00:00Z",
      "systemId": "string",
      "resourceId": "string"
    }
    ```

## Monitoring

The application includes comprehensive monitoring through AWS CloudWatch:

- Query success/error rates
- Query duration
- Cache hit/miss rates
- Records retrieved
- Lambda function metrics
- DynamoDB capacity units

View the dashboard in AWS CloudWatch:
```
https://console.aws.amazon.com/cloudwatch/home?region=us-east-1#dashboards:name=DynamoDBProcessor
```

## Testing

### Unit Tests

Run unit tests:
```powershell
dotnet test src/DynamoDBProcessor.Tests
```

### Integration Tests

Run integration tests:
```powershell
dotnet test src/DynamoDBProcessor.IntegrationTests
```

## CI/CD

The project uses GitHub Actions for continuous integration and deployment:

- Builds on every push and pull request
- Runs unit and integration tests
- Deploys to AWS Lambda on main branch
- Uploads test results and deployment artifacts

## Backup and Recovery

The project includes a comprehensive backup and disaster recovery plan:

- Continuous DynamoDB backups with point-in-time recovery
- Daily on-demand backups
- Cross-region replication
- Automated recovery procedures

See [Backup and Recovery Plan](docs/backup-recovery.md) for details.

## Security

- API key authentication
- Security headers middleware
- Rate limiting
- Input validation
- Secure configuration management

## Contributing

1. Fork the repository
2. Create a feature branch
3. Commit your changes
4. Push to the branch
5. Create a Pull Request

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Support

For support, please contact:
- System Administrator: admin@example.com
- Database Administrator: dba@example.com
- Security Team: security@example.com 