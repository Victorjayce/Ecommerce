# Ecommerce API

An ASP.NET Core Web API for an ecommerce application. It provides product discovery, JWT authentication, user-scoped carts and orders, and Paystack payment processing.

## Features

- Product listing and search, plus authenticated product management
- User registration, login, JWT access tokens, refresh-token rotation, and logout
- Per-user cart and order operations
- Stock validation and stock restoration when eligible unpaid orders are cancelled or expire
- Paystack payment initialization, verification, cancellation, and signed webhooks
- Admin-only endpoints for listing all orders and carts
- Swagger/OpenAPI in the Development environment

## Technology

- .NET 8 / ASP.NET Core
- Entity Framework Core 8 with SQL Server
- ASP.NET Core Identity and JWT bearer authentication
- Paystack REST API
- Swagger via Swashbuckle

## Prerequisites

- .NET 8 SDK
- SQL Server (LocalDB, SQL Server Express, or another SQL Server instance)
- Paystack test credentials for payment flows

## Run locally

1. Clone the repository and enter its directory:

   ```powershell
   git clone https://github.com/Victorjayce/Ecommerce.git
   cd Ecommerce
   ```

2. Configure local settings. The API reads:
   - `ConnectionStrings:DefaultConnection`
   - `Jwt:Issuer`, `Jwt:Audience`, and `Jwt:Secret` (use a strong secret of at least 32 bytes)
   - `Paystack:SecretKey` and `Paystack:CallbackUrl`

   Use .NET user-secrets or environment variables for local secrets rather than committing credentials. For example:

   ```powershell
   dotnet user-secrets init
   dotnet user-secrets set "Jwt:Secret" "<a-long-random-secret>"
   dotnet user-secrets set "Paystack:SecretKey" "<your-paystack-test-secret>"
   dotnet user-secrets set "Paystack:CallbackUrl" "https://your-frontend.example/payment/callback"
   ```

   Configure the SQL Server connection string the same way, for example:

   ```powershell
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<your-sql-server-connection-string>"
   ```

   Review the checked-in settings before using them. Treat any credential found in the repository as compromised and rotate it; never use repository values as production credentials.

3. Apply the database migrations:

   ```powershell
   dotnet ef database update
   ```

   If `dotnet ef` is not installed, install the EF Core CLI tool once with `dotnet tool install --global dotnet-ef`.

4. Optionally run [`seed_products.sql`](seed_products.sql) against the database after migrations. It inserts 250 sample products, including products with zero stock. Running it repeatedly inserts duplicates.

5. Start the API:

   ```powershell
   dotnet run
   ```

   The HTTP launch profile listens at `http://localhost:5156`; the HTTPS profile uses `https://localhost:7136` and `http://localhost:5156`. Swagger is available at `http://localhost:5156/swagger` while running in Development.

On startup, the application ensures the `Admin` and `User` roles exist. Public registration creates users with the `User` role; it does not create an administrator.

## Frontend integration

See [`info.md`](info.md) for the frontend integration guide, including routes, request/response shapes, authentication, checkout, and known API limitations.

## Repository layout

```text
Controllers/   HTTP endpoints
Data/          Entity Framework database context
Dtos/          API request and response models
Interface/     Service contracts
Migrations/    Entity Framework database migrations
Models/        Database entities
Services/      Authentication, catalog, cart, order, and payment logic
```

## Deployment notes

- Configure HTTPS, CORS for the intended frontend origins, and secret storage before deployment.
- Review authorization carefully: several catalog mutation endpoints currently require authentication but are not restricted to administrators.
- The frontend guide documents known route and data-model limitations; do not assume the API is production-ready based on this feature list.
- No license file is currently included. Public visibility alone does not grant permission to reuse or redistribute the code.
