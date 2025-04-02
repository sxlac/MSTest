Feature: Get Exam

    Background:
        * def DeeDb = function() { var DeeDb = Java.type("helpers.database.dee.DeeDb"); return new DeeDb(); }
        # Assuming that we have already created an exam to grade, get the exam ID from the IRIS side
        * def examId = DeeDb().getIrisExamIdFromDeeDb(evaluation.evaluationId)[0].DeeExamId

        # We need to override the oauth Signify token for these requests
        * def irisToken = karate.callSingle('classpath:helpers/iris/auth/irisAuth.feature').irisToken
        * karate.configure('headers', { Authorization: `Bearer ${irisToken}` })

    Scenario: Get Exam
        Given url `${iris.apiUrl}/exam/${examId}`
        When method GET
        Then status 200