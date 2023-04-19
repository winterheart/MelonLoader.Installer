using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using DynamicData;
using MelonLoader.Services;
using Octokit;
using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;

namespace MelonLoader.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private SourceList<Release> _ReleaseItems;

        private readonly ReadOnlyObservableCollection<Release> _ReleaseViewModels;
        public ReadOnlyObservableCollection<Release> ReleaseViewModels => _ReleaseViewModels;

        public MainWindowViewModel(MelonReleasesService source)
        {
            _ReleaseItems = new();
            _ReleaseItems.AddRange(source.GetReleases());

            // Update and filter list of releases reactively
            _ReleaseItems.Connect()
                .Filter(x => !x.TagName.StartsWith("v0.1") && !x.TagName.StartsWith("v0.2"))
                .Filter(x => !x.Prerelease || Config.ShowAlphaPreReleases)
                .Bind(out _ReleaseViewModels)
                .Subscribe();

            // ReleasesList = new ReleaseViewModel(source.GetReleases());
        }

        //public ReleaseViewModel ReleasesList { get; }

#if DEBUG
        public static bool EnableGridLines => true;
#else
        public static bool EnableGridLines => false;
#endif

        private bool _isAutomated = true;
        public bool IsAutomated
        {
            get { return _isAutomated; }
            set {
                this.RaiseAndSetIfChanged(ref _isAutomated, value);
                TryEnableInstallButton();
            }
        }

        private bool _isUpdateAvailable = false;
        public bool IsUpdateAvailable
        {
            get { return _isUpdateAvailable; }
            set { this.RaiseAndSetIfChanged(ref _isUpdateAvailable, value); }
        }

        private string? _unityGameExe;
        public string? UnityGameExe
        {
            get { return _unityGameExe; }
            set {
                // TODO: Check value for actual Path instance
                this.RaiseAndSetIfChanged(ref _unityGameExe, value);
                TryEnableInstallButton();
            }
        }

        private string? _manualZip;
        public string? ManualZip
        {
            get { return _manualZip; }
            set
            {
                // TODO: Check value for actual Path instance
                this.RaiseAndSetIfChanged(ref _manualZip, value);
                TryEnableInstallButton();
            }
        }

        private bool _enableInstallButton;
        public bool EnableInstallButton
        {
            get { return _enableInstallButton; }
            set { this.RaiseAndSetIfChanged(ref _enableInstallButton, value); }
        }

        public static string Version => BuildInfo.Version;

        public static bool AutoUpdateInstaller
        {
            get { return Config.AutoUpdateInstaller; }
            set { Config.AutoUpdateInstaller = value; }
        }

        public static bool CloseAfterCompletion
        {
            get { return Config.CloseAfterCompletion; }
            set { Config.CloseAfterCompletion = value; }
        }

        public static bool UseDarkTheme
        {
            get { return Config.Theme == 0; }
            set { Config.Theme = value? 0 : 1; }
        }

        public static bool HighlightLogFileLocation
        {
            get { return Config.HighlightLogFileLocation; }
            set { Config.HighlightLogFileLocation = value; }
        }

        public static bool RememberLastSelectedGame
        {
            get { return Config.RememberLastSelectedGame; }
            set { Config.RememberLastSelectedGame = value; }
        }

        public static bool ShowAlphaPreReleases
        {
            get { return Config.ShowAlphaPreReleases; }
            set { Config.ShowAlphaPreReleases = value; }
        }

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
            OpenURL(Config.Link_Discord);
        }

        public static void OpenTwitterURL()
        {
            OpenURL(Config.Link_Twitter);
        }

        public static void OpenGitHubURL()
        {
            OpenURL(Config.Link_GitHub);
        }
        public static void OpenWikiURL()
        {
            OpenURL(Config.Link_Wiki);
        }

        private static void OpenURL(string url)
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }

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
        
    }
}
