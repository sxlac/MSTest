
#------------------------------------------------------------------------------------------
# GENERAL
#-------------

variable app_name {
  description = "Used to define the workspace"
}

#-------------------------
#  Kafka Topic
#  You only need this if you are PUBLISHING, not if you are only SUBSCRIBING.
#  If you are publishing more than one topic,  create additional copies of variables as needed for different topics and modify the script accordingly.
#-------------------------
//variable "kafka_servers" {
// type = list
//  description = "Same list for all topics."
//}
//variable "kafka_retentionms" {
//  description = "maximum time we retain a log before we will discard old log segments, generally set to a week or so on preprod and -1 on prod to retain forever"
//}

//variable "kafka_topic" {
//  description = "topic name, either in 'namespace' (ex 'capacity' or 'evaluations') or 'namespace_entity/concept' format (ex. 'okta_users') to avoid redundancy"
//}
//variable "kafka_replication_factor" {
//  description = "Set to at least 2 in prod"
// default = 1
//}
//variable "kafka_partitions" {
//  description = "Defaults to 3 for all envs, discuss with your architect for higher traffic topics."
//  default = 3
//}
#----------------------------
#.  VARIABLES FOR KUBERNETES
#----------------------------

variable "k8s_cluster" {
  description = "Name of kubernetes cluster"
}
variable "k8s_ns" {
  description = "Namespace to create inside kubernetes cluster"
}

#---------------------------
#. VARIABLES FOR OKTA
#---------------------------


variable okta_api_token {
  description = "Don't include in tfvars files > set with 'export TF_VAR_okta_api_token={your token}' before applying"
}
variable okta_app_name {
}

variable okta_url {
}


#--------------------------
#  VARIABLES FOR POSTGRES
# --------------------------

variable admin_username {
  default = "signifypostgres"
}

variable admin_password {
  description = "Postgres admin password"
}

variable host_name {
  description = "Server host of the postgresql instance"
}
variable host_postfix {
  description = "Set to blank when running on localhost, looks like .azure.{env}.signifyhealth.com"
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
  default = "verify-full"
}


#--------------------------
#  VARIABLES FOR AZURE SERVICE BUS
# --------------------------

variable asb_namespace {
  description = "ex. sh-dev-usc-servicebus"
}

variable asb_resourcegroup {
  description = "ex. sh-dev-usc-servicebus-rg"
}

variable asb_rulename {
  description = "identifier for the access rule, ex. MyAppAccessKey"
}

variable asb_listen {
  default = true
}

variable asb_send {
  default = true
}

variable asb_manage {
  default = true
}


#--------------------------
#  VARIABLES FOR REDIS/HELM
# --------------------------

#variable k8s_cluster {
#  description = "DUP from k8s section"
#}

//variable redis_instance_name {
//  description = "Name of your redis instance"
//  default = "servicesample-redis"
//}
//variable redis_namespace_name {
//  description = "Kubernetes namespace that you're putting redis in"
//  default = "servicesample"
//}
//variable redis_master_requested_memory {
//  description = "ex. 64Mi"
// default = "64Mi"
//}
//variable redis_master_requested_cpu {
//  description = "ex. 64m"
//  default = "64m"
//}
//variable redis_slave_requested_memory {
//  description = "ex. 64Mi"
//  default = "64Mi"
//}
//variable redis_slave_requested_cpu {
//  description = "ex. 64m"
//  default = "64m"
//}
