# host_name      = "hcc-qa-usc-postgresql-egfr"
k8s_cluster   = "qa-usc-shared-aks"
okta_app_name = "egfr_QA"

kafka_topics = {
  "egfr" = {
    partitions = 3
    config = {
      "message.timestamp.type" = "CreateTime"
      "retention.ms"           = "172800000"
    }
  }
  "egfr_status" = {
    partitions = 3
    config = {
      "retention.ms"                        = "604800000"
      "cleanup.policy"                      = "delete"
      "delete.retention.ms"                 = "86400000"
      "max.compaction.lag.ms"               = "9223372036854775807"
      "max.message.bytes"                   = "2097164"
      "message.timestamp.difference.max.ms" = "9223372036854775807"
      "message.timestamp.type"              = "CreateTime"
      "min.compaction.lag.ms"               = "0"
      "min.insync.replicas"                 = "2"
      "retention.bytes"                     = "-1"
      "segment.bytes"                       = "104857600"
      "segment.ms"                          = "604800000"
    }
  }
  "egfr_results" = {
    partitions = 3
    config = {
      "retention.ms"                        = "604800000"
      "cleanup.policy"                      = "delete"
      "delete.retention.ms"                 = "86400000"
      "max.compaction.lag.ms"               = "9223372036854775807"
      "max.message.bytes"                   = "2097164"
      "message.timestamp.difference.max.ms" = "9223372036854775807"
      "message.timestamp.type"              = "CreateTime"
      "min.compaction.lag.ms"               = "0"
      "min.insync.replicas"                 = "2"
      "retention.bytes"                     = "-1"
      "segment.bytes"                       = "104857600"
      "segment.ms"                          = "604800000"
    }
  }
}
