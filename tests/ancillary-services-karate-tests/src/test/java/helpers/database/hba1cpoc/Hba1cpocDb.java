package helpers.database.hba1cpoc;

import helpers.database.CommonDbHelpers;
import java.sql.Connection;
import java.sql.PreparedStatement;
import java.sql.SQLException;
import java.util.List;
import java.util.Optional;
import net.minidev.json.JSONArray;

public class Hba1cpocDb {
    private static CommonDbHelpers dbHelpers = new CommonDbHelpers();

    // Can be set either using Kafka (which become environment variables) or from
    // Karate (which writes to system properties)
    private final String connectionUrl = Optional.ofNullable(System.getenv("HBA1CPOC_DB_URL"))
            .orElse(System.getProperty("HBA1CPOC_DB_URL"));
    private final String user = Optional.ofNullable(System.getenv("HBA1CPOC_DB_USERNAME"))
            .orElse(System.getProperty("HBA1CPOC_DB_USERNAME"));
    private final String password = Optional.ofNullable(System.getenv("HBA1CPOC_DB_PASSWORD"))
            .orElse(System.getProperty("HBA1CPOC_DB_PASSWORD"));

    public JSONArray getResultsByEvaluationId(int evaluationId) throws SQLException, Exception {
        String query = "SELECT * FROM \"HBA1CPOC\" WHERE \"EvaluationId\" = ?";

        try (Connection connection = dbHelpers.getConnection(connectionUrl, user, password)) {
            System.out.println(String.format("Searching for EvaluationId %s in the HBA1CPOC table.", evaluationId));

            PreparedStatement statement = connection.prepareStatement(query);
            statement.setInt(1, evaluationId);
            JSONArray results = dbHelpers.getQueryResultsWithRetry(statement, 48, 2500);
            return results;
        } catch (Exception ex) {
            //throw new Exception(String.format("Did not find EvaluationId %s in the HBA1CPOC table!", evaluationId));
            System.out.println(String.format("No EvaluationId %s found in the HBA1CPOC table.", evaluationId));
            return new JSONArray();
        }
    }

    public JSONArray getNotPerformedResultsByEvaluationId(int evaluationId) throws SQLException, Exception {
        String query = "SELECT * FROM \"HBA1CPOCNotPerformed\" np " +
                "INNER JOIN \"HBA1CPOC\" p ON p.\"HBA1CPOCId\" = np.\"HBA1CPOCId\" " +
                "WHERE p.\"EvaluationId\" = ?";

        try (Connection connection = dbHelpers.getConnection(connectionUrl, user, password)) {
            System.out.println(String.format("Searching for EvaluationId %s in the HBA1CPOCNotPerformed table", evaluationId));

            PreparedStatement statement = connection.prepareStatement(query);
            statement.setInt(1, evaluationId);
            return dbHelpers.getQueryResultsWithRetry(statement, 24, 2500);
        } catch (Exception ex) {
            throw new Exception(String.format("Did not find EvaluationId %s in the HBA1CPOCNotPerformed table!", evaluationId));
        }
    }

    public JSONArray getBillingResultsByEvaluationId(int evaluationId) throws SQLException, Exception {
        String query = "SELECT * FROM \"HBA1CPOCRCMBilling\" hc " +
                "INNER JOIN \"HBA1CPOC\" p ON p.\"HBA1CPOCId\" = hc.\"HBA1CPOCId\" " +
                "WHERE p.\"EvaluationId\" = ?";

        try (Connection connection = dbHelpers.getConnection(connectionUrl, user, password)) {
            System.out.println(String.format("Searching for EvaluationId %s in the HBA1CPOCBilling table", evaluationId));

            PreparedStatement statement = connection.prepareStatement(query);
            statement.setInt(1, evaluationId);
            return dbHelpers.getQueryResultsWithRetry(statement, 24, 2500);
        } catch (Exception ex) {
            throw new Exception(String.format("Did not find EvaluationId %s in the HBA1CPOCBilling table!", evaluationId));
        }
    }


    
    public JSONArray getProviderPayResultsWithEvalId(int evaluationId) throws Exception {
        String query = "SELECT * FROM \"ProviderPay\" pp " +
                "INNER JOIN \"HBA1CPOC\" p ON p.\"HBA1CPOCId\" = pp.\"HBA1CPOCId\" " +
                "WHERE p.\"EvaluationId\" = ?";

        try (Connection connection = dbHelpers.getConnection(connectionUrl, user, password)) {
            System.out.println(String.format("Searching for EvaluationId %s in the ProviderPay table.", evaluationId));

            PreparedStatement statement = connection.prepareStatement(query);
            statement.setInt(1, evaluationId);
            return dbHelpers.getQueryResultsWithRetry(statement, 10, 1000);
        } catch (Exception ex) {
            System.out.println(String.format("Did not find EvaluationId %s in the ProviderPay table!", evaluationId));
            return new JSONArray();
        }
    }

    public JSONArray getExamStatusByEvaluationId(int evaluationId) throws SQLException, Exception {
        String query = "SELECT * FROM \"HBA1CPOCStatus\" hs " +
                "INNER JOIN \"HBA1CPOC\" p ON p.\"HBA1CPOCId\" = hs.\"HBA1CPOCId\" " +
                "WHERE p.\"EvaluationId\" = ?";

        try (Connection connection = dbHelpers.getConnection(connectionUrl, user, password)) {
            System.out.println(String.format("Searching for EvaluationId %s in the HBA1CPOCStatus table", evaluationId));

            PreparedStatement statement = connection.prepareStatement(query);
            statement.setInt(1, evaluationId);
            return dbHelpers.getQueryResultsWithRetry(statement, 24, 2500);
        } catch (Exception ex) {
            throw new Exception(String.format("Did not find EvaluationId %s in the HBA1CPOCStatus table!", evaluationId));
        }
    }

        /**
     * Gets a list of Exam Details from Exam table based on EvaluationId and ExamStatusName
     * @param evaluationId, examStatus
     * @return Exam Details queried by ExamStatusName
     * @throws Exception
     * @throws SQLException
     */
    public JSONArray queryExamWithStatusList(int evaluationId,List<String> status) throws SQLException, Exception {
        
        JSONArray results = new JSONArray();
        
        for (String statusStr:status){

                String querystr = "SELECT esc.* FROM \"HBA1CPOC\" e INNER JOIN \n" + //
                        "\"HBA1CPOCStatus\" es ON e.\"HBA1CPOCId\" = es.\"HBA1CPOCId\" \n" + //
                        "                INNER JOIN \"HBA1CPOCStatusCode\" esc \n" + //
                        "                    ON es.\"HBA1CPOCStatusCodeId\" = esc.\"HBA1CPOCStatusCodeId\" \n" + //
                        "                WHERE \"EvaluationId\" = "+evaluationId+" AND  esc.\"StatusCode\" = '"+statusStr+"'";
                        
                System.out.println(String.format("query status code:"+querystr));    
            try (Connection connection = dbHelpers.getConnection(connectionUrl, user, password)) {
                System.out.println(String.format("Searching for EvaluationId %s with status %s in the HBA1CPOC table.", evaluationId,statusStr));
                PreparedStatement statement = connection.prepareStatement(querystr);
                JSONArray records = dbHelpers.getQueryResultsWithRetry(statement, 48, 2500);
                for (Object record:records){results.add(record);}
            } catch (Exception ex) {
                System.out.println(String.format("Could not find HBA1CPOC Exam with EvaluationId %s and HBA1CPOCStatus %s in the HBA1CPOC table.\n,", evaluationId,statusStr,ex));
            }
        }
        return results;     
    }
}