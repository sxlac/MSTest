package helpers.database.oms;

import helpers.database.CommonDbHelpers;
import java.sql.Connection;
import java.sql.PreparedStatement;
import java.sql.SQLException;
import java.util.Optional;
import net.minidev.json.JSONArray;

public class OmsDb {
    private static CommonDbHelpers dbHelpers = new CommonDbHelpers();

    // Can be set either using Kafka (which become environment variables) or from Karate (which writes to system properties)
    private static final String connectionUrl = Optional.ofNullable(System.getenv("OMS_DB_URL")).orElse(System.getProperty("OMS_DB_URL"));
    private static final String user = Optional.ofNullable(System.getenv("OMS_DB_USERNAME")).orElse(System.getProperty("OMS_DB_USERNAME"));
    private static final String password = Optional.ofNullable(System.getenv("OMS_DB_PASSWORD")).orElse(System.getProperty("OMS_DB_PASSWORD"));
  
    public JSONArray getResultsByEvaluationId(int evaluationId) throws SQLException, Exception {
        String query = "SELECT * FROM \"Order\" WHERE \"EvaluationId\" = ? ORDER BY \"ProductCodeName\"";

        try (Connection connection = dbHelpers.getConnection(connectionUrl, user, password)) {
            System.out.println(String.format("Searching for EvaluationId %s in the Order table.", evaluationId));

            PreparedStatement statement = connection.prepareStatement(query);
            statement.setInt(1, evaluationId);
            JSONArray results = dbHelpers.getQueryResultsWithRetry(statement, 48, 2500);
            return results;
        } catch (Exception ex) {
            throw new Exception(String.format("Did not find EvaluationId %s in the Order table! \n %s", evaluationId,ex));
        }
    }

}



