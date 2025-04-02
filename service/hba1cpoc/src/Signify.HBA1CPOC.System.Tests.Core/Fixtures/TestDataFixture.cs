using Signify.QE.Core.Models.Availability;

namespace Signify.HBA1CPOC.System.Tests.Core.Fixtures;

public class TestDataFixture 
{
    private static readonly object TimeSlotLock = new();
    
    private static List<TimeSlot> _appointmentTimeSlots = CreateTimeSlots();

    /**
     * Creates an appointment.
     * Will use existing provider and member, and will create them if they do not yet exist.
     */
    public TimeSlot GetTimeSlot()
    {
        TimeSlot timeSlot;
        
        // Thread locked so that each test gets its own time slot
        // Gets the first non-consumed time slot and then marks it consumed so that the next thread doesn't get the same one
        lock (TimeSlotLock)
        {
            timeSlot = _appointmentTimeSlots.FirstOrDefault(obj => !obj.Consumed);
            timeSlot!.Consumed = true;
        }
        return timeSlot;
    }
    
    /**
     * Creates 30 days worth of 1/2 hour time slots from 9 AM to 5 PM.
     * These also contain a 'consumed' boolean that can be used to track which ones are used.
     * The timeslots will be used for creating appointments forcefully, without having to wait for provider availability.
     */
    private static List<TimeSlot> CreateTimeSlots()
    {
        var timeSlots = new List<TimeSlot>();
        var startDate = DateTime.Today;
        var startTime = new TimeSpan(9, 0, 0);
        var endTime = new TimeSpan(17, 0, 0);

        for (var day = 0; day < 30; day++)
        {
            var currentDay = startDate.AddDays(day);
            var currentSlotStartTime = currentDay.Add(startTime);

            while (currentSlotStartTime.TimeOfDay < endTime)
            {
                var timeSlot = new TimeSlot
                {
                    StartDateTime = currentSlotStartTime,
                    EndDateTime = currentSlotStartTime.AddMinutes(30)
                };
                timeSlots.Add(timeSlot);
                currentSlotStartTime = currentSlotStartTime.AddMinutes(30);
            }
        }

        return timeSlots;
    }
}