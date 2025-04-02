variable "okta_app_name" {
  description = "The Application's display name, in Okta"
  type        = string
}

variable "kafka_topics" {
  description = "A map of Kafka topics and their underlying configuration"
  type = map(object({
    partitions = optional(number, 3)
    config     = optional(map(string))
  }))
  default = {
    "spirometry_status" = {
      partitions = 3
    }
    "spirometry_result" = {
      partitions = 3
    }
    "dps_overread_spirometry_dlq" = {
      partitions = 1
    }
    "dps_cdi_holds_dlq" = {
      partitions = 1
    }
  }
}

variable "k8s_cluster" {
  description = "Name of kubernetes cluster"
}

variable "k8s_ns" {
  description = "Namespace to create inside kubernetes cluster"
  default     = "spirometry"
}

variable "is_prod"{
  default = false
  description = "enable prod workspace to run the module."
}


#--------------------------
#  VARIABLES FOR POSTGRES
# --------------------------

# variable "postgres_version" {
#   default = "11.0.0"
# }

# variable "admin_username" {
#   default = "signifypostgres"
# }

# variable "admin_password" {
#   description = "Postgres admin password"
# }

# variable "host_name" {
#   description = "Server host of the postgresql instance"
# }
# variable "host_postfix" {
#   description = "Set to blank when running on localhost, typically looks like .postgres.database.azure.com"
#   default     = ".postgres.database.azure.com"
# }

# variable "database_name" {
#   description = "database name"
#   default     = "spirometry"
# }

# variable "ssl_mode" {
#   default = "verify-full"
# }

# ######
# ## If you are using the db_dualaccounts file to create two postgres accounts, replace the variables in this section with the
# ## variables defined in db_dualaccountvariables.notused!
# ## Don't forget to also remove the replace the user_name variable in your environment files
# ## with flyway_user_name and appdb_user_name as well if you're creating both accounts!

# variable "user_name" {
#   description = "User name for the db user"
#   default     = "svcspirometry"
# }

# variable "user_password" {
#   description = "Password for the db user"
# }

# variable "flyway_user_name" {
#   description = "Service account for flyway schema changes"
#   default     = "flywayspirometry"
# }

# variable "flyway_user_password" {
#   description = "Password for the flyway service account"
# }
