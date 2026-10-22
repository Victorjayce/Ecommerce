# Ecommerce API

An ASP.NET Core Web API for a basic ecommerce application. The project provides product management, a shared shopping cart, and order checkout functionality backed by SQL Server and Entity Framework Core.

## Features

- Product listing with simple pagination
- Product creation endpoint
- Add, update, remove, and clear cart items
- Stock validation during cart additions and checkout
- Order creation from the current cart
- Order history and single-order lookup
- Order cancellation with stock restoration
- Swagger/OpenAPI documentation in Development

## Tech stack

- .NET 8
- ASP.NET Core Web API
- Entity Framework Core 8
- SQL Server
- Swagger via Swashbuckle

## Prerequisites

- .NET 8 SDK
- SQL Server Express or another SQL Server instance
- Visual Studio, Visual Studio Code, or the .NET CLI

## Getting started

1. Clone the repository and open the solution:

   ```bash
   git clone https://github.com/Victorjayce/Ecommerce.git
   cd Ecommerce
   ```

2. Confirm the SQL Server connection string in `appsettings.json`:

   ```json
   "DefaultConnection": "Server=.\\SQLEXPRESS;Database=EcommerceDB;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
   ```

   Update the server name, authentication settings, or database name for your local environment as needed.

3. Apply the Entity Framework migrations:

   ```bash
   dotnet ef database update
   ```

4. Seed the database with test products by opening `seed_products.sql` in SQL Server Management Studio or Azure Data Studio and executing it against `EcommerceDB`:

   ```sql
   -- seed_products.sql
   ```

   The script inserts 250 sample products, including some out-of-stock products for testing. Run it after the database has been created and migrations have completed.

5. Run the API:

   ```bash
   dotnet run
   ```

   The default launch profiles are:

   - HTTP: `http://localhost:5156`
   - HTTPS: `https://localhost:7136`

6. Open Swagger in a browser:

   ```text
   http://localhost:5156/swagger
   ```

## API overview

All routes use the `/api` prefix.

### Products

| Method | Route | Description |
|---|---|---|
| `GET` | `/api/Products/get?fetched=0` | Returns up to 50 products, skipping the requested offset. |
| `POST` | `/api/Products/add` | Creates a product. |

### Cart

| Method | Route | Description |
|---|---|---|
| `GET` | `/api/Cart/get` | Returns the current cart. |
| `POST` | `/api/Cart/add` | Adds or sets a product quantity in the cart. |
| `PUT` | `/api/Cart/update` | Updates a cart quantity; zero or less removes the item. |
| `DELETE` | `/api/Cart/remove/{productId}` | Removes one product from the cart. |
| `DELETE` | `/api/Cart/clear` | Removes all cart items. |

Cart request bodies use this format:

```json
{
  "productId": 1,
  "quantity": 2
}
```

### Orders

| Method | Route | Description |
|---|---|---|
| `GET` | `/api/Order/get` | Returns all orders. |
| `GET` | `/api/Order/get/{orderId}` | Returns one order. |
| `POST` | `/api/Order/create` | Creates an order from the current cart. |
| `DELETE` | `/api/Order/cancel/{orderId}` | Cancels an eligible order and restores stock. |

Order creation uses this request body:

```json
{
  "fullName": "Alex Doe",
  "phone": "+15551234567",
  "deliveryAddress": "10 Main Street"
}
```

Creating an order validates the cart, captures product names and prices as order-item snapshots, calculates the order total, reduces stock, and clears the cart.

## Project structure

```text
Controllers/   HTTP API controllers
Data/          Entity Framework database context
Dtos/          Request and response models
Interface/     Service contracts
Migrations/    Entity Framework database migrations
Models/        Database entities
Services/      Application and data-access logic
```

## Database

The application uses the following main entities:

- `Product`
- `CartItem`
- `Order`
- `OrderItem`

The cart is currently global to the application and is not associated with a user, account, device, or session.

## Client development

For a Flutter client running locally:

- Android Emulator: use `http://10.0.2.2:5156`
- iOS Simulator: use `http://localhost:5156`
- Physical device: use the development machine's local network IP

The current development API does not require authentication and does not configure CORS. These should be added before exposing the API publicly.

## License

No license has been specified for this repository.
