# CYH Care — Database Setup Guide (Phase 1)

This folder contains the complete SQL Server database setup scripts for **CYH Care ("People Help People")**.

In accordance with architectural principles:
- **100% Stored Procedures** for all CRUD and business logic.
- **Zero Inline SQL** in application code.
- **Dapper + Stored Procedures ONLY** (No EF Core).

---

## 📂 File Sequence

Execute the SQL scripts in this exact order:

| Step | File | Description |
|---|---|---|
| **1** | [`01_Tables.sql`](01_Tables.sql) | Creates `CYHCare` database and all 8 tables with primary keys, foreign keys, indexes, and constraints. |
| **2** | [`02_StoredProcedures.sql`](02_StoredProcedures.sql) | Creates all 23 stored procedures (Registration, Haversine nearby search, Race-safe job acceptance, GPS logs, Payments, Admin summaries). |
| **3** | [`03_Seed.sql`](03_Seed.sql) | Inserts initial verified Admin account and initial system audit record. |

---

## 🛠️ How to Run in SSMS (SQL Server Management Studio)

1. Open **SQL Server Management Studio (SSMS)**.
2. Connect to your SQL Server instance (e.g., `.` or `localhost` or `SQLEXPRESS`).
3. Open [`01_Tables.sql`](01_Tables.sql) (`File -> Open -> File...` or press `Ctrl + O`).
4. Click **Execute** (or press `F5`).
5. Open [`02_StoredProcedures.sql`](02_StoredProcedures.sql) and press `F5`.
6. Open [`03_Seed.sql`](03_Seed.sql) and press `F5`.

---

## ⚡ How to Run via PowerShell (`sqlcmd`)

If you prefer running via command line on your local machine:

```powershell
# Adjust Server name if needed (e.g. localhost or localhost\SQLEXPRESS)
$server = "localhost\SQLEXPRESS"

sqlcmd -S $server -E -i "Database\01_Tables.sql"
sqlcmd -S $server -E -i "Database\02_StoredProcedures.sql"
sqlcmd -S $server -E -i "Database\03_Seed.sql"
```

---

## 👤 Seeded Admin Credentials

* **Email:** `admin@cyhcare.com`
* **Password:** `Admin@123`
* **Role:** `Admin`
* **Status:** Verified (`IsVerified = 1`), Active (`IsActive = 1`)

---

## 🔍 Verification Queries

Run the following queries in SSMS to verify the installation:

```sql
USE [CYHCare];
GO

-- 1. Check all tables
SELECT TABLE_NAME 
FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_TYPE = 'BASE TABLE'
ORDER BY TABLE_NAME;

-- Expected 8 tables:
-- AuditLogs, GpsLogs, HelperProfiles, JobAssignments, Jobs, Notifications, Payments, Users

-- 2. Check all stored procedures
SELECT ROUTINE_NAME 
FROM INFORMATION_SCHEMA.ROUTINES 
WHERE ROUTINE_TYPE = 'PROCEDURE'
ORDER BY ROUTINE_NAME;

-- 3. Verify Admin seed
SELECT UserId, FullName, Email, Role, IsVerified, IsActive, CreatedAt 
FROM dbo.Users 
WHERE Email = 'admin@cyhcare.com';
```
