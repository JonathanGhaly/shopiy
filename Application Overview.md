Small production-style Order Management and Admin Dashboard system.
## Table of Contents 
- Requirement
	- User
		- [[User/Functional Requirement|Functional Requirement]]
		- [[User/Non-Functional Requirement|Non-Functional Requirement]]
	- Admin
		- [[Admin/Functional Requirement|Functional Requirement]]
		- [[Admin/Non-Functional Requirement|Non-Functional Requirement]]
- Database Design & Schema
	- User Authentication Schema
	- E-Commerce Schema
		- Table Design
		- Schema
	- Database Connection Configuration
	- Entity Relationships
	- Index Inventory
	- Query Monitoring
	- Query Performance Targets
	- Backup and Recovery
	- "Index Health" Query 
- [[Backend Architecture]]
	- API Endpoints
	- Data Validation Rules
- [[Frontend Architecture]]
- [[Continuous Integration & Continuous Deployment (CI-CD)]]
- [[Docker Setup]]
- [[Quality Assurance & Testing Plan]]
---
# Requirements
## User
### Functional

- **Browse Product Catalog:** Users can view a paginated list of all active products, with the ability to sort by price and creation date.

- **View Product Profiles:** Users can view a detailed page for any individual product, showcasing descriptions, dynamic metadata attributes (e.g., sizes, colors), and real-time inventory levels.

- **Checkout & Order Creation:** Authenticated users with verified emails can convert items into a formal order by providing verified shipping and billing address JSON structures.

- **Order Confirmation:** Upon successful payment or order entry, users receive an immutable confirmation summary containing their unique Order UUID, receipt calculations, and a delivery tracking status placeholder.

- **Purchase History:** Users can access a personal dashboard displaying all past orders linked to their account ID, along with detailed line-item breakdowns for each invoice.
### Non-Functional

- **Performance (Latency):** API endpoints for catalog browsing, searching, and viewing product details must respond in under 100ms (P95) to ensure a fluid, bounce-resistant shopping experience.
    
- **Scalability (Concurrency):** The checkout and inventory systems must be able to handle traffic spikes (e.g., flash sales, holiday traffic) without degrading performance or dropping connections.
    
- **Security (Data Protection):** * Customer passwords must be hashed using PBKDF2 (ASP.NET Identity default).
    
    - Authentication state must be maintained securely using short-lived JWTs and `HttpOnly`, `SameSite=Strict` refresh cookies to prevent XSS and CSRF attacks.
        
    - All data in transit must be encrypted via TLS 1.2/1.3.
        
- **Availability (Uptime):** The storefront API must maintain an uptime of 99.9%, ensuring customers can browse and place orders 24/7.
    
- **Usability & Accessibility:** The React frontend must be fully responsive (mobile, tablet, desktop) and adhere to WCAG 2.1 Level AA standards to ensure accessibility for users with disabilities (e.g., screen-reader support, keyboard navigation).
## Admin
### Functional

- **Secure Dashboard Authentication:** Authorized administrative or manager accounts can securely log in via specialized claim roles managed by ASP.NET Core Identity.

- **Global Order Oversight:** Administrators can view a comprehensive master table of all platform-wide customer orders, displaying real-time financial metrics and fulfillment statuses.

- **Status Pipeline Filtering:** Admins can quickly filter order volumes using specific status categories (`pending`, `paid`, `shipped`, `delivered`, `cancelled`, `refunded`) to manage active fulfillment workloads.

- **Order Profile Diagnostics:** Admins can drill down into any individual order ID to review absolute line-item prices, localized address records, customer checkout notes, and fulfillment historical timestamps.

- **Status Lifecycles Updates:** Admins can modify an order's fulfillment state. Transitioning an order status to `paid` triggers atomic inventory adjustments, while changing a status to `shipped` permanently restricts any subsequent cancellation requests.
### Non-Functional

- **Performance (Query Segregation):** Heavy administrative data queries (e.g., paginating thousands of global orders, generating financial summaries) must be routed to a **Database Read Replica**. This ensures admin tasks never consume resources needed by the primary database to process live customer checkouts.
    
- **Security (RBAC & Auditability):** * Admin API endpoints must strictly validate JWT claims for `Manager` or `Administrator` roles at the gateway layer before processing requests.
    
    - **Audit Trail:** All destructive or critical state changes (e.g., cancelling an order, manually overriding stock quantities, refunding payments) must be logged with the Admin's UUID, action taken, and an immutable UTC timestamp.
        
- **Data Integrity (Concurrency Controls):** Stock quantity updates and order status transitions must be executed as atomic database transactions (using PostgreSQL row-level locking via `SELECT ... FOR UPDATE`). This prevents race conditions if two admins attempt to process the same order simultaneously.
    
- **Session Management:** Admin dashboard sessions must enforce strict inactivity timeouts. For security compliance, the dashboard must automatically log out or lock the screen after 15 minutes of idle time.
    
- **Resilience (Bulk Operations):** If admins perform bulk actions (e.g., batch-updating the status of 100 orders), the API must handle the request asynchronously or via chunking to prevent browser timeouts and ensure partial successes are recorded if one item fails.

---
# Database Design & Schema
## User Authentication Schema

**Use ASP.NET Core Identity APIs (Recommended)**
ASP.NET handle the Identity setup, it will automatically generate tables prefixed with `AspNet` (e.g., `AspNetUsers`, `AspNetRoles`).
### Business Rules:
- `email` is globally unique, matched case-insensitively, and serves as the primary username.
- `password_hash` is null for third-party OAuth users (e.g., Google, GitHub).
- `email_verified_at` must be set before a user can place an order or complete checkout.
- Users are never hard-deleted from the database, only deactivated via `deleted_at`.
- Password: 8-128 chars, requires uppercase + lowercase + digit + special character, hashed via ASP.NET Identity default (PBKDF2 with HMAC-SHA256, 100,000 iterations).
- Account locks automatically for 15 minutes after 5 consecutive failed login attempts.
- Access Token (JWT) is valid for exactly 15 minutes.
- Refresh Token must be stored in a secure, `HttpOnly`, `SameSite=Strict`, HTTPS-only cookie, expiring in 7 days.
- Refresh Tokens use automatic token rotation; reusing an old refresh token instantly revokes the entire token family.

---
## E-Commerce Schema

Product catalog, orders, and payment tracking.
### Design
#### Products:

| Column         | Type         | Nullable | Unique | Default           | Description                        |
| -------------- | ------------ | -------- | ------ | ----------------- | ---------------------------------- |
| id             | UUID         | No       | Yes    | gen_random_uuid() | Primary key, auto-generated        |
| name           | VARCHAR(300) | No       | No     | -                 | Display name of the organization   |
| slug           | VARCHAR(300) | No       | Yes    | -                 | URL-friendly identifier            |
| description    | TEXT         | No       | No     | -                 |                                    |
| price          | INTEGER      | No       | No     | -                 | Price in cents/piasters            |
| currency       | VARCHAR(3)   | No       | No     | 'EGP'             |                                    |
| sku            | VARCHAR(255) | Yes      | Yes    | -                 | Stock Keeping Unit                 |
| stock_quantity | INTEGER      | Yes      | No     | 0                 |                                    |
| is_active      | BOOLEAN      | Yes      | No     | true              |                                    |
| metadata       | JSONB        | No       | No     | '{}'              | Extra attributes (color, size)     |
| created_at     | TIMESTAMP    | No       | No     | CURRENT_TIMESTAMP | Record creation time (UTC)         |
| updated_at     | TIMESTAMP    | Yes      | No     | CURRENT_TIMESTAMP | Last modification time (UTC)       |
| deleted_at     | TIMESTAMP    | Yes      | No     | -                 | Soft delete marker (null = active) |
#### Categories:

| Column     | Type         | Nullable | Unique | Default           | Description                                  |
| ---------- | ------------ | -------- | ------ | ----------------- | -------------------------------------------- |
| id         | UUID         | No       | Yes    | gen_random_uuid() | Primary key, auto-generated                  |
| name       | VARCHAR(200) | No       | No     | -                 | Display name of the organization             |
| slug       | VARCHAR(200) | No       | Yes    | -                 |                                              |
| parent_id  | TEXT         | No       | No     | -                 | References categories(id) for sub-categories |
| created_by |              | No       | No     | -                 | References users(id)                         |
| created_at | TIMESTAMP    | No       | No     | CURRENT_TIMESTAMP | Record creation time (UTC)                   |
| updated_at | TIMESTAMP    | Yes      | No     | CURRENT_TIMESTAMP | Last modification time (UTC)                 |
| deleted_at | TIMESTAMP    | Yes      | No     | -                 | Soft delete marker (null = active)           |
| sort_order | INTEGER      | No       | No     | 0                 | Display order                                |
####  Product Category:

| Column      | Type | Nullable | Unique | Default | Description                      |
| ----------- | ---- | -------- | ------ | ------- | -------------------------------- |
| product_id  | UUID | No       | No     | -       | Foreign key, to products table   |
| category_id | UUID | No       | No     | -       | Foreign key, to categories table |

Primary key is a composite of (product_id, category_id)

#### Orders:

| Column           | Type        | Nullable | Unique | Default           | Description                                                        |
| ---------------- | ----------- | -------- | ------ | ----------------- | ------------------------------------------------------------------ |
| id               | UUID        | No       | Yes    | gen_random_uuid() | Primary key, auto-generated                                        |
| user_id          | UUID        | No       | No     | -                 | References users(id)                                               |
| status           | VARCHAR(20) | No       | No     | 'pending'         | 'pending', 'paid', 'shipped', 'delivered', 'cancelled', 'refunded' |
| subtotal         | INTEGER     | No       | No     | -                 | Sum of item totals                                                 |
| tax              | INTEGER     | No       | No     | 0                 |                                                                    |
| shipping         | INTEGER     | No       | No     | 0                 |                                                                    |
| total            | INTEGER     | No       | No     | -                 | subtotal + tax + shipping                                          |
| currency         | VARCHAR(3)  | No       | No     | 'EGP'             |                                                                    |
| shipping_address | JSONB       | No       | No     |                   | Snapshot of shipping details                                       |
| billing_address  | JSONB       | No       | No     |                   | Snapshot of billing details                                        |
| notes            | TEXT        | Yes      | No     |                   | Customer or admin notes                                            |
| placed_at        | TIMESTAMP   | No       | No     | CURRENT_TIMESTAMP |                                                                    |
| shipped_at       | TIMESTAMP   | Yes      | No     | -                 |                                                                    |
| delivered_at     | TIMESTAMP   | Yes      | No     | -                 |                                                                    |


#### Order Items:

| Column     | Type    | Nullable | Unique | Default           | Description                 |
| ---------- | ------- | -------- | ------ | ----------------- | --------------------------- |
| id         | UUID    | No       | Yes    | gen_random_uuid() | Primary key, auto-generated |
| order_id   | UUID    | No       | No     |                   | References orders(id)       |
| product_id | UUID    | No       | No     |                   | References products(id)     |
| quantity   | INTEGER | No       | No     | 1                 |                             |
| unit_price | INTEGER | No       | No     |                   | Price at time of purchase   |
| total      | INTEGER | No       | No     |                   | quantity * unit_price       |


--- 
### Schema

```sql
CREATE TABLE products (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  name VARCHAR(300) NOT NULL,
  slug VARCHAR(300) UNIQUE NOT NULL,
  description TEXT,
  price INTEGER NOT NULL CHECK (price >= 0),  -- Store as cents
  currency VARCHAR(3) DEFAULT 'EGP',
  sku VARCHAR(100) UNIQUE,
  stock_quantity INTEGER DEFAULT 0 CHECK (stock_quantity >= 0),
  is_active BOOLEAN DEFAULT true,
  metadata JSONB DEFAULT '{}',
  created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  deleted_at TIMESTAMP 
);

CREATE TABLE categories (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  name VARCHAR(200) NOT NULL,
  slug VARCHAR(200) UNIQUE NOT NULL,
  parent_id UUID REFERENCES categories(id),  -- Self-referencing hierarchy
  created_by UUID REFERENCES users(id),
  created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  deleted_at TIMESTAMP,
  sort_order INTEGER DEFAULT 0
);

CREATE TABLE product_categories (
  product_id UUID NOT NULL REFERENCES products(id) ON DELETE CASCADE,
  category_id UUID NOT NULL REFERENCES categories(id) ON DELETE CASCADE,
  PRIMARY KEY (product_id, category_id)
);

CREATE TABLE orders (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  user_id UUID NOT NULL REFERENCES users(id),
  status VARCHAR(20) DEFAULT 'pending'
    CHECK (status IN ('pending', 'paid', 'shipped', 'delivered', 'cancelled', 'refunded')),
  subtotal INTEGER NOT NULL,
  tax INTEGER NOT NULL DEFAULT 0,
  shipping INTEGER NOT NULL DEFAULT 0,
  total INTEGER NOT NULL,
  currency VARCHAR(3) DEFAULT 'EGP',
  shipping_address JSONB NOT NULL,
  billing_address JSONB NOT NULL,
  notes TEXT,
  placed_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  shipped_at TIMESTAMP,
  delivered_at TIMESTAMP
);

CREATE TABLE order_items (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  order_id UUID NOT NULL REFERENCES orders(id) ON DELETE CASCADE,
  product_id UUID NOT NULL REFERENCES products(id),
  quantity INTEGER NOT NULL CHECK (quantity > 0),
  unit_price INTEGER NOT NULL,  -- Price at time of purchase
  total INTEGER NOT NULL
);

-- Product & Category relationships
CREATE INDEX idx_categories_parent_id ON categories(parent_id);
CREATE INDEX idx_product_categories_category_id ON product_categories(category_id);

-- Order lookups by User
CREATE INDEX idx_orders_user_id ON orders(user_id);

-- Line item lookups
CREATE INDEX idx_order_items_order_id ON order_items(order_id);
CREATE INDEX idx_order_items_product_id ON order_items(product_id);
-- For "Sort by Price: Low to High / High to Low" 
CREATE INDEX idx_products_price ON products(price); 
-- For "New Arrivals" sorting 
CREATE INDEX idx_products_created_at ON products(created_at DESC); 
-- For displaying categories in the correct order in your nav menu
CREATE INDEX idx_categories_sort_order ON categories(sort_order);

-- Only indexes products that are actually active, saving disk space and memory
CREATE INDEX idx_products_active ON products(id) WHERE is_active = true AND deleted_at IS NULL;

-- Instantly find orders that need to be fulfilled or paid
CREATE INDEX idx_orders_status_pending ON orders(status) WHERE status = 'pending';

-- Allows fast querying inside the JSONB object
CREATE INDEX idx_products_metadata_gin ON products USING GIN (metadata);
```

**Key business rules**: Store all monetary values as integers in cents to avoid floating-point rounding errors. `order_items.unit_price` captures the price at time of purchase - products may change price later. Decrement `stock_quantity` atomically when order status changes to 'paid'. Orders cannot be cancelled after status changes to 'shipped'. Categories support nesting via `parent_id` for hierarchical product navigation.
## Database Connection Configuration

```
Primary:      postgresql://app_user@primary.rds.amazonaws.com:5432/ridgeline
Read Replica: postgresql://app_reader@replica.rds.amazonaws.com:5432/ridgeline
Connection Pool: PgBouncer, max 100 connections, transaction mode
SSL: Required (verify-full)
Statement Timeout: 30 seconds (application), 5 minutes (migrations)
```
## Entity Relationships

```
users (AspNetUsers root)
  |
  |-- 1:N --> orders --> 1:N --> order_items --> N:1 --> products
  |                                                       ^
  |-- 1:N --> categories (created_by)                     |
                |                                         |
                |-- 1:N --> categories (sub-categories)   |
                |                                         |
                |-- 1:N --> product_categories <-- 1:N ---|
```

| Parent     | Child              | FK Column   | Cardinality | ON DELETE | Notes                                                                 |
| ---------- | ------------------ | ----------- | ----------- | --------- | --------------------------------------------------------------------- |
| users      | orders             | user_id     | 1:N         | RESTRICT  | Cannot delete a user if they have active or past orders.              |
| orders     | order_items        | order_id    | 1:N         | CASCADE   | Deleting an order purges its associated line items.                   |
| products   | order_items        | product_id  | 1:N         | RESTRICT  | Cannot delete a product that has been ordered (historical integrity). |
| products   | product_categories | product_id  | 1:N         | CASCADE   | Deleting a product removes its category assignments.                  |
| categories | product_categories | category_id | 1:N         | CASCADE   | Deleting a category breaks associations with its products.            |
| categories | categories         | parent_id   | 1:N         | RESTRICT  | Cannot delete a parent category containing active sub-categories.     |
| users      | categories         | created_by  | 1:N         | RESTRICT  | Prevents deletion of admin/staff accounts who managed categories.     |


## Index Inventory

| Index Name                           | Table              | Columns         | Type   | Partial Filter                                  | Optimizes                                                     |
| ------------------------------------ | ------------------ | --------------- | ------ | ----------------------------------------------- | ------------------------------------------------------------- |
| `idx_categories_parent_id`           | categories         | parent_id       | B-Tree | None                                            | Hierarchical sub-category lookups.                            |
| `idx_product_categories_category_id` | product_categories | category_id     | B-Tree | None                                            | Retrieving all products linked to a targeted category.        |
| `idx_orders_user_id`                 | orders             | user_id         | B-Tree | None                                            | Loading customer purchase history profiles.                   |
| `idx_order_items_order_id`           | order_items        | order_id        | B-Tree | None                                            | Line-item retrieval during invoice generation.                |
| `idx_order_items_product_id`         | order_items        | product_id      | B-Tree | None                                            | Internal product performance metrics and sales metrics.       |
| `idx_products_price`                 | products           | price           | B-Tree | None                                            | High-to-low and low-to-high price catalog sorting.            |
| `idx_products_created_at`            | products           | created_at DESC | B-Tree | None                                            | Frontend sorting for "New Arrivals".                          |
| `idx_categories_sort_order`          | categories         | sort_order      | B-Tree | None                                            | Sequential layout of the frontend navigation menu.            |
| `idx_products_active`                | products           | id              | B-Tree | `WHERE is_active = true AND deleted_at IS NULL` | Live marketplace product lookups (excl. hidden/soft-deleted). |
| `idx_orders_status_pending`          | orders             | status          | B-Tree | `WHERE status = 'pending'`                      | Merchant dashboard pipelines for unfulfilled orders.          |
| `idx_products_metadata_gin`          | products           | metadata        | GIN    | None                                            | Dynamic filtering by custom attributes (size, color, brand).  |


## Query Performance Targets

| Query Pattern                                                         | Target | Index Used                                               |
| --------------------------------------------------------------------- | ------ | -------------------------------------------------------- |
| Authenticate user & load roles by unique email                        | < 2ms  | Implicit primary/unique index on email                   |
| Fetch nested sub-categories for a parent category navigation          | < 2ms  | idx_categories_parent_id                                 |
| List active marketplace products assigned to a category               | < 5ms  | idx_product_categories_category_id + idx_products_active |
| Sort active product marketplace by ascending/descending price         | < 5ms  | idx_products_price                                       |
| Sort active product marketplace by newest additions                   | < 5ms  | idx_products_created_at                                  |
| Filter dynamic attributes (e.g., find all products where color = red) | < 10ms | idx_products_metadata_gin                                |
| Fetch historical list of orders placed by a specific user             | < 5ms  | idx_orders_user_id                                       |
| Load detailed summary of an order containing all line-items           | < 3ms  | idx_order_items_order_id                                 |
| Aggregate unfulfilled order pipeline for administrative processing    | < 5ms  | idx_orders_status_pending                                |

---
# API Endpoints

Below is the formal API specification designed to fulfill the application's functional requirements. The API is versioned (`/api/v1/`) and utilizes standard HTTP methods and status codes.
### Authentication & Authorization

Authentication is handled via ASP.NET Core Identity APIs, returning JWTs.

- **Public Endpoints:** No authentication required.
- **User Endpoints:** Require a valid JWT (`Authorization: Bearer <token>`).
- **Admin Endpoints:** Require a valid JWT with the `Admin` role claim.

### Product Catalog (Public)
#### 1. Browse Product Catalog

Retrieves a paginated list of active products. Hidden or soft-deleted products are automatically excluded.
- **Endpoint:** `GET /api/v1/products`
- **Auth:** Public
- **Query Parameters:**
    - `page` (int, default: 1)
    - `limit` (int, default: 20)
    - `sort` (string) - Options: `price_asc`, `price_desc`, `newest`
    - `categoryId` (uuid, optional)
- **Response (200 OK):**
```JSON
{
  "data": [
    {
      "id": "e2b5c... ",
      "name": "High-Performance Compute Node",
      "slug": "high-performance-compute-node",
      "price": 450000, 
      "currency": "EGP",
      "stockQuantity": 12
    }
  ],
  "meta": {
    "totalItems": 142,
    "currentPage": 1,
    "totalPages": 8
  }
}
```

#### 2. View Product Profile

Fetches comprehensive details for a single product using its URL-friendly slug.
- **Endpoint:** `GET /api/v1/products/{slug}`
- **Auth:** Public
- **Response (200 OK):**
``` JSON
{
  "id": "e2b5c...",
  "name": "High-Performance Compute Node",
  "sku": "HPC-NODE-01",
  "description": "...",
  "price": 450000,
  "stockQuantity": 12,
  "metadata": {
    "cores": 64,
    "ram": "256GB"
  },
  "categories": [
    { "id": "...", "name": "Servers", "slug": "servers" }
  ]
}
```

### Customer Checkout & History (User)
#### 3. Checkout & Order Creation
Converts a user's cart into a formal, immutable order. The backend calculates all monetary totals using secure database prices to prevent client-side manipulation.
- **Endpoint:** `POST /api/v1/orders`
- **Auth:** Requires JWT (User)
- **Request Body:**
``` JSON
{
  "items": [
    { "productId": "uuid", "quantity": 2 }
  ],
  "shippingAddress": {
    "street": "123 Main St",
    "city": "Cairo",
    "postalCode": "11511",
    "country": "Egypt"
  },
  "billingAddress": {
    "street": "123 Main St",
    "city": "Cairo",
    "postalCode": "11511",
    "country": "Egypt"
  },
  "notes": "Please leave at the reception."
}
```

- **Response (201 Created):** Returns the immutable confirmation summary.

``` JSON
{
  "orderId": "a1b2c3d4...",
  "status": "pending",
  "subtotal": 900000,
  "tax": 126000,
  "shipping": 5000,
  "total": 1031000,
  "placedAt": "2026-06-25T01:09:56Z"
}
```

#### 4. Purchase History

Retrieves the authenticated user's past orders and invoices.
- **Endpoint:** `GET /api/v1/user/orders`
- **Auth:** Requires JWT (User)
- **Response (200 OK):** Array of order summaries with nested `items` arrays detailing historical purchase prices.
### Admin Dashboard (Protected)

#### 5. Global Order Oversight
Provides administrators with a master view of platform-wide orders. Used to populate the primary dashboard tables.
- **Endpoint:** `GET /api/v1/admin/orders`
- **Auth:** Requires JWT (`Role: Admin`)
- **Query Parameters:**
    - `status` (string, optional) - e.g., `pending`, `shipped`
    - `page`, `limit` (pagination)
- **Response (200 OK):**
``` JSON
{
  "data": [
    {
      "orderId": "uuid",
      "customerEmail": "user@example.com",
      "status": "pending",
      "total": 1031000,
      "placedAt": "2026-06-25T14:30:00Z"
    }
  ],
  "meta": { "totalItems": 45, "pendingCount": 12 }
}
```

#### 6. Order Profile Diagnostics

Fetches the absolute details of an individual order, exposing exact line-item costs, fulfillment timestamps, and unredacted customer notes for administrative review.
- **Endpoint:** `GET /api/v1/admin/orders/{id}`
- **Auth:** Requires JWT (`Role: Admin`)
- **Response (200 OK):** Comprehensive JSON matching the exact schema definition of the `orders` and `order_items` tables.
#### 7. Status Lifecycle Updates

Allows administrators to push orders through the fulfillment pipeline.
- **Endpoint:** `PATCH /api/v1/admin/orders/{id}/status`
- **Auth:** Requires JWT (`Role: Admin`)
- **Request Body:**
``` JSON
{
  "status": "paid" 
}
```

- **Response (200 OK):**

```  JSON
{
  "message": "Order status successfully updated.",
  "orderId": "uuid",
  "newStatus": "paid",
  "updatedAt": "2026-06-25T15:00:00Z"
}
```

- **Validation Notes:**
    
    - Transitioning to `paid` triggers an atomic transaction deducting `quantity` from `products.stock_quantity`.
    
    - Attempting to transition a `shipped` or `delivered` order to `cancelled` will result in a `409 Conflict` error.
#### Error Response Format

```json
{
  "error": {
    "code": "MACHINE_READABLE_CODE",
    "message": "Human-readable explanation",
    "details": {}
  }
}
```

| Code             | Status | When                             |
| ---------------- | ------ | -------------------------------- |
| AUTH_REQUIRED    | 401    | No token provided                |
| AUTH_INVALID     | 401    | Token expired or malformed       |
| FORBIDDEN        | 403    | Insufficient role/permissions    |
| NOT_FOUND        | 404    | Resource missing or no access    |
| VALIDATION_ERROR | 400    | Request failed schema validation |
| CONFLICT         | 409    | Duplicate resource               |
| RATE_LIMITED     | 429    | 100/min auth, 20/min login       |
| INTERNAL_ERROR   | 500    | Unexpected server error          |

## Data Validation Rules
### 1. Authentication & User Management

|**Field**|**Validation Rule (API / FluentValidation)**|**Database Enforcement**|
|---|---|---|
|**Email**|Required. Must match RFC 5322 standard email regex. Max 255 characters. Normalized to lowercase.|`UNIQUE INDEX` case-insensitive.|
|**Password**|Required. Minimum 8, Maximum 128 characters. Must contain at least: 1 uppercase, 1 lowercase, 1 digit, and 1 special character (`!@#$%^&*`).|Hashed via PBKDF2. Raw password is **never** stored.|
|**Name**|Required. 2-200 characters. Letters, spaces, and hyphens only.|`VARCHAR(200) NOT NULL`|
|**User Role**|Must match predefined identity claims (e.g., `Admin`, `Manager`, `Customer`).|Managed via `AspNetRoles`.|

### 2. Product Catalog

|**Field**|**Validation Rule (API / FluentValidation)**|**Database Enforcement**|
|---|---|---|
|**Product Name**|Required. 3-300 characters. Stripped of leading/trailing whitespace.|`VARCHAR(300) NOT NULL`|
|**Product Slug**|Required. 3-300 characters. Regex: `^[a-z0-9-]+$`. Cannot start or end with a hyphen.|`VARCHAR(300) UNIQUE NOT NULL`|
|**SKU**|Optional. 3-100 characters. Uppercase alphanumeric and hyphens.|`VARCHAR(255) UNIQUE`|
|**Price**|Required. Must be an integer `>= 0`. Cannot accept decimals (must be submitted in cents/piasters).|`INTEGER NOT NULL CHECK (price >= 0)`|
|**Currency**|Defaults to `EGP`. Must be a valid 3-letter ISO 4217 code.|`VARCHAR(3) DEFAULT 'EGP'`|
|**Stock Quantity**|Required on creation. Integer `>= 0`. Cannot be negative.|`INTEGER DEFAULT 0 CHECK (stock_quantity >= 0)`|
|**Metadata**|Must be a valid JSON object. Max payload size: 100KB.|`JSONB`|

### 3. Category Management

|**Field**|**Validation Rule (API / FluentValidation)**|**Database Enforcement**|
|---|---|---|
|**Category Name**|Required. 2-200 characters.|`VARCHAR(200) NOT NULL`|
|**Category Slug**|Required. Regex: `^[a-z0-9-]+$`. Must be globally unique.|`VARCHAR(200) UNIQUE NOT NULL`|
|**Parent ID**|Optional. If provided, must be a valid `UUIDv4`. Cannot reference itself (circular dependency check at API level).|`UUID REFERENCES categories(id)`|
|**Sort Order**|Optional. Integer. Defaults to `0`.|`INTEGER DEFAULT 0`|

### 4. Order & Checkout Processing

|**Field**|**Validation Rule (API / FluentValidation)**|**Database Enforcement**|
|---|---|---|
|**Order Status**|Must be strictly one of: `pending`, `paid`, `shipped`, `delivered`, `cancelled`, `refunded`.|`CHECK (status IN (...))`|
|**Item Quantity**|Required. Integer `> 0`. A cart cannot contain 0 or negative items.|`CHECK (quantity > 0)`|
|**Financial Totals**|`subtotal`, `tax`, `shipping`, and `total` must all be mathematically valid integers `>= 0`.|`INTEGER NOT NULL`|
|**Addresses**|`shipping_address` and `billing_address` must contain required properties: `street` (string), `city` (string), `country` (string).|`JSONB NOT NULL`|
|**Order Notes**|Optional. Max 1000 characters. HTML tags stripped to prevent XSS.|`TEXT`|

## Cross-Cutting Security Rules

- **UUID Validation:** Any ID passed via route parameters (e.g., `/api/v1/products/{id}`) must strictly match the UUIDv4 regex pattern: `^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-4[0-9a-fA-F]{3}-[89abAB][0-9a-fA-F]{3}-[0-9a-fA-F]{12}$`. Requests failing this return a `400 Bad Request` before hitting the database.
    
- **Sanitization (XSS Prevention):** All rich-text inputs (like `Product.Description`) must be sanitized using a library like **HtmlSanitizer** on the ASP.NET Core backend to strip malicious `<script>` or `onload` tags before insertion.
    
- **Pagination Limits:** Query parameters for `limit` or `pageSize` must be strictly bounded between `1` and `100` to prevent database Denial of Service (DoS) attacks via massive data pulls.
    
- **Immutable Totals:** Order totals (`unit_price`, `subtotal`, `total`) submitted by the client must be completely ignored. The backend must independently recalculate these values by querying the `products` table directly during the checkout transaction.
### Migration History
Use EF Core (for migration and Entity Management)

| Version | Date       | Description                                         | Reversible | Status  |
| ------- | ---------- | --------------------------------------------------- | ---------- | ------- |
| v001    | 2026-06-10 | Initialize ASP.NET Core Identity Core System Tables | Yes        | Applied |
| v002    | 2026-06-12 | Add Products, Categories, and Product Junctions     | Yes        | Applied |
| v003    | 2026-06-15 | Add Orders and Order Line Items Architecture        | Yes        | Applied |
| v004    | 2026-06-18 | Inject GIN indexes on dynamic product metadata maps | Yes        | Applied |
| v005    | 2026-06-22 | Add soft-deletion index configurations to Products  | Yes        | Applied |

```bash
pnpm db:migrate:status           # Check current migration status
pnpm db:migrate:create --name X  # Create a new migration file
pnpm db:migrate                  # Apply all pending migrations
pnpm db:migrate:rollback         # Rollback the last applied migration
pnpm db:reset                    # Drop all, re-migrate, re-seed (DEV ONLY)
```

## Query Monitoring

> **Prerequisite:** Finding slow queries requires the `pg_stat_statements` extension. 
> Run `CREATE EXTENSION pg_stat_statements;` and ensure it is added to `shared_preload_libraries` in your `postgresql.conf`.

```sql
-- Find queries slower than 100ms
SELECT query, calls, mean_exec_time, total_exec_time
FROM pg_stat_statements
WHERE mean_exec_time > 100
ORDER BY total_exec_time DESC
LIMIT 20;

-- Table sizes including indexes (Monitor growth of Orders/Carts)
SELECT
  schemaname || '.' || tablename AS table_name,
  pg_size_pretty(pg_total_relation_size(schemaname || '.' || tablename)) AS total_size,
  pg_size_pretty(pg_relation_size(schemaname || '.' || tablename)) AS data_size
FROM pg_tables WHERE schemaname = 'public'
ORDER BY pg_total_relation_size(schemaname || '.' || tablename) DESC;

-- Active connections by state (Ensure ASP.NET Connection Pool isn't leaking)
SELECT state, COUNT(*) 
FROM pg_stat_activity
WHERE datname = 'your_ecommerce_db_name' -- CHANGE THIS to your actual DB name
GROUP BY state;
```
## Backup and Recovery
#### Using Azure Database for PostgreSQL

| Backup Type         | Schedule                | Retention | Method                                   |
| ------------------- | ----------------------- | --------- | ---------------------------------------- |
| Automated snapshots | Daily 3 AM UTC          | 30 days   | Azure native automated backups           |
| Transaction logs    | Continuous              | 30 days   | Point-in-time recovery (PITR) via WAL    |
| Manual snapshots    | Before major migrations | 90 days   | Azure manual restore points              |
| Logical backup      | Weekly                  | 12 months | `pg_dump` exported to Azure Blob Storage |

#### Hosting locally / [[Docker Setup]] / Bare VPS (Self-Managed) 

| Backup Type         | Schedule                | Retention | Method                                                    |
| :------------------ | :---------------------- | :-------- | :-------------------------------------------------------- |
| Automated snapshots | Daily 3 AM UTC          | 30 days   | Cron job running `pg_dumpall` to external drive           |
| Transaction logs    | Continuous              | 30 days   | PostgreSQL `pg_receivewal` to backup server               |
| Manual snapshots    | Before major migrations | 90 days   | Manual pg_dump file                                       |
| Logical backup      | Weekly                  | 12 months | `pg_dump` compressed and uploaded to secure cloud storage |

--- 
##  "Index Health" Query 
Because e-commerce databases handle a high volume of concurrent inserts and updates (especially in `shopping_cart` and `cart_items`), indexes can become "bloated" over time, slowing down your React frontend's API responses. Consider adding this fourth query to your monitoring toolset to find missing indexes on your foreign keys:
```sql 
-- Find missing indexes on Foreign Keys that might cause slow JOINs 
SELECT 
	tc.table_name, 
	kcu.column_name, 
	ccu.table_name AS foreign_table_name, 
	ccu.column_name AS foreign_column_name 
FROM 
	information_schema.table_constraints AS tc 
	JOIN information_schema.key_column_usage AS kcu 
	ON tc.constraint_name = kcu.constraint_name 
	JOIN information_schema.constraint_column_usage AS ccu 
	ON ccu.constraint_name = tc.constraint_name 
WHERE 
	tc.constraint_type = 'FOREIGN KEY' 
AND tc.table_schema = 'public';
```

--- 

# Backend Architecture
## Technology Used:
- ASP.NET
- Redis
- PostgreSql

To scale seamlessly, the backend uses **Clean Architecture (Onion Architecture)** combined with the **CQRS (Command Query Responsibility Segregation)** pattern via MediatR. This decouples business workflows from EF Core data-access logic.

---
## Directory Structure

Solution: EcommercePlatform/
  ├── 1. Domain/           # Enterprise logic: Entities, Value Objects, Domain Exceptions
  ├── 2. Application/      # Use Cases: CQRS (Commands/Queries), DTOs, FluentValidation
  ├── 3. Infrastructure/   # Data Access: ApplicationDbContext (Identity), JWT, Repositories
  └── 4. WebAPI/           # Presentation: Controllers, Middleware, SignalR, Program.cs
  
---
## Architectural Layer Responsibilities

### 1. Domain Layer (Zero Dependencies)

Contains raw business data logic.

- **Entities:** `Product`, `Category`, `Order`, `OrderItem`. Your `ApplicationUser` inherits from `IdentityUser<Guid>`.

- **Value Objects:** `Address` (handles immutable structured data inside your `shipping_address` and `billing_address` JSONB columns).

### 2. Application Layer (Depends _only_ on Domain)

Houses business operational commands.

- **CQRS Framework:** Segregates writing data from reading data.

    - _Commands (Write):_ `CreateOrderCommand`, `UpdateProductStockCommand` (forces atomic database work).
    
    - _Queries (Read):_ `GetActiveProductsQuery` (targets read replicas or direct lean projections).

- **Validation:** Pipeline behaviors run input logic across your request schemas before hitting handlers.
### 3. Infrastructure Layer (Depends on Application & Domain)

Handles physical data serialization and third-party integrations.

- **Identity Context:** Core persistence layer inheriting from `IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>`.

- **Read/Write DB Splitting:** Houses the factory pattern configuring distinct connection pools for write endpoints (Primary RDS) and tracking-free read queries (Replica).

### 4. WebAPI Layer (The Entry Point)

Translates network transport envelopes into application-layer structures.

- **Controllers:** Lean routing files executing standard `MediatR.Send(command)` operations.

- **Custom Middleware:** Centralized Global Exception Handlers that map unexpected exceptions down into your documented `Error Response Format`.
---
# Frontend Architecture
## Technology Used:
- React

The frontend uses a **Feature-Based (Domain-Driven) Architecture**. Rather than grouping files by technical identity (e.g., placing all components in one folder, all hooks in another), everything belonging to a standalone business domain is collocated inside a feature container.

---
## Directory Structure

frontend/src/
  ├── assets/              # Global image templates, static SVG icons, branding
  ├── components/          # Shared global cross-cutting UI items (Buttons, Input, Table)
  ├── config/              # Global Axios clients, route definitions, environment keys
  ├── context/             # Global low-frequency contexts (Theme, Localization)
  ├── features/            # Self-contained business domains 
  │   ├── auth/            # Login, Registration components, API hooks, state slices
  │   ├── catalog/         # Product lists, detail items, category dynamic trees
  │   ├── checkout/        # Shopping carts, checkout steps, address verification
  │   └── admin/           # Order management metrics, tracking updates, dashboards
  ├── hooks/               # Global generic custom utility hooks (useDebounce, useMediaQuery)
  ├── store/               # Zustand state engine for micro global states (e.g., Cart store)
  └── App.tsx              # Base router orchestration root

---
## Key Frontend Implementation Patterns

- **Secure Network Calls:** The Axios wrapper is configured globally with `withCredentials: true`. This forces the browser to silently attach the HTTP-Only refresh token cookie on backend token rotation checks without exposing it to local JS memory.

- **State Separation:** Use local component state (`useState`) for UI-only toggles (e.g., "is dropdown open?"). Use lightweight global stores like **Zustand** or **Redux Toolkit** strictly for transactional global states (e.g., items added to the shopping cart, authenticated user profile records).

- **Performance Control:** Catalog queries implement debounced filtering states to keep search queries clean and prevent unoptimized frontend typing streams from overwhelming backend API indices.
---
# Continuous Integration & Continuous Deployment (CI/CD)
Our deployment lifecycle is fully automated using **GitHub Actions**. The pipeline is split into two primary workflows: **CI (Validation)** and **CD (Delivery)**.

---
## 1. CI Pipeline: Pull Request Validation

Every pull request targeting the `main` branch triggers the CI pipeline. Code cannot be merged unless all checks pass.

- **Linting & Formatting:** * Frontend: Runs `pnpm lint` (ESLint) and Prettier checks.
    
    - Backend: Runs `dotnet format --verify-no-changes`.
        
- **Automated Testing:** Executes all Unit and Integration tests (see Testing Plan below).
    
- **Build Verification:** Compiles the .NET binaries and Vite frontend bundle to ensure there are no compilation errors.

---
## 2. CD Pipeline: Production Deployment

When code is merged into the `main` branch, the CD pipeline automatically builds and deploys the new version.

1. **Version Bump:** Automatically tags the release based on semantic versioning.
    
2. **Docker Build & Push:**
    
    - Builds the `ecommerce-api` and `ecommerce-frontend` Docker images.
        
    - Pushes the images to a container registry (e.g., AWS ECR or Docker Hub).
        
3. **Database Migrations:** Runs `dotnet ef database update` against the production database.
    
4. **Rolling Update:** Signals the production server to pull the latest images and restart the containers with zero downtime (via Docker Swarm or Kubernetes rolling updates).
---
## Sample GitHub Actions Workflow 
(`.github/workflows/main.yml`) 

```yaml
name: E-Commerce CI/CD

on:
  push:
    branches: [ "main" ]
  pull_request:
    branches: [ "main" ]

jobs:
  build-and-test-backend:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v4
    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with: { dotnet-version: '8.0.x' }
    - name: Restore dependencies
      run: dotnet restore ./backend
    - name: Build
      run: dotnet build ./backend --no-restore
    - name: Test
      run: dotnet test ./backend --no-build --verbosity normal

  build-and-test-frontend:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v4
    - name: Setup Node
      uses: actions/setup-node@v4
      with: { node-version: '20' }
    - name: Install pnpm
      run: npm install -g pnpm
    - name: Install dependencies
      run: pnpm install --prefix ./frontend
    - name: Lint and Test
      run: |
        pnpm lint --prefix ./frontend
        pnpm test --prefix ./frontend
```
---
# Docker Setup
This multi-container setup mirrors your production ecosystem locally. It configures the ASP.NET Core Web API, the React frontend (compiled via Node and served over Nginx), and a PostgreSQL database instance.

---
## docker-compose.yml
``` yaml
version: '3.8'

services:
  db:
    image: postgres:16-alpine
    container_name: ecommerce_db
    environment:
      POSTGRES_USER: app_user
      POSTGRES_PASSWORD: SecureDevPassword123!
      POSTGRES_DB: ecommerce_dev
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U app_user -d ecommerce_dev"]
      interval: 5s
      timeout: 5s
      retries: 5

  redis:
    image: redis:7-alpine
    container_name: ecommerce_redis
    command: redis-server --save 60 1 --loglevel warning
    ports:
      - "6379:6379"
    volumes:
      - redis_data:/data
    healthcheck:
      test: ["CMD", "redis-cli", "ping"]
      interval: 5s
      timeout: 5s
      retries: 5

  seq:
    image: datalust/seq:latest
    container_name: ecommerce_seq
    environment:
      ACCEPT_EULA: Y
    ports:
      - "5341:80"   # Port 5341 mapped to 80 (Seq UI and ingestion port)
    volumes:
      - seq_data:/data

  api:
    image: ecommerce-api:latest
    build:
      context: ./backend
      dockerfile: Dockerfile
    container_name: ecommerce_api
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ConnectionStrings__Primary=Host=db;Port=5432;Database=ecommerce_dev;Username=app_user;Password=SecureDevPassword123!;
      - ConnectionStrings__Redis=redis:6379,abortConnect=false
      - Logging__Seq__ServerUrl=http://seq:80
    ports:
      - "5000:8080"
    depends_on:
      db:
        condition: service_healthy
      redis:
        condition: service_healthy
      seq:
        condition: service_started

  frontend:
    image: ecommerce-frontend:latest
    build:
      context: ./frontend
      dockerfile: Dockerfile
    container_name: ecommerce_frontend
    ports:
      - "3000:80"
    depends_on:
      - api

volumes:
  postgres_data:
  redis_data:
  seq_data:
```

--- 
## Backend Multi-Stage `Dockerfile` (ASP.NET Core)

``` DockerFile
# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/WebAPI/WebAPI.csproj", "WebAPI/"]
COPY ["src/Application/Application.csproj", "Application/"]
COPY ["src/Infrastructure/Infrastructure.csproj", "Infrastructure/"]
COPY ["src/Domain/Domain.csproj", "Domain/"]
RUN dotnet restore "WebAPI/WebAPI.csproj"

COPY src/ .
WORKDIR "/src/WebAPI"
RUN dotnet build "WebAPI.csproj" -c Release -o /app/build

# Publish Stage
FROM build AS publish
RUN dotnet publish "WebAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "WebAPI.dll"]
```

---
## Frontend Multi-Stage `Dockerfile` (React + Vite + Nginx)

``` DockerFile
# Build Stage
FROM node:20-alpine AS build
WORKDIR /app
COPY package.json pnpm-lock.yaml ./
RUN corepack enable && pnpm install
COPY . .
RUN pnpm build

# Production Runtime Stage
FROM nginx:alpine
COPY --from=build /app/dist /usr/share/nginx/html
# Copy custom nginx routing config to handle React Router SPA routing
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
```

---
# Quality Assurance & Testing Plan

To ensure a stable shopping experience and prevent financial or data-loss bugs, the project follows the **Test Pyramid** strategy.

---
## 1. Testing Pyramid Breakdown

| **Test Level**       | **Scope & Purpose**                                                                                          | **Tools Used**                               | **Target Coverage** |
| -------------------- | ------------------------------------------------------------------------------------------------------------ | -------------------------------------------- | ------------------- |
| **End-to-End (E2E)** | Simulates real user browser flows across the full stack (Frontend + API + DB).                               | Playwright                                   | Critical Paths Only |
| **Integration**      | Tests how the API interacts with the database (e.g., checking if an order actually saves to PostgreSQL).     | xUnit, WebApplicationFactory, Testcontainers | ~60%                |
| **Unit**             | Tests isolated business logic (e.g., calculating cart totals, validating JWT rules, UI component rendering). | xUnit, Moq, Vitest, React Testing Library    | > 80%               |

---
## 2. Backend Testing Strategy (ASP.NET Core)

- **Domain Unit Tests:** Pure C# tests using `xUnit` and `FluentAssertions`. We test core entities without database connections. _Example: Asserting that `Order.TransitionToShipped()` throws an exception if the order is still `pending`._
    
- **API Integration Tests:** We use `Microsoft.AspNetCore.Mvc.Testing` (`WebApplicationFactory`) combined with **Testcontainers**. Testcontainers automatically spins up a real, temporary PostgreSQL Docker container for the test run, applies migrations, runs the API request (like `POST /api/v1/orders`), asserts the 200 OK response, and then destroys the database.
    

---
## 3. Frontend Testing Strategy (React)

- **Component Testing:** We use `Vitest` and `React Testing Library` to mount components in isolation. _Example: Rendering the `AddToCartButton` and ensuring the `onClick` handler fires the correct Zustand store action._
    
- **Mocking APIs:** `MSW` (Mock Service Worker) is used to intercept Axios network requests during tests, allowing us to simulate server errors (500s) and test the UI's error states without needing the real ASP.NET backend running.
    
---
## 4. Critical Path E2E Scenarios

E2E tests take the longest to run, so they are reserved exclusively for "make or break" business functions. The following Playwright scripts must pass on every deployment:

1. **The Guest Checkout Flow:** User browses catalog $\rightarrow$ Adds item to cart $\rightarrow$ Registers for account $\rightarrow$ Completes checkout $\rightarrow$ Views Order Confirmation.
    
2. **The Admin Fulfillment Flow:** Admin logs in $\rightarrow$ Views pending orders $\rightarrow$ Updates an order status to 'shipped' $\rightarrow$ Verifies stock quantity decreases.
    
3. **Authentication Security:** Verifies that a locked-out user (5 failed attempts) sees the correct error message and cannot log in.
---
# Application Logging Strategy & Standards

This document defines the logging architecture for the e-commerce platform using **ASP.NET Core**, **Serilog**, and **Seq**. It outlines our structured logging philosophy, strict rules on data hygiene, and practical implementation standards for .NET developers.

## 1. Structured Logging Philosophy

We utilize **Structured Logging** (Semantic Logging). Instead of writing flat text strings to a file, we emit machine-readable event objects with named properties. This allows tools like Seq to instantly filter, aggregate, and alert on specific variables (e.g., finding all errors where `UserId == "123"` and `Total > 5000`).

**The Golden Rule:** Never use C# string interpolation (`$""`) for log messages. Always use Serilog's message templates.

- **Bad (Flat Text):** `_logger.LogInformation($"Order {order.Id} created by user {userId}");`
    
- **Good (Structured):** `_logger.LogInformation("Order {OrderId} created by user {UserId}", order.Id, userId);`
    

## 2. What to Log (And What Not To)

### Required Log Events

Capture these events to ensure auditability, debugging, and performance monitoring:

1. **Authentication & Security (Audit Trail):**
    
    - Successful logins and failed login attempts (include IP address and email, but **never** the password).
        
    - Account lockouts (e.g., 5 failed attempts).
        
    - Role changes or admin actions (e.g., Admin changing an order status to `cancelled`).
        
2. **Critical Business Transactions:**
    
    - Order creation (Include `OrderId`, `UserId`, `Total`, and `ItemCount`).
        
    - Inventory adjustments (Include `ProductId`, `OldQuantity`, `NewQuantity`).
        
    - Payment state transitions (`pending` -> `paid`).
        
3. **System & Performance Diagnostics:**
    
    - Unhandled exceptions and HTTP 500 errors (include full stack trace).
        
    - Slow database queries (execution time > 100ms).
        
    - External API timeouts or retries (e.g., payment gateway latency).
        
    - Application startup, shutdown, and database migration execution.
        

### Strictly Forbidden (Do Not Log)

To maintain compliance (GDPR/PCI) and security, **never** log the following:

- Plain-text passwords or password hashes.
    
- JWT Access Tokens or Refresh Tokens.
    
- Full credit card numbers or CVVs.
    
- Personally Identifiable Information (PII) beyond what is strictly necessary for auditing (e.g., do not log full billing address JSON in standard info logs; store that only in the DB).
    

## 3. Log Level Standards

Use the correct severity level to prevent "log noise" and ensure alerts trigger only when necessary.

|**Level**|**When to Use**|**Example**|**Alert Routing**|
|---|---|---|---|
|**Trace/Verbose**|High-volume debugging data. Only enabled locally.|Variable states inside a loop.|None|
|**Debug**|Standard diagnostic info for development.|"Entering CalculateTax method."|None|
|**Information**|Normal business operations and lifecycle events.|"Order {OrderId} placed."|Analytics / Dashboards|
|**Warning**|Expected but abnormal states. System recovers gracefully.|"Rate limit exceeded for IP {IpAddress}"|Daily Digest|
|**Error**|Current operation failed, but app is alive. Requires investigation.|"Failed to update inventory for {ProductId}"|Active Alerting|
|**Fatal/Critical**|App crashing, database offline, disk full. Immediate action required.|"Redis connection dropped completely."|PagerDuty / SMS|

## 4. How to Log (Implementation)

### Step 1: Serilog Setup (`Program.cs`)

Configure Serilog to enrich all logs with application context and push them to Seq.

C#

```
using Serilog;
using Serilog.Events;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning) // Reduce framework noise
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithProperty("Environment", builder.Environment.EnvironmentName)
    .WriteTo.Console()
    .WriteTo.Seq(builder.Configuration["Logging:Seq:ServerUrl"] ?? "http://localhost:5341")
    .CreateLogger();

builder.Host.UseSerilog();
```

### Step 2: Global Middleware (Correlation IDs)

Ensure every HTTP request has a unique identifier that flows through the entire transaction.

C#

```
app.Use(async (context, next) =>
{
    // Generate a unique ID for the request
    var correlationId = Guid.NewGuid().ToString("N");
    
    // Extract UserId if authenticated
    var userId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Anonymous";

    // Push properties to the Serilog LogContext
    using (LogContext.PushProperty("CorrelationId", correlationId))
    using (LogContext.PushProperty("UserId", userId))
    {
        await next();
    }
});
```

### Step 3: Injecting and Using the Logger

Use Microsoft's standard `ILogger<T>` interface in your controllers and services. Serilog automatically intercepts this.

C#

```
using Microsoft.Extensions.Logging;

public class OrderService
{
    private readonly ILogger<OrderService> _logger;
    private readonly ApplicationDbContext _db;

    public OrderService(ILogger<OrderService> logger, ApplicationDbContext db)
    {
        _logger = logger;
        _db = db;
    }

    public async Task<Order> PlaceOrderAsync(Guid userId, Cart cart)
    {
        try
        {
            _logger.LogInformation("Processing checkout for User {UserId} with {ItemCount} items", 
                userId, cart.Items.Count);

            // ... business logic ...

            _logger.LogInformation("Order {OrderId} successfully created with Total {TotalAmount}", 
                order.Id, order.Total);
                
            return order;
        }
        catch (DbUpdateException ex)
        {
            // Log the exception object as the FIRST parameter
            _logger.LogError(ex, "Database concurrency conflict while creating Order for User {UserId}", userId);
            throw;
        }
    }
}
```

### Step 4: Querying in Seq

With the above setup, you can open the Seq dashboard (`http://localhost:5341`) and use SQL-like queries to find exact events:

- `OrderId == "a1b2c3d4" and @Level = "Information"`
    
- `UserId == "123" and Has(@Exception)`
    
- `TotalAmount > 100000`
---
# Grafana Log
Integrating Grafana into your architecture perfectly rounds out your observability stack. While **Seq** is ideal for querying structured _logs_ (understanding the "why" and "who" of an event), **Grafana** is the industry standard for visualizing _metrics_ (the "how many," "how fast," and "how healthy").

To feed data into Grafana from an ASP.NET Core application, the best-practice approach is to introduce **Prometheus**. Your API will expose a `/metrics` endpoint, Prometheus will scrape it on a timer, and Grafana will visualize that time-series data.

Here is how to update your multi-container setup and application code to support this.

### 1. Update `docker-compose.yml`

We will add two new services: `prometheus` (the time-series database scraper) and `grafana` (the visualization dashboard). We will map Grafana to port `3001` so it doesn't conflict with your React frontend on `3000`.

Add these to your existing `docker-compose.yml`:

YAML

```
  prometheus:
    image: prom/prometheus:latest
    container_name: ecommerce_prometheus
    ports:
      - "9090:9090"
    volumes:
      - ./prometheus.yml:/etc/prometheus/prometheus.yml
    depends_on:
      api:
        condition: service_started

  grafana:
    image: grafana/grafana:latest
    container_name: ecommerce_grafana
    environment:
      - GF_SECURITY_ADMIN_PASSWORD=SecureAdmin123! # Default login password
    ports:
      - "3001:3000" 
    depends_on:
      - prometheus
```

### 2. Create `prometheus.yml`

In the same root directory as your `docker-compose.yml`, create a file named `prometheus.yml`. This tells Prometheus to look at your .NET API container every 5 seconds to gather metrics.

YAML

```
global:
  scrape_interval: 5s

scrape_configs:
  - job_name: 'ecommerce_api'
    static_configs:
      - targets: ['api:8080'] # Points to the internal docker network API port
```

### 3. Implement OpenTelemetry in ASP.NET Core (.NET 8)

To generate the metrics that Prometheus will scrape, we use **OpenTelemetry**, which is deeply integrated into .NET 8.

**Step A: Install NuGet Packages**

Add these to your `WebAPI.csproj`:

- `OpenTelemetry.Extensions.Hosting`
    
- `OpenTelemetry.Instrumentation.AspNetCore`
    
- `OpenTelemetry.Instrumentation.Runtime`
    
- `OpenTelemetry.Exporter.Prometheus.AspNetCore`
    

**Step B: Update `Program.cs`**

Inject the OpenTelemetry services to track HTTP requests, CPU/Memory usage, and custom business metrics.

C#

```
using OpenTelemetry.Metrics;

var builder = WebApplication.CreateBuilder(args);

// ... existing Serilog & DB setup ...

// 1. Configure OpenTelemetry Metrics
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics.AddAspNetCoreInstrumentation()    // Tracks HTTP request durations & errors
               .AddRuntimeInstrumentation()       // Tracks CPU, Memory, GC collections
               .AddPrometheusExporter();          // Exposes the /metrics endpoint
    });

var app = builder.Build();

// ... existing middleware ...

// 2. Map the Prometheus scraping endpoint
app.MapPrometheusScrapingEndpoint();

app.Run();
```

### 4. Connecting Grafana to Prometheus

Once your `docker-compose up -d` is running with the new services, you can set up your dashboards:

1. Navigate to **Grafana** at `http://localhost:3001`.
    
2. Log in with username `admin` and password `SecureAdmin123!`.
    
3. Go to **Connections > Data Sources > Add data source**.
    
4. Select **Prometheus**.
    
5. In the Connection URL, enter `http://prometheus:9090` (this uses Docker's internal DNS to find the Prometheus container).
    
6. Click **Save & Test**.
    

### 5. Recommended Dashboards to Import

You don't need to build dashboards from scratch. Grafana has a community repository of pre-built templates. You can import them by going to **Dashboards > Import** and pasting these IDs:

- **ID 17706:** (ASP.NET Core / .NET 8) - Shows HTTP request rates, response times, and 4xx/5xx error rates.
    
- **ID 17707:** (.NET Runtime) - Visualizes Garbage Collection (GC), Memory usage, ThreadPool contention, and CPU usage.
    

What specific business KPIs (such as active cart abandonment, total processed order value, or inventory depletion rates) would you like to track, so we can design the custom C# metrics counters for them?