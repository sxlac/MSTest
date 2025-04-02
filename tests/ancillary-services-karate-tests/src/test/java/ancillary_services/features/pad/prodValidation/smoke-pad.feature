@smoke-pad
Feature: PAD Smoke Tests

Background:
    * def DataGen = function() { var DataGen = Java.type('helpers.data.DataGen'); return new DataGen(); }
    * def PadDb = function() { var PadDb = Java.type('helpers.database.pad.PadDb'); return new PadDb(); }
    * def Faker = function() { var Faker = Java.type('helpers.data.Faker'); return new Faker(); }
    * def timestamp = DataGen().isoTimestamp()
    * def dateStamp = DataGen().isoDateStamp()
    * def memberDetails = karate.call("classpath:helpers/member/createMember.js")
    * def appointment = karate.call('classpath:helpers/appointment/createAppointment.feature', { products: ['HHRA', 'PAD'] }).response
    * def evaluation = karate.call('classpath:helpers/eval/startEval.feature').response
    
@TestCaseKey=ANC-T410
Scenario: PAD Not Performed Smoke Test
    * def reasonNotes = Faker().randomQuote()
    * set evaluation.answers =
        """
        [
            {
                'AnswerId': 29561,
                'AnsweredDateTime': '#(timestamp)',
                'AnswerValue': '2'
            },
            {
                'AnswerId': 30958,
                'AnsweredDateTime': '#(timestamp)',
                'AnswerValue': 'Unable to perform'
            },
            {
                'AnswerId': 30967,
                'AnsweredDateTime': '#(timestamp)',
                'AnswerValue': 'Environmental issue'
            },
            {
                'AnswerId': 30971,
                'AnsweredDateTime': '#(timestamp)',
                'AnswerValue': #(reasonNotes)
            },
            {
                'AnswerId': 22034,
                'AnsweredDateTime': '#(dateStamp)',
                'AnswerValue': '#(dateStamp)'
            },
            {
                "AnswerId": 33445,
                "AnsweredDateTime": "#(timestamp)",
                "AnswerValue": "#(timestamp)"
            },
            {
                "AnswerId": 21989,
                "AnsweredDateTime": "#(timestamp)",
                "AnswerValue": "#(providerDetails.signature)"
            },
            {
                "AnswerId": 28386,
                "AnsweredDateTime": "#(timestamp)",
                "AnswerValue": "#(providerDetails.firstName) #(providerDetails.lastName)"
            },
            {
                "AnswerId": 28387,
                "AnsweredDateTime": "#(timestamp)",
                "AnswerValue": "#(providerDetails.nationalProviderIdentifier)"
            },
            {
                "AnswerId": 22019,
                "AnsweredDateTime": "#(timestamp)",
                "AnswerValue": "#(providerDetails.degree)"
            }
        ]
        """

    * karate.call('classpath:helpers/eval/saveEval.feature')
    * karate.call('classpath:helpers/eval/stopEval.feature')
    * karate.call('classpath:helpers/eval/finalizeEval.feature')

    # Validate that the database details are as expected
    * def result = PadDb().getNotPerformedResultsByEvaluationId(evaluation.evaluationId)[0]
    * match result.LeftScoreAnswerValue == null
    * match result.LeftSeverityAnswerValue == null
    * match result.RightScoreAnswerValue == null
    * match result.RightSeverityAnswerValue == null
    * match result.NotPerformedId != null
    * match result.AnswerId == 30967
    * match result.Notes == reasonNotes

    # Verify the status from the database
    * def statusResults = PadDb().getPadStatusByEvaluationId(evaluation.evaluationId)
    * match statusResults[*].StatusCode contains 'PADNotPerformed'