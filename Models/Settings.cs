
namespace MelonLoader.Models
{
    public class Settings
    {
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
