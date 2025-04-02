package helpers.database.dee;

import java.sql.Connection;
import java.sql.PreparedStatement;
import java.sql.SQLException;
import java.util.List;
import java.util.Optional;
import helpers.database.CommonDbHelpers;
import net.minidev.json.JSONArray;

public class DeeDb {
    private static CommonDbHelpers dbHelpers = new CommonDbHelpers();

    // Can be set either using Kafka (which become environment variables) or from
    // Karate (which writes to system properties)
    private static final String connectionUrl = Optional.ofNullable(System.getenv("DEE_DB_URL"))
            .orElse(System.getProperty("DEE_DB_URL"));
    private static final String user = Optional.ofNullable(System.getenv("DEE_DB_USERNAME"))
            .orElse(System.getProperty("DEE_DB_USERNAME"));
    private static final String password = Optional.ofNullable(System.getenv("DEE_DB_PASSWORD"))
            .orElse(System.getProperty("DEE_DB_PASSWORD"));

    public JSONArray getIrisExamIdFromDeeDb(int evaluationId) throws SQLException, Exception {
        String query = "SELECT \"DeeExamId\" FROM public.\"Exam\" WHERE \"EvaluationId\" = ? AND \"DeeExamId\" IS NOT NULL";

        try (Connection connection = dbHelpers.getConnection(connectionUrl, user, password)) {
            System.out.println(String.format("Searching for EvaluationId %s in the Exam table.", evaluationId));

            PreparedStatement statement = connection.prepareStatement(query);
            statement.setInt(1, evaluationId);
            JSONArray results = dbHelpers.getQueryResultsWithRetry(statement, 48, 2500);
            return results;
        } catch (Exception ex) {
            throw new Exception(String.format("Did not find EvaluationId %s in the Exam table!", evaluationId));
        }
    }

    public JSONArray getResultsByEvaluationId(int evaluationId) throws SQLException, Exception {
        String query = "SELECT * FROM public.\"ExamResult\" er INNER JOIN public.\"Exam\" e ON e.\"ExamId\" = er.\"ExamId\" WHERE e.\"EvaluationId\" = ?";

        try (Connection connection = dbHelpers.getConnection(connectionUrl, user, password)) {
            System.out.println(String.format("Searching for EvaluationId %s in the ExamResult table.", evaluationId));

            PreparedStatement statement = connection.prepareStatement(query);
            statement.setInt(1, evaluationId);
            JSONArray results = dbHelpers.getQueryResultsWithRetry(statement, 48, 2500);
            return results;
        } catch (Exception ex) {
            throw new Exception(String.format("Did not find EvaluationId %s in the ExamResult table!", evaluationId));
        }
    }

    public JSONArray getNotPerformedResultsByEvaluationId(int evaluationId) throws SQLException, Exception {
        String query = "SELECT * FROM public.\"DeeNotPerformed\" dnp " +
                "INNER JOIN public.\"Exam\" e ON e.\"ExamId\" = dnp.\"ExamId\" " +
                "INNER JOIN public.\"NotPerformedReason\" npr ON npr.\"NotPerformedReasonId\" = dnp.\"NotPerformedReasonId\" " +
                "WHERE e.\"EvaluationId\" = ?";

        try (Connection connection = dbHelpers.getConnection(connectionUrl, user, password)) {
            System.out.println(
                    String.format("Searching for EvaluationId %s in the DeeNotPerformed table.", evaluationId));

            PreparedStatement statement = connection.prepareStatement(query);
            statement.setInt(1, evaluationId);
            JSONArray results = dbHelpers.getQueryResultsWithRetry(statement, 48, 2500);
            return results;
        } catch (Exception ex) {
            throw new Exception(
                    String.format("Did not find EvaluationId %s in the DeeNotPerformed table!", evaluationId));
        }
    }

    public JSONArray getBillingResultByEvaluationId(int evaluationId) throws SQLException, Exception {
        String query = "SELECT * FROM public.\"DEEBilling\" db " +
                "INNER JOIN public.\"Exam\" e ON e.\"ExamId\" = db.\"ExamId\" " +
                "WHERE e.\"EvaluationId\" = ?";

        try (Connection connection = dbHelpers.getConnection(connectionUrl, user, password)) {
            System.out.println(String.format("Searching for EvaluationId %s in the DEEBilling table.", evaluationId));

            PreparedStatement statement = connection.prepareStatement(query);
            statement.setInt(1, evaluationId);
            JSONArray results = dbHelpers.getQueryResultsWithRetry(statement, 3, 2500);
            return results;
        } catch (Exception ex) {
            throw new Exception(String.format("Did not find EvaluationId %s in the DEEBilling table!", evaluationId));
        }
    }

    public JSONArray getExamStatusByEvaluationId(int evaluationId) throws SQLException, Exception {
        String query = "SELECT * FROM public.\"Exam\" e INNER JOIN public.\"ExamStatus\" es ON e.\"ExamId\" = es.\"ExamId\" WHERE e.\"EvaluationId\" = ?";

        try (Connection connection = dbHelpers.getConnection(connectionUrl, user, password)) {
            System.out.println(String.format("Searching for EvaluationId %s in the ExamStatus table.", evaluationId));

            PreparedStatement statement = connection.prepareStatement(query);
            statement.setInt(1, evaluationId);
            JSONArray results = dbHelpers.getQueryResultsWithRetry(statement, 48, 2500);
            return results;
        } catch (Exception ex) {
            throw new Exception(String.format("Did not find EvaluationId %s in the ExamStatus table!", evaluationId));
        }
    }

    public JSONArray getExamImagesByEvaluationId(int evaluationId) throws SQLException, Exception {
        String query = "SELECT * FROM public.\"ExamImage\" ei INNER JOIN public.\"Exam\" e ON ei.\"ExamId\" = e.\"ExamId\" WHERE e.\"EvaluationId\" = ?";

        try (Connection connection = dbHelpers.getConnection(connectionUrl, user, password)) {
            PreparedStatement statement = connection.prepareStatement(query);
            statement.setInt(1, evaluationId);
            JSONArray results = dbHelpers.getQueryResultsWithRetry(statement, 48, 2500);
            return results;
        } catch (Exception ex) {
            throw new Exception(String.format("Did not find EvaluationId %s in the ExamImage table!", evaluationId));
        }
    }

    public JSONArray getProviderPayResultsWithEvalId(int evaluationId) throws SQLException, Exception {
        String query = "SELECT * FROM public.\"ProviderPay\" pp " + 
        "INNER JOIN public.\"Exam\" dee ON dee.\"ExamId\" = pp.\"ExamId\"" +
        "WHERE \"EvaluationId\" = ?";

        try (Connection connection = dbHelpers.getConnection(connectionUrl, user, password)) {
            PreparedStatement statement = connection.prepareStatement(query);
            statement.setInt(1, evaluationId);
            JSONArray results = dbHelpers.getQueryResultsWithRetry(statement, 48, 2500);
            return results;
        } catch (Exception ex) {
            System.out.println(String.format("Did not find EvaluationId %s in the ProviderPay table!", evaluationId));
            return new JSONArray();
            // throw new Exception(String.format("Did not find EvaluationId %s in the ProviderPay table!", evaluationId));
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
        String query = "SELECT * FROM public.\"ExamStatusCode\" ORDER BY \"ExamStatusCodeId\"";
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
     * Get all Laterality details from LateralityCode table
     * 
     * @return
     * @throws SQLException
     * @throws Exception
     */
    public JSONArray getLateralityCodes() throws SQLException, Exception {
        String query = "SELECT * FROM public.\"LateralityCode\" ORDER BY \"LateralityCodeId\"";
        try (Connection connection = dbHelpers.getConnection(connectionUrl, user, password)) {
            System.out.println(String.format("Searching for codes in LateralityCode table."));
            PreparedStatement statement = connection.prepareStatement(query);
            JSONArray results = dbHelpers.getQueryResultsWithRetry(statement, 3, 2500);
            return results;
        } catch (Exception ex) {
            throw new Exception(String.format("Did not find any entry in LateralityCode table!"));
        }
    }

    /**
     * Get all Not Performed Reasons from NotPerformedReason table
     * 
     * @return
     * @throws SQLException
     * @throws Exception
     */
    public JSONArray getNotPerformedReasons() throws SQLException, Exception {
        String query = "SELECT * FROM public.\"NotPerformedReason\" ORDER BY \"NotPerformedReasonId\"";
        try (Connection connection = dbHelpers.getConnection(connectionUrl, user, password)) {
            System.out.println(String.format("Searching for reasons in NotPerformedReason table."));
            PreparedStatement statement = connection.prepareStatement(query);
            JSONArray results = dbHelpers.getQueryResultsWithRetry(statement, 3, 2500);
            return results;
        } catch (Exception ex) {
            throw new Exception(String.format("Did not find any entry in NotPerformedReason table!"));
        }
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
     * Returns the rowcount for a table. Validates that account used can select from table and there is data in the table.
     * 
     * @param tableName
     * @return
     * @throws SQLException
     * @throws Exception
     */
    public JSONArray checkTableRowCount(String tableName) throws SQLException, Exception {
        try (Connection connection = dbHelpers.getConnection(connectionUrl, user, password)) {
            System.out.println(String.format("Checking RowCount %s.", tableName));
            PreparedStatement statement = connection.prepareStatement("SELECT COUNT(1) FROM public.\"" + tableName + "\";");
            JSONArray results = dbHelpers.getQueryResultsWithRetry(statement, 2, 2500);
            return results;
        } catch (Exception ex) {
            throw new Exception(String.format("Cannot SELECT from %s table!", tableName));
        }
    }

    /**
     * Returns the permissions for a tableName.
     * 
     * @param tableName
     * @return
     * @throws SQLException
     * @throws Exception
     */
    public JSONArray checkTablePermissions(String tableName) throws SQLException, Exception {
        String query = "SELECT privilege_type FROM information_schema.role_table_grants WHERE TABLE_NAME = ? ORDER BY privilege_type;";
        try (Connection connection = dbHelpers.getConnection(connectionUrl, user, password)) {
            System.out.println(String.format("Checking Permissions %s.", tableName));
            PreparedStatement statement = connection.prepareStatement(query);
            statement.setString(1, tableName);
            JSONArray results = dbHelpers.getQueryResultsWithRetry(statement, 2, 2500);
            return results;
        } catch (Exception ex) {
            throw new Exception(String.format("Did not find permissions for %s table!", tableName));
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
            System.out.println(String.format("Searching for EvaluationId %s in the DEE exam table.", evaluationId));

            PreparedStatement statement = connection.prepareStatement(query);
            statement.setInt(1, evaluationId);
            results = dbHelpers.getQueryResultsWithRetry(statement, retryCount, delay);   
        } catch (Exception ex) {
            System.out.println(String.format("Did not find EvaluationId %s in the DEE Exam table! \n %s", evaluationId, ex));
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
                "WHERE e.\"EvaluationId\" = ? AND esc.\"Name\" = '"+statusStr+"'";
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
}