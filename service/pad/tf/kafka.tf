provider "confluent" {}

# Environment naming convention isn't consistent, so we need to map it
locals {
  environment = {
    dev  = "Dev"
    qa   = "QA"
    uat  = "UAT"
    prod = "Prod"
  }
}

# Do a data lookup for the environment, by using the current workspace as the lookup key
data "confluent_environment" "this" {
  display_name = local.environment[terraform.workspace]
}

data "confluent_kafka_cluster" "this" {
  display_name = "hcc-${terraform.workspace}-usc-kafka"
  environment {
    id = data.confluent_environment.this.id
  }
}

data "confluent_service_account" "pad" {
  display_name = "svcPAD"
}

resource "confluent_role_binding" "pad_clinical_support_read" {
  principal   = "User:${data.confluent_service_account.pad.id}"
  role_name   = "DeveloperRead"
  crn_pattern = "${data.confluent_kafka_cluster.this.rbac_crn}/kafka=${data.confluent_kafka_cluster.this.id}/topic=pad_clinical_support"
}

resource "confluent_role_binding" "pad_clinical_support_write" {
  principal   = "User:${data.confluent_service_account.pad.id}"
  role_name   = "DeveloperWrite"
  crn_pattern = "${data.confluent_kafka_cluster.this.rbac_crn}/kafka=${data.confluent_kafka_cluster.this.id}/topic=pad_clinical_support"
}

resource "confluent_role_binding" "dps_cdi_events_dlq_read" {
  principal   = "User:${data.confluent_service_account.pad.id}"
  role_name   = "DeveloperRead"
  crn_pattern = "${data.confluent_kafka_cluster.this.rbac_crn}/kafka=${data.confluent_kafka_cluster.this.id}/topic=dps_cdi_events_dlq"
}

resource "confluent_role_binding" "dps_cdi_events_dlq_write" {
  principal   = "User:${data.confluent_service_account.pad.id}"
  role_name   = "DeveloperWrite"
  crn_pattern = "${data.confluent_kafka_cluster.this.rbac_crn}/kafka=${data.confluent_kafka_cluster.this.id}/topic=dps_cdi_events_dlq"
}

resource "confluent_role_binding" "dps_evaluation_dlq_read" {
  principal   = "User:${data.confluent_service_account.pad.id}"
  role_name   = "DeveloperRead"
  crn_pattern = "${data.confluent_kafka_cluster.this.rbac_crn}/kafka=${data.confluent_kafka_cluster.this.id}/topic=dps_evaluation_dlq"
}

resource "confluent_role_binding" "dps_evaluation_dlq_write" {
  principal   = "User:${data.confluent_service_account.pad.id}"
  role_name   = "DeveloperWrite"
  crn_pattern = "${data.confluent_kafka_cluster.this.rbac_crn}/kafka=${data.confluent_kafka_cluster.this.id}/topic=dps_evaluation_dlq"
}

resource "confluent_role_binding" "dps_pdfdelivery_dlq_read" {
  principal   = "User:${data.confluent_service_account.pad.id}"
  role_name   = "DeveloperRead"
  crn_pattern = "${data.confluent_kafka_cluster.this.rbac_crn}/kafka=${data.confluent_kafka_cluster.this.id}/topic=dps_pdfdelivery_dlq"
}

resource "confluent_role_binding" "dps_pdfdelivery_dlq_write" {
  principal   = "User:${data.confluent_service_account.pad.id}"
  role_name   = "DeveloperWrite"
  crn_pattern = "${data.confluent_kafka_cluster.this.rbac_crn}/kafka=${data.confluent_kafka_cluster.this.id}/topic=dps_pdfdelivery_dlq"
}

resource "confluent_role_binding" "dps_rms_labresult_dlq_read" {
  principal   = "User:${data.confluent_service_account.pad.id}"
  role_name   = "DeveloperRead"
  crn_pattern = "${data.confluent_kafka_cluster.this.rbac_crn}/kafka=${data.confluent_kafka_cluster.this.id}/topic=dps_rms_labresult_dlq"
}

resource "confluent_role_binding" "dps_rms_labresult_dlq_write" {
  principal   = "User:${data.confluent_service_account.pad.id}"
  role_name   = "DeveloperWrite"
  crn_pattern = "${data.confluent_kafka_cluster.this.rbac_crn}/kafka=${data.confluent_kafka_cluster.this.id}/topic=dps_rms_labresult_dlq"
}

# TODO: import other RBAC roles here

resource "confluent_kafka_topic" "this" {
  for_each = var.kafka_topics

  topic_name       = each.key
  partitions_count = each.value.partitions
  config           = each.value.config
}
