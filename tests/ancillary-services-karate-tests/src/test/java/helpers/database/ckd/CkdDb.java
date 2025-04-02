package helpers.database.ckd;

import helpers.database.CommonDbHelpers;
import java.sql.Connection;
import java.sql.PreparedStatement;
import java.sql.SQLException;
import java.util.Optional;
import net.minidev.json.JSONArray;

public class CkdDb {
    private static CommonDbHelpers dbHelpers = new CommonDbHelpers();

    // Can be set either using Kafka (which become environment variables) or from Karate (which writes to system properties)
    private static final String connectionUrl = Optional.ofNullable(System.getenv("CKD_DB_URL")).orElse(System.getProperty("CKD_DB_URL"));
    private static final String user = Optional.ofNullable(System.getenv("CKD_DB_USERNAME")).orElse(System.getProperty("CKD_DB_USERNAME"));
    private static final String password = Optional.ofNullable(System.getenv("CKD_DB_PASSWORD")).orElse(System.getProperty("CKD_DB_PASSWORD"));

    public JSONArray getResultsByEvaluationId(int evaluationId) throws SQLException, Exception {
        String query = "SELECT * FROM \"CKD\" WHERE \"EvaluationId\" = ?";

        try (Connection connection = dbHelpers.getConnection(connectionUrl, user, password)) {
            System.out.println(String.format("Searching for EvaluationId %s in the CKD table.", evaluationId));

            PreparedStatement statement = connection.prepareStatement(query);
            statement.setInt(1, evaluationId);
            JSONArray results = dbHelpers.getQueryResultsWithRetry(statement, 24, 2500);
            return results;
        } catch (Exception ex) {
            throw new Exception(String.format("Did not find EvaluationId %s in the CKD table!", evaluationId));
        }
    }

    public JSONArray getExamResultByCkdId(int CKDId) throws SQLException, Exception {
        String query = "SELECT * FROM \"ExamResult\" WHERE \"CKDId\" = ?";

        try (Connection connection = dbHelpers.getConnection(connectionUrl, user, password)) {
            System.out.println(String.format("Searching for CKDId %s in the ExamResult table.", CKDId));

            PreparedStatement statement = connection.prepareStatement(query);
            statement.setInt(1, CKDId);
            JSONArray results = dbHelpers.getQueryResultsWithRetry(statement, 24, 2500);
            return results;
        } catch (Exception ex) {
            throw new Exception(String.format("Did not find CKDId %s in the ExamResult table!", CKDId));
        }
    }

    public JSONArray getBillingResultByEvalId(int evaluationId) throws SQLException, Exception {
        String query = "SELECT * FROM \"CKDRCMBilling\" rcm " + 
        "INNER JOIN \"CKD\" ckd ON ckd.\"CKDId\" = rcm.\"CKDId\"" +
        "WHERE \"EvaluationId\" = ?";

        try (Connection connection = dbHelpers.getConnection(connectionUrl, user, password)) {
            System.out.println(String.format("Searching for EvaluationId %s in the CKDRCMBilling table.", evaluationId));

            PreparedStatement statement = connection.prepareStatement(query);
            statement.setInt(1, evaluationId);
            JSONArray results = dbHelpers.getQueryResultsWithRetry(statement, 24, 2500);
            return results;
        } catch (Exception ex) {
            throw new Exception(String.format("Did not find EvaluationId %s in the CKDRCMBilling table!", evaluationId));
        }
    }

    public JSONArray getProviderPayByEvalId(int evaluationId) throws SQLException, Exception {
        String query = "SELECT * FROM \"ProviderPay\" pp " + 
        "INNER JOIN \"CKD\" ckd ON ckd.\"CKDId\" = pp.\"CKDId\"" +
        "WHERE \"EvaluationId\" = ?";

        try (Connection connection = dbHelpers.getConnection(connectionUrl, user, password)) {
            System.out.println(String.format("Searching for EvaluationId %s in the ProviderPay table.", evaluationId));

            PreparedStatement statement = connection.prepareStatement(query);
            statement.setInt(1, evaluationId);
            JSONArray results = dbHelpers.getQueryResultsWithRetry(statement, 24, 2500);
            return results;
        } catch (Exception ex) {
            System.out.println(String.format("Did not find EvaluationId %s in the ProviderPay table!", evaluationId));
            return new JSONArray();
            // throw new Exception(String.format("Did not find EvaluationId %s in the ProviderPay table!", evaluationId));
        }
    }
    
    public JSONArray getNotInRcmByEvalIdInExamNotPerf(int evaluationId) throws SQLException, Exception {
        String query = "SELECT * FROM \"CKDRCMBilling\" rcm " +
        "FULL OUTER JOIN \"CKD\" ckd ON ckd.\"CKDId\" = rcm.\"CKDId\"" +
        "INNER JOIN \"ExamNotPerformed\" notperf ON ckd.\"CKDId\" = notperf.\"CKDId\"" +
        "WHERE \"EvaluationId\" = ?";

        try (Connection connection = dbHelpers.getConnection(connectionUrl, user, password)) {
            System.out.println(String.format("Searching for EvaluationId %s in the ExamNotPerformed table and NOT present in CKDRCMBilling", evaluationId));

            PreparedStatement statement = connection.prepareStatement(query);
            statement.setInt(1, evaluationId);
            JSONArray results = dbHelpers.getQueryResultsWithRetry(statement, 24, 2500);
            return results;
        } catch (Exception ex) {
            throw new Exception(String.format("Did not find EvaluationId %s in the CKDRCMBilling table!", evaluationId));
        }
    }

    public JSONArray getNotPerformedResultsByEvaluationId(int evaluationId) throws SQLException, Exception {
        String query = "SELECT * FROM \"ExamNotPerformed\" enp " + 
        "INNER JOIN \"CKD\" c ON c.\"CKDId\" = enp.\"CKDId\" " +
        "INNER JOIN \"NotPerformedReason\" npr ON npr.\"NotPerformedReasonId\" = enp.\"NotPerformedReasonId\" " +
        "WHERE \"EvaluationId\" = ?";

        try (Connection connection = dbHelpers.getConnection(connectionUrl, user, password)) {
            System.out.println(String.format("Searching for EvaluationId %s in the ExamNotPerformed table.", evaluationId));

            PreparedStatement statement = connection.prepareStatement(query);
            statement.setInt(1, evaluationId);
            JSONArray results = dbHelpers.getQueryResultsWithRetry(statement, 48, 2500);
            return results;
        } catch (Exception ex) {
            throw new Exception(String.format("Did not find EvaluationId %s in the ExamNotPerformed table!", evaluationId));
        }
    }

    public JSONArray getExamStatusByEvaluationId(int evaluationId) throws SQLException, Exception {
        String query = "SELECT * FROM \"CKD\" c " + 
        "INNER JOIN \"CKDStatus\" cs ON c.\"CKDId\" = cs.\"CKDId\" " +
        "WHERE \"EvaluationId\" = ?";

        try (Connection connection = dbHelpers.getConnection(connectionUrl, user, password)) {
            System.out.println(String.format("Searching for EvaluationId %s in the CKDStatus table.", evaluationId));

            PreparedStatement statement = connection.prepareStatement(query);
            statement.setInt(1, evaluationId);
            JSONArray results = dbHelpers.getQueryResultsWithRetry(statement, 48, 2500);
            return results;
        } catch (Exception ex) {
            throw new Exception(String.format("Did not find EvaluationId %s in the CKDStatus table!", evaluationId));
        }
    }

    public JSONArray getAnswerValuesByAnswerId(int answerId) throws SQLException, Exception {
        String query = "SELECT * FROM \"LookupCKDAnswer\" la " + 
        "WHERE \"CKDAnswerId\" = ?";

        try (Connection connection = dbHelpers.getConnection(connectionUrl, user, password)) {
            System.out.println(String.format("Searching for AnswerId %s in the LookupCKDAnswer table.", answerId));

            PreparedStatement statement = connection.prepareStatement(query);
            statement.setInt(1, answerId);
            JSONArray results = dbHelpers.getQueryResultsWithRetry(statement, 48, 2500);
            return results;
        } catch (Exception ex) {
            throw new Exception(String.format("Did not find AnswerId %s in the LookupCKDAnswer table!", answerId));
        }
    }
}
