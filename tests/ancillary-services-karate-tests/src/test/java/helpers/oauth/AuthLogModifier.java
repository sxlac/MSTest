package helpers.oauth;
import com.intuit.karate.http.HttpLogModifier;
import org.json.JSONObject;
import org.jsoup.Jsoup;
import org.jsoup.nodes.*;

public class AuthLogModifier implements HttpLogModifier {
    
    public static final HttpLogModifier INSTANCE = new AuthLogModifier();

    @Override
    public boolean enableForUri(String uri) {
        return uri.contains("https://login")||uri.contains("signifyhealth.com");
    }

    @Override
    public String uri(String uri) {
        if (uri.contains("sessionToken=")){
            return uri.replace(uri.split("&")[1].split("=")[1], "***")
                    .replace(uri.split("&")[6].split("=")[1],"***"); 
        }
        return uri;
    }        
    
    @Override
    public String header(String header, String value) {
        if (header.toLowerCase().contains("xss-protection")) {
            return "***";
        }
        if (header.toLowerCase().contains("authorization")) {
            return value.replace(value, "Bearer ***");
        }
        return value;
    }

    @Override
    public String request(String uri, String request) {
        return request;
    }

    @Override
    public String response(String uri, String response) {
        if (response.contains("sessionToken")){
            JSONObject responseJson = new JSONObject(response);
            responseJson.put("sessionToken", "***");
            return responseJson.toString();
        }
        if (response.contains("access_token")){
            Document responseDoc = Jsoup.parse(response);
            return response.replace(responseDoc.select("input[name=access_token]").first().attributes().get("value"), "***")
            .replace(responseDoc.select("input[name=id_token]").first().attributes().get("value"), "***");

        }
        return response;
    }

}
