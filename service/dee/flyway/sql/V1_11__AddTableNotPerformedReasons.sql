/* Replaced when SQL Server Database was migrated to Azure Database for PostgreSQL. */

/*-- All reasons why an evaluation with a DEE product may not be performed
IF OBJECT_ID('NotPerformedReason', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.NotPerformedReason (
		NotPerformedReasonId SMALLINT NOT NULL IDENTITY(1,1) CONSTRAINT [PK_NotPerformedReasonId] PRIMARY KEY CLUSTERED (NotPerformedReasonId ASC),
		AnswerId [int] NOT NULL,
		Reason [VARCHAR] (256) NOT NULL,
    )
END
GO

--Add Lookup values to the newly created NotPerformedReason table.
if not exists (select AnswerId from NotPerformedReason where AnswerId = 30943)
begin
insert into dbo.NotPerformedReason(AnswerId,Reason) values (30943,'Member recently completed')
end

if not exists (select AnswerId from NotPerformedReason where AnswerId = 30944)
begin
insert into dbo.NotPerformedReason(AnswerId,Reason) values (30944,'Scheduled to complete')
end

if not exists (select AnswerId from NotPerformedReason where AnswerId = 30945)
begin
insert into dbo.NotPerformedReason(AnswerId,Reason) values (30945,'Member apprehension')
end

if not exists (select AnswerId from NotPerformedReason where AnswerId = 30946)
begin
insert into dbo.NotPerformedReason(AnswerId,Reason) values (30946,'Not interested')
end

if not exists (select AnswerId from NotPerformedReason where AnswerId = 30947)
begin
insert into dbo.NotPerformedReason(AnswerId,Reason) values (30947,'Other')
end

if not exists (select AnswerId from NotPerformedReason where AnswerId = 30950)
begin
insert into dbo.NotPerformedReason(AnswerId,Reason) values (30950,'Technical issue')
end

if not exists (select AnswerId from NotPerformedReason where AnswerId = 30951)
begin
insert into dbo.NotPerformedReason(AnswerId,Reason) values (30951,'Environmental issue')
end

if not exists (select AnswerId from NotPerformedReason where AnswerId = 30952)
begin
insert into dbo.NotPerformedReason(AnswerId,Reason) values (30952,'No supplies or equipment')
end

if not exists (select AnswerId from NotPerformedReason where AnswerId = 30953)
begin
insert into dbo.NotPerformedReason(AnswerId,Reason) values (30953,'Insufficient training')
end

if not exists (select AnswerId from NotPerformedReason where AnswerId = 50914)
begin
insert into dbo.NotPerformedReason(AnswerId,Reason) values (50914,'Member physically unable')
end
Go


-- Details about evaluations where a DEE exam was not performed
IF OBJECT_ID('DeeNotPerformed', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.DeeNotPerformed (
		DeeNotPerformedId INT NOT NULL IDENTITY(1,1) CONSTRAINT [PK_DeeNotPerformed] PRIMARY KEY CLUSTERED (DeeNotPerformedId ASC),
		ExamId INTEGER NOT NULL REFERENCES Exam ( ExamId),
		NotPerformedReasonId SMALLINT NOT NULL REFERENCES NotPerformedReason ( NotPerformedReasonId),
		CreatedDateTime DATETIME NOT NULL DEFAULT GETDATE()
	)
END
GO*/