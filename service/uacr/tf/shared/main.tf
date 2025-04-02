terraform {
  backend "remote" {
    organization = "sgfy"

    workspaces {
      prefix = "uACR-shared-"
    }
  }
}
