package helpers.fileshare;

import java.io.File;
import java.io.FileInputStream;
import java.util.Optional;
import jcifs.CIFSContext;
import jcifs.context.SingletonContext;
import jcifs.smb.NtlmPasswordAuthenticator;
import jcifs.smb.SmbFile;
import jcifs.smb.SmbFileInputStream;
import jcifs.smb.SmbFileOutputStream;

public class CommonFileshareHelpers {
    public CIFSContext getClientContext() throws Exception {
        // SMB username and password can be set in appCOnfig.json or passed in via Kafka
        // Resulting in either a System Environment Variable or a System Property
        String smbUser = Optional.ofNullable(System.getenv("SMB_USERNAME")).orElse(System.getProperty("SMB_USERNAME"));
        String smbPassword = Optional.ofNullable(System.getenv("SMB_PASSWORD")).orElse(System.getProperty("SMB_PASSWORD"));

        if (smbUser == null || smbPassword == null) {
            throw new Exception("The SMB username and password were not configured properly!" +
            "\nIf running locally, ensure appConfig.json has an entry for username and password in the smb object." +
            "\nIf running in Docker, ensure the SMB_USERNAME and SMB_PASSWORD environment variables are propertly set.");
        }

        CIFSContext context = SingletonContext.getInstance().withCredentials(new NtlmPasswordAuthenticator("censeohealth.com", smbUser, smbPassword));
        return context;
    }

    public void writeFile(CIFSContext context, String filepath, File sourceFile) throws Exception {
        // SMB resource will be the new file to write contents for
        SmbFile target = new SmbFile(filepath, context);
        FileInputStream fis = new FileInputStream(sourceFile);
        SmbFileOutputStream smbfos = new SmbFileOutputStream(target);

        // As long as there are contents to read from the input stream,
        // Write them to the SMB file using the SMB file output stream
        try {
            final byte[] b = new byte[1024];
            int read = 0;
            while ((read = fis.read(b, 0, b.length)) > 0) {
                smbfos.write(b, 0, read);
            }
        } finally {
            // Then close the streams when we are done
            fis.close();
            smbfos.close();
        }
    }

    public Boolean fileExistsInDirectory(CIFSContext context, String filePath) throws Exception
    {
            try (SmbFileInputStream smbfis = new SmbFileInputStream(filePath, context)) {
                final byte[] b = new byte[1024];
                // If the file contains literally anything, return true
                if ((smbfis.read(b, 0, b.length)) > 0) 
                    return true;
            } catch (Exception exception) {
                // If the file was not found return false
                if (exception.getMessage() == "The system cannot find the file specified.") {
                    return false;
                } else {
                    throw new Exception(exception.getMessage());
                }
            }
            
        System.err.println(String.format("The file never showed up at ", filePath));
        return false;
    }
}
