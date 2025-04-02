provider "okta" {}

resource "okta_app_oauth" "spirometry_oktaapp" {
  label          = var.okta_app_name
  type           = "service"
  issuer_mode    = "CUSTOM_URL"
  consent_method = "REQUIRED"
  omit_secret    = true
  response_types = ["token"]
  grant_types    = ["client_credentials"]
}
