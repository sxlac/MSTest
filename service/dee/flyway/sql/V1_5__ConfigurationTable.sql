/* Replaced when SQL Server Database was migrated to Azure Database for PostgreSQL. */

/*IF OBJECT_ID('Configuration', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Configuration (
    ConfigurationId INT NOT NULL IDENTITY(1,1) CONSTRAINT [PK_ConfigurationId] PRIMARY KEY CLUSTERED (ConfigurationId ASC),
    ConfigurationName VARCHAR (256) NOT NULL,
    ConfigurationValue VARCHAR (256) NOT NULL,
    LastUpdated DATETIME NOT NULL  DEFAULT GETUTCDATE()
    )
END
GO


if not exists (select ConfigurationValue from Configuration where ConfigurationName = 'LastRunDateTime')
begin
insert into dbo.Configuration(ConfigurationName,ConfigurationValue,LastUpdated) values ('LastRunDateTime', Convert(VARCHAR,GETUTCDATE(),120),GETUTCDATE())
end
Go*/