using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using DynamicData;
using DynamicData.Binding;
using MelonLoader.Installer.EnumExtensions;
using MelonLoader.Installer.Models;
using MelonLoader.Installer.Services;
using NuGet.Versioning;
using Octokit;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
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

        private static Func<Release, bool> ShowPreReleases(bool showPrereleases)
        {
            return release => !release.Prerelease || showPrereleases;
        }

        private Func<Release, bool> ShowArches(string arch)
        {
            if (_game.GameArch == Architectures.Unknown) return release => true;
            return release => {
                return release.Assets.Any(asset => asset.Name.Contains(_game.GameArch.GetDescription()));
            };
        }

        public MainWindowViewModel(Settings settings, MelonReleasesService service)
        {
            _settings = settings;

            var filterPreReleases = this.WhenAnyValue(x => x.ShowAlphaPreReleases)
                .Select(ShowPreReleases);
            var filterArches = this.WhenAnyValue(x => x.UnityGameExe)
                .Select(ShowArches);

            /*
            if (Version != service.MelonInstallerRelease.Name)
            {
                IsUpdateAvailable = true;
            }
            */

            service.Connect()
                .Filter(x => new NuGetVersion(x.TagName[1..]) >= new NuGetVersion(Settings.SupportedVersion))
                .Filter(filterPreReleases)
                .Filter(filterArches)
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
            get => _settings.AutoUpdateInstaller;
            set
            {
                if (_settings.AutoUpdateInstaller == value) return;
                _settings.AutoUpdateInstaller = value;
                this.RaisePropertyChanged();
            }
        }
        public bool CloseAfterCompletion
        {
            get => _settings.CloseAfterCompletion;
            set
            {
                if (_settings.CloseAfterCompletion == value) return;
                _settings.CloseAfterCompletion = value;
                this.RaisePropertyChanged();
            }
        }
            
        public bool HighlightLogFileLocation {
            get => _settings.HighlightLogFileLocation;
            set
            {
                if (_settings.HighlightLogFileLocation == value) return;
                _settings.HighlightLogFileLocation = value;
                this.RaisePropertyChanged();
            }
        }
        public bool RememberLastSelectedGame {
            get => _settings.RememberLastSelectedGame;
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
            get => _settings.ShowAlphaPreReleases;
            set
            {
                if (_settings.ShowAlphaPreReleases == value) return;
                _settings.ShowAlphaPreReleases = value;
                this.RaisePropertyChanged();
            }
        }
        public bool UseDarkTheme
        {
            get => _settings.Theme == 0;
            set
            {
                if (_settings.Theme == (value ? 1 : 0)) return;
                _settings.Theme = value ? 1 : 0;
                this.RaisePropertyChanged();
            }
        }

        public string LastSelectedGamePath {
            get => _settings.LastSelectedGamePath;
            set
            {
                if (_settings.LastSelectedGamePath == value) return;
                _settings.LastSelectedGamePath = value;
                this.RaisePropertyChanged();
            }
        }



        public bool IsAutomated
        {
            get => _isAutomated;
            set => this.RaiseAndSetIfChanged(ref _isAutomated, value);
        }

        public bool IsUpdateAvailable
        {
            get => _isUpdateAvailable;
            set => this.RaiseAndSetIfChanged(ref _isUpdateAvailable, value);
        }

        public string UnityGameExe
        {
            get => _game.GamePath;
            set {
                // TODO: Check value for actual Path instance
                if (_game.GamePath == value) return;
                _game.GamePath = value;
                this.RaisePropertyChanged();
            }
        }

        public Architectures UnityGameArch => _game.GameArch;

        public string ManualZip
        {
            get => _manualZip;
            // TODO: Check value for actual Path instance
            set => this.RaiseAndSetIfChanged(ref _manualZip, value);
        }

        public bool EnableInstallButton
        {
            get => _enableInstallButton;
            set => this.RaiseAndSetIfChanged(ref _enableInstallButton, value);
        }

        public Release SelectedRelease
        {
            get => _selectedRelease;
            set => this.RaiseAndSetIfChanged(ref _selectedRelease, value);
        }

        public ReadOnlyObservableCollection<Release> Releases => _releases;

        public static string Version => "3.0.8"; // BuildInfo.Version;


        public async void UnityExeOpenFileDialog()
        {
            if (Avalonia.Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime
                {
                    MainWindow: not null
                } desktop) return;
            var result = await desktop.MainWindow.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions(){
                Title = "Select Unity Game Executable",
                AllowMultiple = false,
                /*
                    FileTypeFilter = new List<FilePickerFileType>()
                    {
                        new FilePickerFileType("All files (*.*)")
                        {
                            Patterns = new List<string>() { "*" },
                        },
                    }
                    */
            });

            if (result.Count != 0)
            {
                UnityGameExe = result[0].Path.LocalPath;
            }
        }

        public async void ManualZipOpenFileDialog()
        {
            if (Avalonia.Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime
                {
                    MainWindow: not null
                } desktop) return;
            var result = await desktop.MainWindow.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions(){
                Title = "Select MelonLoader ZIP File",
                AllowMultiple = false,
                FileTypeFilter = new List<FilePickerFileType>()
                {
                    new FilePickerFileType("ZIP archives (*.zip)")
                    {
                        Patterns = new List<string>() { "*.zip" },
                        // MimeTypes = new List<string>() { "application/zip" },
                    },
                }
            });
            if (result.Count != 0)
            {
                ManualZip = result[0].Path.LocalPath;
            }
        }

        public static void OpenDiscordURL()
        {
            OpenUrl(Settings.LinkDiscord);
        }

        public static void OpenTwitterUrl()
        {
            OpenUrl(Settings.LinkTwitter);
        }

        public static void OpenGitHubUrl()
        {
            OpenUrl(Settings.LinkGitHub);
        }
        public static void OpenWikiUrl()
        {
            OpenUrl(Settings.LinkWiki);
        }

        private static void OpenUrl(string url)
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
