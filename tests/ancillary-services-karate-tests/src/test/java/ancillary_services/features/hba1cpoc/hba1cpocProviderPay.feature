@hba1cpoc
@envnot=prod
Feature: HBA1CPOC ProviderPay Tests

    Background:
        * eval if (env == 'prod') karate.abort();
        * def DataGen = function() { var DataGen = Java.type('helpers.data.DataGen'); return new DataGen(); }
        * def Hba1cPOCDb = function() { var Hba1cpocDb = Java.type("helpers.database.hba1cpoc.Hba1cpocDb"); return new Hba1cpocDb(); }
        * def timestamp = DataGen().isoTimestamp()
        * def expirationDate = DataGen().isoDateStamp(30)
        * def dateStamp = DataGen().isoDateStamp()
        * def pdfDeliveryDate = DataGen().utcTimestamp()
        * def Faker = function() { var Faker = Java.type('helpers.data.Faker'); return new Faker(); }

        * def memberDetails = karate.call("classpath:helpers/member/createMember.js")
        * def appointment = karate.call('classpath:helpers/appointment/createAppointment.feature', { products: ['HHRA', 'HBA1CPOC'] }).response
        * def evaluation = karate.call('classpath:helpers/eval/startEval.feature').response
        * def KafkaConsumerHelper = Java.type('helpers.kafka.KafkaConsumerHelper')
        * def kafkaProducerHelper = Java.type('helpers.kafka.KafkaProducerHelper')

    @ignore
    @TestCaseKey=ANC-T695    
    Scenario Outline: HBA1CPOC ProviderPay Valid
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
                    "AnswerValue": <answer_value>,
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

        * karate.call('classpath:helpers/eval/saveEval.feature')
        * karate.call('classpath:helpers/eval/stopEval.feature')
        * karate.call('classpath:helpers/eval/finalizeEval.feature')

        # Validate that the database HBA1CPOC details are as expected using EvaluationId in HBA1CPOC
        * def result = Hba1cPOCDb().getResultsByEvaluationId(evaluation.evaluationId)[0]
        * match result.EvaluationId == evaluation.evaluationId
        * match result.MemberPlanId == memberDetails.memberPlanId
        * match result.CenseoId == memberDetails.censeoId
        * match result.AppointmentId == appointment.appointmentId
        * match result.ProviderId == providerDetails.providerId
        * match result.A1CPercent == <answer_value>
        * match result.NormalityIndicator == <normality>
        * match result.ReceivedDateTime.toString() == dateStamp
        * match result.ExpirationDate.toString() == expirationDate
        * match result.DateOfBirth.toString() == memberDetails.dateOfBirth

        # Publish the PDF event to the pdfdelivery topic
        * string pdfEventKey = evaluation.evaluationId
        * string batchName = `Ancillary_Services_Karate_Tests_${DataGen().formattedDateStamp('yyyMMdd')}`
        * def eventId = DataGen().uuid()
        * string pdfEventValue = {'EventId': '#(eventId)','EvaluationId': '#(evaluation.evaluationId)','CreatedDateTime': '#(timestamp)','DeliveryDateTime': '#(pdfDeliveryDate)','BatchName': '#(batchName)','ProductCodes':['HBA1CPOC'],'BatchId': 14245}
        * string pdfDeliveredToClientHeader = {'Type': 'PdfDeliveredToClient'}
        * kafkaProducerHelper.send("pdfdelivery", pdfEventKey, pdfDeliveredToClientHeader, pdfEventValue)

        # Validate the entry using EvaluationId in ProviderPay table for HBA1CPOC
        * def providerPayResult = Hba1cPOCDb().getProviderPayResultsWithEvalId(evaluation.evaluationId)[0]
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

        # # Validate the entry using EvaluationId in HBA1CPOC & HBA1CPOCStatus tables
        * def examStatusResults = Hba1cPOCDb().getExamStatusByEvaluationId(evaluation.evaluationId)
        # Status 1 = HBA1CPOCPerformed, Status 5 = BillRequestSent, Status 6 = BillableEventRecieved, Status 8 = ProviderPayableEventReceived, Status 9 = ProviderPayRequestSent
        * match examStatusResults[*].HBA1CPOCStatusCodeId contains 1 && 5 && 6 && 8 && 9

        # # Validate that the Kafka events include the expected event headers
        * json headers = KafkaConsumerHelper.getHeadersByTopicAndKey("a1cpoc_status", evaluation.evaluationId + '', 10, 5000)
        * match headers contains 'Performed' && 'ProviderPayRequestSent' && 'BillRequestSent'

        # Validate that the Kafka event - ProviderPayRequestSent - details are as expected for a1cpoc_status
        * json requestSentEvent = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("a1cpoc_status", evaluation.evaluationId + '', "ProviderPayRequestSent", 10, 5000))
        * match requestSentEvent.ProviderPayProductCode == 'HBA1CPOC'
        * match requestSentEvent.PaymentId == providerPayResult.PaymentId
        * assert DataGen().compareUtcDatesString( requestSentEvent.PdfDeliveryDate, pdfDeliveryDate)
        * match requestSentEvent.ProductCode == 'HBA1CPOC'
        * match requestSentEvent.EvaluationId == evaluation.evaluationId
        * match requestSentEvent.MemberPlanId == providerPayResult.MemberPlanId
        * match requestSentEvent.ProviderId == providerPayResult.ProviderId
        * match requestSentEvent.CreateDate == '#notnull'
        * match requestSentEvent.ReceivedDate == '#notnull'

        Examples:
            | answer_value | normality |
            | '3.9'        | 'A'       |
            | '4'          | 'N'       |
            | '6.9'        | 'N'       |

    @ignore
    @TestCaseKey=ANC-T696       
    Scenario Outline: HBA1CPOC Non-Payable - Null Kit Expiration Date
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
                    "AnswerValue": <answer_value>,
                },
                {
                    "AnswerId": 33264,
                    "AnsweredDateTime": '#(timestamp)',
                    "AnswerValue":  '',
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

        * karate.call('classpath:helpers/eval/saveEval.feature')
        * karate.call('classpath:helpers/eval/stopEval.feature')
        * karate.call('classpath:helpers/eval/finalizeEval.feature')

        # Validate the entry using EvaluationId in HBA1CPOC table
        * def evalResult = Hba1cPOCDb().getResultsByEvaluationId(evaluation.evaluationId)[0]
        * match evalResult.EvaluationId == evaluation.evaluationId
        * match evalResult.MemberPlanId == memberDetails.memberPlanId
        * match evalResult.CenseoId == memberDetails.censeoId
        * match evalResult.AppointmentId == appointment.appointmentId
        * match evalResult.ProviderId == providerDetails.providerId
        * match evalResult.ExpirationDate.toString() == '0001-01-01 00:00:00.0'

        # Publish the PDF event to the pdfdelivery topic
        * string pdfEventKey = evaluation.evaluationId
        * string batchName = `Ancillary_Services_Karate_Tests_${DataGen().formattedDateStamp('yyyMMdd')}`
        * def eventId = DataGen().uuid()
        * string pdfDeliveredToClientHeader = {'Type': 'PdfDeliveredToClient'}
        * string pdfEventValue = {'EventId': '#(eventId)','EvaluationId': '#(evaluation.evaluationId)','CreatedDateTime': '#(timestamp)','DeliveryDateTime': '#(pdfDeliveryDate)','BatchName': '#(batchName)','ProductCodes':['HBA1CPOC'],'BatchId': 14245}
        * kafkaProducerHelper.send("pdfdelivery", pdfEventKey, pdfDeliveredToClientHeader, pdfEventValue)

        # Validate the entry using EvaluationId in ProviderPay table
        * def providerPayResult = Hba1cPOCDb().getProviderPayResultsWithEvalId(evaluation.evaluationId)[0]
        * match providerPayResult == '#null'

        # Validate the entry using EvaluationId in HBA1CPOC & HBA1CPOCStatus tables
        * def examStatusResults = Hba1cPOCDb().getExamStatusByEvaluationId(evaluation.evaluationId)
        Then print 'count is : ', examStatusResults

        # Status 7 = HBA1CPOCNotPerformed, Status 9 = ProviderPayRequestSent, - Status 8 = ProviderPayableEventReceived
        * match examStatusResults[*].HBA1CPOCStatusCodeId contains 7
        * match examStatusResults[*].HBA1CPOCStatusCodeId !contains 8
        * match examStatusResults[*].HBA1CPOCStatusCodeId !contains 9

        # Validate that the Kafka events include the expected event headers
        * string headers = KafkaConsumerHelper.getHeadersByTopicAndKey("a1cpoc_status", evaluation.evaluationId + '', 10, 5000)
        * match headers contains 'NotPerformed'
        * match headers !contains 'ProviderPayRequestSent'

        Examples:
            | answer_value | normality |
            | '3.9'        | 'A'       |

    @TestCaseKey=ANC-T566
    @TestCaseKey=ANC-T565
    @TestCaseKey=ANC-T577
    @TestCaseKey=ANC-T579
    Scenario Outline: HBA1CPOC Provider Pay (Business rules met) - (CDIPassedEvent) and (CDIFailedEvent With PayProvider: True)
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
                    "AnswerValue": <answer_value>,
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
        * karate.call("classpath:helpers/eval/saveEval.feature")
        * karate.call("classpath:helpers/eval/stopEval.feature")
        * karate.call("classpath:helpers/eval/finalizeEval.feature")

        * def evalResult = Hba1cPOCDb().getResultsByEvaluationId(evaluation.evaluationId)[0]
        * match evalResult == '#notnull'
        
        # Publish the cdi event to the cdi_events topic
        * string cdiEventName = "cdi_events"
        * string cdiEventKey = evaluation.evaluationId
        * def eventId = DataGen().uuid()
        * string cdiEventHeader = { 'Type' : <cdiEventHeaderName>}
        * string cdiEventValue = {"RequestId":"#(eventId)","EvaluationId":"#(evaluation.evaluationId)","DateTime":"#(timestamp)","Username":"karateTestUser","ApplicationId":"manual","Reason":"reschedule","PayProvider":<payProvider>,"Products":[{"EvaluationId":"#(evaluation.evaluationId)","ProductCode":"HHRA"},{"EvaluationId":"#(evaluation.evaluationId)","ProductCode":"HBA1CPOC"}]}
        * kafkaProducerHelper.send(cdiEventName, cdiEventKey, cdiEventHeader, cdiEventValue)

        # Validate the entry using EvaluationId in ProviderPay table for HBA1CPOC
        * def providerPayResult = Hba1cPOCDb().getProviderPayResultsWithEvalId(evaluation.evaluationId)[0]
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

        # # Validate the entry using EvaluationId in HBA1CPOC & HBA1CPOCStatus tables
        * def examStatusResults = Hba1cPOCDb().getExamStatusByEvaluationId(evaluation.evaluationId)
        # Status 1 = HBA1CPOCPerformed, Status 8 = ProviderPayableEventReceived, Status 9 = ProviderPayRequestSent, Status 10 = CdiPassedReceived, Status 11 = CdiFailedWithPayReceived
        * match examStatusResults[*].HBA1CPOCStatusCodeId contains 1 && <statusCodeId> && 8 && 9

        # Validate that the Kafka event has the expected payment id and Provider Pay Product Code
        * json requestSentEvent = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("a1cpoc_status", evaluation.evaluationId + '', "ProviderPayRequestSent", 10, 5000))
        * match requestSentEvent.EvaluationId == evaluation.evaluationId

        # Validate that the Kafka event has the expected payment id and Provider Pay Product Code
        * json payableEvent = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("a1cpoc_status", evaluation.evaluationId + '', "ProviderPayableEventReceived", 10, 5000))
        * match payableEvent.EvaluationId == evaluation.evaluationId

        # Validate that the Kafka event has the expected payment id and product code
        * json paymentRequested = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("providerpay_internal", providerPayResult.PaymentId, "PaymentRequested", 10, 5000)) 
        * match paymentRequested["event.entityId"] == providerPayResult.PaymentId
        * match paymentRequested["event.providerId"] == providerPayResult.ProviderId
        * match paymentRequested["event.productCode"] == 'HBA1CPOC'
        * string utcDateTime = DataGen().getUtcDateTimeString(providerPayResult.DateOfService.toString())
        * assert paymentRequested["event.dateOfService"] == utcDateTime.split('T')[0]
        * match paymentRequested["event.personId"] == providerPayResult.CenseoId
        * match paymentRequested["event.commonClientId"] == providerPayResult.ClientId
        * match paymentRequested["event.engagementTypeOptions"] == 1
        * match paymentRequested["timestamp"] == '#notnull'
        * match paymentRequested["event.additionalDetails.evaluationId"] == evaluation.evaluationId.toString()
        * match paymentRequested["event.additionalDetails.appointmentId"] == appointment.appointmentId.toString()
        * match paymentRequested["event.additionalDetails.examId"] == evalResult.HBA1CPOCId.toString()

        Examples:
            | answer_value | normality | cdiEventHeaderName | payProvider | statusCodeId |
            | '3.9'        | 'A'       | "CDIPassedEvent"   | true        | 10           |
            # | '3.9'        | 'A'       | "CDIFailedEvent"   | true        | 11           |
            | '4'          | 'N'       | "CDIPassedEvent"   | true        | 10           |
            # | '4'          | 'N'       | "CDIFailedEvent"   | true        | 11           |
            | '6.9'        | 'N'       | "CDIPassedEvent"   | true        | 10           |
            # | '6.9'        | 'N'       | "CDIFailedEvent"   | true        | 11           |
            | '7'          | 'A'       | "CDIPassedEvent"   | true        | 10           |
            # | '7'          | 'A'       | "CDIFailedEvent"   | true        | 11           |

    @TestCaseKey=ANC-T564
    @TestCaseKey=ANC-T578
    @TestCaseKey=ANC-T580
    @TestCaseKey=ANC-T581
    Scenario Outline: HBA1CPOC Non-Payable (Business rules met/not met) - (CDIPassedEvent) and (CDIFailedEvent) <testScenario> <payProvider>
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
                    "AnswerValue": <answer_value>,
                },
                {
                    "AnswerId": 33264,
                    "AnsweredDateTime": '#(timestamp)',
                    "AnswerValue":  '<expirationDateSample>',
                },
                {
                    "AnswerId": 22034,
                    "AnsweredDateTime": '#(timestamp)',
                    "AnswerValue": '<dosSample>'
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
        * karate.call("classpath:helpers/eval/saveEval.feature")
        * karate.call("classpath:helpers/eval/stopEval.feature")
        * karate.call("classpath:helpers/eval/finalizeEval.feature")

        * def evalResult = Hba1cPOCDb().getResultsByEvaluationId(evaluation.evaluationId)[0]
        * match evalResult == "#notnull"
        
        # Publish the cdi event to the cdi_events topic
        * string cdiEventName = "cdi_events"
        * string cdiEventKey = evaluation.evaluationId
        * def eventId = DataGen().uuid()
        * string cdiEventHeader = { 'Type' : <cdiEventHeaderName>}
        * string cdiEventValue = {"RequestId":"#(eventId)","EvaluationId":"#(evaluation.evaluationId)","DateTime":"#(timestamp)","Username":"karateTestUser","ApplicationId":"manual","Reason":"reschedule","PayProvider":<payProvider>,"Products":[{"EvaluationId":"#(evaluation.evaluationId)","ProductCode":"HHRA"},{"EvaluationId":"#(evaluation.evaluationId)","ProductCode":"HBA1CPOC"}]}
        * kafkaProducerHelper.send(cdiEventName, cdiEventKey, cdiEventHeader, cdiEventValue)

        # Validate the entry in the ProviderPay table
        * def providerPayResult = Hba1cPOCDb().getProviderPayResultsWithEvalId(evaluation.evaluationId)[0]
        * match providerPayResult == '#null'

        # Validate that a Kafka event - PaymentRequested - related to ProviderPay was not raised by ProviderPay API
        * string paymentRequestEvent = KafkaConsumerHelper.getMessageByTopicAndHeaderAndAChildField("providerpay_internal", "PaymentRequested", evalResult.CenseoId, 5, 1000)
        * assert paymentRequestEvent.length == 0

        # Validate that a Kafka event - ProviderPayRequestSent - was not raised
        * json requestSentEvent = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("a1cpoc_status", evaluation.evaluationId + '', "ProviderPayRequestSent", 5, 1000))
        * match requestSentEvent == {}

        # Validate that the Kafka event - ProviderPayableEventReceived - was not raised
        * json payableEvent = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("a1cpoc_status", evaluation.evaluationId + '', "ProviderPayableEventReceived", 5, 1000))
        * match payableEvent == {}

        # Validate that the Kafka event - ProviderNonPayableEventReceived - was raised
        * json nonPayableEvent = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("a1cpoc_status", evaluation.evaluationId + '', "ProviderNonPayableEventReceived", 10, 5000))
        * match nonPayableEvent.EvaluationId == evaluation.evaluationId
        * match nonPayableEvent.ProviderId == providerDetails.providerId
        * match nonPayableEvent.ParentCdiEvent == <cdiEventHeaderName>
        * match nonPayableEvent.MemberPlanId == memberDetails.memberPlanId
        * match nonPayableEvent.Reason contains <expectedFailReason>
        * match nonPayableEvent.ProductCode == "HBA1CPOC"
        * match DataGen().RemoveMilliSeconds(nonPayableEvent.CreateDate) == DataGen().RemoveMilliSeconds(DataGen().getUtcDateTimeString(evalResult.CreatedDateTime.toString()))
        * assert nonPayableEvent.ReceivedDate.split('T')[0] == evalResult.ReceivedDateTime.toString()

        # # Validate the entry using EvaluationId in HBA1CPOC & HBA1CPOCStatus tables
        * def examStatusResults = Hba1cPOCDb().getExamStatusByEvaluationId(evaluation.evaluationId)
        # Status 1 = HBA1CPOCPerformed, Status 8 = ProviderPayableEventReceived, Status 9 = ProviderPayRequestSent, Status 13 = ProviderNonPayableEventReceived
        * match examStatusResults[*].HBA1CPOCStatusCodeId contains 1
        * match examStatusResults[*].HBA1CPOCStatusCodeId contains <statusCodeId>
        * match examStatusResults[*].HBA1CPOCStatusCodeId contains 13
        * match examStatusResults[*].HBA1CPOCStatusCodeId !contains 9
        * match examStatusResults[*].HBA1CPOCStatusCodeId !contains 8
        
        Examples:
            | testScenario                  | dosSample                   | expirationDateSample           | answer_value | normality | cdiEventHeaderName | payProvider | statusCodeId | expectedFailReason                            |
            # | 'failedWithoutPayRulesMet'    | #(DataGen().isoDateStamp()) | #(DataGen().isoDateStamp(30))  | '3.9'        | 'A'       | "CDIFailedEvent"   | false       | 12           | "PayProvider is false for the CDIFailedEvent" |
            # | 'failedWithoutPayRulesMet'    | #(DataGen().isoDateStamp()) | #(DataGen().isoDateStamp(30))  | '4'          | 'N'       | "CDIFailedEvent"   | false       | 12           | "PayProvider is false for the CDIFailedEvent" |
            # | 'failedWithoutPayRulesMet'    | #(DataGen().isoDateStamp()) | #(DataGen().isoDateStamp(30))  | '6.9'        | 'N'       | "CDIFailedEvent"   | false       | 12           | "PayProvider is false for the CDIFailedEvent" |
            # | 'failedWithoutPayRulesMet'    | #(DataGen().isoDateStamp()) | #(DataGen().isoDateStamp(30))  | '7'          | 'A'       | "CDIFailedEvent"   | false       | 12           | "PayProvider is false for the CDIFailedEvent" |
            # | 'failedWithoutPayPastExp'     | #(DataGen().isoDateStamp()) | #(DataGen().isoDateStamp(-30)) | '7'          | 'A'       | "CDIFailedEvent"   | false       | 12           | "PayProvider is false for the CDIFailedEvent" |
            # | 'failedWithoutPayNullDos'     | ""                          | #(DataGen().isoDateStamp(30))  | '4'          | 'N'       | "CDIFailedEvent"   | false       | 12           | "PayProvider is false for the CDIFailedEvent" |
            | 'passedPastExp'               | #(DataGen().isoDateStamp()) | #(DataGen().isoDateStamp(-30)) | '4'          | 'N'       | "CDIPassedEvent"   | true        | 10           | "ExpirationDate is before DateOfService"      |
            | 'passedNullDos'               | ""                          | #(DataGen().isoDateStamp())    | '4'          | 'N'       | "CDIPassedEvent"   | true        | 10           | "Invalid ExpirationDate or DateOfService"     |
            # | 'failedWithPayPastExp'        | #(DataGen().isoDateStamp()) | #(DataGen().isoDateStamp(-30)) | '4'          | 'N'       | "CDIFailedEvent"   | true        | 11           | "ExpirationDate is before DateOfService"      |
            # | 'failedWithPayNullDos'        | ""                          | #(DataGen().isoDateStamp())    | '4'          | 'N'       | "CDIFailedEvent"   | true        | 11           | "Invalid ExpirationDate or DateOfService"     |

    @TestCaseKey=ANC-T592
    @ignore
    Scenario Outline: HBA1CPOC ProviderPay - ProviderPay API is not triggered from the PdfDeliveredToClient event 
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
                    "AnswerValue": <answer_value>,
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

        * karate.call('classpath:helpers/eval/saveEval.feature')
        * karate.call('classpath:helpers/eval/stopEval.feature')
        * karate.call('classpath:helpers/eval/finalizeEval.feature')

        * def evalResult = Hba1cPOCDb().getResultsByEvaluationId(evaluation.evaluationId)[0]
        * match evalResult == "#notnull"

        # Validate that the database HBA1CPOC details are as expected using EvaluationId in HBA1CPOC
        * def result = Hba1cPOCDb().getResultsByEvaluationId(evaluation.evaluationId)[0]
        * match result.EvaluationId == evaluation.evaluationId
        * match result.MemberPlanId == memberDetails.memberPlanId
        * match result.CenseoId == memberDetails.censeoId
        * match result.AppointmentId == appointment.appointmentId
        * match result.ProviderId == providerDetails.providerId
        * match result.A1CPercent == <answer_value>
        * match result.NormalityIndicator == <normality>
        * match result.ReceivedDateTime.toString() == dateStamp

        # Publish the PDF event to the pdfdelivery topic
        * string pdfEventKey = evaluation.evaluationId
        * string batchName = `Ancillary_Services_Karate_Tests_${DataGen().formattedDateStamp('yyyMMdd')}`
        * def eventId = DataGen().uuid()
        * string pdfEventValue = {'EventId': '#(eventId)','EvaluationId': '#(evaluation.evaluationId)','CreatedDateTime': '#(timestamp)','DeliveryDateTime': '#(pdfDeliveryDate)','BatchName': '#(batchName)','ProductCodes':['HBA1CPOC'],'BatchId': 14245}
        * string pdfDeliveredToClientHeader = {'Type': 'PdfDeliveredToClient'}
        * kafkaProducerHelper.send("pdfdelivery", pdfEventKey, pdfDeliveredToClientHeader, pdfEventValue)

        # Validate the entry using EvaluationId in ProviderPay table for HBA1CPOC
        * def providerPayResult = Hba1cPOCDb().getProviderPayResultsWithEvalId(evaluation.evaluationId)[0]
        * match providerPayResult == '#null'

        # # Validate the entry using EvaluationId in HBA1CPOCStatus tables
        * def examStatusResults = Hba1cPOCDb().getExamStatusByEvaluationId(evaluation.evaluationId)
        # Status 8 = ProviderPayableEventReceived, Status 9 = ProviderPayRequestSent
        * match examStatusResults[*].HBA1CPOCStatusCodeId !contains 8
        * match examStatusResults[*].HBA1CPOCStatusCodeId !contains 9

        # Validate that a Kafka event - PaymentRequested - related to ProviderPay was not raised by ProviderPay API
        * string paymentRequestEvent = KafkaConsumerHelper.getMessageByTopicAndHeaderAndAChildField("providerpay_internal", "PaymentRequested", evalResult.CenseoId, 5, 1000)
        Then print "BreakPoint", evalResult.CenseoId
        * assert paymentRequestEvent.length == 0

        # Validate that a Kafka event - ProviderPayRequestSent - was not raised
        * json requestSentEvent = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("a1cpoc_status", evaluation.evaluationId + '', "ProviderPayRequestSent", 5, 1000))
        * match requestSentEvent == {}

        Examples:
            | answer_value | normality |
            | '3.9'        | 'A'       |
            | '4'          | 'N'       |
            | '6.9'        | 'N'       |
            
    @TestCaseKey=ANC-T601
    Scenario Outline: HBA1CPOC Non-Payable - Not Performed Exam
        * def randomNotes = Faker().randomQuote()
        * set evaluation.answers =
            """
            [
                {
                    "AnswerId": 33071,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": 2
                },
                {
                    "AnswerId": 33088,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": 'Member refused',
                },
                {
                    "AnswerId": <answer_id>,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": <answer_value>,
                },
                {
                    "AnswerId": 33079,
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

        # Validate that the database HBA1CPOC details are as expected using EvaluationId in HBA1CPOC
        * def result = Hba1cPOCDb().getResultsByEvaluationId(evaluation.evaluationId)[0]
        * def not_performed_result = Hba1cPOCDb().getNotPerformedResultsByEvaluationId(evaluation.evaluationId)[0]
        * match not_performed_result.EvaluationId == evaluation.evaluationId
        * match not_performed_result.MemberPlanId == memberDetails.memberPlanId
        * match not_performed_result.CenseoId == memberDetails.censeoId
        * match not_performed_result.AppointmentId == appointment.appointmentId
        * match not_performed_result.ProviderId == providerDetails.providerId
        * match not_performed_result.HBA1CPOCId != null
        * match not_performed_result.Notes == randomNotes

        # Validate that the Kafka event details are as expected
        * json event = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("A1CPOC_Status", evaluation.evaluationId + '', "NotPerformed", 10, 5000))      
        * match event.Reason == <expected_reason>
        * match event.ProductCode == 'HBA1CPOC'
        * match event.ReasonNotes == randomNotes
        * match event.ReasonType == 'Member refused'

        # Validate that the Kafka events include the expected event headers
        * string headers = KafkaConsumerHelper.getHeadersByTopicAndKey("A1CPOC_Status", evaluation.evaluationId + '', 5, 5000)  
        * match headers contains 'NotPerformed'

        # Publish the cdi event to the cdi_events topic
        * string cdiEventName = "cdi_events"
        * string cdiEventKey = evaluation.evaluationId
        * def eventId = DataGen().uuid()
        * string cdiEventHeader = { 'Type' : <cdiEventHeaderName>}
        * string cdiEventValue = {"RequestId":"#(eventId)","EvaluationId":"#(evaluation.evaluationId)","DateTime":"#(timestamp)","Username":"karateTestUser","ApplicationId":"manual","Reason":"reschedule","PayProvider":<payProvider>,"Products":[{"EvaluationId":"#(evaluation.evaluationId)","ProductCode":"HHRA"},{"EvaluationId":"#(evaluation.evaluationId)","ProductCode":"HBA1CPOC"}]}
        * kafkaProducerHelper.send(cdiEventName, cdiEventKey, cdiEventHeader, cdiEventValue)
        
        # Validate the entry using EvaluationId in HBA1CPOC & HBA1CPOCStatus tables
        * def examStatusResults = Hba1cPOCDb().getExamStatusByEvaluationId(evaluation.evaluationId)
        # Status 7 = HBA1CPOCNotPerformed, Status 14 = BillRequestNotSent 
        * match examStatusResults[*].HBA1CPOCStatusCodeId contains 7 && 14
        
        # Validate that a Kafka event - ProviderPayRequestSent - was not raised
        * json requestSentEvent = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("a1cpoc_status", evaluation.evaluationId + '', "ProviderPayRequestSent", 5, 1000))
        * match requestSentEvent == {}

        # Validate that the Kafka event - ProviderPayableEventReceived - was not raised
        * json payableEvent = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("a1cpoc_status", evaluation.evaluationId + '', "ProviderPayableEventReceived", 5, 1000))
        * match payableEvent == {}

        # Validate that the Kafka event - ProviderPayableEventReceived - was not raised
        * json nonPayableEvent = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("a1cpoc_status", evaluation.evaluationId + '', "ProviderNonPayableEventReceived", 5, 1000))
        * match nonPayableEvent == {}
        
        Examples:
            | answer_id | answer_value                  | expected_reason             | cdiEventHeaderName | payProvider | statusCodeId |
            # | 33074     | 'Member recently completed'   | 'Member recently completed' | "CDIFailedEvent"   | false       | 12           | 
            # | 33074     | 'Member recently completed'   | 'Member recently completed' | "CDIFailedEvent"   | true        | 11           | 
            | 33074     | 'Member recently completed'   | 'Member recently completed' | "CDIPassedEvent"   | true        | 10           |