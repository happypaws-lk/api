# ==============================================================================
# Lightsail Managed PostgreSQL
# ==============================================================================

resource "aws_lightsail_database" "main" {
  relational_database_name = "happypaws-db"
  blueprint_id             = "postgres_16"
  bundle_id                = var.db_bundle_id
  master_database_name     = "happypaws"
  master_username          = var.db_master_username
  master_password          = var.db_master_password

  availability_zone = "ap-southeast-1a"

  # Keep the DB off the public internet. It is reachable from the container
  # service via Lightsail's private network in the same region and account.
  publicly_accessible = false

  backup_retention_enabled     = true
  preferred_backup_window      = "16:00-16:30" # 21:30 SL time — low traffic
  preferred_maintenance_window = "tue:17:00-tue:17:30"
  skip_final_snapshot          = false
  final_snapshot_name          = "happypaws-prod-final"
  apply_immediately            = true
}

# ==============================================================================
# Lightsail Container Service
# ==============================================================================

resource "aws_lightsail_container_service" "main" {
  name        = "happypaws-api"
  power       = var.container_service_power
  scale       = var.container_service_scale
  is_disabled = false
}

# ==============================================================================
# GitHub Actions OIDC — zero static credentials in GitHub Secrets
# ==============================================================================

resource "aws_iam_openid_connect_provider" "github" {
  url            = "https://token.actions.githubusercontent.com"
  client_id_list = ["sts.amazonaws.com"]

  # AWS uses its own CA trust list for GitHub's OIDC provider and ignores this
  # value at runtime. The Terraform schema still requires it; use a placeholder.
  thumbprint_list = ["0000000000000000000000000000000000000000"]
}

data "aws_iam_policy_document" "github_actions_assume_role" {
  statement {
    effect  = "Allow"
    actions = ["sts:AssumeRoleWithWebIdentity"]

    principals {
      type        = "Federated"
      identifiers = [aws_iam_openid_connect_provider.github.arn]
    }

    # Audience must be sts.amazonaws.com — the OIDC client ID configured above.
    condition {
      test     = "StringEquals"
      variable = "token.actions.githubusercontent.com:aud"
      values   = ["sts.amazonaws.com"]
    }

    # Restrict to the main branch of this specific private repo.
    # workflow_dispatch from main also produces this sub claim.
    condition {
      test     = "StringLike"
      variable = "token.actions.githubusercontent.com:sub"
      values = [
        "repo:${var.github_repo}:ref:refs/heads/main",
        "repo:${split("/", var.github_repo)[0]}@*/${split("/", var.github_repo)[1]}@*:ref:refs/heads/main"
      ]
    }
  }
}

resource "aws_iam_role" "github_actions" {
  name               = "happypaws-github-actions-deploy"
  assume_role_policy = data.aws_iam_policy_document.github_actions_assume_role.json
}

data "aws_iam_policy_document" "github_actions_deploy" {
  # Lightsail does not support resource-level ARN scoping for container actions,
  # so Resource = "*" is required for all lightsail:* permissions.
  statement {
    sid    = "LightsailContainerDeploy"
    effect = "Allow"
    actions = [
      "lightsail:CreateContainerServiceDeployment",
      "lightsail:CreateContainerServiceRegistryLogin",
      "lightsail:GetContainerImages",
      "lightsail:GetContainerServiceDeployments",
      "lightsail:GetContainerServices",
      "lightsail:RegisterContainerImage",
    ]
    resources = ["*"]
  }

  statement {
    sid    = "SsmReadProdConfig"
    effect = "Allow"
    actions = [
      "ssm:GetParameter",
      "ssm:GetParameters",
    ]
    resources = ["arn:aws:ssm:ap-southeast-1:*:parameter/happypaws/prod/*"]
  }

  # kms:Decrypt is needed to read SecureString parameters.
  # The ViaService condition limits this permission to SSM calls only.
  statement {
    sid       = "KmsDecryptViaSsm"
    effect    = "Allow"
    actions   = ["kms:Decrypt"]
    resources = ["*"]

    condition {
      test     = "StringEquals"
      variable = "kms:ViaService"
      values   = ["ssm.ap-southeast-1.amazonaws.com"]
    }
  }
}

resource "aws_iam_policy" "github_actions_deploy" {
  name   = "happypaws-github-actions-deploy"
  policy = data.aws_iam_policy_document.github_actions_deploy.json
}

resource "aws_iam_role_policy_attachment" "github_actions_deploy" {
  role       = aws_iam_role.github_actions.name
  policy_arn = aws_iam_policy.github_actions_deploy.arn
}

# ==============================================================================
# SES IAM user
#
# Lightsail Container Service does not expose the EC2 Instance Metadata Service
# (IMDS), so the AWS SDK role credential chain cannot be used. A dedicated
# least-privilege IAM user is the supported pattern for SES from Lightsail
# containers. The access key is auto-generated by Terraform and written to SSM.
# ==============================================================================

resource "aws_iam_user" "ses_sender" {
  name = "happypaws-ses-sender"
}

resource "aws_iam_user_policy" "ses_send" {
  user = aws_iam_user.ses_sender.name

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Effect   = "Allow"
      Action   = ["ses:SendEmail", "ses:SendRawEmail"]
      Resource = "*"
    }]
  })
}

resource "aws_iam_access_key" "ses_sender" {
  user = aws_iam_user.ses_sender.name
}

# ==============================================================================
# SSM Parameter Store
# ==============================================================================

# -- Plain strings: infrastructure outputs that other tools may read freely --

resource "aws_ssm_parameter" "container_service_name" {
  name  = local.ssm.container_service_name
  type  = "String"
  value = aws_lightsail_container_service.main.name
}

resource "aws_ssm_parameter" "container_service_url" {
  name  = local.ssm.container_service_url
  type  = "String"
  value = aws_lightsail_container_service.main.url
}

resource "aws_ssm_parameter" "storage_account_id" {
  name        = local.ssm.storage_account_id
  type        = "String"
  value       = var.storage_account_id
  description = "Cloudflare account ID for R2 storage endpoint construction."

  lifecycle {
    ignore_changes = [value]
  }
}

resource "aws_ssm_parameter" "storage_custom_domain" {
  name        = local.ssm.storage_custom_domain
  type        = "String"
  value       = var.storage_custom_domain
  description = "Public CDN domain or R2 development URL for public assets."
}

resource "aws_ssm_parameter" "cors_allowed_origins" {
  name        = local.ssm.cors_allowed_origins
  type        = "StringList"
  value       = join(",", var.cors_allowed_origins)
  description = "Comma-separated list of allowed CORS origins for the HappyPaws API."
}

resource "aws_ssm_parameter" "aspnetcore_environment" {
  name        = local.ssm.aspnetcore_environment
  type        = "String"
  value       = var.aspnetcore_environment
  description = "ASP.NET Core hosting environment."
}

resource "aws_ssm_parameter" "jwt_issuer" {
  name        = local.ssm.jwt_issuer
  type        = "String"
  value       = var.jwt_issuer
  description = "JWT token issuer URL."
}

resource "aws_ssm_parameter" "jwt_audience" {
  name        = local.ssm.jwt_audience
  type        = "String"
  value       = var.jwt_audience
  description = "JWT token audience URL."
}

resource "aws_ssm_parameter" "jwt_expiry_minutes" {
  name        = local.ssm.jwt_expiry_minutes
  type        = "String"
  value       = tostring(var.jwt_expiry_minutes)
  description = "JWT token lifetime in minutes."
}

resource "aws_ssm_parameter" "gemini_model" {
  name        = local.ssm.gemini_model
  type        = "String"
  value       = var.gemini_model
  description = "Gemini model version for urgency classification."
}

resource "aws_ssm_parameter" "gemini_timeout_seconds" {
  name        = local.ssm.gemini_timeout_seconds
  type        = "String"
  value       = tostring(var.gemini_timeout_seconds)
  description = "Timeout in seconds for Gemini API calls."
}

resource "aws_ssm_parameter" "ses_region" {
  name        = local.ssm.ses_region
  type        = "String"
  value       = var.ses_region
  description = "AWS SES service region for transactional emails."
}

resource "aws_ssm_parameter" "ses_from_address" {
  name        = local.ssm.ses_from_address
  type        = "String"
  value       = var.ses_from_address
  description = "From email address used for SES transactional emails."
}

resource "aws_ssm_parameter" "storage_public_bucket" {
  name        = local.ssm.storage_public_bucket
  type        = "String"
  value       = var.storage_public_bucket
  description = "R2 public bucket name for media assets."
}

resource "aws_ssm_parameter" "storage_private_bucket" {
  name        = local.ssm.storage_private_bucket
  type        = "String"
  value       = var.storage_private_bucket
  description = "R2 private bucket name for KYC documents."
}

resource "aws_ssm_parameter" "rate_limiting_disabled" {
  name        = local.ssm.rate_limiting_disabled
  type        = "String"
  value       = tostring(var.rate_limiting_disabled)
  description = "Controls whether API rate limiting is disabled."
}

resource "aws_ssm_parameter" "features_enable_api_docs" {
  name        = local.ssm.features_enable_api_docs
  type        = "String"
  value       = tostring(var.features_enable_api_docs)
  description = "Controls whether OpenAPI documentation endpoints are enabled in production."
}

# -- SecureStrings: all sensitive values encrypted with the AWS-managed SSM key --

resource "aws_ssm_parameter" "database_url" {
  name        = local.ssm.database_url
  type        = "SecureString"
  value       = local.database_url
  description = "Full PostgreSQL connection URL for the HappyPaws API."
}

resource "aws_ssm_parameter" "ses_access_key_id" {
  name        = local.ssm.ses_access_key_id
  type        = "SecureString"
  value       = aws_iam_access_key.ses_sender.id
  description = "AWS access key ID for the happypaws-ses-sender IAM user."
}

resource "aws_ssm_parameter" "ses_secret_access_key" {
  name        = local.ssm.ses_secret_access_key
  type        = "SecureString"
  value       = aws_iam_access_key.ses_sender.secret
  description = "AWS secret access key for the happypaws-ses-sender IAM user."
}

resource "aws_ssm_parameter" "jwt_key" {
  name        = local.ssm.jwt_key
  type        = "SecureString"
  value       = var.jwt_key
  description = "JWT signing key for the HappyPaws API."

  # Protects manually rotated values from being overwritten on re-apply.
  lifecycle {
    ignore_changes = [value]
  }
}

resource "aws_ssm_parameter" "gemini_api_key" {
  name        = local.ssm.gemini_api_key
  type        = "SecureString"
  value       = var.gemini_api_key
  description = "Google Gemini API key for urgency classification."

  lifecycle {
    ignore_changes = [value]
  }
}

resource "aws_ssm_parameter" "firebase_service_account_json" {
  name        = local.ssm.firebase_service_account_json
  type        = "SecureString"
  value       = var.firebase_service_account_json
  description = "Base64-encoded Firebase service account JSON for FCM push notifications."

  lifecycle {
    ignore_changes = [value]
  }
}

resource "aws_ssm_parameter" "storage_access_key" {
  name        = local.ssm.storage_access_key
  type        = "SecureString"
  value       = var.storage_access_key
  description = "Cloudflare R2 access key ID."

  lifecycle {
    ignore_changes = [value]
  }
}

resource "aws_ssm_parameter" "storage_secret_key" {
  name        = local.ssm.storage_secret_key
  type        = "SecureString"
  value       = var.storage_secret_key
  description = "Cloudflare R2 secret access key."

  lifecycle {
    ignore_changes = [value]
  }
}
