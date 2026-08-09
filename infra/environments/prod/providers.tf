provider "aws" {
  region = "ap-southeast-1"

  default_tags {
    tags = {
      Project     = "happypaws"
      Environment = "prod"
      ManagedBy   = "Terraform"
    }
  }
}
