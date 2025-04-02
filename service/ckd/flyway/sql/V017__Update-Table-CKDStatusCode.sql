UPDATE "CKDStatusCode" AS "statusCodes" SET 
 "StatusCode" = "newStatusCodes"."StatusCode"
FROM (VALUES
  (11, 'CdiPassedReceived'),
  (12, 'CdiFailedWithPayReceived'),
  (13, 'CdiFailedWithoutPayReceived')  
) AS "newStatusCodes" ("CKDStatusCodeId", "StatusCode")
WHERE "statusCodes"."CKDStatusCodeId" = "newStatusCodes"."CKDStatusCodeId";