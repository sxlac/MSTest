/* Replaced when SQL Server Database was migrated to Azure Database for PostgreSQL. */

/*--Add new fields LeftEyeHasPathology and RightEyeHasPathology to ExamResult table.
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[ExamResult]') AND name = 'LeftEyeHasPathology')
BEGIN
	ALTER TABLE ExamResult add  LeftEyeHasPathology BIT NULL;
END	

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[ExamResult]') AND name = 'RightEyeHasPathology')
BEGIN
	ALTER TABLE ExamResult add  RightEyeHasPathology BIT NULL;
END*/