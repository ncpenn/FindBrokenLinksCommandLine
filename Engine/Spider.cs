using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Cache;
using System.Text.RegularExpressions;
using System.Threading;
using Engine.Models;
using HtmlAgilityPack;

namespace Engine
{
    public class Spider :IDisposable
    {
        private readonly CountdownEvent _countdownEvent = new CountdownEvent(1);
        private string _hostOfOriginalUrl;
        private string _originalUrl;
        private readonly HashSet<string> _brokenLinkList = new HashSet<string>();
        private readonly HashSet<string> _linksAlreadyCrawled = new HashSet<string>();
        public delegate void StatusUpdate(string status, bool isBrokenMsg);
        public event StatusUpdate LogSpidering;

        public static bool IsValidUrl(string absoulteUrl)
        {
            Uri uriResult;
            if (Uri.TryCreate(absoulteUrl, UriKind.Absolute, out uriResult))
            {
                return uriResult.IsWellFormedOriginalString() 
                    && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps) 
                    && !uriResult.IsFile;
            }
            return false;
        }

        public void Start(string siteToSpider)
        {
            _hostOfOriginalUrl = BuildHost(siteToSpider);
            _originalUrl = siteToSpider;
            //ThreadPool.SetMaxThreads(10, 10);
            CrawlPageForLinks(siteToSpider);
            _countdownEvent.Signal();
            _countdownEvent.Wait();
        }

        #region Private Methods

        private void CrawlPageForLinks(string url)
        {
            _countdownEvent.AddCount();
            ThreadPool.QueueUserWorkItem(delegate
            {
                var hw = new HtmlWeb();
                //TODO retry loop on failure to load document
                var doc = hw.Load(url);
                foreach (var link in (IEnumerable<HtmlNode>)doc.DocumentNode.SelectNodes("//a[@href]") ?? new List<HtmlNode>())
                {
                    ProcessLink(link, url);
                }
                _countdownEvent.Signal();
            });
        }

        private void ProcessLink(HtmlNode link, string parentPage)
        {
            var linkText = link.Attributes.FirstOrDefault(
                a => string.Equals(a.Name, "href", StringComparison.InvariantCultureIgnoreCase))?.Value;
            linkText = RemoveEscapeChars(linkText);
            if (string.IsNullOrWhiteSpace(linkText)) return;
            linkText = IsExternalLink(linkText) ? linkText : MakeUrlAbsolute(linkText);
            if (IsValidUrl(linkText) && _linksAlreadyCrawled.Add(linkText))
            {
                var response = IsBrokenLink(linkText);
                if (response.IsLinkBroken)
                {
                    if (_brokenLinkList.Add(linkText))
                    {
                        LogSpidering?.Invoke($"{linkText} is broken. Status code: {response.Status}. On page {parentPage}", true);
                    }
                }
                else if (!IsExternalLink(linkText))
                {
                    LogSpidering?.Invoke($"{linkText} is ok", false);
                    CrawlPageForLinks(linkText);
                }
                else
                {
                    LogSpidering?.Invoke($"{linkText} is an external link and is ok", false);
                }
            }
        }

        private string RemoveEscapeChars(string url)
        {
            if (!string.IsNullOrWhiteSpace(url))
            {
                var endsWithSlash = url.EndsWith("/", StringComparison.Ordinal);
                if (endsWithSlash)
                {
                    url = url.Substring(0, url.Length - 1);
                }
                var questionIndex = url.IndexOf("?", StringComparison.Ordinal);
                url = url.Substring(0, questionIndex == -1 ? url.Length : questionIndex);
                var andIndex = url.IndexOf("&", StringComparison.Ordinal);
                url = url.Substring(0, andIndex == -1 ? url.Length : andIndex);
                var hashIndex = url.IndexOf("#", StringComparison.Ordinal);
                url = url.Substring(0, hashIndex == -1 ? url.Length : hashIndex);
                return url;
            }
            return string.Empty;
        }

        private bool IsExternalLink(string url)
        {
            try
            {
                var link = new Uri(url).Host;
                link = link.Replace("www.", string.Empty);
                return !link.StartsWith(_hostOfOriginalUrl);
            }
            catch (FormatException)
            {
                return false;
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

        private string MakeUrlAbsolute(string url)
        {
            var isUrlAbsolute = url.StartsWith("www.") || url.StartsWith("http");

            string baseUrl;
            var regex = new Regex(@"\.[a-z]+/");
            var regexApplied = regex.Match(_originalUrl);
            var index = regexApplied.Index;
            var value = regexApplied.Value;

            if (!string.IsNullOrEmpty(value))
            {
                baseUrl = _originalUrl.Substring(0, index) + value;
            }
            else
            {
                baseUrl = _originalUrl;
            }

            if (!isUrlAbsolute)
            {
                url = url.StartsWith("/") && baseUrl.EndsWith("/") ? baseUrl.Substring(0, baseUrl.Length - 1) + url : baseUrl + url;
            }
            return url;
        }

        private LinkResponse IsBrokenLink(string link)
        {
            var linkResponse = new LinkResponse();
            int statusCode;
            try
            {
                var doc = new HtmlWeb().Load(link);
                var request = (HttpWebRequest)WebRequest.Create(link);
                ServicePointManager.ServerCertificateValidationCallback =
                    (sender, certificate, chain, errors) => true;
                request.Headers.Add("Accept-Language", " en-US");
                request.Accept = " text/html, application/xhtml+xml, */*";
                request.UserAgent = "Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.1; Trident/5.0)";
                request.Timeout = 10000;
                request.AllowAutoRedirect = true;
                request.MaximumAutomaticRedirections = 5;
                request.CachePolicy = new RequestCachePolicy(RequestCacheLevel.NoCacheNoStore);

                using (var response = (HttpWebResponse)request.GetResponse())
                {
                    statusCode = (int)response.StatusCode;
                    if (statusCode >= 500 && statusCode <= 510)
                    {
                        linkResponse.IsLinkBroken = true;
                        linkResponse.Status = statusCode.ToString();
                        return linkResponse;
                    }
                }
            }
            catch (Exception ex)
            {
                var webEx = ex as WebException; 
                linkResponse.IsLinkBroken = true;
                linkResponse.Status = webEx?.Status.ToString() ?? ex.Message;
                return linkResponse;
            }
            linkResponse.Status = statusCode.ToString();
            return linkResponse;
        }
        #endregion

        public void Dispose()
        {
            _countdownEvent.Dispose();
        }
    }
}
