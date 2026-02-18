# API Aggregator - Multi-Project Architecture Plan

## Overview
Implement a multi-project architecture following SOLID principles with Repository → Business → Controller pattern for the API Aggregator application.

---

## Project Structure (4 new projects)

```
proyecto.sln
├── proyecto.Core      (Class Library) - Interfaces, Entities, DTOs, Enums
├── proyecto.Data      (Class Library) - DbContext, Repositories, UnitOfWork
├── proyecto.Business  (Class Library) - Services, Parsers, Validators
├── proyecto.API       (Existing)      - Controllers, Middleware, Auth
└── proyecto.Web       (Existing)      - MVC Frontend (calls API via HttpClient)
```

### Project References
```
Core         ← No dependencies
Data         ← Core
Business     ← Core, Data
API          ← Core, Business
Web          ← Core (for DTOs only, communicates with API via HTTP)
```

---

## Phase 1: proyecto.Core

### Entities (`/Entities`)
- `Source.cs` - API source configuration
- `SourceItem.cs` - Ingested data items
- `Secret.cs` - Encrypted API keys/tokens

### Enums (`/Enums`)
- `ComponentType.cs` - Widget, Api, Feed
- `AuthenticationType.cs` - None, ApiKey, Bearer, Basic

### Interfaces (`/Interfaces/Repositories`)
- `IRepository<T>` - Generic CRUD operations
- `ISourceRepository` - Source-specific methods
- `ISourceItemRepository` - Item-specific methods
- `ISecretRepository` - Secret-specific methods
- `IUnitOfWork` - Transaction management

### Interfaces (`/Interfaces/Services`)
- `ISourceService`
- `ISourceItemService`
- `ISecretService`
- `IIngestionService`

### DTOs (`/DTOs`)
- `/Sources` - SourceDto, CreateSourceDto, UpdateSourceDto
- `/SourceItems` - SourceItemDto, SourceItemExportDto
- `/Common` - PagedResultDto<T>, ApiResponseDto<T>

### Exceptions (`/Exceptions`)
- NotFoundException, ValidationException

---

## Phase 2: proyecto.Data

### DbContext (`/Context`)
- `ApplicationDbContext.cs`

### Configurations (`/Configurations`)
- `SourceConfiguration.cs`
- `SourceItemConfiguration.cs`
- `SecretConfiguration.cs`

### Repositories (`/Repositories`)
- `RepositoryBase<T>.cs` - Generic implementation
- `SourceRepository.cs`
- `SourceItemRepository.cs`
- `SecretRepository.cs`

### UnitOfWork (`/UnitOfWork`)
- `UnitOfWork.cs` - Manages all repositories + transactions

---

## Phase 3: proyecto.Business

### Services (`/Services`)
- `SourceService.cs`
- `SourceItemService.cs`
- `SecretService.cs`
- `IngestionService.cs`

### Parsers (`/Parsers`)
- `IDataParser.cs`
- `JsonDataParser.cs`
- `XmlDataParser.cs`
- `HtmlDataParser.cs`
- `DataParserFactory.cs`

### Encryption (`/Encryption`)
- `IEncryptionService.cs`
- `AesEncryptionService.cs`

### HttpClients (`/HttpClients`)
- `IExternalApiClient.cs`
- `ExternalApiClient.cs`

### Mappers (`/Mappers`)
- `MappingProfile.cs` - AutoMapper configuration

---

## Phase 4: proyecto.API (Modify Existing)

### Controllers (`/Controllers`)
- `SourcesController.cs` - CRUD for sources
- `SourceItemsController.cs` - CRUD for items
- `IngestionController.cs` - Trigger data ingestion
- `ExportController.cs` - Download/Upload JSON

### Middleware (`/Middleware`)
- `ExceptionHandlingMiddleware.cs`

### Configuration
- Swagger
- CORS for Web frontend
- DI registration for all services

---

## Phase 5: proyecto.Web (Modify Existing)

### Services (`/Services`)
- `IApiClient.cs`
- `ApiClient.cs` - HttpClient wrapper for API calls

### Controllers (Modify)
- `ApisController.cs` - Call API for source management
- `CatalogController.cs` - Call API for items
- `HomeController.cs` - Call API for dashboard stats

### Configuration
- HttpClient registration

---

## NuGet Packages

### proyecto.Data
```xml
<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="9.0.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="9.0.0" />
```

### proyecto.Business
```xml
<PackageReference Include="AutoMapper.Extensions.Microsoft.DependencyInjection" Version="12.0.1" />
<PackageReference Include="HtmlAgilityPack" Version="1.11.61" />
```

### proyecto.API
```xml
<PackageReference Include="Swashbuckle.AspNetCore" Version="7.0.0" />
```

---

## Database Schema

```sql
-- Sources
CREATE TABLE Sources (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Url NVARCHAR(500) NOT NULL,
    Name NVARCHAR(200) NOT NULL UNIQUE,
    Description NVARCHAR(500) NULL,
    ComponentType NVARCHAR(100) NOT NULL,
    AuthenticationType NVARCHAR(50) NOT NULL DEFAULT 'None',
    Endpoint NVARCHAR(500) NULL,
    RequiresSecret BIT NOT NULL DEFAULT 0,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL,
    UpdatedAt DATETIME2 NULL
);

-- SourceItems
CREATE TABLE SourceItems (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    SourceId INT NOT NULL REFERENCES Sources(Id) ON DELETE CASCADE,
    Json NVARCHAR(MAX) NOT NULL,
    Name NVARCHAR(200) NULL,
    RecordCount INT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL
);

-- Secrets
CREATE TABLE Secrets (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    SourceId INT NOT NULL UNIQUE REFERENCES Sources(Id) ON DELETE CASCADE,
    EncryptedValue NVARCHAR(MAX) NOT NULL,
    KeyName NVARCHAR(100) NULL,
    CreatedAt DATETIME2 NOT NULL,
    UpdatedAt DATETIME2 NULL
);
```

---

## Implementation Order

1. Create proyecto.Core (entities, interfaces, DTOs)
2. Create proyecto.Data (DbContext, repositories, migrations)
3. Create proyecto.Business (services, parsers)
4. Modify proyecto.API (controllers, DI setup)
5. Modify proyecto.Web (ApiClient, update controllers)
6. Add authentication (Phase 2)

---

## Verification

1. Run `dotnet build` to verify solution compiles
2. Run EF migration: `dotnet ef migrations add InitialCreate -p proyecto.Data -s proyecto.API`
3. Update database: `dotnet ef database update -p proyecto.Data -s proyecto.API`
4. Test API with Swagger at `/swagger`
5. Test source CRUD operations
6. Test data ingestion from external API
