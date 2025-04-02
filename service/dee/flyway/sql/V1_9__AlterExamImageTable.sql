/* Replaced when SQL Server Database was migrated to Azure Database for PostgreSQL. */

/*IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[ExamImage]') AND name = 'NotGradableReasons')
BEGIN
	ALTER TABLE ExamImage ADD NotGradableReasons nvarchar(1000) null
END	

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[ExamImage]') AND name = 'Gradable')
BEGIN
	ALTER TABLE ExamImage ADD Gradable bit null
END*/