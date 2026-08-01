output "eb_environment_url" {
  description = "The URL of the production Elastic Beanstalk environment"
  value       = module.elastic_beanstalk.environment_url
}

output "rds_endpoint" {
  description = "The endpoint of the production RDS database"
  value       = module.rds.db_instance_endpoint
}

output "ecr_repository_url" {
  description = "The URL of the production ECR repository"
  value       = module.ecr.repository_url
}
