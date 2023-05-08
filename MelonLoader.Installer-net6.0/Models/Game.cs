using NuGet.Versioning;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;

namespace MelonLoader.Installer.Models
{
    public enum Arhitectures
    {
        [Description("Unknown")]
        Unknown = 0,
        [Description("MelonLoader.x86")]
        Windows_x86 = 1,
        [Description("MelonLoader.x64")]
        Windows_x64 = 2,
        [Description("MelonLoader.Linux.x64")]
        Linux_x64 = 3,
    };

    public class Game
    {
        private string _gamePath = "";
        private Arhitectures _gameArch = Arhitectures.Unknown;
        private NuGetVersion _melonLoaderVersion = new NuGetVersion(0, 0, 0);

        public string GamePath {
            get { return _gamePath; }
            set
            {
                _gamePath = value;
                _gameArch = getArchitecture(_gamePath);
                _melonLoaderVersion = getVersion(_gamePath);
            }
        }
        public Arhitectures GameArch { get => _gameArch; }
        public NuGetVersion MelonLoaderVersion { get => _melonLoaderVersion; }


        private Arhitectures getArchitecture(string path)
        {
            const int ELF_MAGIC = 0x464c457f;   // '0x7f', 'E', 'L', 'F'
            const int ELF_MACHINE_OFFSET = 18;
            const int ELF_EM_X86_64 = 62;

            const int PE_MAGIC = 0x00004550;    // 'P', 'E', '0x0', '0x0'
            const int PE_POINTER_OFFSET = 60;
            const int PE_IMAGE_FILE_MACHINE_I386 = 0x014c;
            const int PE_IMAGE_FILE_MACHINE_AMD64 = 0x8664;
            int machineUint = 0;

            byte[] data = new byte[4];

            
            if (File.Exists(path))
            {
                try {
                    // First try to determine if there Linux ELF. This is very simple
                    using (Stream s = new FileStream(path, FileMode.Open, FileAccess.Read))
                    {
                        s.Read(data, 0, 4);
                        int elfMagic = BitConverter.ToInt32(data, 0);
                        s.Seek(ELF_MACHINE_OFFSET, 0);
                        s.Read(data, 0, 2);
                        machineUint = BitConverter.ToUInt16(data, 0);

                        if (elfMagic == ELF_MAGIC && machineUint == ELF_EM_X86_64)
                        {
                            return Arhitectures.Linux_x64;
                        }

                        // This is not a Linux system... Guess this is Windows?
                        s.Seek(PE_POINTER_OFFSET, 0);
                        s.Read(data, 0, 4);
                        int peHeaderPtr = BitConverter.ToInt32(data, 0);
                        s.Seek(peHeaderPtr, 0);
                        s.Read(data, 0, 4);
                        int peMagic = BitConverter.ToInt32(data, 0);
                        if (peMagic != PE_MAGIC)
                        {
                            return Arhitectures.Unknown;
                        }
                        s.Read(data, 0, 2);
                        machineUint = BitConverter.ToUInt16(data, 0);

                        if (machineUint == PE_IMAGE_FILE_MACHINE_AMD64)
                        {
                            return Arhitectures.Windows_x64;
                        }
                        if (machineUint == PE_IMAGE_FILE_MACHINE_I386)
                        {
                            return Arhitectures.Windows_x86;
                        }
                    }
                }
                catch (Exception ex)
                {
                    // TODO: Logging
                    throw;
                }
            }

            return Arhitectures.Unknown;
        }

        private NuGetVersion getVersion(string path)
        {
            if (File.Exists(path) && _gameArch != Arhitectures.Unknown)
            {
                try {
                    var gameDir = Path.GetDirectoryName(path);
                    var folderPath = Path.Combine(gameDir, "MelonLoader");

                    List<string> guessPath = new List<string>()
                    {
                        Path.Combine(folderPath, "MelonLoader.ModHandler.dll"), // Legacy path
                        Path.Combine(folderPath, "MelonLoader.dll"),            // Old path
                        Path.Combine(folderPath, "net35", "MelonLoader.dll"),   // New path
                    };

                    foreach (var testPath in guessPath)
                    {
                        if (File.Exists(testPath))
                        {
                            string? fileversion = null;
                            FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(testPath);
                            if (fileVersionInfo != null)
                            {
                                fileversion = fileVersionInfo.FileVersion;
                                if (string.IsNullOrEmpty(fileversion))
                                    fileversion = fileVersionInfo.ProductVersion;
                            }
                            if (!string.IsNullOrEmpty(fileversion))
                                return new NuGetVersion(fileversion);

                        }
                    }
                }
                catch (Exception ex)
                {
                    // TODO: Logging
                    throw;
                }
                
            }
            return new NuGetVersion(0, 0, 0);
        }

    }

}
