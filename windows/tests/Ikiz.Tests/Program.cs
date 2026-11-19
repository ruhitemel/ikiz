using Ikiz.Core;

var failures = new List<string>();
var passed = 0;

void Check(string name, bool ok, string? detail = null)
{
    if (ok)
    {
        passed++;
        Console.WriteLine($"  ok   {name}");
    }
    else
    {
        failures.Add(detail is null ? name : $"{name} — {detail}");
        Console.WriteLine($"  FAIL {name}{(detail is null ? "" : " — " + detail)}");
    }
}

Console.WriteLine("Metin katlama");
Check("İSTANBUL -> istanbul", TextNormalizer.Fold("İSTANBUL") == "istanbul", TextNormalizer.Fold("İSTANBUL"));
Check("Ağrı -> agri", TextNormalizer.Fold("Ağrı") == "agri", TextNormalizer.Fold("Ağrı"));
Check("ŞÖYLE -> soyle", TextNormalizer.Fold("ŞÖYLE") == "soyle", TextNormalizer.Fold("ŞÖYLE"));
Check("boşluk tokenize", TextNormalizer.Tokenize("  Ahmet   Özgeçmiş ").SequenceEqual(new[] { "ahmet", "ozgecmis" }));

var ranges = TextNormalizer.FindRanges("Ahmet_Özgeçmiş.pdf", TextNormalizer.Tokenize("ahm ozg"));
Check("vurgu aralığı", ranges.Count == 2, string.Join(", ", ranges));

Console.WriteLine("Dosya türü beyaz listesi");
Check("pdf kullanıcı içeriği", FileCategories.IsUserContent("pdf"));
Check("png kullanıcı içeriği", FileCategories.IsUserContent("png"));
Check("mp4 kullanıcı içeriği", FileCategories.IsUserContent("mp4"));
Check("mp3 kullanıcı içeriği", FileCategories.IsUserContent("mp3"));
Check("exe listelenmez", !FileCategories.IsUserContent("exe"));
Check("dll listelenmez", !FileCategories.IsUserContent("dll"));
Check("tmp listelenmez", !FileCategories.IsUserContent("tmp"));
Check("pdf kategorisi Belge", FileCategories.CategoryOf("pdf") == "Belge");

Console.WriteLine("Tarama filtresi (geçici klasör)");
var scanDir = Path.Combine(Path.GetTempPath(), "ikiz-scan-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(scanDir);
try
{
    var pdfPath = Path.Combine(scanDir, "belge.pdf");
    var exePath = Path.Combine(scanDir, "kurulum.exe");
    var hiddenPath = Path.Combine(scanDir, "gizli.txt");
    File.WriteAllText(pdfPath, "x");
    File.WriteAllText(exePath, "x");
    File.WriteAllText(hiddenPath, "x");
    File.SetAttributes(hiddenPath, FileAttributes.Hidden);

    var scanned = FolderScanner.Scan(new[] { scanDir }, null, CancellationToken.None);
    Check("tarama yalnızca belgeyi alır", scanned.Count == 1, "bulunan: " + scanned.Count);
    Check("elenen exe ve gizli dosya", scanned.Count == 1 && scanned[0].Extension == "pdf");
}
finally
{
    try { Directory.Delete(scanDir, true); } catch { }
}

Console.WriteLine("Çift anahtarı");
var a = new FileItem { Id = 1, FullPath = @"C:\a\Ahmet.PDF", Name = "Ahmet.PDF", Directory = @"C:\a", Extension = "pdf", Size = 10 };
var b = new FileItem { Id = 2, FullPath = @"C:\b\ahmet.pdf", Name = "ahmet.pdf", Directory = @"C:\b", Extension = "pdf", Size = 10 };
var c = new FileItem { Id = 3, FullPath = @"C:\c\ahmet.pdf", Name = "ahmet.pdf", Directory = @"C:\c", Extension = "pdf", Size = 11 };
Check("aynı ad+boyut+tür aynı anahtar", a.DuplicateKey == b.DuplicateKey);
Check("farklı boyut farklı anahtar", a.DuplicateKey != c.DuplicateKey);

Console.WriteLine("İndeks araması");
var index = new FileIndex();
index.ReplaceAll(new List<FileCandidate>
{
    new(a.FullPath, a.Name, a.Directory, a.Extension, a.Size, DateTime.UtcNow),
    new(b.FullPath, b.Name, b.Directory, b.Extension, b.Size, DateTime.UtcNow),
    new("C:\\c\\baska.pdf", "baska.pdf", "C:\\c", "pdf", 99, DateTime.UtcNow),
    new("C:\\d\\fatura_ocak.pdf", "fatura_ocak.pdf", "C:\\d", "pdf", 20, DateTime.UtcNow),
    new("C:\\e\\fatura_ocak.pdf", "fatura_ocak.pdf", "C:\\e", "pdf", 20, DateTime.UtcNow),
});

var result = index.Search(new SearchRequest { Text = "ahmet", OnlyDuplicates = true });
Check("ahmet araması 1 çift grup", result.Groups.Count == 1, result.Groups.Count.ToString());
Check("ahmet grubunda 2 kopya", result.Groups.Count == 1 && result.Groups[0].Count == 2);
Check("kazanılabilir alan 10 B", result.TotalWaste == 10, result.TotalWaste.ToString());
Check("tümünde 2 çift grup", index.Search(new SearchRequest()).DuplicateGroupCount == 2);

var pathSearch = index.Search(new SearchRequest { Text = "c:\\d", SearchPath = true });
Check("yol araması bulur", pathSearch.MatchedCount == 1, pathSearch.MatchedCount.ToString());
Check("yol araması kapalıyken bulmaz", index.Search(new SearchRequest { Text = "c:\\d", SearchPath = false }).MatchedCount == 0);

Console.WriteLine("Panodakileri gizleme");
var hiddenOne = index.Search(new SearchRequest { HiddenIds = new HashSet<int> { 2 } });
Check("gizlenen kopya eşleşme sayısını düşürür", hiddenOne.MatchedCount == 4, hiddenOne.MatchedCount.ToString());
Check("tek kopya kalan grup listelenmez", hiddenOne.DuplicateGroupCount == 1, hiddenOne.DuplicateGroupCount.ToString());
Check("tür sayımı gizleneni hariç tutar", hiddenOne.TypeCounts["pdf"] == 4, hiddenOne.TypeCounts["pdf"].ToString());
var hiddenMost = index.Search(new SearchRequest { HiddenIds = new HashSet<int> { 2, 4 } });
Check("tüm çiftler gizlenince grup kalmaz", hiddenMost.DuplicateGroupCount == 0, hiddenMost.DuplicateGroupCount.ToString());
Check("boş gizleme listesi eski haline getirir",
    index.Search(new SearchRequest { HiddenIds = new HashSet<int>() }).DuplicateGroupCount == 2);

Console.WriteLine("Silme panosu");
var board = new DeleteBoard();
board.Stage(index.GetByKey(a.DuplicateKey), keepFirstIfNew: true);
Check("grup panoya alındı", board.Count == 2);
Check("bir kopya korunuyor", board.SelectedCount == 1, board.SelectedCount.ToString());
Check("onaysız plan boş", board.Plan().Count == 0);
board.Approve(true);
Check("onaylı plan 1 dosya", board.Plan().Count == 1);
board.SetSelected(b.Id, true);
Check("seçim değişince onay sıfır", !board.Approved && board.Plan().Count == 0);
board.SelectAll();
Check("tümünü seç", board.SelectedCount == 2);
board.SelectNone();
Check("hiçbirini seçme", board.SelectedCount == 0);
board.Clear();
Check("pano temizlendi", board.Count == 0 && board.Mode == DeleteMode.RecycleBin);

Console.WriteLine("Yeniden adlandırma");
Check("boş ad reddedilir", FileRenamer.ValidateBaseName("   ") is not null);
Check("geçersiz karakter reddedilir", FileRenamer.ValidateBaseName("a:b") is not null);
Check("ayrılmış ad reddedilir", FileRenamer.ValidateBaseName("CON") is not null);
Check("geçerli ad kabul edilir", FileRenamer.ValidateBaseName("yeni_ad") is null);
Check("uzantısız tam ad reddedilir", FileRenamer.ValidateFullName("uzantisiz") is not null);
Check("geçerli tam ad kabul edilir", FileRenamer.ValidateFullName("rapor.pdf") is null);

var renameA = new FileItem { Id = 201, FullPath = @"C:\r1\rapor.pdf", Name = "rapor.pdf", Directory = @"C:\r1", Extension = "pdf", Size = 10 };
var renameB = new FileItem { Id = 202, FullPath = @"C:\r2\rapor.pdf", Name = "rapor.pdf", Directory = @"C:\r2", Extension = "pdf", Size = 10 };
var renamePlan = FileRenamer.BuildPlan(new[] { renameA, renameB }, "yeni_ad", keepExtension: true);
Check("iki kopya için plan", renamePlan.Count == 2, renamePlan.Count.ToString());
Check("uzantı korunur", renamePlan.All(p => p.NewFullPath.EndsWith(".pdf")), string.Join(", ", renamePlan.Select(p => p.NewFullPath)));

var renameDir = Path.Combine(Path.GetTempPath(), "ikiz-rename-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(renameDir);
try
{
    var existing = Path.Combine(renameDir, "mevcut.pdf");
    var first = Path.Combine(renameDir, "ilk.pdf");
    var second = Path.Combine(renameDir, "ikinci.pdf");
    File.WriteAllText(existing, "x");
    File.WriteAllText(first, "bir");
    File.WriteAllText(second, "iki");

    var existingItem = new FileItem { Id = 211, FullPath = existing, Name = "mevcut.pdf", Directory = renameDir, Extension = "pdf", Size = 1 };
    var firstItem = new FileItem { Id = 212, FullPath = first, Name = "ilk.pdf", Directory = renameDir, Extension = "pdf", Size = 3 };
    var secondItem = new FileItem { Id = 213, FullPath = second, Name = "ikinci.pdf", Directory = renameDir, Extension = "pdf", Size = 3 };

    var collision = FileRenamer.BuildPlan(new[] { firstItem }, "mevcut", keepExtension: true);
    Check("çakışan ad (2) alır", collision.Count == 1 && Path.GetFileName(collision[0].NewFullPath) == "mevcut (2).pdf",
        string.Join(", ", collision.Select(p => p.NewFullPath)));

    var sameDir = FileRenamer.BuildPlan(new[] { firstItem, secondItem }, "birlesik", keepExtension: true);
    var applied = FileRenamer.Apply(sameDir);
    Check("aynı klasörde iki dosya yeniden adlandı", applied.SuccessCount == 2, applied.FailedCount.ToString());
    Check("ilk hedef birlesik.pdf", File.Exists(Path.Combine(renameDir, "birlesik.pdf")));
    Check("ikinci hedef birlesik (2).pdf", File.Exists(Path.Combine(renameDir, "birlesik (2).pdf")));
    Check("eski adlar kalmadı", !File.Exists(first) && !File.Exists(second));

    var caseFile = Path.Combine(renameDir, "Buyuk.TXT");
    File.WriteAllText(caseFile, "buyuk");
    var caseItem = new FileItem { Id = 214, FullPath = caseFile, Name = "Buyuk.TXT", Directory = renameDir, Extension = "txt", Size = 5 };
    var caseOutcome = FileRenamer.Apply(FileRenamer.BuildPlan(new[] { caseItem }, "buyuk", keepExtension: true));
    Check("yalnızca harf büyüklüğü değişimi", caseOutcome.SuccessCount == 1 && File.Exists(Path.Combine(renameDir, "buyuk.txt")),
        string.Join("; ", caseOutcome.Results.Select(r => r.Error)));

    var idx = new FileIndex();
    idx.ReplaceAll(new List<FileCandidate>
    {
        new(firstItem.FullPath, "ayni.pdf", firstItem.Directory, "pdf", 3, DateTime.UtcNow),
        new(secondItem.FullPath, "ayni.pdf", secondItem.Directory, "pdf", 3, DateTime.UtcNow),
    });
    var oldKey = idx.Search(new SearchRequest()).Groups[0].Key;
    var idToRename = idx.GetByKey(oldKey)[0].Id;
    var targetPath = Path.Combine(renameDir, "tek.pdf");
    var updated = idx.ApplyRenames(new Dictionary<int, string> { [idToRename] = targetPath });
    Check("indeks yeniden adlandırmayı uygular", updated.Count == 1 && updated[0].Name == "tek.pdf" && updated[0].Extension == "pdf");
    Check("eski anahtar bir kopyada kaldı", idx.AliveCountForKey(oldKey) == 1, idx.AliveCountForKey(oldKey).ToString());
    Check("yeni anahtar tek kopya", idx.AliveCountForKey(updated[0].DuplicateKey) == 1);
    Check("çift grup dağıldı", idx.Search(new SearchRequest()).DuplicateGroupCount == 0);
}
finally
{
    try { Directory.Delete(renameDir, true); } catch { }
}

Console.WriteLine("Gerçek silme (geçici klasör)");
var temp = Path.Combine(Path.GetTempPath(), "ikiz-test-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(temp);
try
{
    var p1 = Path.Combine(temp, "kopya.txt");
    var p2 = Path.Combine(temp, "kopya2.txt");
    var p3 = Path.Combine(temp, "cop.txt");
    File.WriteAllText(p1, "bir");
    File.WriteAllText(p2, "iki");
    File.WriteAllText(p3, "uc");

    var item1 = new FileItem { Id = 101, FullPath = p1, Name = "kopya.txt", Directory = temp, Extension = "txt", Size = 3 };
    var item2 = new FileItem { Id = 102, FullPath = p2, Name = "kopya2.txt", Directory = temp, Extension = "txt", Size = 3 };
    var item3 = new FileItem { Id = 103, FullPath = p3, Name = "cop.txt", Directory = temp, Extension = "txt", Size = 2 };

    var permanent = FileDeleter.Delete(new[] { item1, item2 }, DeleteMode.Permanent);
    Check("kalıcı silme 2 dosya", permanent.Deleted.Count == 2 && !File.Exists(p1) && !File.Exists(p2));

    if (OperatingSystem.IsWindows())
    {
        var recycle = FileDeleter.Delete(new[] { item3 }, DeleteMode.RecycleBin);
        Check("geri dönüşüm kutusu", recycle.Deleted.Count == 1 && !File.Exists(p3),
            string.Join("; ", recycle.Failed.Select(f => f.Error)));
    }
}
finally
{
    try { Directory.Delete(temp, true); } catch { }
}

Console.WriteLine();
Console.WriteLine($"{passed} test geçti, {failures.Count} başarısız.");
if (failures.Count > 0)
{
    foreach (var f in failures) Console.WriteLine(" - " + f);
    return 1;
}

return 0;
