# Yapılacaklar

Kullanıcı geri bildirimlerinin işlenme sırası. Tamamlanan maddeler işaretlenir.

1. [x] **Tarama ilerlemesi** — arka planda disk taranırken görsel animasyon, canlı dosya sayısı, geçen süre, hız ve tahmini kalan süre göster.
2. [x] **Geliştirici notu** — Hakkında bölümünde ad + "@ruhitemel" ve "açık kaynak, herkesin kullanımına açık" vurgusu.
3. [x] **Paylaşım öncesi gizlilik denetimi** — kişisel dosya adı / kişi adı / yerel yol / e-posta / IP içermediğini madde madde özetleyen panel ve kopyalanabilir temiz özet.
4. [x] **Panoya al davranışı** — pano otomatik açılmıyor; yalnızca sayı bildirimi güncelleniyor. Başlıktaki **Panoyu göster** düğmesiyle isteyince açılıyor.
5. [x] **Testler, UIA doğrulaması, commit ve yayın** — 51/51 birim testi, 22 UIA kontrolü geçti; derleme 0 uyarı / 0 hata. `v1.1.0` yayınlandı; opak arka plan ve özet düzeltmesi `v1.1.1` ile yayınlandı.
6. [x] **Pano listesini kısaltma ve hızlı çoklu seçim** — panodakiler aramadan gizleniyor (anahtarla kapatılabilir); satır seçimi, Tümünü seç / Kopyaları seç ve seçili dosyalar için pano + sil + adlandır eklendi. `v1.2.0`.
7. [x] **Kurulum, güncelleme ve paket temizliği** — `.msi` sihirbazı eklendi; GitHub Releases gövdesi sürüm notlarıyla sınırlı; yayın paketinden PDB ve yerel yollar çıkarıldı; kullanım rehberi sadeleştirildi. `v1.2.1`.

## Doğrulama özeti

- Derleme: 0 uyarı, 0 hata.
- Birim testleri: 56 geçti, 0 başarısız.
- UIA: pano otomatik açılmıyor, "Panoyu göster" açıyor; geliştirici adı ve açık kaynak notu görünüyor; gizlilik denetiminin 9 maddesi mevcut; Esc paneli kapatıyor; hata kaydı yok.
- Tarama sırasında canlı detay, geçen süre ve kalan tahmin UIA ile görüldü.
- Çoklu seçim UIA: Kopyaları seç 19 dosyayı işaretledi; panoya alınca liste 31 dosya / 12 gruptan 12 dosya / 0 gruba indi; pano temizlenince geri geldi. Silme ve adlandırma panelleri "Seçilenler (19)" kapsamıyla açıldı (25 kontrol geçti).
- Yayın denetimi: `publish` klasöründe `.pdb` yok; hiçbir dosyada `userntw`, `Default Project` veya `C:\Users\` geçmiyor. `vpk pack --msi` ile MSI sihirbaz paketi üretiliyor.
