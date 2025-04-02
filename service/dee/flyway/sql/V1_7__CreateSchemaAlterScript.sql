/* Replaced when SQL Server Database was migrated to Azure Database for PostgreSQL. */

/*IF OBJECT_ID('Exam', 'U') IS NOT NULL
BEGIN

	IF COL_LENGTH('Exam', 'State') IS NULL
	BEGIN
		ALTER TABLE Exam
		ADD [State] VARCHAR(50) NULL
	END

	IF COL_LENGTH('Exam', 'ClientId') IS NULL
	BEGIN
		ALTER TABLE Exam
		ADD [ClientId] int NULL
	END
	
END
GO

if not exists (select ExamStatusCodeId from ExamStatusCode where Name = 'Billable Event Recieved')
begin
INSERT INTO ExamStatusCode  VALUES  ('Billable Event Recieved')
end*/