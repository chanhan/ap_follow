using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Follow.Sports.Basic
{
    /// <summary>
    /// HTTP 提供器
    /// </summary>
    public class HttpAdapter
    {
        [DllImport("wininet.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool InternetGetCookieEx(string pchURL, string pchCookieName, StringBuilder pchCookieData, ref System.UInt32 pcchCookieData, int dwFlags, IntPtr lpReserved);

        /// <summary>
        /// 获取Cookie
        /// </summary>
        /// <param name="domain">主机域名</param>
        /// <param name="url">网址</param>        
        public static CookieContainer GetCookieContainer(Uri uri)
        {
            return GetCookieContainer(uri.Authority, uri.AbsoluteUri);
        }
        /// <summary>
        /// 获取Cookie
        /// </summary>
        /// <param name="domain">主机域名</param>
        /// <param name="url">网址</param>        
        public static CookieContainer GetCookieContainer(string domain, string url)
        {
            string[] cookieArray, cookieNameValue;
            CookieContainer cookies;

            cookies = new CookieContainer();
            cookieArray = GetAllCookie(url).Split(';');
            foreach (string ck in cookieArray)
            {
                cookieNameValue = ck.Split('=');
                Cookie cookie = new Cookie(cookieNameValue[0].Trim(), cookieNameValue[1].Trim());
                if (domain != "")
                {
                    cookie.Domain = domain;
                    cookies.Add(cookie);
                }
                else
                    cookies.Add(new Uri(url), cookie);
                cookie.Expires = DateTime.MaxValue;
            }

            return cookies;
        }
        /// <summary>
        /// 获取完整Cookie
        /// </summary>
        /// <param name="url">网址</param>        
        private static string GetAllCookie(string url)
        {
            uint dataSize;
            StringBuilder sb;

            dataSize = 256;
            sb = new StringBuilder((int)dataSize);
            if (!InternetGetCookieEx(url, null, sb, ref dataSize, 0x2000, IntPtr.Zero))
            {
                if (dataSize < 0)
                    return null;
                sb = new StringBuilder((int)dataSize);
                if (!InternetGetCookieEx(url, null, sb, ref dataSize, 0x00002000, IntPtr.Zero))
                    return null;
            }
            return sb.ToString();
        }
    }
}
