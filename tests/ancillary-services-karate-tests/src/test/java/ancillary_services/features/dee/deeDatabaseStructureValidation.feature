@dee
Feature: Dee Database Structure Validation Check

    Background:
        * def DeeDb = function() { var DeeDb = Java.type("helpers.database.dee.DeeDb"); return new DeeDb(); }

    @TestCaseKey=ANC-T605
    Scenario: Dee ExamStatusCodes added
        * def examStatusCodes = DeeDb().getExamStatusCodes()
        * assert examStatusCodes[0].ExamStatusCodeId == 1
        * assert examStatusCodes[0].Name == 'Exam Created'
        * assert examStatusCodes[1].ExamStatusCodeId == 2
        * assert examStatusCodes[1].Name == 'IRIS Awaiting Interpretation'
        * assert examStatusCodes[2].ExamStatusCodeId == 3
        * assert examStatusCodes[2].Name == 'IRIS Interpreted'
        * assert examStatusCodes[3].ExamStatusCodeId == 4
        * assert examStatusCodes[3].Name == 'Result Data Downloaded'
        * assert examStatusCodes[4].ExamStatusCodeId == 5
        * assert examStatusCodes[4].Name == 'PDF Data Downloaded'
        * assert examStatusCodes[5].ExamStatusCodeId == 6
        * assert examStatusCodes[5].Name == 'Sent To Billing'
        * assert examStatusCodes[6].ExamStatusCodeId == 7
        * assert examStatusCodes[6].Name == 'No DEE Images Taken'
        * assert examStatusCodes[7].ExamStatusCodeId == 8
        * assert examStatusCodes[7].Name == 'IRIS Image Received'
        * assert examStatusCodes[8].ExamStatusCodeId == 9
        * assert examStatusCodes[8].Name == 'Gradable'
        * assert examStatusCodes[9].ExamStatusCodeId == 10
        * assert examStatusCodes[9].Name == 'Not Gradable'
        * assert examStatusCodes[10].ExamStatusCodeId == 11
        * assert examStatusCodes[10].Name == 'DEE Images Found'
        * assert examStatusCodes[11].ExamStatusCodeId == 12
        * assert examStatusCodes[11].Name == 'IRIS Exam Created'
        * assert examStatusCodes[12].ExamStatusCodeId == 13
        * assert examStatusCodes[12].Name == 'IRIS Result Downloaded'
        * assert examStatusCodes[13].ExamStatusCodeId == 14
        * assert examStatusCodes[13].Name == 'PCP Letter Sent'
        * assert examStatusCodes[14].ExamStatusCodeId == 15
        * assert examStatusCodes[14].Name == 'No PCP Found'
        * assert examStatusCodes[15].ExamStatusCodeId == 16
        * assert examStatusCodes[15].Name == 'Member Letter Sent'
        * assert examStatusCodes[16].ExamStatusCodeId == 17
        * assert examStatusCodes[16].Name == 'Sent To Provider Pay'
        * assert examStatusCodes[17].ExamStatusCodeId == 18
        * assert examStatusCodes[17].Name == 'DEE Performed'
        * assert examStatusCodes[18].ExamStatusCodeId == 19
        * assert examStatusCodes[18].Name == 'DEE Not Performed'
        * assert examStatusCodes[19].ExamStatusCodeId == 20
        * assert examStatusCodes[19].Name == 'Billable Event Recieved'
        * assert examStatusCodes[20].ExamStatusCodeId == 21
        * assert examStatusCodes[20].Name == 'DEE Incomplete'
        * assert examStatusCodes[21].ExamStatusCodeId == 22
        * assert examStatusCodes[21].Name == 'Bill Request Not Sent'
        * assert examStatusCodes[22].ExamStatusCodeId == 23
        * assert examStatusCodes[22].Name == 'ProviderPayableEventReceived'
        * assert examStatusCodes[23].ExamStatusCodeId == 24
        * assert examStatusCodes[23].Name == 'ProviderNonPayableEventReceived'
        * assert examStatusCodes[24].ExamStatusCodeId == 25
        * assert examStatusCodes[24].Name == 'ProviderPayRequestSent'
        * assert examStatusCodes[25].ExamStatusCodeId == 26
        * assert examStatusCodes[25].Name == 'CdiPassedReceived'
        * assert examStatusCodes[26].ExamStatusCodeId == 27
        * assert examStatusCodes[26].Name == 'CdiFailedWithPayReceived'
        * assert examStatusCodes[27].ExamStatusCodeId == 28
        * assert examStatusCodes[27].Name == 'CdiFailedWithoutPayReceived'

    @TestCaseKey=ANC-T664
    Scenario: Dee LateralityCodes added
        * def lateralityCodes = DeeDb().getLateralityCodes()
        * assert lateralityCodes[0].LateralityCodeId == 1
        * assert lateralityCodes[0].Name == 'OD'
        * assert lateralityCodes[0].Description == 'Right, Oculu'
        * assert lateralityCodes[1].LateralityCodeId == 2
        * assert lateralityCodes[1].Name == 'OS'
        * assert lateralityCodes[1].Description == 'Left, Oculus Sinster'
        * assert lateralityCodes[2].LateralityCodeId == 3
        * assert lateralityCodes[2].Name == 'OU'
        * assert lateralityCodes[2].Description == 'Both, Oculus Uterque'
        * assert lateralityCodes[3].LateralityCodeId == 4
        * assert lateralityCodes[3].Name == 'Unknown'
        * assert lateralityCodes[3].Description == 'Unknown'

    @TestCaseKey=ANC-T664
    Scenario: Dee NotPerformedReasons added
        * def notPerformedReasons = DeeDb().getNotPerformedReasons()
        * assert notPerformedReasons[0].NotPerformedReasonId == 1
        * assert notPerformedReasons[0].AnswerId == '30943'
        * assert notPerformedReasons[0].Reason == 'Member recently completed'
        * assert notPerformedReasons[1].NotPerformedReasonId == 2
        * assert notPerformedReasons[1].AnswerId == '30944'
        * assert notPerformedReasons[1].Reason == 'Scheduled to complete'
        * assert notPerformedReasons[2].NotPerformedReasonId == 3
        * assert notPerformedReasons[2].AnswerId == '30945'
        * assert notPerformedReasons[2].Reason == 'Member apprehension'
        * assert notPerformedReasons[3].NotPerformedReasonId == 4
        * assert notPerformedReasons[3].AnswerId == '30946'
        * assert notPerformedReasons[3].Reason == 'Not interested'
        * assert notPerformedReasons[4].NotPerformedReasonId == 5
        * assert notPerformedReasons[4].AnswerId == '30947'
        * assert notPerformedReasons[4].Reason == 'Other'
        * assert notPerformedReasons[5].NotPerformedReasonId == 6
        * assert notPerformedReasons[5].AnswerId == '30950'
        * assert notPerformedReasons[5].Reason == 'Technical issue'
        * assert notPerformedReasons[6].NotPerformedReasonId == 7
        * assert notPerformedReasons[6].AnswerId == '30951'
        * assert notPerformedReasons[6].Reason == 'Environmental issue'
        * assert notPerformedReasons[7].NotPerformedReasonId == 8
        * assert notPerformedReasons[7].AnswerId == '30952'
        * assert notPerformedReasons[7].Reason == 'No supplies or equipment'
        * assert notPerformedReasons[8].NotPerformedReasonId == 9
        * assert notPerformedReasons[8].AnswerId == '30953'
        * assert notPerformedReasons[8].Reason == 'Insufficient training'
        * assert notPerformedReasons[9].NotPerformedReasonId == 10
        * assert notPerformedReasons[9].AnswerId == '50914'
        * assert notPerformedReasons[9].Reason == 'Member physically unable'

    @TestCaseKey=ANC-T665
    Scenario: Dee Configuration table added and accessible
        * json configurationTable = DeeDb().checkTableSchema('Configuration')
        * assert configurationTable[0].column_name == 'ConfigurationId'
        * assert configurationTable[0].data_type == 'integer'
        * assert configurationTable[0].is_nullable == 'NO'

        * assert configurationTable[1].column_name == 'ConfigurationName'
        * assert configurationTable[1].data_type == 'character varying'
        * assert configurationTable[1].is_nullable == 'NO'
        * assert configurationTable[1].character_maximum_length == 256

        * assert configurationTable[2].column_name == 'ConfigurationValue'
        * assert configurationTable[2].data_type == 'character varying'
        * assert configurationTable[2].is_nullable == 'NO'
        * assert configurationTable[2].character_maximum_length == 256

        * assert configurationTable[3].column_name == 'LastUpdated'
        * assert configurationTable[3].data_type == 'timestamp with time zone'
        * assert configurationTable[3].is_nullable == 'NO'

        * json configurationTableRowCount = DeeDb().checkTableRowCount("Configuration")
        * assert configurationTableRowCount[0].count != null
        * assert configurationTableRowCount[0].count >= 1

        * json configurationTablePermissions = DeeDb().checkTablePermissions('Configuration')
        * assert configurationTablePermissions[0].privilege_type == 'INSERT'
        * assert configurationTablePermissions[2].privilege_type == 'UPDATE'

    @TestCaseKey=ANC-T665
    Scenario: Dee DEEBilling table added and accessible
        * json deeBillingTable = DeeDb().checkTableSchema('DEEBilling')
        * assert deeBillingTable[0].column_name == 'Id'
        * assert deeBillingTable[0].data_type == 'integer'
        * assert deeBillingTable[0].is_nullable == 'NO'

        * assert deeBillingTable[1].column_name == 'BillId'
        * assert deeBillingTable[1].data_type == 'character varying'
        * assert deeBillingTable[1].is_nullable == 'YES'
        * assert deeBillingTable[1].character_maximum_length == 50

        * assert deeBillingTable[2].column_name == 'ExamId'
        * assert deeBillingTable[2].data_type == 'integer'
        * assert deeBillingTable[2].is_nullable == 'NO'

        * assert deeBillingTable[3].column_name == 'CreatedDateTime'
        * assert deeBillingTable[3].data_type == 'timestamp with time zone'
        * assert deeBillingTable[3].is_nullable == 'NO'

        * json deeBillingTableRowCount = DeeDb().checkTableRowCount("DEEBilling")
        * assert deeBillingTableRowCount[0].count != null
        * assert deeBillingTableRowCount[0].count >= 1

        * json deeBillingTablePermissions = DeeDb().checkTablePermissions('DEEBilling')
        * assert deeBillingTablePermissions[0].privilege_type == 'INSERT'
        * assert deeBillingTablePermissions[2].privilege_type == 'UPDATE'

    @TestCaseKey=ANC-T665
    Scenario: Dee DeeNotPerformed table added and accessible
        * json deeNotPerformedTable = DeeDb().checkTableSchema('DeeNotPerformed')
        * assert deeNotPerformedTable[0].column_name == 'DeeNotPerformedId'
        * assert deeNotPerformedTable[0].data_type == 'integer'
        * assert deeNotPerformedTable[0].is_nullable == 'NO'

        * assert deeNotPerformedTable[1].column_name == 'ExamId'
        * assert deeNotPerformedTable[1].data_type == 'integer'
        * assert deeNotPerformedTable[1].is_nullable == 'NO'

        * assert deeNotPerformedTable[2].column_name == 'NotPerformedReasonId'
        * assert deeNotPerformedTable[2].data_type == 'smallint'
        * assert deeNotPerformedTable[2].is_nullable == 'NO'

        * assert deeNotPerformedTable[3].column_name == 'CreatedDateTime'
        * assert deeNotPerformedTable[3].data_type == 'timestamp with time zone'
        * assert deeNotPerformedTable[3].is_nullable == 'NO'

        * assert deeNotPerformedTable[4].column_name == 'Notes'
        * assert deeNotPerformedTable[4].data_type == 'text'
        * assert deeNotPerformedTable[4].is_nullable == 'YES'

        * json deeNotPerformedTableRowCount = DeeDb().checkTableRowCount("DeeNotPerformed")
        * assert deeNotPerformedTableRowCount[0].count != null
        * assert deeNotPerformedTableRowCount[0].count >= 1

        * json deeNotPerformedTablePermissions = DeeDb().checkTablePermissions('DeeNotPerformed')
        * assert deeNotPerformedTablePermissions[0].privilege_type == 'INSERT'
        * assert deeNotPerformedTablePermissions[2].privilege_type == 'UPDATE'

    @TestCaseKey=ANC-T665
    Scenario: Dee Exam table added and accessible
        * json examTable = DeeDb().checkTableSchema('Exam')
        * assert examTable[0].column_name == 'ExamId'
        * assert examTable[0].data_type == 'integer'
        * assert examTable[0].is_nullable == 'NO'

        * assert examTable[1].column_name == 'DeeExamId'
        * assert examTable[1].data_type == 'integer'
        * assert examTable[1].is_nullable == 'YES'

        * assert examTable[2].column_name == 'EvaluationId'
        * assert examTable[2].data_type == 'bigint'
        * assert examTable[2].is_nullable == 'YES'

        * assert examTable[3].column_name == 'MemberPlanId'
        * assert examTable[3].data_type == 'bigint'
        * assert examTable[3].is_nullable == 'NO'

        * assert examTable[4].column_name == 'ProviderId'
        * assert examTable[4].data_type == 'integer'
        * assert examTable[4].is_nullable == 'NO'

        * assert examTable[5].column_name == 'DateOfService'
        * assert examTable[5].data_type == 'timestamp with time zone'
        * assert examTable[5].is_nullable == 'NO'

        * assert examTable[6].column_name == 'Gradeable'
        * assert examTable[6].data_type == 'boolean'
        * assert examTable[6].is_nullable == 'YES'

        * assert examTable[7].column_name == 'CreatedDateTime'
        * assert examTable[7].data_type == 'timestamp with time zone'
        * assert examTable[7].is_nullable == 'NO'

        * assert examTable[8].column_name == 'State'
        * assert examTable[8].data_type == 'character varying'
        * assert examTable[8].is_nullable == 'YES'
        * assert examTable[8].character_maximum_length == 5

        * assert examTable[9].column_name == 'RequestId'
        * assert examTable[9].data_type == 'uuid'
        * assert examTable[9].is_nullable == 'YES'

        * assert examTable[10].column_name == 'ClientId'
        * assert examTable[10].data_type == 'integer'
        * assert examTable[10].is_nullable == 'YES'

        * assert examTable[11].column_name == 'ReceivedDateTime'
        * assert examTable[11].data_type == 'timestamp with time zone'
        * assert examTable[11].is_nullable == 'YES'

        * assert examTable[12].column_name == 'ExamLocalId'
        * assert examTable[12].data_type == 'character varying'
        * assert examTable[12].is_nullable == 'YES'
        * assert examTable[12].character_maximum_length == 50

        * json examTableRowCount = DeeDb().checkTableRowCount("Exam")
        * assert examTableRowCount[0].count != null
        * assert examTableRowCount[0].count >= 1

        * json examTablePermissions = DeeDb().checkTablePermissions('Exam')
        * assert examTablePermissions[0].privilege_type == 'INSERT'
        * assert examTablePermissions[2].privilege_type == 'UPDATE'

    @TestCaseKey=ANC-T665
    Scenario: Dee ExamDiagnosis table added and accessible
        * json examDiagnosisTable = DeeDb().checkTableSchema('ExamDiagnosis')
        * assert examDiagnosisTable[0].column_name == 'ExamDiagnosisId'
        * assert examDiagnosisTable[0].data_type == 'integer'
        * assert examDiagnosisTable[0].is_nullable == 'NO'

        * assert examDiagnosisTable[1].column_name == 'ExamResultId'
        * assert examDiagnosisTable[1].data_type == 'integer'
        * assert examDiagnosisTable[1].is_nullable == 'NO'

        * assert examDiagnosisTable[2].column_name == 'Diagnosis'
        * assert examDiagnosisTable[2].data_type == 'character varying'
        * assert examDiagnosisTable[2].is_nullable == 'YES'
        * assert examDiagnosisTable[2].character_maximum_length == 50

        * json examDiagnosisTableRowCount = DeeDb().checkTableRowCount("ExamDiagnosis")
        * assert examDiagnosisTableRowCount[0].count != null
        * assert examDiagnosisTableRowCount[0].count >= 1

        * json examDiagnosisTablePermissions = DeeDb().checkTablePermissions('ExamDiagnosis')
        * assert examDiagnosisTablePermissions[0].privilege_type == 'INSERT'
        * assert examDiagnosisTablePermissions[2].privilege_type == 'UPDATE'

    @TestCaseKey=ANC-T665
    Scenario: Dee ExamFinding table added and accessible
        * json examFindingTable = DeeDb().checkTableSchema('ExamFinding')
        * assert examFindingTable[0].column_name == 'ExamFindingId'
        * assert examFindingTable[0].data_type == 'integer'
        * assert examFindingTable[0].is_nullable == 'NO'

        * assert examFindingTable[1].column_name == 'ExamResultId'
        * assert examFindingTable[1].data_type == 'integer'
        * assert examFindingTable[1].is_nullable == 'NO'

        * assert examFindingTable[2].column_name == 'LateralityCodeId'
        * assert examFindingTable[2].data_type == 'integer'
        * assert examFindingTable[2].is_nullable == 'YES'

        * assert examFindingTable[3].column_name == 'Finding'
        * assert examFindingTable[3].data_type == 'character varying'
        * assert examFindingTable[3].is_nullable == 'YES'
        * assert examFindingTable[3].character_maximum_length == 500

        * assert examFindingTable[4].column_name == 'NormalityIndicator'
        * assert examFindingTable[4].data_type == 'character varying'
        * assert examFindingTable[4].is_nullable == 'YES'
        * assert examFindingTable[4].character_maximum_length == 1

        * json examFindingTableRowCount = DeeDb().checkTableRowCount("ExamFinding")
        * assert examFindingTableRowCount[0].count != null
        * assert examFindingTableRowCount[0].count >= 1

        * json examFindingTablePermissions = DeeDb().checkTablePermissions('ExamFinding')
        * assert examFindingTablePermissions[0].privilege_type == 'INSERT'
        * assert examFindingTablePermissions[2].privilege_type == 'UPDATE'

    @TestCaseKey=ANC-T665
    Scenario: Dee ExamImage table added and accessible
        * json examImageTable = DeeDb().checkTableSchema('ExamImage')
        * assert examImageTable[0].column_name == 'ExamImageId'
        * assert examImageTable[0].data_type == 'integer'
        * assert examImageTable[0].is_nullable == 'NO'

        * assert examImageTable[1].column_name == 'ExamId'
        * assert examImageTable[1].data_type == 'integer'
        * assert examImageTable[1].is_nullable == 'NO'

        * assert examImageTable[2].column_name == 'DeeImageId'
        * assert examImageTable[2].data_type == 'integer'
        * assert examImageTable[2].is_nullable == 'YES'

        * assert examImageTable[3].column_name == 'ImageQuality'
        * assert examImageTable[3].data_type == 'character varying'
        * assert examImageTable[3].is_nullable == 'YES'
        * assert examImageTable[3].character_maximum_length == 15

        * assert examImageTable[4].column_name == 'ImageType'
        * assert examImageTable[4].data_type == 'character varying'
        * assert examImageTable[4].is_nullable == 'YES'
        * assert examImageTable[4].character_maximum_length == 15

        * assert examImageTable[5].column_name == 'LateralityCodeId'
        * assert examImageTable[5].data_type == 'integer'
        * assert examImageTable[5].is_nullable == 'YES'

        * assert examImageTable[6].column_name == 'Gradable'
        * assert examImageTable[6].data_type == 'boolean'
        * assert examImageTable[6].is_nullable == 'YES'

        * assert examImageTable[7].column_name == 'NotGradableReasons'
        * assert examImageTable[7].data_type == 'character varying'
        * assert examImageTable[7].is_nullable == 'YES'
        * assert examImageTable[7].character_maximum_length == 1000

        * assert examImageTable[8].column_name == 'ImageLocalId'
        * assert examImageTable[8].data_type == 'character varying'
        * assert examImageTable[8].is_nullable == 'YES'
        * assert examImageTable[8].character_maximum_length == 50

        * json examImageTableRowCount = DeeDb().checkTableRowCount("ExamImage")
        * assert examImageTableRowCount[0].count != null
        * assert examImageTableRowCount[0].count >= 1

        * json examImageTablePermissions = DeeDb().checkTablePermissions('ExamImage')
        * assert examImageTablePermissions[0].privilege_type == 'INSERT'
        * assert examImageTablePermissions[2].privilege_type == 'UPDATE'
    
    @TestCaseKey=ANC-T665
    Scenario: Dee ExamLateralityGrade table added and accessible
        * json examLateralityGradeTable = DeeDb().checkTableSchema('ExamLateralityGrade')
        * assert examLateralityGradeTable[0].column_name == 'ExamLateralityGradeId'
        * assert examLateralityGradeTable[0].data_type == 'integer'
        * assert examLateralityGradeTable[0].is_nullable == 'NO'

        * assert examLateralityGradeTable[1].column_name == 'ExamId'
        * assert examLateralityGradeTable[1].data_type == 'integer'
        * assert examLateralityGradeTable[1].is_nullable == 'NO'

        * assert examLateralityGradeTable[2].column_name == 'LateralityCodeId'
        * assert examLateralityGradeTable[2].data_type == 'integer'
        * assert examLateralityGradeTable[2].is_nullable == 'NO'

        * assert examLateralityGradeTable[3].column_name == 'Gradable'
        * assert examLateralityGradeTable[3].data_type == 'boolean'
        * assert examLateralityGradeTable[3].is_nullable == 'NO'

        * json examLateralityGradeTableRowCount = DeeDb().checkTableRowCount("ExamLateralityGrade")
        * assert examLateralityGradeTableRowCount[0].count != null
        * assert examLateralityGradeTableRowCount[0].count >= 1

        * json examLateralityGradeTablePermissions = DeeDb().checkTablePermissions('ExamLateralityGrade')
        * assert examLateralityGradeTablePermissions[0].privilege_type == 'INSERT'
        * assert examLateralityGradeTablePermissions[2].privilege_type == 'UPDATE'

    @TestCaseKey=ANC-T665
    Scenario: Dee ExamResult table added and accessible
        * json examResultTable = DeeDb().checkTableSchema('ExamResult')
        * assert examResultTable[0].column_name == 'ExamResultId'
        * assert examResultTable[0].data_type == 'integer'
        * assert examResultTable[0].is_nullable == 'NO'

        * assert examResultTable[1].column_name == 'ExamId'
        * assert examResultTable[1].data_type == 'integer'
        * assert examResultTable[1].is_nullable == 'NO'

        * assert examResultTable[2].column_name == 'GradableImage'
        * assert examResultTable[2].data_type == 'boolean'
        * assert examResultTable[2].is_nullable == 'NO'

        * assert examResultTable[3].column_name == 'GraderFirstName'
        * assert examResultTable[3].data_type == 'character varying'
        * assert examResultTable[3].is_nullable == 'YES'
        * assert examResultTable[3].character_maximum_length == 50

        * assert examResultTable[4].column_name == 'GraderLastName'
        * assert examResultTable[4].data_type == 'character varying'
        * assert examResultTable[4].is_nullable == 'YES'
        * assert examResultTable[4].character_maximum_length == 50

        * assert examResultTable[5].column_name == 'GraderNpi'
        * assert examResultTable[5].data_type == 'character varying'
        * assert examResultTable[5].is_nullable == 'YES'
        * assert examResultTable[5].character_maximum_length == 10

        * assert examResultTable[6].column_name == 'GraderTaxonomy'
        * assert examResultTable[6].data_type == 'character varying'
        * assert examResultTable[6].is_nullable == 'YES'
        * assert examResultTable[6].character_maximum_length == 50

        * assert examResultTable[7].column_name == 'DateSigned'
        * assert examResultTable[7].data_type == 'timestamp with time zone'
        * assert examResultTable[7].is_nullable == 'YES'

        * assert examResultTable[8].column_name == 'CarePlan'
        * assert examResultTable[8].data_type == 'character varying'
        * assert examResultTable[8].is_nullable == 'YES'
        * assert examResultTable[8].character_maximum_length == 500

        * assert examResultTable[9].column_name == 'NormalityIndicator'
        * assert examResultTable[9].data_type == 'character varying'
        * assert examResultTable[9].is_nullable == 'YES'
        * assert examResultTable[9].character_maximum_length == 1

        * assert examResultTable[10].column_name == 'LeftEyeHasPathology'
        * assert examResultTable[10].data_type == 'boolean'
        * assert examResultTable[10].is_nullable == 'YES'

        * assert examResultTable[11].column_name == 'RightEyeHasPathology'
        * assert examResultTable[11].data_type == 'boolean'
        * assert examResultTable[11].is_nullable == 'YES'

        * json examResultTableRowCount = DeeDb().checkTableRowCount("ExamResult")
        * assert examResultTableRowCount[0].count != null
        * assert examResultTableRowCount[0].count >= 1

        * json examResultTablePermissions = DeeDb().checkTablePermissions('ExamResult')
        * assert examResultTablePermissions[0].privilege_type == 'INSERT'
        * assert examResultTablePermissions[2].privilege_type == 'UPDATE'

    @TestCaseKey=ANC-T665
    Scenario: Dee ExamStatus table added and accessible
        * json examStatusTable = DeeDb().checkTableSchema('ExamStatus')
        * assert examStatusTable[0].column_name == 'ExamStatusId'
        * assert examStatusTable[0].data_type == 'integer'
        * assert examStatusTable[0].is_nullable == 'NO'

        * assert examStatusTable[1].column_name == 'ExamId'
        * assert examStatusTable[1].data_type == 'integer'
        * assert examStatusTable[1].is_nullable == 'NO'

        * assert examStatusTable[2].column_name == 'CreatedDateTime'
        * assert examStatusTable[2].data_type == 'timestamp with time zone'
        * assert examStatusTable[2].is_nullable == 'NO'

        * assert examStatusTable[3].column_name == 'ReceivedDateTime'
        * assert examStatusTable[3].data_type == 'timestamp with time zone'
        * assert examStatusTable[3].is_nullable == 'NO'

        * assert examStatusTable[4].column_name == 'ExamStatusCodeId'
        * assert examStatusTable[4].data_type == 'integer'
        * assert examStatusTable[4].is_nullable == 'YES'

        * assert examStatusTable[5].column_name == 'DeeEventId'
        * assert examStatusTable[5].data_type == 'uuid'
        * assert examStatusTable[5].is_nullable == 'YES'

        * json examStatusTableRowCount = DeeDb().checkTableRowCount("ExamStatus")
        * assert examStatusTableRowCount[0].count != null
        * assert examStatusTableRowCount[0].count >= 1

        * json examStatusTablePermissions = DeeDb().checkTablePermissions('ExamStatus')
        * assert examStatusTablePermissions[0].privilege_type == 'INSERT'
        * assert examStatusTablePermissions[2].privilege_type == 'UPDATE'

    @TestCaseKey=ANC-T665
    Scenario: Dee ExamStatusCode table added and accessible
        * json examStatusCodeTable = DeeDb().checkTableSchema('ExamStatusCode')
        * assert examStatusCodeTable[0].column_name == 'ExamStatusCodeId'
        * assert examStatusCodeTable[0].data_type == 'integer'
        * assert examStatusCodeTable[0].is_nullable == 'NO'

        * assert examStatusCodeTable[1].column_name == 'Name'
        * assert examStatusCodeTable[1].data_type == 'character varying'
        * assert examStatusCodeTable[1].character_maximum_length == 250
        * assert examStatusCodeTable[1].is_nullable == 'NO'

        * json examStatusCodeTableRowCount = DeeDb().checkTableRowCount("ExamStatusCode")
        * assert examStatusCodeTableRowCount[0].count != null
        * assert examStatusCodeTableRowCount[0].count >= 1

        * json examStatusCodeTablePermissions = DeeDb().checkTablePermissions('ExamStatusCode')
        * match each examStatusCodeTablePermissions[*].privilege_type !contains  'INSERT' || 'UPDATE'

    @TestCaseKey=ANC-T665
    Scenario: Dee LateralityCode table added and accessible
        * json lateralityCodeTable = DeeDb().checkTableSchema('LateralityCode')
        * assert lateralityCodeTable[0].column_name == 'LateralityCodeId'
        * assert lateralityCodeTable[0].data_type == 'integer'
        * assert lateralityCodeTable[0].is_nullable == 'NO'

        * assert lateralityCodeTable[1].column_name == 'Name'
        * assert lateralityCodeTable[1].data_type == 'character varying'
        * assert lateralityCodeTable[1].is_nullable == 'NO'
        * assert lateralityCodeTable[1].character_maximum_length == 12

        * assert lateralityCodeTable[2].column_name == 'Description'
        * assert lateralityCodeTable[2].data_type == 'character varying'
        * assert lateralityCodeTable[2].is_nullable == 'NO'
        * assert lateralityCodeTable[2].character_maximum_length == 256

        * json lateralityCodeTableRowCount = DeeDb().checkTableRowCount("LateralityCode")
        * assert lateralityCodeTableRowCount[0].count != null
        * assert lateralityCodeTableRowCount[0].count >= 1

        * json lateralityCodeTablePermissions = DeeDb().checkTablePermissions('LateralityCode')
        * match each lateralityCodeTablePermissions[*].privilege_type !contains  'INSERT' || 'UPDATE'

    @TestCaseKey=ANC-T665
    Scenario: Dee NonGradableReason table added and accessible
        * json nonGradableReasonTable = DeeDb().checkTableSchema('NonGradableReason')
        * assert nonGradableReasonTable[0].column_name == 'NonGradableReasonId'
        * assert nonGradableReasonTable[0].data_type == 'integer'
        * assert nonGradableReasonTable[0].is_nullable == 'NO'

        * assert nonGradableReasonTable[1].column_name == 'ExamLateralityGradeId'
        * assert nonGradableReasonTable[1].data_type == 'integer'
        * assert nonGradableReasonTable[1].is_nullable == 'NO'

        * assert nonGradableReasonTable[2].column_name == 'Reason'
        * assert nonGradableReasonTable[2].data_type == 'character varying'
        * assert nonGradableReasonTable[2].is_nullable == 'NO'
        * assert nonGradableReasonTable[2].character_maximum_length == 1000

        * json nonGradableReasonTableRowCount = DeeDb().checkTableRowCount("NonGradableReason")
        * assert nonGradableReasonTableRowCount[0].count != null
        * assert nonGradableReasonTableRowCount[0].count >= 1

        * json nonGradableReasonTablePermissions = DeeDb().checkTablePermissions('NonGradableReason')
        * assert nonGradableReasonTablePermissions[0].privilege_type == 'INSERT'
        * assert nonGradableReasonTablePermissions[2].privilege_type == 'UPDATE'

    @TestCaseKey=ANC-T665
    Scenario: Dee NotPerformedReason table added and accessible
        * json notPerformedReasonTable = DeeDb().checkTableSchema('NotPerformedReason')
        * assert notPerformedReasonTable[0].column_name == 'NotPerformedReasonId'
        * assert notPerformedReasonTable[0].data_type == 'integer'
        * assert notPerformedReasonTable[0].is_nullable == 'NO'

        * assert notPerformedReasonTable[1].column_name == 'AnswerId'
        * assert notPerformedReasonTable[1].data_type == 'integer'
        * assert notPerformedReasonTable[1].is_nullable == 'NO'

        * assert notPerformedReasonTable[2].column_name == 'Reason'
        * assert notPerformedReasonTable[2].data_type == 'character varying'
        * assert notPerformedReasonTable[2].is_nullable == 'NO'
        * assert notPerformedReasonTable[2].character_maximum_length == 256

        * json notPerformedReasonTableRowCount = DeeDb().checkTableRowCount("NotPerformedReason")
        * assert notPerformedReasonTableRowCount[0].count != null
        * assert notPerformedReasonTableRowCount[0].count >= 1

        * json notPerformedReasonTablePermissions = DeeDb().checkTablePermissions('NotPerformedReason')
        * match each notPerformedReasonTablePermissions[*].privilege_type !contains  'INSERT' || 'UPDATE'

    @TestCaseKey=ANC-T665
    Scenario: Dee PDFToClient table added and accessible
        * json pdfToClientTable = DeeDb().checkTableSchema('PDFToClient')
        * assert pdfToClientTable[0].column_name == 'PDFDeliverId'
        * assert pdfToClientTable[0].data_type == 'integer'
        * assert pdfToClientTable[0].is_nullable == 'NO'

        * assert pdfToClientTable[1].column_name == 'EventId'
        * assert pdfToClientTable[1].data_type == 'character varying'
        * assert pdfToClientTable[1].is_nullable == 'YES'
        * assert pdfToClientTable[1].character_maximum_length == 40

        * assert pdfToClientTable[2].column_name == 'EvaluationId'
        * assert pdfToClientTable[2].data_type == 'bigint'
        * assert pdfToClientTable[2].is_nullable == 'NO'

        * assert pdfToClientTable[3].column_name == 'DeliveryDateTime'
        * assert pdfToClientTable[3].data_type == 'timestamp with time zone'
        * assert pdfToClientTable[3].is_nullable == 'NO'

        * assert pdfToClientTable[4].column_name == 'DeliveryCreatedDateTime'
        * assert pdfToClientTable[4].data_type == 'timestamp with time zone'
        * assert pdfToClientTable[4].is_nullable == 'NO'

        * assert pdfToClientTable[5].column_name == 'BatchId'
        * assert pdfToClientTable[5].data_type == 'bigint'
        * assert pdfToClientTable[5].is_nullable == 'NO'

        * assert pdfToClientTable[6].column_name == 'BatchName'
        * assert pdfToClientTable[6].data_type == 'character varying'
        * assert pdfToClientTable[6].is_nullable == 'YES'
        * assert pdfToClientTable[6].character_maximum_length == 200

        * assert pdfToClientTable[7].column_name == 'ExamId'
        * assert pdfToClientTable[7].data_type == 'integer'
        * assert pdfToClientTable[7].is_nullable == 'NO'

        * assert pdfToClientTable[8].column_name == 'CreatedDateTime'
        * assert pdfToClientTable[8].data_type == 'timestamp with time zone'
        * assert pdfToClientTable[8].is_nullable == 'NO'

        * json pdfToClientTableRowCount = DeeDb().checkTableRowCount("PDFToClient")
        * assert pdfToClientTableRowCount[0].count != null
        * assert pdfToClientTableRowCount[0].count >= 1

        * json pdfToClientTablePermissions = DeeDb().checkTablePermissions('PDFToClient')
        * assert pdfToClientTablePermissions[0].privilege_type == 'INSERT'
        * assert pdfToClientTablePermissions[2].privilege_type == 'UPDATE'

    @TestCaseKey=ANC-T604
    Scenario: Dee ProviderPay table added and accessible
        * json providerPayTable = DeeDb().checkTableSchema('ProviderPay')
        * assert providerPayTable[0].column_name == 'Id'
        * assert providerPayTable[0].data_type == 'integer'
        * assert providerPayTable[0].is_nullable == 'NO'

        * assert providerPayTable[1].column_name == 'PaymentId'
        * assert providerPayTable[1].data_type == 'character varying'
        * assert providerPayTable[1].is_nullable == 'YES'
        * assert providerPayTable[1].character_maximum_length == 50

        * assert providerPayTable[2].column_name == 'ExamId'
        * assert providerPayTable[2].data_type == 'integer'
        * assert providerPayTable[2].is_nullable == 'NO'

        * assert providerPayTable[3].column_name == 'CreatedDateTime'
        * assert providerPayTable[3].data_type == 'timestamp with time zone'
        * assert providerPayTable[3].is_nullable == 'NO'

        * json providerPayTableRowCount = DeeDb().checkTableRowCount("ProviderPay")
        * assert providerPayTableRowCount[0].count != null
        * assert providerPayTableRowCount[0].count >= 1

        * json providerPayTablePermissions = DeeDb().checkTablePermissions('ProviderPay')
        * assert providerPayTablePermissions[0].privilege_type == 'INSERT'
        * assert providerPayTablePermissions[2].privilege_type == 'UPDATE'

    @TestCaseKey=ANC-T665
    Scenario: Dee Produced table added and accessible
        * json producedTablePermissions = DeeDb().checkTablePermissions('Produced')
        * assert producedTablePermissions[0].privilege_type == 'DELETE'
        * assert producedTablePermissions[1].privilege_type == 'INSERT'
        * assert producedTablePermissions[2].privilege_type == 'REFERENCES'
        * assert producedTablePermissions[3].privilege_type == 'SELECT'
        * assert producedTablePermissions[4].privilege_type == 'TRIGGER'
        * assert producedTablePermissions[5].privilege_type == 'TRUNCATE'
        * assert producedTablePermissions[6].privilege_type == 'UPDATE'