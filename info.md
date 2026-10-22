# Ecommerce API Integration Guide

This document describes the completed HTTP API exposed by the ASP.NET Core .NET 8 backend for the Flutter application. It is based on the controllers, DTOs, services, and models currently implemented in the `Ecommerce` project.

## 1. Development connection

The API uses HTTPS redirection and runs with these Visual Studio launch profiles:

### Database setup and test data

After cloning the repository, create or migrate the `EcommerceDB` database using Entity Framework:

```bash
dotnet ef database update
```

Then run the root-level [`seed_products.sql`](seed_products.sql) script against the `EcommerceDB` database using SQL Server Management Studio, Azure Data Studio, or another SQL Server client. The script inserts 250 sample products, including products with zero stock so out-of-stock behavior can be tested.

Run the seed script only after the database and `ProductsTable` have been created. If the script is executed more than once, it will insert another set of products unless the table is cleared first.

| Profile | Base URL |
|---|---|
| HTTP | `http://localhost:5156` |
| HTTPS | `https://localhost:7136` |
| IIS Express HTTP | `http://localhost:43652` |
| IIS Express HTTPS | `https://localhost:44387` |

All API routes use the `api` prefix. For example:

```text
http://localhost:5156/api/Products/get?fetched=0
```

### Flutter device URLs

`localhost` means the device or emulator itself, not the development computer.

- Android Emulator: use `http://10.0.2.2:5156`.
- iOS Simulator: normally use `http://localhost:5156`.
- Physical device: use the development computer's local network IP, for example `http://192.168.1.10:5156`, and ensure the firewall permits the port.
- HTTPS on an emulator or physical device may fail with the development certificate. Use HTTP for local development or install/trust the development certificate.

The backend currently has no authentication and no CORS configuration. There is no token to send and no `Authorization` header is required by the current endpoints. A production Flutter client should use HTTPS and the backend should add authentication and an explicit CORS policy before deployment.

## 2. HTTP conventions

- Request and response format: `application/json` unless stated otherwise.
- JSON property names are shown in camelCase, which is the ASP.NET Core web default. The server's model binding is case-insensitive.
- IDs and quantities are integers.
- Prices, totals, and unit prices are decimal numbers with two database decimal places. Treat them as numeric values in Dart; avoid binary floating-point arithmetic for money when calculating totals locally.
- Successful responses use `200 OK`.
- Missing products or cart items generally use `400 Bad Request`; missing orders use `404 Not Found`.
- There is no centralized error envelope or documented exception handler. Unexpected server/database errors may return the framework's default error response.
- The Flutter client should validate required fields, positive quantities, stock, and maximum field lengths before sending requests.

## 3. Data models

### Product

Used by product listing and product creation.

| Field | JSON type | Description |
|---|---|---|
| `id` | `int` | Server-generated product ID. |
| `name` | `string` | Required; maximum 200 characters. |
| `description` | `string` | Maximum 1,000 characters. |
| `price` | `number` | Product price. |
| `stock` | `int` | Current stock value. Add-to-cart and checkout validate stock; quantity updates do not currently perform a stock check. |
| `imageUrl` | `string` | Image URL, or an empty string when not supplied. |

Example:

```json
{
  "id": 1,
  "name": "Wireless Headphones",
  "description": "Bluetooth over-ear headphones",
  "price": 59.99,
  "stock": 25,
  "imageUrl": "https://example.com/images/headphones.jpg"
}
```

### AddToCartRequest

Used by both adding and updating a cart item.

```json
{
  "productId": 1,
  "quantity": 2
}
```

| Field | JSON type | Description |
|---|---|---|
| `productId` | `int` | Existing product ID. |
| `quantity` | `int` | Quantity requested. Add requires a positive value. Update values less than or equal to zero remove the item and returns `200 OK` with a null body. |

### CartItem

The cart item response shape is:

```json
{
  "id": 1,
  "productId": 1,
  "name": "Wireless Headphones",
  "unitPrice": 59.99,
  "quantity": 2,
  "total": 119.98,
  "imageUrl": "https://example.com/images/headphones.jpg"
}
```

| Field | JSON type | Description |
|---|---|---|
| `id` | `int` | Cart row ID. |
| `productId` | `int` | Product ID. |
| `name` | `string` | Product name copied into the response. |
| `unitPrice` | `number` | Current product price. |
| `quantity` | `int` | Quantity in the cart. |
| `total` | `number` | `unitPrice * quantity`. |
| `imageUrl` | `string` | Current product image URL. |

All cart endpoints map to the complete `CartItemDto` shape shown above.

### Order

Order endpoints return `OrderDto` objects:

```json
{
	"id": 1,
  "status": 0,
  "total": 119.98,
  "quantity": 2,
  "fullName": "Alex Doe",
  "phone": "+15551234567",
  "address": "10 Main Street",
	"items": [
	{
	  "productId": 1,
	  "productName": "Wireless Headphones",
	  "unitPrice": 59.99,
	  "quantity": 2
	}
  ]
}
```

Order fields:

| Field | JSON type | Description |
|---|---|---|
| `id` | `int` | Server-generated order ID. |
| `status` | `int` currently | Enum value: `0` Pending, `1` Paid, `2` Shipped, `3` Delivered, `4` Cancelled. The backend has not configured string enum serialization. |
| `total` | `number` | Persisted order total. |
| `fullName` | `string` | Delivery recipient; maximum 200 characters. |
| `address` | `string` | Delivery address; maximum 500 characters. |
| `phone` | `string` | Delivery phone; maximum 30 characters. |
| `items` | `array` | Order line items. |

Order item fields:

- `productId`: integer product ID.
- `productName`: product name copied at checkout.
- `unitPrice`: price copied at checkout.
- `quantity`: purchased quantity.

The `quantity` field on `OrderDto` is the aggregate quantity across all order items. `items` contains the product snapshot captured at checkout.

### PlaceOrderRequest

Used by the order creation endpoint:

```json
{
  "fullName": "Alex Doe",
  "phone": "+15551234567",
  "deliveryAddress": "10 Main Street"
}
```

Fields are required and have maximum lengths of 200, 30, and 500 characters respectively.

## 4. Endpoints

### Products

#### List products

```http
GET /api/Products/get?fetched={offset}
```

- `fetched`: query-string integer used as the number of products to skip. The service returns at most 50 products.
- Use `0` for the first page, `50` for the second page, and so on.
- Response: `200 OK` with `Product[]`.

Example response:

```json
[
  {
	"id": 1,
	"name": "Wireless Headphones",
	"description": "Bluetooth over-ear headphones",
	"price": 59.99,
	"stock": 25,
	"imageUrl": "https://example.com/images/headphones.jpg"
  }
]
```

#### Add a product

```http
POST /api/Products/add
Content-Type: application/json
```

Request body: `Product` JSON. The server ignores any supplied `id` and creates a new product.

```json
{
  "name": "Wireless Headphones",
  "description": "Bluetooth over-ear headphones",
  "price": 59.99,
  "stock": 25,
  "imageUrl": "https://example.com/images/headphones.jpg"
}
```

Response: `200 OK` with the created `Product`.

This is currently an unprotected administrative-style endpoint. The Flutter storefront should not expose it to ordinary customers without backend authorization.

### Cart

The cart is currently global to the database/application. It is not associated with a Flutter user, account, device, or session. Until user identity is added, different clients can see the same cart data.

#### Get cart

```http
GET /api/Cart/get
```

Response: `200 OK` with `CartItemDto[]`. See the response shape under [CartItem](#cartitem).

#### Add to cart

```http
POST /api/Cart/add
Content-Type: application/json
```

Request body: `AddToCartRequest`.

```json
{
  "productId": 1,
  "quantity": 2
}
```

- If the product does not exist, the controller returns `400 Bad Request` with an empty body.
- If the product exists, the requested quantity is stored for the cart item. An existing cart item's quantity is replaced rather than incremented.
- Success: `200 OK` with a `CartItem` response.

#### Set cart quantity

```http
PUT /api/Cart/update
Content-Type: application/json
```

Request body: `AddToCartRequest`.

```json
{
  "productId": 1,
  "quantity": 3
}
```

- If the cart item does not exist, response is `400 Bad Request`.
- If `quantity <= 0`, the item is removed and the current implementation returns `200 OK` with a null response body.
- Otherwise, success is `200 OK` with the updated `CartItem`.

#### Remove one product from the cart

```http
DELETE /api/Cart/remove/{productId}
```

- Success: `200 OK` with JSON boolean `true`.
- Missing cart item: `400 Bad Request` with text `Item not found`.

#### Clear the cart

```http
DELETE /api/Cart/clear
```

Success: `200 OK` with the text response `Cart cleared successfully`.

### Orders

#### List orders

```http
GET /api/Order/get
```

Response: `200 OK` with `OrderDto[]`, including each order's item snapshots.

#### Get an order

```http
GET /api/Order/get/{orderId}
```

- Success: `200 OK` with an `OrderDto`.
- Missing order: `404 Not Found` with `Order not found`.

#### Create an order

```http
POST /api/Order/create
Content-Type: application/json
```

Request body: `PlaceOrderRequest`.

```json
{
  "fullName": "Alex Doe",
  "phone": "+15551234567",
  "deliveryAddress": "10 Main Street"
}
```

- The cart must contain at least one item.
- The service validates that each cart quantity is within available stock.
- The order stores product names and prices as snapshots, calculates the total, reduces product stock, and clears the cart.
- Success: `200 OK` with the created `OrderDto`.
- Invalid customer data, an empty cart, or insufficient stock: `400 Bad Request` with `Failed to create order`.

#### Cancel an order

```http
DELETE /api/Order/cancel/{orderId}
```

- Orders that are cancelled or delivered cannot be cancelled.
- A successful cancellation restores the order item quantities to product stock and returns `200 OK` with `Order cancelled successfully`.
- Missing, already cancelled, or delivered orders return `404 Not Found` with `Order not found or already shipped`.

## 5. Recommended Flutter structure

Create separate API clients or repositories so transport details stay out of widgets:

- `ProductApi`: `getProducts(offset)` and, only for administrative screens, `addProduct(product)`.
- `CartApi`: `getCart()`, `addToCart(productId, quantity)`, `updateCart(productId, quantity)`, `removeFromCart(productId)`, and `clearCart()`.
- `OrderApi`: `getOrders()`, `getOrderById(orderId)`, `createOrder(request)`, and `cancelOrder(orderId)`.

Suggested Dart model names:

- `Product.fromJson` / `toJson`
- `CartItem.fromJson`
- `Order.fromJson`
- `OrderItem.fromJson`
- `PlaceOrderRequest.fromJson` / `toJson`

For resilient parsing:

- Parse order `status` as an integer: `0` Pending, `1` Paid, `2` Shipped, `3` Delivered, `4` Cancelled.
- Check HTTP status codes before decoding a success model; error bodies are not a stable JSON schema.

## 6. Current deployment considerations

The core product, cart, checkout, order lookup, and order cancellation flows are implemented. The following infrastructure concerns still apply before a public production deployment:

1. Add authentication and associate carts and orders with a user or session.
2. Add CORS for approved frontend origins.
3. Add consistent validation and stable `ProblemDetails` error responses.
4. Protect administrative/destructive operations and define an order status policy.
5. Add pagination ordering and validate the product-list offset.
6. Add automated integration tests for stock, checkout, cancellation, and concurrent cart updates.

## 7. Quick request sequence for the Flutter developer

1. `GET /api/Products/get?fetched=0` to load the first product page.
2. `POST /api/Cart/add` with `productId` and `quantity` when the customer adds a product.
3. `GET /api/Cart/get` when opening the cart screen.
4. `PUT /api/Cart/update` when changing a quantity, or `DELETE /api/Cart/remove/{productId}` to remove it.
5. `POST /api/Order/create` with customer delivery details to complete checkout.
6. Use `GET /api/Order/get/{orderId}` for a specific order and `DELETE /api/Order/cancel/{orderId}` when cancellation is allowed.
