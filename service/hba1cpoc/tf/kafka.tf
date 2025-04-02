provider "confluent" {}

resource "confluent_kafka_topic" "this" {
  for_each = var.kafka_topics

  topic_name       = each.key
  partitions_count = each.value.partitions
  config           = each.value.config
}
