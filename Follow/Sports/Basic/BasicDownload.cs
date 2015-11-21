using SHGG.FileService;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Cache;
using System.Net.Http;
using System.Text;
using System.Windows.Forms;
using System.Xml.Linq;
using SHGG.DataStructerService;
using System.Text.RegularExpressions;
using NLog;

namespace Follow.Sports.Basic
{
    class WebClientEx : WebClient
    {
        protected override WebRequest GetWebRequest(Uri address)
        {
            WebRequest request = base.GetWebRequest(address);
            if (request is HttpWebRequest)
            {
                HttpWebRequest webRequest = (HttpWebRequest)request;
                //webRequest.KeepAlive = false;
                webRequest.Timeout = 1000000000;
                webRequest.ReadWriteTimeout = 1000000000;
                //會因為來源針對不同流覽器回傳不同資料 所以暫時註解
                //webRequest.UserAgent = @"Mozilla/5.0 (Windows; U; Windows NT 5.2; ru; rv:1.9.0.8) Gecko/2009032609 Firefox/3.0.8 (.NET CLR 4.0.20506)";
            }
            return request;
        }
    }

    /// <summary>
    /// 下載資料。
    /// </summary>
    public class BasicDownload : IDisposable
    {
        #region Property
        public ESport Sport { get; protected set; }
        /// <summary>
        /// 資料網址。
        /// </summary>
        public Uri Uri { get; protected set; }
        /// <summary>
        /// 代理伺服器。
        /// </summary>
        public string Proxy { get; set; }//當前使用
        private List<string> ProxyList { get; set; }
        private List<string> UrlList { get; set; } //多個來源網


        public int ProxyUsing { get; set; }
        /// <summary>
        /// 資料。
        /// </summary>
        public string Data { get; protected set; }
        /// <summary>
        /// 最後下載時間。
        /// </summary>
        public DateTime? LastTime { get; protected set; }
        /// <summary>
        /// 是否有變更。
        /// </summary>
        public bool HasChanged { get; set; }
        /// <summary>
        /// 寫入檔案記錄。
        /// </summary>
        public bool FileWrite { get; set; }

        // 記錄檔操作
        private LogFile Logs;
        private DateTime? FileWriteTime;


        private Timer downloadTimer;
        private int timeoutValue = 60000;

    //    public bool IsChangeProxy { get; set; }
        #endregion
        #region Function
        public BasicDownload(ESport sport, string url) : this(sport, url, Encoding.UTF8) { }
        public BasicDownload(ESport sport, string url, string fileType) : this(sport, url, Encoding.UTF8, fileType) { }
        public BasicDownload(ESport sport, string url, Encoding encoding, string fileType = null)
        {
            this.downloadTimer = new Timer();
            this.downloadTimer.Interval = timeoutValue;
            this.downloadTimer.Tick += TimerEventProcessor;

            this.Logs = new LogFile(sport, fileType);

            // 建立
            this.Sport = sport;
            this.Uri = new Uri(url);


            if (this.Uri.ToString().IndexOf("bet007.com") != -1)//奧訊來源
            {
                this.UrlList = UrlSetting.GetBet007Url();
                this.ProxyList = ProxySetting.GetBet007Proxy(); // 取得奧訊代理
            }

            if (this.Uri.ToString().IndexOf("d.asiascore.com") != -1)//asiascore來源
                this.ProxyList = ProxySetting.GetAsiascoreProxy(); // 取得asiascore代理

            if (this.Uri.ToString().IndexOf("www8.spbo1.com") != -1 || this.Uri.ToString().IndexOf("www8.spbo.com") != -1)//足球走勢來源
                this.ProxyList = ProxySetting.GetSpbo1Proxy(); // 取得足球走勢代理

            if (this.ProxyList == null || this.ProxyList.Count <=0)
                this.ProxyList = ProxySetting.GetProxy(sport); // 取得代理

            this.Client = new WebClientEx();
            //this.Client.CachePolicy = new RequestCachePolicy(RequestCacheLevel.NoCacheNoStore);
            this.Client.Encoding = encoding;

            this.Client.DownloadDataCompleted += this.Client_DownloadDataCompleted;
            this.Client.DownloadStringCompleted += this.Client_DownloadStringCompleted;

            // 建立記錄檔操作
            this.FileWrite = true;

            //奧訊代理 使用順位
            ProxyUsing = 0;//預設值0

        }

        private void TimerEventProcessor(Object myObject,EventArgs myEventArgs)
        {
            this.Cancel();

            string message = this.Uri.ToString() + "\r\n";
            message += string.Format("發生錯誤：資料下載逾時 {0} 秒! \r\n", timeoutValue/1000);
            this.WriteLog(message, true);
        }
        /// <summary>
        /// 釋放記憶體。
        /// </summary>
        public void Dispose()
        {
            //解除委派處理
            this.Client.DownloadDataCompleted -= this.Client_DownloadDataCompleted;
            this.Client.DownloadStringCompleted -= this.Client_DownloadStringCompleted;

            // 讀取資料就取消
            this.Cancel();

            // 釋放記憶體
            this.Client.Dispose();
            this.Client = null;
            this.Uri = null;
            this.Data = null;
        }

        public void Cancel()
        {
            if (this.downloadTimer.Enabled)
                this.downloadTimer.Stop();

            if (this.Client.IsBusy)
                this.Client.CancelAsync();
        }

        /// <summary>
        /// 傳回 URL。
        /// </summary>
        public override string ToString()
        {
            return this.Uri.ToString();
        }

        /// <summary>
        /// 下載。
        /// </summary>
        public void DownloadString(bool random=true)
        {
            // 錯誤處理
            try
            {
                this.TimeoutClearProxy(DateTime.Now);

                // 沒有讀取資料才執行
                if (!this.Client.IsBusy)
                {
                    SetProxy();//設定Proxy

                    Uri uri = null;
                    // 建立 URi
                    if (this.Uri.Query == null || string.IsNullOrEmpty(this.Uri.Query.Trim()))
                    {
                        uri = new Uri(this.Uri.ToString() +(random?"?tk=" + DateTime.Now.Ticks.ToString():""), UriKind.RelativeOrAbsolute);
                    }
                    else
                    {
                        uri = new Uri(this.Uri.ToString() +(random?"&tk=" + DateTime.Now.Ticks.ToString():""), UriKind.RelativeOrAbsolute);
                    }
                    // 非同步讀取
                    this.Client.DownloadStringAsync(uri);

                    //開始計時
                    //this.downloadTimer.Start();
                    //this.HasChanged = false;                   
                }
            }
            catch (Exception ex)
            {
                this.Logs.Error("DownloadString Error!\r\nMessage:{0},\r\nStackTrace:{1}\r\n", ex.Message, ex.StackTrace);
            }
        }
        /// <summary>
        /// 下载足球走势资料
        /// </summary>
        /// <param name="Url">地址</param>
        /// <returns>资料字符串</returns>
        public void DownloadAnalysisString()
        {
            string PageStr = string.Empty;//用于存放还回的html
            //Uri url = new Uri( ""+ "?time=" + DateTime.Now.Ticks.ToString());//Uri类 提供统一资源标识符 (URI) 的对象表示形式和对 URI 各部分的轻松访问。就是处理url地址
            try
            {
                HttpWebRequest httprequest = (HttpWebRequest)WebRequest.Create(this.Uri);//根据url地址创建HTTpWebRequest对象
                httprequest.Method = "get";
                HttpWebResponse response = (HttpWebResponse)httprequest.GetResponse();//使用HttpWebResponse获取请求的还回值
                Stream steam = response.GetResponseStream();//从还回对象中获取数据流
                StreamReader reader = new StreamReader(steam, Encoding.UTF8);//读取数据Encoding.GetEncoding("gb2312")指编码是gb2312，不让中文会乱码的
                PageStr = reader.ReadToEnd();
                reader.Close();
                steam.Close();
                //string host = httprequest.Address.Host;

                if (PageStr == null)
                {
                    return;
                }
                string message = "";
                if (this.Uri != null)
                {
                    message += this.Uri.ToString() + "\r\n";
                }
                if (!string.IsNullOrEmpty(PageStr))
                {
                    this.HasChanged = (this.Data != PageStr);
                    // 指向資料
                    this.Data = PageStr;
                    this.LastTime = DateTime.Now;
                    // 寫入記錄檔
                    message += this.Data.Replace("\r\n", " ").Replace("\n", " ").Trim() + "\r\n";
                }
                else
                {
                    this.HasChanged = false;
                    // 寫入記錄檔
                    message += "讀到空資料\r\n";
                }
                WriteLog(message,true);
            }
            catch (Exception ex)
            {
                this.Logs.Error("DownloadString Error!\r\nMessage:{0},\r\nStackTrace:{1}\r\n", ex.Message, ex.StackTrace);
            }
        }
        /// <summary>
        /// 下載。
        /// </summary>
        public void DownloadData(Dictionary<string, string> header)
        {
            // 錯誤處理
            try
            {
                this.TimeoutClearProxy(DateTime.Now);
                // 沒有讀取資料才執行
                if (!this.Client.IsBusy)
                {
                    SetProxy();//設定Proxy

                    // 設定
                    if (this.Client.Headers.Count == 0 && header != null)
                    {
                        // 參數
                        foreach (KeyValuePair<string, string> data in header)
                        {
                            this.Client.Headers.Add(data.Key, data.Value);
                        }
                    }

                    Uri uri = null;
                    // 建立 URi
                    if (this.Uri.Query == null || string.IsNullOrEmpty(this.Uri.Query.Trim()))
                    {
                        uri = new Uri(this.Uri.ToString() + "?tk=" + DateTime.Now.Ticks.ToString(), UriKind.RelativeOrAbsolute);
                    }
                    else
                    {
                        uri = new Uri(this.Uri.ToString() + "&tk=" + DateTime.Now.Ticks.ToString(), UriKind.RelativeOrAbsolute);
                    }
                    // 非同步讀取
                    this.Client.DownloadDataAsync(uri, "gzip");

                    //開始計時
                    //this.downloadTimer.Start();
                    //this.HasChanged = false;
                }
            }
            catch (Exception ex)
            {
                this.Logs.Error("DownloadData Error!\r\nMessage:{0},\r\nStackTrace:{1}\r\n", ex.Message, ex.StackTrace);
            }
        }

        //設定Proxy
        public void SetProxy()
        {
            // 設定奧訊代理伺服器
            if (ProxyList != null && this.ProxyList.Count > 0)
            {
                // 錯誤處理
                try
                {
                    if (this.ProxyUsing >= 0)//預設值-1不使用Proxy, 取不到資料才使用
                    {
                        if (this.ProxyUsing < this.ProxyList.Count)
                            this.Proxy = this.ProxyList[this.ProxyUsing];
                    }

                    if (!string.IsNullOrEmpty(this.Proxy) && this.Proxy != "-1")//-1不使用Proxy
                        this.Client.Proxy = new WebProxy(this.Proxy);
                }
                catch (Exception ex)
                {
                    this.Logs.Error("SetProxy Error!\r\nMessage:{0},\r\nStackTrace:{1}\r\n", ex.Message, ex.StackTrace);
                }
            }
        }

        //清除Proxy
        private void ClearProxy()
        {
            if (this.Client.Proxy != null)
                this.Client.Proxy = null;//清除Proxy

            if (ProxyList != null)
            {
                int ProxyListCount = this.ProxyList.Count;
                if (ProxyListCount > 0)//Proxy列表
                {
                    this.ProxyUsing++;
                    if (this.ProxyUsing >= ProxyListCount)//超過列表索引 設回預設值
                    {
                        UrlChange();//更新下載來源網
                        this.ProxyUsing = 0;
                    }
                }
            }
            else//未使用proxy 直接檢查是否需要更新下載來源
                UrlChange();//更新下載來源網
        }

        //更新下載來源網
        private void UrlChange()
        {
            if(UrlList != null && UrlList.Count > 1)
            {
                string uri = this.Uri.ToString();
                int idx = 0;
                for (idx = 0; idx < UrlList.Count; idx++)
                {
                    if (uri.Contains(UrlList[idx]))//比對當前使用哪一組url
                        break;
                }

                int newIdx = idx+1;                
                this.Uri = null;//釋放資源
                string newUri = "";
                if (newIdx < UrlList.Count)
                    newUri = uri.Replace(UrlList[idx], UrlList[newIdx]);
                else
                    newUri = uri.Replace(UrlList[idx], UrlList[0]);

                this.Uri = new Uri(newUri);
            }
        }

        // 讀取資料
        private WebClientEx Client;
        // 資料讀取完成
        private void Client_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            //停止計時
            if (this.downloadTimer.Enabled)
                this.downloadTimer.Stop();

            if (e == null)
                return;

            try
            {
                string message = "";
                if (this.Uri != null)
                    message+=this.Uri.ToString() + "\r\n";

                // 判斷錯誤
                if (e.Error != null)
                {
                    this.HasChanged = false;
                   // this.IsChangeProxy = true;//切换代理
                    // 寫入記錄檔
                    message += " 發生錯誤：" + e.Error.Message + " 詳細：" + e.Error.StackTrace + "\r\n";
                }
                else
                {
                    if (!e.Cancelled)
                    {
                        if (!string.IsNullOrEmpty(e.Result))
                        {
                            this.HasChanged = (this.Data != e.Result);
                            // 指向資料
                            this.Data = e.Result;
                            this.LastTime = DateTime.Now;
                            // 寫入記錄檔
                            message += this.Data.Replace("\r\n", " ").Replace("\n", " ").Trim() + "\r\n";
                        }
                        else
                        {
                            this.HasChanged = false;
                            // 寫入記錄檔
                            message += "讀到空資料\r\n";
                        }
                    }
                    else
                    {
                        // 寫入記錄檔
                        message += "取消讀取\r\n";
                    }
                }

                WriteLog(message,(e.Error!=null),false);
            }
            catch (Exception ex)
            {
                this.Logs.Error("Client_DownloadStringCompleted Error!\r\nMessage:{0},\r\nStackTrace:{1}\r\n", ex.Message, ex.StackTrace);
            }
        }
        private void Client_DownloadDataCompleted(object sender, DownloadDataCompletedEventArgs e)
        {
            //停止計時
            if (this.downloadTimer.Enabled)
                this.downloadTimer.Stop();

            if (e == null)
                return;

            try
            {
                string message = "";
                if (this.Uri != null)
                    message += this.Uri.ToString() + "\r\n";

                // 判斷錯誤
                if (e.Error != null)
                {
                    this.HasChanged = false;
                    // 寫入記錄檔
                    message += " 發生錯誤：" + e.Error.Message + " 詳細：" + e.Error.StackTrace + "\r\n";
                }
                else
                {
                    if (!e.Cancelled)
                    {
                        if (e.Result.Length > 0)
                        {
                            StreamReader reader = null;
                            string result = null;
                            // 判斷是否為壓縮格式
                            if (e.UserState != null && e.UserState.ToString() == "gzip")
                            {
                                reader = new StreamReader(new GZipStream(new MemoryStream(e.Result), CompressionMode.Decompress), this.Client.Encoding);
                            }
                            else
                            {
                                reader = new StreamReader(new MemoryStream(e.Result), this.Client.Encoding);
                            }
                            result = reader.ReadToEnd();
                            // 釋放記憶體
                            reader.Close();

                            this.HasChanged = (this.Data != result);
                            // 指向資料
                            this.Data = result;
                            this.LastTime = DateTime.Now;
                            // 寫入記錄檔
                            message += this.Data.Replace("\r\n", " ").Replace("\n", " ").Trim() + "\r\n";
                        }
                        else
                        {
                            this.HasChanged = false;
                            // 寫入記錄檔
                            message += "讀到空資料\r\n";
                        }
                    }
                    else
                    {
                        // 寫入記錄檔
                        message += "取消讀取\r\n";
                    }
                }

                WriteLog(message, (e.Error != null));
            }
            catch (Exception ex)
            {
                this.Logs.Error("Client_DownloadDataCompleted Error!\r\nMessage:{0},\r\nStackTrace:{1}\r\n", ex.Message, ex.StackTrace);
            }
        }

        public void WriteLog(string message, bool error,bool useproxy=true)
        {
            if (!error)//下載正常
            {
                if (this.FileWrite)
                {
                    if (this.HasChanged)//資料變動                    
                        this.Logs.Download(message + "使用 Proxy:" + this.Proxy);

                    // 記錄寫入時間
                    this.FileWriteTime = DateTime.Now;
                }
            }
            else //發生錯誤
            {
                if (useproxy)
                {
                    this.Logs.Proxy(message + "使用 Proxy:" + this.Proxy);
                    ClearProxy();//清除Proxy
                }
                else
                {
                    this.Logs.Error(message);
                }
            }
        }

        /// <summary>
        /// 超过设置时间 未更新 自动更换代理
        /// </summary>
        /// <param name="Time"></param>
        public void TimeoutClearProxy(DateTime Time)
        {
            //第一次检查的时候 
            if (this.LastTime == null)
            {
                this.LastTime = Time;
            }
            //最小30s
            //if (frmMain.UseProxy < 300)
            //{
            //    frmMain.UseProxy = 300;
            //}
            //超过时间为抓取到资料，所有下载切换代理
            TimeSpan span = Time.Subtract(this.LastTime.Value);
            if (span.TotalSeconds > frmMain.UseProxy)
            {
                string message = string.Format("超過{0}秒未下載到資料", frmMain.UseProxy);
               
                this.WriteLog(message, true);
                this.LastTime = DateTime.Now;
            }
            else
            {
               // this.WriteLog(string.Format("没超过{0},使用:{1}", frmMain.UseProxy,this.ProxyUsing), true,false);
            }
            
        }
        #endregion
    }
}
