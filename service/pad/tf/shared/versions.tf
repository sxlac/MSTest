terraform {
  required_providers {
    okta = {
      source  = "okta/okta"
      version = "~> 4.6.1"
    }
  }
  required_version = ">= 1.5.0"
}
