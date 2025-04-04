ALTER TABLE "HBA1CPOC"
ADD COLUMN IF NOT EXISTS "FirstName" 					VARCHAR(100)  NULL,
ADD COLUMN IF NOT EXISTS "MiddleName" 					VARCHAR(100)  NULL,
ADD COLUMN IF NOT EXISTS "LastName"						VARCHAR(100)  NULL,
ADD COLUMN IF NOT EXISTS "DateOfBirth"					TIMESTAMP     NULL,
ADD COLUMN IF NOT EXISTS "AddressLineOne" 				VARCHAR(200)  NULL,
ADD COLUMN IF NOT EXISTS "AddressLineTwo"				VARCHAR(200)  NULL,
ADD COLUMN IF NOT EXISTS "City"							VARCHAR(100)  NULL,
ADD COLUMN IF NOT EXISTS "State"						VARCHAR(2)    NULL,
ADD COLUMN IF NOT EXISTS "ZipCode"						VARCHAR(5)    NULL,
ADD COLUMN IF NOT EXISTS "NationalProviderIdentifier" 	VARCHAR(10)   NULL,
ADD COLUMN IF NOT EXISTS "ExpirationDate" 				TIMESTAMP     NULL,
ADD COLUMN IF NOT EXISTS "A1CPercent"					VARCHAR(50)   NULL
