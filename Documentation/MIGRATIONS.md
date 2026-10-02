# EF Core Migrations

## Purpose

`KUKULCAN.SharedKernel.Auth` exposes an EF Core persistence model for local users, tenant memberships and federated identities.

The main Auth package is provider-neutral. The provider-specific migration packages contain only the provider integration, design-time factory and generated migration artifacts. EF Core migrations are maintained in dedicated packages for:

- PostgreSQL
- SQL Server
- MySQL

Auth does not own a tenant catalog. `TenantMemberships.TenantId` remains a reference to a tenant identifier owned by the consuming application.

## Migration Packages

Install the main Auth package and exactly one provider-specific migration package.

### PostgreSQL

```bash
dotnet add package KUKULCAN.SharedKernel.Auth
dotnet add package KUKULCAN.SharedKernel.Auth.Migrations.PostgreSQL
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
```

### SQL Server

```bash
dotnet add package KUKULCAN.SharedKernel.Auth
dotnet add package KUKULCAN.SharedKernel.Auth.Migrations.SQLServer
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
```

### MySQL

```bash
dotnet add package KUKULCAN.SharedKernel.Auth
dotnet add package KUKULCAN.SharedKernel.Auth.Migrations.MySQL
dotnet add package MySql.EntityFrameworkCore
```

The main Auth assembly remains independent of these database provider packages.

## Runtime Application

The configured `DatabaseProvider` determines the migration assembly used by `AuthDbContext`.

```text
PostgresSql -> KUKULCAN.SharedKernel.Auth.Migrations.PostgreSQL
SqlServer   -> KUKULCAN.SharedKernel.Auth.Migrations.SQLServer
MySql       -> KUKULCAN.SharedKernel.Auth.Migrations.MySQL
```

After registering `AuthDbContext` with the shared database infrastructure, a consuming application can apply pending migrations with:

```csharp
await dbContext.Database.MigrateAsync(cancellationToken);
```

The matching migration package must be present in the application output.

For production deployments, schema changes should normally be executed by an explicit deployment step rather than implicitly at application startup.

## Design-Time Factories

Each migration package contains an `IDesignTimeDbContextFactory<AuthDbContext>`.

The factories require a provider-specific environment variable:

```text
KUKULCAN_AUTH_POSTGRESQL_CONNECTION_STRING
KUKULCAN_AUTH_SQLSERVER_CONNECTION_STRING
KUKULCAN_AUTH_MYSQL_CONNECTION_STRING
```

The values are read only during EF Core design-time operations. Use environment variables or an external secret-management mechanism and never commit credentials to source control.

## Creating a New Migration

Install the EF Core CLI compatible with .NET 10:

```bash
dotnet tool install --global dotnet-ef --version 10.*
```

Create a migration in the migration project for the provider being changed.

### PostgreSQL

```bash
export KUKULCAN_AUTH_POSTGRESQL_CONNECTION_STRING='Host=localhost;Database=KukulcanAuth;Username=...;Password=...;'

dotnet ef migrations add AddAuthenticationChange \
  --project Source/KUKULCAN.SharedKernel.Auth.Migrations.PostgreSQL/KUKULCAN.SharedKernel.Auth.Migrations.PostgreSQL.csproj \
  --context AuthDbContext \
  --output-dir Migrations
```

### SQL Server

```bash
export KUKULCAN_AUTH_SQLSERVER_CONNECTION_STRING='Server=localhost;Database=KukulcanAuth;User Id=...;Password=...;TrustServerCertificate=True;'

dotnet ef migrations add AddAuthenticationChange \
  --project Source/KUKULCAN.SharedKernel.Auth.Migrations.SQLServer/KUKULCAN.SharedKernel.Auth.Migrations.SQLServer.csproj \
  --context AuthDbContext \
  --output-dir Migrations
```

### MySQL

```bash
export KUKULCAN_AUTH_MYSQL_CONNECTION_STRING='Server=localhost;Database=KukulcanAuth;User Id=...;Password=...;'

dotnet ef migrations add AddAuthenticationChange \
  --project Source/KUKULCAN.SharedKernel.Auth.Migrations.MySQL/KUKULCAN.SharedKernel.Auth.Migrations.MySQL.csproj \
  --context AuthDbContext \
  --output-dir Migrations
```

The connection string allows the design-time factory to construct the provider-specific context. The scaffolding command itself generates migration source files; it does not require an application startup database migration.

## Applying Migrations with dotnet ef

A migration package can also be used directly:

```bash
dotnet ef database update \
  --project Source/KUKULCAN.SharedKernel.Auth.Migrations.PostgreSQL/KUKULCAN.SharedKernel.Auth.Migrations.PostgreSQL.csproj \
  --context AuthDbContext
```

Use the SQL Server or MySQL migration project for the corresponding provider.

## Current Initial Schema

The initial migration creates:

```text
Users
  UserId PK
  Email UNIQUE
  PasswordHash

TenantMemberships
  UserId + TenantId PK
  UserId FK -> Users.UserId

FederatedIdentities
  Provider + Subject PK
  UserId FK -> Users.UserId
```

The current model deliberately contains no `Tenants` entity or tenant catalog table.

## Database Schema

For PostgreSQL and SQL Server, Auth tables are stored in the explicit `Auth` schema:

```text
Auth.Users
Auth.TenantMemberships
Auth.FederatedIdentities
```

The `MoveTablesToAuthSchema` migration moves the existing initial tables from the provider default schema into `Auth` without changing the table names.

MySQL treats schemas as databases rather than namespaces inside a database. Following the same provider strategy used by `KUKULCAN.SharedKernel.i18n`, the Auth model does not apply a separate `Auth` schema on MySQL; the tables remain in the database selected by the configured connection string.

The migration history table is managed separately by EF Core and is not part of the Auth table schema requirement.

## Validation

Each provider has an integration test that applies migrations to a real database, verifies there are no pending migrations afterwards, verifies connectivity and queries all three Auth sets.

These tests use PostgreSQL, SQL Server and MySQL Testcontainers.

When the Auth model changes, add a new migration to each supported provider-specific migration project. Do not edit an already-applied migration.
