# EFCore.BulkOperations

[![NuGet](https://img.shields.io/nuget/v/Swevo.EFCore.BulkOperations.svg)](https://www.nuget.org/packages/Swevo.EFCore.BulkOperations/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Swevo.EFCore.BulkOperations.svg)](https://www.nuget.org/packages/Swevo.EFCore.BulkOperations/)
[![CI](https://github.com/Swevo/EFCore.BulkOperations/actions/workflows/build.yml/badge.svg)](https://github.com/Swevo/EFCore.BulkOperations/actions/workflows/build.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

**Free, MIT-licensed bulk insert/update/delete for EF Core.** No commercial license required — ever.

## Why EFCore.BulkOperations?

`SaveChangesAsync` sends one round trip per row, which falls over once you need to insert
thousands of entities at a time. Commercial libraries like Z.EntityFramework.Extensions solve
this well but require a paid license per developer/server. EFCore.BulkOperations gives you the
same core capability — high-throughput bulk insert, plus update/delete — fully free and open
source under MIT.

```csharp
using EFCore.BulkOperations;

await dbContext.BulkInsertAsync(orders);

await dbContext.Orders
    .Where(o => o.Status == OrderStatus.Pending)
    .BulkUpdateAsync(setters => setters.SetProperty(o => o.Status, OrderStatus.Cancelled));

await dbContext.Orders
    .Where(o => o.CreatedAt < cutoff)
    .BulkDeleteAsync();
```

## Install

```bash
dotnet add package Swevo.EFCore.BulkOperations
```

## How it works

- **`BulkInsertAsync`** — on SQL Server, streams entities straight into `SqlBulkCopy` (no
  intermediate `DataTable`, no per-row `SaveChanges` round trip). On every other provider
  (SQLite, InMemory, Npgsql, etc.) it automatically falls back to chunked `AddRange` +
  `SaveChangesAsync` batches with `ChangeTracker.Clear()` between batches, so the same call
  works everywhere — just faster on SQL Server.
- **`BulkUpdateAsync` / `BulkDeleteAsync`** — thin, discoverable wrappers around EF Core's
  native `ExecuteUpdateAsync`/`ExecuteDeleteAsync`, rounding out the bulk-operations API surface
  under one consistent name.
- Database-generated columns (identity primary keys, computed columns) are automatically
  excluded from the insert payload — set `BulkInsertOptions.ExcludeDatabaseGeneratedColumns =
  false` to override.

```csharp
await dbContext.BulkInsertAsync(orders, new BulkInsertOptions
{
    BatchSize = 5000,
    TimeoutSeconds = 60,
});
```

## Roadmap

- `BulkInsertOrUpdateAsync` (upsert/merge) is planned for a future 1.1 release. It's deliberately
  out of scope for 1.0 to ship a well-tested, focused insert/update/delete core first.

## Design goals

- **MIT licensed, forever.** No commercial tier, no per-seat fees.
- **Same API everywhere.** SQL Server gets `SqlBulkCopy` performance; every other provider gets
  a correct, tested fallback — you don't need `#if` branches in your app code.
- **No hard SqlClient dependency at runtime for non-SQL-Server apps** — the SQL Server path is
  only exercised when `DbContext.Database.ProviderName` reports the SQL Server provider.

## Related Packages

| Package | Downloads | Description |
|---|---|---|
| [Swevo.EFCore.Outbox](https://www.nuget.org/packages/Swevo.EFCore.Outbox) | [![Downloads](https://img.shields.io/nuget/dt/Swevo.EFCore.Outbox.svg)](https://www.nuget.org/packages/Swevo.EFCore.Outbox) | Transactional outbox pattern for EF Core + AutoBus |
| [Swevo.EFCore.StronglyTyped](https://www.nuget.org/packages/Swevo.EFCore.StronglyTyped) | [![Downloads](https://img.shields.io/nuget/dt/Swevo.EFCore.StronglyTyped.svg)](https://www.nuget.org/packages/Swevo.EFCore.StronglyTyped) | Compile-time strongly-typed ID generation for  |
| [Swevo.EFCore.SoftDelete](https://www.nuget.org/packages/Swevo.EFCore.SoftDelete) | [![Downloads](https://img.shields.io/nuget/dt/Swevo.EFCore.SoftDelete.svg)](https://www.nuget.org/packages/Swevo.EFCore.SoftDelete) | Compile-time soft-delete generation for EF Core entities using Roslyn source generators |
| [Swevo.EFCore.Seeding](https://www.nuget.org/packages/Swevo.EFCore.Seeding) | [![Downloads](https://img.shields.io/nuget/dt/Swevo.EFCore.Seeding.svg)](https://www.nuget.org/packages/Swevo.EFCore.Seeding) | Fluent, idempotent, dependency-ordered seed data for EF Core |
| [Swevo.EFCore.Pagination](https://www.nuget.org/packages/Swevo.EFCore.Pagination) | [![Downloads](https://img.shields.io/nuget/dt/Swevo.EFCore.Pagination.svg)](https://www.nuget.org/packages/Swevo.EFCore.Pagination) | Offset and cursor-based pagination for EF Core |
| [Swevo.EFCore.JsonColumn](https://www.nuget.org/packages/Swevo.EFCore.JsonColumn) | [![Downloads](https://img.shields.io/nuget/dt/Swevo.EFCore.JsonColumn.svg)](https://www.nuget.org/packages/Swevo.EFCore.JsonColumn) | Compile-time JSON column configuration for EF Core 8+ — [JsonColumn] on owned navigation properties generates ConfigureJsonColumns(ModelBuilder) with OwnsOne( |
| [Swevo.EFCore.MultiTenant](https://www.nuget.org/packages/Swevo.EFCore.MultiTenant) | [![Downloads](https://img.shields.io/nuget/dt/Swevo.EFCore.MultiTenant.svg)](https://www.nuget.org/packages/Swevo.EFCore.MultiTenant) | Compile-time multi-tenancy for EF Core |
| [Swevo.EFCore.RowVersion](https://www.nuget.org/packages/Swevo.EFCore.RowVersion) | [![Downloads](https://img.shields.io/nuget/dt/Swevo.EFCore.RowVersion.svg)](https://www.nuget.org/packages/Swevo.EFCore.RowVersion) | Compile-time optimistic concurrency for EF Core — [Optimistic] source generator adds RowVersion property, IOptimisticEntity, and SaveChangesClientWinsAsync / SaveChangesDatabaseWinsAsync retry extensions |

---

## 💼 Need .NET consulting?

I'm the author of EFCore.BulkOperations and a suite of compile-time source generators
([AutoWire](https://github.com/Swevo/AutoWire), [AutoMap.Generator](https://github.com/Swevo/AutoMap.Generator))
and 28+ Polly v8 resilience packages. I'm available for consulting on **Polly v8 resilience**,
**Azure cloud architecture**, and **clean .NET design**.

**[→ solidqualitysolutions.com](https://www.solidqualitysolutions.com/)** · **[LinkedIn](https://www.linkedin.com/in/justbannister/)**

## Also by the same author

> 🌐 Full suite overview: **[swevo.github.io](https://swevo.github.io/)**

| Package | Description |
|---|---|
| [**FluentPdf**](https://github.com/Swevo/FluentPdf) | Free, MIT-licensed fluent PDF generation — alternative to QuestPDF's commercial license. |
| [**AutoBus**](https://github.com/Swevo/AutoBus) | Free, MIT-licensed message bus — alternative to MassTransit's commercial license. |
| [**AutoArchitecture**](https://github.com/Swevo/AutoArchitecture) | Free, MIT-licensed compile-time architecture rule enforcement — alternative to NDepend. |
| [**AutoAssert**](https://github.com/Swevo/AutoAssert) | Free, MIT-licensed fluent assertions — alternative to FluentAssertions' commercial license. |
| [**AutoWire**](https://github.com/Swevo/AutoWire) | Compile-time DI auto-registration — `[Scoped]`/`[Singleton]`/`[Transient]` generates `IServiceCollection` registration code. |
| [**AutoMap.Generator**](https://github.com/Swevo/AutoMap.Generator) | Compile-time object mapping — `[Map(typeof(Dto))]` generates `ToDto()` extension methods. |
| [**EFCore.Outbox**](https://github.com/Swevo/EFCore.Outbox) | Transactional outbox pattern for EF Core. |
| [**AutoDispatch.Generator**](https://github.com/Swevo/AutoDispatch.Generator) | Compile-time CQRS dispatcher — free alternative to MediatR's commercial license. |
| [**PollyAnalyzers**](https://github.com/Swevo/PollyAnalyzers) | Free Roslyn analyzers for async/resilience anti-patterns — blocking calls, async void, fire-and-forget tasks, swallowed exceptions. |
| [**PollyAction**](https://github.com/Swevo/PollyAction) | Free retry/backoff GitHub Action — wrap any CI step with exponential-backoff retries. |

## License

MIT © Justin Bannister
