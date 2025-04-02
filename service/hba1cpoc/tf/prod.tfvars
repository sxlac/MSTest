# host_name = "sh-prod-usc-postgresql-01"
# host_postfix = ".postgres.database.azure.com"
# user_name = "hba1cpocsvc"
# database_name = "hba1cpoc"

okta_app_name     = "HBA1CPOC_Service"
asb_namespace     = "sh-prod-usc-servicebus"
asb_resourcegroup = "sh-prod-usc-servicebus-rg"
asb_rulename      = "hba1cpocSvcAccessKey"

kafka_topics = {
  "A1CPOC_Status" = {
    partitions = 10
  }
  "A1CPOC_Results" = {
    partitions = 10
  }
}
