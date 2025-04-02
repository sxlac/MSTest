locals {
  app_name             = "signify-spirometry-svc"
  kubernetes_namespace = "spirometry"
}

module "consumer" {
  count                       = terraform.workspace == "prod" ? 1 : 0
  source                      = "git@github.com:hcd-signifyhealth/terraform-confluent-kafka-rbac.git?ref=v2.0.0"
  service_account             = "svc-consumer-spirometry-${terraform.workspace}"
  service_account_description = "Service account for spirometry topic consumption"
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
        name = "overread_spirometry"
        role = "DeveloperRead"
      },
      {
        name = "cdi_holds"
        role = "DeveloperRead"
      },
      {
        name = "cdi_events"
        role = "DeveloperRead"
      },
      {
        name = "rcm_bill"
        role = "DeveloperRead"
      },
      {
        name = "spirometry_status"
        role = "DeveloperWrite"
      },
      {
        name = "spirometry_result"
        role = "DeveloperWrite"
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
        name = "dps_overread_spirometry_dlq"
        role = "DeveloperRead"
      },
      {
        name = "dps_overread_spirometry_dlq"
        role = "DeveloperWrite"
      },
      {
        name = "dps_cdi_holds_dlq"
        role = "DeveloperRead"
      },
      {
        name = "dps_cdi_holds_dlq"
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
      }
    ]
    group = [
      {
        id   = "Signify.Spirometry.Svc"
        role = "DeveloperRead"
      }
    ]
  }
}

provider "confluent" {}

resource "confluent_kafka_topic" "this" {
  for_each = var.kafka_topics

  topic_name       = each.key
  partitions_count = each.value.partitions
  config           = each.value.config
}
