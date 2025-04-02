
provider "azurerm" {
  features {}
}

data "azurerm_servicebus_namespace" "this" {
  name                = var.asb_namespace
  resource_group_name = var.asb_resourcegroup
}

resource "azurerm_servicebus_namespace_authorization_rule" "authrule" {
  name         = var.asb_rulename
  namespace_id = data.azurerm_servicebus_namespace.this.id

  listen = var.asb_listen
  send   = var.asb_send
  manage = var.asb_manage
}
