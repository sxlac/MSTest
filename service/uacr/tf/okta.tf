provider "okta" {}

resource "okta_app_oauth" "uacr_oktaapp" {
  label          = var.okta_app_name
  type           = "service"
  issuer_mode    = "CUSTOM_URL"
  consent_method = "REQUIRED"
  response_types = ["token"]
  grant_types    = ["client_credentials"]
}
