using Follow.Sports.Basic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Follow.Sports
{
    public class BkCBABet007 : Basic.BasicBasketball
    {
        public BkCBABet007(DateTime today)
            : base(ESport.Basketball_CBA)
        {
            if (string.IsNullOrWhiteSpace(this.sWebUrl))
            {
                this.sWebUrl = @"http://interface.win007.com/lq/today.aspx";
            }
            // 設定
            this.AllianceID = 16;
            this.GameType = "BKCN";
            this.GameDate = GetUtcTw(today).Date; // 只取日期
            this.DownHome = new BasicDownload(this.Sport, this.sWebUrl, Encoding.UTF8);
            
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
            Dictionary<string, BasicInfo> gameData = this.GetDataByBet007Basketball(this.DownHome.Data, "CBA", "CBA", true);
            
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
