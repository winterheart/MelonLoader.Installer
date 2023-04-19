using Octokit;
using System.Collections.Generic;

namespace MelonLoader.Services
{
    public class MelonReleasesService
    {
        private readonly GitHubClient client = new(new ProductHeaderValue("MelonLoaderInstaller", BuildInfo.Version));

        public IEnumerable<Release> GetReleases()
        {
            return client.Repository.Release.GetAll("LavaGang", "MelonLoader").Result;
        }
    }
}
