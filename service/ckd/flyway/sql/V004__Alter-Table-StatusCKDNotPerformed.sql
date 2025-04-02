Insert INTO "CKDStatusCode"("CKDStatusCodeId","StatusCode")
select 7 as "CKDStatusCodeId",'CKDNotPerformed' where NOT EXISTS( select 1 from "CKDStatusCode" where "CKDStatusCodeId" = 7)