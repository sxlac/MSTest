# Grafana and Elastic POC

This project allows engineers to perform CRUD operations on ElasticSearch.

## Setup
This console app requires Config.cs to be updated with 4 values.
The elastic certificate fingerprint is shown on the first run on the container.
The username is elastic by default.
The hostname does not need to be updated from localhost:9200 if using default image settings.
The password can be read from the first run, updated via command line, or preset via environment variable.

## Docker image commands

Though the docker-compose folder is provided, I used the following commands to create the containers because of an inconsistency with docker compose
failing to expose the elastic secrets on first run occasionally. 

To create an elastic docker container:

docker run --name elastic -p 9200:9200 -p 9300:9300  -e "discovery.type=single-node" -e "http.cors.enabled=true" -e "http.cors.allow-origin=http://localhost:3000,http://172.17.0.2:" -e "http.cors.allow-headers=X-Requested-With,X-Auth-Token,Content-Length,Authorization" -e "http.cors.allow-credentials=true" -e "bootstrap.memory_lock=true" -e "ELASTIC_PASSWORD=Welcome1" -e "ES_JAVA_OPTS=-Xms512m -Xmx512m" -t docker.elastic.co/elasticsearch/elasticsearch:8.6.2

To create a grafana docker container:

docker run --name grafana -p 3000:3000 -t grafana/grafana-oss:9.4.0-beta1

### Grafana Configuration

Navigate to your local instance (default at localhost:3000) then to configuration and date sources. Add an elastic data source.
If elastic cannot be reached via hostname, you can point to its ip on the docker bridge network. This information can be found in the logs or
by running a docker inspect bridge command.

Set use basic authentication to true and provide the username and password.
Set skip TLS verifcation.

Update the index field to POC (this can be changed in ElasticContext.cs).
Specify the timestamp field as date.

### Execution
Update program.cs to use the ElasticContext public methods to interact with the database and/or generate seed data.