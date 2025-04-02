@hba1cpoc
@envnot=prod
Feature: HBA1CPOC Normality

    Background:
        * eval if (env == 'prod') karate.abort();
        * def DataGen = function() { var DataGen = Java.type('helpers.data.DataGen'); return new DataGen(); }
        * def Hba1cPOCDb = function() { var Hba1cpocDb = Java.type("helpers.database.hba1cpoc.Hba1cpocDb"); return new Hba1cpocDb(); }
        * def timestamp = DataGen().isoTimestamp()
        * def expirationDate = DataGen().isoDateStamp(30)
        * def dateStamp = DataGen().isoDateStamp()

        * def memberDetails = karate.call('classpath:helpers/member/createMember.js')
        * def appointment = karate.call('classpath:helpers/appointment/createAppointment.feature', { products: ['HHRA', 'HBA1CPOC'] }).response
        * def evaluation = karate.call('classpath:helpers/eval/startEval.feature').response
        
    @TestCaseKey=ANC-T327
    Scenario Outline:  HBA1CPOC Normality Validation
        * set evaluation.answers =
            """
            [
                {
                    'AnswerId': 33070,
                    'AnsweredDateTime': '#(timestamp)',
                    'AnswerValue': 1
                },
                {
                    'AnswerId': 33088,
                    'AnsweredDateTime': '#(timestamp)',
                    'AnswerValue': <answer_value>
                },
                {
                    'AnswerId': 33264,
                    'AnsweredDateTime': '#(timestamp)',
                    'AnswerValue': '#(expirationDate)'
                },
                {
                    'AnswerId': 22034,
                    'AnsweredDateTime': '#(timestamp)',
                    'AnswerValue': '#(timestamp)'
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

        * def result = Hba1cPOCDb().getResultsByEvaluationId(evaluation.evaluationId)[0]
        * match result.A1CPercent == <answer_value>
        * match result.NormalityIndicator == <normality>
        * match result.ExpirationDate.toString() == expirationDate
        * match result.DateOfBirth.toString() == memberDetails.dateOfBirth

        # Validate the entry using EvaluationId in HBA1CPOC & HBA1CPOCStatus tables
        * def examStatusResults = Hba1cPOCDb().getExamStatusByEvaluationId(evaluation.evaluationId)
        # Status 1 = HBA1CPOCPerformed 
        * match examStatusResults[*].HBA1CPOCStatusCodeId contains 1

        Examples:
            | answer_value | normality |
            | '3.9'        | 'A'       |
            | '4'          | 'N'       |
            | '6.9'        | 'N'       |
            | '7'          | 'A'       |
            | 'abc'        | 'U'       |
            | '<4'         | 'A'       |
            | '>13'        | 'A'       |
            | '<4%'        | 'A'       |
            | '<4.0'       | 'A'       |
            | '>13.0'      | 'A'       |