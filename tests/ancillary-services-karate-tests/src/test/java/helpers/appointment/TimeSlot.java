package helpers.appointment;

import java.time.LocalDateTime;
import java.util.HashMap;
import java.util.concurrent.atomic.AtomicInteger;

public class TimeSlot {
    private static AtomicInteger counter = new AtomicInteger(0); // Atomic counter to stop parallel threads from getting the same values
    static LocalDateTime now = LocalDateTime.now();
    static LocalDateTime initialDateTime = LocalDateTime.of(now.getYear(), now.getMonth(), now.getDayOfMonth(), 05, 30);

    /**
     * Increments and returns a time slot to be used for scheduling appointments
     * Serves 12 hours of 1/2 hour increments per day, and rolls back the day if 12 hours have been served
     * @return time slot to consume as a string
     */
    public static HashMap<String, String> getTimeSlot() {
        // If 12 hours have been returned, rollback one day and start over
        if (counter.intValue() >= 24) {
            initialDateTime = initialDateTime.minusDays(1);
            counter.set(0);
        }

        HashMap<String, String> timeSlot = new HashMap<String, String>();

        // Increment the counter and use it to set the appointment start time
        LocalDateTime startDateTime = initialDateTime.plusMinutes(counter.incrementAndGet() * 30);

        timeSlot.put("counter", counter.toString());
        timeSlot.put("startDateTime", String.valueOf(startDateTime).replace('T', ' '));
        timeSlot.put("endDateTime", String.valueOf(startDateTime.plusMinutes(30)).replace('T', ' '));

        return timeSlot;
    }
}