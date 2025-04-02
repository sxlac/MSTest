

provider "okta" {}


##########Create Auth Server Policy

#Retrieve the default auth server reference, this will already exist.
data "okta_auth_server" "defaultsvr" {
  name = "default"
}


#Get clientids for the apps to be used as client whitelist in your auth policy
data "okta_app" "appsinpolicy" {
  label = element(var.client_whitelist, count.index)
  count = length(var.client_whitelist)
}


#create new auth policy, assign your apps to it
resource "okta_auth_server_policy" "authpolicy" {

  auth_server_id   = data.okta_auth_server.defaultsvr.id
  status           = "ACTIVE"
  name             = "${var.application_name} Policy"
  description      = "${var.application_name} default policy"
  priority         = 1
  client_whitelist = [for item in data.okta_app.appsinpolicy : item.id]

}

#Create a rule for your auth policy
resource "okta_auth_server_policy_rule" "defaultrule" {
  auth_server_id       = data.okta_auth_server.defaultsvr.id
  policy_id            = okta_auth_server_policy.authpolicy.id
  status               = "ACTIVE"
  name                 = "Default"
  priority             = 1
  grant_type_whitelist = ["client_credentials"]
  group_whitelist      = ["EVERYONE"]
  scope_whitelist      = var.scopes
}
