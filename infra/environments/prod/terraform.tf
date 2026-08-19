terraform {
  required_version = ">= 1.10"

  backend "s3" {
    bucket = "happypaws-terraform-state"
    key    = "prod/terraform.tfstate"
    region = "ap-southeast-1"

    # Native S3 state locking introduced in Terraform 1.10.
    # No DynamoDB table required.
    use_lockfile = true
  }

  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 6.60"
    }
  }
}
