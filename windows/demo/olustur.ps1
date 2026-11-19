# İkiz demo verisi
#
# Gerçek dosyalarina dokunmaz. Yalnizca %TEMP%\ikiz-demo altinda
# ayni ad + ayni boyut + ayni turde ornek dosyalar olusturur.
#
#   powershell -ExecutionPolicy Bypass -File demo\olustur.ps1
#   powershell -ExecutionPolicy Bypass -File demo\olustur.ps1 -Temizle
#
# Sonra uygulamayi bu klasorle ac:
#   dotnet run --project src\Ikiz.App -- "%TEMP%\ikiz-demo"

param(
    [string]$Target = (Join-Path $env:TEMP 'ikiz-demo'),
    [switch]$Temizle
)

$ErrorActionPreference = 'Stop'

if ($Temizle) {
    if (Test-Path $Target) {
        $resolved = (Resolve-Path $Target).Path
        $temp = [IO.Path]::GetFullPath($env:TEMP).TrimEnd('\')
        if (-not $resolved.StartsWith($temp, [StringComparison]::OrdinalIgnoreCase)) {
            throw "Guvenlik: yalnizca %TEMP% altindaki klasor silinebilir."
        }
        Remove-Item $Target -Recurse -Force
        Write-Host "Temizlendi: $resolved"
    }
    return
}

$groups = @(
    @{ Name = 'ahmet_ozgecmis.pdf';              Size = 2516;  Copies = @('Desktop', 'Downloads', 'Yedek') },
    @{ Name = 'ahmet_imza.png';                  Size = 1536;  Copies = @('Desktop', 'Downloads', 'Pictures\2026', 'Yedek') },
    @{ Name = 'ahmet_notlar.txt';                Size = 900;   Copies = @('Desktop', 'Documents') },
    @{ Name = 'logo_final.png';                  Size = 4096;  Copies = @('Desktop', 'Pictures\2026', 'Yedek') },
    @{ Name = 'ahmet_sunum.pptx';                Size = 5120;  Copies = @('Downloads', 'Documents', 'Yedek') },
    @{ Name = 'fatura_ocak.pdf';                 Size = 1200;  Copies = @('Downloads', 'Documents') },
    @{ Name = 'butce_2026.xlsx';                 Size = 6144;  Copies = @('Downloads', 'Yedek') },
    @{ Name = 'Ahmet_Proje_Raporu_Final.docx';   Size = 3072;  Copies = @('Documents', 'Desktop', 'Yedek') },
    @{ Name = 'ahmet-fotograf.jpg';              Size = 4500;  Copies = @('Pictures\2026', 'Desktop', 'Yedek') },
    @{ Name = 'ahmet_muzik.mp3';                 Size = 7168;  Copies = @('Downloads', 'Yedek') },
    @{ Name = 'ahmet_video.mp4';                 Size = 8192;  Copies = @('Pictures\2026', 'Yedek') },
    @{ Name = 'ahmet_veri.csv';                  Size = 1100;  Copies = @('Documents', 'Downloads') }
)

$created = 0
foreach ($group in $groups) {
    $content = ('x' * $group.Size)
    foreach ($folder in $group.Copies) {
        $dir = Join-Path $Target $folder
        New-Item -ItemType Directory -Force -Path $dir | Out-Null
        $path = Join-Path $dir $group.Name
        [IO.File]::WriteAllText($path, $content)
        $created++
    }
}

Write-Host "Olusturuldu: $created dosya"
Write-Host "Klasor    : $Target"
Write-Host "Ac        : dotnet run --project src\Ikiz.App -- `"$Target`""
