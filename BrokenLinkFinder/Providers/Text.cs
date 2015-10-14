using System.Collections.Generic;

namespace BrokenLinkFinder
{
    static class Text
    {
        private static Dictionary<string, string> _textResources = new Dictionary<string, string>();

        static Text()
        {
            _textResources.Add("initial-ask", "Enter a website address: ");
            _textResources.Add("website-prefix", "http://");
            _textResources.Add("website-prefix-ssl", "https://");
            _textResources.Add("invalid-url-warning", "The URL you entered is invalid");
            _textResources.Add("end", "Press Enter to end program.");
        }

        public static string GetText(string key)
        {
            string value;
            if (_textResources.TryGetValue(key, out value))
            {
                return value;
            }
            return string.Empty;
        }
    }
}
