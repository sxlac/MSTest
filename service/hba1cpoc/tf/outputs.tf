output client_id {
  value = okta_app_oauth.appname.client_id
}
output asb_connstring {
   value = azurerm_servicebus_namespace_authorization_rule.authrule.primary_connection_string
   sensitive = true
}