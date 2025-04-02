@smoke-uacr
Feature: uACR Smoke Test

    Background:
        * def DataGen = function() { var DataGen = Java.type('helpers.data.DataGen'); return new DataGen(); }
        * def UacrDb = function() { var UacrDb = Java.type('helpers.database.uacr.UacrDb'); return new UacrDb(); }
        * def timestamp = DataGen().isoTimestamp()
        * def dateStamp = DataGen().isoDateStamp()
        * def KafkaConsumerHelper = Java.type('helpers.kafka.KafkaConsumerHelper')
        * def memberDetails = karate.call("classpath:helpers/member/createMember.js")
        * def appointment = karate.call('classpath:helpers/appointment/createAppointment.feature', { products: ['HHRA', 'UACR'] }).response
        * def evaluation = karate.call('classpath:helpers/eval/startEval.feature').response

    @TestCaseKey=ANC-T752
    Scenario: uACR Not Performed - Provider Unable to Perform
        * set evaluation.answers =
            """
            [
                {
                    "AnswerId": 52456,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "1"
                },
                {
                    "AnswerId": 52459,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": 0
                },
                {
                    "AnswerId": 52470,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "Provider unable to perform"
                },
                {
                    "AnswerId": 52472,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "Technical issue"
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

        * def result = UacrDb().getNotPerformedResultsByEvaluationId(evaluation.evaluationId)[0]
        # Verify not performed details
        * match result.AnswerId == 52472
        * match result.Reason == "Technical issue"
        # Verify member details 
        * match result.FirstName == memberDetails.firstName
        * match result.MiddleName == memberDetails.middleName
        * match result.LastName == memberDetails.lastName
        * match result.AddressLineOne == memberDetails.address.address1
        * match result.AddressLineTwo == memberDetails.address.address2
        * match result.City == memberDetails.address.city
        * match result.State == memberDetails.address.state
        * match result.ZipCode == memberDetails.address.zipCode
        * match result.MemberId == memberDetails.memberId
        * match result.CenseoId == memberDetails.censeoId
        * match result.MemberPlanId == memberDetails.memberPlanId
        # Verify provider details
        * match result.ProviderId == providerDetails.providerId
        * match result.NationalProviderIdentifier == providerDetails.nationalProviderIdentifier
        # Verify evaluation details
        * match result.EvaluationId == evaluation.evaluationId
        * match result.AppointmentId == appointment.appointmentId
        * match result.StatusName == "Exam Not Performed"
        * match result.StatusDateTime != null