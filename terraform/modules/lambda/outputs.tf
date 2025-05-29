output "lambda_function_name" {
  description = "Name of the Lambda function"
  value       = aws_lambda_function.query_processor.function_name
}

output "lambda_function_arn" {
  description = "ARN of the Lambda function"
  value       = aws_lambda_function.query_processor.arn
}

output "lambda_role_arn" {
  description = "ARN of the Lambda IAM role"
  value       = aws_iam_role.lambda_role.arn
}

output "api_endpoint" {
  description = "API Gateway endpoint URL"
  value       = "${aws_apigatewayv2_api.lambda_api.api_endpoint}/api/v1/audit"
}

output "log_group_name" {
  description = "Name of the CloudWatch Log Group"
  value       = var.enable_cloudwatch ? aws_cloudwatch_log_group.lambda_logs[0].name : null
} 