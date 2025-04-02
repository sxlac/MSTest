package helpers.database.fobt;

import helpers.database.CommonDbHelpers;
import java.sql.Connection;
import java.sql.PreparedStatement;
import java.sql.SQLException;
import java.util.List;
import java.util.Optional;
import net.minidev.json.JSONArray;

public class FobtDb {
    private static CommonDbHelpers dbHelpers = new CommonDbHelpers();

    // Can be set either using Kafka (which become environment variables) or from
    // Karate (which writes to system properties)
    private static final String connectionUrl = Optional.ofNullable(System.getenv("FOBT_DB_URL"))
            .orElse(System.getProperty("FOBT_DB_URL"));
    private static final String user = Optional.ofNullable(System.getenv("FOBT_DB_USERNAME"))
            .orElse(System.getProperty("FOBT_DB_USERNAME"));
    private static final String password = Optional.ofNullable(System.getenv("FOBT_DB_PASSWORD"))
            .orElse(System.getProperty("FOBT_DB_PASSWORD"));

    public JSONArray getResultsByEvaluationId(int evaluationId) throws SQLException, Exception {
        String query = "SELECT * FROM \"FOBT\" WHERE \"EvaluationId\" = ?";

        try (Connection connection = dbHelpers.getConnection(connectionUrl, user, password)) {
            System.out.println(String.format("Searching for EvaluationId %s in the FOBT table.", evaluationId));

            PreparedStatement statement = connection.prepareStatement(query);
            statement.setInt(1, evaluationId);
            JSONArray results = dbHelpers.getQueryResultsWithRetry(statement, 50, 4000);
            return results;
        } catch (Exception ex) {
            throw new Exception(String.format("Did not find EvaluationId %s in the FOBT table!", evaluationId));
        }
    }

    public JSONArray getBarcodeHistoryResultByFOBTId(int FOBTId) throws SQLException, Exception {
        String query = "SELECT * FROM \"FOBTBarcodeHistory\" WHERE \"FOBTId\" = ?";

        try (Connection connection = dbHelpers.getConnection(connectionUrl, user, password)) {
            System.out.println(String.format("Searching for FOBTId %s in the FOBTBarcodeHistory table.", FOBTId));

            PreparedStatement statement = connection.prepareStatement(query);
            statement.setInt(1, FOBTId);
            JSONArray results = dbHelpers.getQueryResultsWithRetry(statement, 24, 2500);
            return results;
        } catch (Exception ex) {
            // throw new Exception(String.format("Did not find FOBTId %s in the FOBTBarcodeHistory table!", FOBTId));
            System.out.println(String.format("Did not find BarcodeHistory by FOBTId %s in the BarcodeHistory table!", FOBTId));
            return new JSONArray();
        }
    }

    public JSONArray getPdfToClientResultsWithEvalId(int evaluationId) throws SQLException, Exception {
        String query = "SELECT * FROM \"PDFToClient\" WHERE \"EvaluationId\" = ?";

        try (Connection connection = dbHelpers.getConnection(connectionUrl, user, password)) {
            System.out.println(String.format("Searching for EvaluationId %s in the PDFToClient table.", evaluationId));

            PreparedStatement statement = connection.prepareStatement(query);
            statement.setInt(1, evaluationId);
            JSONArray results = dbHelpers.getQueryResultsWithRetry(statement, 3, 2500);
            return results;
        } catch (Exception ex) {
            throw new Exception(String.format("Did not find EvaluationId %s in the PDFToClient table!", evaluationId));
        }
    }

    public JSONArray getProviderPayByEvalId(int evaluationId) throws SQLException, Exception {
        // TO VERIFY QUERY WHEN FOBT ProviderPay WILL BE ON LOWER ENVIRONMENTS
        String query = "SELECT * FROM \"ProviderPay\" pp " + 
        "INNER JOIN \"FOBT\" FOBT ON fobt.\"FOBTId\" = pp.\"FOBTId\"" +
        "WHERE \"EvaluationId\" = ?";

        try (Connection connection = dbHelpers.getConnection(connectionUrl, user, password)) {
            System.out.println(String.format("Searching for EvaluationId %s in the FOBT ProviderPay table.", evaluationId));

            PreparedStatement statement = connection.prepareStatement(query);
            statement.setInt(1, evaluationId);
            JSONArray results = dbHelpers.getQueryResultsWithRetry(statement, 24, 2500);
            return results;
        } catch (Exception ex) {
            System.out.println(String.format("Did not find EvaluationId %s in the FOBT ProviderPay table!", evaluationId));
            return new JSONArray();
        }
    }

    public JSONArray getNotPerformedResultsByEvaluationId(int evaluationId) throws Exception {
        String query = "SELECT * FROM \"FOBTNotPerformed\" np " +
                "INNER JOIN \"FOBT\" f ON f.\"FOBTId\" = np.\"FOBTId\" " +
                "WHERE f.\"EvaluationId\" = ?";

        try (Connection connection = dbHelpers.getConnection(connectionUrl, user, password)) {
            System.out.println(String.format("Searching for EvaluationId %s in the FOBTNotPerformed table", evaluationId));

            PreparedStatement statement = connection.prepareStatement(query);
            statement.setInt(1, evaluationId);
            return dbHelpers.getQueryResultsWithRetry(statement, 10, 2500);
        } catch (Exception ex) {
            throw new Exception(String.format("Did not find EvaluationId %s in the FOBTNotPerformed table!", evaluationId));
        }
    }

    public JSONArray getLabResultsByEvaluationId(int evaluationId) throws Exception {
        String query = "SELECT * FROM \"LabResults\" lr " +
                "INNER JOIN \"FOBT\" f ON f.\"FOBTId\" = lr.\"FOBTId\" " +
                "WHERE f.\"EvaluationId\" = ?";

        try (Connection connection = dbHelpers.getConnection(connectionUrl, user, password)) {
            System.out.println(String.format("Searching for EvaluationId %s in the LabResults table", evaluationId));

            PreparedStatement statement = connection.prepareStatement(query);
            statement.setInt(1, evaluationId);
            return dbHelpers.getQueryResultsWithRetry(statement, 48, 2500);
        } catch (Exception ex) {
            System.err.println(ex.getMessage());
            throw new Exception(String.format("Did not find EvaluationId %s in the LabResults table!", evaluationId));
        }
    }

    public JSONArray getLabResultsByFOBTId(int fobtId) throws Exception {
        String query = "SELECT * FROM \"LabResults\"" +
                    "WHERE \"FOBTId\" = ?";

        try (Connection connection = dbHelpers.getConnection(connectionUrl, user, password)) {
            System.out.println(String.format("Searching for fobtId %s in the LabResults table", fobtId));

            PreparedStatement statement = connection.prepareStatement(query);
            statement.setInt(1, fobtId);
            return dbHelpers.getQueryResultsWithRetry(statement, 48, 2500);
        } catch (Exception ex) {
            System.err.println(ex.getMessage());
            throw new Exception(String.format("Did not find fobtId %s in the LabResults table!", fobtId));
        }
    }

    public JSONArray getBillingResultsByEvaluationId(int evaluationId) throws Exception {
        String query = "SELECT * FROM \"FOBTBilling\" fb " +
                "INNER JOIN \"FOBT\" f ON f.\"FOBTId\" = fb.\"FOBTId\" " +
                "WHERE f.\"EvaluationId\" = ?";

        try (Connection connection = dbHelpers.getConnection(connectionUrl, user, password)) {
            System.out.println(String.format("Searching for EvaluationId %s in the FOBTBilling table", evaluationId));

            PreparedStatement statement = connection.prepareStatement(query);
            statement.setInt(1, evaluationId);
            return dbHelpers.getQueryResultsWithRetry(statement, 48, 2500);
        } catch (Exception ex) {
            System.err.println(ex.getMessage());
            throw new Exception(String.format("Did not find EvaluationId %s in the FOBTBilling table!", evaluationId));
        }
    }

    public JSONArray getExamStatusByEvaluationId(int evaluationId) throws Exception {
        String query = "SELECT * FROM \"FOBTStatus\" fs " +
                "INNER JOIN \"FOBT\" f ON f.\"FOBTId\" = fs.\"FOBTId\" " +
                "WHERE f.\"EvaluationId\" = ?";

        try (Connection connection = dbHelpers.getConnection(connectionUrl, user, password)) {
            System.out.println(String.format("Searching for EvaluationId %s in the FOBTStatus table", evaluationId));

            PreparedStatement statement = connection.prepareStatement(query);
            statement.setInt(1, evaluationId);
            return dbHelpers.getQueryResultsWithRetry(statement, 48, 2500);
        } catch (Exception ex) {
            System.err.println(ex.getMessage());
            throw new Exception(String.format("Did not find EvaluationId %s in the FOBTStatus table!", evaluationId));
        }
    }

    public JSONArray getExamStatusForStatusCodeByEvaluationId(int evaluationId, int statusCode) throws Exception {
        String query = "SELECT fs.* FROM \"FOBTStatus\" fs " +
                "INNER JOIN \"FOBT\" f ON f.\"FOBTId\" = fs.\"FOBTId\" " +
                "WHERE f.\"EvaluationId\" = ? AND fs.\"FOBTStatusCodeId\" = ?";

        try (Connection connection = dbHelpers.getConnection(connectionUrl, user, password)) {
            System.out.println(String.format("Searching for EvaluationId %s in the FOBTStatus table", evaluationId));

            PreparedStatement statement = connection.prepareStatement(query);
            statement.setInt(1, evaluationId);
            statement.setInt(2, statusCode);
            return dbHelpers.getQueryResultsWithRetry(statement, 48, 2500);
        } catch (Exception ex) {
            System.err.println(ex.getMessage());
            throw new Exception(String.format("Did not find EvaluationId %s in the FOBTStatus table!", evaluationId));
        }
    }

    /**
     * Get all status from FOBTStatusCode table
     * 
     * @return
     * @throws SQLException
     * @throws Exception
     */
    public JSONArray getExamStatusCodes() throws SQLException, Exception {
        String query = "SELECT * FROM \"FOBTStatusCode\" ORDER BY \"FOBTStatusCodeId\" ASC";

        try (Connection connection = dbHelpers.getConnection(connectionUrl, user, password)) {
            System.out.println(String.format("Searching for statuses in FOBTStatusCode table."));

            PreparedStatement statement = connection.prepareStatement(query);
            JSONArray results = dbHelpers.getQueryResultsWithRetry(statement, 3, 2500);
            return results;
        } catch (Exception ex) {
            throw new Exception(String.format("Did not find any entry in FOBTStatusCode table!"));
        }
    }

    /**
     * Verifies the columns in tableName table
     * 
     * @param tableName
     * @return
     * @throws SQLException
     * @throws Exception
     */
    public JSONArray checkTableSchema(String tableName) throws SQLException, Exception {
        String query = "SELECT * FROM information_schema.columns WHERE table_schema = \'public\' AND table_name = ?";
        try (Connection connection = dbHelpers.getConnection(connectionUrl, user, password)) {

            PreparedStatement statement = connection.prepareStatement(query);
            statement.setString(1, tableName);
            JSONArray results = dbHelpers.getQueryResultsWithRetry(statement, 2, 2500);
            return results;
        } catch (Exception ex) {
            throw new Exception(String.format("Did not find %s table!", tableName));
        }
    }

    /**
     * Gets a list of Exam Details from FOBT table based on EvaluationId and FOBTStatusName
     * @param evaluationId, examStatus
     * @return Exam Details queried by StatusCode
     */
    public JSONArray queryExamWithStatusList(int evaluationId,List<String> status) {
        
        JSONArray results = new JSONArray();
        
        for (String statusStr:status){
            String querystr = "SELECT fsc.* FROM \"FOBT\" f " +
                "INNER JOIN \"FOBTStatus\" fs ON f.\"FOBTId\" = fs.\"FOBTId\" " +
                "INNER JOIN \"FOBTStatusCode\" fsc ON fs.\"FOBTStatusCodeId\" = fsc.\"FOBTStatusCodeId\" " +
                "WHERE f.\"EvaluationId\" = ? AND fsc.\"StatusCode\" = ?";
            try (Connection connection = dbHelpers.getConnection(connectionUrl, user, password)) {
                System.out.println(String.format("Searching for EvaluationId %s with status %s in the FOBT table.", evaluationId,statusStr));
                PreparedStatement statement = connection.prepareStatement(querystr);
                statement.setInt(1, evaluationId);
                statement.setString(2,statusStr);
                JSONArray records = dbHelpers.getQueryResultsWithRetry(statement, 48, 2500);
                for (Object record:records){results.add(record);}
            } catch (Exception ex) {
                System.out.println(String.format("Could not find Exam with EvaluationId %s and examStatus %s in the FOBT table.\n", evaluationId,statusStr,ex));
            }
        }
        return results;     
    }

    public JSONArray getExamByEvaluationId(int evaluationId) throws SQLException, Exception {
        return getExamByEvaluationId(evaluationId,50,4000);
    }

    /**
     * Gets a list of Exam Details from FOBT table based on EvaluationId
     * Overloads getExamByEvaluationId(int evaluationId)
     * @param evaluationId
     * @param retryCount
     * @param sleep
     * @return Exam Details queried by EvaluationId
     */
    public JSONArray getExamByEvaluationId(int evaluationId, int retryCount, int sleep) {
        String query = "SELECT * FROM \"FOBT\" WHERE \"EvaluationId\" = ?";
        JSONArray results = new JSONArray();
        try (Connection connection = dbHelpers.getConnection(connectionUrl, user, password)) {
            System.out.println(String.format("Searching for EvaluationId %s in the FOBT table.", evaluationId));

            PreparedStatement statement = connection.prepareStatement(query);
            statement.setInt(1, evaluationId);
            results = dbHelpers.getQueryResultsWithRetry(statement, retryCount, sleep);
        } catch (Exception ex) {
            System.out.println(String.format("Did not find EvaluationId %s in the FOBT table!\n", evaluationId,ex));
        }
        return results;
    }
}