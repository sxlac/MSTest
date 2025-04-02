@pad
@envnot=prod
Feature: PAD Details Tests
# This test is here to cover various values that aren't really exercised through other tests

    Background:
        * eval if (env == 'prod') karate.abort();
        * def DataGen = function() { var DataGen = Java.type('helpers.data.DataGen'); return new DataGen(); }
        * def PadDb = function() { var PadDb = Java.type("helpers.database.pad.PadDb"); return new PadDb(); }
        * def timestamp = DataGen().isoTimestamp()
        * def dateStamp = DataGen().isoDateStamp()
        * def memberDetails = karate.call("classpath:helpers/member/createMember.js")
        * def appointment = karate.call('classpath:helpers/appointment/createAppointment.feature', { products: ['HHRA', 'PAD'] }).response
        * def evaluation = karate.call('classpath:helpers/eval/startEval.feature').response
        * def KafkaConsumerHelper = Java.type('helpers.kafka.KafkaConsumerHelper')
        
    @TestCaseKey=ANC-T697
    Scenario Outline: PAD Details are Correct in Kafka and Database
        * set evaluation.answers =
            """
            [
                {
                    "AnswerId": 29560,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "1"
                },
                {
                    "AnswerId": 29564,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "<left_float_result>"
                },
                {
                    "AnswerId": 30973,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "<right_float_result>"
                },
                {
                    "AnswerId": 22034,
                    "AnsweredDateTime": "#(dateStamp)",
                    "AnswerValue": "#(dateStamp)"
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

        # Verify patient details in the results
        * def padResult = PadDb().getResultsByEvaluationId(evaluation.evaluationId)[0]
        * match padResult == "#notnull"
        * match padResult.EvaluationId == evaluation.evaluationId
        * match padResult.MemberPlanId == memberDetails.memberPlanId
        * match padResult.MemberId == memberDetails.memberId
        * match padResult.CenseoId == memberDetails.censeoId
        * match padResult.AppointmentId == appointment.appointmentId
        * match padResult.ProviderId == providerDetails.providerId
        * match padResult.NationalProviderIdentifier == providerDetails.nationalProviderIdentifier
        * match padResult.ReceivedDateTime.toString() contains dateStamp
        * match padResult.FirstName == memberDetails.firstName
        * match padResult.MiddleName == memberDetails.middleName
        * match padResult.LastName == memberDetails.lastName

        # Verify the status from the database
        * def statusResults = PadDb().getPadStatusByEvaluationId(evaluation.evaluationId)
        * match statusResults[*].StatusCode contains 'PADPerformed'

        # Validate that the Kafka event has the expected billable status
        * json resultEvent = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("PAD_Results", evaluation.evaluationId + '', "ResultsReceived", 10, 5000))   
        * match resultEvent.ProductCode == "PAD"
        * match resultEvent.Results[*].Side contains "L"
        * match resultEvent.Results[*].Side contains "R"

        * json statusEvent = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("PAD_Status", evaluation.evaluationId + '', "Performed", 10, 5000))   
        * match statusEvent.ProductCode == "PAD"
        * match statusEvent.MemberPlanId == memberDetails.memberPlanId
        * match statusEvent.ProviderId == providerDetails.providerId

        Examples:
            | left_outcome | left_float_result | left_answer_result | left_normality | right_outcome | right_float_result | right_answer_result | right_normality |
            | 'Normal'     | 1                 | 31042              | 'N'            | 'Normal'      | 1.4                | 31047               | 'N'             |