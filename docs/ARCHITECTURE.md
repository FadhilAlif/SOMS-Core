# ARCHITECTURE.md — Arsitektur Sistem
## Sales Order Management System (SOMS-Flow)

**Versi**: 1.0  
**Tanggal**: 2026-09-25

---

## 1. Gambaran Umum Arsitektur

SOMS-Flow menggunakan arsitektur **Microservices** dengan pola **API-first** dan **shared database**.
Sistem terdiri dari tiga komponen utama yang berjalan sebagai proses terpisah:

```
┌─────────────────────────────────────────────────────────────┐
│                        BROWSER / CLIENT                      │
└───────────────────────────┬─────────────────────────────────┘
                            │ HTTP
                            ▼
┌─────────────────────────────────────────────────────────────┐
│                   FRONTEND (port: 5000)                      │
│            ASP.NET MVC / Razor Pages / SPA                  │
│    (Hanya tampilan — tidak ada business logic / DB access)  │
└─────────────┬───────────────────────────┬────────────────────┘
              │ HTTP REST                  │ HTTP REST
              ▼                            ▼
┌─────────────────────┐      ┌─────────────────────────────────┐
│  CUSTOMER SERVICE   │      │      SALES ORDER SERVICE         │
│    (port: 5001)     │      │         (port: 5002)             │
│                     │      │                                  │
│  GET /api/customers │      │  GET    /api/orders              │
│                     │      │  GET    /api/orders/{id}         │
│  Controller         │      │  POST   /api/orders              │
│  Service            │      │  PUT    /api/orders/{id}         │
│  Repository         │      │  DELETE /api/orders/{id}         │
│                     │      │  GET    /api/orders/export       │
└────────┬────────────┘      └──────────────┬──────────────────┘
         │ SQL                               │ SQL
         ▼                                   ▼
┌─────────────────────────────────────────────────────────────┐
│                   SQL SERVER (SOMS-Flow_DB)                       │
│                                                              │
│  COM_CUSTOMER   SALES_SO   SALES_SO_LITEM                   │
│  (pelanggan)    (header)   (line items)                     │
└─────────────────────────────────────────────────────────────┘
```

---

## 2. Komponen & Tanggung Jawab

### 2.1 CustomerService
| Aspek | Detail |
|-------|--------|
| **Port** | 5001 (HTTP) / 7001 (HTTPS) |
| **Framework** | .NET 8 Web API |
| **Domain** | Master data pelanggan |
| **DB Tables** | `COM_CUSTOMER` |
| **Endpoint** | `GET /api/customers` |
| **Konsumen** | FrontEnd (populate dropdown) |

### 2.2 SalesOrderService
| Aspek | Detail |
|-------|--------|
| **Port** | 5002 (HTTP) / 7002 (HTTPS) |
| **Framework** | .NET 8 Web API |
| **Domain** | Sales Order (header + line items) |
| **DB Tables** | `SALES_SO`, `SALES_SO_LITEM`, JOIN ke `COM_CUSTOMER` |
| **Endpoints** | 6 endpoint (CRUD + export) |
| **Konsumen** | FrontEnd |

### 2.3 FrontEnd
| Aspek | Detail |
|-------|--------|
| **Port** | 5000 (HTTP) / 7000 (HTTPS) |
| **Framework** | ASP.NET MVC / Razor Pages |
| **Peran** | Cangkang tampilan saja |
| **Batasan** | Tidak boleh akses DB, tidak boleh kalkulasi bisnis |
| **Komunikasi** | HttpClient ke CustomerService dan SalesOrderService |

---

## 3. Skema Database

### 3.1 Entity Relationship Diagram

```
COM_CUSTOMER                SALES_SO                   SALES_SO_LITEM
─────────────────          ──────────────────────     ─────────────────────────
COM_CUSTOMER_ID (PK) ◄──┐  SALES_SO_ID (PK)      ◄──┐ SALES_SO_LITEM_ID (PK)
CUSTOMER_NAME           └─ COM_CUSTOMER_ID (FK)    └─ SALES_SO_ID (FK)
                            SO_NO                       ITEM_NAME
                            ORDER_DATE                  QUANTITY
                            ADDRESS                     PRICE
                                                        [TOTAL = QTY × PRICE]*

* TOTAL dihitung secara runtime, tidak disimpan sebagai kolom
```

### 3.2 Tabel Detail

**COM_CUSTOMER**
| Kolom | Tipe | Constraint |
|-------|------|-----------|
| COM_CUSTOMER_ID | INT IDENTITY | PK, NOT NULL |
| CUSTOMER_NAME | VARCHAR(100) | NOT NULL |

**SALES_SO**
| Kolom | Tipe | Constraint |
|-------|------|-----------|
| SALES_SO_ID | INT IDENTITY | PK, NOT NULL |
| SO_NO | VARCHAR(20) | NOT NULL |
| ORDER_DATE | DATETIME | NOT NULL |
| COM_CUSTOMER_ID | INT | FK → COM_CUSTOMER, NOT NULL |
| ADDRESS | VARCHAR(500) | NULL |

**SALES_SO_LITEM**
| Kolom | Tipe | Constraint |
|-------|------|-----------|
| SALES_SO_LITEM_ID | INT IDENTITY | PK, NOT NULL |
| SALES_SO_ID | INT | FK → SALES_SO, NOT NULL |
| ITEM_NAME | VARCHAR(100) | NOT NULL |
| QUANTITY | INT | NOT NULL |
| PRICE | FLOAT | NOT NULL |

---

## 4. Struktur Internal Service (Layered Architecture)

```
HTTP Request
     │
     ▼
┌────────────┐
│ Controller │  ← Terima request, validasi DTO, return response
└─────┬──────┘
      │
      ▼
┌────────────┐
│  Service   │  ← Business logic, kalkulasi, validasi, orkestrasi
└─────┬──────┘
      │
      ▼
┌────────────┐
│ Repository │  ← Query SQL ke database, tidak ada business logic
└─────┬──────┘
      │
      ▼
┌────────────┐
│ SQL Server │
└────────────┘
```

### 4.1 Folder Structure — CustomerService
```
CustomerService/
├── Controllers/
│   └── CustomersController.cs
├── Services/
│   ├── ICustomerService.cs
│   └── CustomerService.cs
├── Repositories/
│   ├── ICustomerRepository.cs
│   └── CustomerRepository.cs
├── Models/
│   ├── Customer.cs
│   └── ApiResponse.cs
├── appsettings.json
├── appsettings.Development.json
└── Program.cs
```

### 4.2 Folder Structure — SalesOrderService
```
SalesOrderService/
├── Controllers/
│   └── OrdersController.cs
├── Services/
│   ├── IOrderService.cs
│   └── OrderService.cs
├── Repositories/
│   ├── IOrderRepository.cs
│   └── OrderRepository.cs
├── Models/
│   ├── SalesOrder.cs
│   ├── OrderLineItem.cs
│   ├── CreateOrderRequest.cs
│   ├── UpdateOrderRequest.cs
│   └── ApiResponse.cs
├── appsettings.json
├── appsettings.Development.json
└── Program.cs
```

### 4.3 Folder Structure — FrontEnd
```
FrontEnd/
├── Controllers/
│   ├── HomeController.cs
│   └── OrdersController.cs
├── Views/
│   ├── Shared/
│   │   └── _Layout.cshtml
│   └── Orders/
│       ├── Index.cshtml      ← Order List
│       ├── Create.cshtml     ← Create Order
│       └── Edit.cshtml       ← Edit Order
├── Services/
│   ├── ICustomerApiService.cs
│   ├── CustomerApiService.cs
│   ├── IOrderApiService.cs
│   └── OrderApiService.cs
├── Models/
│   └── (ViewModels)
├── wwwroot/
│   ├── css/
│   └── js/
└── Program.cs
```

---

## 5. Alur Komunikasi Antar Service

### 5.1 Alur Create Order

```
Browser
  │
  ├─ GET /Orders/Create → FrontEnd
  │     │
  │     └─ GET http://localhost:5001/api/customers → CustomerService
  │           └─ Query COM_CUSTOMER → SQL Server
  │           └─ Return [{customerId, customerName}]
  │     └─ Render form dengan dropdown customer
  │
  ├─ POST /Orders/Create (form submit) → FrontEnd
  │     │
  │     └─ POST http://localhost:5002/api/orders → SalesOrderService
  │           ├─ Validasi input (BR-01 s/d BR-06)
  │           ├─ Hitung Total tiap item (BR-07)
  │           ├─ Hitung Grand Total (BR-08)
  │           ├─ INSERT SALES_SO
  │           ├─ INSERT SALES_SO_LITEM (per item)
  │           └─ Return 201 { success, salesSoId }
  │     └─ Redirect ke /Orders (Order List)
```

### 5.2 Alur Delete Order

```
Browser
  │
  ├─ Klik Delete → Tampilkan modal konfirmasi dengan SO Number
  │
  ├─ Konfirmasi → DELETE /Orders/{id} → FrontEnd
  │     │
  │     └─ DELETE http://localhost:5002/api/orders/{id} → SalesOrderService
  │           ├─ BEGIN TRANSACTION
  │           ├─ DELETE FROM SALES_SO_LITEM WHERE SALES_SO_ID = {id}
  │           ├─ DELETE FROM SALES_SO WHERE SALES_SO_ID = {id}
  │           └─ COMMIT TRANSACTION
  │     └─ Refresh Order List grid
```

### 5.3 Alur Export Excel

```
Browser
  │
  ├─ Klik Export Excel (dengan filter aktif) → FrontEnd
  │     │
  │     └─ GET http://localhost:5002/api/orders/export?keyword=X&orderDate=Y
  │           ├─ Query data dengan filter (sama dengan GET /orders)
  │           ├─ Generate file .xlsx menggunakan ClosedXML
  │           └─ Return file sebagai attachment
  │     └─ Browser download file SalesOrder_YYYY-MM-DD.xlsx
```

---

## 6. Keputusan Teknis

### 6.1 Mengapa Shared Database?

Pertimbangan: Sesuai requirement teknis (ketentuan 7.1), satu database SQL Server bersama diakses oleh masing-masing service sesuai domain-nya. Ini merupakan **pola pragmatis** untuk skala technical test — di produksi nyata, tiap microservice idealnya memiliki database sendiri.

### 6.2 Mengapa Dapper, bukan EF Core?

- Kontrol penuh atas query SQL (cocok untuk SP dan query kompleks)
- Performa lebih baik untuk read-heavy operations
- Lebih ringan dan mudah dipahami untuk interview/review kode
- EF Core bisa dipilih sebagai alternatif yang valid

### 6.3 Mengapa ClosedXML untuk Export Excel?

- Library open-source, stabil, dan banyak digunakan
- API yang intuitif untuk membuat file .xlsx
- Tidak memerlukan lisensi Microsoft Office
- Alternatif: `EPPlus` (perlu lisensi untuk commercial use)

### 6.4 Mengapa ASP.NET MVC untuk Frontend?

- Satu ekosistem .NET — satu bahasa, satu toolchain
- Built-in HttpClient di .NET 8
- Razor templating yang kuat
- Tidak perlu setup CORS yang kompleks seperti SPA terpisah
- Alternatif valid: Vue.js, React (jika lebih familiar)

---

## 7. Konfigurasi & Environment

### 7.1 appsettings.json (tiap service)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=SOMS-Flow_DB;User Id=sa;Password=YourPass123!;TrustServerCertificate=True;"
  },
  "ApiKey": "soms-Flow-secret-api-key-2026",
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

### 7.2 appsettings.json (FrontEnd)
```json
{
  "ServiceUrls": {
    "CustomerService": "http://localhost:5001",
    "SalesOrderService": "http://localhost:5002"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  }
}
```

---

## 8. Port Assignment

| Komponen | HTTP Port | HTTPS Port |
|----------|-----------|------------|
| FrontEnd | 5000 | 7000 |
| CustomerService | 5001 | 7001 |
| SalesOrderService | 5002 | 7002 |

---

## 9. Dependency Tree

```
FrontEnd
├── depends on → CustomerService (HTTP)
└── depends on → SalesOrderService (HTTP)

SalesOrderService
└── depends on → SQL Server (SOMS-Flow_DB)
    ├── SALES_SO
    ├── SALES_SO_LITEM
    └── COM_CUSTOMER (read-only, untuk JOIN)

CustomerService
└── depends on → SQL Server (SOMS-Flow_DB)
    └── COM_CUSTOMER
```
