locals {
  app_name             = "signify-uacr-svc"
  kubernetes_namespace = "ancillary"
}
module "consumer" {
  source  = "git@github.com:hcd-signifyhealth/terraform-confluent-kafka-rbac.git?ref=v2.0.0"
  service_account             = "svc-consumer-uacr-${terraform.workspace}"
  service_account_description = "Service account for uACR topic consumption"
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
        name = "uacr_results"
        role = "DeveloperWrite"
      },
	  {
        name = "uacr_results"
        role = "DeveloperRead"
      },
      {
        name = "uacr_status"
        role = "DeveloperWrite"
      },
	  {
        name = "uacr_status"
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
        name = "dps_labresult_uacr"
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
      },
      {
        name = "dps_rms_labresult_dlq"
        role = "DeveloperRead"
      },
      {
        name = "dps_rms_labresult_dlq"
        role = "DeveloperWrite"
      }
    ]
    group = [
      {
        id   = "Signify.DPS.Uacr.svc"
        role = "DeveloperRead"
      }
    ]
  }
}

#TBD whether we want to try to manage any ACLs via terraform
#https://github.com/Mongey/terraform-provider-kafka (see sasl_username, sasl_password etc.)

#You will need the password for the confluent user configured for the environment you're executing against.

provider "confluent" {}

resource "confluent_kafka_topic" "this" {
  for_each = var.kafka_topics

  topic_name       = each.key
  partitions_count = each.value.partitions
  config           = each.value.config
}

