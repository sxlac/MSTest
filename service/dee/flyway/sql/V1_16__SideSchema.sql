/* Replaced when SQL Server Database was migrated to Azure Database for PostgreSQL. */

/*-- Creating ExamLateralityGrade Table.
BEGIN
    CREATE TABLE dbo.ExamLateralityGrade (
		ExamLateralityGradeId INT NOT NULL IDENTITY(1,1) CONSTRAINT [PK_ExamLateralityGradeId] PRIMARY KEY CLUSTERED (ExamLateralityGradeId ASC),
		ExamId INT NOT NULL CONSTRAINT FK_ExamLateralityGrade_Exam FOREIGN KEY(ExamId) REFERENCES Exam(ExamId),
		LateralityCodeId INT NOT NULL CONSTRAINT FK_ExamLateralityGrade_LateralityCode FOREIGN KEY(LateralityCodeId) REFERENCES LateralityCode(LateralityCodeId),
		Gradable BIT NOT NULL
    )
END
GO

-- Creating NonGradableReason Table.
BEGIN
    CREATE TABLE dbo.NonGradableReason (
		NonGradableReasonId INT NOT NULL IDENTITY(1,1) CONSTRAINT [PK_NonGradableReasonId] PRIMARY KEY CLUSTERED (NonGradableReasonId ASC),
		ExamLateralityGradeId INT NOT NULL CONSTRAINT FK_NonGradableReason_LateralityCode FOREIGN KEY(ExamLateralityGradeId) REFERENCES ExamLateralityGrade(ExamLateralityGradeId),
		NonGradableReason NVARCHAR(1000) NOT NULL
    )
END
GO*/