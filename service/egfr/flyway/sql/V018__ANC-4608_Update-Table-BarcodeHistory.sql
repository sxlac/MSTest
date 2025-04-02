--- Create Unique constraint on the Barcode column of the BarcodeHistory table
ALTER TABLE public."BarcodeHistory"
    ADD CONSTRAINT "BarcodeHistory_Barcode_key" UNIQUE ("Barcode");