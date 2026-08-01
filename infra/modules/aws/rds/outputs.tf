output "db_instance_endpoint" {
  description = "The connection endpoint for the RDS instance"
  value       = aws_db_instance.postgres.endpoint
}

output "db_instance_address" {
  description = "The address of the RDS instance"
  value       = aws_db_instance.postgres.address
}

output "db_instance_port" {
  description = "The port of the RDS instance"
  value       = aws_db_instance.postgres.port
}

output "db_instance_name" {
  description = "The database name"
  value       = aws_db_instance.postgres.db_name
}

output "db_secret_arn" {
  description = "The ARN of the AWS Secrets Manager secret for the master user password"
  value       = aws_db_instance.postgres.master_user_secret[0].secret_arn
}
