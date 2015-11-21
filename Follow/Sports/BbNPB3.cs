using Follow.Helper;
using Follow.Sports.Basic;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Web;

// 跟盤以 ID 為依據
// 跟盤日期以日本時間顯示

namespace Follow.Sports
{
    public class BbNPB3 : Basic.BasicBaseball
    {
        // 下載資料
        private BasicDownload DownReal;
        private DateTime DownLastTime = DateTime.Now;
        public BbNPB3(DateTime today)
            : base(ESport.Baseball_NPB3)
        {
            // 設定
            this.AllianceID = 46;
            this.GameType = "BBJP";
            this.GameDate = today.Date; // 只取日期
            if (string.IsNullOrWhiteSpace(this.sWebUrl))
            {
                this.sWebUrl = @"http://tslc.stats.com/npb/scoreboard.asp?day={0}&&ref=OFF";
            }
            //尬球乐 日棒
            string url = String.Format(this.sWebUrl, this.GameDate.ToString("yyyyMMdd"));
            this.DownReal = new BasicDownload(this.Sport, url, Encoding.GetEncoding("GB2312"));
        }

        public override void Download()
        {
            // 沒有資料或下載時間超過 3 秒才讀取資料。
            if (!this.DownReal.LastTime.HasValue ||
                DateTime.Now >= this.DownReal.LastTime.Value.AddSeconds(3))
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
            // 取得尬球乐資料
            Dictionary<string, BasicInfo> gameData = base.GetDataByTSLC(this.DownReal.Data);

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
    }
}