## Requirements

| Name | Version |
|------|---------|
| <a name="requirement_terraform"></a> [terraform](#requirement\_terraform) | >= 1.5 |
| <a name="requirement_okta"></a> [okta](#requirement\_okta) | ~> 4.6.1 |

## Providers

| Name | Version |
|------|---------|
| <a name="provider_okta"></a> [okta](#provider\_okta) | ~> 4.6.1 |

## Modules

No modules.

## Resources

| Name | Type |
|------|------|
| [okta_auth_server_policy.authpolicy](https://registry.terraform.io/providers/okta/okta/latest/docs/resources/auth_server_policy) | resource |
| [okta_auth_server_policy_rule.defaultrule](https://registry.terraform.io/providers/okta/okta/latest/docs/resources/auth_server_policy_rule) | resource |
| [okta_app.appsinpolicy](https://registry.terraform.io/providers/okta/okta/latest/docs/data-sources/app) | data source |
| [okta_auth_server.defaultsvr](https://registry.terraform.io/providers/okta/okta/latest/docs/data-sources/auth_server) | data source |

## Inputs

| Name | Description | Type | Default | Required |
|------|-------------|------|---------|:--------:|
| <a name="input_application_name"></a> [application\_name](#input\_application\_name) | This should match the application name used in the main folder | `string` | n/a | yes |
| <a name="input_client_whitelist"></a> [client\_whitelist](#input\_client\_whitelist) | An array containing the app names (w/environments), ie. YourApp-DEV, YourApp-QA, YourApp-UAT.<br>This is used to look up the client ids for you when assigning to auth policy.<br>Only used if you are creating client credentials so you can call other APIs | `list(string)` | n/a | yes |
| <a name="input_scopes"></a> [scopes](#input\_scopes) | Scopes assigned to your users, if your app calls coreapis include their scopes here.<br>Always include the 3 default scopes listed below if you override this. | `list(string)` | <pre>[<br>  "roles",<br>  "profile",<br>  "openapi",<br>  "evaluationapi",<br>  "inventoryapi",<br>  "rcmapi"<br>]</pre> | no |

## Outputs

No outputs.
