@fobt
@envnot=prod
Feature: Fobt ProviderPay - Cancelled and Missing Evaluation Tests

    Background:
        * eval if (env == 'prod') karate.abort();
        * def DataGen = function() { var DataGen = Java.type("helpers.data.DataGen"); return new DataGen(); }
        * def FobtDb = function() { var FobtDb = Java.type('helpers.database.fobt.FobtDb'); return new FobtDb(); }

        * def timestamp = DataGen().isoTimestamp()
        * def dateStamp = DataGen().isoDateStamp()
        * def cdiDateTime = DataGen().timestampWithOffset("-05:00", -1)
        * def monthDayYearCdi = DataGen().getMonthDayYear(cdiDateTime)

        * def memberDetails = karate.call("classpath:helpers/member/createMember.js")
        * def KafkaConsumerHelper = Java.type('helpers.kafka.KafkaConsumerHelper')
        * def KafkaProducerHelper = Java.type('helpers.kafka.KafkaProducerHelper')

    @TestCaseKey=ANC-T832
    Scenario: Fobt ProviderPay - Evaluation is Cancelled without Finalizing
        # Not finalizing eval will lead to 'exam not found' scenario.
        * def appointment = karate.call('classpath:helpers/appointment/createAppointment.feature', { products: ['HHRA', 'FOBT'] }).response
        * def evaluation = karate.call("classpath:helpers/eval/startEval.feature").response
        * set evaluation.answers =
        """
            [
                {
                    'AnswerId': 21113,
                    'AnsweredDateTime': '#(timestamp)',
                    'AnswerValue': 'Yes'
                },
                {
                    'AnswerId': 21119,
                    'AnsweredDateTime': '#(timestamp)',
                    'AnswerValue': #(randomBarcode),
                },
                {
                    'AnswerId': 22034,
                    'AnsweredDateTime': '#(dateStamp)',
                    'AnswerValue': '#(dateStamp)'
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
       
        # Validate that no entry was made into the Fobt table
        * def evalResult = FobtDb().getExamByEvaluationId(evaluation.evaluationId,10,3000)[0]
        * match evalResult == "#null"
        
        # Publish the cdi event to the cdi_events topic as the events raised by cdi service have PAD instead of Fobt
        * string cdiEventName = "cdi_events"
        * string cdiEventKey = evaluation.evaluationId + "_" + monthDayYearCdi
        * def eventId = DataGen().uuid()
        * string cdiEventHeader = { 'Type' : "CDIPassedEvent"}
         # Including Fobt in product code so that PM processes the event as the one published by cdi has PAD instead of Fobt
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
                        "ProductCode":"FOBT"
                    }
                ]
            }
        """
        * string cdiEventValueStr = cdiEventValue
        * KafkaProducerHelper.send(cdiEventName, cdiEventKey, cdiEventHeader, cdiEventValueStr)
        
        # Validate that there is no entry in the ProviderPay table
        * def providerPay = FobtDb().getProviderPayByEvalId(evaluation.evaluationId)[0]
        * match providerPay == "#null"

        # Validate that the Kafka event - ProviderPayRequestSent - was not raised
        * json event = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("FOBT_Status", evaluation.evaluationId + '', "ProviderPayRequestSent", 10, 5000))
        * match event == {} 
        
        # Validate that the Kafka event - ProviderPayableEventReceived - was not raised
        * json payableEvent = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("FOBT_Status", evaluation.evaluationId + '', "ProviderPayableEventReceived", 10, 5000))   
        * match payableEvent == {} 
        ## Additional checks on error queue count increasing to be added
        ## Additional checks on New Relic dashboard updates to be added

    @TestCaseKey=ANC-T831
    Scenario: Fobt ProviderPay - Converted Evaluations - Evaluation is Finalized after Cancelling
        * def appointment = karate.call('classpath:helpers/appointment/createAppointment.feature', { products: ['HHRA', 'FOBT'] }).response
        * def evaluation = karate.call("classpath:helpers/eval/startEval.feature").response
        * set evaluation.answers =
        """
            [
                {
                    'AnswerId': 21113,
                    'AnsweredDateTime': '#(timestamp)',
                    'AnswerValue': 'Yes'
                },
                {
                    'AnswerId': 21119,
                    'AnsweredDateTime': '#(timestamp)',
                    'AnswerValue': #(randomBarcode),
                },
                {
                    'AnswerId': 22034,
                    'AnsweredDateTime': '#(dateStamp)',
                    'AnswerValue': '#(dateStamp)'
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
        # Including Fobt in product code so that PM processes the event.
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
                        "ProductCode":"FOBT"
                    }
                ]
            }
        """
        * string cdiEventValueStr = cdiEventValue
        * KafkaProducerHelper.send(cdiEventName, cdiEventKey, cdiEventHeader, cdiEventValueStr)
        
        * karate.call("classpath:helpers/eval/finalizeEval.feature")
        
        # Validate that an entry was made into the Fobt table since Eval is finalized
        * def evalResult = FobtDb().getExamByEvaluationId(evaluation.evaluationId)[0]
        * match evalResult == "#notnull"

        # Publish the cdi event to the cdi_events topic 
        * def eventId = DataGen().uuid()
        * string cdiEventHeader = { 'Type' : "CDIPassedEvent"}
        # Including Fobt in product code so that PM processes the event.
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
                        "ProductCode":"FOBT"
                    }
                ]
            }
        """
        * string cdiEventValueStr = cdiEventValue
        * KafkaProducerHelper.send(cdiEventName, cdiEventKey, cdiEventHeader, cdiEventValueStr)

        # Validate Database response contains 18 - CdiPassedReceived
        * def result = FobtDb().queryExamWithStatusList(evaluation.evaluationId,["CdiPassedReceived"])
        * match result[*].FOBTStatusCodeId contains 18

        ## Additional checks on error queue count increasing to be added
        ## Additional checks on New Relic dashboard updates to be added
    
    @TestCaseKey=ANC-T833
    Scenario: Fobt ProviderPay - Missing Evaluations - Evaluation is Finalized but never Cancelled
        # Setting product code other than Fobt so that the evaluation is not captured and added to Fobt database. This will lead to a missing evaluation scenario.
        * def appointment = karate.call('classpath:helpers/appointment/createAppointment.feature', { products: ['HHRA', 'PAD'] }).response
        * def evaluation = karate.call("classpath:helpers/eval/startEval.feature").response
        * set evaluation.answers =
        """
            [
                {
                    'AnswerId': 21113,
                    'AnsweredDateTime': '#(timestamp)',
                    'AnswerValue': 'Yes'
                },
                {
                    'AnswerId': 21119,
                    'AnsweredDateTime': '#(timestamp)',
                    'AnswerValue': #(randomBarcode),
                },
                {
                    'AnswerId': 22034,
                    'AnsweredDateTime': '#(dateStamp)',
                    'AnswerValue': '#(dateStamp)'
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
        
        # Validate that no entry was made into the Fobt table since the finalized event did not contain Fobt product code
        * def evalResult = FobtDb().getExamByEvaluationId(evaluation.evaluationId,10,3000)[0]
        * match evalResult == "#null"
        
        # Publish the cdi event to the cdi_events topic as the events raised by cdi service have CKD instead of Fobt
        * string cdiEventName = "cdi_events"
        * string cdiEventKey = evaluation.evaluationId + "_" + monthDayYearCdi
        * def eventId = DataGen().uuid()
        * string cdiEventHeader = { 'Type' : "CDIPassedEvent"}
        # Including Fobt in product code so that PM processes the event as the one published by cdi has PAD instead of Fobt
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
                        "ProductCode":"FOBT"
                    }
                ]
            }
        """
        * string cdiEventValueStr = cdiEventValue
        * KafkaProducerHelper.send(cdiEventName, cdiEventKey, cdiEventHeader, cdiEventValueStr)
        
        # Validate that there is no entry in the ProviderPay table
        * def providerPay = FobtDb().getProviderPayByEvalId(evaluation.evaluationId)[0]
        * match providerPay == "#null"

        # Validate that the Kafka event - ProviderPayRequestSent - was not raised
        * json event = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("FOBT_Status", evaluation.evaluationId + '', "ProviderPayRequestSent", 10, 5000))
        * match event == {} 
        
        # Validate that the Kafka event - ProviderPayableEventReceived - was not raised
        * json payableEvent = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("FOBT_Status", evaluation.evaluationId + '', "ProviderPayableEventReceived", 10, 5000))   
        * match payableEvent == {} 

        ## Additional checks on error queue count increasing to be added
        ## Additional checks on New Relic dashboard updates to be added

