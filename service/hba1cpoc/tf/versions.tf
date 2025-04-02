terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 3.84.0"
    }
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
      version = "1.17.1"
    }
  }
  required_version = ">= 1.5.0"
}
