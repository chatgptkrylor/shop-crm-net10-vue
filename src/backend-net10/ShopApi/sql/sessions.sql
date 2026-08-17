-- Additive: server-side sessions (spec: initialize user session + 20-min timeout).
-- Safe to re-run. Does NOT drop Users/Customers/Interactions.
USE ShopCRM;
GO

IF OBJECT_ID(N'dbo.Sessions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Sessions (
        Id         NVARCHAR(64)  NOT NULL PRIMARY KEY,
        UserId     INT           NOT NULL,
        Username   NVARCHAR(50)  NOT NULL,
        Role       NVARCHAR(20)  NOT NULL,
        CreatedAt  DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
        ExpiresAt  DATETIME2     NOT NULL,
        CONSTRAINT FK_Sessions_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(Id)
    );
END
GO
