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
    "PAD_Status" = {
      partitions = 3
    }
    "PAD_Results" = {
      partitions = 3
    }
    "pad_clinical_support" = {
      partitions = 3
    }
  }
}


# #--------------------------
# #  VARIABLES FOR POSTGRES
# # --------------------------

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
#   description = "Set to blank when running on localhost, looks like .azure.{env}.signifyhealth.com"
# }
# variable "user_name" {
#   description = "User name for the db user"
# }
# variable "user_password" {
#   description = "Password for the db user"
# }

# variable "database_name" {
#   description = "database name"
# }

# variable "ssl_mode" {
#   default = "verify-full"
# }
