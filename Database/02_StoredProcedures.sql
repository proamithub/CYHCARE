-- ============================================================================
-- CYH Care — Phase 1: Stored Procedures
-- Target: SQL Server
-- Database: CYHCare
-- Rule: SET NOCOUNT ON, Transactions with TRY/CATCH + THROW, Haversine for Distance
-- ============================================================================

USE [CYHCare];
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-- ============================================================================
-- 1. sp_User_Register
-- Purpose: Inserts a new User. If Role = 'Helper', also creates HelperProfiles row.
--          Validates duplicate Phone and Email with THROW.
-- ============================================================================
CREATE OR ALTER PROCEDURE [dbo].[sp_User_Register]
    @FullName     NVARCHAR(100),
    @Phone        VARCHAR(15),
    @Email        NVARCHAR(150),
    @PasswordHash NVARCHAR(200),
    @Role         VARCHAR(10),
    @HourlyRate   DECIMAL(10,2) = 100.00
AS
BEGIN
    SET NOCOUNT ON;

    -- Validate Role
    IF @Role NOT IN ('User', 'Helper', 'Admin')
    BEGIN
        THROW 50001, 'Invalid user role specified. Must be User, Helper, or Admin.', 1;
    END

    -- Check duplicate Phone
    IF EXISTS (SELECT 1 FROM [dbo].[Users] WHERE [Phone] = @Phone)
    BEGIN
        THROW 50002, 'Phone number already registered.', 1;
    END

    -- Check duplicate Email
    IF EXISTS (SELECT 1 FROM [dbo].[Users] WHERE [Email] = @Email)
    BEGIN
        THROW 50003, 'Email address already registered.', 1;
    END

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @IsVerified BIT = CASE WHEN @Role = 'Admin' THEN 1 ELSE 0 END;

        INSERT INTO [dbo].[Users] ([FullName], [Phone], [Email], [PasswordHash], [Role], [IsVerified], [IsActive])
        VALUES (@FullName, @Phone, @Email, @PasswordHash, @Role, @IsVerified, 1);

        DECLARE @NewUserId INT = SCOPE_IDENTITY();

        -- If Helper, initialize HelperProfiles
        IF @Role = 'Helper'
        BEGIN
            INSERT INTO [dbo].[HelperProfiles] ([UserId], [KycStatus], [IsOnDuty], [LocationSharing], [HourlyRate], [Rating])
            VALUES (@NewUserId, 'Pending', 0, 1, ISNULL(@HourlyRate, 100.00), 0.00);
        END

        COMMIT TRANSACTION;

        -- Return newly created user record
        SELECT 
            u.[UserId],
            u.[FullName],
            u.[Phone],
            u.[Email],
            u.[Role],
            u.[IsVerified],
            u.[IsActive],
            u.[CreatedAt],
            hp.[HelperId],
            hp.[KycStatus],
            hp.[IsOnDuty]
        FROM [dbo].[Users] u
        LEFT JOIN [dbo].[HelperProfiles] hp ON u.[UserId] = hp.[UserId]
        WHERE u.[UserId] = @NewUserId;

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

-- ============================================================================
-- 2. sp_User_GetByEmail
-- Purpose: Returns user record for authentication (API verifies BCrypt password hash).
-- ============================================================================
CREATE OR ALTER PROCEDURE [dbo].[sp_User_GetByEmail]
    @Email NVARCHAR(150)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP 1
        u.[UserId],
        u.[FullName],
        u.[Phone],
        u.[Email],
        u.[PasswordHash],
        u.[Role],
        u.[IsVerified],
        u.[IsActive],
        u.[CreatedAt],
        hp.[HelperId],
        hp.[KycStatus],
        hp.[IsOnDuty],
        hp.[LocationSharing]
    FROM [dbo].[Users] u
    LEFT JOIN [dbo].[HelperProfiles] hp ON u.[UserId] = hp.[UserId]
    WHERE u.[Email] = @Email;
END
GO

-- ============================================================================
-- 3. sp_User_GetById
-- Purpose: Returns full user profile by UserId.
-- ============================================================================
CREATE OR ALTER PROCEDURE [dbo].[sp_User_GetById]
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP 1
        u.[UserId],
        u.[FullName],
        u.[Phone],
        u.[Email],
        u.[Role],
        u.[IsVerified],
        u.[IsActive],
        u.[CreatedAt],
        hp.[HelperId],
        hp.[KycStatus],
        hp.[IsOnDuty],
        hp.[LocationSharing],
        hp.[HourlyRate],
        hp.[Rating],
        hp.[LastLat],
        hp.[LastLng],
        hp.[LastUpdate]
    FROM [dbo].[Users] u
    LEFT JOIN [dbo].[HelperProfiles] hp ON u.[UserId] = hp.[UserId]
    WHERE u.[UserId] = @UserId;
END
GO

-- ============================================================================
-- 4. sp_Helper_GetPendingKyc
-- Purpose: Admin query to list helpers pending KYC verification.
-- ============================================================================
CREATE OR ALTER PROCEDURE [dbo].[sp_Helper_GetPendingKyc]
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        hp.[HelperId],
        hp.[UserId],
        u.[FullName],
        u.[Phone],
        u.[Email],
        hp.[KycStatus],
        hp.[HourlyRate],
        hp.[CreatedAt] AS [RegisteredAt]
    FROM [dbo].[HelperProfiles] hp
    INNER JOIN [dbo].[Users] u ON hp.[UserId] = u.[UserId]
    CROSS APPLY (SELECT u.[CreatedAt]) AS hp2(CreatedAt)
    WHERE hp.[KycStatus] = 'Pending'
    ORDER BY u.[CreatedAt] ASC;
END
GO

-- ============================================================================
-- 5. sp_Helper_ApproveKyc
-- Purpose: Admin action to approve or reject Helper KYC and set Users.IsVerified.
-- ============================================================================
CREATE OR ALTER PROCEDURE [dbo].[sp_Helper_ApproveKyc]
    @HelperId    INT,
    @KycStatus   VARCHAR(15), -- 'Approved' or 'Rejected'
    @AdminUserId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @KycStatus NOT IN ('Approved', 'Rejected')
    BEGIN
        THROW 50004, 'KycStatus must be Approved or Rejected.', 1;
    END

    IF NOT EXISTS (SELECT 1 FROM [dbo].[HelperProfiles] WHERE [HelperId] = @HelperId)
    BEGIN
        THROW 50005, 'HelperProfile not found.', 1;
    END

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @UserId INT;
        SELECT @UserId = [UserId] FROM [dbo].[HelperProfiles] WHERE [HelperId] = @HelperId;

        UPDATE [dbo].[HelperProfiles]
        SET [KycStatus] = @KycStatus
        WHERE [HelperId] = @HelperId;

        -- Update user verification flag
        UPDATE [dbo].[Users]
        SET [IsVerified] = CASE WHEN @KycStatus = 'Approved' THEN 1 ELSE 0 END
        WHERE [UserId] = @UserId;

        -- Audit log entry
        INSERT INTO [dbo].[AuditLogs] ([UserId], [Action], [Details])
        VALUES (@AdminUserId, 'KYC_STATUS_UPDATED', CONCAT('HelperId: ', @HelperId, ', UserId: ', @UserId, ', Status: ', @KycStatus));

        COMMIT TRANSACTION;

        SELECT 
            hp.[HelperId],
            hp.[UserId],
            hp.[KycStatus],
            u.[IsVerified]
        FROM [dbo].[HelperProfiles] hp
        INNER JOIN [dbo].[Users] u ON hp.[UserId] = u.[UserId]
        WHERE hp.[HelperId] = @HelperId;

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

-- ============================================================================
-- 6. sp_Helper_SetDuty
-- Purpose: Toggle IsOnDuty. Only allowed if KycStatus = 'Approved'.
-- ============================================================================
CREATE OR ALTER PROCEDURE [dbo].[sp_Helper_SetDuty]
    @HelperId INT,
    @IsOnDuty BIT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @KycStatus VARCHAR(15);
    SELECT @KycStatus = [KycStatus] FROM [dbo].[HelperProfiles] WHERE [HelperId] = @HelperId;

    IF @KycStatus IS NULL
    BEGIN
        THROW 50006, 'Helper profile not found.', 1;
    END

    IF @IsOnDuty = 1 AND @KycStatus <> 'Approved'
    BEGIN
        THROW 50007, 'Cannot go on duty: Helper KYC has not been approved yet.', 1;
    END

    UPDATE [dbo].[HelperProfiles]
    SET [IsOnDuty] = @IsOnDuty,
        [LastUpdate] = SYSUTCDATETIME()
    WHERE [HelperId] = @HelperId;

    SELECT [HelperId], [IsOnDuty], [KycStatus], [LastUpdate]
    FROM [dbo].[HelperProfiles]
    WHERE [HelperId] = @HelperId;
END
GO

-- ============================================================================
-- 7. sp_Helper_UpdateLocation
-- Purpose: Helper updates live GPS location (only applied if LocationSharing = 1).
-- ============================================================================
CREATE OR ALTER PROCEDURE [dbo].[sp_Helper_UpdateLocation]
    @HelperId INT,
    @Lat      DECIMAL(9,6),
    @Lng      DECIMAL(9,6)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE [dbo].[HelperProfiles]
    SET [LastLat] = @Lat,
        [LastLng] = @Lng,
        [LastUpdate] = SYSUTCDATETIME()
    WHERE [HelperId] = @HelperId 
      AND [LocationSharing] = 1;

    SELECT [HelperId], [LastLat], [LastLng], [LastUpdate]
    FROM [dbo].[HelperProfiles]
    WHERE [HelperId] = @HelperId;
END
GO

-- ============================================================================
-- 8. sp_Job_Create
-- Purpose: User creates a job request with Status = 'Pending'. Returns JobId.
-- ============================================================================
CREATE OR ALTER PROCEDURE [dbo].[sp_Job_Create]
    @RequesterId INT,
    @ServiceType VARCHAR(50),
    @Address     NVARCHAR(300),
    @Lat         DECIMAL(9,6),
    @Lng         DECIMAL(9,6),
    @ScheduledAt DATETIME2(7)
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM [dbo].[Users] WHERE [UserId] = @RequesterId AND [IsActive] = 1)
    BEGIN
        THROW 50008, 'Requester not found or inactive.', 1;
    END

    INSERT INTO [dbo].[Jobs] ([RequesterId], [ServiceType], [Address], [Lat], [Lng], [ScheduledAt], [Status])
    VALUES (@RequesterId, @ServiceType, @Address, @Lat, @Lng, @ScheduledAt, 'Pending');

    DECLARE @NewJobId INT = SCOPE_IDENTITY();

    SELECT 
        [JobId],
        [RequesterId],
        [ServiceType],
        [Address],
        [Lat],
        [Lng],
        [ScheduledAt],
        [Status],
        [CreatedAt]
    FROM [dbo].[Jobs]
    WHERE [JobId] = @NewJobId;
END
GO

-- ============================================================================
-- 9. sp_Job_GetNearbyHelpers
-- Purpose: Haversine distance calculation to find on-duty, approved helpers within @RadiusKm.
-- ============================================================================
CREATE OR ALTER PROCEDURE [dbo].[sp_Job_GetNearbyHelpers]
    @JobLat   DECIMAL(9,6),
    @JobLng   DECIMAL(9,6),
    @RadiusKm DECIMAL(8,2) = 5.0
AS
BEGIN
    SET NOCOUNT ON;

    -- Haversine formula calculation in Kilometers (Earth radius = 6371 km)
    SELECT 
        hp.[HelperId],
        hp.[UserId],
        u.[FullName],
        u.[Phone],
        hp.[HourlyRate],
        hp.[Rating],
        hp.[LastLat],
        hp.[LastLng],
        hp.[LastUpdate],
        ROUND(
            6371.0 * 2.0 * ASIN(
                SQRT(
                    POWER(SIN(RADIANS(@JobLat - hp.[LastLat]) / 2.0), 2) +
                    COS(RADIANS(@JobLat)) * COS(RADIANS(hp.[LastLat])) *
                    POWER(SIN(RADIANS(@JobLng - hp.[LastLng]) / 2.0), 2)
                )
            ), 2
        ) AS [DistanceKm]
    FROM [dbo].[HelperProfiles] hp
    INNER JOIN [dbo].[Users] u ON hp.[UserId] = u.[UserId]
    WHERE hp.[IsOnDuty] = 1
      AND hp.[KycStatus] = 'Approved'
      AND hp.[LocationSharing] = 1
      AND hp.[LastLat] IS NOT NULL
      AND hp.[LastLng] IS NOT NULL
      AND u.[IsActive] = 1
      AND (
            6371.0 * 2.0 * ASIN(
                SQRT(
                    POWER(SIN(RADIANS(@JobLat - hp.[LastLat]) / 2.0), 2) +
                    COS(RADIANS(@JobLat)) * COS(RADIANS(hp.[LastLat])) *
                    POWER(SIN(RADIANS(@JobLng - hp.[LastLng]) / 2.0), 2)
                )
            )
          ) <= @RadiusKm
    ORDER BY [DistanceKm] ASC;
END
GO

-- ============================================================================
-- 10. sp_Notification_Create
-- Purpose: Inserts a notification for a user.
-- ============================================================================
CREATE OR ALTER PROCEDURE [dbo].[sp_Notification_Create]
    @UserId  INT,
    @JobId   INT = NULL,
    @Message NVARCHAR(300)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO [dbo].[Notifications] ([UserId], [JobId], [Message], [IsRead])
    VALUES (@UserId, @JobId, @Message, 0);

    SELECT SCOPE_IDENTITY() AS [NotificationId];
END
GO

-- ============================================================================
-- 11. sp_Job_Accept
-- Purpose: Race-safe acceptance of a job by a verified, on-duty helper.
--          Updates Status to 'Accepted' only if still 'Pending', then creates JobAssignment.
-- ============================================================================
CREATE OR ALTER PROCEDURE [dbo].[sp_Job_Accept]
    @JobId    INT,
    @HelperId INT
AS
BEGIN
    SET NOCOUNT ON;

    -- Verify Helper is active, approved, and on duty
    IF NOT EXISTS (
        SELECT 1 
        FROM [dbo].[HelperProfiles] hp
        INNER JOIN [dbo].[Users] u ON hp.[UserId] = u.[UserId]
        WHERE hp.[HelperId] = @HelperId 
          AND hp.[KycStatus] = 'Approved' 
          AND hp.[IsOnDuty] = 1 
          AND u.[IsActive] = 1
    )
    BEGIN
        THROW 50009, 'Helper is not verified or currently not on duty.', 1;
    END

    BEGIN TRY
        BEGIN TRANSACTION;

        -- Atomically update status only if still 'Pending'
        UPDATE [dbo].[Jobs]
        SET [Status] = 'Accepted'
        WHERE [JobId] = @JobId AND [Status] = 'Pending';

        IF @@ROWCOUNT = 0
        BEGIN
            THROW 50010, 'Job is no longer available or has already been accepted.', 1;
        END

        INSERT INTO [dbo].[JobAssignments] ([JobId], [HelperId], [AcceptedAt])
        VALUES (@JobId, @HelperId, SYSUTCDATETIME());

        DECLARE @NewAssignmentId INT = SCOPE_IDENTITY();

        COMMIT TRANSACTION;

        SELECT 
            ja.[AssignmentId],
            ja.[JobId],
            ja.[HelperId],
            ja.[AcceptedAt],
            j.[Status] AS [JobStatus],
            j.[RequesterId]
        FROM [dbo].[JobAssignments] ja
        INNER JOIN [dbo].[Jobs] j ON ja.[JobId] = j.[JobId]
        WHERE ja.[AssignmentId] = @NewAssignmentId;

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

-- ============================================================================
-- 12. sp_Job_UpdateStatus
-- Purpose: Validates state transitions:
--          Accepted -> OnTheWay
--          OnTheWay -> Arrived (sets ArrivedAt)
--          Arrived -> InProgress (sets StartedAt)
--          Pending / Accepted -> Cancelled
-- ============================================================================
CREATE OR ALTER PROCEDURE [dbo].[sp_Job_UpdateStatus]
    @JobId     INT,
    @NewStatus VARCHAR(15),
    @HelperId  INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @CurrentStatus VARCHAR(15);
    DECLARE @AssignmentId INT;
    DECLARE @AssignedHelperId INT;

    SELECT 
        @CurrentStatus = j.[Status],
        @AssignmentId = ja.[AssignmentId],
        @AssignedHelperId = ja.[HelperId]
    FROM [dbo].[Jobs] j
    LEFT JOIN [dbo].[JobAssignments] ja ON j.[JobId] = ja.[JobId]
    WHERE j.[JobId] = @JobId;

    IF @CurrentStatus IS NULL
    BEGIN
        THROW 50011, 'Job not found.', 1;
    END

    -- If HelperId is provided, verify this helper is assigned
    IF @HelperId IS NOT NULL AND @AssignedHelperId IS NOT NULL AND @HelperId <> @AssignedHelperId
    BEGIN
        THROW 50012, 'Helper is not assigned to this job.', 1;
    END

    -- Validate Transitions
    IF @NewStatus = 'OnTheWay' AND @CurrentStatus <> 'Accepted'
    BEGIN
        THROW 50013, 'Cannot set OnTheWay: Job must be in Accepted status.', 1;
    END

    IF @NewStatus = 'Arrived' AND @CurrentStatus <> 'OnTheWay'
    BEGIN
        THROW 50014, 'Cannot set Arrived: Job must be in OnTheWay status.', 1;
    END

    IF @NewStatus = 'InProgress' AND @CurrentStatus <> 'Arrived'
    BEGIN
        THROW 50015, 'Cannot set InProgress: Job must be in Arrived status.', 1;
    END

    IF @NewStatus = 'Cancelled' AND @CurrentStatus NOT IN ('Pending', 'Accepted')
    BEGIN
        THROW 50016, 'Cannot cancel a job that is already in progress or completed.', 1;
    END

    BEGIN TRY
        BEGIN TRANSACTION;

        UPDATE [dbo].[Jobs]
        SET [Status] = @NewStatus
        WHERE [JobId] = @JobId;

        IF @NewStatus = 'Arrived' AND @AssignmentId IS NOT NULL
        BEGIN
            UPDATE [dbo].[JobAssignments]
            SET [ArrivedAt] = SYSUTCDATETIME()
            WHERE [AssignmentId] = @AssignmentId;
        END

        IF @NewStatus = 'InProgress' AND @AssignmentId IS NOT NULL
        BEGIN
            UPDATE [dbo].[JobAssignments]
            SET [StartedAt] = SYSUTCDATETIME()
            WHERE [AssignmentId] = @AssignmentId;
        END

        COMMIT TRANSACTION;

        SELECT 
            j.[JobId],
            j.[Status],
            ja.[AssignmentId],
            ja.[ArrivedAt],
            ja.[StartedAt]
        FROM [dbo].[Jobs] j
        LEFT JOIN [dbo].[JobAssignments] ja ON j.[JobId] = ja.[JobId]
        WHERE j.[JobId] = @JobId;

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

-- ============================================================================
-- 13. sp_Job_Complete
-- Purpose: Complete job, set EndedAt, compute WorkingMinutes and Amount, insert Payments.
-- ============================================================================
CREATE OR ALTER PROCEDURE [dbo].[sp_Job_Complete]
    @JobId    INT,
    @HelperId INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @CurrentStatus VARCHAR(15);
    DECLARE @AssignmentId INT;
    DECLARE @StartedAt DATETIME2(7);
    DECLARE @HourlyRate DECIMAL(10,2);

    SELECT 
        @CurrentStatus = j.[Status],
        @AssignmentId = ja.[AssignmentId],
        @StartedAt = ja.[StartedAt],
        @HourlyRate = hp.[HourlyRate]
    FROM [dbo].[Jobs] j
    INNER JOIN [dbo].[JobAssignments] ja ON j.[JobId] = ja.[JobId]
    INNER JOIN [dbo].[HelperProfiles] hp ON ja.[HelperId] = hp.[HelperId]
    WHERE j.[JobId] = @JobId AND ja.[HelperId] = @HelperId;

    IF @AssignmentId IS NULL
    BEGIN
        THROW 50017, 'Active assignment not found for this helper and job.', 1;
    END

    IF @CurrentStatus <> 'InProgress'
    BEGIN
        THROW 50018, 'Cannot complete job: Job status must be InProgress.', 1;
    END

    IF @StartedAt IS NULL
    BEGIN
        SET @StartedAt = SYSUTCDATETIME();
    END

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @EndedAt DATETIME2(7) = SYSUTCDATETIME();
        DECLARE @WorkingMinutes INT = DATEDIFF(MINUTE, @StartedAt, @EndedAt);

        -- Ensure minimum 1 minute for fractional edge cases
        IF @WorkingMinutes <= 0
            SET @WorkingMinutes = 1;

        DECLARE @Amount DECIMAL(10,2) = ROUND((CAST(@WorkingMinutes AS DECIMAL(10,2)) / 60.0) * @HourlyRate, 2);

        -- 1. Mark Job as Completed
        UPDATE [dbo].[Jobs]
        SET [Status] = 'Completed'
        WHERE [JobId] = @JobId;

        -- 2. Update Assignment EndedAt
        UPDATE [dbo].[JobAssignments]
        SET [EndedAt] = @EndedAt
        WHERE [AssignmentId] = @AssignmentId;

        -- 3. Insert Payment record
        INSERT INTO [dbo].[Payments] ([AssignmentId], [WorkingMinutes], [Amount], [Status])
        VALUES (@AssignmentId, @WorkingMinutes, @Amount, 'Pending');

        DECLARE @NewPaymentId INT = SCOPE_IDENTITY();

        COMMIT TRANSACTION;

        SELECT 
            @NewPaymentId AS [PaymentId],
            @AssignmentId AS [AssignmentId],
            @JobId AS [JobId],
            @WorkingMinutes AS [WorkingMinutes],
            @Amount AS [Amount],
            'Pending' AS [PaymentStatus],
            @EndedAt AS [CompletedAt];

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

-- ============================================================================
-- 14. sp_Gps_Insert
-- Purpose: Insert high-frequency GPS coordinate into GpsLogs and update helper cache.
-- ============================================================================
CREATE OR ALTER PROCEDURE [dbo].[sp_Gps_Insert]
    @AssignmentId INT,
    @Lat          DECIMAL(9,6),
    @Lng          DECIMAL(9,6),
    @SpeedKmph    DECIMAL(6,2) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO [dbo].[GpsLogs] ([AssignmentId], [Lat], [Lng], [SpeedKmph], [RecordedAt])
    VALUES (@AssignmentId, @Lat, @Lng, @SpeedKmph, SYSUTCDATETIME());

    DECLARE @NewLogId BIGINT = SCOPE_IDENTITY();

    -- Also keep helper profile latest location in sync
    UPDATE hp
    SET hp.[LastLat] = @Lat,
        hp.[LastLng] = @Lng,
        hp.[LastUpdate] = SYSUTCDATETIME()
    FROM [dbo].[HelperProfiles] hp
    INNER JOIN [dbo].[JobAssignments] ja ON hp.[HelperId] = ja.[HelperId]
    WHERE ja.[AssignmentId] = @AssignmentId;

    SELECT @NewLogId AS [LogId];
END
GO

-- ============================================================================
-- 15. sp_Gps_GetRoute
-- Purpose: Returns all logged GPS breadcrumbs for an assignment in chronological order.
-- ============================================================================
CREATE OR ALTER PROCEDURE [dbo].[sp_Gps_GetRoute]
    @AssignmentId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        [LogId],
        [AssignmentId],
        [Lat],
        [Lng],
        [SpeedKmph],
        [RecordedAt]
    FROM [dbo].[GpsLogs]
    WHERE [AssignmentId] = @AssignmentId
    ORDER BY [RecordedAt] ASC;
END
GO

-- ============================================================================
-- 16. sp_Job_GetByUser
-- Purpose: Returns all jobs created by a user, including assigned helper details.
-- ============================================================================
CREATE OR ALTER PROCEDURE [dbo].[sp_Job_GetByUser]
    @RequesterId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        j.[JobId],
        j.[RequesterId],
        j.[ServiceType],
        j.[Address],
        j.[Lat],
        j.[Lng],
        j.[ScheduledAt],
        j.[Status],
        j.[CreatedAt],
        ja.[AssignmentId],
        ja.[HelperId],
        uHelper.[FullName] AS [HelperName],
        uHelper.[Phone] AS [HelperPhone],
        hp.[Rating] AS [HelperRating],
        hp.[LastLat] AS [HelperLastLat],
        hp.[LastLng] AS [HelperLastLng],
        ja.[AcceptedAt],
        ja.[ArrivedAt],
        ja.[StartedAt],
        ja.[EndedAt],
        p.[Amount] AS [PaymentAmount],
        p.[Status] AS [PaymentStatus]
    FROM [dbo].[Jobs] j
    LEFT JOIN [dbo].[JobAssignments] ja ON j.[JobId] = ja.[JobId]
    LEFT JOIN [dbo].[HelperProfiles] hp ON ja.[HelperId] = hp.[HelperId]
    LEFT JOIN [dbo].[Users] uHelper ON hp.[UserId] = uHelper.[UserId]
    LEFT JOIN [dbo].[Payments] p ON ja.[AssignmentId] = p.[AssignmentId]
    WHERE j.[RequesterId] = @RequesterId
    ORDER BY j.[CreatedAt] DESC;
END
GO

-- ============================================================================
-- 17. sp_Job_GetByHelper
-- Purpose: Returns all jobs assigned to or accepted by a helper.
-- ============================================================================
CREATE OR ALTER PROCEDURE [dbo].[sp_Job_GetByHelper]
    @HelperId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        j.[JobId],
        j.[ServiceType],
        j.[Address],
        j.[Lat],
        j.[Lng],
        j.[ScheduledAt],
        j.[Status],
        j.[CreatedAt],
        uReq.[FullName] AS [RequesterName],
        uReq.[Phone] AS [RequesterPhone],
        ja.[AssignmentId],
        ja.[AcceptedAt],
        ja.[ArrivedAt],
        ja.[StartedAt],
        ja.[EndedAt],
        p.[WorkingMinutes],
        p.[Amount] AS [EarningsAmount],
        p.[Status] AS [PaymentStatus]
    FROM [dbo].[JobAssignments] ja
    INNER JOIN [dbo].[Jobs] j ON ja.[JobId] = j.[JobId]
    INNER JOIN [dbo].[Users] uReq ON j.[RequesterId] = uReq.[UserId]
    LEFT JOIN [dbo].[Payments] p ON ja.[AssignmentId] = p.[AssignmentId]
    WHERE ja.[HelperId] = @HelperId
    ORDER BY ja.[AcceptedAt] DESC;
END
GO

-- ============================================================================
-- 18. sp_Admin_LiveSummary
-- Purpose: Admin dashboard KPI counts: active jobs, on-duty helpers, arrived jobs, etc.
-- ============================================================================
CREATE OR ALTER PROCEDURE [dbo].[sp_Admin_LiveSummary]
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        (SELECT COUNT(1) FROM [dbo].[Jobs] WHERE [Status] IN ('Accepted', 'OnTheWay', 'Arrived', 'InProgress')) AS [ActiveJobsCount],
        (SELECT COUNT(1) FROM [dbo].[Jobs] WHERE [Status] = 'Pending') AS [PendingJobsCount],
        (SELECT COUNT(1) FROM [dbo].[Jobs] WHERE [Status] = 'Arrived') AS [ArrivedJobsCount],
        (SELECT COUNT(1) FROM [dbo].[Jobs] WHERE [Status] = 'InProgress') AS [InProgressJobsCount],
        (SELECT COUNT(1) FROM [dbo].[HelperProfiles] WHERE [IsOnDuty] = 1 AND [KycStatus] = 'Approved') AS [OnDutyHelpersCount],
        (SELECT COUNT(1) FROM [dbo].[HelperProfiles] WHERE [KycStatus] = 'Pending') AS [PendingKycCount],
        (SELECT COUNT(1) FROM [dbo].[Jobs] WHERE [Status] = 'Completed' AND CAST([CreatedAt] AS DATE) = CAST(SYSUTCDATETIME() AS DATE)) AS [CompletedTodayCount];
END
GO

-- ============================================================================
-- 19. sp_Admin_ActiveJobs
-- Purpose: Returns currently active jobs with helper name and latest GPS location for live map.
-- ============================================================================
CREATE OR ALTER PROCEDURE [dbo].[sp_Admin_ActiveJobs]
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        j.[JobId],
        j.[ServiceType],
        j.[Address],
        j.[Lat] AS [JobLat],
        j.[Lng] AS [JobLng],
        j.[Status],
        j.[ScheduledAt],
        uReq.[FullName] AS [RequesterName],
        uReq.[Phone] AS [RequesterPhone],
        ja.[AssignmentId],
        ja.[HelperId],
        uHelper.[FullName] AS [HelperName],
        uHelper.[Phone] AS [HelperPhone],
        hp.[LastLat] AS [HelperLastLat],
        hp.[LastLng] AS [HelperLastLng],
        hp.[LastUpdate] AS [HelperLastUpdate],
        ja.[AcceptedAt],
        ja.[ArrivedAt],
        ja.[StartedAt]
    FROM [dbo].[Jobs] j
    INNER JOIN [dbo].[Users] uReq ON j.[RequesterId] = uReq.[UserId]
    LEFT JOIN [dbo].[JobAssignments] ja ON j.[JobId] = ja.[JobId]
    LEFT JOIN [dbo].[HelperProfiles] hp ON ja.[HelperId] = hp.[HelperId]
    LEFT JOIN [dbo].[Users] uHelper ON hp.[UserId] = uHelper.[UserId]
    WHERE j.[Status] IN ('Accepted', 'OnTheWay', 'Arrived', 'InProgress')
    ORDER BY j.[CreatedAt] DESC;
END
GO

-- ============================================================================
-- 20. sp_Payment_GetByHelper
-- Purpose: Retrieves earnings and payment records for a specific helper.
-- ============================================================================
CREATE OR ALTER PROCEDURE [dbo].[sp_Payment_GetByHelper]
    @HelperId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        p.[PaymentId],
        p.[AssignmentId],
        j.[JobId],
        j.[ServiceType],
        p.[WorkingMinutes],
        p.[Amount],
        p.[Status],
        p.[PaidAt],
        ja.[EndedAt] AS [JobCompletedAt]
    FROM [dbo].[Payments] p
    INNER JOIN [dbo].[JobAssignments] ja ON p.[AssignmentId] = ja.[AssignmentId]
    INNER JOIN [dbo].[Jobs] j ON ja.[JobId] = j.[JobId]
    WHERE ja.[HelperId] = @HelperId
    ORDER BY p.[PaymentId] DESC;
END
GO

-- ============================================================================
-- 21. sp_Payment_MarkPaid
-- Purpose: Admin marks a helper payment as Paid. Writes to AuditLogs.
-- ============================================================================
CREATE OR ALTER PROCEDURE [dbo].[sp_Payment_MarkPaid]
    @PaymentId   INT,
    @AdminUserId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM [dbo].[Payments] WHERE [PaymentId] = @PaymentId)
    BEGIN
        THROW 50019, 'Payment record not found.', 1;
    END

    BEGIN TRY
        BEGIN TRANSACTION;

        UPDATE [dbo].[Payments]
        SET [Status] = 'Paid',
            [PaidAt] = SYSUTCDATETIME()
        WHERE [PaymentId] = @PaymentId;

        INSERT INTO [dbo].[AuditLogs] ([UserId], [Action], [Details])
        VALUES (@AdminUserId, 'PAYMENT_MARKED_PAID', CONCAT('PaymentId: ', @PaymentId, ' set to Paid'));

        COMMIT TRANSACTION;

        SELECT [PaymentId], [Status], [PaidAt]
        FROM [dbo].[Payments]
        WHERE [PaymentId] = @PaymentId;

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

-- ============================================================================
-- 22. sp_Gps_PurgeOld
-- Purpose: Scheduled data retention cleanup to purge GPS logs older than @Days.
-- ============================================================================
CREATE OR ALTER PROCEDURE [dbo].[sp_Gps_PurgeOld]
    @Days INT = 30
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @CutoffDate DATETIME2(7) = DATEADD(DAY, -@Days, SYSUTCDATETIME());

    DELETE FROM [dbo].[GpsLogs]
    WHERE [RecordedAt] < @CutoffDate;

    SELECT @@ROWCOUNT AS [DeletedLogsCount];
END
GO

-- ============================================================================
-- 23. sp_Audit_Insert
-- Purpose: Insert a security or system audit log entry.
-- ============================================================================
CREATE OR ALTER PROCEDURE [dbo].[sp_Audit_Insert]
    @UserId  INT = NULL,
    @Action  VARCHAR(50),
    @Details NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO [dbo].[AuditLogs] ([UserId], [Action], [Details])
    VALUES (@UserId, @Action, @Details);

    SELECT SCOPE_IDENTITY() AS [AuditId];
END
GO

-- ============================================================================
-- 24. sp_Job_GetById
-- Purpose: Returns full details of a single job by JobId.
-- ============================================================================
CREATE OR ALTER PROCEDURE [dbo].[sp_Job_GetById]
    @JobId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        j.[JobId],
        j.[RequesterId],
        j.[ServiceType],
        j.[Address],
        j.[Lat],
        j.[Lng],
        j.[ScheduledAt],
        j.[Status],
        j.[CreatedAt],
        ja.[AssignmentId],
        ja.[HelperId],
        uHelper.[FullName] AS [HelperName],
        uHelper.[Phone] AS [HelperPhone],
        hp.[Rating] AS [HelperRating],
        hp.[LastLat] AS [HelperLastLat],
        hp.[LastLng] AS [HelperLastLng],
        ja.[AcceptedAt],
        ja.[ArrivedAt],
        ja.[StartedAt],
        ja.[EndedAt],
        uReq.[FullName] AS [RequesterName],
        uReq.[Phone] AS [RequesterPhone],
        p.[Amount] AS [PaymentAmount],
        p.[Status] AS [PaymentStatus]
    FROM [dbo].[Jobs] j
    INNER JOIN [dbo].[Users] uReq ON j.[RequesterId] = uReq.[UserId]
    LEFT JOIN [dbo].[JobAssignments] ja ON j.[JobId] = ja.[JobId]
    LEFT JOIN [dbo].[HelperProfiles] hp ON ja.[HelperId] = hp.[HelperId]
    LEFT JOIN [dbo].[Users] uHelper ON hp.[UserId] = uHelper.[UserId]
    LEFT JOIN [dbo].[Payments] p ON ja.[AssignmentId] = p.[AssignmentId]
    WHERE j.[JobId] = @JobId;
END
GO
