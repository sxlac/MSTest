package helpers.database.egfr;

import helpers.database.CommonDbHelpers;

import java.sql.Connection;
import java.sql.PreparedStatement;
import java.sql.SQLException;
import java.util.List;
import java.util.Optional;

import net.minidev.json.JSONArray;
import net.minidev.json.JSONObject;

public class EgfrDb {
    private static CommonDbHelpers dbHelpers = new CommonDbHelpers();

    // Can be set either using Kafka (which become environment variables) or from
    // Karate (which writes to system properties)
    private static final String connectionUrl = Optional.ofNullable(System.getenv("EGFR_DB_URL"))
            .orElse(System.getProperty("EGFR_DB_URL"));
    private static final String user = Optional.ofNullable(System.getenv("EGFR_DB_USERNAME"))
            .orElse(System.getProperty("EGFR_DB_USERNAME"));
    private static final String password = Optional.ofNullable(System.getenv("EGFR_DB_PASSWORD"))
            .orElse(System.getProperty("EGFR_DB_PASSWORD"));

    public JSONArray getResultsByEvaluationId(int evaluationId) throws SQLException, Exception {
        String query = "SELECT * FROM \"Exam\" e " +
                "INNER JOIN \"ExamStatus\" es ON e.\"ExamId\" = es.\"ExamId\" " +
                "INNER JOIN \"ExamStatusCode\" esc ON es.\"ExamStatusCodeId\" = esc.\"ExamStatusCodeId\" " +
                "WHERE \"EvaluationId\" = ?";

        try (Connection connection = dbHelpers.getConnection(connectionUrl, user, password)) {
            System.out.println(String.format("Searching for EvaluationId %s in the EGFR exam table.", evaluationId));

            PreparedStatement statement = connection.prepareStatement(query);
            statement.setInt(1, evaluationId);
            JSONArray results = dbHelpers.getQueryResultsWithRetry(statement, 48, 2500);
            return results;
        } catch (Exception ex) {
            System.out.println(String.format("No EvaluationId %s found in the EGFR exam table.", evaluationId));
            return new JSONArray();
        }
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
                    String.format("Searching for EvaluationId %s in the EGFR ExamNotPerformed table.", evaluationId));

            PreparedStatement statement = connection.prepareStatement(query);
            statement.setInt(1, evaluationId);
            JSONArray results = dbHelpers.getQueryResultsWithRetry(statement, 48, 2500);
            return results;
        } catch (Exception ex) {
            throw new Exception(
                    String.format("Did not find EvaluationId %s in the EGFR ExamNotPerformed table!", evaluationId));
        }
    }

    public JSONArray getBarcodeHistoryByExamId(int examId) throws SQLException, Exception {
        String query = "SELECT * FROM \"BarcodeHistory\"  WHERE \"ExamId\" = ?";

        try (Connection connection = dbHelpers.getConnection(connectionUrl, user, password)) {
            System.out.println(String.format("Searching for ExamId %s in the EGFR BarcodeHistory table.", examId));

            PreparedStatement statement = connection.prepareStatement(query);
            statement.setInt(1, examId);
            JSONArray results = dbHelpers.getQueryResultsWithRetry(statement, 48, 2500);
            return results;
        } catch (Exception ex) {
            throw new Exception(String.format("Did not find ExamId %s in the EGFR BarcodeHistory table!", examId));
        }
    }

    public JSONArray getExamStatusCodeLabResultsReceived() throws SQLException, Exception {
        String query = "SELECT * FROM \"ExamStatusCode\"";

        try (Connection connection = dbHelpers.getConnection(connectionUrl, user, password)) {
            System.out.println(String.format("Searching for Lab Results Received in the ExamStatusCode table."));

            PreparedStatement statement = connection.prepareStatement(query);
            JSONArray results = dbHelpers.getQueryResultsWithRetry(statement, 24, 2500);
            return results;
        } catch (Exception ex) {
            throw new Exception(String.format("Did not find Lab Results Received in the ExamStatusCode table!"));
        }
    }

    public JSONArray checkLabResultsTablePresent() throws SQLException, Exception {
        String query = "SELECT * FROM \"QuestLabResult\"" +
                "WHERE EXISTS (SELECT \"LabResultId\", \"CenseoId\", \"VendorLabTestId\", \"VendorLabTestNumber\", \"eGFRResult\""
                +
                ",\"CreatinineResult\", \"Normality\", \"NormalityCode\", \"MailDate\", \"CollectionDate\", \"AccessionedDate\""
                +
                ",\"CreatedDateTime\" FROM public.\"QuestLabResult\")";

        try (Connection connection = dbHelpers.getConnection(connectionUrl, user, password)) {

            PreparedStatement statement = connection.prepareStatement(query);
            JSONArray results = dbHelpers.getQueryResultsWithRetry(statement, 1, 2500);
            return results;
        } catch (Exception ex) {
            throw new Exception(String.format("Did not find Lab Results Received record in the LabReceived table!"));
        }
    }

    public JSONArray checkBillRequestSentPresent() throws SQLException, Exception {
        String query = "SELECT * FROM information_schema.columns WHERE table_schema = \'public\' AND table_name = \'BillRequestSent\'";
        try (Connection connection = dbHelpers.getConnection(connectionUrl, user, password)) {

            PreparedStatement statement = connection.prepareStatement(query);
            JSONArray results = dbHelpers.getQueryResultsWithRetry(statement, 1, 2500);
            return results;
        } catch (Exception ex) {
            throw new Exception(String.format("Did not find BillRequestSent table!"));
        }
    }

    public JSONArray checkPdfDeliveredToClientPresent() throws SQLException, Exception {
        String query = "SELECT * FROM information_schema.columns WHERE table_schema = \'public\' AND table_name = \'PdfDeliveredToClient\'";
        try (Connection connection = dbHelpers.getConnection(connectionUrl, user, password)) {

            PreparedStatement statement = connection.prepareStatement(query);
            JSONArray results = dbHelpers.getQueryResultsWithRetry(statement, 1, 2500);
            return results;
        } catch (Exception ex) {
            throw new Exception(String.format("Did not find PdfDeliveredToClient table!"));
        }
    }

    public JSONArray checkLabResultsRecordPresentByCenseoId(String CenseoId) throws SQLException, Exception {
        String query = "SELECT * FROM \"QuestLabResult\" WHERE \"CenseoId\" = '%s'";
        String queryFormatted = String.format(query, CenseoId);
        try (Connection connection = dbHelpers.getConnection(connectionUrl, user, password)) {

            PreparedStatement statement = connection.prepareStatement(queryFormatted);
            System.out.println(
                    String.format("Searching for CenseoId with query %s in the EGFR LabResult table.", statement));

            JSONArray results = dbHelpers.getQueryResultsWithRetry(statement, 48, 2500);
            return results;
        } catch (Exception ex) {
            throw new Exception(String.format("Did not find Lab Results Received record in the LabReceived table!"));
        }
    }

    public JSONArray getBillingResultByEvaluationId(int evaluationId) throws Exception, SQLException {
        String query = "SELECT * FROM \"BillRequestSent\" brs " +
                "INNER JOIN \"Exam\" se ON se.\"ExamId\" = brs.\"ExamId\" " +
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
            System.out
                    .println(String.format("Did not find EvaluationId %s in the BillRequestSent table!", evaluationId));
            return new JSONArray();
        }
    }

    public JSONArray getPdfDeliveredToClientByEvaluationId(int evaluationId) throws Exception, SQLException {
        String query = "SELECT * FROM \"PdfDeliveredToClient\" WHERE \"EvaluationId\" = ?";

        try (Connection connection = dbHelpers.getConnection(connectionUrl, user, password)) {
            System.out
                    .println(String.format("Searching for EvaluationId %s in the PdfDeliveredToClient table.",
                            evaluationId));

            PreparedStatement statement = connection.prepareStatement(query);
            statement.setInt(1, evaluationId);
            JSONArray results = dbHelpers.getQueryResultsWithRetry(statement, 24, 2500);
            return results;
        } catch (Exception ex) {
            throw new Exception(
                    String.format("Did not find EvaluationId %s in the PdfDeliveredToClient table!", evaluationId));
        }
    }

    /**
     * Get all status from ExamStatusCode table
     * 
     * @return
     * @throws SQLException
     * @throws Exception
     */
    public JSONArray getExamStatusCodes() throws SQLException, Exception {
        String query = "SELECT * FROM \"ExamStatusCode\" ORDER BY \"ExamStatusCodeId\" ASC";

        try (Connection connection = dbHelpers.getConnection(connectionUrl, user, password)) {
            System.out.println(String.format("Searching for statuses in ExamStatusCode table."));

            PreparedStatement statement = connection.prepareStatement(query);
            JSONArray results = dbHelpers.getQueryResultsWithRetry(statement, 3, 2500);
            return results;
        } catch (Exception ex) {
            throw new Exception(String.format("Did not find any entry in ExamStatusCode table!"));
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
     * Gets the ProviderPay table details based on EvaluationId
     * @param evaluationId
     * @return ProviderPay table contents
     * @throws Exception
     * @throws SQLException
     */
    public JSONArray getProviderPayResultByEvaluationId(int evaluationId) throws Exception, SQLException {
        String query = "SELECT pp.* FROM \"ProviderPay\" pp " +
                "INNER JOIN \"Exam\" e ON e.\"ExamId\" = pp.\"ExamId\" " +
                "WHERE e.\"EvaluationId\" = ?";

        try (Connection connection = dbHelpers.getConnection(connectionUrl, user, password)) {
            System.out
                    .println(String.format("Searching for EvaluationId %s in the ProviderPay table.",
                            evaluationId));

            PreparedStatement statement = connection.prepareStatement(query);
            statement.setInt(1, evaluationId);
            JSONArray results = dbHelpers.getQueryResultsWithRetry(statement, 24, 2500);
            return results;
        } catch (Exception ex) {
            System.out
                    .println(String.format("Did not find EvaluationId %s in the ProviderPay table!", evaluationId));
            return new JSONArray();
        }
    }

    /**
     * Gets an ordered list of status from ExamStatus table based on EvaluationId
     * @param evaluationId
     * @return ExamStatus orderered by ExamStatusCodeId in ascending order
     * @throws Exception
     * @throws SQLException
     */
    public JSONArray getOrderedStatusByEvaluationId(int evaluationId) throws Exception, SQLException {
        String query = "SELECT es.* FROM \"ExamStatus\" es " +
                "INNER JOIN \"Exam\" e ON e.\"ExamId\" = es.\"ExamId\" " +
                "WHERE e.\"EvaluationId\" = ? ORDER BY \"ExamStatusCodeId\"";

        try (Connection connection = dbHelpers.getConnection(connectionUrl, user, password)) {
            System.out
                    .println(String.format("Searching for EvaluationId %s in the ExamStatus table.",
                            evaluationId));

            PreparedStatement statement = connection.prepareStatement(query);
            statement.setInt(1, evaluationId);
            JSONArray results = dbHelpers.getQueryResultsWithRetry(statement, 24, 2500);
            return results;
        } catch (Exception ex) {
            System.out
                    .println(String.format("Did not find any status in ExamStatus table for %s!", evaluationId));
            return new JSONArray();
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
            String querystr = "SELECT esc.* FROM \"Exam\" e " +
                "INNER JOIN \"ExamStatus\" es ON e.\"ExamId\" = es.\"ExamId\" " +
                "INNER JOIN \"ExamStatusCode\" esc ON es.\"ExamStatusCodeId\" = esc.\"ExamStatusCodeId\" " +
                "WHERE e.\"EvaluationId\" = ? AND esc.\"StatusName\" = '"+statusStr+"'";
            try (Connection connection = dbHelpers.getConnection(connectionUrl, user, password)) {
                System.out.println(String.format("Searching for EvaluationId %s with status %s in the EGFR exam table.", evaluationId,statusStr));
                PreparedStatement statement = connection.prepareStatement(querystr);
                statement.setInt(1, evaluationId);
                JSONArray records = dbHelpers.getQueryResultsWithRetry(statement, 48, 2500);
                for (Object record:records){results.add(record);}
            } catch (Exception ex) {
                System.out.println(String.format("Could not find Exam with EvaluationId %s and examStatus %s in the EGFR exam table.\n", evaluationId,statusStr,ex));
            }
        }
        return results;     
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
            System.out.println(String.format("Searching for EvaluationId %s in the EGFR exam table.", evaluationId));

            PreparedStatement statement = connection.prepareStatement(query);
            statement.setInt(1, evaluationId);
            results = dbHelpers.getQueryResultsWithRetry(statement, 10, 3000);   
        } catch (Exception ex) {
            System.out.println(String.format("Did not find EvaluationId %s in the EGFR Exam/ExamStatus table! \n %s", evaluationId, ex));
        }
        return results;
    }

    /**
     * Gets the LabResults table details based on EvaluationId
     * @param evaluationId
     * @return QuestLabResult table contents
     * @throws Exception
     * @throws SQLException
     */
    public JSONArray checkQuestLabResultsRecordPresentByEvaluationId(Integer evaluationId) throws SQLException, Exception {
        String query = "SELECT brs.*, se.\"EvaluationId\" FROM \"QuestLabResult\" brs " +
            "INNER JOIN \"Exam\" se ON se.\"CenseoId\" = brs.\"CenseoId\" " +
            "WHERE se.\"EvaluationId\" = ?";
        try (Connection connection = dbHelpers.getConnection(connectionUrl, user, password)) {
                PreparedStatement statement = connection.prepareStatement(query);
                statement.setInt(1, evaluationId);
                JSONArray results = dbHelpers.getQueryResultsWithRetry(statement, 24, 2500);
                return results;
        } catch (Exception ex) {
            System.out.println(String.format("No EvaluationId %s found in the QuestLabResults.", evaluationId));
            return new JSONArray();
        }
    }

    /**
     * Gets the LabResults table details based on EvaluationId
     * @param evaluationId
     * @return LabResult table contents
     * @throws Exception
     * @throws SQLException
     */
    public JSONArray getLabResultsRecordByEvaluationId(int evaluationId) throws SQLException, Exception {
        String query = "SELECT brs.*, se.\"EvaluationId\" FROM \"LabResult\" brs INNER JOIN \"Exam\" se ON se.\"ExamId\" = brs.\"ExamId\" WHERE se.\"EvaluationId\" = ?";

        try (Connection connection = dbHelpers.getConnection(connectionUrl, user, password)) {
            PreparedStatement statement = connection.prepareStatement(query);
            statement.setInt(1, evaluationId);
            JSONArray results = dbHelpers.getQueryResultsWithRetry(statement, 24, 2500);
            return results;
        } catch (Exception ex) {
            System.out
            .println(String.format("Did not find  ExamId by EvaluationID %s!", evaluationId));
            return new JSONArray();
        }
    }
}
