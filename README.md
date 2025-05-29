# DynamoDB Query Processor

This project implements an AWS Lambda function using ASP.NET Core 8 that processes queries against a DynamoDB table with the following structure:

## Table Design

- **Partition Key (PK)**: `${system_id}#${year_month_day}`
- **Sort Key (SK)**: `${timestamp}#${audit_id}`
- **GSI1**:
  - Partition Key: `GS1_PK #{user_id}`
  - Sort Key: `GS1_SK #{timestamp}#{audit_id}`
- **GSI2**:
  - Partition Key: `GSI2_PK #{resource_id}`
  - Sort Key: `GSI2_SK #{timestamp}#{audit_id}`

## Features

- Query processing with multiple filter criteria
- Pagination support
- Input validation
- AWS API Gateway integration
- DynamoDB query optimization

## Project Structure

```
src/
├── DynamoDBProcessor/
│   ├── Models/
│   │   ├── QueryRequest.cs
│   │   ├── QueryResponse.cs
│   │   └── AuditRecord.cs
│   ├── Services/
│   │   ├── IDynamoDBService.cs
│   │   └── DynamoDBService.cs
│   ├── Validators/
│   │   └── QueryRequestValidator.cs
│   ├── Function.cs
│   └── Program.cs
terraform/
├── modules/
│   └── lambda/
├── environments/
│   ├── dev/
│   ├── staging/
│   └── prod/
.github/
└── workflows/
```

## Infrastructure as Code (Terraform Cloud)

- All infrastructure code is in the `terraform/` directory.
- Multi-environment support: `terraform/environments/dev`, `staging`, `prod`.
- Each environment uses its own [Terraform Cloud](https://app.terraform.io/) workspace.
- Shared modules in `terraform/modules/`.

### Deployment Environments

1. **Development (dev)**
   - Automatically deployed on merge to main branch
   - Used for testing and development
   - Terraform workspace: `dynamodb-processor-dev`

2. **Staging**
   - Manually triggered deployment
   - Used for pre-production testing
   - Terraform workspace: `dynamodb-processor-staging`

3. **Production**
   - Manually triggered deployment with approval
   - Used for production workloads
   - Terraform workspace: `dynamodb-processor-prod`

### Required Secrets

Set these secrets in your GitHub repository:
- `TF_API_TOKEN`: Terraform Cloud API token for authentication

### Deploying

1. **Automatic Deployment to Dev**
   - Push to main branch
   - GitHub Actions will automatically:
     - Build and package the Lambda
     - Deploy to dev environment using Terraform

2. **Manual Deployment to Staging/Production**
   - Go to Actions tab in GitHub
   - Select "Deploy Lambda" workflow
   - Click "Run workflow"
   - Choose environment (staging/production)
   - For production, additional approval may be required

## CI/CD with GitHub Actions

### Build Process
- Lints C# and Terraform code
- Builds and publishes the .NET Lambda
- Packages the build output and Terraform files
- Produces a unique zip artifact named with the project version and source hash

### Deployment Process
- Automatic deployment to dev on merge to main
- Manual deployment to staging/production with approval
- Uses Terraform Cloud for infrastructure management
- Environment-specific configurations and variables

See `.github/workflows/build.yml` and `.github/workflows/deploy.yml` for details.

## API Usage

The Lambda function accepts POST requests with a JSON body containing:

```json
{
    "user_id": "string",
    "start_date": "yyyy-MM-dd",
    "end_date": "yyyy-MM-dd",
    "system_id": "string",
    "resource_id": "string"
}
```

The response includes paginated results and a continuation token if more results are available.

## Development Setup

### Prerequisites
- .NET 8 SDK
- Visual Studio 2022 or VS Code with C# extensions
- AWS CLI configured with appropriate credentials
- Docker Desktop (for local DynamoDB)
- Terraform CLI

### Local Development Environment

1. **Clone and Setup**
   ```powershell
   git clone https://github.com/yourusername/dynamodb-processor.git
   Set-Location dynamodb-processor
   ```

2. **Local DynamoDB Setup**
   ```powershell
   docker run -p 8000:8000 amazon/dynamodb-local
   ```

3. **Environment Variables**
   Create a `src/DynamoDBProcessor/appsettings.Development.json`:
   ```json
   {
     "AWS": {
       "Region": "us-east-1",
       "DynamoDb": {
         "ServiceUrl": "http://localhost:8000"
       }
     },
     "Logging": {
       "LogLevel": {
         "Default": "Debug",
         "Microsoft": "Information"
       }
     }
   }
   ```

4. **Local Development**
   ```powershell
   Set-Location src/DynamoDBProcessor
   dotnet restore
   dotnet build
   dotnet run
   ```

### Debugging
- Use Visual Studio's debugger or VS Code's debug configuration
- Set breakpoints in `Function.cs` and `DynamoDBService.cs`
- Use AWS Toolkit for VS/VS Code for AWS resource inspection 