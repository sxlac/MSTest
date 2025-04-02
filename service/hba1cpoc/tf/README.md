## Requirements

| Name | Version |
|------|---------|
| <a name="requirement_terraform"></a> [terraform](#requirement\_terraform) | >= 1.5.0 |
| <a name="requirement_azurerm"></a> [azurerm](#requirement\_azurerm) | ~> 3.84.0 |
| <a name="requirement_confluent"></a> [confluent](#requirement\_confluent) | ~> 1.55.0 |
| <a name="requirement_okta"></a> [okta](#requirement\_okta) | ~> 4.6.1 |
| <a name="requirement_postgresql"></a> [postgresql](#requirement\_postgresql) | 1.17.1 |

## Providers

| Name | Version |
|------|---------|
| <a name="provider_azurerm"></a> [azurerm](#provider\_azurerm) | ~> 3.84.0 |
| <a name="provider_confluent"></a> [confluent](#provider\_confluent) | ~> 1.55.0 |
| <a name="provider_okta"></a> [okta](#provider\_okta) | ~> 4.6.1 |

## Modules

No modules.

## Resources

| Name | Type |
|------|------|
| [azurerm_servicebus_namespace_authorization_rule.authrule](https://registry.terraform.io/providers/hashicorp/azurerm/latest/docs/resources/servicebus_namespace_authorization_rule) | resource |
| [confluent_kafka_topic.this](https://registry.terraform.io/providers/confluentinc/confluent/latest/docs/resources/kafka_topic) | resource |
| [okta_app_oauth.appname](https://registry.terraform.io/providers/okta/okta/latest/docs/resources/app_oauth) | resource |
| [azurerm_servicebus_namespace.this](https://registry.terraform.io/providers/hashicorp/azurerm/latest/docs/data-sources/servicebus_namespace) | data source |

## Inputs

| Name | Description | Type | Default | Required |
|------|-------------|------|---------|:--------:|
| <a name="input_asb_listen"></a> [asb\_listen](#input\_asb\_listen) | n/a | `bool` | `true` | no |
| <a name="input_asb_manage"></a> [asb\_manage](#input\_asb\_manage) | n/a | `bool` | `true` | no |
| <a name="input_asb_namespace"></a> [asb\_namespace](#input\_asb\_namespace) | ex. sh-dev-usc-servicebus | `string` | n/a | yes |
| <a name="input_asb_resourcegroup"></a> [asb\_resourcegroup](#input\_asb\_resourcegroup) | ex. sh-dev-usc-servicebus-rg | `string` | n/a | yes |
| <a name="input_asb_rulename"></a> [asb\_rulename](#input\_asb\_rulename) | identifier for the access rule, ex. MyAppAccessKey | `string` | n/a | yes |
| <a name="input_asb_send"></a> [asb\_send](#input\_asb\_send) | n/a | `bool` | `true` | no |
| <a name="input_azurerm_subscription_id"></a> [azurerm\_subscription\_id](#input\_azurerm\_subscription\_id) | The Azure subscription ID | `string` | n/a | yes |
| <a name="input_kafka_topics"></a> [kafka\_topics](#input\_kafka\_topics) | A map of Kafka topics and their underlying configuration | <pre>map(object({<br>    partitions = optional(number, 3)<br>    config     = optional(map(string))<br>  }))</pre> | <pre>{<br>  "A1CPOC_Results": {<br>    "partitions": 3<br>  },<br>  "A1CPOC_Status": {<br>    "partitions": 3<br>  }<br>}</pre> | no |
| <a name="input_okta_app_name"></a> [okta\_app\_name](#input\_okta\_app\_name) | The Application's display name, in Okta | `string` | n/a | yes |

## Outputs

| Name | Description |
|------|-------------|
| <a name="output_asb_connstring"></a> [asb\_connstring](#output\_asb\_connstring) | n/a |
| <a name="output_client_id"></a> [client\_id](#output\_client\_id) | n/a |
