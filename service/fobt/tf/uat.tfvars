# host_name     = "sh-uat-usc-postgresql-01"
# host_postfix  = ".azure.uat.signifyhealth.com"
# user_name     = "fobtsvc"
# database_name = "fobt"

okta_app_name = "FOBT_Service_UAT"
kafka_topics = {
  "FOBT_Status" = {
    partitions = 10
  }
  "FOBT_Results" = {
    partitions = 10
  }
  "dps_labs_barcode_dlq" = {
    partitions = 1
  }
  "dps_homeaccess_labresults_dlq" = {
    partitions = 1
  }
}
