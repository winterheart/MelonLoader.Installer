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
        [Description("x86")]
        Windows_x86 = 1,
        [Description("x64")]
        Windows_x64 = 2,
        [Description("Linux.x64")]
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

            const int PE_POINTER_OFFSET = 60;
            const int PE_MACHINE_OFFSET = 4;
            const int PE_X86 = 0x014c;
            const int PE_X64 = 0x8664;
            int machineUint = 0;

            byte[] data = new byte[4096];

            
            if (File.Exists(path))
            {
                try {
                    // First try to determine if there Linux ELF. This is very simple
                    using (Stream s = new FileStream(path, FileMode.Open, FileAccess.Read))
                    {
                        s.Read(data, 0, 20);
                    }

                    int ELF_HEADER_MAGIC = BitConverter.ToInt32(data, 0);
                    machineUint = BitConverter.ToUInt16(data, ELF_MACHINE_OFFSET);

                    if (ELF_HEADER_MAGIC == ELF_MAGIC && machineUint == ELF_EM_X86_64)
                    {
                        return Arhitectures.Linux_x64;
                    }

                    // This is not a Linux system... Guess this is Windows?
                    using (Stream s = new FileStream(path, FileMode.Open, FileAccess.Read))
                    {
                        s.Read(data, 0, 4096);
                    }

                    // DOS header is 64 bytes, last element, long (4 bytes) is the address of the PE header
                    int PE_HEADER_ADDR = BitConverter.ToInt32(data, PE_POINTER_OFFSET);
                    machineUint = BitConverter.ToUInt16(data, PE_HEADER_ADDR + PE_MACHINE_OFFSET);
                }
                catch (Exception ex)
                {
                    // TODO: Logging
                    throw;
                }
            }

            switch (machineUint)
            {
                case PE_X64: return Arhitectures.Windows_x64;
                case PE_X86: return Arhitectures.Windows_x86;
                default: return Arhitectures.Unknown;
            }
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
