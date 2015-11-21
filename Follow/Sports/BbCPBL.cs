using Follow.Sports.Basic;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Web;

// 跟盤以 ID 為依據
// 跟盤日期以韓國時間顯示

namespace Follow.Sports
{
    public class BbCPBL : Basic.BasicBaseball
    {
        //详情来源网
        private string sWebUrl1 = UrlSetting.GetUrl(ESport.Baseball_CPBL, "Url1");//分数页面
        private string sWebUrl2 = UrlSetting.GetUrl(ESport.Baseball_CPBL, "Url2");//单局
        private string sWebUrl3 = UrlSetting.GetUrl(ESport.Baseball_CPBL, "Url3");//全局

        public BbCPBL(DateTime today)
            : base(ESport.Baseball_CPBL)
        {
            // 設定
            this.AllianceID = 30;
            this.GameType = "BBTW";
            this.GameDate = GetUtcTw(today).Date; // 只取日期
            //this.DownHome = new BasicDownload(this.Sport, @"http://www.cpbl.com.tw/Standings/AllScoreqry.aspx?gamekind=01&myfield=F00&mon=3&qyear=2012");

            #region 来源网设定
            //如果xml中没有配置就使用下面的默认地址
            if (string.IsNullOrWhiteSpace(this.sWebUrl))
            {
                this.sWebUrl = "http://www.cpbl.com.tw/standings/Allscoreqry.aspx";
            }
            if (string.IsNullOrWhiteSpace(sWebUrl1))
            {
                sWebUrl1 = "http://online.cpbl.com.tw/online2010/ScoreBoard.aspx?gameno=01&pbyear={0}&game={1}";
            }
            if (string.IsNullOrWhiteSpace(sWebUrl2))
            {
                sWebUrl2 = "http://online.cpbl.com.tw/online2010/Inner.aspx?gameno=01&pbyear={0}&game={1}";
            }
            if (string.IsNullOrWhiteSpace(sWebUrl3))
            {
                sWebUrl3 = "http://online.cpbl.com.tw/online2010/Game.aspx?gameno=01&pbyear={0}&game={1}";
            }
            #endregion

            this.DownHome = new BasicDownload(this.Sport, this.sWebUrl);
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
            BasicInfo gameInfo = null;
            HtmlAgilityPack.HtmlDocument document = new HtmlAgilityPack.HtmlDocument();
            string xPath = "/html[1]/body[1]/center[1]/table[1]/tr[3]/td[1]/td[1]/table[1]/tr[2]/td[1]/table[1]/tr[1]/td[1]/font[1]/select[1]";
            // 載入資料
            document.LoadHtml(this.DownHome.Data);
            // 資料位置 (年度)
            HtmlAgilityPack.HtmlNode nodeGames = document.DocumentNode.SelectSingleNode(xPath);
            // 判斷資料
            if (nodeGames != null && nodeGames.ChildNodes.Count > 0)
            {
                string gameYear = null;
                string gameMonth = null;
                DateTime gameDate = this.GameDate;
                DateTime gameTime = this.GameDate;
                Dictionary<string, string> box = new Dictionary<string, string>();

                #region 取出年度
                foreach (HtmlAgilityPack.HtmlNode year in nodeGames.ChildNodes)
                {
                    // 找出選擇
                    if (year.GetAttributeValue("selected", "") == "selected")
                    {
                        gameYear = year.GetAttributeValue("value", "");
                        break;
                    }
                }
                #endregion
                #region 取出月份
                xPath = "/html[1]/body[1]/center[1]/table[1]/tr[3]/td[1]/td[1]/table[1]/tr[2]/td[1]/table[2]/tr[1]/td[1]/table[1]/tr[1]/td[1]";
                nodeGames = document.DocumentNode.SelectSingleNode(xPath);
                // 沒有資料就離開
                if (nodeGames == null || string.IsNullOrEmpty(nodeGames.InnerText) ||
                    nodeGames.InnerText.LastIndexOf("月") == -1)
                {
                    // 失敗就離開
                    return 0;
                }
                else
                {
                    gameMonth = nodeGames.InnerText.Substring(0, nodeGames.InnerText.LastIndexOf("月")).Trim();
                    // 轉成正確的月份
                    if (gameMonth == "一") gameMonth = "1";
                    if (gameMonth == "二") gameMonth = "2";
                    if (gameMonth == "三") gameMonth = "3";
                    if (gameMonth == "四") gameMonth = "4";
                    if (gameMonth == "五") gameMonth = "5";
                    if (gameMonth == "六") gameMonth = "6";
                    if (gameMonth == "七") gameMonth = "7";
                    if (gameMonth == "八") gameMonth = "8";
                    if (gameMonth == "九") gameMonth = "9";
                    if (gameMonth == "十") gameMonth = "10";
                    if (gameMonth == "十一") gameMonth = "11";
                    if (gameMonth == "十二") gameMonth = "12";
                }
                // 轉成日期
                if (!DateTime.TryParse(gameYear + "/" + gameMonth + "/1", out gameDate)) return 0;
                #endregion
                #region 取出狀態
                xPath = FindXPath(document.DocumentNode, "lblG2");
                // 判斷資料
                if (!string.IsNullOrEmpty(xPath))
                {
                    nodeGames = document.DocumentNode.SelectSingleNode(xPath);
                    // 資料
                    foreach (HtmlAgilityPack.HtmlNode gameRow in nodeGames.ChildNodes[1].ChildNodes)
                    {
                        // 不是資料就往下處理
                        if (gameRow.Name != "tr") continue;
                        if (gameRow.ChildNodes.Count < 2) continue;

                        // 判斷比賽中
                        if (gameRow.ChildNodes[2].ChildNodes[0].Name == "font")
                        {
                            box[gameRow.ChildNodes[0].InnerText] = gameRow.ChildNodes[2].InnerText.Trim();
                        }
                        if (gameRow.ChildNodes[2].ChildNodes[0].Name == "a" &&
                            gameRow.ChildNodes[2].ChildNodes.Count < 5)
                        {
                            box[gameRow.ChildNodes[0].InnerText] = "E";
                        }
                    }
                }
                #endregion

                xPath = "/html[1]/body[1]/center[1]/table[1]/tr[3]/td[1]/td[1]/table[1]/tr[2]/td[1]/table[2]/tr[1]/td[1]/table[1]/tr[2]/td[1]/table[1]";
                nodeGames = document.DocumentNode.SelectSingleNode(xPath);
                // 沒有資料就離開
                if (nodeGames == null || nodeGames.ChildNodes.Count == 0) return 0;
                // 資料
                foreach (HtmlAgilityPack.HtmlNode gameRow in nodeGames.ChildNodes)
                {
                    // 不是資料就往下處理
                    if (gameRow.Name != "tr") continue;
                    // 資料
                    foreach (HtmlAgilityPack.HtmlNode td in gameRow.ChildNodes)
                    {
                        // 不是資料就往下處理
                        if (td.ChildNodes[0].ChildNodes.Count != 2) continue;
                        // 不是日期就往下處理
                        if (!DateTime.TryParse(gameDate.ToString("yyyy-MM") + "-" + td.ChildNodes[0].ChildNodes[0].InnerText, out gameTime)) continue;

                        string webID = null;
                        string webUrl1 = null;
                        string webUrl2 = null;
                        string webUrl3 = null;
                        string[] info = new string[] { "", "", "", "", "", "", "" };
                        int index = 0;
                        // 資料
                        foreach (HtmlAgilityPack.HtmlNode data in td.ChildNodes[0].ChildNodes[1].ChildNodes)
                        {
                            // 資料是分隔就清空
                            if (data.Name == "img")
                            {
                                index = 0;
                                continue;
                            }
                            // 不是資料就往下處理
                            if (data.Name != "#text" || string.IsNullOrEmpty(data.InnerText.Trim())) continue;
                            // 放入資料
                            info[index] = data.InnerText.Trim();
                            // 位置累計
                            index++;
                            // 判斷數量與時間
                            if (index >= 5)
                            {
                                string[] team = new string[] { "", "" };
                                if (info[1].ToLower().IndexOf("vs.") != -1)
                                {
                                    // 判斷時間
                                    if (!DateTime.TryParse(gameTime.ToString("yyyy-MM-dd") + " " + info[3], out gameTime)) continue;
                                    team = info[1].ToLower().Split(new string[] { "vs." }, StringSplitOptions.RemoveEmptyEntries);
                                }
                                else
                                {
                                    // 判斷時間
                                    if (!DateTime.TryParse(gameTime.ToString("yyyy-MM-dd") + " " + info[4], out gameTime)) continue;
                                    team[0] = info[1];
                                    team[1] = info[2];
                                }

                                // 不是今天就往下處理
                                if (gameTime.Date != this.GameDate.Date) continue;
                                //if (gameTime.Date != DateTime.Parse("2013-4-6")) continue;

                                webID = info[0];
                                // 建立比賽資料
                                gameInfo = null;
                                gameInfo = new BasicInfo(this.AllianceID, this.GameType, gameTime, webID);
                                gameInfo.IsBall = true;
                                gameInfo.Away = team[0].Trim();
                                gameInfo.Home = team[1].Trim();

                                #region 下載比賽資料，比賽時間 10 分鐘前就開始處理
                                // 比賽分數
                                webUrl1 = string.Format(sWebUrl1, gameTime.ToString("yyyy"), webID);
                                if (!this.DownReal.ContainsKey(webUrl1) &&
                                    GetUtcKr(DateTime.Now) >= gameTime.AddMinutes(-10))
                                {
                                    this.DownReal[webUrl1] = new BasicDownload(this.Sport, webUrl1);
                                    this.DownReal[webUrl1].DownloadString();
                                }
                                // 單局內容
                                webUrl2 = string.Format(sWebUrl2, gameTime.ToString("yyyy"), webID);
                                if (!this.DownReal.ContainsKey(webUrl2) &&
                                    GetUtcKr(DateTime.Now) >= gameTime.AddMinutes(-10))
                                {
                                    this.DownReal[webUrl2] = new BasicDownload(this.Sport, webUrl2);
                                    this.DownReal[webUrl2].DownloadString();
                                }
                                // 全局內容
                                webUrl3 = string.Format(sWebUrl3, gameTime.ToString("yyyy"), webID);
                                if (!this.DownReal.ContainsKey(webUrl3) &&
                                    GetUtcKr(DateTime.Now) >= gameTime.AddMinutes(-10))
                                {
                                    this.DownReal[webUrl3] = new BasicDownload(this.Sport, webUrl3);
                                    this.DownReal[webUrl3].DownloadString();
                                }
                                #endregion
                                #region 處理比賽資料
                                if (this.DownReal.ContainsKey(webUrl1) &&
                                    this.DownReal.ContainsKey(webUrl2) &&
                                    this.DownReal.ContainsKey(webUrl3) &&
                                    !string.IsNullOrEmpty(this.DownReal[webUrl1].Data) &&
                                    !string.IsNullOrEmpty(this.DownReal[webUrl2].Data) &&
                                    !string.IsNullOrEmpty(this.DownReal[webUrl3].Data))
                                {
                                    HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
                                    int round1 = 0;
                                    int round2 = 0;

                                    #region 單局內容
                                    doc.LoadHtml(this.DownReal[webUrl2].Data);
                                    // 判斷資料
                                    if (doc.DocumentNode.ChildNodes[0].ChildNodes.Count == 7)
                                    {
                                        // 轉成局數
                                        int.TryParse(doc.DocumentNode.ChildNodes[0].ChildNodes[3].ChildNodes[0].InnerText, out round1);
                                        // 判斷比賽結束
                                        if (gameInfo.GameStates == "X")
                                        {
                                            //gameInfo.GameStates = (this.DownReal[webUrl2].Data.IndexOf("比賽結束") != -1) ? ("E") : ("S");
                                        }
                                    }
                                    #endregion
                                    #region 全局內容
                                    doc.LoadHtml(this.DownReal[webUrl3].Data);
                                    // 資料
                                    foreach (HtmlAgilityPack.HtmlNode table in doc.DocumentNode.ChildNodes)
                                    {
                                        // 不是資料就往下處理
                                        if (table.Name != "table") continue;
                                        if (table.ChildNodes.Count <= 5) continue;
                                        // 資料
                                        foreach (HtmlAgilityPack.HtmlNode tr in table.ChildNodes)
                                        {
                                            // 不是資料就往下處理
                                            if (tr.Name != "tr") continue;
                                            // 累計
                                            round2++;
                                        }
                                    }
                                    round2 = (round2 - 1) / 3;
                                    // 判斷局數
                                    if (round1 == 0 && round2 > 0) round1 = round2;
                                    #endregion
                                    #region 比賽分數
                                    doc.LoadHtml(this.DownReal[webUrl1].Data);
                                    // 判斷資料
                                    if (doc.DocumentNode.ChildNodes[0].ChildNodes.Count == 7)
                                    {
                                        // Away
                                        foreach (HtmlAgilityPack.HtmlNode d in doc.DocumentNode.ChildNodes[0].ChildNodes[3].ChildNodes)
                                        {
                                            // 不是資料就往下處理
                                            if (d.Name != "td") continue;
                                            // 加入分數
                                            gameInfo.AwayBoard.Add((!string.IsNullOrEmpty(d.InnerText.Trim())) ? (d.InnerText.Trim()) : (""));
                                        }
                                        gameInfo.AwayBoard.RemoveAt(0);
                                        // RHE
                                        gameInfo.AwayPoint = gameInfo.AwayBoard[gameInfo.AwayBoard.Count - 3];
                                        gameInfo.AwayH = gameInfo.AwayBoard[gameInfo.AwayBoard.Count - 2];
                                        gameInfo.AwayE = gameInfo.AwayBoard[gameInfo.AwayBoard.Count - 1];
                                        // 移除 RHE
                                        gameInfo.AwayBoard.RemoveAt(gameInfo.AwayBoard.Count - 1);
                                        gameInfo.AwayBoard.RemoveAt(gameInfo.AwayBoard.Count - 1);
                                        gameInfo.AwayBoard.RemoveAt(gameInfo.AwayBoard.Count - 1);
                                        // Home
                                        foreach (HtmlAgilityPack.HtmlNode d in doc.DocumentNode.ChildNodes[0].ChildNodes[5].ChildNodes)
                                        {
                                            // 不是資料就往下處理
                                            if (d.Name != "td") continue;
                                            // 加入分數
                                            gameInfo.HomeBoard.Add((!string.IsNullOrEmpty(d.InnerText.Trim())) ? (d.InnerText.Trim()) : (""));
                                        }
                                        gameInfo.HomeBoard.RemoveAt(0);
                                        // RHE
                                        gameInfo.HomePoint = gameInfo.HomeBoard[gameInfo.HomeBoard.Count - 3];
                                        gameInfo.HomeH = gameInfo.HomeBoard[gameInfo.HomeBoard.Count - 2];
                                        gameInfo.HomeE = gameInfo.HomeBoard[gameInfo.HomeBoard.Count - 1];
                                        // 移除 RHE
                                        gameInfo.HomeBoard.RemoveAt(gameInfo.HomeBoard.Count - 1);
                                        gameInfo.HomeBoard.RemoveAt(gameInfo.HomeBoard.Count - 1);
                                        gameInfo.HomeBoard.RemoveAt(gameInfo.HomeBoard.Count - 1);
                                    }
                                    // 移除多餘的分數 測試
                                    round1 = gameInfo.AwayBoard.Count;
                                    if (round1 < gameInfo.HomeBoard.Count) round1 = gameInfo.HomeBoard.Count;
                                    while (round1 > 0)
                                    {
                                        round1--;
                                        if (round1 < gameInfo.AwayBoard.Count &&
                                            string.IsNullOrEmpty(gameInfo.AwayBoard[round1]))
                                        {
                                            gameInfo.AwayBoard.RemoveAt(round1);
                                        }
                                        if (round1 < gameInfo.HomeBoard.Count &&
                                            string.IsNullOrEmpty(gameInfo.HomeBoard[round1]))
                                        {
                                            gameInfo.HomeBoard.RemoveAt(round1);
                                        }
                                        if (gameInfo.AwayBoard.Count == 0 ||
                                            gameInfo.HomeBoard.Count == 0)
                                        {
                                            break;
                                        }
                                        if (!string.IsNullOrEmpty(gameInfo.AwayBoard[gameInfo.AwayBoard.Count - 1]) ||
                                            !string.IsNullOrEmpty(gameInfo.HomeBoard[gameInfo.HomeBoard.Count - 1]))
                                        {
                                            if (string.IsNullOrEmpty(gameInfo.AwayBoard[gameInfo.AwayBoard.Count - 1]))
                                            {
                                                gameInfo.AwayBoard.RemoveAt(gameInfo.AwayBoard.Count - 1);
                                            }
                                            if (string.IsNullOrEmpty(gameInfo.HomeBoard[gameInfo.HomeBoard.Count - 1]))
                                            {
                                                gameInfo.HomeBoard.RemoveAt(gameInfo.HomeBoard.Count - 1);
                                            }
                                            break;
                                        }
                                    }
                                    #endregion
                                    #region 比賽狀態
                                    if (gameInfo.Quarter == 0)
                                    {
                                        gameInfo.GameStates = "X";
                                    }
                                    else
                                    {
                                        // 判斷比賽結束
                                        if (gameInfo.GameStates == "S" ||
                                            gameInfo.GameStates == "X")
                                        {
                                            gameInfo.GameStates = (this.DownReal[webUrl3].Data.IndexOf("再見") != -1) ? ("E") : ("S");
                                        }
                                    }
                                    if (gameInfo.GameStates == "S" &&
                                        box.ContainsKey(gameInfo.Away + gameInfo.Home))
                                    {
                                        // 比賽結束
                                        if (box[gameInfo.Away + gameInfo.Home] == "E")
                                        {
                                            gameInfo.GameStates = "E";
                                        }
                                        else
                                        {
                                            if (gameInfo.Quarter == 0)
                                            {
                                                gameInfo.GameStates = "X";
                                                gameInfo.AwayPoint = "";
                                                gameInfo.AwayH = "0";
                                                gameInfo.AwayE = "0";
                                                gameInfo.HomePoint = "";
                                                gameInfo.HomeH = "0";
                                                gameInfo.HomeE = "0";
                                                gameInfo.BallB = 0;
                                                gameInfo.BallS = 0;
                                                gameInfo.BallO = 0;
                                                gameInfo.Bases = 0;
                                            }
                                        }
                                    }
                                    #endregion
                                }
                                #endregion

                                // 加入
                                this.GameData[gameInfo.WebID] = gameInfo;
                                // 累計
                                result++;
                            }
                        }

                    }
                }
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
