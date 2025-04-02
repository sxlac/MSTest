terraform {
  backend "remote" {
    organization = "sgfy"

    workspaces {
      prefix = "ckdsvc-"
    }
  }
}
