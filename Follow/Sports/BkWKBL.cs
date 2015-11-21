using Follow.Sports.Basic;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Web;

// 跟盤以 隊伍與日期為依據
// 跟盤日期以韓國時間顯示

namespace Follow.Sports
{
    public class BkWKBL : Basic.BasicBasketball
    {
        private string sWebUrl1 = UrlSetting.GetUrl(ESport.Basketball_WKBL, "Url1");
        public BkWKBL(DateTime today) : base(ESport.Basketball_WKBL)
        {
            if (string.IsNullOrWhiteSpace(this.sWebUrl))
            {
                this.sWebUrl = @"http://www.wkbl.or.kr//include/header_game.asp";
            }
            if (string.IsNullOrWhiteSpace(this.sWebUrl1))
            {
                this.sWebUrl1 = @"http://www.wkbl.or.kr/live11/path_live_player.asp?ckey={0}|{1}|{2}|";
            }
            // 設定
            this.AllianceID = 15;
            this.GameType = "BKKRW";
            this.GameDate = GetUtcKr(today).Date; // 只取日期
            //this.DownHome = new BasicDownload(this.Sport, @"http://www.wkbl.or.kr/main/main.asp", Encoding.GetEncoding(949));
            this.DownHome = new BasicDownload(this.Sport, this.sWebUrl, Encoding.UTF8);
            //this.DownHome.Proxy = Proxy;
            this.DownReal = new Dictionary<string, BasicDownload>();
        }
        public override void Download()
        {
            // 沒有資料或下載時間超過 5 分鐘才讀取首頁資料。
            if (this.GameData.Count == 0 ||
                DateTime.Now > this.DownLastTime.AddMinutes(5))
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
            string gameDateStr = null;
            DateTime gameDate = this.GameDate;
            DateTime gameTime = this.GameDate;
            HtmlAgilityPack.HtmlDocument document = new HtmlAgilityPack.HtmlDocument();
           // string xPath = "/html/body/div[3]/div/div/div"; //"/html[1]/body[1]/div[2]/div[5]/div[1]/div[1]/div[1]";
            string xPath = "//ul[@class='week_game']";
            
            // 載入資料
            document.LoadHtml(this.DownHome.Data);
            // 資料位置
            HtmlAgilityPack.HtmlNode nodeGames = document.DocumentNode.SelectSingleNode(xPath);

            // 判斷資料
            if (nodeGames != null && nodeGames.ChildNodes != null)
            {
                // 資料
                foreach (HtmlAgilityPack.HtmlNode game in nodeGames.ChildNodes)
                {

                    /// 不是資料就往下處理
                    //if (game.Name != "div") continue;
                    //if (game.Id.IndexOf("gs") == -1) continue;

                    if (game.Name != "li")
                    {
                        continue;
                    }

                    string season = null;
                    string type = null;
                    string webID = null;
                    string webUrl = null;
                    bool isEnd = false;

                    //判斷比賽是否結束，有時間代表尚未結束
                    string timeCheck = game.ChildNodes[3].ChildNodes[1].ChildNodes[1].InnerText;
                    if (timeCheck == null || !timeCheck.Contains(":"))
                    {
                        isEnd = true;
                    }
                    
                    #region 跟盤 ID
                    webID = game.ChildNodes[3].ChildNodes[3].ChildNodes[5].ChildNodes[1].ChildNodes[5].ChildNodes[1].GetAttributeValue("onClick", "");
                    // 沒有資料，這可能是因為比賽已經結束
                    if (webID == string.Empty)
                    {
                        webID = game.ChildNodes[3].ChildNodes[3].ChildNodes[5].ChildNodes[1].ChildNodes[5].ChildNodes[1].GetAttributeValue("href", "");
                        isEnd = true; // 比賽結束
                    }
                    else
                    {
                         bool isStart = game.ChildNodes[3].ChildNodes[3].ChildNodes[5].ChildNodes[1].ChildNodes[5].InnerHtml.Contains("goLive()");
                         if (!isStart)
                         {
                             //比賽尚未開始，往下處理
                             continue;
                        }

                        // 取代文字
                        webID = webID.ToLower();
                        webID = webID.Replace("open_win(", "");
                        webID = webID.Replace(")", "");
                        webID = webID.Replace("'", "");
                        // 分割
                        string[] data = webID.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                        // 判斷資料
                        if (data.Length > 0)
                        {
                            webID = data[0];
                        }
                    }

                    HttpRequest req = null;
                    // 判斷資料
                    if (webID != string.Empty)
                    {
                        Uri uri = null;
                        // 錯誤處理
                        try
                        {
                            uri = new Uri(@"http://www.a.com" + webID);
                            // 判斷是否有資料
                            if (uri.Query != null && !string.IsNullOrEmpty(uri.Query))
                            {
                                req = new HttpRequest("", uri.AbsoluteUri, uri.Query.Substring(1));
                                // 判斷資料
                                if (req["game_no"] != null && !string.IsNullOrEmpty(req["game_no"].Trim()))
                                {
                                    type = req["game_type"];
                                    season = req["season_gu"];
                                    webID = req["game_no"];
                                    webUrl = string.Format(sWebUrl1, season, type, webID);
                                    webID = type + "|" + webID;//透過type+no區別賽事
                                }
                            }
                        }
                        catch { webID = null; } // 錯誤，清除跟盤 ID
                    }

                    //try
                    //{
                    //    // 取得首頁賽程按鈕
                    //    webID = game.ChildNodes[7].ChildNodes[1].GetAttributeValue("onclick", "");
                    //    // 沒有資料，這可能是因為比賽已經結束
                    //    if (webID == string.Empty)
                    //        isEnd = true; // 比賽結束

                    //    season = nodeGames.ChildNodes[3].GetAttributeValue("value", "");
                    //    type = nodeGames.ChildNodes[5].GetAttributeValue("value", "");
                    //    webID = nodeGames.ChildNodes[7].GetAttributeValue("value", "");
                    //    webUrl = "http://www.wkbl.or.kr/live11/path_live_player.asp?ckey=" + season + "|" + type + "|" + webID + "|";

                    //}
                    //catch (Exception)
                    //{
                    //    webID = null;
                    //}

                    // 沒有跟盤 ID 就往下處理
                    if (webID == null || string.IsNullOrEmpty(webID.Trim()) || webUrl == null || string.IsNullOrEmpty(webID.Trim())) continue;
                    #endregion

                    #region 取得日期
                    //年/
                    gameDateStr = req["ym"].Substring(0, 4) + "/";
                    //月/日
                    gameDateStr += game.ChildNodes[3].ChildNodes[1].ChildNodes[0].InnerText.Substring(0, 5).Replace(".", "/");

                    //gameDateStr = game.ChildNodes[1].ChildNodes[1].ChildNodes[0].GetAttributeValue("alt", "")
                    //            + game.ChildNodes[1].ChildNodes[3].ChildNodes[0].GetAttributeValue("alt", "")
                    //            + game.ChildNodes[1].ChildNodes[5].ChildNodes[0].GetAttributeValue("alt", "");
                    //// 取代文字
                    //gameDateStr = gameDateStr.Replace("년", "/").Replace("월", "/").Replace("일", "");
                    //// 轉成時間失敗就離開
                    if (!DateTime.TryParse(gameDateStr, out gameDate))
                    {
                        break;
                    }

                    #endregion
                    #region 取得時間
                    if (!isEnd)
                    {
                        gameDateStr += " " + timeCheck;
                    }

                    //gameDateStr += " " + game.ChildNodes[5].ChildNodes[3].ChildNodes[1].ChildNodes[2].InnerText;
                    // 轉成日期失敗就離開
                    if (!DateTime.TryParse(gameDateStr, out gameTime))
                    {
                        break;
                    }
                    #endregion

                    // 建立比賽資料
                    gameInfo = null;
                    gameInfo = new BasicInfo(this.AllianceID, this.GameType, gameTime, webID);
                    gameInfo.AcH = true; // 第一個資料是 home 的，所以要交換隊伍資料。
                    //gameInfo.Away = game.ChildNodes[5].ChildNodes[5].InnerText;
                    //gameInfo.Home = game.ChildNodes[5].ChildNodes[1].InnerText;

                     //主 		
                   gameInfo.Home = game.ChildNodes[3].ChildNodes[3].ChildNodes[5].ChildNodes[1].ChildNodes[1].ChildNodes[1].InnerText;
                        //客
                   gameInfo.Away = game.ChildNodes[3].ChildNodes[3].ChildNodes[5].ChildNodes[3].ChildNodes[1].ChildNodes[1].InnerText;


                    #region 下載比賽資料，比賽時間 10 分鐘前就開始處理
                    if (!this.DownReal.ContainsKey(webID) &&
                        GetUtcKr(DateTime.Now) >= gameDate.AddMinutes(-10))
                    {
                        this.DownReal[webID] = new BasicDownload(this.Sport, webUrl, webID);
                        //this.DownReal[webID].Proxy = Proxy;
                        this.DownReal[webID].DownloadString();
                    }
                    #endregion
                    #region 處理比賽資料
                    if (this.DownReal.ContainsKey(webID) &&
                        !string.IsNullOrEmpty(this.DownReal[webID].Data))
                    {
                        System.Xml.XmlDocument doc = new System.Xml.XmlDocument();
                        // 錯誤處理
                        try
                        {

                            // 載入資料
                            doc.LoadXml(this.DownReal[webID].Data);
                            // 取出資料
                            System.Xml.XmlElement livePlayer = (System.Xml.XmlElement)doc.SelectSingleNode("livePlayer");
                            System.Xml.XmlElement team = null;
                            System.Xml.XmlElement player = null;
                            List<int> homeBoard = new List<int>();
                            List<int> awayBoard = new List<int>();                            
                            // 判斷資料，有球員才是準備比賽開始
                            if (livePlayer != null &&
                                livePlayer.SelectSingleNode("team") != null &&
                                livePlayer.SelectSingleNode("player") != null)
                            {
                                team = (System.Xml.XmlElement)livePlayer.SelectSingleNode("team");
                                player = (System.Xml.XmlElement)livePlayer.SelectSingleNode("player");
                                // 取出分數
                                int numAway = 0, numHome = 0;
                                for (int i = 1; i <= 8; i++)
                                {
                                    // 不是正確的分數就往下處理
                                    if (!int.TryParse(team.SelectSingleNode("away_qu" + i.ToString()).InnerText, out numAway) ||
                                        !int.TryParse(team.SelectSingleNode("home_qu" + i.ToString()).InnerText, out numHome))
                                    {
                                        continue;
                                    }
                                    awayBoard.Add(numAway);
                                    homeBoard.Add(numHome);
                                }
                                // 判斷分數
                                if (awayBoard != null)
                                {
                                    int num = 0;

                                    #region 找到最後的分數
                                    for (int i = awayBoard.Count - 1; i > 0; i--)
                                    {
                                        if (awayBoard[i].ToString() != "0" ||
                                            homeBoard[i].ToString() != "0")
                                        {
                                            num = i; break;
                                        }
                                    }
                                    // 如果超過 5 局，就以 5 局算
                                    if (num > 5) num = 5;
                                    #endregion

                                    // 總分
                                    gameInfo.AwayPoint = awayBoard[num].ToString();
                                    gameInfo.HomePoint = homeBoard[num].ToString();

                                    #region 計算真正的分數
                                    awayBoard.Add(0);
                                    homeBoard.Add(0);
                                    for (int i = num; i > 0; i--)
                                    {
                                        // 記錄
                                        awayBoard[awayBoard.Count - 1] = awayBoard[i];
                                        homeBoard[homeBoard.Count - 1] = homeBoard[i];
                                        // 計算
                                        awayBoard[i] = (int)awayBoard[awayBoard.Count - 1] - (int)awayBoard[i - 1];
                                        homeBoard[i] = (int)homeBoard[homeBoard.Count - 1] - (int)homeBoard[i - 1];
                                    }
                                    // 分數
                                    for (int i = 0; i <= num; i++)
                                    {
                                        gameInfo.AwayBoard.Add(awayBoard[i].ToString());
                                        gameInfo.HomeBoard.Add(homeBoard[i].ToString());
                                    }
                                    #endregion
                                }
                            }
                            #region 比賽結束
                            if (isEnd)
                            {
                                // 比賽結束
                                gameInfo.GameStates = "E";
                                gameInfo.Status = "結束";
                            }
                            else
                            {
                                // 比賽中
                                gameInfo.GameStates = "S";
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
            // 傳回
            return result;
        }
        public override bool Update(string connectionString, BasicInfo info)
        {
            return this.Update2(connectionString, info);
        }
        // 下載資料
        private BasicDownload DownHome;
        private Dictionary<string,BasicDownload> DownReal;
        private DateTime DownLastTime = DateTime.Now;
        /// <summary>
        /// 代理伺服器。
        /// </summary>
        public static string Proxy = null;
    }
}
