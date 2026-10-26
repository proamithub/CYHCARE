-- ============================================================================
-- CYH Care — Phase 1: Database Tables & Schema Setup
-- Target: SQL Server
-- Database: CYHCare
-- ============================================================================

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'CYHCare')
BEGIN
    CREATE DATABASE [CYHCare];
END
GO

USE [CYHCare];
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-- ============================================================================
-- 1. Table: Users
-- Description: Core authentication and profile table for Users, Helpers & Admins
-- ============================================================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Users]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Users] (
        [UserId]       INT            IDENTITY(1,1) NOT NULL,
        [FullName]     NVARCHAR(100)  NOT NULL,
        [Phone]        VARCHAR(15)    NOT NULL,
        [Email]        NVARCHAR(150)  NOT NULL,
        [PasswordHash] NVARCHAR(200)  NOT NULL,
        [Role]         VARCHAR(10)    NOT NULL,
        [IsVerified]   BIT            NOT NULL CONSTRAINT [DF_Users_IsVerified] DEFAULT (0),
        [IsActive]     BIT            NOT NULL CONSTRAINT [DF_Users_IsActive] DEFAULT (1),
        [CreatedAt]    DATETIME2(7)   NOT NULL CONSTRAINT [DF_Users_CreatedAt] DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_Users] PRIMARY KEY CLUSTERED ([UserId] ASC),
        CONSTRAINT [UQ_Users_Phone] UNIQUE NONCLUSTERED ([Phone] ASC),
        CONSTRAINT [UQ_Users_Email] UNIQUE NONCLUSTERED ([Email] ASC),
        CONSTRAINT [CK_Users_Role] CHECK ([Role] IN ('User', 'Helper', 'Admin'))
    );
END
GO

-- ============================================================================
-- 2. Table: HelperProfiles
-- Description: Extended profile for Helper role (KYC status, duty, live coordinates)
-- ============================================================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[HelperProfiles]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[HelperProfiles] (
        [HelperId]        INT            IDENTITY(1,1) NOT NULL,
        [UserId]          INT            NOT NULL,
        [KycStatus]       VARCHAR(15)    NOT NULL CONSTRAINT [DF_HelperProfiles_KycStatus] DEFAULT ('Pending'),
        [IsOnDuty]        BIT            NOT NULL CONSTRAINT [DF_HelperProfiles_IsOnDuty] DEFAULT (0),
        [LocationSharing] BIT            NOT NULL CONSTRAINT [DF_HelperProfiles_LocationSharing] DEFAULT (1),
        [LastLat]         DECIMAL(9,6)   NULL,
        [LastLng]         DECIMAL(9,6)   NULL,
        [LastUpdate]      DATETIME2(7)   NULL,
        [HourlyRate]      DECIMAL(10,2)  NOT NULL CONSTRAINT [DF_HelperProfiles_HourlyRate] DEFAULT (100.00),
        [Rating]          DECIMAL(3,2)   NOT NULL CONSTRAINT [DF_HelperProfiles_Rating] DEFAULT (0.00),
        CONSTRAINT [PK_HelperProfiles] PRIMARY KEY CLUSTERED ([HelperId] ASC),
        CONSTRAINT [UQ_HelperProfiles_UserId] UNIQUE NONCLUSTERED ([UserId] ASC),
        CONSTRAINT [FK_HelperProfiles_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([UserId]) ON DELETE CASCADE,
        CONSTRAINT [CK_HelperProfiles_KycStatus] CHECK ([KycStatus] IN ('Pending', 'Approved', 'Rejected'))
    );
END
GO

-- ============================================================================
-- 3. Table: Jobs
-- Description: Assistance requests posted by Requesters (Users)
-- ============================================================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Jobs]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Jobs] (
        [JobId]        INT            IDENTITY(1,1) NOT NULL,
        [RequesterId]  INT            NOT NULL,
        [ServiceType]  VARCHAR(50)    NOT NULL,
        [Address]      NVARCHAR(300)  NOT NULL,
        [Lat]          DECIMAL(9,6)   NOT NULL,
        [Lng]          DECIMAL(9,6)   NOT NULL,
        [ScheduledAt]  DATETIME2(7)   NOT NULL,
        [Status]       VARCHAR(15)    NOT NULL CONSTRAINT [DF_Jobs_Status] DEFAULT ('Pending'),
        [CreatedAt]    DATETIME2(7)   NOT NULL CONSTRAINT [DF_Jobs_CreatedAt] DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_Jobs] PRIMARY KEY CLUSTERED ([JobId] ASC),
        CONSTRAINT [FK_Jobs_Users] FOREIGN KEY ([RequesterId]) REFERENCES [dbo].[Users] ([UserId]),
        CONSTRAINT [CK_Jobs_Status] CHECK ([Status] IN ('Pending', 'Accepted', 'OnTheWay', 'Arrived', 'InProgress', 'Completed', 'Cancelled'))
    );
END
GO

-- ============================================================================
-- 4. Table: JobAssignments
-- Description: Tracks helper assignment, lifecycle timestamps per job
-- ============================================================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[JobAssignments]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[JobAssignments] (
        [AssignmentId] INT          IDENTITY(1,1) NOT NULL,
        [JobId]        INT          NOT NULL,
        [HelperId]     INT          NOT NULL,
        [AcceptedAt]   DATETIME2(7) NOT NULL CONSTRAINT [DF_JobAssignments_AcceptedAt] DEFAULT (SYSUTCDATETIME()),
        [ArrivedAt]    DATETIME2(7) NULL,
        [StartedAt]    DATETIME2(7) NULL,
        [EndedAt]      DATETIME2(7) NULL,
        CONSTRAINT [PK_JobAssignments] PRIMARY KEY CLUSTERED ([AssignmentId] ASC),
        CONSTRAINT [UQ_JobAssignments_JobId] UNIQUE NONCLUSTERED ([JobId] ASC),
        CONSTRAINT [FK_JobAssignments_Jobs] FOREIGN KEY ([JobId]) REFERENCES [dbo].[Jobs] ([JobId]),
        CONSTRAINT [FK_JobAssignments_HelperProfiles] FOREIGN KEY ([HelperId]) REFERENCES [dbo].[HelperProfiles] ([HelperId])
    );
END
GO

-- ============================================================================
-- 5. Table: GpsLogs
-- Description: High-frequency GPS waypoint logs for active helper during assignment
-- ============================================================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[GpsLogs]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[GpsLogs] (
        [LogId]        BIGINT        IDENTITY(1,1) NOT NULL,
        [AssignmentId] INT           NOT NULL,
        [Lat]          DECIMAL(9,6)  NOT NULL,
        [Lng]          DECIMAL(9,6)  NOT NULL,
        [SpeedKmph]    DECIMAL(6,2)  NULL,
        [RecordedAt]   DATETIME2(7)  NOT NULL CONSTRAINT [DF_GpsLogs_RecordedAt] DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_GpsLogs] PRIMARY KEY CLUSTERED ([LogId] ASC),
        CONSTRAINT [FK_GpsLogs_JobAssignments] FOREIGN KEY ([AssignmentId]) REFERENCES [dbo].[JobAssignments] ([AssignmentId]) ON DELETE CASCADE
    );
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_GpsLogs_Assignment' AND object_id = OBJECT_ID(N'[dbo].[GpsLogs]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_GpsLogs_Assignment]
        ON [dbo].[GpsLogs] ([AssignmentId] ASC, [RecordedAt] ASC);
END
GO

-- ============================================================================
-- 6. Table: Payments
-- Description: Financial compensation calculated on completed job working minutes
-- ============================================================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Payments]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Payments] (
        [PaymentId]      INT           IDENTITY(1,1) NOT NULL,
        [AssignmentId]   INT           NOT NULL,
        [WorkingMinutes] INT           NOT NULL,
        [Amount]         DECIMAL(10,2) NOT NULL,
        [Status]         VARCHAR(10)   NOT NULL CONSTRAINT [DF_Payments_Status] DEFAULT ('Pending'),
        [PaidAt]         DATETIME2(7)  NULL,
        CONSTRAINT [PK_Payments] PRIMARY KEY CLUSTERED ([PaymentId] ASC),
        CONSTRAINT [UQ_Payments_AssignmentId] UNIQUE NONCLUSTERED ([AssignmentId] ASC),
        CONSTRAINT [FK_Payments_JobAssignments] FOREIGN KEY ([AssignmentId]) REFERENCES [dbo].[JobAssignments] ([AssignmentId]),
        CONSTRAINT [CK_Payments_Status] CHECK ([Status] IN ('Pending', 'Paid', 'Failed', 'Cancelled'))
    );
END
GO

-- ============================================================================
-- 7. Table: Notifications
-- Description: User and helper real-time alerts and inbox notifications
-- ============================================================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Notifications]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Notifications] (
        [NotificationId] INT            IDENTITY(1,1) NOT NULL,
        [UserId]         INT            NOT NULL,
        [JobId]          INT            NULL,
        [Message]        NVARCHAR(300)  NOT NULL,
        [IsRead]         BIT            NOT NULL CONSTRAINT [DF_Notifications_IsRead] DEFAULT (0),
        [SentAt]         DATETIME2(7)   NOT NULL CONSTRAINT [DF_Notifications_SentAt] DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_Notifications] PRIMARY KEY CLUSTERED ([NotificationId] ASC),
        CONSTRAINT [FK_Notifications_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([UserId]) ON DELETE CASCADE,
        CONSTRAINT [FK_Notifications_Jobs] FOREIGN KEY ([JobId]) REFERENCES [dbo].[Jobs] ([JobId]) ON DELETE NO ACTION
    );
END
GO

-- ============================================================================
-- 8. Table: AuditLogs
-- Description: Security and compliance audit trail
-- ============================================================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[AuditLogs]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[AuditLogs] (
        [AuditId]   BIGINT         IDENTITY(1,1) NOT NULL,
        [UserId]    INT            NULL,
        [Action]    VARCHAR(50)    NOT NULL,
        [Details]   NVARCHAR(500)  NULL,
        [CreatedAt] DATETIME2(7)   NOT NULL CONSTRAINT [DF_AuditLogs_CreatedAt] DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_AuditLogs] PRIMARY KEY CLUSTERED ([AuditId] ASC)
    );
END
GO

-- ============================================================================
-- High-Performance Database Indexes (Speed Optimization)
-- ============================================================================
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_Jobs_Requester_CreatedAt' AND object_id = OBJECT_ID(N'[dbo].[Jobs]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Jobs_Requester_CreatedAt]
        ON [dbo].[Jobs] ([RequesterId] ASC, [CreatedAt] DESC)
        INCLUDE ([Status], [ServiceType], [Address], [Lat], [Lng], [ScheduledAt]);
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_Jobs_Status' AND object_id = OBJECT_ID(N'[dbo].[Jobs]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Jobs_Status]
        ON [dbo].[Jobs] ([Status] ASC, [CreatedAt] DESC)
        INCLUDE ([JobId], [RequesterId], [ServiceType], [Address], [Lat], [Lng]);
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_HelperProfiles_Nearby' AND object_id = OBJECT_ID(N'[dbo].[HelperProfiles]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_HelperProfiles_Nearby]
        ON [dbo].[HelperProfiles] ([IsOnDuty] ASC, [KycStatus] ASC, [LocationSharing] ASC)
        INCLUDE ([UserId], [HourlyRate], [Rating], [LastLat], [LastLng], [LastUpdate]);
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_JobAssignments_Helper' AND object_id = OBJECT_ID(N'[dbo].[JobAssignments]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_JobAssignments_Helper]
        ON [dbo].[JobAssignments] ([HelperId] ASC, [AcceptedAt] DESC)
        INCLUDE ([JobId], [ArrivedAt], [StartedAt], [EndedAt]);
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_Notifications_User_Read' AND object_id = OBJECT_ID(N'[dbo].[Notifications]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Notifications_User_Read]
        ON [dbo].[Notifications] ([UserId] ASC, [IsRead] ASC, [SentAt] DESC);
END
GO
