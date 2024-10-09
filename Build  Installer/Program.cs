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
        // URLs for downloading files
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

        // Method to start the main menu
        static async Task Main(string[] args)
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine("Welcome To the build installer Made by Clixxzy:");
                Console.WriteLine("1. Web Installer");
                Console.WriteLine("2. Manifest Installer");
                Console.WriteLine("3. Exit");
                Console.Write("Choose an option (1, 2, or 3): ");
                string choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        // Call the functionality from Program (1).cs
                        await WebInstallerMenu();
                        break;

                    case "2":
                        // Call the functionality from Program.cs
                        await ManifestInstallerMenu();
                        break;

                    case "3":
                        Console.WriteLine("Exiting the application. Goodbye!");
                        return;

                    default:
                        Console.WriteLine("Invalid choice. Please try again.");
                        break;
                }
            }
        }

        // Web Installer (from Program (1).cs)
        private static async Task WebInstallerMenu()
        {
            Console.Clear();
            Console.WriteLine("Web Installer");

            // Original logic from Program (1).cs DownloadBuildsMenu
            await DownloadBuildsMenu();

            // Return to main menu after execution
            ReturnToMainMenu();
        }

        // Manifest Installer (from Program.cs)
        private static async Task ManifestInstallerMenu()
        {
            Console.Clear();
            Console.WriteLine("Manifest Installer");

            // Original logic from Program.cs Main method
            ManifestInstallerMain();

            // Return to main menu after execution
            ReturnToMainMenu();
        }

        // Method to return to main menu after download or extraction is complete
        private static void ReturnToMainMenu()
        {
            Console.WriteLine("\nPress any key to return to the main menu.");
            Console.ReadKey();
        }

        // Original logic from Program (1).cs for web installer
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
                await DownloadAndExtractFileAsync(fileUrls[index - 1], downloadPath, buildVersions[index - 1]);
            }

            Console.WriteLine("All downloads and extractions are complete.");
        }

        // Original logic from Program.cs for manifest installer
        private static void ManifestInstallerMain()
        {
            var httpClient = new WebClient();
            List<string> versions = JsonConvert.DeserializeObject<List<string>>(httpClient.DownloadString(BASE_URL + "/versions.json"));

            Console.Clear();
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
            catch (Exception ex)
            {
                return; // Go back to the main menu
            }

            if (!(targetVersionIndex >= 0 && targetVersionIndex < versions.Count))
                return; // Go back to the main menu

            var targetVersion = versions[targetVersionIndex].Split("-")[1];
            var manifest = JsonConvert.DeserializeObject<ManifestFile>(httpClient.DownloadString(BASE_URL + $"/{targetVersion}/{targetVersion}.manifest"));

            string targetPath;
            while (true)
            {
                Console.WriteLine("Enter the game folder location (leave blank for current directory):");
                targetPath = Console.ReadLine().Trim();
                if (string.IsNullOrWhiteSpace(targetPath))
                {
                    targetPath = Directory.GetCurrentDirectory();
                    break;
                }
                else if (IsValidPath(targetPath))
                {
                    break;
                }
                else
                {
                    Console.WriteLine("Invalid path. Please enter a valid directory path.");
                }
            }

            // Call the correct Download method with the manifest parameters
            DownloadManifest(manifest, targetVersion, targetPath).GetAwaiter().GetResult();
        }

        // Fixed: Method definition for the missing "Download" method for manifest files
        private static async Task DownloadManifest(ManifestFile manifest, string version, string resultPath)
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

            Console.WriteLine("\n\nFinished Downloading.\nPress any key to return to the main menu.");
            Console.ReadKey();
        }

        // Helper method to format bytes with a suffix
        private static string FormatBytesWithSuffix(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int i = 0;

            double dblSByte = bytes;
            while (bytes >= 1024 && i < suffixes.Length - 1)
            {
                dblSByte = bytes / 1024.0;
                bytes /= 1024;
                i++;
            }

            return $"{dblSByte:0.##} {suffixes[i]}";
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

        // Updated method to handle streaming the download to avoid buffer size issues
        private static async Task DownloadAndExtractFileAsync(string url, string outputDirectory, string version)
        {
            using (HttpClient client = new HttpClient())
            {
                // Ensure the output directory exists
                if (!Directory.Exists(outputDirectory))
                {
                    Directory.CreateDirectory(outputDirectory);
                }

                // Get the file info from the HTTP headers to determine total file size
                var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead); // Stream the download
                response.EnsureSuccessStatusCode();
                long totalBytes = response.Content.Headers.ContentLength ?? -1; // Get total file size in bytes

                // Set the final output path for the downloaded file
                string outputFilePath = Path.Combine(outputDirectory, $"{version}.zip"); // Change to .rar if needed

                // Initialize progress tracking for download
                long completedBytes = 0;
                int progressLength = 0;

                using (var stream = await response.Content.ReadAsStreamAsync())
                using (var fileStream = new FileStream(outputFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                {
                    byte[] buffer = new byte[8192]; // Read in chunks of 8 KB
                    int bytesRead;

                    while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        // Write to the file
                        await fileStream.WriteAsync(buffer, 0, bytesRead);
                        completedBytes += bytesRead;

                        // Calculate and display the download progress
                        double progress = (double)completedBytes / totalBytes * 100;
                        string progressMessage = $"\rDownloading: {FormatBytesWithSuffix(completedBytes)} / {FormatBytesWithSuffix(totalBytes)} ({progress:F2}%)";

                        // Ensure consistent length in progress display
                        int padding = progressLength - progressMessage.Length;
                        if (padding > 0)
                            progressMessage += new string(' ', padding);

                        Console.Write(progressMessage);
                        progressLength = progressMessage.Length;
                    }
                }

                // Extraction Logic with Progress Tracking
                try
                {
                    using (var archive = ArchiveFactory.Open(outputFilePath))
                    {
                        // Count the total number of files and directories
                        var totalEntries = archive.Entries.Count();
                        int currentEntry = 0;

                        foreach (var entry in archive.Entries.Where(e => !e.IsDirectory))
                        {
                            // Extract the current entry
                            try
                            {
                                var outputPath = Path.Combine(outputDirectory, entry.Key);

                                // Ensure the directory exists
                                string directory = Path.GetDirectoryName(outputPath);
                                if (!Directory.Exists(directory))
                                {
                                    Directory.CreateDirectory(directory);
                                }

                                // Extract the file
                                entry.WriteToFile(outputPath, new ExtractionOptions() { Overwrite = true });

                                // Update the progress
                                currentEntry++;
                                double extractionProgress = (double)currentEntry / totalEntries * 100;

                                // Clear the current console line to avoid overlap
                                Console.SetCursorPosition(0, Console.CursorTop);
                                Console.Write(new string(' ', Console.WindowWidth)); // Clear current line
                                Console.SetCursorPosition(0, Console.CursorTop);   // Reset cursor

                                Console.Write($"Extracting: {extractionProgress:F2}%");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"\nError extracting {entry.Key}: {ex.Message}");
                            }
                        }
                    }

                    Console.WriteLine("\nDownload and extraction complete.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\nError extracting files: {ex.Message}");
                }
            }
        }
    }
}
