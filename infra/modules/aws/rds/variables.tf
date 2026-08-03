variable "identifier" {
  description = "The identifier for the RDS instance"
  type        = string
}

variable "vpc_id" {
  description = "VPC ID where the database and security groups will be created"
  type        = string
}

variable "subnet_ids" {
  description = "List of subnet IDs for the database subnet group"
  type        = list(string)
}

variable "eb_security_group_id" {
  description = "Security group ID of the Elastic Beanstalk instances to allow ingress traffic"
  type        = string
}

variable "instance_class" {
  description = "The instance type of the RDS instance"
  type        = string
  default     = "db.t3.micro"
}

variable "engine_version" {
  description = "The PostgreSQL engine version"
  type        = string
  default     = "16"
}

variable "allocated_storage" {
  description = "The allocated storage in gigabytes"
  type        = number
  default     = 20
}

variable "db_username" {
  description = "Username for the master DB user"
  type        = string
  default     = "postgres"
}

variable "db_name" {
  description = "The name of the database to create when the DB instance is created"
  type        = string
  default     = "happypaws"
}

variable "tags" {
  description = "A map of tags to assign to the resources"
  type        = map(string)
  default     = {}
}
