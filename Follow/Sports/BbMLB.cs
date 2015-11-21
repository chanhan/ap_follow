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
    public class BbMLB : Basic.BasicBaseball
    {
        int cnm = 0;
        public BbMLB(DateTime today)
            : base(ESport.Baseball_MLB)
        {
            // 設定
            this.AllianceID = 53;
            this.GameType = "BBUS";
            //this.GameDate = GetUtcUsaEt(today).Date; // 只取日期
            int diffTime = frmMain.GetGameSourceTime("EasternTime");//取得與當地時間差(包含日光節約時間)
            if (diffTime > 0)
                this.GameDate = today.AddHours(-diffTime);
            else
                this.GameDate = GetUtcUsaEt(today);//取得美東時間

            if (string.IsNullOrWhiteSpace(this.sWebUrl))
            {
                this.sWebUrl = @"http://www.cbssports.com/mlb/scoreboard";
            }
            this.DownHome = new BasicDownload(this.Sport, this.sWebUrl);
            //this.DownHome = new BasicDownload(this.Sport, string.Format(@"http://mlb.mlb.com/gdcross/components/game/mlb/year_{0}/month_{1}/day_{2}/master_scoreboard.json", this.GameDate.ToString("yyyy"), this.GameDate.ToString("MM"), this.GameDate.ToString("dd")));
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
            //this.DownloadByBasic();
            this.DownloadByWeb();
        }
        private void DownloadByBasic()
        {
            // 讀取首頁資料
            this.DownHome.DownloadString();
            this.DownLastTime = DateTime.Now;
        }
        private void DownloadByWeb()
        {
            // 沒有資料或下載時間超過 10 分鐘才讀取首頁資料。
            if (this.DownWeb.Url == null ||
                DateTime.Now >= this.DownLastTime.AddMinutes(10))
            {
                // 沒有資料或最後更新時間超過 5 分鐘才讀取首頁資料。
                if (this.DownWeb.Url == null ||
                    DateTime.Now >= this.UpdateLastTime.AddMinutes(5))
                {
                    cnm++;
                    // 讀取網頁
                    this.DownWeb.Navigate(this.DownWeb.Tag.ToString() + "?tk=" + DateTime.Now.Ticks.ToString());
                    jsList.Clear();
                    this.DownLastTime = DateTime.Now;
                }
            }
        }
        public override int Follow()
        {
            //return this.FollowByBasicToCBS();
            //return this.FollowByBasicToMLB();
            return this.FollowByWeb();
        }
        private int FollowByBasicToCBS()
        {
            // 沒有資料就離開
            if (string.IsNullOrEmpty(this.DownHome.Data)) return 0;

            int result = 0;
            BasicInfo gameInfo = null;
            HtmlAgilityPack.HtmlDocument document = new HtmlAgilityPack.HtmlDocument();
            string xPath = "/html[1]/body[1]/div[3]/div[3]/div[1]/div[2]/div[1]";
            // 載入資料
            document.LoadHtml(this.DownHome.Data);
            // 資料位置
            HtmlAgilityPack.HtmlNode nodeGames = document.DocumentNode.SelectSingleNode(xPath);
            // 判斷資料
            if (nodeGames != null && nodeGames.ChildNodes != null)
            {
                DateTime gameDate = this.GameDate;
                DateTime gameTime = this.GameDate;
                // 資料
                foreach (HtmlAgilityPack.HtmlNode gameCol in nodeGames.ChildNodes)
                {
                    // 不是資料就往下處理
                    if (gameCol.Name != "div") continue;
                    if (gameCol.GetAttributeValue("class", "").IndexOf("scoreBox") == -1) continue;
                    // 資料
                    foreach (HtmlAgilityPack.HtmlNode game in gameCol.ChildNodes)
                    {
                        #region span 隊伍與分數
                        if (game.Name == "span" &&
                            game.Id.IndexOf("board") != -1)
                        {
                            HtmlAgilityPack.HtmlNode table = game.ChildNodes[1];
                            // 跟盤 ID
                            string webID = game.Id.Replace("board", "").Trim();

                            // 建立比賽資料
                            gameInfo = null;
                            gameInfo = new BasicInfo(this.AllianceID, this.GameType, gameTime, webID);
                            gameInfo.IsBall = true;

                            #region 客隊
                            gameInfo.Away = table.ChildNodes[1].ChildNodes[0].InnerText;
                            // 截取
                            if (gameInfo.Away.IndexOf("(") != -1)
                            {
                                gameInfo.Away = gameInfo.Away.Substring(0, gameInfo.Away.IndexOf("(")).Trim();
                            }
                            // 分數
                            if (table.ChildNodes[1].ChildNodes.Count > 3)
                            {
                                for (int i = 1; i < table.ChildNodes[1].ChildNodes.Count - 3; i++)
                                {
                                    if (!string.IsNullOrEmpty(table.ChildNodes[1].ChildNodes[i].InnerText.Replace("&nbsp;", "")))
                                    {
                                        gameInfo.AwayBoard.Add(table.ChildNodes[1].ChildNodes[i].InnerText);
                                    }
                                }
                                // 總分
                                gameInfo.AwayPoint = table.ChildNodes[1].ChildNodes[table.ChildNodes[1].ChildNodes.Count - 3].InnerText;
                                gameInfo.AwayH = table.ChildNodes[1].ChildNodes[table.ChildNodes[1].ChildNodes.Count - 2].InnerText;
                                gameInfo.AwayE = table.ChildNodes[1].ChildNodes[table.ChildNodes[1].ChildNodes.Count - 1].InnerText;
                            }
                            #endregion
                            #region 主隊
                            gameInfo.Home = table.ChildNodes[2].ChildNodes[0].InnerText;
                            // 截取
                            if (gameInfo.Home.IndexOf("(") != -1)
                            {
                                gameInfo.Home = gameInfo.Home.Substring(0, gameInfo.Home.IndexOf("(")).Trim();
                            }
                            // 分數
                            if (table.ChildNodes[2].ChildNodes.Count > 3)
                            {
                                for (int i = 1; i < table.ChildNodes[2].ChildNodes.Count - 3; i++)
                                {
                                    if (!string.IsNullOrEmpty(table.ChildNodes[2].ChildNodes[i].InnerText.Replace("&nbsp;", "")))
                                    {
                                        gameInfo.HomeBoard.Add(table.ChildNodes[2].ChildNodes[i].InnerText);
                                    }
                                }
                                // 總分
                                gameInfo.HomePoint = table.ChildNodes[2].ChildNodes[table.ChildNodes[1].ChildNodes.Count - 3].InnerText;
                                gameInfo.HomeH = table.ChildNodes[2].ChildNodes[table.ChildNodes[1].ChildNodes.Count - 2].InnerText;
                                gameInfo.HomeE = table.ChildNodes[2].ChildNodes[table.ChildNodes[1].ChildNodes.Count - 1].InnerText;
                            }
                            #endregion
                            #region 比賽狀態
                            if (table.ChildNodes[0].ChildNodes[0].InnerText.IndexOf("Final") != -1)
                            {
                                gameInfo.GameStates = "E";
                                gameInfo.Status = "結束";
                                gameInfo.BallB = 0;
                                gameInfo.BallS = 0;
                                gameInfo.BallO = 0;
                                gameInfo.Bases = 0;
                            }
                            else if (gameInfo.Quarter > 0)
                            {
                                gameInfo.GameStates = "S";
                            }
                            #endregion

                            // 加入
                            this.GameData[gameInfo.WebID] = gameInfo;
                            // 累計
                            result++;
                        }
                        #endregion
                        #region div BSO Bases
                        if (gameInfo != null &&
                            gameInfo.GameStates == "S" &&
                            game.Name == "div" && game.Id == "alerts" + gameInfo.WebID &&
                            game.ChildNodes[0].ChildNodes[0].ChildNodes[1].ChildNodes.Count > 0)
                        {
                            HtmlAgilityPack.HtmlNode table = game.ChildNodes[0].ChildNodes[0].ChildNodes[1].ChildNodes[0].ChildNodes[0].ChildNodes[0];
                            // 判斷資料
                            if (table != null && table.ChildNodes.Count == 3)
                            {
                                // B
                                if (table.ChildNodes[0].ChildNodes[1].GetAttributeValue("class", "") != "none") gameInfo.BallB = 1;
                                if (table.ChildNodes[0].ChildNodes[3].GetAttributeValue("class", "") != "none") gameInfo.BallB = 2;
                                if (table.ChildNodes[0].ChildNodes[5].GetAttributeValue("class", "") != "none") gameInfo.BallB = 3;
                                // S
                                if (table.ChildNodes[1].ChildNodes[0].GetAttributeValue("class", "") != "none") gameInfo.BallS = 1;
                                if (table.ChildNodes[1].ChildNodes[2].GetAttributeValue("class", "") != "none") gameInfo.BallS = 2;
                                // OUT
                                if (table.ChildNodes[2].ChildNodes[0].GetAttributeValue("class", "") != "none") gameInfo.BallO = 1;
                                if (table.ChildNodes[2].ChildNodes[2].GetAttributeValue("class", "") != "none") gameInfo.BallO = 2;
                            }
                        }
                        #endregion
                    }
                }
            }
            // 傳回
            return result;
        }
        private int FollowByBasicToMLB()
        {
            // 沒有資料就離開
            if (string.IsNullOrEmpty(this.DownHome.Data)) return 0;

            int result = 0;
            BasicInfo gameInfo = null;
            JObject json = new JObject();
            // 載入資料
            json = JObject.Parse(this.DownHome.Data);
            // 判斷資料
            if (json["data"]["games"] != null &&
                json["data"]["games"]["game"] != null)
            {
                DateTime gameTime = this.GameDate;
                string webID = null;
                int num = 0;
                // 資料
                foreach (JObject game in (JArray)json["data"]["games"]["game"])
                {
                    // 日期時間錯誤就離開
                    if (!DateTime.TryParse(game["time_date"].ToString() + " " + game["ampm"].ToString(), out gameTime)) continue;
                    webID = game["gameday"].ToString();
                    // 建立比賽資料
                    gameInfo = null;
                    gameInfo = new BasicInfo(this.AllianceID, this.GameType, gameTime, webID);
                    gameInfo.IsBall = true;
                    gameInfo.Away = game["away_team_name"].ToString();
                    gameInfo.Home = game["home_team_name"].ToString();

                    if (game["linescore"] != null &&
                        game["status"] != null)
                    {
                        // 分數
                        if (game["linescore"]["inning"] is JObject)
                        {
                            if (game["linescore"]["inning"]["away"] != null) gameInfo.AwayBoard.Add((!string.IsNullOrEmpty(game["linescore"]["inning"]["away"].ToString())) ? (game["linescore"]["inning"]["away"].ToString()) : ("0"));
                            if (game["linescore"]["inning"]["home"] != null) gameInfo.HomeBoard.Add((!string.IsNullOrEmpty(game["linescore"]["inning"]["home"].ToString())) ? (game["linescore"]["inning"]["home"].ToString()) : ("0"));
                        }
                        else
                        {
                            foreach (JObject board in (JArray)game["linescore"]["inning"])
                            {
                                if (board["away"] != null) gameInfo.AwayBoard.Add((!string.IsNullOrEmpty(board["away"].ToString())) ? (board["away"].ToString()) : ("0"));
                                if (board["home"] != null) gameInfo.HomeBoard.Add((!string.IsNullOrEmpty(board["home"].ToString())) ? (board["home"].ToString()) : ("0"));
                            }
                        }
                        // R、H、E
                        gameInfo.AwayPoint = game["linescore"]["r"]["away"].ToString();
                        gameInfo.AwayH = game["linescore"]["h"]["away"].ToString();
                        gameInfo.AwayE = game["linescore"]["e"]["away"].ToString();
                        gameInfo.HomePoint = game["linescore"]["r"]["home"].ToString();
                        gameInfo.HomeH = game["linescore"]["h"]["home"].ToString();
                        gameInfo.HomeE = game["linescore"]["e"]["home"].ToString();
                        // 比賽狀態
                        if (game["status"]["status"].ToString().ToLower().IndexOf("final") != -1 ||
                            game["status"]["status"].ToString().ToLower().IndexOf("game over") != -1)
                        {
                            gameInfo.GameStates = "E";
                            // 比賽結束補 X
                            if (gameInfo.AwayBoard.Count != gameInfo.HomeBoard.Count)
                            {
                                gameInfo.HomeBoard.Add("X");
                            }
                        }
                        else if (game["status"]["status"].ToString().ToLower().IndexOf("pre-game") == -1)
                        {
                            gameInfo.GameStates = "S";
                            // BSO
                            if (int.TryParse(game["status"]["b"].ToString(), out num)) gameInfo.BallB = num;
                            if (int.TryParse(game["status"]["s"].ToString(), out num)) gameInfo.BallS = num;
                            if (int.TryParse(game["status"]["o"].ToString(), out num)) gameInfo.BallO = num;
                            if (game["runners_on_base"] != null &&
                                game["runners_on_base"]["status"] != null &&
                                int.TryParse(game["runners_on_base"]["status"].ToString(), out num))
                            {
                                gameInfo.Bases = num;
                            }
                        }
                    }

                    // 加入
                    this.GameData[gameInfo.WebID] = gameInfo;
                    // 累計
                    result++;
                }
            }
            // 傳回
            return result;
        }
        private int FollowByWeb()
        {
            cnm++;           
            // 沒有資料就離開
            if (this.DownWeb.Document == null ||
                this.DownWeb.Document.Body == null)
            {               
                return 0;
            }
            // 不是 Live 就離開
            //HtmlElementCollection CLOSEdiv =this.DownWeb.Document.Body.GetElementsByTagName("div");
            //foreach (HtmlElement item in CLOSEdiv)
            //{
            //    if (item.InnerHtml == "CLOSE") {
            //        item.InvokeMember("Click");
            //    }
            //}
            if (!this.FollowByWebIsLive) return 0;
            Logs.UpdateJson(this.DownWeb.Document.Body.InnerText);          

            int result = 0;
            BasicInfo gameInfo = null;
            HtmlElementCollection divDoc = this.DownWeb.Document.Body.GetElementsByTagName("span");
            // 資料
            foreach (HtmlElement div in divDoc)
            {
                if (div.InnerHtml == "MLB Pre-Season Power Rankings")
                {
                    HtmlElementCollection close = div.Parent.NextSibling.Children;
                    if (close.Count > 1 && close[1].InnerHtml == "CLOSE")
                    {
                        close[1].InvokeMember("Click");
                    }
                }
                if (div.Id != null && div.Id.IndexOf("board") == 0)
                {
                    // 跟盤 ID
                    string webID = div.Id.Replace("board", "").Trim();
                    List<List<string>> teamBoard = this.FollowByWebTeamBoard(webID);
                    List<string> bsoBases = this.FollowByWebBSOBases(webID);
                    //Logs.UpdateJson(this.DownWeb.Document.Body.InnerHtml + "/////=====///" + getTest("c_" + webID + "test") + "//" + getTest("g_" + webID + "_Status") + "//" + getTest("g_" + webID + "_AwayBoard") + "//" + getTest("g_" + webID + "_HomeBoard") + "//" + getTest("g_" + webID + "_BSOBases") + "//" + (this.DownWeb.Url == null).ToString());
                    // 判斷分數
                    if (teamBoard != null && teamBoard.Count > 0 && teamBoard[1].Count > 0)
                    {
                        // 建立比賽資料
                        gameInfo = null;
                        gameInfo = new BasicInfo(this.AllianceID, this.GameType, this.GameDate, webID);
                        gameInfo.IsBall = true;

                        #region 客隊
                        gameInfo.Away = "Away";
                        // 分數
                        if (teamBoard[1].Count > 0)
                        {
                            for (int i = 0; i < teamBoard[1].Count - 3; i++)
                            {
                                gameInfo.AwayBoard.Add(teamBoard[1][i]);
                            }
                            // 總分
                            gameInfo.AwayPoint = teamBoard[1][teamBoard[1].Count - 3];
                            gameInfo.AwayH = teamBoard[1][teamBoard[1].Count - 2];
                            gameInfo.AwayE = teamBoard[1][teamBoard[1].Count - 1];
                        }
                        #endregion
                        #region 主隊
                        gameInfo.Home = "Home";
                        // 分數
                        if (teamBoard[2].Count > 0)
                        {
                            for (int i = 0; i < teamBoard[2].Count - 3; i++)
                            {
                                gameInfo.HomeBoard.Add(teamBoard[2][i]);
                            }
                            // 總分
                            gameInfo.HomePoint = teamBoard[2][teamBoard[2].Count - 3];
                            gameInfo.HomeH = teamBoard[2][teamBoard[2].Count - 2];
                            gameInfo.HomeE = teamBoard[2][teamBoard[2].Count - 1];
                        }
                        #endregion
                        #region BSO Bases
                        if (bsoBases != null && bsoBases.Count == 6)
                        {
                            // BSO
                            gameInfo.BallB = int.Parse(bsoBases[0]);
                            gameInfo.BallS = int.Parse(bsoBases[1]);
                            gameInfo.BallO = int.Parse(bsoBases[2]);

                            int bases = 0;
                            // Bases
                            if (int.Parse(bsoBases[3]) > 0) bases = 1;
                            if (int.Parse(bsoBases[4]) > 0) bases ^= 2;
                            if (int.Parse(bsoBases[5]) > 0) bases ^= 4;
                            gameInfo.Bases = bases;
                        }
                        #endregion
                        #region 比賽狀態
                        if (teamBoard[0][0].ToLower().IndexOf("final") != -1)
                        {
                            gameInfo.GameStates = "E";
                            gameInfo.Status = "結束";
                            gameInfo.BallB = 0;
                            gameInfo.BallS = 0;
                            gameInfo.BallO = 0;
                            gameInfo.Bases = 0;
                        }
                        else if (teamBoard[0][0].ToLower().IndexOf("postponed") != -1)
                        {
                            gameInfo.GameStates = "P";
                            gameInfo.Status = "中止";
                            gameInfo.TrackerText = "因雨延賽";
                            gameInfo.BallB = 0;
                            gameInfo.BallS = 0;
                            gameInfo.BallO = 0;
                            gameInfo.Bases = 0;
                        }
                        else if (teamBoard[0][0].ToLower().IndexOf("delay") != -1)
                        {
                            gameInfo.GameStates = "D";
                            gameInfo.Status = "DELAY";
                            gameInfo.BallB = 0;
                            gameInfo.BallS = 0;
                            gameInfo.BallO = 0;
                            gameInfo.Bases = 0;
                        }
                        else if (gameInfo.Quarter > 0)
                        {
                            gameInfo.GameStates = "S";
                        }
                        #endregion

                        // 加入
                        this.GameData[gameInfo.WebID] = gameInfo;                       
                        cnm = 0;
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

        public string getTest(string s){
            if (this.DownWeb.Document.InvokeScript(s)==null)
            {
                return s+"为NULL";
            }
            else
            {
                return s+"为"+this.DownWeb.Document.InvokeScript(s).ToString();
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
                if (!jsList.Contains(funStatus))
                {
                    ele = this.DownWeb.Document.CreateElement("script");
                    // 狀態
                    ele.SetAttribute("type", @"text/javascript");
                    ele.SetAttribute("text", @"function " + funStatus + "(){return g[" + webID + "][12];}");
                    this.DownWeb.Document.Body.AppendChild(ele);
                    jsList.Add(funStatus);
                }
                //ele = null;
                //if (!jsList.Contains("c_" + webID + "test"))
                //{
                //    ele = this.DownWeb.Document.CreateElement("script");
                //    // 狀態
                //    ele.SetAttribute("type", @"text/javascript");
                //    ele.SetAttribute("text", @"function c_" + webID + "test(){return g.length;}");
                //    this.DownWeb.Document.Body.AppendChild(ele);
                //    jsList.Add("c_" + webID + "test");
                //}
                ele = null;
                if (!jsList.Contains(funAwayBoard))
                {
                    ele = this.DownWeb.Document.CreateElement("script");
                    // AwayBoard
                    ele.SetAttribute("type", @"text/javascript");
                    ele.SetAttribute("text", @"function " + funAwayBoard + "(){return g[" + webID + "][13] + ',' + g[" + webID + "][14];}");
                    // 加入
                    this.DownWeb.Document.Body.AppendChild(ele);
                    jsList.Add(funAwayBoard);
                }
                ele = null;
                if (!jsList.Contains(funHomeBoard))
                {
                    ele = this.DownWeb.Document.CreateElement("script");
                    // 設定
                    ele.SetAttribute("type", @"text/javascript");
                    ele.SetAttribute("text", @"function " + funHomeBoard + "(){return g[" + webID + "][15] + ',' + g[" + webID + "][16];}");
                    // 加入
                    this.DownWeb.Document.Body.AppendChild(ele);
                    jsList.Add(funHomeBoard);
                }
                #endregion

                //if (!isReFun) this.DownWeb.Refresh();
                // 重讀，這是為了避免讀取錯誤，所以再執行一次
                if (isReFun) teamBoard = this.FollowByWebTeamBoard(webID, false);
            }
            // 傳回
            return teamBoard;
        }
        private List<string> FollowByWebBSOBases(string webID, bool isReFun = true)
        {
            List<string> bsoBases = new List<string>();
            string funBSOBases = @"g_" + webID + "_BSOBases";
            // 錯誤處理
            try
            {
                string bsob = this.DownWeb.Document.InvokeScript(funBSOBases).ToString();
                // BSO Bases
                foreach (string num in bsob.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries))
                {
                    bsoBases.Add(num);
                }
            }
            catch // 錯誤，這可能是因為尚未加入 Script
            {
                #region Script
                if (!jsList.Contains(funBSOBases))
                {
                    HtmlElement ele = this.DownWeb.Document.CreateElement("script");
                    // 設定
                    ele.SetAttribute("type", @"text/javascript");
                    ele.SetAttribute("text", @"function " + funBSOBases + "(){return g[" + webID + "][17] + ',' + g[" + webID + "][19];}");
                    // 加入
                    this.DownWeb.Document.Body.AppendChild(ele);
                    jsList.Add(funBSOBases);
                }
                #endregion
                //if (!isReFun) this.DownWeb.Refresh();
                // 重讀，這是為了避免讀取錯誤，所以再執行一次
                if (isReFun) bsoBases = this.FollowByWebBSOBases(webID, false);
            }
            // 傳回
            return bsoBases;
        }
        // 下載資料
        private BasicDownload DownHome;
        private DateTime DownLastTime = DateTime.Now;
        private WebBrowser DownWeb;
        private List<string> jsList = new List<string>();
    }
}
