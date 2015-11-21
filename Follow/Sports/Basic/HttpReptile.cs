using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Follow.Sports.Basic
{
    /// <summary>
    /// HTTP。
    /// </summary>
    public class HttpReptile
    {
        #region Property
        /// <summary>
        /// Cookie 容器。
        /// </summary>
        public CookieContainer Cookie { get; protected set; }
        #endregion
        #region Function
        /// <summary>
        /// 建立。
        /// </summary>
        /// <param name="cookie">Cookie 容器</param>
        public HttpReptile(CookieContainer cookie)
        {
            // 設定
            this.Cookie = cookie;
        }

        /// <summary>
        /// 使用 GET 方式取得資料。
        /// </summary>
        /// <param name="url">地址</param>
        /// <param name="header">HTTP 標頭資訊</param>
        public string GetHtmlByGetMode(string url, HttpHeader header)
        {
            HttpWebRequest request;
            HttpWebResponse response;
            StreamReader reader;
            string result = null;
            // 錯誤處理
            try
            {
                request = (HttpWebRequest)WebRequest.Create(url);
                request.Accept = header.Accept;
                request.UserAgent = header.UserAgent;
                request.Method = header.Method;
                request.ContentType = header.ContentType;
                request.Headers.Add("Accept-Language", header.ALanguage);
                request.Headers.Add("Accept-Encoding", header.AEncoding);
                request.Headers.Add("X-Fsign", "SW9D1eZo");
                request.Headers.Add("X-GeoIP", "1");
                request.Headers.Add("X-utime", "1");
                request.Headers.Add("X-Requested-With", "XMLHttpRequest");
                request.Headers.Add("DNT", "1");
             //   request.Headers.Add("Host", "d.asiascore.com");
                request.Timeout = header.Timeout;
                request.AllowAutoRedirect = false;
                request.CookieContainer = this.Cookie;
                request.KeepAlive = true;
                
                using (response = request.GetResponse() as HttpWebResponse)
                {
                    response.Cookies = request.CookieContainer.GetCookies(request.RequestUri);
                    CookieCollection cook = response.Cookies;
                    //Cookie字符串格式
                    string strcrook = request.CookieContainer.GetCookieHeader(request.RequestUri);

                    if (string.Equals("gzip", response.ContentEncoding, StringComparison.CurrentCultureIgnoreCase))
                        reader = new StreamReader(new GZipStream(response.GetResponseStream(), CompressionMode.Decompress), Encoding.UTF8);
                    else
                        reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
                    result = reader.ReadToEnd();
                    reader.Close();
                }
            }
            catch(Exception) {
            
            }
            // 傳回
            return result;
        }

        /// <summary>
        /// 使用 POST 方式取得資料。
        /// </summary>
        /// <param name="url">地址</param>
        /// <param name="header">HTTP 標頭資訊</param>
        /// <param name="postData">資料</param>
        public string GetHtmlByPostMode(string url, HttpHeader header, string postData)
        {
            HttpWebRequest request;
            HttpWebResponse response;
            StreamReader reader;
            Stream stream;
            string result = null;
            // 錯誤處理
            try
            {
                request = (HttpWebRequest)WebRequest.Create(url);
                request.Referer = header.Referer;
                request.Accept = header.Accept;
                request.UserAgent = header.UserAgent;
                request.Method = header.Method;
                request.ContentType = header.ContentType;
                request.Headers.Add("Accept-Language", header.ALanguage);
                request.Headers.Add("Accept-Encoding", header.AEncoding);
                byte[] postdatabyte = Encoding.ASCII.GetBytes(postData);
                request.ContentLength = postdatabyte.Length;
                request.Timeout = header.Timeout;
                request.AllowAutoRedirect = false;
                request.CookieContainer = this.Cookie;
                request.KeepAlive = true;

                //提交请求
                stream = request.GetRequestStream();
                stream.Write(postdatabyte, 0, postdatabyte.Length);
                stream.Close();

                using (response = request.GetResponse() as HttpWebResponse)
                {
                    response.Cookies = request.CookieContainer.GetCookies(request.RequestUri);
                    CookieCollection cook = response.Cookies;
                    ////Cookie字符串格式
                    string strcrook = request.CookieContainer.GetCookieHeader(request.RequestUri);

                    if (string.Equals("gzip", response.ContentEncoding, StringComparison.CurrentCultureIgnoreCase))
                        reader = new StreamReader(new GZipStream(response.GetResponseStream(), CompressionMode.Decompress), Encoding.UTF8);
                    else
                        reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
                    result = reader.ReadToEnd();
                    reader.Close();
                }
            }
            catch{}
            // 傳回
            return result;
        }
        #endregion
    }

    /// <summary>
    /// HTTP 標頭資訊。
    /// </summary>
    public class HttpHeader
    {
        public string Referer { get; set; }
        public string ContentType { get; set; }
        public string Accept { get; set; }
        public string ALanguage { get; set; }
        public string AEncoding { get; set; }
        public string UserAgent { get; set; }
        public string Method { get; set; }
        public int Timeout { get; set; }
    }
}
