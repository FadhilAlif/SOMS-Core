# PRD — Product Requirements Document
## Sales Order Management System (SOMS)

**Versi**: 1.0  
**Tanggal**: 2026-09-25  
**Status**: Active  
**Penulis**: Berdasarkan Functional Specification Document v1.1

---

## 1. Latar Belakang

Sales Order Management System (SOMS) dibangun sebagai bagian dari technical test posisi **.NET Developer**. Sistem ini mengelola proses penjualan mulai dari data master pelanggan hingga pembuatan, pengeditan, dan penghapusan Sales Order beserta detail item-nya.

Sistem dirancang dengan arsitektur **microservices** untuk memisahkan tanggung jawab antar domain bisnis dan memungkinkan pengembangan serta deployment secara independen.

---

## 2. Tujuan Produk

| Tujuan | Deskripsi |
|--------|-----------|
| **Manajemen Order** | Memungkinkan pengguna membuat, melihat, mengedit, dan menghapus Sales Order |
| **Manajemen Pelanggan** | Menyediakan data master pelanggan untuk referensi order |
| **Pencarian & Filter** | Memudahkan pencarian order berdasarkan keyword atau tanggal |
| **Ekspor Data** | Memungkinkan ekspor data order ke format Excel untuk pelaporan |
| **Integritas Data** | Menjamin konsistensi data melalui validasi dan transaksi atomik |

---

## 3. Pengguna Target

- **Staf Sales / Admin**: Pengguna utama yang membuat dan mengelola Sales Order
- **Manager**: Pengguna yang membutuhkan laporan dan ekspor data order

---

## 4. Ruang Lingkup

### 4.1 Dalam Ruang Lingkup (In Scope)
- Manajemen data master pelanggan (read-only dari UI, seed via SQL)
- CRUD Sales Order: Create, Read, Update, Delete
- Detail line item per order (item name, quantity, price, total)
- Filter dan pencarian order (keyword + tanggal)
- Ekspor data order ke file Excel (.xlsx)
- Antarmuka web responsif

### 4.2 Di Luar Ruang Lingkup (Out of Scope)
- Autentikasi multi-user / sistem login penuh
- Manajemen stok/inventori
- Integrasi pembayaran
- Notifikasi email / push notification
- Mobile application
- Multi-tenancy

---

## 5. Spesifikasi Fungsional

### 5.1 Halaman Order List

**Tujuan**: Menampilkan daftar semua Sales Order dengan kemampuan filter, aksi CRUD, dan ekspor.

**Komponen UI**:
| Komponen | Tipe | Keterangan |
|----------|------|------------|
| Grid / Tabel | Data table | Tampilkan semua order |
| Input Keyword | Text field | Cari berdasarkan SO Number atau Customer Name |
| Input Tanggal | Date picker | Filter berdasarkan Order Date |
| Tombol Cari | Button | Eksekusi filter |
| Tombol Reset | Button | Hapus semua filter |
| Tombol + Tambah Order | Button | Navigasi ke halaman Create |
| Tombol Export Excel | Button | Ekspor data yang sedang difilter |
| Tombol Edit (per baris) | Button | Navigasi ke halaman Edit |
| Tombol Delete (per baris) | Button | Tampilkan popup konfirmasi hapus |

**Kolom Grid**:
| Kolom | Sumber Data | Tipe |
|-------|-------------|------|
| SO Number | `SO_NO` | string |
| Order Date | `ORDER_DATE` | date |
| Customer Name | `CUSTOMER_NAME` (JOIN) | string |
| Address | `ADDRESS` | string |
| Grand Total | SUM(QUANTITY × PRICE) | decimal |

**Behavior**:
- Filter keyword mencari di SO Number ATAU Customer Name (LIKE)
- Filter tanggal mencari berdasarkan Order Date exact atau range
- Setelah hapus berhasil, grid direfresh otomatis (tanpa full page reload)
- Popup konfirmasi hapus menampilkan nomor SO secara dinamis

---

### 5.2 Halaman Order Input — Mode Create

**Tujuan**: Form untuk membuat Sales Order baru.

**Form Header**:
| Field | Tipe | Validasi |
|-------|------|----------|
| SO Number | Text input | Wajib, unik (tidak boleh duplikat) |
| Order Date | Date picker | Wajib, tidak boleh masa depan |
| Customer | Dropdown | Wajib, data dari CustomerService |
| Address | Textarea | Opsional |

**Tabel Line Items** (dinamis):
| Kolom | Tipe | Validasi |
|-------|------|----------|
| Item Name | Text input | Wajib |
| Quantity | Number input | Wajib, integer > 0 |
| Price | Number input | Wajib, > 0 |
| Total | Readonly | Dihitung server (Qty × Price) |
| Hapus baris | Button | Minimal 1 baris harus tetap ada |

**Footer Form**:
- Grand Total (readonly, dari response server)
- Tombol Tambah Item
- Tombol Save → POST /api/orders → redirect ke Order List
- Tombol Cancel → kembali ke Order List tanpa simpan

---

### 5.3 Halaman Order Input — Mode Edit

**Tujuan**: Form untuk mengubah Sales Order yang sudah ada.

**Behavior**:
- Pre-populate semua field dari `GET /api/orders/{id}`
- Fitur identik dengan mode Create
- Submit → PUT /api/orders/{id} (service replace semua items lama)
- Setelah update berhasil → redirect ke Order List
- Tombol Update menggantikan tombol Save

---

### 5.4 Aturan Bisnis Utama

| Kode | Rule | Implementasi |
|------|------|-------------|
| BR-01 | SO Number tidak boleh duplikat | Validasi di SalesOrderService sebelum INSERT |
| BR-02 | Order Date tidak boleh di masa depan | Validasi di SalesOrderService |
| BR-03 | Minimal 1 line item | Validasi di SalesOrderService |
| BR-04 | Quantity > 0 (integer) | Validasi di SalesOrderService |
| BR-05 | Price > 0 | Validasi di SalesOrderService |
| BR-06 | Customer ID harus valid | Cek exist di COM_CUSTOMER |
| BR-07 | Total item = Qty × Price | Kalkulasi di server, bukan JS |
| BR-08 | Grand Total = SUM Total | Kalkulasi di server, bukan JS |
| BR-09 | Delete harus atomik | Gunakan SQL Transaction |
| BR-10 | Export hanya data terfilter | Gunakan filter yang aktif saat export |
| BR-11 | Konfirmasi hapus dinamis | Modal menampilkan SO Number aktual |

---

## 6. Spesifikasi Non-Fungsional

| Aspek | Ketentuan |
|-------|-----------|
| **Platform** | .NET Core / .NET 8+ |
| **Database** | SQL Server (shared single instance) |
| **Komunikasi** | HTTP REST API (JSON) |
| **Konfigurasi** | appsettings.json atau environment variable |
| **Keamanan** | Minimal 1 endpoint dengan JWT atau API Key (nilai plus) |
| **Kode** | Clean, konsisten, deskriptif — siap dijelaskan baris per baris |
| **Testing** | Minimal 1-2 unit test untuk business logic (nilai plus) |

---

## 7. Ketentuan Penyerahan

| Item | Status |
|------|--------|
| GitHub repository (public) | Wajib |
| Source code lengkap (mono-repo) | Wajib |
| Database/schema.sql | Wajib |
| README.md | Wajib |
| CATATAN-DESAIN.md | Wajib |
| File .sql per SP/View/Function | Wajib jika ada penambahan |
| JWT atau API Key (1 endpoint) | Nilai Plus |
| Unit Test (1-2 test) | Nilai Plus |

---

## 8. Kriteria Penerimaan (Acceptance Criteria)

### Minimum Viable (LULUS)
- [ ] CustomerService GET /api/customers mengembalikan data pelanggan
- [ ] SalesOrderService semua 6 endpoint berfungsi dengan benar
- [ ] Frontend menampilkan Order List dengan filter
- [ ] Frontend Create Order dapat menyimpan order baru
- [ ] Frontend Edit Order dapat memperbarui order yang ada
- [ ] Frontend Delete Order dengan konfirmasi dan refresh grid
- [ ] Export Excel berfungsi dengan filter aktif
- [ ] Database schema.sql dapat dijalankan dari nol
- [ ] README.md menjelaskan cara setup dan run

### Nilai Plus (UNGGUL)
- [ ] Minimal 1 endpoint ber-autentikasi (JWT/API Key)
- [ ] Unit test untuk kalkulasi total dan validasi input
- [ ] Stored Procedure untuk query utama
- [ ] CATATAN-DESAIN.md ditulis dengan kata-kata sendiri dan jelas
