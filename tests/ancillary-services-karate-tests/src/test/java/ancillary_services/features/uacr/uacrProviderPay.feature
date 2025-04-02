@uacr
@envnot=prod
Feature: uACR ProviderPay Tests

    Background:
        * eval if (env == 'prod') karate.abort();
        * def DataGen = function() { var DataGen = Java.type('helpers.data.DataGen'); return new DataGen(); }
        * def UacrDb = function() { var UacrDb = Java.type('helpers.database.uacr.UacrDb'); return new UacrDb(); }
        * def timestamp = DataGen().isoTimestamp()
        * def dateStamp = DataGen().isoDateStamp()
        * def cdiDateTime = DataGen().timestampWithOffset("-05:00", -1)
        * def eventId = DataGen().uuid()
        * def KafkaConsumerHelper = Java.type('helpers.kafka.KafkaConsumerHelper')
        * def KafkaProducerHelper = Java.type('helpers.kafka.KafkaProducerHelper')
        * def memberDetails = karate.call("classpath:helpers/member/createMember.js")
        * def appointment = karate.call('classpath:helpers/appointment/createAppointment.feature', { products: ['HHRA', 'UACR'] }).response
        * def evaluation = karate.call('classpath:helpers/eval/startEval.feature').response
        * def sleep = function(pause){ java.lang.Thread.sleep(pause) }

    @TestCaseKey=ANC-T759
    Scenario Outline: Verify EvalStatus when CDIPassedEvent event is published after LabResults - Business Rules Met
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
        
        * def result = UacrDb().getExamDates(evaluation.evaluationId)
        
        # Publish the homeaccess lab results event 
        * string homeAccessResultsReceivedHeader = {'Type': 'KedUacrLabResult'}
        * string homeaccessTopic = "dps_labresult_uacr"
        * def ProperDateOfService = DataGen().getUtcDateTimeString(result[0].DateOfService.toString())
        * def resultsReceivedValue = 
        """
            {
                "EvaluationId": #(evaluation.evaluationId),
                "DateLabReceived": '#(ProperDateOfService)',
                "UrineAlbuminToCreatinineRatioResultColor" : <CMP_uacrResultColor>,
                "CreatinineResult" : <CMP_CreatinineResult>,
                "UrineAlbuminToCreatinineRatioResultDescription" : "Performed",
                "UacrResult": <CMP_uACRResult>,
            }
        """

        * string resultsReceivedValueStr = resultsReceivedValue
        * KafkaProducerHelper.send(homeaccessTopic, evaluation.evaluationId+'', homeAccessResultsReceivedHeader, resultsReceivedValueStr)
        * eval sleep(2000)

        * json performedEvent = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("uacr_status", evaluation.evaluationId + '', "Performed", 10, 5000))
        * match performedEvent.EvaluationId == evaluation.evaluationId

        # Publish the cdi event to the cdi_events topic
        * string cdiEventKey = evaluation.evaluationId
        * string cdiEventHeader = { 'Type' : "CDIPassedEvent"}
        * def cdiEventValue = 
        """
            {
                "RequestId":"#(eventId)",
                "EvaluationId": "#(evaluation.evaluationId)",
                "DateTime":"#(cdiDateTime)",
                "Username":"karateTestUser",
                "ApplicationId":"manual",
                "Reason":"reschedule",
                "PayProvider":<payProvider>,
                "Products":[
                    {
                        "EvaluationId":"#(evaluation.evaluationId)",
                        "ProductCode":"HHRA"
                    },
                    {
                        "EvaluationId":"#(evaluation.evaluationId)",
                        "ProductCode":"UACR"
                    }
                ]
            }
        """
        * string cdiEventValueStr = cdiEventValue
        * KafkaProducerHelper.send("cdi_events", cdiEventKey, cdiEventHeader, cdiEventValueStr)
        
        # Validate ProviderPayableEventReceived Status in Kafka
        * json payableEvent = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("uacr_status", evaluation.evaluationId + '', "ProviderPayableEventReceived", 10, 5000))   
        * match payableEvent.EvaluationId == evaluation.evaluationId
        * match payableEvent.ProviderId == providerDetails.providerId
        * match payableEvent.ParentCdiEvent == "CDIPassedEvent"

        #Validate the entry in the ProviderPay table
        * def providerPay = UacrDb().getProviderPayResultByEvaluationId(evaluation.evaluationId)[0]
        * match providerPay.PaymentId == "#notnull"
        * match providerPay.ExamId == "#notnull"

        # Validate Exam Status Update in database
        * def result = UacrDb().queryExamWithStatusList(evaluation.evaluationId,["Exam Performed", "Lab Results Received", "ProviderPayableEventReceived", "ProviderPayRequestSent", "CDIPassedReceived"])
        # Validate response contains 1 - Exam Performed, 6 - Lab Results Received, 8 - ProviderPayableEventReceived, 9 - ProviderPayRequestSent, relevant cdi event
        * match result[*].ExamStatusCodeId contains [1, 6, 8, 9, <statusId>]

        # Validate ProviderPayRequestSent Status in Kafka
        * json event = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("uacr_status", evaluation.evaluationId + '', "ProviderPayRequestSent", 10, 5000))
        * match event.ProviderPayProductCode == 'UACR' 
        * match event.ProductCode == 'UACR'
        * match event.PaymentId == '#notnull'
        * match event.ParentEventDateTime == '#notnull'
        * match event.EvaluationId == evaluation.evaluationId
        * match event.MemberPlanId == appointment.memberPlanId
        * match event.ProviderId.toString() == appointment.providerId
        * match event.CreatedDate.toString().split('+')[1] == "00:00"
        * match event.ReceivedDate.toString().split('+')[1] == "00:00"
        
        Examples:
        | CMP_uACRResult | CMP_CreatinineResult | CMP_uacrResultColor | statusId | payProvider |
        | 29             | 1.07                 | 'Green'             | 11       | true        |
        | 30             | 1.27                 | 'Red'               | 11       | true        |
        | 31             | 1.27                 | 'Red'               | 11       | true        |
        
    @TestCaseKey=ANC-T757
    Scenario Outline: Verify EvalStatus when CDIPassedEvent event is published after LabResults - Business Rules Not Met
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
        
        * def result = UacrDb().getExamDates(evaluation.evaluationId)
        
        # Publish the homeaccess lab results event 
        * string homeAccessResultsReceivedHeader = {'Type': 'KedUacrLabResult'}
        * string homeaccessTopic = "dps_labresult_uacr"
        * def ProperDateOfService = DataGen().getUtcDateTimeString(result[0].DateOfService.toString())
        * def resultsReceivedValue = 
        """
            {
                "EvaluationId": #(evaluation.evaluationId),
                "DateLabReceived": '#(ProperDateOfService)',
                "UrineAlbuminToCreatinineRatioResultColor" : <CMP_uacrResultColor>,
                "CreatinineResult" : <CMP_CreatinineResult>,
                "UrineAlbuminToCreatinineRatioResultDescription" : "Performed",
                "UacrResult": <CMP_uACRResult>,
            }
        """

        * string resultsReceivedValueStr = resultsReceivedValue
        * KafkaProducerHelper.send(homeaccessTopic, evaluation.evaluationId+'', homeAccessResultsReceivedHeader, resultsReceivedValueStr)
        * eval sleep(2000)

        * json performedEvent = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("uacr_status", evaluation.evaluationId + '', "Performed", 10, 5000))
        * match performedEvent.EvaluationId == evaluation.evaluationId

        # Publish the cdi event to the cdi_events topic
        * string cdiEventKey = evaluation.evaluationId
        * string cdiEventHeader = { 'Type' : "CDIPassedEvent"}
        * def cdiEventValue = 
        """
            {
                "RequestId":"#(eventId)",
                "EvaluationId": "#(evaluation.evaluationId)",
                "DateTime":"#(cdiDateTime)",
                "Username":"karateTestUser",
                "ApplicationId":"manual",
                "Reason":"reschedule",
                "PayProvider":<payProvider>,
                "Products":[
                    {
                        "EvaluationId":"#(evaluation.evaluationId)",
                        "ProductCode":"HHRA"
                    },
                    {
                        "EvaluationId":"#(evaluation.evaluationId)",
                        "ProductCode":"UACR"
                    }
                ]
            }
        """
        * string cdiEventValueStr = cdiEventValue
        * KafkaProducerHelper.send("cdi_events", cdiEventKey, cdiEventHeader, cdiEventValueStr)
        
        # Validate ProviderPayableEventReceived Status in Kafka
        * json nonPayableEvent = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("uacr_status", evaluation.evaluationId + '', "ProviderNonPayableEventReceived", 10, 5000))   
        * match nonPayableEvent.EvaluationId == evaluation.evaluationId
        * match nonPayableEvent.ProviderId == providerDetails.providerId
        * match nonPayableEvent.ParentCdiEvent == "CDIPassedEvent"
        * match nonPayableEvent.Reason == "#notnull"

        #Validate the entry in the ProviderPay table
        * def providerPay = UacrDb().getProviderPayResultByEvaluationId(evaluation.evaluationId)[0]
        * match providerPay == "#null"
        

        # Validate Exam Status Update in database
        * def result = UacrDb().queryExamWithStatusList(evaluation.evaluationId,["Exam Performed","Lab Results Received", "ProviderNonPayableEventReceived", "CDIPassedReceived"])
        # Validate response contains 1 - Exam Performed, 6 - Lab Results Received, 10 - ProviderNonPayableEventReceived relevant cdi event
        * match result[*].ExamStatusCodeId contains [1, 6, 10, <statusId>]

        Examples:
        | CMP_uACRResult | CMP_CreatinineResult | CMP_uacrResultColor | statusId | payProvider |
        | 0              | 1.27                 | 'Grey'              | 11       | true        |
        | -1             | 1.27                 | 'Grey'              | 11       | true        |
        |                | 1.27                 | 'Grey'              | 11       | true        |

    @TestCaseKey=ANC-T760
    Scenario Outline: Verify EvalStatus when CDIPassedEvent event is published before LabResults - Business Rules Met
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

        # Publish the cdi event to the cdi_events topic
        * string cdiEventKey = evaluation.evaluationId
        * string cdiEventHeader = { 'Type' : "CDIPassedEvent"}
        * def cdiEventValue = 
        """
            {
                "RequestId":"#(eventId)",
                "EvaluationId": "#(evaluation.evaluationId)",
                "DateTime":"#(cdiDateTime)",
                "Username":"karateTestUser",
                "ApplicationId":"manual",
                "Reason":"reschedule",
                "PayProvider":<payProvider>,
                "Products":[
                    {
                        "EvaluationId":"#(evaluation.evaluationId)",
                        "ProductCode":"HHRA"
                    },
                    {
                        "EvaluationId":"#(evaluation.evaluationId)",
                        "ProductCode":"UACR"
                    }
                ]
            }
        """
        * string cdiEventValueStr = cdiEventValue
        * KafkaProducerHelper.send("cdi_events", cdiEventKey, cdiEventHeader, cdiEventValueStr)
        * eval sleep(2000)

        * def result = UacrDb().getExamDates(evaluation.evaluationId) 

        * json performedEvent = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("uacr_status", evaluation.evaluationId + '', "Performed", 10, 5000))
        * match performedEvent.EvaluationId == evaluation.evaluationId

        # Publish the homeaccess lab results event 
        * string homeAccessResultsReceivedHeader = {'Type': 'KedUacrLabResult'}
        * string homeaccessTopic = "dps_labresult_uacr"
        * def ProperDateOfService = DataGen().getUtcDateTimeString(result[0].DateOfService.toString())
        * def resultsReceivedValue = 
        """
            {
                "EvaluationId": #(evaluation.evaluationId),
                "DateLabReceived": '#(ProperDateOfService)',
                "UrineAlbuminToCreatinineRatioResultColor" : <CMP_uacrResultColor>,
                "CreatinineResult" : <CMP_CreatinineResult>,
                "UrineAlbuminToCreatinineRatioResultDescription" : "Performed",
                "UacrResult": <CMP_uACRResult>,
            }
        """

        * string resultsReceivedValueStr = resultsReceivedValue
        * KafkaProducerHelper.send(homeaccessTopic, evaluation.evaluationId+'', homeAccessResultsReceivedHeader, resultsReceivedValueStr)

        # Validate ProviderPayableEventReceived Status in Kafka
        * json payableEvent = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("uacr_status", evaluation.evaluationId + '', "ProviderPayableEventReceived", 10, 5000))   
        * match payableEvent.EvaluationId == evaluation.evaluationId
        * match payableEvent.ProviderId == providerDetails.providerId
        * match payableEvent.ParentCdiEvent == "CDIPassedEvent"

        #Validate the entry in the ProviderPay table
        * def providerPay = UacrDb().getProviderPayResultByEvaluationId(evaluation.evaluationId)[0]
        * match providerPay.PaymentId == "#notnull"
        * match providerPay.ExamId == "#notnull"

        # Validate Exam Status Update in database
        * def result = UacrDb().queryExamWithStatusList(evaluation.evaluationId,["Exam Performed", "Lab Results Received", "ProviderPayableEventReceived", "ProviderPayRequestSent", "CDIPassedReceived"])
        # Validate response contains 1 - Exam Performed, 6 - Lab Results Received, 8 - ProviderPayableEventReceived, 9 - ProviderPayRequestSent, relevant cdi event
        * match result[*].ExamStatusCodeId contains [1, 6, 8, 9, <statusId>]

        # Validate ProviderPayRequestSent Status in Kafka
        * json event = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("uacr_status", evaluation.evaluationId + '', "ProviderPayRequestSent", 10, 5000))
        * match event.ProviderPayProductCode == 'UACR' 
        * match event.ProductCode == 'UACR'
        * match event.PaymentId == '#notnull'
        * match event.ParentEventDateTime == '#notnull'
        * match event.EvaluationId == evaluation.evaluationId
        * match event.MemberPlanId == appointment.memberPlanId
        * match event.ProviderId.toString() == appointment.providerId
        * match event.CreatedDate.toString().split('+')[1] == "00:00"
        * match event.ReceivedDate.toString().split('+')[1] == "00:00"
        
        Examples:
        | CMP_uACRResult | CMP_CreatinineResult | CMP_uacrResultColor | statusId | payProvider |
        | 29             | 1.07                 | 'Green'             | 11       | true        |
        | 30             | 1.27                 | 'Red'               | 11       | true        |
        | 31             | 1.27                 | 'Red'               | 11       | true        |
        
    
    @TestCaseKey=ANC-T756
    Scenario Outline: Verify EvalStatus when CDIPassedEvent event is published before LabResults - Business Rules Not Met
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
        
        # Publish the cdi event to the cdi_events topic
        * string cdiEventKey = evaluation.evaluationId
        * string cdiEventHeader = { 'Type' : "CDIPassedEvent"}
        * def cdiEventValue = 
        """
            {
                "RequestId":"#(eventId)",
                "EvaluationId": "#(evaluation.evaluationId)",
                "DateTime":"#(cdiDateTime)",
                "Username":"karateTestUser",
                "ApplicationId":"manual",
                "Reason":"reschedule",
                "PayProvider":<payProvider>,
                "Products":[
                    {
                        "EvaluationId":"#(evaluation.evaluationId)",
                        "ProductCode":"HHRA"
                    },
                    {
                        "EvaluationId":"#(evaluation.evaluationId)",
                        "ProductCode":"UACR"
                    }
                ]
            }
        """
        * string cdiEventValueStr = cdiEventValue
        * KafkaProducerHelper.send("cdi_events", cdiEventKey, cdiEventHeader, cdiEventValueStr)
        * eval sleep(2000)

        * json performedEvent = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("uacr_status", evaluation.evaluationId + '', "Performed", 10, 5000))
        * match performedEvent.EvaluationId == evaluation.evaluationId

        * def result = UacrDb().getExamDates(evaluation.evaluationId)
        
        # Publish the homeaccess lab results event 
        * string homeAccessResultsReceivedHeader = {'Type': 'KedUacrLabResult'}
        * string homeaccessTopic = "dps_labresult_uacr"
        * def ProperDateOfService = DataGen().getUtcDateTimeString(result[0].DateOfService.toString())
        * def resultsReceivedValue = 
        """
            {
                "EvaluationId": #(evaluation.evaluationId),
                "DateLabReceived": '#(ProperDateOfService)',
                "UrineAlbuminToCreatinineRatioResultColor" : <CMP_uacrResultColor>,
                "CreatinineResult" : <CMP_CreatinineResult>,
                "UrineAlbuminToCreatinineRatioResultDescription" : "Performed",
                "UacrResult": <CMP_uACRResult>,
            }
        """

        * string resultsReceivedValueStr = resultsReceivedValue
        * KafkaProducerHelper.send(homeaccessTopic, evaluation.evaluationId+'', homeAccessResultsReceivedHeader, resultsReceivedValueStr)
        
        # Validate ProviderPayableEventReceived Status in Kafka
        * json nonPayableEvent = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("uacr_status", evaluation.evaluationId + '', "ProviderNonPayableEventReceived", 10, 5000))   
        * match nonPayableEvent.EvaluationId == evaluation.evaluationId
        * match nonPayableEvent.ProviderId == providerDetails.providerId
        * match nonPayableEvent.ParentCdiEvent == "CDIPassedEvent"
        * match nonPayableEvent.Reason == "#notnull"

        #Validate the entry in the ProviderPay table
        * def providerPay = UacrDb().getProviderPayResultByEvaluationId(evaluation.evaluationId)[0]
        * match providerPay == "#null"

        # Validate Exam Status Update in database
        * def result = UacrDb().queryExamWithStatusList(evaluation.evaluationId,["Exam Performed","Lab Results Received", "ProviderNonPayableEventReceived", "CDIPassedReceived"])
        # Validate response contains 1 - Exam Performed, 6 - Lab Results Received, 10 - ProviderNonPayableEventReceived relevant cdi event
        * match result[*].ExamStatusCodeId contains [1, 6, 10, 11]

        Examples:
        | CMP_uACRResult | CMP_CreatinineResult | CMP_uacrResultColor | payProvider |
        | 0              | 1.27                 | 'Grey'              | true        |
        | -1             | 1.27                 | 'Grey'              | true        |
        |                | 1.27                 | 'Grey'              | true        |