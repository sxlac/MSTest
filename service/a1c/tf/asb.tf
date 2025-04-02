
provider "azurerm" {
  # whilst the `version` attribute is optional, we recommend pinning to a given version of the Provider
  version = "=2.3.0"
  features {}
}

#resource "azurerm_servicebus_namespace_authorization_rule" "authrule" {
#  name                = var.asb_rulename
#  namespace_name      = var.asb_namespace
#  resource_group_name = var.asb_resourcegroup

#  listen = var.asb_listen
#  send   = var.asb_send
#  manage = var.asb_manage
#}