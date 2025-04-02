@spirometry
@envnot=prod
Feature: Spirometry Normality Determination Tests

    Background:
        * eval if (env == 'prod') karate.abort();
        * def DataGen = function() { var DataGen = Java.type('helpers.data.DataGen'); return new DataGen(); }
        * def SpiroDb = function() { var SpiroDb = Java.type('helpers.database.spirometry.SpirometryDb'); return new SpiroDb(); }
        * def timestamp = DataGen().isoTimestamp()
        * def expirationDate = DataGen().isoDateStamp(30)
        * def dateStamp = DataGen().isoDateStamp()
        
        * def memberDetails = karate.call("classpath:helpers/member/createMember.js")
        * def appointment = karate.call('classpath:helpers/appointment/createAppointment.feature', { products: ['HHRA', 'SPIROMETRY'] }).response
        * def evaluation = karate.call('classpath:helpers/eval/startEval.feature').response
        
    @TestCaseKey=ANC-T711
    Scenario Outline: Spirometry Normality Validation
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
                    "AnswerValue": <fvc>
                },
                {
                    "AnswerId": 51000,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": <fev1>
                },
                {
                    "AnswerId": 51002,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": <fev1_fvc>
                },
                {
                    "AnswerId": 51405,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "Never"
                },        
                {
                    "AnswerId": 51410,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "Never"
                },
                {
                    "AnswerId": 51415,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "Never"
                },
                {
                    "AnswerId": 51420,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": 1
                },
                {
                    "AnswerId": 22034,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "#(dateStamp)"
                },
                {
                    "AnswerId": <Hx_of_COPD_AID>,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": <Hx_of_COPD_answer_value>
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

            * def result = SpiroDb().getResultsByEvaluationId(evaluation.evaluationId)[0]
            * match result.Normality == <normality>
            * match result.HasHistoryOfCopd == <HasHistoryOfCopd>  
            
        Examples:
        | session_grade_id | session_grade_value | fvc | fev1 | fev1_fvc | normality  |Hx_of_COPD_AID|Hx_of_COPD_answer_value                       |HasHistoryOfCopd|
        | 50938            | "B"                 | 70  | 70   | 0.65     | "Abnormal" |29614         |"Chronic obstructive pulmonary disease (COPD)"|true|
        | 50937            | "A"                 | 80  | 80   | 0.7      | "Normal"   |21925         |"Chronic obstructive pulmonary disease (COPD)"|true|
        | 50939            | "C"                 | 80  | 80   | 0.85     | "Normal"   |52027         |"Chronic obstructive pulmonary disease (COPD)"|true|  
        | 50939            | "C"                 | 80  | 80   | 0.85     | "Normal"   |52027         |"Hypertension"                                |false|  
        | 50939            | "C"                 | 80  | 80   | 0.85     | "Normal"   |29614         |"Hypertension"                                |false|  
        | 50939            | "C"                 | 80  | 80   | 0.85     | "Normal"   |29614         |"Diabetes with neuropathy"                    |false|  
        | 50939            | "C"                 | 80  | 80   | 0.85     | "Normal"   |29614         |"Constipation"                                |false|  
        | 50939            | "C"                 | 80  | 80   | 0.85     | "Normal"   |29614         |"Heart failure"                               |false|  
        | 50939            | "C"                 | 80  | 80   | 0.85     | "Normal"   |29614         |"Orthostatic hypotension"                     |false|  
        | 50939            | "C"                 | 80  | 80   | 0.85     | "Normal"   |29614         |"Seasonal allergic rhinitis"                  |false|
        | 50939            | "C"                 | 80  | 80   | 0.85     | "Normal"   |21925         |"Hypertension"                                |false|  
        | 50939            | "C"                 | 80  | 80   | 0.85     | "Normal"   |21925         |"Diabetes with neuropathy"                    |false|  
        | 50939            | "C"                 | 80  | 80   | 0.85     | "Normal"   |21925         |"Constipation"                                |false|  
        | 50939            | "C"                 | 80  | 80   | 0.85     | "Normal"   |21925         |"Heart failure"                               |false|  
        | 50939            | "C"                 | 80  | 80   | 0.85     | "Normal"   |21925         |"Orthostatic hypotension"                     |false|  
        | 50939            | "C"                 | 80  | 80   | 0.85     | "Normal"   |21925         |"Seasonal allergic rhinitis"                  |false|

    @TestCaseKey=ANC-T712
    Scenario Outline: Single Session Grade Id - Spirometry Normality Validation
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
                    "AnswerValue": <fvc>
                },
                {
                    "AnswerId": 51000,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": <fev1>
                },
                {
                    "AnswerId": 51002,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": <fev1_fvc>
                },
                {
                    "AnswerId": 51405,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "Never"
                },        
                {
                    "AnswerId": 51410,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "Never"
                },
                {
                    "AnswerId": 51415,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "Never"
                },
                {
                    "AnswerId": 51420,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": 1
                },
                {
                    "AnswerId": 22034,
                    "AnsweredDateTime": "#(timestamp)",
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

            * def result = SpiroDb().getResultsByEvaluationId(evaluation.evaluationId)[0]
            * match result.Normality == <normality>
            
        Examples:
        | session_grade_id | session_grade_value | fvc | fev1 | fev1_fvc | normality      |
        | 51947            | "A"                 | 80  | 80   | 0.85     | "Normal"       |
        | 51947            | "B"                 | 80  | 80   | 0.85     | "Normal"       |
        | 51947            | "C"                 | 80  | 80   | 0.85     | "Normal"       |
        | 51947            | "D"                 | 80  | 80   | 0.85     | "Undetermined" |
        | 51947            | "E"                 | 80  | 80   | 0.85     | "Undetermined" |
        | 51947            | "F"                 | 80  | 80   | 0.85     | "Undetermined" |