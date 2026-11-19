using System.Diagnostics;
using System.Windows.Threading;
using Ikiz.Core;

namespace Ikiz.App.ViewModels;

public sealed partial class MainViewModel
{
    private readonly DispatcherTimer _scanElapsedTimer = new() { Interval = TimeSpan.FromMilliseconds(250) };
    private readonly Stopwatch _scanStopwatch = new();
    private readonly Queue<(TimeSpan Time, int Files)> _scanSamples = new();
    private int _lastScanCount;
    private int _currentScanFiles;

    private double _scanPercent;
    public double ScanPercent
    {
        get => _scanPercent;
        private set => Set(ref _scanPercent, value);
    }

    private bool _scanIsIndeterminate = true;
    public bool ScanIsIndeterminate
    {
        get => _scanIsIndeterminate;
        private set => Set(ref _scanIsIndeterminate, value);
    }

    private string _scanElapsedText = "00:00";
    public string ScanElapsedText
    {
        get => _scanElapsedText;
        private set => Set(ref _scanElapsedText, value);
    }

    private string _scanEtaText = "klasörler geziliyor…";
    public string ScanEtaText
    {
        get => _scanEtaText;
        private set => Set(ref _scanEtaText, value);
    }

    private string _scanSpeedText = "—";
    public string ScanSpeedText
    {
        get => _scanSpeedText;
        private set => Set(ref _scanSpeedText, value);
    }

    private string _scanDetailText = "";
    public string ScanDetailText
    {
        get => _scanDetailText;
        private set => Set(ref _scanDetailText, value);
    }

    internal void RememberScanCount(int count) => _lastScanCount = count;

    internal void StartScanProgress()
    {
        _scanSamples.Clear();
        _currentScanFiles = 0;
        _scanStopwatch.Restart();
        ScanPercent = 0;
        ScanIsIndeterminate = _lastScanCount == 0;
        ScanElapsedText = "00:00";
        ScanSpeedText = "—";
        ScanEtaText = _lastScanCount == 0
            ? "ilk tarama · toplam bilinmiyor"
            : "tahmin hesaplanıyor…";
        ScanDetailText = _lastScanCount == 0
            ? "İlk taramada toplam dosya sayısı bilinmez; canlı hız ve süre gösterilir."
            : "Önceki taramaya göre kalan süre tahmin edilir.";
        UpdateScanTiming();
        _scanElapsedTimer.Start();
    }

    internal void StopScanProgress()
    {
        _scanElapsedTimer.Stop();
        _scanStopwatch.Stop();
        ScanDetailText = "";
        ScanEtaText = "";
        ScanSpeedText = "";
        ScanIsIndeterminate = true;
        ScanPercent = 0;
        RaiseScan();
    }

    internal void ReportScanProgress(ScanProgress progress)
    {
        _currentScanFiles = progress.Files;
        _scanSamples.Enqueue((_scanStopwatch.Elapsed, progress.Files));
        UpdateScanTiming();
    }

    private void UpdateScanTiming()
    {
        if (!IsScanning) return;

        var elapsed = _scanStopwatch.Elapsed;
        ScanElapsedText = $"{(int)elapsed.TotalMinutes:00}:{elapsed.Seconds:00}";

        while (_scanSamples.Count > 0 && elapsed - _scanSamples.Peek().Time > TimeSpan.FromSeconds(4))
            _scanSamples.Dequeue();

        double speed = 0;
        if (_scanSamples.Count >= 2)
        {
            var first = _scanSamples.Peek();
            var last = _scanSamples.Last();
            var seconds = (last.Time - first.Time).TotalSeconds;
            if (seconds > 0.25 && last.Files >= first.Files)
                speed = (last.Files - first.Files) / seconds;
        }

        if (speed > 0.5)
            ScanSpeedText = $"{HumanSize.Number((int)speed)} dosya/sn";

        if (_lastScanCount > 0)
        {
            ScanIsIndeterminate = false;
            ScanPercent = Math.Clamp(_currentScanFiles * 100.0 / _lastScanCount, 0, 99);
            var remaining = _lastScanCount - _currentScanFiles;
            if (speed > 0.5 && remaining > 0)
            {
                var seconds = remaining / speed;
                ScanEtaText = seconds < 60
                    ? $"≈{Math.Ceiling(seconds):0} sn kaldı"
                    : $"≈{Math.Ceiling(seconds / 60):0} dk kaldı";
            }
            else
            {
                ScanEtaText = "kalan süre hesaplanıyor…";
            }
        }
        else
        {
            ScanIsIndeterminate = true;
            ScanEtaText = speed > 0.5
                ? "ilk tarama · toplam bilinmiyor"
                : "klasörler geziliyor…";
        }

        ScanDetailText = $"{HumanSize.Number(_currentScanFiles)} dosya · {ScanElapsedText} · {ScanSpeedText} · {ScanEtaText}";
        ScanText = $"Taranıyor… {HumanSize.Number(_currentScanFiles)} dosya · {ScanElapsedText}";
        RaiseScan();
    }

    private void RaiseScan()
    {
        Raise(nameof(ScanPercent));
        Raise(nameof(ScanIsIndeterminate));
        Raise(nameof(ScanElapsedText));
        Raise(nameof(ScanEtaText));
        Raise(nameof(ScanSpeedText));
        Raise(nameof(ScanDetailText));
        Raise(nameof(ScanText));
    }
}
