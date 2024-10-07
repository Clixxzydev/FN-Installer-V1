using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using SharpCompress.Archives;
using SharpCompress.Common;
using System.Linq;
using Newtonsoft.Json;
using System.IO.Compression;
using System.Net;
using System.Threading;

namespace EasyInstallerV2
{
    class Program
    {
        // URLs and configurations for the first part
        private static readonly string[] fileUrls = new string[]
        {
            "https://public.simplyblk.xyz/5.40.rar",
            "https://public.simplyblk.xyz/7.00.rar",
            "https://public.simplyblk.xyz/7.10.rar",
            "https://public.simplyblk.xyz/7.20.rar",
            "https://public.simplyblk.xyz/7.30.zip",
            "https://public.simplyblk.xyz/7.40.rar",
            "https://public.simplyblk.xyz/8.51.rar",
            "https://public.simplyblk.xyz/9.10.rar",
            "https://public.simplyblk.xyz/9.41.rar",
            "https://public.simplyblk.xyz/11.31.rar",
            "https://public.simplyblk.xyz/Fortnite%2012.41.zip",
            "https://public.simplyblk.xyz/13.00.rar",
            "https://public.simplyblk.xyz/13.40.zip",
            "https://www.dropbox.com/scl/fi/i7rsxm5pkkn4svj4vtxf7/14.30.rar?rlkey=11rcx7jaoamn5up5b4bgkryvt&st=2hungogj&dl=1"
        };

        private static readonly string[] buildVersions = new string[]
        {
            "5.40", "7.00", "7.10", "7.20", "7.30", "7.40",
            "8.51", "9.10", "9.41", "11.31", "12.41", "13.00",
            "13.40", "14.30"
        };

        private static readonly HttpClient httpClient = new HttpClient();
        public const string BASE_URL = "https://manifest.simplyblk.xyz";
        private const int CHUNK_SIZE = 536870912 / 8;

        class ChunkedFile
        {
            public List<int> ChunksIds = new();
            public string File = string.Empty;
            public long FileSize = 0;
        }

        class ManifestFile
        {
            public string Name = string.Empty;
            public List<ChunkedFile> Chunks = new();
            public long Size = 0;
        }

        static async Task Main(string[] args)
        {
            Console.WriteLine("Welcome to the build installer. Made by Clixxzy");

            while (true)
            {
                Console.WriteLine("\nMain Menu:");
                Console.WriteLine("1. Web installer");
                Console.WriteLine("2. Manifest installer");
                Console.WriteLine("3. Exit");
                Console.Write("Choose an option: ");

                var choice = Console.ReadLine();

                if (choice == "1")
                {
                    await DownloadBuildsMenu();
                }
                else if (choice == "2")
                {
                    await EasyInstallerMenu();
                }
                else if (choice == "3")
                {
                    break;
                }
                else
                {
                    Console.WriteLine("Invalid option, please try again.");
                }
            }

            Console.WriteLine("Exiting the program. Press any key to close.");
            Console.ReadKey();
        }

        private static async Task DownloadBuildsMenu()
        {
            Console.WriteLine("Available Builds:");
            for (int i = 0; i < buildVersions.Length; i++)
            {
                Console.WriteLine($"{i + 1}. {buildVersions[i]}");
            }

            List<int> selectedIndices = new List<int>();
            while (selectedIndices.Count == 0)
            {
                Console.WriteLine("Select the build(s) you want to download (comma-separated numbers):");
                string input = Console.ReadLine();
                selectedIndices = ParseUserInput(input);

                if (selectedIndices.Count == 0)
                {
                    Console.WriteLine("Invalid input. Please try again.");
                }
            }

            string downloadPath;
            while (true)
            {
                Console.WriteLine("Enter the download path (leave blank for current directory):");
                downloadPath = Console.ReadLine().Trim();
                if (string.IsNullOrWhiteSpace(downloadPath))
                {
                    downloadPath = Directory.GetCurrentDirectory();
                    break;
                }
                else if (IsValidPath(downloadPath))
                {
                    break;
                }
                else
                {
                    Console.WriteLine("Invalid path. Please enter a valid directory path.");
                }
            }

            foreach (int index in selectedIndices)
            {
                await DownloadAndExtractFileAsync(fileUrls[index - 1], Path.Combine(downloadPath, buildVersions[index - 1]), buildVersions[index - 1]);
            }

            Console.WriteLine("All downloads and extractions are complete.");
            Console.WriteLine("Press any key to return to the main menu.");
            Console.ReadKey();
        }

        private static async Task EasyInstallerMenu()
        {
            var httpClient = new WebClient();
            List<string> versions = JsonConvert.DeserializeObject<List<string>>(httpClient.DownloadString(BASE_URL + "/versions.json"));

            Console.Clear();
            Console.Title = "Build installer Manifest Version";
            Console.Write("\n\nBuild installer Manifest Version\n\n");
            Console.WriteLine("\nAvailable manifests:");

            for (int i = 0; i < versions.Count; i++)
            {
                Console.WriteLine($" * [{i}] {versions[i]}");
            }

            Console.WriteLine($"\nTotal: {versions.Count}");
            Console.Write("Please enter the number before the Build Version to select it: ");
            var targetVersionStr = Console.ReadLine();
            var targetVersionIndex = 0;

            try
            {
                targetVersionIndex = int.Parse(targetVersionStr);
            }
            catch (Exception)
            {
                return; // Go back to the main menu
            }

            if (!(targetVersionIndex >= 0 && targetVersionIndex < versions.Count))
                return; // Go back to the main menu

            var targetVersion = versions[targetVersionIndex].Split("-")[1];
            var manifest = JsonConvert.DeserializeObject<ManifestFile>(httpClient.DownloadString(BASE_URL + $"/{targetVersion}/{targetVersion}.manifest"));

            Console.Write("Please enter a game folder location: ");
            var targetPath = Console.ReadLine();
            Console.Write("\n");

            await Download(manifest, targetVersion, targetPath);
        }

        private static async Task Download(ManifestFile manifest, string version, string resultPath)
        {
            long totalBytes = manifest.Size;
            long completedBytes = 0;
            int progressLength = 0;

            if (!Directory.Exists(resultPath))
                Directory.CreateDirectory(resultPath);

            SemaphoreSlim semaphore = new SemaphoreSlim(12);

            await Task.WhenAll(manifest.Chunks.Select(async chunkedFile =>
            {
                await semaphore.WaitAsync();

                try
                {
                    WebClient httpClient = new WebClient();
                    string outputFilePath = Path.Combine(resultPath, chunkedFile.File);
                    var fileInfo = new FileInfo(outputFilePath);

                    if (File.Exists(outputFilePath) && fileInfo.Length == chunkedFile.FileSize)
                    {
                        completedBytes += chunkedFile.FileSize;
                        semaphore.Release();
                        return;
                    }

                    Directory.CreateDirectory(Path.GetDirectoryName(outputFilePath));

                    using (FileStream outputStream = File.OpenWrite(outputFilePath))
                    {
                        foreach (int chunkId in chunkedFile.ChunksIds)
                        {
                        retry:
                            try
                            {
                                string chunkUrl = BASE_URL + $"/{version}/" + chunkId + ".chunk";
                                var chunkData = await httpClient.DownloadDataTaskAsync(chunkUrl);

                                byte[] chunkDecompData = new byte[CHUNK_SIZE + 1];
                                int bytesRead;
                                long chunkCompletedBytes = 0;

                                MemoryStream memoryStream = new MemoryStream(chunkData);
                                using (GZipStream decompressionStream = new GZipStream(memoryStream, CompressionMode.Decompress))
                                {
                                    while ((bytesRead = await decompressionStream.ReadAsync(chunkDecompData, 0, chunkDecompData.Length)) > 0)
                                    {
                                        await outputStream.WriteAsync(chunkDecompData, 0, bytesRead);
                                        Interlocked.Add(ref completedBytes, bytesRead);
                                        Interlocked.Add(ref chunkCompletedBytes, bytesRead);

                                        double progress = (double)completedBytes / totalBytes * 100;
                                        string progressMessage = $"\rDownloaded: {FormatBytesWithSuffix(completedBytes)} / {FormatBytesWithSuffix(totalBytes)} ({progress:F2}%)";

                                        int padding = progressLength - progressMessage.Length;
                                        if (padding > 0)
                                            progressMessage += new string(' ', padding);

                                        Console.Write(progressMessage);
                                        progressLength = progressMessage.Length;
                                    }
                                }
                            }
                            catch (Exception)
                            {
                                goto retry;
                            }
                        }
                    }
                }
                finally
                {
                    semaphore.Release();
                }
            }));

            Console.WriteLine("\n\nFinished Downloading.\nPress any key to return to the main menu!");
            Console.ReadKey();
        }

        private static async Task DownloadAndExtractFileAsync(string url, string outputDirectory, string version)
        {
            using (HttpClient client = new HttpClient())
            {
                var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                using (var stream = await response.Content.ReadAsStreamAsync())
                {
                    using (var archive = ArchiveFactory.Open(stream))
                    {
                        foreach (var entry in archive.Entries.Where(e => !e.IsDirectory))
                        {
                            var outputPath = Path.Combine(outputDirectory, entry.Key);
                            entry.WriteToFile(outputPath, new ExtractionOptions() { Overwrite = true });
                        }
                    }
                }
            }
        }

        private static List<int> ParseUserInput(string input)
        {
            return input.Split(',')
                .Select(i => int.TryParse(i.Trim(), out int index) ? index : -1)
                .Where(i => i > 0 && i <= buildVersions.Length)
                .ToList();
        }

        private static bool IsValidPath(string path)
        {
            try
            {
                var fullPath = Path.GetFullPath(path);
                return !string.IsNullOrWhiteSpace(fullPath);
            }
            catch
            {
                return false;
            }
        }

        private static string FormatBytesWithSuffix(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int i = 0;

            while (bytes >= 1024 && i < suffixes.Length - 1)
            {
                bytes /= 1024;
                i++;
            }

            return $"{bytes} {suffixes[i]}";
        }
    }
}
