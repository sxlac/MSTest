# Iris GradeAsNormal API is not functional. All tests with Iris integrations will need to be tested manually until issue is resolved. 
@ignore
@envnot=prod  
# @dee
@parallel=false
Feature: DEE Billing Tests

    Background:
        * eval if (env == 'prod') karate.abort();
        * def DataGen = function() { var DataGen = Java.type('helpers.data.DataGen'); return new DataGen(); }
        * def DeeDb = function() { var DeeDb = Java.type("helpers.database.dee.DeeDb"); return new DeeDb(); }
        * def timestamp = DataGen().isoTimestamp()
        * def dateStamp = DataGen().isoDateStamp()
        * def sleep = function(pause){ java.lang.Thread.sleep(pause) }

        # * def memberDetails = karate.call("classpath:helpers/member/createMember.js")
        # * def appointment = karate.call('classpath:helpers/appointment/createAppointment.feature', { products: ['HHRA', 'DEE'] }).response
        # * def evaluation = karate.call('classpath:helpers/eval/startEval.feature').response
        * def KafkaConsumerHelper = Java.type('helpers.kafka.KafkaConsumerHelper')
        * def kafkaProducerHelper = Java.type('helpers.kafka.KafkaProducerHelper')
        
    @TestCaseKey=ANC-T386
    Scenario: DEE Patient Details in Results
        # * def image1 = karate.readAsString('classpath:ancillary_services/features/dee/images/normalEval/image-1.txt')
        # * def image2 = karate.readAsString('classpath:ancillary_services/features/dee/images/normalEval/image-2.txt')
        # * set evaluation.answers =
        #     """
        #     [
        #         {
        #             "AnswerId": 29554,
        #             "AnsweredDateTime": "#(timestamp)",
        #             "AnswerValue": "1"
        #         },
        #         {
        #             "AnswerId": 28377,
        #             "AnsweredDateTime": "#(timestamp)",
        #             "AnswerValue": "John"
        #         },
        #         {
        #             "AnswerId": 28378,
        #             "AnsweredDateTime": "#(timestamp)",
        #             "AnswerValue": "Doe"
        #         },
        #         {
        #             "AnswerId": 30974,
        #             "AnsweredDateTime": "#(timestamp)",
        #             "AnswerValue": "M"
        #         },
        #         {
        #             "AnswerId": 28383,
        #             "AnsweredDateTime": "#(timestamp)",
        #             "AnswerValue": "TX"
        #         },
        #         {
        #             "AnswerId": 30856,
        #             "AnswerRowId": "5AA52E97-D999-4093-BF1B-7AE171C2DFBC",
        #             "AnsweredDateTime": "#(timestamp)",
        #             "AnswerValue": "#(image1)"
        #         },
        #         {
        #             "AnswerId": 30856,
        #             "AnswerRowId": "B5C78B69-1A5C-40F6-B53A-306F0E1A54C6",
        #             "AnsweredDateTime": "#(timestamp)",
        #             "AnswerValue": "#(image2)"
        #         },
        #         {
        #             "AnswerId": 22034,
        #             "AnsweredDateTime": "#(timestamp)",
        #             "AnswerValue": "#(timestamp)"
        #         },
        #         {
        #             "AnswerId": 33445,
        #             "AnsweredDateTime": "#(timestamp)",
        #             "AnswerValue": "#(timestamp)"
        #         },
        #         {
        #             "AnswerId": 21989,
        #             "AnsweredDateTime": "#(timestamp)",
        #             "AnswerValue": "#(providerDetails.signature)"
        #         },
        #         {
        #             "AnswerId": 28386,
        #             "AnsweredDateTime": "#(timestamp)",
        #             "AnswerValue": "#(providerDetails.firstName) #(providerDetails.lastName)"
        #         },
        #         {
        #             "AnswerId": 28387,
        #             "AnsweredDateTime": "#(timestamp)",
        #             "AnswerValue": "#(providerDetails.nationalProviderIdentifier)"
        #         },
        #         {
        #             "AnswerId": 22019,
        #             "AnsweredDateTime": "#(timestamp)",
        #             "AnswerValue": "#(providerDetails.degree)"
        #         }
        #     ]
        #     """

        # * karate.call('classpath:helpers/eval/saveEval.feature')
        # * karate.call('classpath:helpers/eval/stopEval.feature')
        # * karate.call('classpath:helpers/eval/finalizeEval.feature')
        # # Needed due to there being a 10 second delay between CreateDEE command when message is transferred to ProcessDEE
        # * eval sleep(10000)        


        # * def exam = karate.call('classpath:/helpers/iris/exam/getExam.feature')
        # * karate.call('classpath:helpers/iris/exam/gradeExamAsNormal.feature', exam)

        # * def evalResult = DeeDb().getResultsByEvaluationId(evaluation.evaluationId)[0]
        
        # # Get and check Kafka results
        # * json resultEvent = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("dee_results", evaluation.evaluationId + '', "Result", 10, 5000))            
        # * match resultEvent.IsBillable == true

        # Publish the PDF event to the pdfdelivery topic
        * string pdfEventKey = 510606
        * string batchName = `Ancillary_Services_Karate_Tests_${DataGen().formattedDateStamp('yyyMMdd')}`
        # Karate is not letting this this string be multi-line, sorry
        * def eventId = DataGen().uuid()
        * string pdfDeliveredToClientHeader = {'Type': 'PdfDeliveredToClient'}
        * string pdfEventValue = {'EventId': '#(eventId)','EvaluationId': '510606','CreatedDateTime': '#(timestamp)','DeliveryDateTime': '#(timestamp)','BatchName': '#(batchName)','ProductCodes':['DEE'],'BatchId': 14245}
        * kafkaProducerHelper.send("pdfdelivery", pdfEventKey, pdfDeliveredToClientHeader, pdfEventValue)
        
        # # Validate the entry in the DEEBilling table
        # * def result = DeeDb().getBillingResultByEvaluationId(evaluation.evaluationId)[0]
        # * def billId = result.BillId
        # * match result.BillId != null

        # # Validate the entry in the ExamStatus table
        # * def examStatusResults = DeeDb().getExamStatusByEvaluationId(evaluation.evaluationId)
        # # 4 = Result Data Downloaded, 5 = PDF Data Downloaded,6 = Sent to Billing, 9 = Gradable, 18 = DEE Performed, 20 = Billable Event Received
        # * match examStatusResults[*].ExamStatusCodeId contains 4 && 5 && 6 && 9 && 18 && 20

        # # Validate the entry in the ExamResults table
        # * def examResults = DeeDb().getResultsByEvaluationId(evaluation.evaluationId)[0]
        # * match examResults.RightEyeHasPathology == false
        # * match examResults.NormalityIndicator == "N"
        # * match examResults.Gradeable == true
        # * match examResults.LeftEyeHasPathology == false

        # # Validate Kafka status - BillRequestSent
        # * json billRequestStatusEvent = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("dee_status", evaluation.evaluationId + '', "BillRequestSent", 10, 5000))
        # * match billRequestStatusEvent.BillingProductCode == "DEE"
        # * match billRequestStatusEvent.BillId == billId
        # * match billRequestStatusEvent.PdfDeliveryDate contains dateStamp
        # * match billRequestStatusEvent.ProductCode == "DEE"
        # * match billRequestStatusEvent.MemberPlanId == memberDetails.memberPlanId
        # * match billRequestStatusEvent.ProviderId == providerDetails.providerId
        # * match billRequestStatusEvent.CreateDate contains dateStamp
        # * match billRequestStatusEvent.ReceivedDate contains dateStamp
        
        # # Validate Kafka status - ResultsReceived
        # * json resultsReceivedEvent = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("dee_status", evaluation.evaluationId + '', "ResultsReceived", 10, 5000))
        # * match resultsReceivedEvent.ProductCode == "DEE"
        # * match resultsReceivedEvent.MemberPlanId == memberDetails.memberPlanId
        # * match resultsReceivedEvent.ProviderId == providerDetails.providerId
        # * match resultsReceivedEvent.ReceivedDate contains dateStamp
        # * match resultsReceivedEvent.CreateDate contains dateStamp

    @TestCaseKey=ANC-T556
    Scenario: DEE Only 1 Image Submitted to Iris - BillRequestNotSent
        * def image3 = karate.readAsString('classpath:ancillary_services/features/dee/images/normalEval/image-1.txt')
        * set evaluation.answers =
            """
            [
                {
                    "AnswerId": 29554,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "1"
                },
                {
                    "AnswerId": 28377,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "John"
                },
                {
                    "AnswerId": 28378,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "Doe"
                },
                {
                    "AnswerId": 30974,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "M"
                },
                {
                    "AnswerId": 28383,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "TX"
                },
                {
                    "AnswerId": 30856,
                    "AnswerRowId": "EF1BFDA7-C1EA-4DA1-9C0E-4892CADCEE70",
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "#(image3)"
                },
                {
                    "AnswerId": 22034,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "#(timestamp)"
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
        # Needed due to there being a 10 second delay between CreateDEE command when message is transferred to ProcessDEE
        * eval sleep(10000)        

        * def exam = karate.call('classpath:/helpers/iris/exam/getExam.feature')
        * karate.call('classpath:helpers/iris/exam/gradeExamAsNormal.feature', exam)

        * def evalResult = DeeDb().getResultsByEvaluationId(evaluation.evaluationId)[0]
        
        #Get and check Kafka results
        * json event = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("dee_results", evaluation.evaluationId + '', "Result", 10, 5000))             
        * match event.ProductCode == "DEE"                
        * match event.Determination == "N"
        * match event.IsBillable == false
        * match event.Results[0].Side == "L"
        * match event.Results[0].Gradable == true
        * match event.Results[0].Pathology == false
        * match event.Results[1].Side == "R"
        * match event.Results[1].Pathology == null
        * match event.Results[1].Gradable == false
        * match event.Results[1].AbnormalIndicator == "U"
        * match event.Results[1].NotGradableReasons[0] == "No images are available"

        # Publish the PDF event to the pdfdelivery topic
        * string pdfEventKey = evaluation.evaluationId
        * string batchName = `Ancillary_Services_Karate_Tests_${DataGen().formattedDateStamp('yyyMMdd')}`
        # Karate is not letting this this string be multi-line, sorry
        * def eventId = DataGen().uuid()
        * string pdfDeliveredToClientHeader = {'Type': 'PdfDeliveredToClient'}
        * string pdfEventValue = {'EventId': '#(eventId)','EvaluationId': '#(evaluation.evaluationId)','CreatedDateTime': '#(timestamp)','DeliveryDateTime': '#(timestamp)','BatchName': '#(batchName)','ProductCodes':['DEE'],'BatchId': 14245}
        * kafkaProducerHelper.send("pdfdelivery", pdfEventKey, pdfDeliveredToClientHeader, pdfEventValue)

        # Validate the entry in the ExamStatus table
        * def examStatusResults = DeeDb().getExamStatusByEvaluationId(evaluation.evaluationId)
        # 4 = Result Data Downloaded, 5 = PDF Data Downloaded, 9 = Gradable, 21 = DEE Incomplete, 22 = Bill Request Not Sent
        * match examStatusResults[*].ExamStatusCodeId contains 4 && 5 && 9 && 21 && 22

        # Validate the entry in the ExamResults table
        * def examResults = DeeDb().getResultsByEvaluationId(evaluation.evaluationId)[0]
        * match examResults.RightEyeHasPathology == null
        * match examResults.NormalityIndicator == "N"
        * match examResults.LeftEyeHasPathology == false
        
        # Validate Kafka status - BillRequestNotSent 
        * json billRequestStatusEvent = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("dee_status", evaluation.evaluationId + '', "BillRequestNotSent", 10, 5000))
        * match billRequestStatusEvent.ProductCode == "DEE"
        * match billRequestStatusEvent.PdfDeliveryDate contains dateStamp
        * match billRequestStatusEvent.MemberPlanId == memberDetails.memberPlanId
        * match billRequestStatusEvent.ProviderId == providerDetails.providerId
        
        # Validate Kafka status - ResultsReceived
        * json resultsReceivedEvent = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("dee_status", evaluation.evaluationId + '', "ResultsReceived", 10, 5000))
        * match resultsReceivedEvent.ProductCode == "DEE"
        * match resultsReceivedEvent.MemberPlanId == memberDetails.memberPlanId
        * match resultsReceivedEvent.ProviderId == providerDetails.providerId
        * match resultsReceivedEvent.ReceivedDate contains dateStamp
        * match resultsReceivedEvent.CreateDate contains dateStamp

    @TestCaseKey=ANC-T839
    Scenario: DEE Patient Details in Results with rcm_bill events containing valid billId
        * def image1 = karate.readAsString('classpath:ancillary_services/features/dee/images/normalEval/image-1.txt')
        * def image2 = karate.readAsString('classpath:ancillary_services/features/dee/images/normalEval/image-2.txt')
        * set evaluation.answers =
            """
            [
                {
                    "AnswerId": 29554,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "1"
                },
                {
                    "AnswerId": 28377,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "John"
                },
                {
                    "AnswerId": 28378,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "Doe"
                },
                {
                    "AnswerId": 30974,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "M"
                },
                {
                    "AnswerId": 28383,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "TX"
                },
                {
                    "AnswerId": 30856,
                    "AnswerRowId": "5AA52E97-D999-4093-BF1B-7AE171C2DFBC",
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "#(image1)"
                },
                {
                    "AnswerId": 30856,
                    "AnswerRowId": "B5C78B69-1A5C-40F6-B53A-306F0E1A54C6",
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "#(image2)"
                },
                {
                    "AnswerId": 22034,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "#(timestamp)"
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
        # Needed due to there being a 10 second delay between CreateDEE command when message is transferred to ProcessDEE
        * eval sleep(10000)        


        * def exam = karate.call('classpath:/helpers/iris/exam/getExam.feature')
        * karate.call('classpath:helpers/iris/exam/gradeExamAsNormal.feature', exam)

        * def evalResult = DeeDb().getResultsByEvaluationId(evaluation.evaluationId)[0]
        
        # Get and check Kafka results
        * json resultEvent = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("dee_results", evaluation.evaluationId + '', "Result", 10, 5000))            
        * match resultEvent.IsBillable == true

        # Publish the PDF event to the pdfdelivery topic
        * string pdfEventKey = evaluation.evaluationId
        * string batchName = `Ancillary_Services_Karate_Tests_${DataGen().formattedDateStamp('yyyMMdd')}`
        # Karate is not letting this this string be multi-line, sorry
        * def eventId = DataGen().uuid()
        * string pdfDeliveredToClientHeader = {'Type': 'PdfDeliveredToClient'}
        * string pdfEventValue = {'EventId': '#(eventId)','EvaluationId': '#(evaluation.evaluationId)','CreatedDateTime': '#(timestamp)','DeliveryDateTime': '#(timestamp)','BatchName': '#(batchName)','ProductCodes':['DEE'],'BatchId': 14245}
        * kafkaProducerHelper.send("pdfdelivery", pdfEventKey, pdfDeliveredToClientHeader, pdfEventValue)
        
        # Validate the entry in the DEEBilling table
        * def result = DeeDb().getBillingResultByEvaluationId(evaluation.evaluationId)[0]
        * def billId = result.BillId
        * match result.BillId != null

        # Validate the entry in the ExamStatus table
        * def examStatusResults = DeeDb().getExamStatusByEvaluationId(evaluation.evaluationId)
        # 4 = Result Data Downloaded, 5 = PDF Data Downloaded,6 = Sent to Billing, 9 = Gradable, 18 = DEE Performed, 20 = Billable Event Received
        * match examStatusResults[*].ExamStatusCodeId contains 4 && 5 && 6 && 9 && 18 && 20

        # Validate the entry in the ExamResults table
        * def examResults = DeeDb().getResultsByEvaluationId(evaluation.evaluationId)[0]
        * match examResults.RightEyeHasPathology == false
        * match examResults.NormalityIndicator == "N"
        * match examResults.Gradeable == true
        * match examResults.LeftEyeHasPathology == false

        # Validate Kafka status - BillRequestSent
        * json billRequestStatusEvent = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("dee_status", evaluation.evaluationId + '', "BillRequestSent", 10, 5000))
        * match billRequestStatusEvent.BillingProductCode == "DEE"
        * match billRequestStatusEvent.BillId == billId
        * match billRequestStatusEvent.PdfDeliveryDate contains dateStamp
        * match billRequestStatusEvent.ProductCode == "DEE"
        * match billRequestStatusEvent.MemberPlanId == memberDetails.memberPlanId
        * match billRequestStatusEvent.ProviderId == providerDetails.providerId
        * match billRequestStatusEvent.CreateDate contains dateStamp
        * match billRequestStatusEvent.ReceivedDate contains dateStamp
        
        # Validate Kafka status - ResultsReceived
        * json resultsReceivedEvent = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("dee_status", evaluation.evaluationId + '', "ResultsReceived", 10, 5000))
        * match resultsReceivedEvent.ProductCode == "DEE"
        * match resultsReceivedEvent.MemberPlanId == memberDetails.memberPlanId
        * match resultsReceivedEvent.ProviderId == providerDetails.providerId
        * match resultsReceivedEvent.ReceivedDate contains dateStamp
        * match resultsReceivedEvent.CreateDate contains dateStamp        


        # Publish the RCM bill accepted event to rcm_bill 
        * string rcmBillId = result.BillId.toString()
        * string productCode = "DEE"
        * string rcmBillTopic = "rcm_bill"
        * string billAcceptedHeader = {'Type': 'BillRequestAccepted'}
        * string billAcceptedValue = {'RCMBillId': '#(rcmBillId)','RCMProductCode': '#(productCode)'}
        * kafkaProducerHelper.send(rcmBillTopic, "bill-" + rcmBillId, billAcceptedHeader, billAcceptedValue)
         
        # Validate that the billing details were updated as expected 
        * def billAcceptedResult = DeeDb().getBillingResultByEvaluationId(evaluation.evaluationId)[0]
        * match billAcceptedResult.Accepted == true
        * match billAcceptedResult.AcceptedAt == '#notnull'