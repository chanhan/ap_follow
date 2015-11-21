using Follow.Sports.Basic;
using System;
using System.Collections.Generic;
using System.Text;
using System.Web;

// 跟盤以 ID 為依據
// 跟盤日期以台灣時間顯示

namespace Follow.Sports
{
    public class BkEBT : Basic.BasicBasketball
    {
        private string sWebUrl1 = UrlSetting.GetUrl(ESport.Basketball_EBT, "Url1");
        public BkEBT(DateTime today) : base(ESport.Basketball_EBT)
        {
            if (string.IsNullOrWhiteSpace(this.sWebUrl))
            {
                this.sWebUrl = @"http://d.flashscore.com/x/feed/f_3_0_8_asia_1";
            }
            if (string.IsNullOrWhiteSpace(this.sWebUrl1))
            {
                this.sWebUrl1 = "http://d.flashscore.com/x/feed/proxy";
            }
            // 設定
            this.AllianceID = 23;
            this.GameType = "BKEBT";
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
            this.DownHomeHeader["Cookie"] = "__utma=175935605.237435887.1433729535.1433729535.1433732345.2; __utmb=175935605.4.10.1433732345; __utmc=175935605; __utmz=175935605.1433732345.2.2.utmcsr=flashscore.com|utmccn=(referral)|utmcmd=referral|utmcct=/tennis/; __utmt=1; __gads=ID=95d4003915b743c6:T=1433729537:S=ALNI_MYpkSaA3-m9gggcZp-An3QTk6uUOg";
            this.DownHomeHeader["Host"] = "d.flashscore.com";
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
            Dictionary<string, BasicInfo> gameData = this.GetDataByAsiaScore(this.DownHome.Data, "europe", " eurobasket", 10);
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
