package helpers.fileshare;

import java.io.BufferedWriter;
import java.io.File;
import java.io.FileOutputStream;
import java.io.IOException;
import java.io.OutputStreamWriter;
import java.io.Writer;
import java.util.LinkedHashMap;
import java.util.Map;
import java.util.UUID;

import helpers.data.DataGen;
import jcifs.CIFSContext;

public class FobtFileshare {
    private static CommonFileshareHelpers fileShareHelpers = new CommonFileshareHelpers();
    private static DataGen dataGen = new DataGen();

    /**
     * Directories
     */
    private static final String env = System.getProperty("karate.env").toUpperCase();
    private static final String baseDir = String.format("smb://censeohealth.com/dfs$/Private/DPS/%s/HomeAccess", env);

    /**
     * Create a .txt file containing the FOBT lab results with expected headers and values.
     * Most values are hardcoded because they're not actually used.
     */
    public void createResultsFileWithDefaultValues(String fileName, Map<String, Object> memberDetails, UUID orderCorrelationId, String barcode, String labResults, String abnormalIndicator, String exception) throws Exception {

        // Map containing all of the key value pairs for the results file
        LinkedHashMap<String, String> fileContents = new LinkedHashMap<String, String>() {
            {
                put("accession_number", "7739819");
                put("sequence_number", "");
                put("patient_first_name", memberDetails.get("firstName").toString());
                put("patient_middle_name", memberDetails.get("middleName").toString());
                put("patient_last_name", memberDetails.get("lastName").toString());
                put("patient_address_line1", "4420 Harpers Ferry Dr");
                put("patient_address_line2", "Mysore");
                put("patient_city_name", "Grand Prairie");
                put("patient_state", "TX");
                put("patient_zip_code", "75052");
                put("patient_phone_number", "0000000000");
                put("patient_birth_date", memberDetails.get("dateOfBirth").toString());
                put("patient_gender", memberDetails.get("gender").toString());
                put("patient_ssnmember_id", "");
                put("hicn", "H7863896400");
                put("group_number", "H5216");
                put("patient_relationship_code", "");
                put("claim_paid_date", "");
                put("kit_id", barcode);
                put("collection_date", "01/07/2023");
                put("service_date", "01/11/2023");
                put("release_date", "01/12/2023");
                put("diagnosis_code1", "");
                put("diagnosis_code2", "");
                put("diagnosis_code3", "");
                put("diagnosis_code4", "");
                put("diagnosis_code5", "");
                put("vendor_lab_test_name", "Fecal Immunoassay Test");
                put("labid", "1306102678");
                put("labaddr", "2401 W Hassell Rd #1510");
                put("labcity", "Hoffman Estates");
                put("labst", "IL");
                put("labzip", "60169");
                put("loinc_cd", "29771-3");
                put("loinc_desc", "Hemoccult Stl Ql IA");
                put("lab_results", labResults);
                put("results_units", "");
                put("normals_low", "");
                put("normals_high", "");
                put("abnormal_ind", abnormalIndicator);
                put("exception", exception);
                put("component_cpt", "82274");
                put("program_name", "2020 Signify Health IHWA Test Kits");
                put("custom1", "H5216");
                put("custom2", "7093004663122");
                put("custom3", "");
                put("custom4", "271789847");
                put("custom5", "03113866");
                put("custom6", "C7658930");
                put("custom7", "143024376");
                put("custom8", "11");
                put("custom9", orderCorrelationId.toString());
                put("custom10", "");
            }
        };

        String keys = "";
        String values = "";

        // Create pipe delimited strings of keys and values
        for (Map.Entry<String, String> entry : fileContents.entrySet())
        {
            keys += entry.getKey() + "|";
            values += entry.getValue() + "|";
        };

        // Create a file in the target directory using the provided fileName
        String newFileDir = String.format("%s/target/%s", System.getProperty("user.dir"), fileName);
        try (Writer writer = new BufferedWriter(new OutputStreamWriter (new FileOutputStream(newFileDir), "utf-8"))) {
            writer.write(String.format("01|Home Access|%s", dataGen.formattedDateStamp("yyyyMMdd"))); // This line needs to be here but value doesn't really matter
            writer.write(System.lineSeparator());
            writer.write(keys);
            writer.write(System.lineSeparator());
            writer.write(values);
            writer.write(System.lineSeparator());
            writer.write("03|Home Access|26|26"); // This line needs to be here but value doesn't really matter
        } catch (IOException ex) {
            ex.printStackTrace();
        };

        System.out.println(String.format("File %s is written to %s for correlationId: %s", fileName, newFileDir, orderCorrelationId.toString()));
        // Add the file to the base directory on the fileshare
        String targetFilePath = String.format("%s/%s", baseDir, fileName);
        CIFSContext context = fileShareHelpers.getClientContext();
        File source = new File(newFileDir);
        fileShareHelpers.writeFile(context, targetFilePath, source);
    };

    /**
     * Waits for the file to move out of the base directory
     * Current Dev and UAT SFTPJob timer is every 60 seconds
     */
    public void waitForFileProcessing(String fileName) throws Exception
    {
        String filePath = String.format("%s/%s", baseDir, fileName); // Full SMB filepath
        CIFSContext context = fileShareHelpers.getClientContext();

        System.out.println(String.format("Waiting for file %s to process...", fileName));

        for (int i = 0; i < 75; i++) {
            if (!fileShareHelpers.fileExistsInDirectory(context, filePath)) {// Once the file is not found we're good to go
                System.out.println(String.format("File %s moved from path %s after %o Seconds", fileName, filePath, i));
                return;
            }
            else
                Thread.sleep(1000);
        }

        throw new Exception(String.format("The file %s didn't process in time!", fileName));
    }
}
