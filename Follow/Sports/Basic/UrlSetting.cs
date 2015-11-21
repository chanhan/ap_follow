using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

namespace Follow.Sports.Basic
{
    public class UrlSetting
    {
        // 取得奧訊域名
        public static List<string> GetBet007Url()
        {
            return GetUrl("bet007", "dxbf.bet007.com");
        }

        public static string GetFootballUrl(string key)
        {
            return GetUrl2("football", key);
        }
        public static string GetUrls(string key, string Elementkey)
        {
            return GetUrl2(key, Elementkey);
        }
        private static List<string> GetUrl(string xmlKey, string defaultValue)
        {
            string xmlFile = string.Format(@"{0}\{1}.xml", Application.StartupPath, System.IO.Path.GetFileNameWithoutExtension(Application.ExecutablePath));

            List<string> urlList = new List<string>();

            // 取得資料
            if (System.IO.File.Exists(xmlFile))
            {
                try
                {
                    XDocument xDoc = XDocument.Load(xmlFile);
                    if (xDoc != null)
                    {
                        var elems = xDoc.Descendants("Url");
                        foreach (XElement elem in elems)
                        {
                            XElement keyElem = elem.Element(xmlKey);
                            if (keyElem != null)
                            {
                                foreach (string url in keyElem.Value.Split(','))
                                {
                                    if (!string.IsNullOrEmpty(url.Trim()))//不為空資料
                                        urlList.Add(url.Trim());
                                }
                            }
                        }
                    }
                }
                catch { }
            }

            if (urlList.Count == 0)//空陣列寫入預設值
                urlList.Add(defaultValue);

            return urlList;
        }

        /// <summary>
        /// 获取来源网
        /// </summary>
        /// <param name="xmlKey">类型(比如 football 取足球的)</param>
        /// <param name="xmlElement">取xmlKey里面中的子元素</param>
        private static string GetUrl2(string xmlKey, string xmlElement)
        {
            string xmlFile = string.Format(@"{0}\{1}.xml", Application.StartupPath, System.IO.Path.GetFileNameWithoutExtension(Application.ExecutablePath));

            string sUrl = string.Empty;

            // 取得資料
            if (System.IO.File.Exists(xmlFile))
            {
                try
                {
                    XDocument xDoc = XDocument.Load(xmlFile);
                    if (xDoc != null)
                    {
                        var elems = xDoc.Descendants("Url");
                        foreach (XElement elem in elems)
                        {
                            XElement keyElem = elem.Element(xmlKey);
                            if (keyElem != null)
                            {
                                XElement x = keyElem.Element(xmlElement);
                                if (x != null)
                                {
                                    sUrl = x.Value.Trim();
                                }
                            }
                        }
                    }
                }
                catch { }
            }

            return sUrl;
        }

        public static string GetUrl(ESport sportType, string key = "Url", string section = "Section")
        {
            string xmlFile = string.Format(@"{0}\{1}.xml", Application.StartupPath, System.IO.Path.GetFileNameWithoutExtension(Application.ExecutablePath));

            // 取得資料
            if (System.IO.File.Exists(xmlFile))
            {
                try
                {
                    if (sportType == ESport.None)
                        return null;

                    if (string.IsNullOrWhiteSpace(section))
                        return null;

                    XDocument xDoc = XDocument.Load(xmlFile);
                    if (xDoc != null)
                    {
                        foreach (XElement xelem in xDoc.Descendants(section))
                        {
                            string sports = null, url = null;
                            if (xelem.Element(key) != null)
                                url = xelem.Element(key).Value.Trim();

                            if (xelem.Element("Sports") != null)
                                sports = xelem.Element("Sports").Value;

                            if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(sports))
                                continue;

                            foreach (string sport in sports.Split(','))
                            {
                                if (Program.GetSportType(sport) == sportType)
                                    return url;
                            }
                        }
                    }
                }
                catch { }
            }
            return null;
        }

    }
}
