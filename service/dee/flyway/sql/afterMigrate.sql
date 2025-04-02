/* Replaced when SQL Server Database was migrated to Azure Database for PostgreSQL. */

/*Permissions*/

--Administrators/Managers
--Tech Leads
/*
IF NOT EXISTS (SELECT 1 FROM master.dbo.syslogins WHERE loginname = N'CENSEOHEALTH\development team leads')
CREATE LOGIN [CENSEOHEALTH\development team leads] FROM WINDOWS
GO


IF NOT EXISTS (SELECT 1 FROM sys.sysusers WHERE name = N'CENSEOHEALTH\development team leads')
CREATE USER [CENSEOHEALTH\development team leads] FOR LOGIN [CENSEOHEALTH\development team leads]
GO
ALTER ROLE [db_datareader] ADD MEMBER [CENSEOHEALTH\development team leads]
GO

--Technical Directors
IF NOT EXISTS (SELECT 1 FROM master.dbo.syslogins WHERE loginname = N'CENSEOHEALTH\Technical Directors')
CREATE LOGIN [CENSEOHEALTH\Technical Directors] FROM WINDOWS
GO


IF NOT EXISTS (SELECT 1 FROM sys.sysusers WHERE name = N'CENSEOHEALTH\Technical Directors')
CREATE USER [CENSEOHEALTH\Technical Directors] FOR LOGIN [CENSEOHEALTH\Technical Directors]
GO
ALTER ROLE [db_datareader] ADD MEMBER [CENSEOHEALTH\Technical Directors]
GO
*/
--Users

/*svcdee${Env}*/
/*
IF NOT EXISTS (SELECT 1 FROM master.dbo.syslogins WHERE loginname = N'CENSEOHEALTH\svcdee${Env}')
CREATE LOGIN [CENSEOHEALTH\svcdee${Env}] FROM WINDOWS
GO

IF NOT EXISTS (SELECT 1 FROM sys.sysusers WHERE name =  N'CENSEOHEALTH\svcdee${Env}')
CREATE USER [CENSEOHEALTH\svcdee${Env}] FOR LOGIN [CENSEOHEALTH\svcdee${Env}]
GO
ALTER ROLE [db_datareader] ADD MEMBER [CENSEOHEALTH\svcdee${Env}]
GO
ALTER ROLE [db_datawriter] ADD MEMBER [CENSEOHEALTH\svcdee${Env}]
GO
*/