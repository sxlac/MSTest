package helpers.data;

import java.time.Instant;
import java.time.LocalDate;
import java.time.LocalDateTime;
import java.time.OffsetDateTime;
import java.time.ZoneId;
import java.time.ZoneOffset;
import java.time.ZonedDateTime;
import java.time.format.DateTimeFormatter;
import java.time.temporal.ChronoUnit;
import java.util.UUID;
import java.util.concurrent.ThreadLocalRandom;
import net.minidev.json.JSONObject;

import org.apache.commons.lang3.StringUtils;
import org.joda.time.DateTime;

import com.ibm.icu.text.SimpleDateFormat;


public class DataGen {

    /**
     * Returns the current UTC timestamp
     * 
     * @return Timestamp in (yyyy-MM-ddTHH:mm:ss.SSSZ) format
     */
    public String utcTimestamp() {
        return utcTimestamp(0);
    }

    /**
     * Returns the current UTC timestamp
     *
     * @param days
     * @return Timestamp in (yyyy-MM-ddTHH:mm:ss.SSSZ) format
     */
    public String utcTimestamp(int days) {
        return Instant.now().plus(days, ChronoUnit.DAYS).toString();
    }

    /**
     * Returns the current timestamp
     * 
     * @return Timestamp in ISO-8601 (yyyy-MM-ddTHH:mm:ss.SSS) format
     */
    public String isoTimestamp() {
        return isoTimestamp(0);
    }

    /**
     * Returns a timestamp adjusted using the provided plusDays
     * 
     * @param plusDays Add this number of days to the current date
     *                 Provide a negative integer to subtract days
     * @return Timestamp in ISO-8601 (yyyy-MM-ddTHH:mm:ss.SSS) format
     */
    public String isoTimestamp(int plusDays) {
        return LocalDateTime.now().plusDays(plusDays).toString();
    }

    /**
     * Returns a date stamp using the provided date format
     * 
     * @param pattern The pattern to apply to the current date time.
     *                Must be a valid format (ex: yyyy-MM-dd) or an error will be
     *                thrown.
     *                See {@link DateTimeFormatter#ofPattern(String)} for more
     *                information.
     * @return The currrent date in the pattern.
     */
    public String formattedDateStamp(String pattern) {
        return formattedDateStamp(0, pattern);
    }

    /**
     * Returns a date stamp using the provided date format
     * 
     * @param plusDays Add this number of days to the current date
     *                 Provide a negative integer to subtract days
     * @param pattern  The pattern to apply to the current date time.
     *                 Must be a valid format (ex: yyyy-MM-dd) or an error will be
     *                 thrown.
     *                 See {@link DateTimeFormatter#ofPattern(String)} for more
     *                 information.
     * @return The desired date in the desired pattern.
     */
    public String formattedDateStamp(int plusDays, String pattern) {
        DateTimeFormatter formatter;
        try {
            formatter = DateTimeFormatter.ofPattern(pattern);
        } catch (Exception ex) {
            System.err.println("Unable to create a DateTimeFormatter using th supplied format!");
            throw ex;
        }
        return formatter.format(LocalDate.now().plusDays(plusDays));
    }

    /**
     * Returns a date time stamp using the provided date format
     * 
     * @param pattern The pattern to apply to the current date time.
     *                Must be a valid format (ex: yyyy-MM-dd HH:MM:sss) or an error
     *                will be thrown.
     *                See {@link DateTimeFormatter#ofPattern(String)} for more
     *                information.
     * @return The currrent date in the pattern.
     */
    public String formattedDateTimeStamp(String pattern) {
        return formattedDateTimeStamp(0, pattern);
    }

    /**
     * Returns a date time stamp using the provided date format
     * 
     * @param plusDays Add this number of days to the current date
     *                 Provide a negative integer to subtract days
     * @param pattern  The pattern to apply to the current date time.
     *                 Must be a valid format (ex: yyyy-MM-dd) or an error will be
     *                 thrown.
     *                 See {@link DateTimeFormatter#ofPattern(String)} for more
     *                 information.
     * @return The desired date in the desired pattern.
     */
    public String formattedDateTimeStamp(int plusDays, String pattern) {
        DateTimeFormatter formatter;
        try {
            formatter = DateTimeFormatter.ofPattern(pattern);
        } catch (Exception ex) {
            System.err.println("Unable to create a DateTimeFormatter using th supplied format!");
            throw ex;
        }
        return formatter.format(LocalDateTime.now().plusDays(plusDays));
    }

    /**
     * Returns the current date stamp
     * 
     * @return Date stamp in ISO-8601 (yyyy-MM-dd) format
     */
    public String isoDateStamp() {
        return isoDateStamp(0);
    }

    /**
     * Returns a date stamp adjusted using the provided plusDays
     * 
     * @param plusDays Add this number of days to the current date
     *                 Provide a negative integer to subtract days
     * @return Date stamp in ISO-8601 (yyyy-MM-dd) format
     */
    public String isoDateStamp(int plusDays) {
        return LocalDate.now().plusDays(plusDays).toString();
    }

    /**
     * Returns the current day as a number
     * 
     * @return Current day in number representation
     */
    public String todayAsNumber() {
        return String.valueOf(LocalDate.now().getDayOfWeek().getValue());
    }

    /**
     * Returns a random appointment window 
     * 
     * @return Appointment window details as JSONObject
     */
    public static JSONObject randomAppointmentWindow() {
        JSONObject appointmentWindow = new JSONObject();

        int randomMinusDays = (int)Math.floor(Math.random() * 29);
        int randomHour = (int)Math.floor(Math.random() * 23);

        LocalDateTime startDateTime = LocalDateTime.now().minusDays(randomMinusDays).withHour(randomHour);
        LocalDateTime endDateTime = startDateTime.plusMinutes(15);

        appointmentWindow.put("startDateTime", startDateTime.toString());
        appointmentWindow.put("endDateTime", endDateTime.toString());

        return appointmentWindow;
    }

    /**
     * Generates a random UUID and converts to a String.
     * 
     * @return Random UUID as String
     */
    public String uuid() {
        return UUID.randomUUID().toString();
    }

    /**
     * Comapres 2 string DateTime that does not contain timezone info
     * 
     * @param dateTimeString1 First datetime as string
     * @param dateTimeString2 Second datetime as string
     * @return true/false based on the comparison
     */
    public boolean compareDatesString(String dateTimeString1, String dateTimeString2) {
        try {
            LocalDateTime dateTime1 = LocalDateTime.parse(dateTimeString1);
            LocalDateTime dateTime2 = LocalDateTime.parse(dateTimeString2);

            return dateTime1.isEqual(dateTime2);
        } catch (Exception ex) {
            System.err.println("DateTime in incorrect format! 1st date: " + dateTimeString1 + " and 2nd date: "
                    + dateTimeString2 + ". Exception message: " + ex.getMessage());
        }

        return false;
    }

    /**
     * Comapres 2 string UTC DateTime
     * 
     * @param dateTimeString1 First datetime as string
     * @param dateTimeString2 Second datetime as string
     * @return true/false based on the comparison
     */
    public boolean compareUtcDatesString(String dateTimeString1, String dateTimeString2) {
        try {
            Instant dateTime1 = Instant.parse(dateTimeString1);
            Instant dateTime2 = Instant.parse(dateTimeString2);

            return dateTime1.equals(dateTime2);
        } catch (Exception ex) {
            System.err.println("DateTime in incorrect format! 1st date: " + dateTimeString1 + " and 2nd date: "
                    + dateTimeString2 + ". Exception message: " + ex.getMessage());
        }

        return false;
    }

    /**
     * Comapres 2 string DateTime strings containing offsets after removing the milliseconds
     * 
     * @param dateTimeString1 First datetime as string
     * @param dateTimeString2 Second datetime as string
     * @return true/false based on the comparison
     */
    public boolean compareDatesStringWithOffset(String dateTimeString1, String dateTimeString2) {
        try {
            String date1 = dateTimeString1.split(".")[0];

            if (dateTimeString1.contains("+")) {
                date1 = date1 + dateTimeString1.split("(?=+)")[1];
            } else if (dateTimeString1.contains("-")) {
                date1 = date1 + dateTimeString1.split("(?=-)")[1];
            }

            String date2 = dateTimeString1.split(".")[0];

            if (dateTimeString2.contains("+")) {
                date2 = date2 + dateTimeString1.split("(?=+)")[1];
            } else if (dateTimeString2.contains("-")) {
                date2 = date2 + dateTimeString1.split("(?=-)")[1];
            }

            SimpleDateFormat format = new SimpleDateFormat("yyyy-MM-ddTHH:mm:ss zzz");
            java.util.Date parsedDate1 = format.parse(date1);
            java.util.Date parsedDate2 = format.parse(date2);

            return parsedDate1.equals(parsedDate2);
        } catch (Exception ex) {
            System.err.println("DateTime in incorrect format! 1st date: " + dateTimeString1 + " and 2nd date: "
                    + dateTimeString2 + ". Exception message: " + ex.getMessage());
        }

        return false;
    }

    /**
     * Returns a time stamp using the provided date format
     * 
     * @param pattern The pattern to apply to the current date time.
     *                Must be a valid format (ex: yyyy-MM-dd) or an error will be
     *                thrown.
     *                See {@link DateTimeFormatter#ofPattern(String)} for more
     *                information.
     * @return The currrent date in the pattern.
     */
    public String formattedTimeStamp(String pattern) {
        return formattedTimeStamp(0, pattern);
    }

    public String formattedTimeStamp(int plusDays, String pattern) {
        DateTimeFormatter formatter;
        try {
            formatter = DateTimeFormatter.ofPattern(pattern);
        } catch (Exception ex) {
            System.err.println("Unable to create a DateTimeFormatter using th supplied format!");
            throw ex;
        }
        return formatter.format(LocalDateTime.now().plusDays(plusDays));
    }

    /**
     * Get the UTC DateTime from a DateTime string and returns in
     * "uuuu-MM-dd'T'HH:mm:ssxxxxx" format. Ex: 2023-08-02T14:41:24+00:00
     * 
     * @param inputDate input datetime
     * @return
     */
    public String getUtcDateTimeString(String inputDate) {
        try {

            System.out.println("Input date: " + inputDate);
            inputDate = inputDate.replace(" ", "T");

            LocalDateTime local = LocalDateTime.parse(inputDate);
            ZonedDateTime zonedDateTime = ZonedDateTime.of(local, ZoneId.systemDefault());

            String outputPattern = "uuuu-MM-dd'T'HH:mm:ssxxxxx";
            DateTimeFormatter formatterWithZone = DateTimeFormatter.ofPattern(outputPattern);
            String formattedString = zonedDateTime.format(formatterWithZone);

            OffsetDateTime odt = OffsetDateTime.parse(formattedString, formatterWithZone);
            OffsetDateTime utcDateTime = odt.withOffsetSameInstant(ZoneOffset.UTC);
            String utcString = utcDateTime.format(formatterWithZone);
            System.out.println("Formatted UTC date with Zone: " + utcString);
            return utcString;

        } catch (Exception ex) {
            System.err.println("Input dateWithZone in incorrect format! Message: " + ex.getMessage());
        }
        return "";
    }

    /**
     * Removes the millisecond portion of a datetime string.
     * Often times, the millisecond precision is not defined.
     *
     * @param inputDate input datetime
     * @return
     */
    public String RemoveMilliSeconds(String input) {
        try {
            DateTimeFormatter formatter = DateTimeFormatter.ofPattern("uuuu-MM-dd'T'HH:mm:ssxxx");
            ZonedDateTime output = ZonedDateTime.parse(input);
            return output.format(formatter);

        } catch (Exception ex) {
            System.err.println("Input date in incorrect format! Message: " +
                    ex.getMessage());
        }
        return "";
    }

    /**
     * Returns a random Integer in a min and max range.
     * 
     * @param min random integer generated from min Intager param
     * @param max random integer generated to max Intager param
     *            See {@link ThreadLocalRandom#current() for more information.
     * @return Random Integer
     */
    public Integer randomInteger(Integer min, Integer max) {
        return ThreadLocalRandom.current().nextInt(min, max + 1);
    }

    /**
     * Case insensitive string comparison
     * 
     * @param first
     * @param second
     * @return
     */
    public boolean compareStrings(String first, String second) {
        return first.equalsIgnoreCase(second);
    }

    /**
     * Returns current UTC timestamp with Offset e.g.
     * 2023-10-05T09:33:51.310844+00:00.
     * If we use {@link #timestampWithOffset(String, int)} with "+00:00" it adds Z
     * instead of "+00:00".
     * 
     * @param days
     * @return
     */
    public String utcTimestampWithOffset(int days) {
        String offsetUtcDateTime = utcTimestamp(days).split("Z", 0)[0] + "+00:00";
        return offsetUtcDateTime;
    }

    /**
     * Returns current timestamp with Offset supplied. e.g.
     * 2023-10-05T04:33:17.105669-05:00
     * 
     * @param offsetId Offset in the format like "+01:00" or "-05:00"
     * @param days     Number of days to add
     * @return
     */
    public String timestampWithOffset(String offsetId, int days) {
        String offsetDateTime = OffsetDateTime
                .of(LocalDateTime.now(ZoneId.of(offsetId)).plusDays(days), ZoneOffset.of(offsetId))
                .toString();
        return offsetDateTime;
    }
    public String getMonthDayYear(String date) {
        String year = StringUtils.substring(date, 0, 4);
        String month = StringUtils.substring(date, 5, 7);
        String day = StringUtils.substring(date, 8, 10);
        String monthDayYear = month + "/" + day + "/" + year;

        return monthDayYear;
    }

    /**
     * Returns LGC barcode. e.g.
     * LGC-1234-1234-1234
     * 
     * @return String
     */
    public String GetLGCBarcode(){
        Faker faker = new Faker();
        return "LGC-"+faker.randomDigit(4)+"-"+faker.randomDigit(4)+"-"+faker.randomDigit(4);
    }

    /**
     * Returns invalid barcode. e.g.
     * CLG-1234-1234-1234
     * 
     * @return String
     */
    public String GetInvalidBarcode(){
        Faker faker = new Faker();
        return "CLG-"+faker.randomDigit(4)+"-"+faker.randomDigit(4)+"-"+faker.randomDigit(4);
    }

    /**
     * Returns alphacode barcode. e.g.
     * CLGDGJ
     * 
     * @return String
     */
    public String GetAlfacode(){
        Faker faker = new Faker();
        return ""+faker.randomChar()+faker.randomChar()+faker.randomChar()+faker.randomChar()+faker.randomChar()+faker.randomChar();
    }

    /**
     * Returns invalid alphacode barcode. e.g.
     * CLGDGJ1
     * 
     * @return String
     */
    public String GetInvalidAlfacode(){
        Faker faker = new Faker();
        return ""+faker.randomChar()+faker.randomChar()+faker.randomChar()+faker.randomChar()+faker.randomChar()+faker.randomChar()+faker.randomDigit(1);
    }

    /**
     * 
     * 
     * 
     * @return String
     */
    public String GetDateTimeUtc(){
        Instant instant = Instant.now();
        return instant.toString();
    }
}
