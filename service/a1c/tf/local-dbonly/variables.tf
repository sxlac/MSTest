
#------------------------------------------------------------------------------------------
# GENERAL
#-------------

variable app_name {
  description = "Used to define the workspace"
}


#--------------------------
#  VARIABLES FOR POSTGRES
# --------------------------

variable admin_username {
  description = "signifypostgres@{hostname}"
}

variable admin_password {
  description = "Postgres admin password"
}

variable host_name {
  description = "Server host of the postgresql instance; includes postfix when not local"
}
variable user_name {
  description = "User name for the db user"
}
variable user_password {
  description = "Password for the db user"
}
variable database_name {
  description = "database name"
}

variable ssl_mode {
  default = "require"
}