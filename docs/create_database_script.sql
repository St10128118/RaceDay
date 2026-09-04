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

------------------------------------------------------------
-- TABLE: dbo.User
------------------------------------------------------------
CREATE TABLE dbo.[User] (
    UserId INT IDENTITY(1,1) PRIMARY KEY,
    Role NVARCHAR(50) NOT NULL,
    FirstName NVARCHAR(100) NOT NULL,
    LastName NVARCHAR(100) NOT NULL,
    Email NVARCHAR(200) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(500) NOT NULL,
    Phone NVARCHAR(50),
    DateOfBirth DATE,
    Gender NVARCHAR(20),
    IdNumber NVARCHAR(50),
    EmergencyContactName NVARCHAR(200),
    EmergencyContactPhone NVARCHAR(50),
    OrganisationName NVARCHAR(200)
);
GO

------------------------------------------------------------
-- TABLE: dbo.Event
------------------------------------------------------------
CREATE TABLE dbo.Event (
    EventId INT IDENTITY(1,1) PRIMARY KEY,
    OrganiserId INT NOT NULL,
    Name NVARCHAR(200) NOT NULL,
    Description NVARCHAR(MAX),
    EventType NVARCHAR(50) NOT NULL,
    Location NVARCHAR(200) NOT NULL,
    City NVARCHAR(100) NOT NULL,
    Province NVARCHAR(100) NOT NULL,
    DistanceKm DECIMAL(6,2) NOT NULL,
    EventDateTime DATETIME2 NOT NULL,
    Status NVARCHAR(50) NOT NULL,
    MaxParticipants INT,
    FOREIGN KEY (OrganiserId) REFERENCES dbo.[User](UserId)
);
GO

------------------------------------------------------------
-- TABLE: dbo.EventCategory
------------------------------------------------------------
CREATE TABLE dbo.EventCategory (
    CategoryId INT IDENTITY(1,1) PRIMARY KEY,
    EventId INT NOT NULL,
    Name NVARCHAR(100) NOT NULL,
    DistanceKm DECIMAL(6,2),
    MinAge INT,
    MaxAge INT,
    FOREIGN KEY (EventId) REFERENCES dbo.Event(EventId)
);
GO

------------------------------------------------------------
-- TABLE: dbo.EventEnrolment
------------------------------------------------------------
CREATE TABLE dbo.EventEnrolment (
    EnrolmentId INT IDENTITY(1,1) PRIMARY KEY,
    EventId INT NOT NULL,
    CategoryId INT NOT NULL,
    ParticipantId INT NOT NULL,
    EnrolmentDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    PaymentStatus NVARCHAR(50) NOT NULL,
    BibNumber NVARCHAR(50),
    IsConfirmed BIT NOT NULL DEFAULT 0,
    FOREIGN KEY (EventId) REFERENCES dbo.Event(EventId),
    FOREIGN KEY (CategoryId) REFERENCES dbo.EventCategory(CategoryId),
    FOREIGN KEY (ParticipantId) REFERENCES dbo.[User](UserId)
);
GO
