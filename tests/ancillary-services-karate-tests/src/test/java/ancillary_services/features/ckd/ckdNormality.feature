@ckd
@envnot=prod
Feature: CKD Normality Determination Tests

    Background:
        * eval if (env == 'prod') karate.abort();
        * def DataGen = function() { var DataGen = Java.type('helpers.data.DataGen'); return new DataGen(); }
        * def CkdDb = function() { var CkdDb = Java.type('helpers.database.ckd.CkdDb'); return new CkdDb(); }
        * def timestamp = DataGen().isoTimestamp()
        * def expirationDate = DataGen().isoDateStamp(30)
        * def dateStamp = DataGen().isoDateStamp()

        * def memberDetails = karate.call("classpath:helpers/member/createMember.js")
        * def appointment = karate.call('classpath:helpers/appointment/createAppointment.feature', { products: ['HHRA', 'CKD'] }).response
        * def evaluation = karate.call('classpath:helpers/eval/startEval.feature').response
        * def KafkaConsumerHelper = Java.type('helpers.kafka.KafkaConsumerHelper')
        
    @TestCaseKey=ANC-T328
    Scenario Outline: CKD Normality Business Rules Validation
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

        # Validate that the database CKD details are as expected using EvaluationId in CKD 
        * def performedResult = CkdDb().getResultsByEvaluationId(evaluation.evaluationId)[0]
        * match performedResult.ProviderId == providerDetails.providerId
        * match performedResult.CKDAnswer == <answer_value>
        * match performedResult.EvaluationId == evaluation.evaluationId
        * match performedResult.MemberPlanId == memberDetails.memberPlanId
        * match performedResult.CenseoId == memberDetails.censeoId
        * match performedResult.AppointmentId == appointment.appointmentId

        # Validate that the Kafka event details are as expected
        * json event = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("ckd_status", evaluation.evaluationId + '', "Performed", 10, 5000))    
        
        * match event.ProductCode == 'CKD'
        * match event.MemberPlanId == memberDetails.memberPlanId
        * match event.ProviderId == providerDetails.providerId
        
         # Validate that the Kafka events include the expected event headers
         * string headers = KafkaConsumerHelper.getHeadersByTopicAndKey("ckd_status", evaluation.evaluationId + '', 10, 5000)
        
         * match headers contains 'Performed'

         # Validate the entry using EvaluationId in CKD & CKDStatus tables
         * def examStatusResults = CkdDb().getExamStatusByEvaluationId(evaluation.evaluationId)
         # Status 1 = CKDPerformed
         * match examStatusResults[*].CKDStatusCodeId contains 1

        # Validate the entry using CKDAnswerValue in LookupCKDAnswer table
        * def examAnswerIdValues = CkdDb().getAnswerValuesByAnswerId(<answer_id>)[0]

        # Validate that the Kafka event details are as expected for ckd_results
        * json ckdResults = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("ckd_results", evaluation.evaluationId + '', "Result", 10, 5000))
        * match ckdResults.ProductCode == 'CKD'
        * match ckdResults.EvaluationId == evaluation.evaluationId
        * match ckdResults.PerformedDate != null
        * match ckdResults.ReceivedDate != null
        * match ckdResults.ExpiryDate != null
        * match ckdResults.IsBillable == true
        * match ckdResults.Determination == examAnswerIdValues.NormalityIndicator
        * match ckdResults.Results[0].Type == 'Albumin'
        * match ckdResults.Results[0].Result == examAnswerIdValues.Albumin.toString()
        * match ckdResults.Results[1].Type == 'Creatinine'
        * match ckdResults.Results[1].Result == examAnswerIdValues.Creatinine.toString()
        * match ckdResults.Results[2].Type == 'uAcr'
        * match ckdResults.Results[2].Result == examAnswerIdValues.Acr 

        Examples:
            | answer_id | answer_value                                           |
            | 20962     | 'Albumin: 10 - Creatinine: 0.1 ; Cannot be determined' |
            | 20963     | 'Albumin: 30 - Creatinine: 0.1 ; Abnormal'             |
            | 20964     | 'Albumin: 80 - Creatinine: 0.1 ; High Abnormal'        |
            | 20965     | 'Albumin: 150 - Creatinine: 0.1 ; High Abnormal'       |
            | 20966     | 'Albumin: 10 - Creatinine: 0.5 ; Normal'               |
            | 20967     | 'Albumin: 30 - Creatinine: 0.5 ; Abnormal'             |
            | 20968     | 'Albumin: 80 - Creatinine: 0.5 ; Abnormal'             |
            | 20969     | 'Albumin: 150 - Creatinine: 0.5 ; Abnormal'            |
            | 20970     | 'Albumin: 10 - Creatinine: 1.0 ; Normal'               |
            | 20971     | 'Albumin: 30 - Creatinine: 1.0 ; Abnormal'             |
            | 20972     | 'Albumin: 80 - Creatinine: 1.0 ; Abnormal'             |
            | 20973     | 'Albumin: 150 - Creatinine: 1.0 ; Abnormal'            |
            | 20974     | 'Albumin: 10 - Creatinine: 2.0 ; Normal'               |
            | 20975     | 'Albumin: 30 - Creatinine: 2.0 ; Normal'               |
            | 20976     | 'Albumin: 80 - Creatinine: 2.0 ; Abnormal'             |
            | 20977     | 'Albumin: 150 - Creatinine: 2.0 ; Abnormal'            |
            | 20978     | 'Albumin: 10 - Creatinine: 3.0 ; Normal'               |
            | 20979     | 'Albumin: 30 - Creatinine: 3.0 ; Normal'               |
            | 20980     | 'Albumin: 80 - Creatinine: 3.0 ; Normal'               |
            | 20981     | 'Albumin: 150 - Creatinine: 3.0 ; Abnormal'            |
            