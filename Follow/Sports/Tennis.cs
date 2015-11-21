using Follow.Sports.Basic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Transactions;
using System.Web;
using System.Linq;
using System.IO;

namespace Follow.Sports
{
    public class Tennis : Basic.BasicFB
    {
        #region 成員變數
        // 下載資料
        private TennisDownload DownHome;
        private List<TennisDownload> DownReal;
        private DateTime DownLastTime = DateTime.Now;
        private Dictionary<string, string> DownHomeHeader;

        private DataTable dtNameControlForUser;
        private DataTable dtAlliance;

        // 旗標: 是否正在取得資料
        private bool _onDataLoading = false;

        // 時間字串格式
        private const string DATE_STRING_FORMAT = "yyyy-MM-dd";

        private const string TIME_STRING_FORMAT = "HH:mm";
        //全部比赛信息
        Dictionary<string, Dictionary<string, BasicInfo>> gameData = new Dictionary<string, Dictionary<string, BasicInfo>>();
        //更新的比赛信息
        Dictionary<string, Dictionary<string, BasicInfo>> updateGameData = new Dictionary<string, Dictionary<string, BasicInfo>>();
        //新增的比赛信息
        Dictionary<string, Dictionary<string, BasicInfo>> addGameData = new Dictionary<string, Dictionary<string, BasicInfo>>();
        public Dictionary<string, BasicInfo> deleteGameData = new Dictionary<string, BasicInfo>();
        public Dictionary<string, BasicInfo> changeDateGameData = new Dictionary<string, BasicInfo>();
        private int startIndex = -1;
        private int endIndex = 7;
        private List<BasicInfo> oldOverDayGame = new List<BasicInfo>();
        private List<BasicInfo> newOverDayGame = new List<BasicInfo>();
        #endregion
        private string sWebUrl1 = UrlSetting.GetUrl(ESport.Tennis, "Url1");
        private string sWebUrl2 = UrlSetting.GetUrl(ESport.Tennis, "Url2");
        private string sWebUrl3 = UrlSetting.GetUrl(ESport.Tennis, "Url3");
        public Tennis(DateTime today)
            : base(ESport.Tennis)
        {
            // Tennis 讀取翻譯表/聯盟表
            this.LoadNameControlAndAlliance();

            this.Logs = new LogFile(ESport.Tennis);//設定log type
            if (string.IsNullOrWhiteSpace(this.sWebUrl))
            {
                this.sWebUrl = "http://d.flashscore.com/x/feed/f_2_0_8_en-asia_1";
            }
            if (string.IsNullOrWhiteSpace(this.sWebUrl1))
            {
                this.sWebUrl1 = "http://d.flashscore.com/x/feed/proxy";
            }
            if (string.IsNullOrWhiteSpace(this.sWebUrl2))
            {
                this.sWebUrl2 = "http://d.flashscore.com/x/feed/f_2_-1_8_en-asia_1";
            }
            if (string.IsNullOrWhiteSpace(this.sWebUrl3))
            {
                this.sWebUrl3 = "http://d.flashscore.com/x/feed/f_2_";
            }
            // 設定
            this.AllianceID = 0;
            this.GameType = "TN";
            this.GameDate = GetUtcTw(today).Date; // 只取日期
            this.DownHome = new TennisDownload(this.Sport, this.GameDate, this.sWebUrl, "f_2_0_8_en-asia_1");
            this.DownHomeHeader = new Dictionary<string, string>();
            this.DownHomeHeader["Accept"] = "*/*";
            this.DownHomeHeader["Accept-Charset"] = "utf-8;q=0.7,*;q=0.3";
            this.DownHomeHeader["Accept-Encoding"] = "gzip,deflate,sdch";
            this.DownHomeHeader["Accept-Language"] = "*";
            this.DownHomeHeader["X-Fsign"] = "SW9D1eZo";
            this.DownHomeHeader["X-GeoIP"] = "1";
            this.DownHomeHeader["X-utime"] = "1";
            this.DownHomeHeader["Cookie"] = "__utma=175935605.237435887.1433729535.1433729535.1433729535.1; __utmb=175935605.6.10.1433729535; __utmc=175935605; __utmz=175935605.1433729535.1.1.utmcsr=(direct)|utmccn=(direct)|utmcmd=(none); __utmt=1; __gads=ID=95d4003915b743c6:T=1433729537:S=ALNI_MYpkSaA3-m9gggcZp-An3QTk6uUOg";
            this.DownHomeHeader["Host"] = "d.flashscore.com";
            this.DownHomeHeader["Referer"] = this.sWebUrl1;
            // 昨天
            this.DownReal = new List<TennisDownload>();
            this.DownReal.Add(new TennisDownload(this.Sport, this.GameDate.AddDays(startIndex), this.sWebUrl2, "f_2_-1_8_en-asia_1"));
            // 往後 7 天
            for (int i = 1; i <= endIndex; i++)
            {
                this.DownReal.Add(new TennisDownload(this.Sport, this.GameDate.AddDays(i), this.sWebUrl3 + i + "_8_en-asia_1", "f_2_" + i + "_8_en-asia_1"));
            }

            #region 從資料庫中取得舊資料

            GetOldGames();

            #endregion 從資料庫中取得舊資料
        }

        private void GetOldGames()
        {
            DataTable dt = this.GetTennisData(frmMain.ConnectionString, this.GameDate.AddDays(startIndex - 1), this.GameDate.AddDays(endIndex - 1));
            foreach (DataRow dr in dt.Rows)
            {
                int allianceid = dr.Field<int>("AllianceID");
                string webID = dr.Field<string>("WebID");
                string gameDate = dr.Field<DateTime>("GameDate").ToString(DATE_STRING_FORMAT);
                DateTime gameTime = DateTime.Parse(gameDate + " " + dr.Field<TimeSpan>("GameTime").ToString(@"hh\:mm"));
                BasicInfo basic = new BasicInfo(allianceid, this.GameType, gameTime, webID);
                basic.AllianceName = dr.Field<string>("alliancename");
                basic.GameStates = dr.Field<string>("GameStates");
                basic.Home = dr.Field<string>("home");
                basic.Away = dr.Field<string>("away");
                basic.HomePoint = dr.Field<string>("RA");
                basic.AwayPoint = dr.Field<string>("RB");
                basic.WN = dr.Field<int>("WN");
                basic.PR = dr.Field<int>("PR");
                basic.HomeBoard = dr.Field<string>("RunsA") != null ? dr.Field<string>("RunsA").Split(',').ToList() : null;
                basic.AwayBoard = dr.Field<string>("RunsB") != null ? dr.Field<string>("RunsB").Split(',').ToList() : null;
                basic.TrackerText = dr.Field<string>("TrackerText");
                Dictionary<string, BasicInfo> dic = this.gameData.ContainsKey(gameDate) ? this.gameData[gameDate] : new Dictionary<string, BasicInfo>();
                if (!dic.ContainsKey(basic.WebID))
                {
                    dic.Add(basic.WebID, basic);
                }
                this.gameData[gameDate] = dic;
            }
        }
        public override void Download()
        {
            // 讀取首頁資料。
            this.DownHome.DownloadData(this.DownHomeHeader);
            // 下載昨天資料
            this.DownReal[0].DownloadData(this.DownHomeHeader);
            this.DownLastTime = DateTime.Now;
            //下載往後 7 天
            for (int i = 1; i <= endIndex; i++)
			{
			  // 沒有資料或下載時間超過 2 小時才讀取資料。
                if (DownReal[i].LastTime == null ||
                    DateTime.Now >= DownReal[i].LastTime.Value.AddHours(2))
                {
                    DownReal[i].DownloadData(this.DownHomeHeader);
                }
			}
        }
        public override int Follow()
        {
            // 聯盟資料
            Dictionary<string, List<string>> teamData = new Dictionary<string, List<string>>();

            // 賽事資料集合 ( Key: 開賽日期 Value: 當日賽事集合[Key: WebID] )
            Dictionary<string, Dictionary<string, BasicInfo>> gameList = new Dictionary<string, Dictionary<string, BasicInfo>>();
            // 處理資料
            DateTime time = DateTime.Now;//记录解析赛事资料前的日期，与解析出来的比赛日期比对，如果日期不相等，则舍弃
            Dictionary<string, BasicInfo> todayGame = this.GetTennisData(this.DownHome);
            Dictionary<string, BasicInfo> yestodayGame = this.GetTennisData(this.DownReal[0]);
            newOverDayGame = new List<BasicInfo>();
            if (todayGame != null && todayGame.Count > 0 && this.DownHome.GameDate.Date == time.Date)
            {
                newOverDayGame = todayGame.Values.Where(p => p.OverDayGame).ToList();//跨天赛事
                newOverDayGame.ForEach(p =>
                {
                    todayGame.Remove(p.WebID);//从今日资料中移除跨天赛事，加入到昨日
                });
                gameList[this.DownHome.GameDate.ToString(DATE_STRING_FORMAT)] = todayGame;
            }
            if (yestodayGame != null && yestodayGame.Count > 0 && this.DownReal[0].GameDate.Date == time.AddDays(-1).Date)
            {
                newOverDayGame.ForEach(p =>
                {
                    if (!yestodayGame.ContainsKey(p.WebID))
                    {
                        yestodayGame.Add(p.WebID, p);
                    }
                });
                // 昨天
                gameList[this.DownReal[0].GameDate.ToString(DATE_STRING_FORMAT)] = yestodayGame;
            }
            // 往後 7 天
            for (int i = 1; i <= endIndex; i++)
            {
                Dictionary<string, BasicInfo> gameByDate = this.GetTennisData(this.DownReal[i]);
                if (gameByDate != null && gameByDate.Count > 0 && this.DownReal[i].GameDate.Date == time.AddDays(i).Date)
                {
                    gameList[this.DownReal[i].GameDate.ToString(DATE_STRING_FORMAT)] = gameByDate;
                }
            }
            if (gameList != null && gameList.Count > 0)
            {
                foreach (KeyValuePair<string, Dictionary<string, BasicInfo>> pair in gameList)
                {
                    foreach (var item in pair.Value)
                    {
                        BasicInfo basic = item.Value;
                        // 聯盟/隊伍資料
                        string alliance = basic.AllianceName;
                        string teamA = basic.Home;
                        string teamB = basic.Away;
                        List<string> teamList = (teamData.ContainsKey(alliance)) ? teamData[alliance] : new List<string>();
                        if (!teamList.Contains(teamA)) { teamList.Add(teamA); }
                        if (!teamList.Contains(teamB)) { teamList.Add(teamB); }
                        teamData[alliance] = teamList;
                    }
                }
            }

            // 新增聯盟資料
            AddAllianceAndTeamToTable(teamData);

            //比对赛事资料
            CompareData(gameList);
            // 傳回
            return gameList.Count;
        }
        public override int Update(string connectionString)
        {
            if (updateGameData == null) { return 0; }
            if (addGameData == null) { return 0; }
            if (deleteGameData == null) { return 0; }
            if (changeDateGameData == null) { return 0; }
            int result = UpdateData(connectionString, updateGameData, addGameData, deleteGameData, changeDateGameData);
            return result;
        }
        protected int UpdateData(string connectionString, Dictionary<string, Dictionary<string, BasicInfo>> updateGameData, Dictionary<string, Dictionary<string, BasicInfo>> addGameData, Dictionary<string, BasicInfo> deleteGameData, Dictionary<string, BasicInfo> changeDateGameData)
        {
            StringBuilder sb = new StringBuilder();
            int result = 0;
            if (deleteGameData.Count > 0)
            {
                List<string> list = deleteGameData.Values.Select(p => "'" + p.WebID + "'").ToList();
                sb.AppendFormat("begin\r\n");
                sb.AppendFormat("DELETE FROM [TennisSchedules] WHERE webid IN({0})\r\n", string.Join(",", list));
                sb.AppendFormat("end\r\n");
                result += ExecuteScalar(connectionString, sb.ToString());
                this.Logs.Update("deleteGameData:\r\n" + sb.ToString());
            }
            sb = new StringBuilder();
            if (changeDateGameData.Count > 0)
            {
                List<string> list = changeDateGameData.Values.Select(p => "'" + p.WebID + "'").ToList();
                sb.AppendFormat("begin\r\n");
                sb.AppendFormat("DELETE FROM [TennisSchedules] WHERE webid IN({0})\r\n", string.Join(",", list));
                sb.AppendFormat("end\r\n");
                result += ExecuteScalar(connectionString, sb.ToString());
                this.Logs.Update("changeDateGameData:\r\n" + sb.ToString());
            }
            sb = new StringBuilder();
            sb.AppendFormat("declare @webid nvarchar(50)\r\n");
            sb.AppendFormat("declare @allianceid int,@teamaid int ,@teambid int\r\n");
            foreach (KeyValuePair<string, Dictionary<string, BasicInfo>> item in addGameData)
            {

                sb.AppendFormat("begin\r\n");
                string gameDate = item.Key;
                Dictionary<string, BasicInfo> basic = item.Value;
                foreach (KeyValuePair<string, BasicInfo> pair in basic)
                {
                    BasicInfo game = pair.Value;
                    sb.AppendFormat("set @webid=null\r\n");
                    sb.AppendFormat("select @webid=webid from [TennisSchedules] with(nolock) where webid='{0}'\r\n", game.WebID, game.GameType);
                    sb.AppendFormat("if @webid is null\r\n");
                    sb.AppendFormat("begin\r\n");
                    sb.AppendFormat("select @allianceid=allianceid from [TNAlliance] with(nolock) where alliancename='{0}'\r\n", game.AllianceName);
                    sb.AppendFormat("SELECT @teamaid=id FROM NameControl WITH (NOLOCK) WHERE (GTLangx = N'TN') AND (GameType = N'Name') AND SourceText='{0}'\r\n", game.Home);
                    sb.AppendFormat("SELECT @teambid=id FROM NameControl WITH (NOLOCK) WHERE (GTLangx = N'TN') AND (GameType = N'Name') AND SourceText='{0}'\r\n", game.Away);
                    sb.AppendFormat("INSERT INTO [TennisSchedules] ([AllianceID],[GameDate],[GameTime],[GameStates],[TeamAID],[TeamBID],[RunsA],[RunsB],[RA],[RB],[WN],[PR],[WebID],[TrackerText],[OrderBy]) VALUES ({0},'{1}','{2}','{3}',{4},{5},{6},{7},'{8}','{9}',{10},{11},'{12}',{13},{14})\r\n",
                    "@allianceid",
                    game.GameTime.ToString(DATE_STRING_FORMAT),
                    game.GameTime.ToString("HH:mm"),
                    game.GameStates, "@teamaid", "@teambid",
                    game.HomeBoard.Count > 0 ? "'" + string.Join(",", game.HomeBoard) + "'" : "NULL",
                    game.AwayBoard.Count > 0 ? "'" + string.Join(",", game.AwayBoard) + "'" : "NULL",
                    game.HomePoint,
                    game.AwayPoint, game.WN, game.PR,
                    game.WebID,
                    game.TrackerText != null ? "'" + game.TrackerText + "'" : "NULL",
                    game.OrderBy
                    );
                    sb.AppendFormat("end\r\n");
                }
                sb.AppendFormat("end\r\n");
                result += ExecuteScalar(connectionString, sb.ToString());
                this.Logs.Update("addGameData:\r\n" + sb.ToString());
                sb = new StringBuilder();
                sb.AppendFormat("declare @webid nvarchar(50)\r\n");
                sb.AppendFormat("declare @allianceid int,@teamaid int ,@teambid int\r\n");
            }
            sb = new StringBuilder();
            foreach (KeyValuePair<string, Dictionary<string, BasicInfo>> item in updateGameData)
            {
                sb.AppendFormat("begin\r\n");
                string gameDate = item.Key;
                Dictionary<string, BasicInfo> basic = item.Value;
                foreach (KeyValuePair<string, BasicInfo> pair in basic)
                {
                    BasicInfo game = pair.Value;
                    sb.AppendFormat("UPDATE [TennisSchedules] SET [GameStates] ='{0}',[RunsA] ={1},[RunsB] = {2},[RA] = '{3}',[RB] = '{4}',[TrackerText] ={5},[WN] = {6},[PR]={7},[GameDate]='{8}',[GameTime]='{9}',[ChangeCount] =[ChangeCount]+1,[OrderBy]={10} where webid='{11}'\r\n",
                    game.GameStates,
                    game.HomeBoard.Count > 0 ? "'" + string.Join(",", game.HomeBoard) + "'" : "NULL",
                    game.AwayBoard.Count > 0 ? "'" + string.Join(",", game.AwayBoard) + "'" : "NULL",
                    game.HomePoint, game.AwayPoint,
                    game.TrackerText != null ? "'" + game.TrackerText + "'" : "NULL",
                    game.WN, game.PR,
                    game.GameTime.ToString(DATE_STRING_FORMAT),
                    game.GameTime.ToString(TIME_STRING_FORMAT),
                    game.OrderBy,
                    game.WebID);
                }
                sb.AppendFormat("end\r\n");
                result += ExecuteScalar(connectionString, sb.ToString());
                this.Logs.Update("updateGameData:\r\n" + sb.ToString());
                sb = new StringBuilder();
            }
            sb = new StringBuilder();
            if (updateGameData.Count > 0 || addGameData.Count > 0 || deleteGameData.Count > 0 || changeDateGameData.Count > 0)
            {
                foreach (KeyValuePair<string, Dictionary<string, BasicInfo>> item in this.gameData)
                {
                    sb.AppendFormat("begin\r\n");
                    string key = item.Key;
                    Dictionary<string, BasicInfo> basic = item.Value;
                    foreach (KeyValuePair<string, BasicInfo> pair in basic)
                    {
                        BasicInfo game = pair.Value;
                        if (game.GameTime.ToString(DATE_STRING_FORMAT) == key && !game.OverDayGame)
                        {
                            sb.AppendFormat("UPDATE [TennisSchedules] SET [OrderBy] ={0} where webid='{1}'\r\n", game.OrderBy, game.WebID);
                        }
                    }
                    sb.AppendFormat("end\r\n");
                    ExecuteScalar(connectionString, sb.ToString());
                    this.Logs.Update("OrderBy:\r\n" + sb.ToString());
                    sb = new StringBuilder();
                }
            }
            return result;
        }

        private void CompareData(Dictionary<string, Dictionary<string, BasicInfo>> gameList)
        {
            try
            {
                if (gameList.Count == 0) return;
                updateGameData = new Dictionary<string, Dictionary<string, BasicInfo>>();
                addGameData = new Dictionary<string, Dictionary<string, BasicInfo>>();
                deleteGameData = new Dictionary<string, BasicInfo>();
                changeDateGameData = new Dictionary<string, BasicInfo>();
                List<string> gameDateList = gameList.Keys.ToList();
                foreach (KeyValuePair<string, Dictionary<string, BasicInfo>> pair in this.gameData)
                {
                    string oldKey = pair.Key;
                    Dictionary<string, BasicInfo> oldData = pair.Value;
                    foreach (string gameDate in gameDateList)
                    {
                        if (gameDate != oldKey)
                        {
                            Dictionary<string, BasicInfo> newData = gameList[gameDate];
                            List<BasicInfo> oldInfoList = oldData.Values.ToList();
                            foreach (BasicInfo game in oldInfoList)
                            {
                                if (newData.ContainsKey(game.WebID) && !game.OverDayGame && !changeDateGameData.ContainsKey(game.WebID))
                                {
                                    changeDateGameData.Add(game.WebID, newData[game.WebID]);//比赛推迟或手动改日期
                                }
                            }
                        }
                    }
                }
                foreach (string gameDate in gameDateList)
                {
                    if (!this.gameData.ContainsKey(gameDate))
                    {
                        addGameData[gameDate] = gameList[gameDate];//新增一整天的比赛信息
                        continue;
                    }
                    Dictionary<string, BasicInfo> oldGames = this.gameData[gameDate];
                    Dictionary<string, BasicInfo> newGames = gameList[gameDate];
                    List<BasicInfo> infoList = newGames.Values.ToList();
                    List<BasicInfo> oldInfoList = oldGames.Values.ToList();
                    foreach (BasicInfo info in oldInfoList)
                    {
                        string webID = info.WebID;
                        if (!newGames.ContainsKey(webID) && !changeDateGameData.ContainsKey(webID) && !info.OverDayGame && !deleteGameData.ContainsKey(webID))
                        {
                            deleteGameData.Add(webID, info);//比赛信息错误需要删除
                        }
                    }
                    foreach (BasicInfo info in infoList)
                    {
                        string webID = info.WebID;
                        if (!oldGames.ContainsKey(webID))
                        {
                            //if (info.OverDayGame)//跨0点时跨天赛事出现在第二天，原来日期中没有赛事，因此被判断为新增赛事，导致赛事不更新，正确应判断为更新赛事
                            //{
                            //    if (!updateGameData.ContainsKey(gameDate))
                            //    {
                            //        updateGameData[gameDate] = new Dictionary<string, BasicInfo>();
                            //    }
                            //    updateGameData[gameDate].Add(webID, info);//新增更新的比赛信息
                            //    info.Changed = true;
                            //}
                            if (!info.OverDayGame)
                            {
                                if (!addGameData.ContainsKey(gameDate))
                                {
                                    addGameData[gameDate] = new Dictionary<string, BasicInfo>();
                                }
                                addGameData[gameDate].Add(webID, info);//新增一天的某场比赛信息
                            }
                            continue;
                        }
                        BasicInfo oldInfo = oldGames[webID];

                        // 比對新舊資料是否相同 只当有变动的的资料的时候才检查是否正常
                        if (!oldInfo.ToString().Equals(info.ToString()))
                        {
                            if (!updateGameData.ContainsKey(gameDate))
                            {
                                updateGameData[gameDate] = new Dictionary<string, BasicInfo>();
                            }
                            updateGameData[gameDate].Add(webID, info);//新增更新的比赛信息
                            info.Changed = true;
                        }
                        else
                        {
                            info.Changed = false;
                        }

                        newGames[webID] = info;
                    }
                }
                //跨天赛事可能因为没有昨日赛事更新慢，单独拿出来比对
                List<string> oldWebID = oldOverDayGame.Select(p => p.WebID).ToList();
                List<string> newWebID = newOverDayGame.Select(p => p.WebID).ToList();
                newWebID.ForEach(p =>
                {
                    bool isUpdate = false;
                    BasicInfo newGame = newOverDayGame.Single(newgame => newgame.WebID == p);
                    if (!oldWebID.Contains(p))
                    {
                        isUpdate = true;
                    }
                    else
                    {
                        BasicInfo oldGame = oldOverDayGame.Single(old => old.WebID == p);
                        if (!oldGame.ToString().Equals(newGame.ToString()))
                        {
                            isUpdate = true;
                        }
                    }
                    if (isUpdate)
                    {
                        string gameDate = newGame.GameTime.ToString(DATE_STRING_FORMAT);
                        if (!updateGameData.ContainsKey(gameDate))
                        {
                            updateGameData[gameDate] = new Dictionary<string, BasicInfo>();
                        }
                        if (updateGameData[gameDate].ContainsKey(p))
                        {
                            updateGameData[gameDate].Remove(p);
                        }
                        updateGameData[gameDate].Add(p, newGame);//新增更新的比赛信息
                        newGame.Changed = true;
                    }
                });
                oldOverDayGame = newOverDayGame;

                if (changeDateGameData.Count > 0)
                {
                    foreach (KeyValuePair<string, Dictionary<string, BasicInfo>> pair in gameData)
                    {
                        PrintGame(pair.Value, pair.Key, "Before changeDateGameData:" + string.Join(",", changeDateGameData.Values.Select(p => "'" + p.WebID + "'").ToList()));
                    }
                }
                if (deleteGameData.Count > 0)
                {
                    foreach (KeyValuePair<string, Dictionary<string, BasicInfo>> pair in gameData)
                    {
                        PrintGame(pair.Value, pair.Key, "Before deleteGameData:" + string.Join(",", deleteGameData.Values.Select(p => "'" + p.WebID + "'").ToList()));
                    }
                }
                this.gameData = gameList;
                if (changeDateGameData.Count > 0)
                {
                    foreach (KeyValuePair<string, Dictionary<string, BasicInfo>> pair in gameData)
                    {
                        PrintGame(pair.Value, pair.Key, "After changeDateGameData:" + string.Join(",", changeDateGameData.Values.Select(p => "'" + p.WebID + "'").ToList()));
                    }
                }
                if (deleteGameData.Count > 0)
                {
                    foreach (KeyValuePair<string, Dictionary<string, BasicInfo>> pair in gameData)
                    {
                        PrintGame(pair.Value, pair.Key, "After deleteGameData:" + string.Join(",", deleteGameData.Values.Select(p => "'" + p.WebID + "'").ToList()));
                    }
                }
            }
            catch (Exception ex)
            {
                this.Logs.Error("CompareData Error Message:{0},\r\nStackTrace:{1}\r\n", ex.Message, ex.StackTrace);
            }
        }
        private Dictionary<string, BasicInfo> GetTennisData(TennisDownload download)
        {
            string dataSource = download.Data;
           // dataSource = Read(@"D:\TFS_Code\TFSmain_SP_TFS_CODE\AP\Follow\game.txt");
            // 沒有資料就離開
            if (string.IsNullOrEmpty(dataSource)) return null;
            Dictionary<string, BasicInfo> result = new Dictionary<string, BasicInfo>();
            BasicInfo gameInfo = null;
            string[] all = dataSource.Split(new string[] { "¬~ZA÷" }, StringSplitOptions.RemoveEmptyEntries);
            // 聯盟 (第一筆是多餘的)
            int orderBy = 1;
            for (int allianceIndex = 1; allianceIndex < all.Length; allianceIndex++)
            {
                // 比賽集合
                string[] games = ("ZA÷" + all[allianceIndex]).Split(new string[] { "~" }, StringSplitOptions.RemoveEmptyEntries);
                string allianceName = null;
                string gameState = null;
                // 比賽資料
                for (int gameIndex = 0; gameIndex < games.Length; gameIndex++)
                {
                    Dictionary<string, string> info = new Dictionary<string, string>();

                    #region 取出資料
                    string[] data = games[gameIndex].Split(new string[] { "¬" }, StringSplitOptions.RemoveEmptyEntries);
                    // 資料
                    foreach (string d in data)
                    {
                        string[] txt = d.Split(new string[] { "÷" }, StringSplitOptions.RemoveEmptyEntries);
                        // 判斷並記錄
                        if (txt.Length == 2) info[txt[0]] = txt[1];
                    }
                    #endregion
                    #region 第一筆是聯盟
                    if (gameIndex == 0)
                    {
                        allianceName = info["ZA"];
                        if (info["ZF"] == "2")
                        {
                            gameState = "只顯示</br>最終比分";
                        }
                        continue;
                    }
                    else
                    {
                        // 沒有編號就往下處理
                        if (!info.ContainsKey("AA")) continue; 
                    }
                    #endregion

                    // 沒有隊伍就往下處理
                    if (!info.ContainsKey("AE") || !info.ContainsKey("AF")) continue;

                    // 時間是 1970 年加上 Ti
                    DateTime gameTime = DateTime.Parse("1970/1/1 00:00:00").AddTicks(long.Parse(info["AD"]) * 10000000);
                    // 轉成台灣時間 UTC+8
                    gameTime = gameTime.AddHours(8);
                    gameInfo = null;
                    gameInfo = new BasicInfo(this.AllianceID, this.GameType, gameTime, info["AA"], true);
                    #region 設定
                    gameInfo.OverDayGame = false;
                    string runsA = "";
                    string runsB = "";
                    gameInfo.Home = info["AE"].Replace("GOAL", "").Replace("SET", "");
                    gameInfo.Away = info["AF"].Replace("GOAL", "").Replace("SET", "");
                    gameInfo.HomePoint = (info.ContainsKey("AG")) ? (info["AG"]) : ("-");
                    gameInfo.AwayPoint = (info.ContainsKey("AH")) ? (info["AH"]) : ("-");
                    gameInfo.WN = 0;
                    gameInfo.PR = 0;
                    gameInfo.TrackerText = "只有最終結果";
                    gameInfo.GameStates = "";
                    #endregion
                    #region 分數
                    string[] nums = new string[] { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J" };
                    for (int i = 0; i < nums.Length; i += 2)
                    {
                        runsA += "," + ((info.ContainsKey("B" + nums[i])) ? (info["B" + nums[i]]) : (""));
                        runsA += ((info.ContainsKey("D" + nums[i])) ? (string.Format("<sup>{0}</sup>", info["D" + nums[i]])) : (""));
                        runsB += "," + ((info.ContainsKey("B" + nums[i + 1])) ? (info["B" + nums[i + 1]]) : (""));
                        runsB += ((info.ContainsKey("D" + nums[i + 1])) ? (string.Format("<sup>{0}</sup>", info["D" + nums[i + 1]])) : (""));
                        // 盤數
                        if (info.ContainsKey("B" + nums[i]))
                        {
                            gameInfo.GameStates = (i / 2 + 1).ToString();
                        }
                    }
                    // 球數
                    runsA += "," + ((info.ContainsKey("WA") && info["AB"] == "2") ? (info["WA"]) : (""));
                    runsA = runsA.Substring(1);
                    runsB += "," + ((info.ContainsKey("WB") && info["AB"] == "2") ? (info["WB"]) : (""));
                    runsB = runsB.Substring(1);
                    #endregion
                    #region LIVE
                    if ((info.ContainsKey("AN") && info["AN"] == "y") ||
                        (info.ContainsKey("AI") && info["AI"] == "y"))
                    {
                        gameInfo.TrackerText = "即時更新";
                    }
                    #endregion
                    #region 比賽狀態
                    switch (info["AB"])
                    {
                        case "1":
                            gameInfo.GameStates = "未開賽";
                            break;
                        case "2":
                            gameInfo.GameStates = string.Format("第 {0} 盤", gameInfo.GameStates);
                            if (info.ContainsKey("WC"))
                                gameInfo.PR = Convert.ToInt32(info["WC"]);
                            break;
                        case "3":
                            switch (info["AC"])
                            {
                                case "5":
                                    gameInfo.GameStates = "取消";
                                    break;
                                case "8":
                                    gameInfo.GameStates = "結束</br>(比賽中棄權)";
                                    break;
                                case "9":
                                    gameInfo.GameStates = "不戰而勝";
                                    break;
                                case "36":
                                    gameInfo.GameStates = "中斷";
                                    break;
                                default:
                                    gameInfo.GameStates = "結束";
                                    break;
                            }

                            #region 輸贏
                            int teamA = 0;
                            int teamB = 0;
                            if (int.TryParse(gameInfo.HomePoint.ToString(), out teamA) &&
                                int.TryParse(gameInfo.AwayPoint.ToString(), out teamB))
                            {
                                if (teamA > teamB) gameInfo.WN = 1;
                                if (teamA < teamB) gameInfo.WN = 2;
                            }
                            #endregion
                            break;
                    }
                    gameInfo.AllianceName = allianceName;
                    gameInfo.HomeBoard = runsA.Split(',').ToList();
                    gameInfo.AwayBoard = runsB.Split(',').ToList();
                    //最後修正只顯示最終比分的賽事狀態，有些結束賽事的ZF字段也為2，需要排除這種結束賽事
                    if (gameState != null && info["AB"] !="2" && info["AB"] != "3" && info["AC"] != "3")
                    {
                        gameInfo.GameStates = gameState;
                    }
                    #endregion
                    #region 排序
                    gameInfo.OrderBy = orderBy;
                    #endregion
                    // 加入
                    result[gameInfo.WebID] = gameInfo;
                }
                orderBy++;
            }
            // 傳回
            if (result.Count > 0)
            {
                DateTime maxGameTime = result.Values.Max(p => p.GameTime);//取最大开赛时间 作为比赛日期
                download.GameDate = maxGameTime.Date;
                List<BasicInfo> overDayGames = result.Values.Where(p => p.GameTime.Date < maxGameTime.Date).ToList();//比赛日期小于最大开赛时间的日期的为跨天赛事
                StringBuilder sb = new StringBuilder("OverDayGame:\r\n");
                overDayGames.ForEach(p =>
                {
                    p.OverDayGame = true;
                    sb.Append(p.ToString() + "\r\n");
                });
                if (overDayGames.Count > 0)
                {
                    this.Logs.GameInfo(sb.ToString());
                }
                PrintGame(result, download.GameDate.ToString(DATE_STRING_FORMAT), "DownGame", true);
            }
            return result;
        }
        //public string Read(string path)
        //{
        //    StringBuilder sb = new StringBuilder();
        //    StreamReader sr = new StreamReader(path, Encoding.UTF8);
        //    String line;
        //    while ((line = sr.ReadLine()) != null)
        //    {
        //        sb.Append(line);
        //    }
        //    return sb.ToString();
        //}
        private void PrintGame(Dictionary<string, BasicInfo> game, string gameDate, string printMessage, bool isDown = false)
        {
            StringBuilder sb = new StringBuilder("\r\n" + printMessage + "\r\n");
            sb.Append("GameDate:" + gameDate + "\r\n");
            sb.Append("GameWebID:" + string.Join(",", game.Values.Select(p => "'" + p.WebID + "'")) + "\r\n");
            sb.Append("--------------------------------------------------\r\n");
            foreach (var item in game.Values)
            {
                sb.Append(item.ToString() + "\r\n");
                sb.Append("--------------------------------------------------\r\n");
            }
            if (isDown)
            {
                this.Logs.DownGameInfo(sb.ToString());
            }
            else
            {
                this.Logs.GameInfo(sb.ToString());
            }
        }
        #region 讀取資料

        private void LoadNameControlAndAlliance()
        {
            // 正在讀取資料時 不處理
            if (_onDataLoading) { return; }

            _onDataLoading = true;

            string sql = "SELECT * FROM NameControl WITH (NOLOCK)"
                + " WHERE (GTLangx = N'TN')"
                + "   AND (GameType = N'Name')"
                + "ORDER BY Indexs, LEN(SourceText) DESC";
            this.LoadData(frmMain.ConnectionString, sql, ref dtNameControlForUser);


            sql = @"SELECT AllianceID, AllianceName, ShowName FROM TNAlliance WITH (NOLOCK)";
            this.LoadData(frmMain.ConnectionString, sql, ref dtAlliance);

            _onDataLoading = false;
        }

        // 讀取資料
        private void LoadData(string connectionString, string sql, ref DataTable dt)
        {
            if (dt != null)
                dt.Dispose();

            dt = null;
            dt = new DataTable();

            SqlConnection conn = new SqlConnection(connectionString);
            SqlDataAdapter da = new SqlDataAdapter(sql, conn);
            // 錯誤處理
            try
            {
                // 開啟
                conn.Open();
                // 讀取
                da.Fill(dt);
                dt.PrimaryKey = new DataColumn[] { dt.Columns["ID"] };

                // 關閉
                conn.Close();
                da.Dispose();
            }
            catch (Exception ex)
            {
                this.Logs.Error("LoadData Error Message:{0},\r\nStackTrace:{1}\r\n", ex.Message, ex.StackTrace);
            }
            conn = null;
            da = null;
        }

        #endregion

        #region 新增聯盟、队伍資料

        /// <summary>
        /// 新增聯盟、队伍資料
        /// </summary>
        /// <param name="allianceList">聯盟清單 (Key: 聯盟名稱)</param>
        private void AddAllianceAndTeamToTable(Dictionary<string, List<string>> teamData)
        {
            if (teamData.Count == 0) return;
            bool updated = false;

            //  using (TransactionScope scope = new TransactionScope())
            //  {
            using (SqlConnection conn = new SqlConnection(frmMain.ConnectionString))
            {
                try
                {
                    conn.Open();
                    foreach (KeyValuePair<string, List<string>> data in teamData)
                    {
                        string allianceName = data.Key;
                        List<string> teams = data.Value;

                        // 新增聯盟
                        int allianceID;
                        updated |= AddAllianceData(conn, allianceName, out allianceID);

                        // 新增隊伍
                        foreach (string teamName in teams)
                        {
                            int teamID;
                            updated |= AddTeamData(conn, teamName, out teamID);
                        }
                    }
                }
                catch (Exception ex)
                {
                    this.Logs.Error("AddAllianceAndTeamToTable Error Message:{0},\r\nStackTrace:{1}\r\n", ex.Message, ex.StackTrace);
                }
            }

            // 資料有更新, 重新取得資料
            if (updated) { this.LoadNameControlAndAlliance(); }
        }

        //更新显示名称，因为原来网球一个队名分两次存储，查询、写入消耗性能，
        //现在改为一次存储，这段代码是用原来的显示名称,校正现有的显示名称,避免手动修改
        private string UpdateTeamShowName(SqlConnection conn, string teamName)
        {
            string sql = string.Format(@"
 declare @sourceText nvarchar(100)='{0}',@index int,@showname nvarchar(100)
 begin
   set @index= charindex('(',@sourceText)
   if @index > 0
      begin
      if exists(select id from NameControl  WITH (NOLOCK) WHERE (GTLangx = N'TN') AND (GameType = N'Name') and sourceText=  ltrim(rtrim(substring(@sourceText,0,@index))))
      	begin
          select @showname = changeText+' '+substring(@sourceText,@index,len(@sourceText)-@index+1) from NameControl  WITH (NOLOCK) WHERE (GTLangx = N'TN') AND (GameType = N'Name') and sourceText=ltrim(rtrim(substring(@sourceText,0,@index)))
	    end
	  else
	   begin
	     set @showname=@sourceText
	   end
	  end
	else   
	   begin
	     set @index= charindex('/',@sourceText)
		   if @index > 0
		   begin
		    set @showname=''
		      if exists(select id  from NameControl  WITH (NOLOCK) WHERE (GTLangx = N'TN') AND (GameType = N'Name') and sourceText=ltrim(rtrim(substring(@sourceText,0,@index))))
	             begin
	               set @showname=@showname+( select top 1 changeText  from NameControl  WITH (NOLOCK) WHERE (GTLangx = N'TN') AND (GameType = N'Name') and sourceText=  ltrim(rtrim(substring(@sourceText,0,@index))))
			     end
	          else
	             begin
	               set @showname=@showname+ltrim(rtrim(substring(@sourceText,0,@index)))
	             end
              set @showname=@showname+'/'
              if exists(select id  from NameControl  WITH (NOLOCK) WHERE (GTLangx = N'TN') AND (GameType = N'Name') and sourceText= ltrim(rtrim(substring(@sourceText,@index+1,len(@sourceText)-@index+1))))
	             begin
	               set @showname=@showname+( select top 1 changeText  from NameControl  WITH (NOLOCK) WHERE (GTLangx = N'TN') AND (GameType = N'Name') and sourceText=  ltrim(rtrim(substring(@sourceText,@index+1,len(@sourceText)-@index+1))))
	             end
	          else
	            begin
	              set @showname=@showname+ltrim(rtrim(substring(@sourceText,@index+1,len(@sourceText)-@index+1)))
	            end
		   end
		   else
		      begin
			    set @showname=@sourceText
			  end
	   end
      select @showname
 end", teamName);
            SqlCommand cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            object o = cmd.ExecuteScalar();
            return o.ToString();
        }
        private bool AddTeamData(SqlConnection conn, string teamName, out int teamID)
        {
            teamID = 0;
            bool update = false;

            // 檢查暫存表中是否有資料
            var q = from DataRow x in dtNameControlForUser.AsEnumerable()
                    where x.Field<string>("SourceText").Equals(teamName)
                    select x.Field<int>("ID");

            // 已存在資料, 直接取得隊伍 ID
            if (q.Any())
            {
                teamID = q.First();
            }
            else
            {
                // 新增隊伍資料
                string sql = @"IF EXISTS(SELECT ID FROM NameControl WITH (NOLOCK) WHERE (GTLangx = N'TN') AND (GameType = N'Name') AND SourceText= @TeamName)
                                BEGIN
	                                SELECT ID FROM NameControl WITH (NOLOCK) WHERE (GTLangx = N'TN') AND (GameType = N'Name') AND SourceText= @TeamName
                                END
                                ELSE
                                BEGIN
	                                INSERT INTO NameControl([Category], [Langx],[GameType],[GTLangx],[AppType],[SourceText],[ChangeText])
	                                VALUES(2,'en', 'Name', 'TN','First',@TeamName,@ShowName);
	                                SELECT SCOPE_IDENTITY() AS ID;
                                END";

                SqlCommand cmd = conn.CreateCommand();
                cmd.CommandText = sql;
                cmd.Parameters.Add("@TeamName", SqlDbType.NVarChar).Value = teamName;
                cmd.Parameters.Add("@ShowName", SqlDbType.NVarChar).Value = UpdateTeamShowName(conn, teamName);
                object o = cmd.ExecuteScalar();
                update = true;
            }

            return update;
        }

        /// <summary>
        /// 新增聯盟資料
        /// </summary>
        /// <param name="conn">資料庫連線</param>
        /// <param name="allianceName">聯盟名稱</param>
        /// <param name="allianceID">聯盟編號</param>
        /// <returns>是否更新</returns>
        private bool AddAllianceData(SqlConnection conn, string allianceName, out int allianceID)
        {
            allianceID = 0;
            bool update = false;

            // 檢查暫存表中是否有資料
            var q = from DataRow x in dtAlliance.AsEnumerable()
                    where x.Field<string>("AllianceName").Equals(allianceName)
                    select x.Field<int>("AllianceID");

            // 已存在資料, 直接取得聯盟 ID
            if (q.Any())
            {
                allianceID = q.First();
            }
            else
            {
                // 新增聯盟資料
                string sql = @"IF EXISTS(SELECT AllianceID FROM TNAlliance WITH (NOLOCK) WHERE AllianceName = @AllianceName)
                                BEGIN
	                                SELECT AllianceID FROM TNAlliance WITH (NOLOCK) WHERE AllianceName = @AllianceName;
                                END
                                ELSE
                                BEGIN
	                                INSERT INTO TNAlliance(AllianceName, ShowName)
	                                VALUES(@AllianceName, @AllianceName);
	                                SELECT SCOPE_IDENTITY() AS AllianceID;
                                END";

                SqlCommand cmd = conn.CreateCommand();
                cmd.CommandText = sql;
                cmd.Parameters.Add("@AllianceName", SqlDbType.NVarChar, 100).Value = allianceName;
                object o = cmd.ExecuteScalar();
                allianceID = Convert.ToInt32(o);
                update = true;
            }

            return update;
        }

        #endregion


    }
    public class TennisDownload : BasicDownload
    {
        // 更新時間
        public int UpdateSecond { set; get; }

        // 抓取賽事時間
        public DateTime GameDate { set; get; }

        // 建構式
        public TennisDownload(ESport sport, DateTime gameDate, string url, string fileType)
            : base(sport, url, fileType)
        {
            this.GameDate = gameDate;
        }
    }
}
