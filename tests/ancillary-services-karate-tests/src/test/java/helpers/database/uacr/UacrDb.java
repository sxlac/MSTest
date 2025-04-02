package helpers.database.uacr;

import helpers.database.CommonDbHelpers;
import java.sql.Connection;
import java.sql.PreparedStatement;
import java.sql.SQLException;
import java.util.List;
import java.util.Optional;
import net.minidev.json.JSONArray;

public class UacrDb {
    private static CommonDbHelpers dbHelpers = new CommonDbHelpers();

    // Can be set either using Kafka (which become environment variables) or from Karate (which writes to system properties)
    private static final String connectionUrl = Optional.ofNullable(System.getenv("UACR_DB_URL")).orElse(System.getProperty("UACR_DB_URL"));
    private static final String user = Optional.ofNullable(System.getenv("UACR_DB_USERNAME")).orElse(System.getProperty("UACR_DB_USERNAME"));
    private static final String password = Optional.ofNullable(System.getenv("UACR_DB_PASSWORD")).orElse(System.getProperty("UACR_DB_PASSWORD"));
  
    public JSONArray getResultsByEvaluationId(int evaluationId) throws SQLException, Exception {
        return getResultsByEvaluationId(evaluationId, 48, 2500);
    }
    
    public JSONArray getResultsByEvaluationId(int evaluationId, int retryCount, int delay) throws SQLException, Exception {
        String query = "SELECT * FROM \"Exam\" e " +
                "INNER JOIN \"ExamStatus\" es ON e.\"ExamId\" = es.\"ExamId\" " +
                "INNER JOIN \"ExamStatusCode\" esc ON es.\"ExamStatusCodeId\" = esc.\"ExamStatusCodeId\" " +
                "WHERE e.\"EvaluationId\" = ?";
        JSONArray results = new JSONArray();
        try (Connection connection = dbHelpers.getConnection(connectionUrl, user, password)) {
            System.out.println(String.format("Searching for EvaluationId %s in the UACR exam table.", evaluationId));

            PreparedStatement statement = connection.prepareStatement(query);
            statement.setInt(1, evaluationId);
            results = dbHelpers.getQueryResultsWithRetry(statement, retryCount, delay);   
        } catch (Exception ex) {
            System.out.println(String.format("Did not find EvaluationId %s in the UACR Exam/ExamStatus table! \n %s", evaluationId, ex));
        }
        return results;
    }

    public JSONArray getNotPerformedResultsByEvaluationId(int evaluationId) throws SQLException, Exception {
        String query = "SELECT * FROM \"ExamNotPerformed\" enp " +
                "INNER JOIN \"Exam\" e ON e.\"ExamId\" = enp.\"ExamId\" " +
                "INNER JOIN \"NotPerformedReason\" npr ON enp.\"NotPerformedReasonId\" = npr.\"NotPerformedReasonId\" "
                +
                "INNER JOIN \"ExamStatus\" es ON enp.\"ExamId\" = es.\"ExamId\" " +
                "INNER JOIN \"ExamStatusCode\" esc ON es.\"ExamStatusCodeId\" = esc.\"ExamStatusCodeId\" " +
                "WHERE e.\"EvaluationId\" = ?";

        try (Connection connection = dbHelpers.getConnection(connectionUrl, user, password)) {
            System.out.println(
                    String.format("Searching for EvaluationId %s in the UACR ExamNotPerformed table.", evaluationId));

            PreparedStatement statement = connection.prepareStatement(query);
            statement.setInt(1, evaluationId);
            JSONArray results = dbHelpers.getQueryResultsWithRetry(statement, 48, 2500);
            return results;
        } catch (Exception ex) {
            throw new Exception(
                    String.format("Did not find EvaluationId %s in the UACR ExamNotPerformed table!", evaluationId));
        }
    }

    public JSONArray getBarcodeByExamId(int examId) throws SQLException, Exception {
        String query = "SELECT * FROM \"BarcodeExam\"  WHERE \"ExamId\" = ?";

        try (Connection connection = dbHelpers.getConnection(connectionUrl, user, password)) {
            System.out.println(String.format("Searching for ExamId %s in the UACR BarcodeExam table.", examId));

            PreparedStatement statement = connection.prepareStatement(query);
            statement.setInt(1, examId);
            JSONArray results = dbHelpers.getQueryResultsWithRetry(statement, 48, 2500);
            return results;
        } catch (Exception ex) {
            throw new Exception(String.format("Did not find ExamId %s in the UACR BarcodeExam table!", examId));
        }
    }

    public JSONArray getLabResultsByEvaluationId(int evaluationId) throws SQLException, Exception {
        String query = "SELECT * FROM \"LabResult\" lr " +
                "WHERE lr.\"EvaluationId\" = ?";

        try (Connection connection = dbHelpers.getConnection(connectionUrl, user, password)) {
            System.out.println(String.format("Searching for EvaluationId %s in the UACR LabResult table.", evaluationId));

            PreparedStatement statement = connection.prepareStatement(query);
            statement.setInt(1, evaluationId);
            JSONArray results = dbHelpers.getQueryResultsWithRetry(statement, 48, 2500);
            return results;
        } catch (Exception ex) {
            throw new Exception(String.format("Did not find EvaluationId %s in the UACR LabResult table! \n %s", evaluationId,ex));
        }
    }
    public JSONArray getBillingResultByEvaluationId(int evaluationId) throws Exception, SQLException {
        String query = "SELECT * FROM \"BillRequest\" br " +
                "INNER JOIN \"Exam\" e ON e.\"ExamId\" = br.\"ExamId\" " +
                "WHERE e.\"EvaluationId\" = ?";

        try (Connection connection = dbHelpers.getConnection(connectionUrl, user, password)) {
            System.out
                    .println(String.format("Searching for EvaluationId %s in the BillRequest table.",
                            evaluationId));

            PreparedStatement statement = connection.prepareStatement(query);
            statement.setInt(1, evaluationId);
            JSONArray results = dbHelpers.getQueryResultsWithRetry(statement, 24, 2500);
            return results;
        } catch (Exception ex) {
            System.out
                    .println(String.format("Did not find EvaluationId %s in the BillRequest table!", evaluationId));
            return new JSONArray();
        }
    }

    public JSONArray getBillRequestAcceptedByEvaluationId(int evaluationId) throws Exception, SQLException {
        String query = "SELECT * FROM \"BillRequest\" br " +
                "INNER JOIN \"Exam\" e ON e.\"ExamId\" = br.\"ExamId\" " +
                "WHERE e.\"EvaluationId\" = ? AND br.\"Accepted\" = true";

        try (Connection connection = dbHelpers.getConnection(connectionUrl, user, password)) {
            System.out
                    .println(String.format("Searching for EvaluationId %s in the BillRequest table.",
                            evaluationId));

            PreparedStatement statement = connection.prepareStatement(query);
            statement.setInt(1, evaluationId);
            JSONArray results = dbHelpers.getQueryResultsWithRetry(statement, 25, 2500);
            return results;
        } catch (Exception ex) {
            System.out
                    .println(String.format("Did not find EvaluationId %s in the BillRequest table!", evaluationId));
            return new JSONArray();
        }
    }

    /**
     * Gets the ProviderPay table details based on EvaluationId
     * @param evaluationId
     * @return ProviderPay table contents
     */
    public JSONArray getProviderPayResultByEvaluationId(int evaluationId) throws Exception, SQLException {
        String query = "SELECT pp.* FROM \"ProviderPay\" pp " +
                "INNER JOIN \"Exam\" e ON e.\"ExamId\" = pp.\"ExamId\" " +
                "WHERE e.\"EvaluationId\" = ?";
        JSONArray results = new JSONArray();
        try (Connection connection = dbHelpers.getConnection(connectionUrl, user, password)) {
            System.out.println(String.format("Searching for EvaluationId %s in the ProviderPay table.",evaluationId));

            PreparedStatement statement = connection.prepareStatement(query);
            statement.setInt(1, evaluationId);
            results = dbHelpers.getQueryResultsWithRetry(statement, 24, 2500);
            
        } catch (Exception ex) {
            System.out.println(String.format("Did not find EvaluationId %s in the ProviderPay table!\n%s", evaluationId,ex)); 
        }
        return results;
    }
    
    /**
     * Gets the PdfDeliveredToClient table details based on EvaluationId
     * @param evaluationId
     * @return PdfDeliveredToClient table contents
     */
    public JSONArray getPdfDeliveredToClientByEvaluationId(int evaluationId) throws Exception, SQLException {
        String query = "SELECT * FROM \"PdfDeliveredToClient\" WHERE \"EvaluationId\" = ?";
        JSONArray results = new JSONArray();
        try (Connection connection = dbHelpers.getConnection(connectionUrl, user, password)) {
            System.out
                    .println(String.format("Searching for EvaluationId %s in the PdfDeliveredToClient table.",
                            evaluationId));

            PreparedStatement statement = connection.prepareStatement(query);
            statement.setInt(1, evaluationId);
            results = dbHelpers.getQueryResultsWithRetry(statement, 24, 2500);
            
        } catch (Exception ex) {
            System.out
                    .println(String.format("Did not find EvaluationId %s in the PdfDeliveredToClient table!\n%s", evaluationId,ex));
        }
        return results;
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
            String querystr = "SELECT esc.* FROM \"Exam\" e " +
                "INNER JOIN \"ExamStatus\" es ON e.\"ExamId\" = es.\"ExamId\" " +
                "INNER JOIN \"ExamStatusCode\" esc ON es.\"ExamStatusCodeId\" = esc.\"ExamStatusCodeId\" " +
                "WHERE e.\"EvaluationId\" = ? AND esc.\"StatusName\" = '"+statusStr+"'";
            try (Connection connection = dbHelpers.getConnection(connectionUrl, user, password)) {
                System.out.println(String.format("Searching for EvaluationId %s with status %s in the UACR exam table.", evaluationId,statusStr));
                PreparedStatement statement = connection.prepareStatement(querystr);
                statement.setInt(1, evaluationId);
                JSONArray records = dbHelpers.getQueryResultsWithRetry(statement, 48, 2500);
                for (Object record:records){results.add(record);}
            } catch (Exception ex) {
                System.out.println(String.format("Could not find Exam with EvaluationId %s and examStatus %s in the UACR exam table.\n", evaluationId,statusStr,ex));
            }
        }
        return results;     
    }

    /**
     * Returns the tableName table schema. Useful for validating column names, data types, data size and nullability.
     * 
     * @param tableName
     * @return
     * @throws SQLException
     * @throws Exception
     */
    public JSONArray checkTableSchema(String tableName) throws SQLException, Exception {
        String query = "SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = ? ORDER BY ordinal_position";
        try (Connection connection = dbHelpers.getConnection(connectionUrl, user, password)) {
            System.out.println(String.format("Checking Schema %s.", tableName));
            PreparedStatement statement = connection.prepareStatement(query);
            statement.setString(1, tableName);
            JSONArray results = dbHelpers.getQueryResultsWithRetry(statement, 2, 2500);
            return results;
        } catch (Exception ex) {
            throw new Exception(String.format("Did not find %s table!", tableName));
        }
    }

    /**
     * Returns ExamId if record exists
     * 
     * @param evaluationId, retryCount, delay
     * @return ExamId if Exam exists
     * @throws SQLException
     * @throws Exception
     */
    public JSONArray getExamId(int evaluationId, int retryCount, int delay) throws SQLException, Exception {
        String query = "SELECT e.\"ExamId\" FROM \"Exam\" e WHERE e.\"EvaluationId\" = ?";
        JSONArray results = new JSONArray();
        try (Connection connection = dbHelpers.getConnection(connectionUrl, user, password)) {
            System.out.println(String.format("Searching for EvaluationId %s in the UACR exam table.", evaluationId));

            PreparedStatement statement = connection.prepareStatement(query);
            statement.setInt(1, evaluationId);
            results = dbHelpers.getQueryResultsWithRetry(statement, retryCount, delay);   
        } catch (Exception ex) {
            System.out.println(String.format("Did not find EvaluationId %s in the UACR Exam/ExamStatus table! \n %s", evaluationId, ex));
        }
        return results;
    }

    /**
     * Returns DateOfService if record exists
     * 
     * @param evaluationId
     * @return DateOfService if Exam exists
     * @throws SQLException
     * @throws Exception
     */
    public JSONArray getExamDates(int evaluationId) throws SQLException, Exception {
        String query = "SELECT e.\"DateOfService\", e.\"EvaluationCreatedDateTime\", e.\"EvaluationReceivedDateTime\" FROM \"Exam\" e WHERE e.\"EvaluationId\" = ?";
        JSONArray results = new JSONArray();
        try (Connection connection = dbHelpers.getConnection(connectionUrl, user, password)) {
            System.out.println(String.format("Searching for EvaluationId %s in the UACR exam table.", evaluationId));

            PreparedStatement statement = connection.prepareStatement(query);
            statement.setInt(1, evaluationId);
            results = dbHelpers.getQueryResultsWithRetry(statement, 10, 3000);   
        } catch (Exception ex) {
            System.out.println(String.format("Did not find EvaluationId %s in the UACR Exam/ExamStatus table! \n %s", evaluationId, ex));
        }
        return results;
    }
}



