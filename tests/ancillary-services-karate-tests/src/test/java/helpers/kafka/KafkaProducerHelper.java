package helpers.kafka;

import java.util.ArrayList;
import java.util.List;
import java.util.Optional;
import java.util.Properties;
import java.util.concurrent.ExecutionException;

import org.apache.kafka.clients.CommonClientConfigs;
import org.apache.kafka.clients.producer.KafkaProducer;
import org.apache.kafka.clients.producer.ProducerConfig;
import org.apache.kafka.clients.producer.ProducerRecord;
import org.apache.kafka.clients.producer.RecordMetadata;
import org.apache.kafka.common.config.SaslConfigs;
import org.apache.kafka.common.header.Header;
import org.apache.kafka.common.header.internals.RecordHeader;
import org.apache.kafka.common.serialization.StringSerializer;
import org.json.JSONObject;

public class KafkaProducerHelper {

    private static final String bootstrapServers = Optional.ofNullable(System.getenv("KAFKA_BOOTSTRAP_SERVERS")).orElse(System.getProperty("KAFKA_BOOTSTRAP_SERVERS"));
    private static final String username = Optional.ofNullable(System.getenv("KAFKA_PUB_USERNAME")).orElse(System.getProperty("KAFKA_PUB_USERNAME"));
    private static final String password = Optional.ofNullable(System.getenv("KAFKA_PUB_PASSWORD")).orElse(System.getProperty("KAFKA_PUB_PASSWORD"));
    private static final String homeAccessUsername = Optional.ofNullable(System.getenv("KAFKA_HOME_ACCESS_PUB_USERNAME")).orElse(System.getProperty("KAFKA_HOME_ACCESS_PUB_USERNAME"));
    private static final String homeAccessPassword = Optional.ofNullable(System.getenv("KAFKA_HOME_ACCESS_PUB_PASSWORD")).orElse(System.getProperty("KAFKA_HOME_ACCESS_PUB_PASSWORD"));

    /**
     * Sends a Kafka message to a topic synchronously.
     * 
     * @param topic
     * @param key
     * @param headers
     * @param value
     * @throws InterruptedException
     * @throws ExecutionException 
     * */
    public static void send(String topic, String key, String headers, String value) throws InterruptedException, ExecutionException {
        String jsonValue = new JSONObject(value).toString();
       
        JSONObject jsonObject = new JSONObject(headers);
        List<Header> kafkaHeaders = buildHeaders(jsonObject);

        System.out.println(String.format("Sending Kafka message topic=[%s] headers=[%s] jsonValue=[\n%s\n]", topic, headers, new JSONObject(jsonValue).toString(4)));

        try (KafkaProducer<String, String> producer = new KafkaProducer<>(buildProperties(topic))) {
            ProducerRecord<String, String> producerRecord = new ProducerRecord<String, String>(topic, key, jsonValue);
            for (Header kafkaHeader : kafkaHeaders) {
                producerRecord.headers().add(kafkaHeader);
            }

            RecordMetadata recordMetadata = producer.send(producerRecord).get();

            System.out.println(String.format("Sent Kafka message topic=[%s], partition=[%d], offset=[%d] headers=[%s] jsonValue=[\n%s\n]", 
                recordMetadata.topic(), 
                recordMetadata.partition(), 
                recordMetadata.offset(), 
                headers, 
                new JSONObject(jsonValue).toString(4))
            );

            producer.flush();
        }
    }

    private static List<Header> buildHeaders(JSONObject jsonObject) {
        List<Header> headers = new ArrayList<>();

        for(String key : jsonObject.keySet()){
            headers.add(new RecordHeader(key, jsonObject.get(key).toString().getBytes()));
        }
        return headers;
    }

     private static Properties buildProperties(String topic) {
        Properties properties = new Properties();
        properties.setProperty(ProducerConfig.BOOTSTRAP_SERVERS_CONFIG, bootstrapServers);
        properties.setProperty(ProducerConfig.KEY_SERIALIZER_CLASS_CONFIG, StringSerializer.class.getName());
        properties.setProperty(ProducerConfig.VALUE_SERIALIZER_CLASS_CONFIG, StringSerializer.class.getName());
        if(!bootstrapServers.contains("localhost"))
        {
            properties.put(CommonClientConfigs.SECURITY_PROTOCOL_CONFIG, "SASL_SSL");
            properties.put(SaslConfigs.SASL_MECHANISM, "PLAIN");

            if(topic.equals("homeaccess_labresults") || topic.equals("egfr_lab_results")|| topic.equals("dps_labresult_uacr")|| topic.equals("dps_labresult_egfr")){
                properties.put(SaslConfigs.SASL_JAAS_CONFIG, "org.apache.kafka.common.security.plain.PlainLoginModule required username=\"" + homeAccessUsername + "\" password=\"" + homeAccessPassword + "\";");
            }else
            {
                properties.put(SaslConfigs.SASL_JAAS_CONFIG, "org.apache.kafka.common.security.plain.PlainLoginModule required username=\"" + username + "\" password=\"" + password + "\";");
            }
        }
        return properties;
    }
}

