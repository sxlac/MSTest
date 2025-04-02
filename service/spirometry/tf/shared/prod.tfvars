application_name                 = "spirometry"
scopes                           = ["evaluationapi", "memberapi", "providerapi", "rcmapi", "schedulingapi", "appointmentapi", "providerpayapi", "cdi"] # note this should match Okta:Scopes section in appsettings.json
client_whitelist                 = ["spirometry"]
okta_auth_server_policy_priority = 9
