using DynamicData;
using Octokit;
using System;

namespace MelonLoader.Services
{
    public class MelonReleasesService
    {
        private readonly GitHubClient client = new(new ProductHeaderValue("MelonLoaderInstaller", BuildInfo.Version));
        private readonly SourceList<Release> _releases = new();

        public IObservable<IChangeSet<Release>> Connect() => _releases.Connect();

        public MelonReleasesService()
        {
            try
            {
                _releases.AddRange(client.Repository.Release.GetAll("LavaGang", "MelonLoader").Result);
            } catch (Exception)
            {
                // TODO Exception handling
            }
        }
    }
}
