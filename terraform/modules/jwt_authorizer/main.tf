terraform {
  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.0"
    }
  }
}

locals {
  lambda_name = "jwt-authorizer-${var.environment}"
  tags = {
    Environment = var.environment
    Service     = "jwt-authorizer"
  }
}

# Lambda Function
resource "aws_lambda_function" "authorizer" {
  filename         = var.lambda_zip_path
  function_name    = local.lambda_name
  role            = aws_iam_role.lambda_role.arn
  handler         = "JwtAuthorizer::JwtAuthorizer.Function::FunctionHandler"
  runtime         = "provided.al2"
  timeout         = 10
  memory_size     = 128
  tags            = local.tags

  environment {
    variables = {
      JWT_ISSUER     = var.jwt_issuer
      JWT_AUDIENCE   = var.jwt_audience
      JWT_SIGNING_KEY = var.jwt_signing_key
    }
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
          "logs:CreateLogGroup",
          "logs:CreateLogStream",
          "logs:PutLogEvents"
        ]
        Resource = "arn:aws:logs:*:*:*"
      }
    ]
  })
}

# CloudWatch Log Group
resource "aws_cloudwatch_log_group" "lambda_logs" {
  name              = "/aws/lambda/${local.lambda_name}"
  retention_in_days = var.environment == "prod" ? 30 : 7
  tags              = local.tags
} 