using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

class Program
{
    // Predefined file URLs
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

    private static readonly string[] fileNames = new string[]
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

    static async Task Main(string[] args)
    {
        Console.WriteLine("Welcome to the build installer. Made by clixxzy");

        // Display file options
        for (int i = 0; i < fileNames.Length; i++)
        {
            Console.WriteLine($"{i + 1}. {fileNames[i]}");
        }

        Console.WriteLine("Select the build you want to download (comma-separated numbers):");
        string input = Console.ReadLine();
        string[] selectedFiles = input.Split(',');

        Console.WriteLine("Enter the download path (leave blank for current directory):");
        string downloadPath = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(downloadPath))
        {
            downloadPath = Directory.GetCurrentDirectory();
        }

        // Download selected files
        foreach (string file in selectedFiles)
        {
            if (int.TryParse(file.Trim(), out int fileIndex) && fileIndex > 0 && fileIndex <= fileUrls.Length)
            {
                string url = fileUrls[fileIndex - 1];
                string fileName = Path.Combine(downloadPath, $"{fileNames[fileIndex - 1]}.zip");
                await DownloadFileAsync(url, fileName);
            }
            else
            {
                Console.WriteLine($"Invalid selection: {file}. Please select a valid file number.");
            }
        }

        Console.WriteLine("All downloads are complete. Press any key to exit.");
        Console.ReadKey();
    }

    private static async Task DownloadFileAsync(string url, string filePath)
    {
        using (WebClient webClient = new WebClient())
        {
            try
            {
                Console.WriteLine($"Starting download: {filePath}");
                webClient.DownloadProgressChanged += (s, e) =>
                {
                    Console.Write($"\rDownload progress: {e.ProgressPercentage}%");
                };

                await webClient.DownloadFileTaskAsync(new Uri(url), filePath);
                Console.WriteLine($"\nDownload completed: {filePath}");
            }
            catch (WebException webEx)
            {
                Console.WriteLine($"\nError downloading file: {webEx.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nAn unexpected error occurred: {ex.Message}");
            }
        }
    }
}
