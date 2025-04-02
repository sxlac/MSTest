locals {
  app_name             = "signify-egfr-svc"
  kubernetes_namespace = "ancillary"
}
module "consumer" {
  source  = "git@github.com:hcd-signifyhealth/terraform-confluent-kafka-rbac.git?ref=v2.0.0"
  service_account             = "svc-consumer-egfr-${terraform.workspace}"
  service_account_description = "Service account for eGFR topic consumption"
  app_name             = local.app_name
  kubernetes_namespace = local.kubernetes_namespace
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
        name = "egfr_results"
        role = "DeveloperWrite"
      },
	  {
        name = "egfr_results"
        role = "DeveloperRead"
      },
      {
        name = "egfr_status"
        role = "DeveloperWrite"
      },
	  {
        name = "egfr_status"
        role = "DeveloperRead"
      },
      {
        name = "dps_oms_order"
        role = "DeveloperRead"    
      },
      {
        name = "dps_oms_order"
        role = "DeveloperWrite"    
      },
      {
        name = "labs_barcode"
        role = "DeveloperRead"
      },
      {
        name = "dps_labresult_egfr"
        role = "DeveloperRead"
      },
      {
        name = "egfr_lab_results"
        role = "DeveloperRead"
      },
      {
        name = "dps_rms_labresult"
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
        name = "dps_labresult_dlq"
        role = "DeveloperRead"
      },
      {
        name = "dps_labresult_dlq"
        role = "DeveloperWrite"
      }
    ]
    group = [
      {
        id   = "eGFR.svc"
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
