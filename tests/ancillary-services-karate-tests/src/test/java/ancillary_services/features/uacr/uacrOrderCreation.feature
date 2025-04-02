@uacr
@envnot=prod
Feature: uACR Order creation Tests

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

    @TestCaseKey=ANC-T768
    Scenario: Submit uACR Performed eval and verify OrderCreation event is published
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
        
        * def result = UacrDb().queryExamWithStatusList(evaluation.evaluationId,["Order Requested", "Exam Performed"])

        # Validate Exam Status Update in database
        # Validate response contains 14 - Order Requested
        * match result[*].ExamStatusCodeId contains [1, 14]
        
        * def result = UacrDb().getExamDates(evaluation.evaluationId)
        # chcek if the data received via EvaluationFinalizedEvent event is written to the database properly
        * match DataGen().RemoveMilliSeconds(evalEvent.ReceivedDateTime) == DataGen().getUtcDateTimeString(result[0].EvaluationReceivedDateTime.toString())
        * match DataGen().RemoveMilliSeconds(evalEvent.CreatedDateTime) == DataGen().getUtcDateTimeString(result[0].EvaluationCreatedDateTime.toString())
        * match evalEvent.DateOfService == DataGen().getUtcDateTimeString(result[0].DateOfService.toString()).split('+')[0]

        # Verify Kafka message contains Performed header 
        * json kafkaEvent = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("dps_oms_order", evaluation.evaluationId + '', "OrderCreationEvent", 10, 5000))
        * match kafkaEvent.Context.LgcBarcode == evaluation.answers[4].AnswerValue
        * match kafkaEvent.Context.LgcAlphaCode == evaluation.answers[5].AnswerValue
        * match kafkaEvent.ProductCode == "UACR"
        * match kafkaEvent.EvaluationId == evaluation.evaluationId
        * match kafkaEvent.Vendor == "LetsGetChecked"
        
    @TestCaseKey=ANC-T793
    Scenario: Submit invalid uACR Performed eval and verify OrderCreation event is not published - no barcodes
        
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

        # Validate Exam Status Update in database
        # Validate response does not contain 14 - Order Requested
        * match result == []

        # Verify Kafka message NOT published 
        * json kafkaEvent = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("dps_oms_order", evaluation.evaluationId + '', "OrderCreationEvent", 10, 5000))
        * match kafkaEvent == {}

    @TestCaseKey=ANC-T793
    Scenario: Submit invalid uACR Performed eval and verify OrderCreation event is not published - no numeric barcode
        
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

        # Validate Exam Status Update in database
        # Validate response does not contain 14 - Order Requested
        * match result == []

        # Verify Kafka message NOT published 
        * json kafkaEvent = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("dps_oms_order", evaluation.evaluationId + '', "OrderCreationEvent", 10, 5000))
        * match kafkaEvent == {}

    @TestCaseKey=ANC-T793
    Scenario: Submit invalid uACR Performed eval and verify OrderCreation event is not published - no Alpha barcode
        
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

        # Validate Exam Status Update in database
        # Validate response does not contain 14 - Order Requested
        * match result == []

        # Verify Kafka message NOT published 
        * json kafkaEvent = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("dps_oms_order", evaluation.evaluationId + '', "OrderCreationEvent", 10, 5000))
        * match kafkaEvent == {}

    @TestCaseKey=ANC-T793
    Scenario: Submit invalid uACR Performed eval and verify OrderCreation event is not published - invalid numeric barcode
        
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

        # Validate Exam Status Update in database
        # Validate response does not contain 14 - Order Requested
        * match result != []

        # Verify Kafka message NOT published 
        * json kafkaEvent = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("dps_oms_order", evaluation.evaluationId + '', "OrderCreationEvent", 10, 5000))
        * match kafkaEvent == {}

    @TestCaseKey=ANC-T793
    Scenario: Submit invalid uACR Performed eval and verify OrderCreation event is not published - invalid alpha barcode
        
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

        # Validate Exam Status Update in database
        # Validate response does not contain 14 - Order Requested
        * match result != []

        # Verify Kafka message NOT published 
        * json kafkaEvent = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("dps_oms_order", evaluation.evaluationId + '', "OrderCreationEvent", 10, 5000))
        * match kafkaEvent == {}
        