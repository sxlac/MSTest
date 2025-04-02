@ckd
@envnot=prod
Feature: CKD ProviderPay Tests

    Background:
        * eval if (env == 'prod') karate.abort();
        * def DataGen = function() { var DataGen = Java.type('helpers.data.DataGen'); return new DataGen(); }
        * def CkdDb = function() { var CkdDb = Java.type('helpers.database.ckd.CkdDb'); return new CkdDb(); }
        * def Faker = function() { var Faker = Java.type('helpers.data.Faker'); return new Faker(); }

        * def timestamp = DataGen().isoTimestamp()
        * def expirationDate = DataGen().isoDateStamp(30)
        * def dateStamp = DataGen().isoDateStamp()
        * def pdfDeliveryDate = DataGen().utcTimestamp()

        * def memberDetails = karate.call("classpath:helpers/member/createMember.js")
        * def appointment = karate.call('classpath:helpers/appointment/createAppointment.feature', { products: ['HHRA', 'CKD'] }).response
        * def evaluation = karate.call('classpath:helpers/eval/startEval.feature').response
        * def KafkaConsumerHelper = Java.type('helpers.kafka.KafkaConsumerHelper')
        * def kafkaProducerHelper = Java.type('helpers.kafka.KafkaProducerHelper')


    @ignore
    @TestCaseKey=ANC-T685
    Scenario Outline: CKD ProviderPay Valid
        * set evaluation.answers =
            """
            [
                {
                    'AnswerId': 20950,
                    'AnsweredDateTime': '#(timestamp)',
                    'AnswerValue': '1'
                },
                {
                    'AnswerId': <answer_id>,
                    'AnsweredDateTime': '#(timestamp)',
                    'AnswerValue': <answer_value>
                },
                {
                    'AnswerId': 33263,
                    'AnsweredDateTime': '#(timestamp)',
                    'AnswerValue': '#(expirationDate)'
                },
                {
                    "AnswerId": 22034,
                    "AnsweredDateTime": '#(dateStamp)',
                    "AnswerValue": '#(dateStamp)'
                }
            ]
            """

        * karate.call('classpath:helpers/eval/saveEval.feature')
        * karate.call('classpath:helpers/eval/stopEval.feature')
        * karate.call('classpath:helpers/eval/finalizeEval.feature')

        # Validate the entry using EvaluationId in CKD table
        * def evalResult = CkdDb().getResultsByEvaluationId(evaluation.evaluationId)[0]
        * match evalResult.CKDAnswer == <answer_value>

        # Publish the PDF event to the pdfdelivery topic
        * string pdfEventKey = evaluation.evaluationId
        * string batchName = `Ancillary_Services_Karate_Tests_${DataGen().formattedDateStamp('yyyMMdd')}`
        * def eventId = DataGen().uuid()
        * string pdfEventValue = {'EventId': '#(eventId)','EvaluationId': '#(evaluation.evaluationId)','CreatedDateTime': '#(timestamp)','DeliveryDateTime': '#(pdfDeliveryDate)','BatchName': '#(batchName)','ProductCodes':['CKD'],'BatchId': 14245}
        * string pdfDeliveredToClientHeader = {'Type': 'PdfDeliveredToClient'}
        * kafkaProducerHelper.send("pdfdelivery", pdfEventKey, pdfDeliveredToClientHeader, pdfEventValue)

        # Validate the entry using EvaluationId in ProviderPay table
        * def providerPayResult = CkdDb().getProviderPayByEvalId(evaluation.evaluationId)[0]
        * match providerPayResult.ProviderId == providerDetails.providerId
        * match providerPayResult.AddressLineOne == memberDetails.address.address1
        * match providerPayResult.CKDId == evalResult.CKDId
        * match providerPayResult.EvaluationId == evaluation.evaluationId
        * match providerPayResult.MemberId == memberDetails.memberId
        * match providerPayResult.CenseoId == memberDetails.censeoId
        * match providerPayResult.ClientId == 14
        * match providerPayResult.AddressLineTwo == memberDetails.address.address2
        * match providerPayResult.MemberPlanId == memberDetails.memberPlanId
        * match providerPayResult.UserName == 'karate'
        * match providerPayResult.FirstName == memberDetails.firstName
        * match providerPayResult.ZipCode == memberDetails.address.zipCode
        * match providerPayResult.NationalProviderIdentifier == providerDetails.nationalProviderIdentifier
        * match providerPayResult.City == memberDetails.address.city
        * match providerPayResult.MiddleName == memberDetails.middleName
        * match providerPayResult.AppointmentId == appointment.appointmentId
        * match providerPayResult.State == memberDetails.address.state
        * match providerPayResult.PaymentId != null
        * match providerPayResult.LastName == memberDetails.lastName
        * match providerPayResult.ApplicationId == 'Signify.Evaluation.Service'

        # Validate the entry using EvaluationId in CKD & CKDStatus tables
        * def examStatusResults = CkdDb().getExamStatusByEvaluationId(evaluation.evaluationId)
        # Status 1 = CKDPerformed - Status 9 = ProviderPayableEventReceived - Status 10 = ProviderPayRequestSent
        * match examStatusResults[*].CKDStatusCodeId contains 1 && 9 && 10

        # Validate that the Kafka events include the expected event headers
        * string headers = KafkaConsumerHelper.getHeadersByTopicAndKey("ckd_status", evaluation.evaluationId + '', 10, 5000)
        * match headers contains 'Performed' && 'ProviderPayRequestSent'

        # Validate the entry using CKDAnswerValue in LookupCKDAnswer table
        * def examAnswerIdValues = CkdDb().getAnswerValuesByAnswerId(<answer_id>)[0]

        # Validate that the Kafka event details are as expected for ckd_status
        * json ckdStatus = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("ckd_status", evaluation.evaluationId + '', "ProviderPayRequestSent", 10, 5000)) 
        * match ckdStatus.ProviderPayProductCode == 'CKD'
        * match ckdStatus.PaymentId == providerPayResult.PaymentId
        * assert DataGen().compareUtcDatesString( ckdStatus.PdfDeliveryDate, pdfDeliveryDate)
        * match ckdStatus.ProductCode == 'CKD'
        * match ckdStatus.EvaluationId == evaluation.evaluationId
        * match ckdStatus.MemberPlanId == providerPayResult.MemberPlanId
        * match ckdStatus.ProviderId == providerPayResult.ProviderId
        * match ckdStatus.CreatedDate == '#notnull'
        * match ckdStatus.ReceivedDate == '#notnull'

        Examples:
            | answer_id | answer_value                                           |
            | 20963     | 'Albumin: 30 - Creatinine: 0.1 ; Abnormal'             |  
        
    @ignore
    @TestCaseKey=ANC-T686
    Scenario Outline: CKD Non-Payable - Null Kit Expiration Date
        * set evaluation.answers =
            """
            [
                {
                    'AnswerId': 20950,
                    'AnsweredDateTime': '#(timestamp)',
                    'AnswerValue': '1'
                },
                {
                    'AnswerId': <answer_id>,
                    'AnsweredDateTime': '#(timestamp)',
                    'AnswerValue': <answer_value>
                },
                {
                    'AnswerId': 33263,
                    'AnsweredDateTime': '#(timestamp)',
                    'AnswerValue': ''
                },
                {
                    "AnswerId": 22034,
                    "AnsweredDateTime": '#(dateStamp)',
                    "AnswerValue": '#(dateStamp)'
                }
            ]
            """

        * karate.call('classpath:helpers/eval/saveEval.feature')
        * karate.call('classpath:helpers/eval/stopEval.feature')
        * karate.call('classpath:helpers/eval/finalizeEval.feature')

        # Validate the entry using EvaluationId in CKD table
        * def evalResult = CkdDb().getResultsByEvaluationId(evaluation.evaluationId)[0]
        * match evalResult.CKDAnswer == <answer_value>

        # Publish the PDF event to the pdfdelivery topic
        * string pdfEventKey = evaluation.evaluationId
        * string batchName = `Ancillary_Services_Karate_Tests_${DataGen().formattedDateStamp('yyyMMdd')}`
        * def eventId = DataGen().uuid()
        * string pdfDeliveredToClientHeader = {'Type': 'PdfDeliveredToClient'}
        * string pdfEventValue = {'EventId': '#(eventId)','EvaluationId': '#(evaluation.evaluationId)','CreatedDateTime': '#(timestamp)','DeliveryDateTime': '#(pdfDeliveryDate)','BatchName': '#(batchName)','ProductCodes':['CKD'],'BatchId': 14245}
        * kafkaProducerHelper.send("pdfdelivery", pdfEventKey, pdfDeliveredToClientHeader, pdfEventValue)

        # Validate the entry using EvaluationId in ProviderPay table
        * def providerPayResult = CkdDb().getProviderPayByEvalId(evaluation.evaluationId)[0]
        * match providerPayResult == '#null'

        # Validate the entry using EvaluationId in CKD & CKDStatus tables
        * def examStatusResults = CkdDb().getExamStatusByEvaluationId(evaluation.evaluationId)
        # Status 1 = CKDPerformed - Status 9 = ProviderPayableEventReceived - Status 10 = ProviderPayRequestSent
        * match examStatusResults[*].CKDStatusCodeId contains 1
        * match examStatusResults[*].CKDStatusCodeId !contains 9
        * match examStatusResults[*].CKDStatusCodeId !contains 10

        # Validate that the Kafka events include the expected event headers
        * string headers = KafkaConsumerHelper.getHeadersByTopicAndKey("ckd_status", evaluation.evaluationId + '', 10, 5000)
        * match headers contains 'Performed'
        * match headers !contains 'ProviderPayRequestSent'

        # Validate the entry using CKDAnswerValue in LookupCKDAnswer table
        * def examAnswerIdValues = CkdDb().getAnswerValuesByAnswerId(<answer_id>)[0]

        Examples:
            | answer_id | answer_value                                           |
            | 20963     | 'Albumin: 30 - Creatinine: 0.1 ; Abnormal'             |     
           
    
    @ignore
    @TestCaseKey=ANC-T687
    Scenario: CKD Non-Payable - Null Kit Expiration Date and Invalid Strip Result
        * def sleep = function(pause){ java.lang.Thread.sleep(pause) }
        * set evaluation.answers =
            """
            [
                {
                    'AnswerId': 20950,
                    'AnsweredDateTime': '#(timestamp)',
                    'AnswerValue': '1'
                },
                {
                    'AnswerId': 20983,
                    'AnsweredDateTime': '#(timestamp)',
                    'AnswerValue': ''
                },
                {
                    'AnswerId': 33263,
                    'AnsweredDateTime': '#(timestamp)',
                    'AnswerValue': ''
                },
                {
                    "AnswerId": 22034,
                    "AnsweredDateTime": '#(dateStamp)',
                    "AnswerValue": '#(dateStamp)'
                }
            ]
            """

        * karate.call('classpath:helpers/eval/saveEval.feature')
        * karate.call('classpath:helpers/eval/stopEval.feature')
        * karate.call('classpath:helpers/eval/finalizeEval.feature')

        # Validate the entry using EvaluationId in CKD table
        * def evalResult = CkdDb().getResultsByEvaluationId(evaluation.evaluationId)[0]
        * match evalResult.CKDAnswer == null
        * match evalResult.ExpirationDate == null
        
        # Publish the PDF event to the pdfdelivery topic
        * string pdfEventKey = evaluation.evaluationId
        * string batchName = `Ancillary_Services_Karate_Tests_${DataGen().formattedDateStamp('yyyMMdd')}`
        * def eventId = DataGen().uuid()
        * string pdfDeliveredToClientHeader = {'Type': 'PdfDeliveredToClient'}
        * string pdfEventValue = {'EventId': '#(eventId)','EvaluationId': '#(evaluation.evaluationId)','CreatedDateTime': '#(timestamp)','DeliveryDateTime': '#(pdfDeliveryDate)','BatchName': '#(batchName)','ProductCodes':['CKD'],'BatchId': 14245}
        * kafkaProducerHelper.send("pdfdelivery", pdfEventKey, pdfDeliveredToClientHeader, pdfEventValue)

        # Validate the entry using EvaluationId in ProviderPay table
        * def providerPayResult = CkdDb().getProviderPayByEvalId(evaluation.evaluationId)[0]
        * match providerPayResult == '#null'
        
        # Validate that the Kafka events include the expected event headers and values
        * json event = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("ckd_status", evaluation.evaluationId + '', "Performed", 10, 5000))    
        * match event.ProductCode == 'CKD'
        * match event.ProviderId == providerDetails.providerId
        * match event.MemberPlanId == memberDetails.memberPlanId

        # Validate that the Kafka events include the expected event headers
        * string headers = KafkaConsumerHelper.getHeadersByTopicAndKey("ckd_status", evaluation.evaluationId + '', 10, 5000)
        * match headers contains 'Performed'
        * match headers !contains 'ProviderPayRequestSent'

        # It takes ~5 seconds to populate in the CKDStatus table
        * eval sleep(6000)

        # Validate the entry using EvaluationId in CKD & CKDStatus tables
        * def examStatusResults = CkdDb().getExamStatusByEvaluationId(evaluation.evaluationId)
        # Status 1 = CKDPerformed - Status 9 = ProviderPayableEventReceived - Status 10 = ProviderPayRequestSent
        * match examStatusResults[*].CKDStatusCodeId contains 1 
        * match examStatusResults[*].CKDStatusCodeId !contains 9
        * match examStatusResults[*].CKDStatusCodeId !contains 10

    @ignore
    @TestCaseKey=ANC-T688
    Scenario: CKD Non-Payable - Invalid Strip Result
        * def sleep = function(pause){ java.lang.Thread.sleep(pause) }
        * set evaluation.answers =
            """
            [
                {
                    'AnswerId': 20950,
                    'AnsweredDateTime': '#(timestamp)',
                    'AnswerValue': '1'
                },
                {
                    'AnswerId': 20983,
                    'AnsweredDateTime': '#(timestamp)',
                    'AnswerValue': ''
                },
                {
                    'AnswerId': 33263,
                    'AnsweredDateTime': '#(timestamp)',
                    'AnswerValue': '#(expirationDate)'
                },
                {
                    "AnswerId": 22034,
                    "AnsweredDateTime": '#(dateStamp)',
                    "AnswerValue": '#(dateStamp)'
                }
            ]
            """

        * karate.call('classpath:helpers/eval/saveEval.feature')
        * karate.call('classpath:helpers/eval/stopEval.feature')
        * karate.call('classpath:helpers/eval/finalizeEval.feature')

        # Validate the entry using EvaluationId in CKD table
        * def evalResult = CkdDb().getResultsByEvaluationId(evaluation.evaluationId)[0]
        * match evalResult.CKDAnswer == null
        
        # Publish the PDF event to the pdfdelivery topic
        * string pdfEventKey = evaluation.evaluationId
        * string batchName = `Ancillary_Services_Karate_Tests_${DataGen().formattedDateStamp('yyyMMdd')}`
        * def eventId = DataGen().uuid()
        * string pdfDeliveredToClientHeader = {'Type': 'PdfDeliveredToClient'}
        * string pdfEventValue = {'EventId': '#(eventId)','EvaluationId': '#(evaluation.evaluationId)','CreatedDateTime': '#(timestamp)','DeliveryDateTime': '#(pdfDeliveryDate)','BatchName': '#(batchName)','ProductCodes':['CKD'],'BatchId': 14245}
        * kafkaProducerHelper.send("pdfdelivery", pdfEventKey, pdfDeliveredToClientHeader, pdfEventValue)

        # Validate the entry using EvaluationId in ProviderPay table
        * def providerPayResult = CkdDb().getProviderPayByEvalId(evaluation.evaluationId)[0]
        * match providerPayResult == '#null'
        
        # Validate that the Kafka events include the expected event headers and values
        * json event = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("ckd_status", evaluation.evaluationId + '', "Performed", 10, 5000))    
        * match event.ProductCode == 'CKD'
        * match event.ProviderId == providerDetails.providerId
        * match event.MemberPlanId == memberDetails.memberPlanId

        # Validate that the Kafka events include the expected event headers
        * string headers = KafkaConsumerHelper.getHeadersByTopicAndKey("ckd_status", evaluation.evaluationId + '', 10, 5000)
        * match headers contains 'Performed'
        * match headers !contains 'ProviderPayRequestSent'

        # It takes ~5 seconds to populate in the CKDStatus table
        * eval sleep(6000)

        # Validate the entry using EvaluationId in CKD & CKDStatus tables
        * def examStatusResults = CkdDb().getExamStatusByEvaluationId(evaluation.evaluationId)
        # Status 1 = CKDPerformed - Status 9 = ProviderPayableEventReceived - Status 10 = ProviderPayRequestSent
        * match examStatusResults[*].CKDStatusCodeId contains 1
        * match examStatusResults[*].CKDStatusCodeId !contains 10
    
    @ignore
    @TestCaseKey=ANC-T689
    Scenario Outline: CKD Non-Payable - ExpirationDate before DateOfService
        * def sleep = function(pause){ java.lang.Thread.sleep(pause) }
        * def pastExpirationDate = DataGen().isoDateStamp(-30)
        * set evaluation.answers =
            """
            [
                {
                    'AnswerId': 20950,
                    'AnsweredDateTime': '#(timestamp)',
                    'AnswerValue': '1'
                },
                {
                    'AnswerId': <answer_id>,
                    'AnsweredDateTime': '#(timestamp)',
                    'AnswerValue': <answer_value>
                },
                {
                    'AnswerId': 33263,
                    'AnsweredDateTime': '#(timestamp)',
                    'AnswerValue': '#(pastExpirationDate)'
                },
                {
                    "AnswerId": 22034,
                    "AnsweredDateTime": '#(dateStamp)',
                    "AnswerValue": '#(dateStamp)'
                }
            ]
            """

        * karate.call('classpath:helpers/eval/saveEval.feature')
        * karate.call('classpath:helpers/eval/stopEval.feature')
        * karate.call('classpath:helpers/eval/finalizeEval.feature')

        # Validate the entry using EvaluationId in CKD table
        * def evalResult = CkdDb().getResultsByEvaluationId(evaluation.evaluationId)[0]
        * match evalResult.CKDAnswer == <answer_value>
        
        # Publish the PDF event to the pdfdelivery topic
        * string pdfEventKey = evaluation.evaluationId
        * string batchName = `Ancillary_Services_Karate_Tests_${DataGen().formattedDateStamp('yyyMMdd')}`
        * def eventId = DataGen().uuid()
        * string pdfDeliveredToClientHeader = {'Type': 'PdfDeliveredToClient'}
        * string pdfEventValue = {'EventId': '#(eventId)','EvaluationId': '#(evaluation.evaluationId)','CreatedDateTime': '#(timestamp)','DeliveryDateTime': '#(pdfDeliveryDate)','BatchName': '#(batchName)','ProductCodes':['CKD'],'BatchId': 14245}
        * kafkaProducerHelper.send("pdfdelivery", pdfEventKey, pdfDeliveredToClientHeader, pdfEventValue)

        # Validate the entry using EvaluationId in ProviderPay table
        * def providerPayResult = CkdDb().getProviderPayByEvalId(evaluation.evaluationId)[0]
        * match providerPayResult == '#null'
        
        # Validate that the Kafka events include the expected event headers and values
        * json event = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("ckd_status", evaluation.evaluationId + '', "Performed", 10, 5000))    
        * match event.ProductCode == 'CKD'
        * match event.ProviderId == providerDetails.providerId
        * match event.MemberPlanId == memberDetails.memberPlanId

        # Validate that the Kafka events include the expected event headers
        * string headers = KafkaConsumerHelper.getHeadersByTopicAndKey("ckd_status", evaluation.evaluationId + '', 10, 5000)
        * match headers contains 'Performed'
        * match headers !contains 'ProviderPayRequestSent'

        # It takes ~5 seconds to populate in the CKDStatus table
        * eval sleep(6000)

        # Validate the entry using EvaluationId in CKD & CKDStatus tables
        * def examStatusResults = CkdDb().getExamStatusByEvaluationId(evaluation.evaluationId)
        # Status 1 = CKDPerformed - Status 9 = ProviderPayableEventReceived - Status 10 = ProviderPayRequestSent
        * match examStatusResults[*].CKDStatusCodeId contains 1
        * match examStatusResults[*].CKDStatusCodeId !contains 9
        * match examStatusResults[*].CKDStatusCodeId !contains 10

        # Validate the entry using CKDAnswerValue in LookupCKDAnswer table
        * def examAnswerIdValues = CkdDb().getAnswerValuesByAnswerId(<answer_id>)[0]

        Examples:
            | answer_id | answer_value                                  |
            | 20963     | 'Albumin: 30 - Creatinine: 0.1 ; Abnormal'    |    
