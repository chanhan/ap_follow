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
        #region 取得玩運彩-棒球資料

        /// <summary>
        /// 取得玩運彩棒球資料
        /// </summary>
        /// <param name="html">網頁內容</param>
        /// <returns>賽事資料集合</returns>
        protected Dictionary<string, BasicInfo> GetDataByPlaySport(string html)
        {
            try
            {
                if (String.IsNullOrEmpty(html) || !html.Contains("html")) { return null; }
                
                Dictionary<string, BasicInfo> gameData = new Dictionary<string, BasicInfo>();
                HtmlDocument document = new HtmlDocument();
                // 載入資料
                document.LoadHtml(html);
                // 取得賽事資料位置
                HtmlNodeCollection gameNodes = document.DocumentNode.SelectNodes("//div[contains(@id, 'gamebox')]");

                foreach (HtmlNode node in gameNodes)
                {
                    string webId = node.Id.Split('-')[1];
                    string teamAway = node.GetAttributeValue("data-namea", String.Empty).Trim();
                    string teamHome = node.GetAttributeValue("data-nameh", String.Empty).Trim();

                    // 玩運彩取不到[開賽時間], 直接用開賽日期
                    BasicInfo gameInfo = new BasicInfo(this.AllianceID, this.GameType, this.GameDate, webId)
                    {
                        Away = teamAway,
                        Home = teamHome,
                        IsBall = true
                    };

                    // 比賽狀態 & BSO
                    HtmlNode statusNode = node.SelectSingleNode(String.Format("//td[@id='{0}_addinfo']/div[1]", webId)); // 大字狀態
                    HtmlNode inningNode = node.SelectSingleNode(String.Format("//td[@id='{0}_inning']", webId));  // 局數(僅開賽時候顯示)

                    string status = String.Empty;
                    if (inningNode != null)
                    {
                        // 取局數資料
                        status = inningNode.InnerText.Trim();
                    }

                    // 取得狀態欄資料
                    if (statusNode != null && String.IsNullOrEmpty(status))
                    {
                        status = statusNode.InnerText.Trim();
                    }

                    // 沒有比賽狀態, 不處理
                    if (String.IsNullOrEmpty(status)) { continue; }
                    // 設定開賽狀態
                    GetGameStatus(status, gameInfo);

                    #region BSOB

                    if ("S".Equals(gameInfo.GameStates))//開賽中取得BSOB
                    {
                        var divColl = statusNode.Descendants("div");
                        foreach (HtmlNode div in divColl)
                        {
                            string className = div.GetAttributeValue("class", String.Empty);
                            if (String.IsNullOrEmpty(className)) { continue; }

                            if (className.Contains("sb sb"))
                            {
                                // 取得壘包資料
                                GetBases(div, gameInfo);
                                continue;
                            }

                            if (className.Contains("str_sbo"))
                            {
                                // 取得 BSO 資訊
                                GetBSO(div, gameInfo);
                                continue;
                            }
                        }
                    }

                    #endregion BSOB

                    #region 比分

                    // 開賽/結束/中止才取比分
                    if ("S".Equals(gameInfo.GameStates) || "E".Equals(gameInfo.GameStates) || "P".Equals(gameInfo.GameStates))
                    {
                        HtmlNodeCollection trColl = node.SelectNodes("*/table[@class='scorebox']/tr");
                        if (trColl.Count == 3)
                        {
                            int beginInning = 0;
                            string R, H, E;

                            // 第一個 tr: 局數
                            var tdColl = trColl[0].SelectNodes("td");
                            Int32.TryParse(tdColl[1].InnerText.Trim(), out beginInning);
                            if (beginInning > 1)//資料不完整，缺乏前面局數比分
                            {
                                GetGameScore(gameInfo, beginInning - 1, true);//取得遺漏比分
                            }

                            // 第二個 tr: 客隊比分
                            tdColl = trColl[1].SelectNodes("td");
                            GetTeamScore(tdColl, gameInfo.AwayBoard, out R, out H, out E);
                            gameInfo.AwayPoint = R;
                            gameInfo.AwayH = H;
                            gameInfo.AwayE = E;

                            // 第三個 tr: 主隊比分
                            tdColl = trColl[2].SelectNodes("td");
                            GetTeamScore(tdColl, gameInfo.HomeBoard, out R, out H, out E);
                            gameInfo.HomePoint = R;
                            gameInfo.HomeH = H;
                            gameInfo.HomeE = E;
                        }

                        // 根據狀態判斷是否要補分
                        AddInningScore(status, gameInfo);
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
        private void GetTeamScore(HtmlNodeCollection tdColl, List<string> scoreboard, out string R, out string H, out string E)
        {
            R = String.Empty;
            H = String.Empty;
            E = String.Empty;

            foreach (HtmlNode td in tdColl)
            {
                string id = td.Id.ToLower().Trim();
                if (String.IsNullOrEmpty(id)) { continue; }

                string inning = id.Substring(id.Length - 1);
                string value = td.InnerText.Trim();
                switch (inning)
                {
                    case "r":
                        R = value;
                        break;

                    case "e":
                        E = value;
                        break;

                    case "h":
                        H = value;
                        break;

                    default:
                        if (!String.IsNullOrEmpty(value))
                        {
                            scoreboard.Add(value);
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// 根據狀態判斷是否要補分
        /// </summary>
        /// <param name="status">狀態</param>
        /// <param name="gameInfo">賽事資訊</param>
        private void AddInningScore(string status, BasicInfo gameInfo)
        {
            //開賽中且狀態文字包含"局"
            if ("S".Equals(gameInfo.GameStates) && status.Contains("局"))
            {
                int inning;
                // 因有 "6局下 結束" 的狀態, 故以 ["局", " "] 做 split
                string[] tmp = status.Split(new string[] { "局", " " }, StringSplitOptions.RemoveEmptyEntries);
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
                        for (int i=0; i < diff; i++)
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
        private void GetBSO(HtmlNode div, BasicInfo gameInfo)
        {
            // BSO
            int b=0, s=0, o=0;
            IEnumerable<HtmlNode> nodeBSO = div.Descendants("span");
            foreach (HtmlNode span in nodeBSO)
            {
                string spanClassName = span.GetAttributeValue("class", String.Empty).ToLower();
                switch (spanClassName)
                {
                    case "yliteon":
                        s++;
                        break;

                    case "gliteon":
                        b++;
                        break;

                    case "rliteon":
                        o++;
                        break;
                }
            }

            gameInfo.BallB = b;
            gameInfo.BallS = s;
            gameInfo.BallO = o;
        }

        /// <summary>
        /// 取得壘包資訊
        /// </summary>
        /// <param name="div">壘包資料節點</param>
        /// <param name="gameInfo">賽事資訊</param>
        private void GetBases(HtmlNode div, BasicInfo gameInfo)
        {
            // 壘包
            int num;
            string bases = div.GetAttributeValue("class", String.Empty);
            if (bases.Length > 0) { bases = bases.Substring(bases.Length - 1); } // 取最後一碼
            Int32.TryParse(bases, out num);
            switch (num)
            {
                case 3: // 3 壘有人
                    gameInfo.Bases = 4;
                    break;

                case 4: // 1,2 壘有人
                    gameInfo.Bases = 3;
                    break;

                default:
                    gameInfo.Bases = num;
                    break;
            }
        }

        // 取得比賽狀態
        private void GetGameStatus(string status, BasicInfo game)
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
            else if (status.Contains("比賽結束"))
            {
                game.GameStates = "E";
                game.Status = "結束";
                game.BallB = 0;
                game.BallS = 0;
                game.BallO = 0;
                game.Bases = 0;
            }
            else if (status.Contains("比賽延期"))
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
                // 補上下局數中的空白
                //game.Status = status.Insert(status.Length - 1, " ");
            }
        }

        #endregion
    }
}
