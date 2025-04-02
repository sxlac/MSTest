docker run --name elastic -p 9200:9200 -p 9300:9300  -e "discovery.type=single-node" -e "http.cors.enabled=true" -e "http.cors.allow-origin=http://localhost:3000,http://172.17.0.2:" -e "http.cors.allow-headers=X-Requested-With,X-Auth-Token,Content-Length,Authorization" -e "http.cors.allow-credentials=true" -e "bootstrap.memory_lock=true" -e "ELASTIC_PASSWORD=Welcome1" -e "ES_JAVA_OPTS=-Xms512m -Xmx512m" -t docker.elastic.co/elasticsearch/elasticsearch:8.6.2

docker run --name grafana -p 3000:3000 -t grafana/grafana-oss:9.4.0-beta1

--net elastic

// Create cert
bin/elasticsearch-certutil cert --ca elastic-stack-ca.p12
// Move to local
docker cp elastic:/usr/share/elasticsearch/test /tmp 
