UPDATE public."ExamNotPerformed" 
SET "CreatedDateTime" = "CKDStatus"."CreatedDateTime"
FROM public."CKDStatus"
WHERE public."ExamNotPerformed"."CKDId" = public."CKDStatus"."CKDId"
AND public."ExamNotPerformed"."CreatedDateTime"='0001-01-01T00:00:00+00:00'
AND public."CKDStatus"."CKDStatusCodeId" = 7 --NotPerformed
