Insert INTO "FOBTStatusCode"("FOBTStatusCodeId","StatusCode")
select 9 as "FOBTStatusCodeId",'FOBTNotPerformed' where NOT EXISTS( select 1 from "FOBTStatusCode" where "FOBTStatusCodeId" = 9)