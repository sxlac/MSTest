#Uncomment the following section when executing towards a remote environment.
#This requires credentials be added to your .terraformrc (see readme)
#In general, you can have one workspace for your all your application resources per env
#(db, Kafka, Okta etc) as well with the exception of resources that are
#shared across all pre-prod instances, which maintain a separate state.  But separating
#each type of resource into its own tf file within the folder will share the terraform
#workspace while maintaining 'cleanness':


terraform {
  backend "remote" {
    organization = "signify-health"
    workspaces {
      prefix = "a1csvc-shared-"
    }
  }
}

