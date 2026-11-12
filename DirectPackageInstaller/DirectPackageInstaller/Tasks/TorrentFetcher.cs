using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using DirectPackageInstaller.Others;
using MonoTorrent;
using MonoTorrent.Client;

namespace DirectPackageInstaller.Tasks
{
    /// <summary>
    /// Downloads the .pkg files of a torrent (magnet link, .torrent file or
    /// URL to a .torrent) and returns their local paths, so they can be
    /// loaded by DirectPackageInstaller like any other local file.
    /// </summary>
    public static class TorrentFetcher
    {
        private static ClientEngine? Engine;
        private static TorrentManager? Current;

        public static bool IsTorrentSource(string? Source)
        {
            if (string.IsNullOrWhiteSpace(Source))
                return false;

            Source = Source.Trim();

            if (Source.StartsWith("magnet:?", StringComparison.OrdinalIgnoreCase))
                return true;

            string Path = Source;
            if (Source.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                try { Path = new Uri(Source).AbsolutePath; }
                catch { return false; }
            }

            return Path.EndsWith(".torrent", StringComparison.OrdinalIgnoreCase);
        }

        public static async Task<string[]> DownloadPackages(string Source, Func<string, Task> SetStatus)
        {
            Source = Source.Trim();

            string BaseDir = System.IO.Path.Combine(App.WorkingDirectory, "Torrents");
            string SaveDir = System.IO.Path.Combine(BaseDir, "Downloads");
            Directory.CreateDirectory(SaveDir);

            if (Engine == null)
            {
                var Builder = new EngineSettingsBuilder
                {
                    CacheDirectory = System.IO.Path.Combine(BaseDir, "Cache")
                };
                Engine = new ClientEngine(Builder.ToSettings());
            }

            //Release the previous torrent, if any
            if (Current != null)
            {
                try
                {
                    await Current.StopAsync();
                    await Engine.RemoveAsync(Current);
                }
                catch { }
                Current = null;
            }

            await SetStatus("Torrent: adding...");

            TorrentManager Manager;

            if (Source.StartsWith("magnet:?", StringComparison.OrdinalIgnoreCase))
            {
                var Magnet = MagnetLink.Parse(Source);
                Manager = await Engine.AddAsync(Magnet, SaveDir);
            }
            else
            {
                string TorrentPath = Source;

                if (Source.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    using var Http = new HttpClient();
                    var Data = await Http.GetByteArrayAsync(Source);
                    TorrentPath = System.IO.Path.Combine(BaseDir, "download.torrent");
                    await File.WriteAllBytesAsync(TorrentPath, Data);
                }

                var Parsed = MonoTorrent.Torrent.Load(TorrentPath);
                Manager = await Engine.AddAsync(Parsed, SaveDir);
            }

            Current = Manager;

            await Manager.StartAsync();

            if (!Manager.HasMetadata)
            {
                await SetStatus("Torrent: waiting for metadata...");

                using var MetaTimeout = new CancellationTokenSource(TimeSpan.FromMinutes(5));
                try
                {
                    await Manager.WaitForMetadataAsync(MetaTimeout.Token);
                }
                catch (OperationCanceledException)
                {
                    await Manager.StopAsync();
                    throw new AbortException("Torrent: no metadata received after 5 minutes (no peers?)");
                }
            }

            var Pkgs = Manager.Files
                .Where(x => x.Path.EndsWith(".pkg", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (Pkgs.Count == 0)
            {
                await Manager.StopAsync();
                throw new AbortException("The torrent doesn't contain any .pkg file");
            }

            //Download only the .pkg files
            foreach (var Entry in Manager.Files)
            {
                if (!Pkgs.Contains(Entry))
                    await Manager.SetFilePriorityAsync(Entry, Priority.DoNotDownload);
            }

            long Total = Pkgs.Sum(x => x.Length);

            while (!Pkgs.All(x => x.BitField.AllTrue))
            {
                if (Manager.Error != null)
                {
                    var Reason = Manager.Error.Exception?.Message ?? Manager.Error.Reason.ToString();
                    await Manager.StopAsync();
                    throw new AbortException($"Torrent error: {Reason}");
                }

                double Done = Pkgs.Sum(x => x.Length * (x.BitField.PercentComplete / 100.0));
                double Percent = Total > 0 ? Done / Total * 100.0 : 0;
                double Speed = Manager.Monitor.DownloadRate / 1048576.0;

                await SetStatus($"Torrent: {Percent:0.0}% - {Speed:0.0} MB/s - {Manager.OpenConnections} peers");
                await Task.Delay(1000);
            }

            await Manager.StopAsync();

            //Incomplete files may be moved to the final path when the download ends
            for (int i = 0; i < 30 && !Pkgs.All(x => System.IO.File.Exists(x.DownloadCompleteFullPath)); i++)
                await Task.Delay(1000);

            return Pkgs.Select(x => x.DownloadCompleteFullPath).ToArray();
        }
    }
}
