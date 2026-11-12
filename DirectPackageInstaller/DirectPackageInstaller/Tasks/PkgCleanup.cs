using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DirectPackageInstaller.Tasks
{
    /// <summary>
    /// Watches a Remote Package Installer task on the PS4 and deletes the
    /// local .pkg file once the console has finished downloading it and
    /// its post-download preparing stage has ended (or stopped changing).
    /// Only local files sent through the "file" source are ever touched.
    /// </summary>
    public static class PkgCleanup
    {
        private static readonly Regex LengthTotal = new Regex("\"length_total\"\\s*:\\s*0x([0-9A-Fa-f]+)", RegexOptions.Compiled);
        private static readonly Regex Transferred = new Regex("\"transferred_total\"\\s*:\\s*0x([0-9A-Fa-f]+)", RegexOptions.Compiled);
        private static readonly Regex ErrorCode = new Regex("\"error\"\\s*:\\s*(-?\\d+)", RegexOptions.Compiled);
        private static readonly Regex PreparingPercent = new Regex("\"preparing_percent\"\\s*:\\s*(-?\\d+)", RegexOptions.Compiled);
        private static readonly Regex LocalCopyPercent = new Regex("\"local_copy_percent\"\\s*:\\s*(-?\\d+)", RegexOptions.Compiled);

        //After the download ends, wait this long without any change in the
        //post-download progress values before deleting the file anyway.
        private static readonly TimeSpan SettleTime = TimeSpan.FromSeconds(60);

        public static void Watch(string psIp, int taskId, string localFile)
        {
            if (taskId < 0 || string.IsNullOrWhiteSpace(localFile))
                return;

            if (!localFile.EndsWith(".pkg", StringComparison.OrdinalIgnoreCase))
                return;

            _ = Task.Run(() => WatchAsync(psIp, taskId, localFile));
        }

        private static async Task WatchAsync(string psIp, int taskId, string localFile)
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            var uri = $"http://{psIp}:12800/api/get_task_progress";
            var body = $"{{\"task_id\":{taskId}}}";

            int failures = 0;
            bool DownloadFinished = false;
            DateTime LastChange = DateTime.UtcNow;
            (int, int) LastState = (-2, -2);
            var deadline = DateTime.UtcNow.AddHours(24);

            while (DateTime.UtcNow < deadline)
            {
                await Task.Delay(5000);

                try
                {
                    var response = await client.PostAsync(uri, new StringContent(body, Encoding.UTF8));
                    var text = await response.Content.ReadAsStringAsync();

                    if (!text.Contains("\"success\""))
                    {
                        // Task unknown or failed on the PS4: keep the file.
                        if (++failures >= 5)
                        {
                            Log($"Gave up watching task {taskId}: {text.Trim()}");
                            return;
                        }
                        continue;
                    }

                    failures = 0;

                    var err = ErrorCode.Match(text);
                    if (err.Success && err.Groups[1].Value != "0")
                    {
                        Log($"Task {taskId} reported error {err.Groups[1].Value}, file kept.");
                        return;
                    }

                    var total = LengthTotal.Match(text);
                    var done = Transferred.Match(text);
                    if (!total.Success || !done.Success)
                        continue;

                    ulong totalBytes = Convert.ToUInt64(total.Groups[1].Value, 16);
                    ulong doneBytes = Convert.ToUInt64(done.Groups[1].Value, 16);

                    if (totalBytes > 0 && doneBytes >= totalBytes)
                    {
                        int preparing = ReadInt(PreparingPercent, text);
                        int localCopy = ReadInt(LocalCopyPercent, text);

                        if (!DownloadFinished)
                        {
                            DownloadFinished = true;
                            LastChange = DateTime.UtcNow;
                            LastState = (preparing, localCopy);
                            Log($"Task {taskId}: download finished, waiting for the PS4 to finish preparing (preparing={preparing}%, local_copy={localCopy}%).");
                        }
                        else if ((preparing, localCopy) != LastState)
                        {
                            LastState = (preparing, localCopy);
                            LastChange = DateTime.UtcNow;
                            Log($"Task {taskId}: preparing={preparing}%, local_copy={localCopy}%.");
                        }

                        bool PrepareDone = preparing >= 100;
                        bool Settled = DateTime.UtcNow - LastChange >= SettleTime;

                        if (PrepareDone || Settled)
                        {
                            if (File.Exists(localFile))
                            {
                                File.Delete(localFile);
                                Log($"Deleted {localFile} (task {taskId}: {(PrepareDone ? "preparing reached 100%" : "no changes for " + (int)SettleTime.TotalSeconds + "s after the download")}).");
                            }
                            return;
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (++failures >= 5)
                    {
                        Log($"Gave up watching task {taskId}: {ex.Message}");
                        return;
                    }
                }
            }
        }

        private static int ReadInt(Regex Pattern, string Text)
        {
            var m = Pattern.Match(Text);
            return m.Success && int.TryParse(m.Groups[1].Value, out int v) ? v : -1;
        }

        private static void Log(string message)
        {
            try
            {
                File.AppendAllText(Path.Combine(App.WorkingDirectory, "DPI-CLEANUP.log"),
                    $"{DateTime.Now:s} {message}{Environment.NewLine}");
            }
            catch { }
        }
    }
}
