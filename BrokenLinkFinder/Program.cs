using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Engine;

namespace BrokenLinkFinder
{
    class Program
    {
        static readonly object Object = new object();
        private static string _fileNameUniqifier;

        private static void Main()
        {
            Console.WriteLine("Enter absolute URL of website (must strart with 'http://'): ");
            var websiteToCheckForBrokenLinks = "http://www.successfulblogging.com/resources/";
            //var websiteToCheckForBrokenLinks = "http://nathanpennington.blogspot.com";
            if (Spider.IsValidUrl(websiteToCheckForBrokenLinks))
            {
                using (var spider = new Spider())
                {
                    spider.LogSpidering += HandleProgressLogs;
                    Directory.CreateDirectory(@"c:\temp");
                    _fileNameUniqifier = $"{new Uri(websiteToCheckForBrokenLinks).Host} - {DateTime.Now.ToString("yyMMddhhmmss")}";
                    spider.Start(websiteToCheckForBrokenLinks);
                }
            }
        }

        private static void HandleProgressLogs(string status, bool isBrokenMessage)
        {
            if (isBrokenMessage)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(status);
                lock (Object)
                {
                    File.AppendAllLines($@"c:\temp\brokenlinks-{_fileNameUniqifier}.txt", new List<string> { status });
                }
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine(status);
            }
        }
    }
}
