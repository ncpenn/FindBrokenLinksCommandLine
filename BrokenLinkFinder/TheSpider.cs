using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using HtmlAgilityPack;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;

namespace BrokenLinkFinder
{
    class TheSpider
    {
        private List<LinkInfo> _internalLinks;
        public List<LinkInfo> ExternalBrokenLinks;
        public string HostOfSiteSpidered;

        public TheSpider(string website)
        {
            HostOfSiteSpidered = BuildHost(website);
            _internalLinks = new List<LinkInfo>();
            ExternalBrokenLinks = new List<LinkInfo>();
        }

        public void SpiderTheWebsite(string website)
        {
            if (IsValidUrl(website))
            {
                var nodes = new HtmlWeb().Load(website).DocumentNode.SelectNodes("//a/@href");
                BuildLinkLists(nodes, website);
                var linkInfo = _internalLinks.FirstOrDefault(p => p.AlreadySpidered == false);

                if (linkInfo != null)
                {
                    linkInfo.AlreadySpidered = true;
                    var empty = string.Empty;
                    SpiderTheWebsite(linkInfo.Link);
                }
            }
        }

        #region Private Methods

        private bool IsValidUrl(string website)
        {
            Uri uriResult;
            bool isValid = Uri.TryCreate(website, UriKind.Absolute, out uriResult)
                            && (uriResult.Scheme == Uri.UriSchemeHttp
                                || uriResult.Scheme == Uri.UriSchemeHttps);
            if (uriResult != null && (uriResult.IsFile || !uriResult.IsWellFormedOriginalString()) || !isValid)
            {
                return false;
            }
            return true;
        }

        private void BuildLinkLists(IEnumerable<HtmlNode> nodes, string website)
        {
            if (nodes != null && nodes.Any())
            {
                foreach (var node in nodes)
                {
                    var url = node.Attributes.FirstOrDefault(a => a.Name == "href").Value;
                    var absoluteUrl = MakeUrlAbsolute(url, website);
                    if (IsValidUrl(absoluteUrl))
                    {
                        AddLinkToCorrectList(website, absoluteUrl);
                    }
                }
            }
        }

        private void AddLinkToCorrectList(string website, string url)
        {
            var host = BuildHost(website);
            var linkInfo = new LinkInfo { Link = url, PageTheLinkIsOn = website, AlreadySpidered = false };
            var internallinkStrs = _internalLinks.Select(p => p.Link);
            var externallinkStrs = ExternalBrokenLinks.Select(p => p.Link);

            Console.WriteLine(url);

            if (url.IndexOf(host) != -1)
            {
                if (!internallinkStrs.Contains(url))
                {
                    _internalLinks.Add(linkInfo);
                }
            }
            else
            {
                if (!externallinkStrs.Contains(url))
                {
                    string statusCode;
                    if (!IsLinkWorking(url, out statusCode))
                    {
                        linkInfo.Status = statusCode;
                        ExternalBrokenLinks.Add(linkInfo);
                    }                 
                }
            }
        }

        private string BuildHost(string website)
        {
            var host = new Uri(website, UriKind.Absolute).Host;
            if (host.Contains("www."))
            {
                host = host.Substring(4);
            }
            return host;
        }

        private string MakeUrlAbsolute(string url, string website)
        {
            var isWwwDotInString = url.Contains("www.");

            string baseUrl;
            var regex = new Regex(@"\.[a-z]+/");
            var regexApplied = regex.Match(website);
            var index = regexApplied.Index;
            var value = regexApplied.Value;

            if (!string.IsNullOrEmpty(value))
            {
                baseUrl = website.Substring(0, index) + value;
            }
            else
            {
                baseUrl = website;
            }

            if (!isWwwDotInString)
            {
                url = baseUrl + url;
            }
            return url;
        }

        private bool IsLinkWorking(string link, out string status)
        {
            status = string.Empty;
            try
            {
                var request = WebRequest.Create(link) as HttpWebRequest;
                request.Timeout = 5000;

                var response = request.GetResponse() as HttpWebResponse;
                int statusCode = (int)response.StatusCode;

                if (statusCode >= 500 && statusCode <= 510)
                {
                    status = statusCode.ToString();
                }
            }
            catch (WebException ex)
            {
                status = ex.Status.ToString();
            }
            catch (Exception ex)
            {
                status = ex.Message;
            }

            if (string.IsNullOrEmpty(status))
            {
                return true;
            }
            return false;
        }
        #endregion
    }
}
