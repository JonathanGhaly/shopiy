# Shopiy Backend API

An ASP.NET Core REST API designed with **Clean Architecture** patterns, leveraging Entity Framework Core (PostgreSQL) and MediatR (CQRS).

##  Features & Architecture

The application is structured into four distinct layers:

1. **Domain:** Enterprise core models and specifications (`Product`, `Category`, `Order`, `OrderItem`).
2. **Application:** Core business logic, DTO definitions, AutoMapper profiles, FluentValidation, and CQRS handlers via MediatR (`CreateProduct`, `UpdateProduct`, `GetProducts`, etc.).
3. **Infrastructure:** PostgreSQL persistence configuration (`ApplicationDbContext`), Redis caching provider, global query filters, and Identity security services.
4. **Api:** Controllers layer serving HTTP endpoints, route mapping, JWT token checks, and standard Swagger documentation.

##  Requirements & Tech Stack

- **Framework:** .NET 10.0 SDK
- **Database:** PostgreSQL (using JSONB columns for dynamic specifications mapping)
- **Caching:** Redis Distributed Cache (with dynamic fallback to In-Memory cache)
- **Libraries:**
  - AutoMapper (object mapping)
  - MediatR (mediator implementation of CQRS)
  - FluentValidation (request validation)
  - Microsoft.AspNetCore.Authentication.JwtBearer

##  Configuration & Connection Setup

Configure your PostgreSQL and Redis settings inside `src/Shopiy.Api/appsettings.json` (or `appsettings.Development.json`):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=shopiy;Username=postgres;Password=your_password",
    "Redis": "localhost:6379"
  },
  "JwtSettings": {
    "Secret": "your_secure_32_byte_secret_key_here",
    "Issuer": "Shopiy",
    "Audience": "ShopiyClient",
    "ExpiryMinutes": 60
  }
}
```

## Getting Started Locally

1. **Restore dependencies:**
   ```bash
   dotnet restore
   ```

2. **Database Migrations:**
   Ensure your database server is running and configure the connection string, then apply migrations:
   ```bash
   dotnet ef database update --project src/Shopiy.Infrastructure --startup-project src/Shopiy.Api
   ```

3. **Run the Application:**
   Start the API host server:
   ```bash
   dotnet run --project src/Shopiy.Api
   ```
   The API will be available locally on: `http://localhost:5000` / `https://localhost:5001`. Access Swagger docs at `/swagger`.
