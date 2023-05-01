using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using MelonLoader.Installer.Models;
using MelonLoader.Installer.Services;
using MelonLoader.Installer.ViewModels;
using MelonLoader.Installer.Views;

namespace MelonLoader.Installer
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var releases = new MelonReleasesService();
                var settings = new Settings();
                desktop.MainWindow = new MainWindow
                {
                    DataContext = new MainWindowViewModel(settings, releases),
                };
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
