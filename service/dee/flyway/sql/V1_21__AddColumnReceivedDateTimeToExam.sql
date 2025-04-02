/* Replaced when SQL Server Database was migrated to Azure Database for PostgreSQL. */

/*--Add new field ReceivedDateTime to Exam table.
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Exam]') AND name = 'ReceivedDateTime')
BEGIN
	ALTER TABLE Exam add ReceivedDateTime DATETIME NULL;
END*/