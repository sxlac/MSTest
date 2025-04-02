@egfr
@envnot=prod
Feature: eGFR Billing Ked Tests

    Background:
        * eval if (env == 'prod') karate.abort();
        * def DataGen = function() { var DataGen = Java.type('helpers.data.DataGen'); return new DataGen(); }
        * def EgfrDb = function() { var EgfrDb = Java.type('helpers.database.egfr.EgfrDb'); return new EgfrDb(); }
        * def timestamp = DataGen().isoTimestamp()
        * def dateStamp = DataGen().isoDateStamp()
        * def KafkaConsumerHelper = Java.type('helpers.kafka.KafkaConsumerHelper')
        * def kafkaProducerHelper = Java.type('helpers.kafka.KafkaProducerHelper')
        * def memberDetails = karate.call("classpath:helpers/member/createMember.js")
        * def appointment = karate.call('classpath:helpers/appointment/createAppointment.feature', { products: ['HHRA', 'EGFR'] }).response
        * def evaluation = karate.call('classpath:helpers/eval/startEval.feature').response
        * def sleep = function(pause){ java.lang.Thread.sleep(pause) }

  
    Scenario Outline: eGFR Billing and pdfDeliveredToClient
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
        * string resultsReceivedValue = {'EvaluationId': '#(parseInt(evaluation.evaluationId))','DateLabReceived': '#(ProperDateOfService)',,'EgfrResult': '#(CMP_eGFRResult)','EstimatedGlomerularFiltrationRateResultColor': '#(CMP_EstimatedGlomerularFiltrationRateResultColor)','EstimatedGlomerularFiltrationRateResultDescription': '#(CMP_Description)'}
        * kafkaProducerHelper.send(homeaccessTopic, memberDetails.censeoId, homeAccessResultsReceivedHeader, resultsReceivedValue)
        * eval sleep(2000) 

        # Publish the PDF event to the pdfdelivery topic
        * string pdfEventKey = evaluation.evaluationId
        * string batchName = `Ancillary_Services_Karate_Tests_${DataGen().formattedDateStamp('yyyMMdd')}`
        * def batchId = 2
        * def eventId = DataGen().uuid()
        
        * string pdfDeliveredToClientHeader = {'Type': 'PdfDeliveredToClient'}
        * string pdfEventValue = {'EventId': '#(eventId)','EvaluationId': '#(evaluation.evaluationId)','CreatedDateTime': '#(timestamp)','DeliveryDateTime': '#(timestamp)','BatchName': '#(batchName)','ProductCodes':['EGFR'],'BatchId': #(batchId)}
        * kafkaProducerHelper.send("pdfdelivery", pdfEventKey, pdfDeliveredToClientHeader, pdfEventValue) 
        * eval sleep(2000) 
        
        # Validate the entry in the BillRequestSent table
        * def billingResult = EgfrDb().getBillingResultByEvaluationId(evaluation.evaluationId)[0]
        * match billingResult.BillId == '#notnull'
        * match billingResult.ExamId == '#notnull'
        * match billingResult.BillingProductCode == 'eGFR'

        # Validate record present in table PdfDeliveredToClient in database
        * def resultPdfDeliveredToClient = EgfrDb().getPdfDeliveredToClientByEvaluationId(evaluation.evaluationId)[0]
        * match resultPdfDeliveredToClient.EvaluationId == evaluation.evaluationId

        # Validate BillRequestSent Status in Kafka
        * json event = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("egfr_status", evaluation.evaluationId + '', "BillRequestSent", 10, 5000))
        * match event.BillingProductCode == 'EGFR' 
        * match event.BillId == billingResult.BillId.toString()
        * match event.PdfDeliveryDate == '#notnull'
        * match event.EvaluationId == evaluation.evaluationId
        * match event.MemberPlanId == appointment.memberPlanId

        * match event.ProviderId.toString() == appointment.providerId
        * match event.CreatedDate.toString().split('+')[1] == "00:00"
        * match event.ReceivedDate.toString().split('+')[1] == "00:00"
        * match DataGen().RemoveMilliSeconds(event.CreatedDate) == DataGen().getUtcDateTimeString(result.EvaluationCreatedDateTime.toString())
        * match DataGen().RemoveMilliSeconds(event.ReceivedDate) == DataGen().getUtcDateTimeString(result.EvaluationReceivedDateTime.toString())
        
        * string headers = KafkaConsumerHelper.getHeadersByTopicAndKey("egfr_status", evaluation.evaluationId + '', 10, 5000)
        * match headers contains 'Performed' && 'BillRequestSent'

        # Validate Exam Status Update in database
        * def result = EgfrDb().getResultsByEvaluationId(evaluation.evaluationId)
        * match result[*].ExamStatusCodeId contains 1 && 6 && 4 && 5 && 3

        Examples:
        | CMP_eGFRResult | CMP_EstimatedGlomerularFiltrationRateResultColor | CMP_Description | expected_normality_indicator   |is_billable |
        |   65           | Green                                            |                 | 'N'                            |true        |
        |   45           | Green                                            |                 | 'A'                            |true        |

  
    Scenario Outline: eGFR BillRequestNotSent and PdfDeliveredToClient Quest
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
        * string resultsReceivedValue = {'EvaluationId': '#(parseInt(evaluation.evaluationId))','DateLabReceived': '#(ProperDateOfService)',,'EgfrResult': '#(parseInt(CMP_eGFRResult))','EstimatedGlomerularFiltrationRateResultColor': '#(CMP_EstimatedGlomerularFiltrationRateResultColor)','EstimatedGlomerularFiltrationRateResultDescription': '#(CMP_Description)'}
        * kafkaProducerHelper.send(homeaccessTopic, memberDetails.censeoId, homeAccessResultsReceivedHeader, resultsReceivedValue)
        * eval sleep(2000) 
       
        # Publish the PDF event to the pdfdelivery topic
        * string pdfEventKey = evaluation.evaluationId
        * string batchName = `Ancillary_Services_Karate_Tests_${DataGen().formattedDateStamp('yyyMMdd')}`
        * def batchId = 2
        * def eventId = DataGen().uuid()
        
        * string pdfDeliveredToClientHeader = {'Type': 'PdfDeliveredToClient'}
        * string pdfEventValue = {'EventId': '#(eventId)','EvaluationId': '#(evaluation.evaluationId)','CreatedDateTime': '#(timestamp)','DeliveryDateTime': '#(timestamp)','BatchName': '#(batchName)','ProductCodes':['EGFR'],'BatchId': #(batchId)}
        * kafkaProducerHelper.send("pdfdelivery", pdfEventKey, pdfDeliveredToClientHeader, pdfEventValue) 
        * eval sleep(2000) 
        
        # Validate the entry in the BillRequestSent table
        * def billingResult = EgfrDb().getBillingResultByEvaluationId(evaluation.evaluationId)[0]
        * match billingResult == '#null'


        # Validate record present in table PdfDeliveredToClient in database
        * def resultPdfDeliveredToClient = EgfrDb().getPdfDeliveredToClientByEvaluationId(evaluation.evaluationId)[0]
        * match resultPdfDeliveredToClient.EvaluationId == evaluation.evaluationId

        # Validate BillRequestNotSent Status in Kafka
        * json event = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("egfr_status", evaluation.evaluationId + '', "BillRequestNotSent", 10, 5000))
        * match event.BillingProductCode == 'EGFR' 
        * match event.PdfDeliveryDate == '#notnull'
        * match event.EvaluationId == evaluation.evaluationId
        * match event.MemberPlanId == appointment.memberPlanId
        * match event.ProviderId.toString() == appointment.providerId
        * match event.CreatedDate.toString().split('+')[1] == "00:00"
        * match event.ReceivedDate.toString().split('+')[1] == "00:00"
        * match DataGen().RemoveMilliSeconds(event.CreatedDate) == DataGen().getUtcDateTimeString(result.EvaluationCreatedDateTime.toString())
        * match DataGen().RemoveMilliSeconds(event.ReceivedDate) == DataGen().getUtcDateTimeString(result.EvaluationReceivedDateTime.toString())
        
        * string headers = KafkaConsumerHelper.getHeadersByTopicAndKey("egfr_status", evaluation.evaluationId + '', 10, 5000)
        * match headers contains 'Performed' && 'BillRequestNotSent'

        # Validate Exam Status Update in database
        * def result = EgfrDb().getResultsByEvaluationId(evaluation.evaluationId)

        # Validate response contains 1 - Exam Performed, 6 - Lab Results Received, 7 - Bill Request Not Sent, 5 - Client PDF Delivered
        * match result[*].ExamStatusCodeId contains 1 && 6 && 7 && 5

        Examples:
        | CMP_eGFRResult | CMP_EstimatedGlomerularFiltrationRateResultColor | CMP_Description | expected_normality_indicator   |is_billable |
        | 0              | Gray                                             |   "unknown"     | 'U'                            |false       |



    Scenario Outline: eGFR Billing normal flow BillRequestSent and pdf delivered before results received Quest
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

        # Publish the PDF event to the pdfdelivery topic
        * string pdfEventKey = evaluation.evaluationId
        * string batchName = `Ancillary_Services_Karate_Tests_${DataGen().formattedDateStamp('yyyMMdd')}`
        * def batchId = 2
        * def eventId = DataGen().uuid()
        
        * string pdfDeliveredToClientHeader = {'Type': 'PdfDeliveredToClient'}
        * string pdfEventValue = {'EventId': '#(eventId)','EvaluationId': '#(evaluation.evaluationId)','CreatedDateTime': '#(timestamp)','DeliveryDateTime': '#(timestamp)','BatchName': '#(batchName)','ProductCodes':['EGFR'],'BatchId': #(batchId)}
        * kafkaProducerHelper.send("pdfdelivery", pdfEventKey, pdfDeliveredToClientHeader, pdfEventValue) 
        * eval sleep(2000) 

        # Publish the homeaccess lab results
        * string homeAccessResultsReceivedHeader = {'Type': 'KedEgfrLabResult'}
        * string homeaccessTopic = "dps_labresult_egfr"
        * def ProperDateOfService = DataGen().getUtcDateTimeString(result.DateOfService.toString())
        * string resultsReceivedValue = {'EvaluationId': '#(parseInt(evaluation.evaluationId))','DateLabReceived': '#(ProperDateOfService)',,'EgfrResult': '#(CMP_eGFRResult)','EstimatedGlomerularFiltrationRateResultColor': '#(CMP_EstimatedGlomerularFiltrationRateResultColor)','EstimatedGlomerularFiltrationRateResultDescription': '#(CMP_Description)'}
        * kafkaProducerHelper.send(homeaccessTopic, memberDetails.censeoId, homeAccessResultsReceivedHeader, resultsReceivedValue)
        * eval sleep(2000) 
        
        # Validate the entry in the BillRequestSent table
        * def billingResult = EgfrDb().getBillingResultByEvaluationId(evaluation.evaluationId)[0]
        * match billingResult.BillId == '#notnull'
        * match billingResult.ExamId == '#notnull'
        * match billingResult.BillingProductCode == 'eGFR'


        # Validate record present in table PdfDeliveredToClient in database
        * def resultPdfDeliveredToClient = EgfrDb().getPdfDeliveredToClientByEvaluationId(evaluation.evaluationId)[0]
        * match resultPdfDeliveredToClient.EvaluationId == evaluation.evaluationId

        # Validate BillRequestSent Status in Kafka
        * json event = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("egfr_status", evaluation.evaluationId + '', "BillRequestSent", 10, 5000))
        * match event.BillingProductCode == 'EGFR' 
        * match event.BillId == billingResult.BillId.toString()
        * match event.PdfDeliveryDate == '#notnull'
        * match event.EvaluationId == evaluation.evaluationId
        * match event.MemberPlanId == appointment.memberPlanId

        * match event.ProviderId.toString() == appointment.providerId
        * match event.CreatedDate.toString().split('+')[1] == "00:00"
        * match event.ReceivedDate.toString().split('+')[1] == "00:00"
        * match DataGen().RemoveMilliSeconds(event.CreatedDate) == DataGen().getUtcDateTimeString(result.EvaluationCreatedDateTime.toString())
        * match DataGen().RemoveMilliSeconds(event.ReceivedDate) == DataGen().getUtcDateTimeString(result.EvaluationReceivedDateTime.toString())
        
        * string headers = KafkaConsumerHelper.getHeadersByTopicAndKey("egfr_status", evaluation.evaluationId + '', 10, 5000)
        * match headers contains 'Performed' && 'BillRequestSent'

        # Validate Exam Status Update in database
        * def result = EgfrDb().getResultsByEvaluationId(evaluation.evaluationId)
        # Validate response contains Exam Performed, Lab Results Received, Bill Request Sent, Client PDF Delivered, Billable Event Received
        * match result[*].ExamStatusCodeId contains 1 && 6 && 4 && 5 && 3  

        Examples:
        | CMP_eGFRResult | CMP_EstimatedGlomerularFiltrationRateResultColor | CMP_Description | expected_normality_indicator   |is_billable |
        |   65           | Green                                            |                 | 'N'                            |true        |
        |   45           | Green                                            |                 | 'A'                            |true        |
        
    

    Scenario Outline: eGFR abnormal flow BillRequestNotSent and pdf delivered before results received
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

        # Publish the PDF event to the pdfdelivery topic
        * string pdfEventKey = evaluation.evaluationId
        * string batchName = `Ancillary_Services_Karate_Tests_${DataGen().formattedDateStamp('yyyMMdd')}`
        * def batchId = 2
        * def eventId = DataGen().uuid()
                
        * string pdfDeliveredToClientHeader = {'Type': 'PdfDeliveredToClient'}
        * string pdfEventValue = {'EventId': '#(eventId)','EvaluationId': '#(evaluation.evaluationId)','CreatedDateTime': '#(timestamp)','DeliveryDateTime': '#(timestamp)','BatchName': '#(batchName)','ProductCodes':['EGFR'],'BatchId': #(batchId)}
        * kafkaProducerHelper.send("pdfdelivery", pdfEventKey, pdfDeliveredToClientHeader, pdfEventValue) 

        # Publish the homeaccess lab results
        * string homeAccessResultsReceivedHeader = {'Type': 'KedEgfrLabResult'}
        * string homeaccessTopic = "dps_labresult_egfr"
        * def ProperDateOfService = DataGen().getUtcDateTimeString(result.DateOfService.toString())
        * string resultsReceivedValue = {'EvaluationId': '#(parseInt(evaluation.evaluationId))','DateLabReceived': '#(ProperDateOfService)','EgfrResult': '#(CMP_eGFRResult)','EstimatedGlomerularFiltrationRateResultColor': '#(CMP_EstimatedGlomerularFiltrationRateResultColor)','EstimatedGlomerularFiltrationRateResultDescription': '#(CMP_Description)'}
        * kafkaProducerHelper.send(homeaccessTopic, memberDetails.censeoId, homeAccessResultsReceivedHeader, resultsReceivedValue)
        * eval sleep(2000) 
        
        # Validate the entry in the BillRequestSent table
        * def billingResult = EgfrDb().getBillingResultByEvaluationId(evaluation.evaluationId)[0]
        * match billingResult == '#null'


        # Validate record present in table PdfDeliveredToClient in database
        * def resultPdfDeliveredToClient = EgfrDb().getPdfDeliveredToClientByEvaluationId(evaluation.evaluationId)[0]
        * match resultPdfDeliveredToClient.EvaluationId == evaluation.evaluationId

        # Validate BillRequestNotSent Status in Kafka
        * json event = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("egfr_status", evaluation.evaluationId + '', "BillRequestNotSent", 10, 5000))
        * match event.BillingProductCode == 'EGFR' 
        * match event.PdfDeliveryDate == '#notnull'
        * match event.EvaluationId == evaluation.evaluationId
        * match event.MemberPlanId == appointment.memberPlanId
        * match event.ProviderId.toString() == appointment.providerId
        * match event.CreatedDate.toString().split('+')[1] == "00:00"
        * match event.ReceivedDate.toString().split('+')[1] == "00:00"
        * match DataGen().RemoveMilliSeconds(event.CreatedDate) == DataGen().getUtcDateTimeString(result.EvaluationCreatedDateTime.toString())
        * match DataGen().RemoveMilliSeconds(event.ReceivedDate) == DataGen().getUtcDateTimeString(result.EvaluationReceivedDateTime.toString())
        
        * string headers = KafkaConsumerHelper.getHeadersByTopicAndKey("egfr_status", evaluation.evaluationId + '', 10, 5000)
        * match headers contains 'Performed' && 'BillRequestNotSent'

        # Validate Exam Status Update in database
        * def result = EgfrDb().getResultsByEvaluationId(evaluation.evaluationId)
        # Validate response contains 1 - Exam Performed, 6 - Lab Results Received, 7 - Bill Request Not Sent, 5 - Client PDF Delivered
        * match result[*].ExamStatusCodeId contains 1 && 6 && 7 && 5

        Examples:
        | CMP_eGFRResult | CMP_EstimatedGlomerularFiltrationRateResultColor | CMP_Description | 
        | 0              | Green                                            |                 |
        | 0              | Gray                                             |   "unknown"     |
        | 0              |                                                  |   "unknown"     | 
    

    Scenario Outline: eGFR Billing and pdfDeliveredToClient with rcm_bill events containing valid billId
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
        * string resultsReceivedValue = {'EvaluationId': '#(parseInt(evaluation.evaluationId))','DateLabReceived': '#(ProperDateOfService)','EgfrResult': '#(CMP_eGFRResult)','EstimatedGlomerularFiltrationRateResultDescription': '#(CMP_Description)'}
        * kafkaProducerHelper.send(homeaccessTopic, memberDetails.censeoId, homeAccessResultsReceivedHeader, resultsReceivedValue)
        * eval sleep(2000) 

        # Publish the PDF event to the pdfdelivery topic
        * string pdfEventKey = evaluation.evaluationId
        * string batchName = `Ancillary_Services_Karate_Tests_${DataGen().formattedDateStamp('yyyMMdd')}`
        * def batchId = 2
        * def eventId = DataGen().uuid()
        
        * string pdfDeliveredToClientHeader = {'Type': 'PdfDeliveredToClient'}
        * string pdfEventValue = {'EventId': '#(eventId)','EvaluationId': '#(evaluation.evaluationId)','CreatedDateTime': '#(timestamp)','DeliveryDateTime': '#(timestamp)','BatchName': '#(batchName)','ProductCodes':['EGFR'],'BatchId': #(batchId)}
        * kafkaProducerHelper.send("pdfdelivery", pdfEventKey, pdfDeliveredToClientHeader, pdfEventValue) 
        * eval sleep(2000) 
        
        # Validate the entry in the BillRequestSent table
        * def billingResult = EgfrDb().getBillingResultByEvaluationId(evaluation.evaluationId)[0]
        * match billingResult.BillId == '#notnull'
        * match billingResult.ExamId == '#notnull'
        * match billingResult.BillingProductCode == 'eGFR'


        # Validate record present in table PdfDeliveredToClient in database
        * def resultPdfDeliveredToClient = EgfrDb().getPdfDeliveredToClientByEvaluationId(evaluation.evaluationId)[0]
        * match resultPdfDeliveredToClient.EvaluationId == evaluation.evaluationId

        # Validate BillRequestSent Status in Kafka
        * json event = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("egfr_status", evaluation.evaluationId + '', "BillRequestSent", 10, 5000))
        * match event.BillingProductCode == 'EGFR' 
        * match event.BillId == billingResult.BillId.toString()
        * match event.PdfDeliveryDate == '#notnull'
        * match event.EvaluationId == evaluation.evaluationId
        * match event.MemberPlanId == appointment.memberPlanId

        * match event.ProviderId.toString() == appointment.providerId
        * match event.CreatedDate.toString().split('+')[1] == "00:00"
        * match event.ReceivedDate.toString().split('+')[1] == "00:00"
        * match DataGen().RemoveMilliSeconds(event.CreatedDate) == DataGen().getUtcDateTimeString(result.EvaluationCreatedDateTime.toString())
        * match DataGen().RemoveMilliSeconds(event.ReceivedDate) == DataGen().getUtcDateTimeString(result.EvaluationReceivedDateTime.toString())
        
        * string headers = KafkaConsumerHelper.getHeadersByTopicAndKey("egfr_status", evaluation.evaluationId + '', 10, 5000)
        * match headers contains 'Performed' && 'BillRequestSent'

        # Validate Exam Status Update in database
        * def result = EgfrDb().getResultsByEvaluationId(evaluation.evaluationId)
        # Validate response contains Exam Performed, Lab Results Received, Bill Request Sent, Client PDF Delivered, Billable Event Received
        * match result[*].ExamStatusCodeId contains 1 && 6 && 4 && 5 && 3

        # Publish the RCM bill accepted event to rcm_bill 
        * string rcmBillId = billingResult.BillId
        * string productCode = "EGFR"
        * string rcmBillTopic = "rcm_bill"
        * string billAcceptedHeader = {'Type': 'BillRequestAccepted'}
        * string billAcceptedValue = {'RCMBillId': '#(rcmBillId)','RCMProductCode': '#(productCode)'}
        * kafkaProducerHelper.send(rcmBillTopic, "bill-" + rcmBillId, billAcceptedHeader, billAcceptedValue)
        * eval sleep(2000) 

        # Validate that the billing details were updated as expected 
        * def billAcceptedResult = EgfrDb().getBillingResultByEvaluationId(evaluation.evaluationId)[0]
        * match billAcceptedResult.Accepted == true
        * match billAcceptedResult.AcceptedAt == '#notnull'

        Examples:
        | CMP_eGFRResult | CMP_EstimatedGlomerularFiltrationRateResultColor | CMP_Description| expected_normality_indicator   |is_billable |
        |    65          | Green                                            |                |'N'                            |true        |
        |    45          | Green                                            |   Bad Test     |'A'                            |true        |

    ### Commenting out ANC-T726 and ANC-T727 since RCM Bill event is generated by T-Checks 
    ### and possibly overwrites the tweaked event that we publish resulting in test failures

    # @TestCaseKey=ANC-T726
    # Scenario Outline: eGFR Billing and pdfDeliveredToClient with rcm_bill events containing in-valid billId
    #     * set evaluation.answers =
    #         """
    #         [
    #            {
    #                 "AnswerId": 52456,
    #                 "AnsweredDateTime": "#(timestamp)",
    #                 "AnswerValue": "1"
    #             },
    #             {
    #                 "AnswerId": 51261,
    #                 "AnsweredDateTime": "#(timestamp)",
    #                 "AnswerValue": "1"
    #             },
    #             {
    #                 "AnswerId": 51276,
    #                 "AnsweredDateTime": "#(timestamp)",
    #                 "AnswerValue": #(evaluation.evaluationId)
    #             },
    #             {
    #                 "AnswerId": 22034,
    #                 "AnsweredDateTime": "#(dateStamp)",
    #                 "AnswerValue": "#(dateStamp)"
    #             },
                # {
                #     "AnswerId": 33445,
                #     "AnsweredDateTime": "#(timestamp)",
                #     "AnswerValue": "#(timestamp)"
                # },
                # {
                #     "AnswerId": 21989,
                #     "AnsweredDateTime": "#(timestamp)",
                #     "AnswerValue": "#(providerDetails.signature)"
                # },
                # {
                #     "AnswerId": 28386,
                #     "AnsweredDateTime": "#(timestamp)",
                #     "AnswerValue": "#(providerDetails.firstName) #(providerDetails.lastName)"
                # },
                # {
                #     "AnswerId": 28387,
                #     "AnsweredDateTime": "#(timestamp)",
                #     "AnswerValue": "#(providerDetails.nationalProviderIdentifier)"
                # },
                # {
                #     "AnswerId": 22019,
                #     "AnsweredDateTime": "#(timestamp)",
                #     "AnswerValue": "#(providerDetails.degree)"
                # }
    #         ]
    #         """
                
    #     * karate.call('classpath:helpers/eval/saveEval.feature')
    #     * karate.call('classpath:helpers/eval/stopEval.feature')
    #     * karate.call('classpath:helpers/eval/finalizeEval.feature')
    #     * def result = EgfrDb().getResultsByEvaluationId(evaluation.evaluationId)[0]

    #     # Publish the homeaccess lab results
    #     * string homeAccessResultsReceivedHeader = {'Type': 'EgfrLabResult'}
    #     * string homeaccessTopic = "egfr_lab_results"
    #     * def ProperDateOfService = DataGen().getUtcDateTimeString(result.DateOfService.toString())
    #     * string resultsReceivedValue = {'CenseoId': '#(memberDetails.censeoId)','VendorLabTestId': '888888888','VendorLabTestNumber': 'K12344431','EgfrResult': '#(CMP_eGFRResult)','CreatinineResult': '#(CMP_CreatinineResult)','MailDate': '#(ProperDateOfService)','AccessionedDate': '#(ProperDateOfService)','CollectionDate': '#(ProperDateOfService)','CreatedDateTime': '#(ProperDateOfService)'}
    #     * kafkaProducerHelper.send(homeaccessTopic, memberDetails.censeoId, homeAccessResultsReceivedHeader, resultsReceivedValue)
    #     * eval sleep(2000) 

    #     # Publish the PDF event to the pdfdelivery topic
    #     * string pdfEventKey = evaluation.evaluationId
    #     * string batchName = `Ancillary_Services_Karate_Tests_${DataGen().formattedDateStamp('yyyMMdd')}`
    #     * def batchId = 2
    #     * def eventId = DataGen().uuid()
        
    #     * string pdfDeliveredToClientHeader = {'Type': 'PdfDeliveredToClient'}
    #     * string pdfEventValue = {'EventId': '#(eventId)','EvaluationId': '#(evaluation.evaluationId)','CreatedDateTime': '#(timestamp)','DeliveryDateTime': '#(timestamp)','BatchName': '#(batchName)','ProductCodes':['EGFR'],'BatchId': #(batchId)}
    #     * kafkaProducerHelper.send("pdfdelivery", pdfEventKey, pdfDeliveredToClientHeader, pdfEventValue) 
        
    #     # Validate the entry in the BillRequestSent table
    #     * def billingResult = EgfrDb().getBillingResultByEvaluationId(evaluation.evaluationId)[0]
    #     * match billingResult.BillId == '#notnull'
    #     * match billingResult.ExamId == '#notnull'
    #     * match billingResult.BillingProductCode == 'eGFR'

    #     # Validate record present in table PdfDeliveredToClient in database
    #     * def resultPdfDeliveredToClient = EgfrDb().getPdfDeliveredToClientByEvaluationId(evaluation.evaluationId)[0]
    #     * match resultPdfDeliveredToClient.EvaluationId == evaluation.evaluationId

    #     # Validate BillRequestSent Status in Kafka
    #     * json event = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("egfr_status", evaluation.evaluationId + '', "BillRequestSent", 10, 5000))
    #     * match event.BillingProductCode == 'EGFR' 
    #     * match event.BillId == billingResult.BillId.toString()
    #     * match event.PdfDeliveryDate == '#notnull'
    #     * match event.EvaluationId == evaluation.evaluationId
    #     * match event.MemberPlanId == appointment.memberPlanId

    #     * match event.ProviderId.toString() == appointment.providerId
    #     * match event.CreatedDate.toString().split('+')[1] == "00:00"
    #     * match event.ReceivedDate.toString().split('+')[1] == "00:00"
    #     * match DataGen().RemoveMilliSeconds(event.CreatedDate) == DataGen().getUtcDateTimeString(result.EvaluationCreatedDateTime.toString())
    #     * match DataGen().RemoveMilliSeconds(event.ReceivedDate) == DataGen().getUtcDateTimeString(result.EvaluationReceivedDateTime.toString())
        
    #     * string headers = KafkaConsumerHelper.getHeadersByTopicAndKey("egfr_status", evaluation.evaluationId + '', 10, 5000)
    #     * match headers contains 'Performed' && 'BillRequestSent'

    #     # Validate Exam Status Update in database
    #     * def result = EgfrDb().getResultsByEvaluationId(evaluation.evaluationId)
    #     * def resultToCheck = result[0].ExamStatusCodeId.toString() + "," + result[1].ExamStatusCodeId.toString() + "," + result[2].ExamStatusCodeId.toString() + "," + result[3].ExamStatusCodeId.toString() + "," + result[4].ExamStatusCodeId.toString()
    #     # Validate response contains Exam Performed, Lab Results Received, Bill Request Sent, Client PDF Delivered, Billable Event Received
    #     * match resultToCheck contains "1" && "6" && "4" && "5" && "3"

    #     # Publish the RCM bill accepted event to rcm_bill 
    #     * string rcmBillId = DataGen().uuid()
    #     * string productCode = "EGFR"
    #     * string rcmBillTopic = "rcm_bill"
    #     * string billAcceptedHeader = {'Type': 'BillRequestAccepted'}
    #     * string billAcceptedValue = {'RCMBillId': '#(rcmBillId)','RCMProductCode': '#(productCode)'}
    #     * kafkaProducerHelper.send(rcmBillTopic,  "bill-" + rcmBillId, billAcceptedHeader, billAcceptedValue)

    #     # Validate that the billing details were updated as expected 
    #     * def billAcceptedResult = EgfrDb().getBillingResultByEvaluationId(evaluation.evaluationId)[0]
    #     * match billAcceptedResult.Accepted == false
    #     * match billAcceptedResult.AcceptedAt == '#null'

    #     Examples:
    #     | CMP_eGFRResult | CMP_CreatinineResult |  
    #     | 65             | 1.27                 |
    
    # @TestCaseKey=ANC-T727
    # Scenario Outline: eGFR Billing and pdfDeliveredToClient with rcm_bill events containing in-valid billId but EvalId in request
    #     * set evaluation.answers =
    #         """
    #         [
    #             {
    #                 "AnswerId": 52456,
    #                 "AnsweredDateTime": "#(timestamp)",
    #                 "AnswerValue": "1"
    #             },
    #             {
    #                 "AnswerId": 51261,
    #                 "AnsweredDateTime": "#(timestamp)",
    #                 "AnswerValue": "1"
    #             },
    #             {
    #                 "AnswerId": 51276,
    #                 "AnsweredDateTime": "#(timestamp)",
    #                 "AnswerValue": #(evaluation.evaluationId)
    #             },
    #             {
    #                 "AnswerId": 22034,
    #                 "AnsweredDateTime": "#(dateStamp)",
    #                 "AnswerValue": "#(dateStamp)"
    #             },
                # {
                #     "AnswerId": 33445,
                #     "AnsweredDateTime": "#(timestamp)",
                #     "AnswerValue": "#(timestamp)"
                # },
                # {
                #     "AnswerId": 21989,
                #     "AnsweredDateTime": "#(timestamp)",
                #     "AnswerValue": "#(providerDetails.signature)"
                # },
                # {
                #     "AnswerId": 28386,
                #     "AnsweredDateTime": "#(timestamp)",
                #     "AnswerValue": "#(providerDetails.firstName) #(providerDetails.lastName)"
                # },
                # {
                #     "AnswerId": 28387,
                #     "AnsweredDateTime": "#(timestamp)",
                #     "AnswerValue": "#(providerDetails.nationalProviderIdentifier)"
                # },
                # {
                #     "AnswerId": 22019,
                #     "AnsweredDateTime": "#(timestamp)",
                #     "AnswerValue": "#(providerDetails.degree)"
                # }
    #         ]
    #         """
                
    #     * karate.call('classpath:helpers/eval/saveEval.feature')
    #     * karate.call('classpath:helpers/eval/stopEval.feature')
    #     * karate.call('classpath:helpers/eval/finalizeEval.feature')
    #     * def result = EgfrDb().getResultsByEvaluationId(evaluation.evaluationId)[0]

    #     # Publish the homeaccess lab results
    #     * string homeAccessResultsReceivedHeader = {'Type': 'EgfrLabResult'}
    #     * string homeaccessTopic = "egfr_lab_results"
    #     * def ProperDateOfService = DataGen().getUtcDateTimeString(result.DateOfService.toString())
    #     * string resultsReceivedValue = {'CenseoId': '#(memberDetails.censeoId)','VendorLabTestId': '888888888','VendorLabTestNumber': 'K12344431','EgfrResult': '#(CMP_eGFRResult)','CreatinineResult': '#(CMP_CreatinineResult)','MailDate': '#(ProperDateOfService)','AccessionedDate': '#(ProperDateOfService)','CollectionDate': '#(ProperDateOfService)','CreatedDateTime': '#(ProperDateOfService)'}
    #     * kafkaProducerHelper.send(homeaccessTopic, memberDetails.censeoId, homeAccessResultsReceivedHeader, resultsReceivedValue)
    #     * eval sleep(2000) 

    #     # Publish the PDF event to the pdfdelivery topic
    #     * string pdfEventKey = evaluation.evaluationId
    #     * string batchName = `Ancillary_Services_Karate_Tests_${DataGen().formattedDateStamp('yyyMMdd')}`
    #     * def batchId = 2
    #     * def eventId = DataGen().uuid()
        
    #     * string pdfDeliveredToClientHeader = {'Type': 'PdfDeliveredToClient'}
    #     * string pdfEventValue = {'EventId': '#(eventId)','EvaluationId': '#(evaluation.evaluationId)','CreatedDateTime': '#(timestamp)','DeliveryDateTime': '#(timestamp)','BatchName': '#(batchName)','ProductCodes':['EGFR'],'BatchId': #(batchId)}
    #     * kafkaProducerHelper.send("pdfdelivery", pdfEventKey, pdfDeliveredToClientHeader, pdfEventValue) 
        
    #     # Validate the entry in the BillRequestSent table
    #     * def billingResult = EgfrDb().getBillingResultByEvaluationId(evaluation.evaluationId)[0]
    #     * match billingResult.BillId == '#notnull'
    #     * match billingResult.ExamId == '#notnull'
    #     * match billingResult.BillingProductCode == 'eGFR'

    #     # Validate record present in table PdfDeliveredToClient in database
    #     * def resultPdfDeliveredToClient = EgfrDb().getPdfDeliveredToClientByEvaluationId(evaluation.evaluationId)[0]
    #     * match resultPdfDeliveredToClient.EvaluationId == evaluation.evaluationId

    #     # Validate BillRequestSent Status in Kafka
    #     * json event = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("egfr_status", evaluation.evaluationId + '', "BillRequestSent", 10, 5000))
    #     * match event.BillingProductCode == 'EGFR' 
    #     * match event.BillId == billingResult.BillId.toString()
    #     * match event.PdfDeliveryDate == '#notnull'
    #     * match event.EvaluationId == evaluation.evaluationId
    #     * match event.MemberPlanId == appointment.memberPlanId

    #     * match event.ProviderId.toString() == appointment.providerId
    #     * match event.CreatedDate.toString().split('+')[1] == "00:00"
    #     * match event.ReceivedDate.toString().split('+')[1] == "00:00"
    #     * match DataGen().RemoveMilliSeconds(event.CreatedDate) == DataGen().getUtcDateTimeString(result.EvaluationCreatedDateTime.toString())
    #     * match DataGen().RemoveMilliSeconds(event.ReceivedDate) == DataGen().getUtcDateTimeString(result.EvaluationReceivedDateTime.toString())
        
    #     * string headers = KafkaConsumerHelper.getHeadersByTopicAndKey("egfr_status", evaluation.evaluationId + '', 10, 5000)
    #     * match headers contains 'Performed' && 'BillRequestSent'

    #     # Validate Exam Status Update in database
    #     * def result = EgfrDb().getResultsByEvaluationId(evaluation.evaluationId)
    #     * def resultToCheck = result[0].ExamStatusCodeId.toString() + "," + result[1].ExamStatusCodeId.toString() + "," + result[2].ExamStatusCodeId.toString() + "," + result[3].ExamStatusCodeId.toString() + "," + result[4].ExamStatusCodeId.toString()
    #     # Validate response contains Exam Performed, Lab Results Received, Bill Request Sent, Client PDF Delivered, Billable Event Received
    #     * match resultToCheck contains "1" && "6" && "4" && "5" && "3"

    #     # Publish the RCM bill accepted event to rcm_bill 
    #     * string rcmBillId = DataGen().uuid()
    #     * string productCode = "EGFR"
    #     * string rcmBillTopic = "rcm_bill"
    #     * string billAcceptedHeader = {'Type': 'BillRequestAccepted'}
    #     * string billAcceptedValue = {'RCMBillId': '#(rcmBillId)','RCMProductCode': '#(productCode)', 'AdditionalDetails': { 'EvaluationId': '#(evaluation.evaluationId)' }}
    #     * kafkaProducerHelper.send(rcmBillTopic,  "bill-" + rcmBillId, billAcceptedHeader, billAcceptedValue)

    #     # Validate that the billing details were updated as expected 
    #     * def billAcceptedResult = EgfrDb().getBillingResultByEvaluationId(evaluation.evaluationId)[0]
    #     * match billAcceptedResult.Accepted == false
    #     * match billAcceptedResult.AcceptedAt == '#null'

    #     Examples:
    #     | CMP_eGFRResult | CMP_CreatinineResult |  
    #     | 65             | 1.27                 |
