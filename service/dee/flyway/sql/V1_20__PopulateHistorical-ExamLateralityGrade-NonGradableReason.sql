/* Replaced when SQL Server Database was migrated to Azure Database for PostgreSQL. */

/*ALTER TABLE dbo.ExamLateralityGrade
ADD CONSTRAINT [UQ_ExamLateralityGrade] UNIQUE NONCLUSTERED (ExamId, LateralityCodeId); -- Do not allow multiple records with the same ExamId and LateralityCodeId
GO

CREATE FUNCTION dbo.PivotDelimited
(
    @input NVARCHAR(4000),
    @separator NVARCHAR(8)
)
RETURNS TABLE*/
/*
Given:

@input = 'a; b; cde'
@separator = '; '

Outputs the following table:

| PartNumber | Part  |
|------------|-------|
| 1          | 'a'   |
| 2          | 'b'   |
| 3          | 'cde' |

*/
/*AS
RETURN (
    WITH Parts(PartNumber, StartIndex, EndIndex) AS
    (
        SELECT  1, 1, CHARINDEX(@separator, @input)
         UNION  ALL
        SELECT  PartNumber + 1, EndIndex + LEN(REPLACE(@separator,' ','_')/*because LEN excludes whitespaces*/), CHARINDEX(@separator, @input, EndIndex + 1)
          FROM  Parts
         WHERE  EndIndex > 0
    )
    SELECT  PartNumber,
            SUBSTRING(@input, StartIndex,
                CASE
                    WHEN EndIndex > 0 THEN EndIndex - StartIndex
                    ELSE LEN(@input)
                END
            ) AS Part
      FROM  Parts
)
GO

BEGIN TRANSACTION;

INSERT  INTO dbo.ExamLateralityGrade -- See V1_16 where this is defined
        (ExamId, LateralityCodeId, Gradable)
SELECT  i.ExamId, i.LateralityCodeId, i.Gradable
  FROM  dbo.ExamImage i
 WHERE  i.Gradable IS NOT NULL -- Constraint in destination
   AND  i.LateralityCodeId IS NOT NULL -- Constraint in destination
   AND  i.LateralityCodeId IN (1,2) -- Right/Left, ignore 3 (Both) and 4 (Unknown)
   AND  NOT EXISTS -- Ignore if there are matching records already in the new table
        (
            SELECT  *
              FROM  dbo.ExamLateralityGrade g
             WHERE  g.ExamId = i.ExamId
               AND  g.LateralityCodeId = i.LateralityCodeId
        )
 GROUP  BY i.ExamId, i.LateralityCodeId, i.Gradable; -- Exams with multiple images of the same side should only have one record inserted*/

/*
In the ExamImage table, the column NotGradableReasons unfortunately can
have more than one reason in the same field, so it can look like:

'Insufficient View of Macula; Insufficient View of Optic Nerve'

With the new schema, we're looking to break these reasons out into separate
records as they should have originally been. This is where the function
defined above comes into play. With the CROSS APPLY below, it will take a
single ExamImage record that may look like the above example, and select two
records, with values:

'Insufficient View of Macula'
'Insufficient View of Optic Nerve'
*/

/*DECLARE @delimiter NVARCHAR(2) = '; ';

INSERT  INTO dbo.NonGradableReason -- See V1_16 where this is defined
        (ExamLateralityGradeId, Reason)
SELECT  g.ExamLateralityGradeId, parts.Part
  FROM  dbo.ExamImage i
 INNER  JOIN dbo.ExamLateralityGrade g ON (g.ExamId = i.ExamId AND g.LateralityCodeId = i.LateralityCodeId) -- Join to the records we just inserted in the statement above
 CROSS  APPLY dbo.PivotDelimited(i.NotGradableReasons, @delimiter) AS parts
 WHERE  i.Gradable = 0 -- We only want results where the image is not gradable
   AND  NULLIF(i.NotGradableReasons, '') IS NOT NULL -- Ignore if there are no reasons
   AND  NOT EXISTS -- Ignore if there already are not gradable reason(s) for this side
        (
            SELECT  *
              FROM  dbo.NonGradableReason r
             INNER  JOIN dbo.ExamLateralityGrade gg ON (gg.ExamLateralityGradeId = r.ExamLateralityGradeId)
             WHERE  gg.ExamId = i.ExamId
               AND  gg.LateralityCodeId = i.LateralityCodeId
        )
   AND  NULLIF(parts.Part,'') IS NOT NULL -- Ignore any empty reasons (ex the first part in '; reason2')
 GROUP  BY g.ExamLateralityGradeId, parts.Part; -- Ignore duplicates

COMMIT TRANSACTION;

-- Or keep it?
--DROP FUNCTION dbo.PivotDelimited; -- Housekeeping; can add back later if needed*/