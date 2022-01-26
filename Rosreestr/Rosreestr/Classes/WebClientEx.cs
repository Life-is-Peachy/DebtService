using System;
using System.Net;

namespace Rosreestr.Sessions
{
    public class WebClientEx : WebClient
    {
        public CookieContainer CookieContainer { get; private set; }


        public WebClientEx()
        {
            CookieContainer = new CookieContainer();
        }

        public WebClientEx(CookieContainer container)
        {
            CookieContainer = container;
        }

        protected override WebRequest GetWebRequest(Uri address)
        {
            WebRequest r = base.GetWebRequest(address);
            HttpWebRequest request = r as HttpWebRequest;

            if (request != null)
            {
                request.CookieContainer = CookieContainer;
            }

            return r;
        }

        protected override WebResponse GetWebResponse(WebRequest request, IAsyncResult result)
        {
            WebResponse response = base.GetWebResponse(request, result);
            ReadCookies(response);

            return response;
        }

        protected override WebResponse GetWebResponse(WebRequest request)
        {
            WebResponse response = base.GetWebResponse(request);
            ReadCookies(response);

            return response;
        }

        private void ReadCookies(WebResponse r)
        {
            HttpWebResponse response = r as HttpWebResponse;
            if (response != null)
            {
                CookieCollection cookies = response.Cookies;
                CookieContainer.Add(cookies);
            }
        }
    }
}
