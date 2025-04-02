@hba1cpoc
@envnot=prod
Feature: Hba1cpoc ProviderPay - Cancelled and Missing Evaluation Tests

    Background:
        * def DataGen = function() { var DataGen = Java.type("helpers.data.DataGen"); return new DataGen(); }
        * def Hba1cpocDb = function() { var Hba1cpocDb = Java.type('helpers.database.hba1cpoc.Hba1cpocDb'); return new Hba1cpocDb(); }

        * def timestamp = DataGen().isoTimestamp()
        * def dateStamp = DataGen().isoDateStamp()
        * def cdiDateTime = DataGen().timestampWithOffset("-05:00", -1)
        * def monthDayYearCdi = DataGen().getMonthDayYear(cdiDateTime)

        * def memberDetails = karate.call("classpath:helpers/member/createMember.js")
        * def KafkaConsumerHelper = Java.type('helpers.kafka.KafkaConsumerHelper')
        * def KafkaProducerHelper = Java.type('helpers.kafka.KafkaProducerHelper')

    Scenario: Hba1cpoc ProviderPay - Evaluation is Cancelled without Finalizing
        # Not finalizing eval will lead to 'exam not found' scenario.
        * def appointment = karate.call('classpath:helpers/appointment/createAppointment.feature', { products: ['HHRA', 'Hba1cpoc'] }).response
        * def evaluation = karate.call("classpath:helpers/eval/startEval.feature").response
        * set evaluation.answers =
        """
            [
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
        # Save Stop Cancel
        * karate.call("classpath:helpers/eval/saveEval.feature")
        * karate.call("classpath:helpers/eval/stopEval.feature")
        * karate.call("classpath:helpers/eval/cancelEval.feature")
       
        # Validate that no entry was made into the Hba1cpoc table
        * def evalResult = Hba1cpocDb().getResultsByEvaluationId(evaluation.evaluationId)[0]
        * match evalResult == "#null"
        
        # Publish the cdi event to the cdi_events topic as the events raised by cdi service have PAD instead of Hba1cpoc
        * string cdiEventName = "cdi_events"
        * string cdiEventKey = evaluation.evaluationId + "_" + monthDayYearCdi
        * def eventId = DataGen().uuid()
        * string cdiEventHeader = { 'Type' : "CDIPassedEvent"}
         # Including Hba1cpoc in product code so that PM processes the event as the one published by cdi has PAD instead of Hba1cpoc
        * def cdiEventValue = 
        """
            {
                "RequestId":"#(eventId)",
                "EvaluationId": "#(evaluation.evaluationId)",
                "DateTime":"#(cdiDateTime)",
                "Username":"karateTestUser",
                "ApplicationId":"manual",
                "Reason":"reschedule",
                "Products":[
                    {
                        "EvaluationId":"#(evaluation.evaluationId)",
                        "ProductCode":"HHRA"
                    },
                    {
                        "EvaluationId":"#(evaluation.evaluationId)",
                        "ProductCode":"Hba1cpoc"
                    }
                ]
            }
        """
        * string cdiEventValueStr = cdiEventValue
        * KafkaProducerHelper.send(cdiEventName, cdiEventKey, cdiEventHeader, cdiEventValueStr)
        
        # Validate that there is no entry in the ProviderPay table
        * def providerPay = Hba1cpocDb().getProviderPayResultsWithEvalId(evaluation.evaluationId)[0]
        * match providerPay == "#null"

        # Validate that the Kafka event - ProviderPayRequestSent - was not raised
        * json event = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("A1CPOC_Status", evaluation.evaluationId + '', "ProviderPayRequestSent", 10, 5000))
        * match event == {} 
        
        # Validate that the Kafka event - ProviderPayableEventReceived - was not raised
        * json payableEvent = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("A1CPOC_Status", evaluation.evaluationId + '', "ProviderPayableEventReceived", 10, 5000))   
        * match payableEvent == {} 
        ## Additional checks on error queue count increasing to be added
        ## Additional checks on New Relic dashboard updates to be added

  
    Scenario: Hba1cpoc ProviderPay - Converted Evaluations - Evaluation is Finalized after Cancelling
        * def appointment = karate.call('classpath:helpers/appointment/createAppointment.feature', { products: ['HHRA', 'HBA1CPOC'] }).response
        * def evaluation = karate.call("classpath:helpers/eval/startEval.feature").response
        * def timestamp = DataGen().isoTimestamp()
        * def dateStamp = DataGen().isoDateStamp()
        * def expirationDate = DataGen().isoDateStamp(30)
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
                    "AnswerValue": 'abc',
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
        # Save Stop Cancel Finalize
        * karate.call("classpath:helpers/eval/saveEval.feature")
        * karate.call("classpath:helpers/eval/stopEval.feature")
        * karate.call("classpath:helpers/eval/cancelEval.feature")        
        
        # Publish the cdi event to the cdi_events topic
        * string cdiEventName = "cdi_events"
        * string cdiEventKey = evaluation.evaluationId + "_" + monthDayYearCdi
        * def eventId = DataGen().uuid()
        * string cdiEventHeader = { 'Type' : "CDIFailedEvent"}
        # Including Hba1cpoc in product code so that PM processes the event. Explicitly publishing the event as the one published by cdi does not have Hba1cpoc Product Code
        * def cdiEventValue = 
        """
            {
                "RequestId":"#(eventId)",
                "EvaluationId": "#(evaluation.evaluationId)",
                "DateTime":"#(cdiDateTime)",
                "Username":"karateTestUser",
                "ApplicationId":"manual",
                "Reason":"reschedule",
                "PayProvider": false,
                "Products":[
                    {
                        "EvaluationId":"#(evaluation.evaluationId)",
                        "ProductCode":"HHRA"
                    },
                    {
                        "EvaluationId":"#(evaluation.evaluationId)",
                        "ProductCode":"HBA1CPOC"
                    }
                ]
            }
        """
        * string cdiEventValueStr = cdiEventValue
        * KafkaProducerHelper.send(cdiEventName, cdiEventKey, cdiEventHeader, cdiEventValueStr)
        
        * def sleep = function(pause){ java.lang.Thread.sleep(pause) }
        * sleep(5000)
        * karate.call("classpath:helpers/eval/finalizeEval.feature")
        
        # Validate that no entry was made into the Hba1cpoc table since Product Code was missing
        * def evalResult = Hba1cpocDb().getResultsByEvaluationId(evaluation.evaluationId)[0]

        # Publish the cdi event to the cdi_events topic 
        * def eventId = DataGen().uuid()
        * string cdiEventHeader = { 'Type' : "CDIPassedEvent"}
        # Including Hba1cpoc in product code so that PM processes the event.
        * def cdiEventValue = 
        """
            {
                "RequestId":"#(eventId)",
                "EvaluationId": "#(evaluation.evaluationId)",
                "DateTime":"#(cdiDateTime)",
                "Username":"karateTestUser",
                "ApplicationId":"manual",
                "Reason":"reschedule",
                "Products":[
                    {
                        "EvaluationId":"#(evaluation.evaluationId)",
                        "ProductCode":"HHRA"
                    },
                    {
                        "EvaluationId":"#(evaluation.evaluationId)",
                        "ProductCode":"HBA1CPOC"
                    }
                ]
            }
        """
        * string cdiEventValueStr = cdiEventValue
        * KafkaProducerHelper.send(cdiEventName, cdiEventKey, cdiEventHeader, cdiEventValueStr)

        # Validate Database response contains 10 - CDIPassedReceived
        * def result = Hba1cpocDb().queryExamWithStatusList(evaluation.evaluationId,["CdiPassedReceived"])
        * match result[*].HBA1CPOCStatusCodeId contains 10

        ## Additional checks on error queue count increasing to be added
        ## Additional checks on New Relic dashboard updates to be added

    
 
    Scenario: Hba1cpoc ProviderPay - Missing Evaluations - Evaluation is Finalized but never Cancelled
        # Setting product code other than Hba1cpoc so that the evaluation is not captured and added to Hba1cpoc database. This will lead to a missing evaluation scenario.
        * def appointment = karate.call('classpath:helpers/appointment/createAppointment.feature', { products: ['HHRA', 'PAD'] }).response
        * def evaluation = karate.call("classpath:helpers/eval/startEval.feature").response
        * set evaluation.answers =
        """
            [
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
        # Save Stop Finalize
        * karate.call("classpath:helpers/eval/saveEval.feature")
        * karate.call("classpath:helpers/eval/stopEval.feature")
        * karate.call("classpath:helpers/eval/finalizeEval.feature")
        
        # Validate that no entry was made into the Hba1cpoc table since the finalized event did not contain Hba1cpoc product code
        * def evalResult = Hba1cpocDb().getResultsByEvaluationId(evaluation.evaluationId)[0]
        * match evalResult == "#null"
        
        # Publish the cdi event to the cdi_events topic as the events raised by cdi service have CKD instead of Hba1cpoc
        * string cdiEventName = "cdi_events"
        * string cdiEventKey = evaluation.evaluationId + "_" + monthDayYearCdi
        * def eventId = DataGen().uuid()
        * string cdiEventHeader = { 'Type' : "CDIPassedEvent"}
        # Including Hba1cpoc in product code so that PM processes the event as the one published by cdi has PAD instead of Hba1cpoc
        * def cdiEventValue = 
        """
            {
                "RequestId":"#(eventId)",
                "EvaluationId": "#(evaluation.evaluationId)",
                "DateTime":"#(cdiDateTime)",
                "Username":"karateTestUser",
                "ApplicationId":"manual",
                "Reason":"reschedule",
                "Products":[
                    {
                        "EvaluationId":"#(evaluation.evaluationId)",
                        "ProductCode":"HHRA"
                    },
                    {
                        "EvaluationId":"#(evaluation.evaluationId)",
                        "ProductCode":"HBA1CPOC"
                    }
                ]
            }
        """
        * string cdiEventValueStr = cdiEventValue
        * KafkaProducerHelper.send(cdiEventName, cdiEventKey, cdiEventHeader, cdiEventValueStr)
        
        # Validate that there is no entry in the ProviderPay table
        * def providerPay = Hba1cpocDb().getProviderPayResultsWithEvalId(evaluation.evaluationId)[0]
        * match providerPay == "#null"

        # Validate that the Kafka event - ProviderPayRequestSent - was not raised
        * json event = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("A1CPOC_Status", evaluation.evaluationId + '', "ProviderPayRequestSent", 10, 5000))
        * match event == {} 
        
        # Validate that the Kafka event - ProviderPayableEventReceived - was not raised
        * json payableEvent = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("A1CPOC_Status", evaluation.evaluationId + '', "ProviderPayableEventReceived", 10, 5000))   
        * match payableEvent == {} 

        ## Additional checks on error queue count increasing to be added
        ## Additional checks on New Relic dashboard updates to be added
