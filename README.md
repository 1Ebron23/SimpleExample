# SimpleExample - User CRUD API

A Clean Architecture ASP.NET Core 9 Web API for User CRUD operations using SQL Server and Entity Framework Core.

## Project Structure

```
SimpleExample/
├── SimpleExample.Domain/          # Entities and domain models
├── SimpleExample.Application/     # Business logic, services, interfaces, and DTOs
├── SimpleExample.Infrastructure/  # Data access, EF Core, repositories
└── SimpleExample.API/            # Web API controllers and configuration
```

## Architecture

This project follows **Clean Architecture** principles:

- **Domain Layer**: Contains core business entities (User, BaseEntity)
- **Application Layer**: Contains business logic interfaces (IUserService, IUserRepository) and DTOs
- **Infrastructure Layer**: Implements data access using EF Core and SQL Server with Repository Pattern
- **API Layer**: ASP.NET Core Web API with controllers and dependency injection configuration

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [SQL Server LocalDB](https://learn.microsoft.com/en-us/sql/database-engine/configure-windows/sql-server-express-localdb) (included with Visual Studio) or SQL Server Express
- Optional: [SQL Server Management Studio (SSMS)](https://learn.microsoft.com/en-us/sql/ssms/download-sql-server-management-studio-ssms) or Azure Data Studio

## Database Setup

### 1. Install SQL Server

The project uses SQL Server LocalDB by default (included with Visual Studio). 

If you need to install it separately:
- Download [SQL Server Express](https://www.microsoft.com/en-us/sql-server/sql-server-downloads)
- Or install [LocalDB standalone](https://learn.microsoft.com/en-us/sql/database-engine/configure-windows/sql-server-express-localdb)

### 2. Configure Connection String

The connection string is located in:
- `SimpleExample.API/appsettings.json`
- `SimpleExample.API/appsettings.Development.json`

Default connection string (LocalDB):
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=SimpleExampleDB;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
}
```

**For SQL Server Express, use:**
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=SimpleExampleDB;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
}
```

**For SQL Server with authentication:**
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=SimpleExampleDB;User Id=your_username;Password=your_password;MultipleActiveResultSets=true;TrustServerCertificate=True"
}
```

### 3. Apply Database Migrations

The migration has already been created. To create the database and apply the schema:

```bash
cd SimpleExample.API
dotnet ef database update --project ..\SimpleExample.Infrastructure
```

This will:
- Create the `SimpleExampleDB` database
- Create the `Users` table with all necessary columns and indexes

## Running the Application

### Start the API

From the root directory:

```bash
cd SimpleExample.API
dotnet run
```

Or simply:

```bash
dotnet run --project SimpleExample.API
```

The API will start on:
- HTTPS: `https://localhost:5001`
- HTTP: `http://localhost:5000`

### Access Swagger UI

Once the application is running, open your browser and navigate to:

```
https://localhost:5001/swagger
```

This provides an interactive API documentation where you can test all endpoints.

## API Endpoints

### User CRUD Operations

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/users` | Get all users |
| GET | `/api/users/{id}` | Get user by ID |
| POST | `/api/users` | Create a new user |
| PUT | `/api/users/{id}` | Update an existing user |
| DELETE | `/api/users/{id}` | Delete a user |

### Example Requests

**Create User (POST /api/users)**
```json
{
  "firstName": "John",
  "lastName": "Doe",
  "email": "john.doe@example.com"
}
```

**Update User (PUT /api/users/{id})**
```json
{
  "firstName": "Jane",
  "lastName": "Doe",
  "email": "jane.doe@example.com"
}
```

**Response (GET /api/users/{id})**
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "firstName": "John",
  "lastName": "Doe",
  "email": "john.doe@example.com",
  "createdAt": "2026-01-26T12:00:00Z",
  "updatedAt": "2026-01-26T12:00:00Z"
}
```

## Project Dependencies

### SimpleExample.Domain
- No external dependencies (pure .NET)

### SimpleExample.Application
- References: `SimpleExample.Domain`

### SimpleExample.Infrastructure
- References: `SimpleExample.Domain`, `SimpleExample.Application`
- NuGet Packages:
  - `Microsoft.EntityFrameworkCore.SqlServer` (9.0.1)
  - `Microsoft.EntityFrameworkCore.Design` (9.0.1)

### SimpleExample.API
- References: `SimpleExample.Application`, `SimpleExample.Infrastructure`
- NuGet Packages:
  - `Swashbuckle.AspNetCore` (7.2.0)
  - `Microsoft.EntityFrameworkCore.Design` (9.0.1)

## Entity Framework Core Commands

### Create a new migration
```bash
cd SimpleExample.API
dotnet ef migrations add <MigrationName> --project ..\SimpleExample.Infrastructure
```

### Apply migrations to database
```bash
cd SimpleExample.API
dotnet ef database update --project ..\SimpleExample.Infrastructure
```

### Remove last migration
```bash
cd SimpleExample.API
dotnet ef migrations remove --project ..\SimpleExample.Infrastructure
```

### View migration SQL
```bash
cd SimpleExample.API
dotnet ef migrations script --project ..\SimpleExample.Infrastructure
```

## Development

### Build the Solution
```bash
dotnet build
```

### Run Tests (if added)
```bash
dotnet test
```

### Clean Build Artifacts
```bash
dotnet clean
```

## Key Features

- ✅ Clean Architecture with clear separation of concerns
- ✅ Repository Pattern for data access abstraction
- ✅ Generic Repository for reusable CRUD operations
- ✅ Service layer for business logic
- ✅ DTOs for data transfer between layers
- ✅ Entity Framework Core with SQL Server
- ✅ Swagger/OpenAPI documentation
- ✅ Async/await throughout for better performance
- ✅ GUID primary keys
- ✅ Automatic timestamp tracking (CreatedAt, UpdatedAt)
- ✅ Email uniqueness constraint
- ✅ Explicit types (no var keyword)

## Troubleshooting

### Connection Issues

If you encounter database connection errors:

1. **Verify SQL Server LocalDB is installed:**
   ```bash
   sqllocaldb info
   ```
   If not installed, download from [Microsoft](https://learn.microsoft.com/en-us/sql/database-engine/configure-windows/sql-server-express-localdb)

2. **Start LocalDB instance:**
   ```bash
   sqllocaldb start mssqllocaldb
   ```

3. **Check SQL Server service is running:**
   - Windows: Check Services for "SQL Server (SQLEXPRESS)" or "SQL Server (MSSQLSERVER)"
   - Or use: `Get-Service | Where-Object {$_.Name -like "*SQL*"}`

4. **Test connection using SSMS or Azure Data Studio**

### Migration Issues

If migrations fail:

1. Ensure the connection string is correct for your SQL Server instance
2. Verify SQL Server is running
3. Check that your Windows user has permissions (when using Trusted_Connection=True)
4. Try dropping and recreating the database:
   ```sql
   DROP DATABASE IF EXISTS SimpleExampleDB;
   ```
   Then run `dotnet ef database update` again
5. For LocalDB issues, try:
   ```bash
   sqllocaldb stop mssqllocaldb
   sqllocaldb delete mssqllocaldb
   sqllocaldb create mssqllocaldb
   sqllocaldb start mssqllocaldb
   ```

## License

This is a sample project for educational purposes.
