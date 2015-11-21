using Follow.Sports.Basic;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Web;

// 跟盤以 ID 為依據
// 跟盤日期以韓國時間顯示

namespace Follow.Sports
{
    public class BkNCAABet007 : Basic.BasicBasketball
    {
        public BkNCAABet007(DateTime today)
            : base(ESport.Basketball_NCAA)
        {
            if (string.IsNullOrWhiteSpace(this.sWebUrl))
            {
                this.sWebUrl = @"http://interface.win007.com/lq/today.aspx";
            }
            // 設定
            this.AllianceID = 14;
            this.GameType = "BKNCAA";
            this.GameDate = GetUtcUsaEt(today).Date; // 只取日期
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
            Dictionary<string, BasicInfo> gameData = this.GetDataByBet007Basketball(this.DownHome.Data, "NCAA", "NCAA", true);

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
    }
}
