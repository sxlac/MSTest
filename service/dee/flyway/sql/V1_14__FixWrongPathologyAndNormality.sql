/* Replaced when SQL Server Database was migrated to Azure Database for PostgreSQL. */

/*--To fix Normality Indicator
create table #TempWrongNormalityResultIds
(
    ExamResultId int
)

Insert into #TempWrongNormalityResultIds
Select ExamResultId from ExamResult where ExamResultId not in (Select ExamResultId from ExamFinding) and  NormalityIndicator = 'A'

Update ExamResult set ExamResult.NormalityIndicator = 'U'  From ExamResult
inner join #TempWrongNormalityResultIds on ExamResult.ExamResultId = #TempWrongNormalityResultIds.ExamResultId


If(OBJECT_ID('tempdb..#TempWrongNormalityResultIds') Is Not Null)
Begin
    Drop Table #TempWrongNormalityResultIds
End

--To fix LeftEyeHasPathology and RighEyeHasPathology
create table #TempWrongPathologyResultIds
(
    ExamResultId int
)

Insert into #TempWrongPathologyResultIds
Select ExamResultId from ExamResult where ExamResultId not in (Select ExamResultId from ExamFinding) and  LeftEyeHasPathology = 0 and RightEyeHasPathology = 0

Update ExamResult set ExamResult.LeftEyeHasPathology = NULL, ExamResult.RightEyeHasPathology = NULL From ExamResult
inner join #TempWrongPathologyResultIds on ExamResult.ExamResultId = #TempWrongPathologyResultIds.ExamResultId

If(OBJECT_ID('tempdb..#TempWrongPathologyResultIds') Is Not Null)
Begin
    Drop Table #TempWrongPathologyResultIds
End*/