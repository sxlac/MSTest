

variable "application_name" {
	description="This should match the application name used in the main folder"
}
variable "scopes" {
	description="Scopes assigned to your users, if your app calls coreapis include their scopes here. Always include the 3 default scopes listed below if you override this."
	default=["roles","profile","openapi","evaluationapi","inventoryapi","rcmapi"]
}
variable "okta_url" {
	description="Either oktapreview.com (preproduction) or okta.com (prod)"
}

variable "okta_api_token" {
  	description = "Don't include in tfvars files > set with 'export TF_VAR_okta_api_token={your token}' before applying"
}

variable "client_whitelist" {}
