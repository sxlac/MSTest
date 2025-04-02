@ignore
Feature: Cancel Evaluation

    Background:
    * configure retry = { count: 7, interval: 1000 }

    Scenario: Cancel Evaluation
        * def changeDateTime = function() { var DataGen = Java.type('helpers.data.DataGen'); return new DataGen().isoTimestamp(); }

        Given url evaluationApi
        And path `evaluation/${evaluation.evaluationId}/status`
        And request
            """
            {
            "evaluationId": #(evaluation.evaluationId),
            "statusCode": "Cancel",
            "latitude": 32.925496267,
            "longitude": 32.925496267,
            "changeDateTime": "#(changeDateTime())",
            "userName": "karate",
            "applicationId": "virtus",
            "providerId": #(providerDetails.providerId)
            }
            """
        And retry until responseStatus == 200
        When method POST
        Then status 200 