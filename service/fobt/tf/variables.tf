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
    "FOBT_Status" = {
      partitions = 3
    }
    "FOBT_Results" = {
      partitions = 3
    }
    "dps_labs_barcode_dlq" = {
      partitions = 1
    }
    "dps_homeaccess_labresults_dlq" = {
      partitions = 1
    }
  }
}

variable "is_prod"{
  default = false
  description = "enable prod workspace to run the module."
}

# #--------------------------
# #  VARIABLES FOR POSTGRES
# # --------------------------

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
#   description = "Set to blank when running on localhost, looks like .azure.{env}.signifyhealth.com"
# }
# variable user_name {
#   description = "User name for the db user"
# }
# variable user_password {
#   description = "Password for the db user"
# }

# variable database_name {
#   description = "database name"
# }

# variable ssl_mode {
#   default = "verify-full"
# }
