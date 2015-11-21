using Follow.Sports.Basic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Follow.Sports
{
    /// <summary>
    /// 澳洲棒球 
    /// </summary>
    class BbABL : Basic.BasicBaseball
    {
        // 下載資料
        private BasicDownload DownHome;
        private DateTime DownLastTime = DateTime.Now;

        public BbABL(DateTime today)
            : base(ESport.Baseball_ABL)
        {
            // 設定
            this.AllianceID = 104;
            this.GameType = "BBAU";
            //int diffTime = frmMain.GetGameSourceTime("EasternTime");//取得與當地時間差(包含日光節約時間)
            //if (diffTime > 0)
            this.GameDate = today.AddHours(2);//澳洲 时间
            //else
            //    this.GameDate = GetUtcUsaEt(today);//

            //如果没有配置就使用默认来源网
            if (string.IsNullOrWhiteSpace(this.sWebUrl))
            {
                this.sWebUrl = "http://web.theabl.com.au/gdcross/components/game/win/year_{0}/month_{1}/day_{2}/master_scoreboard.xml";
            }

            this.DownHome = new BasicDownload(this.Sport, string.Format(this.sWebUrl, this.GameDate.ToString("yyyy"), this.GameDate.ToString("MM"), this.GameDate.ToString("dd")));
        }
        public override void Download()
        {
            // 沒有資料  或者 超过上次下载时间2秒(最少只能2秒下载一次)
            if (this.GameData.Count == 0 ||
                DateTime.Now > this.DownLastTime.AddSeconds(2))
            {
                this.DownHome.DownloadString();
                this.DownLastTime = DateTime.Now;
            }
        }

        public override int Follow()
        {
            // 沒有資料就離開
            if (string.IsNullOrEmpty(this.DownHome.Data)) return 0;

            int result = 0;
            // 取得資料
            Dictionary<string, BasicInfo> gameData = this.GetDataByABLGame(this.DownHome.Data);
            foreach (KeyValuePair<string, BasicInfo> data in gameData)
            {
                if (data.Value != null)
                {
                    this.GameData[data.Key] = data.Value;
                }
            }
            result = gameData.Count;
            return result;
        }
    }
}
