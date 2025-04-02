okta_app_name = "uacr"

kafka_topics = {
  "uacr_status" = {
    partitions = 10
  }
  "uacr_results" = {
    partitions = 10
  }
  "dps_rms_labresult_dlq" = {
      partitions = 10
    }
}
