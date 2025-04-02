@hba1cpoc
@envnot=prod
Feature: HBA1CPOC Exam Performed 

    Background:
        * eval if (env == 'prod') karate.abort();
        * def DataGen = function() { var DataGen = Java.type('helpers.data.DataGen'); return new DataGen(); }
        * def Hba1cPOCDb = function() { var Hba1cpocDb = Java.type("helpers.database.hba1cpoc.Hba1cpocDb"); return new Hba1cpocDb(); }
        * def timestamp = DataGen().isoTimestamp()
        * def dateStamp = DataGen().isoDateStamp()
        * def expirationDate = DataGen().isoDateStamp(30)

        * def memberDetails = karate.call("classpath:helpers/member/createMember.js")
        * def appointment = karate.call('classpath:helpers/appointment/createAppointment.feature', { products: ['HHRA', 'HBA1CPOC'] }).response
        * def evaluation = karate.call('classpath:helpers/eval/startEval.feature').response
        * def KafkaConsumerHelper = Java.type('helpers.kafka.KafkaConsumerHelper')
        
    @TestCaseKey=ANC-T324
    Scenario Outline: HBA1CPOC - Performed
        * set evaluation.answers =
            """
            [
                {
                    "AnswerId": 33070,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": 1
                },
                {
                    "AnswerId": 33088,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": <answer_value>,
                },
                {
                    "AnswerId": 33264,
                    "AnsweredDateTime": '#(timestamp)',
                    "AnswerValue":  '#(expirationDate)',
                },
                {
                    "AnswerId": 22034,
                    "AnsweredDateTime": '#(dateStamp)',
                    "AnswerValue": '#(dateStamp)'
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
        * match result.EvaluationId == evaluation.evaluationId
        * match result.MemberPlanId == memberDetails.memberPlanId
        * match result.CenseoId == memberDetails.censeoId
        * match result.AppointmentId == appointment.appointmentId
        * match result.ProviderId == providerDetails.providerId
        * match result.A1CPercent == <answer_value>
        * match result.NormalityIndicator == <normality>
        * match result.ReceivedDateTime.toString() == dateStamp
        * match result.ExpirationDate.toString() == expirationDate
        * match result.DateOfBirth.toString() == memberDetails.dateOfBirth

        # Validate that the Kafka event details are as expected
        * json event = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("A1CPOC_Status", evaluation.evaluationId + '', "Performed", 10, 5000))          
        * match event.MemberPlanId == memberDetails.memberPlanId
        * match event.ProductCode == 'HBA1CPOC'

        # Validate that the Kafka events include the expected event headers
        * string headers = KafkaConsumerHelper.getHeadersByTopicAndKey("A1CPOC_Status", evaluation.evaluationId + '', 10, 5000)
        * match headers contains 'Performed'

        # Validate the entry using EvaluationId in HBA1CPOC & HBA1CPOCStatus tables
        * def examStatusResults = Hba1cPOCDb().getExamStatusByEvaluationId(evaluation.evaluationId)
        # Status 1 = HBA1CPOCPerformed 
        * match examStatusResults[*].HBA1CPOCStatusCodeId contains 1

        Examples:
            | answer_value | normality |
            | '3.9'        | 'A'       |
            | '4'          | 'N'       |
            | '6.9'        | 'N'       |