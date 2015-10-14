using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BrokenLinkFinder
{
    class Entry
    {
        static void Main(string[] args)
        {
            var Program = new Entry();
            Program.Start();
            
            Console.WriteLine(Text.GetText("end"));
            Console.ReadKey();
        }

        private void Start()
        {         
            Console.Write(Text.GetText("initial-ask"));

            var websiteToCheckForBrokenLinks = Console.ReadLine();
            var spider = new TheSpider(websiteToCheckForBrokenLinks);

            spider.SpiderTheWebsite(websiteToCheckForBrokenLinks);
            WriteDataToFile(spider.ExternalBrokenLinks, spider.HostOfSiteSpidered);
        }

        private void WriteDataToFile(List<LinkInfo> listOfLinksWithInfo, string host)
        {
            var filePath = "C:\\brokenlinks-" + host + ".txt";
            using (var writer = new StreamWriter(filePath))
            {
                var text = new StringBuilder();
                foreach (var item in listOfLinksWithInfo)
                {
                    text.AppendLine(item.PageTheLinkIsOn);
                    text.AppendLine(item.Link);
                    text.AppendLine(item.Status);
                    text.AppendLine("------");
                    text.AppendLine();
                    writer.WriteLine(text);
                }
            }
        }
    }
}
