using Follow.Sports.Basic;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Web;

// 跟盤以 ID 為依據
// 跟盤日期以俄國時間顯示

namespace Follow.Sports
{
    public class IhKHL : Basic.BasicIceHockey
    {
        public IhKHL(DateTime today) : base(ESport.Hockey_KHL)
        {
            // 設定
            this.AllianceID = 18;
            this.GameType = "IHRU";
            this.GameDate = GetUtcRu(today).Date; // 只取日期
            this.DownHome = new BasicDownload(this.Sport, @"http://d.asiascore.com/x/feed/f_4_0_8_asia_1");
            this.DownHomeHeader = new Dictionary<string, string>();
            this.DownHomeHeader["Accept"] = "*/*";
            this.DownHomeHeader["Accept-Charset"] = "utf-8;q=0.7,*;q=0.3";
            this.DownHomeHeader["Accept-Encoding"] = "gzip,deflate,sdch";
            this.DownHomeHeader["Accept-Language"] = "*";
            this.DownHomeHeader["X-Fsign"] = "SW9D1eZo";
            this.DownHomeHeader["X-GeoIP"] = "1";
            this.DownHomeHeader["X-utime"] = "1";
            this.DownHomeHeader["Cookie"] = "__utma=190588603.635099785.1357704170.1360998879.1361003436.71; __utmb=190588603.1.10.1361003436; __utmc=190588603; __utmz=190588603.1361003436.71.19.utmcsr=asiascore.com|utmccn=(referral)|utmcmd=referral|utmcct=/";
            this.DownHomeHeader["Host"] = "d.asiascore.com";
            this.DownHomeHeader["Referer"] = "http://d.asiascore.com/x/feed/proxy";
            //this.DownRealHeader["User-Agent"] = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.17 (KHTML, like Gecko) Chrome/24.0.1312.57 Safari/537.17";
            //this.DownRealHeader["Connection"] = "keep-alive";
            //this.DownHome = new BasicDownload(this.Sport, @"http://en.khl.ru/calendar/");
            this.DownReal = new Dictionary<string, BasicDownload>();
        }
        public override void Download()
        {
            // 讀取首頁資料。
            this.DownHome.DownloadData(this.DownHomeHeader);
            this.DownLastTime = DateTime.Now;
        }
        public override int Follow()
        {
            // 沒有資料就離開
            if (string.IsNullOrEmpty(this.DownHome.Data)) return 0;

            int result = 0;
            // 取得資料
            Dictionary<string, BasicInfo> gameData = this.GetDataByAsiaScore(this.DownHome.Data, "russia", " khl", 20);
            // 判斷資料
            if (gameData != null && gameData.Count > 0)
            {
                // 資料
                foreach (KeyValuePair<string, BasicInfo> data in gameData)
                {
                    // 比賽狀態
                    if (data.Value.GameStates == "S")
                    {
                        int num = 0;
                        // 加時
                        if (data.Value.Quarter == 4)
                        {
                            if (!string.IsNullOrEmpty(data.Value.Status) &&
                                int.TryParse(data.Value.Status, out num))
                            {
                                num = 20 - num;                             // 還原時間
                                data.Value.Status = (5 - num).ToString();   // 經過時間
                            }
                        }
                    }
                    // 加入
                    this.GameData[data.Key] = data.Value;
                }
                result = gameData.Count;
            }
            // 傳回
            return result;
        }
        private void Download_old()
        {
            // 沒有資料或下載時間超過 5 分鐘才讀取首頁資料。
            if (this.GameData.Count == 0 ||
                DateTime.Now >= this.DownLastTime.AddMinutes(5))
            {
                this.DownHome.DownloadString();
                this.DownLastTime = DateTime.Now;
            }
            // 下載比賽資料
            foreach (KeyValuePair<string, BasicDownload> real in this.DownReal)
            {
                // 沒有資料或下載時間超過 10 秒才讀取資料。
                if (real.Value.LastTime == null ||
                    DateTime.Now >= real.Value.LastTime.Value.AddSeconds(10))
                {
                    real.Value.DownloadString();
                }
            }
        }
        private int Follow_old()
        {
            // 沒有資料就離開
            if (string.IsNullOrEmpty(this.DownHome.Data)) return 0;

            int result = 0;
            BasicInfo gameInfo = null;
            HtmlAgilityPack.HtmlDocument document = new HtmlAgilityPack.HtmlDocument();
            string xPath = "/html[1]/body[1]/div[2]/div[1]/div[4]/div[2]/div[1]/div[1]/div[1]/ul[1]";
            // 載入資料
            document.LoadHtml(this.DownHome.Data);
            // 資料位置
            HtmlAgilityPack.HtmlNode nodeGames = document.DocumentNode.SelectSingleNode(xPath);
            // 判斷資料
            if (nodeGames != null && nodeGames.ChildNodes != null)
            {
                // 取得賽程
                Dictionary<string, BasicInfo> schedules = this.GetSchedules(document);
                // 資料
                foreach (HtmlAgilityPack.HtmlNode game in nodeGames.ChildNodes)
                {
                    // 不是資料就往下處理
                    if (game.Name != "li") continue;
                    if (game.ChildNodes.Count < 7) continue;
                    // 找到今天的比賽 (昨天也要找，因為會有隔一天的問題
                    if (game.GetAttributeValue("title", "") != "today" &&
                        game.GetAttributeValue("title", "") != "yesterday") continue;

                    string webID = game.ChildNodes[5].GetAttributeValue("href", "");
                    string webUrl = webID;
                    string away = game.ChildNodes[3].ChildNodes[1].ChildNodes[1].InnerText;
                    string home = game.ChildNodes[3].ChildNodes[3].ChildNodes[1].InnerText;
                    DateTime gameDate = GetUtcRu(DateTime.Now);
                    // 昨天就日期 - 1 天
                    if (game.GetAttributeValue("title", "") == "yesterday")
                    {
                        gameDate = gameDate.AddDays(-1);
                    }

                    #region 跟盤 ID
                    Uri uri = null;
                    // 錯誤處理
                    try
                    {
                        uri = new Uri(webID);
                        // 判斷是否有資料
                        if (uri.Query != null && uri.Segments.Length == 3)
                        {
                            webID = uri.Segments[2].Replace("/", "").Replace(".html", "");
                        }
                    }
                    catch { webID = null; } // 錯誤，清除跟盤 ID 
                    // 沒有跟盤 ID 就往下處理
                    if (webID == null || string.IsNullOrEmpty(webID.Trim())) continue;
                    #endregion

                    // 從賽程中找相同的比賽
                    foreach (KeyValuePair<string, BasicInfo> schedule in schedules)
                    {
                        // 日期不對就往下處理
                        if (schedule.Value.GameTime.Date != gameDate.Date) continue;
                        // 資料不對就往下處理
                        if (schedule.Value.Away != away ||
                            schedule.Value.Home != home) continue;

                        webID = schedule.Key;
                        // 建立比賽資料
                        gameInfo = null;
                        gameInfo = new BasicInfo(this.AllianceID, this.GameType, schedule.Value.GameTime, webID);
                        gameInfo.Away = away;
                        gameInfo.Home = home;

                        #region 下載比賽資料
                        if (!this.DownReal.ContainsKey(webID))
                        {
                            this.DownReal[webID] = new BasicDownload(this.Sport, webUrl, webID);
                            this.DownReal[webID].DownloadString();
                        }
                        #endregion
                        #region 處理比賽資料
                        if (this.DownReal.ContainsKey(webID) &&
                            !string.IsNullOrEmpty(this.DownReal[webID].Data))
                        {
                            HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
                            // 錯誤處理
                            try
                            {
                                // 載入資料
                                doc.LoadHtml(this.DownReal[webID].Data);

                                #region 跟盤 ID
                                xPath = "/html[1]/body[1]/center[1]/div[1]/div[3]/table[1]/tbody[1]/tr[1]/td[1]/div[1]/div[2]";
                                HtmlAgilityPack.HtmlNode nodeData = doc.DocumentNode.SelectSingleNode(xPath);
                                string nowID = null;
                                // 判斷資料
                                if (nodeData != null && !string.IsNullOrEmpty(nodeData.InnerText))
                                {
                                    nowID = nodeData.InnerText.Replace("\n", "").Trim();
                                    // 取出資料
                                    if (nowID.IndexOf(".") != -1)
                                    {
                                        nowID = nowID.Substring(0, nowID.IndexOf(".")).Replace("Game", "").Replace(" ", "").Trim();
                                    }
                                }
                                else
                                {
                                    // 比賽中
                                    xPath = "/html[1]/body[1]/div[1]/div[5]/h2[1]";
                                    nodeData = doc.DocumentNode.SelectSingleNode(xPath);
                                    // 判斷資料
                                    if (nodeData != null && !string.IsNullOrEmpty(nodeData.InnerText))
                                    {
                                        nowID = nodeData.InnerText.Replace("\n", "").Trim();
                                        // 取出資料
                                        if (nowID.IndexOf(".") != -1)
                                        {
                                            nowID = nowID.Substring(nowID.IndexOf(".") + 1).Replace("Game", "").Replace(" ", "").Trim();
                                        }
                                    }
                                }
                                // 判斷跟盤 ID 是否相同，不同就離開。
                                if (webID != nowID) break;
                                #endregion
                                #region 分數
                                xPath = "/html[1]/head[1]/script[3]";
                                nodeData = doc.DocumentNode.SelectSingleNode(xPath);
                                // 判斷資料
                                if (nodeData == null || string.IsNullOrEmpty(nodeData.InnerText))
                                {
                                    // 比賽中
                                    xPath = "/html[1]/head[1]/script[6]";
                                    nodeData = doc.DocumentNode.SelectSingleNode(xPath);
                                }
                                // 判斷資料
                                if (nodeData != null && !string.IsNullOrEmpty(nodeData.InnerText))
                                {
                                    foreach (string script in nodeData.InnerText.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries))
                                    {
                                        // 找到資料
                                        if (script.IndexOf("var olEvents") != -1)
                                        {
                                            string txt = script.Substring(script.IndexOf("["));
                                            int awayPoint = 0;
                                            int homePoint = 0;
                                            int awayOld = 0;
                                            int homeOld = 0;
                                            // 去頭尾
                                            txt = txt.Substring(1);
                                            txt = txt.Substring(0, txt.Length - 2).Replace("'", "");
                                            string[] data = txt.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                                            // 資料
                                            for (int i = data.Length - 1; i >= 0; i--)
                                            {
                                                string[] info = data[i].Split(new string[] { "|" }, StringSplitOptions.None);
                                                // 依類型處理
                                                switch (info[0])
                                                {
                                                    case "go": // 計算總分
                                                        if (info[2] == "A") awayPoint++; else homePoint++;
                                                        // 目前分數 = 目前總分 - 之前的總分
                                                        gameInfo.AwayBoard[gameInfo.AwayBoard.Count - 1] = (awayPoint - awayOld).ToString();
                                                        gameInfo.HomeBoard[gameInfo.HomeBoard.Count - 1] = (homePoint - homeOld).ToString();
                                                        gameInfo.AwayPoint = awayPoint.ToString();
                                                        gameInfo.HomePoint = homePoint.ToString();
                                                        
                                                        // 分割
                                                        string[] time = info[1].Split(new string[] { ":" }, StringSplitOptions.RemoveEmptyEntries);
                                                        // 判斷時間
                                                        if (time.Length == 2)
                                                        {
                                                            // 計算目前秒數
                                                            int seconds = int.Parse(time[0]) * 60 + int.Parse(time[1]);
                                                            DateTime nowTime = DateTime.Parse("2000-01-01 00:00:00");
                                                            // (到數)目前時間 = 總秒數 - 已過秒數
                                                            seconds = gameInfo.Quarter * (20 * 60) - seconds;
                                                            nowTime = nowTime.AddSeconds(seconds);
                                                            gameInfo.Status = nowTime.ToString("mm:ss");
                                                        }
                                                        else
                                                        {
                                                            gameInfo.Status = null;
                                                        }
                                                        break;
                                                    case "ga": // 各局
                                                        // 開始
                                                        if (info[2] == "0" && info[3] != "0")
                                                        {
                                                            gameInfo.AwayBoard.Add("0");
                                                            gameInfo.HomeBoard.Add("0");
                                                            gameInfo.AwayPoint = "0";
                                                            gameInfo.HomePoint = "0";
                                                            gameInfo.Status = null;
                                                        }
                                                        // 結束
                                                        if (info[2] == "1")
                                                        {
                                                            if (info[3] == "0")
                                                            {
                                                                // 比賽結束
                                                                gameInfo.GameStates = "E";
                                                            }
                                                            else
                                                            {
                                                                // 記錄目前總分
                                                                awayOld = awayPoint;
                                                                homeOld = homePoint;
                                                                gameInfo.Status = "結束";
                                                            }
                                                            gameInfo.Status = "結束";
                                                        }
                                                        break;
                                                }
                                            }
                                        }
                                    }
                                }
                                #endregion
                                #region 狀態
                                xPath = "/html[1]/body[1]/center[1]/div[1]/div[3]/table[1]/tbody[1]/tr[1]/td[1]/div[1]/table[1]/tbody[1]/tr[2]/td[1]/div[1]";
                                nodeData = doc.DocumentNode.SelectSingleNode(xPath);
                                // 判斷資料
                                if (nodeData == null || string.IsNullOrEmpty(nodeData.InnerText))
                                {
                                    // 比賽中
                                    xPath = "/html[1]/body[1]/div[1]/div[6]/div[2]/p[1]";
                                    nodeData = doc.DocumentNode.SelectSingleNode(xPath);
                                }
                                // 判斷資料
                                if (nodeData != null && !string.IsNullOrEmpty(nodeData.InnerText))
                                {
                                    if (nodeData.InnerText == "finished" ||
                                        nodeData.InnerText == "game ended")
                                    {
                                        gameInfo.GameStates = "E";
                                        gameInfo.Status = "結束";
                                    }
                                    else if (gameInfo.Quarter != 0 && gameInfo.GameStates == "X")
                                    {
                                        gameInfo.GameStates = "S";
                                    }
                                }
                                #endregion
                            }
                            catch { }
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
        // 下載資料
        private BasicDownload DownHome;
        private Dictionary<string,BasicDownload> DownReal;
        private DateTime DownLastTime = DateTime.Now;
        private Dictionary<string, string> DownHomeHeader;
        // 取得賽程
        private Dictionary<string,BasicInfo> GetSchedules(HtmlAgilityPack.HtmlDocument document)
        {
            Dictionary<string, BasicInfo> schedules = new Dictionary<string, BasicInfo>();
            BasicInfo gameInfo = null;
            string xPath = "/html[1]/body[1]/div[2]/div[2]";
            // 資料位置
            HtmlAgilityPack.HtmlNode nodeGames = document.DocumentNode.SelectSingleNode(xPath);
            // 判斷資料
            if (nodeGames != null && nodeGames.ChildNodes != null)
            {
                string gameDateStr = null;
                DateTime gameDate = this.GameDate;
                DateTime gameTime = this.GameDate;
                // 資料
                foreach (HtmlAgilityPack.HtmlNode games in nodeGames.ChildNodes)
                {
                    // 不是資料就往下處理
                    if (games.Name != "div") continue;

                    #region 取得日期
                    if (games.GetAttributeValue("class", "") == "matchDate")
                    {
                        gameDateStr = games.ChildNodes[1].ChildNodes[0].InnerText;
                        // 轉成日期
                        DateTime.TryParse(gameDateStr, out gameDate);
                        // 往下處理
                        continue;
                    }
                    #endregion

                    if (games.GetAttributeValue("class", "") == "matches")
                    {
                        // 資料
                        foreach (HtmlAgilityPack.HtmlNode game in games.ChildNodes)
                        {
                            // 不是資料就往下處理
                            if (game.Name != "div") continue;

                            // 資料
                            foreach (HtmlAgilityPack.HtmlNode info in game.ChildNodes)
                            {
                                // 不是資料就往下處理
                                if (info.Name != "div") continue;

                                string webID = info.ChildNodes[1].ChildNodes[1].InnerText;
                                string gameTimeStr = info.ChildNodes[1].ChildNodes[3].InnerText.Replace("<!--", "").Replace("-->", "").Trim();

                                #region 取得時間
                                // 判斷格式錯誤就離開
                                //if (gameTimeStr.IndexOf("<!--") != -1) break;
                                if (gameTimeStr.LastIndexOf(" ") != -1)
                                {
                                    gameTimeStr = gameTimeStr.Substring(0, gameTimeStr.LastIndexOf(" ")).Trim();
                                }
                                // 轉成日期，失敗就離開
                                if (!DateTime.TryParse(gameDate.ToString("yyyy-MM-dd") + " " + gameTimeStr, out gameTime))
                                {
                                    break;
                                }
                                #endregion

                                // 建立比賽資料
                                gameInfo = null;
                                gameInfo = new BasicInfo(this.AllianceID, this.GameType, gameTime, webID);
                                gameInfo.Away = info.ChildNodes[3].ChildNodes[1].ChildNodes[1].InnerText;
                                gameInfo.Home = info.ChildNodes[3].ChildNodes[3].ChildNodes[1].InnerText;

                                // 加入
                                schedules[webID] = gameInfo;
                            }
                        }
                    }
                }
            }
            // 傳回
            return schedules;
        }
    }
}
