output "container_service_url" {
  description = "Public URL of the Lightsail container service. Use this as your API base URL."
  value       = aws_lightsail_container_service.main.url
}

output "database_endpoint" {
  description = "Private host address of the Lightsail managed PostgreSQL instance."
  value       = aws_lightsail_database.main.master_endpoint_address
}

output "github_actions_role_arn" {
  description = "ARN of the GitHub Actions OIDC deploy role. Add this value to the AWS_DEPLOY_ROLE_ARN GitHub secret."
  value       = aws_iam_role.github_actions.arn
}

output "ses_iam_user_name" {
  description = "Name of the IAM user created for SES. Its credentials are stored in SSM — you do not need to manage them manually."
  value       = aws_iam_user.ses_sender.name
}
