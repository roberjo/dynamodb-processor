terraform {
  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.0"
    }
  }
}

locals {
  lambda_name = "dynamodb-query-processor-${var.environment}"
  tags = {
    Environment = var.environment
    Service     = "dynamodb-processor"
  }
}

# Lambda Function
resource "aws_lambda_function" "query_processor" {
  filename         = var.lambda_zip_path
  function_name    = local.lambda_name
  role            = aws_iam_role.lambda_role.arn
  handler         = "DynamoDBProcessor::DynamoDBProcessor.Function::FunctionHandler"
  runtime         = "dotnet8"
  timeout         = var.lambda_timeout
  memory_size     = var.lambda_memory
  tags            = local.tags

  environment {
    variables = {
      DYNAMODB_TABLE_NAME = var.dynamodb_table_name
      LOG_LEVEL          = var.log_level
      ENVIRONMENT        = var.environment
    }
  }

  tracing_config {
    mode = var.enable_xray ? "Active" : "PassThrough"
  }
}

# IAM Role for Lambda
resource "aws_iam_role" "lambda_role" {
  name = "${local.lambda_name}-role"
  tags = local.tags

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Action = "sts:AssumeRole"
        Effect = "Allow"
        Principal = {
          Service = "lambda.amazonaws.com"
        }
      }
    ]
  })
}

# IAM Policy for Lambda
resource "aws_iam_role_policy" "lambda_policy" {
  name = "${local.lambda_name}-policy"
  role = aws_iam_role.lambda_role.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Action = [
          "dynamodb:Query",
          "dynamodb:Scan"
        ]
        Resource = [
          var.dynamodb_table_arn,
          "${var.dynamodb_table_arn}/index/*"
        ]
      },
      {
        Effect = "Allow"
        Action = [
          "logs:CreateLogGroup",
          "logs:CreateLogStream",
          "logs:PutLogEvents"
        ]
        Resource = "arn:aws:logs:*:*:*"
      },
      {
        Effect = "Allow"
        Action = [
          "xray:PutTraceSegments",
          "xray:PutTelemetryRecords"
        ]
        Resource = "*"
      }
    ]
  })
}

# CloudWatch Log Group
resource "aws_cloudwatch_log_group" "lambda_logs" {
  count             = var.enable_cloudwatch ? 1 : 0
  name              = "/aws/lambda/${local.lambda_name}"
  retention_in_days = var.environment == "prod" ? 30 : 7
  tags              = local.tags
}

# API Gateway
resource "aws_apigatewayv2_api" "lambda_api" {
  name          = "${local.lambda_name}-api"
  protocol_type = "HTTP"
  tags          = local.tags

  cors_configuration {
    allow_headers = ["Content-Type", "Authorization", "X-Amz-Date", "X-Api-Key", "X-Amz-Security-Token"]
    allow_methods = ["GET", "POST"]
    allow_origins = ["*"]
    max_age       = 300
  }
}

resource "aws_apigatewayv2_stage" "lambda_stage" {
  api_id = aws_apigatewayv2_api.lambda_api.id
  name   = var.environment
  auto_deploy = true
  tags        = local.tags
}

resource "aws_apigatewayv2_integration" "lambda_integration" {
  api_id           = aws_apigatewayv2_api.lambda_api.id
  integration_type = "AWS_PROXY"

  connection_type    = "INTERNET"
  description        = "Lambda integration"
  integration_method = "POST"
  integration_uri    = aws_lambda_function.query_processor.invoke_arn
}

# GET route for /api/v1/audit
resource "aws_apigatewayv2_route" "get_audit_route" {
  api_id    = aws_apigatewayv2_api.lambda_api.id
  route_key = "GET /api/v1/audit"
  target    = "integrations/${aws_apigatewayv2_integration.lambda_integration.id}"
  authorization_type = "CUSTOM"
  authorizer_id     = var.jwt_authorizer_id
}

# POST route for /api/v1/audit
resource "aws_apigatewayv2_route" "post_audit_route" {
  api_id    = aws_apigatewayv2_api.lambda_api.id
  route_key = "POST /api/v1/audit"
  target    = "integrations/${aws_apigatewayv2_integration.lambda_integration.id}"
  authorization_type = "CUSTOM"
  authorizer_id     = var.jwt_authorizer_id
}

# Lambda Permission for GET
resource "aws_lambda_permission" "api_gw_get" {
  statement_id  = "AllowExecutionFromAPIGatewayGET"
  action        = "lambda:InvokeFunction"
  function_name = aws_lambda_function.query_processor.function_name
  principal     = "apigateway.amazonaws.com"
  source_arn    = "${aws_apigatewayv2_api.lambda_api.execution_arn}/*/*"
}

# Lambda Permission for POST
resource "aws_lambda_permission" "api_gw_post" {
  statement_id  = "AllowExecutionFromAPIGatewayPOST"
  action        = "lambda:InvokeFunction"
  function_name = aws_lambda_function.query_processor.function_name
  principal     = "apigateway.amazonaws.com"
  source_arn    = "${aws_apigatewayv2_api.lambda_api.execution_arn}/*/*"
} 