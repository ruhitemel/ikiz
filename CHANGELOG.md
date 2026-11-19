# Değişiklik günlüğü

Bu dosya [Keep a Changelog](https://keepachangelog.com/tr/1.1.0/) biçimini izler. Sürüm numaraları [Semantic Versioning](https://semver.org/lang/tr/) kullanır.

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
