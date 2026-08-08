variable "app_name" {
  description = "The name of the Elastic Beanstalk application"
  type        = string
}

variable "vpc_id" {
  description = "The ID of the VPC for the Elastic Beanstalk environment"
  type        = string
}

variable "subnet_ids" {
  description = "List of subnet IDs where the EC2 instances will run"
  type        = list(string)
}

variable "eb_security_group_id" {
  description = "Security group ID attached to the Elastic Beanstalk instances"
  type        = string
}

variable "environment_variables" {
  description = "A map of environment variables to pass to the application"
  type        = map(string)
  default     = {}
}

variable "db_secret_arn" {
  description = "The ARN of the AWS Secrets Manager secret containing the database credentials"
  type        = string
}

variable "tags" {
  description = "A map of tags to assign to the resources"
  type        = map(string)
  default     = {}
}
