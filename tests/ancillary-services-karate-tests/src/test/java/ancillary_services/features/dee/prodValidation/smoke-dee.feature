@smoke-dee
Feature: DEE Smoke Tests

Background:
    * def DataGen = function() { var DataGen = Java.type('helpers.data.DataGen'); return new DataGen(); }
    * def DeeDb = function() { var DeeDb = Java.type("helpers.database.dee.DeeDb"); return new DeeDb(); }
    * def Faker = function() { var Faker = Java.type('helpers.data.Faker'); return new Faker(); }
    * def timestamp = DataGen().isoTimestamp()
    * def dateStamp = DataGen().isoDateStamp()

    * def memberDetails = karate.call("classpath:helpers/member/createMember.js")
    * def appointment = karate.call('classpath:helpers/appointment/createAppointment.feature', { products: ['HHRA', 'DEE'] }).response
    * def evaluation = karate.call('classpath:helpers/eval/startEval.feature').response

@TestCaseKey=ANC-T679
Scenario: DEE Not Performed - With Notes
    * def randomNotes = Faker().randomQuote()
    * set evaluation.answers =
        """
        [
            {
                'AnswerId': 29555,
                'AnsweredDateTime': '#(timestamp)',
                'AnswerValue': '2'
            },
            {
                'AnswerId': 28377,
                'AnsweredDateTime': '#(timestamp)',
                'AnswerValue': '#(memberDetails.firstName)'
            },
            {
                'AnswerId': 28378,
                'AnsweredDateTime': '#(timestamp)',
                'AnswerValue': '#(memberDetails.lastName)'
            },
            {
                'AnswerId': 30974,
                'AnsweredDateTime': '#(timestamp)',
                'AnswerValue': '#(memberDetails.gender)'
            },
            {
                'AnswerId': 28383,
                'AnsweredDateTime': '#(timestamp)',
                'AnswerValue': '#(memberDetails.address.state)'
            },
            {
                'AnswerId': 30951,
                'AnsweredDateTime': '#(dateStamp)',
                'AnswerValue': 'Environmental issue'
            },
            {
                'AnswerId': 30955,
                'AnsweredDateTime': '#(dateStamp)',
                'AnswerValue': #(randomNotes)
            }
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

    # Verify not performed details
    * def result = DeeDb().getNotPerformedResultsByEvaluationId(evaluation.evaluationId)[0]
    * match result.AnswerId == 30951
    * match result.Reason == 'Environmental issue'