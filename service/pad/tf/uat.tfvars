# host_name     = "sh-uat-usc-postgresql-01"
# host_postfix  = ".azure.uat.signifyhealth.com"
# user_name     = "padsvc"
# database_name = "pad"

okta_app_name = "PAD_Service_UAT"

kafka_topics = {
  "PAD_Status" = {
    partitions = 10
  }
  "PAD_Results" = {
    partitions = 10
  }
  "pad_clinical_support" = {
    partitions = 10
    config = {
      "retention.ms" = "-1"
    }
  }
}
