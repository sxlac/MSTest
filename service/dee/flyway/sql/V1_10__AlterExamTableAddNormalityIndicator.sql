/* Replaced when SQL Server Database was migrated to Azure Database for PostgreSQL. */

/*IF NOT EXISTS (
  SELECT * 
  FROM   sys.columns 
  WHERE  object_id = OBJECT_ID(N'[dbo].[ExamFinding]') 
         AND name = 'NormalityIndicator'
)
BEGIN
	ALTER TABLE ExamFinding
	ADD NormalityIndicator varchar(1) null;
END



IF NOT EXISTS (
  SELECT * 
  FROM   sys.columns 
  WHERE  object_id = OBJECT_ID(N'[dbo].[ExamResult]') 
         AND name = 'NormalityIndicator'
)
BEGIN
	ALTER TABLE ExamResult
	ADD NormalityIndicator varchar(1) null;
END*/