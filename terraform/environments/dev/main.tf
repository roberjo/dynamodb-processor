terraform {
  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.0"
    }
  }
  backend "remote" {
    organization = "your-org-name"
    workspaces {
      name = "dynamodb-processor-dev"
    }
  }
}

provider "aws" {
  region = var.aws_region
}

module "jwt_authorizer" {
  source = "../modules/jwt_authorizer"

  lambda_zip_path = var.jwt_authorizer_zip_path
  environment     = var.environment
  jwt_issuer      = var.jwt_issuer
  jwt_audience    = var.jwt_audience
  jwt_signing_key = var.jwt_signing_key
}

module "lambda" {
  source = "../modules/lambda"

  lambda_zip_path      = var.lambda_zip_path
  dynamodb_table_name  = var.dynamodb_table_name
  dynamodb_table_arn   = var.dynamodb_table_arn
  environment          = var.environment
  log_level           = var.log_level
  lambda_memory       = var.lambda_memory
  lambda_timeout      = var.lambda_timeout
  enable_xray         = var.enable_xray
  enable_cloudwatch   = var.enable_cloudwatch
  jwt_authorizer_id   = module.jwt_authorizer.lambda_function_name
} 