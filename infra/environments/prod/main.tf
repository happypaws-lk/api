module "vpc" {
  source = "../../modules/aws/vpc"

  project_name = "happypaws-prod"
  vpc_cidr     = "10.0.0.0/16"
}

resource "aws_security_group" "eb_instance" {
  name        = "happypaws-prod-eb-sg"
  description = "Security group for Elastic Beanstalk EC2 instances"
  vpc_id      = module.vpc.vpc_id

  ingress {
    description = "Allow HTTP from anywhere"
    from_port   = 80
    to_port     = 80
    protocol    = "tcp"
    cidr_blocks = ["0.0.0.0/0"]
  }

  egress {
    description = "Allow all outbound traffic"
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }

  tags = {
    Name = "happypaws-prod-eb-sg"
  }
}

module "ecr" {
  source = "../../modules/aws/ecr"

  repository_name = "happypaws-api-prod"
  tags = {
    Component = "API"
  }
}

module "rds" {
  source = "../../modules/aws/rds"

  identifier           = "happypaws-db-prod"
  vpc_id               = module.vpc.vpc_id
  subnet_ids           = module.vpc.isolated_subnet_ids
  eb_security_group_id = aws_security_group.eb_instance.id
  db_username          = var.db_username
  allocated_storage    = 20
  instance_class       = "db.t3.micro"
}

module "elastic_beanstalk" {
  source = "../../modules/aws/elastic_beanstalk"

  app_name             = "happypaws-api"
  vpc_id               = module.vpc.vpc_id
  subnet_ids           = module.vpc.public_subnet_ids
  eb_security_group_id = aws_security_group.eb_instance.id
  db_secret_arn        = module.rds.db_secret_arn

  environment_variables = {
    "DB_HOST" = module.rds.db_instance_address
    "DB_PORT" = module.rds.db_instance_port
    "DB_NAME" = module.rds.db_instance_name
    "DB_USER" = var.db_username
  }
}

# ------------------------------------------------------------------------------
# SSM Parameter Store
# ------------------------------------------------------------------------------

resource "aws_ssm_parameter" "vpc_id" {
  name  = "/happypaws/prod/vpc/vpc_id"
  type  = "String"
  value = module.vpc.vpc_id
}

resource "aws_ssm_parameter" "db_endpoint" {
  name  = "/happypaws/prod/database/endpoint"
  type  = "String"
  value = module.rds.db_instance_address
}

resource "aws_ssm_parameter" "db_port" {
  name  = "/happypaws/prod/database/port"
  type  = "String"
  value = module.rds.db_instance_port
}

resource "aws_ssm_parameter" "db_name" {
  name  = "/happypaws/prod/database/name"
  type  = "String"
  value = module.rds.db_instance_name
}

resource "aws_ssm_parameter" "eb_url" {
  name  = "/happypaws/prod/eb/environment_url"
  type  = "String"
  value = module.elastic_beanstalk.environment_url
}

resource "aws_ssm_parameter" "ecr_url" {
  name  = "/happypaws/prod/ecr/repository_url"
  type  = "String"
  value = module.ecr.repository_url
}

resource "aws_ssm_parameter" "db_secret_arn" {
  name  = "/happypaws/prod/database/secret_arn"
  type  = "SecureString"
  value = module.rds.db_secret_arn
}

resource "aws_ssm_parameter" "firebase_service_account" {
  name        = "/happypaws/prod/firebase/service_account_json"
  type        = "SecureString"
  value       = "PLACEHOLDER_BASE64_FIREBASE_KEY"
  description = "Base64 encoded Firebase Service Account JSON"

  lifecycle {
    ignore_changes = [value]
  }
}
