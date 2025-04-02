/* Replaced when SQL Server Database was migrated to Azure Database for PostgreSQL. */

/*IF OBJECT_ID('ProviderPay', 'U') IS NULL
BEGIN
	CREATE TABLE "ProviderPay"
				(
					Id  INT IDENTITY(1, 1) PRIMARY KEY,
					PaymentId  VARCHAR(50),
					ExamId INTEGER NOT NULL REFERENCES Exam ( ExamId),
					CreatedDateTime DATETIME NOT NULL DEFAULT GETDATE()
				);

END
GO*/