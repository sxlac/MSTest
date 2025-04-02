package helpers.kafka;

import org.apache.kafka.clients.CommonClientConfigs;
import org.apache.kafka.clients.consumer.*;
import org.apache.kafka.common.TopicPartition;
import org.apache.kafka.common.config.SaslConfigs;
import org.apache.kafka.common.header.Header;
import org.json.JSONObject;
import org.json.JSONTokener;
import org.json.JSONArray;

import java.nio.charset.StandardCharsets;
import java.time.Duration;
import java.util.*;
import java.util.concurrent.atomic.AtomicBoolean;
import java.util.stream.Collectors;

public class KafkaConsumerHelper {

    private static final String bootstrapServers = Optional.ofNullable(System.getenv("KAFKA_BOOTSTRAP_SERVERS"))
            .orElse(System.getProperty("KAFKA_BOOTSTRAP_SERVERS"));
    private static final String username = Optional.ofNullable(System.getenv("KAFKA_USERNAME"))
            .orElse(System.getProperty("KAFKA_USERNAME"));
    private static final String password = Optional.ofNullable(System.getenv("KAFKA_PASSWORD"))
            .orElse(System.getProperty("KAFKA_PASSWORD"));
    private static final String groupID = Optional.ofNullable(System.getenv("KAFKA_GROUP_ID"))
            .orElse(System.getProperty("KAFKA_GROUP_ID"));

    private static AtomicBoolean consume = new AtomicBoolean(true);
    private static List<ConsumerRecord<String, String>> consumedMessages = new ArrayList<ConsumerRecord<String, String>>();
    private static Thread consumingThread = null;
    private static KafkaConsumerHelper instance = null;
    private static List<String> topics = Arrays.asList("evaluation", "dee_results", "dee_status", "PAD_Results",
            "PAD_Status", "spirometry_status", "spirometry_result",
            "FOBT_Results", "FOBT_Status", "A1CPOC_Results", "A1CPOC_Status", "ckd_results", "ckd_status",
            "providerpay_internal", "egfr_lab_results", "egfr_status", "egfr_results","uacr_status","dps_oms_order", "uacr_results",
            "dps_labresult_egfr", "dps_labresult_uacr");


    /**
     * Singleton pattern. Private constructor can only be invoked by synchronized
     * static Instance() method.
     */
    private KafkaConsumerHelper() {
        readMessages();
    }

    public synchronized static KafkaConsumerHelper Instance() {
        if (instance == null) {
            instance = new KafkaConsumerHelper();
        }

        return instance;
    }

    /**
     * There is a bug in the method that returns early
     * if there are more events with the same key published to the same topic.
     * Retaining this method for now as it is being used widely.
     *
     * @deprecated use {@link #getKafkaMessageByTopicAndKeyAndHeader()} instead.
     */
    @Deprecated
    public static JSONObject getMessageByTopicAndKeyAndHeader(String topic, String key, String header, int retryCount,
            int sleepMillis) {
        System.out.printf("getMessageByTopicAndKeyAndHeader topic = %s, key = %s, header = %s\n", topic, key, header);

        for (int i = 0; i < retryCount; i++) {

            List<ConsumerRecord<String, String>> records = consumedMessages.stream()
                    .filter(r -> r.key().equalsIgnoreCase(key) && r.topic().equalsIgnoreCase(topic))
                    .collect(Collectors.toList());

            for (ConsumerRecord<String, String> consumerRecord : records) {
                System.out.printf("consumerRecord key = %s, value = %s\n", consumerRecord.key(),
                        consumerRecord.value());
            }
            if (!records.isEmpty()) {
                for (ConsumerRecord<String, String> record : records) {
                    for (Header head : record.headers()) {
                        String headerValue = new String(head.value(), StandardCharsets.UTF_8);
                        System.out.printf("header found = %s\n", headerValue);
                        if (headerValue.equalsIgnoreCase(header)) {
                            JSONTokener tokener = new JSONTokener(record.value());
                            return new JSONObject(tokener);
                        }
                    }
                }
                return new JSONObject();
            }
            try {
                Thread.sleep(sleepMillis);
            } catch (InterruptedException e) {
            }
        }
        return new JSONObject();
    }

    /**
     * Find a kafka message within a topic using a key and a header name
     * 
     * @param topic
     * @param key
     * @param header
     * @param retryCount  Times to retry
     * @param sleepMillis Time to sleep between retries
     * @return
     */
    public static JSONObject getKafkaMessageByTopicAndKeyAndHeader(String topic, String key, String header,
            int retryCount,
            int sleepMillis) {
        System.out.printf("getKafkaMessageByTopicAndKeyAndHeader topic = %s, key = %s, header = %s\n", topic, key, header);

        for (int i = 0; i < retryCount; i++) {

            List<ConsumerRecord<String, String>> records = consumedMessages.stream()
                    .filter(r -> r.key().equalsIgnoreCase(key) && r.topic().equalsIgnoreCase(topic))
                    .collect(Collectors.toList());

            for (ConsumerRecord<String, String> consumerRecord : records) {
                System.out.printf("consumerRecord key = %s, value = %s\n", consumerRecord.key(),
                        consumerRecord.value());
            }
            if (!records.isEmpty()) {
                for (ConsumerRecord<String, String> record : records) {
                    for (Header head : record.headers()) {
                        String headerValue = new String(head.value(), StandardCharsets.UTF_8);
                        System.out.printf("header found = %s\n", headerValue);
                        if (headerValue.equalsIgnoreCase(header)) {
                            JSONTokener tokener = new JSONTokener(record.value());
                            return new JSONObject(tokener);
                        }
                    }
                }
            }
            try {
                Thread.sleep(sleepMillis);
            } catch (InterruptedException e) {
            }
        }
        return new JSONObject();
    }

    public static String getHeadersByTopicAndKey(String topic, String key, int retryCount, int sleepMillis) {
        String headers = "";
        System.out.printf("getHeadersByTopicAndKey topic = %s, key = %s\n", topic, key);

        for (int i = 0; i < retryCount; i++) {

            List<ConsumerRecord<String, String>> records = consumedMessages.stream()
                    .filter(r -> r.key().equalsIgnoreCase(key) && r.topic().equalsIgnoreCase(topic))
                    .collect(Collectors.toList());

            if (!records.isEmpty()) {
                for (ConsumerRecord<String, String> record : records) {
                    for (Header head : record.headers()) {
                        headers += new String(head.value(), StandardCharsets.UTF_8) + ",";
                    }
                }
                System.out.printf("headers found = %s\n", headers);

                return headers;
            }
            try {
                Thread.sleep(sleepMillis);
            } catch (InterruptedException e) {
            }
        }
        return headers;
    }
    
    public static List<String> getHeadersListByTopicAndKey(String topic, String key, int retryCount, int sleepMillis) {
        List<String> headers = new ArrayList<String>();
        System.out.printf("getHeadersJsonArrayByTopicAndKey topic = %s, key = %s\n", topic, key);

        for (int i = 0; i < retryCount; i++) {

            List<ConsumerRecord<String, String>> records = consumedMessages.stream()
                    .filter(r -> r.key().equalsIgnoreCase(key) && r.topic().equalsIgnoreCase(topic))
                    .collect(Collectors.toList());

            if (!records.isEmpty()) {
                for (ConsumerRecord<String, String> record : records) {
                    for (Header head : record.headers()) {
                        headers.add(new String(head.value(), StandardCharsets.UTF_8));
                    }
                }
                System.out.printf("headers found = %s\n", headers);

                return headers;
            }
            try {
                Thread.sleep(sleepMillis);
            } catch (InterruptedException e) {
            }
        }
        return headers;
    }

    public static String getEventsByTopicAndKey(String topic, String key, int retryCount, int sleepMillis) {
        String events = "";
        System.out.printf("getEventsByTopicAndKey topic = %s, key = %s\n", topic, key);

        for (int i = 0; i < retryCount; i++) {

            List<ConsumerRecord<String, String>> records = consumedMessages.stream()
                    .filter(r -> r.key().equalsIgnoreCase(key) && r.topic().equalsIgnoreCase(topic))
                    .collect(Collectors.toList());

            if (!records.isEmpty()) {
                for (ConsumerRecord<String, String> record : records) {
                    events += record.value();
                    for (Header head : record.headers()) {
                        events += new String(head.value(), StandardCharsets.UTF_8) + ",";
                    }
                }
                System.out.printf("events found = %s\n", events);

                return events;
            }
            try {
                Thread.sleep(sleepMillis);
            } catch (InterruptedException e) {
            }
        }
        return events;
    }

    public static String getMessageByTopicAndHeaderAndAChildField(String topic, String header, String childFeld,
            int retryCount, int sleepMillis) {
        String events = "";
        System.out.printf("getMessageByTopicAndHeaderAndAChildField topic = %s, header = %s, childFeld = %s\n", topic,
                header, childFeld);

        for (int i = 0; i < retryCount; i++) {

            List<ConsumerRecord<String, String>> records = consumedMessages.stream()
                    .filter(r -> r.topic().equalsIgnoreCase(topic))
                    .collect(Collectors.toList());

            if (!records.isEmpty()) {
                for (ConsumerRecord<String, String> record : records) {
                    for (Header head : record.headers()) {
                        String headerValue = new String(head.value(), StandardCharsets.UTF_8);
                        System.out.printf("header details = %s\n", headerValue);
                        if (headerValue.equalsIgnoreCase(header) && record.value().contains(childFeld)) {
                            events += record.value();
                            System.out.printf("event found with requested header and childField = %s\n", events);
                            return events;
                        }
                    }
                }
            }
            try {
                Thread.sleep(sleepMillis);
            } catch (InterruptedException e) {
                System.out.printf(e.getMessage());
            }
        }
        return events;
    }

    public static void StopConsuming() throws InterruptedException {
        consume.set(false);
        consumingThread.join();
    }

    public void readMessages() throws SecurityException {
        System.out.println("Starting KafkaConsumerHelper");

        Properties kafkaProps = new Properties();
        kafkaProps.put(ConsumerConfig.BOOTSTRAP_SERVERS_CONFIG, bootstrapServers);
        kafkaProps.put(ConsumerConfig.GROUP_ID_CONFIG, groupID + "." + Math.floor(Math.random() * 1000000));
        kafkaProps.put(ConsumerConfig.KEY_DESERIALIZER_CLASS_CONFIG,
                "org.apache.kafka.common.serialization.StringDeserializer");
        kafkaProps.put(ConsumerConfig.VALUE_DESERIALIZER_CLASS_CONFIG,
                "org.apache.kafka.common.serialization.StringDeserializer");
        kafkaProps.put(ConsumerConfig.AUTO_OFFSET_RESET_CONFIG, "latest");

        if (!bootstrapServers.contains("localhost")) {
            kafkaProps.put(CommonClientConfigs.SECURITY_PROTOCOL_CONFIG, "SASL_SSL");
            kafkaProps.put(SaslConfigs.SASL_MECHANISM, "PLAIN");
            kafkaProps.put(SaslConfigs.SASL_JAAS_CONFIG,
                    "org.apache.kafka.common.security.plain.PlainLoginModule required username=\"" + username
                            + "\" password=\"" + password + "\";");
        }

        consumingThread = new Thread(() -> {
            try (KafkaConsumer<String, String> consumer = new KafkaConsumer<String, String>(kafkaProps)) {
                consumer.subscribe(topics, new ConsumerRebalanceListener() {
                    @Override
                    public void onPartitionsRevoked(Collection<TopicPartition> collection) {
                        collection.forEach(tt -> System.out.println("partition : " + tt.partition() + " revoked"));
                    }

                    @Override
                    public void onPartitionsAssigned(Collection<TopicPartition> collection) {
                        collection.forEach(tt -> System.out.println("partition : " + tt.partition() + " assigned"));
                    }
                });

                while (consume.get()) {
                    ConsumerRecords<String, String> records = consumer.poll(Duration.ofMillis(500));
                    for (ConsumerRecord<String, String> record : records) {
                        consumedMessages.add(record);
                        System.out.printf("consumed topic = %s, partition = %d, offset = %d, key = %s, message = %s\n",
                                record.topic(), record.partition(), record.offset(), record.key(), record.value());
                    }
                }
            }
        });
        consumingThread.start();
    }

    public static JSONObject getEventByTopicAndKeyAndHeaderAndIndex(String topic, String key, String header, int index,int retryCount, int sleepMillis) {	
        System.out.printf("getEventByTopicAndKeyAndHeaderAndIndex topic = %s, key = %s, header = %s\n", topic, key, header);	
        JSONArray eventArray = new JSONArray();

        for (int i = 0; i < retryCount; i++) {	
            List<ConsumerRecord<String, String>> records = consumedMessages.stream()	
                .filter(r -> r.key().equalsIgnoreCase(key) && r.topic().equalsIgnoreCase(topic))	
                    .collect(Collectors.toList());	
            for (ConsumerRecord<String, String> record : records) {	
                for (Header head : record.headers()) {	
                    String headerValue = new String(head.value(), StandardCharsets.UTF_8);	
                    System.out.printf("header found = %s\n", headerValue);	
                    if (headerValue.equalsIgnoreCase(header)) {	
                        JSONTokener tokener = new JSONTokener(record.value());	
                        eventArray.put(new JSONObject(tokener));	
                    }	
                }	
            }
            if (!eventArray.isEmpty()) {	
                JSONObject event = eventArray.getJSONObject(index);	
                return event;	
            }	
            try {	
                Thread.sleep(sleepMillis);	
            }catch (InterruptedException e) {	
            }	
        }	
    return new JSONObject();   
    }
}
