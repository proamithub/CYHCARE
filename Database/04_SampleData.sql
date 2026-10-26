-- ============================================================================
-- CYH Care — Local Sample Seed Data for Demo & Testing
-- Target: SQL Server (VRS-AMITPC\SQLEXPRESS)
-- Database: CYHCare
-- ============================================================================

USE [CYHCare];
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-- Standard BCrypt Hash for password: Password@123
DECLARE @DefaultPasswordHash NVARCHAR(200) = N'$2a$11$jE1e8wQzP/P/n9E35pG9j.yCq9P5vV3X/a4C0T4A/vQ2w9q/u7Icy';

-- 1. Seed Requesters (Users)
IF NOT EXISTS (SELECT 1 FROM [dbo].[Users] WHERE [Email] = 'ramesh.patient@gmail.com')
BEGIN
    INSERT INTO [dbo].[Users] ([FullName], [Phone], [Email], [PasswordHash], [Role], [IsVerified], [IsActive])
    VALUES (N'Ramesh Kumar', '9876543210', 'ramesh.patient@gmail.com', @DefaultPasswordHash, 'User', 1, 1);
END

IF NOT EXISTS (SELECT 1 FROM [dbo].[Users] WHERE [Email] = 'sunita.sharma@gmail.com')
BEGIN
    INSERT INTO [dbo].[Users] ([FullName], [Phone], [Email], [PasswordHash], [Role], [IsVerified], [IsActive])
    VALUES (N'Sunita Sharma', '9811223344', 'sunita.sharma@gmail.com', @DefaultPasswordHash, 'User', 1, 1);
END

-- 2. Seed Helpers
DECLARE @Helper1UserId INT;
IF NOT EXISTS (SELECT 1 FROM [dbo].[Users] WHERE [Email] = 'rohit.helper@gmail.com')
BEGIN
    INSERT INTO [dbo].[Users] ([FullName], [Phone], [Email], [PasswordHash], [Role], [IsVerified], [IsActive])
    VALUES (N'Rohit Verma', '9899112233', 'rohit.helper@gmail.com', @DefaultPasswordHash, 'Helper', 1, 1);
    SET @Helper1UserId = SCOPE_IDENTITY();

    INSERT INTO [dbo].[HelperProfiles] ([UserId], [KycStatus], [IsOnDuty], [LocationSharing], [LastLat], [LastLng], [LastUpdate], [HourlyRate], [Rating])
    VALUES (@Helper1UserId, 'Approved', 1, 1, 28.567200, 77.210000, SYSUTCDATETIME(), 120.00, 4.90);
END

DECLARE @Helper2UserId INT;
IF NOT EXISTS (SELECT 1 FROM [dbo].[Users] WHERE [Email] = 'anjali.helper@gmail.com')
BEGIN
    INSERT INTO [dbo].[Users] ([FullName], [Phone], [Email], [PasswordHash], [Role], [IsVerified], [IsActive])
    VALUES (N'Anjali Singh', '9877445566', 'anjali.helper@gmail.com', @DefaultPasswordHash, 'Helper', 1, 1);
    SET @Helper2UserId = SCOPE_IDENTITY();

    INSERT INTO [dbo].[HelperProfiles] ([UserId], [KycStatus], [IsOnDuty], [LocationSharing], [LastLat], [LastLng], [LastUpdate], [HourlyRate], [Rating])
    VALUES (@Helper2UserId, 'Approved', 1, 1, 28.570000, 77.208000, SYSUTCDATETIME(), 100.00, 4.80);
END

DECLARE @Helper3UserId INT;
IF NOT EXISTS (SELECT 1 FROM [dbo].[Users] WHERE [Email] = 'vikram.pending@gmail.com')
BEGIN
    INSERT INTO [dbo].[Users] ([FullName], [Phone], [Email], [PasswordHash], [Role], [IsVerified], [IsActive])
    VALUES (N'Vikram Yadav', '9812345678', 'vikram.pending@gmail.com', @DefaultPasswordHash, 'Helper', 0, 1);
    SET @Helper3UserId = SCOPE_IDENTITY();

    INSERT INTO [dbo].[HelperProfiles] ([UserId], [KycStatus], [IsOnDuty], [LocationSharing], [LastLat], [LastLng], [LastUpdate], [HourlyRate], [Rating])
    VALUES (@Helper3UserId, 'Pending', 0, 1, 28.580000, 77.220000, SYSUTCDATETIME(), 90.00, 0.00);
END

-- 3. Seed Sample Jobs
DECLARE @RequesterId INT = (SELECT TOP 1 [UserId] FROM [dbo].[Users] WHERE [Role] = 'User');
DECLARE @HelperProfile1 INT = (SELECT TOP 1 [HelperId] FROM [dbo].[HelperProfiles] hp INNER JOIN [dbo].[Users] u ON hp.[UserId] = u.[UserId] WHERE u.[Email] = 'rohit.helper@gmail.com');
DECLARE @HelperProfile2 INT = (SELECT TOP 1 [HelperId] FROM [dbo].[HelperProfiles] hp INNER JOIN [dbo].[Users] u ON hp.[UserId] = u.[UserId] WHERE u.[Email] = 'anjali.helper@gmail.com');

IF @RequesterId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [dbo].[Jobs] WHERE [Address] LIKE '%AIIMS Hospital%')
BEGIN
    -- Active Job 1 (InProgress)
    INSERT INTO [dbo].[Jobs] ([RequesterId], [ServiceType], [Address], [Lat], [Lng], [ScheduledAt], [Status], [CreatedAt])
    VALUES (@RequesterId, 'Hospital Companion (OPD Visit)', N'AIIMS Hospital, Gate No. 2, Sri Aurobindo Marg, New Delhi', 28.567200, 77.210000, SYSUTCDATETIME(), 'InProgress', SYSUTCDATETIME());

    DECLARE @JobId1 INT = SCOPE_IDENTITY();

    IF @HelperProfile1 IS NOT NULL
    BEGIN
        INSERT INTO [dbo].[JobAssignments] ([JobId], [HelperId], [AcceptedAt], [ArrivedAt], [StartedAt])
        VALUES (@JobId1, @HelperProfile1, DATEADD(MINUTE, -45, SYSUTCDATETIME()), DATEADD(MINUTE, -25, SYSUTCDATETIME()), DATEADD(MINUTE, -20, SYSUTCDATETIME()));

        DECLARE @AssignId1 INT = SCOPE_IDENTITY();

        -- Gps breadcrumbs
        INSERT INTO [dbo].[GpsLogs] ([AssignmentId], [Lat], [Lng], [SpeedKmph], [RecordedAt])
        VALUES (@AssignId1, 28.565000, 77.208000, 15.5, DATEADD(MINUTE, -40, SYSUTCDATETIME())),
               (@AssignId1, 28.566200, 77.209100, 12.0, DATEADD(MINUTE, -30, SYSUTCDATETIME())),
               (@AssignId1, 28.567200, 77.210000, 0.0, DATEADD(MINUTE, -25, SYSUTCDATETIME()));
    END

    -- Active Job 2 (Accepted)
    INSERT INTO [dbo].[Jobs] ([RequesterId], [ServiceType], [Address], [Lat], [Lng], [ScheduledAt], [Status], [CreatedAt])
    VALUES (@RequesterId, 'Elderly Assistance & Escort', N'Safdarjung Hospital, Ring Road, New Delhi', 28.570000, 77.208000, DATEADD(HOUR, 2, SYSUTCDATETIME()), 'Accepted', SYSUTCDATETIME());

    DECLARE @JobId2 INT = SCOPE_IDENTITY();

    IF @HelperProfile2 IS NOT NULL
    BEGIN
        INSERT INTO [dbo].[JobAssignments] ([JobId], [HelperId], [AcceptedAt])
        VALUES (@JobId2, @HelperProfile2, DATEADD(MINUTE, -10, SYSUTCDATETIME()));
    END

    -- Pending Job 3 (New request searching for helpers)
    INSERT INTO [dbo].[Jobs] ([RequesterId], [ServiceType], [Address], [Lat], [Lng], [ScheduledAt], [Status], [CreatedAt])
    VALUES (@RequesterId, 'Medicine & Lab Report Pickup', N'Max Super Speciality Hospital, Saket, New Delhi', 28.528300, 77.211700, DATEADD(HOUR, 3, SYSUTCDATETIME()), 'Pending', SYSUTCDATETIME());

    PRINT 'Sample test jobs and telemetry successfully created in CYHCare database.';
END
GO
