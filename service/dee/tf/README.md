## Requirements

| Name | Version |
|------|---------|
| <a name="requirement_terraform"></a> [terraform](#requirement\_terraform) | >= 0.13 |
| <a name="requirement_confluent"></a> [confluent](#requirement\_confluent) | ~> 1.55.0 |
| <a name="requirement_okta"></a> [okta](#requirement\_okta) | ~> 4.6.1 |

## Providers

| Name | Version |
|------|---------|
| <a name="provider_confluent"></a> [confluent](#provider\_confluent) | 1.55.0 |
| <a name="provider_okta"></a> [okta](#provider\_okta) | 4.6.1 |

## Modules

No modules.

## Resources

| Name | Type |
|------|------|
| [confluent_kafka_topic.this](https://registry.terraform.io/providers/confluentinc/confluent/latest/docs/resources/kafka_topic) | resource |
| [okta_app_oauth.dee_service_oktaapp](https://registry.terraform.io/providers/okta/okta/latest/docs/resources/app_oauth) | resource |

## Inputs

| Name | Description | Type | Default | Required |
|------|-------------|------|---------|:--------:|
| <a name="input_kafka_retention_ms"></a> [kafka\_retention\_ms](#input\_kafka\_retention\_ms) | Maximum time we retain a log before we will discard old log segments, in milliseconds.<br>Generally set to a week or so on preprod and -1 on prod to retain forever. | `number` | n/a | yes |
| <a name="input_kafka_topics"></a> [kafka\_topics](#input\_kafka\_topics) | A map of Kafka topics and their underlying configuration | <pre>map(object({<br>    partitions = optional(number, 3)<br>  }))</pre> | <pre>{<br>  "dee_results": {<br>    "paritions": 3<br>  },<br>  "dee_status": {<br>    "partitions": 3<br>  }<br>}</pre> | no |
| <a name="input_okta_app_name"></a> [okta\_app\_name](#input\_okta\_app\_name) | The Application's display name, in Okta | `string` | n/a | yes |

## Outputs

| Name | Description |
|------|-------------|
| <a name="output_client_id"></a> [client\_id](#output\_client\_id) | n/a |
