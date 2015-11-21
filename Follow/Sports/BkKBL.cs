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
    public class BkKBL : Basic.BasicBasketball
    {
        public BkKBL(DateTime today) : base(ESport.Basketball_KBL)
        {
            if (string.IsNullOrWhiteSpace(this.sWebUrl))
            {
                this.sWebUrl = @"http://basket2.7m.cn/DataFile/S2_fbig1.js";
            }
            // 設定
            this.AllianceID = 14;
            this.GameType = "BKKR";
            this.GameDate = GetUtcKr(today).Date; // 只取日期
            this.DownHome = new BasicDownload(this.Sport, sWebUrl);
        }
        public override void Download()
        {
            // 沒有資料或下載時間超過 2 秒才讀取首頁資料。
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
            if (!this.DownHome.HasChanged) return 0;

            int result = 0;
            BasicInfo gameInfo = null;
            string[] javasctript = this.DownHome.Data.Replace("\r\n", "").Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries);
            string[] data = null;
            string webID = null;
            DateTime gameTime = DateTime.Now;
            
            for (int index = 1; index < javasctript.Length; index++)
            {
                // 找到正確的比賽
                if (javasctript[index].IndexOf("韓甲") == -1) continue;
                // 取出資料
                webID = javasctript[index].Substring(0, javasctript[index].IndexOf("="));
                data = javasctript[index].Substring(javasctript[index].IndexOf("=") + 2).Replace("'", "").Split(',');
                // 正確的跟盤 ID
                webID = webID.Replace("bDt", "").Replace("[", "").Replace("]", "");
                // 取出時間
                if (DateTime.TryParse(data[1] + "/" + data[2] + "/" + data[3] + " " + data[4] + ":" + data[5] + ":" + data[6], out gameTime))
                {
                    // 建立比賽資料
                    gameInfo = null;
                    gameInfo = new BasicInfo(this.AllianceID, this.GameType, gameTime, webID);
                    //gameInfo.AcH = true;
                    gameInfo.Away = data[11];
                    gameInfo.Home = data[13];
                    // 比賽狀態
                    if (data[0] != "0")
                    {
                        // 分數
                        // Away
                        for (var i = 15; i < 20; i++)
                        {
                            gameInfo.AwayBoard.Add(data[i]);
                        }
                        gameInfo.AwayPoint = data[21];
                        // Home
                        for (var i = 22; i < 27; i++)
                        {
                            gameInfo.HomeBoard.Add(data[i]);
                        }
                        gameInfo.HomePoint = data[28];
                        // 判斷狀態
                        switch (data[0])
                        {
                            case "9": // 比賽結束
                                gameInfo.GameStates = "E";
                                // 移除其它分數
                                gameInfo.AwayBoard.RemoveAt(gameInfo.AwayBoard.Count - 1);
                                gameInfo.HomeBoard.RemoveAt(gameInfo.HomeBoard.Count - 1);
                                gameInfo.Status = "結束";
                                break;
                            case "11": // 比賽結束(加時)
                                gameInfo.GameStates = "E";
                                gameInfo.Status = "結束";
                                break;
                            case "12": // 中斷
                                gameInfo.GameStates = "P";
                                break;
                            case "13": // 取消
                                gameInfo.GameStates = "C";
                                break;
                            case "10": // 加時
                                gameInfo.GameStates = "S";
                                break;
                            case "1": // 第一節
                            case "2":
                                gameInfo.GameStates = "S";
                                // 移除其它分數
                                gameInfo.AwayBoard.RemoveRange(1, gameInfo.AwayBoard.Count - 1);
                                gameInfo.HomeBoard.RemoveRange(1, gameInfo.HomeBoard.Count - 1);
                                gameInfo.Status = data[0] == "2" ? "結束" : "";
                                break;
                            case "3": // 第二節
                            case "4":
                                gameInfo.GameStates = "S";
                                // 移除其它分數
                                gameInfo.AwayBoard.RemoveRange(2, gameInfo.AwayBoard.Count - 2);
                                gameInfo.HomeBoard.RemoveRange(2, gameInfo.HomeBoard.Count - 2);
                                gameInfo.Status = data[0] == "4" ? "結束" : "";
                                break;
                            case "5": // 第三節
                            case "6":
                                gameInfo.GameStates = "S";
                                // 移除其它分數
                                gameInfo.AwayBoard.RemoveRange(3, gameInfo.AwayBoard.Count - 3);
                                gameInfo.HomeBoard.RemoveRange(3, gameInfo.HomeBoard.Count - 3);
                                gameInfo.Status = data[0] == "6" ? "結束" : "";
                                break;
                            case "7": // 第四節
                            case "8":
                                gameInfo.GameStates = "S";
                                // 移除其它分數
                                gameInfo.AwayBoard.RemoveRange(4, gameInfo.AwayBoard.Count - 4);
                                gameInfo.HomeBoard.RemoveRange(4, gameInfo.HomeBoard.Count - 4);
                                gameInfo.Status = data[0] == "8" ? "結束" : "";
                                break;
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
        // 下載資料
        private BasicDownload DownHome;
        private DateTime DownLastTime = DateTime.Now;
    }

    public class BkKBL_NV : Basic.BasicBasketball
    {
        public BkKBL_NV(DateTime today) : base(ESport.Basketball_KBL)
        {
            // 設定
            this.AllianceID = 14;
            this.GameType = "BKKR";
            this.GameDate = GetUtcKr(today).Date; // 只取日期
            this.DownHome = new BasicDownload(this.Sport, @"http://sports.news.naver.com/sports/index.nhn?category=kbl&ctg=schedule", Encoding.GetEncoding(949));
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
            string xPath = "/html[1]/body[1]/center[1]/table[2]/tr[1]/td[2]/div[1]/table[3]/tbody[1]"; // table[3] 改 p[1] 就是年月
            // 載入資料
            document.LoadHtml(this.DownHome.Data);
            // 資料位置
            HtmlAgilityPack.HtmlNode nodeGames = document.DocumentNode.SelectSingleNode(xPath);
            // 判斷資料
            if (nodeGames == null)
            {
                xPath = "/html[1]/body[1]/center[1]/table[2]/tr[1]/td[1]/td[1]/div[1]/table[3]/tbody[1]"; // table[3] 改 p[1] 就是年月
                nodeGames = document.DocumentNode.SelectSingleNode(xPath);
            }
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
                    int index = 0;

                    #region 取得日期
                    if (game.ChildNodes.Count == 9)
                    {
                        gameDateStr = game.ChildNodes[1].InnerText.Replace("월", "/").Replace("일", "").Replace(" ", "");
                        // 截斷日期
                        if (gameDateStr.IndexOf("(") != -1)
                            gameDateStr = DateTime.Now.ToString("yyyy") + "/" + gameDateStr.Substring(0, gameDateStr.IndexOf("(")).Trim();
                        // 判斷日期，失敗就往下處理
                        if (!DateTime.TryParse(gameDateStr, out gameDate))
                        {
                            gameDateStr = null;
                            continue;
                        }
                        index = 2;
                    }
                    // 沒有日期就往下處理
                    if (string.IsNullOrEmpty(gameDateStr)) continue;
                    #endregion
                    #region 取得時間
                    // 判斷日期，錯誤就離開
                    if (!DateTime.TryParse(gameDate.ToString("yyyy/MM/dd") + " " + game.ChildNodes[index + 3].InnerText, out gameTime)) continue;
                    // 判斷是否為今天，不是今天就往下處理
                    if (this.GameDate.Date != gameTime.Date)
                    {
                        gameDateStr = null;
                        continue;
                    }
                    #endregion
                    #region 跟盤 ID
                    webID = game.ChildNodes[index + 5].ChildNodes[1].GetAttributeValue("href", "");
                    Uri uri = null;
                    HttpRequest req = null;
                    // 錯誤處理
                    try
                    {
                        uri = new Uri(@"http://www.a.com" + webID);
                        // 判斷是否有資料
                        if (uri.Query != null && !string.IsNullOrEmpty(uri.Query))
                        {
                            req = new HttpRequest("", uri.AbsoluteUri, uri.Query.Substring(1));
                            // 判斷資料
                            if (req["gameid"] != null && !string.IsNullOrEmpty(req["gameid"].Trim()))
                            {
                                webID = req["gameid"];
                                webUrl = @"http://sportsdata.naver.com/data//kbl_game/"
                                       + webID.Substring(0, 4) + "/" + webID.Substring(4, 2) + "/" + webID + ".nsd";
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
                    gameInfo.AcH = true;

                    string[] teamName = game.ChildNodes[index + 1].InnerText.Split(new string[] { "vs" }, StringSplitOptions.RemoveEmptyEntries);
                    // 隊伍名稱
                    if (teamName.Length == 2)
                    {
                        gameInfo.Away = teamName[0].Trim();
                        gameInfo.Home = teamName[1].Trim();
                    }

                    #region 下載比賽資料，比賽時間 10 分鐘前就開始處理
                    if (!this.DownReal.ContainsKey(webID) &&
                        GetUtcKr(DateTime.Now) >= gameTime.AddMinutes(-10))
                    {
                        this.DownReal[webID] = new BasicDownload(this.Sport, webUrl, Encoding.GetEncoding(949), webID);
                        this.DownReal[webID].DownloadString();
                    }
                    #endregion
                    #region 處理比賽資料
                    if (this.DownReal.ContainsKey(webID) &&
                        !string.IsNullOrEmpty(this.DownReal[webID].Data))
                    {
                        string[] data = this.DownReal[webID].Data.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
                        // 判斷數量
                        if (data != null && data.Length == 7 && !string.IsNullOrEmpty(data[3]))
                        {
                            string strJson = (data[3].IndexOf("{") != -1) ? (data[3].Substring(data[3].IndexOf("{"))) : (data[3]);
                            // 錯誤處理
                            try
                            {
                                JObject json = JObject.Parse(strJson);
                                string home_team = json["home_team"].ToString();
                                string away_team = json["away_team"].ToString();

                                // 隊伍名稱
                                gameInfo.Away = json["team_avg_rec"][away_team]["short_name"].ToString();
                                gameInfo.Home = json["team_avg_rec"][home_team]["short_name"].ToString();
                                gameInfo.Status = json["live_text"]["time"].ToString().Replace("경기전", "賽前");

                                #region 分數
                                for (int i = 1; i < 5; i++)
                                {
                                    if (json["quarter_score"][home_team].SelectToken("Q" + i.ToString()) != null)
                                    {
                                        gameInfo.AwayBoard.Add(json["quarter_score"][away_team]["Q" + i.ToString()].ToString());
                                        gameInfo.HomeBoard.Add(json["quarter_score"][home_team]["Q" + i.ToString()].ToString());
                                    }
                                }
                                #region 延長賽
                                bool hasOT = false;
                                int home_OT = 0;
                                int away_OT = 0;
                                // 延長賽分數
                                for (int i = 1; i < 5; i++)
                                {
                                    if (json["quarter_score"][home_team].SelectToken("X" + i.ToString()) != null)
                                    {
                                        hasOT = true;
                                        away_OT += int.Parse(json["quarter_score"][away_team]["X" + i.ToString()].ToString());
                                        home_OT += int.Parse(json["quarter_score"][home_team]["X" + i.ToString()].ToString());
                                    }
                                }
                                // 判斷
                                if (hasOT)
                                {
                                    gameInfo.AwayBoard.Add(away_OT.ToString());
                                    gameInfo.HomeBoard.Add(home_OT.ToString());
                                }
                                #endregion
                                // 總分
                                gameInfo.AwayPoint = json["quarter_score"][away_team]["total_score"].ToString();
                                gameInfo.HomePoint = json["quarter_score"][home_team]["total_score"].ToString();
                                #endregion
                                #region  取得比賽中的時間
                                if (json["live_text"][json["live_text"]["quarter"].ToString()] != null)
                                {
                                    string time = null;
                                    // 錯誤處理
                                    try
                                    {
                                        // 取到最後的時間
                                        foreach (JProperty obj in json["live_text"][json["live_text"]["quarter"].ToString()])
                                        {
                                            time = obj.Name;
                                        }
                                    }
                                    catch { time = null; }
                                    // 加入狀態
                                    if (time != null && !string.IsNullOrEmpty(time))
                                    {
                                        // 轉換
                                        DateTime oldTime = DateTime.Parse("00:" + time);
                                        DateTime newTime = DateTime.Parse("00:" + "10:00").AddSeconds(0 - (oldTime.Second + 60 * oldTime.Minute));
                                        // 設定
                                        gameInfo.Status = newTime.ToString("mm:ss");
                                    }
                                }
                                else
                                {
                                    if (gameInfo.Status != "賽前")
                                    {
                                        gameInfo.Status = null;
                                    }
                                }
                                #endregion
                                #region 比賽結束
                                if (json["live_text"]["quarter"].ToString().ToLower() == "end")
                                {
                                    gameInfo.GameStates = "E";
                                    gameInfo.Status = "結束";
                                }
                                else
                                {
                                    // 有分數才是比賽開始
                                    if (gameInfo.Quarter > 0)
                                    {
                                        gameInfo.GameStates = "S";
                                    }
                                }
                                #endregion
                            }
                            catch { }
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
        // 下載資料
        private BasicDownload DownHome;
        private Dictionary<string, BasicDownload> DownReal;
        private DateTime DownLastTime = DateTime.Now;
    }
}
