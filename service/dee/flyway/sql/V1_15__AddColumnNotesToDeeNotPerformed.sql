/* Replaced when SQL Server Database was migrated to Azure Database for PostgreSQL. */

/*--Add new field Notes to DeeNotPerformed table.
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[DeeNotPerformed]') AND name = 'Notes')
BEGIN
	ALTER TABLE DeeNotPerformed add  Notes varchar(MAX) NULL;
END*/