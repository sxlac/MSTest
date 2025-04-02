/* Replaced when SQL Server Database was migrated to Azure Database for PostgreSQL. */

/*IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Exam]') AND name = 'State')
BEGIN
	ALTER TABLE Exam ADD State nvarchar(5) null
END	

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Exam]') AND name = 'RequestId')
BEGIN
	ALTER TABLE Exam ADD RequestId UniqueIdentifier null
END*/