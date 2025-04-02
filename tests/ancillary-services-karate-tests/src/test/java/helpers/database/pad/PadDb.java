package helpers.database.pad;

import helpers.database.CommonDbHelpers;
import java.sql.Connection;
import java.sql.PreparedStatement;
import java.sql.SQLException;
import java.util.Optional;
import net.minidev.json.JSONArray;

public class PadDb {
    private static CommonDbHelpers dbHelpers = new CommonDbHelpers();

    // Can be set either using Kafka (which become environment variables) or from Karate (which writes to system properties)
    private final String connectionUrl = Optional.ofNullable(System.getenv("PAD_DB_URL")).orElse(System.getProperty("PAD_DB_URL"));
    private final String user = Optional.ofNullable(System.getenv("PAD_DB_USERNAME")).orElse(System.getProperty("PAD_DB_USERNAME"));
    private final String password = Optional.ofNullable(System.getenv("PAD_DB_PASSWORD")).orElse(System.getProperty("PAD_DB_PASSWORD"));

    public JSONArray getResultsByEvaluationId(int evaluationId) throws SQLException, Exception {
        return getResultsByEvaluationId(evaluationId, 48, 2500);
    }

    public JSONArray getResultsByEvaluationId(int evaluationId, int retryCount, int delay) throws SQLException, Exception {
        String query = "SELECT * FROM \"PAD\" WHERE \"EvaluationId\" = ?";

        try (Connection connection = dbHelpers.getConnection(connectionUrl, user, password)) {
            System.out.println(String.format("Searching for EvaluationId %s in the PAD table.", evaluationId));

            PreparedStatement statement = connection.prepareStatement(query);
            statement.setInt(1, evaluationId);
            JSONArray results = dbHelpers.getQueryResultsWithRetry(statement, retryCount, delay);
            return results;
        } catch (Exception ex) {
            System.out.println(String.format("Did not find EvaluationId %s in the PAD table!", evaluationId));
            return new JSONArray();
        }
    }
    
    public JSONArray getBillingResultsWithEvalId(int evaluationId) throws Exception {
        String query = "SELECT * FROM \"PADRCMBilling\" prb " +
                "INNER JOIN \"PAD\" p ON p.\"PADId\" = prb.\"PADId\" " +
                "WHERE p.\"EvaluationId\" = ?";

        try (Connection connection = dbHelpers.getConnection(connectionUrl, user, password)) {
            System.out.println(String.format("Searching for EvaluationId %s in the PADRCMBilling table.", evaluationId));

            PreparedStatement statement = connection.prepareStatement(query);
            statement.setInt(1, evaluationId);
            return dbHelpers.getQueryResultsWithRetry(statement, 10, 1000);
        } catch (Exception ex) {
            throw new Exception(
                    String.format("Did not find EvaluationId %s in the PADRCMBilling table!", evaluationId));
        }
    }

    public JSONArray getProviderPayByEvalId(int evaluationId) throws Exception {
        return getProviderPayByEvalId(evaluationId, 10, 1000);
    }

    public JSONArray getProviderPayByEvalId(int evaluationId, int retryCount, int delay) throws Exception {
        String query = "SELECT * FROM \"ProviderPay\" pp " +
                "INNER JOIN \"PAD\" p ON p.\"PADId\" = pp.\"PADId\" " +
                "WHERE p.\"EvaluationId\" = ?";

        try (Connection connection = dbHelpers.getConnection(connectionUrl, user, password)) {
            System.out.println(String.format("Searching for EvaluationId %s in the ProviderPay table.", evaluationId));

            PreparedStatement statement = connection.prepareStatement(query);
            statement.setInt(1, evaluationId);
            JSONArray results = dbHelpers.getQueryResultsWithRetry(statement, retryCount, delay);
            return results;
        } catch (Exception ex) {
            // throw new Exception(
            // String.format("Did not find EvaluationId %s in the ProviderPay table!", evaluationId));
            System.out.println(String.format("Did not find EvaluationId %s in the ProviderPay table!", evaluationId));
            return new JSONArray();
        }
    }

    public JSONArray getWaveformDocsByMemberPlanId(int memberPlanId) throws Exception {
        String query = "SELECT * FROM \"WaveformDocument\" WHERE \"MemberPlanId\" = ? ORDER BY \"WaveformDocumentId\" ASC";

        try (Connection connection = dbHelpers.getConnection(connectionUrl, user, password)) {
            System.out.println(
                    String.format("Searching for MemberPlanId %s in the WaveformDocument table.", memberPlanId));

            PreparedStatement statement = connection.prepareStatement(query);
            statement.setInt(1, memberPlanId);
            return dbHelpers.getQueryResultsWithRetry(statement, 10, 2500);
        } catch (Exception ex) {
            throw new Exception(
                    String.format("Did not find MemberPlanId %s in the WaveformDocument table!", memberPlanId));
        }
    }

    public JSONArray getNotPerformedResultsByEvaluationId(int evaluationId) throws Exception {
        String query = "SELECT * FROM \"NotPerformed\" np " +
                "INNER JOIN \"PAD\" p ON p.\"PADId\" = np.\"PADId\" " +
                "WHERE p.\"EvaluationId\" = ?";

        try (Connection connection = dbHelpers.getConnection(connectionUrl, user, password)) {
            System.out.println(String.format("Searching for EvaluationId %s in the NotPerformed table.", evaluationId));

            PreparedStatement statement = connection.prepareStatement(query);
            statement.setInt(1, evaluationId);
            return dbHelpers.getQueryResultsWithRetry(statement, 10, 2500);
        } catch (Exception ex) {
            throw new Exception(String.format("Did not find EvaluationId %s in the NotPerformed table!", evaluationId));
        }
    }

    public JSONArray getPadStatusByEvaluationId(int evaluationId) throws Exception {
        String query = "SELECT * FROM \"PADStatus\" ps " +
                "INNER JOIN \"PAD\" p ON p.\"PADId\" = ps.\"PADId\" " +
                "INNER JOIN \"PADStatusCode\" psc ON psc.\"PADStatusCodeId\" = ps.\"PADStatusCodeId\"" +
                "WHERE p.\"EvaluationId\" = ?";

        try (Connection connection = dbHelpers.getConnection(connectionUrl, user, password)) {
            System.out.println(String.format("Searching for EvaluationId %s in the PADStatus table.", evaluationId));

            PreparedStatement statement = connection.prepareStatement(query);
            statement.setInt(1, evaluationId);
            return dbHelpers.getQueryResultsWithRetry(statement, 10, 2500);
        } catch (Exception ex) {
            throw new Exception(String.format("Did not find EvaluationId %s in the PADStatus table!", evaluationId));
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

    public JSONArray getAoESymptomSupportResultsByEvaluationId(int evaluationId) throws Exception {
        String query = "SELECT * FROM \"PAD\" np " +
                "INNER JOIN \"AoeSymptomSupportResult\" p ON p.\"PADId\" = np.\"PADId\" " +
                "WHERE np.\"EvaluationId\" = ?";

        try (Connection connection = dbHelpers.getConnection(connectionUrl, user, password)) {
            System.out.println(String.format("Searching for EvaluationId %s in the PAD table.", evaluationId));

            PreparedStatement statement = connection.prepareStatement(query);
            statement.setInt(1, evaluationId);
            return dbHelpers.getQueryResultsWithRetry(statement, 10, 2500);
        } catch (Exception ex) {
            throw new Exception(String.format("Did not find EvaluationId %s in the PAD table!", evaluationId));
        }
    }
}