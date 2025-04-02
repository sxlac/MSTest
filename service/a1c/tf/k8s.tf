provider "kubernetes" {
	config_context = var.k8s_cluster
}

resource "kubernetes_namespace" "a1csvc_k8s_namespace" {
 metadata {
    name = var.k8s_ns

     labels = {
      "field.cattle.io/projectId" = ""
    }

     annotations = {
      "cattle.io/status" = "",
      "field.cattle.io/projectId" = "",
      "lifecycle.cattle.io/create.namespace-auth" = ""
    }
  }

  lifecycle {
    ignore_changes = [
      metadata.0.labels["field.cattle.io/projectId"],
      metadata.0.annotations["cattle.io/status"],
      metadata.0.annotations["field.cattle.io/projectId"],
      metadata.0.annotations["lifecycle.cattle.io/create.namespace-auth"]
    ]
  }

}