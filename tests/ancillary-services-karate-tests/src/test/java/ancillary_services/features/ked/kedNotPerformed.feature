@ked
@uacr
@egfr
@envnot=prod
Feature: KED Lab Not Performed Tests

    Background:
        * eval if (env == 'prod') karate.abort();
        * def DataGen = function() { var DataGen = Java.type('helpers.data.DataGen'); return new DataGen(); }
        * def UacrDb = function() { var UacrDb = Java.type('helpers.database.uacr.UacrDb'); return new UacrDb(); }
        * def timestamp = DataGen().isoTimestamp()
        * def dateStamp = DataGen().isoDateStamp()
        * def KafkaConsumerHelper = Java.type('helpers.kafka.KafkaConsumerHelper')
        * def memberDetails = karate.call("classpath:helpers/member/createMember.js")
        * def appointment = karate.call('classpath:helpers/appointment/createAppointment.feature', { products: ['HHRA', 'UACR'] }).response
        * def evaluation = karate.call('classpath:helpers/eval/startEval.feature').response
        * def Faker = function() { var Faker = Java.type('helpers.data.Faker'); return new Faker(); }
        
    @TestCaseKey=ANC-T799
    Scenario Outline: KED Not Performed - Provider Unable to Perform - <expected_reason>
        * def randomNotes = Faker().randomQuote()
        * set evaluation.answers =
            """
            [
                {
                    "AnswerId": 52457,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "1"
                },
                {
                    "AnswerId": 52461,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "Provider unable to perform"
                },
                {
                    "AnswerId": <ked_answer_id>,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": <answer_value>
                },
                {
                    "AnswerId": 52460,
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

        * def result = UacrDb().getNotPerformedResultsByEvaluationId(evaluation.evaluationId)[0]
        # Verify not performed details
        * match result.AnswerId == <answer_id>
        * match result.Reason == <expected_reason>
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

        # Verify Kafka message contains NotPerformed header 
        * json kafkaEvent = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("uacr_status", evaluation.evaluationId + '', "NotPerformed", 10, 5000))
        * match kafkaEvent.ProviderId == providerDetails.providerId
        * match kafkaEvent.ProductCode == "UACR"
        * match kafkaEvent.EvaluationId == evaluation.evaluationId
        * match kafkaEvent.MemberPlanId == memberDetails.memberPlanId
        * match kafkaEvent.ReasonNotes == randomNotes
        * match kafkaEvent.CreatedDate.toString().split('+')[1] == "00:00"
        * match kafkaEvent.ReceivedDate.toString().split('+')[1] == "00:00"
        * match DataGen().RemoveMilliSeconds(kafkaEvent.CreatedDate) == DataGen().getUtcDateTimeString(result.EvaluationCreatedDateTime.toString())
        * match DataGen().RemoveMilliSeconds(kafkaEvent.ReceivedDate) == DataGen().getUtcDateTimeString(result.EvaluationReceivedDateTime.toString())

        # Verify Kafka message NOT published 
        * json kafkaEvent = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("dps_oms_order", evaluation.evaluationId + '', "OrderCreationEvent", 10, 5000))
        * match kafkaEvent == {}

        Examples:
        |ked_answer_id | answer_id | answer_value                                                     | expected_reason           |
        | 52463        | 52472     | "Technical issue (please call Mobile Support at (877) 570-9359)" | "Technical issue"         |
        | 52464        | 52473     |"Environmental issue"                                            | "Environmental issue"      |
        | 52465        | 52474     |"No supplies or equipment"                                       | "No supplies or equipment" |
        | 52466        | 52475     |"Insufficient training"                                          | "Insufficient training"    |

    
    @TestCaseKey=ANC-T799
    Scenario Outline: KED Not Performed - Member Refused - <expected_reason>
        * def randomNotes = Faker().randomQuote()
        * set evaluation.answers =
            """
            [
                {
                    "AnswerId": 52457,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "1"
                },
                {
                    "AnswerId": 52462,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "Member refused"
                },                       
                {
                    "AnswerId": <ked_answer_id>,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": <answer_value>
                },
                {
                    "AnswerId": 52460,
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

        * def result = UacrDb().getNotPerformedResultsByEvaluationId(evaluation.evaluationId)[0]
        # Verify not performed details
        * match result.AnswerId == <answer_id>
        * match result.Reason == <expected_reason>
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

        # Verify Kafka message contains NotPerformed header 
        * json kafkaEvent = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("uacr_status", evaluation.evaluationId + '', "NotPerformed", 10, 5000))
        * match kafkaEvent.ProviderId == providerDetails.providerId
        * match kafkaEvent.ProductCode == "UACR"
        * match kafkaEvent.EvaluationId == evaluation.evaluationId
        * match kafkaEvent.MemberPlanId == memberDetails.memberPlanId
        * match kafkaEvent.ReasonNotes == randomNotes
        * match kafkaEvent.CreatedDate.toString().split('+')[1] == "00:00"
        * match kafkaEvent.ReceivedDate.toString().split('+')[1] == "00:00"
        * match DataGen().RemoveMilliSeconds(kafkaEvent.CreatedDate) == DataGen().getUtcDateTimeString(result.EvaluationCreatedDateTime.toString())
        * match DataGen().RemoveMilliSeconds(kafkaEvent.ReceivedDate) == DataGen().getUtcDateTimeString(result.EvaluationReceivedDateTime.toString())

        # Verify Kafka message NOT published 
        * json kafkaEvent = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("dps_oms_order", evaluation.evaluationId + '', "OrderCreationEvent", 10, 5000))
        * match kafkaEvent == {}

        Examples:
        |ked_answer_id | answer_id | answer_value                | expected_reason             |
        | 52467        | 52476     | "Scheduled to complete"     | "Scheduled to complete"     |
        | 52468        | 52477     | "Member apprehension"       | "Member apprehension"       |
        | 52469        | 52478     | "Not interested"            | "Not interested"            |        