@egfr
@envnot=prod
Feature: eGFR Lab Not Performed Tests

    Background:
        * eval if (env == 'prod') karate.abort();
        * def DataGen = function() { var DataGen = Java.type('helpers.data.DataGen'); return new DataGen(); }
        * def EgfrDb = function() { var EgfrDb = Java.type('helpers.database.egfr.EgfrDb'); return new EgfrDb(); }
        * def timestamp = DataGen().isoTimestamp()
        * def dateStamp = DataGen().isoDateStamp()
        * def KafkaConsumerHelper = Java.type('helpers.kafka.KafkaConsumerHelper')
        * def memberDetails = karate.call("classpath:helpers/member/createMember.js")
        * def appointment = karate.call('classpath:helpers/appointment/createAppointment.feature', { products: ['HHRA', 'EGFR'] }).response
        * def Faker = function() { var Faker = Java.type('helpers.data.Faker'); return new Faker(); }
        
    @TestCaseKey=ANC-T410
    Scenario Outline: eGFR Not Performed - Provider Unable to Perform
        * def evaluation = karate.call('classpath:helpers/eval/startEval.feature').response
        * def randomNotes = Faker().randomQuote()
        * set evaluation.answers =
            """
            [
                {
                    "AnswerId": 52456,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "1"
                },
                {
                    "AnswerId": 22034,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "#(timestamp)"
                },
                {
                    "AnswerId": 51262,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": 0
                },
                {
                    "AnswerId": 51263,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "Provider unable to perform"
                },
                {
                    "AnswerId": <answer_id>,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": <answer_value>
                },
                {
                    "AnswerId": 52480,
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

        * def result = EgfrDb().getNotPerformedResultsByEvaluationId(evaluation.evaluationId)[0]
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
        * json kafkaEvent = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("egfr_status", evaluation.evaluationId + '', "NotPerformed", 10, 5000))
        * match kafkaEvent.ProviderId == providerDetails.providerId
        * match kafkaEvent.ProductCode == "EGFR"
        * match kafkaEvent.EvaluationId == evaluation.evaluationId
        * match kafkaEvent.MemberPlanId == memberDetails.memberPlanId
        * match kafkaEvent.ReasonNotes == randomNotes
        * match kafkaEvent.CreatedDate.toString().split('+')[1] == "00:00"
        * match kafkaEvent.ReceivedDate.toString().split('+')[1] == "00:00"
        * match DataGen().RemoveMilliSeconds(kafkaEvent.CreatedDate) == DataGen().getUtcDateTimeString(result.EvaluationCreatedDateTime.toString())
        * match DataGen().RemoveMilliSeconds(kafkaEvent.ReceivedDate) == DataGen().getUtcDateTimeString(result.EvaluationReceivedDateTime.toString())

        Examples:
            | answer_id | answer_value                                                     | expected_reason            |
            | 51266     | "Technical issue (please call Mobile Support at (877) 570-9359)" | "Technical issue"          |
            | 51267     | "Environmental issue"                                            | "Environmental issue"      |
            | 51268     | "No supplies or equipment"                                       | "No supplies or equipment" |
            | 51269     | "Insufficient training"                                          | "Insufficient training"    |

    @TestCaseKey=ANC-T411
    Scenario Outline: eGFR Not Performed - Member Refused
        * def evaluation = karate.call('classpath:helpers/eval/startEval.feature').response
        * def randomNotes = Faker().randomQuote()
        * set evaluation.answers =
            """
            [
                {
                    "AnswerId": 52456,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "1"
                },
                {
                    "AnswerId": 22034,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "#(timestamp)"
                },
                {
                    "AnswerId": 51262,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": 0
                },
                {
                    "AnswerId": 51264,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "Member refused"
                },
                {
                    "AnswerId": <answer_id>,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": <answer_value>
                },
                {
                    "AnswerId": 52480,
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

        * def result = EgfrDb().getNotPerformedResultsByEvaluationId(evaluation.evaluationId)[0]
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
        * json kafkaEvent = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("egfr_status", evaluation.evaluationId + '', "NotPerformed", 10, 5000))
        * match kafkaEvent.ProviderId == providerDetails.providerId
        * match kafkaEvent.ProductCode == "EGFR"
        * match kafkaEvent.EvaluationId == evaluation.evaluationId
        * match kafkaEvent.MemberPlanId == memberDetails.memberPlanId
        * match kafkaEvent.CreatedDate.toString().split('+')[1] == "00:00"
        * match kafkaEvent.ReceivedDate.toString().split('+')[1] == "00:00"
        * match kafkaEvent.ReasonNotes == randomNotes
        * match DataGen().RemoveMilliSeconds(kafkaEvent.CreatedDate) == DataGen().getUtcDateTimeString(result.EvaluationCreatedDateTime.toString())
        * match DataGen().RemoveMilliSeconds(kafkaEvent.ReceivedDate) == DataGen().getUtcDateTimeString(result.EvaluationReceivedDateTime.toString())

        Examples:
            | answer_id | answer_value                | expected_reason             |
            | 51272     | "Member recently completed" | "Member recently completed" |
            | 51273     | "Scheduled to complete"     | "Scheduled to complete"     |
            | 51274     | "Member apprehension"       | "Member apprehension"       |
            | 51275     | "Not interested"            | "Not interested"            |

    @TestCaseKey=ANC-T648     
    Scenario Outline: eGFR Not Performed - Clinically not relevant
        * def evaluation = karate.call('classpath:helpers/eval/startEval.feature').response
        * def randomNotes = Faker().randomQuote()
        * set evaluation.answers =
            """
            [
                {
                "AnswerId": 52456,
                "AnsweredDateTime": "#(timestamp)",
                "AnswerValue": "1"
                },
                {
                    "AnswerId": 22034,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "#(timestamp)"
                },
                {
                    "AnswerId": 51262,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": 0
                },
                {
                    "AnswerId": 51265,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": <answer_value>
                },
                {
                    "AnswerId": 52480,
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
    
        * def result = EgfrDb().getNotPerformedResultsByEvaluationId(evaluation.evaluationId)[0]
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
        * json kafkaEvent = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("egfr_status", evaluation.evaluationId + '', "NotPerformed", 10, 5000))
        * match kafkaEvent.ProviderId == providerDetails.providerId
        * match kafkaEvent.ProductCode == "EGFR"
        * match kafkaEvent.ReasonType ==  <expected_reason>
        * match kafkaEvent.Reason ==  <expected_reason>
        * match kafkaEvent.EvaluationId == evaluation.evaluationId
        * match kafkaEvent.MemberPlanId == memberDetails.memberPlanId
        * match kafkaEvent.ReasonNotes == randomNotes
        * match kafkaEvent.CreatedDate.toString().split('+')[1] == "00:00"
        * match kafkaEvent.ReceivedDate.toString().split('+')[1] == "00:00"
        * match DataGen().RemoveMilliSeconds(kafkaEvent.CreatedDate) == DataGen().getUtcDateTimeString(result.EvaluationCreatedDateTime.toString())
        * match DataGen().RemoveMilliSeconds(kafkaEvent.ReceivedDate) == DataGen().getUtcDateTimeString(result.EvaluationReceivedDateTime.toString())

        Examples:
        | answer_id | answer_value                | expected_reason             |
        | 51265     | "Clinically not relevant" | "Clinically not relevant" |

    @TestCaseKey=ANC-T410
    Scenario Outline: eGFR Not Performed non KED - Provider Unable to Perform
        * def evaluation = karate.call('classpath:helpers/eval/startEval.feature',{formVersion:589}).response
        * def randomNotes = Faker().randomQuote()
        * set evaluation.answers =
            """
            [
                {
                    "AnswerId": 22034,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "#(timestamp)"
                },
                {
                    "AnswerId": 51262,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": 0
                },
                {
                    "AnswerId": 51263,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "Provider unable to perform"
                },
                {
                    "AnswerId": <answer_id>,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": <answer_value>
                },
                {
                    "AnswerId": 52480,
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

        * def result = EgfrDb().getNotPerformedResultsByEvaluationId(evaluation.evaluationId)[0]
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
        * json kafkaEvent = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("egfr_status", evaluation.evaluationId + '', "NotPerformed", 10, 5000))
        * match kafkaEvent.ProviderId == providerDetails.providerId
        * match kafkaEvent.ProductCode == "EGFR"
        * match kafkaEvent.EvaluationId == evaluation.evaluationId
        * match kafkaEvent.MemberPlanId == memberDetails.memberPlanId
        * match kafkaEvent.ReasonNotes == randomNotes
        * match kafkaEvent.CreatedDate.toString().split('+')[1] == "00:00"
        * match kafkaEvent.ReceivedDate.toString().split('+')[1] == "00:00"
        * match DataGen().RemoveMilliSeconds(kafkaEvent.CreatedDate) == DataGen().getUtcDateTimeString(result.EvaluationCreatedDateTime.toString())
        * match DataGen().RemoveMilliSeconds(kafkaEvent.ReceivedDate) == DataGen().getUtcDateTimeString(result.EvaluationReceivedDateTime.toString())

        Examples:
            | answer_id | answer_value                                                     | expected_reason            |
            | 51266     | "Technical issue (please call Mobile Support at (877) 570-9359)" | "Technical issue"          |
            | 51267     | "Environmental issue"                                            | "Environmental issue"      |
            | 51268     | "No supplies or equipment"                                       | "No supplies or equipment" |
            | 51269     | "Insufficient training"                                          | "Insufficient training"    |

    @TestCaseKey=ANC-T411
    Scenario Outline: eGFR Not Performed non KED - Member Refused
        * def evaluation = karate.call('classpath:helpers/eval/startEval.feature',{formVersion:589}).response
        * def randomNotes = Faker().randomQuote()
        * set evaluation.answers =
            """
            [
                {
                    "AnswerId": 22034,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "#(timestamp)"
                },
                {
                    "AnswerId": 51262,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": 0
                },
                {
                    "AnswerId": 51264,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "Member refused"
                },
                {
                    "AnswerId": <answer_id>,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": <answer_value>
                },
                {
                    "AnswerId": 52480,
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

        * def result = EgfrDb().getNotPerformedResultsByEvaluationId(evaluation.evaluationId)[0]
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
        * json kafkaEvent = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("egfr_status", evaluation.evaluationId + '', "NotPerformed", 10, 5000))
        * match kafkaEvent.ProviderId == providerDetails.providerId
        * match kafkaEvent.ProductCode == "EGFR"
        * match kafkaEvent.EvaluationId == evaluation.evaluationId
        * match kafkaEvent.MemberPlanId == memberDetails.memberPlanId
        * match kafkaEvent.ReasonNotes == randomNotes
        * match kafkaEvent.CreatedDate.toString().split('+')[1] == "00:00"
        * match kafkaEvent.ReceivedDate.toString().split('+')[1] == "00:00"
        * match DataGen().RemoveMilliSeconds(kafkaEvent.CreatedDate) == DataGen().getUtcDateTimeString(result.EvaluationCreatedDateTime.toString())
        * match DataGen().RemoveMilliSeconds(kafkaEvent.ReceivedDate) == DataGen().getUtcDateTimeString(result.EvaluationReceivedDateTime.toString())

        Examples:
            | answer_id | answer_value                | expected_reason             |
            | 51272     | "Member recently completed" | "Member recently completed" |
            | 51273     | "Scheduled to complete"     | "Scheduled to complete"     |
            | 51274     | "Member apprehension"       | "Member apprehension"       |
            | 51275     | "Not interested"            | "Not interested"            |

    @TestCaseKey=ANC-T648     
    Scenario Outline: eGFR Not Performed non KED - Clinically not relevant
        * def evaluation = karate.call('classpath:helpers/eval/startEval.feature',{formVersion:589}).response
        * def randomNotes = Faker().randomQuote()
        * set evaluation.answers =
            """
            [
                {
                    "AnswerId": 22034,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "#(timestamp)"
                },
                {
                    "AnswerId": 51262,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": 0
                },
                {
                    "AnswerId": 51265,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": <answer_value>
                },
                {
                    "AnswerId": 52480,
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
    
        * def result = EgfrDb().getNotPerformedResultsByEvaluationId(evaluation.evaluationId)[0]
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
        * json kafkaEvent = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("egfr_status", evaluation.evaluationId + '', "NotPerformed", 10, 5000))
        * match kafkaEvent.ProviderId == providerDetails.providerId
        * match kafkaEvent.ProductCode == "EGFR"
        * match kafkaEvent.ReasonType ==  <expected_reason>
        * match kafkaEvent.Reason ==  <expected_reason>
        * match kafkaEvent.EvaluationId == evaluation.evaluationId
        * match kafkaEvent.MemberPlanId == memberDetails.memberPlanId
        * match kafkaEvent.ReasonNotes == randomNotes
        * match kafkaEvent.CreatedDate.toString().split('+')[1] == "00:00"
        * match kafkaEvent.ReceivedDate.toString().split('+')[1] == "00:00"
        * match DataGen().RemoveMilliSeconds(kafkaEvent.CreatedDate) == DataGen().getUtcDateTimeString(result.EvaluationCreatedDateTime.toString())
        * match DataGen().RemoveMilliSeconds(kafkaEvent.ReceivedDate) == DataGen().getUtcDateTimeString(result.EvaluationReceivedDateTime.toString())

        Examples:
        | answer_id | answer_value                | expected_reason             |
        | 51265     | "Clinically not relevant" | "Clinically not relevant" |