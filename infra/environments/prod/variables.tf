variable "aws_region" {
  description = "The AWS region to deploy to"
  type        = string
  default     = "ap-southeast-1"
}

variable "db_username" {
  description = "Username for the RDS master user"
  type        = string
  default     = "happypaws_admin"
  sensitive   = true
}

variable "db_password" {
  description = "Password for the RDS master user (managed by Secrets Manager, optional here but listed for future-proofing)"
  type        = string
  default     = ""
  sensitive   = true
}
