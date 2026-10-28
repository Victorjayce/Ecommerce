# Frontend integration guide

This guide documents the HTTP API implemented by this .NET 8 backend. Routes and behavior below are based on the controllers, DTOs, services, and database model in the repository. Swagger is enabled in the Development environment and is the best place to inspect the running API.

## 1. Run and connect

The default launch profiles are:

| Profile | Base URL |
|---|---|
| HTTP | `http://localhost:5156` |
| HTTPS | `https://localhost:7136` |
| IIS Express HTTP | `http://localhost:43652` |
| IIS Express HTTPS | `https://localhost:44387` |

Open `http://localhost:5156/swagger` while the API is running in Development.

For Flutter development:

- Android Emulator: `http://10.0.2.2:5156`
- iOS Simulator: `http://localhost:5156`
- Physical device: use the development computer's LAN IP and allow the port through its firewall.
- A device may not trust the local HTTPS development certificate. Use HTTP only for local development or install and trust the certificate.

The API currently does not configure CORS. Browser-based frontends hosted on a different origin need a server-side CORS policy before browser requests will work. Do not solve this by allowing every origin in production.

## 2. HTTP and response conventions

- All routes start with `/api`; JSON fields are camelCase.
- Send and receive JSON using `Content-Type: application/json` where a request body is required.
- `OrderStatus` and `PaymentStatus` values in JSON are numbers. Their database columns are stored as text; this does not change the default JSON enum representation.
- The shared controller result mapper generally returns `200 OK` with data, `204 No Content` for successful null results, and errors with an `{ "error": "..." }` body. Some payment failures return a `PaymentResultDto` body instead. Check the HTTP status before parsing a success model; error shapes are not uniform across all framework-generated responses.
- Use `Authorization: Bearer <access-token>` for protected endpoints. In Swagger, paste the access token without the `Bearer ` prefix.
- Authentication tokens, prices, and IDs should be treated as opaque API values by the frontend. Use decimal-safe handling for currency rather than binary floating-point arithmetic.

## 3. Authentication

The authentication routes for signup, login, and refresh are anonymous. Outside those routes, product search/listing and the Paystack webhook are the only anonymous endpoints; all other routes require a valid access token unless otherwise indicated below.

### Register

`POST /api/Auth/user/signup`

```json
{
  "fullName": "Alex Doe",
  "userName": "alex",
  "email": "alex@example.com",
  "password": "a-password-of-at-least-8-characters"
}
```

Returns a user object containing `id`, `userName`, `fullName`, `email`, `phoneNumber`, and `role`. Registration assigns the `User` role.

### Log in

`POST /api/Auth/user/login`

```json
{
  "email": "alex@example.com",
  "password": "a-password-of-at-least-8-characters"
}
```

Successful response:

```json
{
  "token": "<access-token>",
  "refreshToken": "<refresh-token>",
  "expiresAt": "2026-01-01T12:15:00Z"
}
```

The access token expires after 15 minutes. Keep both tokens in an appropriately secure client-side store; do not log them.

### Refresh tokens

`POST /api/Auth/user/refresh-token`

```json
{
  "token": "<expired-or-current-access-token>",
  "refreshToken": "<current-refresh-token>"
}
```

A successful refresh returns a new access-token/refresh-token pair and invalidates the previous refresh token. If refresh fails, ask the user to log in again. Refresh tokens expire after six months.

### Log out

`POST /api/Auth/logout/{refreshToken}`

Requires an access token. The refresh token is a URL path segment and is revoked for the authenticated user. URL-encode the token before placing it in the path. On logout, clear both tokens in the client.

## 4. Data models

### Product

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

`name` is required and limited to 200 characters; `description` is limited to 1,000 characters. Price is a decimal with two database decimal places. `imageUrl` is a string and defaults to empty when unset.

### Cart item

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

`unitPrice` and `imageUrl` are read from the current product. The cart response contains an array of these items.

### Order

```json
{
  "id": 1,
  "fullName": "Alex Doe",
  "phone": "+15551234567",
  "address": "10 Main Street",
  "status": 0,
  "total": 119.98,
  "quantity": 2,
  "items": [
    {
      "productId": 1,
      "productName": "Wireless Headphones",
      "unitPrice": 59.99,
      "quantity": 2
    }
  ],
  "paymentReference": null,
  "authorizationUrl": null
}
```

`items` preserve product name and unit-price snapshots from checkout. `quantity` is the sum of all item quantities. `paymentReference` and `authorizationUrl` are populated on order creation when payment initialization succeeds; they can be null.

Order status values:

| Value | Meaning |
|---:|---|
| `0` | Pending |
| `1` | Paid |
| `2` | Shipped |
| `3` | Delivered |
| `4` | Cancelled |

Payment status values are `0` Pending, `1` Successful, `2` Failed, and `3` Cancelled.

### Payment result

Payment operations return an object with `success`, `message`, `reference`, `authorizationUrl`, and `status`. `authorizationUrl` and `reference` can be null when not applicable. A `success: false` field may be returned with either a successful HTTP status (for a payment that is still pending or an order already cancelled) or an error status; check both the HTTP status and body.

## 5. Product endpoints

| Method | Route | Auth | Behavior |
|---|---|---|---|
| `GET` | `/api/Products/get?fetched=0` | Anonymous | Returns up to 50 products, skipping `fetched` rows. Use offsets `0`, `50`, `100`, etc. |
| `GET` | `/api/Products/search?query=headphones` | Anonymous | Returns up to 50 matches; `query` is optional and an empty query returns an empty list. |
| `POST` | `/api/Products/add` | Required | Creates a product; the server assigns its ID. |
| `PUT` | `/api/Products/update` | Required | Updates a product by `id`. |
| `DELETE` | `/api/Products/remove?productId=1` | Required | Removes the product by query-string ID. |

Example create request:

```json
{
  "name": "Wireless Headphones",
  "description": "Bluetooth over-ear headphones",
  "price": 59.99,
  "stock": 25,
  "imageUrl": "https://example.com/images/headphones.jpg"
}
```

The update service currently changes only `name`, `description`, and `price`; it does not update `stock` or `imageUrl`. These mutation routes require authentication but are not restricted to the `Admin` role in the current implementation. Do not expose them as customer-facing storefront actions.

The list endpoint uses offset pagination without an explicit stable sort or input validation. The frontend should use non-negative offsets and tolerate products moving between pages if catalog records change.

## 6. Cart endpoints

All cart routes require authentication. Cart data is scoped using the user ID in the access token.

| Method | Route | Auth | Behavior |
|---|---|---|---|
| `GET` | `/api/Cart/get` | User | Returns the current user's cart. |
| `GET` | `/api/Cart/get-all` | Admin | Returns all cart items. |
| `POST` | `/api/Cart/add` | User | Adds a product or replaces its existing quantity. |
| `PUT` | `/api/Cart/update` | User | Replaces the quantity; a value `<= 0` removes the item. |
| `DELETE` | `/api/Cart/remove/{productId}` | User | Removes one item. |
| `DELETE` | `/api/Cart/clear` | User | Clears the current user's cart. |

Add/update request:

```json
{
  "productId": 1,
  "quantity": 2
}
```

Adding requires a positive quantity not greater than current stock. Adding an existing product replaces its quantity rather than incrementing it. Updating does not currently check stock; checkout does. A successful update with quantity `<= 0` returns `204 No Content`. Remove and clear return a boolean on success.

**Known data-model limitation:** the database currently enforces a unique index on `ProductId` alone, even though cart rows also have a user ID. As a result, two different users may be unable to add the same product concurrently/to their separate carts. This should be corrected in the schema before relying on multi-user carts in production.

## 7. Order endpoints

| Method | Route | Auth | Behavior |
|---|---|---|---|
| `GET` | `/api/Order/get-all` | Admin | Returns all orders. |
| `GET` | `/api/Order/get/{orderId}` | User | Intended to return one of the current user's orders; see known issue below. |
| `GET` | `/api/Order/get-all/{userId}` | User | Returns orders for the supplied user ID; see security warning below. |
| `POST` | `/api/Order/create` | User | Creates an order from the authenticated user's cart and starts payment initialization. |
| `DELETE` | `/api/Order/cancel/{orderId}` | User | Cancels an eligible unpaid order and restores its stock. |

Create request:

```json
{
  "fullName": "Alex Doe",
  "phone": "+15551234567",
  "deliveryAddress": "10 Main Street"
}
```

The backend requires these values to be non-empty. The database limits them to 200, 30, and 500 characters respectively, so validate those limits in the frontend. The cart must contain items and each quantity must be within current stock. On success, the server snapshots item names/prices, calculates the total, reduces stock, clears the cart, and attempts to initialize Paystack payment.

If order creation returns an `authorizationUrl`, open it for the user to complete payment. A payment initialization problem does not necessarily mean the order was not created: inspect the order response and its nullable payment fields before retrying. The order can also be passed to `POST /api/Payment/initialize/{orderId}` to retrieve or initialize a payment attempt.

`GET /api/Order/get/{orderId}` currently ignores the route's `orderId` and queries by user ID only. Do not rely on it to fetch a particular order until the backend is corrected.

`GET /api/Order/get-all/{userId}` currently accepts a caller-supplied user ID rather than enforcing that it matches the authenticated user. Do not use this route to access customer data; it should be fixed to derive the ID from the token or restricted to administrators.

Order cancellation is only for unpaid orders. A pending payment attempt is required by the current service. Successful or delivered orders cannot be cancelled. The separate payment-cancel endpoint cancels a payment attempt; it does not cancel the order or restore stock.

## 8. Payments (Paystack)

All payment routes require authentication except the webhook.

| Method | Route | Auth | Behavior |
|---|---|---|---|
| `POST` | `/api/Payment/initialize/{orderId}` | User | Initializes payment for an order owned by the current user, or reuses its pending attempt. |
| `GET` | `/api/Payment/verify/{reference}` | User | Verifies a payment reference for the current user. |
| `DELETE` | `/api/Payment/cancel/{orderId}` | User | Cancels a pending payment attempt for the current user's order. |
| `POST` | `/api/Payment/webhook` | Anonymous, Paystack signature checked | Receives Paystack `charge.success` events. Configure this URL in Paystack; do not call it from the frontend. |

The server calculates the amount from the saved order total and uses NGN. A successful initialization returns the Paystack authorization URL and reference. After the customer returns from Paystack, call the verify route with the returned reference and use its status/message to update the UI. A signed webhook can also update the order/payment status.

Configure the Paystack secret and callback URL through local secrets or deployment secret storage. Never put a secret key in frontend code or publish it in repository settings.

## 9. Suggested frontend request sequence

1. Register or log in, then store the returned access/refresh tokens securely.
2. Load products with `GET /api/Products/get?fetched=0` or search with `GET /api/Products/search?query=...`.
3. Send cart and order requests with the access-token bearer header.
4. Read the cart with `GET /api/Cart/get`; use add/update/remove/clear as needed.
5. Submit delivery details to `POST /api/Order/create`.
6. If the response includes an authorization URL, open it and complete the Paystack flow.
7. Verify the returned payment reference with `GET /api/Payment/verify/{reference}`.
8. Refresh an expired access token using the refresh route; if it fails, log in again.

## 10. Current deployment limitations

- CORS is not configured.
- Repository settings include development configuration and credentials; treat any published credential as compromised and rotate it. Never use repository values as production credentials or put secrets in frontend code.
- Product writes are authenticated but not admin-only.
- The cart unique index does not include user ID.
- The order lookup route ignores its order ID; the user-orders route trusts a caller-supplied user ID.
- Payment/order cancellation and payment initialization failures have edge cases; use returned HTTP status, message, and nullable fields rather than assuming every order response is a completed payment.
- Set up HTTPS, production-grade secret storage, explicit authorization policy, and integration tests before deployment.
