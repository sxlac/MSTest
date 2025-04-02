locals {
  app_name             = "signify-fobt-svc"
  kubernetes_namespace = "ancillary"
}
module "consumer" {
  source                      = "git@github.com:hcd-tech/terraform-confluent-kafka-rbac.git?ref=v2.0.0"
  service_account             = "svc-consumer-fobt-${terraform.workspace}"
  service_account_description = "Service account for FOBT topic consumption"
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
        name = "rcm_bill"
        role = "DeveloperRead"
      },
      {
        name = "FOBT_Results"
        role = "DeveloperWrite"
      },
	  {
        name = "FOBT_Results"
        role = "DeveloperRead"
      },
      {
        name = "FOBT_Status"
        role = "DeveloperWrite"
      },
	  {
        name = "FOBT_Status"
        role = "DeveloperRead"
      },
      {
        name = "labs_barcode"
        role = "DeveloperRead"
      },
      {
        name = "labs_holds"
        role = "DeveloperRead"
      },
      {
        name = "homeaccess_labresults"
        role = "DeveloperRead"
      },
      {
        name = "dps_evaluation_dlq"
        role = "DeveloperWrite"
      },
      {
        name = "dps_evaluation_dlq"
        role = "DeveloperRead"
      },
      {
        name = "dps_labs_barcode_dlq"
        role = "DeveloperWrite"
      },
      {
        name = "dps_labs_barcode_dlq"
        role = "DeveloperRead"
      },
      {
        name = "dps_labs_holds_dlq"
        role = "DeveloperWrite"
      },
      {
        name = "dps_labs_holds_dlq"
        role = "DeveloperRead"
      },
      {
        name = "dps_homeaccess_labresults_dlq"
        role = "DeveloperWrite"
      },
      {
        name = "dps_homeaccess_labresults_dlq"
        role = "DeveloperRead"
      },
      {
        name = "dps_pdfdelivery_dlq"
        role = "DeveloperWrite"
      },
      {
        name = "dps_pdfdelivery_dlq"
        role = "DeveloperRead"
      },
      {
        name = "dps_cdi_events_dlq"
        role = "DeveloperWrite"
      },
      {
        name = "dps_cdi_events_dlq"
        role = "DeveloperRead"
      },
      {
        name = "dps_rcm_bill_dlq"
        role = "DeveloperWrite"
      },
      {
        name = "dps_rcm_bill_dlq"
        role = "DeveloperRead"
      }
    ]
    group = [
      {
        id   = "Signify.FOBT.Service.v1"
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
