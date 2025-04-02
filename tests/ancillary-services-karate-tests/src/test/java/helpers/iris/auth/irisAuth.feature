Feature: IRIS Authentication

    Scenario: Get IRIS Token
        Given url iris.authUrl
        * header Content-Type = 'application/x-www-form-urlencoded'
        * def requestBody = `grant_type=client_credentials&client_id=${iris.clientId}&client_secret=${iris.secret}&scope=api://9affabc2-e53c-4378-a1b6-88ffdbb412fd/.default`
        And request requestBody
        When method POST
        Then status 200
        * def irisToken = response.access_token