@spirometry
@envnot=prod
Feature: Publish Overread Results to Kafka

    Background:
        * eval if (env == 'prod') karate.abort();
        * def DataGen = function() { var DataGen = Java.type('helpers.data.DataGen'); return new DataGen(); }
        * def SpiroDb = function() { var SpiroDb = Java.type('helpers.database.spirometry.SpirometryDb'); return new SpiroDb(); }
        * def timestamp = DataGen().isoTimestamp()
        * def dateStamp = DataGen().isoDateStamp()

        * def memberDetails = karate.call('classpath:helpers/member/createMember.js')
        * def appointment = karate.call('classpath:helpers/appointment/createAppointment.feature', { products: ['HHRA', 'SPIROMETRY'] }).response
        * def evaluation = karate.call('classpath:helpers/eval/startEval.feature').response
        * def kafkaProducerHelper = Java.type('helpers.kafka.KafkaProducerHelper')
        * def KafkaConsumerHelper = Java.type('helpers.kafka.KafkaConsumerHelper')
        * def sleep = function(pause){ java.lang.Thread.sleep(pause) }

    @TestCaseKey=ANC-T706
    @TestCaseKey=ANC-T512
    @TestCaseKey=ANC-T516
    @TestCaseKey=ANC-T514
    @TestCaseKey=ANC-T515
    @TestCaseKey=ANC-T518
    @TestCaseKey=ANC-T519
    Scenario Outline: Spirometry Process Manager Publishes Overread Results to Kafka- Considering the history of COPD is true
        * set evaluation.answers =
            """
            [
                {
                    "AnswerId": 50919,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": 1
                },
                {
                    "AnswerId": <session_grade_id>,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": <session_grade_value>
                },
                {
                    "AnswerId": 50999,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": 100
                },
                {
                    "AnswerId": 51000,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": 30
                },
                {
                    "AnswerId": 51002,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": 0.3
                },
                {
                    "AnswerId": 50944,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "Yes" 
                },        
                {
                    "AnswerId": 50948,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue":  "No"   
                },
                {
                    "AnswerId": 50952,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "Unknown"
                },
                {
                    "AnswerId": 22034,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "#(timestamp)"
                },
                {
                    "AnswerId": 29614,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": <Hx_of_COPD>
                },
                {
                    "AnswerId": 51420,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue":  <LungFunctionScore>
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
        
        # Publish the Overread event to the overread_spirometry topic
        * string overrreadEventKey = appointment.appointmentId
        * def OverreadId = DataGen().uuid()

        * string overreadHeader = {'Type': 'OverreadProcessed'}
        * string overreadEventValue = {'OverreadId': '#(OverreadId)','MemberId':'','AppointmentId': '#(appointment.appointmentId)','SessionId': '#(OverreadId)','PerformedDateTime': '#(timestamp)','OverreadDateTime': '#(timestamp)','BestTestId': '#(OverreadId)','BestFvcTestId': '#(OverreadId)','BestFvcTestComment': 'TestComment','BestFev1TestId': '#(OverreadId)','BestFev1TestComment': 'TestComment','BestPefTestId': '#(OverreadId)','BestPefTestComment': 'TestComment','Comment': 'TestComment','Fev1FvcRatio':<overread_ratio>,'OverreadBy': 'JohnDoe','ObstructionPerOverread':<obstructionPerOverread>,'ReceivedDateTime': '#(timestamp)'}
        * kafkaProducerHelper.send("overread_spirometry", overrreadEventKey, overreadHeader, overreadEventValue)

        # Validate that the ResultsReceived event published to spirometry_result topic has the expected properties as in the result contract
        * json event = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("spirometry_result", evaluation.evaluationId + '', "ResultsReceived", 10, 5000)) 
        
        * match event.ProductCode == 'SPIROMETRY'
        * match event.EvaluationId == evaluation.evaluationId
        * match event.PerformedDate contains dateStamp
        * match event.ReceivedDate contains dateStamp
        * match event.IsBillable == <IsBillable>
        * match event.Determination == <determination>
        * match event.Results.Fev1OverFvc == <overread_ratio>
        * match event.Results.SessionGrade  == <session_grade_value>
        * match event.Results.Fvc == 100
        * match event.Results.FvcNormality == 'N' 
        * match event.Results.Fev1 == 30   
        * match event.Results.Fev1Normality == 'A' 
        * match event.Results.HasSmokedTobacco == null
        * match event.Results.TotalYearsSmoking == null
        * match event.Results.ProducesSputumWithCough == null
        * match event.Results.CoughMucusOccurrenceFrequency == null
        * match event.Results.HadWheezingPast12mo == null
        * match event.Results.GetsShortnessOfBreathAtRest == null
        * match event.Results.GetsShortnessOfBreathWithMildExertion == null
        * match event.Results.NoisyChestOccurrenceFrequency == null
        * match event.Results.ShortnessOfBreathPhysicalActivityOccurrenceFrequency == null
        * match event.Results.LungFunctionScore == <LungFunctionScore>
        * match event.Results.Copd == null

        # Validate that the ResultsReceived event published to spirometry_status topic has the expected properties as in the result contract
        * json resultsEvent = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("spirometry_status", evaluation.evaluationId + '', "ResultsReceived", 10, 5000)) 
        
        * match resultsEvent.ProductCode == 'SPIROMETRY'
        * match resultsEvent.EvaluationId == evaluation.evaluationId
        * match resultsEvent.MemberPlanId == appointment.memberPlanId
        * match resultsEvent.ProviderId.toString() == appointment.providerId
        * match resultsEvent.CreateDate contains dateStamp
        * match resultsEvent.ReceivedDate contains dateStamp

        # Validate Exam Status Update in database
        * def examStatus = SpiroDb().getExamStatusByEvaluationId(evaluation.evaluationId)
        * match examStatus[*].Name contains 'Spirometry Exam Performed' && 'Overread Processed' && 'Results Received'
        
        #Validate NeedFlags value in Eval_saga tb
        * eval sleep(20000)  
        * def evalSaga = SpiroDb().getEvalSagaByEvaluationId(evaluation.evaluationId)
        * json sagaData = JSON.parse(evalSaga[0].Data)
        * match sagaData.NeedsFlag ==  <NeedsFlag>
        * match sagaData.NeedsOverread ==  true

        Examples:
        | session_grade_id | session_grade_value | obstructionPerOverread|  determination|IsBillable|             Hx_of_COPD                       |LungFunctionScore|overread_ratio|NeedsFlag |
        | 51947            | "E"                 | "YES"                 |  "A"          |true      |"Chronic obstructive pulmonary disease (COPD)"|19               |    0.65      |true      |
        | 51947            | "E"                 | "YES"                 |  "A"          |true      |"Chronic obstructive pulmonary disease"       |19               |    0.65      |true      |
        | 51947            | "E"                 | "YES"                 |  "A"          |true      |               "COPD"                         |19               |    0.65      |true      |
        | 51947            | "E"                 | "YES"                 |  "A"          |true      |"Chronic obstructive pulmonary disease (COPD)"|17               |    0.5       |true      |
        | 51947            | "F"                 | "NO"                  |  "N"          |true      |"Chronic obstructive pulmonary disease (COPD)"|15               |    0.7       |false     |
        | 50940            | "D"                 | "INCONCLUSIVE"        |  "U"          |false     |"Chronic obstructive pulmonary disease (COPD)"|15               |    0.8       |false     |
        | 50940            | "D"                 | "YES"                 |  "A"          |true      |"Hypertension"                                |15               |    0.5       |true      |
        | 50940            | "D"                 | "NO"                  |  "N"          |true      |"Hypertension"                                |15               |    0.7       |false     |
        | 50940            | "D"                 | "INCONCLUSIVE"        |  "U"          |false     |"Hypertension"                                |15               |    1.5       |false     |
        | 50940            | "D"                 | "NO"                  |  "N"          |true      |"Chronic obstructive pulmonary disease (COPD)"|19               |    0.7       |false     |
        | 50940            | "D"                 | "INCONCLUSIVE"        |  "U"          |false     |"Chronic obstructive pulmonary disease (COPD)"|19               |    1.0       |false     |
                 
    
    @TestCaseKey=ANC-T510
    @TestCaseKey=ANC-T553
    Scenario Outline: Spirometry Process Manager Publishes Overread Results to Kafka- Considering the history of COPD is false & ObstructionPerOverread=YES/NO/INCONCLUSIVE
        * set evaluation.answers =
            """
            [
                {
                    "AnswerId": 50919,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": 1
                },
                {
                    "AnswerId": 50940,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "D"
                },
                {
                    "AnswerId": 50999,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": 100
                },
                {
                    "AnswerId": 51000,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": 30
                },
                {
                    "AnswerId": 51002,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": 0.3
                },
                {
                    "AnswerId": 50944,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "Yes" 
                },        
                {
                    "AnswerId": 50948,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue":  "No"   
                },
                {
                    "AnswerId": 50952,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "Unknown"
                },
                {
                    "AnswerId": 22034,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "#(timestamp)"
                },
                {
                    "AnswerId": 29614,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "Hypertension"
                },
                {
                    "AnswerId": 51420,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue":  19
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
        
        # Publish the Overread event to the overread_spirometry topic
        * string overrreadEventKey = appointment.appointmentId
        * def OverreadId = DataGen().uuid()

        * string overreadHeader = {'Type': 'OverreadProcessed'}
        * string overreadEventValue = {'OverreadId': '#(OverreadId)','MemberId':'','AppointmentId': '#(appointment.appointmentId)','SessionId': '#(OverreadId)','PerformedDateTime': '#(timestamp)','OverreadDateTime': '#(timestamp)','BestTestId': '#(OverreadId)','BestFvcTestId': '#(OverreadId)','BestFvcTestComment': 'TestComment','BestFev1TestId': '#(OverreadId)','BestFev1TestComment': 'TestComment','BestPefTestId': '#(OverreadId)','BestPefTestComment': 'TestComment','Comment': 'TestComment','Fev1FvcRatio':<overread_ratio>,'OverreadBy': 'JohnDoe','ObstructionPerOverread':<ObstructionPerOverread>,'ReceivedDateTime': '#(timestamp)'}
        * kafkaProducerHelper.send("overread_spirometry", overrreadEventKey, overreadHeader, overreadEventValue)

        # Validate that the ResultsReceived event published to spirometry_result topic has the expected properties as in the result contract
        * json event = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("spirometry_result", evaluation.evaluationId + '', "ResultsReceived", 10, 5000)) 
        
        * match event.ProductCode == 'SPIROMETRY'
        * match event.EvaluationId == evaluation.evaluationId
        * match event.PerformedDate contains dateStamp
        * match event.ReceivedDate contains dateStamp
        * match event.IsBillable == <IsBillable>
        * match event.Determination == <determination>
        * match event.Results.Fev1OverFvc == <overread_ratio>
        * match event.Results.SessionGrade  == 'D'
        * match event.Results.Fvc == 100
        * match event.Results.FvcNormality == 'N' 
        * match event.Results.Fev1 == 30   
        * match event.Results.Fev1Normality == 'A' 
        * match event.Results.HasSmokedTobacco == null
        * match event.Results.TotalYearsSmoking == null
        * match event.Results.ProducesSputumWithCough == null
        * match event.Results.CoughMucusOccurrenceFrequency == null
        * match event.Results.HadWheezingPast12mo == null
        * match event.Results.GetsShortnessOfBreathAtRest == null
        * match event.Results.GetsShortnessOfBreathWithMildExertion == null
        * match event.Results.NoisyChestOccurrenceFrequency == null
        * match event.Results.ShortnessOfBreathPhysicalActivityOccurrenceFrequency == null
        * match event.Results.LungFunctionScore == 19
        * match event.Results.Copd == null

        # Validate that the ResultsReceived event published to spirometry_status topic has the expected properties as in the result contract
        * json resultsEvent = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("spirometry_status", evaluation.evaluationId + '', "ResultsReceived", 10, 5000)) 
        
        * match resultsEvent.ProductCode == 'SPIROMETRY'
        * match resultsEvent.EvaluationId == evaluation.evaluationId
        * match resultsEvent.MemberPlanId == appointment.memberPlanId
        * match resultsEvent.ProviderId.toString() == appointment.providerId
        * match resultsEvent.CreateDate contains dateStamp
        * match resultsEvent.ReceivedDate contains dateStamp

        # Validate Exam Status Update in database
        * def examStatus = SpiroDb().getExamStatusByEvaluationId(evaluation.evaluationId)
        * match examStatus[*].Name contains 'Spirometry Exam Performed' && 'Overread Processed' && 'Results Received'
        
        # Validate NeedFlags value in EvaluationSaga tb
        * eval sleep(10000)  
        * def evalSaga = SpiroDb().getEvalSagaByEvaluationId(evaluation.evaluationId)
        * json sagaData = JSON.parse(evalSaga[0].Data)
        * match sagaData.NeedsFlag ==  false
        * match sagaData.NeedsOverread ==  true

        Examples:
        | ObstructionPerOverread| determination|overread_ratio|IsBillable|
        |          "YES"        |      "A"     |     0.6      |   true   |
        |          "NO"         |      "N"     |     0.7      |   true   |
        |      "INCONCLUSIVE"   |      "U"     |     1.25     |   false  |

    @TestCaseKey=ANC-T509
    Scenario Outline: Spirometry Process Manager Publishes Overread Results to Kafka- when Dx:COPD is asserted
        * set evaluation.answers =
            """
            [
                {
                    "AnswerId": 50919,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": 1
                },
                {
                    "AnswerId": 50940,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "D"
                },
                {
                    "AnswerId": 50999,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": 100
                },
                {
                    "AnswerId": 51000,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": 30
                },
                {
                    "AnswerId": 51002,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": 0.3
                },
                {
                    "AnswerId": 50944,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "Yes" 
                },        
                {
                    "AnswerId": 50948,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue":  "No"   
                },
                {
                    "AnswerId": 50952,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "Unknown"
                },
                {
                    "AnswerId": 22034,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "#(timestamp)"
                },
                {
                    "AnswerId": 50993,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "YES"
                },
                {
                    "AnswerId": 51420,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue":  19
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
        
        # Publish the Overread event to the overread_spirometry topic
        * string overrreadEventKey = appointment.appointmentId
        * def OverreadId = DataGen().uuid()

        * string overreadHeader = {'Type': 'OverreadProcessed'}
        * string overreadEventValue = {'OverreadId': '#(OverreadId)','MemberId':'','AppointmentId': '#(appointment.appointmentId)','SessionId': '#(OverreadId)','PerformedDateTime': '#(timestamp)','OverreadDateTime': '#(timestamp)','BestTestId': '#(OverreadId)','BestFvcTestId': '#(OverreadId)','BestFvcTestComment': 'TestComment','BestFev1TestId': '#(OverreadId)','BestFev1TestComment': 'TestComment','BestPefTestId': '#(OverreadId)','BestPefTestComment': 'TestComment','Comment': 'TestComment','Fev1FvcRatio':<overread_ratio>,'OverreadBy': 'JohnDoe','ObstructionPerOverread':<ObstructionPerOverread>,'ReceivedDateTime': '#(timestamp)'}
        * kafkaProducerHelper.send("overread_spirometry", overrreadEventKey, overreadHeader, overreadEventValue)

        # Validate that the ResultsReceived event published to spirometry_result topic has the expected properties as in the result contract
        * json event = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("spirometry_result", evaluation.evaluationId + '', "ResultsReceived", 10, 5000)) 
        
        * match event.ProductCode == 'SPIROMETRY'
        * match event.EvaluationId == evaluation.evaluationId
        * match event.PerformedDate contains dateStamp
        * match event.ReceivedDate contains dateStamp
        * match event.IsBillable == <IsBillable>
        * match event.Determination == <determination>
        * match event.Results.Fev1OverFvc == <overread_ratio>
        * match event.Results.SessionGrade  == 'D'
        * match event.Results.Fvc == 100
        * match event.Results.FvcNormality == 'N' 
        * match event.Results.Fev1 == 30   
        * match event.Results.Fev1Normality == 'A' 
        * match event.Results.HasSmokedTobacco == null
        * match event.Results.TotalYearsSmoking == null
        * match event.Results.ProducesSputumWithCough == null
        * match event.Results.CoughMucusOccurrenceFrequency == null
        * match event.Results.HadWheezingPast12mo == null
        * match event.Results.GetsShortnessOfBreathAtRest == null
        * match event.Results.GetsShortnessOfBreathWithMildExertion == null
        * match event.Results.NoisyChestOccurrenceFrequency == null
        * match event.Results.ShortnessOfBreathPhysicalActivityOccurrenceFrequency == null
        * match event.Results.LungFunctionScore == 19
        * match event.Results.Copd == true

        # Validate that the ResultsReceived event published to spirometry_status topic has the expected properties as in the result contract
        * json resultsEvent = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("spirometry_status", evaluation.evaluationId + '', "ResultsReceived", 10, 5000)) 
        
        * match resultsEvent.ProductCode == 'SPIROMETRY'
        * match resultsEvent.EvaluationId == evaluation.evaluationId
        * match resultsEvent.MemberPlanId == appointment.memberPlanId
        * match resultsEvent.ProviderId.toString() == appointment.providerId
        * match resultsEvent.CreateDate contains dateStamp
        * match resultsEvent.ReceivedDate contains dateStamp

        # Validate Exam Status Update in database
        * def examStatus = SpiroDb().getExamStatusByEvaluationId(evaluation.evaluationId)
        * match examStatus[*].Name contains 'Spirometry Exam Performed' && 'Overread Processed' && 'Results Received'
        
        #Validate NeedFlags value in EvaluationSaga tb
        * eval sleep(10000)  
        * def evalSaga = SpiroDb().getEvalSagaByEvaluationId(evaluation.evaluationId)
        * json sagaData = JSON.parse(evalSaga[0].Data)
        * match sagaData.NeedsFlag ==  false
        * match sagaData.NeedsOverread ==  true

        Examples:
        | ObstructionPerOverread| determination|overread_ratio|IsBillable|
        |          "YES"        |      "A"     |     0.6      |   true   |
        |          "NO"         |      "N"     |     0.7      |   true   |
        |      "INCONCLUSIVE"   |      "U"     |     1.00     |   false  |
