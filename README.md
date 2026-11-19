# İkiz

Windows için hızlı dosya arama ve çift kopya temizleme aracı. Everything benzeri anlık arama yapar; **aynı ad + aynı boyut + aynı tür** kuralıyla çiftleri gruplar ve **yalnızca sen onayladıktan sonra** dosya işlemi yapar.

> Gizlilik: Tüm arama, gruplama ve silme işlemleri bu cihazda yapılır. Dosya adları, yollar veya içerikler hiçbir sunucuya gönderilmez. Güncelleme denetimi yalnızca genel GitHub Releases üzerinden sürüm bilgisi okur.

## Özellikler

- **Anında Türkçe duyarsız arama** — `İ/I/ı → i`, `ş → s`, `ğ → g`, `ç → c`, `ö → o`, `ü → u`; çoklu kelime AND mantığı; isteğe bağlı yol arama.
- **Yalnızca belge ve medya** — görsel, ses, video ve belge dosyaları indekslenir; sistem, gizli, çalıştırılabilir ve uygulama dosyaları listeye girmez.
- **Güvenli temizleme** — önizleme listesi, tek tek seçim, onay kutusu; varsayılan hedef **Geri Dönüşüm Kutusu**.
- **Esnek silme** — yalnızca bu dosya, diğer kopyalar (seçili dosya kalır) veya tüm kopyalar (diskte hiç kalmaz).
- **Sağ tık menüsü** — panoya ekle/çıkar, adı düzenle, toplu yeniden adlandırma, silme ve son silmeyi geri alma.
- **Açık/koyu tema** — Windows tercihiyle başlar, seçim kalıcıdır.
- **Otomatik güncelleme** — kurulumlu sürümler GitHub Releases üzerinden yeni sürümü denetler, indirir ve yeniden başlatınca uygular.
- **Erişilebilirlik** — klavye kısayolları, ekran okuyucu etiketleri ve yüksek kontrastlı iki tema.

## Hızlı başlangıç (Windows)

Gereksinimler: Windows 10/11 (x64), [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

```powershell
git clone https://github.com/ruhitemel/ikiz.git
cd ikiz
dotnet run --project windows/src/Ikiz.App
```

Belirli bir klasörle açmak için:

```powershell
dotnet run --project windows/src/Ikiz.App -- "D:\Arsiv"
```

Testler:

```powershell
dotnet run --project windows/tests/Ikiz.Tests/Ikiz.Tests.csproj
```

Ayrıntılı kullanım, kısayollar ve güvenlik kuralları: [`windows/README.md`](windows/README.md).

Kurulumlu sürüm (güncellemeleri almak için) ve taşınabilir sürüm indirmek için [Releases](https://github.com/ruhitemel/ikiz/releases) sayfasına bak. `Ikiz.Setup.exe` ile kurulan sürüm otomatik güncellemeyi etkinleştirir; taşınabilir sürümde güncelleme denetimi yerine sürüm sayfası açılır.

## Arayüz prototipi

[`prototip/index.html`](prototip/index.html) tek dosyalık, bağımlılıksız bir arayüz prototipidir. Örnek dosyalarla çalışır, gerçek dosyaya erişmez ve hiçbir şey silmez. Tarayıcıda açarak akışı inceleyebilirsin.

## Proje yapısı

```text
ikiz/
├── windows/
│   ├── Ikiz.slnx
│   ├── src/Ikiz.Core/     # platformdan bağımsız çekirdek
│   ├── src/Ikiz.App/      # WPF arayüzü (.NET 8)
│   ├── tests/Ikiz.Tests/  # bağımlılıksız doğrulama programı
│   └── demo/olustur.ps1   # örnek veri üreteci
├── prototip/index.html    # arayüz prototipi
└── .github/               # CI, release ve şablonlar
```

## Katkı ve lisans

Katkılar memnuniyetle karşılanır; [CONTRIBUTING.md](CONTRIBUTING.md) ve [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md) dosyalarına bak. Güvenlik açıklarını herkese açık issue yerine [SECURITY.md](SECURITY.md) içindeki yöntemle bildir.

Bu proje [MIT Lisansı](LICENSE) ile açık kaynaktır. Geliştirici notu uygulamanın **Hakkında** bölümünde yer alır.

## Yol haritası

- [x] Windows MVP: WPF arayüzü, indeks, arama, gruplama, güvenli silme, geri alma
- [x] Açık/koyu tema, toplu yeniden adlandırma, otomatik güncelleme altyapısı
- [ ] Canlı dosya izleme (USN Journal / FileSystemWatcher)
- [ ] Kural bazlı otomatik seçim ve zamanlanmış tarama
- [ ] macOS sürümü (SwiftUI + Spotlight)
