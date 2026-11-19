# Güvenlik politikası

İkiz, dosyalarını yerel olarak işler. Güvenliğini ciddiye alıyoruz.

## Desteklenen sürümler

| Sürüm | Destek |
| --- | --- |
| 1.2.x | Evet |
| 1.1.x | Güvenlik düzeltmeleri |
| 1.0.x | Hayır (1.2.x sürümüne güncelle) |

## Bir açık bulursan

Güvenlik açıklarını **herkese açık Issue olarak bildirme**. Bunun yerine GitHub üzerinden proje sorumlusuna özel mesaj gönder veya depo ayarlarındaki *Security → Report a vulnerability* bölümünü kullan.

Lütfen şunları ekle:

- etkilenen sürüm,
- adımlar veya minimal örnek,
- olası etki,
- varsa düzeltme önerisi.

Bildirimleri 7 gün içinde onaylamaya, çözüm için makul bir süre içinde sürüm yayınlamaya çalışırız. Düzeltme yayınlandıktan sonra bildiren kişinin adı isteği üzerine anılır.

## Güvenlik modeli

- Uygulama arama ve silme işlemlerini yalnızca bu cihazda yapar; dosya içeriklerini veya yollarını göndermez.
- Güncelleme denetimi yalnızca sabitlenmiş (SHA-256 ile doğrulanan) `https://github.com/ruhitemel/ikiz` deposunun Releases verisini okur; indirilen paketler Velopack tarafından doğrulanır.
- Çalışma zamanında derleme bütünlüğü, aynı dizin doğrulaması (DLL side-loading engeli) ve Release sürümünde hata ayıklayıcı / bellek kancası (`DOTNET_STARTUP_HOOKS`, profiler) koruması (`RuntimeGuard`) uygulanır.
- Yayın ikilileri ReadyToRun (yerel x64 makine kodu) ve deterministik olarak derlenir; `.pdb` simgeleri ve yerel derleme yolları içermez.
- Sembolik bağlantılar (`ReparsePoint`), sistem dosyaları (`System`) ve belge/medya beyaz listesi dışındaki uzantılar hem tarama hem de silme/yeniden adlandırma anında (TOCTOU koruması) reddedilir.
- Hata günlüğü (`%LOCALAPPDATA%\Ikiz\ikiz-error.log`) kullanıcı adını ve profil yolunu otomatik maskeler; 512 KB sınırını aşmaz.
- Onay olmadan hiçbir dosya işlemi yapılmaz; pano yalnızca oturum belleğinde tutulur.
- Varsayılan silme yöntemi Geri Dönüşüm Kutusu'dur; kalıcı silme ayrı onay ister.
- Repo, gizli anahtar veya erişim belirteci içermez; yayın iş akışında GitHub'ın geçici `GITHUB_TOKEN` değeri kullanılır.

## Kendi kullanımında

- Uygulamayı yalnızca [Releases](https://github.com/ruhitemel/ikiz/releases) sayfasından veya kaynak kodundan derleyerek edin.
- Yedekleme olmayan kritik klasörlerde toplu silme yapmadan önce önizlemeyi dikkatlice kontrol et.
- Bir güncelleme beklenmedik davranıyorsa sürüm notlarını oku ve geri bildirim gönder.
