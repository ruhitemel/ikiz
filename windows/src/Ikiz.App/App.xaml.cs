using System.IO;
using System.Windows;
using Ikiz.App.Services;
using Velopack;

namespace Ikiz.App;

public partial class App : Application
{
    public static string? StartupFolder { get; private set; }

    [STAThread]
    public static void Main(string[] args)
    {
        RuntimeGuard.Enforce();

        // Velopack kurulum/güncelleme kancalarını WPF başlamadan önce işler.
        VelopackApp.Build().Run();

        var app = new App();
        app.InitializeComponent();
        app.Run();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        ThemeManager.Initialize();
        base.OnStartup(e);
        if (e.Args.Length > 0 && Directory.Exists(e.Args[0]))
            StartupFolder = Path.GetFullPath(e.Args[0]);

        DispatcherUnhandledException += (_, args) => AppLog.Write("dispatcher", args.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            if (args.ExceptionObject is Exception ex) AppLog.Write("domain", ex);
        };
    }
}
