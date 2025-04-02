terraform {
  required_providers {
    okta = {
      source  = "okta/okta"
      version = "~> 4.6.1"
    }
    postgresql = {
      source = "cyrilgdn/postgresql"
    }
    confluent = {
      source  = "confluentinc/confluent"
      version = "~> 1.65.0"
    }
  }
  required_version = ">= 1.5"
}
