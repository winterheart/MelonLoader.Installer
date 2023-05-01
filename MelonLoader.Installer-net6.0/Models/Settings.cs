
namespace MelonLoader.Installer.Models
{
    public class Settings
    {
        /// <summary>
        /// Owner of GitHub MelonLoader repository
        /// </summary>
        public static string MelonLoaderRepoOwner { get; } = "LavaGang";
        /// <summary>
        /// Name of GitHub MelonLoader repository
        /// </summary>
        public static string MelonLoaderRepoName { get; } = "MelonLoader";
        /// <summary>
        /// Owner of GitHub MelonLoader Installer repository
        /// </summary>
        public static string MelonLoaderInstallerRepoOwner { get; } = "LavaGang";
        /// <summary>
        /// Name of GitHub MelonLoader Installer repository
        /// </summary>
        public static string MelonLoaderInstallerRepoName { get; } = "MelonLoader.Installer";

        public static string LinkDiscord { get; } = "https://discord.gg/2Wn3N2P";
        public static string LinkTwitter { get; } = "https://twitter.com/lava_gang";
        public static string LinkGitHub { get; } = "https://github.com/LavaGang";
        public static string LinkWiki { get; } = "https://melonwiki.xyz";

        public bool AutoUpdateInstaller { get; set; } = true;
        public bool CloseAfterCompletion { get; set; } = true;
        public bool HighlightLogFileLocation { get; set; } = true;
        public bool RememberLastSelectedGame { get; set; } = false;
        public bool ShowAlphaPreReleases { get; set; } = false;
        public int Theme { get; set; } = 0;
        public string LastSelectedGamePath { get; set; } = "";

        public void Save()
        {
            // TODO: implement save
        }

        public void Load()
        {
            // TODO: implement load
        }
    }
}
