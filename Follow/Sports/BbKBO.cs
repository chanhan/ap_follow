using Follow.Sports.Basic;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Follow.Sports
{
    public class BbKBO : Basic.BasicBaseball
    {
        private string sWebUrl1 = UrlSetting.GetUrl(ESport.Baseball_KBO, "Url1");
        public BbKBO(DateTime today)
            : base(ESport.Baseball_KBO)
        {
            // 設定
            this.AllianceID = 66;
            this.GameType = "BBKR";
            this.GameDate = GetUtcKr(today).Date; // 只取日期

            if (string.IsNullOrWhiteSpace(this.sWebUrl))
            {
                this.sWebUrl = @"http://sports.news.naver.com/schedule/index.nhn?category=kbo";
            }
            if (string.IsNullOrWhiteSpace(this.sWebUrl1))
            {
                this.sWebUrl1 = @"http://sportsdata.naver.com/ndata/kbo/{0}/{1}/{2}.nsd";
            }
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
            //HtmlAgilityPack.HtmlDocument document = new HtmlAgilityPack.HtmlDocument();
            //string xPath = "/html[1]/body[1]/div[1]/div[2]/div[1]/div[6]";
            //// 載入資料
            //document.LoadHtml(this.DownHome.Data);
            // 資料位置
            //HtmlAgilityPack.HtmlNode nodeGames = document.DocumentNode.SelectSingleNode(xPath);


            HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(this.DownHome.Data);
            HtmlAgilityPack.HtmlNode nodeGames = doc.GetElementbyId("calendarWrap");
            // 判斷資料
            if (nodeGames != null && nodeGames.ChildNodes != null)
            {
                DateTime gameDate = this.GameDate;
                DateTime gameTime = this.GameDate;
                // 資料
                foreach (HtmlAgilityPack.HtmlNode gameRow in nodeGames.ChildNodes)
                {
                    // 不是資料就往下處理
                    if (gameRow.Name != "div") continue;
                    if (gameRow.GetAttributeValue("class", "").IndexOf("nogame") != -1) continue;

                    HtmlAgilityPack.HtmlNode table = gameRow.ChildNodes[1].ChildNodes[3];
                    // 資料
                    for (int tr = 1; tr < table.ChildNodes.Count; tr += 2)
                    {
                        string webID = null;
                        string webUrl = null;
                        int td = 1;

                        #region 取得日期
                        if (tr == 1)
                        {
                            string dateStr = table.ChildNodes[tr].ChildNodes[1].ChildNodes[1].ChildNodes[0].InnerText.Replace(".", "/");
                            // 日期錯誤就離開
                            if (!DateTime.TryParse(gameDate.ToString("yyyy/") + " " + dateStr, out gameDate))
                            {
                                break;
                            }
                            td += 2;
                        }
                        #endregion

                        // 資料錯誤就往下處理
                        if (table.ChildNodes[tr].ChildNodes[td + 2].ChildNodes.Count != 10) continue;

                        #region 取得時間
                        string timeStr = table.ChildNodes[tr].ChildNodes[td].InnerText;
                        // 時間錯誤就離開
                        if (!DateTime.TryParse(gameDate.ToString("yyyy/MM/dd") + " " + timeStr, out gameTime)) break;
                        #endregion
                        #region 跟盤 ID
                        if (table.ChildNodes[tr].ChildNodes[td + 4].ChildNodes[1].ChildNodes.Count < 2)
                        {
                            // 沒有跟盤 ID，比賽是中止
                            webID = "P" + table.ChildNodes[tr].ChildNodes[td + 2].ChildNodes[0].InnerText
                                        + table.ChildNodes[tr].ChildNodes[td + 2].ChildNodes[8].InnerText;
                        }
                        else
                        {
                            if (table.ChildNodes[tr].ChildNodes[td + 4].ChildNodes[1].ChildNodes[7].Name.ToLower()=="a")
                            {
                                webID = table.ChildNodes[tr].ChildNodes[td + 4].ChildNodes[1].ChildNodes[7].GetAttributeValue("href", "");
                            }
                            else
                            {
                                webID = table.ChildNodes[tr].ChildNodes[td + 4].ChildNodes[1].ChildNodes[3].GetAttributeValue("href", "");
                            }                           

                            int findFirst = webID.IndexOf("\'");
                            int findLast = webID.LastIndexOf("\'");
                            if (findFirst != -1 && findLast != -1)
                            {
                                findFirst++;
                                webID = webID.Substring(findFirst, findLast - findFirst);
                            }
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
                                        // 判斷編號是否為日期
                                        if (DateTime.TryParse(webID.Substring(0, 4) + "/" + webID.Substring(4, 2) + "/" + webID.Substring(6, 2), out gameDate) &&
                                            gameDate.Date == gameTime.Date)
                                        {
                                            webUrl = string.Format(sWebUrl1, webID.Substring(0, 4), webID.Substring(4, 2), webID) + "?tk=" + DateTime.Now.Ticks.ToString();                                                
                                        }
                                        else
                                        {
                                            gameDate = gameTime.Date;
                                            webUrl = string.Format(sWebUrl1, gameTime.Date.ToString("yyyy"), gameTime.Date.ToString("MM"), webID);  
                                        }
                                    }
                                }
                            }
                            catch { webID = null; } // 錯誤，清除跟盤 ID 
                            // 沒有跟盤 ID 就往下處理
                            if (webID == null || string.IsNullOrEmpty(webID.Trim())) continue;
                        }
                        #endregion

                        // 不是今天就往下處理
                        if (gameTime.Date != this.GameDate.Date) continue;
                        //if (gameTime.Date != DateTime.Parse("2013-7-7")) continue;

                        // 建立比賽資料
                        gameInfo = null;
                        gameInfo = new BasicInfo(this.AllianceID, this.GameType, gameTime, webID);
                        gameInfo.IsBall = true;
                        gameInfo.Away = table.ChildNodes[tr].ChildNodes[td + 2].ChildNodes[0].InnerText;
                        gameInfo.Home = table.ChildNodes[tr].ChildNodes[td + 2].ChildNodes[8].InnerText;

                        // 比賽中止
                        if (webID.Substring(0, 1) == "P")
                        {
                            //因比賽中止無法透過webID取得正常比分資料，所以取緩存/DB
                            GetGameScore(gameInfo, -1, true);

                            gameInfo.GameStates = "P";
                            gameInfo.TrackerText = "因雨延賽";

                            //BSOB 清除
                            gameInfo.BallB = 0;
                            gameInfo.BallS = 0;
                            gameInfo.BallO = 0;
                            gameInfo.Bases = 0;
                        }
                        else
                        {
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
                                string data = this.DownReal[webID].Data;
                                int relayStart = data.IndexOf("sportscallback_relay");
                                // 判斷
                                if (relayStart != -1)
                                {
                                    int dataStart = data.IndexOf("document", relayStart);
                                    int dataEnd = data.LastIndexOf("}");
                                    // 沒有資料就離開
                                    if (dataStart == -1 || dataEnd == -1)
                                    {
                                        continue;
                                    }
                                    else
                                    {
                                        dataStart += 10;
                                        // 錯誤處理
                                        try
                                        {
                                            string strJson = data.Substring(dataStart, dataEnd - dataStart) + "}";
                                            JObject json = JObject.Parse(strJson);

                                            // 隊伍名稱
                                            gameInfo.Away = json["gameInfo"]["aFullName"].ToString();
                                            gameInfo.Home = json["gameInfo"]["hFullName"].ToString();
                                            gameInfo.Status = json["relayTexts"]["currentBatter"]["inn"].ToString();

                                            #region Away
                                            string nums = null;
                                            string[] num = null;
                                            // 分數
                                            if (json["scoreBoard"]["inn"]["away"] != null)
                                            {
                                                nums = json["scoreBoard"]["inn"]["away"].ToString();
                                                num = nums.Replace("\r\n", "").Replace("[", "").Replace("]", "").Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                                                // 判斷分數
                                                if (num != null)
                                                {
                                                    foreach (string board in num)
                                                    {
                                                        gameInfo.AwayBoard.Add(board.Trim());
                                                    }
                                                }
                                            }
                                            // 總分
                                            if (json["scoreBoard"]["rheb"]["away"]["r"] != null)
                                            {
                                                gameInfo.AwayPoint = json["scoreBoard"]["rheb"]["away"]["r"].ToString();
                                            }
                                            if (json["scoreBoard"]["rheb"]["away"]["h"] != null)
                                            {
                                                gameInfo.AwayH = json["scoreBoard"]["rheb"]["away"]["h"].ToString();
                                            }
                                            if (json["scoreBoard"]["rheb"]["away"]["e"] != null)
                                            {
                                                gameInfo.AwayE = json["scoreBoard"]["rheb"]["away"]["e"].ToString();
                                            }
                                            #endregion
                                            #region Home
                                            // 分數
                                            if (json["scoreBoard"]["inn"]["home"] != null)
                                            {
                                                nums = json["scoreBoard"]["inn"]["home"].ToString();
                                                num = num = nums.Replace("\r\n", "").Replace("[", "").Replace("]", "").Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                                                // 判斷分數
                                                if (num != null)
                                                {
                                                    foreach (string board in num)
                                                    {
                                                        gameInfo.HomeBoard.Add(board.Trim());
                                                    }
                                                }
                                            }
                                            // 總分
                                            if (json["scoreBoard"]["rheb"]["home"]["r"] != null)
                                            {
                                                gameInfo.HomePoint = json["scoreBoard"]["rheb"]["home"]["r"].ToString();
                                            }
                                            if (json["scoreBoard"]["rheb"]["home"]["h"] != null)
                                            {
                                                gameInfo.HomeH = json["scoreBoard"]["rheb"]["home"]["h"].ToString();
                                            }
                                            if (json["scoreBoard"]["rheb"]["home"]["e"] != null)
                                            {
                                                gameInfo.HomeE = json["scoreBoard"]["rheb"]["home"]["e"].ToString();
                                            }
                                            #endregion
                                            #region BSOB
                                            int bsob = 0;
                                            // Bases
                                            if (json["groundBaseAndBallCount"]["b1"]["name"].ToString().Length > 0) bsob += 1;
                                            if (json["groundBaseAndBallCount"]["b2"]["name"].ToString().Length > 0) bsob += 2;
                                            if (json["groundBaseAndBallCount"]["b3"]["name"].ToString().Length > 0) bsob += 4;
                                            gameInfo.Bases = bsob;
                                            // BSO
                                            if (int.TryParse(json["groundBaseAndBallCount"]["sbo"]["b"].ToString(), out bsob)) gameInfo.BallB = bsob;
                                            if (int.TryParse(json["groundBaseAndBallCount"]["sbo"]["s"].ToString(), out bsob)) gameInfo.BallS = bsob;
                                            if (int.TryParse(json["groundBaseAndBallCount"]["sbo"]["o"].ToString(), out bsob)) gameInfo.BallO = bsob;
                                            #endregion
                                            #region 比賽結束
                                            if (json["relayTexts"]["final"] != null &&
                                                json["relayTexts"]["final"].ToString() != "[]")
                                            {
                                                gameInfo.GameStates = "E";
                                                gameInfo.Status = "結束";
                                                gameInfo.BallB = 0;
                                                gameInfo.BallS = 0;
                                                gameInfo.BallO = 0;
                                                gameInfo.Bases = 0;
                                                // 判斷分數
                                                if (gameInfo.AwayBoard.Count > gameInfo.HomeBoard.Count)
                                                {
                                                    gameInfo.HomeBoard.Add("X");
                                                }
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
                                        catch { continue; }
                                    }
                                }
                            }
                            #endregion
                        }

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
        public override bool Update(string connectionString, BasicInfo info)
        {
            string status = info.WebID.Substring(0, 1);
            // 中止時，沒有跟盤 ID
            if (status == "P")
            {
                //return Update2(connectionString, info);
                // 改用多跟分來源
                return Update4(connectionString, info);
            }
            else
            {
                return base.Update(connectionString, info);
            }
        }
        // 下載資料
        private BasicDownload DownHome;
        private Dictionary<string, BasicDownload> DownReal;
        private DateTime DownLastTime = DateTime.Now;
    }
}
