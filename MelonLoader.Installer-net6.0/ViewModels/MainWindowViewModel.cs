using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using DynamicData;
using DynamicData.Binding;
using MelonLoader.Installer.Models;
using MelonLoader.Installer.Services;
using Octokit;
using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;

namespace MelonLoader.Installer.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private bool _isAutomated = true;
        private bool _isUpdateAvailable = false;
        private bool _enableInstallButton;
        private string _manualZip = "";
        private Game _game = new();
        private Release _selectedRelease;
        private Settings _settings;
        private readonly ReadOnlyObservableCollection<Release> _releases;

        /// <summary>
        /// Try to enable Install button based on state of three variables:
        /// If UnityGameExe filled and this is automated installation, then activate button 
        /// OR
        /// If UnityGameExe filled and ManualZip filled, then activate button
        /// Otherwise deactivate button
        /// </summary>
        private void TryEnableInstallButton()
        {
            if ((!string.IsNullOrEmpty(UnityGameExe) && IsAutomated) || (!string.IsNullOrEmpty(UnityGameExe) && !string.IsNullOrEmpty(ManualZip)))
            {
                EnableInstallButton = true;
            }
            else
            {
                EnableInstallButton = false;
            }
        }

        private Func<Release, bool> MakeFilter(bool showPrereleases)
        {
            return release => !release.Prerelease || showPrereleases;
        }

        public MainWindowViewModel(Settings settings, MelonReleasesService service)
        {
            _settings = settings;

            var filterPreReleases = this.WhenAnyValue(x => x.ShowAlphaPreReleases)
               .Select(MakeFilter);

            /*
            if (Version != service.MelonInstallerRelease.Name)
            {
                IsUpdateAvailable = true;
            }
            */

            service.Connect()
                .Filter(x => !x.TagName.StartsWith("v0.1") && !x.TagName.StartsWith("v0.2"))
                .Filter(filterPreReleases)
                .Sort(SortExpressionComparer<Release>.Descending(t => t.TagName))
                .Bind(out _releases)
                .DisposeMany()
                .Subscribe(x => SelectedRelease = _releases.First());
            
            _selectedRelease = _releases.First();

            this.WhenAnyValue(x => x.IsAutomated, x => x.UnityGameExe, x => x.ManualZip)
                .Subscribe(x => TryEnableInstallButton());

            this.WhenAnyValue(
                x => x.AutoUpdateInstaller,
                x => x.CloseAfterCompletion,
                x => x.HighlightLogFileLocation,
                x => x.RememberLastSelectedGame,
                x => x.ShowAlphaPreReleases,
                x => x.UseDarkTheme,
                x => x.LastSelectedGamePath
                )
                .Subscribe(x => _settings.Save());
        }

#if DEBUG
        public static bool EnableGridLines => true;
#else
        public static bool EnableGridLines => false;
#endif

        // Settings properties
        public bool AutoUpdateInstaller
        {
            get { return _settings.AutoUpdateInstaller; }
            set
            {
                if (_settings.AutoUpdateInstaller == value) return;
                _settings.AutoUpdateInstaller = value;
                this.RaisePropertyChanged();
            }
        }
        public bool CloseAfterCompletion
        {
            get { return _settings.CloseAfterCompletion; }
            set
            {
                if (_settings.CloseAfterCompletion == value) return;
                _settings.CloseAfterCompletion = value;
                this.RaisePropertyChanged();
            }
        }
            
        public bool HighlightLogFileLocation {
            get
            { return _settings.HighlightLogFileLocation; }
            set
            {
                if (_settings.HighlightLogFileLocation == value) return;
                _settings.HighlightLogFileLocation = value;
                this.RaisePropertyChanged();
            }
        }
        public bool RememberLastSelectedGame {
            get
            { return _settings.RememberLastSelectedGame; }
            set
            {
                if (_settings.RememberLastSelectedGame == value)
                    return;
                _settings.RememberLastSelectedGame = value;
                this.RaisePropertyChanged();
            }
        }
        public bool ShowAlphaPreReleases
        {
            get { return _settings.ShowAlphaPreReleases; }
            set
            {
                if (_settings.ShowAlphaPreReleases == value) return;
                _settings.ShowAlphaPreReleases = value;
                this.RaisePropertyChanged();
            }
        }
        public bool UseDarkTheme
        {
            get { return _settings.Theme == 0; }
            set
            {
                if (_settings.Theme == (value ? 1 : 0)) return;
                _settings.Theme = value ? 1 : 0;
                this.RaisePropertyChanged();
            }
        }

        public string LastSelectedGamePath {
            get { return _settings.LastSelectedGamePath; }
            set
            {
                if (_settings.LastSelectedGamePath == value) return;
                _settings.LastSelectedGamePath = value;
                this.RaisePropertyChanged();
            }
        }



        public bool IsAutomated
        {
            get { return _isAutomated; }
            set {
                this.RaiseAndSetIfChanged(ref _isAutomated, value);
            }
        }

        public bool IsUpdateAvailable
        {
            get { return _isUpdateAvailable; }
            set { this.RaiseAndSetIfChanged(ref _isUpdateAvailable, value); }
        }

        public string UnityGameExe
        {
            get { return _game.GamePath; }
            set {
                // TODO: Check value for actual Path instance
                if (_game.GamePath == value) return;
                _game.GamePath = value;
                this.RaisePropertyChanged();
            }
        }

        public string ManualZip
        {
            get { return _manualZip; }
            set
            {
                // TODO: Check value for actual Path instance
                this.RaiseAndSetIfChanged(ref _manualZip, value);
            }
        }

        public bool EnableInstallButton
        {
            get { return _enableInstallButton; }
            set { this.RaiseAndSetIfChanged(ref _enableInstallButton, value); }
        }

        public Release SelectedRelease
        {
            get { return _selectedRelease; }
            set { this.RaiseAndSetIfChanged(ref _selectedRelease, value); }
        }

        public ReadOnlyObservableCollection<Release> Releases => _releases;

        public static string Version => "3.0.8"; // BuildInfo.Version;


        public async void UnityExeOpenFileDialog()
        {
            if (Avalonia.Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                && desktop.MainWindow is not null)
            {
                var fileDialog = new OpenFileDialog
                {
                    Title = "Select Unity Game Executable",
                    Directory = Environment.CurrentDirectory,
                };
                var result = await fileDialog.ShowAsync(desktop.MainWindow);
                if (result != null && !string.IsNullOrEmpty(result.FirstOrDefault()))
                {
                    UnityGameExe = result[0];
                }
            }
        }

        public async void ManualZipOpenFileDialog()
        {
            if (Avalonia.Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                && desktop.MainWindow is not null)
            {
                var fileDialog = new OpenFileDialog
                {
                    Title = "Select Melon Zip File",
                    Directory = Environment.CurrentDirectory,
                };
                var result = await fileDialog.ShowAsync(desktop.MainWindow);
                if (result != null && !string.IsNullOrEmpty(result.FirstOrDefault()))
                {
                    ManualZip = result[0];
                }
            }
        }

        public static void OpenDiscordURL()
        {
            OpenURL(Settings.LinkDiscord);
        }

        public static void OpenTwitterURL()
        {
            OpenURL(Settings.LinkTwitter);
        }

        public static void OpenGitHubURL()
        {
            OpenURL(Settings.LinkGitHub);
        }
        public static void OpenWikiURL()
        {
            OpenURL(Settings.LinkWiki);
        }

        private static void OpenURL(string url)
        {
            try
	        {
                Process.Start(new ProcessStartInfo() { FileName = url, UseShellExecute = true });
	        }
	        catch {
                // TODO catch
            }
        }
       
    }
}
