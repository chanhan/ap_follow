using Follow.Sports.Basic;
using System;
using System.Collections.Generic;
using System.Text;
using System.Web;

// 跟盤以 ID 為依據
// 跟盤日期以台灣時間顯示

namespace Follow.Sports
{
    public class BkEurocupBet007 : Basic.BasicBasketball
    {
        public BkEurocupBet007(DateTime today)
            : base(ESport.Basketball_Eurocup)
        {
            if (string.IsNullOrWhiteSpace(this.sWebUrl))
            {
                this.sWebUrl = @"http://interface.win007.com/lq/today.aspx";
            }
            // 設定
            this.AllianceID = 26;
            this.GameType = "BKEL2";
            this.GameDate = GetUtcTw(today).Date; // 只取日期 
            this.DownHome = new BasicDownload(this.Sport, this.sWebUrl, Encoding.UTF8);
            // http://dxbf.bet007.com/nba_date.aspx?time=2013-11-06
            //this.DownHomeHeader = new Dictionary<string, string>();
            //this.DownHomeHeader["Accept"] = "*/*";
            //this.DownHomeHeader["Accept-Charset"] = "gb2312";
            //this.DownHomeHeader["Accept-Encoding"] = "gzip, deflate";
            //this.DownHomeHeader["Host"] = "dxbf.bet007.com";
            //this.DownHomeHeader["User-Agent"] = "User-Agent	Mozilla/5.0 (compatible; MSIE 10.0; Windows NT 6.1; WOW64; Trident/6.0)";
            //this.DownHomeHeader["Connection"] = "keep-alive";
        }
        public override void Download()
        {
            // 讀取首頁資料。
            this.DownHome.DownloadString();
            this.DownLastTime = DateTime.Now;
        }
        public override int Follow()
        {
            // 沒有資料就離開
            if (string.IsNullOrEmpty(this.DownHome.Data)) return 0;

            int result = 0;
            // 取得資料
            Dictionary<string, BasicInfo> gameData = this.GetDataByBet007Basketball(this.DownHome.Data, "欧协杯", "歐協杯", true);
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
        //private Dictionary<string, string> DownHomeHeader;
    }
}
