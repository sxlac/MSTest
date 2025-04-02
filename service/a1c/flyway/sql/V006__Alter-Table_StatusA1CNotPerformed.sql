Insert INTO "A1CStatusCode"("A1CStatusCodeId","StatusCode")
select 10 as "A1CStatusCodeId",'A1CNotPerformed' where NOT EXISTS( select 1 from "A1CStatusCode" where "A1CStatusCodeId" = 10)