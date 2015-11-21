using Follow.Sports.Basic;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Follow.Sports
{
    public class IhBF : Basic.BasicFB
    {
        #region 成員變數
        // 下載資料
        private IHDownload DownHome;
        private List<IHDownload> DownReal;
        private DateTime DownLastTime = DateTime.Now;
        private Dictionary<string, string> DownHomeHeader;

        private DataTable dtTeam;
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
        private int playMinutes = 20;
        private int startIndex = -1;
        private int endIndex = 7;
        private List<BasicInfo> oldOverDayGame = new List<BasicInfo>();
        private List<BasicInfo> newOverDayGame = new List<BasicInfo>();
        #endregion
        private string sWebUrl1 = UrlSetting.GetUrl(ESport.Hockey_IHBF, "Url1");
        private string sWebUrl2 = UrlSetting.GetUrl(ESport.Hockey_IHBF, "Url2");
        private string sWebUrl3 = UrlSetting.GetUrl(ESport.Hockey_IHBF, "Url3");
        public IhBF(DateTime today)
            : base(ESport.Hockey_IHBF)
        {
            if (string.IsNullOrWhiteSpace(this.sWebUrl))
            {
                this.sWebUrl = "http://d.flashscore.com/x/feed/f_4_0_8_asia_1";
            }
            if (string.IsNullOrWhiteSpace(this.sWebUrl1))
            {
                this.sWebUrl1 = "http://d.flashscore.com/x/feed/proxy";
            }
            if (string.IsNullOrWhiteSpace(this.sWebUrl2))
            {
                this.sWebUrl2 = "http://d.flashscore.com/x/feed/f_4_-1_8_asia_1";
            }
            if (string.IsNullOrWhiteSpace(this.sWebUrl3))
            {
                this.sWebUrl3 = "http://d.flashscore.com/x/feed/f_4_";
            }
            this.Logs = new LogFile(ESport.Hockey_IHBF);//設定log type
            // 設定
            this.AllianceID = 0;
            this.GameType = "IHBF";
            this.GameDate = GetUtcTw(today).Date; // 只取日期
            this.DownHome = new IHDownload(this.Sport, this.GameDate, sWebUrl, "f_4_0_8_asia_1");
            this.DownHomeHeader = new Dictionary<string, string>();
            this.DownHomeHeader["Accept"] = "*/*";
            this.DownHomeHeader["Accept-Charset"] = "utf-8;q=0.7,*;q=0.3";
            this.DownHomeHeader["Accept-Encoding"] = "gzip,deflate,sdch";
            this.DownHomeHeader["Accept-Language"] = "*";
            this.DownHomeHeader["X-Fsign"] = "SW9D1eZo";
            this.DownHomeHeader["X-GeoIP"] = "1";
            this.DownHomeHeader["X-utime"] = "1";
            this.DownHomeHeader["Cookie"] = "__utma=175935605.237435887.1433729535.1433729535.1433732345.2; __utmb=175935605.2.10.1433732345; __utmc=175935605; __utmz=175935605.1433732345.2.2.utmcsr=flashscore.com|utmccn=(referral)|utmcmd=referral|utmcct=/tennis/; __utmt=1; __gads=ID=95d4003915b743c6:T=1433729537:S=ALNI_MYpkSaA3-m9gggcZp-An3QTk6uUOg";
            this.DownHomeHeader["Host"] = "d.flashscore.com";
            this.DownHomeHeader["Referer"] = this.sWebUrl1;
            // 昨天
            this.DownReal = new List<IHDownload>();
            this.DownReal.Add(new IHDownload(this.Sport, this.GameDate.AddDays(startIndex), this.sWebUrl2, "f_4_-1_8_asia_1"));
            // 往後 7 天
            for (int i = 1; i <= endIndex; i++)
            {
                this.DownReal.Add(new IHDownload(this.Sport, this.GameDate.AddDays(i), sWebUrl3 + i + "_8_asia_1", "f_4_" + i + "_8_asia_1"));
            }
            // 讀取隊伍表/聯盟表
            this.LoadTeamAndAlliance();

            #region 從資料庫中取得舊資料

            GetOldGames();

            #endregion 從資料庫中取得舊資料
        }
        protected DataTable GetIceHockeyData(string connectionString, DateTime startTime, DateTime endDate)
        {
            string sql = @"SELECT [GID]
      ,[OrderBy]
      ,ih.[GameType]
      ,ih.[AllianceID]
      ,(select [AllianceName] from [IceHockeyAlliance] where [GameType]=@GameType and [AllianceID]=ih.[AllianceID])as alliancename
      ,[Number]
      ,[GameDate]
      ,[GameTime]
      ,[GameStates]
      ,(select [TeamName] from [IceHockeyTeam] where [GameType]=@GameType and [TeamID]=ih.[TeamAID])as home
      ,(select [TeamName] from [IceHockeyTeam] where [GameType]=@GameType and [TeamID]=ih.[TeamBID])as away
      ,[TeamAID]
      ,[TeamBID]
      ,[CtrlStates]
      ,[CtrlAdmin]
      ,[RunsA]
      ,[RunsB]
      ,[RA]
      ,[RB]
      ,[WebID]
      ,[TrackerText]
      ,[StatusText]
      ,ih.[Display]
      ,[Record]
      ,[ChangeCount]
      ,[ShowJS]
  FROM  [IceHockeySchedules] as ih WITH (NOLOCK)
  WHERE ih.[GameDate] between @GameStartDate and @GameEndDate AND ih.[GameType] = @GameType";
            DataTable table = new DataTable();
            try
            {
                table = ExecuteDataTable(connectionString, sql, new SqlParameter("@GameStartDate", startTime.Date), new SqlParameter("@GameEndDate", endDate.Date), new SqlParameter("@GameType", GameType));
            }
            catch { }
            return table;
        }

        private void GetOldGames()
        {
            DataTable dt = this.GetIceHockeyData(frmMain.ConnectionString, this.GameDate.AddDays(startIndex), this.GameDate.AddDays(endIndex));
            foreach (DataRow dr in dt.Rows)
            {
                int allianceid = dr.Field<int>("AllianceID");
                string webID = dr.Field<string>("WebID");
                string gameDate = dr.Field<DateTime>("GameDate").ToString(DATE_STRING_FORMAT);
                DateTime gameTime = DateTime.Parse(gameDate + " " + dr.Field<TimeSpan>("GameTime").ToString(@"hh\:mm"));
                BasicInfo basic = new BasicInfo(allianceid, this.GameType, gameTime, webID);
                basic.AllianceName = dr.Field<string>("alliancename");
                basic.GameStates = dr.Field<string>("GameStates");
                basic.Status = dr.Field<string>("StatusText");
                basic.Home = dr.Field<string>("home");
                basic.Away = dr.Field<string>("away");
                basic.HomePoint = dr.Field<int>("RA").ToString();
                basic.AwayPoint = dr.Field<int>("RB").ToString();
                basic.HomeBoard = dr.Field<string>("RunsA") != null ? dr.Field<string>("RunsA").Split(',').ToList() : null;
                basic.AwayBoard = dr.Field<string>("RunsB") != null ? dr.Field<string>("RunsB").Split(',').ToList() : null;
                basic.TrackerText = dr.Field<string>("TrackerText");
                basic.Record = dr.Field<string>("Record");
                basic.OrderBy = dr.Field<int>("OrderBy");
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
            Dictionary<string, BasicInfo> todayGame = this.GetDataByAsiaScore(this.DownHome, playMinutes);
            Dictionary<string, BasicInfo> yestodayGame = this.GetDataByAsiaScore(this.DownReal[0], playMinutes);
            newOverDayGame = new List<BasicInfo>();
            if (todayGame != null && todayGame.Count > 0 && this.DownHome.GameDate.Date==time.Date)
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
                Dictionary<string, BasicInfo> gameByDate = this.GetDataByAsiaScore(this.DownReal[i], playMinutes);
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
                sb.AppendFormat("DELETE FROM [IceHockeySchedules] WHERE webid IN({0}) AND GameType='{1}'\r\n", string.Join(",", list), this.GameType);
                sb.AppendFormat("end\r\n");
                result += ExecuteScalar(connectionString, sb.ToString());
                this.Logs.Update("deleteGameData:\r\n" + sb.ToString());
            }
            sb = new StringBuilder();
            if (changeDateGameData.Count > 0)
            {
                List<string> list = changeDateGameData.Values.Select(p => "'" + p.WebID + "'").ToList();
                sb.AppendFormat("begin\r\n");
                sb.AppendFormat("DELETE FROM [IceHockeySchedules] WHERE webid IN({0}) AND GameType='{1}' AND CtrlStates in(0,2)\r\n", string.Join(",", list), this.GameType);
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
                    sb.AppendFormat("select @webid=webid from [IceHockeySchedules] with(nolock) where webid='{0}' AND GameType='{1}'\r\n", game.WebID, game.GameType);
                    sb.AppendFormat("if @webid is null\r\n");
                    sb.AppendFormat("begin\r\n");
                    sb.AppendFormat("select @allianceid=allianceid from [IceHockeyAlliance] with(nolock) where alliancename='{0}' and gametype='{1}'\r\n", game.AllianceName, GameType);
                    sb.AppendFormat("SELECT @teamaid=teamid FROM [IceHockeyTeam] WITH (NOLOCK) WHERE TeamName='{0}' and gametype='{1}'\r\n", game.Home, GameType);
                    sb.AppendFormat("SELECT @teambid=teamid FROM [IceHockeyTeam] WITH (NOLOCK) WHERE TeamName='{0}' and gametype='{1}'\r\n", game.Away, GameType);
                    sb.AppendFormat("INSERT INTO [IceHockeySchedules] ([AllianceID],[GameDate],[GameTime],[GameStates],[TeamAID],[TeamBID],[RunsA],[RunsB],[RA],[RB],[WebID],[TrackerText],[StatusText],[GameType],[Display]) VALUES ({0},'{1}','{2}','{3}',{4},{5},{6},{7},'{8}','{9}','{10}',{11},'{12}','{13}',{14})\r\n",
                    "@allianceid",
                    game.GameTime.ToString(DATE_STRING_FORMAT),
                    game.GameTime.ToString("HH:mm"),
                    game.GameStates, "@teamaid", "@teambid",
                    game.HomeBoard.Count > 0 ? "'" + string.Join(",", game.HomeBoard) + "'" : "NULL",
                    game.AwayBoard.Count > 0 ? "'" + string.Join(",", game.AwayBoard) + "'" : "NULL",
                    game.HomePoint,
                    game.AwayPoint,
                    game.WebID,
                    game.TrackerText != null ? "'" + game.TrackerText + "'" : "NULL",
                    game.Status,
                    game.GameType,
                    game.Display
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
            sb.AppendFormat("declare @ctrlStates int\r\n");
            foreach (KeyValuePair<string, Dictionary<string, BasicInfo>> item in updateGameData)
            {
                sb.AppendFormat("begin\r\n");
                string gameDate = item.Key;
                Dictionary<string, BasicInfo> basic = item.Value;
                foreach (KeyValuePair<string, BasicInfo> pair in basic)
                {
                    BasicInfo game = pair.Value;
                    sb.AppendFormat("select @ctrlStates=CtrlStates from [IceHockeySchedules] with(nolock) where webid='{0}'\r\n", game.WebID);
                    sb.AppendFormat("IF @ctrlStates=0 OR @ctrlStates=2\r\n");
                    sb.AppendFormat("begin\r\n");
                    sb.AppendFormat("UPDATE [IceHockeySchedules] SET [GameStates] ='{0}',[RunsA] ={1},[RunsB] = {2},[RA] = '{3}',[RB] = '{4}',[TrackerText] ={5},[GameDate]='{6}',[GameTime]='{7}',[ChangeCount] =[ChangeCount]+1,[StatusText]='{8}' where webid='{9}' AND GameType='{10}'\r\n",
                    game.GameStates,
                    game.HomeBoard.Count > 0 ? "'" + string.Join(",", game.HomeBoard) + "'" : "NULL",
                    game.AwayBoard.Count > 0 ? "'" + string.Join(",", game.AwayBoard) + "'" : "NULL",
                    game.HomePoint, game.AwayPoint,
                    game.TrackerText != null ? "'" + game.TrackerText + "'" : "NULL",
                    game.GameTime.ToString(DATE_STRING_FORMAT),
                    game.GameTime.ToString(TIME_STRING_FORMAT),
                    game.Status,
                    game.WebID,
                    game.GameType);
                    sb.AppendFormat("end\r\n");
                    sb.AppendFormat("ELSE IF @ctrlStates=1\r\n");
                    sb.AppendFormat("begin\r\n");
                    sb.AppendFormat("UPDATE [IceHockeySchedules] SET [GameStates] ='{0}',[TrackerText] ={1},[StatusText] = {2},[Record]={3},[GameDate]='{4}',[GameTime]='{5}',[ChangeCount] =[ChangeCount]+1 where webid='{6}' and gametype='{7}'\r\n",
                    game.GameStates,
                    game.TrackerText != null ? "'" + game.TrackerText + "'" : "NULL",
                    game.Status != null ? "'" + game.Status + "'" : "NULL",
                    game.Record != null ? "'" + game.Record + "'" : "NULL",
                    game.GameTime.ToString(DATE_STRING_FORMAT),
                    game.GameTime.ToString(TIME_STRING_FORMAT),
                    game.WebID, game.GameType);
                    sb.AppendFormat("end\r\n");
                    sb.AppendFormat("ELSE IF @ctrlStates=3\r\n");
                    sb.AppendFormat("begin\r\n");
                    sb.AppendFormat("UPDATE [IceHockeySchedules] SET [RunsA] ={0},[RunsB] = {1},[RA] = {2},[RB] = {3},[StatusText] = {4},[ChangeCount] =[ChangeCount]+1 where webid='{5}' and gametype='{6}'\r\n",
                     game.HomeBoard.Count > 0 ? "'" + string.Join(",", game.HomeBoard) + "'" : "NULL",
                     game.AwayBoard.Count > 0 ? "'" + string.Join(",", game.AwayBoard) + "'" : "NULL",
                     game.HomePoint, game.AwayPoint,
                     game.Status != null ? "'" + game.Status + "'" : "NULL",
                     game.WebID, game.GameType);
                    sb.AppendFormat("end\r\n");
                }
                sb.AppendFormat("end\r\n");
                result += ExecuteScalar(connectionString, sb.ToString());
                this.Logs.Update("updateGameData:\r\n" + sb.ToString());
                sb = new StringBuilder();
                sb.AppendFormat("declare @ctrlStates int\r\n");
            }
            sb = new StringBuilder();
            if (updateGameData.Count > 0 || addGameData.Count > 0 || deleteGameData.Count > 0 || changeDateGameData.Count > 0)
            {
                foreach (KeyValuePair<string, Dictionary<string, BasicInfo>> item in this.gameData)
                {
                    sb.AppendFormat("begin\r\n");
                    string key = item.Key;
                    bool isUpdate = false;
                    Dictionary<string, BasicInfo> basic = item.Value;
                    foreach (KeyValuePair<string, BasicInfo> pair in basic)
                    {
                        BasicInfo game = pair.Value;
                        if (game.GameTime.ToString(DATE_STRING_FORMAT) == key && !game.OverDayGame)
                        {
                            sb.AppendFormat("UPDATE [IceHockeySchedules] SET [OrderBy] ={0} where webid='{1}' AND  GameType='{2}'\r\n", game.OrderBy, game.WebID, this.GameType);
                            isUpdate = true;
                        }
                    }
                    sb.AppendFormat("end\r\n");
                    if (isUpdate)
                    {
                        ExecuteScalar(connectionString, sb.ToString());
                        this.Logs.Update("OrderBy:\r\n" + sb.ToString());
                    }
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
                                info.Display = 0;
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

        /// <summary>
        /// 取得資料。(Asiascore)
        /// </summary>
        /// <param name="data">資料</param>
        /// <param name="playMinute">比賽的時間長度</param>
        protected Dictionary<string, BasicInfo> GetDataByAsiaScore(IHDownload download, int playMinute)
        {
            string html = download.Data;
            // 沒有資料就離開
            if (string.IsNullOrEmpty(html))
                return null;
            Dictionary<string, BasicInfo> result = new Dictionary<string, BasicInfo>();
            BasicInfo gameInfo = null;
            // 判斷資料
            string[] all = html.Split(new string[] { "¬~ZA÷" }, StringSplitOptions.RemoveEmptyEntries);

            // 聯盟 (第一筆是多餘的)
            int orderBy = 1;
            for (int allianceIndex = 1; allianceIndex < all.Length; allianceIndex++)
            {
                // 比賽集合
                string[] games = ("ZA÷" + all[allianceIndex]).Split(new string[] { "~" }, StringSplitOptions.RemoveEmptyEntries);
                string allianceName = null;
                string trackerText = null;
                // 聯盟資料
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
                        // 記錄
                        info[txt[0]] = txt[1];
                    }
                    #endregion
                    #region 第一筆是聯盟
                    if (gameIndex == 0)
                    {
                        allianceName = info["ZA"];
                        if (info["ZF"] == "2")
                        {
                            trackerText = "只顯示最終比分";
                        }
                        continue;
                    }
                    else
                    {
                        // 沒有編號就往下處理
                        if (!info.ContainsKey("AA"))
                            continue;
                    }
                    #endregion

                    // 沒有隊伍就往下處理
                    if (!info.ContainsKey("AE") || !info.ContainsKey("AF"))
                        continue;

                    // 時間是 1970 年加上 Ti
                    DateTime gameTime = DateTime.Parse("1970/1/1 00:00:00").AddTicks(long.Parse(info["AD"]) * 10000000);
                    // 轉成台灣時間 UTC+8
                    gameTime = gameTime.AddHours(8);


                    // 建立比賽資料
                    gameInfo = null;
                    gameInfo = new BasicInfo(this.AllianceID, this.GameType, gameTime, info["AA"]);
                    gameInfo.OverDayGame = false;
                    gameInfo.Home = info["AE"].Replace("GOAL", "").Replace("'", "''");
                    gameInfo.Away = info["AF"].Replace("GOAL", "").Replace("'", "''");
                    // 2014/04/18, 新增聯盟(含賽別)
                    gameInfo.AllianceName = allianceName.Replace("'", "''");
                    gameInfo.AwayPoint = "0";
                    gameInfo.HomePoint = "0";
                    gameInfo.TrackerText = "";
                    #region 分數
                    if (info.ContainsKey("AG"))
                    {
                        string[] nums = new string[] { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J" };
                        for (int i = 0; i < nums.Length; i += 2)
                        {
                            // 沒有資料就離開
                            if (!info.ContainsKey("B" + nums[i]))
                                break;
                            // 分數
                            gameInfo.HomeBoard.Add(info["B" + nums[i]]);
                            gameInfo.AwayBoard.Add(info["B" + nums[i + 1]]);
                        }
                        gameInfo.AwayPoint = info["AH"];
                        gameInfo.HomePoint = info["AG"];
                    }
                    #endregion
                    #region 比賽狀態
                    switch (info["AB"])
                    {
                        case "2":
                            gameInfo.GameStates = "S";
                            // 狀態
                            if (info.ContainsKey("AC") &&
                                (info["AC"] == "38" ||
                                 info["AC"] == "46"))
                            {
                                gameInfo.Status = "中場休息";
                            }
                            if (info.ContainsKey("BX"))
                            {
                                // 剩餘時間
                                gameInfo.Status = (playMinute - int.Parse(info["BX"])).ToString();
                            }

                            break;
                        case "3":
                            if (info.ContainsKey("AC"))
                            {
                                switch (info["AC"])
                                {
                                    case "4":
                                    case "5":
                                    case "36":
                                        gameInfo.GameStates = "P";
                                        gameInfo.Status = "中止";
                                        break;
                                    default:
                                        gameInfo.GameStates = "E";
                                        gameInfo.Status = "結束";
                                        break;
                                }
                            }
                            else
                            {
                                gameInfo.GameStates = "E";
                                gameInfo.Status = "結束";
                            }
                            break;
                    }
                    //最後修正只顯示最終比分的賽事狀態，有些結束賽事的ZF字段也為2，需要排除這種結束賽事
                    if (trackerText != null && info["AB"] != "3" && info["AC"] != "3")
                    {
                        gameInfo.TrackerText = trackerText;
                    }
                    #endregion
                    //if (gameInfo.GameStates == "S")
                    //{
                    //    int num = 0;
                    //    // 加時
                    //    if (gameInfo.Quarter == 4)
                    //    {
                    //        if (!string.IsNullOrEmpty(gameInfo.Status) &&
                    //            int.TryParse(gameInfo.Status, out num))
                    //        {
                    //            num = 20 - num;                             // 還原時間
                    //            gameInfo.Status = (5 - num).ToString();   // 經過時間
                    //        }
                    //    }
                    //}
                    // 加入
                    #region 排序
                    gameInfo.OrderBy = orderBy;
                    #endregion
                    gameInfo.Display = 1;
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
                PrintGame(result, download.GameDate.ToString(DATE_STRING_FORMAT), "DownGame",true);
            }
            return result;
        }
        private void PrintGame(Dictionary<string, BasicInfo> game ,string gameDate,string printMessage,bool isDown=false)
        {
            StringBuilder sb = new StringBuilder("\r\n" + printMessage + "\r\n");
            sb.Append("GameDate:" + gameDate + "\r\n");
            sb.Append("GameWebID:" + string.Join(",",game.Values.Select(p=>"'"+p.WebID+"'")) + "\r\n");
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


        private void LoadTeamAndAlliance()
        {
            // 正在讀取資料時 不處理
            if (_onDataLoading) { return; }

            _onDataLoading = true;

            string sql = string.Format("SELECT [TeamID],[TeamName],[ShowName],[AllianceID] FROM [IceHockeyTeam]  WITH (NOLOCK) Where GameType='{0}'", GameType);
            this.LoadData(frmMain.ConnectionString, sql, ref dtTeam);


            sql = string.Format("SELECT AllianceID, AllianceName, ShowName FROM [IceHockeyAlliance] WITH (NOLOCK)  Where GameType='{0}'", GameType);
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
                            updated |= AddTeamData(conn, teamName, allianceID, out teamID);
                        }
                    }
                }
                catch (Exception ex)
                {
                    this.Logs.Error("AddAllianceAndTeamToTable Error Message:{0},\r\nStackTrace:{1}\r\n", ex.Message, ex.StackTrace);
                }
            }

            // 資料有更新, 重新取得資料
            if (updated) { this.LoadTeamAndAlliance(); }
        }
        private bool AddTeamData(SqlConnection conn, string teamName, int allianceID, out int teamID)
        {
            teamID = 0;
            bool update = false;

            // 檢查暫存表中是否有資料
            var q = from DataRow x in dtTeam.AsEnumerable()
                    where x.Field<string>("TeamName").Equals(teamName) && x.Field<int>("AllianceID").Equals(allianceID)
                    select x.Field<int>("TeamID");

            // 已存在資料, 直接取得隊伍 ID
            if (q.Any())
            {
                teamID = q.First();
            }
            else
            {
                // 新增隊伍資料
                string sql = @"IF EXISTS(SELECT 1 FROM [IceHockeyTeam] WITH (NOLOCK) WHERE [TeamName]= @TeamName and GameType=@GameType and AllianceID = @AllianceID)
                                BEGIN
	                                SELECT TeamID FROM [IceHockeyTeam] WITH (NOLOCK) WHERE [TeamName]= @TeamName and GameType=@GameType and AllianceID = @AllianceID
                                END
                                ELSE
                                BEGIN
	                                INSERT INTO IceHockeyTeam([AllianceID],[TeamName], [ShowName],GameType)
	                                VALUES(@AllianceID,@TeamName,@ShowName,@GameType);
	                                SELECT SCOPE_IDENTITY() AS ID;
                                END";

                SqlCommand cmd = conn.CreateCommand();
                cmd.CommandText = sql;
                cmd.Parameters.Add("@TeamName", SqlDbType.NVarChar).Value = teamName;
                cmd.Parameters.Add("@ShowName", SqlDbType.NVarChar).Value = teamName;
                cmd.Parameters.Add("@AllianceID", SqlDbType.NVarChar).Value = allianceID;
                cmd.Parameters.Add("@GameType", SqlDbType.NVarChar, 10).Value = GameType;
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
                string sql = @"IF EXISTS(SELECT 1 FROM IceHockeyAlliance WITH (NOLOCK) WHERE AllianceName = @AllianceName AND GameType=@GameType)
                                BEGIN
	                                SELECT AllianceID FROM IceHockeyAlliance WITH (NOLOCK) WHERE AllianceName = @AllianceName AND GameType=@GameType;
                                END
                                ELSE
                                BEGIN
	                                INSERT INTO IceHockeyAlliance(AllianceName, ShowName,GameType,Lever)
	                                VALUES(@AllianceName, @AllianceName,@GameType,1);
	                                SELECT SCOPE_IDENTITY() AS AllianceID;
                                END";

                SqlCommand cmd = conn.CreateCommand();
                cmd.CommandText = sql;
                cmd.Parameters.Add("@AllianceName", SqlDbType.NVarChar, 100).Value = allianceName;
                cmd.Parameters.Add("@GameType", SqlDbType.NVarChar, 10).Value = GameType;
                object o = cmd.ExecuteScalar();
                allianceID = Convert.ToInt32(o);
                update = true;
            }

            return update;
        }

        #endregion

    }
    public class IHDownload : BasicDownload
    {
        // 更新時間
        public int UpdateSecond { set; get; }

        // 抓取賽事時間
        public DateTime GameDate { set; get; }

        // 建構式
        public IHDownload(ESport sport, DateTime gameDate, string url, string fileType)
            : base(sport, url, fileType)
        {
            this.GameDate = gameDate;
        }
    }
}
