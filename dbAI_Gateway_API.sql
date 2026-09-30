USE master;
GO
IF EXISTS (SELECT * FROM sys.databases WHERE name = 'dbAIGATEWAYAPI')
BEGIN
    ALTER DATABASE dbAIGATEWAYAPI SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE dbAIGATEWAYAPI;
END
GO

CREATE DATABASE dbAIGATEWAYAPI;
GO
USE dbAIGATEWAYAPI;
GO
-- 1. Bảng Users
CREATE TABLE Users (
    Id int IDENTITY(1,1) PRIMARY KEY,
    Email varchar(255) NOT NULL UNIQUE,
    PasswordHash varchar(50) NOT NULL,
    FullName nvarchar(150) NOT NULL,
    Status varchar(20) NOT NULL DEFAULT 'Active',
    CreatedAt datetime2 NOT NULL DEFAULT GETUTCDATE()
);
CREATE UNIQUE INDEX IX_Users_Email ON Users(Email);

-- 2. Bảng Roles
CREATE TABLE Roles (
    Id int IDENTITY(1,1) PRIMARY KEY,
    Name varchar(50) NOT NULL UNIQUE
);

-- Seed dữ liệu ban đầu cho Roles
INSERT INTO Roles (Name) VALUES ('User'), ('Admin');

-- 3. Bảng UserRoles
CREATE TABLE UserRoles (
    UserId int NOT NULL,
    RoleId int NOT NULL,
    PRIMARY KEY (UserId, RoleId),
    FOREIGN KEY (UserId) REFERENCES Users(Id),
    FOREIGN KEY (RoleId) REFERENCES Roles(Id)
);

-- 4. Bảng RefreshTokens
CREATE TABLE RefreshTokens (
    Id int IDENTITY(1,1) PRIMARY KEY,
    UserId int NOT NULL,
    TokenHash varchar(500) NOT NULL UNIQUE,
    FamilyId uniqueidentifier NOT NULL,
    CreatedAt datetime2 NOT NULL,
    ExpiresAt datetime2 NOT NULL,
    RevokedAt datetime2 NULL,
    FOREIGN KEY (UserId) REFERENCES Users(Id)
);
CREATE UNIQUE INDEX IX_RefreshTokens_TokenHash ON RefreshTokens(TokenHash);
CREATE INDEX IX_RefreshTokens_UserId ON RefreshTokens(UserId);
CREATE INDEX IX_RefreshTokens_FamilyId ON RefreshTokens(FamilyId);

-- 5. Bảng Conversations
CREATE TABLE Conversations (
    Id int IDENTITY(1,1) PRIMARY KEY,
    UserId int NOT NULL,
    Title nvarchar(200) NULL,
    CreatedAt datetime2 NOT NULL,
    UpdatedAt datetime2 NOT NULL,
    FOREIGN KEY (UserId) REFERENCES Users(Id)
);
CREATE INDEX IX_Conversations_UserId ON Conversations(UserId);
CREATE INDEX IX_Conversations_UserId_UpdatedAt ON Conversations(UserId, UpdatedAt);

-- 6. Bảng Messages
CREATE TABLE Messages (
    Id int IDENTITY(1,1) PRIMARY KEY,
    ConversationId int NOT NULL,
    Role varchar(20) NOT NULL,
    Content nvarchar(max) NOT NULL,
    Model varchar(100) NULL,
    CreatedAt datetime2 NOT NULL,
    FOREIGN KEY (ConversationId) REFERENCES Conversations(Id) ON DELETE CASCADE
);
CREATE INDEX IX_Messages_ConversationId_CreatedAt ON Messages(ConversationId, CreatedAt);

-- 7. Bảng AiRequestLogs
CREATE TABLE AiRequestLogs (
    Id int IDENTITY(1,1) PRIMARY KEY,
    UserId int NOT NULL,
    ConversationId int NULL,
    Provider varchar(50) NOT NULL,
    Model varchar(100) NOT NULL,
    RequestedAt datetime2 NOT NULL,
    LatencyMs bigint NULL,
    InputTokens int NULL,
    OutputTokens int NULL,
    Status varchar(20) NOT NULL,
    ErrorCode varchar(100) NULL,
    FOREIGN KEY (UserId) REFERENCES Users(Id),
    FOREIGN KEY (ConversationId) REFERENCES Conversations(Id)
);
CREATE INDEX IX_AiRequestLogs_UserId_RequestedAt ON AiRequestLogs(UserId, RequestedAt);
CREATE INDEX IX_AiRequestLogs_Model_RequestedAt ON AiRequestLogs(Model, RequestedAt);
CREATE INDEX IX_AiRequestLogs_Status_RequestedAt ON AiRequestLogs(Status, RequestedAt);
CREATE INDEX IX_AiRequestLogs_ConversationId ON AiRequestLogs(ConversationId);

Select * from Users
Select * from RefreshTokens
Select * from Conversations
Select * from AiRequestLogs
Select * from Messages