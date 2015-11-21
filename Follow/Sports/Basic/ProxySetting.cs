using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using SHGG.DataStructerService;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;

namespace Follow.Sports.Basic
{
    class ProxySetting
    {
        // 取得 奧訊Proxy 設定
        public static List<string> GetBet007Proxy()
        {
            return GetProxy(ESport.None, "Bet007Proxy");
        }

        // 取得 足球走勢 Proxy
        public static List<string> GetSpbo1Proxy()
        {
            return GetProxy(ESport.None, "Spbo1Proxy");
        }

        // 取得 Asiascore Proxy 設定
        public static List<string> GetAsiascoreProxy()
        {
            return GetProxy(ESport.None, "AsiascoreProxy");
        }


        // 取得 Proxy 設定
        public static List<string> GetProxy(ESport sportType, string section = null)
        {
            string xmlFile = string.Format(@"{0}\{1}.xml", Application.StartupPath, System.IO.Path.GetFileNameWithoutExtension(Application.ExecutablePath));

            // 取得資料
            if (System.IO.File.Exists(xmlFile))
            {
                bool isSportType = false;
                try
                {
                    if (sportType != ESport.None && string.IsNullOrEmpty(section))
                    {
                        isSportType = true;
                        section = "Section";
                    }
                    if (string.IsNullOrEmpty(section))
                        return null;

                    XDocument xDoc = XDocument.Load(xmlFile);
                    if (xDoc != null)
                    {
                        foreach (XElement xelem in xDoc.Descendants(section))
                        {
                            string sports = null, proxy = null, sDefault = null;
                            if (xelem.Element("Proxy") != null)
                                proxy = xelem.Element("Proxy").Value.Trim();

                            if (xelem.Element("default") != null)
                                sDefault = xelem.Element("default").Value.Trim();

                            if (isSportType)
                            {
                                if (xelem.Element("Sports") != null)
                                    sports = xelem.Element("Sports").Value;

                                if (string.IsNullOrEmpty(proxy) || string.IsNullOrEmpty(sports))
                                    continue;

                                foreach (string sport in sports.Split(','))
                                {
                                    if (Program.GetSportType(sport) == sportType)
                                        return GetProxyList(proxy, sDefault);
                                }
                            }
                            else
                            {
                                if (!string.IsNullOrEmpty(proxy))
                                    return GetProxyList(proxy, sDefault);
                            }
                        }
                    }
                }
                catch { }
            }
            return null;
        }

        private static List<string> GetProxyList(string sProxylist, string sDefault)
        {
            List<string> proxyList = new List<string>();

            try
            {
                if (!string.IsNullOrEmpty(sProxylist))
                {
                    string[] ipList = sProxylist.Trim().Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);//ip切割

                    //string[] ipPort;
                    for (int i = 0; i < ipList.Length; i++)
                    {
                        ipList[i] = ipList[i].Trim();
                        //不做IP檢查，因為proxy有可能是domian name
                        //ipPort = ipList[i].Split(new string[] { ":" }, StringSplitOptions.RemoveEmptyEntries);//ip, port
                        //if (IsValidIP(ipPort[0]) || ipPort[0] == "-1")//ip檢查 
                            proxyList.Add(ipList[i]);
                    }

                    int iDefault = 0;
                    if (!string.IsNullOrEmpty(sDefault) && int.TryParse(sDefault, out iDefault))//取得預設值使用
                    {
                        if (iDefault > 0 && iDefault < proxyList.Count)//作SWAP 將預設值放置第一順位
                        {
                            string tmp = proxyList[0];
                            proxyList[0] = string.Copy(proxyList[iDefault]);
                            proxyList[iDefault] = tmp;
                        }
                    }
                }
            }
            catch { }

            return proxyList;
        }

        //IP檢查
        private bool IsValidIP(string addr)
        {
            bool valid = false;
            try
            {
                string pattern = @"^([1-9]|[1-9][0-9]|1[0-9][0-9]|2[0-4][0-9]|25[0-5])(\.([0-9]|[1-9][0-9]|1[0-9][0-9]|2[0-4][0-9]|25[0-5])){3}$";
                Regex check = new Regex(pattern);

                if (!string.IsNullOrEmpty(addr))
                {
                    string[] ipPort = addr.Split(new string[] { ":" }, StringSplitOptions.RemoveEmptyEntries);//ip, port
                    valid = check.IsMatch(ipPort[0], 0);
                }
            }
            catch { }

            return valid;
        }

        public static void RefreshIESettings(string strProxy)//設定IE Proxy
        {
            const int INTERNET_OPTION_PROXY = 38;
            const int INTERNET_OPEN_TYPE_PROXY = 3;

            Struct_INTERNET_PROXY_INFO struct_IPI;

            // Filling in structure 
            struct_IPI.dwAccessType = INTERNET_OPEN_TYPE_PROXY;
            struct_IPI.proxy = Marshal.StringToHGlobalAnsi(strProxy);
            struct_IPI.proxyBypass = Marshal.StringToHGlobalAnsi("local");

            // Allocating memory 
            IntPtr intptrStruct = Marshal.AllocCoTaskMem(Marshal.SizeOf(struct_IPI));

            // Converting structure to IntPtr 
            Marshal.StructureToPtr(struct_IPI, intptrStruct, true);

            bool iReturn = InternetSetOption(IntPtr.Zero, INTERNET_OPTION_PROXY, intptrStruct, Marshal.SizeOf(struct_IPI));
        }

        [DllImport("wininet.dll", SetLastError = true)]
        private static extern bool InternetSetOption(IntPtr hInternet, int dwOption, IntPtr lpBuffer, int lpdwBufferLength);
        private struct Struct_INTERNET_PROXY_INFO
        {
            public int dwAccessType;
            public IntPtr proxy;
            public IntPtr proxyBypass;
        }; 
    }
}
