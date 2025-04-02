package helpers.database.spirometry;

import java.sql.Connection;
import java.sql.PreparedStatement;
import java.sql.SQLException;
import java.util.Optional;
import net.minidev.json.JSONArray;
import helpers.database.CommonDbHelpers;

public class SpirometryDb {
    // Can be set either using Kafka (which become environment variables) or from
    // Karate (which writes to system properties)
    private static CommonDbHelpers dbHelpers = new CommonDbHelpers();

    private static final String connectionUrl = Optional.ofNullable(System.getenv("SPIROMETRY_DB_URL"))
            .orElse(System.getProperty("SPIROMETRY_DB_URL"));
    private static final String user = Optional.ofNullable(System.getenv("SPIROMETRY_DB_USERNAME"))
            .orElse(System.getProperty("SPIROMETRY_DB_USERNAME"));
    private static final String password = Optional.ofNullable(System.getenv("SPIROMETRY_DB_PASSWORD"))
            .orElse(System.getProperty("SPIROMETRY_DB_PASSWORD"));

    public JSONArray getExamByEvaluationId(int evaluationId) throws Exception, SQLException {
        String query = "SELECT * FROM \"SpirometryExam\" WHERE \"EvaluationId\" = ?";

        try (Connection connection = dbHelpers.getConnection(connectionUrl, user, password)) {
            System.out
                    .println(String.format("Searching for EvaluationId %s in the SpirometryExam table.", evaluationId));

            PreparedStatement statement = connection.prepareStatement(query);
            statement.setInt(1, evaluationId);
            JSONArray results = dbHelpers.getQueryResultsWithRetry(statement, 24, 2500);
            return results;
        } catch (Exception ex) {
            throw new Exception(
                    String.format("Did not find EvaluationId %s in the SpirometryExam table!", evaluationId));
        }
    }

    public JSONArray getResultsByEvaluationId(int evaluationId) throws Exception, SQLException {
        String query = "SELECT * FROM \"SpirometryExamResults\" ser " +
                "INNER JOIN \"SpirometryExam\" se ON ser.\"SpirometryExamId\" = se.\"SpirometryExamId\" " +
                "INNER JOIN \"NormalityIndicator\" ni ON ser.\"NormalityIndicatorId\" = ni.\"NormalityIndicatorId\" " +
                "WHERE se.\"EvaluationId\" = ?";

        try (Connection connection = dbHelpers.getConnection(connectionUrl, user, password)) {
            System.out.println(
                    String.format("Searching for evaluationId %s in the SpirometryExamResults table.", evaluationId));

            PreparedStatement statement = connection.prepareStatement(query);
            statement.setInt(1, evaluationId);
            JSONArray results = dbHelpers.getQueryResultsWithRetry(statement, 5, 15000);
            return results;
        } catch (Exception ex) {
            throw new Exception(
                    String.format("Did not find evaluationId %s in the SpirometryExamResults table!", evaluationId));
        }
    }

    public JSONArray getBillingResultByEvaluationId(int evaluationId) throws Exception, SQLException {
        String query = "SELECT * FROM \"BillRequestSent\" brs " +
                "INNER JOIN \"SpirometryExam\" se ON se.\"SpirometryExamId\" = brs.\"SpirometryExamId\" " +
                "WHERE se.\"EvaluationId\" = ?";

        try (Connection connection = dbHelpers.getConnection(connectionUrl, user, password)) {
            System.out
                    .println(String.format("Searching for EvaluationId %s in the BillRequestSent table.",
                            evaluationId));

            PreparedStatement statement = connection.prepareStatement(query);
            statement.setInt(1, evaluationId);
            JSONArray results = dbHelpers.getQueryResultsWithRetry(statement, 24, 2500);
            return results;
        } catch (Exception ex) {
            throw new Exception(
                    String.format("Did not find EvaluationId %s in the BillRequestSent table!", evaluationId));
        }
    }

    public JSONArray getNotPerformedByEvaluationId(int evaluationId) throws SQLException, Exception {
        String query = "SELECT * FROM \"ExamNotPerformed\" enp " +
                        "INNER JOIN \"SpirometryExam\" p ON p.\"SpirometryExamId\" = enp.\"SpirometryExamId\" " +
                        "WHERE \"EvaluationId\" = ?";

        try (Connection connection = dbHelpers.getConnection(connectionUrl, user, password)) {
            System.out.println(String.format("Searching for EvaluationId %s in the SpirometryExam table.", evaluationId));

            PreparedStatement statement = connection.prepareStatement(query);
            statement.setInt(1, evaluationId);
            JSONArray results = dbHelpers.getQueryResultsWithRetry(statement, 48, 2500);
            return results;
        } catch (Exception ex) {
            throw new Exception(String.format("Did not find EvaluationId %s in the ExamNotPerformed table!", evaluationId));
        }
    }

    public JSONArray getOverreadResultByAppointmentId(int appointmentId) throws SQLException, Exception {
        String query = "SELECT * FROM \"OverreadResult\"" +
                        "WHERE \"AppointmentId\" = ?";

        try (Connection connection = dbHelpers.getConnection(connectionUrl, user, password)) {
            System.out.println(String.format("Searching for AppointmentId %s in the OverreadResult table.", appointmentId));

            PreparedStatement statement = connection.prepareStatement(query);
            statement.setInt(1, appointmentId);
            JSONArray results = dbHelpers.getQueryResultsWithRetry(statement, 48, 2500);
            return results;
        } catch (Exception ex) {
            System.out.println(String.format("Did not find AppointmentId %s in the OverreadResult table!\n%s", appointmentId,ex));
            return new JSONArray();
        }
    }

    public JSONArray getExamStatusByEvaluationId(int evaluationId) throws Exception, SQLException {
        String query = "SELECT * FROM \"SpirometryExam\" se " +
                "INNER JOIN \"ExamStatus\" ex ON se.\"SpirometryExamId\" = ex.\"SpirometryExamId\" " +
                "INNER JOIN \"StatusCode\" sc ON sc.\"StatusCodeId\" = ex.\"StatusCodeId\" " +
                "WHERE se.\"EvaluationId\" = ?";

        try (Connection connection = dbHelpers.getConnection(connectionUrl, user, password)) {
            System.out.println(
                    String.format("Searching for evaluationId %s in the SpirometryExam table.", evaluationId));

            PreparedStatement statement = connection.prepareStatement(query);
            statement.setInt(1, evaluationId);
            JSONArray results = dbHelpers.getQueryResultsWithRetry(statement, 24, 2500);
            return results;
        } catch (Exception ex) {
            throw new Exception(
                    String.format("Did not find evaluationId %s in the SpirometryExam table!", evaluationId));
        }
    }

    public JSONArray getEvalSagaByEvaluationId(int evaluationId) throws Exception, SQLException {
        String query = "SELECT * FROM \"Spirometry_EvaluationSaga\" se " +
                        "WHERE se.\"Correlation_EvaluationId\" = ?";

        try (Connection connection = dbHelpers.getConnection(connectionUrl, user, password)) {
            System.out.println(
                    String.format("Searching for evaluationId %s in the Spirometry_EvaluationSaga table.", evaluationId));

            PreparedStatement statement = connection.prepareStatement(query);
            statement.setInt(1, evaluationId);
            JSONArray results = dbHelpers.getQueryResultsWithRetry(statement, 24, 2500);
            return results;
        } catch (Exception ex) {
            throw new Exception(
                    String.format("Did not find evaluationId %s in the Spirometry_EvaluationSaga table!", evaluationId));
        }
    }

     /**
     * Get all status from StatusCode table
     * 
     * @return
     * @throws SQLException
     * @throws Exception
     */
    public JSONArray getStatusCodes() throws SQLException, Exception {
        String query = "SELECT * FROM \"StatusCode\" ORDER BY \"StatusCodeId\" ASC";

        try (Connection connection = dbHelpers.getConnection(connectionUrl, user, password)) {
            System.out.println(String.format("Searching for statuses in StatusCode table."));

            PreparedStatement statement = connection.prepareStatement(query);
            JSONArray results = dbHelpers.getQueryResultsWithRetry(statement, 3, 2500);
            return results;
        } catch (Exception ex) {
            throw new Exception(String.format("Did not find any entry in StatusCode table!"));
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

    public JSONArray getProviderPayResultsWithEvalId(int evaluationId) throws Exception, SQLException {
        String query = "SELECT * FROM \"ProviderPay\" pp " + 
                       "INNER JOIN \"SpirometryExam\" se ON pp.\"SpirometryExamId\" = se.\"SpirometryExamId\" " +
                       "WHERE \"EvaluationId\" = ?";

        try (Connection connection = dbHelpers.getConnection(connectionUrl, user, password)) {
            System.out.println(
                    String.format("Searching for evaluationId %s in the ProviderPay table.", evaluationId));

            PreparedStatement statement = connection.prepareStatement(query);
            statement.setInt(1, evaluationId);
            JSONArray results = dbHelpers.getQueryResultsWithRetry(statement, 24, 2500);
            return results;
        } catch (Exception ex) {
                System.out.println(String.format("Did not find EvaluationId %s in the ProviderPay table!", evaluationId));
                return new JSONArray();
        }
    }
}