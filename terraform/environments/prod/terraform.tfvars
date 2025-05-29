aws_region         = "us-east-1"
lambda_zip_path    = "./build/DynamoDBProcessor-prod.zip"
dynamodb_table_name = "dynamodb-table-prod"
dynamodb_table_arn  = "arn:aws:dynamodb:us-east-1:123456789012:table/dynamodb-table-prod"

# Environment-specific variables
environment       = "prod"
log_level         = "Info"
lambda_memory     = 1024
lambda_timeout    = 30
enable_xray       = true
enable_cloudwatch = true

# JWT Authorizer Configuration
jwt_authorizer_zip_path = "./build/JwtAuthorizer-prod.zip"
jwt_issuer = "https://auth.example.com"
jwt_audience = "dynamodb-processor-prod"
jwt_signing_key = "prod-signing-key-replace-with-secure-key" 