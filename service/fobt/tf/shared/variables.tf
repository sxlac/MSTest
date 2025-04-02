variable "application_name" {
  description = "This should match the application name used in the main folder"
  type        = string
}

variable "scopes" {
  description = <<-EOF
  Scopes assigned to your users, if your app calls coreapis include their scopes here.
  Always include the 3 default scopes listed below if you override this.
  EOF

  type = list(string)

  default = ["roles", "profile", "openapi", "evaluationapi", "inventoryapi", "rcmapi"]
}

variable "client_whitelist" {
  description = <<-EOF
  An array containing the app names (w/environments), ie. YourApp-DEV, YourApp-QA, YourApp-UAT.
  This is used to look up the client ids for you when assigning to auth policy.
  Only used if you are creating client credentials so you can call other APIs
  EOF
  type        = list(string)
}
