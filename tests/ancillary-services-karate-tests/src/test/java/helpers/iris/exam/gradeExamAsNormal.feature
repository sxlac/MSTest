Feature: Grade Exam as Normal

    Background:
        # We need to override the oauth Signify token for these requests
        * def irisToken = karate.callSingle('classpath:helpers/iris/auth/irisAuth.feature').irisToken
        * karate.configure('headers', { Authorization: `Bearer ${irisToken}` })

    Scenario: Grade Exam as Normal
        Given url `${iris.apiUrl}/exam/${exam.examId}/gradeasnormal`
        And header Content-Length = 0
        When method PUT
        Then status 200