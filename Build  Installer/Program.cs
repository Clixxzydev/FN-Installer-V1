using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using SharpCompress.Archives;
using SharpCompress.Common;
using System.Linq;

class Program
{
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
        "https://public.simplyblk.xyz/13.40.zip",
        "https://www.dropbox.com/scl/fi/i7rsxm5pkkn4svj4vtxf7/14.30.rar?rlkey=11rcx7jaoamn5up5b4bgkryvt&st=2hungogj&dl=1"
    };

    private static readonly string[] buildVersions = new string[]
    {
        "5.40",
        "7.00",
        "7.10",
        "7.20",
        "7.30",
        "7.40",
        "8.51",
        "9.10",
        "9.41",
        "11.31",
        "12.41",
        "13.40",
        "14.30"
    };

    private static readonly HttpClient httpClient = new HttpClient();

    static async Task Main(string[] args)
    {
        Console.WriteLine("Welcome to the build installer. Made by Clixxzy");

        while (true)
        {
            // Display file options
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

            // Download and extract selected files
            foreach (int index in selectedIndices)
            {
                await DownloadAndExtractFileAsync(fileUrls[index - 1], Path.Combine(downloadPath, buildVersions[index - 1]), buildVersions[index - 1]);
            }

            Console.WriteLine("All downloads and extractions are complete.");
            Console.WriteLine("Do you want to download more files? (Y/N)");
            string response = Console.ReadLine().Trim().ToUpper();
            if (response != "Y")
            {
                break;
            }
        }

        Console.WriteLine("Press any key to exit.");
        Console.ReadKey();
    }

    private static bool IsValidPath(string path)
    {
        try
        {
            Path.GetFullPath(path);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static List<int> ParseUserInput(string input)
    {
        List<int> result = new List<int>();
        string[] selections = input.Split(',');

        foreach (string selection in selections)
        {
            if (int.TryParse(selection.Trim(), out int index) && index > 0 && index <= fileUrls.Length)
            {
                result.Add(index);
            }
        }

        return result;
    }

    private static async Task DownloadAndExtractFileAsync(string url, string destinationFolder, string buildVersion)
    {
        try
        {
            Directory.CreateDirectory(destinationFolder);
            string fileName = Path.GetFileName(new Uri(url).LocalPath);
            string filePath = Path.Combine(destinationFolder, fileName);

            await DownloadFileAsync(url, filePath, buildVersion);
            await ExtractFileAsync(filePath, destinationFolder, buildVersion);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing {buildVersion}: {ex.Message}");
        }
    }

    private static async Task DownloadFileAsync(string url, string filePath, string buildVersion)
    {
        try
        {
            Console.WriteLine($"Starting download: {buildVersion}");
            using (var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
            {
                response.EnsureSuccessStatusCode();
                var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                var readBytes = 0L;

                using (var contentStream = await response.Content.ReadAsStreamAsync())
                using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                {
                    var buffer = new byte[8192];
                    var isMoreToRead = true;

                    do
                    {
                        var read = await contentStream.ReadAsync(buffer, 0, buffer.Length);
                        if (read == 0)
                        {
                            isMoreToRead = false;
                        }
                        else
                        {
                            await fileStream.WriteAsync(buffer, 0, read);
                            readBytes += read;
                            if (totalBytes != -1)
                            {
                                var percentage = (int)((double)readBytes / totalBytes * 100);
                                Console.Write($"\r{buildVersion} download progress: {percentage}%");
                            }
                        }
                    }
                    while (isMoreToRead);
                }
            }
            Console.WriteLine($"\nDownload completed: {buildVersion}");
        }
        catch (HttpRequestException httpEx)
        {
            Console.WriteLine($"\nError downloading file {buildVersion}: {httpEx.Message}");
            throw;
        }
    }

    private static async Task ExtractFileAsync(string sourceFile, string destinationFolder, string buildVersion)
    {
        Console.WriteLine($"Extracting {buildVersion}...");
        await Task.Run(() =>
        {
            try
            {
                using (var archive = ArchiveFactory.Open(sourceFile))
                {
                    var totalEntries = archive.Entries.Where(entry => !entry.IsDirectory).Count();
                    var extractedEntries = 0;

                    foreach (var entry in archive.Entries.Where(entry => !entry.IsDirectory))
                    {
                        entry.WriteToDirectory(destinationFolder, new ExtractionOptions()
                        {
                            ExtractFullPath = true,
                            Overwrite = true
                        });
                        extractedEntries++;
                        int percentage = (int)((double)extractedEntries / totalEntries * 100);
                        Console.Write($"\r{buildVersion} extraction progress: {percentage}%");
                    }
                }
                Console.WriteLine($"\nExtraction completed: {buildVersion}");

                // Remove the archive file after successful extraction
                File.Delete(sourceFile);
                Console.WriteLine($"Removed archive file: {Path.GetFileName(sourceFile)}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nError extracting file {buildVersion}: {ex.Message}");
                throw;
            }
        });
    }
}