# Engineering Rules

# Automated Billing System — Aturan Coding & Business Logic Constraints

| Atribut | Nilai |
|---|---|
| **Versi dokumen** | 1.0 |
| **Tanggal** | 19 Oktober 2026 |
| **Berlaku untuk** | Seluruh kode di `src/` dan `tests/` |
| **Dokumen terkait** | [PRD.md](./PRD.md) · [Architecture.md](./Architecture.md) · [Design_system.md](./Design_system.md) |

> **Kata kunci:** **WAJIB** (*MUST*) = aturan keras, dicek di review/CI. **SEBAIKNYA** (*SHOULD*) = default kuat, penyimpangan perlu alasan. **DILARANG** (*MUST NOT*) = tidak boleh.

---

## Daftar Isi

1. [C# & .NET 8 Coding Standards](#1-c--net-8-coding-standards)
2. [Business Logic Constraints](#2-business-logic-constraints)
3. [Git Commit Conventions](#3-git-commit-conventions)
4. [Testing Rules](#4-testing-rules)
5. [Definition of Done & Review Checklist](#5-definition-of-done--review-checklist)

---

## 1. C# & .NET 8 Coding Standards

### 1.1 Pengaturan Proyek

`Directory.Build.props` **WAJIB** menyertakan:

```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <LangVersion>12</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <AnalysisLevel>latest-recommended</AnalysisLevel>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
  </PropertyGroup>
</Project>
```

### 1.2 Naming Conventions

| Elemen | Konvensi | Contoh |
|---|---|---|
| Namespace, class, record, struct, enum, method, property, event | `PascalCase` | `InvoiceService`, `IssueInvoiceAsync` |
| Interface | `I` + `PascalCase` | `IQueuePublisher` |
| Parameter & variabel lokal | `camelCase` | `subscriptionId` |
| Field privat | `_camelCase` | `_dbContext` |
| Konstanta & `static readonly` publik | `PascalCase` | `MaxDequeueCount` |
| Method async | Akhiran `Async` | `ProcessAsync` |
| Enum | Singular `PascalCase` (`[Flags]` boleh plural) | `InvoiceStatus.Pending` |
| Generic type parameter | `T` / `TEntity` | `Repository<TEntity>` |
| File | Satu tipe publik per file; nama file = nama tipe | `Invoice.cs` |
| DTO request/response | `{Aksi}{Entitas}Request`, `{Entitas}Response` | `CreateCustomerRequest`, `InvoiceResponse` |
| Message queue | `{Nama}Message` | `InvoiceJobMessage` |
| Validator | `{Request}Validator` | `CreateCustomerRequestValidator` |
| Konfigurasi EF | `{Entity}Configuration` | `InvoiceConfiguration` |
| Test method | `Method_Scenario_ExpectedResult` | `ProcessAsync_DuplicateKey_DoesNotCreateInvoice` |

**DILARANG:** notasi Hungarian, singkatan ambigu (`cust`, `sub`, `inv` sebagai nama tipe), nama jamak untuk satu entitas, dan nama generik (`Helper`, `Manager`, `Util`) tanpa konteks.

### 1.3 Struktur File & Namespace

- **WAJIB** memakai **file-scoped namespace**.
- Namespace **WAJIB** mengikuti struktur folder (`BillingSystem.Domain.Invoices`).
- Urutan anggota: konstanta → field → konstruktor → properti → method publik → method privat.
- Satu `using` per baris; urutan: `System.*` dahulu, kemudian lainnya (diatur `.editorconfig`).

```csharp
namespace BillingSystem.Domain.Invoices;

public sealed class Invoice
{
    // ...
}
```

**`.editorconfig` (cuplikan wajib):**

```ini
root = true

[*.cs]
csharp_style_namespace_declarations = file_scoped:error
csharp_using_directive_placement = outside_namespace:warning
dotnet_sort_system_directives_first = true
csharp_style_var_for_built_in_types = false:suggestion
csharp_style_var_when_type_is_apparent = true:suggestion
csharp_prefer_braces = true:warning
dotnet_naming_rule.private_fields_underscore.severity = warning
dotnet_naming_rule.private_fields_underscore.symbols = private_fields
dotnet_naming_rule.private_fields_underscore.style = underscore_camel
dotnet_naming_symbols.private_fields.applicable_kinds = field
dotnet_naming_symbols.private_fields.applicable_accessibilities = private
dotnet_naming_style.underscore_camel.required_prefix = _
dotnet_naming_style.underscore_camel.capitalization = camel_case
```

### 1.4 Clean Code

| Aturan | Level |
|---|---|
| Method **SEBAIKNYA** ≤ 30 baris, satu tingkat abstraksi, satu tanggung jawab | SEBAIKNYA |
| Parameter method **SEBAIKNYA** ≤ 4; selebihnya gunakan objek/record | SEBAIKNYA |
| **WAJIB** *guard clause* / *early return* alih-alih `if` bersarang dalam | WAJIB |
| **DILARANG** *magic number/string*: gunakan konstanta, enum, atau Options | DILARANG |
| **WAJIB** `sealed` untuk class yang tidak dirancang untuk diwariskan | WAJIB |
| **WAJIB** `record` (immutable) untuk DTO dan message | WAJIB |
| Entity domain **WAJIB** menjaga invariannya sendiri (setter `private`/`init`, perubahan lewat method bermakna bisnis) | WAJIB |
| **DILARANG** logika bisnis di Controller, `DbContext`, atau `BackgroundService` — letakkan di Domain/Application | DILARANG |
| **DILARANG** `catch (Exception)` kosong atau yang menelan error; log + rethrow/terjemahkan | DILARANG |
| Komentar menjelaskan **mengapa**, bukan **apa**; XML doc untuk API publik pada `Application`/`Domain` | SEBAIKNYA |
| **DILARANG** kode mati dan `TODO` tanpa nomor issue (`// TODO(#42): ...`) | DILARANG |
| **WAJIB** *nullable reference types* dipatuhi; **DILARANG** `!` (null-forgiving) tanpa alasan tertulis | WAJIB |
| Gunakan `DateOnly` untuk tanggal bisnis, `DateTimeOffset`/`DateTime` UTC untuk timestamp; **DILARANG** `DateTime.Now`/`DateTime.UtcNow` langsung — gunakan `TimeProvider` | WAJIB |
| Uang **WAJIB** `decimal`; **DILARANG** `float`/`double` | WAJIB |

### 1.5 Async/Await Rules

| # | Aturan |
|---|---|
| A1 | Semua I/O (DB, queue, HTTP) **WAJIB** async end-to-end (*async all the way*). |
| A2 | **DILARANG** `.Result`, `.Wait()`, `.GetAwaiter().GetResult()` — risiko deadlock dan *thread starvation*. |
| A3 | **DILARANG** `async void` (kecuali *event handler*). Gunakan `async Task`. |
| A4 | Method async publik **WAJIB** menerima `CancellationToken ct` sebagai **parameter terakhir** dan **meneruskannya** ke seluruh pemanggilan turunan (EF Core, `QueueClient`, `HttpClient`). |
| A5 | `BackgroundService` **WAJIB** menghormati `stoppingToken`; **DILARANG** `Thread.Sleep` (gunakan `await Task.Delay(..., ct)` atau `PeriodicTimer`). |
| A6 | **DILARANG** `Task.Run` untuk membungkus I/O pada ASP.NET Core. |
| A7 | Pekerjaan independen **SEBAIKNYA** dijalankan paralel dengan `Task.WhenAll` / `Parallel.ForEachAsync` dengan batas konkurensi (`MaxDegreeOfParallelism`). |
| A8 | Boleh mengembalikan `Task` langsung (*elide async/await*) **hanya** bila method tidak memiliki `using`, `try/catch`, atau kode setelah await. |
| A9 | `ConfigureAwait(false)` tidak diperlukan di ASP.NET Core/Worker; **WAJIB** dipakai hanya pada library yang berpotensi dipakai di konteks ber-`SynchronizationContext`. |
| A10 | Satu `DbContext` **DILARANG** dipakai paralel oleh beberapa task; gunakan scope DI terpisah per pesan/operasi. |
| A11 | `OperationCanceledException` akibat shutdown **bukan error**: jangan dicatat sebagai failure dan jangan dihitung sebagai retry. |

```csharp
// Contoh benar
public async Task<InvoiceResponse?> GetAsync(Guid id, CancellationToken ct)
{
    return await _db.Invoices
        .AsNoTracking()
        .Where(i => i.Id == id)
        .Select(i => new InvoiceResponse(i.Id, i.InvoiceNumber, i.Status, i.Amount, i.DueDate))
        .SingleOrDefaultAsync(ct);
}
```

### 1.6 Entity Framework Core Rules

| # | Aturan |
|---|---|
| E1 | Konfigurasi memakai **Fluent API** via `IEntityTypeConfiguration<T>`; **DILARANG** data annotation untuk skema database. |
| E2 | Query baca **WAJIB** `AsNoTracking()` dan memakai **proyeksi** (`Select`) ke DTO. |
| E3 | **DILARANG** *lazy loading*. Muat relasi eksplisit (`Include`) hanya bila perlu. |
| E4 | List **WAJIB** diberi `OrderBy` deterministik dengan *tie-breaker* `Id` sebelum `Skip/Take`. |
| E5 | Cegah N+1: **DILARANG** query di dalam loop. |
| E6 | Entity yang diubah konkuren (`Subscription`, `Invoice`) **WAJIB** memiliki `RowVersion`. |
| E7 | Migrasi **WAJIB** ditinjau manual, bernama deskriptif (`20261019_AddInvoiceIdempotencyIndexes`), dan tidak diedit setelah di-merge. |
| E8 | `EnableRetryOnFailure` aktif pada SQL Server; operasi dalam transaksi manual **WAJIB** dibungkus `CreateExecutionStrategy().ExecuteAsync(...)`. |
| E9 | SQL mentah **WAJIB** terparameter (`FromSql`/`ExecuteSql` interpolasi aman); **DILARANG** konkatenasi string. |
| E10 | Hak DB aplikasi pada `AuditLogs` hanya `INSERT` dan `SELECT`. |

### 1.7 API & Error Handling Standards

- Versi API pada path: `/api/v1/...`.
- Error **WAJIB** berformat **ProblemDetails (RFC 7807)** lewat *global exception handler*; **DILARANG** membocorkan stack trace atau pesan exception internal ke klien.
- Pemetaan status: `400` validasi · `401/403` auth · `404` tidak ditemukan · `409` konflik (duplikat, transisi status tidak valid, concurrency) · `429` rate limit · `202` job diterima · `503` dependensi bermasalah.
- Validasi input memakai **FluentValidation** di batas API; invarian domain tetap dijaga di Domain.
- Konfigurasi **WAJIB** lewat Options pattern dengan `ValidateOnStart()`; **DILARANG** membaca `IConfiguration` langsung di service bisnis.
- **Logging terstruktur:** `_logger.LogInformation("Invoice {InvoiceId} issued for {SubscriptionId}", id, subId)`; **DILARANG** interpolasi string pada pesan log dan **DILARANG** mencatat PII (email, alamat) atau secret.

### 1.8 Struktur Lapisan

| Lapisan | Boleh bergantung pada | Isi |
|---|---|---|
| Domain | — | Entity, enum, state machine, domain exception |
| Application | Domain | Service/use case, DTO, validator, interface, message contract |
| Infrastructure | Application, Domain | EF Core, QueueClient, implementasi interface |
| Api / Worker | Semua (composition root) | Controller/endpoint, hosted service, DI, konfigurasi |

---

## 2. Business Logic Constraints

### 2.1 Aturan Idempotency (Mencegah Tagihan Ganda)

| ID | Aturan |
|---|---|
| **BR-01** | Satu faktur **WAJIB** memiliki `IdempotencyKey` berformat `INV:{SubscriptionId}:{BillingPeriodStart:yyyyMMdd}`. Key dihasilkan **hanya** oleh `IdempotencyKeyFactory` (satu sumber kebenaran), bukan dirakit manual. |
| **BR-02** | Database **WAJIB** memiliki unique index pada `Invoices.IdempotencyKey` **dan** pada (`SubscriptionId`, `BillingPeriodStart`). Unique index adalah penjaga otoritatif; pengecekan di aplikasi hanyalah *fast path*. |
| **BR-03** | Hanya langganan `Status = Active` dengan `NextBillingDate ≤ AsOfDate` yang boleh ditagih; `Paused`, `Cancelled`, `Expired` tidak pernah ditagih. |
| **BR-04** | Satu pesan = satu (langganan, periode). `BillingPeriodStart` pada pesan **WAJIB** sama dengan `Subscription.NextBillingDate`; bila tidak sama, pesan dianggap *stale* → `InvoiceJobSkipped`. |
| **BR-05** | Pemrosesan duplikat **WAJIB** mengembalikan hasil sukses **tanpa efek samping** (tidak ada invoice/audit baru) dan menghapus pesan. |
| **BR-06** | `DbUpdateException` karena pelanggaran unique (SQL error **2601** / **2627**) **WAJIB** diperlakukan sebagai *duplikat sukses*, bukan error. |
| **BR-07** | Perhitungan `NextBillingDate` **WAJIB** dari `StartDate` + `n × siklus` (dengan *clamp* akhir bulan), **DILARANG** menambah dari `NextBillingDate` sebelumnya (mencegah *drift*). |
| **BR-08** | `Amount` dan `Currency` invoice **WAJIB** disalin (*snapshot*) dari Plan saat penerbitan; perubahan Plan tidak boleh memengaruhi invoice yang sudah terbit. |
| **BR-09** | Nomor faktur **WAJIB** dari `SEQUENCE` database (`INV-yyyyMM-######`); **DILARANG** `MAX()+1`. |
| **BR-10** | Catch-up setelah downtime: scan hanya mengirim **satu periode berikutnya** per langganan per siklus; periode berikutnya diambil pada scan selanjutnya (tidak ada penagihan massal sekaligus). |
| **BR-11** | `Resume` langganan menetapkan `NextBillingDate = max(NextBillingDate, hari bisnis ini)` — masa jeda tidak ditagih mundur. |
| **BR-12** | Hari bisnis dievaluasi pada zona `Billing:BusinessTimeZone` (default `Asia/Jakarta`); timestamp teknis tetap UTC. |

**Rumus periode (referensi):**

```csharp
public static class BillingPeriodCalculator
{
    public static DateOnly GetPeriodStart(DateOnly anchor, BillingCycle cycle, int periodIndex)
        => anchor.AddMonths(MonthsOf(cycle) * periodIndex);   // AddMonths melakukan clamp akhir bulan

    public static DateOnly GetPeriodEnd(DateOnly anchor, BillingCycle cycle, int periodIndex)
        => GetPeriodStart(anchor, cycle, periodIndex + 1).AddDays(-1);

    private static int MonthsOf(BillingCycle cycle) => cycle switch
    {
        BillingCycle.Monthly   => 1,
        BillingCycle.Quarterly => 3,
        BillingCycle.Yearly    => 12,
        _ => throw new ArgumentOutOfRangeException(nameof(cycle))
    };
}
```

**Implementasi referensi — penerbitan invoice idempoten & atomik:**

```csharp
public async Task<IssueResult> IssueAsync(InvoiceJobMessage msg, CancellationToken ct)
{
    // Fast path (murah). Bukan penjaga utama.
    if (await _db.Invoices.AsNoTracking().AnyAsync(i => i.IdempotencyKey == msg.IdempotencyKey, ct))
        return IssueResult.DuplicateSkipped;

    var strategy = _db.Database.CreateExecutionStrategy();

    return await strategy.ExecuteAsync(async () =>
    {
        _db.ChangeTracker.Clear();                                   // aman bila strategy mengulang lambda
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var sub = await _db.Subscriptions.Include(s => s.Plan)
            .SingleOrDefaultAsync(s => s.Id == msg.SubscriptionId, ct);

        if (sub is null || !sub.IsBillableFor(msg.BillingPeriodStart))
            return IssueResult.Skipped;                              // business skip (diaudit oleh pemanggil)

        var seq = await _db.Database
            .SqlQuery<long>($"SELECT NEXT VALUE FOR billing.seq_InvoiceNumber AS [Value]")
            .SingleAsync(ct);

        var invoice = Invoice.Issue(sub, msg.BillingPeriodStart, msg.BillingPeriodEnd, seq,
                                    msg.IdempotencyKey, _clock.GetUtcNow(), _options.PaymentTermDays);

        _db.Invoices.Add(invoice);
        _db.AuditLogs.Add(AuditLog.ForInvoiceIssued(invoice, "System", msg.CorrelationId, _clock.GetUtcNow()));
        sub.AdvanceNextBillingDate();                                // dihitung dari StartDate (BR-07)

        try
        {
            await _db.SaveChangesAsync(ct);                          // Invoice + AuditLog + Subscription: satu SaveChanges
            await tx.CommitAsync(ct);
            return IssueResult.Created;
        }
        catch (DbUpdateException ex) when (SqlErrors.IsUniqueViolation(ex))   // BR-06
        {
            await tx.RollbackAsync(ct);
            return IssueResult.DuplicateSkipped;
        }
    });
}
```

### 2.2 Error Handling & Retry Policy (Queue Message)

#### 2.2.1 Klasifikasi Error

| Kelas | Contoh | Tindakan | Pesan queue |
|---|---|---|---|
| **Duplicate** | Unique violation, IdempotencyKey sudah ada | Anggap sukses | **Dihapus** |
| **Business skip** | Langganan tidak ditemukan/dibatalkan, periode *stale*, plan nonaktif | `AuditLog InvoiceJobSkipped` | **Dihapus** |
| **Transient** | Timeout SQL, deadlock (1205), koneksi putus, throttling storage | Retry dengan backoff | **Tidak dihapus**; `visibility = backoff` |
| **Malformed** | JSON rusak, `SchemaVersion` tak dikenal, field wajib kosong | Karantina langsung | **Ke poison queue**, dihapus dari queue utama |
| **Unknown** | Exception tak terduga | Diperlakukan transient sampai batas | Retry → poison setelah batas |

#### 2.2.2 Aturan

| ID | Aturan |
|---|---|
| **BR-20** | Pesan **WAJIB** dihapus **hanya setelah** transaksi database ter-commit (atau setelah keputusan *duplicate/skip/poison* tercatat). |
| **BR-21** | `MaxDequeueCount = 5`. Pesan dengan `DequeueCount > 5` **WAJIB** dipindah ke `billing-invoice-jobs-poison`. |
| **BR-22** | Backoff berjenjang berdasarkan `DequeueCount`: **30 detik → 2 menit → 10 menit → 30 menit**, ditambah jitter ±20% untuk menghindari *thundering herd*. |
| **BR-23** | Backoff diterapkan dengan `UpdateMessageAsync(visibilityTimeout)` (tanpa mengubah isi pesan) atau dengan membiarkan visibility timeout habis. |
| **BR-24** | Pesan *malformed* **DILARANG** di-retry; langsung ke poison. |
| **BR-25** | Pesan poison **WAJIB** dibungkus `PoisonEnvelope` (pesan asli, alasan, stack ringkas, `DequeueCount`, waktu) dan menghasilkan `AuditLog InvoiceJobDeadLettered`. |
| **BR-26** | `visibilityTimeout` pemrosesan (default 120 dtk) **WAJIB** lebih besar daripada durasi pemrosesan terburuk; handler **WAJIB** punya timeout internal lebih pendek. |
| **BR-27** | Pesan memiliki TTL (`MessageTtlDays`, default 7 hari). Pesan kedaluwarsa tidak diproses ulang. |
| **BR-28** | Operasi poison tidak boleh menghilangkan pesan: **kirim ke poison queue dahulu**, baru hapus dari queue utama (jika gagal di tengah, idempotency melindungi dari duplikasi). |
| **BR-29** | Kegagalan enqueue pada API **WAJIB** menghasilkan `AuditLog BillingRunEnqueueFailed` dan respons `503` (bukan `202`). |
| **BR-30** | Worker **WAJIB** *graceful*: saat `stoppingToken` aktif, berhenti mengambil pesan baru; pesan berjalan boleh tuntas atau dibiarkan kembali ke queue. |

```csharp
public static TimeSpan GetBackoff(long dequeueCount, Random rng)
{
    var baseDelay = dequeueCount switch
    {
        <= 1 => TimeSpan.FromSeconds(30),
        2    => TimeSpan.FromMinutes(2),
        3    => TimeSpan.FromMinutes(10),
        _    => TimeSpan.FromMinutes(30)
    };
    var jitter = 1 + (rng.NextDouble() * 0.4 - 0.2);                // ±20%
    return TimeSpan.FromTicks((long)(baseDelay.Ticks * jitter));
}
```

### 2.3 Transaksi Database (Atomic: Invoice + AuditLog)

| ID | Aturan |
|---|---|
| **BR-40** | Pembuatan **Invoice + AuditLog (`InvoiceIssued`) + pergeseran `Subscription.NextBillingDate`** **WAJIB** dalam **satu transaksi atomik** (satu `SaveChanges` atau satu `BeginTransaction`). Gagal sebagian = *rollback* total. |
| **BR-41** | Setiap perubahan status invoice (`InvoiceStatusChanged`) **WAJIB** menulis perubahan status dan audit log dalam **satu transaksi**. |
| **BR-42** | **DILARANG** mengirim pesan queue **di dalam** transaksi DB. Urutan di API: *commit audit* → *enqueue*. Konsistensi lintas-sistem ditangani idempotency + audit `EnqueueFailed` (Outbox adalah peningkatan roadmap). |
| **BR-43** | Transaksi **WAJIB** singkat: tidak ada I/O eksternal (HTTP/queue) di dalamnya. |
| **BR-44** | Isolation level default `READ COMMITTED`; kebenaran diandalkan pada **unique constraint + `RowVersion`**, bukan pada isolation tinggi. |
| **BR-45** | Konflik concurrency (`DbUpdateConcurrencyException`) pada API → `409 Conflict`; pada Worker → diperlakukan *transient* (retry) lalu ditangani idempotency. |
| **BR-46** | Batch Overdue Sweeper memproses maksimal **500 invoice per transaksi**; tiap perubahan status tetap menghasilkan audit log dalam transaksi yang sama. |
| **BR-47** | `AuditLogs` **append-only**: tidak ada `UPDATE`/`DELETE` dari aplikasi (dibatasi di level hak DB). |

### 2.4 Aturan State Machine Invoice

| Dari \ Ke | Pending | Paid | Overdue | Failed |
|---|:---:|:---:|:---:|:---:|
| **(baru)** | ✅ | — | — | — |
| **Pending** | ❌ | ✅ | ✅ (otomatis) | ✅ |
| **Paid** | ❌ | ❌ | ❌ | ❌ |
| **Overdue** | ❌ | ✅ | ❌ | ✅ (otomatis > grace) |
| **Failed** | ✅ (retry manual) | ❌ | ❌ | ❌ |

| ID | Aturan |
|---|---|
| **BR-50** | Transisi hanya boleh melalui method domain `Invoice.TransitionTo(newStatus, ...)`. **DILARANG** mengubah `Status` langsung. Transisi tidak valid → `InvalidStatusTransitionException` → `409`. |
| **BR-51** | `Paid` bersifat **terminal & immutable**; `PaidAt` **WAJIB** terisi saat menjadi `Paid`. |
| **BR-52** | Transisi ke `Failed` **WAJIB** menyertakan `FailureReason`. |
| **BR-53** | `Pending → Overdue` otomatis bila `DueDate < tanggal bisnis hari ini`. `Overdue → Failed` otomatis bila `DueDate + OverdueGraceDays (30)` terlewati. |
| **BR-54** | `Failed → Pending` (retry manual) **WAJIB** diaudit dan **tidak** mengubah `DueDate` secara otomatis kecuali diminta eksplisit. |

```csharp
public void TransitionTo(InvoiceStatus target, DateTimeOffset now, string? reason = null)
{
    if (!AllowedTransitions.Contains((Status, target)))
        throw new InvalidStatusTransitionException(InvoiceNumber, Status, target);

    if (target == InvoiceStatus.Failed && string.IsNullOrWhiteSpace(reason))
        throw new DomainValidationException("FailureReason wajib untuk status Failed.");

    Status = target;
    PaidAt = target == InvoiceStatus.Paid ? now : PaidAt;
    FailureReason = target == InvoiceStatus.Failed ? reason : null;
    UpdatedAt = now;
}

private static readonly HashSet<(InvoiceStatus From, InvoiceStatus To)> AllowedTransitions =
[
    (InvoiceStatus.Pending, InvoiceStatus.Paid),
    (InvoiceStatus.Pending, InvoiceStatus.Overdue),
    (InvoiceStatus.Pending, InvoiceStatus.Failed),
    (InvoiceStatus.Overdue, InvoiceStatus.Paid),
    (InvoiceStatus.Overdue, InvoiceStatus.Failed),
    (InvoiceStatus.Failed,  InvoiceStatus.Pending),
];
```

### 2.5 Aturan Langganan & Master Data

| ID | Aturan |
|---|---|
| **BR-60** | Subscription hanya boleh dibuat untuk Customer **aktif** dan Plan **aktif**. |
| **BR-61** | Plan yang pernah dipakai **DILARANG** dihapus fisik; hanya dinonaktifkan. Customer dengan langganan `Active/Paused` **DILARANG** dinonaktifkan. |
| **BR-62** | `Cancel` bersifat final: langganan `Cancelled` tidak dapat diaktifkan kembali (buat langganan baru). Faktur yang sudah ada tetap berlaku. |
| **BR-63** | Langganan yang melewati `EndDate` diubah menjadi `Expired` oleh Scheduler dan tidak ditagih lagi. |
| **BR-64** | Email Customer unik (case-insensitive) dan disimpan *lowercase*. |

### 2.6 Aturan Keamanan Data

| ID | Aturan |
|---|---|
| **BR-70** | Semua endpoint (kecuali health) **WAJIB** `[Authorize]`; operasi tulis memerlukan role `FinanceAdmin`. |
| **BR-71** | Endpoint trigger **WAJIB** rate-limited (1 req/menit/user) dan mengembalikan `Retry-After` pada `429`. |
| **BR-72** | Secret **DILARANG** masuk repositori; gunakan User Secrets / Key Vault / environment variable. |
| **BR-73** | Log dan audit **DILARANG** menyimpan PII mentah; gunakan ID. `OldValue/NewValue` pada audit untuk Customer **WAJIB** me-*mask* email/alamat. |
| **BR-74** | `asOfDate` (simulasi) hanya diterima bila `Billing:AllowSimulatedDate = true`; selain itu `400`. |

---

## 3. Git Commit Conventions

Proyek memakai **[Conventional Commits 1.0.0](https://www.conventionalcommits.org/)**.

### 3.1 Format

```
<type>(<scope>): <deskripsi singkat imperatif>

[body — apa & mengapa, bukan bagaimana]

[footer — BREAKING CHANGE / Refs #issue]
```

### 3.2 Type

| Type | Kapan dipakai | Efek SemVer |
|---|---|---|
| `feat` | Fitur baru | MINOR |
| `fix` | Perbaikan bug | PATCH |
| `docs` | Perubahan dokumentasi saja | — |
| `refactor` | Perubahan struktur kode tanpa mengubah perilaku | — |
| `test` | Menambah/memperbaiki test | — |
| `perf` | Peningkatan performa | PATCH |
| `build` | Build system, dependensi, Docker | — |
| `ci` | Konfigurasi CI/CD | — |
| `chore` | Pekerjaan rutin non-produksi | — |
| `style` | Format/whitespace (tanpa perubahan logika) | — |
| `revert` | Membatalkan commit sebelumnya | — |

### 3.3 Scope yang Dipakai

`api` · `worker` · `domain` · `application` · `infra` · `db` (migrasi/skema) · `queue` · `billing` · `invoice` · `subscription` · `audit` · `ui` · `docs` · `deps`

### 3.4 Aturan

1. Subjek **WAJIB** imperatif, huruf kecil di awal, **tanpa titik**, ≤ **72 karakter**.
2. Satu commit = **satu perubahan logis** yang dapat di-*build* dan lulus test.
3. Body di-*wrap* pada 100 karakter dan menjelaskan **motivasi** serta dampak.
4. Perubahan yang memutus kompatibilitas **WAJIB** ditandai `!` setelah scope **dan/atau** footer `BREAKING CHANGE:`.
5. Referensikan issue pada footer: `Refs #12` / `Closes #12`.
6. **DILARANG** commit yang menyertakan secret, file build (`bin/`, `obj/`), atau perubahan format massal bercampur logika.
7. Migrasi EF Core **WAJIB** di commit terpisah dengan type `feat(db)` / `fix(db)`.

### 3.5 Contoh

```text
feat(billing): add idempotency key factory for invoice jobs

Key format INV:{subscriptionId}:{yyyyMMdd} is generated in one place to
prevent drift between producer and consumer.

Refs #14
```

```text
feat(db): add unique indexes for invoice idempotency and period
fix(worker): treat sql unique violation as duplicate success
fix(api): return 409 on invalid invoice status transition
feat(queue): add exponential backoff with jitter for invoice jobs
feat(worker): add overdue sweeper to billing scheduler
refactor(application): extract billing period calculator
perf(db): add filtered index for due subscription scan
test(worker): cover concurrent duplicate message processing
docs: add architecture sequence diagrams for manual trigger
build(docker): add azurite and sqlserver to compose file
chore(deps): bump Azure.Storage.Queues to latest patch
feat(api)!: rename billing trigger payload field asOf to asOfDate

BREAKING CHANGE: clients must send asOfDate instead of asOf.
```

### 3.6 Branching & Pull Request

| Elemen | Aturan |
|---|---|
| **Branch utama** | `main` (selalu *deployable*, dilindungi) |
| **Penamaan branch** | `feat/<ringkas>`, `fix/<ringkas>`, `docs/<ringkas>`, `refactor/<ringkas>` — contoh: `feat/invoice-idempotency` |
| **Merge** | *Squash merge* dengan judul PR mengikuti Conventional Commits |
| **PR** | Deskripsi: konteks, perubahan, cara uji, dampak skema DB/queue; centang checklist di bagian 5 |
| **Rilis** | Tag SemVer (`v1.0.0`) dengan *changelog* otomatis dari commit |

---

## 4. Testing Rules

| Lapisan | Jenis | Alat | Wajib mencakup |
|---|---|---|---|
| Domain | Unit | xUnit + FluentAssertions | State machine (semua sel matriks 2.4), `BillingPeriodCalculator` (akhir bulan, tahun kabisat), invarian entity |
| Application | Unit | xUnit + Moq/NSubstitute | Use case, klasifikasi error, `GetBackoff`, validator |
| Infrastructure/Worker | Integration | Testcontainers (SQL Server) + Azurite | Unique index, transaksi atomik (rollback saat audit gagal), retry → poison |
| End-to-end | Integration | `WebApplicationFactory` + Worker in-process | Trigger → invoice terbit; **trigger berulang tetap 1 invoice** |
| Konkurensi | Integration | `Task.WhenAll` | **N pesan identik paralel → tepat 1 invoice** (BR-02, BR-06) |

**Aturan:**

- Test **WAJIB** deterministik: gunakan `FakeTimeProvider` (`Microsoft.Extensions.TimeProvider.Testing`), bukan jam sistem.
- Pola *Arrange–Act–Assert*; satu perilaku per test; nama `Method_Scenario_ExpectedResult`.
- Cakupan target ≥ **80%** pada `Domain` dan `Application`.
- Bug yang diperbaiki **WAJIB** disertai test regresi.

---

## 5. Definition of Done & Review Checklist

### 5.1 Definition of Done

- [ ] Build bersih tanpa warning (`TreatWarningsAsErrors`).
- [ ] Semua test lulus; test baru untuk perilaku baru.
- [ ] Mengikuti naming, async, dan EF Core rules (bagian 1).
- [ ] Aturan bisnis terkait (BR-xx) dipenuhi dan, bila perlu, diuji.
- [ ] Perubahan skema disertai migrasi yang ditinjau + indeks yang tepat.
- [ ] Perubahan kontrak API/queue didokumentasikan di `Architecture.md` (dan versi message dinaikkan bila perlu).
- [ ] Tidak ada secret/PII pada kode, log, atau audit.
- [ ] Commit mengikuti Conventional Commits.

### 5.2 Checklist Reviewer (Hal yang Paling Sering Salah)

- [ ] Adakah `.Result`/`.Wait()`/`async void`? `CancellationToken` diteruskan?
- [ ] Query baca memakai `AsNoTracking` + proyeksi? Ada N+1?
- [ ] Invoice + AuditLog + NextBillingDate dalam **satu transaksi**?
- [ ] Pesan dihapus **setelah** commit? Duplikat ditangani sebagai sukses?
- [ ] Error diklasifikasikan benar (transient vs malformed vs business)?
- [ ] Transisi status melalui `TransitionTo`, bukan set langsung?
- [ ] Penggunaan `TimeProvider`, bukan `DateTime.UtcNow`?
- [ ] Uang memakai `decimal`?
