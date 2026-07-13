# BloomRush Order Fulfillment

BloomRush es una API de cumplimiento de ordenes para una tienda de flores. El
sistema mantiene catalogo, clientes, inventario, ordenes, lineas de orden y
eventos de auditoria de fulfillment.

## Domain

El dominio es order fulfillment con inventario limitado.

Una orden (`Order`) representa el encabezado del pedido: cliente, estado,
prioridad y timestamps. Las lineas (`OrderLine`) representan los productos
dentro de la orden.

Este MVP crea ordenes single-line desde `Seeder.SeedOrders(int n)`: cada orden
sembrada contiene una sola `OrderLine` con cantidad `1`. El modelo soporta
multi-line porque `Order.Lines` es una lista y `FulfillmentService` recorre
todas las lineas; simplemente el seeder del MVP mantiene el caso simple.

## Runbook

Flujo recomendado en Swagger:

1. `POST /seed`
2. `GET /inventory`
3. `POST /orders/seed?n=5`
4. `GET /orders`
5. `POST /orders/{orderId}/fulfill`
6. `GET /inventory`
7. `POST /seed`
8. `POST /orders/burst?n=50`
9. `GET /orders`
10. `GET /reports/order-status`
11. `GET /reports/top-products`

No overselling check: despues de `POST /orders/burst?n=50`, `GET /inventory`
debe mostrar `QuantityOnHand >= 0` para todos los productos. Cuando el stock se
agota, las ordenes siguientes deben quedar `Backordered`.

## Technique To Code Map

| Contract line | Code that proves it |
| --- | --- |
| Migration-time seed for catalog and customers | `BloomRush.Data/BloomRushDbContext.cs`, `OnModelCreating`, `HasData(...)` |
| Shared baseline data | `BloomRush.Data/BloomRushBaselineData.cs` |
| Reset endpoint restores baseline stock | `BloomRush.Api/Program.cs`, `POST /seed`; `BloomRush.Api/Seeding/Seeder.cs`, `RestoreBaseline()` |
| Orders are single-line MVP | `BloomRush.Api/Seeding/Seeder.cs`, `SeedOrders(int n)` creates one `OrderLine` per `Order` |
| LINQ report endpoint with grouping/aggregation | `BloomRush.Api/Program.cs`, `GET /reports/top-products` uses `Join`, `GroupBy`, `Sum`, `Count` |
| Extra LINQ reports | `GET /reports/order-status`, `GET /reports/inventory-value`, `GET /reports/customers` |
| Concurrency token on inventory row | `BloomRush.Data/Entities/InventoryItem.cs`, `RowVersion`; `BloomRush.Data/BloomRushDbContext.cs`, `.IsRowVersion()` |
| One order fulfilled with own DbContext | `BloomRush.Api/Fulfillment/FulfillmentService.cs`, `_factory.CreateDbContextAsync(...)` |
| One transaction per order | `FulfillmentService.TryFulfillOneAttemptAsync(...)`, `BeginTransactionAsync`, `SaveChangesAsync`, `CommitAsync` |
| Inventory decrement + order status + audit event land atomically | same transaction in `TryFulfillOneAttemptAsync(...)` updates `InventoryItems`, `Orders`, and inserts `FulfillmentEvents` |
| Race loser catches concurrency exception | `FulfillmentService.FulfillOneAsync(...)`, `catch (DbUpdateConcurrencyException)` |
| Race loser reloads and retries bounded | `MaxConcurrencyAttempts = 3`; each retry calls `TryFulfillOneAttemptAsync(...)`, which creates a fresh DbContext |
| Too many race losses backorder | `MarkBackorderedAfterConcurrencyAsync(...)` |
| Burst endpoint returns immediately | `BloomRush.Api/Program.cs`, `POST /orders/burst`, `_ = Task.Run(...)`, then `Results.Accepted(...)` |
| Burst fans out over single-order path | `FulfillmentService.FulfillBurstAsync(...)` calls `FulfillOneAsync(orderId, ct)` for each order |
| Structured Serilog stream | `Program.cs` configures `Log.Logger`; `FulfillmentService.FulfillBurstAsync(...)` logs `{OrderId}` and `{Result}` |

## Fulfillment Flow

Normal single order:

```text
POST /orders/{orderId}/fulfill
    -> Program.cs validates the order exists and is Pending
    -> IFulfillmentService.FulfillOneAsync(orderId)
    -> TryFulfillOneAttemptAsync(orderId)
    -> load Order + Lines
    -> load matching InventoryItems
    -> if stock is missing: Status = Backordered, insert FulfillmentEvent
    -> if stock exists: decrement QuantityOnHand, Status = Fulfilled, insert FulfillmentEvent
    -> SaveChangesAsync
    -> CommitAsync
```

Burst:

```text
POST /orders/burst?n=50
    -> Seeder.SeedOrders(50)
    -> Task.Run starts background work
    -> endpoint immediately returns 202 Accepted
    -> background scope resolves IFulfillmentService
    -> FulfillBurstAsync(orderIds)
    -> FulfillOneAsync(orderId) per order
```

## Big-O

Let:

- `n` = number of orders in a burst
- `p` = number of baseline products
- `l` = number of lines in one order
- `m` = number of order lines in the report
- `g` = number of distinct products in the report

| Area | Complexity | Why it fits |
| --- | --- | --- |
| Current burst task creation | `O(n)` | One task is created per order ID in `FulfillBurstAsync`. The MVP burst size is small and local. |
| Priority queue | Not active in current MVP | All seeded orders are `Standard`, so no priority queue is used. If a `PriorityQueue<int,int>` planner is added later, enqueue/dequeue are `O(log n)` each and ordering all burst IDs is `O(n log n)`. |
| Seeder product lookup dictionary | Build `O(p)`, lookup `O(1)` average | `SeedOrders` creates `productIdsBySku`, so SKU to ProductId lookup is constant-time for each order. |
| Fulfillment inventory lookup | Current code is `O(l^2)` worst case in memory; MVP single-line is `O(1)` | The code loops lines and uses `FirstOrDefault` over loaded inventory rows. Since seeded orders are single-line, the cost is constant. For true multi-line, a dictionary by `ProductId` would reduce this to `O(l)`. |
| Top products grouping | `O(m)` grouping plus `O(g log g)` sort | The report groups all order lines by product, then sorts product groups by total quantity. This fits because `g` is bounded by catalog size. |
| Order status report | `O(n)` grouping | Groups orders by enum status and computes counts/totals. |
| Inventory value report | `O(p)` | Reads inventory rows and calculates value per product. |

## Token Vs Lock

This project uses EF optimistic concurrency with SQL Server `rowversion`.

`RowVersion` fits because inventory is stored in SQL Server and multiple API
requests/background tasks can attempt to update the same row. EF includes the
original rowversion in the update. If another transaction changed that row first,
SQL updates zero rows and EF throws `DbUpdateConcurrencyException`. The loser
reloads fresh data and retries, or backorders after bounded retries.

A C# `lock` or `Interlocked` is useful for in-memory state inside one process.
It does not protect data across multiple app instances, multiple processes, or
direct database writers. For database inventory, the database token is the safer
source of truth.

## ACID And Isolation

Each order fulfillment uses one transaction:

```text
BeginTransactionAsync
    update InventoryItems
    update Orders.Status / CompletedAtUtc
    insert FulfillmentEvents
SaveChangesAsync
CommitAsync
```

- Atomicity: stock decrement, status change, and audit event commit together or
  fail together.
- Consistency: the service checks stock before decrementing and uses RowVersion
  to avoid writing based on stale inventory.
- Isolation: the transaction plus rowversion prevents two concurrent writers
  from both succeeding with the same stale stock value.
- Durability: after `CommitAsync`, SQL Server persists the result.

This is why the order cannot be half-fulfilled: there should not be a committed
inventory decrement without the matching order status and audit event.

## Non-Key Indexes

| Index | Why it exists |
| --- | --- |
| `Customer.Email` unique | Baseline reset finds customers by email and prevents duplicate customer identities. |
| `Product.Sku` unique | SKU is the external product identifier. Seeder and order creation resolve products by SKU. |
| `Order.Status` non-unique | Reports and operational queries often filter/group by status (`Pending`, `Fulfilled`, `Backordered`). |
| FK indexes from EF migrations | Relationships such as `OrderLines.OrderId`, `OrderLines.ProductId`, `Orders.CustomerId`, and `FulfillmentEvents.OrderId` support joins/includes. |
| `InventoryItems.ProductId` unique | Enforces one inventory row per product. |

## Benchmarks

Do not invent benchmark numbers. Capture them on the same machine/database before
submission and paste them here.

Suggested benchmark sequence:

```text
POST /seed
POST /orders/burst?n=20
GET /reports/order-status
GET /inventory

POST /seed
POST /orders/burst?n=50
GET /reports/order-status
GET /inventory

POST /seed
POST /orders/burst?n=100
GET /reports/order-status
GET /inventory
```

| Scenario | Orders | Elapsed time | Fulfilled | Backordered | Min stock after run | Notes |
| --- | ---: | ---: | ---: | ---: | ---: | --- |
| Burst local run | 20 | TODO | TODO | TODO | TODO | Use Swagger/network timing or console timestamps |
| Burst local run | 50 | TODO | TODO | TODO | TODO | Confirm no product goes below zero |
| Burst local run | 100 | TODO | TODO | TODO | TODO | Expect more backorders after stock runs out |

Parallelism vs concurrency: parallelism is doing multiple fulfillment attempts
at the same time; concurrency is handling overlapping access to shared inventory
correctly. Parallel execution does not always win if SQL/database contention and
RowVersion retries dominate the work.

## Status Codes

| Endpoint | Status | Why |
| --- | --- | --- |
| `GET /` | `200 OK` | Health/readiness message. |
| `POST /seed` | `200 OK` | Baseline reset completed and returns summary. |
| `GET /orders` | `200 OK` | Returns order summary list. |
| `GET /orders/{orderId}` | `200 OK` | Order found and returned. |
| `GET /orders/{orderId}` | `404 Not Found` | Order ID does not exist. |
| `GET /inventory` | `200 OK` | Returns inventory snapshot. |
| `POST /orders/seed?n=...` | `200 OK` | Orders were created and IDs are returned. |
| `POST /orders/seed?n=...` | `400 Bad Request` | `n <= 0` or baseline data is missing. |
| `POST /orders/burst?n=...` | `202 Accepted` | Work was accepted and continues in background. |
| `POST /orders/burst?n=...` | `400 Bad Request` | `n <= 0` or baseline data is missing. |
| `POST /orders/{orderId}/fulfill` | `200 OK` | Order fulfilled, backordered, or already finished. |
| `POST /orders/{orderId}/fulfill` | `404 Not Found` | Order ID does not exist. |
| `GET /reports/top-products` | `200 OK` | Returns grouped product report. |
| `GET /reports/order-status` | `200 OK` | Returns grouped status report. |
| `GET /reports/inventory-value` | `200 OK` | Returns inventory value report. |
| `GET /reports/customers` | `200 OK` | Returns customer aggregate report. |
| Any endpoint | `500 Internal Server Error` | Unhandled infrastructure error, for example SQL Server unavailable. |

