variable "kafka_topics" {
  description = "A map of Kafka topics and their underlying configuration"
  type = map(object({
    partitions = optional(number, 3)
    config     = optional(map(string))
  }))
  default = {
    "uacr_status" = {
      partitions = 3
    }
    "uacr_results" = {
      partitions = 3
    }
    "dps_rms_labresult_dlq" = {
      partitions = 3
    }
  }
}

variable "okta_app_name" {
  description = "The Application's display name, in Okta"
  type        = string
}

#--------------------------
#  VARIABLES FOR POSTGRES
# --------------------------


# variable postgres_version {
#   default = "13.0.0"
# }

# variable admin_username {
#   default = "signifypostgres"
# }

# variable admin_password {
#   description = "Postgres admin password"
# }

# variable host_name {
#   description = "Server host of the postgresql instance"
# }
# variable host_postfix {
#   description = "Set to blank when running on localhost, typically looks like .postgres.database.azure.com"
#   default = ".postgres.database.azure.com"
# }

# variable database_name {
#   description = "database name"
#   default = "uACR"
# }

# variable ssl_mode {
#   default = "verify-full"
# }

######
## If you are using the db_dualaccounts file to create two postgres accounts, replace the variables in this section with the
## variables defined in db_dualaccountvariables.notused!
## Don't forget to also remove the replace the user_name variable in your environment files
## with flyway_user_name and appdb_user_name as well if you're creating both accounts!

# variable user_name {
#   description = "User name for the db user"
#   default = "svcuACR"
# }

# variable user_password {
#   description = "Password for the db user"
# }

# variable flyway_user_name {
#   description = "Service account for flyway schema changes"
#   default = "flywayuACR"
# }

# variable flyway_user_password {
#   description = "Password for the flyway service account"
# }

## End of replaceable variables.
########



#--------------------------
#  VARIABLES FOR REDIS/HELM
# --------------------------

#variable k8s_cluster {
#  description = "DUP from k8s section"
#}


## End of replaceable variables.
########
