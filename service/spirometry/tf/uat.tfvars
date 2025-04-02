# host_name        = "sh-uat-usc-postgresql-pm"

k8s_cluster   = "uat-usc-shared-aks"
okta_app_name = "spirometry_UAT"

kafka_topics = {
  "spirometry_status" = {
    partitions = 10
  }
  "spirometry_result" = {
    partitions = 10
  }
  "dps_overread_spirometry_dlq" = {
    partitions = 1
  }
  "dps_cdi_holds_dlq" = {
    partitions = 1
  }
}
