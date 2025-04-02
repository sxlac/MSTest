package helpers.database;

import java.sql.Connection;
import java.sql.DriverManager;
import java.sql.PreparedStatement;
import java.sql.ResultSet;
import java.util.Properties;

import net.minidev.json.JSONArray;
import net.minidev.json.JSONObject;

public class CommonDbHelpers {

    public Connection getConnection(String connectionUrl, String user, String password) throws Exception {
        if (connectionUrl.isEmpty() || user.isEmpty() || password.isEmpty()) {
            throw new Exception("Not all required connection details for the database query were provided!" +
                    "\nEnsure that DB_URL, DB_USERNAME, and DB_PASSWORD are set for the current environment in karate-config.js." +
                    "\nIf running in a Docker container, ensure the environment variables are propertly set.");
        }

        Properties props = new Properties();
        props.setProperty("user", user);
        props.setProperty("password", password);

        return DriverManager.getConnection(connectionUrl, props);
    }

    public JSONArray getQueryResultsWithRetry(PreparedStatement statement, int retryCount, int sleepMillis)throws Exception {
        JSONArray results = new JSONArray();

        for (int i = 0; i < retryCount; i++) {
            ResultSet resultSet = statement.executeQuery();

            if (!resultSet.isBeforeFirst()) {
                Thread.sleep(sleepMillis);
            } else {
                while (resultSet.next()) {
                    JSONObject result = new JSONObject();
                    int columns = resultSet.getMetaData().getColumnCount();
                    for (int j = 0; j < columns; j++) {
                        result.put(resultSet.getMetaData().getColumnLabel(j + 1), resultSet.getObject(j + 1));
                    }
                    results.add(result);
                }
                return results;
            }
        }
        throw new Exception("Did not find the expected results from the database.");
    }
}
