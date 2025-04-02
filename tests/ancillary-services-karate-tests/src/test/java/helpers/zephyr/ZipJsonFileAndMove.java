package helpers.zephyr;

import java.io.BufferedReader;
import java.io.File;
import java.io.FileInputStream;
import java.io.FileOutputStream;
import java.io.FileWriter;
import java.io.IOException;
import java.io.InputStreamReader;
import java.io.PrintWriter;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.StandardCopyOption;
import java.util.Optional;
import java.util.zip.ZipEntry;
import java.util.zip.ZipOutputStream;

import org.apache.http.HttpEntity;
import org.apache.http.HttpHeaders;
import org.apache.http.HttpResponse;
import org.apache.http.client.methods.HttpPost;
import org.apache.http.entity.ContentType;
import org.apache.http.entity.mime.MultipartEntityBuilder;
import org.apache.http.entity.mime.content.FileBody;
import org.apache.http.entity.mime.content.StringBody;
import org.apache.http.impl.client.CloseableHttpClient;
import org.apache.http.impl.client.HttpClients;
import org.json.JSONObject;


public class ZipJsonFileAndMove {

    public static final String ROOTDIR = System.getProperty("user.dir");
    public static final String APITOKEN = Optional.ofNullable(System.getenv("ZEPHYR_API_TOKEN")).orElse(System.getProperty("ZEPHYR_API_TOKEN"));
    public static final String APIURL = Optional.ofNullable(System.getenv("ZEPHYR_API_URL")).orElse(System.getProperty("ZEPHYR_API_URL"));
    public static final String TESTCYCLE = Optional.ofNullable(System.getenv("CREATE_TEST_CYCLE")).orElse(System.getProperty("CREATE_TEST_CYCLE"));
    public static final String TESTCYCLE_DESC = Optional.ofNullable(System.getenv("ZEPHYR_DESCRIPTION")).orElse(System.getProperty("ZEPHYR_DESCRIPTION"));
    public static final String COMMIT_ID = Optional.ofNullable(System.getenv("COMMIT_ID")).orElse(System.getProperty("COMMIT_ID"));
    public static final String ANCILLARY_SERVICE = Optional.ofNullable(System.getenv("ANCILLARY_SERVICE")).orElse(System.getProperty("ANCILLARY_SERVICE"));
    public static final String ENV = System.getProperty("karate.env");

    /** 
     * zip single or mulitipel JSON reports from targets folder
     * Returns the name of the file called zephyr.zip
     * @return String
     * @throws IOException
     */
    public static String zipJsonFiles() throws IOException {
            String dirPath = ROOTDIR + "/target/karate-reports/";
            String zipFile = "zephyr.zip";
            System.out.println(dirPath);

            File directory = new File(dirPath);
            File[] files = directory.listFiles((dir, name) -> name.toLowerCase().endsWith(".json"));

            if (files != null && files.length > 0) {
                try {
                    FileOutputStream fos = new FileOutputStream(zipFile);
                    ZipOutputStream zos = new ZipOutputStream(fos);

                    for (File file : files) {
                        addtoZip(file, zos);
                    }

                    zos.close();
                    fos.close();

                    System.out.println("Successfully zipped file");
                    return zipFile;
                } catch (Exception e) {
                    System.out.println(e);
                }
            } else {
                System.out.println("No JSON files found. Can't zip");
            }
            return zipFile + " not created!";
    }

    
    /** 
     * Private method which does the actual zipping of the file
     * @param file
     * @param zos
     * @throws Exception
     */
    private static void addtoZip(File file, ZipOutputStream zos) throws Exception {
        FileInputStream fis = new FileInputStream(file);

        ZipEntry zipEntry = new ZipEntry(file.getName());

        zos.putNextEntry(zipEntry);

        byte[] buffer = new byte[1024];
        int length;
        while ((length = fis.read(buffer)) >= 0) {
            zos.write(buffer, 0, length);
        }

        fis.close();
        zos.closeEntry();

    }

    /*
     * Move the zipped file from main project directory to helpers/zephyr directory
     */
    public static void fileMover() {
            try {
                Path sourcePath = Path.of(ROOTDIR + "/zephyr.zip");
                Path targetDir = Path.of(ROOTDIR + "/src/test/java/helpers/zephyr");
                Path targetPath = targetDir.resolve(sourcePath.getFileName());

                Files.move(sourcePath, targetPath, StandardCopyOption.REPLACE_EXISTING);

                System.out.println("Copy to zephyr folder successful");
            } catch (IOException e) {
                System.out.println(e.getMessage());
            }
    }

    /*
     * POST the zipped file to Zephyr endpoint and create a Test Cycle
     */
    public static boolean sendFileToZephyr() {
            String filePath = ROOTDIR + "/src/test/java/helpers/zephyr/zephyr.zip";
            
            if (TESTCYCLE.equals("true")) {
                try (CloseableHttpClient httpClient = HttpClients.createDefault()) {

                    HttpPost request = new HttpPost(APIURL);
                    request.setHeader(HttpHeaders.AUTHORIZATION, "Bearer " + APITOKEN);

                    File file = new File(filePath);
                    FileBody fileBody = new FileBody(file, ContentType.DEFAULT_BINARY);

                    JSONObject testCycleJson = new JSONObject();
                    //TODO Need to implement a way to generate meaningful Test Cycle Name
                    //Other fields can also be set to customise the Test Cycle
                    if (ENV.equals("uat")){
                        testCycleJson.put("name", "UAT Integration Automation Tests for Commit : "+COMMIT_ID+" ("+ANCILLARY_SERVICE+")");
                    }
                    else if (ENV.equals("prod")){
                        testCycleJson.put("name", "PROD Integration Automation Tests for Commit : "+COMMIT_ID+" ("+ANCILLARY_SERVICE+")");
                    }
                    testCycleJson.put("description", "Karate API Automation Test Cycle for : "+TESTCYCLE_DESC);
                    StringBody testCycleBody = new StringBody(testCycleJson.toString(), ContentType.APPLICATION_JSON);

                    HttpEntity multipartEntity = MultipartEntityBuilder
                                                .create()
                                                .addPart("file", fileBody)
                                                .addPart("testCycle", testCycleBody)
                                                .build();
                    
                    request.setEntity(multipartEntity);

                    HttpResponse response = httpClient.execute(request);
                    int statusCode = response.getStatusLine().getStatusCode();
                    BufferedReader rd = new BufferedReader(new InputStreamReader(
                        response.getEntity().getContent()));

                    String line = new String();
                    while ((line = rd.readLine()) != null) {
                        System.out.println(line);
                        if(line.contains("\"testCycle\"")){
                            JSONObject testCycle = new JSONObject(line);
                            String TestCycleKey = testCycle.getJSONObject("testCycle").get("key").toString();
                            String TestCycleId = testCycle.getJSONObject("testCycle").get("id").toString();
                            System.out.println("TestCycleKey from zephyr api response : "+TestCycleKey);
                            System.out.println("TestCycleId from zephyr api response : "+TestCycleId);
                            try {
                                FileWriter fileWriter = new FileWriter("target/zephyr.txt");
                                PrintWriter printWriter = new PrintWriter(fileWriter);
                                printWriter.print(TestCycleKey+","+TestCycleId);
                                printWriter.close();
                                fileWriter.close();
                            } catch (Exception e) {
                                throw new IllegalStateException("Failed to write TestCycleKey & TestCycleId to text file!", e);
                            }
                        }
                    }
                    if (statusCode == 200){
                        System.out.println("Zip file uploaded successfully and Test Cycle created");
                        return true;
                    } else {
                        System.out.println("File upload failed. Status code: " +statusCode);
                        return false;
                    }
                } catch(IOException e){
                    e.printStackTrace();
                }
            } 
            
            System.out.println("Zephyr Test Cycle not created. createTestCyle field set to " + TESTCYCLE);
            return false;
    }
}
