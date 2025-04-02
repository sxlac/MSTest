variable "application_name" {
  description = "This should match the application name used in the main folder"
  type        = string
}

variable "scopes" {
  description = "If your app requires client credentials to call other apis, put the scopes that it needs to have to be granted access to those scopes.  If your app does not require client credentials, you won't need this."
  type        = list(string)
  default = [
    "evaluationapi",
    "inventoryapi",
    "labsapi",
    "memberapi",
    "openid",
    "profile",
    "providerapi",
    "providerpayapi",
    "rcmapi",
    "roles"
  ]
}

variable "client_whitelist" {
  description = "An array containing the app names (w/environments), ie. YourApp-DEV, YourApp-QA, YourApp-UAT.  This is used to look up the client ids for you when assigning to auth policy. Only used if you are creating client credentials so you can call other APIs"
  type        = list(string)
}

variable "grant_type_whitelist" {
  description = "Accepted grant type values: authorization_code, implicit, password, client_credentials"
  type        = list(string)
}
