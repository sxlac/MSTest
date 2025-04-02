# Note:
#
# DBRE team have a schedule to migrate DBs created by teams outside of their control.
#
# Therefore they do not need to be onboarded to the remote state, and this has
# been agreed between the SRE <-> DBRE teams.
# Reference:
# - https://jira.signifyhealth.com/browse/ANC-3742
#
#
# # ---------------------------------------------------------------------------------------------------------------------
# #  AZURE PROVIDER
# # ---------------------------------------------------------------------------------------------------------------------

# provider "postgresql" {
#   host              = "${var.host_name}${var.host_postfix}"
#   username          = "${var.admin_username}@${var.host_name}"
#   password          = var.admin_password
#   database_username = var.admin_username
#   sslmode           = var.ssl_mode
#   connect_timeout   = 30
#   expected_version  = "10.0.0"
#   superuser         = false
# }


# # ---------------------------------------------------------------------------------------------------------------------
# #  POSTGRES USER
# # ---------------------------------------------------------------------------------------------------------------------
# resource "postgresql_role" "dbuser" {
#   name     = var.user_name
#   login    = true
#   password = var.user_password
# }

# # ---------------------------------------------------------------------------------------------------------------------
# #  POSTGRES DATABASE
# # ---------------------------------------------------------------------------------------------------------------------

# resource "postgresql_database" "db" {
#   name  = var.database_name
#   owner = postgresql_role.dbuser.name
# }

# # ---------------------------------------------------------------------------------------------------------------------
# #  POSTGRES PRIVILEGES
# # ---------------------------------------------------------------------------------------------------------------------
# resource "postgresql_default_privileges" "privileges" {
#   role        = postgresql_role.dbuser.name
#   owner       = var.admin_username
#   database    = postgresql_database.db.name
#   schema      = "public"
#   object_type = "table"
#   privileges  = ["ALL"]
# }
