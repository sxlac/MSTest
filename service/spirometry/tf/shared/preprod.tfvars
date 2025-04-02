application_name                 = "spirometry"
scopes                           = ["evaluationapi", "memberapi", "providerapi", "rcmapi", "schedulingapi", "appointmentapi", "providerpayapi", "cdi"] # note this should match Okta:Scopes section in appsettings.json
client_whitelist                 = ["spirometry_DEV", "spirometry_QA", "spirometry_UAT"]
okta_auth_server_policy_priority = 21
