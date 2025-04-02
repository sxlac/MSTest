
# ---------------------------------------------------------------------------------------------------------------------
#  AZURE PROVIDER
# ---------------------------------------------------------------------------------------------------------------------

provider postgresql {
  host              = var.host_name
  username          = var.admin_username
  password          = var.admin_password
  database_username = var.admin_username
  sslmode           = var.ssl_mode
  connect_timeout   = 30
  expected_version  = "10.0.0"
  superuser         = false
}


# ---------------------------------------------------------------------------------------------------------------------
#  POSTGRES USER
# ---------------------------------------------------------------------------------------------------------------------
resource postgresql_role dbuser {
  name     = var.user_name
  login    = true
  password = var.user_password
}

# ---------------------------------------------------------------------------------------------------------------------
#  POSTGRES DATABASE
# ---------------------------------------------------------------------------------------------------------------------

resource postgresql_database db {
  name  = var.database_name
  owner = postgresql_role.dbuser.name
}

# ---------------------------------------------------------------------------------------------------------------------
#  POSTGRES PRIVILEGES
# ---------------------------------------------------------------------------------------------------------------------
resource postgresql_default_privileges privileges {
  role        = postgresql_role.dbuser.name
  owner       = var.admin_username
  database    = postgresql_database.db.name
  schema      = "public"
  object_type = "table"
  privileges  = ["ALL"]
}
