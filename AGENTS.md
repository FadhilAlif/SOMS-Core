# AGENTS.md — Sales Order Management System (SOMS-Flow)
---

## 🎯 Konteks Proyek

**Sales Order Management System (SOMS-Flow)** adalah technical test untuk posisi **.NET Developer**.
Sistem dibangun menggunakan arsitektur **microservices** dengan stack:
- **Backend**: C# / .NET Core 8+
- **Database**: SQL Server (1 shared DB, 3 tabel — TIDAK BOLEH diubah strukturnya)
- **Frontend**: ASP.NET MVC / Razor Pages (atau SPA)
- **Komunikasi**: HTTP REST API antar service

Deadline: **1 hari kerja**. Prioritaskan fitur wajib sebelum fitur nilai plus.

---

## 📁 Struktur Monorepo

```
SOMS-Flow/
├── CustomerService/          ← .NET Web API (port: 5001)
├── SalesOrderService/        ← .NET Web API (port: 5002)
├── FrontEnd/                 ← ASP.NET Razor Pages (port: 5000)
├── Database/
│   ├── schema.sql            ← WAJIB: CREATE TABLE + 3 seed customers
│   └── *.sql                 ← 1 file per SP/View/Function
├── docs/
│   ├── PRD.md
│   ├── ARCHITECTURE.md
│   └── PROGRESS.md
├── AGENTS.md                 ← file ini
├── README.md
└── CATATAN-DESAIN.md
```

---

## 📐 Aturan Arsitektur (TIDAK BOLEH Dilanggar)

### Database
- ❌ Dilarang menambah, mengubah, atau menghapus kolom/tabel yang sudah ada
- ✅ Boleh menambahkan Stored Procedure, View, atau Function
- ✅ Setiap SP/View/Function WAJIB disimpan sebagai file `.sql` terpisah di `/Database/`
- ✅ Operasi DELETE order WAJIB menggunakan transaksi database (atomik)

### Frontend / Service Boundary
- ❌ Dilarang menghitung `TOTAL` atau `Grand Total` di JavaScript/frontend
- ❌ Dilarang akses database langsung dari frontend
- ✅ Semua kalkulasi dan validasi bisnis dilakukan di service layer
- ✅ Frontend hanya menampilkan data dan meneruskan input ke service

### Konfigurasi
- ❌ Dilarang hardcode connection string, API key, atau secret
- ✅ Gunakan `appsettings.json` atau environment variable

---

## 🗄️ Skema Database (Referensi)

```sql
-- Tabel Master Pelanggan
CREATE TABLE COM_CUSTOMER (
    COM_CUSTOMER_ID  INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    CUSTOMER_NAME    VARCHAR(100) NOT NULL
);

-- Tabel Sales Order Header
CREATE TABLE SALES_SO (
    SALES_SO_ID     INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    SO_NO           VARCHAR(20) NOT NULL,
    ORDER_DATE      DATETIME NOT NULL,
    COM_CUSTOMER_ID INT NOT NULL,
    ADDRESS         VARCHAR(500) NULL,
    CONSTRAINT FK_SO_CUSTOMER FOREIGN KEY (COM_CUSTOMER_ID)
        REFERENCES COM_CUSTOMER(COM_CUSTOMER_ID)
);

-- Tabel Sales Order Detail Item
CREATE TABLE SALES_SO_LITEM (
    SALES_SO_LITEM_ID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    SALES_SO_ID       INT NOT NULL,
    ITEM_NAME         VARCHAR(100) NOT NULL,
    QUANTITY          INT NOT NULL,
    PRICE             FLOAT NOT NULL,
    CONSTRAINT FK_LITEM_SO FOREIGN KEY (SALES_SO_ID)
        REFERENCES SALES_SO(SALES_SO_ID)
);
```

**Relasi**: `COM_CUSTOMER` ← `SALES_SO` ← `SALES_SO_LITEM`
**Kalkulasi**: `TOTAL per item = QUANTITY × PRICE` | `Grand Total = SUM(semua TOTAL)`

---

## 🔌 Kontrak API (Minimum yang Harus Dipenuhi)

### CustomerService — `http://localhost:5001`

| Method | Endpoint | Deskripsi |
|--------|----------|-----------|
| GET | `/api/customers` | Ambil semua pelanggan |

**Response 200**:
```json
[{ "customerId": 1, "customerName": "PT Maju Bersama" }]
```

---

### SalesOrderService — `http://localhost:5002`

| Method | Endpoint | Deskripsi |
|--------|----------|-----------|
| GET | `/api/orders` | List order (filter: `keyword`, `orderDate`) |
| GET | `/api/orders/{id}` | Detail order + items |
| POST | `/api/orders` | Buat order baru |
| PUT | `/api/orders/{id}` | Update order (replace semua items) |
| DELETE | `/api/orders/{id}` | Hapus order + items (atomik) |
| GET | `/api/orders/export` | Export ke Excel .xlsx |

**Format error konsisten**:
```json
{
  "success": false,
  "message": "Penjelasan singkat",
  "errors": ["detail error 1", "detail error 2"]
}
```

---

## ✅ Business Rules (Wajib Diimplementasi di Service Layer)

| # | Rule |
|---|------|
| BR-01 | SO Number tidak boleh duplikat |
| BR-02 | Order Date tidak boleh di masa depan (> hari ini) |
| BR-03 | Minimal 1 line item per order |
| BR-04 | Quantity harus integer > 0 |
| BR-05 | Price harus > 0 |
| BR-06 | Customer ID harus valid (exist di DB) |
| BR-07 | `Total` per item = `Quantity × Price` (dihitung server) |
| BR-08 | `Grand Total` = SUM semua Total (dihitung server) |
| BR-09 | Delete order: hapus LITEM dulu, lalu SO — dalam 1 transaksi |
| BR-10 | Export Excel: hanya data yang sedang difilter, bukan semua data |
| BR-11 | Popup konfirmasi hapus WAJIB menampilkan nomor SO secara dinamis |

---

## 🏗️ Pola Internal Tiap Service

```
Controller → Service (Interface) → Repository (Interface) → Database
```

Setiap layer:
- **Controller**: Terima HTTP request, validasi DTO, panggil Service, return response
- **Service**: Business logic, kalkulasi, validasi, orkestrasi
- **Repository**: Query SQL, tidak ada logic bisnis
- **Model/DTO**: Objek data, pisahkan Request dan Response DTO

---

## 📦 NuGet Packages yang Digunakan

### CustomerService
| Package | Kegunaan |
|---------|----------|
| `Microsoft.Data.SqlClient` | Koneksi SQL Server |
| `Dapper` | Micro-ORM untuk query SQL |

### SalesOrderService
| Package | Kegunaan |
|---------|----------|
| `Microsoft.Data.SqlClient` | Koneksi SQL Server |
| `Dapper` | Micro-ORM |
| `ClosedXML` | Generate file Excel `.xlsx` |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | JWT auth (nilai plus) |

---

## 📋 Deliverables Checklist

### Wajib
- [ ] `/CustomerService/` — project Web API berfungsi di port 5001
- [ ] `/SalesOrderService/` — project Web API berfungsi di port 5002
- [ ] `/FrontEnd/` — web UI berfungsi di port 5000
- [ ] `/Database/schema.sql` — siap dijalankan dari nol, include 3 sample customers
- [ ] `README.md` — panduan setup lengkap
- [ ] `CATATAN-DESAIN.md` — maks 1 halaman, kata-kata sendiri

### Nilai Plus
- [ ] Minimal 1 endpoint diamankan dengan JWT atau API Key
- [ ] 1–2 unit test untuk logika bisnis (kalkulasi total, validasi input)
- [ ] Stored Procedure untuk operasi DB utama

---

## 🔑 Konvensi Penamaan

| Elemen | Konvensi | Contoh |
|--------|----------|--------|
| Class | PascalCase | `SalesOrderService` |
| Method | PascalCase | `GetOrderById` |
| Variable / Property | camelCase | `grandTotal` |
| Interface | `I` prefix + PascalCase | `IOrderRepository` |
| DTO Request | `...Request` suffix | `CreateOrderRequest` |
| DTO Response | model name | `OrderDetailResponse` |
| File SQL | snake_case | `sp_get_orders.sql` |

---

## 🚦 Urutan Implementasi (Rekomendasi)

```
1. Database/schema.sql
2. CustomerService (endpoint tunggal GET /api/customers)
3. SalesOrderService — Repository & Service layer
4. SalesOrderService — Controllers (semua endpoint)
5. SalesOrderService — Export Excel
6. FrontEnd — Order List page
7. FrontEnd — Create Order page
8. FrontEnd — Edit Order page
9. Security (API Key / JWT)  ← nilai plus
10. Unit Test                ← nilai plus
11. README.md + CATATAN-DESAIN.md
```

---

## 📚 Dokumen Referensi

- [`docs/PRD.md`](docs/PRD.md) — Product Requirements Document lengkap
- [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) — Diagram arsitektur & keputusan teknis
- [`docs/PROGRESS.md`](docs/PROGRESS.md) — Tracking progres pengerjaan
- [`docs/Project-Requirement.pdf`](docs/Project-Requirement.pdf) — Dokumen asli technical test
