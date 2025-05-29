aws_region         = "us-east-1"
lambda_zip_path    = "./build/DynamoDBProcessor-staging.zip"
dynamodb_table_name = "dynamodb-table-staging"
dynamodb_table_arn  = "arn:aws:dynamodb:us-east-1:123456789012:table/dynamodb-table-staging"

# Environment-specific variables
environment       = "staging"
log_level         = "Info"
lambda_memory     = 512
lambda_timeout    = 30
enable_xray       = true
enable_cloudwatch = true

# JWT Authorizer Configuration
jwt_authorizer_zip_path = "./build/JwtAuthorizer-staging.zip"
jwt_issuer = "https://staging.auth.example.com"
jwt_audience = "dynamodb-processor-staging"
jwt_signing_key = "staging-signing-key-replace-in-production" 