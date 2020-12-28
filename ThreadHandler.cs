﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Security.Cryptography;
using System.Windows.Forms;
using MelonLoader.LightJson;

namespace MelonLoader
{
    internal static class ThreadHandler
    {
        internal static void CheckForInstallerUpdate()
        {
            if (Program.RunInstallerUpdateCheck)
            {
                GetReleases();
                return;
            }
            Program.webClient.Headers.Clear();
            Program.webClient.Headers.Add("User-Agent", "request");
            string response = null;
            try { response = Program.webClient.DownloadString(Config.Repo_API_Installer); } catch { response = null; }
            if (string.IsNullOrEmpty(response))
            {
                GetReleases();
                return;
            }
            if (Program.Closing)
                return;
            JsonArray data = JsonValue.Parse(response).AsJsonArray;
            if (data.Count <= 0)
            {
                GetReleases();
                return;
            }
            JsonValue release = data[0];
            JsonArray assets = release["assets"].AsJsonArray;
            if (assets.Count <= 0)
            {
                GetReleases();
                return;
            }
            string version = release["tag_name"].AsString;
            if (version.Equals(BuildInfo.Version))
            {
                GetReleases();
                return;
            }
            Program.mainForm.Invoke(new Action(() => { Program.mainForm.InstallerUpdateNotice.Visible = true; }));
            if (!Config.AutoUpdateInstaller)
            {
                GetReleases();
                return;
            }
            OperationHandler.CurrentOperation = OperationHandler.Operations.INSTALLER_UPDATE;
            Program.mainForm.Invoke(new Action(() => {
                Program.mainForm.Tab_Output.Text = "UPDATE   ";
                Program.SetCurrentOperation("Updating Installer...");
                Program.mainForm.PageManager.Controls.Clear();
                Program.mainForm.PageManager.Controls.Add(Program.mainForm.Tab_Output);
            }));
            string downloadurl = assets[0]["browser_download_url"].AsString;
            string temp_path = TempFileCache.CreateFile();
            try { Program.webClient.DownloadFileAsync(new Uri(downloadurl), temp_path); while (Program.webClient.IsBusy) { } }
            catch (Exception ex)
            {
                TempFileCache.ClearCache();
                Program.LogError(ex.ToString());
                RefreshReleases();
                return;
            }
            Program.SetTotalPercentage(50);
            if (Program.Closing)
                return;
            string repo_hash = null;
            try { repo_hash = Program.webClient.DownloadString(assets[1]["browser_download_url"].AsString); } catch { repo_hash = null; }
            if (string.IsNullOrEmpty(repo_hash))
            {
                TempFileCache.ClearCache();
                Program.LogError("Failed to get SHA512 Hash from Repo!");
                RefreshReleases();
                return;
            }
            if (Program.Closing)
                return;
            SHA512Managed sha512 = new SHA512Managed();
            byte[] checksum = sha512.ComputeHash(File.ReadAllBytes(temp_path));
            if ((checksum == null) || (checksum.Length <= 0))
            {
                TempFileCache.ClearCache();
                Program.LogError("Failed to get SHA512 Hash from Temp File!");
                RefreshReleases();
                return;
            }
            string file_hash = BitConverter.ToString(checksum).Replace("-", string.Empty);
            if (string.IsNullOrEmpty(file_hash) || !file_hash.Equals(repo_hash))
            {
                TempFileCache.ClearCache();
                Program.LogError("Failed to get SHA512 Hash from Temp File!");
                RefreshReleases();
                return;
            }
            Program.OperationSuccess();
            string exe_path = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
            string tmp_file_path = Path.Combine(exe_path, (Path.GetFileNameWithoutExtension(downloadurl) + ".tmp.exe"));
            if (File.Exists(tmp_file_path))
                File.Delete(tmp_file_path);
            File.Move(temp_path, tmp_file_path);
            Process.Start(tmp_file_path);
            Process.GetCurrentProcess().Kill();
        }

        private static void GetReleases()
        {
            Program.mainForm.Invoke(new Action(() => {
                Program.mainForm.PageManager.Controls.Add(Program.mainForm.Tab_ManualZip);
                Program.mainForm.PageManager.Controls.Add(Program.mainForm.Tab_Settings);
            }));
            RefreshReleases();
        }

        internal static void RefreshReleases()
        {
            Program.mainForm.Invoke(new Action(() => {
                Program.mainForm.PageManager.Cursor = Cursors.Hand;
                Program.mainForm.Tab_Automated.Text = "Automated   ";
                Program.mainForm.PleaseWait_PleaseWait.Visible = true;
                Program.mainForm.PleaseWait_PleaseWait.ForeColor = SystemColors.Highlight;
                Program.mainForm.PleaseWait_PleaseWait.Text = "PLEASE WAIT";
                Program.mainForm.PleaseWait_PleaseWait.Location = new Point(161, 36);
                Program.mainForm.PleaseWait_PleaseWait.Size = new Size(127, 25);
                Program.mainForm.PleaseWait_Text.Visible = true;
                Program.mainForm.PleaseWait_Text.Text = "Getting List of Releases from GitHub...";
                Program.mainForm.PleaseWait_Text.Location = new Point(105, 79);
                Program.mainForm.PleaseWait_Text.Size = new Size(250, 22);
                Program.mainForm.Error_Retry.Visible = false;
                Program.mainForm.Automated_UnityGame_Text.Visible = false;
                Program.mainForm.Automated_UnityGame_Select.Visible = false;
                Program.mainForm.Automated_UnityGame_Display.Visible = false;
                Program.mainForm.Automated_Arch_Text.Visible = false;
                Program.mainForm.Automated_Arch_Selection.Visible = false;
                Program.mainForm.Automated_Arch_AutoDetect.Visible = false;
                Program.mainForm.Automated_x64Only.Visible = false;
                Program.mainForm.Automated_Divider.Visible = false;
                Program.mainForm.Automated_Install.Visible = false;
                Program.mainForm.Automated_Version_Text.Visible = false;
                Program.mainForm.Automated_Version_Selection.Visible = false;
                Program.mainForm.Automated_Version_Latest.Visible = false;
                Program.mainForm.Automated_Version_Selection.Items.Clear();
            }));
            ParseReleasesURL();
            Program.mainForm.Invoke(new Action(() =>
            {
                if (Program.mainForm.Automated_Version_Selection.Items.Count <= 0)
                {
                    Program.mainForm.PageManager.Cursor = Cursors.Hand;
                    Program.mainForm.PleaseWait_PleaseWait.ForeColor = Color.Red;
                    Program.mainForm.PleaseWait_PleaseWait.Text = "ERROR";
                    Program.mainForm.PleaseWait_PleaseWait.Location = new Point(184, 36);
                    Program.mainForm.PleaseWait_PleaseWait.Size = new Size(72, 25);
                    Program.mainForm.PleaseWait_Text.Text = "Failed to get List of Releases from GitHub!";
                    Program.mainForm.PleaseWait_Text.Location = new Point(94, 79);
                    Program.mainForm.PleaseWait_Text.Size = new Size(266, 19);
                    Program.mainForm.Error_Retry.Visible = true;
                    return;
                }
                Program.mainForm.PleaseWait_PleaseWait.Visible = false;
                Program.mainForm.PleaseWait_Text.Visible = false;
                Program.mainForm.Error_Retry.Visible = false;
                Program.mainForm.Automated_Version_Selection.SelectedIndex = 0;
                Program.mainForm.Automated_UnityGame_Text.Visible = true;
                Program.mainForm.Automated_UnityGame_Select.Visible = true;
                Program.mainForm.Automated_UnityGame_Display.Visible = true;
                Program.mainForm.Automated_Arch_Text.Visible = true;
                Program.mainForm.Automated_Divider.Visible = true;
                Program.mainForm.Automated_Install.Visible = true;
                Program.mainForm.Automated_Version_Text.Visible = true;
                Program.mainForm.Automated_Version_Selection.Visible = true;
                Program.mainForm.Automated_Version_Latest.Visible = true;
            }));
        }

        internal static void ParseReleasesURL()
        {
            Program.webClient.Headers.Clear();
            Program.webClient.Headers.Add("User-Agent", "Unity web player");
            string response = null;
            try { response = Program.webClient.DownloadString(Config.Repo_API_MelonLoader); } catch { response = null; }
            if (string.IsNullOrEmpty(response))
                return;
            if (Program.Closing)
                return;
            JsonArray data = JsonValue.Parse(response).AsJsonArray;
            if (data.Count <= 0)
                return;
            List<string> releasesList = new List<string>();
            foreach (JsonValue release in data)
            {
                JsonArray assets = release["assets"].AsJsonArray;
                if (assets.Count <= 0)
                    continue;
                if (!Config.ShowAlphaReleases && release["prerelease"].AsBoolean)
                    continue;
                string version = release["tag_name"].AsString;
                releasesList.Add(version);
            }
            releasesList.Sort();
            releasesList.Reverse();
            Program.mainForm.Invoke(new Action(() => { Program.mainForm.Automated_Version_Selection.Items.AddRange(releasesList.ToArray()); }));
        }
    }
}
