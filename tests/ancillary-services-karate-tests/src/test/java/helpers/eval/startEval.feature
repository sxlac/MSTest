@ignore
Feature: Start Evaluation

    Background:
    * configure retry = { count: 5, interval: 1000 }

    # formVersionId is set in karate-config.js since it has potential to change per environment. It can also be passed in as a parameter to the startEval feature call.
    Scenario: Start Evaluation
        * def startDateTime = function() { var DataGen = Java.type('helpers.data.DataGen'); return new DataGen().isoTimestamp(); }
        * def formVersionId = karate.get('formVersion', formVersionId)
        Given url evaluationApi
        And path 'evaluation'
        And request
            """
            {
                "formVersionId" : #(formVersionId),
                "appointmentId": #(appointment.appointmentId),
                "startDateTime": #(startDateTime()),
                "providerId": #(providerDetails.providerId),
                "memberPlanId": #(memberDetails.memberPlanId),
                "Longitude": 32.925496267,
                "Latitude": 96.84474318,
                "userName": "karate",
                "applicationId": "virtus"
            }
            """
        And retry until responseStatus == 201
        When method POST
        Then status 201