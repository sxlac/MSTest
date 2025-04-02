## Requirements

| Name | Version |
|------|---------|
| <a name="requirement_terraform"></a> [terraform](#requirement\_terraform) | >= 1.5.0 |
| <a name="requirement_confluent"></a> [confluent](#requirement\_confluent) | ~> 1.55.0 |
| <a name="requirement_kubernetes"></a> [kubernetes](#requirement\_kubernetes) | ~> 2.24.0 |
| <a name="requirement_okta"></a> [okta](#requirement\_okta) | ~> 4.6.3 |

## Providers

| Name | Version |
|------|---------|
| <a name="provider_confluent"></a> [confluent](#provider\_confluent) | ~> 1.55.0 |
| <a name="provider_kubernetes"></a> [kubernetes](#provider\_kubernetes) | ~> 2.24.0 |
| <a name="provider_okta"></a> [okta](#provider\_okta) | ~> 4.6.3 |

## Modules

No modules.

## Resources

| Name | Type |
|------|------|
| [confluent_kafka_topic.this](https://registry.terraform.io/providers/confluentinc/confluent/latest/docs/resources/kafka_topic) | resource |
| [kubernetes_namespace.spirometry_k8s_namespace](https://registry.terraform.io/providers/hashicorp/kubernetes/latest/docs/resources/namespace) | resource |
| [okta_app_oauth.spirometry_oktaapp](https://registry.terraform.io/providers/okta/okta/latest/docs/resources/app_oauth) | resource |

## Inputs

| Name | Description | Type | Default | Required |
|------|-------------|------|---------|:--------:|
| <a name="input_k8s_cluster"></a> [k8s\_cluster](#input\_k8s\_cluster) | Name of kubernetes cluster | `any` | n/a | yes |
| <a name="input_k8s_ns"></a> [k8s\_ns](#input\_k8s\_ns) | Namespace to create inside kubernetes cluster | `string` | `"spirometry"` | no |
| <a name="input_kafka_topics"></a> [kafka\_topics](#input\_kafka\_topics) | A map of Kafka topics and their underlying configuration | <pre>map(object({<br>    partitions = optional(number, 3)<br>    config     = optional(map(string))<br>  }))</pre> | <pre>{<br>  "spirometry_result": {<br>    "partitions": 3<br>  },<br>  "spirometry_status": {<br>    "partitions": 3<br>  }<br>}</pre> | no |
| <a name="input_okta_app_name"></a> [okta\_app\_name](#input\_okta\_app\_name) | The Application's display name, in Okta | `string` | n/a | yes |

## Outputs

| Name | Description |
|------|-------------|
| <a name="output_client_id"></a> [client\_id](#output\_client\_id) | n/a |
