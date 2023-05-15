using NuGet.Versioning;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;

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
        public Architectures GameArch { get => _gameArch; }
        public NuGetVersion MelonLoaderVersion { get => _melonLoaderVersion; }


        private static Architectures GetArchitecture(string path)
        {
            const int ELF_MAGIC = 0x464c457f;   // '0x7f', 'E', 'L', 'F'
            const int ELF_MACHINE_OFFSET = 18;
            const int ELF_EM_X86_64 = 62;

            const int PE_MAGIC = 0x00004550;    // 'P', 'E', '0x0', '0x0'
            const int PE_POINTER_OFFSET = 60;
            const int PE_IMAGE_FILE_MACHINE_I386 = 0x014c;
            const int PE_IMAGE_FILE_MACHINE_AMD64 = 0x8664;

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
                        int machineUint = BitConverter.ToUInt16(data, 0);

                        if (elfMagic == ELF_MAGIC && machineUint == ELF_EM_X86_64)
                        {
                            return Architectures.LinuxX64;
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
                            return Architectures.Unknown;
                        }
                        s.Read(data, 0, 2);
                        machineUint = BitConverter.ToUInt16(data, 0);

                        if (machineUint == PE_IMAGE_FILE_MACHINE_AMD64)
                        {
                            return Architectures.WindowsX64;
                        }
                        if (machineUint == PE_IMAGE_FILE_MACHINE_I386)
                        {
                            return Architectures.WindowsX86;
                        }
                    }
                }
                catch (Exception ex)
                {
                    // TODO: Logging
                    throw;
                }
            }

            return Architectures.Unknown;
        }

        private NuGetVersion GetVersion(string path)
        {
            if (File.Exists(path) && _gameArch != Architectures.Unknown)
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
                            fileversion = fileVersionInfo.FileVersion;
                            if (string.IsNullOrEmpty(fileversion))
                                fileversion = fileVersionInfo.ProductVersion;

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
