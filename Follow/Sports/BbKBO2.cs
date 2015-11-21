using Follow.Sports.Basic;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Web;

// 跟盤以 ID 為依據
// 跟盤日期以台灣時間顯示
// 韓棒 - 玩運彩

namespace Follow.Sports
{
    public class BbKBO2 : Basic.BasicBaseball
    {
        public BbKBO2(DateTime today)
            : base(ESport.Baseball_KBO2)
        {
            // 設定
            this.AllianceID = 66;
            this.GameType = "BBKR";
            this.GameDate = today.Date; // 只取日期

            if (string.IsNullOrWhiteSpace(this.sWebUrl))
            {
                this.sWebUrl = @"http://www.playsport.cc/livescore.php?aid=9&gamedate={0:yyyyMMdd}&mode=11";
            }
            // 運彩 Url 參數: aid=9 (賽事種類: 韓國職棒) gamedate (時間: yyyyMMdd) mode=11 (盤口: 國際)
            string url = String.Format(this.sWebUrl, this.GameDate);
            this.DownReal = new BasicDownload(this.Sport, url, Encoding.GetEncoding("big5"));
        }

        public override void Download()
        {
            // 沒有資料或下載時間超過 2 秒才讀取資料。
            if (!this.DownReal.LastTime.HasValue ||
                DateTime.Now >= this.DownReal.LastTime.Value.AddSeconds(2))
            {
                this.DownReal.DownloadString();
            }
        }

        public override int Follow()
        {
            // 沒有資料就離開
            if (string.IsNullOrEmpty(this.DownReal.Data))
                return 0;

            int result = 0;
            // 取得玩運彩棒球資料
            Dictionary<string, BasicInfo> gameData = base.GetDataByPlaySport(this.DownReal.Data);

            if (gameData != null && gameData.Count > 0)
            {
                // 資料
                foreach (KeyValuePair<string, BasicInfo> data in gameData)
                {
                    BasicInfo info = data.Value;
                    if (base.CheckGame(info))//檢查比分是否合法
                    {
                        this.GameData[data.Key] = info;// 加入
                        result++;// 累計
                    }
                }
            }

            // 傳回
            return result;
        }

        public override bool Update(string connectionString, BasicInfo info)
        {
            // 多來源跟分
            return this.Update4(connectionString, info);
        }

        // 下載資料
        private BasicDownload DownReal;
        private DateTime DownLastTime = DateTime.Now;
    }
}