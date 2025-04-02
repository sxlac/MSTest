--- Cleanup BarcodeHistory table by replacing duplicate barcodes with a concatenated string
UPDATE "BarcodeHistory"
SET "Barcode" = CONCAT("Barcode", '__DUP', "BarcodeHistoryId")
    WHERE "Barcode" IN
          (
              SELECT
                  "Barcode"
              FROM "BarcodeHistory"
              GROUP BY
                  "Barcode"
              HAVING COUNT(*) > 1
          );