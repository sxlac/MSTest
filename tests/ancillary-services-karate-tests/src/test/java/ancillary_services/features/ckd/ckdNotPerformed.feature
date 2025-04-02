@ckd
@envnot=prod
Feature: CKD Not Performed Tests

    Background:
        * eval if (env == 'prod') karate.abort();
        * def DataGen = function() { var DataGen = Java.type('helpers.data.DataGen'); return new DataGen(); }
        * def CkdDb = function() { var CkdDb = Java.type('helpers.database.ckd.CkdDb'); return new CkdDb(); }
        * def Faker = function() { var Faker = Java.type('helpers.data.Faker'); return new Faker(); }
        * def timestamp = DataGen().isoTimestamp()
        * def expirationDate = DataGen().isoDateStamp(30)
        * def dateStamp = DataGen().isoDateStamp()

        * def memberDetails = karate.call("classpath:helpers/member/createMember.js")
        * def appointment = karate.call('classpath:helpers/appointment/createAppointment.feature', { products: ['HHRA', 'CKD'] }).response
        * def evaluation = karate.call('classpath:helpers/eval/startEval.feature').response
        * def KafkaConsumerHelper = Java.type('helpers.kafka.KafkaConsumerHelper')
        * def kafkaProducerHelper = Java.type('helpers.kafka.KafkaProducerHelper')

    @TestCaseKey=ANC-T675
    Scenario Outline: CKD Not Performed - Member Refused
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
        * json event = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("ckd_status", evaluation.evaluationId + '', "NotPerformed", 10, 5000))    
        
        * match event.ReasonType == 'Member Refused'
        * match event.Reason == <expected_reason>
        * match event.ReasonNotes == randomNotes
        * match event.ProductCode == 'CKD'
        * match event.MemberPlanId == memberDetails.memberPlanId
        * match event.ProviderId == providerDetails.providerId
        
        # Validate that the Kafka events include the expected event headers
        * string headers = KafkaConsumerHelper.getHeadersByTopicAndKey("ckd_status", evaluation.evaluationId + '', 10, 5000)
        * match headers contains 'NotPerformed'

        # Validate the entry using EvaluationId in CKD & CKDStatus tables
        * def examStatusResults = CkdDb().getExamStatusByEvaluationId(evaluation.evaluationId)
        # Status 7 = CKDNotPerformed
        * match examStatusResults[*].CKDStatusCodeId contains only 7

        Examples:
            | answer_id | answer_value                | expected_reason             |
            | 30863     | 'Member recently completed' | 'Member recently completed' |
            | 30864     | 'Scheduled to complete'     | 'Scheduled to complete'     |
            | 30865     | 'Member apprehension'       | 'Member apprehension'       |
            | 30866     | 'Not interested'            | 'Not interested'            |
            | 30867     | 'Other'                     | 'Other'                     |

        @TestCaseKey=ANC-T676
        Scenario Outline: CKD Not Performed - Provider Unable to Perform
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
                    'AnswerId': 30862,
                    'AnsweredDateTime': '#(timestamp)',
                    'AnswerValue': 'Unable to perform'
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
        * match notPerformedResult.EvaluationId == evaluation.evaluationId
        * match notPerformedResult.MemberPlanId == memberDetails.memberPlanId
        * match notPerformedResult.CenseoId == memberDetails.censeoId
        * match notPerformedResult.AppointmentId == appointment.appointmentId
        * match notPerformedResult.ProviderId == providerDetails.providerId
        * match notPerformedResult.ExamNotPerformedId != null
        * match notPerformedResult.AnswerId == <answer_id>
        * match notPerformedResult.Reason == <expected_reason>
        * match notPerformedResult.Notes == randomNotes

        # Validate that the Kafka event details are as expected
        * json event = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("ckd_status", evaluation.evaluationId + '', "NotPerformed", 10, 5000))    
        
        * match event.ReasonType == 'Unable to Perform'
        * match event.Reason == <expected_reason>
        * match event.ReasonNotes == randomNotes
        * match event.ProductCode == 'CKD'
        * match event.MemberPlanId == memberDetails.memberPlanId
        * match event.ProviderId == providerDetails.providerId

        # Validate that the Kafka events include the expected event headers
        * string headers = KafkaConsumerHelper.getHeadersByTopicAndKey("ckd_status", evaluation.evaluationId + '', 10, 5000)
        * match headers contains 'NotPerformed'

        # Validate the entry using EvaluationId in CKD & CKDStatus tables
        * def examStatusResults = CkdDb().getExamStatusByEvaluationId(evaluation.evaluationId)
        # Status 7 = CKDNotPerformed
        * match examStatusResults[*].CKDStatusCodeId contains only 7

        Examples:
            | answer_id | answer_value                | expected_reason            |
            | 30870     | 'Member recently completed' | 'Technical issue'          |
            | 30871     | 'Scheduled to complete'     | 'Environmental issue'      |
            | 30872     | 'Member apprehension'       | 'No supplies or equipment' |
            | 30873     | 'Insufficient training'     | 'Insufficient training'    |
            | 50899     | 'Member physically unable'  | 'Member physically unable' |

        @TestCaseKey=ANC-T329
        Scenario: CKD Not Performed - PDF Delivery with BillRequestNotSent Message
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
                    'AnswerId': 30867,
                    'AnsweredDateTime': '#(timestamp)',
                    'AnswerValue': 'Other'
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
        * match notPerformedResult.AnswerId == 30867
        * match notPerformedResult.Reason == 'Other'
        * match notPerformedResult.Notes == randomNotes

        # Validate that the Kafka event details are as expected
        * json event = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("ckd_status", evaluation.evaluationId + '', "NotPerformed", 10, 5000))    
        * match event.ReasonType == 'Member Refused'
        * match event.Reason == 'Other'
        * match event.ReasonNotes == randomNotes
        * match event.ProductCode == 'CKD'
        * match event.MemberPlanId == memberDetails.memberPlanId
        * match event.ProviderId == providerDetails.providerId

        # Publish the PDF event to the pdfdelivery topic
        * string pdfEventKey = evaluation.evaluationId
        * string batchName = `Ancillary_Services_Karate_Tests_${DataGen().formattedDateStamp('yyyMMdd')}`
        * def eventId = DataGen().uuid()
        * string pdfDeliveredToClientHeader = {'Type': 'PdfDeliveredToClient'}
        * string pdfEventValue = {'EventId': '#(eventId)','EvaluationId': '#(evaluation.evaluationId)','CreatedDateTime': '#(timestamp)','DeliveryDateTime': '#(timestamp)','BatchName': '#(batchName)','ProductCodes':['CKD'],'BatchId': 14245}
        * kafkaProducerHelper.send("pdfdelivery", pdfEventKey, pdfDeliveredToClientHeader, pdfEventValue)

        # Validate that the BillRequestNotSent event published to Kafka
        * json event = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("ckd_status", evaluation.evaluationId + '',"BillRequestNotSent", 10, 5000))
        * match event.PdfDeliveryDate contains  dateStamp 
        * match event.ProductCode == 'CKD' 
        * match event.EvaluationId == evaluation.evaluationId
        * match event.MemberPlanId == appointment.memberPlanId
        * match event.ProviderId.toString() == appointment.providerId
        * match event.CreatedDate contains  dateStamp
        * match event.ReceivedDate contains  dateStamp

        # Additional check to cover fixed issue ANC-2723 (Billing requests sent to RCM even if CKD was not performed)
        * def InExamNotPerfNotInRcm = CkdDb().getNotInRcmByEvalIdInExamNotPerf(evaluation.evaluationId)[0]
        * match InExamNotPerfNotInRcm != null
        * match InExamNotPerfNotInRcm.CenseoId == memberDetails.censeoId
        * match InExamNotPerfNotInRcm.Notes == randomNotes

        # Validate the entry using EvaluationId in CKD & CKDStatus tables
        * def examStatusResults = CkdDb().getExamStatusByEvaluationId(evaluation.evaluationId)
        # Status 7 = CKDNotPerformed  -  Status 8 = BillRequestNotSent
        * match examStatusResults[*].CKDStatusCodeId contains 7 && 8