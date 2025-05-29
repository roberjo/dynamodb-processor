variable "aws_region" {
  description = "AWS region"
  type        = string
}

variable "lambda_zip_path" {
  description = "Path to the Lambda function deployment package"
  type        = string
}

variable "dynamodb_table_name" {
  description = "Name of the DynamoDB table"
  type        = string
}

variable "dynamodb_table_arn" {
  description = "ARN of the DynamoDB table"
  type        = string
}

variable "environment" {
  description = "Environment name (dev, staging, prod)"
  type        = string
}

variable "log_level" {
  description = "Log level for the Lambda function"
  type        = string
}

variable "lambda_memory" {
  description = "Memory allocation for the Lambda function in MB"
  type        = number
}

variable "lambda_timeout" {
  description = "Timeout for the Lambda function in seconds"
  type        = number
}

variable "enable_xray" {
  description = "Enable X-Ray tracing"
  type        = bool
}

variable "enable_cloudwatch" {
  description = "Enable CloudWatch logging"
  type        = bool
}

variable "jwt_authorizer_zip_path" {
  description = "Path to the JWT authorizer Lambda function deployment package"
  type        = string
}

variable "jwt_issuer" {
  description = "JWT token issuer"
  type        = string
  sensitive   = true
}

variable "jwt_audience" {
  description = "JWT token audience"
  type        = string
  sensitive   = true
}

variable "jwt_signing_key" {
  description = "JWT token signing key"
  type        = string
  sensitive   = true
} 