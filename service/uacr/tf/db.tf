# Note:
#
# DBRE team have a schedule to migrate DBs created by teams outside of their control.
#
# Therefore they do not need to be onboarded to the remote state, and this has
# been agreed between the SRE <-> DBRE teams.
# Reference:
# - https://jira.signifyhealth.com/browse/ANC-3742

# If you are using NServiceBus or Signify.CAP (which the template assumes you are) then your application db service account
# needs full schema creation permissions just like flyway, so there is no purpose in creating two separate accounts for
# flyway vs. the application.
#
# If you are NOT using NServiceBus or Signify.CAP and need a database, it is more secure to create separate accounts (principle of least access)
# and you should remove this file and follow the instructions in db_dualaccounts.notused to use that file instead.

# ---------------------------------------------------------------------------------------------------------------------
#  AZURE PROVIDER
# ---------------------------------------------------------------------------------------------------------------------

# provider postgresql {
#   host              = "${var.host_name}${var.host_postfix}"

#   #v13 format
#   username          = var.admin_username
#   #if you're targeting v10 or 11, use the following line instead of the above.
#   #username         = "${var.admin_username}@${var.host_name}"

#   password          = var.admin_password
#   database_username = var.admin_username
#   sslmode           = var.ssl_mode
#   connect_timeout   = 30
#   expected_version  = var.postgres_version
#   superuser         = false
# }


# ---------------------------------------------------------------------------------------------------------------------
#  POSTGRES USER
#  The user has elevated access to create/modify schema, used for both flyway and application access
#  for applications that need to create schema (ie. they use Signify.CAP or NServiceBus)
# ---------------------------------------------------------------------------------------------------------------------

# resource postgresql_role dbuser {
#   name     = var.user_name
#   login    = true
#   password = var.user_password
# }


# ---------------------------------------------------------------------------------------------------------------------
#  POSTGRES DATABASE
# ---------------------------------------------------------------------------------------------------------------------

# resource postgresql_database db {
#   name  = var.database_name
#   owner = postgresql_role.dbuser.name
# }

# ---------------------------------------------------------------------------------------------------------------------
#  POSTGRES PRIVILEGES FOR  dbuser
# ---------------------------------------------------------------------------------------------------------------------

# resource postgresql_default_privileges dbprivileges {
#   role        = postgresql_role.dbuser.name
#   owner       = var.admin_username
#   database    = postgresql_database.db.name
#   schema      = "public"
#   object_type = "table"
#   privileges  = ["DELETE","INSERT","REFERENCES","SELECT","TRIGGER","TRUNCATE","UPDATE"]
# }

# ---------------------------------------------------------------------------------------------------------------------
#  POSTGRES FLYWAY USER
# This one has elevated access to create/modify schema, used for flyway.
# ---------------------------------------------------------------------------------------------------------------------

# resource postgresql_role flywaydbuser {
#   name     = var.flyway_user_name
#   login    = true
#   password = var.flyway_user_password
# }

# ---------------------------------------------------------------------------------------------------------------------
#  POSTGRES PRIVILEGES FOR flyway user
# ---------------------------------------------------------------------------------------------------------------------

# resource postgresql_default_privileges flywaydbprivileges {
#   role        = postgresql_role.flywaydbuser.name
#   owner       = var.admin_username
#   database    = postgresql_database.db.name
#   schema      = "public"
#   object_type = "table"
#   privileges  = ["DELETE","INSERT","REFERENCES","SELECT","TRIGGER","TRUNCATE","UPDATE"]
# }
