@egfr
@envnot=prod
Feature: eGFR ProviderPay Tests

    Background:
        * eval if (env == 'prod') karate.abort();
        * def DataGen = function() { var DataGen = Java.type('helpers.data.DataGen'); return new DataGen(); }
        * def EgfrDb = function() { var EgfrDb = Java.type('helpers.database.egfr.EgfrDb'); return new EgfrDb(); }
        * def timestamp = DataGen().isoTimestamp()
        * def dateStamp = DataGen().isoDateStamp()
        * def cdiDateTime = DataGen().timestampWithOffset("-05:00", -1)
        * def eventId = DataGen().uuid()
        * string cdiTopicName = "cdi_events"
        * def KafkaConsumerHelper = Java.type('helpers.kafka.KafkaConsumerHelper')
        * def kafkaProducerHelper = Java.type('helpers.kafka.KafkaProducerHelper')
        * def memberDetails = karate.call("classpath:helpers/member/createMember.js")
        * def appointment = karate.call('classpath:helpers/appointment/createAppointment.feature', { products: ['HHRA', 'EGFR'] }).response
        * def evaluation = karate.call('classpath:helpers/eval/startEval.feature').response
        * def sleep = function(pause){ java.lang.Thread.sleep(pause) }

    Scenario Outline: eGFR ProviderPay - CDIPassed and CDIFailed(PayProvider: true) after LabResults - Business Rules Met
        * set evaluation.answers =
            """
                [
                    {
                        "AnswerId": 52456,
                        "AnsweredDateTime": "#(timestamp)",
                        "AnswerValue": "1"
                    },
                    {
                        "AnswerId": 51261,
                        "AnsweredDateTime": "#(timestamp)",
                        "AnswerValue": "1"
                    },
                    {
                        "AnswerId": 52484,
                        "AnsweredDateTime": "#(timestamp)",
                        "AnswerValue": #(DataGen().GetLGCBarcode())
                    },
                    {
                        "AnswerId": 52483,
                        "AnsweredDateTime": "#(timestamp)",
                        "AnswerValue": #(DataGen().GetAlfacode())
                    },
                    {
                        "AnswerId": 52480,
                        "AnsweredDateTime": "#(timestamp)",
                        "AnswerValue": #(DataGen().GetAlfacode())
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

        * def result = EgfrDb().getResultsByEvaluationId(evaluation.evaluationId)[0]

        # Publish the homeaccess lab results
        * string homeAccessResultsReceivedHeader = {'Type': 'KedEgfrLabResult'}
        * string homeaccessTopic = "dps_labresult_egfr"
        * def ProperDateOfService = DataGen().getUtcDateTimeString(result.DateOfService.toString())
        * string resultsReceivedValue = {'EvaluationId': '#(parseInt(evaluation.evaluationId))','DateLabReceived': '#(ProperDateOfService)',,'EgfrResult': '#(CMP_eGFRResult)','EstimatedGlomerularFiltrationRateResultDescription': '#(CMP_Description)','EstimatedGlomerularFiltrationRateResultColor': '#(CMP_EstimatedGlomerularFiltrationRateResultColor)'}
        * kafkaProducerHelper.send(homeaccessTopic, memberDetails.censeoId, homeAccessResultsReceivedHeader, resultsReceivedValue)
        * eval sleep(2000) 

        * json performedEvent = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("egfr_status", evaluation.evaluationId + '', "Performed", 10, 5000))
        * match performedEvent.EvaluationId == evaluation.evaluationId

        # Publish the cdi event to the cdi_events topic
        * string cdiEventKey = evaluation.evaluationId
        * string cdiEventHeader = { 'Type' : <cdiEventHeaderName>}
        * string cdiEventValue = {"RequestId":"#(eventId)","EvaluationId": "#(evaluation.evaluationId)","DateTime":"#(cdiDateTime)","Username":"karateTestUser","ApplicationId":"manual","Reason":"reschedule","PayProvider":<payProvider>,"Products":[{"EvaluationId":"#(evaluation.evaluationId)","ProductCode":"HHRA"},{"EvaluationId":"#(evaluation.evaluationId)","ProductCode":"EGFR"}]}
        * kafkaProducerHelper.send(cdiTopicName, cdiEventKey, cdiEventHeader, cdiEventValue)
        * eval sleep(2000) 
        
        # Validate ProviderPayableEventReceived Status in Kafka
        * json payableEvent = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("egfr_status", evaluation.evaluationId + '', "ProviderPayableEventReceived", 10, 5000))   
        * match payableEvent.EvaluationId == evaluation.evaluationId
        * match payableEvent.ProviderId == providerDetails.providerId
        * match payableEvent.ParentCdiEvent == <cdiEventHeaderName>

        # Validate the entry in the ProviderPay table
        * def providerPay = EgfrDb().getProviderPayResultByEvaluationId(evaluation.evaluationId)[0]
        * match providerPay.PaymentId == "#notnull"
        * match providerPay.ExamId == "#notnull"

        # Validate Exam Status Update in database
        * def result = EgfrDb().queryExamWithStatusList(evaluation.evaluationId,["Exam Performed","Lab Results Received", "ProviderPayRequestSent", "ProviderPayableEventReceived","CDIPassedReceived"])
        # Validate response contains 1 - Exam Performed, 6 - Lab Results Received, 8 - ProviderPayableEventReceived, 9 - ProviderPayRequestSent, relevant cdi event
        * match result[*].ExamStatusCodeId contains [1, 6, 8, 9, <statusId>]

        # Validate ProviderPayRequestSent Status in Kafka
        * json event = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("egfr_status", evaluation.evaluationId + '', "ProviderPayRequestSent", 10, 5000))
        * match event.ProviderPayProductCode == 'EGFR' 
        * match event.ProductCode == 'EGFR'
        * match event.PaymentId == providerPay.PaymentId.toString()
        * match event.ParentEventDateTime == '#notnull'
        * match event.EvaluationId == evaluation.evaluationId
        * match event.MemberPlanId == appointment.memberPlanId
        * match event.ProviderId.toString() == appointment.providerId
        * match event.CreatedDate.toString().split('+')[1] == "00:00"
        * match event.ReceivedDate.toString().split('+')[1] == "00:00"
        
        Examples:
        | summary                   | CMP_eGFRResult  |CMP_EstimatedGlomerularFiltrationRateResultColor|  CMP_Description    | cdiEventHeaderName    | payProvider   | statusId  |
        | "cdiPassedAndNormal"      |    65            |                                                | Normal              | "CDIPassedEvent"      | true          | 11      |
        # | "cdiFailededAndNormal"    | 65             |                                                |  1.27               | "CDIFailedEvent"      | true          | 12      |
        | "cdiPassedAndAbnormal"    |    55            | Gray                                           |  Abnormal           | "CDIPassedEvent"      | true          | 11      |
        # | "cdiFailedAndAbnormal"    | 55              ||  1.27                    | "CDIFailedEvent"      | true          | 12      |

    @TestCaseKey=ANC-T629
    @ignore
    Scenario Outline: eGFR ProviderPay - CDIFailed(PayProvider: false) after LabResults - Business Rules Met
        * set evaluation.answers =
            """
                [
                    {
                        "AnswerId": 52456,
                        "AnsweredDateTime": "#(timestamp)",
                        "AnswerValue": "1"
                    },
                    {
                        "AnswerId": 51261,
                        "AnsweredDateTime": "#(timestamp)",
                        "AnswerValue": "1"
                    },
                    {
                        "AnswerId": 52484,
                        "AnsweredDateTime": "#(timestamp)",
                        "AnswerValue": #(DataGen().GetLGCBarcode())
                    },
                    {
                        "AnswerId": 52483,
                        "AnsweredDateTime": "#(timestamp)",
                        "AnswerValue": #(DataGen().GetAlfacode())
                    },
                    {
                        "AnswerId": 52480,
                        "AnsweredDateTime": "#(timestamp)",
                        "AnswerValue": #(DataGen().GetAlfacode())
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
        
        * def result = EgfrDb().getResultsByEvaluationId(evaluation.evaluationId)[0]

        # Publish the homeaccess lab results
        * string homeAccessResultsReceivedHeader = {'Type': 'KedEgfrLabResult'}
        * string homeaccessTopic = "dps_labresult_egfr"
        * def ProperDateOfService = DataGen().getUtcDateTimeString(result.DateOfService.toString())
        * string resultsReceivedValue = {'EvaluationId': '#(parseInt(evaluation.evaluationId))','DateLabReceived': '#(ProperDateOfService)',,'EgfrResult': '#(CMP_eGFRResult)','EstimatedGlomerularFiltrationRateResultDescription': '#(CMP_Description)','EstimatedGlomerularFiltrationRateResultColor': '#(CMP_EstimatedGlomerularFiltrationRateResultColor)'}
        * kafkaProducerHelper.send(homeaccessTopic, memberDetails.censeoId, homeAccessResultsReceivedHeader, resultsReceivedValue)
        * eval sleep(2000) 
        
        * json performedEvent = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("egfr_status", evaluation.evaluationId + '', "Performed", 10, 5000))
        * match performedEvent.EvaluationId == evaluation.evaluationId

        # Publish the cdi event to the cdi_events topic
        * string cdiEventKey = evaluation.evaluationId
        * string cdiEventHeader = { 'Type' : <cdiEventHeaderName>}
        * string cdiEventValue = {"RequestId":"#(eventId)","EvaluationId": "#(evaluation.evaluationId)","DateTime":"#(cdiDateTime)","Username":"karateTestUser","ApplicationId":"manual","Reason":"reschedule","PayProvider":<payProvider>,"Products":[{"EvaluationId":"#(evaluation.evaluationId)","ProductCode":"HHRA"},{"EvaluationId":"#(evaluation.evaluationId)","ProductCode":"EGFR"}]}
        * kafkaProducerHelper.send(cdiTopicName, cdiEventKey, cdiEventHeader, cdiEventValue)
        
        # Validate ProviderNonPayableEventReceived Status in Kafka
        * json nonPayableEvent = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("egfr_status", evaluation.evaluationId + '', "ProviderNonPayableEventReceived", 10, 5000))   
        * match nonPayableEvent.EvaluationId == evaluation.evaluationId
        * match nonPayableEvent.ProviderId == providerDetails.providerId
        * match nonPayableEvent.ParentCdiEvent == <cdiEventHeaderName>
        * match nonPayableEvent.Reason == "#notnull"

        # Validate the entry in the ProviderPay table
        * def providerPay = EgfrDb().getProviderPayResultByEvaluationId(evaluation.evaluationId)[0]
        * match providerPay == "#null"

        # Validate Exam Status Update in database
        * def result = EgfrDb().queryExamWithStatusList(evaluation.evaluationId,["Exam Performed","Lab Results Received", "ProviderNonPayableEventReceived","CDIFailedWithoutPayReceived"])
        # Validate response contains 1 - Exam Performed, 6 - Lab Results Received, 10 - "ProviderNonPayableEventReceived", relevant cdi event
        * match result[*].ExamStatusCodeId contains [1, 6, 10, 13]


        Examples:
        | summary                           | CMP_eGFRResult   | CMP_EstimatedGlomerularFiltrationRateResultColor|  CMP_Description     | cdiEventHeaderName    | payProvider   | 
        | "cdiFailedWithoutPayAndNormal"    | 65               |                                                 | Normal               | "CDIFailedEvent"      | false         | 
        | "cdiFailedWithoutPayAndAbnormal"  | 55               |                                                 | Normal               | "CDIFailedEvent"      | false         | 

  
    Scenario Outline: eGFR ProviderPay - CdiEvents after LabResults - Business Rules Not Met
        * set evaluation.answers =
            """
                [
                    {
                        "AnswerId": 52456,
                        "AnsweredDateTime": "#(timestamp)",
                        "AnswerValue": "1"
                    },
                    {
                        "AnswerId": 51261,
                        "AnsweredDateTime": "#(timestamp)",
                        "AnswerValue": "1"
                    },
                    {
                        "AnswerId": 52484,
                        "AnsweredDateTime": "#(timestamp)",
                        "AnswerValue": #(DataGen().GetLGCBarcode())
                    },
                    {
                        "AnswerId": 52483,
                        "AnsweredDateTime": "#(timestamp)",
                        "AnswerValue": #(DataGen().GetAlfacode())
                    },
                    {
                        "AnswerId": 52480,
                        "AnsweredDateTime": "#(timestamp)",
                        "AnswerValue": #(DataGen().GetAlfacode())
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
        
        * def result = EgfrDb().getResultsByEvaluationId(evaluation.evaluationId)[0]

        # Publish the homeaccess lab results
        * string homeAccessResultsReceivedHeader = {'Type': 'KedEgfrLabResult'}
        * string homeaccessTopic = "dps_labresult_egfr"
        * def ProperDateOfService = DataGen().getUtcDateTimeString(result.DateOfService.toString())
        * def EgfrResult = CMP_eGFRResult == null ? null : (CMP_eGFRResult)
        * string resultsReceivedValue = {'EvaluationId': '#(parseInt(evaluation.evaluationId))','DateLabReceived': '#(ProperDateOfService)',,'EgfrResult': '#(EgfrResult)','EstimatedGlomerularFiltrationRateResultDescription': '#(CMP_Description)','EstimatedGlomerularFiltrationRateResultColor': '#(CMP_EstimatedGlomerularFiltrationRateResultColor)'}
        * kafkaProducerHelper.send(homeaccessTopic, memberDetails.censeoId, homeAccessResultsReceivedHeader, resultsReceivedValue)
        * eval sleep(3000) 
        
        * json performedEvent = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("egfr_status", evaluation.evaluationId + '', "Performed", 10, 5000))
        * match performedEvent.EvaluationId == evaluation.evaluationId

        # Publish the cdi event to the cdi_events topic
        * string cdiEventKey = evaluation.evaluationId
        * string cdiEventHeader = { 'Type' : <cdiEventHeaderName>}
        * string cdiEventValue = {"RequestId":"#(eventId)","EvaluationId": "#(evaluation.evaluationId)","DateTime":"#(cdiDateTime)","Username":"karateTestUser","ApplicationId":"manual","Reason":"reschedule","PayProvider":<payProvider>,"Products":[{"EvaluationId":"#(evaluation.evaluationId)","ProductCode":"HHRA"},{"EvaluationId":"#(evaluation.evaluationId)","ProductCode":"EGFR"}]}
        * kafkaProducerHelper.send(cdiTopicName, cdiEventKey, cdiEventHeader, cdiEventValue)
        * eval sleep(3000) 
        
        # Validate ProviderNonPayableEventReceived Status in Kafka
        * json nonPayableEvent = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("egfr_status", evaluation.evaluationId + '', "ProviderNonPayableEventReceived", 10, 5000))   
        * match nonPayableEvent.EvaluationId == evaluation.evaluationId
        * match nonPayableEvent.ProviderId == providerDetails.providerId
        * match nonPayableEvent.ParentCdiEvent == <cdiEventHeaderName>
        * match nonPayableEvent.Reason == "#notnull"

        # Validate the entry in the ProviderPay table
        * def providerPay = EgfrDb().getProviderPayResultByEvaluationId(evaluation.evaluationId)[0]
        * match providerPay == "#null"

        # Validate Exam Status Update in database
        * def result = EgfrDb().queryExamWithStatusList(evaluation.evaluationId,["Exam Performed","Lab Results Received", "ProviderNonPayableEventReceived", "CDIPassedReceived"])
        # Validate response contains 1 - Exam Performed, 6 - Lab Results Received, 10 - "ProviderNonPayableEventReceived", relevant cdi event
        * match result[*].ExamStatusCodeId contains [1, 6, 10, <statusId>]

        Examples:
        | summary                               | CMP_eGFRResult | CMP_EstimatedGlomerularFiltrationRateResultColor|  CMP_Description      | cdiEventHeaderName    | payProvider  | statusId  |
        | "cdiPassedAndUndetermined"            |   -1           |                                                 | Abnormal              | "CDIPassedEvent"      | true         | 11      |
        | "cdiPassedAndUndetermined"            |    0           |                                                 |                       | "CDIPassedEvent"      | true         | 11      |
        | "cdiPassedAndUndetermined"            |                |                                                 |                       | "CDIPassedEvent"      | true         | 11      |
        # | "cdiFailedWithPayAndUndetermined"     |   -1           | 1.27                  | "CDIFailedEvent"      | true         | 12      |
        # | "cdiFailedWithPayAndUndetermined"     |   0            | 1.27                  | "CDIFailedEvent"      | true         | 12      |
        # | "cdiFailedWithPayAndUndetermined"     |                | 1.27                  | "CDIFailedEvent"      | true         | 12      |
        # | "cdiFailedWithoutPayAndUndetermined"  |   -1           | 1.27                  | "CDIFailedEvent"      | false        | 13      |
        # | "cdiFailedWithoutPayAndUndetermined"  |   0            | 1.27                  | "CDIFailedEvent"      | false        | 13      |
        # | "cdiFailedWithoutPayAndUndetermined"  |                | 1.27                  | "CDIFailedEvent"      | false        | 13      |

    @TestCaseKey=ANC-T631
    Scenario Outline: eGFR ProviderPay - CDIPassed and CDIFailed(PayProvider: true) before LabResults - Business Rules Met
        * set evaluation.answers =
            """
                [
                    {
                        "AnswerId": 52456,
                        "AnsweredDateTime": "#(timestamp)",
                        "AnswerValue": "1"
                    },
                    {
                        "AnswerId": 51261,
                        "AnsweredDateTime": "#(timestamp)",
                        "AnswerValue": "1"
                    },
                    {
                        "AnswerId": 52484,
                        "AnsweredDateTime": "#(timestamp)",
                        "AnswerValue": #(DataGen().GetLGCBarcode())
                    },
                    {
                        "AnswerId": 52483,
                        "AnsweredDateTime": "#(timestamp)",
                        "AnswerValue": #(DataGen().GetAlfacode())
                    },
                    {
                        "AnswerId": 52480,
                        "AnsweredDateTime": "#(timestamp)",
                        "AnswerValue": #(DataGen().GetAlfacode())
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

        * def result = EgfrDb().getResultsByEvaluationId(evaluation.evaluationId)[0]

        * json performedEvent = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("egfr_status", evaluation.evaluationId + '', "Performed", 10, 5000))
        * match performedEvent.EvaluationId == evaluation.evaluationId

        # Publish the cdi event to the cdi_events topic
        * string cdiEventKey = evaluation.evaluationId
        * string cdiEventHeader = { 'Type' : <cdiEventHeaderName>}
        * string cdiEventValue = {"RequestId":"#(eventId)","EvaluationId": "#(evaluation.evaluationId)","DateTime":"#(cdiDateTime)","Username":"karateTestUser","ApplicationId":"manual","Reason":"reschedule","PayProvider":<payProvider>,"Products":[{"EvaluationId":"#(evaluation.evaluationId)","ProductCode":"HHRA"},{"EvaluationId":"#(evaluation.evaluationId)","ProductCode":"EGFR"}]}
        * kafkaProducerHelper.send(cdiTopicName, cdiEventKey, cdiEventHeader, cdiEventValue)
        
        # Publish the homeaccess lab results
        * string homeAccessResultsReceivedHeader = {'Type': 'KedEgfrLabResult'}
        * string homeaccessTopic = "dps_labresult_egfr"
        * def ProperDateOfService = DataGen().getUtcDateTimeString(result.DateOfService.toString())
        * def EgfrResult = CMP_eGFRResult == null ? null : (CMP_eGFRResult)
        * string resultsReceivedValue = {'EvaluationId': '#(parseInt(evaluation.evaluationId))','DateLabReceived': '#(ProperDateOfService)',,'EgfrResult': '#(EgfrResult)','EstimatedGlomerularFiltrationRateResultDescription': '#(CMP_Description)','EstimatedGlomerularFiltrationRateResultColor': '#(CMP_EstimatedGlomerularFiltrationRateResultColor)'}
        * kafkaProducerHelper.send(homeaccessTopic, memberDetails.censeoId, homeAccessResultsReceivedHeader, resultsReceivedValue)
        * eval sleep(2000) 
        
        # Validate ProviderPayableEventReceived Status in Kafka
        * json payableEvent = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("egfr_status", evaluation.evaluationId + '', "ProviderPayableEventReceived", 10, 5000))   
        * match payableEvent.EvaluationId == evaluation.evaluationId
        * match payableEvent.ProviderId == providerDetails.providerId
        * match payableEvent.ParentCdiEvent == <cdiEventHeaderName>

        # Validate the entry in the ProviderPay table
        * def providerPay = EgfrDb().getProviderPayResultByEvaluationId(evaluation.evaluationId)[0]
        * match providerPay.PaymentId == "#notnull"
        * match providerPay.ExamId == "#notnull"

        # Validate Exam Status Update in database
        * def result = EgfrDb().queryExamWithStatusList(evaluation.evaluationId,["Exam Performed","Lab Results Received", "ProviderPayRequestSent", "ProviderPayableEventReceived","CDIPassedReceived"])
        # Validate response contains 1 - Exam Performed, 6 - Lab Results Received, 8 - ProviderPayableEventReceived, 9 - ProviderPayRequestSent, relevant cdi event
        * match result[*].ExamStatusCodeId contains [1, 6, 8, 9, <statusId>]


        # Validate ProviderPayRequestSent Status in Kafka
        * json event = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("egfr_status", evaluation.evaluationId + '', "ProviderPayRequestSent", 10, 5000))
        * match event.ProviderPayProductCode == 'EGFR' 
        * match event.ProductCode == 'EGFR'
        * match event.PaymentId == providerPay.PaymentId.toString()
        * match event.ParentEventDateTime == '#notnull'
        * match event.EvaluationId == evaluation.evaluationId
        * match event.MemberPlanId == appointment.memberPlanId
        * match event.ProviderId.toString() == appointment.providerId
        * match event.CreatedDate.toString().split('+')[1] == "00:00"
        * match event.ReceivedDate.toString().split('+')[1] == "00:00"
        
        Examples:
        | summary                   | CMP_eGFRResult   | CMP_EstimatedGlomerularFiltrationRateResultColor|  CMP_Description     | cdiEventHeaderName    | payProvider   | statusId  |
        | "cdiPassedAndNormal"      | 65               |                                                 | Normal               | "CDIPassedEvent"      | true          | 11      |
        # | "cdiFailededAndNormal"    | 65               | 1.27                 | "CDIFailedEvent"      | true          | 12      |
        | "cdiPassedAndAbnormal"    | 55               |                                                 | Normal               | "CDIPassedEvent"      | true          | 11      |
        # | "cdiFailedAndAbnormal"    | 55               | 1.27                 | "CDIFailedEvent"      | true          | 12      |
        
    @TestCaseKey=ANC-T632
    @ignore
    Scenario Outline: eGFR ProviderPay - CDIFailed(PayProvider: false) before LabResults - Business Rules Met
        * set evaluation.answers =
            """
                [
                    {
                        "AnswerId": 52456,
                        "AnsweredDateTime": "#(timestamp)",
                        "AnswerValue": "1"
                    },
                    {
                        "AnswerId": 51261,
                        "AnsweredDateTime": "#(timestamp)",
                        "AnswerValue": "1"
                    },
                    {
                        "AnswerId": 52484,
                        "AnsweredDateTime": "#(timestamp)",
                        "AnswerValue": #(DataGen().GetLGCBarcode())
                    },
                    {
                        "AnswerId": 52483,
                        "AnsweredDateTime": "#(timestamp)",
                        "AnswerValue": #(DataGen().GetAlfacode())
                    },
                    {
                        "AnswerId": 52480,
                        "AnsweredDateTime": "#(timestamp)",
                        "AnswerValue": #(DataGen().GetAlfacode())
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
        
        * def result = EgfrDb().getResultsByEvaluationId(evaluation.evaluationId)[0]
        
        * json performedEvent = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("egfr_status", evaluation.evaluationId + '', "Performed", 10, 5000))
        * match performedEvent.EvaluationId == evaluation.evaluationId

        # Publish the cdi event to the cdi_events topic
        * string cdiEventKey = evaluation.evaluationId
        * string cdiEventHeader = { 'Type' : <cdiEventHeaderName>}
        * string cdiEventValue = {"RequestId":"#(eventId)","EvaluationId": "#(evaluation.evaluationId)","DateTime":"#(cdiDateTime)","Username":"karateTestUser","ApplicationId":"manual","Reason":"reschedule","PayProvider":<payProvider>,"Products":[{"EvaluationId":"#(evaluation.evaluationId)","ProductCode":"HHRA"},{"EvaluationId":"#(evaluation.evaluationId)","ProductCode":"EGFR"}]}
        * kafkaProducerHelper.send(cdiTopicName, cdiEventKey, cdiEventHeader, cdiEventValue)

        # Publish the homeaccess lab results
        * string homeAccessResultsReceivedHeader = {'Type': 'KedEgfrLabResult'}
        * string homeaccessTopic = "dps_labresult_egfr"
        * def ProperDateOfService = DataGen().getUtcDateTimeString(result.DateOfService.toString())
        * def EgfrResult = CMP_eGFRResult == null ? null : (CMP_eGFRResult)
        * string resultsReceivedValue = {'EvaluationId': '#(parseInt(evaluation.evaluationId))','DateLabReceived': '#(ProperDateOfService)',,'EgfrResult': '#(EgfrResult)','EstimatedGlomerularFiltrationRateResultDescription': '#(CMP_Description)','EstimatedGlomerularFiltrationRateResultColor': '#(CMP_EstimatedGlomerularFiltrationRateResultColor)'}
        * kafkaProducerHelper.send(homeaccessTopic, memberDetails.censeoId, homeAccessResultsReceivedHeader, resultsReceivedValue)
        * eval sleep(2000) 

        # Validate ProviderNonPayableEventReceived Status in Kafka
        * json nonPayableEvent = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("egfr_status", evaluation.evaluationId + '', "ProviderNonPayableEventReceived", 10, 5000))   
        * match nonPayableEvent.EvaluationId == evaluation.evaluationId
        * match nonPayableEvent.ProviderId == providerDetails.providerId
        * match nonPayableEvent.ParentCdiEvent == <cdiEventHeaderName>
        * match nonPayableEvent.Reason == "#notnull"

        # Validate the entry in the ProviderPay table
        * def providerPay = EgfrDb().getProviderPayResultByEvaluationId(evaluation.evaluationId)[0]
        * match providerPay == "#null"

        # Validate Exam Status Update in database
        * def result = EgfrDb().queryExamWithStatusList(evaluation.evaluationId,["Exam Performed","Lab Results Received", "ProviderNonPayableEventReceived", "CDIFailedWithoutPayReceived"])
        # Validate response contains 1 - Exam Performed, 6 - Lab Results Received, 10 - ProviderNonPayableEventReceived, relevant cdi event
        * match result[*].ExamStatusCodeId contains [1, 6, 10, 13]

        Examples:
        | summary                           | CMP_eGFRResult  | CMP_EstimatedGlomerularFiltrationRateResultColor|  CMP_Description      | cdiEventHeaderName    | payProvider   | 
        | "cdiFailedWithoutPayAndNormal"    | 65              |                                                 | Normal                | "CDIFailedEvent"      | false         | 
        | "cdiFailedWithoutPayAndAbnormal"  | 55              |                                                 | Normal                | "CDIFailedEvent"      | false         | 
    
    @TestCaseKey=ANC-T633
    Scenario Outline: eGFR ProviderPay - CdiEvents before LabResults - Business Rules Not Met
        * set evaluation.answers =
            """
                [
                    {
                        "AnswerId": 52456,
                        "AnsweredDateTime": "#(timestamp)",
                        "AnswerValue": "1"
                    },
                    {
                        "AnswerId": 51261,
                        "AnsweredDateTime": "#(timestamp)",
                        "AnswerValue": "1"
                    },
                    {
                        "AnswerId": 52484,
                        "AnsweredDateTime": "#(timestamp)",
                        "AnswerValue": #(DataGen().GetLGCBarcode())
                    },
                    {
                        "AnswerId": 52483,
                        "AnsweredDateTime": "#(timestamp)",
                        "AnswerValue": #(DataGen().GetAlfacode())
                    },
                    {
                        "AnswerId": 52480,
                        "AnsweredDateTime": "#(timestamp)",
                        "AnswerValue": #(DataGen().GetAlfacode())
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
        
        * def result = EgfrDb().getResultsByEvaluationId(evaluation.evaluationId)[0]
        
        * json performedEvent = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("egfr_status", evaluation.evaluationId + '', "Performed", 10, 5000))
        * match performedEvent.EvaluationId == evaluation.evaluationId

        # Publish the cdi event to the cdi_events topic
        * string cdiEventKey = evaluation.evaluationId
        * string cdiEventHeader = { 'Type' : <cdiEventHeaderName>}
        * string cdiEventValue = {"RequestId":"#(eventId)","EvaluationId": "#(evaluation.evaluationId)","DateTime":"#(cdiDateTime)","Username":"karateTestUser","ApplicationId":"manual","Reason":"reschedule","PayProvider":<payProvider>,"Products":[{"EvaluationId":"#(evaluation.evaluationId)","ProductCode":"HHRA"},{"EvaluationId":"#(evaluation.evaluationId)","ProductCode":"EGFR"}]}
        * kafkaProducerHelper.send(cdiTopicName, cdiEventKey, cdiEventHeader, cdiEventValue)
        
        # Publish the homeaccess lab results
        * string homeAccessResultsReceivedHeader = {'Type': 'KedEgfrLabResult'}
        * string homeaccessTopic = "dps_labresult_egfr"
        * def ProperDateOfService = DataGen().getUtcDateTimeString(result.DateOfService.toString())
        * def EgfrResult = CMP_eGFRResult == null ? null : (CMP_eGFRResult)
        * string resultsReceivedValue = {'EvaluationId': '#(parseInt(evaluation.evaluationId))','DateLabReceived': '#(ProperDateOfService)',,'EgfrResult': '#(EgfrResult)','EstimatedGlomerularFiltrationRateResultDescription': '#(CMP_Description)','EstimatedGlomerularFiltrationRateResultColor': '#(CMP_EstimatedGlomerularFiltrationRateResultColor)'}
        * kafkaProducerHelper.send(homeaccessTopic, memberDetails.censeoId, homeAccessResultsReceivedHeader, resultsReceivedValue)
        * eval sleep(2000) 

        # Validate ProviderNonPayableEventReceived Status in Kafka
        * json nonPayableEvent = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("egfr_status", evaluation.evaluationId + '', "ProviderNonPayableEventReceived", 10, 5000))   
        * match nonPayableEvent.EvaluationId == evaluation.evaluationId
        * match nonPayableEvent.ProviderId == providerDetails.providerId
        * match nonPayableEvent.ParentCdiEvent == <cdiEventHeaderName>
        * match nonPayableEvent.Reason == "#notnull"

        # Validate the entry in the ProviderPay table
        * def providerPay = EgfrDb().getProviderPayResultByEvaluationId(evaluation.evaluationId)[0]
        * match providerPay == "#null"

        # Validate Exam Status Update in database
        * def result = EgfrDb().queryExamWithStatusList(evaluation.evaluationId,["Exam Performed","Lab Results Received", "ProviderNonPayableEventReceived","CDIPassedReceived"])
        # Validate response contains 1 - Exam Performed, 6 - Lab Results Received, 7 - Bill Request Not Sent, 5 - Client PDF Delivered
        * match result[*].ExamStatusCodeId contains [1, 6, 10, <statusId>]

        Examples:
        | summary                               | CMP_eGFRResult | CMP_EstimatedGlomerularFiltrationRateResultColor|  CMP_Description     | cdiEventHeaderName    | payProvider  | statusId  |
        | "cdiPassedAndUndetermined"            | -1             |                                                 | Normal               | "CDIPassedEvent"      | true         | 11      |
        | "cdiPassedAndUndetermined"            | 0              |                                                 | Normal               | "CDIPassedEvent"      | true         | 11      |
        | "cdiPassedAndUndetermined"            |                |                                                 | Normal               | "CDIPassedEvent"      | true         | 11      |
        # | "cdiFailedWithPayAndUndetermined"     | -1             | 1.27                 | "CDIFailedEvent"      | true         | 12      |
        # | "cdiFailedWithPayAndUndetermined"     | 0              | 1.27                 | "CDIFailedEvent"      | true         | 12      |
        # | "cdiFailedWithPayAndUndetermined"     |                | 1.27                 | "CDIFailedEvent"      | true         | 12      |
        # | "cdiFailedWithoutPayAndUndetermined"  | -1             | 1.27                 | "CDIFailedEvent"      | false        | 13      |
        # | "cdiFailedWithoutPayAndUndetermined"  | 0              | 1.27                 | "CDIFailedEvent"      | false        | 13      |
        # | "cdiFailedWithoutPayAndUndetermined"  |                | 1.27                 | "CDIFailedEvent"      | false        | 13      |

    @TestCaseKey=ANC-T634
    Scenario Outline: eGFR ProviderPay - CdiEvents - NotPerformed
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
        
        * def result = EgfrDb().getResultsByEvaluationId(evaluation.evaluationId)[0]

        # Publish the homeaccess lab results
        * string homeAccessResultsReceivedHeader = {'Type': 'KedEgfrLabResult'}
        * string homeaccessTopic = "dps_labresult_egfr"
        * def ProperDateOfService = DataGen().getUtcDateTimeString(result.DateOfService.toString())
        * def EgfrResult = CMP_eGFRResult == null ? null : (CMP_eGFRResult)
        * string resultsReceivedValue = {'EvaluationId': '#(parseInt(evaluation.evaluationId))','DateLabReceived': '#(ProperDateOfService)',,'EgfrResult': '#(EgfrResult)','EstimatedGlomerularFiltrationRateResultDescription': '#(CMP_Description)','EstimatedGlomerularFiltrationRateResultColor': '#(CMP_EstimatedGlomerularFiltrationRateResultColor)'}
        * kafkaProducerHelper.send(homeaccessTopic, memberDetails.censeoId, homeAccessResultsReceivedHeader, resultsReceivedValue)
        * eval sleep(2000) 
        
        * json performedEvent = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("egfr_status", evaluation.evaluationId + '', "NotPerformed", 10, 5000))
        * match performedEvent.EvaluationId == evaluation.evaluationId

        # Publish the cdi event to the cdi_events topic
        * string cdiEventKey = evaluation.evaluationId
        * string cdiEventHeader = { 'Type' : <cdiEventHeaderName>}
        * string cdiEventValue = {"RequestId":"#(eventId)","EvaluationId": "#(evaluation.evaluationId)","DateTime":"#(cdiDateTime)","Username":"karateTestUser","ApplicationId":"manual","Reason":"reschedule","PayProvider":<payProvider>,"Products":[{"EvaluationId":"#(evaluation.evaluationId)","ProductCode":"HHRA"},{"EvaluationId":"#(evaluation.evaluationId)","ProductCode":"EGFR"}]}
        * kafkaProducerHelper.send(cdiTopicName, cdiEventKey, cdiEventHeader, cdiEventValue)
        
        # Validate ProviderNonPayableEventReceived Status in Kafka
        * json nonPayableEvent = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("egfr_status", evaluation.evaluationId + '', "ProviderNonPayableEventReceived", 5, 5000))   
        * match nonPayableEvent == {}
       
        # Validate ProviderPayableEventReceived Status in Kafka
        * json payableEvent = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("egfr_status", evaluation.evaluationId + '', "ProviderPayableEventReceived", 5, 5000))   
        * match payableEvent == {}

        # Validate the entry in the ProviderPay table
        * def providerPay = EgfrDb().getProviderPayResultByEvaluationId(evaluation.evaluationId)[0]
        * match providerPay == "#null"

        # Validate Exam Status Update in database
        * def result = EgfrDb().queryExamWithStatusList(evaluation.evaluationId,["Exam Not Performed"])
        #* def statusToCheck = result[0].ExamStatusCodeId.toString() + "," + result[1].ExamStatusCodeId.toString()
        # Validate response contains 2 - Exam Not Performed
        * match result[*].ExamStatusCodeId contains 2
        # * match result[*].ExamStatusCodeId !contains <statusId>

        Examples:
        | CMP_eGFRResult | CMP_EstimatedGlomerularFiltrationRateResultColor|  CMP_Description     | answer_id | answer_value                                                     | expected_reason    | cdiEventHeaderName    | payProvider   | statusId  |
        | 65             |                                                 | Normal               | 51266     | "Technical issue (please call Mobile Support at (877) 570-9359)" | "Technical issue"  | "CDIPassedEvent"      | true          | 11        |
        | 65             |                                                 | Normal               | 51266     | "Technical issue (please call Mobile Support at (877) 570-9359)" | "Technical issue"  | "CDIFailedEvent"      | true          | 12        |
        | 65             |                                                 | Normal               | 51266     | "Technical issue (please call Mobile Support at (877) 570-9359)" | "Technical issue"  | "CDIFailedEvent"      | false         | 13        |
