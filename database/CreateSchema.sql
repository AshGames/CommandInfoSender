IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'Expediteur')
BEGIN
    EXEC('CREATE SCHEMA Expediteur');
END
GO

IF OBJECT_ID('Expediteur.JobHistory', 'U') IS NULL
BEGIN
    CREATE TABLE Expediteur.JobHistory
    (
        Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        ExecutionDate DATETIME2 NOT NULL,
        Succeeded BIT NOT NULL,
        Message NVARCHAR(1024) NOT NULL,
        OrderNumber NVARCHAR(64) NULL,
        Recipient NVARCHAR(256) NULL
    );
END
GO

IF OBJECT_ID('Expediteur.ScheduleConfiguration', 'U') IS NULL
BEGIN
    CREATE TABLE Expediteur.ScheduleConfiguration
    (
        Id INT NOT NULL CONSTRAINT PK_ScheduleConfiguration PRIMARY KEY,
        IntervalHours INT NOT NULL,
        NextExecutionUtc DATETIME2 NOT NULL,
        IsActive BIT NOT NULL
    );

    INSERT INTO Expediteur.ScheduleConfiguration (Id, IntervalHours, NextExecutionUtc, IsActive)
    VALUES (1, 4, SYSUTCDATETIME(), 1);
END
GO

IF OBJECT_ID('dbo.ObtenirAccusesCommande', 'P') IS NULL
BEGIN
    EXEC('CREATE PROCEDURE dbo.ObtenirAccusesCommande AS BEGIN SET NOCOUNT ON; END');
END
GO
