package helpers.newRelic;

import java.io.IOException;
import java.util.Optional;

import org.apache.http.client.methods.HttpPost;
import org.apache.http.entity.StringEntity;
import org.apache.http.impl.client.CloseableHttpClient;
import org.apache.http.impl.client.HttpClients;
import org.apache.http.util.EntityUtils;
import org.json.JSONObject;
import org.apache.http.HttpResponse;

public class CommonNewRelicHelpers {

    private static final String uri = Optional.ofNullable(System.getenv("NEW_RELIC_URI")).orElse(System.getProperty("NEW_RELIC_URI"));
    private static final String id = Optional.ofNullable(System.getenv("NEW_RELIC_ID")).orElse(System.getProperty("NEW_RELIC_ID"));
    private static final String key = Optional.ofNullable(System.getenv("NEW_RELIC_KEY")).orElse(System.getProperty("NEW_RELIC_KEY"));    
    
    /*
     * Check if custom event exists
     */
    public static boolean checkCustomEvent(String event, String property, String value) {

        try (CloseableHttpClient httpClient = HttpClients.createDefault()) {

            HttpPost request = new HttpPost(uri);
            request.setHeader("Content-Type", "application/json");
            request.setHeader("API-Key", key);
            
            StringEntity nrql = new StringEntity("{\"query\":\"{ actor {account(id: " + id + "){nrql( query: \\\"SELECT * FROM " + event + " WHERE " + property + " = " + value + " LIMIT MAX\\\" ) { results}}}}\", \"variables\":\"\"}");

            request.setEntity(nrql);

            HttpResponse response = httpClient.execute(request);

            String result = EntityUtils.toString(response.getEntity());

            if(result.contains(value)){
                return true;
            }
        } catch(IOException e){
            e.printStackTrace();
        }
        return false;
    }  

    /*
     * Gets a single custom event
     */
    public static JSONObject getCustomEvent(String event, String property, String value) {

        try (CloseableHttpClient httpClient = HttpClients.createDefault()) {

            HttpPost request = new HttpPost(uri);
            request.setHeader("Content-Type", "application/json");
            request.setHeader("API-Key", key);
            
            StringEntity nrql = new StringEntity("{\"query\":\"{ actor {account(id: " + id + "){nrql( query: \\\"SELECT * FROM " + event + " WHERE " + property + " = " + value + " LIMIT MAX\\\" ) { results}}}}\", \"variables\":\"\"}");

            request.setEntity(nrql);

            HttpResponse response = httpClient.execute(request);

            return new JSONObject(EntityUtils.toString(response.getEntity()));
        } catch(IOException e){
            e.printStackTrace();
        }
        return new JSONObject();
    } 

    /*
     * Gets a list custom events
     */
    public static JSONObject getCustomEvents(String event) {

        try (CloseableHttpClient httpClient = HttpClients.createDefault()) {

            HttpPost request = new HttpPost(uri);
            request.setHeader("Content-Type", "application/json");
            request.setHeader("API-Key", key);
            
            StringEntity nrql = new StringEntity("{\"query\":\"{ actor {account(id: " + id + "){nrql( query: \\\"SELECT * FROM " + event + " LIMIT MAX\\\" ) { results}}}}\", \"variables\":\"\"}");

            request.setEntity(nrql);

            HttpResponse response = httpClient.execute(request);

            return new JSONObject(EntityUtils.toString(response.getEntity()));
        } catch(IOException e){
            e.printStackTrace();
        }
        return new JSONObject();
    }   
}
