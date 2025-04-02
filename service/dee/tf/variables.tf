variable "okta_app_name" {
  description = "The Application's display name, in Okta"
  type        = string
}

variable "kafka_retention_ms" {
  description = <<-EOF
    Maximum time we retain a log before we will discard old log segments, in milliseconds.
    Generally set to a week or so on preprod and -1 on prod to retain forever.
  EOF
  type        = number
}

variable "kafka_topics" {
  description = "A map of Kafka topics and their underlying configuration"
  type = map(object({
    partitions = optional(number, 3)
  }))
  default = {
    "dee_results" = {
      paritions = 3
    }
    "dee_status" = {
      partitions = 3
    }
  }
}
