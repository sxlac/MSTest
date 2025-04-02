terraform {
  backend "remote" {
    organization = "sgfy"
    workspaces {
      prefix = "padsvc-shared-"
    }
  }
}
