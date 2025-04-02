provider "okta" {}

data "okta_auth_server" "defaultsvr" {
  name = "default"
}

data "okta_app" "appsinpolicy" {
  label = element(var.client_whitelist, count.index)
  count = length(var.client_whitelist)
}

resource "okta_auth_server_policy" "authpolicy" {
  auth_server_id   = data.okta_auth_server.defaultsvr.id
  status           = "ACTIVE"
  name             = "${var.application_name} Policy"
  description      = "${var.application_name} default policy"
  priority         = 1
  client_whitelist = [for item in data.okta_app.appsinpolicy : item.id]

  lifecycle {
    ignore_changes = [
      priority
    ]
  }
}

resource "okta_auth_server_policy_rule" "defaultrule" {
  auth_server_id       = data.okta_auth_server.defaultsvr.id
  policy_id            = okta_auth_server_policy.authpolicy.id
  status               = "ACTIVE"
  name                 = "Default"
  priority             = 1
  grant_type_whitelist = var.grant_type_whitelist
  group_whitelist      = ["EVERYONE"]
  scope_whitelist      = var.scopes

  lifecycle {
    ignore_changes = [
      priority
    ]
  }
}
