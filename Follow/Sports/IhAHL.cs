using Follow.Sports.Basic;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Web;

// 跟盤以 隊伍與日期為依據
// 跟盤日期以台灣時間顯示

namespace Follow.Sports
{
    public class IhAHL : Basic.BasicIceHockey
    {
        private int diffTime = 0;//時間差
        public IhAHL(DateTime today) : base(ESport.Hockey_AHL)
        {
            if (string.IsNullOrWhiteSpace(this.sWebUrl))
            {
                this.sWebUrl = @"http://theahl.com/stats/schedule.php?date=";
            }
            // 設定
            this.AllianceID = 21;
            this.GameType = "IHUS2";

            if (today.Date == DateTime.Now.Date)//以現在時間為基準去切換美東時間
            {
                diffTime = frmMain.GetGameSourceTime("EasternTime");//取得與當地時間差(包含日光節約時間)
                if (diffTime > 0)
                    this.GameDate = today.AddHours(-diffTime);
                else
                    this.GameDate = GetUtcUsaEt(today);//取得美東時間
            }
            else
                this.GameDate = today;//手動指定時間

            //this.DownHome = new BasicDownload(this.Sport, @"http://theahl.com/stats/schedule.php"); // 以網站的資料頁面為主
            this.DownHome = new BasicDownload(this.Sport, this.sWebUrl + this.GameDate.ToString("yyyy-MM-dd"));
        }
        public override void Download()
        {
            // 沒有資料或下載時間超過 10 秒鐘才讀取首頁資料。
            if (this.GameData.Count == 0 ||
                DateTime.Now >= this.DownLastTime.AddSeconds(10))
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
            BasicInfo gameInfo = null;
            HtmlAgilityPack.HtmlDocument document = new HtmlAgilityPack.HtmlDocument();

            // 載入資料
            document.LoadHtml(this.DownHome.Data);
            //document.Load(@"C:\Users\Administrator\Desktop\ahl.txt");//本機資料測試
            // 資料位置
            //string xPath = "/html[1]/body[1]/div[1]/div[3]/div[1]/div[1]/div[2]/div[3]";
            //HtmlAgilityPack.HtmlNode nodeGames = document.DocumentNode.SelectSingleNode(xPath);

            HtmlAgilityPack.HtmlNode nodeGames = document.GetElementbyId("AHLscoreBox");
            // 判斷資料
            if (nodeGames != null && nodeGames.ChildNodes != null)
            {
                DateTime gameDate = this.GameDate;
                DateTime gameTime = this.GameDate;
                //string gameDateStr = null;
                int gameQuarter = 0;
                // 資料
                foreach (HtmlAgilityPack.HtmlNode game in nodeGames.ChildNodes)
                {

                    string webID = null;
                    #region 跟盤 ID
                    HtmlAgilityPack.HtmlNodeCollection GameDateInfo = game.SelectNodes("div[@class='final-hd-link']/a");
                    if (GameDateInfo != null && GameDateInfo.Count > 0)
                    {

                        var href = GameDateInfo[0].GetAttributeValue("href", "");
                        if (string.IsNullOrEmpty(href))//取到空值
                            continue;

                        try
                        {
                            if (!href.Contains("http"))
                                href = "http://" + href;

                            Uri uri = new Uri(href);
                            // 判斷是否有資料
                            if (uri.Query != null && !string.IsNullOrEmpty(uri.Query))
                            {
                                HttpRequest req = new HttpRequest("", uri.AbsoluteUri, uri.Query.Substring(1));
                                // 判斷資料
                                if (req["game_id"] != null && !string.IsNullOrEmpty(req["game_id"].Trim()))//已開賽
                                {
                                    webID = req["game_id"];
                                }
                                else if (req["file_path"] != null && !string.IsNullOrEmpty(req["file_path"].Trim()))//未開賽
                                {
                                    webID = req["file_path"];
                                    if (webID.IndexOf("/") != -1)
                                    {
                                        webID = webID.Substring(webID.IndexOf("/") + 1).Trim();
                                    }
                                    if (webID.IndexOf("-") != -1)
                                    {
                                        webID = webID.Substring(0, webID.IndexOf("-")).Trim();
                                    }
                                }
                            }
                        }
                        catch { }
                        
                    }
                    else
                        continue;
           
                    // 沒有跟盤 ID 就往下處理
                    if (webID == null || string.IsNullOrEmpty(webID.Trim())) continue;
                    #endregion

                    // 建立比賽資料，時間以台灣為主
                    DateTime GameTime;
                    if (diffTime > 0)
                        GameTime = this.GameDate.AddHours(diffTime);
                    else
                    {
                        TimeSpan ts = DateTime.Now - this.GameDate;
                        GameTime = this.GameDate.AddHours(ts.TotalHours);
                    }

                    gameInfo = null;
                    gameInfo = new BasicInfo(this.AllianceID, this.GameType, GameTime, webID);
                    gameQuarter = 0;

                    #region 處理比賽資料
                    #region 狀態
                    gameInfo.Status = game.ChildNodes[1].InnerText.ToLower().Trim();
                    gameInfo.Status = gameInfo.Status.ToLower().Trim();
                    // 判斷目前局數
                    if (gameInfo.Status.IndexOf("1st") != -1) gameQuarter = 1;
                    if (gameInfo.Status.IndexOf("2nd") != -1) gameQuarter = 2;
                    if (gameInfo.Status.IndexOf("3rd") != -1) gameQuarter = 3;
                    if (gameInfo.Status.IndexOf("ot") != -1) gameQuarter = 4;
                    // 判斷
                    if (gameInfo.Status.IndexOf("final") != -1)
                    {
                        gameInfo.GameStates = "E";
                        gameInfo.Status = "結束";
                        gameQuarter = 10;
                    }
                    else if (gameQuarter > 0) // 有局數才有比賽
                    {
                        // 取代文字
                        gameInfo.GameStates = "S";
                        // 取代文字
                        gameInfo.Status = gameInfo.Status.Replace("half", "");
                        gameInfo.Status = gameInfo.Status.Replace("halftime", "");
                        gameInfo.Status = gameInfo.Status.Replace("qtr", "");
                        gameInfo.Status = gameInfo.Status.Replace("1st", "");
                        gameInfo.Status = gameInfo.Status.Replace("2nd", "");
                        gameInfo.Status = gameInfo.Status.Replace("3rd", "");
                        gameInfo.Status = gameInfo.Status.Replace("4th", "");
                        gameInfo.Status = gameInfo.Status.Replace("ot", "");
                        gameInfo.Status = gameInfo.Status.Replace("2ot", "");
                        gameInfo.Status = gameInfo.Status.Trim();
                        if (gameInfo.Status == "00:00")
                        {
                            gameInfo.Status = "結束";
                        }
                    }
                    #endregion
                    HtmlAgilityPack.HtmlNode info = null;
                    // 判斷資料格式
                    if (game.ChildNodes[5].ChildNodes.Count == 5)
                    {
                        #region 比賽未開始，只有隊伍名稱
                        // Away
                        info = game.ChildNodes[5].ChildNodes[1];
                        gameInfo.Away = info.ChildNodes[1].InnerText;
                        // Home
                        info = game.ChildNodes[5].ChildNodes[3];
                        gameInfo.Home = info.ChildNodes[1].InnerText;
                        #endregion
                    }
                    else
                    {
                        #region Away
                        info = game.ChildNodes[5].ChildNodes[3];
                        gameInfo.Away = info.ChildNodes[1].InnerText;
                        gameInfo.AwayPoint = info.ChildNodes[info.ChildNodes.Count - 10].InnerText;
                        // 分數
                        for (int i = 3; i < info.ChildNodes.Count - 10; i += 2)
                        {
                            if (info.ChildNodes[i].InnerText != null && !string.IsNullOrEmpty(info.ChildNodes[i].InnerText.Trim()) &&
                                gameInfo.AwayBoard.Count < gameQuarter)
                            {
                                gameInfo.AwayBoard.Add(info.ChildNodes[i].InnerText.Trim());
                            }
                        }
                        #endregion
                        #region Home
                        info = game.ChildNodes[5].ChildNodes[5];
                        gameInfo.Home = info.ChildNodes[1].InnerText;
                        gameInfo.HomePoint = info.ChildNodes[info.ChildNodes.Count - 8].InnerText;
                        // 分數
                        for (int i = 3; i < info.ChildNodes.Count - 8; i += 2)
                        {
                            if (info.ChildNodes[i].InnerText != null && !string.IsNullOrEmpty(info.ChildNodes[i].InnerText.Trim()) &&
                                gameInfo.HomeBoard.Count < gameQuarter)
                            {
                                gameInfo.HomeBoard.Add(info.ChildNodes[i].InnerText.Trim());
                            }
                        }
                        #endregion
                    }
                    #region 處理隊伍名稱
                    Dictionary<string, string> webName = this.GetWebName();
                    // Away
                    gameInfo.Away = gameInfo.Away.Replace("\n", "");
                    gameInfo.Away = gameInfo.Away.Replace("\t", "");
                    gameInfo.Away = gameInfo.Away.Trim();
                    // 刪除無用資料
                    if (gameInfo.Away.IndexOf("(") != -1)
                    {
                        gameInfo.Away = gameInfo.Away.Substring(0, gameInfo.Away.IndexOf("(")).Trim();
                    }
                    // 變更名稱
                    if (webName.ContainsKey(gameInfo.Away))
                    {
                        gameInfo.Away = webName[gameInfo.Away];
                    }
                    // Home
                    gameInfo.Home = info.ChildNodes[1].InnerText;
                    gameInfo.Home = gameInfo.Home.Replace("\n", "");
                    gameInfo.Home = gameInfo.Home.Replace("\t", "");
                    gameInfo.Home = gameInfo.Home.Trim();
                    // 刪除無用資料
                    if (gameInfo.Home.IndexOf("(") != -1)
                    {
                        gameInfo.Home = gameInfo.Home.Substring(0, gameInfo.Home.IndexOf("(")).Trim();
                    }
                    // 變更名稱
                    if (webName.ContainsKey(gameInfo.Home))
                    {
                        gameInfo.Home = webName[gameInfo.Home];
                    }
                    #endregion
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
        public override bool Update(string connectionString, BasicInfo info)
        {
            // 以隊伍名稱當作更新的依據
            //return this.Update2(connectionString, info);
            return this.Update3(connectionString, info);//昨天跟今天的隊伍比對
        }
        private Dictionary<string, string> GetWebName()
        {
            Dictionary<string, string> teamName = new Dictionary<string, string>();
            // 設定
            teamName.Add("Peoria", "Peoria Rivermen");
            teamName.Add("Hamilton", "Hamilton Bulldogs");
            teamName.Add("Chicago", "Chicago Wolves");
            teamName.Add("Abbotsford", "Abbotsford Heat");
            teamName.Add("Bridgeport", "Bridgeport Sound Tigers");
            teamName.Add("Albany", "Albany Devils");
            teamName.Add("Grand Rapids", "Grand Rapids Griffins");
            teamName.Add("Rochester", "Rochester Americans");
            teamName.Add("Lake Erie", "Lake Erie Monsters");
            teamName.Add("Toronto", "Toronto Marlies");
            teamName.Add("Portland", "Portland Pirates");
            teamName.Add("Springfield", "Springfield Falcons");
            teamName.Add("Adirondack", "Adirondack Flames");
            teamName.Add("St. John's", "St. John's IceCaps");
            teamName.Add("Hershey", "Hershey Bears");
            teamName.Add("Norfolk", "Norfolk Admirals");
            teamName.Add("Binghamton", "Binghamton Senators");
            teamName.Add("W-B/Scranton", "Wilkes-Barre/Scranton");
            teamName.Add("Milwaukee", "Milwaukee Admirals");
            teamName.Add("Rockford", "Rockford IceHogs");
            teamName.Add("Texas", "Texas Stars");
            teamName.Add("San Antonio", "San Antonio Rampage");
            teamName.Add("Charlotte", "Charlotte Checkers");
            teamName.Add("Houston", "Houston Aeros");
            teamName.Add("Manchester", "Manchester Monarchs");
            teamName.Add("Connecticut", "Connecticut Whale");
            teamName.Add("Providence", "Providence Bruins");
            teamName.Add("Worcester", "Worcester Sharks");
            teamName.Add("Syracuse", "Syracuse Crunch");
            teamName.Add("Oklahoma City", "Oklahoma City");
            teamName.Add("Hartford", "Hartford Wolf Pack");
            teamName.Add("Utica", "Utica Comets");
            teamName.Add("Iowa", "Iowa Wild");
            teamName.Add("Lehigh Valley", "Lehigh Valley Phantoms");
            // 傳回
            return teamName;
        }
        // 下載資料
        private BasicDownload DownHome;
        private DateTime DownLastTime = DateTime.Now;
    }
}
