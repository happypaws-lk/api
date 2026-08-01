terraform {
  required_version = ">= 1.5.0"

  backend "s3" {
    bucket = "happypaws-terraform-state-prod"
    key    = "prod/terraform.tfstate"
    region = "ap-southeast-1"
    # DynamoDB table intentionally omitted as per requirements (using native S3 locking)
  }

  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 6.57"
    }
  }
}

provider "aws" {
  region = var.aws_region

  default_tags {
    tags = {
      Environment = "production"
      ManagedBy   = "Terraform"
      Project     = "HappyPaws"
    }
  }
}
