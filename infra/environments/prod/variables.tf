variable "db_master_username" {
  description = "Master username for the Lightsail managed PostgreSQL instance."
  type        = string
  default     = "happypaws"
}

variable "db_master_password" {
  description = "Master password for the Lightsail managed PostgreSQL instance. Must be at least 8 characters."
  type        = string
  sensitive   = true
}

variable "db_bundle_id" {
  description = "Bundle ID for the Lightsail managed PostgreSQL instance. Controls RAM, vCPU, and storage."
  type        = string
  default     = "micro_2_0"
}

variable "container_service_power" {
  description = "Compute power for the Lightsail container service. Controls vCPU and RAM per node."
  type        = string
  default     = "small"

  validation {
    condition     = contains(["nano", "micro", "small", "medium", "large", "xlarge"], var.container_service_power)
    error_message = "power must be one of: nano, micro, small, medium, large, xlarge."
  }
}

variable "container_service_scale" {
  description = "Number of compute nodes. Set to 1 for standard deployment, 3 for high availability."
  type        = number
  default     = 1

  validation {
    condition     = var.container_service_scale >= 1 && var.container_service_scale <= 20
    error_message = "scale must be between 1 and 20."
  }
}

variable "github_repo" {
  description = "GitHub repository in org/repo format. Used to scope the OIDC trust policy to this repo's main branch only."
  type        = string
  default     = "happypaws-lk/api"
}

variable "jwt_key" {
  description = "Secret key for signing JWT tokens. Minimum 32 characters."
  type        = string
  sensitive   = true
}

variable "gemini_api_key" {
  description = "Google Gemini API key for the urgency classification service."
  type        = string
  sensitive   = true
}

variable "firebase_service_account_json" {
  description = "Base64-encoded Firebase service account JSON for FCM push notifications."
  type        = string
  sensitive   = true
}

variable "storage_account_id" {
  description = "Cloudflare account ID. Used to construct the R2 storage endpoint URL."
  type        = string
}

variable "storage_access_key" {
  description = "Cloudflare R2 access key ID."
  type        = string
  sensitive   = true
}

variable "storage_secret_key" {
  description = "Cloudflare R2 secret access key."
  type        = string
  sensitive   = true
}
