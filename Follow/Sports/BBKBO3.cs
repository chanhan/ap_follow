using Follow.Sports.Basic;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Follow.Sports
{
    class BBKBO3 : Basic.BasicBaseball
    {
        BasicDownload DownHome;
        public BBKBO3(DateTime today)
            : base(ESport.Baseball_KBO3)
        {
            if (string.IsNullOrWhiteSpace(sWebUrl))
            {
                sWebUrl = "http://data.cast.sports.media.daum.net/bs/kbo/{0}|{1}";
            }
            // 設定
            this.AllianceID = 66;
            this.GameType = "BBKR";
            this.GameDate = today.Date; // 只取日期
            //获取来源网id
            List<string> list = getWebID();
            // 運彩 Url 參數: aid=9 (賽事種類: 韓國職棒) gamedate (時間: yyyyMMdd) mode=11 (盤口: 國際)
            string url = String.Format(this.sWebUrl, list.Count > 0 ? list[0] : "", this.GameDate.ToString("yyyy"));
            this.DownHome = new BasicDownload(this.Sport, url);
        }
        public override void Download()
        {
            // 沒有資料或下載時間超過 2 秒才讀取資料。
            if (!this.DownHome.LastTime.HasValue ||
                DateTime.Now >= this.DownHome.LastTime.Value.AddSeconds(2))
            {
                this.DownHome.DownloadString();
            }
        }
        public override int Follow()
        {
            // 沒有資料就離開
            if (string.IsNullOrWhiteSpace(this.DownHome.Data))
                return 0;

            #region 变量
            int result = 0;
            BasicInfo gameInfo;
            string webID = string.Empty;
            int b = 0, s = 0, o = 0;
            #endregion 变量

            JObject obj = JObject.Parse(this.DownHome.Data);
            //检查json合法性
            if (obj == null || obj["registry"] == null || obj["registry"]["scoreboard"] == null)
            {
                return 0;
            }
            List<JToken> games = obj["registry"]["scoreboard"].Children().ToList();
            foreach (JProperty Pgame in games)
            {
                JToken game = Pgame.Value;
                //检查json合法性
                if (game["game_code"] == null || game["away_team"] == null
                    || game["home_team"] == null || game["status"] == null
                    || game["home"] == null || game["away"] == null)
                {
                    continue;
                }
                //"20150616NCKT0|2015"
                webID = game["game_code"].ToString().Split('|')[0];

                // 建立比賽資料
                gameInfo = null;
                gameInfo = new BasicInfo(this.AllianceID, this.GameType, DateTime.Now, webID);
                gameInfo.IsBall = true;
                gameInfo.Away = game["away_team"].ToString();
                gameInfo.Home = game["home_team"].ToString();

                // 沒有比賽狀態, 不處理
                if (string.IsNullOrWhiteSpace(game["status"].ToString())) { continue; }

                GetGameStatus(game["status"].ToString(), gameInfo, game["half"].ToString(), game["inning"].ToString());

                #region 分数
                // 開賽/結束/中止才取比分
                if ("S".Equals(gameInfo.GameStates) || "E".Equals(gameInfo.GameStates) || "P".Equals(gameInfo.GameStates))
                {
                    if (game["home"]["inning"].ToString() != "")
                    {
                        gameInfo.HomeBoard = game["home"]["inning"].ToString().Split(',').ToList();
                    }
                    gameInfo.HomeE = game["home"]["e"].ToString();
                    gameInfo.HomeH = game["home"]["h"].ToString();
                    gameInfo.HomePoint = game["home"]["r"].ToString();

                    if (game["away"]["inning"].ToString() != "")
                    {
                        gameInfo.AwayBoard = game["away"]["inning"].ToString().Split(',').ToList();
                    }
                    gameInfo.AwayE = game["away"]["e"].ToString();
                    gameInfo.AwayH = game["away"]["h"].ToString();
                    gameInfo.AwayPoint = game["away"]["r"].ToString();

                    // 判斷分數 是否需要补X
                    if ("E".Equals(gameInfo.GameStates) && gameInfo.AwayBoard.Count > gameInfo.HomeBoard.Count)
                    {
                        gameInfo.HomeBoard.Add("X");
                    }
                }
                #endregion 分数
                #region BSOB
                if ("S".Equals(gameInfo.GameStates))
                {
                    //检查合法性
                    if (game["ball"] == null || game["out"] == null || game["strike"] == null
                    || game["bases"] == null)
                    {
                        continue;
                    }
                    b = 0; s = 0; o = 0;
                    int.TryParse(game["ball"].ToString(), out b);
                    int.TryParse(game["out"].ToString(), out o);
                    int.TryParse(game["strike"].ToString(), out s);

                    gameInfo.BallB = b;
                    gameInfo.BallO = o;
                    gameInfo.BallS = s;

                    string bases = string.Concat(game["bases"]["base3"] == null || game["bases"]["base3"].ToString() == "" ? "0" : "1",
                        game["bases"]["base2"] == null || game["bases"]["base2"].ToString() == "" ? "0" : "1",
                        game["bases"]["base1"] == null || game["bases"]["base1"].ToString() == "" ? "0" : "1");

                    gameInfo.Bases = getBases(bases);
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
        /// <summary>
        /// 获取来源网WebID
        /// </summary>
        /// <returns></returns>
        private List<string> getWebID()
        {
            string sSql = "SELECT DISTINCT WebID FROM [dbo].[BaseballSchedules] WHERE  GameType='BBKR' AND GameDate=@Date";
            using (SqlConnection conn = new SqlConnection(frmMain.ConnectionString))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand(sSql, conn);
                cmd.Parameters.Add("@Date", SqlDbType.Date).Value = this.GameDate.Date;
                try
                {
                    SqlDataReader dr = cmd.ExecuteReader();
                    List<string> list = new List<string>();
                    while (dr.Read())
                    {
                        if (dr["WebID"] != DBNull.Value && !string.IsNullOrWhiteSpace(dr["WebID"].ToString()))
                        {
                            list.Add(dr["WebID"].ToString());
                        }
                    }
                    return list;
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }

        /// <summary>
        /// 取得比赛状态
        /// </summary>
        private void GetGameStatus(string status, BasicInfo game, string half, string inning)
        {
            status = status.ToLower();

            if (status.Contains("befo"))
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
            else if (status.Contains("end"))
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
                game.Status = string.Format("{0}局 {1}", inning, half == "first" ? "上" : "下");
            }
        }
    }
}
