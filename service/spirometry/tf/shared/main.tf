terraform {
  backend "remote" {
    organization = "sgfy"

    workspaces {
      prefix = "spirometry-shared-"
    }
  }
}
