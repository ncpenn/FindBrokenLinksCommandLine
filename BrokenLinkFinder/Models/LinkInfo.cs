using System.Net;

namespace BrokenLinkFinder
{
    class LinkInfo
    {
        public string Link { get; set; }
        public string PageTheLinkIsOn { get; set; }
        public bool AlreadySpidered { get; set; }
        public string Status { get; set; }
    }
}
