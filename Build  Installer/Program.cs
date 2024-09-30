using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.IO.Compression;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Select a file to download:");
        Console.WriteLine("1. 8.51");
        Console.WriteLine("2. 9.10");
        Console.WriteLine("3. 14.30 NOT WORKING");
        int choice = int.Parse(Console.ReadLine());

        string[] urls = {
            "https://public.simplyblk.xyz/8.51.rar",
            "https://public.simplyblk.xyz/9.10.rar",
            "https://drive.google.com/uc?export=download&id=1Ah0IiQrKaMUEmXCFTGlVemR3C3hDifJH"
        };

        Console.WriteLine("Enter the download directory:");
        string downloadDirectory = Console.ReadLine();

        string filePath = Path.Combine(downloadDirectory, $"file{choice}.rar");
        await DownloadFileAsync(urls[choice - 1], filePath);
        ExtractRar(filePath);
    }

    static async Task DownloadFileAsync(string url, string filePath)
    {
        using (WebClient client = new WebClient())
        {
            client.DownloadProgressChanged += (s, e) =>
            {
                Console.WriteLine($"Download progress: {e.ProgressPercentage}%");
            };

            await client.DownloadFileTaskAsync(new Uri(url), filePath);
        }
    }

    static void ExtractRar(string filePath)
    {
        string extractPath = Path.Combine(Path.GetDirectoryName(filePath), "extracted");
        System.IO.Compression.ZipFile.ExtractToDirectory(filePath, extractPath);
        Console.WriteLine($"Files extracted to: {extractPath}");
    }
}
