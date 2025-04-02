#Instance Variables (change these)
postgres_name                   = "hcc-uat-usc-postgresql-egfr"
rg_name             			= "hcc-uat-usc-postgresql-egfr-rg"
maintenance_window_day_of_week  = 0
maintenance_window_start_hour   = 3
maintenance_window_start_minute = 0
postgres_sku_name = "GP_Standard_D2s_v3" #Usually similar to but slightly less powerful than prod.

#These shouldn't need to be changed
rg_location                    = "Central US"
admin_username              = "signifypostgres"

#Networking Variables (don't change these)
priv_endpoint_vnet_rg       = "sh-uat-usc-networking-vnet-ii-rg"
priv_endpoint_vnet_name     = "sh-uat-usc-networking-vnet-ii" 
priv_endpoint_subnet_name   = "hcc-uat-postgresql-flexible-subnet"
