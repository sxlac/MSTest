/* Replaced when SQL Server Database was migrated to Azure Database for PostgreSQL. */

/*IF NOT EXISTS (SELECT ExamStatusCodeId FROM ExamStatusCode WHERE Name = 'ProviderPayableEventReceived')
BEGIN
  INSERT INTO ExamStatusCode ([Name]) VALUES 
  ('ProviderPayableEventReceived')
END

IF NOT EXISTS (SELECT ExamStatusCodeId FROM ExamStatusCode WHERE Name = 'ProviderNonPayableEventReceived')
BEGIN
  INSERT INTO ExamStatusCode ([Name]) VALUES
	('ProviderNonPayableEventReceived')
END

IF NOT EXISTS (SELECT ExamStatusCodeId FROM ExamStatusCode WHERE Name = 'ProviderPayRequestSent')
BEGIN
  INSERT INTO ExamStatusCode ([Name]) 
  VALUES ('ProviderPayRequestSent')
END

IF NOT EXISTS (SELECT ExamStatusCodeId FROM ExamStatusCode WHERE Name = 'CdiPassedReceived')
BEGIN
  INSERT INTO ExamStatusCode ([Name])
    VALUES ('CdiPassedReceived')
END

IF NOT EXISTS (SELECT ExamStatusCodeId FROM ExamStatusCode WHERE Name = 'CdiFailedWithPayReceived')
BEGIN
  INSERT INTO ExamStatusCode ([Name])
    VALUES ('CdiFailedWithPayReceived')
END

IF NOT EXISTS (SELECT ExamStatusCodeId FROM ExamStatusCode WHERE Name = 'CdiFailedWithoutPayReceived')
BEGIN
  INSERT INTO ExamStatusCode ([Name])
    VALUES ('CdiFailedWithoutPayReceived')
END*/