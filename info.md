# Ecommerce API Integration Guide

This document describes the current HTTP API exposed by the ASP.NET Core .NET 8 backend for the Flutter application.

> **Source of truth:** This guide reflects the code currently implemented in the `Ecommerce` project. Order placement is not implemented yet. Do not build the Flutter checkout flow as if an order-creation endpoint already exists.

## 1. Development connection

The API uses HTTPS redirection and runs with these Visual Studio launch profiles:

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
- Invalid or missing products/cart items generally use `400 Bad Request`.
- There is no centralized error envelope or documented exception handler. Unexpected server/database errors may return the framework's default error response, especially outside Development.
- The controllers do not explicitly validate positive quantities or all required product fields. The Flutter client should validate input before sending it.

## 3. Data models

### Product

Used by product listing and product creation.

| Field | JSON type | Description |
|---|---|---|
| `id` | `int` | Server-generated product ID. |
| `name` | `string` | Required; maximum 200 characters. |
| `description` | `string` | Maximum 1,000 characters. |
| `price` | `number` | Product price. |
| `stock` | `int` | Current stock value. The current cart service does not enforce it. |
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
| `quantity` | `int` | Quantity requested. Add should normally be positive. Update values less than or equal to zero remove the item internally, although that response currently becomes `200` with a null body. |

### CartItem

The intended cart item response shape is:

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

**Current implementation note:** `GET /api/Cart/get` maps only `productId`, `quantity`, `name`, and `unitPrice`; `id`, `total`, and `imageUrl` may be absent or have default values in that response. Add/update responses use the fuller shape. Flutter should safely treat these fields as nullable/defaulted until the backend mapping is made consistent.

### Order

The current order-list response is an array of persisted `Order` entities, not `OrderDto` objects:

```json
{
  "id": 1,
  "status": 0,
  "total": 119.98,
  "createdAt": "2026-10-20T12:30:00Z",
  "fullName": "Alex Doe",
  "phone": "+15551234567",
  "deliveryAddress": "10 Main Street",
  "items": [
	{
	  "id": 1,
	  "orderId": 1,
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
| `createdAt` | ISO-8601 datetime string | Created timestamp, stored in UTC by default. |
| `fullName` | `string` | Delivery recipient; maximum 200 characters. |
| `phone` | `string` | Delivery phone; maximum 30 characters. |
| `deliveryAddress` | `string` | Delivery address; maximum 500 characters. |
| `items` | `array` | Order line items. |

Order item fields:

- `id`: integer line-item ID.
- `orderId`: integer parent order ID.
- `productId`: integer product ID.
- `productName`: product name copied at checkout.
- `unitPrice`: price copied at checkout.
- `quantity`: purchased quantity.

The `OrderDto` type also exists in the backend, but no current controller returns it. Its `quantity` field is an aggregate quantity and its `items` field contains `OrderItem` objects.

### PlaceOrderRequest (reserved for future checkout)

The backend defines this request model, but there is currently no HTTP endpoint accepting it:

```json
{
  "fullName": "Alex Doe",
  "phone": "+15551234567",
  "deliveryAddress": "10 Main Street"
}
```

Fields are required by the order entity and have maximum lengths of 200, 30, and 500 characters respectively. Do not call an order-placement route until the backend exposes and implements one.

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

Response: `200 OK` with `CartItem[]`. See the response-shape note under [CartItem](#cartitem).

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
- If the product exists, the requested quantity is added to any existing quantity.
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

Response: `200 OK` with `Order[]`, including each order's `items` collection.

#### Clear all orders

```http
DELETE /api/Order/clear
```

Success: `200 OK` with an empty body.

This deletes every order and should be treated as an administrative/destructive operation. It is currently unprotected and should not be called by the customer-facing Flutter app.

#### Place an order

**Not currently available.** There is no `POST` order endpoint in `OrderController`. `PlaceOrderDto` and service methods exist as a blueprint/stub only; the service currently returns `null` for order creation and update.

## 5. Recommended Flutter structure

Create separate API clients or repositories so transport details stay out of widgets:

- `ProductApi`: `getProducts(offset)` and, only for administrative screens, `addProduct(product)`.
- `CartApi`: `getCart()`, `addToCart(productId, quantity)`, `updateCart(productId, quantity)`, `removeFromCart(productId)`, and `clearCart()`.
- `OrderApi`: `getOrders()` and, only for administrative tooling, `clearOrders()`. Keep `placeOrder()` disabled until a backend route is implemented.

Suggested Dart model names:

- `Product.fromJson` / `toJson`
- `CartItem.fromJson`
- `Order.fromJson`
- `OrderItem.fromJson`
- `PlaceOrderRequest` reserved for the future checkout endpoint

For resilient parsing:

- Accept absent/null `id`, `total`, and `imageUrl` on cart-list results because of the current server mapping inconsistency.
- Parse order `status` as an integer today, but isolate that conversion so it can later accept string values such as `Pending` if the backend enables enum-string serialization.
- Parse `createdAt` with `DateTime.parse` and display it in the user's local timezone.
- Check HTTP status codes before decoding a success model; error bodies are not a stable JSON schema.

## 6. Backend limitations to resolve before production

1. Add authentication and associate carts/orders with a user or session.
2. Add CORS for the approved frontend origins.
3. Implement `POST` order placement, including cart validation, stock checks, order-item snapshots, total calculation, and cart clearing.
4. Add an order update/status policy instead of exposing destructive operations to customers.
5. Add consistent validation and stable error responses, preferably `ProblemDetails`.
6. Make cart-list mapping return the same complete `CartItemDto` shape as add/update.
7. Fix cart lookup logic so existing items are consistently located by `ProductId`, not only by the cart row's database key.
8. Decide whether enum values should be serialized as strings or remain numeric, then document and test that contract.
9. Add pagination ordering to product listing and validate the `fetched` offset.
10. Enforce stock and positive quantities on the server; client-side validation alone is not sufficient.

## 7. Quick request sequence for the Flutter developer

1. `GET /api/Products/get?fetched=0` to load the first product page.
2. `POST /api/Cart/add` with `productId` and `quantity` when the customer adds a product.
3. `GET /api/Cart/get` when opening the cart screen.
4. `PUT /api/Cart/update` when changing a quantity, or `DELETE /api/Cart/remove/{productId}` to remove it.
5. Keep checkout UI behind a feature flag or disabled state until the backend exposes order placement.
6. `GET /api/Order/get` can currently be used only for the existing order-list behavior; it is not a customer-scoped order history endpoint yet.
