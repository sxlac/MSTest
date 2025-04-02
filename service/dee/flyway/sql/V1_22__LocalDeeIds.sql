/* Replaced when SQL Server Database was migrated to Azure Database for PostgreSQL. */

/*ALTER TABLE dbo.Exam
ADD ExamLocalId nvarchar(50) NULL;
GO

UPDATE  upd
   SET  upd.ExamLocalId = upd.DeeExamId
  FROM  dbo.Exam upd
 WHERE  upd.DeeExamId IN
        (
        SELECT  DeeExamId
          FROM  dbo.Exam
         GROUP  by DeeExamId
        HAVING  COUNT(*) < 2
        ) and upd.DeeExamId IS NOT NULL

UPDATE  dbo.Exam
   SET  ExamLocalId = CONCAT(ExamId,'-',DeeExamId)
 WHERE  ExamLocalId IS NULL;
GO

ALTER TABLE dbo.Exam
ADD CONSTRAINT unique_ExamLocalId UNIQUE (ExamLocalId);
GO

ALTER TABLE dbo.ExamImage
ADD ImageLocalId nvarchar(50) NULL;
GO

UPDATE  upd
   SET  upd.ImageLocalId = upd.DeeImageId
  FROM  dbo.ExamImage upd
 WHERE  upd.DeeImageId IN
        (
        SELECT  DeeImageId
          FROM  dbo.ExamImage
         GROUP  by DeeImageId
        HAVING  COUNT(*) < 2
        ) and upd.DeeImageId IS NOT NULL

UPDATE  dbo.ExamImage
   SET  ImageLocalId = CONCAT(ExamImageId,'-',DeeImageId)
 WHERE  ImageLocalId IS NULL;
GO

ALTER TABLE dbo.ExamImage
ADD CONSTRAINT unique_ImageLocalId UNIQUE (ImageLocalId);
GO*/