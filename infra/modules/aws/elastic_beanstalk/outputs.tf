output "environment_name" {
  description = "The name of the Elastic Beanstalk environment"
  value       = aws_elastic_beanstalk_environment.main.name
}

output "environment_url" {
  description = "The URL of the Elastic Beanstalk environment"
  value       = aws_elastic_beanstalk_environment.main.endpoint_url
}

output "application_name" {
  description = "The name of the Elastic Beanstalk application"
  value       = aws_elastic_beanstalk_application.main.name
}
