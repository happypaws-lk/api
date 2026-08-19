locals {
  # Standard PostgreSQL connection URL assembled from the managed DB instance outputs.
  # Stored in SSM as DATABASE_URL so the API gets one connection string instead of
  # five separate host/port/name/user/password variables.
  database_url = "postgresql://${var.db_master_username}:${var.db_master_password}@${aws_lightsail_database.main.master_endpoint_address}:${aws_lightsail_database.main.master_endpoint_port}/happypaws"

  # Centralised SSM path registry. All parameter names are defined here so there is
  # one place to update if a path ever needs to change.
  ssm = {
    # Plain strings — infrastructure outputs & runtime configuration
    container_service_name   = "/happypaws/prod/container_service_name"
    container_service_url    = "/happypaws/prod/container_service_url"
    aspnetcore_environment   = "/happypaws/prod/aspnetcore_environment"
    jwt_issuer               = "/happypaws/prod/jwt/issuer"
    jwt_audience             = "/happypaws/prod/jwt/audience"
    jwt_expiry_minutes       = "/happypaws/prod/jwt/expiry_minutes"
    gemini_model             = "/happypaws/prod/gemini/model"
    gemini_timeout_seconds   = "/happypaws/prod/gemini/timeout_seconds"
    ses_region               = "/happypaws/prod/ses/region"
    ses_from_address         = "/happypaws/prod/ses/from_address"
    storage_account_id       = "/happypaws/prod/storage/account_id"
    storage_public_bucket    = "/happypaws/prod/storage/public_bucket"
    storage_private_bucket   = "/happypaws/prod/storage/private_bucket"
    storage_custom_domain    = "/happypaws/prod/storage/custom_domain"
    cors_allowed_origins     = "/happypaws/prod/cors/allowed_origins"
    rate_limiting_disabled   = "/happypaws/prod/rate_limiting_disabled"
    features_enable_api_docs = "/happypaws/prod/features_enable_api_docs"

    # SecureStrings — sensitive values
    database_url                  = "/happypaws/prod/database_url"
    ses_access_key_id             = "/happypaws/prod/ses/access_key_id"
    ses_secret_access_key         = "/happypaws/prod/ses/secret_access_key"
    jwt_key                       = "/happypaws/prod/jwt_key"
    gemini_api_key                = "/happypaws/prod/gemini_api_key"
    firebase_service_account_json = "/happypaws/prod/firebase_service_account_json"
    storage_access_key            = "/happypaws/prod/storage/access_key"
    storage_secret_key            = "/happypaws/prod/storage/secret_key"
  }
}
