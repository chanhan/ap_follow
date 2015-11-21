using Follow.Sports.Basic;
using Newtonsoft.Json.Linq;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Forms;
using HtmlAgilityPack;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;

// 跟盤以 ID 為依據
// 跟盤日期以美國時間顯示

namespace Follow.Sports
{
    public class BbMLB2 : Basic.BasicBaseball
    {
        private int iIndex = -1;
        public BbMLB2(DateTime today)
            : base(ESport.Baseball_MLB2)
        {
            //清空
            this.Dispose();
            // 設定
            this.AllianceID = 53;
            this.GameType = "BBUS";
            if (string.IsNullOrWhiteSpace(this.sWebUrl))
            {
                this.sWebUrl = @"http://scores.espn.go.com/mlb/scoreboard";
            }
            this.DownHome = new BasicDownload(this.Sport, this.sWebUrl);
        }
        public override void Download()
        {
            if (this.DownHome.LastTime == null ||
                DateTime.Now >= this.DownHome.LastTime.Value.AddSeconds(4)) //更改4秒，为保证每5秒的计时器 都会进行下载。也就是保证5秒下载一次
            {
                this.DownHome.DownloadString();
            }
        }



        public override int Follow()
        {

            // 沒有資料就離開
            if (string.IsNullOrWhiteSpace(this.DownHome.Data)) return 0;
            //Logs.DownloadBrowser(this.DownHome.Data);
            int result = 0;
            int iWrong = 0;
            HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(this.DownHome.Data);
            try
            {
                iWrong = 1;
                List<HtmlNode> list = doc.DocumentNode.Descendants("script").ToList();
                if (iIndex == -1)
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        if (list[i].InnerText.Contains("window.espn.scoreboardData"))
                        {
                            iIndex = i;
                            break;
                        }
                    }
                }
                iWrong = 2;
                string script = list[iIndex].InnerText.TrimEnd(';');
                iWrong = 22;
                if (!script.Contains("window.espn.scoreboardData"))
                {
                    iIndex = -1;
                    return 0;
                }
                script = script.Substring(0, script.LastIndexOf("window.espn.scoreboardSettings") - 1).Substring(script.IndexOf("=") + 1);

                iWrong = 3;
                JObject obj = JObject.Parse(script);
                if (obj["events"] == null)
                {
                    return 0;
                }
                List<JToken> games = obj["events"].Children().ToList();
                iWrong = 4;
                DateTime GameTime;
                JToken competitions, competitorsA, competitorsB;
                string status;
                int R = 0, H = 0, E = 0;
                int b = 0, s = 0, o = 0;
                bool first, second, third;
                foreach (JToken game in games)
                {
                    if (game["competitions"] == null)
                    {
                        return 0;
                    }
                    competitions = game["competitions"].ToList()[0];
                    iWrong = 5;
                    if (competitions == null || competitions["startDate"] == null)
                    {
                        return 0;
                    }
                    //json中的时间是[2015-05-23T16:11Z]格式    ///是否 和机器设置的区域有关 
                    //区域性名称和标识符。 https://msdn.microsoft.com/zh-cn/library/System.Globalization.CultureInfo%28v=vs.80%29.aspx
                    IFormatProvider culture = new CultureInfo("zh-TW", true);
                    GameTime = DateTime.Parse(competitions["startDate"].ToObject<string>(), culture);
                    iWrong = 6;
                    BasicInfo gameInfo = new BasicInfo(this.AllianceID, this.GameType, GameTime, competitions["id"].ToObject<string>());//比賽資料初始化
                    gameInfo.IsBall = true;
                    iWrong = 7;
                    if (game["status"] == null || game["status"]["type"] == null || game["status"]["type"]["shortDetail"] == null)
                    {
                        return 0;
                    }
                    // 比赛状态
                    status = game["status"]["type"]["shortDetail"].ToObject<string>();
                    iWrong = 8;
                    // 沒有比賽狀態, 不處理
                    if (String.IsNullOrEmpty(status)) { continue; }

                    iWrong = 9;
                    competitorsA = competitions["competitors"].ToList()[0];
                    if (competitorsA == null || competitorsA["team"] == null || competitorsA["team"]["name"] == null)
                    {
                        return 0;
                    }
                    iWrong = 10;
                    competitorsB = competitions["competitors"].ToList()[1];
                    if (competitorsB == null || competitorsB["team"] == null || competitorsB["team"]["name"] == null)
                    {
                        return 0;
                    }
                    iWrong = 11;
                    gameInfo.Home = competitorsA["team"]["name"].ToObject<string>();
                    gameInfo.Away = competitorsB["team"]["name"].ToObject<string>();
                    iWrong = 12;
                    //主队分数处理
                    R = H = E = 0;
                    ProcessScore(competitorsA, gameInfo.HomeBoard, out R, out H, out E);
                    if (R == -1)
                    {
                        //等于-1表示competitions["score"] == null  不更新此数据
                        return 0;
                    }
                    gameInfo.HomePoint = R.ToString();
                    gameInfo.HomeH = H.ToString();
                    gameInfo.HomeE = E.ToString();

                    //客队分数处理
                    R = H = E = 0;
                    ProcessScore(competitorsB, gameInfo.AwayBoard, out R, out H, out E);
                    if (R == -1)
                    {
                        //等于-1表示competitions["score"] == null  不更新此数据
                        return 0;
                    }
                    gameInfo.AwayPoint = R.ToString();
                    gameInfo.AwayH = H.ToString();
                    gameInfo.AwayE = E.ToString();

                    iWrong = 13;
                    //获取状态
                    GetGameStatus(status, gameInfo);

                    iWrong = 14;
                    if ("S".Equals(gameInfo.GameStates) && competitions["situation"] != null)//開賽中取得BSOB
                    {
                        iWrong = 140;
                        b = s = o = 0;
                        first = second = third = false;
                        int.TryParse(competitions["situation"]["balls"] == null ? "0" : competitions["situation"]["balls"].ToObject<string>(), out b);
                        int.TryParse(competitions["situation"]["strikes"] == null ? "0" : competitions["situation"]["strikes"].ToObject<string>(), out s);
                        int.TryParse(competitions["situation"]["outs"] == null ? "0" : competitions["situation"]["outs"].ToObject<string>(), out o);

                        gameInfo.BallB = b;
                        gameInfo.BallS = s;
                        gameInfo.BallO = o;
                        iWrong = 141;
                        bool.TryParse(competitions["situation"]["onFirst"] == null ? "0" : competitions["situation"]["onFirst"].ToObject<string>(), out first);
                        bool.TryParse(competitions["situation"]["onSecond"] == null ? "0" : competitions["situation"]["onSecond"].ToObject<string>(), out second);
                        bool.TryParse(competitions["situation"]["onThird"] == null ? "0" : competitions["situation"]["onThird"].ToObject<string>(), out third);
                        iWrong = 142;
                        gameInfo.Bases = getBases(string.Concat(third ? "1" : "0", second ? "1" : "0", first ? "1" : "0"));
                    }
                    iWrong = 15;
                    // 加入
                    this.GameData[gameInfo.WebID] = gameInfo;
                    // 累計
                    result++;
                }

            }
            catch (Exception e)
            {
                Logs.DownloadBrowser(" \r\n======================== \r\n iWrong:" + iWrong + "  \r\n Message:" + e.Message + " \r\n Source:" + e.Source + "\r\n stacktrace:" + e.StackTrace);
            }
            // 傳回
            return result;
        }

        /// <summary>
        /// 处理分数
        /// </summary>
        private void ProcessScore(JToken competitions, List<string> score, out int r, out int h, out int e)
        {
            ////因为json数据中，字段名称有时会没有，所以加强==null的判断
            if (competitions["linescores"] != null)
            {
                foreach (JToken item in competitions["linescores"].Children())
                {
                    if (item["value"] == null)
                    {
                        continue;
                    }
                    score.Add(item["value"].ToObject<string>());
                }
            }

            if (competitions["score"] == null || competitions["hits"] == null || competitions["errors"] == null)
            {
                r = h = e = -1;
            }
            else
            {
                int.TryParse(competitions["score"].ToObject<string>(), out r);
                int.TryParse(competitions["hits"].ToObject<string>(), out h);
                int.TryParse(competitions["errors"].ToObject<string>(), out e);
            }
        }
        public override bool Update(string connectionString, BasicInfo info)
        {
            // 以隊伍名稱當作更新的依據
            return this.Update3(connectionString, info);
        }

        // 取得比賽狀態
        private void GetGameStatus(string status, BasicInfo game)
        {
            status = status.ToLower();
            if (status.Contains("final"))
            {
                game.GameStates = "E";
                game.Status = "結束";
                game.BallB = 0;
                game.BallS = 0;
                game.BallO = 0;
                game.Bases = 0;

                //结束之后补X
                if (game.AwayBoard.Count() > game.HomeBoard.Count())
                {
                    game.HomeBoard.Add("X");
                }
            }
            else if (status.Contains("postponed"))
            {
                game.GameStates = "P";
                game.Status = "中止";
                game.TrackerText = "因雨延賽";
                game.BallB = 0;
                game.BallS = 0;
                game.BallO = 0;
                game.Bases = 0;
            }
            else if (status.Contains("delay"))
            {
                game.GameStates = "D";
                game.Status = "DELAY";
                game.BallB = 0;
                game.BallS = 0;
                game.BallO = 0;
                game.Bases = 0;
            }
            else if (game.Quarter > 0)
            {
                string[] statusInfo = status.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                if (statusInfo.Length == 2)//狀態取外頁，第X局取單獨賽事比分，所以可能會發生不同步的狀況ex:外頁四局結束  賽事五局上
                {
                    if (statusInfo[0].Contains("end"))
                    {
                        if (game.Quarter == game.HomeBoard.Count)//主客隊比分數目相同
                            game.Status = "結束";
                        else
                            game.Status = string.Format("{0}局 上", game.Quarter);//賽事比分已經換局
                    }
                    else if (statusInfo[0].Contains("top") || statusInfo[0].Contains("mid"))
                        game.Status = string.Format("{0}局 上", game.Quarter);
                    else if (statusInfo[0].Contains("bot"))
                        game.Status = string.Format("{0}局 下", game.Quarter);

                    game.GameStates = "S";
                }
            }
            game.Status = game.Status != null ? game.Status.Replace("-", "").Trim() : game.Status;
        }

        // 下載資料
        private BasicDownload DownHome;
    }
}
