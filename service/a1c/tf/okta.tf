provider okta {
  org_name  = "signifyhealth"
  base_url  = var.okta_url
  api_token = var.okta_api_token
}

resource okta_app_oauth appname {
 label                       = var.okta_app_name
 type                        = "service"
 issuer_mode                 = "CUSTOM_URL"
 consent_method              = "REQUIRED"
 omit_secret                 = true
 response_types              = ["token"]
 grant_types                 = ["client_credentials"]
}

