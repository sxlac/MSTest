package helpers.endToEndHelpers;

import java.io.File;
import java.util.ArrayList;
import java.util.List;
import com.github.javafaker.Faker;
import java.io.FileWriter;
import com.opencsv.CSVWriter;
import jcifs.CIFSContext;
import java.time.Instant;
import java.time.OffsetDateTime;
import java.time.ZoneOffset;

import helpers.fileshare.CommonFileshareHelpers;



public class CsvOverreadHelpers {
    private static CommonEndToEndHelpers fileshareHelpers = new CommonEndToEndHelpers();
    private static Faker faker = new Faker();
    private static final String env = System.getProperty("karate.env").toUpperCase();
    private static final String baseDir = String.format("smb://censeohealth.com/dfs$/Private/Teams/DPS/%s/HomeAccess/Vendors/LetsGetChecked/KED/Pending", env);
    private static final String invalidDir = String.format("smb://censeohealth.com/dfs$/Private/Teams/DPS/%s/HomeAccess/Vendors/LetsGetChecked/KED/Invalid", env);
    private static final String completedDir = String.format("smb://censeohealth.com/dfs$/Private/Teams/DPS/%s/HomeAccess/Vendors/LetsGetChecked/KED/Complete", env);

    private static final String completeDirSpiro = String.format("smb://censeohealth.com/dfs$/Private/DPS/%s/HomeAccess/SpiroOverreadCsv/Complete", env);
    private static final String baseDirSpiro = String.format("smb://censeohealth.com/dfs$/Private/DPS/%s/HomeAccess/SpiroOverreadCsv/Pending", env);
    private static final String invalidDirSpiro = String.format("smb://censeohealth.com/dfs$/Private/DPS/%s/HomeAccess/SpiroOverreadCsv/Invalid", env);

    public void addFileToDirectory(String fileName, String Dir) throws Exception {
        String targetFilePath = String.format("%s/%s", Dir, fileName);
        CIFSContext context = fileshareHelpers.getClientContext();
        File source = new File(String.format("%s/src/test/java/helpers/fileshare/%s", System.getProperty("user.dir"), fileName));
        CommonFileshareHelpers CommonFileshareHelpers = new CommonFileshareHelpers();
        CommonFileshareHelpers.writeFile(context, targetFilePath, source);
        source.delete();
    }

    public void checkKedCsvMovedToInvalidFolder(String fileName, int retryCount, int sleepTime) throws Exception {
        checkFileMovedToCorrectFolder(fileName, retryCount, sleepTime, invalidDir, true);
    }

    public void checkSpiroCsvMovedToInvalidFolder(String fileName, int retryCount, int sleepTime) throws Exception {
        checkFileMovedToCorrectFolder(fileName, retryCount, sleepTime, invalidDirSpiro, true);
    }

    public void createAndDropSpiroCsvToPendingFolder(String fileName, boolean trainingMode, Integer appointmentInt) throws Exception {
        String appointment = appointmentInt.toString();
        writeValidSpiroCsvFile(fileName, trainingMode, appointment); 
        addSpiroCsvToPendingDirectory(fileName);
    }

    public void writeValidSpiroCsvFile(String fileName, boolean trainingMode, String appointment) throws Exception {

        writeSpiroCsvFile(fileName, appointment, trainingMode);
    }

    public void checkKedCsvMovedToCompleteFolder(String fileName, int retryCount, int sleepTime) throws Exception {
        checkFileMovedToCorrectFolder(fileName, retryCount, sleepTime, completedDir, true);
    }

    public void checkSpiroCsvMovedToCompleteFolder(String fileName, int retryCount, int sleepTime) throws Exception {
        checkFileMovedToCorrectFolder(fileName, retryCount, sleepTime, completeDirSpiro, true);
    }

    public void addKedCsvToPendingDirectory(String fileName) throws Exception {
        addFileToDirectory(fileName, baseDir);
    }

    public void addSpiroCsvToPendingDirectory(String fileName) throws Exception {
        addFileToDirectory(fileName, baseDirSpiro);
    }

    public void createAndDropKedCsvToPendingFolder(String fileName, String DateResultReported, Integer EvaluationId,
    String UacrUrineAlbuminToCreatinineRatioResultLabValue, String CreatinineResultLabValue, 
    String EstimatedGlomerularFiltrationRateResultLabValue, String DateLabReceived) 
     throws Exception {
        writeKedCsvFile(fileName, DateResultReported, EvaluationId, UacrUrineAlbuminToCreatinineRatioResultLabValue, CreatinineResultLabValue, EstimatedGlomerularFiltrationRateResultLabValue, DateLabReceived);
        addKedCsvToPendingDirectory(fileName);
    }

    private void writeKedCsvFile(String fileName, String DateResultReported, Integer EvaluationId,
    String UacrUrineAlbuminToCreatinineRatioResultLabValue, String CreatinineResultLabValue, 
    String EstimatedGlomerularFiltrationRateResultLabValue, String DateLabReceived) 
     throws Exception {

        File source = new File(String.format("%s/src/test/java/helpers/fileshare/%s", System.getProperty("user.dir"), fileName));
        List<String[]> csvData = createKedCsvFile(DateResultReported, EvaluationId, UacrUrineAlbuminToCreatinineRatioResultLabValue, CreatinineResultLabValue, EstimatedGlomerularFiltrationRateResultLabValue, DateLabReceived);
        try (CSVWriter writer = new CSVWriter(new FileWriter(source))) {
            writer.writeAll(csvData);
        }
    }

    private static List<String[]> createKedCsvFile(String DateResultReported, Integer EvaluationId, 
        String UacrUrineAlbuminToCreatinineRatioResultLabValue, String CreatinineResultLabValue, 
        String EstimatedGlomerularFiltrationRateResultLabValue, String DateLabReceived) {
        String[] header = {
            "ParticipantId",
            "FirstName",
            "MiddleName",
            "LastName",
            "DateofBirth",
            "Gender",
            "ProviderID",
            "ProviderFirstName",
            "ProviderLastName",
            "OrderingProviderNPI",
            "OrderingProviderFirstName",
            "OrderingProviderLastName",
            "PayorCode",
            "PolicyNumber",
            "PlanName",
            "GroupNumber",
            "LineOfBusiness",
            "PlanType",
            "SubPlanName",
            "ClientSubCode",
            "LocationId",
            "ParticipantIdentifier1",
            "SubscriberRelationshipToPatient",
            "DateLabReceived",
            "DateResultReported",
            "LGCBarcode",
            "TestCode",
            "UACR_(URINE_ALBUMIN_TO_CREATININE_RATIO)_ResultLabValue",
            "UACR_(URINE_ALBUMIN_TO_CREATININE_RATIO)_ResultLabUnit",
            "UACR_(URINE_ALBUMIN_TO_CREATININE_RATIO)_ResultDescription",
            "CREATININE_ResultLabValue",
            "CREATININE_ResultLabUnit",
            "CREATININE_ResultDescription",
            "ESTIMATED_GLOMERULAR_FILTRATION_RATE_ResultLabValue",
            "ESTIMATED_GLOMERULAR_FILTRATION_RATE_ResultLabUnit",
            "ESTIMATED_GLOMERULAR_FILTRATION_RATE_ResultDescription",
            "UACR_(URINE_ALBUMIN_TO_CREATININE_RATIO)_LOINC",
            "UACR_(URINE_ALBUMIN_TO_CREATININE_RATIO)_CPT",
            "UACR_(URINE_ALBUMIN_TO_CREATININE_RATIO)_CPT II",
            "CREATININE_LOINC",
            "CREATININE_CPT",
            "CREATININE_CPT II",
            "ESTIMATED_GLOMERULAR_FILTRATION_RATE_LOINC",
            "ESTIMATED_GLOMERULAR_FILTRATION_RATE_CPT",
            "ESTIMATED_GLOMERULAR_FILTRATION_RATE_CPT II",
        };
        String[] record = {
            String.format("%s", EvaluationId),
            "Valid",
            "R",
            "Us",
            "30/05/1958",
            "Gender",
            "ProviderID",
            "ProviderFirstName",
            "ProviderLastName",
            "OrderingProviderNPI",
            "OrderingProviderFirstName",
            "OrderingProviderLastName",
            "PayorCode",
            "PolicyNumber",
            "PlanName",
            "GroupNumber",
            "LineOfBusiness",
            "PlanType",
            "SubPlanName",
            "ClientSubCode",
            "LocationId",
            "ParticipantIdentifier1",
            "SubscriberRelationshipToPatient",
            String.format("%s", DateLabReceived),
            String.format("%s", DateResultReported),
            "LGCBarcode",
            "TestCode",
            String.format("%s", UacrUrineAlbuminToCreatinineRatioResultLabValue),
            "UACR_(URINE_ALBUMIN_TO_CREATININE_RATIO)_ResultLabUnit",
            "UACR_(URINE_ALBUMIN_TO_CREATININE_RATIO)_ResultDescription",
            String.format("%s", CreatinineResultLabValue),
            "CREATININE_ResultLabUnit",
            "CREATININE_ResultDescription",
            String.format("%s", EstimatedGlomerularFiltrationRateResultLabValue),
            "ESTIMATED_GLOMERULAR_FILTRATION_RATE_ResultLabUnit",
            "ESTIMATED_GLOMERULAR_FILTRATION_RATE_ResultDescription",
            "UACR_(URINE_ALBUMIN_TO_CREATININE_RATIO)_LOINC",
            "UACR_(URINE_ALBUMIN_TO_CREATININE_RATIO)_CPT",
            "UACR_(URINE_ALBUMIN_TO_CREATININE_RATIO)_CPT II",
            "CREATININE_LOINC",
            "CREATININE_CPT",
            "CREATININE_CPT II",
            "ESTIMATED_GLOMERULAR_FILTRATION_RATE_LOINC",
            "ESTIMATED_GLOMERULAR_FILTRATION_RATE_CPT",
            "ESTIMATED_GLOMERULAR_FILTRATION_RATE_CPT II",
        };
        List<String[]> list = new ArrayList<>();
        list.add(header);
        list.add(record);

        return list;
    }

    /*
    * Generates correct csv filename to check due to Async Job should add Year:month:day:hour:minute to filename
    */
    public String[] generateExpectedFilenameWithDate(String fileName, Boolean Csv) throws Exception {

        Instant instant = Instant.now();
        OffsetDateTime now = instant.atOffset(ZoneOffset.of("+00:00"));
        int year = now.getYear();

        int month = now.getMonthValue();
        String monthAsString = Integer.toString(month);
        if (month<10){ monthAsString = '0'+ monthAsString; };

        int day = now.getDayOfMonth();
        String dayAsString = Integer.toString(day);
        if (day<10){ dayAsString = '0'+ dayAsString; };

        int hour = now.getHour();
        String hourAsString = Integer.toString(hour);
        if (hour<10){ hourAsString = '0'+hourAsString; };


        int minute = now.getMinute();
        String minuteAsString = Integer.toString(minute);
        if (minute<10){ minuteAsString = '0'+minuteAsString; };

        // In some cases Async job takes more than a minute to process the file and as a result
        // naming of file can contain one minute plus than it was in time of sending file in Pending folder
        String plusOneMinute = Integer.toString(minute + 1);
        if (minute<10){ plusOneMinute = '0'+plusOneMinute; };


        System.out.println(String.format("%s%s%s%s%s", year, monthAsString, dayAsString, hourAsString, minuteAsString));
        if (Csv) {
            String expectedDateTime = "_" + year + monthAsString + dayAsString + hourAsString + minuteAsString + ".csv";
            String expectedDateTimePlusMinute = "_" + year + monthAsString + dayAsString + hourAsString + plusOneMinute + ".csv";
            String expectedCsvNaming = fileName.replaceAll(".csv", String.format("%s", expectedDateTime));
            String expectedCsvNamingPlusMinute = fileName.replaceAll(".csv", String.format("%s", expectedDateTimePlusMinute));
            String[] csvNamingArray = {expectedCsvNaming, expectedCsvNamingPlusMinute};
            return csvNamingArray;
        } else {
            String expectedDateTimeXlsx = "_" + year + monthAsString + dayAsString + hourAsString + minuteAsString + ".xlsx";
            String expectedDateTimePlusMinuteXlsx = "_" + year + monthAsString + dayAsString + hourAsString + plusOneMinute + ".xlsx";
            String expectedXlsxNaming = fileName.replaceAll(".xlsx", String.format("%s", expectedDateTimeXlsx));
            String expectedXlsxNamingPlusMinute = fileName.replaceAll(".xlsx", String.format("%s", expectedDateTimePlusMinuteXlsx));
            String[] XlsxNamingArray = {expectedXlsxNaming, expectedXlsxNamingPlusMinute};
            return XlsxNamingArray;
        }
    }

    /**
     * Waits for the file matching the provided filName to move out of the baseDir.
     */
    public void checkFileMovedToCorrectFolder(String fileName, int retryCount, int sleepTime, String Dir, Boolean Csv) throws Exception {

        String[] expectedCsvNaming = generateExpectedFilenameWithDate(fileName, Csv);
        CIFSContext context = fileshareHelpers.getClientContext();
        String filePath = String.format("%s/%s", Dir, expectedCsvNaming[0]);
        String filePathWIthDeviation = String.format("%s/%s", Dir, expectedCsvNaming[1]);
        CommonFileshareHelpers CommonFileshareHelpers = new CommonFileshareHelpers();
        System.out.println(String.format("Starting to check if file is present in correct dir %s ", filePath));
        for (int i = 0; i < retryCount; i++) {
            if (!CommonFileshareHelpers.fileExistsInDirectory(context, filePath))
                Thread.sleep(sleepTime);
            else 
                return;
        }
        for (int i = 0; i < retryCount; i++) {
            if (!CommonFileshareHelpers.fileExistsInDirectory(context, filePathWIthDeviation))
                Thread.sleep(sleepTime);
            else 
                return;
        }
        throw new Exception("File did not move");
    }

    private void writeSpiroCsvFile(String fileName, String randomValidAppointmentId, boolean trainingMode) throws Exception {

        File source = new File(String.format("%s/src/test/java/helpers/fileshare/%s", System.getProperty("user.dir"), fileName));
        List<String[]> csvData = createSpiroCSVFile(randomValidAppointmentId, String.valueOf(trainingMode));
        try (CSVWriter writer = new CSVWriter(new FileWriter(source))) {
            writer.writeAll(csvData);
        }
    }

    private static List<String[]> createSpiroCSVFile(String randomValidAppointmentId, String trainingMode) {
        String[] header = {

            "member_id", "appointment_id", "is_testing",
            "deeplink_callback_url", "provider_email", "profile_id",
            "first_name", "last_name", "date_of_birth",
            "height_in_ftin_at_time", "weight_in_lbs_at_time","session_id",
            "taken_at_tz_est", "fev1_measured_session_best", "fev1_percent_predicted_session_best",
            "fvc_measured_session_best", "fvc_percent_predicted_session_best", "fev1fvc_measured_session_best",
            "session_grading", "interpretation_restriction", "interpretation_obstruction",
            "number_tests", "overwriting_id", "fev1_measured_overwritten",
            "fvc_measured_overwritten", "fev1fvc_measured_overwritten", "is_obstruction",
            "overwritting_comments", "overwritten_on", "overwritten_by"
        };

        String randomFirstAndLastName = String.format("%s %s", faker.name().firstName(), faker.name().lastName());

        String[] record = {

            "", String.format("%s", randomValidAppointmentId), "f",
            String.format("signifyhome://spirometryResult?appointmentId=%s&isTraining=" + trainingMode, randomValidAppointmentId), faker.internet().emailAddress(), "b2dbd90b-11e1-4814-9b2a-7a44c4f20cb1",
            "", "", "",
            "5'5", "185", "20b480c7-e577-4dc3-99b7-399a3beba461",
            "2023-06-30 09:08:58.112588+00", "1.43", "59.25",
            "1.82", "58.8", "0.79",
            "D","Moderate restriction", "Normal","3",
            "47cd4cb1-3514-43fe-b636-a46e5b564ca9", "", "",
            "0.79", "no", String.format("Usable No obstruction %s, RRT, RPFT", randomFirstAndLastName),
            "2023-06-1 16:48:58.512588+00", String.format("%s, %s", randomFirstAndLastName, faker.internet().emailAddress())
    };

        List<String[]> list = new ArrayList<>();
        list.add(header);
        list.add(record);

        return list;
    }

}
