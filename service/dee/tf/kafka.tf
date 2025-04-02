provider "confluent" {}

locals {
  app_name             = "signify-dee-svc"
  kubernetes_namespace = "ancillary"
}

module "consumer" {
  count                       = terraform.workspace == "prod" ? 1 : 0
  source                      = "git@github.com:hcd-tech/terraform-confluent-kafka-rbac.git?ref=v2.0.0"
  service_account             = "svc-consumer-dee-${terraform.workspace}"
  service_account_description = "Service account for dee topic consumption"
  app_name                    = local.app_name
  kubernetes_namespace        = local.kubernetes_namespace
  rbac = {
    topic = [
      {
        name = "evaluation"
        role = "DeveloperRead"
      },
      {
        name = "pdfdelivery"
        role = "DeveloperRead"
      },
      {
        name = "cdi_events"
        role = "DeveloperRead"
      },
      {
        name = "cdi_holds"
        role = "DeveloperRead"
      },
      {
        name = "rcm_bill"
        role = "DeveloperRead"
      },
      {
        name = "dee_results"
        role = "DeveloperWrite"
      },
      {
        name = "dee_results"
        role = "DeveloperRead"
      },
      {
        name = "dee_status"
        role = "DeveloperWrite"
      },
      {
        name = "dee_status"
        role = "DeveloperRead"
      },
      {
        name = "dps_evaluation_dlq"
        role = "DeveloperRead"
      },
      {
        name = "dps_evaluation_dlq"
        role = "DeveloperWrite"
      },
      {
        name = "dps_pdfdelivery_dlq"
        role = "DeveloperRead"
      },
      {
        name = "dps_pdfdelivery_dlq"
        role = "DeveloperWrite"
      },
      {
        name = "dps_cdi_events_dlq"
        role = "DeveloperRead"
      },
      {
        name = "dps_cdi_events_dlq"
        role = "DeveloperWrite"
      },
      {
        name = "dps_rcm_bill_dlq"
        role = "DeveloperRead"
      },
      {
        name = "dps_rcm_bill_dlq"
        role = "DeveloperWrite"
      },
      {
        name = "dps_cdi_holds_dlq"
        role = "DeveloperRead"
      },
      {
        name = "dps_cdi_holds_dlq"
        role = "DeveloperWrite"
      }
    ]
    group = [
      {
        id   = "Signify.Dee.Service"
        role = "DeveloperRead"
      }
    ]
  }
}

resource "confluent_kafka_topic" "this" {
  for_each = var.kafka_topics

  topic_name       = each.key
  partitions_count = each.value.partitions
  config = {
    "retention.ms" = var.kafka_retention_ms
  }
}
