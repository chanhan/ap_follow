using Follow.Sports.Basic;
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
    public class BbNPB : Basic.BasicBaseball
    {
        private string sWebUrl1 = UrlSetting.GetUrl(ESport.Baseball_NPB, "Url1");
        private string sWebUrl2 = UrlSetting.GetUrl(ESport.Baseball_NPB, "Url2");
        private string sWebUrl3 = UrlSetting.GetUrl(ESport.Baseball_NPB, "Url3");
        private string sWebUrl4 = UrlSetting.GetUrl(ESport.Baseball_NPB, "Url4");
        private string sWebUrl5 = UrlSetting.GetUrl(ESport.Baseball_NPB, "Url5");
        private string sWebUrl6 = UrlSetting.GetUrl(ESport.Baseball_NPB, "Url6");
        public BbNPB(DateTime today)
            : base(ESport.Baseball_NPB)
        {
            // 設定
            this.AllianceID = 46;
            this.GameType = "BBJP";
            this.GameDate = GetUtcJp(today).Date; // 只取日期
            #region 来源网设定
            if (string.IsNullOrWhiteSpace(this.sWebUrl))
            {
                this.sWebUrl = @"http://baseball.yahoo.co.jp/npb/schedule/";
            }
            if (string.IsNullOrWhiteSpace(this.sWebUrl1))
            {
                this.sWebUrl1 = @"http://baseball.yahoo.co.jp/npbopen/schedule/";
            }
            if (string.IsNullOrWhiteSpace(this.sWebUrl2))
            {
                this.sWebUrl2 = @"http://baseball.yahoo.co.jp/npbopen/game/{0}/top";
            }
            if (string.IsNullOrWhiteSpace(this.sWebUrl3))
            {
                this.sWebUrl3 = @"http://baseball.yahoo.co.jp/npbopen/game/{0}/stats";
            }
            if (string.IsNullOrWhiteSpace(this.sWebUrl4))
            {
                this.sWebUrl4 = @"http://live.baseball.yahoo.co.jp/npbopen/game/{0}/score";
            }
            if (string.IsNullOrWhiteSpace(this.sWebUrl5))
            {
                this.sWebUrl5 = @"http://baseball.yahoo.co.jp/npb/game/{0}/top";
            }
            if (string.IsNullOrWhiteSpace(this.sWebUrl6))
            {
                this.sWebUrl6 = @"http://live.baseball.yahoo.co.jp/npb/game/{0}/score";
            }
            #endregion
            this.DownHome = new BasicDownload(this.Sport, this.sWebUrl);
            this.DownHomeWarmUp = new BasicDownload(this.Sport, this.sWebUrl1);
            this.DownReal = new Dictionary<string, BasicDownload>();
        }
        public override void Download()
        {
            // 沒有資料或下載時間超過 10 分鐘才讀取首頁資料。
            if (this.GameData.Count == 0 ||
                DateTime.Now > this.DownLastTime.AddMinutes(10))
            {
                this.DownHome.DownloadString();
                this.DownHomeWarmUp.DownloadString();
                this.DownLastTime = DateTime.Now;
            }

            // 下載比賽資料
            foreach (KeyValuePair<string, BasicDownload> real in this.DownReal)
            {
                // 沒有資料或下載時間超過 2 秒才讀取資料。
                if (real.Value.LastTime == null ||
                    DateTime.Now >= real.Value.LastTime.Value.AddSeconds(5))
                {
                    real.Value.DownloadString();
                }
            }
        }

        public override int Follow()
        {
            int ret = this.FollowByWeb();
            ret += this.FollowByWarmUp();
            return ret;
        }
        private int FollowByWarmUp()
        {
            // 沒有資料就離開
            if (string.IsNullOrEmpty(this.DownHomeWarmUp.Data)) return 0;

            int result = 0;
            BasicInfo gameInfo = null;
            HtmlAgilityPack.HtmlDocument document = new HtmlAgilityPack.HtmlDocument();
            //string xPath = "/html[1]/body[1]/div[1]/div[2]/div[3]/div[8]/table[1]";
            // 載入資料
            document.LoadHtml(this.DownHomeWarmUp.Data);
            // 資料位置
            HtmlAgilityPack.HtmlNode contentNode = document.GetElementbyId("contents");
            if (contentNode == null) { return 0; }

            HtmlAgilityPack.HtmlNode nodeGames = contentNode.SelectSingleNode("//div[contains(@class,'NpbSchedule')]/table");

            // 判斷資料
            if (nodeGames != null && nodeGames.ChildNodes != null)
            {
                DateTime gameDate = this.GameDate;
                DateTime gameTime = this.GameDate;
                string gameDateStr = null;

                // 資料    ChildNodes[1] 取table中的tbody
                foreach (HtmlAgilityPack.HtmlNode game in nodeGames.ChildNodes[1].ChildNodes)
                {
                    // 不是資料就往下處理
                    if (game.Name != "tr") continue;

                    string webID = null;
                    string webUrl = null;
                    string webStatus = null;
                    int timeIndex = 1;
                    int idIndex = 6;
                    int awayIndex = 0;
                    int homeIndex = 4;

                    #region 取得日期
                    if (game.ChildNodes.Count == 14)
                    {
                        gameDateStr = game.ChildNodes[1].InnerText.Replace("月", "/").Replace("日", "").Trim();
                        // 截斷日期
                        if (gameDateStr.IndexOf("（") != -1)
                            gameDateStr = DateTime.Now.ToString("yyyy") + "/" + gameDateStr.Substring(0, gameDateStr.IndexOf("（")).Trim();
                        // 判斷日期，失敗就往下處理
                        if (!DateTime.TryParse(gameDateStr, out gameDate))
                        {
                            gameDateStr = null;
                            continue;
                        }

                        idIndex = 8;
                        awayIndex = 2;
                        homeIndex = 6;
                    }
                    // 沒有日期就往下處理
                    if (string.IsNullOrEmpty(gameDateStr) && game.ChildNodes.Count != 12) continue;
                    // 錯誤的陣列就往下處理
                    if (game.ChildNodes.Count <= 7) continue;
                    #endregion
                    #region 取得時間
                    // 判斷日期，錯誤就離開
                    string timeStr = game.NextSibling.NextSibling.ChildNodes[timeIndex].InnerText;
                    // 取出資料
                    timeStr = timeStr.Substring(0, timeStr.IndexOf(" "));
                    if (!DateTime.TryParse(gameDate.ToString("yyyy/MM/dd") + " " + timeStr, out gameTime)) continue;
                    // 判斷是否為今天，不是今天就往下處理
                    if (this.GameDate.Date != gameTime.Date)
                    {
                        gameDateStr = null;
                        continue;
                    }
                    #endregion
                    #region 跟盤 ID
                    webID = game.ChildNodes[idIndex].ChildNodes[0].GetAttributeValue("href", "");
                    webStatus = game.ChildNodes[idIndex].ChildNodes[0].InnerText;
                    Uri uri = null;
                    // 錯誤處理
                    try
                    {
                        uri = new Uri(@"http://www.a.com" + webID);
                        // 判斷是否有資料 
                        if (uri.Segments.Length == 5)
                        {
                            webID = uri.Segments[3].Replace("/", "");
                            if (webStatus == "中止")
                            {
                                webUrl = string.Format(sWebUrl2, webID);
                            }
                            else if (webStatus.IndexOf("速報") == -1)
                            {
                                webUrl = string.Format(sWebUrl3, webID);
                            }
                            else
                            {
                                webUrl = string.Format(sWebUrl4, webID);
                            }
                        }
                    }
                    catch { webID = null; } // 錯誤，清除跟盤 ID 
                    // 沒有跟盤 ID 就往下處理
                    if (webID == null || string.IsNullOrEmpty(webID.Trim()))
                    {
                        gameDateStr = null;
                        continue;
                    }
                    #endregion

                    // 建立比賽資料
                    gameInfo = null;
                    gameInfo = new BasicInfo(this.AllianceID, this.GameType, gameTime, webID);
                    gameInfo.IsBall = true; // 賽程的主客隊與資料是相反的
                    gameInfo.Away = game.ChildNodes[homeIndex].InnerText;
                    gameInfo.AwayPoint = "";
                    gameInfo.Home = game.ChildNodes[awayIndex].InnerText;
                    gameInfo.HomePoint = "";
                    gameInfo.GameStates = (webStatus == "中止") ? ("P") : ("X");

                    #region 下載比賽資料，比賽時間 10 分鐘前就開始處理
                    if (!this.DownReal.ContainsKey(webID) &&
                        GetUtcKr(DateTime.Now) >= gameTime.AddMinutes(-10))
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
                        HtmlAgilityPack.HtmlNode node = null;

                        // 讀取資料
                        doc.LoadHtml(this.DownReal[webID].Data);

                        // 判斷是中止
                        if (gameInfo.GameStates == "P")
                        {
                            string findxpath = FindXPath(doc.DocumentNode, "yjSNLiveBattlereview");
                            if (!string.IsNullOrEmpty(findxpath))
                            {
                                node = doc.DocumentNode.SelectSingleNode(findxpath);
                                if (node != null && node.ChildNodes.Count > 0)
                                {
                                    // 原因 
                                    gameInfo.TrackerText = node.ChildNodes[node.ChildNodes.Count - 2].InnerText;
                                }
                            }
                        }
                        else if (webStatus.IndexOf("速報") == -1)
                        {
                            #region 分數
                            node = doc.DocumentNode.SelectSingleNode(FindXPath(doc.DocumentNode, "scoreboard"));
                            if (node != null && node.ChildNodes.Count >= 1)
                            {
                                // Away
                                foreach (HtmlAgilityPack.HtmlNode data in node.ChildNodes[1].ChildNodes[1].ChildNodes[2].ChildNodes)
                                {
                                    // 不是資料就往下處理
                                    if (data.Name != "td")
                                        continue;
                                    // 分數
                                    gameInfo.AwayBoard.Add(data.InnerText.Replace("&nbsp;", ""));
                                }
                                gameInfo.AwayPoint = gameInfo.AwayBoard[gameInfo.AwayBoard.Count - 3];
                                gameInfo.AwayH = gameInfo.AwayBoard[gameInfo.AwayBoard.Count - 2];
                                gameInfo.AwayE = gameInfo.AwayBoard[gameInfo.AwayBoard.Count - 1];
                                gameInfo.AwayBoard.RemoveAt(gameInfo.AwayBoard.Count - 1);
                                gameInfo.AwayBoard.RemoveAt(gameInfo.AwayBoard.Count - 1);
                                gameInfo.AwayBoard.RemoveAt(gameInfo.AwayBoard.Count - 1);
                                // Home
                                foreach (HtmlAgilityPack.HtmlNode data in node.ChildNodes[1].ChildNodes[1].ChildNodes[4].ChildNodes)
                                {
                                    // 不是資料就往下處理
                                    if (data.Name != "td")
                                        continue;
                                    gameInfo.HomeBoard.Add(data.InnerText.Replace("&nbsp;", ""));
                                }
                                gameInfo.HomePoint = gameInfo.HomeBoard[gameInfo.HomeBoard.Count - 3];
                                gameInfo.HomeH = gameInfo.HomeBoard[gameInfo.HomeBoard.Count - 2];
                                gameInfo.HomeE = gameInfo.HomeBoard[gameInfo.HomeBoard.Count - 1];
                                gameInfo.HomeBoard.RemoveAt(gameInfo.HomeBoard.Count - 1);
                                gameInfo.HomeBoard.RemoveAt(gameInfo.HomeBoard.Count - 1);
                                gameInfo.HomeBoard.RemoveAt(gameInfo.HomeBoard.Count - 1);
                            }
                            // 移除無用的分數
                            if (gameInfo.Quarter > 0)
                            {
                                int index = gameInfo.AwayBoard.Count - 1;
                                while (index >= 0)
                                {
                                    if (gameInfo.AwayBoard[index] == "-")
                                        gameInfo.AwayBoard[index] = "0";

                                    if (gameInfo.HomeBoard[index] == "-")
                                        gameInfo.HomeBoard[index] = "0";

                                    if (string.IsNullOrEmpty(gameInfo.AwayBoard[index]))
                                        gameInfo.AwayBoard.RemoveAt(index);
                                    if (string.IsNullOrEmpty(gameInfo.HomeBoard[index]))
                                        gameInfo.HomeBoard.RemoveAt(index);
                                    index--;
                                }
                                gameInfo.GameStates = "S";
                            }
                            #endregion
                            #region 狀態
                            node = doc.DocumentNode.SelectSingleNode(FindXPath(doc.DocumentNode, "yjSNLiveGamecard"));
                            if (node != null)
                            {
                                if (node.InnerText.IndexOf("試合前") != -1)
                                {
                                    gameInfo.GameStates = "X";
                                    gameInfo.BallB = 0;
                                    gameInfo.BallS = 0;
                                    gameInfo.BallO = 0;
                                    gameInfo.Bases = 0;
                                    gameInfo.AwayPoint = "";
                                    gameInfo.AwayBoard.Clear();
                                    gameInfo.AwayH = null;
                                    gameInfo.AwayE = null;
                                    gameInfo.HomePoint = "";
                                    gameInfo.HomeBoard.Clear();
                                    gameInfo.HomeH = null;
                                    gameInfo.HomeE = null;
                                }
                                if (node.InnerText.IndexOf("試合終了") != -1)
                                {
                                    gameInfo.GameStates = "E";
                                    gameInfo.BallB = 0;
                                    gameInfo.BallS = 0;
                                    gameInfo.BallO = 0;
                                    gameInfo.Bases = 0;
                                }
                            }
                            // 比賽結束
                            if (FindXPath(doc.DocumentNode, "afterdata") != "")
                            {
                                node = doc.DocumentNode.SelectSingleNode(FindXPath(doc.DocumentNode, "afterdata"));
                                if (node != null && !string.IsNullOrEmpty(node.InnerText))
                                {
                                    gameInfo.GameStates = "E";
                                    gameInfo.BallB = 0;
                                    gameInfo.BallS = 0;
                                    gameInfo.BallO = 0;
                                    gameInfo.Bases = 0;
                                }
                            }
                            #endregion
                        }
                        else
                        {
                            #region 分數
                            node = doc.DocumentNode.SelectSingleNode(FindXPath(doc.DocumentNode, "scoreboard"));
                            if (node != null && node.ChildNodes.Count >= 1)
                            {
                                // Away
                                foreach (HtmlAgilityPack.HtmlNode data in node.ChildNodes[1].ChildNodes[1].ChildNodes[2].ChildNodes)
                                {
                                    // 不是資料就往下處理
                                    if (data.Name != "td")
                                        continue;
                                    // 分數
                                    gameInfo.AwayBoard.Add(data.InnerText.Replace("&nbsp;", ""));
                                }
                                gameInfo.AwayPoint = gameInfo.AwayBoard[gameInfo.AwayBoard.Count - 3];
                                gameInfo.AwayH = gameInfo.AwayBoard[gameInfo.AwayBoard.Count - 2];
                                gameInfo.AwayE = gameInfo.AwayBoard[gameInfo.AwayBoard.Count - 1];
                                gameInfo.AwayBoard.RemoveAt(gameInfo.AwayBoard.Count - 1);
                                gameInfo.AwayBoard.RemoveAt(gameInfo.AwayBoard.Count - 1);
                                gameInfo.AwayBoard.RemoveAt(gameInfo.AwayBoard.Count - 1);
                                // Home
                                foreach (HtmlAgilityPack.HtmlNode data in node.ChildNodes[1].ChildNodes[1].ChildNodes[4].ChildNodes)
                                {
                                    // 不是資料就往下處理
                                    if (data.Name != "td")
                                        continue;
                                    gameInfo.HomeBoard.Add(data.InnerText.Replace("&nbsp;", ""));
                                }
                                gameInfo.HomePoint = gameInfo.HomeBoard[gameInfo.HomeBoard.Count - 3];
                                gameInfo.HomeH = gameInfo.HomeBoard[gameInfo.HomeBoard.Count - 2];
                                gameInfo.HomeE = gameInfo.HomeBoard[gameInfo.HomeBoard.Count - 1];
                                gameInfo.HomeBoard.RemoveAt(gameInfo.HomeBoard.Count - 1);
                                gameInfo.HomeBoard.RemoveAt(gameInfo.HomeBoard.Count - 1);
                                gameInfo.HomeBoard.RemoveAt(gameInfo.HomeBoard.Count - 1);
                            }
                            // 移除無用的分數
                            if (gameInfo.Quarter > 0)
                            {
                                int index = gameInfo.AwayBoard.Count - 1;
                                while (index >= 0)
                                {
                                    if (gameInfo.AwayBoard[index] == "-")
                                        gameInfo.AwayBoard[index] = "0";

                                    if (gameInfo.HomeBoard[index] == "-")
                                        gameInfo.HomeBoard[index] = "0";

                                    if (string.IsNullOrEmpty(gameInfo.AwayBoard[index]))
                                        gameInfo.AwayBoard.RemoveAt(index);
                                    if (string.IsNullOrEmpty(gameInfo.HomeBoard[index]))
                                        gameInfo.HomeBoard.RemoveAt(index);
                                    index--;
                                }
                                gameInfo.GameStates = "S";
                            }
                            #endregion
                            #region BSO
                            node = doc.DocumentNode.SelectSingleNode(FindXPath(doc.DocumentNode, "sbo"));
                            if (node != null && node.ChildNodes.Count >= 1)
                            {
                                string txt = null;
                                int bStart = 0;
                                int bEnd = 0;
                                // B
                                txt = node.ChildNodes[3].ChildNodes[3].ChildNodes[1].InnerHtml.Replace("\n", "");
                                bStart = txt.IndexOf("<b>");
                                bEnd = txt.IndexOf("</b>");
                                if (bStart != -1 && bEnd != -1)
                                {
                                    gameInfo.BallB = txt.Substring(bStart + 3, bEnd - bStart - 3).Length;
                                    if (gameInfo.BallB > 3)
                                        gameInfo.BallB = 0;
                                }
                                // S
                                txt = node.ChildNodes[3].ChildNodes[3].ChildNodes[3].InnerHtml.Replace("\n", "");
                                bStart = txt.IndexOf("<b>");
                                bEnd = txt.IndexOf("</b>");
                                if (bStart != -1 && bEnd != -1)
                                {
                                    gameInfo.BallS = txt.Substring(bStart + 3, bEnd - bStart - 3).Length;
                                    if (gameInfo.BallS > 2)
                                        gameInfo.BallS = 0;
                                }
                                // O
                                txt = node.ChildNodes[3].ChildNodes[3].ChildNodes[5].InnerHtml.Replace("\n", "");
                                bStart = txt.IndexOf("<b>");
                                bEnd = txt.IndexOf("</b>");
                                if (bStart != -1 && bEnd != -1)
                                {
                                    gameInfo.BallO = txt.Substring(bStart + 3, bEnd - bStart - 3).Length;
                                    if (gameInfo.BallO > 2)
                                    {
                                        gameInfo.BallB = 0;
                                        gameInfo.BallS = 0;
                                        gameInfo.BallO = 0;
                                    }
                                }
                            }
                            #endregion
                            #region Bases
                            node = doc.DocumentNode.SelectSingleNode(FindXPath(doc.DocumentNode, "base"));
                            if (node != null && node.ChildNodes.Count >= 1)
                            {
                                if (!string.IsNullOrEmpty(FindXPath(doc.DocumentNode, "base1")))
                                {
                                    gameInfo.Bases += 1;
                                }
                                if (!string.IsNullOrEmpty(FindXPath(doc.DocumentNode, "base2")))
                                {
                                    gameInfo.Bases += 2;
                                }
                                if (!string.IsNullOrEmpty(FindXPath(doc.DocumentNode, "base3")))
                                {
                                    gameInfo.Bases += 4;
                                }
                            }
                            #endregion
                            #region 狀態
                            node = doc.DocumentNode.SelectSingleNode(FindXPath(doc.DocumentNode, "livenavi"));
                            if (node != null)
                            {
                                if (node.InnerText.IndexOf("試合前") != -1)
                                {
                                    gameInfo.GameStates = "X";
                                    gameInfo.BallB = 0;
                                    gameInfo.BallS = 0;
                                    gameInfo.BallO = 0;
                                    gameInfo.Bases = 0;
                                    gameInfo.AwayPoint = "";
                                    gameInfo.AwayBoard.Clear();
                                    gameInfo.AwayH = null;
                                    gameInfo.AwayE = null;
                                    gameInfo.HomePoint = "";
                                    gameInfo.HomeBoard.Clear();
                                    gameInfo.HomeH = null;
                                    gameInfo.HomeE = null;
                                }
                                if (node.InnerText.IndexOf("試合終了") != -1)
                                {
                                    gameInfo.GameStates = "E";
                                    gameInfo.BallB = 0;
                                    gameInfo.BallS = 0;
                                    gameInfo.BallO = 0;
                                    gameInfo.Bases = 0;
                                }
                            }
                            // 比賽結束
                            if (FindXPath(doc.DocumentNode, "afterdata") != "")
                            {
                                node = doc.DocumentNode.SelectSingleNode(FindXPath(doc.DocumentNode, "afterdata"));
                                if (node != null && !string.IsNullOrEmpty(node.InnerText))
                                {
                                    gameInfo.GameStates = "E";
                                    gameInfo.BallB = 0;
                                    gameInfo.BallS = 0;
                                    gameInfo.BallO = 0;
                                    gameInfo.Bases = 0;
                                }
                            }
                            #endregion
                        }
                    #endregion
                    }

                    if (base.CheckGame(gameInfo))//檢查比分是否合法
                    {
                        this.GameData[gameInfo.WebID] = gameInfo;// 加入
                        result++;// 累計
                    }
                }
            }
            // 傳回
            return result;
        }
        private int FollowByWeb()
        {
            // 沒有資料就離開
            if (string.IsNullOrEmpty(this.DownHome.Data)) return 0;

            int result = 0;
            BasicInfo gameInfo = null;
            HtmlAgilityPack.HtmlDocument document = new HtmlAgilityPack.HtmlDocument();
            //string xPath = "/html[1]/body[1]/div[1]/div[2]/div[3]/div[8]/table[1]";
            // 載入資料
            document.LoadHtml(this.DownHome.Data);
            // 資料位置
            HtmlAgilityPack.HtmlNode contentNode = document.GetElementbyId("contents");
            if (contentNode == null) { return 0; }

            HtmlAgilityPack.HtmlNode nodeGames = contentNode.SelectSingleNode("//div[contains(@class,'NpbSchedule')]/table");

            // 判斷資料
            if (nodeGames != null && nodeGames.ChildNodes != null)
            {
                DateTime gameDate = this.GameDate;
                DateTime gameTime = this.GameDate;
                string gameDateStr = null;

                // 資料
                foreach (HtmlAgilityPack.HtmlNode game in nodeGames.ChildNodes)
                {
                    // 不是資料就往下處理
                    if (game.Name != "tr") continue;

                    string webID = null;
                    string webUrl = null;
                    string webStatus = null;
                    int timeIndex = 8;
                    int idIndex = 6;
                    int awayIndex = 0;
                    int homeIndex = 4;

                    #region 取得日期
                    if (game.ChildNodes.Count == 15)
                    {
                        gameDateStr = game.ChildNodes[1].InnerText.Replace("月", "/").Replace("日", "").Trim();
                        // 截斷日期
                        if (gameDateStr.IndexOf("（") != -1)
                            gameDateStr = DateTime.Now.ToString("yyyy") + "/" + gameDateStr.Substring(0, gameDateStr.IndexOf("（")).Trim();
                        // 判斷日期，失敗就往下處理
                        if (!DateTime.TryParse(gameDateStr, out gameDate))
                        {
                            gameDateStr = null;
                            continue;
                        }
                        timeIndex = 11;
                        idIndex = 9;
                        awayIndex = 3;
                        homeIndex = 7;
                    }
                    // 沒有日期就往下處理
                    if (string.IsNullOrEmpty(gameDateStr)) continue;
                    // 錯誤的陣列就往下處理
                    if (timeIndex >= game.ChildNodes.Count) continue;
                    #endregion
                    #region 取得時間
                    // 判斷日期，錯誤就離開
                    string timeStr = game.ChildNodes[timeIndex].InnerText;
                    // 取出資料
                    timeStr = timeStr.Substring(0, timeStr.IndexOf(" "));
                    if (!DateTime.TryParse(gameDate.ToString("yyyy/MM/dd") + " " + timeStr, out gameTime)) continue;
                    // 判斷是否為今天，不是今天就往下處理
                    if (this.GameDate.Date != gameTime.Date)
                    {
                        gameDateStr = null;
                        continue;
                    }
                    #endregion
                    #region 跟盤 ID
                    webID = game.ChildNodes[idIndex].ChildNodes[0].GetAttributeValue("href", "");
                    webStatus = game.ChildNodes[idIndex].ChildNodes[0].InnerText;
                    Uri uri = null;
                    // 錯誤處理
                    try
                    {
                        uri = new Uri(@"http://www.a.com" + webID);
                        // 判斷是否有資料
                        if (uri.Segments.Length == 4)
                        {
                            webID = uri.Segments[3].Replace("/", "");
                            if (webStatus == "中止")
                            {
                                webUrl = string.Format(sWebUrl5, webID);
                            }
                            else
                            {
                                webUrl = string.Format(sWebUrl6, webID);
                            }
                        }
                    }
                    catch { webID = null; } // 錯誤，清除跟盤 ID 
                    // 沒有跟盤 ID 就往下處理
                    if (webID == null || string.IsNullOrEmpty(webID.Trim()))
                    {
                        gameDateStr = null;
                        continue;
                    }
                    #endregion

                    // 建立比賽資料
                    gameInfo = null;
                    gameInfo = new BasicInfo(this.AllianceID, this.GameType, gameTime, webID);
                    gameInfo.IsBall = true; // 賽程的主客隊與資料是相反的
                    gameInfo.Away = game.ChildNodes[homeIndex].InnerText;
                    gameInfo.AwayPoint = "";
                    gameInfo.Home = game.ChildNodes[awayIndex].InnerText;
                    gameInfo.HomePoint = "";
                    gameInfo.GameStates = (webStatus == "中止") ? ("P") : ("X");

                    #region 下載比賽資料，比賽時間 10 分鐘前就開始處理
                    if (!this.DownReal.ContainsKey(webID) &&
                        GetUtcKr(DateTime.Now) >= gameTime.AddMinutes(-10))
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
                        HtmlAgilityPack.HtmlNode node = null;

                        // 讀取資料
                        doc.LoadHtml(this.DownReal[webID].Data);

                        // 判斷是中止
                        if (gameInfo.GameStates == "P")
                        {
                            string findxpath = FindXPath(doc.DocumentNode, "yjSNLiveBattlereview");
                            if (!string.IsNullOrEmpty(findxpath))
                            {
                                node = doc.DocumentNode.SelectSingleNode(findxpath);
                                if (node != null && node.ChildNodes.Count > 0)
                                {
                                    // 原因
                                    gameInfo.TrackerText = node.ChildNodes[node.ChildNodes.Count - 2].InnerText;
                                }
                            }
                        }
                        else
                        {
                            #region 分數
                            node = doc.DocumentNode.SelectSingleNode(FindXPath(doc.DocumentNode, "scoreboard"));
                            if (node != null && node.ChildNodes.Count >= 1)
                            {
                                // Away
                                foreach (HtmlAgilityPack.HtmlNode data in node.ChildNodes[1].ChildNodes[1].ChildNodes[2].ChildNodes)
                                {
                                    // 不是資料就往下處理
                                    if (data.Name != "td")
                                        continue;
                                    // 分數
                                    gameInfo.AwayBoard.Add(data.InnerText.Replace("&nbsp;", ""));
                                }
                                gameInfo.AwayPoint = gameInfo.AwayBoard[gameInfo.AwayBoard.Count - 3];
                                gameInfo.AwayH = gameInfo.AwayBoard[gameInfo.AwayBoard.Count - 2];
                                gameInfo.AwayE = gameInfo.AwayBoard[gameInfo.AwayBoard.Count - 1];
                                gameInfo.AwayBoard.RemoveAt(gameInfo.AwayBoard.Count - 1);
                                gameInfo.AwayBoard.RemoveAt(gameInfo.AwayBoard.Count - 1);
                                gameInfo.AwayBoard.RemoveAt(gameInfo.AwayBoard.Count - 1);
                                // Home
                                foreach (HtmlAgilityPack.HtmlNode data in node.ChildNodes[1].ChildNodes[1].ChildNodes[4].ChildNodes)
                                {
                                    // 不是資料就往下處理
                                    if (data.Name != "td")
                                        continue;
                                    gameInfo.HomeBoard.Add(data.InnerText.Replace("&nbsp;", ""));
                                }
                                gameInfo.HomePoint = gameInfo.HomeBoard[gameInfo.HomeBoard.Count - 3];
                                gameInfo.HomeH = gameInfo.HomeBoard[gameInfo.HomeBoard.Count - 2];
                                gameInfo.HomeE = gameInfo.HomeBoard[gameInfo.HomeBoard.Count - 1];
                                gameInfo.HomeBoard.RemoveAt(gameInfo.HomeBoard.Count - 1);
                                gameInfo.HomeBoard.RemoveAt(gameInfo.HomeBoard.Count - 1);
                                gameInfo.HomeBoard.RemoveAt(gameInfo.HomeBoard.Count - 1);
                            }
                            // 移除無用的分數
                            if (gameInfo.Quarter > 0)
                            {
                                int index = gameInfo.AwayBoard.Count - 1;
                                while (index >= 0)
                                {
                                    if (gameInfo.AwayBoard[index] == "-")
                                        gameInfo.AwayBoard[index] = "0";

                                    if (gameInfo.HomeBoard[index] == "-")
                                        gameInfo.HomeBoard[index] = "0";

                                    if (string.IsNullOrEmpty(gameInfo.AwayBoard[index]))
                                        gameInfo.AwayBoard.RemoveAt(index);
                                    if (string.IsNullOrEmpty(gameInfo.HomeBoard[index]))
                                        gameInfo.HomeBoard.RemoveAt(index);
                                    index--;
                                }
                                gameInfo.GameStates = "S";
                            }
                            #endregion
                            #region BSO
                            node = doc.DocumentNode.SelectSingleNode(FindXPath(doc.DocumentNode, "sbo"));
                            if (node != null && node.ChildNodes.Count >= 1)
                            {
                                string txt = null;
                                int bStart = 0;
                                int bEnd = 0;
                                // B
                                txt = node.ChildNodes[3].ChildNodes[3].ChildNodes[1].InnerHtml.Replace("\n", "");
                                bStart = txt.IndexOf("<b>");
                                bEnd = txt.IndexOf("</b>");
                                if (bStart != -1 && bEnd != -1)
                                {
                                    gameInfo.BallB = txt.Substring(bStart + 3, bEnd - bStart - 3).Length;
                                    if (gameInfo.BallB > 3)
                                        gameInfo.BallB = 0;
                                }
                                // S
                                txt = node.ChildNodes[3].ChildNodes[3].ChildNodes[3].InnerHtml.Replace("\n", "");
                                bStart = txt.IndexOf("<b>");
                                bEnd = txt.IndexOf("</b>");
                                if (bStart != -1 && bEnd != -1)
                                {
                                    gameInfo.BallS = txt.Substring(bStart + 3, bEnd - bStart - 3).Length;
                                    if (gameInfo.BallS > 2)
                                        gameInfo.BallS = 0;
                                }
                                // O
                                txt = node.ChildNodes[3].ChildNodes[3].ChildNodes[5].InnerHtml.Replace("\n", "");
                                bStart = txt.IndexOf("<b>");
                                bEnd = txt.IndexOf("</b>");
                                if (bStart != -1 && bEnd != -1)
                                {
                                    gameInfo.BallO = txt.Substring(bStart + 3, bEnd - bStart - 3).Length;
                                    if (gameInfo.BallO > 2)
                                    {
                                        gameInfo.BallB = 0;
                                        gameInfo.BallS = 0;
                                        gameInfo.BallO = 0;
                                    }
                                }
                            }
                            #endregion
                            #region Bases
                            node = doc.DocumentNode.SelectSingleNode(FindXPath(doc.DocumentNode, "base"));
                            if (node != null && node.ChildNodes.Count >= 1)
                            {
                                if (!string.IsNullOrEmpty(FindXPath(doc.DocumentNode, "base1")))
                                {
                                    gameInfo.Bases += 1;
                                }
                                if (!string.IsNullOrEmpty(FindXPath(doc.DocumentNode, "base2")))
                                {
                                    gameInfo.Bases += 2;
                                }
                                if (!string.IsNullOrEmpty(FindXPath(doc.DocumentNode, "base3")))
                                {
                                    gameInfo.Bases += 4;
                                }
                            }
                            #endregion
                            #region 狀態
                            node = doc.DocumentNode.SelectSingleNode(FindXPath(doc.DocumentNode, "livenavi"));
                            if (node != null)
                            {
                                if (node.InnerText.IndexOf("試合前") != -1)
                                {
                                    gameInfo.GameStates = "X";
                                    gameInfo.BallB = 0;
                                    gameInfo.BallS = 0;
                                    gameInfo.BallO = 0;
                                    gameInfo.Bases = 0;
                                    gameInfo.AwayPoint = "";
                                    gameInfo.AwayBoard.Clear();
                                    gameInfo.AwayH = null;
                                    gameInfo.AwayE = null;
                                    gameInfo.HomePoint = "";
                                    gameInfo.HomeBoard.Clear();
                                    gameInfo.HomeH = null;
                                    gameInfo.HomeE = null;
                                }
                                if (node.InnerText.IndexOf("試合終了") != -1)
                                {
                                    gameInfo.GameStates = "E";
                                    gameInfo.BallB = 0;
                                    gameInfo.BallS = 0;
                                    gameInfo.BallO = 0;
                                    gameInfo.Bases = 0;
                                }
                            }
                            // 比賽結束
                            node = doc.DocumentNode.SelectSingleNode(FindXPath(doc.DocumentNode, "afterdata"));
                            if (node != null && !string.IsNullOrEmpty(node.InnerText))
                            {
                                gameInfo.GameStates = "E";
                                gameInfo.BallB = 0;
                                gameInfo.BallS = 0;
                                gameInfo.BallO = 0;
                                gameInfo.Bases = 0;
                            }
                            #endregion
                        }
                    #endregion
                    }

                    if (base.CheckGame(gameInfo))//檢查比分是否合法
                    {
                        this.GameData[gameInfo.WebID] = gameInfo;// 加入
                        result++;// 累計
                    }
                }
            }
            // 傳回
            return result;
        }
        public override bool Update(string connectionString, BasicInfo info)
        {
            // 以隊伍名稱當作更新的依據
            //return this.Update2(connectionString, info);
            // 改用多來源跟分
            return this.Update4(connectionString, info);
        }
        // 下載資料
        private BasicDownload DownHome;
        private BasicDownload DownHomeWarmUp;
        private Dictionary<string, BasicDownload> DownReal;
        private DateTime DownLastTime = DateTime.Now;
    }
}
