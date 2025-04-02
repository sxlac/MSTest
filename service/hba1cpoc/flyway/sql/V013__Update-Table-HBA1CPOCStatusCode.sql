UPDATE "HBA1CPOCStatusCode" AS "statusCodes" SET 
 "StatusCode" = "newStatusCodes"."StatusCode"
FROM (VALUES
  (10, 'CdiPassedReceived'),
  (11, 'CdiFailedWithPayReceived'),
  (12, 'CdiFailedWithoutPayReceived')  
) AS "newStatusCodes" ("HBA1CPOCStatusCodeId", "StatusCode")
WHERE "statusCodes"."HBA1CPOCStatusCodeId" = "newStatusCodes"."HBA1CPOCStatusCodeId";