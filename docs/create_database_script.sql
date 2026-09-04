------------------------------------------------------------
-- DROP DATABASE IF EXISTS
------------------------------------------------------------
IF EXISTS (SELECT name FROM sys.databases WHERE name = 'RaceDayDB')
BEGIN
    ALTER DATABASE RaceDayDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE RaceDayDB;
END
GO

------------------------------------------------------------
-- CREATE DATABASE
------------------------------------------------------------
CREATE DATABASE RaceDayDB;
GO

------------------------------------------------------------
-- SWITCH TO DATABASE
------------------------------------------------------------
USE RaceDayDB;
GO

------------------------------------------------------------
-- DROP TABLES IF EXISTS (reverse dependency order)
------------------------------------------------------------
IF OBJECT_ID('dbo.Result', 'U') IS NOT NULL DROP TABLE dbo.Result;
IF OBJECT_ID('dbo.EventEnrolment', 'U') IS NOT NULL DROP TABLE dbo.EventEnrolment;
IF OBJECT_ID('dbo.EventCategory', 'U') IS NOT NULL DROP TABLE dbo.EventCategory;
IF OBJECT_ID('dbo.Route', 'U') IS NOT NULL DROP TABLE dbo.Route;
IF OBJECT_ID('dbo.Event', 'U') IS NOT NULL DROP TABLE dbo.Event;
IF OBJECT_ID('dbo.User', 'U') IS NOT NULL DROP TABLE dbo.[User];
GO
