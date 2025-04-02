
CREATE SEQUENCE PdfDeliveredToClient_PdfDeliveredToClientId_seq;

ALTER TABLE public."PdfDeliveredToClient" ALTER COLUMN "PdfDeliveredToClientId" SET DEFAULT nextval('PdfDeliveredToClient_PdfDeliveredToClientId_seq');