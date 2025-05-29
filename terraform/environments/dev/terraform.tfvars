aws_region         = "us-east-1"
lambda_zip_path    = "./build/DynamoDBProcessor-dev.zip"
dynamodb_table_name = "dynamodb-table-dev"
dynamodb_table_arn  = "arn:aws:dynamodb:us-east-1:123456789012:table/dynamodb-table-dev"

# Environment-specific variables
environment       = "dev"
log_level         = "Debug"
lambda_memory     = 256
lambda_timeout    = 30
enable_xray       = true
enable_cloudwatch = true

# JWT Authorizer Configuration
jwt_authorizer_zip_path = "./build/JwtAuthorizer-dev.zip"
jwt_issuer = "https://dev.auth.example.com"
jwt_audience = "dynamodb-processor-dev"
jwt_signing_key = "dev-signing-key-replace-in-production" 