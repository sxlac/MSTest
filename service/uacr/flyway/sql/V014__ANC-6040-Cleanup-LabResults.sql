-- remove any existing lab results
TRUNCATE public."LabResult";

-- remove status where lab results have been received
DELETE FROM public."ExamStatus" WHERE "ExamStatusCodeId" = 6;