using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace MelonLoader.Installer.Models
{
    public class ZipFile
    {
        private static readonly HttpClient HttpClient = new();
        private static readonly SHA512 Hasher = SHA512.Create();
        
        public string Filename { get; set; }
        public string Version { get; set; }
        public string Url { get; set; }

        public string Sha512Sum { get; } = "";

        private string CachePath => Path.Combine("cache", Version, Filename);

        public ZipFile(string filename, string version, string url)
        {
            Filename = filename;
            Version = version;
            Url = url;
        }

        public async Task<Stream> LoadZipFileAsync()
        {
            if (File.Exists(CachePath))
            {
                Stream data = File.OpenRead(CachePath);
                var checksumUrl = Url.Replace("zip", "sha512");
                var checksumDownloaded = await HttpClient.GetByteArrayAsync(checksumUrl);
                var checksum = Hasher.ComputeHash(checksumDownloaded);

                return data;
            }
            else
            {
                var data = await HttpClient.GetByteArrayAsync(Url);
                if (!Directory.Exists(Path.Combine("cache", Version)))
                {
                    Directory.CreateDirectory(Path.Combine("cache", Version));
                }
                // Caching
                await using (var fs = File.OpenWrite(CachePath))
                {
                    fs.Write(data);
                }

                return new MemoryStream(data);
            }
        }
    }
}
