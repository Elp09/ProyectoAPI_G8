-- ============================================================
-- Script de creación de base de datos: proyectoDbData
-- Curso SC-701 Programacion Avanzada Web - Grupo 8
-- Esquema de normalización: edu.univ.ingest.v1
-- ============================================================

-- Crear la base de datos si no existe
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'proyectoDbData')
BEGIN
    CREATE DATABASE proyectoDbData;
END
GO

USE proyectoDbData;
GO

-- ============================================================
-- SOURCES
-- Fuentes de datos configuradas (APIs, feeds, websites)
-- ============================================================
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Sources' AND xtype='U')
BEGIN
    CREATE TABLE Sources (
        Id              INT             IDENTITY(1,1)   PRIMARY KEY,
        Name            NVARCHAR(200)   NOT NULL,
        Url             NVARCHAR(500)   NOT NULL,
        Description     NVARCHAR(500)   NULL,
        ComponentType   NVARCHAR(100)   NOT NULL DEFAULT 'api', -- 'api', 'feed', 'website', 'widget'
        RequiresSecret  BIT             NOT NULL DEFAULT 0,
        AuthType        NVARCHAR(50)    NOT NULL DEFAULT 'none', -- 'none', 'apikey', 'bearer', 'basic'
        Endpoint        NVARCHAR(500)   NULL,
        CreatedAt       DATETIME        NOT NULL DEFAULT GETDATE()
    );
END
GO

-- ============================================================
-- SOURCEITEMS
-- Items normalizados y guardados por el usuario.
-- El campo Json almacena SIEMPRE el esquema edu.univ.ingest.v1:
-- {
--   "schemaVersion": "edu.univ.ingest.v1",
--   "exportedAt": "...",
--   "source": { "id", "name", "type", "url", "requiresSecret" },
--   "normalized": { "id", "externalId", "title", "content",
--                   "summary", "publishedAt", "url", "author",
--                   "language", "category": { "primary", "secondary" } },
--   "raw": { "format", "data": { "original": ... } }
-- }
-- ============================================================
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='SourceItems' AND xtype='U')
BEGIN
    CREATE TABLE SourceItems (
        Id              INT             IDENTITY(1,1)   PRIMARY KEY,
        SourceId        INT             NOT NULL,
        Json            NVARCHAR(MAX)   NOT NULL,
        CreatedAt       DATETIME        NOT NULL DEFAULT GETDATE(),
        Endpoint        NVARCHAR(500)   NULL,
        IsLocalUpload   BIT             NOT NULL DEFAULT 0,
        SavedBy         NVARCHAR(256)   NULL,   -- email del usuario que guardó el item

        CONSTRAINT FK_SourceItems_Sources
            FOREIGN KEY (SourceId) REFERENCES Sources(Id)
            ON DELETE CASCADE
    );
END
GO

-- ============================================================
-- SECRETS
-- Credenciales y API keys asociadas a fuentes
-- IMPORTANTE: en producción KeyValue debe almacenarse encriptado
-- ============================================================
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Secrets' AND xtype='U')
BEGIN
    CREATE TABLE Secrets (
        Id          INT             IDENTITY(1,1)   PRIMARY KEY,
        SourceId    INT             NULL,
        KeyName     NVARCHAR(200)   NOT NULL,
        KeyValue    NVARCHAR(MAX)   NOT NULL,   -- encriptar antes de guardar
        CreatedAt   DATETIME        NOT NULL DEFAULT GETDATE(),

        CONSTRAINT FK_Secrets_Sources
            FOREIGN KEY (SourceId) REFERENCES Sources(Id)
            ON DELETE SET NULL
    );
END
GO

-- ============================================================
-- ÍNDICES para mejorar rendimiento de consultas frecuentes
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SourceItems_SourceId')
    CREATE INDEX IX_SourceItems_SourceId ON SourceItems(SourceId);

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SourceItems_CreatedAt')
    CREATE INDEX IX_SourceItems_CreatedAt ON SourceItems(CreatedAt DESC);

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Secrets_SourceId')
    CREATE INDEX IX_Secrets_SourceId ON Secrets(SourceId);
GO

-- ============================================================
-- Nota: Las tablas de Identity (AspNetUsers, AspNetRoles, etc.)
-- son creadas automáticamente por las migraciones de EF Core.
-- Ejecutar: dotnet ef database update (desde proyecto.API)
-- ============================================================

PRINT 'Base de datos proyectoDbData creada exitosamente.';
GO
