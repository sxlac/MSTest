terraform {
  required_providers {
    confluent = {
      source  = "confluentinc/confluent"
      version = "~> 1.55.0"
    }
    okta = {
      source  = "okta/okta"
      version = "~> 4.6.1"
    }
    postgresql = {
      source  = "cyrilgdn/postgresql"
      version = "1.16.0"
    }
  }
  required_version = ">= 1.5.0"
}
