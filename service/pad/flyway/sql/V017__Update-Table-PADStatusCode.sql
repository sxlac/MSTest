UPDATE "PADStatusCode" AS "statusCodes" SET 
 "StatusCode" = "newStatusCodes"."StatusCode"
FROM (VALUES
  (9, 'CdiPassedReceived'),
  (10, 'CdiFailedWithPayReceived'),
  (11, 'CdiFailedWithoutPayReceived')  
) AS "newStatusCodes" ("PADStatusCodeId", "StatusCode")
WHERE "statusCodes"."PADStatusCodeId" = "newStatusCodes"."PADStatusCodeId";
