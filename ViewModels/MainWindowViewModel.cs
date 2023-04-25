﻿using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using DynamicData;
using DynamicData.Binding;
using MelonLoader.Models;
using MelonLoader.Services;
using Octokit;
using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ListView;

namespace MelonLoader.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private bool _isAutomated = true;
        private bool _isUpdateAvailable = false;
        private bool _enableInstallButton;
        private string _manualZip = "";
        private string _unityGameExe = "";
        private Release _selectedRelease;
        private ReleaseAsset _selectedReleaseAsset;
        private Settings _settings;
        private readonly ReadOnlyObservableCollection<Release> _releases;
        private readonly SourceList<ReleaseAsset> _releaseAssetList = new();
        private readonly ReadOnlyObservableCollection<ReleaseAsset> _releaseAssets;

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

        private void UpdateReleaseAssetList(Release release)
        {
            _releaseAssetList.Clear();
            var dict = release.Assets.ToDictionary(keySelector: x => x.Name, elementSelector: x => x);
            _releaseAssetList.AddRange(release.Assets);
            SelectedReleaseAsset = ReleaseAssets.First();
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

            service.Connect()
                .Filter(x => !x.TagName.StartsWith("v0.1") && !x.TagName.StartsWith("v0.2"))
                .Filter(filterPreReleases)
                .Sort(SortExpressionComparer<Release>.Descending(t => t.TagName))
                .Bind(out _releases)
                .DisposeMany()
                .Subscribe(x => SelectedRelease = _releases.First());
            
            _selectedRelease = _releases.First();

            _releaseAssetList.AddRange(_selectedRelease.Assets);
            _releaseAssetList.Connect()
                .Filter(x => x.Name.EndsWith(".zip"))
                .Bind(out _releaseAssets)
                .Subscribe();
            _selectedReleaseAsset = _releaseAssets.First();

            this.WhenAnyValue(x => x.IsAutomated, x => x.UnityGameExe, x => x.ManualZip)
                .Subscribe(x => TryEnableInstallButton());

            this.WhenAnyValue(x => x.SelectedRelease)
                .Subscribe(x => UpdateReleaseAssetList(SelectedRelease));

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
            get { return _unityGameExe; }
            set {
                // TODO: Check value for actual Path instance
                this.RaiseAndSetIfChanged(ref _unityGameExe, value);
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

        public ReleaseAsset SelectedReleaseAsset
        {
            get { return _selectedReleaseAsset; }
            set { this.RaiseAndSetIfChanged(ref _selectedReleaseAsset, value); }
        }

        public ReadOnlyObservableCollection<Release> Releases => _releases;
        public ReadOnlyObservableCollection<ReleaseAsset> ReleaseAssets => _releaseAssets;

        public static string Version => BuildInfo.Version;


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
       
    }
}