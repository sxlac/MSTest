terraform {
  required_providers {
    azurerm = {
      source = "hashicorp/azurerm"
    }
    kubernetes = {
      source = "hashicorp/kubernetes"
    }
    okta = {
      source = "terraform-providers/okta"
    }
    postgresql = {
      source = "terraform-providers/postgresql"
    }
  }
  required_version = ">= 0.13"
}
