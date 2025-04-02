# host_name         = "sh-prod-usc-postgresql-01"
# host_postfix      = ".postgres.database.azure.com"
# user_name         = "ckdsvc"
# database_name     = "ckd"

okta_app_name = "CKD_Service"
kafka_topics = {
  "ckd_status" = {
    partitions = 10
  }
  "ckd_results" = {
    partitions = 10
  }
}
