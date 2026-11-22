/*
    Replaces the website's team members ("Meet our team") with the Advisory Services Department
    from the team structure chart (v2.0, 11 November 2026). Dr. Yasser Elgamal is not a row here:
    he is the founder in the centre of the section.

    - Deletes every current TeamMembers row (the placeholders). Their photos stay in the media library.
    - Inserts the people below without photos (the site shows their initials); add photos, bios and
      practices later from the admin panel.
    - Runs in one transaction: either all of it applies or nothing does. Safe to run more than once.

    Run against the live database (YelgWebSite), e.g. in SSMS, or:
        sqlcmd -S <server> -d YelgWebSite -E -f 65001 -i replace-team-members.sql
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

DELETE FROM dbo.TeamMembers;

DECLARE @now datetime2 = SYSUTCDATETIME();
DECLARE @fpa bigint = (SELECT TOP (1) Id FROM dbo.TeamPractices
                       WHERE Deleted = 0 AND Title_En LIKE N'%FP&A%' ORDER BY Id);

INSERT INTO dbo.TeamMembers
    (SortOrder, IsPublished, Name_En, Name_Ar, Role_En, Role_Ar, Bio_En, Bio_Ar,
     Credentials, PhotoId, LinkedInUrl, TeamPracticeId,
     AddedDate, LastModifiedDate, Deleted)
VALUES
    (1, 1, N'Abdul Hamid Yusuf',  N'عبد الحميد يوسف',  N'Group Head',                  N'رئيس المجموعة',                          N'', N'', N'', NULL, NULL, NULL, @now, @now, 0),
    (2, 1, N'Khaled Reda',        N'خالد رضا',         N'Technology Business Head',    N'مدير تكنولوجيا الأعمال',                 N'', N'', N'', NULL, NULL, NULL, @now, @now, 0),
    (3, 1, N'Mahmoud Kamal',      N'محمود كمال',       N'HR Section Head',             N'رئيس قسم الموارد البشرية',               N'', N'', N'', NULL, NULL, NULL, @now, @now, 0),
    (4, 1, N'Sally Abdelrahman',  N'سالي عبد الرحمن',  N'Head of Logistics & Admin',   N'رئيسة الشؤون اللوجستية والإدارية',       N'', N'', N'', NULL, NULL, NULL, @now, @now, 0),
    (5, 1, N'Ahmed Abdelsalam',   N'أحمد عبد السلام',  N'FP&A Manager',                N'مدير التخطيط والتحليل المالي',           N'', N'', N'', NULL, NULL, @fpa, @now, @now, 0),
    (6, 1, N'Mostafa Tariq',      N'مصطفى طارق',       N'Team Leader',                 N'قائد فريق',                              N'', N'', N'', NULL, NULL, NULL, @now, @now, 0),
    (7, 1, N'Ahmed Nazier',       N'أحمد نذير',        N'Assistant Manager',           N'مساعد مدير',                             N'', N'', N'', NULL, NULL, NULL, @now, @now, 0),
    (8, 1, N'Maged Samy',         N'ماجد سامي',        N'Assistant Manager',           N'مساعد مدير',                             N'', N'', N'', NULL, NULL, NULL, @now, @now, 0);

COMMIT TRANSACTION;

SELECT SortOrder, Name_En, Name_Ar, Role_En, Role_Ar, TeamPracticeId
FROM dbo.TeamMembers
ORDER BY SortOrder;
