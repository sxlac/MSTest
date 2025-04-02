@uacr
@envnot=prod
Feature: uACR Validate ProviderPay and Billing flows when the BarCode is Invalid

    Background:
        * eval if (env == 'prod') karate.abort();
        * def DataGen = function() { var DataGen = Java.type('helpers.data.DataGen'); return new DataGen(); }
        * def UacrDb = function() { var UacrDb = Java.type('helpers.database.uacr.UacrDb'); return new UacrDb(); }
        * def timestamp = DataGen().isoTimestamp()
        * def dateStamp = DataGen().isoDateStamp()
        * def cdiDateTime = DataGen().timestampWithOffset("-05:00", -1)
        * def KafkaConsumerHelper = Java.type('helpers.kafka.KafkaConsumerHelper')
        * def memberDetails = karate.call("classpath:helpers/member/createMember.js")
        * def appointment = karate.call('classpath:helpers/appointment/createAppointment.feature', { products: ['HHRA', 'UACR'] }).response
        * def evaluation = karate.call('classpath:helpers/eval/startEval.feature').response
        * def KafkaProducerHelper = Java.type('helpers.kafka.KafkaProducerHelper')
    
    @TestCaseKey=ANC-T1097
    Scenario: Performed with Invalid BarCode - verify no OrderCreation but ProviderPay and Billing done
        
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

        # Verify barcode details
        * def barcodeExam = UacrDb().getBarcodeByExamId(result[0].ExamId)
        * match barcodeExam[0].Barcode == evaluation.answers[4].AnswerValue+'|'+ evaluation.answers[5].AnswerValue

        # Verify Kafka message NOT published 
        * json kafkaEvent = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("dps_oms_order", evaluation.evaluationId + '', "OrderCreationEvent", 10, 5000))
        * match kafkaEvent == {}
        
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
                "UrineAlbuminToCreatinineRatioResultColor" : 'Green',
                "CreatinineResult" : 1.07,
                "UrineAlbuminToCreatinineRatioResultDescription" : "Performed",
                "UacrResult": 29,
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

        # Publish the cdi event to the cdi_events topic
        * string cdiEventKey = evaluation.evaluationId
        * string cdiEventHeader = { 'Type' : "CDIPassedEvent"}
        * def cdiEventValue = 
        """
            {
                "RequestId": "#(eventId)",
                "EvaluationId": "#(evaluation.evaluationId)",
                "DateTime": "#(cdiDateTime)",
                "Username": "karateTestUser",
                "ApplicationId": "manual",
                "Reason": "reschedule",
                "PayProvider": true,
                "Products":[
                    {
                        "EvaluationId": "#(evaluation.evaluationId)",
                        "ProductCode": "HHRA"
                    },
                    {
                        "EvaluationId": "#(evaluation.evaluationId)",
                        "ProductCode": "uACR"
                    }
                ]
            }
        """
        * string cdiEventValueStr = cdiEventValue
        * KafkaProducerHelper.send("cdi_events", cdiEventKey, cdiEventHeader, cdiEventValueStr)
        
        # Validate the DB table for LabResults
        * def labResults = UacrDb().getLabResultsByEvaluationId(evaluation.evaluationId)[0]
        * match labResults.EvaluationId == evaluation.evaluationId

        # Verify Kafka message present in uacr_results
        * json kafkaEvent = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("uacr_results", evaluation.evaluationId + '', "ResultsReceived", 12, 5000))
        * print kafkaEvent
        * match kafkaEvent.ProductCode == "UACR"
        * match kafkaEvent.EvaluationId == evaluation.evaluationId
        * match kafkaEvent.Determination == 'N'
        * match kafkaEvent.IsBillable == true
        * match kafkaEvent.PerformedDate != null
        * match kafkaEvent.ReceivedDate != null
        * match kafkaEvent.Result.AbnormalIndicator == 'N'
        * match kafkaEvent.Result.UacrResult == 29
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
        
        # Validate ProviderPayableEventReceived Status in Kafka
        * json payableEvent = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("uacr_status", evaluation.evaluationId + '', "ProviderPayableEventReceived", 10, 5000))   
        * match payableEvent.EvaluationId == evaluation.evaluationId
        * match payableEvent.ProviderId == providerDetails.providerId
        * match payableEvent.ParentCdiEvent == "CDIPassedEvent"

        #Validate the entry in the ProviderPay table
        * def providerPay = UacrDb().getProviderPayResultByEvaluationId(evaluation.evaluationId)[0]
        * match providerPay.PaymentId == "#notnull"
        * match providerPay.ExamId == "#notnull"

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

        # Validate the DB table for Exam status
        * def result = UacrDb().queryExamWithStatusList(evaluation.evaluationId,["CDIPassedReceived", "Billable Event Received", "Exam Performed", "Lab Results Received", "Bill Request Sent", "Client PDF Delivered", "ProviderPayableEventReceived", "ProviderPayRequestSent"])
        * match result[*].ExamStatusCodeId contains [1, 3, 4, 5, 8, 6, 11]