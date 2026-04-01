# Puantaj & Hakedis Uygulamasi

NVI TD projesi icin altyuklenici personel puantaj takip ve hakedis hesaplama uygulamasi.

PDF devam takip formlarindaki el yazisi giris/cikis saatlerini **Google Gemini AI** ile otomatik okur, puantaj ve hakedis hesaplamalarini yapar, Excel ciktisi uretir.

---

## Ozellikler

- **Personel yonetimi** — Excel'den toplu import veya manuel ekleme
- **PDF otomatik okuma** — Gemini Vision AI ile devam takip formlarini parse etme
- **Puantaj girisi** — PDF'den gelen verileri gorup manuel duzenleme
- **Hakedis hesaplama** — FM/RT ucreti, yemek hakki otomatik; vergi, GSS, kantin manuel
- **Excel cikti** — Puantaj ve Hakedis kapak Excel dosyalari
- **Belge yonetimi** — PDF/resim yukleme ve arsivleme

---

## Gereksinimler

### 1. .NET 9 SDK

Bilgisayarinizda .NET 9 SDK kurulu olmalidir.

**macOS (Homebrew ile):**
```bash
brew install dotnet@9
```

**Windows:**
https://dotnet.microsoft.com/download/dotnet/9.0 adresinden SDK'yi indirip kurun.

**Kontrol:**
```bash
dotnet --version
# 9.0.xxx gibi bir cikti gormelisiniz
```

### 2. Gemini API Key (Ucretsiz)

PDF otomatik okuma icin Google Gemini API anahtari gereklidir.

1. https://aistudio.google.com/apikey adresine gidin
2. Google hesabinizla giris yapin
3. **"Create API Key"** butonuna tiklayin
4. Olusturulan anahtari kopyalayin

> **Not:** Free tier dakikada 15 istek (RPM) ve gunluk 1500 istek sinirina sahiptir. 159 personel icin yeterlidir.

---

## Kurulum

### 1. Projeyi indirin

```bash
git clone https://github.com/murataslan1/puantaj-app.git
cd puantaj-app/PuantajApp
```

Veya ZIP olarak indirip acin.

### 2. Uygulamayi calistirin

```bash
cd PuantajApp
dotnet run
```

Ilk calistirmada NuGet paketleri otomatik indirilir (internet gerekir, 1-2 dakika surebilir).
Uygulama penceresi acilacaktir.

> **Not:** API key'i ayrica bir dosyaya yazmaniza gerek yok. Uygulama icinden gireceksiniz ve otomatik kaydedilecek.

---

## Kullanim Kilavuzu (Adim Adim)

Uygulama 6 sekmeden olusur. Islemleri asagidaki siraya gore yapin:

```
Personel Import → PDF Parse → Onayla → Puantaj Kontrol → Hakedis Hesapla → Excel Cikti
```

---

### ADIM 1: Personel Ekleme (Personel Sekmesi)

Bu adimda calisanlari sisteme tanimlarsiniz. Iki yontem vardir:

#### Yontem A: Excel'den Toplu Import (Onerilen)

1. **Personel** sekmesine gidin
2. **"Excel'den Import"** butonuna tiklayin
3. Personel listesini iceren `.xlsx` dosyasini secin
4. Personeller tabloya yuklenecektir

**Excel Format Gereksinimleri:**
- Ilk satir baslik satiri olmalidir
- Uygulama sutunlari otomatik tanir (Ad Soyad, TC, Unvan, Birim, vb.)
- Sira numarasi (No) sutunu varsa otomatik atlanir
- Ornek format:

| No | Ad Soyad | TC | Unvan | Birim | Baslama | Birim Ucreti | Yillik Izin | Gececi |
|---|---|---|---|---|---|---|---|---|
| 1 | AYSEL KOKSALDI | 12345678901 | Kisisel.Op. | Pasaport | 01.01.2024 | 15000 | 14 | 0 |

#### Yontem B: Manuel Ekleme

1. **Personel** sekmesindeki formu doldurun (Ad Soyad, TC, Unvan, Birim, Birim Ucreti vb.)
2. **"Kaydet"** butonuna tiklayin

#### Personel Silme

1. Tablodan silmek istediginiz personeli secin (satirina tiklayin)
2. **"Sil"** butonuna tiklayin

---

### ADIM 2: PDF Okuma / Gemini AI Parse (PDF Aktar Sekmesi)

Bu adimda devam takip formu PDF'lerini AI ile okutursunuz.

1. **PDF Aktar** sekmesine gidin
2. **Ay** ve **Yil** degerlerini ayarlayin (hangi ayin puantaji?)
3. **API Key** alanina Gemini API anahtarinizi yapisitirin
   - Ilk giriste elle yazin, sonraki acilislarda otomatik hatirlanir
   - Key, uygulamanin `.env` dosyasina guvenli sekilde kaydedilir
4. **"PDF Sec..."** butonuyla devam takip formu PDF'lerini secin
   - Birden fazla PDF secebilirsiniz (Ctrl+Click veya Shift+Click)
5. **"Tumu Parse Et"** butonuna tiklayin
6. Bekleme suresi: Her PDF arasi ~4.5 saniye (API limiti icin)
   - 159 PDF icin toplam ~12 dakika
   - 429 hatasi alirsa otomatik bekleyip tekrar dener

**Parse Sonuclari:**
- **Eslesti** — PDF'deki isim veritabanindaki bir personelle eslestirildi
- **Eslesmedi** — Isim bulunamadi (personel import edilmemis olabilir)
- **Hata** — PDF okunamiyor veya API hatasi

> **Onemli:** Parse isleminden ONCE personellerin import edilmis olmasi gerekir. Aksi takdirde eslestirme yapilamaz.

---

### ADIM 3: Parse Sonuclarini Onaylama (PDF Aktar Sekmesi)

Parse tamamlandiktan sonra her PDF'i tek tek onaylamaniz gerekir.

1. PDF listesinde "Eslesti" yazan satirlari kontrol edin
2. **Eslesen Personel** kolonundaki ismin dogru oldugunu dogrulayin
3. Her satir icin **"Onayla+Kaydet"** butonuna tiklayin
4. Durum "Onaylandi" olarak degisecektir

> **Onemli:** "Onayla+Kaydet" tiklamadan puantaj kaydi olusturulmaz. Bu adimi ATLAMAYIN.

**Onaylama ne yapar?**
- PDF'den okunan giris/cikis saatlerini veritabanina yazar
- Ilgili personelin o ay icin puantaj kayitlarini olusturur
- Puantaj ve Hakedis sekmelerinde gorunur hale getirir

---

### ADIM 4: Puantaj Kontrolu (Puantaj Sekmesi)

Bu adimda PDF'den okunan verileri kontrol edip gerekirse duzeltirsiniz.

1. **Puantaj** sekmesine gidin
2. **Ay** ve **Yil** secin
3. **Personel** dropdown'undan bir personel secin
4. Tabloda o personelin gunluk giris/cikis saatleri gorunur

**Tablo Kolonlari:**
| Kolon | Aciklama |
|---|---|
| Gun | Gun numarasi (1-31) |
| Tip | Hi = Hafta ici, HS = Hafta sonu, RT = Resmi tatil |
| Giris | Ise giris saati (ornek: 09:00) |
| Cikis | Isten cikis saati (ornek: 18:00) |
| Mi/Yi/R | mi = Mazeret izni, yi = Yillik izin, r = Rapor |
| FM Grs | Fazla mesai giris saati |
| FM Cks | Fazla mesai cikis saati |
| FM St | Fazla mesai suresi (saat, otomatik hesaplanir) |
| Sure | Net calisma suresi (dinlenme dusulmus, otomatik) |
| Yemek | Yemek hakki (3+ saat calisma = 1 yemek) |
| Aciklama | Ek not |

5. Gerekirse satirlari duzeltebilirsiniz (giris/cikis saatleri, izin tipi, FM vb.)
6. **"Kaydet"** butonuna tiklayin

**Alt paneldeki ozet kartlari:**
- Toplam Sure (saat) — o aydaki toplam calisma suresi
- Yemek (gun) — yemek hakki gun sayisi
- FM (saat) — toplam fazla mesai
- RT FM (saat) — resmi tatil fazla mesai

---

### ADIM 5: Hakedis Hesaplama (Hakedis Sekmesi)

Bu adimda tum personellerin hakedisini hesaplarsiniz.

1. **Hakedis** sekmesine gidin
2. Parametreleri girin:
   - **Ay** / **Yil** — hesaplanacak donem
   - **Is Gunu** — o aydaki is gunu sayisi (ornek: 21)
   - **Yemek Br** — gunluk yemek birim ucreti (ornek: 150.00)
3. **"Hesapla"** butonuna tiklayin
4. Tum personellerin hakedisi tabloda gorunur

**Tablo Kolonlari:**
| Kolon | Aciklama |
|---|---|
| Ad Soyad | Personel adi |
| Hak.Gun | Hakedis gun sayisi |
| FM Saat / FM Ucr | Fazla mesai suresi ve ucreti |
| RT Saat / RT Ucr | Resmi tatil FM suresi ve ucreti |
| Yemek | Yemek hakki tutari |
| FM Ym | Fazla mesai yemek tutari |
| Hakedis | Faturalanacak toplam hakedis |

5. Bir personeli secin, alt panelde **manuel degerler** girin:
   - **Vergi Matrahi** — vergi matrahi tutari
   - **TSS-GSS** — TSS-GSS farki
   - **Kantin** — kantin ucreti
   - **Kesilecek** — hakedisten kesilecek tutar
6. **"Kaydet"** butonuna tiklayin

**Sag altta TOPLAM HAKEDIS tutari (TL) gorunur.**

---

### ADIM 6: Excel Cikti Olusturma (Excel Cikti Sekmesi)

Bu adimda puantaj ve hakedis verilerini Excel dosyasina aktarirsiniz.

1. **Excel Cikti** sekmesine gidin
2. Parametreleri girin: Ay, Yil, Is Gunu, Yemek Br
3. **Kayit Yeri** alaninda dosyanin kaydedilecegi klasoru secin ("Sec..." butonu ile)
4. Iki tur cikti olusturabilirsiniz:
   - **"Puantaj Excel Olustur"** → `Puantaj_OCAK_2026_Mesai.xlsx`
   - **"Hakedis Kapak Excel Olustur"** → `NVI_TD_OCAK_2026_HAKEDIS.xlsx`
5. Dosyalar sectiginiz klasore kaydedilir

---

### ADIM 7: Belge Yonetimi (Belgeler Sekmesi) — Opsiyonel

Personellere ait PDF/resim belgelerini arsivleyebilirsiniz.

1. **Belgeler** sekmesine gidin
2. **Personel**, **Ay**, **Yil** ve **Belge Tipi** secin:
   - `devam_takip` — devam takip formu
   - `izin_formu` — izin formu
   - `rapor` — saglik raporu
3. **"PDF/Resim Yukle"** ile belgeyi ekleyin
4. Yuklenen belgeler listede gorunur
5. **"Ac"** ile goruntuleyin, **"Sil"** ile kaldirin

---

## Hakedis Formulleri

| Hesaplama | Formul |
|---|---|
| Net Calisma Suresi | Giris-Cikis farki - dinlenme suresi |
| Yemek Hakki | 3 saat ve uzeri calisma = 1 yemek |
| FM Ucreti | FM Saat x Birim Ucreti / 225 x 1.5 |
| RT FM Ucreti | RT Saat x Birim Ucreti / 225 x 2 |
| Faturalanacak Hakedis | Temel + FM + RT + Yemek + Kantin + Vergi + GSS - Kesilecek |

**Dinlenme suresi dusumu:**
| Calisma Suresi | Dinlenme |
|---|---|
| 0 - 4 saat | 0.25 saat |
| 4 - 7.5 saat | 0.5 saat |
| 7.5 - 11 saat | 1 saat |
| 11 - 15 saat | 1.5 saat |
| 15+ saat | 2 saat |

---

## Dosya Yapisi

```
puantaj-app/
  README.md                  <- Bu dosya
  PuantajApp/
    PuantajApp.csproj        <- Proje dosyasi
    Program.cs               <- Giris noktasi
    App.axaml                <- Tema ve global stiller
    .env                     <- Gemini API Key (otomatik kaydedilir, repo'ya girmez)
    .gitignore
    Data/
      AppDbContext.cs         <- Veritabani (SQLite)
    Models/                  <- Veri modelleri
    Services/                <- Is mantigi servisleri
      GeminiService.cs       <- Gemini AI entegrasyonu
      ExcelImportService.cs  <- Excel import
      ExcelExportService.cs  <- Excel export
      HesaplamaService.cs   <- Hakedis hesaplama
      EnvService.cs          <- .env dosya yonetimi
      BelgeService.cs        <- Belge arsivleme
    Views/                   <- Ekran tasarimlari (AXAML)
    ViewModels/              <- Ekran mantiklari
    puantaj.db               <- Veritabani dosyasi (otomatik olusur)
```

---

## Sorun Giderme

**S: Uygulama acilmiyor.**
C: `dotnet --version` komutuyla .NET 9 kurulu oldugundan emin olun. 9.0.x olmalidir.

**S: PDF parse "API Hatasi 404" diyor.**
C: Gemini modeli degismis olabilir. `GeminiService.cs` dosyasindaki `MODEL` degerini kontrol edin. Mevcut: `gemini-2.5-flash-lite`.

**S: PDF parse "API Hatasi 429" diyor.**
C: API kota limiti doldu. Uygulama otomatik olarak bekleyip tekrar dener (3 deneme). Cok fazla 429 aliyorsaniz birkac dakika bekleyin veya https://aistudio.google.com/apikey adresinden yeni bir API key olusturun.

**S: PDF parse ediliyor ama personelle eslesmedi.**
C: Once Personel sekmesinden personelleri import edin. PDF'deki isimler veritabanindaki isimlerle eslestirilir.

**S: Puantaj sekmesinde veri gorunmuyor.**
C: PDF Aktar sekmesinde parse sonuclarini "Onayla+Kaydet" ile onayladiniz mi? Onaylanmadan puantaj kaydi olusturulmaz.

**S: Hakedis sekmesinde liste bos.**
C: Once puantaj kayitlarinin oldugundan emin olun (Adim 4). Sonra "Hesapla" butonuna tiklayin.

**S: Excel'den import yanlis isimler getiriyor.**
C: Excel dosyanizin ilk satirinin baslik satiri oldugundan emin olun. Uygulama "Ad Soyad", "TC", "Unvan" gibi basliklari otomatik tanir.

**S: Veritabanini sifirlamak istiyorum.**
C: `PuantajApp/puantaj.db` dosyasini silip uygulamayi yeniden baslatin. Bos veritabani otomatik olusur.

**S: API key'imi nereye giriyorum?**
C: PDF Aktar sekmesindeki "API Key" alanina yapisitirin. Otomatik olarak `.env` dosyasina kaydedilir. Bir dahaki acilista tekrar girmenize gerek kalmaz.

**S: macOS'ta dosya izin hatasi aliyorum.**
C: Terminal'de `chmod +x` gerekebilir veya System Preferences > Privacy'den izin verin.

---

## Teknolojiler

- **C# / .NET 9** — Uygulama platformu
- **Avalonia UI** — Cross-platform masaustu UI framework
- **SQLite** — Yerel veritabani (Entity Framework Core)
- **Google Gemini 2.5 Flash Lite** — PDF Vision AI
- **ClosedXML** — Excel okuma/yazma
- **CommunityToolkit.Mvvm** — MVVM pattern

---

## Teknik Mimari

### Uygulama Katmanlari

```mermaid
graph TB
    subgraph UI["UI Katmani (Views - AXAML)"]
        MW[MainWindow]
        PV[PersonelView]
        PAV[PdfAktarView]
        PTV[PuantajView]
        HV[HakedisView]
        ECV[ExcelCiktiView]
        BV[BelgeView]
    end

    subgraph VM["ViewModel Katmani (MVVM)"]
        MVM[MainWindowViewModel]
        PVM[PersonelViewModel]
        PAVM[PdfAktarViewModel]
        PTVM[PuantajViewModel]
        HVM[HakedisViewModel]
        ECVM[ExcelCiktiViewModel]
        BVM[BelgeViewModel]
    end

    subgraph SVC["Servis Katmani"]
        GS[GeminiService]
        EIS[ExcelImportService]
        EES[ExcelExportService]
        HS[HesaplamaService]
        BS[BelgeService]
        ES[EnvService]
    end

    subgraph DATA["Veri Katmani"]
        DB[(SQLite - puantaj.db)]
        CTX[AppDbContext<br/>Entity Framework Core]
    end

    subgraph EXT["Dis Servisler"]
        GEMINI[Google Gemini API<br/>gemini-2.5-flash-lite]
        EXCEL[Excel Dosyalari<br/>.xlsx]
        PDF[PDF Dosyalari<br/>Devam Takip Formu]
        ENV[.env Dosyasi<br/>API Key]
    end

    MW --> MVM
    PV --> PVM
    PAV --> PAVM
    PTV --> PTVM
    HV --> HVM
    ECV --> ECVM
    BV --> BVM

    PAVM --> GS
    PVM --> EIS
    ECVM --> EES
    HVM --> HS
    BVM --> BS
    PAVM --> ES

    GS --> GEMINI
    EIS --> EXCEL
    EES --> EXCEL
    GS --> PDF
    ES --> ENV

    PVM --> CTX
    PAVM --> CTX
    PTVM --> CTX
    HVM --> CTX
    BVM --> CTX
    CTX --> DB

    style UI fill:#1E3A5F,stroke:#2563EB,color:#F1F5F9
    style VM fill:#1E293B,stroke:#334155,color:#CBD5E1
    style SVC fill:#1E293B,stroke:#334155,color:#CBD5E1
    style DATA fill:#14532D,stroke:#22C55E,color:#F1F5F9
    style EXT fill:#7F1D1D,stroke:#EF4444,color:#FCA5A5
```

### Veri Akisi (Is Sureci)

```mermaid
flowchart LR
    A[Excel Dosyasi<br/>Personel Listesi] -->|ExcelImportService| B[(SQLite DB<br/>Personeller)]
    C[PDF Dosyalari<br/>Devam Takip Formu] -->|GeminiService| D[PuantajParseResult<br/>JSON]
    D -->|Eslestirme + Onay| E[(SQLite DB<br/>PuantajKayitlar)]
    B --> F{HesaplamaService}
    E --> F
    F -->|Hesapla| G[(SQLite DB<br/>HakedisEkVeri)]
    E -->|ExcelExportService| H[Puantaj.xlsx]
    G -->|ExcelExportService| I[Hakedis.xlsx]

    style A fill:#2563EB,stroke:#3B82F6,color:#fff
    style C fill:#2563EB,stroke:#3B82F6,color:#fff
    style B fill:#22C55E,stroke:#16A34A,color:#fff
    style E fill:#22C55E,stroke:#16A34A,color:#fff
    style G fill:#22C55E,stroke:#16A34A,color:#fff
    style D fill:#F59E0B,stroke:#D97706,color:#fff
    style F fill:#8B5CF6,stroke:#7C3AED,color:#fff
    style H fill:#EF4444,stroke:#DC2626,color:#fff
    style I fill:#EF4444,stroke:#DC2626,color:#fff
```

### Gemini AI PDF Parse Akisi

```mermaid
sequenceDiagram
    participant U as Kullanici
    participant App as PdfAktarViewModel
    participant GS as GeminiService
    participant API as Gemini API
    participant DB as SQLite

    U->>App: PDF Sec + "Tumu Parse Et"
    loop Her PDF icin
        App->>GS: ParsePdfAsync(pdfBytes)
        GS->>GS: PDF → Base64 donusum
        GS->>API: POST /generateContent<br/>(PDF + Prompt)
        alt Basarili (200)
            API-->>GS: JSON yanit
            GS->>GS: JSON parse → PuantajParseResult
            GS-->>App: ParseResult
            App->>App: Personel eslestirme
        else Rate Limit (429)
            API-->>GS: 429 Hata
            GS->>GS: Exponential Backoff<br/>(5s → 15s → 45s)
            GS->>API: Retry
        end
        App->>App: 4.5s bekleme (Rate Limit)
    end
    U->>App: "Onayla+Kaydet"
    App->>DB: PuantajKayit INSERT
```

### Veritabani Semasi

```mermaid
erDiagram
    Personeller {
        int Id PK
        string AdSoyad
        string TC
        string Unvan
        string Birim
        datetime BaslamaTarihi
        decimal BirimUcreti
        decimal YillikIzinHakki
        int Gececi
    }

    PuantajKayitlar {
        int Id PK
        int PersonelId FK
        int Yil
        int Ay
        int Gun
        string GunTipi
        string GirisSaati
        string CikisSaati
        string IzinTipi
        string FmGiris
        string FmCikis
        decimal FmSaat
        string Aciklama
    }

    HakedisEkVeri {
        int Id PK
        int PersonelId FK
        int Yil
        int Ay
        decimal VergiMatrahi
        decimal TssGssFarki
        decimal KantinUcreti
        decimal HakedistenKesilecek
    }

    Belgeler {
        int Id PK
        int PersonelId FK
        int Yil
        int Ay
        string BelgeTipi
        string DosyaAdi
        string DosyaYolu
        datetime YuklenmeTarihi
    }

    HakedisParametre {
        int Id PK
        int Yil
        int Ay
        int IsGunu
        decimal YemekBirimUcreti
    }

    Personeller ||--o{ PuantajKayitlar : "puantaj kayitlari"
    Personeller ||--o{ HakedisEkVeri : "hakedis ek verileri"
    Personeller ||--o{ Belgeler : "belgeler"
```

### MVVM Mimari Deseni

```mermaid
graph LR
    subgraph View["View (AXAML)"]
        V1[".axaml dosyalari<br/>UI tanimlamalari"]
        V2["Data Binding<br/>{Binding Property}"]
        V3["Command Binding<br/>{Binding XCommand}"]
    end

    subgraph ViewModel["ViewModel (C#)"]
        VM1["ObservableProperty<br/>UI state"]
        VM2["RelayCommand<br/>UI aksiyonlari"]
        VM3["Servis cagrilari"]
    end

    subgraph Model["Model + Services"]
        M1["Entity modelleri<br/>Personel, PuantajKayit..."]
        M2["Servisler<br/>GeminiService, HesaplamaService..."]
        M3["AppDbContext<br/>EF Core"]
    end

    V2 -->|TwoWay Binding| VM1
    V3 -->|Command| VM2
    VM2 --> VM3
    VM3 --> M2
    VM3 --> M3
    M3 --> M1

    style View fill:#1E3A5F,stroke:#2563EB,color:#F1F5F9
    style ViewModel fill:#1E293B,stroke:#334155,color:#CBD5E1
    style Model fill:#14532D,stroke:#22C55E,color:#F1F5F9
```
