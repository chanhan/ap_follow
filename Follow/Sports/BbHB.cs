using Follow.Sports.Basic;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Follow.Sports
{
    public class BbHB : Basic.BasicBaseball
    {
        private Dictionary<string, BasicDownload> DownReal;
        public BbHB(DateTime today)
            : base(ESport.Baseball_HB)
        {
            // 設定
            this.AllianceID = 119;
            this.GameType = "BBNL";
            this.GameDate = GetUtcTw(today).Date; // 只取日期

            this.DownReal = new Dictionary<string, BasicDownload>();

            if (string.IsNullOrWhiteSpace(this.sWebUrl))
            {
                this.sWebUrl = "http://www.knbsbstats.nl/{0}/HB/live{1}/xlive.htm";
            }

            //一天最多只會有4场
            this.DownReal["1"] = new BasicDownload(ESport.Baseball_HB, string.Format(sWebUrl, this.GameDate.ToString("yyyy"), "1"), Encoding.UTF8);
            this.DownReal["2"] = new BasicDownload(ESport.Baseball_HB, string.Format(sWebUrl, this.GameDate.ToString("yyyy"), "2"), Encoding.UTF8);
            this.DownReal["3"] = new BasicDownload(ESport.Baseball_HB, string.Format(sWebUrl, this.GameDate.ToString("yyyy"), "3"), Encoding.UTF8);
            this.DownReal["4"] = new BasicDownload(ESport.Baseball_HB, string.Format(sWebUrl, this.GameDate.ToString("yyyy"), "4"), Encoding.UTF8);
        }

        public override void Download()
        {
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
            int result = 0;
            DateTime GDate = DateTime.Now;
            BasicInfo gameInfo;
            string isTbody = string.Empty;
            HtmlNode temp = null;
            string StrDate = string.Empty, strDate1 = string.Empty, strTime = string.Empty;
            foreach (var item in DownReal)
            {
                //检查是否有资料
                if (string.IsNullOrWhiteSpace(item.Value.Data)) { continue; }
                //变量                
                HtmlDocument htmlDoc = new HtmlDocument();
                //加载资料
                htmlDoc.LoadHtml(item.Value.Data);
                if (htmlDoc == null) { continue; }

                //日期
                HtmlNode sponsor = htmlDoc.GetElementbyId("sponsor");
                if (sponsor == null || string.IsNullOrWhiteSpace(sponsor.InnerText)
                    || sponsor.InnerText.ToLower().IndexOf("date") == -1
                    || sponsor.InnerText.ToLower().IndexOf("start") == -1) { continue; }

                StrDate = strDate1 = strTime = "";
                StrDate = sponsor.InnerText.Trim().Replace("\r\n", "|").ToLower();
                StrDate = StrDate.Substring(StrDate.IndexOf("date"));
                strDate1 = StrDate.Substring(5, StrDate.IndexOf("|") - 5);
                StrDate = StrDate.Substring(StrDate.IndexOf("start"));
                strTime = StrDate.Substring(6, StrDate.IndexOf("|") - 6);
                if (!DateTime.TryParse(strDate1 + " " + strTime, out GDate))
                {
                    //预防上面截取的方法取不到时间 就用数组的方式再取一次
                    string[] date = sponsor.InnerText.Trim().Replace("\r\n", "|").Split('|');
                    if (date.Length < 14 || date[2].Length < 6 || date[13].Length < 7) { continue; }
                    if (!DateTime.TryParse(date[2].Substring(5) + date[13].Substring(6), out GDate)) { continue; }
                }
                GDate = GDate.AddHours(6);//荷兰时间转换为我们的时间
                // 建立比賽資料
                gameInfo = null;
                gameInfo = new BasicInfo(this.AllianceID, this.GameType, GDate, GDate.ToString("yyyyMMdd") + item.Key);
                gameInfo.IsBall = true;

                HtmlNode board = htmlDoc.GetElementbyId("board");
                if (board == null) { continue; }
                isTbody = string.Empty;
                if (board.InnerHtml.IndexOf("tbody") != -1)
                {
                    isTbody = "/tbody[1]";
                }
                temp = null;
                temp = board.SelectSingleNode("./table[1]" + isTbody + "/tr[1]/td[3]");
                if (temp == null) { continue; }
                gameInfo.Away = temp.InnerText.Trim();

                temp = null;
                temp = board.SelectSingleNode("./table[1]" + isTbody + "/tr[1]/td[7]");
                if (temp == null) { continue; }
                gameInfo.Home = temp.InnerText.Trim();

                //状态
                GetGameStatus(board.SelectNodes("./table[1]" + isTbody + "/tr[2]/td"), gameInfo);

                #region 分数
                // 開賽/結束/中止才取比分
                if ("S".Equals(gameInfo.GameStates) || "E".Equals(gameInfo.GameStates) || "P".Equals(gameInfo.GameStates))
                {
                    int beginInning = 0;
                    string R, H, E = string.Empty;

                    HtmlNode Inning = board.SelectSingleNode("./table[1]" + isTbody + "/tr[1]/td[10]/table[1]" + isTbody + "/tr[1]/td[2]");
                    if (Inning == null) { continue; }
                    Int32.TryParse(Inning.InnerText.Trim().Replace("&nbsp;", ""), out beginInning);
                    if (beginInning > 1)//資料不完整，缺乏前面局數比分
                    {
                        GetGameScore(gameInfo, beginInning - 1, true);//取得遺漏比分
                    }
                    //Away
                    GetTeamScore(board.SelectNodes("./table[1]" + isTbody + "/tr[1]/td[10]/table[1]" + isTbody + "/tr[2]/td"), gameInfo.AwayBoard, out R, out H, out E);
                    gameInfo.AwayE = E;
                    gameInfo.AwayH = H;
                    gameInfo.AwayPoint = R;

                    //home
                    GetTeamScore(board.SelectNodes("./table[1]" + isTbody + "/tr[1]/td[10]/table[1]" + isTbody + "/tr[3]/td"), gameInfo.HomeBoard, out R, out H, out E);
                    gameInfo.HomeE = E;
                    gameInfo.HomeH = H;
                    gameInfo.HomePoint = R;

                    // 判斷分數 是否需要补X
                    if ("E".Equals(gameInfo.GameStates) && gameInfo.AwayBoard.Count > gameInfo.HomeBoard.Count)
                    {
                        gameInfo.HomeBoard.Add("X");
                    }
                    else
                    {
                        //以状态为准，补比分0
                        AddInningScore(gameInfo.Status, gameInfo);
                    }
                }
                #endregion 分数
                #region BSOB
                if ("S".Equals(gameInfo.GameStates))//開賽中取得BSOB
                {
                    int b, s, o = 0;
                    HtmlNode situation = htmlDoc.GetElementbyId("situation");
                    isTbody = string.Empty;
                    if (situation != null && situation.InnerHtml.IndexOf("tbody") != -1)
                    {
                        isTbody = "/tbody[1]";
                    }
                    if (situation != null &&
                        situation.SelectSingleNode("./table[1]" + isTbody + "/tr[5]/td[2]") != null &&
                        situation.SelectSingleNode("./table[1]" + isTbody + "/tr[4]/td[2]") != null &&
                        situation.SelectSingleNode("./table[1]" + isTbody + "/tr[8]/td[2]") != null &&
                        situation.SelectSingleNode("./table[1]" + isTbody + "/tr[7]/td[2]") != null &&
                        situation.SelectSingleNode("./table[1]" + isTbody + "/tr[6]/td[2]") != null
                        )
                    {

                        string BS = situation.SelectSingleNode("./table[1]" + isTbody + "/tr[4]/td[2]").InnerText.Replace("&nbsp;", "").Trim();
                        if (!string.IsNullOrWhiteSpace(BS) && BS.Trim() != "-")
                        {
                            int.TryParse(BS.Split('-')[0], out b);
                            int.TryParse(BS.Split('-')[1], out s);
                            gameInfo.BallB = b;
                            gameInfo.BallS = s;
                        }
                        int.TryParse(situation.SelectSingleNode("./table[1]" + isTbody + "/tr[5]/td[2]").InnerText.Replace("&nbsp;", "").Trim(), out o);
                        gameInfo.BallO = o;

                        //876
                        string bases = string.Concat(situation.SelectSingleNode("./table[1]" + isTbody + "/tr[8]/td[2]").InnerText.Replace("&nbsp;", "").Trim() != "-" ? "1" : "0",
                            situation.SelectSingleNode("./table[1]" + isTbody + "/tr[7]/td[2]").InnerText.Replace("&nbsp;", "").Trim() != "-" ? "1" : "0",
                            situation.SelectSingleNode("./table[1]" + isTbody + "/tr[6]/td[2]").InnerText.Replace("&nbsp;", "").Trim() != "-" ? "1" : "0");
                        gameInfo.Bases = getBases(bases);
                    }

                }

                #endregion BSOB

                // 加入
                this.GameData[gameInfo.WebID] = gameInfo;
                // 累計
                result++;
            }
            return result;
        }

        public override bool Update(string connectionString, BasicInfo info)
        {
            return base.Update(connectionString, info);

        }
        private void GetGameStatus(HtmlNodeCollection Collection, BasicInfo game)
        {
            if (Collection == null || Collection.Count < 4)
            {
                return;
            }
            string status = Collection[3].InnerText.Trim().ToLower();
            if (status.Contains("final"))
            {
                game.GameStates = "E";
                game.Status = "結束";
                game.BallB = 0;
                game.BallS = 0;
                game.BallO = 0;
                game.Bases = 0;
            }
            else if (status.Contains("cancel"))
            {
                game.GameStates = "P";
                game.Status = "中止";
                game.TrackerText = "因雨延賽";
                game.BallB = 0;
                game.BallS = 0;
                game.BallO = 0;
                game.Bases = 0;
            }
            else
            {
                game.GameStates = "S";
                game.Status = string.Format("{0}局 {1}", status, Collection[0].InnerText.Replace("&nbsp;", "").Trim() != "" ? "上" : "下");
            }
        }

        private void GetTeamScore(HtmlNodeCollection tdColl, List<string> scoreboard, out string R, out string H, out string E)
        {
            R = H = E = string.Empty;
            if (tdColl == null) return;
            //去掉没用的
            tdColl.RemoveAt(0);
            foreach (HtmlNode td in tdColl)
            {
                if (string.IsNullOrWhiteSpace(td.InnerText.Trim()) || td.InnerText.Trim() == "-" || td.InnerText.Trim() == "&nbsp;")
                {
                    continue;
                }
                scoreboard.Add(td.InnerText.Replace("&nbsp;", "").Trim());
            }
            if (scoreboard.Count > 0)
            {
                E = scoreboard[scoreboard.Count - 1];
                scoreboard.RemoveAt(scoreboard.Count - 1);
                H = scoreboard[scoreboard.Count - 1];
                scoreboard.RemoveAt(scoreboard.Count - 1);
                R = scoreboard[scoreboard.Count - 1];
                scoreboard.RemoveAt(scoreboard.Count - 1);
            }
        }
        private void AddInningScore(string status, BasicInfo gameInfo)
        {
            //開賽中且狀態文字包含"局"
            if ("S".Equals(gameInfo.GameStates) && status.Contains("局"))
            {
                int inning;
                // 2局 上
                string[] tmp = status.Replace("半", "").Split(new string[] { "局", " " }, StringSplitOptions.RemoveEmptyEntries);
                if (Int32.TryParse(tmp[0], out inning))
                {
                    // 上半場: 取客隊比分, 下半場: 取主隊比分
                    List<string> scoreboard = ("上".Equals(tmp[1])) ? gameInfo.AwayBoard : gameInfo.HomeBoard;
                    int quarter = scoreboard.Count;
                    if (quarter < inning)
                    {
                        // 局數差
                        int diff = inning - quarter;
                        // 補差額局數分數
                        for (int i = 0; i < diff; i++)
                        {
                            scoreboard.Add("0");
                        }
                    }
                }
            }
        }
    }
}
