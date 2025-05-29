variable "lambda_zip_path" {
  description = "Path to the Lambda function deployment package"
  type        = string
}

variable "dynamodb_table_name" {
  description = "Name of the DynamoDB table to query"
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
  description = "Logging level for the Lambda function"
  type        = string
  default     = "Info"
}

variable "lambda_memory" {
  description = "Memory allocation for the Lambda function in MB"
  type        = number
  default     = 256
}

variable "lambda_timeout" {
  description = "Timeout for the Lambda function in seconds"
  type        = number
  default     = 30
}

variable "enable_xray" {
  description = "Enable AWS X-Ray tracing"
  type        = bool
  default     = true
}

variable "enable_cloudwatch" {
  description = "Enable CloudWatch logging"
  type        = bool
  default     = true
}

variable "jwt_authorizer_id" {
  description = "ID of the JWT authorizer Lambda function"
  type        = string
} 