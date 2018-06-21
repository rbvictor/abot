using Abot.Poco;
using log4net;
using System;
using System.CodeDom;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using log4net.Core;

namespace Abot.Core
{
    public class HttpClientPageRequester : IPageRequester
    {
        static ILog _logger = LogManager.GetLogger("AbotLogger");

        protected CrawlConfiguration _config;
        protected IWebContentExtractor _extractor;
        protected CookieContainer _cookieContainer = new CookieContainer();

        public HttpClientPageRequester(CrawlConfiguration config)
            : this(config, null)
        {

        }

        public HttpClientPageRequester(CrawlConfiguration config, IWebContentExtractor contentExtractor)
        {
            if (config == null)
                throw new ArgumentNullException("config");

            _config = config;

            if (_config.HttpServicePointConnectionLimit > 0)
                ServicePointManager.DefaultConnectionLimit = _config.HttpServicePointConnectionLimit;

            if (!_config.IsSslCertificateValidationEnabled)
                ServicePointManager.ServerCertificateValidationCallback +=
                    (sender, certificate, chain, sslPolicyErrors) => true;

            _extractor = contentExtractor ?? new WebContentExtractor();
        }

        /// <summary>
        /// Make an http web request to the url and download its content
        /// </summary>
        public virtual CrawledPage MakeRequest(Uri uri)
        {
            return MakeRequest(uri, (x) => new CrawlDecision { Allow = true });
        }

        /// <summary>
        /// Make an http web request to the url and download its content based on the param func decision
        /// </summary>
        public virtual CrawledPage MakeRequest(Uri uri, Func<CrawledPage, CrawlDecision> shouldDownloadContent)
        {
            if (uri == null)
                throw new ArgumentNullException("uri");

            CrawledPage crawledPage = new CrawledPage(uri);
            HttpResponseMessage response = null;
            try
            {
                HttpClient httpclient = new HttpClient(GetHandler());
                crawledPage.RequestStarted = DateTime.Now;

                response = httpclient.GetAsync(uri).ConfigureAwait(false).GetAwaiter().GetResult();
                ProcessResponseObject(response);
            }
            catch (HttpRequestException e)
            {
                crawledPage.WebException = new WebExceptionWrapper(e);

                _logger.DebugFormat("Error occurred requesting url [{0}]", uri.AbsoluteUri);
                _logger.Debug(e);
            }
            catch (Exception e)
            {
                _logger.DebugFormat("Error occurred requesting url [{0}]", uri.AbsoluteUri);
                _logger.Debug(e);
            }
            finally
            {
                try
                {

                    crawledPage.HttpWebRequest = (HttpWebRequest) WebRequest.Create(uri);
                    ////////////var response222 = new HttpWebResponse(); 
                    
                    crawledPage.RequestCompleted = DateTime.Now;
                    if (response != null)
                    {
                        /////var httpWebResponseWrapper = new HttpWebResponseWrapper(response);
                        /////crawledPage.HttpWebResponse = httpWebResponseWrapper;
                        CrawlDecision shouldDownloadContentDecision = shouldDownloadContent(crawledPage);
                        if (shouldDownloadContentDecision.Allow)
                        {
                            crawledPage.DownloadContentStarted = DateTime.Now;
                            ////////////////////crawledPage.Content = _extractor.GetContent(httpWebResponseWrapper);
                            crawledPage.DownloadContentCompleted = DateTime.Now;
                        }
                        else
                        {
                            _logger.DebugFormat("Links on page [{0}] not crawled, [{1}]", crawledPage.Uri.AbsoluteUri, shouldDownloadContentDecision.Reason);
                        }

                        //response.Close();//Should already be closed by _extractor but just being safe
                    }
                }
                catch (Exception e)
                {
                    _logger.DebugFormat("Error occurred finalizing requesting url [{0}]", uri.AbsoluteUri);
                    _logger.Debug(e);
                }
            }

            return crawledPage;
        }

        protected virtual HttpClient GetClient()
        {
            //TODO THis should be a static instance or at least per crawl!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!1
            var client = new HttpClient(GetHandler());
            client.DefaultRequestHeaders.UserAgent.ParseAdd(_config.UserAgentString);
            client.DefaultRequestHeaders.Accept.ParseAdd("*/*");

            if (_config.HttpRequestTimeoutInSeconds > 0)
                client.Timeout = TimeSpan.FromMilliseconds(_config.HttpRequestTimeoutInSeconds * 1000);

            //Supposedly this does not work... https://github.com/sjdirect/abot/issues/122
            //if (_config.IsAlwaysLogin)
            //{
            //    request.Credentials = new NetworkCredential(_config.LoginUser, _config.LoginPassword);
            //    request.UseDefaultCredentials = false;
            //}

            return client;
        }

        protected virtual HttpClientHandler GetHandler()
        {
            var clientHandler = new HttpClientHandler();
            clientHandler.AllowAutoRedirect = _config.IsHttpRequestAutoRedirectsEnabled;

            if (_config.HttpRequestMaxAutoRedirects > 0)
                clientHandler.MaxAutomaticRedirections = _config.HttpRequestMaxAutoRedirects;

            if (_config.IsHttpRequestAutomaticDecompressionEnabled)
                clientHandler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

            if (_config.IsSendingCookiesEnabled)
                clientHandler.CookieContainer = _cookieContainer;

            //Supposedly this does not work... https://github.com/sjdirect/abot/issues/122
            //if (_config.IsAlwaysLogin)
            //{
            //    request.Credentials = new NetworkCredential(_config.LoginUser, _config.LoginPassword);
            //    request.UseDefaultCredentials = false;
            //}

            return clientHandler;
        }



        protected virtual void ProcessResponseObject(HttpResponseMessage response)
        {
            //Cookies are automatically handled in HttpClient, no need to add them to the container!!!!
            //if (response != null && _config.IsSendingCookiesEnabled)
            //{
            //    response.
            //    CookieCollection cookies = response.Cookies;
            //    _cookieContainer.Add(cookies);
            //}
        }

        public void Dispose()
        {
            if (_extractor != null)
            {
                _extractor.Dispose();
            }
            _cookieContainer = null;
            _config = null;
        }
    }
}