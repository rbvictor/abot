using System.Net;
using System.Net.Http;

namespace Abot.Poco
{
    public class WebExceptionWrapper : WebException
    {
        private HttpRequestException e;

        public WebExceptionWrapper(HttpRequestException e)
        {
            this.e = e;
        }
    }
}
