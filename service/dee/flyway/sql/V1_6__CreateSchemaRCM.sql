/* Replaced when SQL Server Database was migrated to Azure Database for PostgreSQL. */

/*IF OBJECT_ID('DEEBilling', 'U') IS NULL
BEGIN
	CREATE TABLE "DEEBilling"
				(
					Id  INT IDENTITY(1, 1) PRIMARY KEY,
					BillId  VARCHAR(50),
					ExamId INTEGER NOT NULL REFERENCES Exam ( ExamId),
					CreatedDateTime DATETIME NOT NULL DEFAULT GETDATE()
				);

END
GO*/