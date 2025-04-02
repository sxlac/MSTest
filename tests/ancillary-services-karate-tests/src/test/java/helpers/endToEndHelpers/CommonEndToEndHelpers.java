package helpers.endToEndHelpers;

import java.io.File;
import java.io.FileOutputStream;
import java.util.Optional;
import jcifs.CIFSContext;
import jcifs.context.SingletonContext;
import jcifs.smb.NtlmPasswordAuthenticator;
import helpers.fileshare.CommonFileshareHelpers;

import java.util.LinkedHashMap;
import org.apache.poi.ss.usermodel.Row;
import org.apache.poi.xssf.usermodel.XSSFSheet;
import org.apache.poi.xssf.usermodel.XSSFWorkbook;

public class CommonEndToEndHelpers {

    /*
    * Creates a CIFSContext for getting and writing files on SMB.
    */
    public CIFSContext getClientContext() throws Exception {
        // SMB username and password can be set in appCOnfig.json or passed in via Kafka
        // Resulting in either a System Environment Variable or a System Property
    
        String smbUser = Optional.ofNullable(System.getenv("SMB_USERNAME")).orElse(System.getProperty("SMB_USERNAME"));
        String smbPassword = Optional.ofNullable(System.getenv("SMB_PASSWORD"))
                .orElse(System.getProperty("SMB_PASSWORD"));

        if (smbUser == null || smbPassword == null) {
            throw new Exception("The SMB username and password were not configured properly!" +
                    "\nIf running locally, ensure appConfig.json has an entry for username and password in the smb object." +
                    "\nIf running in Docker, ensure the SMB_USERNAME and SMB_PASSWORD environment variables are propertly set.");
        }

        CIFSContext context = SingletonContext.getInstance().withCredentials(new NtlmPasswordAuthenticator("censeohealth.com", smbUser, smbPassword));
        return context;
    }

    File createXlsxFile(String fileName, LinkedHashMap<String, String> fileContents, String sheetname) throws Exception {
        String filePath = "target/" + fileName;
        
        FileOutputStream fos = new FileOutputStream(new File(filePath));
        try (XSSFWorkbook workBook = new XSSFWorkbook()) {
            XSSFSheet sheet = workBook.createSheet(sheetname);

            int i = 0;
            Row headers = sheet.createRow(0);
            Row values = sheet.createRow(1);

            for (String key : fileContents.keySet()) {
                headers.createCell(i).setCellValue(key);
                values.createCell(i).setCellValue(fileContents.get(key));
                i++;
            }

            workBook.write(fos);
        }
        fos.close();

        return new File(filePath);
    }

    /**
     * Drops the provided source file onto the baseDir.
     * The filename provided will be used as the name of the file created.
     */
    void dropFileToDir(String fileName, File sourceFile, String Dir) throws Exception {
        String targetFilePath = String.format("%s/%s", Dir, fileName);
        CIFSContext context = getClientContext();
        CommonFileshareHelpers CommonFileshareHelpers = new CommonFileshareHelpers();
        CommonFileshareHelpers.writeFile(context, targetFilePath, sourceFile);
    }
}
