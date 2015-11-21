using Follow.Sports.Basic;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;


namespace Follow.Sports
{
    public class BkNCAA : Basic.BasicBasketball
    {
        DateTime gameTime;
        // 下載資料
        private BasicDownload DownHome;
        private DateTime DownLastTime = DateTime.Now;

        public BkNCAA(DateTime today)
            : base(ESport.Basketball_NCAA)
        {
            if (string.IsNullOrWhiteSpace(this.sWebUrl))
            {
                this.sWebUrl = @"http://data.ncaa.com/jsonp/scoreboard/basketball-men/d1/{0}/{1:00}/{2:00}/scoreboard.html";
            }
            // 設定
            this.AllianceID = 30;
            this.GameType = "BKNCAA";
            int diffTime = frmMain.GetGameSourceTime("EasternTime");//取得與當地時間差(包含日光節約時間)
            if (diffTime > 0)
                this.GameDate = today.AddHours(-diffTime);
            else
                this.GameDate = GetUtcUsaEt(today);//取得美東時間

            string url = String.Format(this.sWebUrl,
                this.GameDate.Year, this.GameDate.Month, this.GameDate.Day);

            this.DownHome = new BasicDownload(this.Sport, url);
        }

        public override void Download()
        {
            // 讀取首頁資料。
            this.DownHome.DownloadString();
            this.DownLastTime = DateTime.Now;
        }

        public override int Follow()
        {
            // 沒有資料就離開
            if (string.IsNullOrEmpty(this.DownHome.Data)) return 0;

            int result = 0;

            try
            {
                string data = this.DownHome.Data;
                // 取得 json 資料
                data = data.Replace(data.Substring(0, data.IndexOf("{")), "")
                        .Replace(data.Substring(data.LastIndexOf("}") + 1), "");

                JObject obj = JsonConvert.DeserializeObject<JObject>(data);
                JArray array = obj["scoreboard"] as JArray;
                if (array != null)
                {
                    JObject scoreboard = array[0] as JObject;
                    JArray games = scoreboard["games"] as JArray;
                    // 賽程資料
                    if (games != null)
                    {
                        foreach (JObject game in games)
                        {
                            string date = game["startDate"].ToString();
                            string time = game["startTime"].ToString().ToUpper();
                            // 賽程時間未定, 標記訊息
                            if (time.Equals("TBA"))
                                continue;

                            string comment = String.Empty;
                            string webID = game["id"].ToString();
                            string gameState = game["gameState"].ToString().ToLower();
                            
                            //// 取得 Utc 紀元時間
                            //double epoch = Convert.ToDouble(game["startTimeEpoch"].ToString());
                            //DateTime gameTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(epoch);
                            string[] strTime = time.Split(' ');
                            if (strTime.Length >= 3)
                            {
                                if (strTime[1] == "PM")
                                {
                                    DateTime dateTemp = (Convert.ToDateTime(date + " " + strTime[0]).AddHours(12));
                                    gameTime = dateTemp.AddHours(13);
                                }
                                else if (strTime[1] == "AM")
                                {
                                    DateTime dateTemp = (Convert.ToDateTime(date + " " + strTime[0]));
                                    gameTime = dateTemp.AddHours(13);
                                }
                            }
                            string home = String.Empty;
                            string away = String.Empty;
                            //主場隊伍
                            JObject teamHome = game["home"] as JObject;
                            if (teamHome != null)
                                home = teamHome["nameRaw"].ToString();

                            //客場對伍
                            JObject teamAway = game["away"] as JObject;
                            if (teamAway != null)
                                away = teamAway["nameRaw"].ToString();

                            //若主隊或客隊是空值, 不處理
                            if (String.IsNullOrEmpty(home) || String.IsNullOrEmpty(away))
                                continue;

                            BasicInfo gameInfo = new BasicInfo(this.AllianceID, this.GameType, gameTime, webID);
                            gameInfo.Home = home;
                            gameInfo.Away = away;

                            //剩餘時間
                            gameInfo.Status = game["timeclock"].ToString();
                            if (gameInfo.Status.IndexOf(":") == 0)//剩餘時間小於1分鐘
                            {
                                gameInfo.Status = gameInfo.Status.Replace(":", "");
                                int findIndex = gameInfo.Status.IndexOf(".");
                                if (findIndex > -1)//剩餘時間有小數點
                                    gameInfo.Status = gameInfo.Status.Substring(0, gameInfo.Status.Length - findIndex);//去除小數點
                            }

                            int iTime;
                            if (int.TryParse(gameInfo.Status, out iTime))
                            {
                                if (iTime >= 10)
                                    gameInfo.Status = "00:" + gameInfo.Status;//剩餘超過10秒
                                else
                                    gameInfo.Status = "00:0" + gameInfo.Status;
                            }
                            else
                                gameInfo.Status = "00:" + gameInfo.Status;

                            //比賽狀態
                            switch (gameState)
                            {
                                case "pre":
                                    gameInfo.GameStates = "X";
                                    break;
                                case "live":
                                    gameInfo.GameStates = "S";
                                    if (string.IsNullOrEmpty(gameInfo.Status))
                                        gameInfo.Status = "中場休息";
                                    break;
                                case "final":
                                    gameInfo.GameStates = "E";
                                    break;
                            }

                            // 上,下,OT 比分
                            JArray scoreHome = teamHome["scoreBreakdown"] as JArray;
                            for (int i = 0; i < scoreHome.Count; i++)
                                gameInfo.HomeBoard.Add(scoreHome[i].ToString());

                            JArray scoreAway = teamAway["scoreBreakdown"] as JArray;
                            for (int i = 0; i < scoreAway.Count; i++)
                                gameInfo.AwayBoard.Add(scoreAway[i].ToString());

                            // 總分
                            gameInfo.AwayPoint = teamHome["currentScore"].ToString();
                            gameInfo.HomePoint = teamAway["currentScore"].ToString();

                            // 加入
                            this.GameData[gameInfo.WebID] = gameInfo;

                            // 累計
                            result++;
                        }
                    }
                }
            }
            catch { }

            // 傳回
            return result;
        }

    }
}
