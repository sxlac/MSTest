ALTER TABLE public."PAD" ALTER COLUMN "UserName" DROP NOT NULL;

-- Update existing PDF records that have CreatedDateTime improperly set to "0001-01-01 00:00:00+00"
UPDATE	public."PDFToClient" p
   SET	"CreatedDateTime" = s."CreatedDateTime"
  FROM	public."PADStatus" s
 WHERE	p."PADId" = s."PADId"
   AND	p."CreatedDateTime" < '1900-01-01'
   AND	s."PADStatusCodeId" = 3; -- BillableEventReceived (there is no event that tracks PDF Delivered)
