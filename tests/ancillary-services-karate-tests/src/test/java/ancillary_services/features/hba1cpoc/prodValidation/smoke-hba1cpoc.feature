@smoke-hba1cpoc
Feature: HBA1CPOC smoke Tests

Background:
    * def DataGen = function() { var DataGen = Java.type('helpers.data.DataGen'); return new DataGen(); }
    * def Hba1cPOCDb = function() { var Hba1cpocDb = Java.type("helpers.database.hba1cpoc.Hba1cpocDb"); return new Hba1cpocDb(); }
    * def Faker = function() { var Faker = Java.type('helpers.data.Faker'); return new Faker(); }
    * def timestamp = DataGen().isoTimestamp()
    * def dateStamp = DataGen().isoDateStamp()

    * def memberDetails = karate.call("classpath:helpers/member/createMember.js")
    * def appointment = karate.call('classpath:helpers/appointment/createAppointment.feature', { products: ['HHRA', 'HBA1CPOC'] }).response
    * def evaluation = karate.call('classpath:helpers/eval/startEval.feature').response
    

Scenario: HBA1CPOC Not Performed - smoke Test
    * def randomNotes = Faker().randomQuote()
    * set evaluation.answers =
        """
        [
            {
                "AnswerId": 33071,
                "AnsweredDateTime": "#(timestamp)",
                "AnswerValue": 2
            },
            {
                "AnswerId": 33088,
                "AnsweredDateTime": "#(timestamp)",
                "AnswerValue": 'Member refused',
            },
            {
                "AnswerId": 33074,
                "AnsweredDateTime": "#(timestamp)",
                "AnswerValue": 'Member recently completed'
            },
            {
                "AnswerId": 33079,
                "AnsweredDateTime": "#(dateStamp)",
                "AnswerValue": #(randomNotes)
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

    # Validate that the database HBA1CPOC details are as expected using EvaluationId in HBA1CPOC
    * def result = Hba1cPOCDb().getResultsByEvaluationId(evaluation.evaluationId)[0]
    * def not_performed_result = Hba1cPOCDb().getNotPerformedResultsByEvaluationId(evaluation.evaluationId)[0]
    * match not_performed_result.EvaluationId == evaluation.evaluationId
    * match not_performed_result.MemberPlanId == memberDetails.memberPlanId
    * match not_performed_result.CenseoId == memberDetails.censeoId
    * match not_performed_result.AppointmentId == appointment.appointmentId
    * match not_performed_result.ProviderId == providerDetails.providerId
    * match not_performed_result.HBA1CPOCId != null
    * match not_performed_result.Notes == randomNotes

    # Validate the entry using EvaluationId in HBA1CPOC & HBA1CPOCStatus tables
    * def examStatusResults = Hba1cPOCDb().getExamStatusByEvaluationId(evaluation.evaluationId)
    # Status 7 = HBA1CPOCNotPerformed, Status 14 = BillRequestNotSent 
    * match examStatusResults[*].HBA1CPOCStatusCodeId contains 7 && 14
    
       