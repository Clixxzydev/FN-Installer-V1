using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.IO.Compression;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Select a Build to download:");
        Console.WriteLine("1. 7.40");
        Console.WriteLine("2. 8.51");
        Console.WriteLine("3. 9.10");
        Console.WriteLine("4. 9.51");
        Console.WriteLine("5. 11.31");
        Console.WriteLine("6. 13.40");
        Console.WriteLine("7. 14.30 NOT WORKING");
        int choice = int.Parse(Console.ReadLine());

        string[] urls = {
            "https://public.simplyblk.xyz/7.40.rar",
            "https://public.simplyblk.xyz/8.51.rar",
            "https://public.simplyblk.xyz/7.40.rar",
            "https://public.simplyblk.xyz/9.10.rar",
            "https://public.simplyblk.xyz/11.31.rar",
            "https://public.simplyblk.xyz/13.40.zip",
            "https://drive.google.com/uc?export=download&id=1Ah0IiQrKaMUEmXCFTGlVemR3C3hDifJH"
        };

        Console.WriteLine("Enter the download directory:");
        string downloadDirectory = Console.ReadLine();

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

}
