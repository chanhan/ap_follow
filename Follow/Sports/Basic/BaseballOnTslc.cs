using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Follow.Sports.Basic
{
    public partial class BasicBaseball
    {
        #region 取得尬球乐-棒球資料
        public List<string> xpath;
        /// <summary>
        /// 取得尬球乐棒球資料
        /// </summary>
        /// <param name="html">網頁內容</param>
        /// <returns>賽事資料集合</returns>
        protected Dictionary<string, BasicInfo> GetDataByTSLC(string html)
        {
            try
            {
                if (String.IsNullOrEmpty(html) || !html.Contains("html")) { return null; }

                Dictionary<string, BasicInfo> gameData = new Dictionary<string, BasicInfo>();
                HtmlDocument document = new HtmlDocument();
                // 載入資料
                document.LoadHtml(System.Web.HttpUtility.HtmlDecode(html));
                // 取得賽事資料位置
                HtmlNodeCollection gameNodes = document.DocumentNode.SelectNodes("//table[contains(@class, 'shsTable')]");

                foreach (HtmlNode node in gameNodes)
                {
                    HtmlNodeCollection teams = node.SelectNodes(".//td[@class='shsNamD']");
                    //开赛之后Count==2,未开赛前 Count==3
                    if (teams != null && teams.Count != 2)
                    {
                        //检查队伍  
                        continue;
                    }
                    //string teamAway = System.Web.HttpUtility.HtmlDecode(teams[0].InnerText);
                    //string teamHome = System.Web.HttpUtility.HtmlDecode(teams[1].InnerText);
                    string teamAway = teams[0].InnerText;
                    string teamHome = teams[1].InnerText;
                    //直接用開賽日期
                    BasicInfo gameInfo = new BasicInfo(this.AllianceID, this.GameType, this.GameDate, Helper.StringHelper.GetMd5Str(teamAway + teamHome + this.GameDate))
                    {
                        Away = teamAway,
                        Home = teamHome,
                        IsBall = true
                    };

                    // 比賽狀態 
                    HtmlNode statusNode = node.SelectSingleNode(".//tr[@class='shsTableTtlRow']/td[1]");
                    string status = String.Empty;

                    // 取得狀態欄資料
                    if (statusNode != null && String.IsNullOrEmpty(status))
                    {
                        status = System.Web.HttpUtility.HtmlDecode(statusNode.InnerText.Trim());
                    }

                    // 沒有比賽狀態, 不處理
                    if (String.IsNullOrEmpty(status)) { continue; }

                    // 設定開賽狀態
                    this.GetGameStatusTSLC(status, gameInfo);

                    #region BSOB

                    if ("S".Equals(gameInfo.GameStates))//開賽中取得BSOB
                    {
                        HtmlNode shsGameDetails = node.SelectSingleNode(".//td[@class='shsGameDetails']");
                        if (shsGameDetails != null)
                        {
                            //一定要带有src的img
                            HtmlNodeCollection imgAll = shsGameDetails.SelectNodes(".//img[@src!='']");
                            if (imgAll != null && imgAll.Count >= 4)
                            {
                                GetBasesTSLC(imgAll[0], gameInfo);//第一个图片垒包
                                GetBSOTSLC(imgAll[1], "B", gameInfo);//坏球
                                GetBSOTSLC(imgAll[2], "S", gameInfo);//好球
                                GetBSOTSLC(imgAll[3], "O", gameInfo);//出局
                            }
                        }
                    }

                    #endregion BSOB

                    #region 比分

                    // 開賽/結束/中止才取比分
                    if ("S".Equals(gameInfo.GameStates) || "E".Equals(gameInfo.GameStates) || "P".Equals(gameInfo.GameStates))
                    {
                        HtmlNodeCollection trColl = node.SelectNodes(".//tr[@class='shsRow0Row']");
                        if (trColl != null && trColl.Count >= 2)
                        {
                            string R, H, E, sInning = string.Empty;
                            int beginInning = 0;
                            HtmlNode Inning = node.SelectSingleNode(".//tr[@class='shsTableTtlRow']/td[2]");
                            if (Inning != null)
                            {
                                sInning = Inning.InnerText;
                            }
                            Int32.TryParse(sInning, out beginInning);
                            if (beginInning > 1)//資料不完整，缺乏前面局數比分
                            {
                                GetGameScore(gameInfo, beginInning - 1, true);//取得遺漏比分
                            }

                            // 第一個 tr: 客隊比分
                            var tdColl = trColl[0].SelectNodes("td");
                            GetTeamScoreTSLC(tdColl, gameInfo.AwayBoard, out R, out H, out E);
                            gameInfo.AwayPoint = R;
                            gameInfo.AwayH = H;
                            gameInfo.AwayE = E;

                            // 第二個 tr: 主隊比分
                            tdColl = trColl[1].SelectNodes("td");
                            GetTeamScoreTSLC(tdColl, gameInfo.HomeBoard, out R, out H, out E);
                            gameInfo.HomePoint = R;
                            gameInfo.HomeH = H;
                            gameInfo.HomeE = E;
                        }

                        // 根據狀態判斷是否要補分
                        AddInningScoreTSLC(status, gameInfo);
                    }
                    #endregion 比分

                    gameData[gameInfo.WebID] = gameInfo;
                }

                return gameData;
            }
            catch { throw; }
        }

        #region 取得比分

        /// <summary>
        /// 取得比分
        /// </summary>
        /// <param name="tdColl">比分資料節點集合</param>
        /// <param name="scoreboard">賽事比分</param>
        /// <param name="R">R分</param>
        /// <param name="H">H分</param>
        /// <param name="E">E分</param>
        private void GetTeamScoreTSLC(HtmlNodeCollection tdColl, List<string> scoreboard, out string R, out string H, out string E)
        {
            R = String.Empty;
            H = String.Empty;
            E = String.Empty;
            //去掉没用的
            tdColl.RemoveAt(0);
            foreach (HtmlNode td in tdColl)
            {
                if (string.IsNullOrWhiteSpace(td.InnerText.Trim()))
                {
                    continue;
                }
                scoreboard.Add(td.InnerText.Trim());
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

        /// <summary>
        /// 根據狀態判斷是否要補分
        /// </summary>
        /// <param name="status">狀態</param>
        /// <param name="gameInfo">賽事資訊</param>
        private void AddInningScoreTSLC(string status, BasicInfo gameInfo)
        {
            //開賽中且狀態文字包含"局"
            if ("S".Equals(gameInfo.GameStates) && status.Contains("局"))
            {
                int inning;
                // 因有 "下半局 5局" 的狀態,Replace=>下局 5局   Split==> [下] [5]
                string[] tmp = status.Replace("半", "").Split(new string[] { "局", " " }, StringSplitOptions.RemoveEmptyEntries);
                if (Int32.TryParse(tmp[1], out inning))
                {
                    // 上半場: 取客隊比分, 下半場: 取主隊比分
                    List<string> scoreboard = ("上".Equals(tmp[0])) ? gameInfo.AwayBoard : gameInfo.HomeBoard;
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

        #endregion 取得比分

        /// <summary>
        /// 取得 BSO 資訊
        /// </summary>
        /// <param name="div">壘包資料節點</param>
        /// <param name="gameInfo">賽事資訊</param>
        private void GetBSOTSLC(HtmlNode Img, string sType, BasicInfo gameInfo)
        {
            // BSO
            int iNumber = 0;
            string src = Img.GetAttributeValue("src", String.Empty);
            int.TryParse(src.Substring(src.LastIndexOf('/') + 1, 1), out iNumber);
            switch (sType)
            {
                case "B":
                    gameInfo.BallB = iNumber;
                    break;
                case "S":
                    gameInfo.BallS = iNumber;
                    break;
                case "O":
                    gameInfo.BallO = iNumber;
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// 取得壘包資訊
        /// </summary>
        /// <param name="div">壘包資料節點</param>
        /// <param name="gameInfo">賽事資訊</param>
        private void GetBasesTSLC(HtmlNode img, BasicInfo gameInfo)
        {
            // 壘包
            string src = img.GetAttributeValue("src", String.Empty);
            //来源网 3垒是 001  正常我们这边逻辑是 3垒应该是100
            //需要反转字符串
            gameInfo.Bases = getBases(string.Concat<char>(src.Substring(src.IndexOf('.') - 3, 3).ToCharArray().Reverse<char>()));
        }

        // 取得比賽狀態
        private void GetGameStatusTSLC(string status, BasicInfo game)
        {
            status = status.ToLower();

            if (status.Contains("比賽時間"))
            {
                // 未開賽
                game.GameStates = "X";
                game.Status = String.Empty;
                game.TrackerText = String.Empty;
                game.BallB = 0;
                game.BallS = 0;
                game.BallO = 0;
                game.Bases = 0;
            }
            else if (status.Contains("比賽終了"))
            {
                game.GameStates = "E";
                game.Status = "結束";
                game.BallB = 0;
                game.BallS = 0;
                game.BallO = 0;
                game.Bases = 0;
            }
            else if (status.Contains("延賽"))
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
                game.Status = status;
            }
        }


        /// <summary>
        /// 转成存储格式
        /// 001=1垒 | 011=1,2垒 | 111=123全垒打 | 101=1,3垒 | 110=2,3垒
        /// </summary>
        /// <param name="sBases">001,011,111,101,110</param>
        /// <returns></returns>
        public int getBases(string sBases)
        {
            //001 = 1壘有人, 011 = 12壘有人,100= 3垒有人
            int sum = 0;
            for (int i = 0; i < 3; i++)
            {
                //2進制轉換
                int num = int.Parse(sBases.Substring(i, 1));

                sum += (num * (int)Math.Pow(2, 2 - i));
            }

            return sum;
        }
        /// <summary>
        /// 转成存储格式(数组格式下标顺序从左到右)
        /// 001=1垒 | 011=1,2垒 | 111=123全垒打 | 101=1,3垒 | 110=2,3垒
        /// </summary>
        /// <param name="sBases"></param>
        /// <returns></returns>
        public int getBases2(string[] sBases)
        {
            int b = 0;
            // Bases
            if (int.Parse(sBases[0]) > 0) b = 1;
            if (int.Parse(sBases[1]) > 0) b ^= 2;
            if (int.Parse(sBases[2]) > 0) b ^= 4;
            return b;
        }
        #endregion
    }
}
