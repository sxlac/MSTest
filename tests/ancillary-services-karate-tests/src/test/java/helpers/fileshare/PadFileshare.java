package helpers.fileshare;

import java.io.File;
import java.time.LocalDate;
import jcifs.CIFSContext;
import java.time.LocalDateTime;
import java.time.ZoneOffset;

public class PadFileshare {
    private static CommonFileshareHelpers fileShareHelpers = new CommonFileshareHelpers();

    /**
     * Constants
     */
    private static final String env = System.getProperty("karate.env").toUpperCase();
    private static final String clientId = "14";
    private static final LocalDate now = LocalDate.now();
    private static final String year = String.valueOf(now.getYear());
    private static final String month = String.valueOf(now.getMonthValue());

    /**
     * Directories
     */
    private static final String baseDir = String.format("smb://censeohealth.com/dfs$/Private/DPS/%s/PAD/VendorResults", env);
    private static final String incomingDir = String.format("%s/Incoming/SemlerScientific", baseDir);
    private static final String pendingDir = String.format("%s/Pending/SemlerScientific", baseDir);
    private static final String processedDir = String.format("%s/Processed/SemlerScientific/ClientId/%s/%s/%s", baseDir, clientId, year, month);
    private static final String alreadyUploadedFailedDir = String.format("%s/Failed/FileAlreadyUploaded/SemlerScientific", baseDir);

    public static String getDirectoryPath(String dir) throws IllegalArgumentException {
        switch(dir.toUpperCase()) {
            case "INCOMING": return incomingDir;
            case "PENDING": return pendingDir;
            case "PROCESSED": return processedDir;
            case "ALREADY_UPLOADED_FAILED": return alreadyUploadedFailedDir;
            default: throw new IllegalArgumentException("Directory must be equal to INCOMING, PENDING, FAILED or PROCESSED");
        }
    }

    /**
     * Appends filename to the incoming directory, connects to it via SMB,
     * Then writes the file contents from SamplePADWaveform.pdf to the new file.
     * 
     * @param fileName
     */
    public void addPdfToIncomingDirectory(String fileName) throws Exception {
        //
        String targetFilePath = String.format("%s/%s", incomingDir, fileName);
        CIFSContext context = fileShareHelpers.getClientContext();
        File source = new File(String.format("%s/src/test/java/helpers/fileshare/pdfs/SamplePADWaveform.pdf", System.getProperty("user.dir")));
        fileShareHelpers.writeFile(context, targetFilePath, source);
    }

    /**
     * Polling wait mechanism to watch for an expected file to show up in the
     * provided directory.
     * 
     * @param fileName    - The full name of the file
     * @param directory   - The directory to check
     * @param retryCount  - The number of times to retry
     * @param waitSeconds - The time to wait between retries
     */
    public boolean verifyDocumentMovesToDirectory(String fileName, String dir, int retryCount, int waitMilliseconds) throws Exception {
        String dirPath = getDirectoryPath(dir);
        String filePath = String.format("%s/%s", dirPath, fileName);
        CIFSContext context = fileShareHelpers.getClientContext();

        System.out.println(String.format("Checking to see if pdf exists at path: %s", filePath));

        for (int i = 0; i < retryCount; i++) {
            if (fileShareHelpers.fileExistsInDirectory(context, filePath))
                return true;
            else
                Thread.sleep(waitMilliseconds);
        }

        System.err.println(String.format("The file %s never showed up in the %s directory!", fileName, dir));
        return false;
    }


    public String[] generateExpectedFilenameWithDate(String fileName) throws Exception {
        {

        LocalDateTime now = LocalDateTime.now(ZoneOffset.UTC);
        int year = now.getYear();

        int month = now.getMonthValue();
        String monthAsString = Integer.toString(month);
        if (month<10){ monthAsString = '0'+ monthAsString; };

        int day = now.getDayOfMonth();
        String dayAsString = Integer.toString(day);
        if (day<10){ dayAsString = '0'+ dayAsString; };

        LocalDateTime hour = now.minusHours(0);
        int corrected_hour = hour.getHour();
        String hourAsString = Integer.toString(corrected_hour);
        if (corrected_hour<10){ hourAsString = '0'+hourAsString; };


        int minute = now.getMinute();
        String minuteAsString = Integer.toString(minute);
        if (minute<10){ minuteAsString = '0'+minuteAsString; };

        // In some cases Async job takes more than a minute to process the file and as a result
        // naming of file can contain one minute plus than it was in time of sending file in Pending folder
        String plusOneMinute = Integer.toString(minute + 1);
        if (minute<10){ plusOneMinute = '0'+plusOneMinute; };


        System.out.println(String.format("%s%s%s%s%s", year, monthAsString, dayAsString, hourAsString, minuteAsString));

        String expectedDateTime = "_" + year + monthAsString + dayAsString + hourAsString + minuteAsString + ".PDF";
        String expectedDateTimePlusMinute = "_" + year + monthAsString + dayAsString + hourAsString + plusOneMinute + ".PDF";
        String expectedNaming = fileName.replaceAll(".PDF", String.format("%s", expectedDateTime));
        String expectedNamingPlusMinute = fileName.replaceAll(".PDF", String.format("%s", expectedDateTimePlusMinute));
        String[] NamingArray = {expectedNaming, expectedNamingPlusMinute};
        return NamingArray;
        }
    }

    /**
     * Waits for the file matching the provided filName to move out of the baseDir.
     */
    public void checkFileMovedToCorrectFolder(String fileName, int retryCount, int sleepTime, String Dir) throws Exception {

        String[] expectedCsvNaming = generateExpectedFilenameWithDate(fileName);
        CIFSContext context = fileShareHelpers.getClientContext();
        String filePath = String.format("%s/%s", Dir, expectedCsvNaming[0]);
        String filePathWIthDeviation = String.format("%s/%s", Dir, expectedCsvNaming[1]);
        for (int i = 0; i < retryCount; i++) {
            System.out.println(String.format("File is not present in correct dir %s Retry", filePath));
            if (!fileShareHelpers.fileExistsInDirectory(context, filePath))
                Thread.sleep(sleepTime);
            else 
                return;
        }
        for (int i = 0; i < retryCount; i++) {
            System.out.println(String.format("File is not present in correct dir %s Retry", filePathWIthDeviation));
            if (!fileShareHelpers.fileExistsInDirectory(context, filePathWIthDeviation))
                Thread.sleep(sleepTime);
            else 
                return;
        }
        throw new Exception("File did not move");
    }

    public void checkFileMovedToFailedFolder(String fileName, int retryCount, int sleepTime) throws Exception {
        checkFileMovedToCorrectFolder(fileName, retryCount, sleepTime, alreadyUploadedFailedDir);
    }
}