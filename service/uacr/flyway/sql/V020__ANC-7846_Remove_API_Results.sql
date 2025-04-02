DELETE
FROM "LabResult"
WHERE "ReceivedDate" = '-infinity' and "CreatedDateTime" > '2025-03-03'and "CreatedDateTime" < '2025-03-15';