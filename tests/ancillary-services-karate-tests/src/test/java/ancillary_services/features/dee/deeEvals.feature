# Iris GradeAsNormal API is not functional. All tests with Iris integrations will need to be tested manually until issue is resolved. 
@ignore
@envnot=prod
# @dee
@parallel=false
Feature: DEE Evaluation Tests
# See https://cvs-hcd.atlassian.net/wiki/spaces/AncillarySvcs/pages/51220985/DEE+Form+Questions for answer definitions

    Background:
        * eval if (env == 'prod') karate.abort();
        * def DataGen = function() { var DataGen = Java.type('helpers.data.DataGen'); return new DataGen(); }
        * def DeeDb = function() { var DeeDb = Java.type("helpers.database.dee.DeeDb"); return new DeeDb(); }
        * def timestamp = DataGen().isoTimestamp()
        * def dateStamp = DataGen().isoDateStamp()
        * def sleep = function(pause){ java.lang.Thread.sleep(pause) }

        * def memberDetails = karate.call("classpath:helpers/member/createMember.js")
        * def appointment = karate.call('classpath:helpers/appointment/createAppointment.feature', { products: ['HHRA', 'DEE'] }).response
        * def evaluation = karate.call('classpath:helpers/eval/startEval.feature').response
        * def KafkaConsumerHelper = Java.type('helpers.kafka.KafkaConsumerHelper')
        
    @TestCaseKey=ANC-T384
    Scenario: DEE Patient Details in Results
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
                    "AnswerRowId": "6DBB912A-A078-4E7B-BE03-9DDB182628E4",
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "#(image1)"
                },
                {
                    "AnswerId": 30856,
                    "AnswerRowId": "FDBEC026-C6BE-4E5E-BE80-D6AC93E61F78",
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "#(image2)"
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
        # Needed due to there being a 10 second delay between CreateDEE command when message is transferred to ProcessDEE
        * eval sleep(10000)        

        * def exam = karate.call('classpath:/helpers/iris/exam/getExam.feature')
        * karate.call('classpath:helpers/iris/exam/gradeExamAsNormal.feature', exam)

        # Get and check database results
        * def result = DeeDb().getResultsByEvaluationId(evaluation.evaluationId)[0]
        * match result.ProviderId == providerDetails.providerId
        * match result.EvaluationId == evaluation.evaluationId

        # Get and check Kafka results
        * json resultEvent = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("dee_results", evaluation.evaluationId + '', "Result", 10, 5000))            
        * match resultEvent.IsBillable == true

        * def examStatusResults = DeeDb().getExamStatusByEvaluationId(evaluation.evaluationId)
        # 4 = Result Data Downloaded, 5 = PDF Data Downloaded 9 = Gradable, 18 = DEE Performed
        * match examStatusResults[*].ExamStatusCodeId contains 4 && 5 && 9 && 18

        # Validate the entry in the ExamResults table
        * def examResults = DeeDb().getResultsByEvaluationId(evaluation.evaluationId)[0]
        * match examResults.RightEyeHasPathology == false
        * match examResults.NormalityIndicator == "N"
        * match examResults.Gradeable == true
        * match examResults.LeftEyeHasPathology == false            
   
        # Get and check Kafka results
        * json event = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("dee_results", evaluation.evaluationId + '', "Result", 10, 5000))
        * match event.ProductCode == "DEE"
        * match event.EvaluationId == evaluation.evaluationId
        * match event.Determination == "N"

    @TestCaseKey=ANC-T391
    Scenario: DEE Patient Details in Results - Submit 12 Images for One Evaluation - Not Submitted for Grading
        
        # Submitting 12 images total to test ANC-2558
        * def image1 = karate.readAsString('classpath:ancillary_services/features/dee/images/normalEval/image-1.txt')
        * def image2 = karate.readAsString('classpath:ancillary_services/features/dee/images/normalEval/image-2.txt') 
        * def image3 = karate.readAsString('classpath:ancillary_services/features/dee/images/non-gradable/image-3.txt')
        * def image4 = karate.readAsString('classpath:ancillary_services/features/dee/images/non-gradable/image-4.txt')
        * def image5 = karate.readAsString('classpath:ancillary_services/features/dee/images/non-gradable/image-5.txt')
        * def image6 = karate.readAsString('classpath:ancillary_services/features/dee/images/non-gradable/image-6.txt')
        * def image7 = karate.readAsString('classpath:ancillary_services/features/dee/images/non-gradable/image-7.txt')
        * def image8 = karate.readAsString('classpath:ancillary_services/features/dee/images/non-gradable/image-8.txt')
        * def image9 = karate.readAsString('classpath:ancillary_services/features/dee/images/non-gradable/image-9.txt')
        * def image10 = karate.readAsString('classpath:ancillary_services/features/dee/images/non-gradable/image-10.txt') 
        * def image11 = karate.readAsString('classpath:ancillary_services/features/dee/images/non-gradable/image-11.txt')
        * def image12 = karate.readAsString('classpath:ancillary_services/features/dee/images/non-gradable/image-12.txt') 
        
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
                    "AnswerRowId": "6DBB912A-A078-4E7B-BE03-9DDB182628E4",
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "#(image1)"
                },
                {
                    "AnswerId": 30856,
                    "AnswerRowId": "FDBEC026-C6BE-4E5E-BE80-D6AC93E61F78",
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "#(image2)"
                },
                {
                    "AnswerId": 30856,
                    "AnswerRowId": "517E7337-B10B-4646-901E-0D1969AC6367",
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "#(image3)"
                },
                {
                    "AnswerId": 30856,
                    "AnswerRowId": "0032DB3A-D1E4-487E-8097-B8FAABD65F3B",
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "#(image4)"
                },
                {
                    "AnswerId": 30856,
                    "AnswerRowId": "5E36BE31-2556-48DD-B5D8-2C1D3D2CC353",
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "#(image5)"
                },
                {
                    "AnswerId": 30856,
                    "AnswerRowId": "31DB0E65-4AE0-4228-A249-34DC7F7BE841",
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "#(image6)"
                },
                {
                    "AnswerId": 30856,
                    "AnswerRowId": "C7ADB6B1-CA3A-43F1-8ED2-3AC04525BAAC",
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "#(image7)"
                },
                {
                    "AnswerId": 30856,
                    "AnswerRowId": "BE7386E6-24F1-44EC-9318-9998306E7DA4",
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "#(image8)"
                },
                {
                    "AnswerId": 30856,
                    "AnswerRowId": "263D85AB-8851-4A30-8118-2345804E158B",
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "#(image9)"
                },
                {
                    "AnswerId": 30856,
                    "AnswerRowId": "3647DCEA-3036-42A2-9982-943AD04C5B93",
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "#(image10)"
                },
                {
                    "AnswerId": 30856,
                    "AnswerRowId": "53859D16-D1C0-4337-8FC7-BD668E6DD761",
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "#(image11)"
                },
                {
                    "AnswerId": 30856,
                    "AnswerRowId": "AD6AE8D9-A52D-4A67-8FAE-87D434E30D49",
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "#(image12)"
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
        * eval sleep(20000) 

        # Get the DeeExamId from Exam table
        * def deeExamId = DeeDb().getIrisExamIdFromDeeDb(evaluation.evaluationId)[0].DeeExamId

        # Get and check database results
        * def imageResults = DeeDb().getExamImagesByEvaluationId(evaluation.evaluationId)
        * match each imageResults[*].ProviderId == providerDetails.providerId
        * match each imageResults[*].RequestId == '#notnull'
        * match each imageResults[*].ClientId == 14
        * match each imageResults[*].DeeExamId == deeExamId
        * match each imageResults[*].MemberPlanId == memberDetails.memberPlanId
        * match each imageResults[*].count == 12
      
        
    @TestCaseKey=ANC-T694
    Scenario: DEE Performed - Incomplete - Image Only Submitted for Left Laterality
        * def image1 = karate.readAsString('classpath:ancillary_services/features/dee/images/normalEval/image-1.txt')
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
                    "AnswerRowId": "6DBB912A-A078-4E7B-BE03-9DDB182628E4",
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "#(image1)"
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
        # Needed due to there being a 10 second delay between CreateDEE command when message is transferred to ProcessDEE
        * eval sleep(10000)        

        * def exam = karate.call('classpath:/helpers/iris/exam/getExam.feature')
        * karate.call('classpath:helpers/iris/exam/gradeExamAsNormal.feature', exam)

        # Validate the entry in the ExamResults table
        * def examResult = DeeDb().getResultsByEvaluationId(evaluation.evaluationId)[0]
        * match examResult.ProviderId == providerDetails.providerId
        * match examResult.EvaluationId == evaluation.evaluationId
        * match examResult.NormalityIndicator == "N"
        * match examResult.Gradeable == true
        * match examResult.LeftEyeHasPathology == false
        * match examResult.RightEyeHasPathology == null 

        * def examStatusResults = DeeDb().getExamStatusByEvaluationId(evaluation.evaluationId)
        # 4 = Result Data Downloaded, 5 = PDF Data Downloaded 9 = Gradable, 18 = DEE Performed
        * match examStatusResults[*].ExamStatusCodeId contains 4 && 5 && 9 && 18
   
        # Get and check Kafka results - Result
        * json event = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("dee_results", evaluation.evaluationId + '', "Result", 10, 5000))
        * match event.ProductCode == "DEE"
        * match event.EvaluationId == evaluation.evaluationId
        * match event.IsBillable == false
        * match event.Determination == "N"
        * match event.Results[0].Side == "L"
        * match event.Results[0].Gradable == true
        * match event.Results[0].Pathology == false
        * match event.Results[1].Side == "R"
        * match event.Results[1].Gradable == false
        * match event.Results[1].AbnormalIndicator == "U"
        * match event.Results[1].Pathology == null
        * match event.Results[1].NotGradableReasons[0] == "No images are available"





