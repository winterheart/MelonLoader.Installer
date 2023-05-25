using NuGet.Versioning;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace MelonLoader.Installer.Models
{
    public enum Architectures
    {
        [Description("Unknown")]
        Unknown = 0,
        [Description("MelonLoader.x86")]
        WindowsX86 = 1,
        [Description("MelonLoader.x64")]
        WindowsX64 = 2,
        [Description("MelonLoader.Linux.x64")]
        LinuxX64 = 3,
    }

    public class Game
    {
        private string _gamePath = "";
        private Architectures _gameArch = Architectures.Unknown;
        private NuGetVersion _melonLoaderVersion = new NuGetVersion(0, 0, 0);

        public string GamePath {
            get => _gamePath;
            set
            {
                _gamePath = value;
                _gameArch = GetArchitecture(_gamePath);
                _melonLoaderVersion = GetVersion(_gamePath);
            }
        }
        public Architectures GameArch => _gameArch;
        public NuGetVersion MelonLoaderVersion => _melonLoaderVersion;

        private static Architectures GetArchitecture(string path)
        {
            const int ELF_MAGIC = 0x464c457f;   // '0x7f', 'E', 'L', 'F'
            const int ELF_MACHINE_OFFSET = 18;
            const int ELF_EM_X86_64 = 62;

            const int PE_MAGIC = 0x00004550;    // 'P', 'E', '0x0', '0x0'
            const int PE_POINTER_OFFSET = 60;
            const int PE_IMAGE_FILE_MACHINE_I386 = 0x014c;
            const int PE_IMAGE_FILE_MACHINE_AMD64 = 0x8664;

            var data = new byte[4];
            
            if (File.Exists(path))
            {
                try {
                    // First try to determine if there Linux ELF. This is very simple
                    using (Stream s = new FileStream(path, FileMode.Open, FileAccess.Read))
                    {
                        s.Read(data, 0, 4);
                        var elfMagic = BitConverter.ToInt32(data, 0);
                        s.Seek(ELF_MACHINE_OFFSET, 0);
                        s.Read(data, 0, 2);
                        int machineUint = BitConverter.ToUInt16(data, 0);

                        if (elfMagic == ELF_MAGIC && machineUint == ELF_EM_X86_64)
                        {
                            return Architectures.LinuxX64;
                        }

                        // This is not a Linux system... Guess this is Windows?
                        s.Seek(PE_POINTER_OFFSET, 0);
                        s.Read(data, 0, 4);
                        var peHeaderPtr = BitConverter.ToInt32(data, 0);
                        s.Seek(peHeaderPtr, 0);
                        s.Read(data, 0, 4);
                        var peMagic = BitConverter.ToInt32(data, 0);
                        if (peMagic != PE_MAGIC)
                        {
                            return Architectures.Unknown;
                        }
                        s.Read(data, 0, 2);
                        machineUint = BitConverter.ToUInt16(data, 0);

                        switch (machineUint)
                        {
                            case PE_IMAGE_FILE_MACHINE_AMD64:
                                return Architectures.WindowsX64;
                            case PE_IMAGE_FILE_MACHINE_I386:
                                return Architectures.WindowsX86;
                        }
                    }
                }
                catch (Exception ex)
                {
                    // TODO: Log and handle
                }
            }

            return Architectures.Unknown;
        }

        private NuGetVersion GetVersion(string path)
        {
            if (!File.Exists(path) || _gameArch == Architectures.Unknown) return new NuGetVersion(0, 0, 0);
            try {
                var baseDir = Path.GetDirectoryName(path);
                var melonDir = Path.Combine(baseDir!, "MelonLoader");

                var guessPath = new List<string>
                {
                    Path.Combine(melonDir, "MelonLoader.ModHandler.dll"), // Legacy path
                    Path.Combine(melonDir, "MelonLoader.dll"),            // Old path
                    Path.Combine(melonDir, "net35", "MelonLoader.dll"),   // New path
                };

                foreach (var testPath in guessPath)
                {
                    if (!File.Exists(testPath)) continue;
                    var fileVersionInfo = FileVersionInfo.GetVersionInfo(testPath);
                    var fileversion = fileVersionInfo.FileVersion;
                    if (string.IsNullOrEmpty(fileversion))
                        fileversion = fileVersionInfo.ProductVersion;

                    if (!string.IsNullOrEmpty(fileversion))
                        return new NuGetVersion(fileversion);
                }
            }
            catch (Exception ex)
            {
                // TODO: Logging
                throw;
            }
            return new NuGetVersion(0, 0, 0);
        }

        public void InstallMelonLoader(Stream zipArchiveStream)
        {
            var baseDir = Path.GetDirectoryName(GamePath);

            ExtraCreate(baseDir!);
            using var zip = new ZipArchive(zipArchiveStream);

            foreach (var entry in zip.Entries)
            {
                var path = Path.Combine(baseDir!, entry.FullName);
                var filename = Path.GetFileName(path);
                if (!string.IsNullOrEmpty(filename))
                {
                    var directory = Path.GetDirectoryName(path);
                    if (!Directory.Exists(directory))
                        Directory.CreateDirectory(directory!);
                    using var targetStream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write);
                    using var entryStream = entry.Open();
                    try
                    {
                        entryStream.CopyTo(targetStream);
                    }
                    catch (Exception ex)
                    {
                        // TODO: Log and handle
                    }
                    continue;
                }

                if (!Directory.Exists(path))
                {
                    try
                    {
                        Directory.CreateDirectory(path);
                    }
                    catch (Exception ex)
                    {
                        // TODO: Log and handle
                    }
                }
            }
            _melonLoaderVersion = GetVersion(GamePath);
        }
        
        public void UninstallMelonLoader()
        {
            var baseDir = Path.GetDirectoryName(GamePath);
            var melonDir = Path.Combine(baseDir!, "MelonLoader");
            try
            {
                if (Directory.Exists(melonDir))
                    Directory.Delete(melonDir, true);
                ExtraCleanup(baseDir!);
            }
            catch (Exception ex)
            {
                // TODO: Log and handle
            }

            _melonLoaderVersion = GetVersion(GamePath);
        }

        private static void ExtraCreate(string destination)
        {
            var createPaths = new List<string>
            {
                Path.Combine(destination, "Mods"),
                Path.Combine(destination, "Plugins"),
                Path.Combine(destination, "UserData"),
            };
            foreach (var path in createPaths.Where(path => !Directory.Exists(path)))
            {
                try
                {
                    Directory.CreateDirectory(path);
                }
                catch (Exception ex)
                {
                    // TODO: log and exception handle
                }
            }
        }
        
        private static void ExtraCleanup(string destination)
        {
            var deletePaths = new List<string>
            {
                Path.Combine(destination, "version.dll"),
                Path.Combine(destination, "winmm.dll"),
                Path.Combine(destination, "winhttp.dll"),
                Path.Combine(destination, "dobby.dll"),
                Path.Combine(destination, "MelonLoader.dll"),
                Path.Combine(destination, "Mods", "MelonLoader.dll"),
                Path.Combine(destination, "Plugins", "MelonLoader.dll"),
                Path.Combine(destination, "UserData", "MelonLoader.dll"),
                Path.Combine(destination, "MelonLoader.ModHandler.dll"),
                Path.Combine(destination, "Mods", "MelonLoader.ModHandler.dll"),
                Path.Combine(destination, "Plugins", "MelonLoader.ModHandler.dll"),
                Path.Combine(destination, "UserData", "MelonLoader.ModHandler.dll"),
            };
            var logPath = Path.Combine(destination, "Logs");
            foreach (var file in deletePaths.Where(File.Exists))
            {
                try
                {
                    File.Delete(file);
                }
                catch (Exception ex)
                {
                    // TODO: log and exception handle
                }
                
            }

            if (Directory.Exists(logPath))
            {
                try
                {
                    Directory.Delete(logPath, true);
                }
                catch (Exception e)
                {
                    // TODO: log and exception handle
                }
            }
        }
        
    }

}
