
# ---------------------------------------------------------------------------------------------------------------------
#  VARIABLES FOR AZURE RESOURCE GROUP BLOCK
# ---------------------------------------------------------------------------------------------------------------------


variable rg_name {
  description = "name of resource group in Azure."
}

variable rg_location {
  description = "Azure region for the environment"
}

variable rg_owner {
  description = "Which team owns this resource"
  default = "labrats"
}

variable rg_description {
  description = "Description of this resource"
  default = "Resource group for egfr Azure Flexible Postgresql"
}

variable rg_contact {
  description = "Email address of contact for this resource owner (usually the tech lead)"
  default = "labrats@signifyhealth.com"
}

variable rg_system {
  description = "What system does the resource belong to (can be empty for shared infrastructure, but that's rare for dev teams to manage"
  default = "egfr"
}

variable rg_shared_infra {
  description = "Whether this is shared infrastructure, usually false for dev teams"
  default = false
}

variable priv_endpoint_vnet_rg {
  description = "name of vnet resource group in Azure."
}

variable priv_endpoint_vnet_name {
  description = "Name of vnet to use"
}

variable priv_endpoint_subnet_name {
  description = "Name of subnet to use"
}

variable priv_dns_zone_name {
  description = "Name of private dns zone to use"
  default = "privatelink.postgres.database.azure.com" 
}

variable priv_dns_zone_rg {
  description = "Resource group of private dns zone"
  default = "sh-prod-usc-networking-rg"
}

variable prod_subscription_id {
  description = "The Azure subscriptionId for production (not overwritten for other environments as DNS controlled by prod)"
  default = "755929fe-7901-49df-963d-48b81042a93f"
}

variable admin_username {
  description = "Postgres username"
}

variable admin_password {
  description = "Postgres password"
}

variable postgres_name {
  description = "Postgres server name"
}

variable postgres_sku_name {
  description = "Postgres sku name"
}

variable postgres_storage_mb {
  description = "Postgres storage mb"
  default     = 131072
}

variable postgres_storage_backup_retention_days {
  description = "Postgres storage backup retention days"
  default     = 7
}

variable postgres_storage_geo_redundant_backup {
  description = "Postgres storage geo redundant backup"
  default     = false
}

variable postgres_version {
  description = "Postgres version"
  default     = "13"
}
  
variable maintenance_window_day_of_week {
  description = "The day of week for maintenance window. Defaults to 0"
  default = 0
} 

variable maintenance_window_start_hour {
  description = "The start hour for maintenance window. Defaults to 0"
  default = 0
}

variable maintenance_window_start_minute {
  description = "The start minute for maintenance window. Defaults to 0"
  default = 0
}

