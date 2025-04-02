@ckd
@envnot=prod
Feature: CKD CDI events based ProviderPay tests

    Background:
        * eval if (env == 'prod') karate.abort();
        * def DataGen = function() { var DataGen = Java.type('helpers.data.DataGen'); return new DataGen(); }
        * def CkdDb = function() { var CkdDb = Java.type('helpers.database.ckd.CkdDb'); return new CkdDb(); }
        * def Faker = function() { var Faker = Java.type('helpers.data.Faker'); return new Faker(); }

        * def timestamp = DataGen().isoTimestamp()
        * def cdiDateTime = DataGen().timestampWithOffset("-05:00", -1)

        * def expirationDate = DataGen().isoDateStamp(30)
        * def dateStamp = DataGen().isoDateStamp()

        * def memberDetails = karate.call("classpath:helpers/member/createMember.js")
        * def appointment = karate.call('classpath:helpers/appointment/createAppointment.feature', { products: ['HHRA', 'CKD'] }).response
        * def evaluation = karate.call('classpath:helpers/eval/startEval.feature').response
        * def KafkaConsumerHelper = Java.type('helpers.kafka.KafkaConsumerHelper')
        * def kafkaProducerHelper = Java.type('helpers.kafka.KafkaProducerHelper')

    @TestCaseKey=ANC-T582
    @TestCaseKey=ANC-T584
    Scenario Outline: CKD Provider Pay. <cdiEventHeaderName>. payProvider <payProvider>. TC - <TC>
                      1. (Business rules met) - CDIPassedEvent
                      2. CDIFailedEvent payProvider - true
        * set evaluation.answers =
            """
                [
                    {
                        'AnswerId': 20950,
                        'AnsweredDateTime': '#(timestamp)',
                        'AnswerValue': '1'
                    },
                    {
                        'AnswerId':  <answer_id>,
                        'AnsweredDateTime': '#(timestamp)',
                        'AnswerValue':  <answer_value>,
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
        * karate.call("classpath:helpers/eval/saveEval.feature")
        * karate.call("classpath:helpers/eval/stopEval.feature")
        * karate.call("classpath:helpers/eval/finalizeEval.feature")

        * def evalResult = CkdDb().getResultsByEvaluationId(evaluation.evaluationId)[0]
        * match evalResult == '#notnull'

        # Publish the cdi event to the cdi_events topic
        # * string cdiEventName = "cdi_events"
        # * string cdiEventKey = evaluation.evaluationId
        # * def eventId = DataGen().uuid()
        # * string cdiEventHeader = { 'Type' : <cdiEventHeaderName>}
        # * string cdiEventValue = {"RequestId":"#(eventId)","EvaluationId": "#(evaluation.evaluationId)","DateTime":"#(cdiDateTime)","Username":"karateTestUser","ApplicationId":"manual","Reason":"reschedule","PayProvider":<payProvider>,"Products":[{"EvaluationId":"#(evaluation.evaluationId)","ProductCode":"HHRA"},{"EvaluationId":"#(evaluation.evaluationId)","ProductCode":"CKD"}]}
        # * kafkaProducerHelper.send(cdiEventName, cdiEventKey, cdiEventHeader, cdiEventValue)
        
        # Validate the entry in the ProviderPay table
        * def providerPayResult = CkdDb().getProviderPayByEvalId(evaluation.evaluationId)[0]
        * match providerPayResult.ProviderId == providerDetails.providerId
        * match providerPayResult.AddressLineOne == memberDetails.address.address1
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
        # Status 1 = CKDPerformed, Status 9 = ProviderPayableEventReceived, Status 10 = ProviderPayRequestSent
        # Status 11 = CdiPassedReceived, Status 12 = CdiFailedWithPayReceived
        * match examStatusResults[*].CKDStatusCodeId contains 1 && 9 && 10 && <statusId>

        # Validate that the Kafka events include the expected event headers
        * string headers = KafkaConsumerHelper.getHeadersByTopicAndKey("ckd_status", evaluation.evaluationId + '', 10, 5000)
        * match headers contains 'Performed' && 'ProviderPayRequestSent' && 'ProviderPayableEventReceived'

        # Validate that the Kafka event has the expected payment id and product code
        * json paymentRequested = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("providerpay_internal", providerPayResult.PaymentId, "PaymentRequested", 10, 5000)) 
        * match paymentRequested["event.entityId"] == providerPayResult.PaymentId
        * match paymentRequested["event.providerId"] == providerPayResult.ProviderId
        * match paymentRequested["event.productCode"] == 'CKD'
        * string utcDateTime = DataGen().getUtcDateTimeString(providerPayResult.DateOfService.toString())
        * assert paymentRequested["event.dateOfService"] == utcDateTime.split('T')[0]
        * match paymentRequested["event.personId"] == providerPayResult.CenseoId
        * match paymentRequested["event.commonClientId"] == providerPayResult.ClientId
        * match paymentRequested["event.engagementTypeOptions"] == 1
        * match paymentRequested["timestamp"] == '#notnull'

        # Validate that the Kafka event - ProviderPayableEventReceived - was raised
        * json payableEvent = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("ckd_status", evaluation.evaluationId + '', "ProviderPayableEventReceived", 10, 5000))   
        * match payableEvent.EvaluationId == evaluation.evaluationId
        * match payableEvent.ProviderId == providerDetails.providerId
        * match payableEvent.ParentCdiEvent == <cdiEventHeaderName>

        # Validate that the Kafka event details are as expected for ckd_status
        * json ckdStatus = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("ckd_status", evaluation.evaluationId + '', "ProviderPayRequestSent", 10, 5000)) 
        * match ckdStatus.EvaluationId == evaluation.evaluationId
        * match ckdStatus.ProviderId == providerDetails.providerId
        * match ckdStatus.MemberPlanId == memberDetails.memberPlanId
        * match ckdStatus.CreatedDate == '#notnull'
        * match ckdStatus.ProviderPayProductCode == 'CKD'
        * match ckdStatus.ReceivedDate == '#notnull'
        * match ckdStatus.ProductCode == 'CKD'
        * string utcDateTime = DataGen().getUtcDateTimeString(providerPayResult.DateOfService.toString())
        * match ckdStatus.PaymentId == providerPayResult.PaymentId

        Examples:
        | answer_id | answer_value                                          | cdiEventHeaderName | payProvider | statusId     | TC      |
        | 20963     | 'Albumin: 30 - Creatinine: 0.1 ; Abnormal'            | "CDIPassedEvent"   | true        | 11           |ANC-T582 |
        # | 20963     | 'Albumin: 30 - Creatinine: 0.1 ; Abnormal'            | "CDIFailedEvent"   | true        | 12           |ANC-T584 |
        | 20966     | 'Albumin: 10 - Creatinine: 0.5 ; Normal'              | "CDIPassedEvent"   | true        | 11           |ANC-T582 |
        # | 20966     | 'Albumin: 10 - Creatinine: 0.5 ; Normal'              | "CDIFailedEvent"   | true        | 12           |ANC-T584 |
        | 20970     | 'Albumin: 10 - Creatinine: 1.0 ; Normal'              | "CDIPassedEvent"   | true        | 11           |ANC-T582 |
        # | 20970     | 'Albumin: 10 - Creatinine: 1.0 ; Normal'              | "CDIFailedEvent"   | true        | 12           |ANC-T584 |
        | 20965     | 'Albumin: 150 - Creatinine: 0.1 ; High Abnormal'      | "CDIPassedEvent"   | true        | 11           |ANC-T582 |
        # | 20965     | 'Albumin: 150 - Creatinine: 0.1 ; High Abnormal'      | "CDIFailedEvent"   | true        | 12           |ANC-T584 |
        | 20962     | 'Albumin: 10 - Creatinine: 0.1 ; Cannot be determined'| "CDIPassedEvent"   | true        | 11           |ANC-T582 |
        # | 20962     | 'Albumin: 10 - Creatinine: 0.1 ; Cannot be determined'| "CDIFailedEvent"   | true        | 12           |ANC-T584 |

    @TestCaseKey=ANC-T583
    @TestCaseKey=ANC-T585
    @TestCaseKey=ANC-T586
    Scenario Outline: CKD Non-Payable (Business rules met/not met) for (CDIPassedEvent) and (CDIFailedEvent). <testScenario> <expectedFailReason>. TC - <TC>
        * set evaluation.answers =
            """
                [
                    {
                        'AnswerId': 20950,
                        'AnsweredDateTime': '#(timestamp)',
                        'AnswerValue': '1'
                    },
                    {
                        'AnswerId':  <answerId>,
                        'AnsweredDateTime': '#(timestamp)',
                        'AnswerValue':  <answer_value>
                    },
                    {
                        'AnswerId': 33263,
                        'AnsweredDateTime': '#(timestamp)',
                        'AnswerValue': '<expirationDateSample>',
                    },
                    {
                        "AnswerId": 22034,
                        "AnsweredDateTime": '#(timestamp)',
                        "AnswerValue": '<dosSample>'
                    }
                ]
            """
        * karate.call("classpath:helpers/eval/saveEval.feature")
        * karate.call("classpath:helpers/eval/stopEval.feature")
        * karate.call("classpath:helpers/eval/finalizeEval.feature")

        * def evalResult = CkdDb().getResultsByEvaluationId(evaluation.evaluationId)[0]
        * match evalResult == "#notnull"
        
        # Publish the cdi event to the cdi_events topic
        * string cdiEventName = "cdi_events"
        * string cdiEventKey = evaluation.evaluationId
        * def eventId = DataGen().uuid()
        * string cdiEventHeader = { 'Type' : <cdiEventHeaderName>}
        * string cdiEventValue = {"RequestId":"#(eventId)","EvaluationId":"#(evaluation.evaluationId)","DateTime":"#(cdiDateTime)","Username":"karateTestUser","ApplicationId":"manual","Reason":"reschedule","PayProvider":<payProvider>,"Products":[{"EvaluationId":"#(evaluation.evaluationId)","ProductCode":"HHRA"},{"EvaluationId":"#(evaluation.evaluationId)","ProductCode":"CKD"}]}
        * kafkaProducerHelper.send(cdiEventName, cdiEventKey, cdiEventHeader, cdiEventValue)
        

        # Validate the entry in the ProviderPay table
        * def providerPayResult = CkdDb().getProviderPayByEvalId(evaluation.evaluationId)[0]
        * match providerPayResult == '#null'

        # Validate the entry using EvaluationId in CKD & CKDStatus tables
        * def examStatusResults = CkdDb().getExamStatusByEvaluationId(evaluation.evaluationId)
        # Status 1 = CKDPerformed, Status 9 = ProviderPayableEventReceived, Status 10 = ProviderPayRequestSent,
        # Status 13 = CdiFailedWithoutPayReceived
        * match examStatusResults[*].CKDStatusCodeId contains 1 && <statusId> && 14
        * match examStatusResults[*].CKDStatusCodeId !contains 9
        * match examStatusResults[*].CKDStatusCodeId !contains 10        

        # Validate that a Kafka event related to ProviderPay was not raised
        * string paymentRequestEvent = KafkaConsumerHelper.getMessageByTopicAndHeaderAndAChildField("providerpay_internal", "PaymentRequested", evalResult.CenseoId, 10, 5000)
        * assert paymentRequestEvent.length == 0
        # Validate that the Kafka event was not raised
        * json event = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("ckd_status", evaluation.evaluationId + '', "ProviderPayRequestSent", 10, 5000))   
        * match event == {}

        # Validate that the Kafka event - ProviderPayableEventReceived - was not raised
        * json nonPayableEvent = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("ckd_status", evaluation.evaluationId + '', "ProviderPayableEventReceived", 5, 1000))   
        * match nonPayableEvent == {}

        # Validate that the Kafka event - ProviderNonPayableEventReceived - was raised.
        * json nonPayableEvent = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("ckd_status", evaluation.evaluationId + '', "ProviderNonPayableEventReceived", 10, 5000))
        * match nonPayableEvent.EvaluationId == evaluation.evaluationId
        * match nonPayableEvent.ProviderId == providerDetails.providerId
        * match nonPayableEvent.ParentCdiEvent == <cdiEventHeaderName>
        * match nonPayableEvent.MemberPlanId == memberDetails.memberPlanId
        * match nonPayableEvent.Reason contains <expectedFailReason>
        * match nonPayableEvent.ProductCode == "CKD"
        * match DataGen().RemoveMilliSeconds(nonPayableEvent.CreatedDate) == DataGen().RemoveMilliSeconds(DataGen().getUtcDateTimeString(evalResult.CreatedDateTime.toString()))
        * assert nonPayableEvent.ReceivedDate.split('T')[0] == evalResult.ReceivedDateTime.toString()

        Examples:
        |answerId| testScenario                  | dosSample                   | expirationDateSample           | answer_value                                          | cdiEventHeaderName  | payProvider | statusId     | expectedFailReason                            | TC     |
        # |20963   | 'failedWithoutPayRulesMet'    | #(DataGen().isoDateStamp()) | #(DataGen().isoDateStamp(30))  | 'Albumin: 30 - Creatinine: 0.1 ; Abnormal'            | "CDIFailedEvent"    | false       | 13           | "PayProvider is false for the CDIFailedEvent" |ANC-T585|
        # |20963   | 'failedWithoutPayRulesMet'    | #(DataGen().isoDateStamp()) | #(DataGen().isoDateStamp(30))  | 'Albumin: 30 - Creatinine: 0.1 ; Abnormal'            | "CDIFailedEvent"    | false       | 13           | "PayProvider is false for the CDIFailedEvent" |ANC-T585|
        # |20966   | 'failedWithoutPayRulesMet'    | #(DataGen().isoDateStamp()) | #(DataGen().isoDateStamp(30))  | 'Albumin: 10 - Creatinine: 0.5 ; Normal'              | "CDIFailedEvent"    | false       | 13           | "PayProvider is false for the CDIFailedEvent" |ANC-T585|
        # |20966   | 'failedWithoutPayRulesMet'    | #(DataGen().isoDateStamp()) | #(DataGen().isoDateStamp(30))  | 'Albumin: 10 - Creatinine: 0.5 ; Normal'              | "CDIFailedEvent"    | false       | 13           | "PayProvider is false for the CDIFailedEvent" |ANC-T585|
        # |20970   | 'failedWithoutPayPastExp'     | #(DataGen().isoDateStamp()) | #(DataGen().isoDateStamp(-30)) | 'Albumin: 10 - Creatinine: 1.0 ; Normal'              | "CDIFailedEvent"    | false       | 13           | "PayProvider is false for the CDIFailedEvent" |ANC-T585|
        # |20970   | 'failedWithoutPayNullDos'     | ""                          | #(DataGen().isoDateStamp(30))  | 'Albumin: 10 - Creatinine: 1.0 ; Normal'              | "CDIFailedEvent"    | false       | 13           | "PayProvider is false for the CDIFailedEvent" |ANC-T585|
        |20965   | 'passedPastExp'               | #(DataGen().isoDateStamp()) | #(DataGen().isoDateStamp(-30)) | 'Albumin: 150 - Creatinine: 0.1 ; High Abnormal'      | "CDIPassedEvent"    | true        | 9            | "ExpirationDate is before DateOfService"      |ANC-T583|
        |20963   | 'passedNullDos'               | ""                          | #(DataGen().isoDateStamp())    | 'Albumin: 150 - Creatinine: 0.1 ; High Abnormal'      | "CDIPassedEvent"    | true        | 9            | "Invalid ExpirationDate or DateOfService"     |ANC-T583|
        # |20962   | 'failedWithPayPastExp'        | #(DataGen().isoDateStamp()) | #(DataGen().isoDateStamp(-30)) | 'Albumin: 10 - Creatinine: 0.1 ; Cannot be determined'| "CDIFailedEvent"    | true        | 12           | "ExpirationDate is before DateOfService"      |ANC-T586|
        # |20962   | 'failedWithPayNullDos'        | ""                          | #(DataGen().isoDateStamp())    | 'Albumin: 10 - Creatinine: 0.1 ; Cannot be determined'| "CDIFailedEvent"    | true        | 12           | "Invalid ExpirationDate or DateOfService"     |ANC-T586|
        # |20962   | 'Null Kit Expiration'         | ""                          | ""                             | 'Albumin: 10 - Creatinine: 0.1 ; Cannot be determined'| "CDIFailedEvent"    | true        | 12           | "Invalid ExpirationDate or DateOfService"     |ANC-T586|                               
        # |20962   | 'Null Kit Expiration'         | ""                          | ""                             | 'Albumin: 10 - Creatinine: 0.1 ; Cannot be determined'| "CDIFailedEvent"    | false       | 12           | "PayProvider is false for the CDIFailedEvent" |ANC-T585|                        
        |20962   | 'Null Kit Expiration'         | ""                          | ""                             | 'Albumin: 10 - Creatinine: 0.1 ; Cannot be determined'| "CDIPassedEvent"    | true        | 12           | "Invalid ExpirationDate or DateOfService"     |ANC-T583|                         
        # |20962   | 'Invalid Strip Result'        | #(DataGen().isoDateStamp()) | ""                             | 'Albumin: 10 - Creatinine: 0.1 ; Cannot be determined'| "CDIFailedEvent"    | true        | 12           | "Invalid ExpirationDate or DateOfService"     |ANC-T586|                   
        # |20962   | 'Invalid Strip Result'        | #(DataGen().isoDateStamp()) | ""                             | 'Albumin: 10 - Creatinine: 0.1 ; Cannot be determined'| "CDIFailedEvent"    | false       | 12           | "PayProvider is false for the CDIFailedEvent" |ANC-T585|              
        |20962   | 'Invalid Strip Result'        | #(DataGen().isoDateStamp()) | ""                             | 'Albumin: 10 - Creatinine: 0.1 ; Cannot be determined'| "CDIPassedEvent"    | true        | 12           | "Invalid ExpirationDate or DateOfService"     |ANC-T583|
    
    @TestCaseKey=ANC-T606       
    Scenario Outline: CKD Non-Payable - Not Performed. <answer_value>, <cdiEventHeaderName>, payProvider - <payProvider>
        * def randomNotes = Faker().randomQuote()
        * set evaluation.answers =
            """
            [
                {
                    'AnswerId': 20949,
                    'AnsweredDateTime': '#(timestamp)',
                    'AnswerValue': 'No'
                },
                {
                    'AnswerId': 30861,
                    'AnsweredDateTime': '#(timestamp)',
                    'AnswerValue': 'Member refused'
                },
                {
                    'AnswerId': <answer_id>,
                    'AnsweredDateTime': '#(timestamp)',
                    'AnswerValue': <answer_value>
                },
                {
                    'AnswerId': 30868,
                    'AnsweredDateTime': '#(timestamp)',
                    'AnswerValue': #(randomNotes)
                },
                {
                    'AnswerId': 22034,
                    'AnsweredDateTime': '#(dateStamp)',
                    'AnswerValue': '#(dateStamp)'
                }
            ]
            """

        * karate.call('classpath:helpers/eval/saveEval.feature')
        * karate.call('classpath:helpers/eval/stopEval.feature')
        * karate.call('classpath:helpers/eval/finalizeEval.feature')

        # Validate that the database CKD details are as expected using EvaluationId in CKD and ExamNotPerformed
        * def notPerformedResult = CkdDb().getNotPerformedResultsByEvaluationId(evaluation.evaluationId)[0]
        * match notPerformedResult.MemberPlanId == memberDetails.memberPlanId
        * match notPerformedResult.CenseoId == memberDetails.censeoId
        * match notPerformedResult.AppointmentId == appointment.appointmentId
        * match notPerformedResult.ProviderId == providerDetails.providerId
        * match notPerformedResult.ExamNotPerformedId != null
        * match notPerformedResult.AnswerId == <answer_id>
        * match notPerformedResult.Reason == <expected_reason>
        * match notPerformedResult.Notes == randomNotes

        # Validate that the Kafka event details are as expected
        * json event = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("ckd_status", evaluation.evaluationId + '', "NotPerformed", 10, 5000))   
        
        * match event.ReasonType == 'Member Refused'
        * match event.Reason == <expected_reason>
        * match event.ReasonNotes == randomNotes
        * match event.ProductCode == 'CKD'
        * match event.MemberPlanId == memberDetails.memberPlanId
        * match event.ProviderId == providerDetails.providerId
        
        # Validate that the Kafka events include the expected event headers
        * string headers = KafkaConsumerHelper.getHeadersByTopicAndKey("ckd_status", evaluation.evaluationId + '', 10, 5000)
        * match headers contains 'NotPerformed'

        # Publish the cdi event to the cdi_events topic
        # * string cdiEventName = "cdi_events"
        # * string cdiEventKey = evaluation.evaluationId
        # * def eventId = DataGen().uuid()
        # * string cdiEventHeader = { 'Type' : <cdiEventHeaderName>}
        # * string cdiEventValue = {"RequestId":"#(eventId)","EvaluationId":"#(evaluation.evaluationId)","DateTime":"#(cdiDateTime)","Username":"karateTestUser","ApplicationId":"manual","Reason":"reschedule","PayProvider":<payProvider>,"Products":[{"EvaluationId":"#(evaluation.evaluationId)","ProductCode":"HHRA"},{"EvaluationId":"#(evaluation.evaluationId)","ProductCode":"CKD"}]}
        # * kafkaProducerHelper.send(cdiEventName, cdiEventKey, cdiEventHeader, cdiEventValue)
        
        # Validate the entry using EvaluationId in CKD & CKDStatus tables
        * def examStatusResults = CkdDb().getExamStatusByEvaluationId(evaluation.evaluationId)
        # Status 7 = CKDNotPerformed
        * match examStatusResults[*].CKDStatusCodeId contains only 7

        # Validate that a Kafka event - ProviderPayRequestSent - was not raised
        * json requestSentEvent = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("ckd_status", evaluation.evaluationId + '', "ProviderPayRequestSent", 5, 1000))
        * match requestSentEvent == {}
        
        # Validate that the Kafka event - ProviderPayableEventReceived - was not raised
        * json payableEvent = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("ckd_status", evaluation.evaluationId + '', "ProviderPayableEventReceived", 5, 1000))
         * match payableEvent == {}
        
        # Validate that the Kafka event - ProviderPayableEventReceived - was not raised
        * json nonPayableEvent = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("ckd_status", evaluation.evaluationId + '', "ProviderNonPayableEventReceived", 5, 1000))
        * match nonPayableEvent == {}

        Examples:
            | answer_id | answer_value                | expected_reason             | cdiEventHeaderName | payProvider |
            | 30863     | 'Member recently completed' | 'Member recently completed' |  "CDIPassedEvent"  | true        |
            # | 30863     | 'Member recently completed' | 'Member recently completed' |  "CDIFailedEvent"  | true        |
            # | 30863     | 'Member recently completed' | 'Member recently completed' |  "CDIFailedEvent"  | false       |
            | 30864     | 'Scheduled to complete'     | 'Scheduled to complete'     |  "CDIPassedEvent"  | true        |
            # | 30864     | 'Scheduled to complete'     | 'Scheduled to complete'     |  "CDIFailedEvent"  | true        |
            # | 30864     | 'Scheduled to complete'     | 'Scheduled to complete'     |  "CDIFailedEvent"  | false       |
            | 30865     | 'Member apprehension'       | 'Member apprehension'       |  "CDIPassedEvent"  | true        |
            # | 30865     | 'Member apprehension'       | 'Member apprehension'       |  "CDIFailedEvent"  | true        |
            # | 30865     | 'Member apprehension'       | 'Member apprehension'       |  "CDIFailedEvent"  | false       |
            | 30866     | 'Not interested'            | 'Not interested'            |  "CDIPassedEvent"  | true        |
            # | 30866     | 'Not interested'            | 'Not interested'            |  "CDIFailedEvent"  | true        |
            # | 30866     | 'Not interested'            | 'Not interested'            |  "CDIFailedEvent"  | false       |
            | 30867     | 'Other'                     | 'Other'                     |  "CDIPassedEvent"  | true        |
            # | 30867     | 'Other'                     | 'Other'                     |  "CDIFailedEvent"  | true        |
            # | 30867     | 'Other'                     | 'Other'                     |  "CDIFailedEvent"  | false       |
