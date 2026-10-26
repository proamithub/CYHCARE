-- ============================================================================
-- CYH Care — Phase 1: Seed Data
-- Target: SQL Server
-- Database: CYHCare
-- ============================================================================

USE [CYHCare];
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-- ============================================================================
-- Seed 1: Default Admin User
-- Email: admin@cyhcare.com
-- Default Password: Admin@123 (BCrypt hash)
-- ============================================================================
IF NOT EXISTS (SELECT 1 FROM [dbo].[Users] WHERE [Email] = 'admin@cyhcare.com')
BEGIN
    INSERT INTO [dbo].[Users] 
    (
        [FullName],
        [Phone],
        [Email],
        [PasswordHash],
        [Role],
        [IsVerified],
        [IsActive],
        [CreatedAt]
    )
    VALUES 
    (
        N'CYH Care Admin',
        '9999999999',
        N'admin@cyhcare.com',
        -- BCrypt hash for: Admin@123 (WorkFactor 11)
        N'$2a$11$jE1e8wQzP/P/n9E35pG9j.yCq9P5vV3X/a4C0T4A/vQ2w9q/u7Icy',
        'Admin',
        1,
        1,
        SYSUTCDATETIME()
    );

    PRINT 'Default Admin user seeded successfully (admin@cyhcare.com).';
END
ELSE
BEGIN
    PRINT 'Admin user already exists. Skipping seed.';
END
GO

-- ============================================================================
-- Seed Audit Log
-- ============================================================================
IF NOT EXISTS (SELECT 1 FROM [dbo].[AuditLogs] WHERE [Action] = 'DATABASE_SEEDED')
BEGIN
    INSERT INTO [dbo].[AuditLogs] ([UserId], [Action], [Details])
    VALUES (NULL, 'DATABASE_SEEDED', N'Database schema, stored procedures, and initial admin seeded.');
END
GO
