using Follow.Sports.Basic;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Forms;

// 跟盤以 ID 為依據
// 跟盤日期以美國時間顯示

namespace Follow.Sports
{
    public class FbNFL : Basic.BasicFbAf
    {
        public FbNFL(DateTime today) : base(ESport.Football_NFL)
        {
            // 設定
            this.AllianceID = 1;
            this.GameType = "AFUS";
            int diffTime = frmMain.GetGameSourceTime("EasternTime");//取得與當地時間差(包含日光節約時間)
            if (diffTime > 0)
                this.GameDate = today.AddHours(-diffTime);
            else
                this.GameDate = GetUtcUsaEt(today);//取得美東時間
            if (string.IsNullOrWhiteSpace(this.sWebUrl))
            {
                this.sWebUrl = @"http://www.cbssports.com/nfl/scoreboard";
            }
            this.DownWeb = new WebBrowser();
            this.DownWeb.AllowNavigation = false;
            this.DownWeb.AllowWebBrowserDrop = false;
            this.DownWeb.ScriptErrorsSuppressed = true;
            this.DownWeb.IsWebBrowserContextMenuEnabled = false;
            this.DownWeb.ScrollBarsEnabled = false;
            this.DownWeb.WebBrowserShortcutsEnabled = false;
            this.DownWeb.TabStop = false;
            this.DownWeb.Tag = this.sWebUrl;
        }
        public override void Download()
        {
            // 沒有資料或下載時間超過 10 分鐘才讀取首頁資料。
            if (this.DownWeb.Url == null ||
                DateTime.Now >= this.DownLastTime.AddMinutes(10))
            {
                // 沒有資料或最後更新時間超過 5 分鐘才讀取首頁資料。
                if (this.DownWeb.Url == null ||
                    DateTime.Now >= this.UpdateLastTime.AddMinutes(5))
                {
                    // 讀取網頁
                    this.DownWeb.Navigate(this.DownWeb.Tag.ToString() + "?tk=" + DateTime.Now.Ticks.ToString());
                    this.DownLastTime = DateTime.Now;
                }
            }
        }
        public override int Follow()
        {
            // 沒有資料就離開
            if (this.DownWeb.Document == null ||
                this.DownWeb.Document.Body == null) return 0;
            // 不是 Live 就離開
            if (!this.FollowByWebIsLive) return 0;

            Logs.DownloadBrowser(this.DownWeb.Document.Body.InnerText);

            int result = 0;
            BasicInfo gameInfo = null;
            HtmlElementCollection divDoc = this.DownWeb.Document.Body.GetElementsByTagName("span");
            // 資料
            foreach (HtmlElement div in divDoc)
            {
                if (div.Id != null && div.Id.IndexOf("board") == 0)
                {
                    // 跟盤 ID
                    string webID = div.Id.Replace("board", "").Trim();
                    List<List<string>> teamBoard = this.FollowByWebTeamBoard(webID);

                    // 判斷分數
                    if (teamBoard != null && teamBoard.Count > 0 && teamBoard[1].Count > 0)
                    {
                        // 建立比賽資料
                        gameInfo = null;
                        gameInfo = new BasicInfo(this.AllianceID, this.GameType, this.GameDate, webID);
                        gameInfo.Away = "Away";
                        gameInfo.Home = "Home";

                        #region 分數
                        // 客隊
                        for (int i = 0; i < teamBoard[1].Count; i++)
                        {
                            gameInfo.AwayBoard.Add(teamBoard[1][i]);
                        }
                        // 總分
                        gameInfo.AwayPoint = teamBoard[1][teamBoard[1].Count - 1];
                        gameInfo.AwayBoard.RemoveAt(gameInfo.AwayBoard.Count - 1);

                        // 主隊
                        for (int i = 0; i < teamBoard[2].Count; i++)
                        {
                            gameInfo.HomeBoard.Add(teamBoard[2][i]);
                        }
                        // 總分
                        gameInfo.HomePoint = teamBoard[2][teamBoard[2].Count - 1];
                        gameInfo.HomeBoard.RemoveAt(gameInfo.HomeBoard.Count - 1);
                        #endregion
                        #region 比賽狀態
                        if (teamBoard[0][0].ToLower().IndexOf("final") != -1)
                        {
                            gameInfo.GameStates = "E";
                            gameInfo.Status = "結束";
                        }
                        else if (teamBoard[0][0].ToLower().IndexOf("postponed") != -1)
                        {
                            gameInfo.GameStates = "P";
                            gameInfo.Status = "中止";
                        }
                        else if (teamBoard[0][0].ToLower().IndexOf("delay") != -1)
                        {
                            gameInfo.GameStates = "D";
                            gameInfo.Status = "DELAY";
                        }
                        else if (teamBoard[0][0].ToLower().IndexOf("halftime") != -1)
                        {
                            gameInfo.GameStates = "S";
                            gameInfo.Status = "中場休息";
                        }
                        else if (gameInfo.Quarter > 0)
                        {
                            DateTime dtTimer = DateTime.Now;
                            // 轉換成時間
                            if (DateTime.TryParse(teamBoard[0][0], out dtTimer))
                            {
                                gameInfo.GameStates = "S";
                                gameInfo.Status = teamBoard[0][0];
                            }
                            else
                            {
                                // 比賽未開始
                                gameInfo.AwayBoard.Clear();
                                gameInfo.AwayPoint = "";
                                gameInfo.HomeBoard.Clear();
                                gameInfo.HomePoint = "";
                            }
                            
                        }
                        #endregion

                        // 加入
                        this.GameData[gameInfo.WebID] = gameInfo;
                        // 累計
                        result++;
                    }
                }
            }
            // 傳回
            return result;
        }
        private bool FollowByWebIsLive
        {
            get
            {
                bool result = false;
                // 判斷是否有資料
                if (this.DownWeb.Document != null)
                {
                    HtmlElement live = this.DownWeb.Document.GetElementById("live");
                    // 有找到
                    if (live != null && !string.IsNullOrEmpty(live.InnerText))
                    {
                        //Console.WriteLine(live.InnerText);
                        // 完整判斷
                        if (!result && (live.InnerText.IndexOf("No need to refresh page as stats will update LIVE!") != -1)) result = true;
                        if (!result && (live.InnerText.IndexOf("No need to refresh page as stats will update LIVE") != -1)) result = true;
                        // 完整判斷 小寫
                        if (!result && (live.InnerText.ToLower().IndexOf("no need to refresh page as stats will update live!") != -1)) result = true;
                        if (!result && (live.InnerText.ToLower().IndexOf("no need to refresh page as stats will update live") != -1)) result = true;
                    }
                }
                // 傳回值
                return result;
            }
        }
        private List<List<string>> FollowByWebTeamBoard(string webID, bool isReFun = true)
        {
            List<List<string>> teamBoard = new List<List<string>>();
            string funStatus = @"g_" + webID + "_Status";
            string funAwayBoard = @"g_" + webID + "_AwayBoard";
            string funHomeBoard = @"g_" + webID + "_HomeBoard";
            // 錯誤處理
            try
            {
                string status = this.DownWeb.Document.InvokeScript(funStatus).ToString();
                string awayBoard = this.DownWeb.Document.InvokeScript(funAwayBoard).ToString();
                string homeBoard = this.DownWeb.Document.InvokeScript(funHomeBoard).ToString();
                List<string> lst;

                // Status
                lst = new List<string>();
                lst.Add(status);
                teamBoard.Add(lst);

                // AwayBoard
                lst = new List<string>();
                foreach (string num in awayBoard.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries))
                {
                    lst.Add(num);
                }
                teamBoard.Add(lst);

                // HomeBoard
                lst = new List<string>();
                foreach (string num in homeBoard.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries))
                {
                    lst.Add(num);
                }
                teamBoard.Add(lst);
            }
            catch // 錯誤，這可能是因為尚未加入 Script
            {
                #region Script
                HtmlElement ele;
                ele = null;
                ele = this.DownWeb.Document.CreateElement("script");
                // 狀態
                ele.SetAttribute("type", @"text/javascript");
                ele.SetAttribute("text", @"function " + funStatus + "(){return g[" + webID + "][16];}");
                this.DownWeb.Document.Body.AppendChild(ele);

                ele = null;
                ele = this.DownWeb.Document.CreateElement("script");
                // AwayBoard
                ele.SetAttribute("type", @"text/javascript");
                ele.SetAttribute("text", @"function " + funAwayBoard + "(){return g[" + webID + "][27] + ',' + g[" + webID + "][17];}");
                // 加入
                this.DownWeb.Document.Body.AppendChild(ele);

                ele = null;
                ele = this.DownWeb.Document.CreateElement("script");
                // 設定
                ele.SetAttribute("type", @"text/javascript");
                ele.SetAttribute("text", @"function " + funHomeBoard + "(){return g[" + webID + "][28] + ',' + g[" + webID + "][18];}");
                // 加入
                this.DownWeb.Document.Body.AppendChild(ele);
                #endregion

                // 重讀，這是為了避免讀取錯誤，所以再執行一次
                if (isReFun) teamBoard = this.FollowByWebTeamBoard(webID, false);
            }
            // 傳回
            return teamBoard;
        }
        // 下載資料
        private WebBrowser DownWeb;
        private DateTime DownLastTime = DateTime.Now;
    }
}
