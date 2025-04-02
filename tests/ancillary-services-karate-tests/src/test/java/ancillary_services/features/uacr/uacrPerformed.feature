@uacr
@envnot=prod
Feature: uACR Lab Performed Tests

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

    @TestCaseKey=ANC-T753
    Scenario: Submit uACR Performed eval and verify
        * set evaluation.answers =
            """
            [
                {
                    "AnswerId": 52456,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "1"
                },
                {
                    "AnswerId": 52458,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "1"
                },
                {
                    "AnswerId": 51276,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": #(evaluation.evaluationId)
                },
                {
                    "AnswerId": 22034,
                    "AnsweredDateTime": "#(dateStamp)",
                    "AnswerValue": "#(dateStamp)"
                },
                {
                    "AnswerId": 52482,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": #(DataGen().GetLGCBarcode())
                },
                {
                    "AnswerId": 52481,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": #(DataGen().GetAlfacode())
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

        * json evalEvent = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("evaluation", evaluation.evaluationId + '', "EvaluationFinalizedEvent", 10, 5000))
        * print evalEvent
        * match evalEvent.Id == '#notnull'	
        
        * def result = UacrDb().getResultsByEvaluationId(evaluation.evaluationId)
        # Verify member details
        * match result[0].FirstName == memberDetails.firstName
        * match result[0].MiddleName == memberDetails.middleName
        * match result[0].LastName == memberDetails.lastName
        * match result[0].AddressLineOne == memberDetails.address.address1
        * match result[0].AddressLineTwo == memberDetails.address.address2
        * match result[0].City == memberDetails.address.city
        * match result[0].State == memberDetails.address.state
        * match result[0].ZipCode == memberDetails.address.zipCode
        * match result[0].MemberId == memberDetails.memberId
        * match result[0].CenseoId == memberDetails.censeoId
        * match result[0].MemberPlanId == memberDetails.memberPlanId
        # Verify provider details
        * match result[0].ProviderId == providerDetails.providerId
        * match result[0].NationalProviderIdentifier == providerDetails.nationalProviderIdentifier
        # Verify evaluation details
        * match result[0].EvaluationId == evaluation.evaluationId
        * match result[0].AppointmentId == appointment.appointmentId
        * match result[0].StatusDateTime != null
        * match result[0].DateOfService != null

        # Validate Exam Status Update in database
        # Validate response contains 14 - Order Requested
        * match result[*].StatusName contains ["Order Requested", "Exam Performed"]
        * match result[*].ExamStatusCodeId contains [1, 14]

        # chcek if the data received via EvaluationFinalizedEvent event is written to the database properly
        * match DataGen().RemoveMilliSeconds(evalEvent.ReceivedDateTime) == DataGen().getUtcDateTimeString(result[0].EvaluationReceivedDateTime.toString())
        * match DataGen().RemoveMilliSeconds(evalEvent.CreatedDateTime) == DataGen().getUtcDateTimeString(result[0].EvaluationCreatedDateTime.toString())
        * match evalEvent.DateOfService == DataGen().getUtcDateTimeString(result[0].DateOfService.toString()).split('+')[0]

        # Verify barcode details
        * def barcodeHistory = UacrDb().getBarcodeByExamId(result[0].ExamId)
        * match barcodeHistory[0].Barcode == evaluation.answers[4].AnswerValue+'|'+ evaluation.answers[5].AnswerValue
        
        # Verify Kafka message contains Performed header 
        * json kafkaEvent = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("uacr_status", evaluation.evaluationId + '', "Performed", 10, 5000))
        * print kafkaEvent
        * match kafkaEvent.Barcode == barcodeHistory[0].Barcode
        * match kafkaEvent.ProviderId == providerDetails.providerId
        * match kafkaEvent.ProductCode == "UACR"
        * match kafkaEvent.EvaluationId == evaluation.evaluationId
        * match kafkaEvent.MemberPlanId == memberDetails.memberPlanId
        * match kafkaEvent.CreatedDate.toString().split('+')[1] == "00:00"
        * match kafkaEvent.ReceivedDate.toString().split('+')[1] == "00:00"
        * match DataGen().RemoveMilliSeconds(kafkaEvent.CreatedDate) == DataGen().getUtcDateTimeString(result[0].EvaluationCreatedDateTime.toString())
        * match DataGen().RemoveMilliSeconds(kafkaEvent.ReceivedDate) == DataGen().getUtcDateTimeString(result[0].EvaluationReceivedDateTime.toString())

    @TestCaseKey=ANC-T793
    Scenario: Submit invalid uACR Performed evaluation - no barcodes
        * set evaluation.answers = 
        """
        [
            {
                "AnswerId": 52456,
                "AnsweredDateTime": "#(timestamp)",
                "AnswerValue": "1"
            },
            {
                "AnswerId": 52458,
                "AnsweredDateTime": "#(timestamp)",
                "AnswerValue": "1"
            },
            {
                "AnswerId": 51276,
                "AnsweredDateTime": "#(timestamp)",
                "AnswerValue": #(evaluation.evaluationId)
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

        * json evalEvent = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("evaluation", evaluation.evaluationId + '', "EvaluationFinalizedEvent", 10, 5000))
        * match evalEvent.Id == '#notnull'	
        
        * def result = UacrDb().getExamId(evaluation.evaluationId, 3, 500)
        # Verify DB result is empty
        * match result == []

        # Verify Kafka message contains Performed header 
        * json kafkaEvent = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("uacr_status", evaluation.evaluationId + '', "Performed", 10, 5000))
        * match kafkaEvent == {}

    @TestCaseKey=ANC-T793
    Scenario: Submit invalid uACR Performed evaluation - no numeric barcode
        * set evaluation.answers = 
        """
        [
            {
                "AnswerId": 52456,
                "AnsweredDateTime": "#(timestamp)",
                "AnswerValue": "1"
            },
            {
                "AnswerId": 52458,
                "AnsweredDateTime": "#(timestamp)",
                "AnswerValue": "1"
            },
            {
                "AnswerId": 51276,
                "AnsweredDateTime": "#(timestamp)",
                "AnswerValue": #(evaluation.evaluationId)
            },
            {
                "AnswerId": 22034,
                "AnsweredDateTime": "#(dateStamp)",
                "AnswerValue": "#(dateStamp)"
            },
            {
                "AnswerId": 52481,
                "AnsweredDateTime": "#(timestamp)",
                "AnswerValue": #(DataGen().GetAlfacode())
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

        * json evalEvent = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("evaluation", evaluation.evaluationId + '', "EvaluationFinalizedEvent", 10, 5000))
        * match evalEvent.Id == '#notnull'	
        
        * def result = UacrDb().getExamId(evaluation.evaluationId, 3, 500)
        # Verify DB result is empty
        * match result == []

        # Verify Kafka message contains Performed header 
        * json kafkaEvent = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("uacr_status", evaluation.evaluationId + '', "Performed", 10, 5000))
        * match kafkaEvent == {}

    @TestCaseKey=ANC-T793
    Scenario: Submit invalid uACR Performed evaluation - no Alpha barcode
        * set evaluation.answers = 
        """
        [
            {
                "AnswerId": 52456,
                "AnsweredDateTime": "#(timestamp)",
                "AnswerValue": "1"
            },
            {
                "AnswerId": 52458,
                "AnsweredDateTime": "#(timestamp)",
                "AnswerValue": "1"
            },
            {
                "AnswerId": 51276,
                "AnsweredDateTime": "#(timestamp)",
                "AnswerValue": #(evaluation.evaluationId)
            },
            {
                "AnswerId": 22034,
                "AnsweredDateTime": "#(dateStamp)",
                "AnswerValue": "#(dateStamp)"
            },
            {
                "AnswerId": 52482,
                "AnsweredDateTime": "#(timestamp)",
                "AnswerValue": #(DataGen().GetLGCBarcode())
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

        * json evalEvent = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("evaluation", evaluation.evaluationId + '', "EvaluationFinalizedEvent", 10, 5000))
        * match evalEvent.Id == '#notnull'	
        
        * def result = UacrDb().getExamId(evaluation.evaluationId, 3, 500)
        # Verify DB result is empty
        * match result == []

        # Verify Kafka message contains Performed header 
        * json kafkaEvent = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("uacr_status", evaluation.evaluationId + '', "Performed", 10, 5000))
        * match kafkaEvent == {}

    @TestCaseKey=ANC-T793
    Scenario: Submit invalid uACR Performed evaluation - invalid numeric barcode
        * set evaluation.answers = 
        """
        [
            {
                "AnswerId": 52456,
                "AnsweredDateTime": "#(timestamp)",
                "AnswerValue": "1"
            },
            {
                "AnswerId": 52458,
                "AnsweredDateTime": "#(timestamp)",
                "AnswerValue": 1
            },
            {
                "AnswerId": 51276,
                "AnsweredDateTime": "#(timestamp)",
                "AnswerValue": #(evaluation.evaluationId)
            },
            {
                "AnswerId": 22034,
                "AnsweredDateTime": "#(dateStamp)",
                "AnswerValue": "#(dateStamp)"
            },
            {
                "AnswerId": 52482,
                "AnsweredDateTime": "#(timestamp)",
                "AnswerValue": #(DataGen().GetInvalidBarcode())
            },
            {
                "AnswerId": 52481,
                "AnsweredDateTime": "#(timestamp)",
                "AnswerValue": #(DataGen().GetAlfacode())
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

        * json evalEvent = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("evaluation", evaluation.evaluationId + '', "EvaluationFinalizedEvent", 10, 5000))
        * match evalEvent.Id == '#notnull'	
        
        * def result = UacrDb().getExamId(evaluation.evaluationId, 3, 500)
        # Verify DB result is empty
        * match result != []

        # Verify Kafka message contains Performed header 
        * json kafkaEvent = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("uacr_status", evaluation.evaluationId + '', "Performed", 10, 5000))
        * match kafkaEvent != {}

    @TestCaseKey=ANC-T793
    Scenario: Submit invalid uACR Performed evaluation - invalid Alpha barcode
        * set evaluation.answers = 
        """
        [  
            {
                "AnswerId": 52456,
                "AnsweredDateTime": "#(timestamp)",
                "AnswerValue": "1"
            },
            {
                "AnswerId": 52458,
                "AnsweredDateTime": "#(timestamp)",
                "AnswerValue": 1
            },
            {
                "AnswerId": 51276,
                "AnsweredDateTime": "#(timestamp)",
                "AnswerValue": #(evaluation.evaluationId)
            },
            {
                "AnswerId": 22034,
                "AnsweredDateTime": "#(dateStamp)",
                "AnswerValue": "#(dateStamp)"
            },
            {
                "AnswerId": 52482,
                "AnsweredDateTime": "#(timestamp)",
                "AnswerValue": #(DataGen().GetLGCBarcode())
            },
            {
                "AnswerId": 52481,
                "AnsweredDateTime": "#(timestamp)",
                "AnswerValue": #(DataGen().GetInvalidAlfacode())
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

        * json evalEvent = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("evaluation", evaluation.evaluationId + '', "EvaluationFinalizedEvent", 10, 5000))
        * match evalEvent.Id == '#notnull'	
        
        * def result = UacrDb().getExamId(evaluation.evaluationId, 3, 500)
        # Verify DB result is empty
        * match result != []
 
        # Verify Kafka message contains Performed header 
        * json kafkaEvent = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("uacr_status", evaluation.evaluationId + '', "Performed", 10, 5000))
        * match kafkaEvent != {}