terraform {
  backend "remote" {
    organization = "sgfy"
    workspaces {
      prefix = "hba1cpocsvc-shared-"
    }
  }
}
