using Follow.Sports.Basic;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

// 跟盤以 ID 為依據
// 跟盤日期以美國時間顯示

namespace Follow.Sports
{
    public class BkNBA : Basic.BasicBasketball
    {
        private string sWebUrl1 = UrlSetting.GetUrl(ESport.Basketball_NBA, "Url1");
        public BkNBA(DateTime today)
            : base(ESport.Basketball_NBA)
        {
            if (string.IsNullOrWhiteSpace(this.sWebUrl))
            {
                this.sWebUrl = @"http://insider.espn.go.com/nba/caster/realtime";
            }
            if (string.IsNullOrWhiteSpace(this.sWebUrl1))
            {
                this.sWebUrl1 = @"http://scores.espn.go.com/nba/scoreboard";
            }
            // 設定
            this.AllianceID = 1;
            this.GameType = "BKUS";
            int diffTime = frmMain.GetGameSourceTime("EasternTime");//取得與當地時間差(包含日光節約時間)
            if (diffTime > 0)
                this.GameDate = today.AddHours(-diffTime);
            else
                this.GameDate = GetUtcUsaEt(today);//取得美東時間

            this.DownHome = new BasicDownload(this.Sport, this.sWebUrl);

            this.DownReal = new BasicDownload(this.Sport, this.sWebUrl1);

            this.DownWeb = new WebBrowser();
            this.DownWeb.AllowNavigation = false;
            this.DownWeb.AllowWebBrowserDrop = false;
            this.DownWeb.ScriptErrorsSuppressed = false;
            this.DownWeb.IsWebBrowserContextMenuEnabled = false;
            this.DownWeb.ScrollBarsEnabled = false;
            this.DownWeb.WebBrowserShortcutsEnabled = false;
            this.DownWeb.TabStop = false;
            this.DownWeb.Tag = this.sWebUrl;


            this.DownWebScores = new WebBrowser();
            this.DownWebScores.AllowNavigation = false;
            this.DownWebScores.AllowWebBrowserDrop = false;
            this.DownWebScores.ScriptErrorsSuppressed = false;
            this.DownWebScores.IsWebBrowserContextMenuEnabled = false;
            this.DownWebScores.ScrollBarsEnabled = false;
            this.DownWebScores.WebBrowserShortcutsEnabled = false;
            this.DownWebScores.TabStop = false;
            this.DownWebScores.Tag = this.sWebUrl1;
        }
        public override void Download()
        {
            //this.DownloadByBasic();
            this.DownloadByWeb();
        }
        private void DownloadByBasic()//無使用
        {
            // 下載首頁資料
            this.DownHome.DownloadString();
            // 沒有資料或讀取時間超過 1 分鐘才讀取比賽資料。
            if (this.GameData.Count == 0 ||
                DateTime.Now > this.DownLastTime.AddMinutes(1))
            {
                this.DownReal.DownloadString();
                this.DownLastTime = DateTime.Now;
            }
        }
        private void DownloadByWeb()//NBA 的更新頻率要加快
        {
            #region http://insider.espn.go.com/nba/caster/realtime 資料
            // 沒有資料或下載時間超過 10 秒才讀取首頁資料。
            if (this.DownWeb.Url == null ||
                DateTime.Now >= this.DownLastTime.AddSeconds(10))
            {
                // 沒有資料或最後更新時間超過 5 秒才讀取首頁資料。
                if (this.DownWeb.Url == null ||
                    DateTime.Now >= this.UpdateLastTime.AddSeconds(5))
                {
                    // 讀取網頁
                    this.DownWeb.Navigate(this.DownWeb.Tag.ToString() + "?tk=" + DateTime.Now.Ticks.ToString());
                    this.DownLastTime = DateTime.Now;
                }
            }
            #endregion 資料

            #region http://scores.espn.go.com/nba/scoreboard

            // 沒有資料或讀取時間超過 10 秒才讀取比賽資料。
            if (this.GameData.Count == 0 ||
                DateTime.Now > this.DownLastTime.AddSeconds(10))
            {
                this.DownWebScores.Navigate(this.DownWebScores.Tag.ToString() + "?tk=" + DateTime.Now.Ticks.ToString());
                this.DownLastTime = DateTime.Now;
            }
            #endregion
        }
        public override int Follow()
        {
            //return this.FollowByBasic();
            return this.FollowByWeb();
        }
        private int FollowByBasic()
        {
            // 沒有資料就離開
            if (string.IsNullOrEmpty(this.DownHome.Data)) return 0;

            int result = 0;
            BasicInfo gameInfo = null;
            HtmlAgilityPack.HtmlDocument document = new HtmlAgilityPack.HtmlDocument();
            string xPath = "/html[1]/body[1]/div[4]";
            // 載入資料
            document.LoadHtml(this.DownHome.Data);
            // 資料位置
            HtmlAgilityPack.HtmlNode nodeGames = document.DocumentNode.SelectSingleNode(xPath);
            // 判斷資料
            if (nodeGames != null && nodeGames.ChildNodes != null)
            {
                DateTime gameDate = this.GameDate;
                DateTime gameTime = this.GameDate;
                int gameQuarter = 0;
                // 取得跟盤 ID
                List<string> webID = this.GetWebID(document);
                int webIndex = 0;
                // 取得比賽資料，比對資料來計算出各局分數
                Dictionary<string, BasicInfo> otherData = this.GetOtherData2();
                // 比賽資料
                foreach (HtmlAgilityPack.HtmlNode game in nodeGames.ChildNodes)
                {
                    // 不是資料就往下處理
                    if (game.Name != "div" || string.IsNullOrEmpty(game.Id)) continue;

                    // 建立比賽資料，跟盤 ID 就是之前取得的資料
                    gameInfo = null;
                    gameInfo = new BasicInfo(this.AllianceID, this.GameType, gameTime, webID[webIndex]);
                    gameQuarter = 0;
                    webIndex++;

                    #region 資料
                    foreach (HtmlAgilityPack.HtmlNode info in game.ChildNodes)
                    {
                        // 不是資料就往下處理
                        if (game.Name != "div" || string.IsNullOrEmpty(info.Id)) continue;

                        string id = info.Id.ToLower().Trim();
                        int board = 0;
                        // Away
                        if (id.IndexOf("awayteam") == 0)
                        {
                            gameInfo.Away = info.InnerText;
                        }
                        // Away 分數
                        if (id.IndexOf("awayscore") == 0 && int.TryParse(info.InnerText, out board))
                        {
                            gameInfo.AwayBoard.Add(board.ToString());
                            gameInfo.AwayPoint = board.ToString();
                        }
                        // Home
                        if (id.IndexOf("hometeam") == 0)
                        {
                            gameInfo.Home = info.InnerText;
                        }
                        // Home 分數
                        if (id.IndexOf("homescore") == 0 && int.TryParse(info.InnerText, out board))
                        {
                            gameInfo.HomeBoard.Add(board.ToString());
                            gameInfo.HomePoint = board.ToString();
                        }
                        // Status
                        if (id.IndexOf("clock") == 0)
                        {
                            gameInfo.Status = info.InnerText.ToLower();
                            // 判斷目前局數
                            if (gameInfo.Status.IndexOf("1st") != -1) gameQuarter = 1;
                            if (gameInfo.Status.IndexOf("2nd") != -1) gameQuarter = 2;
                            if (gameInfo.Status.IndexOf("half") != -1) gameQuarter = 2;
                            if (gameInfo.Status.IndexOf("3rd") != -1) gameQuarter = 3;
                            if (gameInfo.Status.IndexOf("4th") != -1) gameQuarter = 4;
                            if (gameInfo.Status.IndexOf("ot") != -1) gameQuarter = 5;
                            // 判斷
                            if (gameInfo.Status.IndexOf("final") != -1)
                            {
                                gameInfo.GameStates = "E";
                                gameInfo.Status = "結束";
                            }
                            else if (gameQuarter > 0) // 有局數才有比賽
                            {
                                gameInfo.GameStates = "S";
                                // 取代文字
                                gameInfo.Status = gameInfo.Status.Replace("halftime", "中場休息");
                                gameInfo.Status = gameInfo.Status.Replace("half", "中場休息");
                                gameInfo.Status = gameInfo.Status.Replace("qtr", "");
                                gameInfo.Status = gameInfo.Status.Replace("1st", "");
                                gameInfo.Status = gameInfo.Status.Replace("2nd", "");
                                gameInfo.Status = gameInfo.Status.Replace("3rd", "");
                                gameInfo.Status = gameInfo.Status.Replace("4th", "");
                                gameInfo.Status = gameInfo.Status.Replace("ot", "");
                                gameInfo.Status = gameInfo.Status.Replace("2ot", "");
                                gameInfo.Status = gameInfo.Status.Replace("of", "");
                                gameInfo.Status = gameInfo.Status.Replace("end", "結束");
                                gameInfo.Status = gameInfo.Status.Trim();
                                if (gameInfo.Status == "0:00")
                                {
                                    gameInfo.Status = "結束";
                                }
                            }
                        }
                    }
                    #endregion
                    #region 分數
                    if (otherData != null &&
                        otherData.ContainsKey(gameInfo.WebID) &&
                        otherData[gameInfo.WebID].Quarter > 0)
                    {
                        // 以比賽資料的局為基礎
                        for (int i = gameInfo.AwayBoard.Count; i < otherData[gameInfo.WebID].AwayBoard.Count; i++)
                        {
                            gameInfo.AwayBoard.Add("0");
                            gameInfo.HomeBoard.Add("0");
                        }
                        int point = 0;
                        // 補上缺少的局
                        if (gameQuarter > gameInfo.AwayBoard.Count)
                        {
                            gameInfo.AwayBoard.Add("0");
                            gameInfo.HomeBoard.Add("0");
                        }
                        // 計算正確的分數
                        // 總分扣除各局分數，最後一局不計算，結果就是最後一局的分數
                        // Away
                        point = int.Parse(gameInfo.AwayPoint);
                        for (int i = 0; i < otherData[gameInfo.WebID].AwayBoard.Count; i++)
                        {
                            gameInfo.AwayBoard[i] = otherData[gameInfo.WebID].AwayBoard[i];
                            point -= int.Parse(gameInfo.AwayBoard[i]);
                        }
                        if (point != 0) // 還有分數，加入最後一局
                        {
                            point += int.Parse(gameInfo.AwayBoard[gameInfo.AwayBoard.Count - 1]);
                            gameInfo.AwayBoard[gameInfo.AwayBoard.Count - 1] = point.ToString();
                        }
                        // Home
                        point = int.Parse(gameInfo.HomePoint);
                        for (int i = 0; i < otherData[gameInfo.WebID].HomeBoard.Count; i++)
                        {
                            gameInfo.HomeBoard[i] = otherData[gameInfo.WebID].HomeBoard[i];
                            point -= int.Parse(gameInfo.HomeBoard[i]);
                        }
                        if (point != 0) // 還有分數，加入最後一局
                        {
                            point += int.Parse(gameInfo.HomeBoard[gameInfo.HomeBoard.Count - 1]);
                            gameInfo.HomeBoard[gameInfo.HomeBoard.Count - 1] = point.ToString();
                        }
                    }
                    #endregion

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
            // 沒有資料就離開
            if (this.DownWeb.Document == null ||
                this.DownWeb.Document.Body == null) return 0;

            Logs.DownloadBrowser(this.DownWeb.Document.Body.InnerText);

            int result = 0;
            BasicInfo gameInfo = null;
            List<string> webID = this.GetWebID();
            // 判斷跟盤 ID
            if (webID != null && webID.Count > 0)
            {
                DateTime gameDate = this.GameDate;
                DateTime gameTime = this.GameDate;
                int gameQuarter = 0;
                // 取得比賽資料，比對資料來計算出各局分數
                Dictionary<string, BasicInfo> otherData = this.GetOtherData2();
                // 資料
                for (int webIndex = 0; webIndex < webID.Count; webIndex++)
                {
                    // 建立比賽資料，跟盤 ID 就是之前取得的資料
                    gameInfo = null;
                    gameInfo = new BasicInfo(this.AllianceID, this.GameType, gameTime, webID[webIndex]);
                    gameQuarter = 0;

                    #region 資料
                    HtmlElement div = null;
                    // Away
                    div = this.DownWeb.Document.GetElementById("awayTeam" + (webIndex + 1).ToString());
                    if (div != null && div.InnerText != null && !string.IsNullOrEmpty(div.InnerText.Trim()))
                    {
                        gameInfo.Away = div.InnerText.Trim();
                    }
                    // Away 分數
                    div = this.DownWeb.Document.GetElementById("awayScore" + (webIndex + 1).ToString());
                    if (div != null && div.InnerText != null && !string.IsNullOrEmpty(div.InnerText.Trim()))
                    {
                        gameInfo.AwayBoard.Add(div.InnerText.Trim());
                        gameInfo.AwayPoint = div.InnerText.Trim();
                    }
                    // Home
                    div = this.DownWeb.Document.GetElementById("homeTeam" + (webIndex + 1).ToString());
                    if (div != null && div.InnerText != null && !string.IsNullOrEmpty(div.InnerText.Trim()))
                    {
                        gameInfo.Home = div.InnerText.Trim();
                    }
                    // Home 分數
                    div = this.DownWeb.Document.GetElementById("homeScore" + (webIndex + 1).ToString());
                    if (div != null && div.InnerText != null && !string.IsNullOrEmpty(div.InnerText.Trim()))
                    {
                        gameInfo.HomeBoard.Add(div.InnerText.Trim());
                        gameInfo.HomePoint = div.InnerText.Trim();
                    }
                    // Status
                    div = this.DownWeb.Document.GetElementById("clock" + (webIndex + 1).ToString());
                    if (div != null && div.InnerText != null && !string.IsNullOrEmpty(div.InnerText.Trim()))
                    {
                        gameInfo.Status = div.InnerText.Trim().Replace("\r\n", " ").Replace("<br>", " ").ToLower();
                        if (otherData != null && otherData.ContainsKey(gameInfo.WebID) &&
                            (otherData[gameInfo.WebID].Status.IndexOf("final") != -1 ||
                            otherData[gameInfo.WebID].Status.IndexOf("end") != -1))
                            gameInfo.Status = otherData[gameInfo.WebID].Status;

                        // 判斷目前局數
                        if (gameInfo.Status.IndexOf("1st") != -1) gameQuarter = 1;
                        if (gameInfo.Status.IndexOf("2nd") != -1) gameQuarter = 2;
                        if (gameInfo.Status.IndexOf("half") != -1) gameQuarter = 2;
                        if (gameInfo.Status.IndexOf("3rd") != -1) gameQuarter = 3;
                        if (gameInfo.Status.IndexOf("4th") != -1) gameQuarter = 4;
                        if (gameInfo.Status.IndexOf("ot") != -1) gameQuarter = 5;
                        // 判斷
                        if (gameInfo.Status.IndexOf("final") != -1)
                        {
                            gameInfo.GameStates = "E";
                            gameInfo.Status = "結束";
                        }
                        else if (gameQuarter > 0) // 有局數才有比賽
                        {
                            gameInfo.GameStates = "S";
                        }
                    }
                    #endregion
                    #region 分數
                    if (otherData != null &&
                        otherData.ContainsKey(gameInfo.WebID) &&
                        otherData[gameInfo.WebID].Quarter > 0)
                    {
                        int awayPoint1 = 0;
                        int awayPoint2 = 0;
                        int homePoint1 = 0;
                        int homePoint2 = 0;
                        // 正常情況下，內頁分數更新會比外頁快，所以分數會比較大
                        // 但是如果內頁停止更新分數了，外頁分數就可能比內頁高
                        // 這種情況下，就以外頁的分數為主
                        if (int.TryParse(gameInfo.AwayPoint, out awayPoint1) &&
                            int.TryParse(gameInfo.HomePoint, out homePoint1) &&
                            int.TryParse(otherData[gameInfo.WebID].AwayPoint, out awayPoint2) &&
                            int.TryParse(otherData[gameInfo.WebID].HomePoint, out homePoint2) &&
                            awayPoint2 > awayPoint1 || homePoint2 > homePoint1)
                        {
                            gameInfo.AwayPoint = otherData[gameInfo.WebID].AwayPoint;
                            gameInfo.AwayBoard[0] = otherData[gameInfo.WebID].AwayPoint;
                            gameInfo.HomePoint = otherData[gameInfo.WebID].HomePoint;
                            gameInfo.HomeBoard[0] = otherData[gameInfo.WebID].HomePoint;
                            gameInfo.Status = otherData[gameInfo.WebID].Status;
                        }
                        // 以比賽資料的局為基礎
                        for (int i = gameInfo.AwayBoard.Count; i < otherData[gameInfo.WebID].AwayBoard.Count; i++)
                        {
                            gameInfo.AwayBoard.Add("0");
                            gameInfo.HomeBoard.Add("0");
                        }
                        int point = 0;
                        // 補上缺少的局
                        while (gameQuarter > gameInfo.AwayBoard.Count)
                        {
                            gameInfo.AwayBoard.Add("0");
                            gameInfo.HomeBoard.Add("0");
                        }
                        // 計算正確的分數
                        // 總分扣除各局分數，最後一局不計算，結果就是最後一局的分數
                        // Away
                        int tryIntParse = 0;
                        if (Int32.TryParse(gameInfo.AwayPoint, out point))
                        {
                            for (int i = 0; i < otherData[gameInfo.WebID].AwayBoard.Count; i++)
                            {
                                gameInfo.AwayBoard[i] = otherData[gameInfo.WebID].AwayBoard[i];
                                if (Int32.TryParse(gameInfo.AwayBoard[i], out tryIntParse))
                                    point -= tryIntParse;
                            }
                            if (point > 0) // 還有分數，加入最後一局
                            {
                                int offset = gameInfo.AwayBoard.Count - 1;
                                if (offset >= 0 && Int32.TryParse(gameInfo.AwayBoard[offset], out tryIntParse))
                                {
                                    point += tryIntParse;
                                    gameInfo.AwayBoard[offset] = point.ToString();
                                }
                            }
                        }

                        // Home
                        if (Int32.TryParse(gameInfo.HomePoint, out point))
                        {
                            for (int i = 0; i < otherData[gameInfo.WebID].HomeBoard.Count; i++)
                            {
                                gameInfo.HomeBoard[i] = otherData[gameInfo.WebID].HomeBoard[i];
                                if (Int32.TryParse(gameInfo.HomeBoard[i], out tryIntParse))
                                    point -= tryIntParse;
                            }

                            if (point > 0) // 還有分數，加入最後一局
                            {
                                int offset = gameInfo.HomeBoard.Count - 1;
                                if (offset >= 0 && Int32.TryParse(gameInfo.HomeBoard[offset], out tryIntParse))
                                {
                                    point += tryIntParse;
                                    gameInfo.HomeBoard[offset] = point.ToString();
                                }
                            }
                        }
                    }
                    #endregion
                    // 取代文字
                    if (!string.IsNullOrEmpty(gameInfo.Status))
                    {
                        gameInfo.Status = gameInfo.Status.Replace("halftime", "中場休息");
                        gameInfo.Status = gameInfo.Status.Replace("half", "中場休息");
                        gameInfo.Status = gameInfo.Status.Replace("qtr", "");
                        gameInfo.Status = gameInfo.Status.Replace("1st", "");
                        gameInfo.Status = gameInfo.Status.Replace("2nd", "");
                        gameInfo.Status = gameInfo.Status.Replace("3rd", "");
                        gameInfo.Status = gameInfo.Status.Replace("4th", "");
                        gameInfo.Status = gameInfo.Status.Replace("ot", "");
                        gameInfo.Status = gameInfo.Status.Replace("2ot", "");
                        gameInfo.Status = gameInfo.Status.Replace("of", "");
                        gameInfo.Status = gameInfo.Status.Replace("end", "結束");
                        gameInfo.Status = gameInfo.Status.Trim();
                        if (gameInfo.Status == "0:00")
                        {
                            gameInfo.Status = "結束";
                        }
                    }

                    //if (otherData.ContainsKey(gameInfo.WebID))
                    //{
                    //    if (!string.IsNullOrEmpty(otherData[gameInfo.WebID].Status))
                    //    {
                    //        otherData[gameInfo.WebID].Status = otherData[gameInfo.WebID].Status.Replace("halftime", "中場休息");
                    //        otherData[gameInfo.WebID].Status = otherData[gameInfo.WebID].Status.Replace("half", "中場休息");
                    //        otherData[gameInfo.WebID].Status = otherData[gameInfo.WebID].Status.Replace("qtr", "");
                    //        otherData[gameInfo.WebID].Status = otherData[gameInfo.WebID].Status.Replace("1st", "");
                    //        otherData[gameInfo.WebID].Status = otherData[gameInfo.WebID].Status.Replace("2nd", "");
                    //        otherData[gameInfo.WebID].Status = otherData[gameInfo.WebID].Status.Replace("3rd", "");
                    //        otherData[gameInfo.WebID].Status = otherData[gameInfo.WebID].Status.Replace("4th", "");
                    //        otherData[gameInfo.WebID].Status = otherData[gameInfo.WebID].Status.Replace("ot", "");
                    //        otherData[gameInfo.WebID].Status = otherData[gameInfo.WebID].Status.Replace("2ot", "");
                    //        otherData[gameInfo.WebID].Status = otherData[gameInfo.WebID].Status.Replace("of", "");
                    //        otherData[gameInfo.WebID].Status = otherData[gameInfo.WebID].Status.Replace("end", "結束");
                    //        otherData[gameInfo.WebID].Status = otherData[gameInfo.WebID].Status.Replace("final", "結束");
                    //        otherData[gameInfo.WebID].Status = otherData[gameInfo.WebID].Status.Trim();
                    //        if (otherData[gameInfo.WebID].Status == "0:00")
                    //        {
                    //            otherData[gameInfo.WebID].Status = "結束";
                    //        }
                    //    }
                    //    if (otherData[gameInfo.WebID].Status != gameInfo.Status)
                    //    {
                    //        string[] gameInfoTime = gameInfo.Status.Split(new char[] { ':' });
                    //        string[] otherDataTime = otherData[gameInfo.WebID].Status.Split(new char[] { ':' });
                    //        if (gameInfoTime.Length == 2 && otherDataTime.Length == 2)
                    //        {
                    //            int iGameTimeMin, iGameTimeSec;
                    //            int iOtherTimeMin, iOtherTimeSec;
                    //            if (int.TryParse(gameInfoTime[0], out iGameTimeMin) && int.TryParse(gameInfoTime[1], out iGameTimeSec) &&
                    //               int.TryParse(otherDataTime[0], out iOtherTimeMin) && int.TryParse(otherDataTime[1], out iOtherTimeSec))
                    //            {
                    //                if (iGameTimeMin * 60 + iGameTimeSec > iOtherTimeMin * 60 + iOtherTimeSec)
                    //                    gameInfo.Status = otherData[gameInfo.WebID].Status;
                    //            }
                    //        }                            
                    //    }
                    //}

                    ///分数中出现 -  横杠 就不加入更新 直接跳过
                    if (gameInfo.HomeBoard.IndexOf("-") != -1 || gameInfo.AwayBoard.IndexOf("-") != -1)
                    {
                        continue;
                    }

                    //因为gameInfo必须和otherData的数据进行整合
                    //如果otherData为null
                    //gameInfo数据是错误的，就不需要加入
                    //add by le
                    if (otherData != null &&
                        otherData.ContainsKey(gameInfo.WebID))
                    {
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
        // 下載資料
        private BasicDownload DownHome;
        private BasicDownload DownReal;
        private DateTime DownLastTime = DateTime.Now;
        private WebBrowser DownWeb;
        private WebBrowser DownWebScores;
        // 取得跟盤 ID
        private List<string> GetWebID(HtmlAgilityPack.HtmlDocument document)
        {
            List<string> webID = new List<string>();
            string xPath = "/html[1]/script[1]";
            // 資料位置
            HtmlAgilityPack.HtmlNode nodeGames = document.DocumentNode.SelectSingleNode(xPath);
            // 判斷資料
            if (nodeGames != null && !string.IsNullOrEmpty(nodeGames.InnerText))
            {
                string script = nodeGames.InnerText;
                string[] scriptData = script.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
                int index = 1; // 索引位置
                // 資料
                foreach (string data in scriptData)
                {
                    // 判斷 ID
                    if (data.ToLower().IndexOf(string.Format("gametable[{0}]", index)) == 0)
                    {
                        string id = data.Substring(data.LastIndexOf("=") + 1).Replace(";", "").Trim();
                        // 加入
                        webID.Add(id);
                        // 累計
                        index++;
                    }
                }
            }
            // 傳回
            return webID;
        }
        private List<string> GetWebID(bool isReFun = true)
        {
            List<string> webID = new List<string>();
            // 錯誤處理
            try
            {
                string id = this.DownWeb.Document.InvokeScript("getWebID").ToString();
                // 判斷資料
                if (!string.IsNullOrEmpty(id))
                {
                    foreach (string str in id.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        // 加入
                        webID.Add(str.Trim());
                    }
                }
            }
            catch // 錯誤，這可能是因為尚未加入 Script
            {
                #region Script
                string myFunction = "function getWebID() {"
                                  + "var games=\"\";"
                                  + "for(var i=1;i<= gameCount;i++){"
                                  + "games+= \",\"+gameTable[i].toString();"
                                  + "}"
                                  + "return games;"
                                  + "}";
                HtmlElement ele;
                ele = null;
                ele = this.DownWeb.Document.CreateElement("script");
                // 狀態
                ele.SetAttribute("type", @"text/javascript");
                ele.SetAttribute("text", myFunction);
                this.DownWeb.Document.Body.AppendChild(ele);
                #endregion

                // 重讀，這是為了避免讀取錯誤，所以再執行一次
                if (isReFun) webID = this.GetWebID(false);
            }
            // 傳回
            return webID;
        }
        // 取得比賽資料
        private Dictionary<string, BasicInfo> GetOtherData()
        {
            // 沒有資料就離開
            if (string.IsNullOrEmpty(this.DownReal.Data)) return null;

            Dictionary<string, BasicInfo> gameData = new Dictionary<string, BasicInfo>();
            BasicInfo gameInfo = null;
            DateTime gameDate = this.GameDate;
            DateTime gameTime = this.GameDate;
            HtmlAgilityPack.HtmlDocument document = new HtmlAgilityPack.HtmlDocument();
            //update 修改为用id查找
            string xPath = "html[1]/body[1]//div[@id='content']/div[3]/div[3]";
            // 載入資料
            document.LoadHtml(this.DownReal.Data);
            // 資料位置
            HtmlAgilityPack.HtmlNode nodeGames = document.DocumentNode.SelectSingleNode(xPath);
            // 判斷資料
            if (nodeGames != null && nodeGames.ChildNodes != null)
            {
                // 資料
                foreach (HtmlAgilityPack.HtmlNode games in nodeGames.ChildNodes)
                {
                    foreach (HtmlAgilityPack.HtmlNode game in games.ChildNodes)
                    {
                        // 不是資料就離開
                        if (game.ChildNodes.Count != 1 &&
                            game.ChildNodes[0].ChildNodes.Count != 2) break;

                        // 建立比賽資料，跟盤 ID 就是之前取得的資料
                        gameInfo = null;
                        gameInfo = new BasicInfo(this.AllianceID, this.GameType, gameTime, game.ChildNodes[0].ChildNodes[0].InnerText);

                        #region 資料
                        foreach (HtmlAgilityPack.HtmlNode info in game.ChildNodes[0].ChildNodes[1].ChildNodes)
                        {
                            // 不是資料就往下處理
                            if (game.Name != "div") continue;

                            string id = info.GetAttributeValue("class", "").ToLower().Trim();
                            int board = 0;
                            int point = 0;
                            // Away
                            if (id.IndexOf("team") != -1 &&
                                id.IndexOf("away") != -1)
                            {
                                gameInfo.Away = info.ChildNodes[2].ChildNodes[0].InnerText;
                                point = info.ChildNodes[info.ChildNodes.Count - 1].ChildNodes.Count;
                                // 分數
                                for (int i = 0; i < point - 1; i++)
                                {
                                    if (int.TryParse(info.ChildNodes[info.ChildNodes.Count - 1].ChildNodes[i].InnerText, out board))
                                    {
                                        gameInfo.AwayBoard.Add(board.ToString());
                                    }
                                }
                                // 有分數才有總分
                                if (gameInfo.AwayBoard.Count > 0)
                                {
                                    gameInfo.AwayPoint = info.ChildNodes[info.ChildNodes.Count - 1].ChildNodes[point - 1].InnerText;
                                }
                            }
                            // Away
                            if (id.IndexOf("team") != -1 &&
                                id.IndexOf("home") != -1)
                            {
                                gameInfo.Home = info.ChildNodes[2].ChildNodes[0].InnerText;
                                point = info.ChildNodes[info.ChildNodes.Count - 1].ChildNodes.Count;
                                // 分數
                                for (int i = 0; i < point - 1; i++)
                                {
                                    if (int.TryParse(info.ChildNodes[info.ChildNodes.Count - 1].ChildNodes[i].InnerText, out board))
                                    {
                                        gameInfo.HomeBoard.Add(board.ToString());
                                    }
                                }
                                // 有分數才有總分
                                if (gameInfo.HomeBoard.Count > 0)
                                {
                                    gameInfo.HomePoint = info.ChildNodes[info.ChildNodes.Count - 1].ChildNodes[point - 1].InnerText;
                                }
                            }
                            // Status
                            if (id.IndexOf("game-header") == 0)
                            {
                                gameInfo.Status = info.ChildNodes[0].InnerText.ToLower();
                                // 判斷
                                if (gameInfo.Status.IndexOf("final") != -1)
                                {
                                    gameInfo.GameStates = "E";
                                    //gameInfo.Status = "結束";
                                }
                            }
                        }
                        #endregion

                        // 加入
                        gameData[gameInfo.WebID] = gameInfo;
                    }
                }
            }
            // 傳回
            return gameData;
        }

        private Dictionary<string, BasicInfo> GetOtherData2()
        {
            // 沒有資料就離開
            if (this.DownWebScores.Document == null ||
                this.DownWebScores.Document.Body == null) return null;

            Dictionary<string, BasicInfo> gameData = new Dictionary<string, BasicInfo>();
            BasicInfo gameInfo = null;
            DateTime gameDate = this.GameDate;
            DateTime gameTime = this.GameDate;

            #region 取得日期(新版)
            //HtmlElement spanDate = this.webNBA.Document.GetElementById("sbpDate");
            //if (spanDate.InnerText == null || !DateTime.TryParse(spanDate.InnerText.Replace("Scores for", "").Trim(), out gameDate))
            //{
            //    return null;
            //}
            #endregion 取得日期

            #region 资料
            string webId = String.Empty;
            foreach (HtmlElement game in this.DownWebScores.Document.GetElementsByTagName("article"))
            {
                if (game.Id != null && game.GetAttribute("className").ToLower().IndexOf("js-show") != -1)
                {
                    //网页ID
                    webId = game.Id.Trim();
                    gameInfo = null;
                    gameInfo = new BasicInfo(this.AllianceID, this.GameType, gameTime, webId);
                    // 队伍
                    HtmlElementCollection teams = game.GetElementsByTagName("tbody");
                    if (teams == null || teams.Count == 0 || teams[0].Id != "teams")
                    {
                        continue;
                    }
                    gameInfo.Away = teams[0].GetElementsByTagName("h2")[0].InnerText.Trim();
                    gameInfo.Home = teams[0].GetElementsByTagName("h2")[1].InnerText.Trim();

                    //away分
                    HtmlElementCollection awayTd = teams[0].GetElementsByTagName("tr")[0].GetElementsByTagName("td");
                    foreach (HtmlElement item in awayTd)
                    {
                        if (item.GetAttribute("className").ToLower() == "score" && item.InnerText != null)
                        {
                            gameInfo.AwayBoard.Add(item.InnerText.ToString());
                        }
                        if (item.GetAttribute("className").ToLower() == "total" && item.InnerText != null)
                        {
                            gameInfo.AwayPoint = item.InnerText.ToString();
                        }
                    }

                    //home分
                    HtmlElementCollection homeTd = teams[0].GetElementsByTagName("tr")[1].GetElementsByTagName("td");
                    foreach (HtmlElement item in homeTd)
                    {
                        if (item.GetAttribute("className").ToLower() == "score" && item.InnerText != null)
                        {
                            gameInfo.HomeBoard.Add(item.InnerText.ToString());
                        }
                        if (item.GetAttribute("className").ToLower() == "total" && item.InnerText != null)
                        {
                            gameInfo.HomePoint = item.InnerText.ToString();
                        }
                    }
                    //状态
                    HtmlElementCollection StatusTh = game.GetElementsByTagName("th");
                    if (StatusTh.Count > 0)
                    {
                        gameInfo.Status = StatusTh[0].InnerText.ToLower();
                        gameInfo.Status = gameInfo.Status.Replace("-", "").Trim();
                    }
                    // 判斷
                    if (gameInfo.Status.IndexOf("final") != -1)
                    {
                        gameInfo.GameStates = "E";
                        //gameInfo.Status = "結束";
                    }

                    // 加入
                    gameData[gameInfo.WebID] = gameInfo;
                }
            }
            #endregion 资料
            // 傳回
            return gameData;
        }
    }
}
