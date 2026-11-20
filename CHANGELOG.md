# Değişiklik günlüğü

Bu dosya [Keep a Changelog](https://keepachangelog.com/tr/1.1.0/) biçimini izler. Sürüm numaraları [Semantic Versioning](https://semver.org/lang/tr/) kullanır.

## [1.4.1] - 2026-11-20

### Eklendi

- **Güncelleme bildirim kartı (İndir ve kur / Ertele)** — yeni bir sürüm yayınlandığında uygulama açılışında ekranın sağ altında küçük bir bildirim kartı belirir; tek tıkla **İndir ve kur** (canlı ilerleme çubuğuyla indirir ve yeniden başlatır), **Ertele** veya **Sürüm notları** seçenekleri sunar.

### Düzeltildi

- **Hakkında alt çubuk yerleşimi** — "MIT lisansıyla açık kaynak…" metni ile **Paylaşım denetimi** ve **Kapat** düğmelerinin üst üste binmesine yol açan eksik sütun tanımı düzeltildi.

[1.4.1]: https://github.com/ruhitemel/ikiz/releases/tag/v1.4.1

## [1.4.0] - 2026-11-20

### Eklendi

- **Üç sabit sekme (sade arayüz)** — üst çubukta kalabalık anahtarlar yerine **Çift Kopyalar**, **Hızlı Dosya Arama** (hem dosya adında hem klasör yolunda anlık arama) ve **Boş Klasör ve 0 KB** sabit sekmeleri.
- **SHA-256 dosya karşılaştırma ekranı** — sağ tık menüsündeki **Karşılaştır…** veya çoklu seçim çubuğundaki **Karşılaştır** düğmesiyle açılır; dosyaların gerçek içeriklerini **SHA-256 parmak izi**, tam bayt boyutu, değiştirilme tarihi ve önizleme metniyle karşılaştırır (`%100 Birebir Aynı İçerik` veya `Dikkat: İçerikler Farklı`).
- **En eskiyi / en yeniyi koru kısayolları** — karşılaştırma ekranında **En eskiyi koru** (asıl dosyayı bırakır, diğerlerini seçer) ve **En yeniyi koru** tek tık düğmeleriyle seçilenleri doğrudan panoya alma veya silme.
- **Dosya konumunu aç (Klasörde göster)** — tüm dosya satırlarında, çift gruplarında, boş klasörlerde, 0 KB dosyalarda ve karşılaştırma ekranında sağ tık → **Dosya konumunu aç (Klasörde göster)** ve **Kayıtlı olduğu klasörü aç** seçenekleri.

[1.4.0]: https://github.com/ruhitemel/ikiz/releases/tag/v1.4.0

## [1.3.0] - 2026-11-19

### Eklendi

- **Boş klasör ve 0 KB dosya tarama ve onaylı temizleme** — tarama sırasında ek disk okuması yapmadan içeriksiz boş klasörleri (`KLS`) ve `0 KB` boyutundaki dosyaları tespit eder.
- **Boş klasör ve 0 KB görünümü** — filtre çubuğundaki **Boş klasör ve 0 KB** anahtarı ve seçim çubuğundaki **Boş / 0 KB seç** kısayolu ile **Boş klasörler** ve **0 KB dosyalar** gruplarını anında listeler ve seçer.
- **Onaylı silme ve geri alma** — boş klasörler ve 0 KB dosyalar aynı silme panosu, onay kutusu, Geri Dönüşüm Kutusu (varsayılan) / kalıcı silme ve son silmeyi geri alma akışıyla temizlenir; silme anında klasörün gerçekten boş olduğu (TOCTOU) doğrulanır.

[1.3.0]: https://github.com/ruhitemel/ikiz/releases/tag/v1.3.0

## [1.2.2] - 2026-11-19

### Güvenlik

- **Çalışma zamanı bütünlük ve kurcalama koruması (`RuntimeGuard`)** — güncelleme deposu URL'si ve geliştirici/depo bilgileri SHA-256 özetiyle sabitlendi (hex-edit ile sahte güncelleme sunucusuna yönlendirme engellendi); DLL side-loading kontrolü ve Release derlemesinde hata ayıklayıcı / CLR profiler / `DOTNET_STARTUP_HOOKS` enjeksiyon koruması eklendi.
- **ReadyToRun ve deterministik derleme** — yayın paketleri yerel x64 makine koduna (`PublishReadyToRun`) ve deterministik (`PathMap`) olarak derlenir.
- **Dosya işlemi (TOCTOU) sertleştirmesi** — silme ve yeniden adlandırma anında sembolik bağlantı (`ReparsePoint`), sistem dosyası (`System`), klasör dışına kaçış ve belge/medya beyaz listesi tekrar doğrulanır.
- **URL ve günlük güvenliği** — dış bağlantılar yalnızca `https://github.com/ruhitemel/ikiz` adresine izin verir; hata günlüğü kullanıcı adını ve profil dizinini otomatik maskeler ve 512 KB ile sınırlandırılır.

[1.2.2]: https://github.com/ruhitemel/ikiz/releases/tag/v1.2.2

## [1.2.1] - 2026-11-19

### Değiştirildi

- **Kurulum sihirbazı** — artık `.msi` paketi de yayınlanıyor; alışılmış **İleri → İleri → Kurulum → Bitti** akışıyla kurulur. Hızlı tek tık kurulum için `ikiz-win-Setup.exe` korundu.
- **Sürüm notları** — GitHub Releases gövdesi yalnızca ilgili sürümün notlarını içerir; tüm değişiklik günlüğü artık eklenmez.
- **Kullanım rehberi** — kurulum paketindeki rehber ve Windows README'sindeki **Kullanım rehberi** sade bir dille yeniden yazıldı.

### Güvenlik

- Yayın paketinde hata ayıklama simgeleri (`.pdb`) ve yerel derleme yolları bulunmaz; paket yalnızca uygulama dosyalarını içerir. Yayın iş akışı kişisel yol sızıntısını denetler.
- Geliştirici bilgisi uygulamada yalnızca **Hakkında** bölümünde gösterilir.

[1.2.1]: https://github.com/ruhitemel/ikiz/releases/tag/v1.2.1

## [1.2.0] - 2026-11-19

### Eklendi

- **Panodakileri gizle** — silme panosuna alınan dosyalar sonuç listesinden çıkarılır; dosya sayısı, tür çipleri, çift grubu sayısı ve kazanılabilir alan yeniden hesaplanır. Panodan çıkarınca geri gelir. Filtre çubuğundaki anahtarla kapatılabilir (varsayılan açık).
- **Ana ekranda çoklu seçim** — her satırda seçim kutusu; **Tümünü seç** ve **Kopyaları seç** (her gruptan bir kopya korunur, fazlalıklar işaretlenir) kısayolları.
- **Seçili dosyalarla toplu işlem** — **Panoya al**, **Sil** (onaylı, Geri Dönüşüm Kutusu varsayılan) ve **Adlandır** (toplu yeniden adlandırma); silme/adlandırma panellerinde "Seçilenler" kapsamı.

[1.2.0]: https://github.com/ruhitemel/ikiz/releases/tag/v1.2.0

## [1.1.1] - 2026-11-19

### Düzeltildi

- Paylaşım denetimi özetindeki " yerel yol içermez" ifadesiyle çelişen maskelenmiş yol örneği kaldırıldı; yalnızca kök sayısı gösteriliyor.
- **Hakkında** ve **Paylaşım denetimi** panellerinin arkası opak hale getirildi; ekran görüntüsünde arka plandaki dosya adları ve yollar görünmüyor.

[1.1.1]: https://github.com/ruhitemel/ikiz/releases/tag/v1.1.1

## [1.1.0] - 2026-11-19

### Eklendi

- **Canlı tarama ilerlemesi** — arka planda disk taranırken ilerleme çubuğu (animasyonlu), taranan dosya sayısı, geçen süre, anlık hız ve tahmini kalan süre. İlk taramada toplam bilinmediği için belirsiz animasyon ve canlı sayaç gösterilir.
- **Paylaşım öncesi gizlilik denetimi** — **Hakkında → Paylaşım denetimi** ile e-posta, IP, kişisel yol, kullanıcı adı, günlük, gizli anahtar ve veri aktarımı maddelerini tek tek kontrol eder; kişisel bilgi içermeyen özeti panoya kopyalar.
- **Geliştirici ve lisans vurgusu** — Hakkında bölümünde `Ruhi Temel (@ruhitemel)` ve "Açık kaynaklıdır, herkesin kullanımına açıktır." notu.

### Değiştirildi

- **Panoya al** artık silme panosunu otomatik açmıyor; yalnızca kısa bildirim ve sayı güncelleniyor. Panel isteğe bağlı olarak başlıktaki **Panoyu göster** düğmesiyle açılıyor.

[1.1.0]: https://github.com/ruhitemel/ikiz/releases/tag/v1.1.0

## [1.0.0] - 2026-11-19

### Eklendi

- Windows (WPF, .NET 8) uygulaması: anında Türkçe duyarsız arama, belge/medya filtresi, çift kopya gruplama ve sanallaştırılmış sonuç listesi.
- Silme panosu, önizleme listesi ve açık onay ile Geri Dönüşüm Kutusu / kalıcı silme.
- Sağ tık menüsü: panoya ekle/çıkar, Adı düzenle, toplu yeniden adlandırma, yalnızca bu dosyayı sil, aslı koruyarak diğer kopyaları sil, tüm kopyaları sil, son silmeyi geri al.
- Açık/koyu tema, kalıcı tema seçimi, klavye kısayolları ve ekran okuyucu etiketleri.
- **Konum ekle** paneli: tüm disk harfleri (birim adı, türü ve boş alanıyla), çoklu sürücü seçimi, klasör seçimi ve mevcut kökleri kaldırma.
- Uygulama simgesi, sürüm bilgisi, **Hakkında** bölümü ve geliştirici notu.
- Velopack tabanlı otomatik güncelleme altyapısı: GitHub Releases denetimi, indirme ve yeniden başlatma.
- GitHub Actions ile sürekli derleme/test ve sürüm etiketinde (`v*`) paketleme/yayınlama.
- Açık kaynak belgeleri: README, lisans, katkı, davranış kuralları ve güvenlik politikası.

[1.0.0]: https://github.com/ruhitemel/ikiz/releases/tag/v1.0.0
