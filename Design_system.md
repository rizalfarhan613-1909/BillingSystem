# Design System

# Automated Billing System — Spesifikasi UI Admin Dashboard

| Atribut | Nilai |
|---|---|
| **Versi dokumen** | 1.0 |
| **Tanggal** | 19 Oktober 2026 |
| **Pengguna utama** | Admin Keuangan (`FinanceAdmin`) dan `ReadOnlyAuditor` |
| **Stack UI** | Tailwind CSS 3.4+ (framework-agnostic: React/Vue/Blazor/Razor) |
| **Dokumen terkait** | [PRD.md](./PRD.md) · [Architecture.md](./Architecture.md) · [Rules.md](./Rules.md) |

---

## Daftar Isi

1. [Design Principles](#1-design-principles)
2. [Design Tokens: Warna & Tipografi](#2-design-tokens-warna--tipografi)
3. [Status Badge](#3-status-badge)
4. [Component Specs](#4-component-specs)
5. [Layout & Responsiveness](#5-layout--responsiveness)
6. [Aksesibilitas, Lokalisasi & Interaksi](#6-aksesibilitas-lokalisasi--interaksi)
7. [Pemetaan Komponen ke API](#7-pemetaan-komponen-ke-api)

---

## 1. Design Principles

Dashboard dipakai Admin Keuangan untuk keputusan yang menyangkut uang. Karena itu desain mengutamakan **kejelasan, ketepatan, dan kepercayaan** dibanding dekorasi.

| # | Prinsip | Penerapan |
|---|---|---|
| 1 | **Kejelasan sebelum estetika** | Hierarki visual tegas; angka penting paling besar; sedikit warna dengan makna tetap. |
| 2 | **Status terbaca sekilas** | Warna konsisten untuk status (hijau/kuning/merah) **dipadukan dengan ikon dan label teks** — tidak mengandalkan warna saja. |
| 3 | **Akurasi angka** | Angka uang rata kanan, `tabular-nums`, format `id-ID` (Rp), tanpa pembulatan menyesatkan. |
| 4 | **Aksi berisiko butuh konfirmasi** | Mengubah status invoice dan memicu job penagihan selalu melewati dialog konfirmasi yang menjelaskan dampaknya. |
| 5 | **Umpan balik instan, tanpa menebak** | Setiap aksi punya state: *loading*, *success*, *error*, *empty*. Operasi asinkron (`202 Accepted`) dijelaskan sebagai "diterima & sedang diproses". |
| 6 | **Kepadatan informasi yang nyaman** | Tabel padat tetapi lega untuk dibaca; baris 48 px; pemisah halus. |
| 7 | **Dapat dipercaya & dapat ditelusuri** | Setiap invoice menyediakan riwayat audit; tampilkan `runId` dan waktu pada hasil aksi. |
| 8 | **Aksesibel secara default** | Kontras WCAG AA, navigasi keyboard penuh, fokus terlihat, `aria-live` untuk toast dan hasil. |
| 9 | **Responsif tanpa kehilangan fungsi** | Seluruh fungsi tersedia di tablet; di ponsel tabel berubah menjadi daftar kartu. |
| 10 | **Konsistensi** | Semua komponen memakai token yang sama (warna, radius, spasi, bayangan). |

---

## 2. Design Tokens: Warna & Tipografi

### 2.1 Konfigurasi Tailwind

```js
// tailwind.config.js
const defaultTheme = require('tailwindcss/defaultTheme');
const colors = require('tailwindcss/colors');

/** @type {import('tailwindcss').Config} */
module.exports = {
  content: ['./src/**/*.{html,js,ts,jsx,tsx,vue,razor,cshtml}'],
  theme: {
    extend: {
      fontFamily: {
        sans: ['Inter', ...defaultTheme.fontFamily.sans],
        mono: ['JetBrains Mono', ...defaultTheme.fontFamily.mono],
      },
      colors: {
        brand: colors.indigo,        // aksi utama, fokus, link
        surface: colors.slate,       // latar, border, teks netral
      },
      borderRadius: { card: '0.75rem' },
      boxShadow: { card: '0 1px 2px 0 rgb(15 23 42 / 0.06), 0 1px 3px 0 rgb(15 23 42 / 0.08)' },
    },
  },
  plugins: [require('@tailwindcss/forms')],
};
```

### 2.2 Palet Warna

#### Warna Netral & Brand

| Token | Tailwind | Hex | Penggunaan |
|---|---|---|---|
| `bg-app` | `bg-slate-50` | `#F8FAFC` | Latar halaman |
| `bg-surface` | `bg-white` | `#FFFFFF` | Kartu, tabel, panel |
| `border-default` | `border-slate-200` | `#E2E8F0` | Pemisah, border kartu/tabel |
| `text-primary` | `text-slate-900` | `#0F172A` | Judul, angka utama |
| `text-secondary` | `text-slate-600` | `#475569` | Teks pendukung |
| `text-muted` | `text-slate-500` | `#64748B` | Label, placeholder, metadata |
| `brand-primary` | `bg-brand-600` / `hover:bg-brand-700` | `#4F46E5` / `#4338CA` | Tombol utama, item navigasi aktif |
| `brand-ring` | `ring-brand-500` | `#6366F1` | Fokus ring |
| `sidebar-bg` | `bg-slate-900` | `#0F172A` | Sidebar (gelap) |

#### Warna Semantik Status (Status Badge Colors)

| Status | Makna | Warna | Latar | Teks | Ring/Border | Ikon |
|---|---|---|---|---|---|---|
| **Paid** | Lunas | 🟢 **Green** | `bg-green-50` (`#F0FDF4`) | `text-green-700` (`#15803D`) | `ring-green-600/20` | Check-circle |
| **Pending** | Menunggu pembayaran | 🟡 **Yellow** | `bg-yellow-50` (`#FEFCE8`) | `text-yellow-800` (`#854D0E`) | `ring-yellow-600/30` | Clock |
| **Overdue** | Lewat jatuh tempo | 🔴 **Red** | `bg-red-50` (`#FEF2F2`) | `text-red-700` (`#B91C1C`) | `ring-red-600/20` | Alert-triangle |
| **Failed** | Pembayaran gagal | 🔴 **Red (tegas)** | `bg-red-100` (`#FEE2E2`) | `text-red-800` (`#991B1B`) | `ring-red-700/30` | X-circle |

> **Pembeda Overdue vs Failed:** keduanya merah sesuai kebutuhan produk. Dibedakan oleh **ikon, label teks**, dan tingkat kepekatan (Failed lebih pekat). Warna tidak pernah menjadi satu-satunya pembawa makna (WCAG 1.4.1).

#### Warna Status Langganan (pendukung)

| Status | Warna | Kelas |
|---|---|---|
| Active | Green | `bg-green-50 text-green-700 ring-green-600/20` |
| Paused | Yellow | `bg-yellow-50 text-yellow-800 ring-yellow-600/30` |
| Cancelled | Slate | `bg-slate-100 text-slate-700 ring-slate-500/20` |
| Expired | Slate | `bg-slate-100 text-slate-600 ring-slate-500/20` |

#### Warna Umpan Balik (Toast/Alert)

| Jenis | Kelas |
|---|---|
| Success | `bg-green-50 text-green-800 border-green-200` |
| Info (mis. job diterima) | `bg-brand-50 text-brand-800 border-brand-200` |
| Warning | `bg-yellow-50 text-yellow-800 border-yellow-200` |
| Error | `bg-red-50 text-red-800 border-red-200` |

### 2.3 Tipografi

| Peran | Font | Kelas Tailwind | Ukuran / Line-height | Bobot |
|---|---|---|---|---|
| Judul halaman (H1) | Inter | `text-2xl font-semibold tracking-tight text-slate-900` | 24 / 32 | 600 |
| Judul bagian (H2) | Inter | `text-lg font-semibold text-slate-900` | 18 / 28 | 600 |
| Judul kartu / label | Inter | `text-sm font-medium text-slate-500` | 14 / 20 | 500 |
| **Angka metrik (Summary Card)** | Inter | `text-3xl font-semibold tabular-nums text-slate-900` | 30 / 36 | 600 |
| Teks isi | Inter | `text-sm text-slate-700` | 14 / 20 | 400 |
| Teks tabel | Inter | `text-sm text-slate-700` | 14 / 20 | 400 |
| Header tabel | Inter | `text-xs font-semibold uppercase tracking-wide text-slate-500` | 12 / 16 | 600 |
| Angka uang di tabel | Inter | `text-sm font-medium tabular-nums text-right` | 14 / 20 | 500 |
| Nomor invoice, ID, runId | JetBrains Mono | `font-mono text-xs text-slate-600` | 12 / 16 | 400 |
| Badge | Inter | `text-xs font-medium` | 12 / 16 | 500 |
| Caption / helper | Inter | `text-xs text-slate-500` | 12 / 16 | 400 |

**Aturan tipografi:** semua angka uang **WAJIB** `tabular-nums` dan rata kanan di tabel; maksimum dua bobot font per layar (400/500/600); panjang baris teks bacaan ≤ 75 karakter.

### 2.4 Spasi, Radius & Elevasi

| Token | Nilai | Tailwind |
|---|---|---|
| Skala spasi | basis 4 px | `p-1`…`p-8` (4, 8, 12, 16, 20, 24, 32 px) |
| Padding kartu | 20–24 px | `p-5 sm:p-6` |
| Jarak antar kartu | 16–24 px | `gap-4 lg:gap-6` |
| Radius kartu / modal | 12 px | `rounded-xl` |
| Radius kontrol (input, tombol, badge) | 8 px / penuh | `rounded-lg` / `rounded-full` |
| Elevasi kartu | halus | `shadow-sm ring-1 ring-slate-900/5` |
| Elevasi modal / drawer | sedang | `shadow-xl` |

---

## 3. Status Badge

Satu komponen untuk seluruh status invoice.

```html
<!-- Paid -->
<span class="inline-flex items-center gap-1 rounded-full bg-green-50 px-2.5 py-1 text-xs font-medium text-green-700 ring-1 ring-inset ring-green-600/20">
  <svg class="h-3.5 w-3.5" aria-hidden="true"><!-- check-circle --></svg> Paid
</span>

<!-- Pending -->
<span class="inline-flex items-center gap-1 rounded-full bg-yellow-50 px-2.5 py-1 text-xs font-medium text-yellow-800 ring-1 ring-inset ring-yellow-600/30">
  <svg class="h-3.5 w-3.5" aria-hidden="true"><!-- clock --></svg> Pending
</span>

<!-- Overdue -->
<span class="inline-flex items-center gap-1 rounded-full bg-red-50 px-2.5 py-1 text-xs font-medium text-red-700 ring-1 ring-inset ring-red-600/20">
  <svg class="h-3.5 w-3.5" aria-hidden="true"><!-- alert-triangle --></svg> Overdue
</span>

<!-- Failed -->
<span class="inline-flex items-center gap-1 rounded-full bg-red-100 px-2.5 py-1 text-xs font-medium text-red-800 ring-1 ring-inset ring-red-700/30">
  <svg class="h-3.5 w-3.5" aria-hidden="true"><!-- x-circle --></svg> Failed
</span>
```

**Pemetaan di kode (sumber kebenaran tunggal):**

```ts
export const invoiceStatusStyle = {
  Paid:    { label: 'Paid',    classes: 'bg-green-50 text-green-700 ring-green-600/20',   icon: 'check-circle'   },
  Pending: { label: 'Pending', classes: 'bg-yellow-50 text-yellow-800 ring-yellow-600/30', icon: 'clock'          },
  Overdue: { label: 'Overdue', classes: 'bg-red-50 text-red-700 ring-red-600/20',          icon: 'alert-triangle' },
  Failed:  { label: 'Failed',  classes: 'bg-red-100 text-red-800 ring-red-700/30',         icon: 'x-circle'       },
} as const;
```

**Ketentuan:** nilai `status` dari API (`Pending|Paid|Overdue|Failed`) dipetakan 1:1; status tak dikenal tampil abu-abu dengan label apa adanya (tidak boleh crash).

---

## 4. Component Specs

### 4.1 Summary Cards

Tiga kartu di bagian atas dashboard, data dari `GET /api/v1/dashboard/summary`.

| Kartu | Nilai Utama | Info Sekunder | Ikon | Aksen |
|---|---|---|---|---|
| **Total Revenue** | Jumlah invoice `Paid` pada periode (mis. bulan ini), format `Rp 125.000.000` | Delta vs periode sebelumnya (▲ hijau / ▼ merah), mis. `▲ 12,4% dari bulan lalu` | Banknotes | Brand |
| **Pending Invoices** | Jumlah invoice `Pending` (`18`) | Total tertunggak `Rp 27.000.000` | Clock | Yellow |
| **Active Subscriptions** | Jumlah langganan `Active` (`342`) | Mis. "+8 minggu ini" (opsional) | Users/Repeat | Green |

**Anatomi & kelas:**

```html
<section class="rounded-xl bg-white p-5 shadow-sm ring-1 ring-slate-900/5 sm:p-6" aria-labelledby="card-revenue">
  <div class="flex items-start justify-between">
    <h3 id="card-revenue" class="text-sm font-medium text-slate-500">Total Revenue</h3>
    <span class="flex h-9 w-9 items-center justify-center rounded-lg bg-brand-50 text-brand-600">
      <svg class="h-5 w-5" aria-hidden="true"><!-- banknotes --></svg>
    </span>
  </div>
  <p class="mt-3 text-3xl font-semibold tabular-nums text-slate-900">Rp 125.000.000</p>
  <p class="mt-1 flex items-center gap-1 text-xs text-slate-500">
    <span class="font-medium text-green-700">▲ 12,4%</span> dari bulan lalu
  </p>
</section>
```

**State:**

| State | Tampilan |
|---|---|
| Loading | *Skeleton* (`animate-pulse`, blok abu `bg-slate-200`) setinggi kartu; tidak menggeser layout |
| Sukses | Nilai + info sekunder |
| Empty (data 0) | Nilai `0` / `Rp 0` + teks bantu "Belum ada data pada periode ini" |
| Error | Kartu dengan pesan "Gagal memuat" + tombol *Coba lagi* |

**Perilaku:** kartu **Pending Invoices** dapat diklik → membuka tabel invoice dengan filter `status=Pending` (kartu bertindak sebagai pintasan filter). Pembaruan data: saat halaman dimuat, setelah aksi manual trigger/ubah status, dan polling opsional tiap 60 detik.

**Tata letak grid:** `grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3 lg:gap-6`.

---

### 4.2 Invoice Data Table

Komponen utama halaman **Invoices** dan bagian bawah Dashboard. Seluruh filter, pencarian, sorting, dan pagination dijalankan **di server** (`GET /api/v1/invoices`).

#### 4.2.1 Toolbar

| Elemen | Spesifikasi |
|---|---|
| **Search** | Input dengan ikon cari, placeholder "Cari nomor invoice, nama, atau email…"; *debounce* 300 ms; minimal 2 karakter; tombol hapus (×); parameter `search`. |
| **Filter status** | Segmented chips/tab: **Semua · Pending · Paid · Overdue · Failed**, masing-masing menampilkan jumlah bila tersedia. Mendukung multi-pilih (`status=Pending,Overdue`). Chip aktif: `bg-brand-600 text-white`. |
| **Filter tambahan (opsional)** | Rentang `DueDate` (date range), dropdown Customer. |
| **Reset filter** | Tautan "Reset" muncul saat ada filter aktif. |
| **Ekspor (opsional v1.1)** | Tombol sekunder CSV. |

#### 4.2.2 Kolom

| Kolom | Rata | Sortable | Isi | Parameter sort |
|---|---|:---:|---|---|
| Invoice No. | Kiri | ✅ | `INV-202610-000123` (mono, tautan ke detail) | `invoiceNumber` |
| Customer | Kiri | ✅ | Nama (tebal) + email (muted, `text-xs`) | `customerName` |
| Plan | Kiri | ❌ | Nama plan + siklus | — |
| Periode | Kiri | ✅ | `01 Okt – 31 Okt 2026` | `periodStart` |
| Jumlah | **Kanan** | ✅ | `Rp 1.500.000` (`tabular-nums`) | `amount` |
| Jatuh Tempo | Kiri | ✅ | `15 Okt 2026`; merah bila lewat dan belum `Paid` | `dueDate` |
| Status | Kiri | ✅ | Status Badge | `status` |
| Aksi | Kanan | ❌ | Menu `⋯` (lihat 4.2.4) | — |

- **Sort default:** `dueDate` ascending (yang paling mendesak di atas), tie-breaker `id`.
- Header kolom sortable adalah `<button>` dengan ikon panah dan `aria-sort="ascending|descending|none"`; siklus klik: asc → desc → netral.

#### 4.2.3 Pagination

| Elemen | Spesifikasi |
|---|---|
| Ukuran halaman | Dropdown `10 / 25 / 50` (default **10**, maksimum 100 sesuai API) |
| Navigasi | Sebelumnya · nomor halaman (dengan elipsis) · Berikutnya |
| Info | "Menampilkan **1–10** dari **248** invoice" |
| Sinkronisasi URL | State ditulis ke query string (`?status=Overdue&search=budi&sortBy=dueDate&sortDir=asc&page=2&pageSize=10`) agar dapat di-*bookmark*/dibagikan dan tombol *Back* berfungsi |
| Reset halaman | Mengubah filter/search/pageSize mengembalikan ke halaman 1 |

#### 4.2.4 Aksi Baris & Aturan Status

Menu aksi hanya menampilkan transisi yang **valid** menurut state machine ([Rules.md §2.4](./Rules.md#24-aturan-state-machine-invoice)); aksi lain disembunyikan/dinonaktifkan.

| Status saat ini | Aksi tersedia |
|---|---|
| Pending | Lihat detail · **Tandai Paid** · **Tandai Failed** (wajib alasan) |
| Overdue | Lihat detail · **Tandai Paid** · **Tandai Failed** (wajib alasan) |
| Failed | Lihat detail · **Retry** (kembali ke Pending) |
| Paid | Lihat detail saja |

- Setiap aksi ubah status membuka **dialog konfirmasi** (ringkasan invoice, status lama → baru, kolom alasan untuk `Failed`).
- Hasil: toast sukses, baris diperbarui (*optimistic UI* dengan rollback bila `409`), pesan error dari `ProblemDetails.detail`.
- **Detail invoice** (drawer kanan, `max-w-md`): data lengkap + **timeline riwayat audit** (`GET /audit-logs?entityName=Invoice&entityId=…`).
- Role `ReadOnlyAuditor`: aksi ubah status disembunyikan.

#### 4.2.5 State Tabel

| State | Tampilan |
|---|---|
| Loading | 5–10 baris *skeleton*; header tetap tampil |
| Empty (tanpa data) | Ilustrasi sederhana + "Belum ada invoice. Picu job penagihan untuk menerbitkan invoice." + tombol **Run Billing Job** |
| Empty (hasil filter nol) | "Tidak ada invoice yang cocok dengan filter." + tombol **Reset filter** |
| Error | Banner merah di atas tabel + tombol *Coba lagi* (data lama dipertahankan bila ada) |
| Refreshing | Bar progres tipis di atas tabel; baris tetap terlihat (tanpa kedipan) |

#### 4.2.6 Kelas Tailwind Tabel (ringkas)

```html
<div class="overflow-x-auto rounded-xl bg-white shadow-sm ring-1 ring-slate-900/5">
  <table class="min-w-full divide-y divide-slate-200">
    <thead class="bg-slate-50">
      <tr>
        <th scope="col" class="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500">Invoice No.</th>
        <!-- ... -->
        <th scope="col" class="px-4 py-3 text-right text-xs font-semibold uppercase tracking-wide text-slate-500">Jumlah</th>
      </tr>
    </thead>
    <tbody class="divide-y divide-slate-100 bg-white">
      <tr class="h-12 hover:bg-slate-50 focus-within:bg-slate-50">
        <td class="whitespace-nowrap px-4 py-3 font-mono text-xs text-brand-700">INV-202610-000123</td>
        <!-- ... -->
        <td class="whitespace-nowrap px-4 py-3 text-right text-sm font-medium tabular-nums text-slate-900">Rp 1.500.000</td>
      </tr>
    </tbody>
  </table>
</div>
```

---

### 4.3 Manual Trigger Button (Simulasi Billing Job)

Komponen untuk menjalankan `POST /api/v1/billing/trigger`. Penempatan: **header halaman Dashboard/Invoices** (kanan atas) sebagai tombol primer.

#### 4.3.1 Tombol

```html
<button type="button"
  class="inline-flex items-center gap-2 rounded-lg bg-brand-600 px-4 py-2.5 text-sm font-semibold text-white shadow-sm
         hover:bg-brand-700 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-brand-600
         disabled:cursor-not-allowed disabled:bg-slate-300">
  <svg class="h-4 w-4" aria-hidden="true"><!-- play --></svg>
  Run Billing Job
</button>
```

#### 4.3.2 Alur Interaksi

```
[Idle] ──klik──► [Dialog Konfirmasi] ──"Jalankan"──► [Submitting] ──202──► [Accepted]
                       │ "Batal"                          │ 429                │
                       ▼                                  ▼                    ▼
                    [Idle]                           [Cooldown]        toast + refresh data
                                                          │ 5xx/4xx
                                                          ▼
                                                      [Error toast]
```

#### 4.3.3 Dialog Konfirmasi

| Elemen | Spesifikasi |
|---|---|
| Judul | "Jalankan job penagihan?" |
| Penjelasan | "Sistem akan memindai langganan yang jatuh tempo dan menerbitkan invoice di latar belakang. Invoice yang sudah ada **tidak akan ditagih dua kali**." |
| **Mode simulasi** | Input tanggal **"Simulasikan sebagai tanggal"** (`<input type="date">`, label bantu "Hanya untuk demo/simulasi"). **Hanya tampil bila** `meta.simulationEnabled = true`. Dikirim sebagai `asOfDate`. Bila diisi, tampil banner kuning di dialog: "Mode simulasi — tanggal bisnis dianggap *{tanggal}*." |
| Tombol | **Jalankan** (primer) · **Batal** (sekunder) |
| Fokus | Fokus awal pada **Batal** (mencegah aksi tak sengaja); `Esc` menutup; fokus dikembalikan ke tombol pemicu |

#### 4.3.4 State Tombol

| State | Tampilan & Perilaku |
|---|---|
| **Idle** | Tombol primer aktif |
| **Submitting** | Disabled, spinner, label "Mengirim…"; `aria-busy="true"` |
| **Accepted (202)** | Toast info (`aria-live="polite"`): "Job diterima. Invoice sedang diproses di latar belakang." + `runId` (mono, tombol salin) + tautan "Lihat aktivitas". Tabel & kartu di-*refresh* otomatis setelah 3 dtk dan 10 dtk (polling singkat) karena pemrosesan bersifat asinkron |
| **Cooldown (429)** | Disabled dengan hitung mundur dari header `Retry-After` ("Coba lagi dalam 42 dtk") |
| **Error 403** | Toast merah "Anda tidak memiliki izin menjalankan job penagihan." (tombol disembunyikan untuk `ReadOnlyAuditor`) |
| **Error 400** | Pesan validasi (mis. `asOfDate` tidak diizinkan) ditampilkan di dalam dialog |
| **Error 503** | Toast merah "Antrean tidak tersedia saat ini. Coba lagi beberapa saat." + tombol *Coba lagi* |

#### 4.3.5 Kontrak yang Dipakai

```http
POST /api/v1/billing/trigger
{ "asOfDate": "2026-11-01" }          // opsional

202 → { "runId": "…", "status": "Accepted", "requestedAt": "…" }
429 → Retry-After: 42
```

**Catatan UX penting:** respons `202` berarti *diterima*, **bukan** *selesai*. Antarmuka **tidak boleh** menampilkan "Invoice berhasil dibuat" — hanya "Job diterima".

#### 4.3.6 Panel "Aktivitas Terakhir" (Pendukung)

Daftar ringkas 5 entri terbaru dari `GET /api/v1/audit-logs?entityName=BillingRun` (aksi `BillingRunRequested`, `BillingRunScanned`) menampilkan: waktu relatif, aktor, jumlah invoice yang di-*enqueue*. Membantu admin memverifikasi bahwa job berjalan.

---

### 4.4 Komponen Pendukung

| Komponen | Spesifikasi singkat |
|---|---|
| **Tombol** | Primer (`bg-brand-600`), Sekunder (`bg-white ring-1 ring-slate-300 text-slate-700`), Destruktif (`bg-red-600`), Ghost. Tinggi 40 px; ikon + teks; fokus ring `brand-500`. |
| **Input** | `rounded-lg border-slate-300 focus:border-brand-500 focus:ring-brand-500`; label di atas; pesan error `text-xs text-red-600` + ikon; `aria-describedby`. |
| **Toast** | Kanan-atas (desktop) / atas-penuh (mobile); otomatis hilang 6 dtk (error: tetap sampai ditutup); `role="status"` / `role="alert"` untuk error. |
| **Modal/Dialog** | `role="dialog"`, `aria-modal`, *focus trap*, backdrop `bg-slate-900/50`. |
| **Drawer** | Panel kanan untuk detail invoice; lebar `w-full sm:max-w-md`. |
| **Skeleton** | `animate-pulse rounded bg-slate-200`; menjaga dimensi final agar tidak terjadi *layout shift*. |
| **Empty state** | Ikon, judul, penjelasan satu kalimat, satu aksi utama. |
| **Breadcrumb/Judul halaman** | H1 + deskripsi singkat + slot aksi kanan (mis. Run Billing Job). |

---

## 5. Layout & Responsiveness Structure

### 5.1 App Shell

```
┌────────────────────────────────────────────────────────────────────┐
│ Topbar (h-16): [☰ mobile] Judul/Breadcrumb      [Run Billing Job] [👤] │
├───────────────┬────────────────────────────────────────────────────┤
│ Sidebar       │ Main (p-4 sm:p-6 lg:p-8, max-w-7xl, mx-auto)        │
│ (w-64, gelap) │                                                    │
│               │  H1 + deskripsi                       [aksi halaman]│
│ • Dashboard   │  ┌──────────┐ ┌──────────┐ ┌──────────┐            │
│ • Invoices    │  │ Revenue  │ │ Pending  │ │ Active   │ Summary    │
│ • Subscript.  │  └──────────┘ └──────────┘ └──────────┘ Cards      │
│ • Plans       │  ┌──────────────────────────────────────────────┐ │
│ • Customers   │  │ Toolbar: [Search…] [Semua|Pending|Paid|…]     │ │
│ • Audit Logs  │  ├──────────────────────────────────────────────┤ │
│               │  │ Invoice Data Table                            │ │
│ [Env: DEV]    │  ├──────────────────────────────────────────────┤ │
│               │  │ Menampilkan 1–10 dari 248   « 1 2 3 … »       │ │
│               │  └──────────────────────────────────────────────┘ │
└───────────────┴────────────────────────────────────────────────────┘
```

### 5.2 Breakpoint & Perilaku

| Breakpoint | Lebar | Sidebar | Summary Cards | Tabel Invoice |
|---|---|---|---|---|
| **base (mobile)** | < 640 px | Tersembunyi → **drawer** via tombol ☰ | 1 kolom | **Daftar kartu** (lihat 5.3) |
| **sm** | ≥ 640 px | Drawer | 2 kolom | Daftar kartu |
| **md (tablet)** | ≥ 768 px | **Ikon saja** (`w-16`), tooltip label | 2 kolom (kartu ke-3 melebar penuh) | Tabel dengan scroll horizontal; kolom Plan/Periode disembunyikan |
| **lg (laptop)** | ≥ 1024 px | **Penuh** (`w-64`) | **3 kolom** | Tabel penuh semua kolom |
| **xl** | ≥ 1280 px | Penuh | 3 kolom | Tabel penuh; konten `max-w-7xl` terpusat |

Kelas utama:

```html
<div class="min-h-screen bg-slate-50">
  <aside class="fixed inset-y-0 left-0 z-30 hidden w-16 bg-slate-900 md:block lg:w-64"><!-- nav --></aside>
  <div class="md:pl-16 lg:pl-64">
    <header class="sticky top-0 z-20 flex h-16 items-center justify-between border-b border-slate-200 bg-white/90 px-4 backdrop-blur sm:px-6"><!-- topbar --></header>
    <main class="mx-auto max-w-7xl p-4 sm:p-6 lg:p-8">
      <section class="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3 lg:gap-6"><!-- summary cards --></section>
      <section class="mt-6 lg:mt-8"><!-- toolbar + table --></section>
    </main>
  </div>
</div>
```

### 5.3 Tabel di Ponsel: Daftar Kartu

Di bawah `md`, baris tabel menjadi kartu bertumpuk agar tetap terbaca tanpa scroll horizontal:

```
┌───────────────────────────────────────┐
│ INV-202610-000123            [Overdue]│
│ Budi Hartono                          │
│ Pro Monthly · 01–31 Okt 2026          │
│ Jatuh tempo 15 Okt 2026   Rp 1.500.000│
│                              [  ⋯  ]  │
└───────────────────────────────────────┘
```

- Filter status menjadi **chip yang dapat di-scroll horizontal** (`overflow-x-auto`); search tetap di atas.
- Pagination disederhanakan: **Sebelumnya / Berikutnya** + "Hal. 2 dari 25".
- Tombol **Run Billing Job** tetap di topbar (ikon + label pada ≥ `sm`, hanya ikon pada mobile dengan `aria-label`).
- Target sentuh minimum **44 × 44 px**.

### 5.4 Struktur Halaman

| Halaman | Isi |
|---|---|
| **Dashboard** | Summary Cards → Panel "Aktivitas Terakhir" → Invoice Data Table (ringkas, 10 baris terbaru/mendesak) |
| **Invoices** | Toolbar + Invoice Data Table penuh + Drawer detail |
| **Subscriptions** | Tabel (Customer, Plan, Status, NextBillingDate) + aksi Pause/Resume/Cancel + form buat langganan |
| **Plans** | Tabel/kartu plan + form CRUD |
| **Customers** | Tabel + form CRUD |
| **Audit Logs** | Tabel filter (entitas, aksi, rentang waktu) read-only |

### 5.5 Indikator Lingkungan

Chip kecil di sidebar bawah: `DEV` (kuning) / `STAGING` / `PROD` (tanpa chip). Bila `simulationEnabled = true`, tampil label **"Simulation mode"** agar admin tidak mengira data demo adalah data produksi.

---

## 6. Aksesibilitas, Lokalisasi & Interaksi

### 6.1 Aksesibilitas (target WCAG 2.1 AA)

| Area | Ketentuan |
|---|---|
| Kontras | Teks ≥ 4,5:1, komponen UI ≥ 3:1. Kombinasi badge pada §2.2 dipilih memenuhi target ini; verifikasi dengan alat kontras saat implementasi. |
| Warna bukan satu-satunya pembawa makna | Status selalu memiliki **ikon + label teks**. |
| Keyboard | Semua kontrol dapat dijangkau dengan `Tab`; urutan fokus logis; `Esc` menutup modal/drawer; fokus terlihat (`focus-visible:ring-2 ring-brand-500`). |
| Semantik | `<table>` sungguhan dengan `<th scope>`; `aria-sort` pada header; landmark `<nav>`, `<main>`, `<header>`. |
| Pembaca layar | Toast memakai `aria-live`; tombol ikon memiliki `aria-label`; spinner memiliki teks tersembunyi `sr-only`. |
| Gerak | Hormati `prefers-reduced-motion` (nonaktifkan animasi skeleton/transisi). |
| Target sentuh | ≥ 44 px pada layar sentuh. |

### 6.2 Lokalisasi (id-ID)

```ts
const idr = new Intl.NumberFormat('id-ID', { style: 'currency', currency: 'IDR', maximumFractionDigits: 0 });
idr.format(1500000);   // "Rp 1.500.000"

const date = new Intl.DateTimeFormat('id-ID', { day: '2-digit', month: 'short', year: 'numeric', timeZone: 'Asia/Jakarta' });
date.format(new Date('2026-10-15'));  // "15 Okt 2026"
```

- Uang dan tanggal bisnis ditampilkan dalam zona **Asia/Jakarta**; timestamp audit ditampilkan lokal dengan tooltip UTC.
- Format mata uang mengikuti `currency` dari API (bukan hard-code IDR).
- Teks antarmuka berbahasa Indonesia; istilah teknis/keuangan baku (*Invoice, Pending, Paid, Overdue, Failed*) dipertahankan agar konsisten dengan API dan dokumentasi.

### 6.3 Umpan Balik & Penanganan Error

| Situasi | Pola |
|---|---|
| Validasi form | Pesan inline di bawah field + ringkasan error di atas form bila > 2 error |
| `409` transisi tidak valid | Toast error dengan `detail` dari `ProblemDetails`; data baris dimuat ulang |
| `401` | Alihkan ke login, simpan lokasi tujuan |
| `403` | Halaman/aksi disembunyikan sesuai role; bila tetap terpanggil, toast izin |
| Jaringan putus | Banner "Koneksi bermasalah" + coba ulang otomatis dengan *backoff* |

### 6.4 Dark Mode

Di luar cakupan v1.0. Token warna disusun semantik (`bg-app`, `bg-surface`, `text-primary`) agar dukungan `dark:` mudah ditambahkan pada rilis berikutnya.

---

## 7. Pemetaan Komponen ke API

| Komponen UI | Endpoint | Parameter / Catatan |
|---|---|---|
| Summary Cards | `GET /api/v1/dashboard/summary` | Mengembalikan `totalRevenue`, `pendingInvoices`, `activeSubscriptions`, `meta.simulationEnabled` |
| Invoice Data Table | `GET /api/v1/invoices` | `status` (koma), `search`, `sortBy`, `sortDir`, `page`, `pageSize` → `{ items, page, pageSize, totalCount }` |
| Detail invoice | `GET /api/v1/invoices/{id}` + `GET /api/v1/audit-logs?entityName=Invoice&entityId={id}` | Riwayat pada drawer |
| Aksi ubah status | `PATCH /api/v1/invoices/{id}/status` | Body: `{ "status": "Paid\|Failed\|Pending", "reason": "…" }`; `409` bila transisi tidak valid |
| Manual Trigger Button | `POST /api/v1/billing/trigger` | Body opsional `{ "asOfDate": "yyyy-MM-dd" }`; `202`, `403`, `429` (+`Retry-After`), `503` |
| Aktivitas Terakhir | `GET /api/v1/audit-logs?entityName=BillingRun` | Aksi `BillingRunRequested`, `BillingRunScanned` |
| Navigasi Subscriptions/Plans/Customers | `GET/POST/PUT/DELETE /api/v1/{subscriptions\|plans\|customers}` | Lihat [Architecture.md §4.1.2](./Architecture.md#412-daftar-endpoint-apiv1) |

**Contoh body ubah status:**

```json
{ "status": "Failed", "reason": "Kartu kredit ditolak oleh bank penerbit" }
```

**Contoh respons list invoice (potongan):**

```json
{
  "items": [
    {
      "id": "3f2b8c1a-…",
      "invoiceNumber": "INV-202610-000123",
      "customer": { "id": "…", "fullName": "Budi Hartono", "email": "budi@contoh.id" },
      "plan": { "name": "Pro Monthly", "billingCycle": "Monthly" },
      "billingPeriodStart": "2026-10-01",
      "billingPeriodEnd": "2026-10-31",
      "amount": 1500000.00,
      "currency": "IDR",
      "status": "Overdue",
      "issueDate": "2026-10-01",
      "dueDate": "2026-10-15"
    }
  ],
  "page": 1,
  "pageSize": 10,
  "totalCount": 248
}
```
