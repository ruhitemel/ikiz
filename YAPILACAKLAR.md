# Yapılacaklar

Kullanıcı geri bildirimlerinin işlenme sırası. Tamamlanan maddeler işaretlenir.

1. [x] **Tarama ilerlemesi** — arka planda disk taranırken görsel animasyon, canlı dosya sayısı, geçen süre, hız ve tahmini kalan süre göster.
2. [x] **Geliştirici notu** — Hakkında bölümünde ad + "@ruhitemel" ve "açık kaynak, herkesin kullanımına açık" vurgusu.
3. [x] **Paylaşım öncesi gizlilik denetimi** — kişisel dosya adı / kişi adı / yerel yol / e-posta / IP içermediğini madde madde özetleyen panel ve kopyalanabilir temiz özet.
4. [x] **Panoya al davranışı** — pano otomatik açılmıyor; yalnızca sayı bildirimi güncelleniyor. Başlıktaki **Panoyu göster** düğmesiyle isteyince açılıyor.
5. [x] **Testler, UIA doğrulaması, commit ve yayın** — 51/51 birim testi, 22 UIA kontrolü geçti; derleme 0 uyarı / 0 hata. `v1.1.0` yayınlandı; opak arka plan ve özet düzeltmesi `v1.1.1` ile yayınlandı.
6. [x] **Pano listesini kısaltma ve hızlı çoklu seçim** — panodakiler aramadan gizleniyor (anahtarla kapatılabilir); satır seçimi, Tümünü seç / Kopyaları seç ve seçili dosyalar için pano + sil + adlandır eklendi. `v1.2.0`.
7. [x] **Kurulum, güncelleme ve paket temizliği** — `.msi` sihirbazı eklendi; GitHub Releases gövdesi sürüm notlarıyla sınırlı; yayın paketinden PDB ve yerel yollar çıkarıldı; kullanım rehberi sadeleştirildi. `v1.2.1`.
8. [x] **Güvenlik ve tersine mühendislik sertleştirmesi** — `RuntimeGuard` (SHA-256 URL/bütünlük sabitleme, anti-debugger/profiler/startup-hook, DLL side-loading kontrolü), ReadyToRun yerel kod derlemesi, silme/adlandırma TOCTOU + `ReparsePoint` koruması, `OpenUrl` HTTPS kısıtı ve günlük maskeleme eklendi. `v1.2.2`.
9. [x] **Boş klasör ve 0 KB dosya tarama ve onaylı silme** — tek geçişte boş klasör ve 0 KB dosya tespiti, **Boş klasör ve 0 KB** filtresi, **Boş / 0 KB seç** hızlı seçimi, onaylı Geri Dönüşüm Kutusu / kalıcı silme ve geri alma desteği eklendi. `v1.3.0`.
10. [x] **3 sabit sekme, SHA-256 karşılaştırma ekranı ve klasörde göster** — **Çift Kopyalar / Hızlı Dosya Arama / Boş Klasör ve 0 KB** sabit sekmeleri, SHA-256 içerik parmak izi doğrulamalı **Karşılaştırma ekranı** (**En eskiyi koru / En yeniyi koru**) ve sağ tık **Dosya konumunu aç (Klasörde göster)** / **Kayıtlı olduğu klasörü aç** eklendi. `v1.4.0`.
11. [x] **Küçük güncelleme uyarı kartı (İndir ve kur / Ertele)** — yeni sürüm çıktığında açılışta ve açık kaldığı sürece periyodik olarak sağ altta beliren kompakt bildirim kartı ve Hakkında alt çubuk yerleşim düzeltmesi eklendi. `v1.4.1` / `v1.4.2`.

## Doğrulama özeti

- Derleme: 0 uyarı, 0 hata.
- Birim testleri: 67 geçti, 0 başarısız.
- UIA: pano otomatik açılmıyor, "Panoyu göster" açıyor; geliştirici adı ve açık kaynak notu görünüyor; gizlilik denetiminin 9 maddesi mevcut; Esc paneli kapatıyor; hata kaydı yok.
- Tarama sırasında canlı detay, geçen süre ve kalan tahmin UIA ile görüldü.
- Çoklu seçim UIA: Kopyaları seç 19 dosyayı işaretledi; panoya alınca liste 31 dosya / 12 gruptan 12 dosya / 0 gruba indi; pano temizlenince geri geldi. Silme ve adlandırma panelleri "Seçilenler (19)" kapsamıyla açıldı (25 kontrol geçti).
- Yayın denetimi: `publish` klasöründe `.pdb` yok; hiçbir dosyada `userntw`, `Default Project` veya `C:\Users\` geçmiyor. `vpk pack --msi` ile MSI sihirbaz paketi üretiliyor.
