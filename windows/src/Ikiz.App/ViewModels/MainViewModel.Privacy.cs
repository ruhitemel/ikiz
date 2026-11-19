using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using Ikiz.App.Services;

namespace Ikiz.App.ViewModels;

public sealed record PrivacyItemVm(string Status, string Title, string Detail, bool IsWarning);

public sealed partial class MainViewModel
{
    private bool _privacyOpen;
    public bool PrivacyOpen
    {
        get => _privacyOpen;
        private set => Set(ref _privacyOpen, value);
    }

    private bool _privacyHasWarning;
    public bool PrivacyHasWarning
    {
        get => _privacyHasWarning;
        private set => Set(ref _privacyHasWarning, value);
    }

    private string _privacySummary = "";
    public string PrivacySummary
    {
        get => _privacySummary;
        private set => Set(ref _privacySummary, value);
    }

    public ObservableCollection<PrivacyItemVm> PrivacyItems { get; } = new();

    public void OpenPrivacy()
    {
        RefreshPrivacyCheck();
        PrivacyOpen = true;
    }

    public void ClosePrivacy() => PrivacyOpen = false;

    public void RefreshPrivacyCheck()
    {
        var names = _index.Snapshot.Take(20_000).Select(f => f.Name).ToList();
        var findings = PrivacyCheckService.Check(_roots, names, AppPaths.LogFile, _index.AliveCount);

        PrivacyItems.Clear();
        foreach (var f in findings)
        {
            PrivacyItems.Add(new PrivacyItemVm(
                f.IsWarning ? "UYARI" : f.IsOk ? "OK" : "BİLGİ",
                f.Title,
                f.Detail,
                f.IsWarning));
        }

        PrivacyHasWarning = findings.Any(f => f.IsWarning);
        var lines = new List<string>
        {
            "İKİZ — PAYLAŞIM ÖNCESİ GİZLİLİK DENETİMİ",
            DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
            "",
        };
        lines.AddRange(findings.Select(f => $"[{ (f.IsWarning ? "UYARI" : f.IsOk ? "OK" : "BİLGİ") }] {f.Title}: {f.Detail}"));
        lines.Add("");
        lines.Add("Bu özet kişisel dosya adı, kullanıcı adı veya yerel yol içermez.");
        PrivacySummary = string.Join(Environment.NewLine, lines);
    }

    public void CopyPrivacySummary()
    {
        try
        {
            Clipboard.SetText(PrivacySummary);
            Toast("Denetim özeti panoya kopyalandı");
        }
        catch (Exception ex)
        {
            AppLog.Write("privacy-clipboard", ex);
            Toast("Özet kopyalanamadı", error: true);
        }
    }
}
