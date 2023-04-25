using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using MelonLoader.Models;
using MelonLoader.Services;
using MelonLoader.ViewModels;
using MelonLoader.Views;

namespace MelonLoader
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
