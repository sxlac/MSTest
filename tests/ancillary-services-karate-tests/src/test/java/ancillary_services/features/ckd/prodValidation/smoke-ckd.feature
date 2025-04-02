@smoke-ckd
Feature: CKD Smoke Tests
    Background:
        * def DataGen = function() { var DataGen = Java.type('helpers.data.DataGen'); return new DataGen(); }
        * def CkdDb = function() { var CkdDb = Java.type('helpers.database.ckd.CkdDb'); return new CkdDb(); }
        * def Faker = function() { var Faker = Java.type('helpers.data.Faker'); return new Faker(); }
        * def timestamp = DataGen().isoTimestamp()
        * def dateStamp = DataGen().isoDateStamp()
        * def memberDetails = karate.call("classpath:helpers/member/createMember.js")
        * def appointment = karate.call('classpath:helpers/appointment/createAppointment.feature', { products: ['HHRA', 'CKD'] }).response
        * def evaluation = karate.call('classpath:helpers/eval/startEval.feature').response
    
    Scenario: CKD Not Performed - Provider Unable to Perform
        * def randomNotes = Faker().randomQuote()
        * set evaluation.answers =
            """
            [
                {
                    'AnswerId': 20949,
                    'AnsweredDateTime': '#(timestamp)',
                    'AnswerValue': 'No'
                },
                {
                    'AnswerId': 30862,
                    'AnsweredDateTime': '#(timestamp)',
                    'AnswerValue': 'Unable to perform'
                },
                {
                    'AnswerId': 30872,
                    'AnsweredDateTime': '#(timestamp)',
                    'AnswerValue': 'No supplies or equipment'
                },
                {
                    'AnswerId': 30868,
                    'AnsweredDateTime': '#(timestamp)',
                    'AnswerValue': #(randomNotes)
                },
                {
                    'AnswerId': 22034,
                    'AnsweredDateTime': '#(dateStamp)',
                    'AnswerValue': '#(dateStamp)'
                }
            ]
            """
        * karate.call('classpath:helpers/eval/saveEval.feature')
        * karate.call('classpath:helpers/eval/stopEval.feature')
        * karate.call('classpath:helpers/eval/finalizeEval.feature')
        # Validate that the database CKD details are as expected using EvaluationId in CKD and ExamNotPerformed
        * def notPerformedResult = CkdDb().getNotPerformedResultsByEvaluationId(evaluation.evaluationId)[0]
        * match notPerformedResult.EvaluationId == evaluation.evaluationId
        * match notPerformedResult.MemberPlanId == memberDetails.memberPlanId
        * match notPerformedResult.CenseoId == memberDetails.censeoId
        * match notPerformedResult.AppointmentId == appointment.appointmentId
        * match notPerformedResult.ProviderId == providerDetails.providerId
        * match notPerformedResult.ExamNotPerformedId != null
        * match notPerformedResult.AnswerId == 30872
        * match notPerformedResult.Reason == 'No supplies or equipment'
        * match notPerformedResult.Notes == randomNotes
        # Validate the entry using EvaluationId in CKD & CKDStatus tables
        * def examStatusResults = CkdDb().getExamStatusByEvaluationId(evaluation.evaluationId)
        # Status 7 = CKDNotPerformed
        * match examStatusResults[*].CKDStatusCodeId contains only 7
