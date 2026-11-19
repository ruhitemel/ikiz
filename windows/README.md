# İkiz — Windows

Everything benzeri hızlı arama + **aynı ad + aynı boyut + aynı tür** kuralıyla çift kopya bulma ve onaylı temizleme.

Bu klasördeki uygulama **gerçek dosyalarla çalışır**: seçtiğin klasörleri tarar, çiftleri gruplar ve yalnızca onay verdiğinde siler. Varsayılan silme yöntemi **Geri Dönüşüm Kutusu**'dur. Sağ tık menüsünden tek dosya silme, **aslı kalacak şekilde diğer kopyaları silme**, toplu yeniden adlandırma ve son silmeyi geri alma yapılabilir.

## Gereksinimler

- Windows 10/11 (x64)
- .NET 8 SDK (`dotnet --info` ile kontrol et)

## Çalıştırma

```powershell
dotnet run --project src\Ikiz.App
```

Belirli bir klasörle açmak için:

```powershell
dotnet run --project src\Ikiz.App -- "D:\Arsiv"
```

Release derlemesi:

```powershell
dotnet publish src\Ikiz.App -c Release -r win-x64 --self-contained false -o publish
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

## Nasıl çalışır

1. **Tarama** — açılışta Masaüstü, Belgeler, İndirilenler, Resimler, Müzik ve Videolar taranır. Başlık çubuğundaki **Konum ekle** ile sürücülerin tamamı (`C:\`, `D:\` …; birim adları ve boş alanlarıyla birlikte listelenir), bağlı ağ/çıkarılabilir diskler veya belirli bir klasör eklenir. Birden fazla sürücü işaretlenip tek seferde **Seçilenleri ekle ve tara** denilebilir. Eklenen kökler panelde listelenir ve kaldırılabilir; **Yeniden indeksle** tüm kökleri baştan tarar. Tarama arka planda sürerken animasyonlu ilerleme çubuğu, taranan dosya sayısı, **geçen süre**, anlık hız ve **tahmini kalan süre** canlı gösterilir; ilk taramada toplam bilinmediği için belirsiz animasyon kullanılır. Yalnızca **belge, görsel, ses ve video** dosyaları indekslenir; sistem, gizli, çalıştırılabilir ve uygulama dosyaları listeye hiç girmez. Windows, Program Files, ProgramData, AppData, `$Recycle.Bin`, `System Volume Information`, `node_modules` ve `.git` gibi klasörler atlanır.
2. **Anında arama** — her tuşta sonuçlar yenilenir. Türkçe karakter katlama vardır: `İ`, `I`, `ı` → `i`; `ş` → `s`; `ğ` → `g`; `ç` → `c`; `ö` → `o`; `ü` → `u`. Birden çok kelime AND olarak aranır. Varsayılan olarak yalnızca dosya adında arar; "Yol da arama" tam yolu kapsar.
3. **Çift gruplama** — normalleştirilmiş ad + boyut + uzantı aynıysa dosyalar tek grupta toplanır. "Kazanılabilir alan" = `boyut × (kopya sayısı − 1)`.
4. **Silme panosu** — bir grubun "Panoya al" düğmesi veya satırdaki çöp kutusu, **o dosyanın bütün kopyalarını** panoya alır. Sağ tık → **Panoya ekle** ise yalnızca o dosyayı ekler. Pano otomatik açılmaz; kısa bir bildirim ve sayı güncellenir. İncelemek için başlık çubuğundaki **Panoyu göster** düğmesine bas. Pano bellekte bekler; panel kapatılsa bile korunur. Filtre çubuğundaki **Panodakileri gizle** açıkken (varsayılan) panodaki dosyalar sonuçlardan çıkar; panodan çıkarınca geri görünür.

   Ana ekranda hızlı çalışmak için satırlardaki kutuları işaretle ya da sonuç başlığındaki **Tümünü seç** / **Kopyaları seç** bağlantılarını kullan. **Kopyaları seç** her gruptan bir kopyayı korur, fazlalıkları işaretler. Ardından **Panoya al**, **Sil** (onaylı) veya **Adlandır** (toplu) düğmeleriyle tek seferde işlem yap. Seçim işlem sonrası otomatik temizlenir.
5. **Kontrol** — panoda her kopya ayrı satırdır:
   - tek tek işaretle / kaldır,
   - "Tümünü seç" / "Hiçbirini seç",
   - "1 koru" ile ilk kopyayı bırak, diğerlerini seç,
   - "×" ile satırı panodan çıkar.
6. **Onay** — "Listeyi kontrol ettim..." kutusu işaretlenmeden silme düğmesi açılmaz. Seçim değişirse onay sıfırlanır.
7. **Silme** — Geri Dönüşüm Kutusu (varsayılan) veya Kalıcı sil. Bir grubun tüm kopyaları seçilirse kırmızı uyarı çıkar. Sonuç bildirim olarak gösterilir ve oturumda kazanılan alan birikir.
8. **Sağ tık menüsü** — dosya satırında veya grup başlığında sağ tıkla:
   - **Panoya ekle / Panodan çıkar** (yalnızca o dosya),
   - **Adı düzenle…** — yalnızca bu dosya, bu adın tüm kopyaları, bu türdeki tüm dosyalar veya tüm sonuçlar için toplu yeniden adlandırma; canlı önizleme ve uzantı koruma,
   - **Sil… (yalnızca bu dosya)** — yalnızca seçili kopyayı siler (`Del`),
   - **Tüm kopyaları sil… (bu dosya kalsın)** — sağ tık yaptığın dosya korunur, diğer kopyalar silinir,
   - **Her kopyayı sil… (diskte hiç kalmaz)** — grubun tamamı silinir; kırmızı uyarı gösterilir,
   - **Son silmeyi geri al** — geri dönüşüm kutusundan özgün konuma geri yükler.
9. **Tema** — başlık çubuğundaki **Açık / Koyu** seçiciyle anında değiştirilir. İlk açılışta Windows'un tema ayarına uyulur; seçim `%LOCALAPPDATA%\Ikiz\theme.txt` içinde saklanır.
10. **Paylaşım denetimi** — **Hakkında → Paylaşım denetimi** ekranı; e-posta adresi, IP adresi, kişisel yol, kullanıcı adı, hata kaydı, gizli anahtar, veri aktarımı ve geliştirici notu maddelerini tek tek işaretler. Ekranda hiçbir dosya adı, kişi adı veya yerel yol gösterilmez; **Özeti kopyala** ile güvenli, kişisel bilgi içermeyen bir metin panoya alınır.
11. **Hakkında** — geliştirici notu `Ruhi Temel (@ruhitemel)`, MIT lisansı ve "açık kaynak, herkesin kullanımına açık" vurgusu burada yer alır.

## Kısayollar

| Tuş | İşlev |
| --- | --- |
| `Ctrl+K` | Arama kutusuna git |
| `/` | Arama kutusuna git (metin alanında değilken) |
| `F2` | Fare üzerindeki dosyayı yeniden adlandır |
| `Del` | Fare üzerindeki dosyayı sil (onay ister) |
| `Esc` | Açık diyaloğu / silme panosunu kapat |

## Okunabilirlik

- Gövde metni 13–15 px, ikincil metin en az 11 px; mikro etiketler yalnızca rozet ve simge içindedir.
- İkincil metin renkleri her iki temada **WCAG AA** eşiğinin üzerindedir (en düşük 4,8:1).
- Koyu temada ikincil metin `#98A7B9`, açık temada `#5B6878`; iki tema arasında geçişte vurgu renkleri de yenilenir.
- Yazı tipi `Segoe UI Variable Text` (Windows 10'da `Segoe UI`), ClearType ile çizilir. Satır aralıkları uzun paragraflarda artırıldı.

## Güvenlik kuralları

- Onay olmadan **hiçbir** dosya işlemi yapılmaz; pano yalnızca bellekte bekler.
- Silme her zaman ad, klasör ve boyut gösteren bir önizleme listesi üzerinden yapılır.
- **Tüm kopyaları sil** seçeneğinde sağ tık yapılan dosya korunur; yalnızca diğer kopyalar silinir.
- **Her kopyayı sil** seçeneğinde diskte hiç kopya kalmayacağı uyarısı gösterilir.
- Yeniden adlandırma önizlemesi gösterilir; birden fazla dosya etkilenecekse ayrıca onay kutusu istenir. Çakışan adlara `(2)`, `(3)` eklenir.
- Geri Dönüşüm Kutusu'na taşınan son işlem, bildirimdeki **Geri al** düğmesi veya sağ tık → **Son silmeyi geri al** ile geri yüklenebilir.
- Seçim veya silme modu değişince onay sıfırlanır.
- Geri Dönüşüm Kutusu varsayılandır; kalıcı silme ayrı seçenektir.
- Aynı dosyanın tüm kopyaları seçilirse "dosya tamamen silinecek" uyarısı verilir.
- Simge bağlantıları (reparse point), `$Recycle.Bin`, `System Volume Information`, `node_modules`, `.git` gibi klasörler taranmaz.
- Güncellemeler **Hakkında** penceresinden denetlenir. Kurulumlu sürüm (`Ikiz.Setup.exe`) yeni sürümü indirip yeniden başlatınca uygular; taşınabilir sürümde sürüm sayfası açılır.
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
  src/Ikiz.App/           # WPF arayüzü (.NET 8, WinUI'siz)
    Theme/Dark.xaml       # koyu tema renkleri
    Theme/Light.xaml      # açık tema renkleri
    ThemeManager.cs       # tema değiştirme + kalıcı seçim
  tests/Ikiz.Tests/       # bağımlılıksız doğrulama programı
  demo/olustur.ps1        # örnek veri üreteci
```

Testler:

```powershell
dotnet run --project tests\Ikiz.Tests
```

## Bilinen sınırlar / sonraki adımlar

- Tarama şu an her açılışta ve her "Yeniden indeksle"de baştan yapılır; canlı dosya izleme (USN Journal / FileSystemWatcher) yok.
- MFT tabanlı indeks yok; milyonlarca dosyada ilk tarama disk hızına bağlıdır.
- Ağ sürücüleri ve çok uzun yollar için ek test gerekir.
- Arayüz tek pencere; dosya önizleme, kural bazlı otomatik seçim ve zamanlanmış tarama planlanıyor.
- Windows'ta geri dönüşüm kutusu `SHFileOperation` ile yapılır; kalıcı silme `File.Delete` kullanır.
