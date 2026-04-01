# Puantaj & Hakedis Uygulamasi

NVi TD projesi icin altyuklenici personel puantaj takip ve hakedis hesaplama uygulamasi.

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

> **Not:** Free tier gunluk 250 istek sinirina sahiptir. 159 personel icin yeterlidir.

---

## Kurulum

### 1. Projeyi indirin

Proje klasorunu bilgisayariniza kopyalayin veya ZIP olarak indirip acin.

### 2. Gemini API Key'i ayarlayin

`PuantajApp/` klasoru icinde `.env` dosyasini acin ve API anahtarinizi yapisitirin:

```
GEMINI_API_KEY=buraya_api_anahtarinizi_yapisitirin
```

> `.env` dosyasi gizli bir dosyadir. macOS'ta Finder'da gormek icin `Cmd+Shift+.` tuslayin.
> Windows'ta Notepad ile acabilirsiniz.

### 3. Uygulamayi calistirin

Terminal (macOS) veya Komut Satiri (Windows) acin, proje klasorune gidin ve calistirin:

```bash
cd /yol/klasor/puantaj_app/PuantajApp
dotnet run
```

Ilk calistirmada NuGet paketleri otomatik indirilir (internet gerekir, 1-2 dakika surebilir).
Uygulama penceresi acilacaktir.

---

## Kullanim

### Adim 1: Personel Ekleme

1. **Personel** sekmesine gidin
2. Iki yontemle personel ekleyebilirsiniz:
   - **Excel'den Import:** "Excel'den Import" butonuna tiklayin, personel listesini iceren `.xlsx` dosyasini secin
   - **Manuel:** Formu doldurup "Kaydet" butonuna tiklayin

**Excel Import Formati:**

| A (Ad Soyad) | B (TC) | C (Unvan) | D (Birim) | E (Baslama) | F (Birim Ucreti) | G (Yillik Izin) | H (Gececi) |
|---|---|---|---|---|---|---|---|
| AYSEL KOKSALDI | 12345678901 | Kisisel.Op. | Pasaport | 01.01.2024 | 15000 | 14 | 0 |

Ilk satir baslik satiridir, veriler 2. satirdan baslar.

### Adim 2: PDF Okuma (Gemini AI)

1. **PDF Aktar** sekmesine gidin
2. **Ay** ve **Yil** degerlerini ayarlayin
3. **API Key** alaninda anahtarinizin gorundugundan emin olun (`.env`'den otomatik okunur)
4. **"PDF Sec..."** butonuyla devam takip formu PDF'lerini secin (birden fazla secebilirsiniz)
5. **"Tumu Parse Et"** butonuna tiklayin
6. Gemini AI her PDF'i okuyup personelle eslestirecektir
7. Sonuclari kontrol edin, dogru olanlarda **"Onayla+Kaydet"** butonuna tiklayin

> **Ipucu:** 159 PDF icin yaklasik 15-20 dakika surer (API rate limiti nedeniyle).

### Adim 3: Puantaj Kontrolu

1. **Puantaj** sekmesine gidin
2. Ay, Yil ve Personel secin
3. PDF'den okunan veriler tabloda gorunur
4. Gerekirse satirlari duzenleyin (giris/cikis saatleri, izin tipi, fazla mesai vb.)
5. **"Kaydet"** butonuna tiklayin

**Kolon Aciklamalari:**
- **Tip:** Hi = Hafta ici, HS = Hafta sonu, RT = Resmi tatil
- **Mi/Yi/R:** mi = Mazeret izni, yi = Yillik izin, r = Rapor
- **FM Grs/Cks:** Fazla mesai giris/cikis saati
- **FM St:** Fazla mesai saat (otomatik hesaplanir)
- **Sure:** Dinlenme dusulmus net calisma suresi (otomatik)

### Adim 4: Hakedis Hesaplama

1. **Hakedis** sekmesine gidin
2. Ay, Yil, Is Gunu ve Yemek Birim Ucreti ayarlayin
3. **"Hesapla"** butonuna tiklayin — tum personelin hakedisi hesaplanir
4. Bir personel secin, alt panelde **manuel alanlari** girin:
   - Vergi Matrahi
   - TSS-GSS Farki
   - Kantin Ucreti
   - Hakedisten Kesilecek
5. **"Kaydet"** butonuna tiklayin

### Adim 5: Excel Cikti

1. **Excel Cikti** sekmesine gidin
2. Ay, Yil, Is Gunu, Yemek Birim Ucretini ayarlayin
3. Kayit yerini secin (varsayilan: Masaustu)
4. **"Puantaj Excel Olustur"** → `Puantaj_OCAK_2026_Mesai.xlsx`
5. **"Hakedis Kapak Excel Olustur"** → `NVI_TD_OCAK_2026_HAKEDIS.xlsx`

### Adim 6: Belge Yonetimi (Opsiyonel)

1. **Belgeler** sekmesine gidin
2. Personel, Ay, Yil secin
3. Belge tipini secin (devam_takip, izin_formu, rapor)
4. **"PDF/Resim Yukle"** ile belgeyi ekleyin
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
- 4 saate kadar: 0.25 saat
- 4-7.5 saat: 0.5 saat
- 7.5-11 saat: 1 saat
- 11-15 saat: 1.5 saat
- 15 saat ustu: 2 saat

---

## Dosya Yapisi

```
puantaj_app/
  README.md                  ← Bu dosya
  PLAN.md                    ← Teknik plan dokumani
  PuantajApp/
    PuantajApp.csproj        ← Proje dosyasi
    Program.cs               ← Giris noktasi
    .env                     ← Gemini API Key (gizli)
    .gitignore
    Data/
      AppDbContext.cs         ← Veritabani (SQLite)
    Models/                  ← Veri modelleri
    Services/                ← Is mantigi servisleri
    Views/                   ← Ekran tasarimlari (AXAML)
    ViewModels/              ← Ekran mantiklari
    puantaj.db               ← Veritabani dosyasi (otomatik olusur)
```

---

## Sik Sorulan Sorular

**S: Uygulama acilmiyor, hata alirim.**
C: `dotnet --version` komutuyla .NET 9 kurulu oldugundan emin olun. 9.0.x olmalidir.

**S: PDF parse "API Hatasi 400" diyor.**
C: `.env` dosyasindaki API key'i kontrol edin. Eksik veya hatali olabilir.

**S: PDF parse ediliyor ama personelle eslesmedi.**
C: Oncelikle Personel sekmesinden personelleri import edin. PDF'deki isimler veritabanindaki isimlerle eslestirilir.

**S: Veritabanini sifirlamak istiyorum.**
C: `PuantajApp/puantaj.db` dosyasini silip uygulamayi yeniden baslatin. Bos veritabani olusur.

**S: Excel ciktisini nereye kaydediyor?**
C: Excel Cikti sekmesindeki "Kayit Yeri" alaninda gosterilen klasore kaydeder. "Sec..." ile degistirebilirsiniz.

**S: macOS'ta dosya izin hatasi alirim.**
C: Terminal'de `chmod +x` gerekebilir veya System Preferences > Privacy'den izin verin.
