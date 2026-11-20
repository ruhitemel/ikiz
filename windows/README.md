# İkiz — Windows

Everything benzeri hızlı arama + **aynı ad + aynı boyut + aynı tür** kuralıyla çift kopya bulma ve onaylı temizleme.

İkiz seçtiğin klasörleri tarar, aynı dosyaları grup grup gösterir. **Sen onaylamadan hiçbir dosyayı silmez.** Varsayılan silme yöntemi **Geri Dönüşüm Kutusu**'dur.

## Kurulum ve güncelleme

[Releases](https://github.com/ruhitemel/ikiz/releases) sayfasından kurulum dosyasını indir:

- **`ikiz-win-Setup.exe`** — tek tıkla kurar, soru sormaz.
- **`ikiz-win.msi`** — alışılmış sihirbazla kurulur: **İleri → İleri → Kurulum → Bitti**. "Bu kullanıcı için mi, tüm kullanıcılar için mi?" sorusu gelirse ev bilgisayarında **Bu kullanıcı için** yeterlidir.

Kurulumlu sürüm GitHub Releases üzerinden yeni sürümü denetler. Kontrol açılışta yapılır; ayrıca **Hakkında → Güncellemeleri denetle** de diyebilirsin. Yeni sürüm indirilir, uygulama yeniden başlatıldığında kurulur. Taşınabilir (`Portable`) sürümde otomatik güncelleme yoktur; sürüm sayfasından yeni dosyayı indirmen gerekir.

## Gereksinimler

- Windows 10/11 (x64)
- Kaynaktan çalıştırmak için .NET 8 SDK (`dotnet --info` ile kontrol et)

## Kaynaktan çalıştırma

```powershell
dotnet run --project src\Ikiz.App
```

Belirli bir klasörle açmak için:

```powershell
dotnet run --project src\Ikiz.App -- "D:\Arsiv"
```

Release derlemesi:

```powershell
dotnet publish src\Ikiz.App -c Release -r win-x64 --self-contained true -o publish
```

## Demo verisi

Gerçek dosyalara dokunmadan, `%TEMP%\ikiz-demo` altında 31 örnek dosya üretir (12 çift grup):

```powershell
powershell -ExecutionPolicy Bypass -File demo\olustur.ps1
dotnet run --project src\Ikiz.App -- "$env:TEMP\ikiz-demo"
```

Temizlemek için:

```powershell
powershell -ExecutionPolicy Bypass -File demo\olustur.ps1 -Temizle
```

## Kullanım rehberi

1. **Klasör seç** — açılışta Masaüstü, Belgeler, İndirilenler, Resimler, Müzik ve Videolar taranır. Başka bir disk veya klasör eklemek için üstteki **Konum ekle** düğmesine bas. Diskleri işaretleyip **Seçilenleri ekle ve tara** de. Listeden bir kökü kaldırabilir, **Yeniden indeksle** ile her şeyi baştan tarayabilirsin. Tarama sürerken ilerleme çubuğu, dosya sayısı, geçen süre ve kalan süre görünür. Yalnızca belge, resim, ses ve video dosyaları taranır; sistem ve uygulama dosyaları listeye girmez.
2. **Ara** — arama kutusuna birkaç harf yaz; sonuçlar anında gelir. `İ/I/ı` aynı harf sayılır; `ş/s`, `ğ/g`, `ç/c`, `ö/o`, `ü/u` da öyle. Birden fazla kelime yazarsan ikisinin de olması gerekir. Varsayılan olarak dosya adına bakar; **Yol da arama** açarsan klasör adları da aranır.
3. **Çiftleri gör** — adı, boyutu ve türü aynı olan dosyalar bir grupta toplanır. "Kazanılabilir alan", gereksiz kopyaların toplam boyutudur.
4. **Hızlı seç** — her satırın başındaki kutuyu işaretle. Toplu işlem için **Tümünü seç**, **Kopyaları seç** veya **Boş / 0 KB seç** bağlantısını kullan. **Kopyaları seç** her gruptan bir dosyayı bırakır, fazlalıkları işaretler. **Boş / 0 KB seç** boş klasörleri ve 0 KB dosyaları listeler ve tümünü işaretler (filtre çubuğundaki **Boş klasör ve 0 KB** anahtarıyla da açıp kapatabilirsin). Sonra **Panoya al**, **Sil** veya **Adlandır** düğmesine bas. İşlem bitince seçim temizlenir.
5. **Panoyu kontrol et** — **Panoya al** pano listesini kendiliğinden açmaz; üstteki **Panoyu göster** düğmesiyle aç. Panodaki dosyalar bellekte bekler. **Panodakileri gizle** açıkken (varsayılan) bu dosyalar sonuç listesinden çıkar; panodan çıkarınca geri gelir. Panoda tek tek seçebilir, **Tümünü seç**, **Hiçbirini seç** veya **1 koru** diyebilirsin. "×" ile bir satırı çıkarırsın.
6. **Onay ver ve sil** — "Listeyi kontrol ettim..." kutusunu işaretlemeden silme düğmesi açılmaz. Seçimi değiştirirsen onay sıfırlanır. Varsayılan işlem **Geri Dönüşüm Kutusu'na taşımaktır**; kalıcı silme ayrı ve uyarılı bir seçenektir. Bir grubun tamamı seçilirse kırmızı uyarı görürsün. Son silme işlemi bildirimdeki **Geri al** ile ya da sağ tık → **Son silmeyi geri al** ile döndürülebilir.
7. **Sağ tık menüsü** — dosya satırında veya grup başlığında sağ tıkla:
   - **Panoya ekle / Panodan çıkar** — yalnızca o dosyayı ekler veya çıkarır,
   - **Adı düzenle** — tek dosya, o adın kopyaları, o türdeki dosyalar veya tüm sonuçlar için toplu ad değiştirme; önizleme gösterilir, uzantı korunur,
   - **Sil (yalnızca bu dosya)** — sadece o kopyayı siler,
   - **Tüm kopyaları sil (bu dosya kalsın)** — üzerine tıkladığın dosya kalır,
   - **Her kopyayı sil (diskte hiç kalmaz)** — grubun tamamı gider, kırmızı uyarı çıkar,
   - **Son silmeyi geri al**.
8. **Tema ve paylaşım** — üstteki **Açık / Koyu** seçici temayı anında değiştirir; seçim saklanır. **Hakkında → Paylaşım denetimi** ekranı, ekran görüntüsü paylaşmadan önce kişisel bilgi olup olmadığını madde madde kontrol eder; **Özeti kopyala** ile temiz bir metin alırsın. Geliştirici bilgisi yalnızca **Hakkında** bölümündedir.

## Kısayollar

| Tuş | İşlev |
| --- | --- |
| `Ctrl+K` | Arama kutusuna git |
| `/` | Arama kutusuna git (metin alanında değilken) |
| `F2` | Fare üzerindeki dosyayı yeniden adlandır |
| `Del` | Fare üzerindeki dosyayı sil (onay ister) |
| `Esc` | Açık diyaloğu / silme panosunu kapat |

## Güvenlik kuralları

- Onay olmadan **hiçbir** dosya işlemi yapılmaz; pano yalnızca bellekte bekler.
- Silme her zaman ad, klasör ve boyut gösteren bir önizleme listesi üzerinden yapılır.
- **Tüm kopyaları sil** seçeneğinde sağ tık yapılan dosya korunur; yalnızca diğer kopyalar silinir.
- **Her kopyayı sil** seçeneğinde diskte hiç kopya kalmayacağı uyarısı gösterilir.
- Yeniden adlandırma önizlemesi gösterilir; birden fazla dosya etkilenecekse ayrıca onay kutusu istenir. Çakışan adlara `(2)`, `(3)` eklenir.
- Geri Dönüşüm Kutusu'na taşınan son işlem geri alınabilir.
- Seçim veya silme modu değişince onay sıfırlanır.
- Geri Dönüşüm Kutusu varsayılandır; kalıcı silme ayrı seçenektir.
- Simge bağlantıları (reparse point), `$Recycle.Bin`, `System Volume Information`, `node_modules`, `.git` gibi klasörler taranmaz.
- Arama, gruplama ve silme bu cihazda yapılır; dosya adları, yollar veya içerikler hiçbir sunucuya gönderilmez. Güncelleme denetimi yalnızca genel GitHub Releases verisini okur.
- Hatalar `%LOCALAPPDATA%\Ikiz\ikiz-error.log` dosyasına yazılır.

## Proje yapısı

```
windows/
  Ikiz.slnx
  src/Ikiz.Core/          # platformdan bağımsız çekirdek
    TextNormalizer.cs     # Türkçe katlama + vurgu aralıkları
    FileItem.cs           # dosya kaydı + çift anahtarı
    FileCategories.cs     # belge / görsel / ses / video beyaz listesi
    FileIndex.cs          # bellek içi indeks, arama, gruplama
    FolderScanner.cs      # klasör taraması
    DeleteBoard.cs        # silme panosu durum makinesi
    FileDeleter.cs        # geri dönüşüm kutusu / kalıcı silme
    FileRenamer.cs        # ad doğrulama, çakışma çözümü, toplu yeniden adlandırma
    RecycleBinRestorer.cs # geri dönüşüm kutusundan geri yükleme
  src/Ikiz.App/           # WPF arayüzü (.NET 8)
    Theme/Dark.xaml       # koyu tema renkleri
    Theme/Light.xaml      # açık tema renkleri
    ThemeManager.cs       # tema değiştirme + kalıcı seçim
  installer/              # kurulum sihirbazı metinleri
  tests/Ikiz.Tests/       # bağımlılıksız doğrulama programı
  demo/olustur.ps1        # örnek veri üreteci
```

Testler:

```powershell
dotnet run --project tests\Ikiz.Tests
```

## Bilinen sınırlar / sonraki adımlar

- Tarama her açılışta ve her "Yeniden indeksle"de baştan yapılır; canlı dosya izleme yok.
- MFT tabanlı indeks yok; milyonlarca dosyada ilk tarama disk hızına bağlıdır.
- Ağ sürücüleri ve çok uzun yollar için ek test gerekir.
- Dosya önizleme, kural bazlı otomatik seçim ve zamanlanmış tarama planlanıyor.
- Windows'ta geri dönüşüm kutusu `SHFileOperation` ile yapılır; kalıcı silme `File.Delete` kullanır.
