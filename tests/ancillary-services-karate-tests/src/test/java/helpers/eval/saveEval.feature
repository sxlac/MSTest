@ignore
Feature: Save Evaluation

    Background:
    * configure retry = { count: 10, interval: 1000 }

    Scenario: Save Evaluation
        Given url evaluationApi
        And path `evaluation/${evaluation.evaluationId}/answer`
        And request
            """
            {
                "Longitude": 32.925496267,
                "Latitude": 96.84474318,
                "ApplicationId": "virtus",
                "Answers": #(evaluation.answers)
            }
            """
        And retry until responseStatus == 201
        When method POST
        Then status 201