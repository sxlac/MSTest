@smoke-spirometry
Feature: Spirometry Smoke Tests

Background:
    * def DataGen = function() { var DataGen = Java.type('helpers.data.DataGen'); return new DataGen(); }
    * def Faker = function() { var Faker = Java.type('helpers.data.Faker'); return new Faker(); }
    * def SpirometryDb = function() { var SpirometryDb = Java.type("helpers.database.spirometry.SpirometryDb"); return new SpirometryDb(); }
    * def timestamp = DataGen().isoTimestamp()
    * def dateStamp = DataGen().isoDateStamp()
    
    * def memberDetails = karate.call("classpath:helpers/member/createMember.js")
    * def appointment = karate.call('classpath:helpers/appointment/createAppointment.feature', { products: ['HHRA', 'SPIROMETRY'] }).response
    * def evaluation = karate.call('classpath:helpers/eval/startEval.feature').response

Scenario: Spirometry Not Performed - Unable to Perform
    * def randomNotes = Faker().randomQuote()
    * set evaluation.answers =
        """
        [
            {
                'AnswerId': 50920,
                'AnsweredDateTime': '#(timestamp)',
                "AnswerValue": 'No'
            },
            {
                'AnswerId': 50922,
                'AnsweredDateTime': '#(timestamp)',
                'AnswerValue': 'Unable to perform'
            },
            {
                'AnswerId': 50929,
                'AnsweredDateTime': '#(timestamp)',
                'AnswerValue': 'Environmental issue'
            },
            {
                'AnswerId': 50927,
                'AnsweredDateTime': '#(timestamp)',
                'AnswerValue': #(randomNotes)
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
    * def result = SpirometryDb().getNotPerformedByEvaluationId(evaluation.evaluationId)[0]
    * match result.EvaluationId == evaluation.evaluationId
    * match result.MemberPlanId == memberDetails.memberPlanId
    * match result.ExamNotPerformedId != null
    * match result.NotPerformedReasonId == 6
    * match result.Notes == randomNotes
    * match result.CenseoId == memberDetails.censeoId
    * match result.AppointmentId == appointment.appointmentId
    * match result.ProviderId == providerDetails.providerId
