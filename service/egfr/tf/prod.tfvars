# host_name                = "hcc-prod-usc-postgresql-egfr"
k8s_cluster   = "prd-usc-shared-aks"
okta_app_name = "egfr"

kafka_topics = {
  "egfr" = {
    partitions = 10
    config = {
      "message.timestamp.type" = "CreateTime"
      "retention.ms"           = "-1"
    }
  }
  "egfr_status" = {
    partitions = 10
    config = {
      "retention.ms" = "-1"
    }
  }
  "egfr_results" = {
    partitions = 10
    config = {
      "retention.ms" = "-1"
    }
  }
}
