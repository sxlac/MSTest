@uacr
@envnot=prod
Feature: uACR Lab Performed Billing Tests

    Background:
        * eval if (env == 'prod') karate.abort();
        * def DataGen = function() { var DataGen = Java.type('helpers.data.DataGen'); return new DataGen(); }
        * def UacrDb = function() { var UacrDb = Java.type('helpers.database.uacr.UacrDb'); return new UacrDb(); }
        * def timestamp = DataGen().isoTimestamp()
        * def dateStamp = DataGen().isoDateStamp()
        * def KafkaConsumerHelper = Java.type('helpers.kafka.KafkaConsumerHelper')
        * def KafkaProducerHelper = Java.type('helpers.kafka.KafkaProducerHelper')
        * def memberDetails = karate.call("classpath:helpers/member/createMember.js")
        * def appointment = karate.call('classpath:helpers/appointment/createAppointment.feature', { products: ['HHRA', 'UACR'] }).response
        * def evaluation = karate.call('classpath:helpers/eval/startEval.feature').response

    @TestCaseKey=ANC-T792
    Scenario Outline: Verify that EvalStatus is updated when PdfDeliveredToClient event is published to pdfdelivery topic 
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
        
        # Publish the PDF event to the pdfdelivery topic
        * string pdfEventKey = evaluation.evaluationId
        * string batchName = `Ancillary_Services_Karate_Tests_${DataGen().formattedDateStamp('yyyMMdd')}`
        * def batchId = 2
        * def eventId = DataGen().uuid()
        
        * string pdfDeliveredToClientHeader = {'Type': 'PdfDeliveredToClient'}
        * def pdfEventValue = 
        """
            {
                'EventId': '#(eventId)',
                'EvaluationId': '#(evaluation.evaluationId)',
                'CreatedDateTime': '#(timestamp)',
                'DeliveryDateTime': '#(timestamp)',
                'BatchName': '#(batchName)',
                'ProductCodes':['UACR'],
                'BatchId': #(batchId)}
        """
        * string pdfEventValueStr = pdfEventValue
        * KafkaProducerHelper.send("pdfdelivery", pdfEventKey, pdfDeliveredToClientHeader, pdfEventValueStr) 

        # Validate the DB table for LabResults
        * def labResults = UacrDb().getLabResultsByEvaluationId(evaluation.evaluationId)[0]
        * match labResults.EvaluationId == evaluation.evaluationId

        # Verify Kafka message present in uacr_results
        * json kafkaEvent = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("uacr_results", evaluation.evaluationId + '', "ResultsReceived", 12, 5000))
        * print kafkaEvent
        * match kafkaEvent.ProductCode == "UACR"
        * match kafkaEvent.EvaluationId == evaluation.evaluationId
        * match kafkaEvent.Determination == <expected_normality_indicator>
        * match kafkaEvent.IsBillable == <is_billable>
        * match kafkaEvent.PerformedDate != null
        * match kafkaEvent.ReceivedDate != null
        * match kafkaEvent.Result.AbnormalIndicator == <expected_normality_indicator>
        * match kafkaEvent.Result.UacrResult == <CMP_uACRResult>
        * match kafkaEvent.Result.Description == "Performed"
        * match kafkaEvent.ReceivedDate.toString().split('+')[1] == "00:00"
        * match kafkaEvent.PerformedDate.toString().split('T')[1] == "00:00:00+00:00"
        * match DataGen().RemoveMilliSeconds(kafkaEvent.PerformedDate) == DataGen().getUtcDateTimeString(result[0].DateOfService.toString())
        * match DataGen().RemoveMilliSeconds(kafkaEvent.ReceivedDate) == DataGen().getUtcDateTimeString(labResults.CreatedDateTime.toString())

        # Validate record present in table PdfDeliveredToClient in database
        * def resultPdfDeliveredToClient = UacrDb().getPdfDeliveredToClientByEvaluationId(evaluation.evaluationId)[0]
        * match resultPdfDeliveredToClient.EvaluationId == evaluation.evaluationId

        * def billingResult = UacrDb().getBillingResultByEvaluationId(evaluation.evaluationId)[0]
        * match billingResult.BillId == '#notnull'
        * match billingResult.ExamId == '#notnull'
        * match billingResult.BillingProductCode == 'uACR'

        # Validate BillRequest Status in Kafka
        * json event = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("uacr_status", evaluation.evaluationId + '', "BillRequestSent", 10, 5000))
        * match event.BillingProductCode == 'UACR' 
        * match event.BillId == billingResult.BillId.toString()
        * match event.PdfDeliveryDate == '#notnull'
        * match event.EvaluationId == evaluation.evaluationId
        * match event.MemberPlanId == appointment.memberPlanId

        * match event.ProviderId.toString() == appointment.providerId
        * match event.CreatedDate.toString().split('+')[1] == "00:00"
        * match event.ReceivedDate.toString().split('+')[1] == "00:00"
        * match DataGen().RemoveMilliSeconds(event.CreatedDate) == DataGen().getUtcDateTimeString(result[0].EvaluationCreatedDateTime.toString())
        * match DataGen().RemoveMilliSeconds(event.ReceivedDate) == DataGen().getUtcDateTimeString(result[0].EvaluationReceivedDateTime.toString())
        
        # Validate the DB table for Exam status
        * def result = UacrDb().queryExamWithStatusList(evaluation.evaluationId,["Order Requested", "Billable Event Received", "Exam Performed", "Lab Results Received", "Bill Request Sent", "Client PDF Delivered"])
        * match result[*].ExamStatusCodeId contains [1, 3, 4, 5, 6, 14]

        Examples:
        | CMP_uACRResult | CMP_CreatinineResult | expected_normality_indicator | CMP_uacrResultColor | is_billable | expected_normality | 
        | 29             | 1.07                 | 'N'                          | 'Green'             | true        | 'Normal'           | 
        | 30             | 1.27                 | 'A'                          | 'Red'               | true        | 'Abnormal'         | 
        | 31             | 1.27                 | 'A'                          | 'Red'               | true        | 'Abnormal'         | 
        
    @TestCaseKey=ANC-T766
    Scenario Outline: Verify that EvalStatus is updated when PdfDeliveredToClient event is published to pdfdelivery topic 
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
        
        # Publish the PDF event to the pdfdelivery topic
        * string pdfEventKey = evaluation.evaluationId
        * string batchName = `Ancillary_Services_Karate_Tests_${DataGen().formattedDateStamp('yyyMMdd')}`
        * def batchId = 2
        * def eventId = DataGen().uuid()
        
        * string pdfDeliveredToClientHeader = {'Type': 'PdfDeliveredToClient'}
        * def pdfEventValue = 
        """
            {
                'EventId': '#(eventId)',
                'EvaluationId': '#(evaluation.evaluationId)',
                'CreatedDateTime': '#(timestamp)',
                'DeliveryDateTime': '#(timestamp)',
                'BatchName': '#(batchName)',
                'ProductCodes':['UACR'],
                'BatchId': #(batchId)}
        """
        * string pdfEventValueStr = pdfEventValue
        * KafkaProducerHelper.send("pdfdelivery", pdfEventKey, pdfDeliveredToClientHeader, pdfEventValueStr) 
        
        # Validate the DB table for LabResults
        * def labResults = UacrDb().getLabResultsByEvaluationId(evaluation.evaluationId)[0]
        * match labResults.EvaluationId == evaluation.evaluationId

        # Verify Kafka message present in uacr_results
        * json kafkaEvent = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("uacr_results", evaluation.evaluationId + '', "ResultsReceived", 12, 5000))
        * print kafkaEvent
        * match kafkaEvent.ProductCode == "UACR"
        * match kafkaEvent.EvaluationId == evaluation.evaluationId
        * match kafkaEvent.Determination == <expected_normality_indicator>
        * match kafkaEvent.IsBillable == <is_billable>
        * match kafkaEvent.PerformedDate != null
        * match kafkaEvent.ReceivedDate != null
        * match kafkaEvent.Result.AbnormalIndicator == <expected_normality_indicator>
        * match kafkaEvent.Result.UacrResult == <CMP_uACRResult>
        * match kafkaEvent.Result.Description == "Performed"
        * match kafkaEvent.ReceivedDate.toString().split('+')[1] == "00:00"
        * match kafkaEvent.PerformedDate.toString().split('T')[1] == "00:00:00+00:00"
        * match DataGen().RemoveMilliSeconds(kafkaEvent.PerformedDate) == DataGen().getUtcDateTimeString(result[0].DateOfService.toString())
        * match DataGen().RemoveMilliSeconds(kafkaEvent.ReceivedDate) == DataGen().getUtcDateTimeString(labResults.CreatedDateTime.toString())

        * def billingResult = UacrDb().getBillingResultByEvaluationId(evaluation.evaluationId)[0]
        * match billingResult == '#null'

        # Validate BillRequest Status in Kafka
        * json event = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("uacr_status", evaluation.evaluationId + '', "BillRequestNotSent", 10, 5000))
        * match event.BillingProductCode == 'UACR' 
        * match event.PdfDeliveryDate == '#notnull'
        * match event.EvaluationId == evaluation.evaluationId
        * match event.MemberPlanId == appointment.memberPlanId
        * match event.ProviderId.toString() == appointment.providerId
        * match event.CreatedDate.toString().split('+')[1] == "00:00"
        * match event.ReceivedDate.toString().split('+')[1] == "00:00"
        * match DataGen().RemoveMilliSeconds(event.CreatedDate) == DataGen().getUtcDateTimeString(result[0].EvaluationCreatedDateTime.toString())
        * match DataGen().RemoveMilliSeconds(event.ReceivedDate) == DataGen().getUtcDateTimeString(result[0].EvaluationReceivedDateTime.toString())
        
        # Validate the DB table for Exam status
        * def result = UacrDb().queryExamWithStatusList(evaluation.evaluationId,["Order Requested", "Exam Performed", "Bill Request Not Sent", "Client PDF Delivered"])
        * match result[*].ExamStatusCodeId contains [1, 7, 5, 14]

        Examples:
        | CMP_uACRResult | CMP_CreatinineResult | expected_normality_indicator | CMP_uacrResultColor | is_billable | expected_normality | 
        | 0              | 1.27                 | 'U'                          | 'Grey'              | false       | 'Undetermined'     | 

    @TestCaseKey=ANC-T747    
    Scenario Outline: Verify that EvalStatus is updated when PdfDeliveredToClient event is published to pdfdelivery topic with rcm_bill events containing valid billId
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
        
        # Publish the PDF event to the pdfdelivery topic
        * string pdfEventKey = evaluation.evaluationId
        * string batchName = `Ancillary_Services_Karate_Tests_${DataGen().formattedDateStamp('yyyMMdd')}`
        * def batchId = 2
        * def eventId = DataGen().uuid()
        
        * string pdfDeliveredToClientHeader = {'Type': 'PdfDeliveredToClient'}
        * def pdfEventValue = 
        """
            {
                'EventId': '#(eventId)',
                'EvaluationId': '#(evaluation.evaluationId)',
                'CreatedDateTime': '#(timestamp)',
                'DeliveryDateTime': '#(timestamp)',
                'BatchName': '#(batchName)',
                'ProductCodes':['UACR'],
                'BatchId': #(batchId)}
        """
        * string pdfEventValueStr = pdfEventValue
        * KafkaProducerHelper.send("pdfdelivery", pdfEventKey, pdfDeliveredToClientHeader, pdfEventValueStr) 

        # Validate the DB table for LabResults
        * def labResults = UacrDb().getLabResultsByEvaluationId(evaluation.evaluationId)[0]
        * match labResults.EvaluationId == evaluation.evaluationId

        
        # Verify Kafka message present in uacr_results
        * json kafkaEvent = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("uacr_results", evaluation.evaluationId + '', "ResultsReceived", 12, 5000))
        * print kafkaEvent
        * match kafkaEvent.ProductCode == "UACR"
        * match kafkaEvent.EvaluationId == evaluation.evaluationId
        * match kafkaEvent.Determination == <expected_normality_indicator>
        * match kafkaEvent.IsBillable == <is_billable>
        * match kafkaEvent.PerformedDate != null
        * match kafkaEvent.ReceivedDate != null
        * match kafkaEvent.Result.AbnormalIndicator == <expected_normality_indicator>
        * match kafkaEvent.Result.UacrResult == <CMP_uACRResult>
        * match kafkaEvent.Result.Description == "Performed"
        * match kafkaEvent.ReceivedDate.toString().split('+')[1] == "00:00"
        * match kafkaEvent.PerformedDate.toString().split('T')[1] == "00:00:00+00:00"
        * match DataGen().RemoveMilliSeconds(kafkaEvent.PerformedDate) == DataGen().getUtcDateTimeString(result[0].DateOfService.toString())
        * match DataGen().RemoveMilliSeconds(kafkaEvent.ReceivedDate) == DataGen().getUtcDateTimeString(labResults.CreatedDateTime.toString())

        # Validate record present in table PdfDeliveredToClient in database
        * def resultPdfDeliveredToClient = UacrDb().getPdfDeliveredToClientByEvaluationId(evaluation.evaluationId)[0]
        * match resultPdfDeliveredToClient.EvaluationId == evaluation.evaluationId

        * def billingResult = UacrDb().getBillingResultByEvaluationId(evaluation.evaluationId)[0]
        * match billingResult.BillId == '#notnull'
        * match billingResult.ExamId == '#notnull'
        * match billingResult.BillingProductCode == 'uACR'

        # Validate BillRequest Status in Kafka
        * json event = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("uacr_status", evaluation.evaluationId + '', "BillRequestSent", 10, 5000))
        * match event.BillingProductCode == 'UACR' 
        * match event.BillId == billingResult.BillId.toString()
        * match event.PdfDeliveryDate == '#notnull'
        * match event.EvaluationId == evaluation.evaluationId
        * match event.MemberPlanId == appointment.memberPlanId

        * match event.ProviderId.toString() == appointment.providerId
        * match event.CreatedDate.toString().split('+')[1] == "00:00"
        * match event.ReceivedDate.toString().split('+')[1] == "00:00"
        * match DataGen().RemoveMilliSeconds(event.CreatedDate) == DataGen().getUtcDateTimeString(result[0].EvaluationCreatedDateTime.toString())
        * match DataGen().RemoveMilliSeconds(event.ReceivedDate) == DataGen().getUtcDateTimeString(result[0].EvaluationReceivedDateTime.toString())
        
        # Validate the DB table for Exam status
        * def result = UacrDb().queryExamWithStatusList(evaluation.evaluationId,["Order Requested","Billable Event Received","Exam Performed","Lab Results Received","Bill Request Sent","Client PDF Delivered"])
        * match result[*].ExamStatusCodeId contains [1, 3, 4, 5, 6, 14]

         # Publish the RCM bill accepted event to rcm_bill 
         * string rcmBillId = billingResult.BillId.toString()
         * string productCode = "uACR"
         * string rcmBillTopic = "rcm_bill"
         * string billAcceptedHeader = {'Type': 'BillRequestAccepted'}
         * string billAcceptedValue = {'RCMBillId': '#(rcmBillId)','RCMProductCode': '#(productCode)'}
         * KafkaProducerHelper.send(rcmBillTopic, "bill-" + rcmBillId, billAcceptedHeader, billAcceptedValue)
 
         # Validate that the billing details were updated as expected 
         * def billAcceptedResult = UacrDb().getBillRequestAcceptedByEvaluationId(evaluation.evaluationId)[0]
         * match billAcceptedResult.Accepted == true
         * match billAcceptedResult.AcceptedAt == '#notnull'

        Examples:
        | CMP_uACRResult | CMP_CreatinineResult | expected_normality_indicator | CMP_uacrResultColor | is_billable | expected_normality | 
        | 29             | 1.07                 | 'N'                          | 'Green'             | true        | 'Normal'           | 
        | 30             | 1.27                 | 'A'                          | 'Red'               | true        | 'Abnormal'         | 
        | 31             | 1.27                 | 'A'                          | 'Red'               | true        | 'Abnormal'         |         
