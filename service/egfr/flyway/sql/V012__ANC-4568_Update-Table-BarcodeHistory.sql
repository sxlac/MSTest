-- Removing the Unique constraint on the Barcode column of the BarcodeHistory table 
-- so that exams in error queue can be re-processed. 
ALTER TABLE public."BarcodeHistory"
    DROP CONSTRAINT "BarcodeHistory_Barcode_key";