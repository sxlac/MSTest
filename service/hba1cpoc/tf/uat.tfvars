# host_name = "sh-uat-usc-postgresql-01"
# host_postfix = ".postgres.database.azure.com"
# user_name = "hba1cpocsvc"
# database_name = "hba1cpoc"

okta_app_name     = "HBA1CPOC_Service_UAT"
asb_namespace     = "sh-uat-usc-servicebus"
asb_resourcegroup = "sh-uat-usc-servicebus-rg"
asb_rulename      = "hba1cpocSvcAccessKey"

kafka_topics = {
  "A1CPOC_Status" = {
    partitions = 10
  }
  "A1CPOC_Results" = {
    partitions = 10
  }
}
