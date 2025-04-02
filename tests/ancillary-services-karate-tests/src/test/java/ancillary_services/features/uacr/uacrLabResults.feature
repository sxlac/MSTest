@uacr
@envnot=prod
Feature: uACR Lab Performed Results Tests

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

    @TestCaseKey=ANC-T788
    Scenario Outline: Verify that an event is produced to uacr_results topic with normality <expected_normality_indicator>
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
        
        # Validate the DB table for LabResults
        * def labResults = UacrDb().getLabResultsByEvaluationId(evaluation.evaluationId)[0]
        * match labResults.EvaluationId == evaluation.evaluationId
        * match DataGen().RemoveMilliSeconds(ProperDateOfService) == DataGen().getUtcDateTimeString(labResults.ReceivedDate.toString())
        * match labResults.UacrResult == <CMP_uACRResult>
        * match labResults.ResultColor == <CMP_uacrResultColor>
        * match labResults.Normality == <expected_normality>
        * match labResults.NormalityCode == <expected_normality_indicator>
        * match labResults.ResultDescription == 'Performed'
        
        # Validate the DB table for Exam status
        * def result = UacrDb().queryExamWithStatusList(evaluation.evaluationId,["Order Requested", "Exam Performed", "Lab Results Received"])
        * match result[*].ExamStatusCodeId contains [1, 14, 6]

        Examples:
        | CMP_uACRResult | CMP_CreatinineResult | expected_normality_indicator | CMP_uacrResultColor |is_billable | expected_normality |
        | 29             | 1.07                 | 'N'                          | 'Green'             |true        | 'Normal'           |
        | 30             | 1.27                 | 'A'                          | 'Red'               |true        | 'Abnormal'         |
        | 31             | 1.27                 | 'A'                          | 'Red'               |true        | 'Abnormal'         |
        | 0              | 1.27                 | 'U'                          | 'Grey'              |false       | 'Undetermined'     |

        