# EF Core Migrations

## Purpose

`KUKULCAN.SharedKernel.Auth` exposes an EF Core persistence model for local users, tenant memberships and federated identities.

The main Auth package is deliberately **provider-neutral**. EF Core migration artifacts are maintained in separate provider-specific packages so the authentication library does not acquire a compile-time dependency on a production database provider.

The supported providers are:

- PostgreSQL
- SQL Server
- MySQL

Auth does not own a tenant catalog. `TenantMemberships.TenantId` remains a reference to a tenant identifier owned by the consuming application.

## Why Migrations Are Separated by Provider

The Auth model is common, but EF Core generates provider-specific migration metadata and SQL. Keeping one migration project per provider gives each database engine its own:

- EF Core provider package.
- `IDesignTimeDbContextFactory<AuthDbContext>`.
- Generated migrations.
- Model snapshot.
- Integration-test execution path.

The structure is therefore:

```text
KUKULCAN.SharedKernel.Auth
        |
        +-- AuthDbContext / common model
        |
        +-- Auth.Migrations.PostgreSQL
        |
        +-- Auth.Migrations.SQLServer
        |
        +-- Auth.Migrations.MySQL
```

This prevents the main Auth package from becoming coupled to PostgreSQL, SQL Server or MySQL while allowing each supported provider to evolve its migration representation independently.

## Migration Packages

Install the main Auth package and the provider-specific migration package required by the consuming application.

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

The main Auth assembly remains independent of these provider packages.

## Current Migration Projects

The repository currently contains:

```text
Source/
├── KUKULCAN.SharedKernel.Auth.Migrations.PostgreSQL/
│   └── Migrations/
├── KUKULCAN.SharedKernel.Auth.Migrations.SQLServer/
│   └── Migrations/
└── KUKULCAN.SharedKernel.Auth.Migrations.MySQL/
    └── Migrations/
```

Each migration project references `KUKULCAN.SharedKernel.Auth` and contains the EF Core design-time dependency and database provider required by that project.

## Current Migration History

The current history is intentionally provider-specific because the migration timestamps and generated artifacts are produced independently.

### PostgreSQL

```text
InitialCreate
    |
MoveTablesToAuthSchema
```

### SQL Server

```text
InitialCreate
    |
MoveTablesToAuthSchema
```

### MySQL

```text
InitialCreate
```

The PostgreSQL and SQL Server `MoveTablesToAuthSchema` migration was introduced to place the Auth tables in the explicit `Auth` schema. MySQL does not receive this migration because MySQL uses the term schema for the database itself.

## Database Schema Decision

### PostgreSQL and SQL Server

Auth tables are stored in the explicit `Auth` schema:

```text
Auth.Users
Auth.TenantMemberships
Auth.FederatedIdentities
```

The `MoveTablesToAuthSchema` migration moves the existing initial tables from the provider default schema into `Auth` without changing their logical table names.

This isolates authentication tables from tables belonging to the consuming application and makes the database ownership boundary explicit.

### MySQL

MySQL treats a schema as a database rather than as a namespace within a database.

For that reason, Auth does **not** introduce an additional `Auth` schema/database layer on MySQL. The Auth tables remain in the database selected by the configured connection string.

This is intentional provider-specific behavior, not an omission.

The EF Core migration history table is managed by EF Core separately and is not part of the Auth table schema requirement.

## Runtime Migration Assembly Selection

The configured `DatabaseProvider` determines the migration assembly associated with `AuthDbContext`:

```text
PostgresSql -> KUKULCAN.SharedKernel.Auth.Migrations.PostgreSQL
SqlServer   -> KUKULCAN.SharedKernel.Auth.Migrations.SQLServer
MySql       -> KUKULCAN.SharedKernel.Auth.Migrations.MySQL
```

The matching migration package must therefore be present in the consuming application's output.

After registering `AuthDbContext` with the shared database infrastructure, a consuming application can apply pending migrations with:

```csharp
await dbContext.Database.MigrateAsync(cancellationToken);
```

For production deployments, the preferred operational model is to execute schema changes as an explicit deployment/database step rather than implicitly migrating the database every time the application starts. This keeps schema changes observable and controllable independently of application startup.

## Design-Time Factories

Each migration package contains an `IDesignTimeDbContextFactory<AuthDbContext>`.

The factory constructs the same Auth model used at runtime but supplies the minimum design-time services required by EF Core. This avoids requiring an ASP.NET Core application host merely to scaffold a migration.

The factories require these provider-specific environment variables:

```text
KUKULCAN_AUTH_POSTGRESQL_CONNECTION_STRING
KUKULCAN_AUTH_SQLSERVER_CONNECTION_STRING
KUKULCAN_AUTH_MYSQL_CONNECTION_STRING
```

The connection string is read only by the design-time factory.

Use environment variables or an external secret-management mechanism. Never commit database credentials to source control.

## Prerequisites

Use the EF Core CLI compatible with .NET 10:

```bash
dotnet tool install --global dotnet-ef --version 10.*
dotnet ef --version
```

The repository projects target `net10.0`, so the EF Core tooling and provider packages must remain on the EF Core 10 line.

Before generating a migration:

1. Start from an up-to-date `Develop`.
2. Create a feature branch.
3. Change the Auth model or its EF Core configuration.
4. Add or update behavior tests following the repository TDD workflow.
5. Confirm the intended test is RED before changing production code when the behavior is new.
6. Implement the source change and reach GREEN.
7. Only then generate the database migration artifacts.
8. Generate the migration for every supported provider affected by the model change.

## Creating a New Migration

A model change must be represented by a **new migration**. Do not edit an already-applied migration.

Use the same logical migration name for each provider so the history is easy to correlate, while allowing EF Core to generate provider-specific files.

### PostgreSQL

Set the design-time connection:

```bash
export KUKULCAN_AUTH_POSTGRESQL_CONNECTION_STRING='Host=localhost;Database=KukulcanAuth;Username=...;Password=...;'
```

Generate the migration:

```bash
dotnet ef migrations add <MigrationName> \
  --project Source/KUKULCAN.SharedKernel.Auth.Migrations.PostgreSQL/KUKULCAN.SharedKernel.Auth.Migrations.PostgreSQL.csproj \
  --context AuthDbContext \
  --output-dir Migrations
```

### SQL Server

Set the design-time connection:

```bash
export KUKULCAN_AUTH_SQLSERVER_CONNECTION_STRING='Server=localhost;Database=KukulcanAuth;User Id=...;Password=...;TrustServerCertificate=True;'
```

Generate the migration:

```bash
dotnet ef migrations add <MigrationName> \
  --project Source/KUKULCAN.SharedKernel.Auth.Migrations.SQLServer/KUKULCAN.SharedKernel.Auth.Migrations.SQLServer.csproj \
  --context AuthDbContext \
  --output-dir Migrations
```

### MySQL

Set the design-time connection:

```bash
export KUKULCAN_AUTH_MYSQL_CONNECTION_STRING='Server=localhost;Database=KukulcanAuth;User Id=...;Password=...;'
```

Generate the migration:

```bash
dotnet ef migrations add <MigrationName> \
  --project Source/KUKULCAN.SharedKernel.Auth.Migrations.MySQL/KUKULCAN.SharedKernel.Auth.Migrations.MySQL.csproj \
  --context AuthDbContext \
  --output-dir Migrations
```

The connection string allows the design-time factory to construct the provider-specific context. The scaffolding command generates migration source files; it does not itself perform an application-startup migration.

## Why Every Provider Must Be Generated

A common mistake is to modify the common model and generate a migration for only one provider.

That leaves the other provider's model snapshot behind the common model. The repository therefore treats a supported Auth model change as a cross-provider migration task:

```text
Auth model change
      |
      +--> PostgreSQL migration
      |
      +--> SQL Server migration
      |
      +--> MySQL migration
```

If a change is genuinely provider-specific, only the affected provider should receive a migration, but the reason must be explicit and the other providers must remain internally consistent.

## Reviewing a Generated Migration

Before applying or committing a migration, inspect the generated files:

```bash
git diff
git status
```

Review at least:

- `Up` operations.
- `Down` operations.
- Table and column changes.
- Primary and foreign keys.
- Unique constraints.
- Indexes.
- Provider-specific types.
- Schema/database qualification.
- The generated model snapshot.

Pay particular attention to destructive operations such as dropping columns, dropping tables or recreating data-bearing structures.

The migration should express the intended schema change and should not contain unrelated changes caused by accidental model drift.

## Inspecting the Migration List

For each provider:

```bash
dotnet ef migrations list \
  --project Source/KUKULCAN.SharedKernel.Auth.Migrations.PostgreSQL/KUKULCAN.SharedKernel.Auth.Migrations.PostgreSQL.csproj \
  --context AuthDbContext
```

Use the corresponding SQL Server or MySQL migration project for those providers.

The migration list should show the existing history followed by the newly generated migration.

## Generating SQL for Review

Before applying a migration to an important environment, generate the SQL script:

```bash
dotnet ef migrations script \
  --project Source/KUKULCAN.SharedKernel.Auth.Migrations.PostgreSQL/KUKULCAN.SharedKernel.Auth.Migrations.PostgreSQL.csproj \
  --context AuthDbContext
```

Repeat with the SQL Server and MySQL projects.

Reviewing the generated SQL is particularly useful for schema moves, indexes, constraints and potentially destructive changes.

## Applying Migrations with dotnet ef

For PostgreSQL:

```bash
dotnet ef database update \
  --project Source/KUKULCAN.SharedKernel.Auth.Migrations.PostgreSQL/KUKULCAN.SharedKernel.Auth.Migrations.PostgreSQL.csproj \
  --context AuthDbContext
```

For SQL Server:

```bash
dotnet ef database update \
  --project Source/KUKULCAN.SharedKernel.Auth.Migrations.SQLServer/KUKULCAN.SharedKernel.Auth.Migrations.SQLServer.csproj \
  --context AuthDbContext
```

For MySQL:

```bash
dotnet ef database update \
  --project Source/KUKULCAN.SharedKernel.Auth.Migrations.MySQL/KUKULCAN.SharedKernel.Auth.Migrations.MySQL.csproj \
  --context AuthDbContext
```

The corresponding provider-specific environment variable must be available to the design-time factory.

## Initial Schema

The initial migration creates the Auth persistence model:

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

The consuming application owns the tenant catalog and Auth stores the membership relationship through `TenantMemberships.TenantId`.

## Migration History Is Immutable

Once a migration has been committed and can be applied by another environment, treat it as immutable.

Do not:

- Rename an already-published migration.
- Edit an already-applied migration to change its SQL.
- Rewrite the migration snapshot to hide an existing schema change.
- Delete an old migration merely because a later migration supersedes it.

Instead, create another migration:

```text
InitialCreate
    |
MoveTablesToAuthSchema
    |
AddUserDisplayName
    |
FutureChange
```

This keeps databases at different points in the migration history reproducible and allows EF Core to calculate the correct pending changes.

## Rollback

A rollback should normally be performed by moving the database to a known previous migration:

```bash
dotnet ef database update <PreviousMigration> \
  --project <provider-migration-project> \
  --context AuthDbContext
```

For production environments, rollback must be evaluated against the data already stored in the changed schema. A migration's `Down` method is a technical rollback mechanism, not a guarantee that data loss can be recovered.

When a migration is already deployed broadly, a forward corrective migration is often safer than attempting to rewrite history.

## Validation

Each provider has integration tests that apply migrations to a real database, verify that there are no pending migrations afterwards, verify connectivity and exercise the Auth persistence sets.

The integration suites use:

- PostgreSQL Testcontainers.
- SQL Server Testcontainers.
- MySQL Testcontainers.

After changing the model and generating migrations, run the complete test suite and the provider integration suites.

The important invariant is:

```text
Model
  ==
Applied migrations
  ==
Model snapshot
```

for each supported provider.

## Recommended Change Workflow

The repository's standard workflow remains:

```text
TEST
  ↓
RED
  ↓
Source
  ↓
GREEN
  ↓
Coverage
  ↓
PR
  ↓
merge
  ↓
CI GREEN
  ↓
delete feature branch
```

EF Core migrations are part of the Source/schema change and must not be used to bypass the test-first process.

For a persistence-model change, the practical sequence is:

1. Branch from `Develop`.
2. Add the behavior test.
3. Run the focused test and confirm RED when the behavior is new.
4. Change the Auth model/configuration.
5. Run the focused test and confirm GREEN.
6. Generate the migration for every affected provider.
7. Review the generated migration and snapshots.
8. Run provider integration tests against real databases.
9. Run coverage according to the repository workflow.
10. Commit the model and migration artifacts together.
11. Create the PR to `Develop`.
12. Wait for all required Actions to become GREEN.
13. Merge only after the checks are GREEN.
14. Wait for post-merge Actions to become GREEN.
15. Allow the feature branch to be removed by the repository cleanup workflow.

## Production Deployment Guidance

Migration generation belongs in source control, but production execution should be a deliberate deployment operation.

A production deployment should:

1. Build and validate the exact migration artifacts.
2. Generate or inspect the provider-specific SQL when appropriate.
3. Back up the database according to the application's operational policy.
4. Apply the migration using the selected deployment mechanism.
5. Verify the resulting migration history.
6. Deploy the application version that expects the new schema.
7. Validate the application and database health.

Automatic `MigrateAsync` at application startup can be useful for controlled development or local environments, but it is not the default production deployment strategy documented by this repository.

## Summary of the Architecture

The final design is intentionally:

```text
                Common Auth Model
                       |
          +------------+------------+
          |            |            |
     PostgreSQL    SQL Server     MySQL
          |            |            |
   Migration pkg  Migration pkg  Migration pkg
          |            |            |
       Auth.*        Auth.*       Database.*
       schema        schema        namespace
```

This gives KUKULCAN.SharedKernel.Auth a single authentication model while preserving the database-specific behavior required by each supported EF Core provider.
