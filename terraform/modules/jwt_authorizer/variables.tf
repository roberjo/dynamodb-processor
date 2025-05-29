variable "lambda_zip_path" {
  description = "Path to the Lambda function deployment package"
  type        = string
}

variable "environment" {
  description = "Environment name (dev, staging, prod)"
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