using Follow.Sports.Basic;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Data;
using System.Data.SqlClient;
using System.IO; 

// 跟盤以 ID 為依據
// 跟盤日期以美國時間顯示

namespace Follow.Sports
{
    public class BkWNBA : Basic.BasicBasketball
    {
        private new LogFile Logs;
        private string sWebUrl1 = UrlSetting.GetUrl(ESport.Basketball_WNBA, "Url1");
        public BkWNBA(DateTime today) : base(ESport.Basketball_WNBA)
        {
            this.Logs = new LogFile(ESport.Basketball_WNBA);//設定log type
            if (string.IsNullOrWhiteSpace(this.sWebUrl))
            {
                this.sWebUrl = @"http://scores.espn.go.com/wnba/scoreboard";
            }
            if (string.IsNullOrWhiteSpace(this.sWebUrl1))
            {
                this.sWebUrl1 = @"http://espn.go.com/wnba/boxscore?gameId={0}";
            }
            // 設定
            this.AllianceID = 19;
            this.GameType = "BKUSW";
            int diffTime = frmMain.GetGameSourceTime("EasternTime");//取得與當地時間差(包含日光節約時間)
            if (diffTime > 0)
                this.GameDate = today.AddHours(-diffTime);
            else
                this.GameDate = GetUtcUsaEt(today);//取得美東時間

            this.DownHome = new BasicDownload(this.Sport, this.sWebUrl); // 以網站的資料頁面為主
            this.DownScore = new Dictionary<string, BasicDownload>();
        }
        public override void Download()
        {
            // 沒有資料或下載時間超過 10 秒鐘才讀取首頁資料。
            if (this.GameData.Count == 0 ||
                DateTime.Now >= this.DownLastTime.AddSeconds(5))
            {
                this.DownHome.DownloadString();
                this.DownLastTime = DateTime.Now;
            }
            
            foreach (KeyValuePair<string, BasicDownload> game in this.DownScore)
            {
                // 沒有資料或下載時間超過 5秒重讀取資料。
                if (game.Value.LastTime == null ||
                    DateTime.Now >= game.Value.LastTime.Value.AddSeconds(5))
                {
                    game.Value.DownloadString();
                }
            }
        }
        public override int Follow()
        {
            // 沒有資料就離開
            if (string.IsNullOrEmpty(this.DownHome.Data)) return 0;

            int result = 0;
            HtmlAgilityPack.HtmlDocument document = new HtmlAgilityPack.HtmlDocument();
            Dictionary<string, HtmlAgilityPack.HtmlNode> games = new Dictionary<string, HtmlAgilityPack.HtmlNode>();
            // 載入資料
            document.LoadHtml(this.DownHome.Data);

            // 找到日期
            string gameDateStr = document.DocumentNode.SelectSingleNode("//*[@id=\"content\"]/div[2]/div/div/div[1]/h2").InnerText.Replace("Scores for", "").Trim();
            DateTime gameDate = DateTime.Now;
            // 轉換日期
            if (!DateTime.TryParse(gameDateStr, out gameDate)) return 0;
            // 找到資料
            games["gamesLeft"] = document.DocumentNode.SelectSingleNode("//*[@id=\"gamesLeft\"]");
            games["gamesRight"] = document.DocumentNode.SelectSingleNode("//*[@id=\"gamesRight\"]");
            // 處理資料
            foreach (KeyValuePair<string, HtmlAgilityPack.HtmlNode> data in games)
            {
                foreach (HtmlAgilityPack.HtmlNode game in data.Value.ChildNodes)
                {
                    // 不是資料就往下處理
                    if (game.Name != "div") continue;
                    if (game.Id.IndexOf("-gameHeader") == -1) continue;

                    string webID = game.Id.Replace("-gameHeader", "");
                    if (!this.DownScore.ContainsKey(webID))//沒有完整分數資料，則加入下載
                    {
                        string url = string.Format(this.sWebUrl1, webID);
                        this.DownScore.Add(webID, new BasicDownload(this.Sport, url, webID));
                        this.DownScore[webID].DownloadString();
                    }

                    try
                    {
                        BasicInfo gameInfo = ProcessGameData(game, gameDate, webID);
                        // 加入
                        this.GameData[gameInfo.WebID] = gameInfo;
                        // 累計
                        result++;
                    }
                    catch(Exception ex)
                    {
                        StringBuilder sb = new StringBuilder();
                        sb.AppendLine("ProcessGameData Error!!");
                        sb.AppendLine("Message: " + ex.Message);
                        sb.AppendLine("StackTrace:");
                        sb.AppendLine(ex.StackTrace);
                        sb.AppendLine("webID: " + webID);
                        sb.AppendLine("gameId: " + game.Id);

                        this.Logs.Error(sb.ToString());
                    }
                }
            }
            // 傳回
            return result;
        }

        private BasicInfo ProcessGameData(HtmlAgilityPack.HtmlNode game, DateTime gameDate, string webID)
        {
            // 建立比賽資料，時間以台灣為主
            BasicInfo gameInfo = new BasicInfo(this.AllianceID, this.GameType, gameDate, webID);
            gameInfo.Away = game.SelectSingleNode("div/div[@class='team visitor']/div[@class='team-capsule']/p[@class='team-name']/span/a").InnerText;
            gameInfo.Home = game.SelectSingleNode("div/div[@class='team home']/div[@class='team-capsule']/p[@class='team-name']/span/a").InnerText;
            gameInfo.Status = game.SelectSingleNode("div/div[@class='game-header']/div/p").InnerText;
            gameInfo.Status = gameInfo.Status.ToLower();
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
            gameInfo.Status = gameInfo.Status.Replace("&nbsp;", "");//移除空白字元
            gameInfo.Status = gameInfo.Status.Trim();

            if (gameInfo.Status == "00:00") gameInfo.Status = "結束";

            //目前有顯示的分數欄位

            HtmlAgilityPack.HtmlNode scoreHeader = game.SelectSingleNode("div/div[@class='game-header']/ul[@class='score header']");
            if (scoreHeader != null)//是否已經有比分
            {
                string linescoreHeader = game.SelectSingleNode("div/div[@class='game-header']/ul[@class='score header']").InnerText;
                if (linescoreHeader.Length > 7)//資料不完整，缺乏前面局數比分     2 3 4 OT OT T
                {
                    //第1個數字才是有顯示的比分
                    int lossInning = 1;
                    int.TryParse(linescoreHeader.Substring(0, 1), out lossInning);
                    if (lossInning > 1)
                    {
                        lossInning--;
                        GetGameScore(gameInfo, lossInning);//無暫存取得遺漏比分                            
                    }
                }

                List<int> FullAwayBoard = new List<int>();//完整比分
                List<int> FullHomeBoard = new List<int>();//完整比分
                if (this.DownScore.ContainsKey(webID) && !string.IsNullOrEmpty(this.DownScore[webID].Data))
                {
                    HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
                    doc.LoadHtml(this.DownScore[webID].Data);

                    HtmlAgilityPack.HtmlNodeCollection HomeAway = doc.GetElementbyId("gamepackageTop").SelectNodes("div[@class='line-score clear']/div[@class='line-score-container']/table/tr");
                    if (HomeAway != null && HomeAway.Count == 3)
                    {
                        foreach (HtmlAgilityPack.HtmlNode scores in HomeAway)
                        {
                            if (scores.Attributes["class"] != null)//忽略periods
                                continue;

                            HtmlAgilityPack.HtmlNodeCollection score = scores.SelectNodes("td");
                            if (score != null)
                            {
                                bool isAway = false;
                                if (FullAwayBoard.Count == 0)
                                    isAway = true;

                                int period = score.Count - 1;
                                for (int i = 1; i < period; i++)//第0個值為隊名
                                {
                                    string tmpNum = score[i].InnerHtml.Replace("\n", "").Replace("\t", "").Trim();
                                    int tryInt = 0;
                                    int.TryParse(tmpNum, out tryInt);

                                    if (isAway)
                                        FullAwayBoard.Add(tryInt);
                                    else
                                        FullHomeBoard.Add(tryInt);
                                }
                            }
                        }
                    }
                }

                //首頁比分計算
                foreach (HtmlAgilityPack.HtmlNode numDoc in game.SelectNodes("div/div[@class='team visitor']/ul[@class='score']/li"))
                {
                    int num = 0;
                    if (int.TryParse(numDoc.InnerText, out num))
                    {
                        if (gameInfo.AwayBoard.Count < 4)//沒有OT
                            gameInfo.AwayBoard.Add(numDoc.InnerText);
                        else
                        {
                            int otDiff = FullAwayBoard.Count - gameInfo.AwayBoard.Count;//OT數差別
                            if (FullAwayBoard.Count == 0 || otDiff == 0)
                                gameInfo.AwayBoard.Add(numDoc.InnerText);
                            else
                            {
                                int sum = 0;
                                for (int i = 0; i < otDiff - 1; i++)
                                {
                                    sum += FullAwayBoard[4 + i];//最後一個OT不累加
                                    gameInfo.AwayBoard.Add(FullAwayBoard[4 + i].ToString());//OT1 OT2 OT3 ...
                                }

                                gameInfo.AwayBoard.Add((num - sum).ToString());//剩下的OT分數
                            }
                        }
                    }
                }

                foreach (HtmlAgilityPack.HtmlNode numDoc in game.SelectNodes("div/div[@class='team home']/ul[@class='score']/li"))
                {
                    int num = 0;
                    if (int.TryParse(numDoc.InnerText, out num))
                    {
                        if (gameInfo.HomeBoard.Count < 4)//沒有OT
                            gameInfo.HomeBoard.Add(numDoc.InnerText);
                        else
                        {
                            int otDiff = FullHomeBoard.Count - gameInfo.HomeBoard.Count;//OT數差別
                            if (FullHomeBoard.Count == 0 || otDiff == 0)//尚未取得完整比分資料 或者是最後一個比分則為總分
                                gameInfo.HomeBoard.Add(numDoc.InnerText);
                            else
                            {
                                int sum = 0;
                                for (int i = 0; i < otDiff - 1; i++)
                                {
                                    sum += FullHomeBoard[4 + i];//最後一個OT不累加
                                    gameInfo.HomeBoard.Add(FullHomeBoard[4 + i].ToString());//OT1 OT2 OT3 ...
                                }

                                gameInfo.HomeBoard.Add((num - sum).ToString());//剩下的OT分數
                            }
                        }
                    }
                }
                // 總分
                if (gameInfo.AwayBoard.Count > 0)
                {
                    gameInfo.AwayPoint = gameInfo.AwayBoard[gameInfo.AwayBoard.Count - 1];
                    gameInfo.AwayBoard.RemoveAt(gameInfo.AwayBoard.Count - 1);
                    gameInfo.HomePoint = gameInfo.HomeBoard[gameInfo.HomeBoard.Count - 1];
                    gameInfo.HomeBoard.RemoveAt(gameInfo.HomeBoard.Count - 1);
                }
            }

            if (gameInfo.Status.IndexOf("final") != -1)
            {
                gameInfo.GameStates = "E";
            }
            else if (gameInfo.Quarter > 0)
            {
                gameInfo.GameStates = "S";
            }

            return gameInfo;
        }

        private void GetGameScore(BasicInfo game, int inning)//取得遺漏比分
        {
            List<string> AwayBoard = new List<string>();
            List<string> HomeBoard = new List<string>();
            if (this.GameData.ContainsKey(game.WebID))
            {
                AwayBoard = this.GameData[game.WebID].AwayBoard;
                HomeBoard = this.GameData[game.WebID].HomeBoard;
            }
            else
            {
                DataTable dt = GetGameDBInfo(game.WebID);//從資料庫取得舊資料
                if (dt != null && dt.Rows.Count == 1)
                {
                    if (dt.Rows[0]["RunsA"] != null && dt.Rows[0]["RunsB"] != null)
                    {
                        string RunsA = dt.Rows[0]["RunsA"].ToString();
                        AwayBoard = new List<string>(RunsA.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries));

                        string RunsB = dt.Rows[0]["RunsB"].ToString();
                        HomeBoard = new List<string>(RunsB.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries));
                    }
                }
            }

            if (AwayBoard.Count > 0 && HomeBoard.Count > 0)
            {
                for (int i = 0; i < inning; i++)
                {
                    string oldscore = AwayBoard[i];
                    if (!string.IsNullOrEmpty(oldscore))
                        game.AwayBoard.Add(oldscore);

                    oldscore = HomeBoard[i];
                    if (!string.IsNullOrEmpty(oldscore))
                        game.HomeBoard.Add(oldscore);
                }
            }
        }

        private DataTable GetGameDBInfo(string WebID)//從資料庫取得舊資料
        {
            string sSqlCommand = "SELECT [GID],[GameType],[GameStates],[GameDate],[GameTime],[TeamAID],[TeamBID],[RunsA],[RunsB]" + "\r\n"
                               + "FROM [dbo].[BasketballSchedules] WITH (NOLOCK)" + "\r\n"
                               + "WHERE ([GameType] = @GameType)" + "\r\n"
                               + "  AND ([CtrlStates] = 2)" + "\r\n"
                               + "  AND ([IsDeleted] = 0)" + "\r\n"
                               + "  AND ([WebID] = @WebID)";

            using (SqlConnection con = new SqlConnection(frmMain.ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand(sSqlCommand, con))
                {
                    cmd.Parameters.Add(new SqlParameter("@GameType", this.GameType));
                    cmd.Parameters.Add(new SqlParameter("@WebID", WebID));

                    // 讀取資料
                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {

                        DataTable dt = new DataTable();
                        da.Fill(dt);

                        dt.PrimaryKey = new DataColumn[] { dt.Columns["GID"] };

                        return dt;
                    }
                }
            }
        }

        // 下載資料
        private Dictionary<string, BasicDownload> DownScore;
        private BasicDownload DownHome;
        private DateTime DownLastTime = DateTime.Now;
    }
}
