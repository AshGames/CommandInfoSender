/*
    Script de base de données de démonstration pour l'application Expéditeur d'accusés de commande.
    Version sans table Client : les informations destinataire sont portées par les collaborateurs.
*/

IF DB_ID('CommandesDemo') IS NULL
BEGIN
    PRINT 'Création de la base CommandesDemo';
    EXEC('CREATE DATABASE CommandesDemo COLLATE French_CI_AS');
END
GO

USE CommandesDemo;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'Expediteur')
BEGIN
    EXEC('CREATE SCHEMA Expediteur');
END
GO

IF OBJECT_ID('dbo.Collaborateur', 'U') IS NOT NULL
    DROP TABLE dbo.Collaborateur;
GO
CREATE TABLE dbo.Collaborateur
(
    CollaborateurId INT IDENTITY(1,1) CONSTRAINT PK_Collaborateur PRIMARY KEY,
    Nom NVARCHAR(100) NOT NULL,
    Prenom NVARCHAR(100) NOT NULL,
    Email NVARCHAR(256) NOT NULL UNIQUE,
    Telephone NVARCHAR(32) NULL,
    Commentaire NVARCHAR(200) NULL,
    DateCreation DATETIME2 NOT NULL CONSTRAINT DF_Collaborateur_DateCreation DEFAULT SYSUTCDATETIME()
);
GO

IF OBJECT_ID('dbo.Produit', 'U') IS NOT NULL
    DROP TABLE dbo.Produit;
GO
CREATE TABLE dbo.Produit
(
    ProduitId INT IDENTITY(1,1) CONSTRAINT PK_Produit PRIMARY KEY,
    Reference NVARCHAR(64) NOT NULL UNIQUE,
    Libelle NVARCHAR(200) NOT NULL,
    Description NVARCHAR(400) NULL,
    PrixUnitaire DECIMAL(10,2) NOT NULL,
    TauxTva DECIMAL(5,2) NOT NULL CONSTRAINT DF_Produit_TauxTva DEFAULT 5.50
);
GO

IF OBJECT_ID('dbo.Commande', 'U') IS NOT NULL
    DROP TABLE dbo.Commande;
GO
CREATE TABLE dbo.Commande
(
    CommandeId INT IDENTITY(1,1) CONSTRAINT PK_Commande PRIMARY KEY,
    NumeroCommande NVARCHAR(50) NOT NULL UNIQUE,
    CollaborateurId INT NOT NULL,
    ClientNom NVARCHAR(150) NOT NULL,
    ClientEmail NVARCHAR(256) NOT NULL,
    DateCommande DATE NOT NULL,
    EstAccuse BIT NOT NULL CONSTRAINT DF_Commande_EstAccuse DEFAULT 0,
    Commentaire NVARCHAR(400) NULL,
    CONSTRAINT FK_Commande_Collaborateur FOREIGN KEY (CollaborateurId) REFERENCES dbo.Collaborateur(CollaborateurId)
);
GO

IF OBJECT_ID('dbo.CommandeLigne', 'U') IS NOT NULL
    DROP TABLE dbo.CommandeLigne;
GO
CREATE TABLE dbo.CommandeLigne
(
    CommandeLigneId INT IDENTITY(1,1) CONSTRAINT PK_CommandeLigne PRIMARY KEY,
    CommandeId INT NOT NULL,
    ProduitId INT NOT NULL,
    Description NVARCHAR(200) NOT NULL,
    Quantite DECIMAL(10,2) NOT NULL,
    PrixUnitaire DECIMAL(10,2) NOT NULL,
    CONSTRAINT FK_CommandeLigne_Commande FOREIGN KEY (CommandeId) REFERENCES dbo.Commande(CommandeId),
    CONSTRAINT FK_CommandeLigne_Produit FOREIGN KEY (ProduitId) REFERENCES dbo.Produit(ProduitId)
);
GO

IF OBJECT_ID('Expediteur.JobHistory', 'U') IS NOT NULL
    DROP TABLE Expediteur.JobHistory;
GO
CREATE TABLE Expediteur.JobHistory
(
    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_JobHistory PRIMARY KEY,
    ExecutionDate DATETIME2 NOT NULL,
    Succeeded BIT NOT NULL,
    Message NVARCHAR(1024) NOT NULL,
    OrderNumber NVARCHAR(64) NULL,
    Recipient NVARCHAR(256) NULL
);
GO

IF OBJECT_ID('Expediteur.ScheduleConfiguration', 'U') IS NOT NULL
    DROP TABLE Expediteur.ScheduleConfiguration;
GO
CREATE TABLE Expediteur.ScheduleConfiguration
(
    Id INT NOT NULL CONSTRAINT PK_ScheduleConfiguration PRIMARY KEY,
    IntervalHours INT NOT NULL,
    NextExecutionUtc DATETIME2 NOT NULL,
    IsActive BIT NOT NULL
);
GO

TRUNCATE TABLE Expediteur.ScheduleConfiguration;
INSERT INTO Expediteur.ScheduleConfiguration (Id, IntervalHours, NextExecutionUtc, IsActive)
VALUES (1, 4, DATEADD(HOUR, 4, SYSUTCDATETIME()), 1);
GO

TRUNCATE TABLE dbo.Collaborateur;
INSERT INTO dbo.Collaborateur (Nom, Prenom, Email, Telephone, Commentaire)
VALUES
    ('Mezda', 'Ria', 'mezdariachraf@hotmail.fr', '+33 1 23 45 67 89', 'Contacts principaux pour accusés'),
    ('Dupont', 'Élodie', 'elodie.dupont@epicesdeluxe.fr', '+33 1 98 76 54 32', 'Back-up commercial');
GO

TRUNCATE TABLE dbo.Produit;
INSERT INTO dbo.Produit (Reference, Libelle, Description, PrixUnitaire, TauxTva)
VALUES
    ('EP-CANELLE-001', 'Cannelle de Ceylan', 'Bâtons premium pour infusion et pâtisserie', 4.80, 5.50),
    ('EP-NOIX-002', 'Noix de Muscade', 'Noix entières intensité élevée', 3.90, 5.50),
    ('PT-LEVURE-010', 'Levure de boulanger', 'Levure instantanée pour viennoiseries', 2.40, 5.50),
    ('PT-SUIC-020', 'Sucre glace extra fin', 'Sucre glace spécial pâtisserie', 1.90, 5.50);
GO

TRUNCATE TABLE dbo.CommandeLigne;
TRUNCATE TABLE dbo.Commande;
GO

INSERT INTO dbo.Commande (NumeroCommande, CollaborateurId, ClientNom, ClientEmail, DateCommande, Commentaire)
VALUES
    ('CMD-20240915-001', 1, 'La Boulangerie Dorée', 'mezdariachraf@hotmail.fr', '2024-09-15', 'Commande spéciale vitrine automne'),
    ('CMD-20240918-002', 2, 'Pâtisserie Les Saveurs', 'elodie.dupont@epicesdeluxe.fr', '2024-09-18', 'Préparation atelier macarons');
GO

INSERT INTO dbo.CommandeLigne (CommandeId, ProduitId, Description, Quantite, PrixUnitaire)
VALUES
    (1, 1, 'Cannelle de Ceylan - bâtons 100g', 12, 4.80),
    (1, 2, 'Noix de muscade entières 50g', 8, 3.90),
    (1, 4, 'Sucre glace extra fin 500g', 15, 1.90),
    (2, 3, 'Levure de boulanger - sachet 125g', 20, 2.40),
    (2, 4, 'Sucre glace extra fin 500g', 25, 1.85);
GO

TRUNCATE TABLE Expediteur.JobHistory;
INSERT INTO Expediteur.JobHistory (Id, ExecutionDate, Succeeded, Message, OrderNumber, Recipient)
VALUES
    (NEWID(), DATEADD(DAY, -2, SYSUTCDATETIME()), 1, 'Traitement manuel de démonstration', 'CMD-20240901-000', 'demo@client.fr');
GO

IF OBJECT_ID('dbo.ObtenirAccusesCommande', 'P') IS NULL
    EXEC('CREATE PROCEDURE dbo.ObtenirAccusesCommande AS BEGIN SET NOCOUNT ON; END');
GO

ALTER PROCEDURE dbo.ObtenirAccusesCommande
    @DateExecution DATETIME2 = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        com.NumeroCommande,
        CONCAT(coll.Nom, ' ', coll.Prenom) AS Client,
        coll.Email AS EmailDestinataire,
        com.DateCommande,
        prod.Reference AS ReferenceProduit,
        cl.Description,
        cl.Quantite,
        cl.PrixUnitaire
    FROM dbo.Commande com
    INNER JOIN dbo.Collaborateur coll ON coll.CollaborateurId = com.CollaborateurId
    INNER JOIN dbo.CommandeLigne cl ON cl.CommandeId = com.CommandeId
    INNER JOIN dbo.Produit prod ON prod.ProduitId = cl.ProduitId
    ORDER BY com.DateCommande DESC, com.NumeroCommande, prod.Reference;
END
GO

PRINT 'Base CommandesDemo prête avec données de test (sans table Client).';
GO
