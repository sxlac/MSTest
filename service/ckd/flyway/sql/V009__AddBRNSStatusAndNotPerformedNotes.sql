Insert INTO "CKDStatusCode"("CKDStatusCodeId","StatusCode")
select 8 as "CKDStatusCodeId",'BillRequestNotSent' where NOT EXISTS( select 1 from "CKDStatusCode" where "CKDStatusCodeId" = 8);

ALTER TABLE public."ExamNotPerformed" ADD COLUMN "Notes" VARCHAR NULL;