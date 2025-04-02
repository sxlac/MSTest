@ignore
Feature: Cancel Appointments
# Calling this feature requires a pre-existing appointment with the information stored under a variable called appointment

    Background:
        * def DataGen = function() { var DataGen = Java.type('helpers.data.DataGen'); return new DataGen(); }
        * def timestamp = DataGen().isoTimestamp()
        * configure retry = { count: 5, interval: 1000 }

    Scenario: Cancel Appointment
        Given url appointmentApi
        And path `appointment/${appointment.appointmentId}/cancel`
        And request
        """
            {
                'reasonCode': 'Other',
                'reasonOther': 'Other',
                'signature': 'string',
                'createdDateTime': '#(timestamp)',
                'providerId': #(providerDetails.providerId),
                'applicationId': 'Dialer'
            }
        """
        And retry until responseStatus == 200
        When method POST
        Then status 200