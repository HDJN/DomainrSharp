﻿using System;
using System.Net;
#if (WINRT || NET45)
using System.Net.Http;
using System.Threading.Tasks;
#endif
using Newtonsoft.Json;

#if (SILVERLIGHT && !WINDOWS_PHONE)
namespace DomainrSharp.Silverlight
#elif WINDOWS_PHONE
namespace DomainrSharp.WindowsPhone
#elif WINRT
namespace DomainrSharp.WinRT
#else
namespace DomainrSharp
#endif
{
    public class DomainrSharpService
    {
        private static string QueryUrl = "http://domai.nr/api/json/search?q={0}";
        private static string InfoUrl = "http://domai.nr/api/json/info?q={0}";

#if !WINRT
        public delegate void SearchResultHandler(object sender, SearchResultsEventsArgs e);
        public delegate void DomainrInfoHandler(object sender, DomainrInfoEventArgs e);

        public event SearchResultHandler SearchCompleted;
        public event DomainrInfoHandler InfoDownloadCompleted;
#endif

        public string ClientID { get; set; }

        public DomainrSharpService() { }

        public DomainrSharpService(string clientId)
        {
            ClientID = clientId;
        }

#if (!SILVERLIGHT && !WINRT)
        /// <summary>
        /// Searches the specified search term.
        /// </summary>
        /// <param name="searchTerm">The search term.</param>
        /// <returns></returns>
        public SearchResult Search(string searchTerm)
        {
            if (string.IsNullOrEmpty(searchTerm))
                throw new NullReferenceException("Search term cannot be empty");

            SearchResult result = null;
            ZippedClient client = new ZippedClient();
            string url = string.Format(QueryUrl, searchTerm);
            if (!string.IsNullOrEmpty(ClientID))
                url += "&client_id=" + ClientID;

            string json = client.DownloadString(url);
            if (!string.IsNullOrEmpty(json))
            {
                result = JsonConvert.DeserializeObject<SearchResult>(json);
            }

            return result;
        }
#endif

#if !WINRT
        /// <summary>
        /// Does the search asynchronously
        /// </summary>
        /// <param name="searchTerm">The search term.</param>
        public void SearchAsync(string searchTerm)
        {
            if (string.IsNullOrEmpty(searchTerm))
                throw new NullReferenceException("Search term cannot be empty");

#if (NET45)
            string url = string.Format(QueryUrl, searchTerm);
            if (!string.IsNullOrEmpty(ClientID))
                url += "&client_id=" + ClientID;

            HttpClient client = new HttpClient();
            client.GetAsync(url).ContinueWith((requestTask) =>
            {
                HttpResponseMessage response = requestTask.Result;
                response.EnsureSuccessStatusCode();

                response.Content.ReadAsStringAsync().ContinueWith(readTask => ParseSearchResultString(readTask.Result));
            });
#else
#if (!SILVERLIGHT && !WINRT)
            ZippedClient client = new ZippedClient();
#else
            WebClient client = new SharpGIS.GZipWebClient();
#endif
            
            client.DownloadStringCompleted += (s, e) =>
            {
                if (e.Error == null)
                {
                    ParseSearchResultString(e.Result);
                }
                else
                {
                    if (SearchCompleted != null)
                        SearchCompleted(this, new SearchResultsEventsArgs(e.Error));
                }
            };
            string url = string.Format(QueryUrl, searchTerm);
            if (!string.IsNullOrEmpty(ClientID))
                url += "&client_id=" + ClientID;
            client.DownloadStringAsync(new Uri(url, UriKind.Absolute));
#endif
        }
#endif

#if (WINRT || NET45)
#if (WINRT)
        public async Task<SearchResult> SearchAsync(string searchTerm)
#elif NET45
        public async Task<SearchResult> SearchTaskAsync(string searchTerm)
#endif
        {
            var handler = new HttpClientHandler {AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate};
            var httpClient = new HttpClient(handler);

            var url = string.Format(QueryUrl, searchTerm);
            if (!string.IsNullOrEmpty(ClientID))
                url += "&client_id=" + ClientID;

            var resultString = await httpClient.GetStringAsync(url);

            try
            {
                return JsonConvert.DeserializeObject<SearchResult>(resultString);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Downloads the information asynchronously
        /// </summary>
        /// <param name="domain">The domain.</param>
        /// <returns></returns>
#if WINRT
        public async Task<DomainrInfo> InfoDownloadAsync(string domain)
#elif NET45
        public async Task<DomainrInfo> InfoDownloadTaskAsync(string domain)
#endif
        {
            var handler = new HttpClientHandler { AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate };
            var httpClient = new HttpClient(handler);

            var url = string.Format(InfoUrl, domain);
            if (!string.IsNullOrEmpty(ClientID))
                url += "&client_id=" + ClientID;

            var resultString = await httpClient.GetStringAsync(url);

            try
            {
                return JsonConvert.DeserializeObject<DomainrInfo>(resultString);
            }
            catch
            {
                return null;
            }
        }
#endif

#if !WINRT
        private void ParseSearchResultString(string json)
        {
            if (json != null)
            {
                try
                {
                    var result = JsonConvert.DeserializeObject<SearchResult>(json);
                    if (result != null)
                    {
                        if (SearchCompleted != null)
                            SearchCompleted(this, new SearchResultsEventsArgs(result));
                    }
                    else
                    {
                        if (SearchCompleted != null)
                            SearchCompleted(this, new SearchResultsEventsArgs(new SearchResult()) { Result = null });
                    }
                }
                catch (Exception ex)
                {
                    if (SearchCompleted != null)
                        SearchCompleted(this, new SearchResultsEventsArgs(ex));
                }
            }
            else
            {
                if (SearchCompleted != null)
                    SearchCompleted(this, new SearchResultsEventsArgs(new SearchResult()) { Result = null });
            }
        }

        private void ParseInfoDownloadString(string json)
        {
            if (json != null)
            {
                try
                {
                    var domainrInfo = JsonConvert.DeserializeObject<DomainrInfo>(json);
                    if (domainrInfo != null)
                    {
                        if (InfoDownloadCompleted != null)
                            InfoDownloadCompleted(this, new DomainrInfoEventArgs(domainrInfo));
                    }
                    else
                    {
                        if (InfoDownloadCompleted != null)
                            InfoDownloadCompleted(this, new DomainrInfoEventArgs(new DomainrInfo()) { Result = null });
                    }
                }
                catch (Exception ex)
                {
                    if (InfoDownloadCompleted != null)
                        InfoDownloadCompleted(this, new DomainrInfoEventArgs(ex));
                }
            }
            else
            {
                if (InfoDownloadCompleted != null)
                    InfoDownloadCompleted(this, new DomainrInfoEventArgs(new DomainrInfo()) { Result = null });
            }
        }
#endif

#if !WINRT
        /// <summary>
        /// Downloads the extra domainr information
        /// </summary>
        /// <param name="domain">The domain.</param>
        public void InfoDownloadAsync(string domain)
        {
            if (string.IsNullOrEmpty(domain))
                throw new NullReferenceException("Domain cannot be empty");

#if (NET45)
            string url = string.Format(InfoUrl, domain);
            if (!string.IsNullOrEmpty(ClientID))
                url += "&client_id=" + ClientID;

            HttpClient client = new HttpClient();
            client.GetAsync(url).ContinueWith((requestTask) =>
            {
                HttpResponseMessage response = requestTask.Result;
                response.EnsureSuccessStatusCode();

                response.Content.ReadAsStringAsync().ContinueWith(readTask =>
                {
                    ParseInfoDownloadString(readTask.Result);
                });
            });
#else
#if (!SILVERLIGHT)
            ZippedClient client = new ZippedClient();
#else
            WebClient client = new SharpGIS.GZipWebClient();
#endif
            client.DownloadStringCompleted += (s, e) =>
            {
                if (e.Error == null)
                {
                    ParseInfoDownloadString(e.Result);
                }
                else
                {
                    if (InfoDownloadCompleted != null)
                        InfoDownloadCompleted(this, new DomainrInfoEventArgs(e.Error));
                }
            };
            string url = string.Format(InfoUrl, domain);
            if (!string.IsNullOrEmpty(ClientID))
                url += "&client_id=" + ClientID;
            client.DownloadStringAsync(new Uri(url, UriKind.Absolute));
#endif
        }
#endif

#if (!SILVERLIGHT && !WINRT)
        /// <summary>
        /// Downloads the extra domainr information
        /// </summary>
        /// <param name="domain">The domain.</param>
        public DomainrInfo InfoDownload(string domain)
        {
            if (string.IsNullOrEmpty(domain))
                throw new NullReferenceException("Domain cannot be empty");

            DomainrInfo result = null;

            ZippedClient client = new ZippedClient();
            string url = string.Format(InfoUrl, domain);
            if (!string.IsNullOrEmpty(ClientID))
                url += "&client_id=" + ClientID;

            string json = client.DownloadString(url);

            if (!string.IsNullOrEmpty(json))
            {
                result = JsonConvert.DeserializeObject<DomainrInfo>(json);
            }

            return result;
        }
#endif
    }

#if (!SILVERLIGHT && !WINRT)
    internal class ZippedClient : WebClient
    {
        protected override WebRequest GetWebRequest(Uri address)
        {
            HttpWebRequest request = base.GetWebRequest(address) as HttpWebRequest;
            request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
            return request;
        }
    }
#endif
}