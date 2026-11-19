# Katkı rehberi

İkiz'e katkı sağlamak için teşekkürler. Küçük veya büyük fark etmez; her katkı değerlidir.

## Başlamadan önce

- Windows 10/11 (x64) ve [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) yüklü olsun.
- Depoyu çatalla (fork), kendi çatalına klonla ve `main` üzerinden bir dal aç:

```powershell
git switch -c ozellik/aciklama
```

## Geliştirme

```powershell
dotnet build windows/Ikiz.slnx
dotnet run --project windows/tests/Ikiz.Tests/Ikiz.Tests.csproj
dotnet run --project windows/src/Ikiz.App
```

Gerçek dosyalarla denemek için `windows/demo/olustur.ps1` betiği `%TEMP%\ikiz-demo` altında örnek veri oluşturur. **Kişisel klasörlerde deneme yaparken dosyaların yedekli olduğundan emin ol.**

## Kurallar

- Tüm iletişim ve arayüz metinleri Türkçedir; kod yorumları Türkçe veya sade İngilizce olabilir.
- Dosya silme akışında **onay olmadan işlem yapılmaz** kuralını bozma. Yeni bir işlem ekliyorsan önizleme ve onay adımlarını da ekle.
- Varsayılan silme yöntemi Geri Dönüşüm Kutusu'dur. Kalıcı silme her zaman ayrı ve belirgin bir seçenektir.
- Arayüz renkleri `DynamicResource` ile bağlanır; yeni renkleri hem `Theme/Dark.xaml` hem `Theme/Light.xaml` dosyalarına aynı anahtarla ekle.
- Ekran okuyucu kullanıcıları için yeni denetimlere `AutomationProperties.Name` ver.
- Yeni davranışlar için `tests/Ikiz.Tests` içinde doğrulama ekle.
- Gerçek e-posta adresi, kullanıcı adı, yerel disk yolu veya özel anahtar gönderme. Geliştirici kimliği yalnızca uygulamanın *Hakkında* bölümünde yer alır.

## Değişiklik gönderme

1. Değişikliklerini açıkça açıklayan commit'ler oluştur.
2. `main` dalını kendi çatalında güncel tut ve pull request aç.
3. PR açıklamasında neyi neden değiştirdiğini, nasıl test ettiğini yaz.
4. CI (derleme + test) yeşil olmadan birleştirme yapılmaz.

Sorular için Issue açabilirsin; güvenlik açıkları için [SECURITY.md](SECURITY.md) içindeki yöntemi kullan.
