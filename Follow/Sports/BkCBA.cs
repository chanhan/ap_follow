using Follow.Sports.Basic;
using System;
using System.Collections.Generic;
using System.Text;
using System.Web;

// 跟盤以 ID 為依據
// 跟盤日期以台灣時間顯示

namespace Follow.Sports
{
    public class BkCBA : Basic.BasicBasketball
    {
        private string sWebUrl1 = UrlSetting.GetUrl(ESport.Basketball_CBA, "Url1");
        public BkCBA(DateTime today) : base(ESport.Basketball_CBA)
        {
            if (string.IsNullOrWhiteSpace(this.sWebUrl))
            {
                this.sWebUrl = @"http://d.asiascore.com/x/feed/f_3_0_8_asia_1";
            }
            if (string.IsNullOrWhiteSpace(this.sWebUrl1))
            {
                this.sWebUrl1 = "http://d.asiascore.com/x/feed/proxy";
            }
            // 設定
            this.AllianceID = 16;
            this.GameType = "BKCN";
            this.GameDate = GetUtcTw(today).Date; // 只取日期
            this.DownHome = new BasicDownload(this.Sport, this.sWebUrl);
            this.DownHomeHeader = new Dictionary<string, string>();
            this.DownHomeHeader["Accept"] = "*/*";
            this.DownHomeHeader["Accept-Charset"] = "utf-8;q=0.7,*;q=0.3";
            this.DownHomeHeader["Accept-Encoding"] = "gzip,deflate,sdch";
            this.DownHomeHeader["Accept-Language"] = "*";
            this.DownHomeHeader["X-Fsign"] = "SW9D1eZo";
            this.DownHomeHeader["X-GeoIP"] = "1";
            this.DownHomeHeader["X-utime"] = "1";
            this.DownHomeHeader["Cookie"] = "__utma=190588603.635099785.1357704170.1360998879.1361003436.71; __utmb=190588603.1.10.1361003436; __utmc=190588603; __utmz=190588603.1361003436.71.19.utmcsr=asiascore.com|utmccn=(referral)|utmcmd=referral|utmcct=/";
            this.DownHomeHeader["Host"] = "d.asiascore.com";
            this.DownHomeHeader["Referer"] = this.sWebUrl1;
            //this.DownRealHeader["User-Agent"] = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.17 (KHTML, like Gecko) Chrome/24.0.1312.57 Safari/537.17";
            //this.DownRealHeader["Connection"] = "keep-alive";
        }
        public override void Download()
        {
            // 讀取首頁資料。
            this.DownHome.DownloadData(this.DownHomeHeader);
            this.DownLastTime = DateTime.Now;
        }
        public override int Follow()
        {
            // 沒有資料就離開
            if (string.IsNullOrEmpty(this.DownHome.Data)) return 0;

            int result = 0;
            // 取得資料
            Dictionary<string, BasicInfo> gameData = this.GetDataByAsiaScore(this.DownHome.Data, "china", " cba", 12);
            // 判斷資料
            if (gameData != null && gameData.Count > 0)
            {
                // 資料
                foreach (KeyValuePair<string, BasicInfo> data in gameData)
                {
                    // 加入
                    this.GameData[data.Key] = data.Value;
                }
                result = gameData.Count;
            }
            // 傳回
            return result;
        }
        // 下載資料
        private BasicDownload DownHome;
        private DateTime DownLastTime = DateTime.Now;
        private Dictionary<string, string> DownHomeHeader;
    }
}
