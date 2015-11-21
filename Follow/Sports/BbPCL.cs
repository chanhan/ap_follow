using Follow.Sports.Basic;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Web;

// 跟盤以 ID 為依據
// 跟盤日期以美國時間顯示

namespace Follow.Sports
{
    public class BbPCL : Basic.BasicBaseball
    {
        private string sWebUrl1 = UrlSetting.GetUrl(ESport.Baseball_PCL, "Url1");
        public BbPCL(DateTime today) : base(ESport.Baseball_PCL)
        {
            // 設定
            this.AllianceID = 77;
            this.GameType = "BB3APCL";
            //this.GameDate = GetUtcUsaEt(today).Date; // 只取日期
            //this.GameDate = DateTime.Now.AddDays(-1).Date;
            int diffTime = frmMain.GetGameSourceTime("EasternTime");//取得與當地時間差(包含日光節約時間)
            if (diffTime > 0)
                this.GameDate = today.AddHours(-diffTime);
            else
                this.GameDate = GetUtcUsaEt(today);//取得美東時間

            if (string.IsNullOrWhiteSpace(this.sWebUrl))
            {
                this.sWebUrl = @"http://www.milb.com/lookup/json/named.schedule_vw_complete.bam?game_date='{0}'&season={1}&league_id=112";
            }
            if (string.IsNullOrWhiteSpace(this.sWebUrl1))
            {
                this.sWebUrl1 = @"http://www.milb.com/gdcross/components/game/aaa/year_{0}/month_{1}/day_{2}/{3}/linescore.json";
            }

            this.DownHome = new BasicDownload(this.Sport, string.Format(this.sWebUrl, this.GameDate.ToString("yyyy/MM/dd").Replace("-", "/"), this.GameDate.ToString("yyyy")));
            
            this.DownReal = new Dictionary<string, BasicDownload>();
        }
        public override void Download()
        {
            // 沒有資料或下載時間超過 10 分鐘才讀取首頁資料。
            if (this.GameData.Count == 0 ||
                DateTime.Now > this.DownLastTime.AddMinutes(10))
            {
                this.DownHome.DownloadString();
                this.DownLastTime = DateTime.Now;
            }
            // 下載比賽資料
            foreach (KeyValuePair<string, BasicDownload> real in this.DownReal)
            {
                // 沒有資料或下載時間超過 2 秒才讀取資料。
                if (real.Value.LastTime == null ||
                    DateTime.Now >= real.Value.LastTime.Value.AddSeconds(2))
                {
                    real.Value.DownloadString();
                }
            }
        }
        public override int Follow()
        {
            // 沒有資料就離開
            if (string.IsNullOrEmpty(this.DownHome.Data)) return 0;

            int result = 0;
            // 取得資料
            Dictionary<string, BasicInfo> gameData = this.GetDataByMilbForSchedules(this.DownHome.Data);
            // 判斷資料
            if (gameData != null && gameData.Count > 0)
            {
                foreach (KeyValuePair<string, BasicInfo> data in gameData)
                {
                    // 加入
                    this.GameData[data.Key] = data.Value;

                    string webUrl = string.Format(sWebUrl1, data.Value.WebID.Substring(4, 4), data.Value.WebID.Substring(9, 2), data.Value.WebID.Substring(12, 2), data.Key); 
                    // 下載比賽資料
                    if (!this.DownReal.ContainsKey(data.Key))
                    {
                        this.DownReal[data.Key] = new BasicDownload(this.Sport, webUrl, data.Value.WebID);
                        this.DownReal[data.Key].DownloadString();
                    }
                    // 處理比賽資料
                    if (this.DownReal.ContainsKey(data.Key) &&
                        !string.IsNullOrEmpty(this.DownReal[data.Key].Data))
                    {
                        BasicInfo gameInfo = this.GetDataByMilbForGame(this.DownReal[data.Key].Data, data.Key);
                        // 加入
                        if (gameInfo != null)
                        {
                            gameInfo.IsBall = true;
                            gameInfo.Away = data.Value.Away;
                            gameInfo.Home = data.Value.Home;
                            this.GameData[data.Key] = gameInfo;
                        }
                    }
                }
                result = gameData.Count;
            }
            // 傳回
            return result;
        }
        // 下載資料
        private BasicDownload DownHome;
        private Dictionary<string, BasicDownload> DownReal;
        private DateTime DownLastTime = DateTime.Now;
    }
}
