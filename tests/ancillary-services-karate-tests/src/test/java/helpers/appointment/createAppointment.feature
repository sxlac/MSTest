@ignore
Feature: Create Appointments
# Calling this feature file requires two things;
#   - Define memberDetails and call createMember.feature to fill out those details
#   - Pass in the product to book appointment 
#       Ex: karate.call('classpath:helpers/eval/startEval.feature'), { products: ['HHRA', 'PAD'] }
# If no products are passed in, all products defined in karate-config.js will be used

    Background:
        * def DataGen = function() { var DataGen = Java.type('helpers.data.DataGen'); return new DataGen(); }
        * def TimeSlot = Java.type('helpers.appointment.TimeSlot')
        * configure retry = { count: 90, interval: 1000 }

    * eval
    """
    // Skip if timeSlot is already defined so the appointment is booked with the desired timeSlot
    if (typeof timeSlot === 'undefined' ? TimeSlot.getTimeSlot() : timeSlot) {
        var timeSlot;
        // If not running in production use the getTimeSlot helper to get a unique timeSlot
        //TODO: Use availability api to get real-time provider availability instead
        if (env !== 'prod') {
            var timeSlot = Java.type('helpers.appointment.TimeSlot');
            timeSlot = timeSlot.getTimeSlot();
        } else {
            timeSlot = Java.type('helpers.data.DataGen').randomAppointmentWindow()
        }
    }
    """
    Scenario: Create Appointment
        Given url appointmentApi
        And path '/appointment'
        And request
            """
            {
                "startDateTime": "#(timeSlot.startDateTime)",
                "endDateTime": "#(timeSlot.endDateTime)",
                "address": {
                    "address1": "123 Main St.",
                    "address2": "Apt. 200",
                    "city": "Dallas",
                    "state": "TX",
                    "zipCode": "75244",
                    "location": {
                        "latitude": 1,
                        "longitude": 1
                    }
                },
                "phoneNumbers": [
                    "1234567890"
                ],
                "id": "#(DataGen().uuid())",
                "tentativeAppointmentId": 0,
                "primaryAppointmentPhone": "1234567890",
                "secondaryAppointmentPhone": "1234567890",
                "outreachId": "#(DataGen().uuid())",
                "notes": "Notes",
                "outreachNotes": "Outreach notes",
                "householdIdentifier": "#(DataGen().uuid())",
                "planId": 37,
                "memberPlanId": "#(memberDetails.memberPlanId)",
                "providerId": "#(providerDetails.providerId)",
                "startDate": "#(timeSlot.startDateTime)",
                "address1": "123 Main St.",
                "address2": "Apt. 200",
                "city": "Dallas",
                "state": "TX",
                "zipCode": "75244",
                "staticLocation": true,
                "isNearbyAddress": true,
                "userName": "gtestivus",
                "applicationId": "Postman",
                "products": #(products),
                "latitude": 1,
                "longitude": 1, 
                "locationTypeId": 1
            }
            """
        And retry until responseStatus == 200
        When method POST
        Then status 200