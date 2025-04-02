@hba1cpoc
@envnot=prod
Feature: HBA1CPOC NotPerformed

    Background:
        * eval if (env == 'prod') karate.abort();
        * def DataGen = function() { var DataGen = Java.type('helpers.data.DataGen'); return new DataGen(); }
        * def Hba1cPOCDb = function() { var Hba1cpocDb = Java.type("helpers.database.hba1cpoc.Hba1cpocDb"); return new Hba1cpocDb(); }
        * def Faker = function() { var Faker = Java.type('helpers.data.Faker'); return new Faker(); }
        * def timestamp = DataGen().isoTimestamp()
        * def dateStamp = DataGen().isoDateStamp()

        * def memberDetails = karate.call("classpath:helpers/member/createMember.js")
        * def appointment = karate.call('classpath:helpers/appointment/createAppointment.feature', { products: ['HHRA', 'HBA1CPOC'] }).response
        * def evaluation = karate.call('classpath:helpers/eval/startEval.feature').response
        * def KafkaConsumerHelper = Java.type('helpers.kafka.KafkaConsumerHelper')
        
    @TestCaseKey=ANC-T314
    Scenario Outline: HBA1CPOC Not Performed - Member refused
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
                    "AnswerId": <answer_id>,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": <answer_value>,
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

        # Validate that the Kafka event details are as expected
        * json event = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("A1CPOC_Status", evaluation.evaluationId + '', "NotPerformed", 10, 5000))      
        * match event.Reason == <expected_reason>
        * match event.ProductCode == 'HBA1CPOC'
        * match event.ReasonNotes == randomNotes
        * match event.ReasonType == 'Member refused'

         # Validate that the Kafka events include the expected event headers
        * string headers = KafkaConsumerHelper.getHeadersByTopicAndKey("A1CPOC_Status", evaluation.evaluationId + '', 5, 5000)  
        * match headers contains 'NotPerformed'

        # Validate the entry using EvaluationId in HBA1CPOC & HBA1CPOCStatus tables
        * def examStatusResults = Hba1cPOCDb().getExamStatusByEvaluationId(evaluation.evaluationId)
        # Status 7 = HBA1CPOCNotPerformed, Status 14 = BillRequestNotSent 
        * match examStatusResults[*].HBA1CPOCStatusCodeId contains 7 && 14

        # Validate that the Kafka event details are as expected
        * json nonBillablevent = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("A1CPOC_Status", evaluation.evaluationId + '', "BillRequestNotSent", 10, 5000))      
        * match nonBillablevent.BillingProductCode == 'HBA1CPOC'
        * match nonBillablevent.ProductCode == 'HBA1CPOC'
        * match nonBillablevent.EvaluationId == evaluation.evaluationId
        * match nonBillablevent.MemberPlanId == memberDetails.memberPlanId
        * match nonBillablevent.ProviderId == providerDetails.providerId
        * match nonBillablevent.CreateDate contains dateStamp
        * match nonBillablevent.ReceivedDate contains dateStamp

        Examples:
            | answer_id | answer_value                                     | expected_reason             |
            | 33074     | 'Member recently completed'                      | 'Member recently completed' | 
            | 33075     | 'Scheduled to complete'                          | 'Scheduled to complete'     |
            | 33076     | 'Member apprehension'                            | 'Member apprehension'       |
            | 33077	    | 'Not interested'                                 | 'Not interested'            |
            | 33078     | 'Other'                                          | 'Other'                     |

        @TestCaseKey=ANC-T313
        Scenario Outline: HBA1CPOC Not Performed - Unable to perform
            * def randomNotes = Faker().randomQuote()
            * set evaluation.answers =
                """
                [
                    {
                        "AnswerId": 21112,
                        "AnsweredDateTime": "#(timestamp)",
                        "AnswerValue": 'No'
                    },
                    {
                        "AnswerId": 30878,
                        "AnsweredDateTime": "#(timestamp)",
                        "AnswerValue": 'Unable to perform'
                    },
                    {
                        "AnswerId": <answer_id>,
                        "AnsweredDateTime": "#(timestamp)",
                        "AnswerValue": <answer_value>
                    },
                    {
                        "AnswerId": 33086,
                        "AnsweredDateTime": "#(dateStamp)",
                        "AnswerValue": #(randomNotes)
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
    
            # Validate that the database FOBT details are as expected using EvaluationId in HBA1CPOC and HBA1CPOCNotPerformed
            * def result = Hba1cPOCDb().getResultsByEvaluationId(evaluation.evaluationId)[0]
            * def not_performed_result = Hba1cPOCDb().getNotPerformedResultsByEvaluationId(evaluation.evaluationId)[0]
            * match not_performed_result.EvaluationId == evaluation.evaluationId
            * match not_performed_result.MemberPlanId == memberDetails.memberPlanId
            * match not_performed_result.CenseoId == memberDetails.censeoId
            * match not_performed_result.AppointmentId == appointment.appointmentId
            * match not_performed_result.ProviderId == providerDetails.providerId
            * match not_performed_result.HBA1CPOCNotPerformedId != null
            * match not_performed_result.Notes == randomNotes
    
            # Validate that the Kafka event details are as expected
            * json event = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("A1CPOC_Status", evaluation.evaluationId + '', "NotPerformed", 10, 5000))      
            * match event.Reason == <expected_reason>
            * match event.ProductCode == 'HBA1CPOC'
            * match event.ReasonNotes == randomNotes
            * match event.ReasonType == 'Unable to perform'

            # Validate the entry using EvaluationId in HBA1CPOC & HBA1CPOCStatus tables
            * def examStatusResults = Hba1cPOCDb().getExamStatusByEvaluationId(evaluation.evaluationId)
            # Status 7 = HBA1CPOCNotPerformed, Status 14 = BillRequestNotSent 
            * match examStatusResults[*].HBA1CPOCStatusCodeId contains 7 && 14
            Examples:
            | answer_id | answer_value                                                     | expected_reason                                                          |
            | 33081     | 'Technical issue - please call Mobile Support at 877 570-9359)'  | 'Technical issue'                                                        |
            | 33082     | 'Environmental issue'                                            | 'Environmental issue'                                                    |
            | 33083     | 'No supplies or equipment'                                       | 'No supplies or equipment'                                               |
            | 33084     | 'Insufficient training'                                          | 'Insufficient training'                                                  |
            | 50905     | 'Member physically unable'                                       | 'Member physically unable'                                               |