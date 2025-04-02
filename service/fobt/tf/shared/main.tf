terraform {
  backend "remote" {
    organization = "sgfy"
    workspaces {
      prefix = "fobtsvc-shared-"
    }
  }
}
