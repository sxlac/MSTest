@ignore
Feature: Create an appointment for the Demo

    Background: Create an appointment for the Demo Background
        * configure retry = { count: 45, interval: 1000 }
        * def DataGen = function() { var DataGen = Java.type('helpers.data.DataGen'); return new DataGen(); }
        * def Faker = function() { var Faker = Java.type('helpers.data.Faker'); return new Faker(); }
        * def TimeSlot = Java.type('helpers.appointment.TimeSlot')
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
        * def npi = ["9999383938"]
        * def products = ['HHRA', 'SPIROMETRY']
        * def address = 
        """
            {       
                "address1": "8771 Rexford Drive",
                "address2": "",
                "city": "Dallas",
                "state": "TX",
                "county": "Dallas",
                "zipCode": "75209",
                "location": {
                    "latitude": 1,
                    "longitude": 1
                }
            }
        """
        * def memberFirstName = "Della"
        * def memberLastName = "Ledner"
        * def memberDOB = "1960-06-24"
        * def censeoId = "X"+Faker().randomDigit(7)
        * print "CenseoId: "+censeoId
        
    Scenario: Create an appointment for the Demo
        # Get ProviderId by NPI
        Given url providerApi
        And path '/GetProvidersByNpi'
        And request npi
        And retry until responseStatus == 200
        When method POST
        Then status 200
        * def providerId = response[0].providerId
        
        # Create member 
        * def memberDetails =
            """
            {
                "planName": "#(planName)",
                "censeoId": "#(censeoId)",
                "gender": "M",
                "firstName": #(memberFirstName),
                "middleName": "",
                "lastName": #(memberLastName),
                "dateOfBirth": #(memberDOB),
                "emailAddress": "ahr@signifyhealth.com",
                "hicNumber": null,
                "preferredProviderGender": null,
                "preferredProviderLanguage": null,
                "address": #(address),
                "mbi": null,
                "subscriberId": "014245",
                "notes": null,
                "nextCallTime": "2020-04-16T16:51:42.1548254-05:00",
                "pcp_FirstName": "Shaun",
                "pcp_LastName": "Scott",
                "pcp_Address": null,
                "npi": null,
                "phoneNumbers": [
                    {
                        "phoneNumberType": "Mobile",
                        "phoneNumber": "4699995550"
                    }
                ],
                "memberProducts": #(products),
                "memberPlanStatus": "Active",
                "memberUpdateSource": "Gilenya",
                "phoneNumberUpdateSource": "Gilenya",
                "doNotCall": false,
                "censeoDoNotCall": false,
                "isDeafOrHardOfHearing": false,
                "requiresSignLanguage": false,
                "userName": "vastest1",
                "applicationId": "Postman",
                "externalId": "#(DataGen().uuid())",
                "householdIdentifier": null,
                "isValidAddress": true,
                "hPlan": "14254",
                "formId": 1,
                "memberId": null
            }
            """
        Given url memberApi + 'member/'
        And request memberDetails
        And retry until responseStatus == 200   
        When method POST
        Then status 200
        
        * set memberDetails.memberId = response.memberId
        * set memberDetails.memberPlanId = response.memberPlanId
        

        # Create Appointment
        Given url appointmentApi
        And path '/appointment'
        And request
            """
            {
                "startDateTime": "#(timeSlot.startDateTime)",
                "endDateTime": "#(timeSlot.endDateTime)",
                "address": #(address),
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
                "providerId": "#(providerId)",
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
        * print response