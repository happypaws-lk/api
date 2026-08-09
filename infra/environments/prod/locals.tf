locals {
  # Standard PostgreSQL connection URL assembled from the managed DB instance outputs.
  # Stored in SSM as DATABASE_URL so the API gets one connection string instead of
  # five separate host/port/name/user/password variables.
  database_url = "postgresql://${var.db_master_username}:${var.db_master_password}@${aws_lightsail_database.main.master_endpoint_address}:${aws_lightsail_database.main.master_endpoint_port}/happypaws"

  # Centralised SSM path registry. All parameter names are defined here so there is
  # one place to update if a path ever needs to change.
  ssm = {
    # Plain strings — infrastructure outputs
    container_service_name = "/happypaws/prod/container_service_name"
    container_service_url  = "/happypaws/prod/container_service_url"
    storage_account_id     = "/happypaws/prod/storage/account_id"

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
