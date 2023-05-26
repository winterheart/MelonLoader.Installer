using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace MelonLoader.Installer.Models;

public class ZipFile
{
    private static readonly HttpClient HttpClient = new();
    private static readonly SHA512 Hasher = SHA512.Create();
        
    public string Filename { get; set; }
    public string Version { get; set; }
    public string Url { get; set; }

    private string CachePath => Path.Combine("cache", Version, Filename);

    public ZipFile(string filename, string version, string url)
    {
        Filename = filename;
        Version = version;
        Url = url;
    }

    private async Task<Stream> FetchZipFileAsync()
    {
        // Cache hit
        if (File.Exists(CachePath))
        {
            return File.OpenRead(CachePath);
        }

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

    /// <summary>
    /// Loads Zip archive from Github
    /// </summary>
    /// <returns>Stream of downloaded file</returns>
    /// <exception cref="Exception">When downloaded file has SHA512 checksum mismatch</exception>
    public async Task<Stream> LoadZipFileAsync()
    {
        var checksumUrl = Url.Replace(".zip", ".sha512");
        try
        {
            var checksumDownloaded = await HttpClient.GetStringAsync(checksumUrl);

            var data = await FetchZipFileAsync();
            var checksum = Convert.ToHexString(await Hasher.ComputeHashAsync(data));
            if (checksum == checksumDownloaded) return data;
            
            // Checksum mismatch            
            // Delete cache
            File.Delete(CachePath);
            throw new Exception($"Checksum mismatch on {CachePath} ({Url}): {checksum} != {checksumDownloaded}");
        }
        catch (Exception ex)
        {
            // TODO: log and handle
            throw;
        }
    }
}
