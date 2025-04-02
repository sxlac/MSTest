/* Replaced when SQL Server Database was migrated to Azure Database for PostgreSQL. */

/*if not exists (select ExamStatusCodeId from ExamStatusCode where Name = 'DEE Images Found')
begin
insert into ExamStatusCode values ('DEE Images Found')
end
if not exists (select ExamStatusCodeId from ExamStatusCode where Name = 'IRIS Exam Created')
begin
insert into ExamStatusCode values ('IRIS Exam Created')
end
if not exists (select ExamStatusCodeId from ExamStatusCode where Name = 'IRIS Result Downloaded')
begin
insert into ExamStatusCode values ('IRIS Result Downloaded')
end
if not exists (select ExamStatusCodeId from ExamStatusCode where Name = 'PCP Letter Sent')
begin
insert into ExamStatusCode values ('PCP Letter Sent')
end
if not exists (select ExamStatusCodeId from ExamStatusCode where Name = 'No PCP Found')
begin
insert into ExamStatusCode values ('No PCP Found')
end
if not exists (select ExamStatusCodeId from ExamStatusCode where Name = 'Member Letter Sent')
begin
insert into ExamStatusCode values ('Member Letter Sent')
end
if not exists (select ExamStatusCodeId from ExamStatusCode where Name = 'Sent To Provider Pay')
begin
insert into ExamStatusCode values ('Sent To Provider Pay')
end

Update ExamStatusCode set Name = 'IRIS Awaiting Interpretation' where Name = 'Awaiting Interpretation'
Update ExamStatusCode set Name = 'IRIS Interpreted' where Name = 'Interpreted'
Update ExamStatusCode set Name = 'IRIS Image Received' where Name = 'Image Received'
Update ExamStatusCode set Name = 'Result Data Downloaded' where Name = 'Results Downloaded'
Update ExamStatusCode set Name = 'PDF Data Downloaded' where Name = 'PDF Created'
Update ExamStatusCode set Name = 'No DEE Images Taken' where Name = 'No Images Taken'*/