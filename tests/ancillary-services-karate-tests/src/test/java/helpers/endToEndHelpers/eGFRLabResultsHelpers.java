package helpers.endToEndHelpers;

import java.io.File;
import java.util.LinkedHashMap;

import org.apache.commons.lang.StringUtils;



public class eGFRLabResultsHelpers {
    private static CsvOverreadHelpers OverreadFileshareHelpers = new CsvOverreadHelpers();
    // private static HomeAccessFileshare HomeAccess = new HomeAccessFileshare();
    private static CommonEndToEndHelpers fileshareHelpers = new CommonEndToEndHelpers();
    /**
     * Directories
     */
    private static final String env = System.getProperty("karate.env").toUpperCase();
    
    private static final String PendingDir = String.format("smb://censeohealth.com/dfs$/Private/DPS/%s/HomeAccess/Quest/eGFRLabResults/Pending", env);
    private static final String CompleteDir = String.format("smb://censeohealth.com/dfs$/Private/DPS/%s/HomeAccess/Quest/eGFRLabResults/Complete", env);


    public void createAndDropEGFRLabResultsToPendingFolder(String fileName, String SVC_AccessionedDate, String SVC_CollectionDate, String MBR_SubscriberID, String sheetname, String CMP_eGFRResult) throws Exception {
        LinkedHashMap<String, String> fileContents = createEGFRLabResults(SVC_AccessionedDate, SVC_CollectionDate, MBR_SubscriberID, CMP_eGFRResult); 
        File EGFRFile = fileshareHelpers.createXlsxFile(fileName, fileContents, sheetname);
        fileshareHelpers.dropFileToDir(fileName, EGFRFile, PendingDir);
    }
    /**
     * Path in for creating and dropping a eGFR Lab Results file.
     */

    public void checkEGFRMovedToCompleteFolder(String fileName, int retryCount, int sleepTime) throws Exception {
        OverreadFileshareHelpers.checkFileMovedToCorrectFolder(fileName, retryCount, sleepTime, CompleteDir, false);
    }

    private LinkedHashMap<String, String> createEGFRLabResults(String SVC_AccessionedDate, String SVC_CollectionDate, String MBR_SubscriberID, String CMP_eGFRResult) {

        final LinkedHashMap<String, String> fileContents = new LinkedHashMap<String, String>() {
            {
                put("SVC_ID", "888888888");
                put("MBR_LOB", "Medicare");
                put("MBR_SubscriberID", MBR_SubscriberID);
                put("MBR_FirstName", "Joy");
                put("MBR_MiddleName", "D");
                put("MBR_LastName", "Burns");
                put("MBR_Gender", "F");
                put("Age", "75");
                put("MBR_BirthDate", "29/01/1947");
                put("MBR_AddressLine1", "4321 Ferry Street");
                put("MBR_AddressLine2", "");
                put("MBR_City", "Huntsville");
                put("MBR_Zip","35816");
                put("MBR_State", "AL");
                put("MBR_MailType", "DirectMail");
                put("SVC_MailDate", "01/08/2022");
                put("SVC_eGFR", "Y");
                put("SVC_QuestAccessionNumber", "K12344431");
                put("SVC_CollectionDate",  SVC_CollectionDate);
                put("SVC_AccessionedDate",SVC_AccessionedDate);
                put("Abnormal_eGFR", "N");
                put("CMP_eGFRResult", CMP_eGFRResult);
                put("CMP_CreatinineResult", "1.27");
            }
        };
        return fileContents;
    }

    public String changeFormatForExcelFile(String date) throws Exception {
        String year = StringUtils.substring(date, 0, 4);
        String month = StringUtils.substring(date, 5, 7);
        String day = StringUtils.substring(date, 8, 10);
        String excelFormat = month + "/" + day + "/" + year;
        return excelFormat;
    }
}
