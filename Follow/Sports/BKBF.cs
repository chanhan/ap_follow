using Follow.Sports.Basic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;

namespace Follow.Sports
{
    public class BKBF : BasicFB
    {
        private string sWebUrl1 = UrlSetting.GetUrl(ESport.Basketball_BF, "Url1");
        private string sWebUrl2 = UrlSetting.GetUrl(ESport.Basketball_BF, "Url2");
        // 時間字串格式
        private const string DATE_STRING_FORMAT = "yyyy-MM-dd";
        // 旗標: 是否正在取得資料
        private bool _onDataLoading = false;
        // 主客互換旗標
        private const bool AcH = true;
        private DataTable dtAlliance;
        private DataTable dtTeam;
        private List<BkBFDownload> DownReal = new List<BkBFDownload>();
        private BkBFDownload todayUpdate;
        private Dictionary<string, string> DownHomeHeader;
        private DateTime preDate;
        private long ticks = 0;
        System.Timers.Timer timer = new System.Timers.Timer(180000);//定时更新队伍资料
        public BKBF(DateTime today)
            : base(ESport.Basketball_BF)
        {
            if (string.IsNullOrWhiteSpace(this.sWebUrl))
            {
                this.sWebUrl = "http://d.asiascore.com/x/feed/proxy";
            }
            if (string.IsNullOrWhiteSpace(this.sWebUrl1))
            {
                this.sWebUrl1 = "http://d.asiascore.com/x/feed/u_3_1~";
            }
            if (string.IsNullOrWhiteSpace(this.sWebUrl2))
            {
                this.sWebUrl2 = "http://d.asiascore.com/x/feed/f_3_{0}_8_en-asia_1~";
            }
            // 讀取聯盟/隊伍翻譯表
            this.LoadAllianceAndTeamTable();
            timer.Elapsed += new System.Timers.ElapsedEventHandler(Timer_TimesUp);
            timer.AutoReset = true;
            timer.Enabled = true;
            timer.Start();
            ticks = DateTime.Now.Ticks;
            this.Logs = new LogFile(ESport.Basketball_BF);//設定log type
            // 設定
            this.AllianceID = 0;
            this.GameType = "BKBF";
            this.GameDate = GetUtcRu(today).Date; // 只取日期
            preDate = this.GameDate;
            this.DownHomeHeader = new Dictionary<string, string>();
            this.DownHomeHeader["Accept"] = "*/*";
            this.DownHomeHeader["Accept-Encoding"] = "gzip, deflate";
            this.DownHomeHeader["Accept-Language"] = "*";
            this.DownHomeHeader["Cookie"] = "__utmc=190588603; __utma=190588603.185314071.1417484917.1419992293.1419995616.20; __utmb=190588603.7.10.1419995616; __utmz=190588603.1419933759.18.5.utmcsr=asiascore.com|utmccn=(referral)|utmcmd=referral|utmcct=/basketball/; __utmt=1; __gads=ID=d33757aa8b7cb83c:T=1419995622:S=ALNI_MZLTynHPufg_Iqf0GwfwSasubCRCg";
            this.DownHomeHeader["DNT"] = "1";
            this.DownHomeHeader["Host"] = "d.asiascore.com";
            this.DownHomeHeader["Referer"] = this.sWebUrl;
            this.DownHomeHeader["User-Agent"] = "core";
            this.DownHomeHeader["X-Fsign"] = "SW9D1eZo";
            this.DownHomeHeader["X-GeoIP"] = "1";
            this.DownHomeHeader["X-Requested-With"] = "XMLHttpRequest";
            this.DownHomeHeader["X-utime"] = "1";

            todayUpdate = new BkBFDownload(this.Sport, this.GameDate, sWebUrl1, Encoding.UTF8, ticks, true, sWebUrl1);

            for (int i = -1; i <= 7; i++)
            {
                DateTime gameDate = this.GameDate.AddDays(i);
                string url = string.Format(sWebUrl2, i.ToString());
                string fileType = string.Format("f_3_{0}_8_en-asia_1~", i.ToString());
                DownReal.Add(new BkBFDownload(this.Sport, gameDate, url, Encoding.UTF8, 0, true, fileType));
            }
            GetOldGames();
        }

        //取得数据库中的今日比赛资料
        private void GetOldGames()
        {
            DateTime gameDate = this.GameDate;
            BasicInfo info = new BasicInfo(AllianceID, GameType, gameDate, gameDate.ToString(DATE_STRING_FORMAT));
            info.Away = "BF篮球";
            info.Home = "跟盤";
            info.Status = this.GetData(frmMain.ConnectionString, info);
            this.GameData[info.WebID] = info;
        }

        private void Timer_TimesUp(object sender, System.Timers.ElapsedEventArgs e)
        {
            this.LoadAllianceAndTeamTable();
        }
        public void DownloadData()
        {
            //隔天重新下载资料
            if (preDate.ToString(DATE_STRING_FORMAT) != DateTime.Now.ToString(DATE_STRING_FORMAT))
            {
                foreach (BkBFDownload download in DownReal)
                {
                    download.IsUpdateData = true;
                }
                gameList = new Dictionary<string, Dictionary<string, BasicInfo>>();//清空比赛信息，重新下载
                updateScroe = new Dictionary<string, BFUpdateScroe>();
            }
            //首次读取9天比赛资料
            foreach (BkBFDownload download in DownReal)
            {
                //未读取到数据才下载
                if (download.IsUpdateData)
                {
                    download.DownloadData(this.DownHomeHeader);
                }
            }
        }

        public override void Download()
        {
            // 讀取更新資料。
            todayUpdate.DownloadData(this.DownHomeHeader);
            todayUpdate.ticks = DateTime.Now.Ticks;
            this.ticks = todayUpdate.ticks;
            DownloadData();
        }

        Dictionary<string, BFUpdateScroe> updateScroe = new Dictionary<string, BFUpdateScroe>();

        void UpDateScore(string str, long tick)
        {
            if (tick < this.ticks)
            {
                return;
            }
            //  str = "SA÷3¬~AA÷Ye9qSufk¬BX÷9¬AG÷32¬BC÷11¬AH÷56¬BD÷21¬~AA÷pWCmRa9e¬BX÷10¬AG÷42¬BC÷19¬AH÷47¬BD÷20¬~AA÷lrWmwRU2¬AB÷3¬AC÷3¬AH÷78¬BB÷13¬BD÷17¬BF÷27¬BH÷21¬AS÷1¬AZ÷1¬AG÷86¬BA÷22¬BC÷20¬BE÷19¬BG÷25¬~AA÷jXJvTLvq¬BX÷11¬AG÷38¬BC÷17¬~AA÷lOXCX1WR¬BX÷12¬AG÷60¬BC÷26¬AH÷62¬BD÷29¬~AA÷tbU4ZN1F¬BX÷5¬AG÷51¬BE÷2¬AH÷51¬BF÷8¬~AA÷KlT8YsHL¬BX÷5¬AG÷44¬BE÷8¬AH÷62¬BF÷12¬~AA÷bZv0z4n9¬BX÷4¬AH÷79¬BH÷11¬AG÷71¬BG÷11¬~A2÷1420392333¬~A1÷8797ea3d0d78d235dd4c057d563b812b¬~";
            //  string[] key = { "AA","BX","AH","BF","AG","BG"};
            string[] awayNums = new string[] { "BA", "BC", "BE", "BG", "BI" };//客队各小节分数key
            string[] homeNums = new string[] { "BB", "BD", "BF", "BH", "BJ" };//主队各小节分数key
            if (!string.IsNullOrEmpty(str))
            {
                Dictionary<string, string> info = new Dictionary<string, string>();
                string[] data = str.Split(new string[] { "¬~" }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < data.Length; i++)
                {
                    string[] game = data[i].Split(new string[] { "¬" }, StringSplitOptions.RemoveEmptyEntries);
                    for (int j = 0; j < game.Length; j++)
                    {
                        string[] txt = game[j].Split(new string[] { "÷" }, StringSplitOptions.RemoveEmptyEntries);
                        info[txt[0]] = txt[1];
                    }
                    if (info.Count > 0)
                    {
                        BFUpdateScroe score = new BFUpdateScroe();
                        if (info.ContainsKey("AA"))//联盟
                        {
                            score.Alliance = info["AA"];
                            if (info.ContainsKey("BX"))//比赛进行时间
                            {
                                score.PlayMinutes = info["BX"];
                            }
                            if (info.ContainsKey("AG"))//客队总分
                            {
                                score.AwayPoint = info["AG"];
                            }
                            if (info.ContainsKey("AH"))//主队总分
                            {
                                score.HomePoint = info["AH"];
                            }
                            for (int k = 0; k < awayNums.Length; k++)//判断客队各节比赛分数
                            {
                                if (info.ContainsKey(awayNums[k]))
                                {
                                    score.AwayScore = info[awayNums[k]];//更新的分数
                                    score.Quarter = k + 1;//更新分数的节数
                                    break;
                                }

                            }
                            for (int k = 0; k < homeNums.Length; k++)//判断主队各节比赛分数
                            {
                                if (info.ContainsKey(homeNums[k]))
                                {
                                    score.HomeScore = info[homeNums[k]];
                                    score.Quarter = k + 1;
                                    break;
                                }

                            }
                            if (info.ContainsKey("AB"))//比赛状态
                            {
                                score.GameStatus = info["AB"];
                            }
                            if (info.ContainsKey("AC"))//比赛状态信息
                            {
                                score.GameInfo = info["AC"];
                            }
                            if (!updateScroe.ContainsKey(score.Alliance))
                            {
                                updateScroe.Add(score.Alliance, score);
                            }
                            else
                            {
                                updateScroe[score.Alliance] = score;
                            }
                        }
                    }
                    info = new Dictionary<string, string>();
                }

            }
        }
        Dictionary<string, Dictionary<string, BasicInfo>> gameList = new Dictionary<string, Dictionary<string, BasicInfo>>();
        // 取得資料
        public override int Follow()
        {
            int result = 0;
            UpDateScore(todayUpdate.Data, todayUpdate.ticks);
            //为增加比分即时性，9天比赛资料只读一次,或者可以做成每个一段时间重读一次
            foreach (BkBFDownload download in DownReal)
            {
                if (!download.IsUpdateData)
                {
                    continue;
                }
                // 取得資料
                DateTime gameDate = new DateTime();
                Dictionary<string, BasicInfo> gameData = this.GetDataBKBF(download.Data, ref gameDate, AcH);
                if (gameData != null && gameData.Count > 0)
                {

                    try
                    {
                        download.IsUpdateData = false;//读取到数据后不再重复解析
                        if (!gameData.ContainsKey(gameDate.ToString(DATE_STRING_FORMAT)))
                        {
                            gameList.Add(gameDate.ToString(DATE_STRING_FORMAT), gameData);
                        }
                        else
                        {
                            gameList[gameDate.ToString(DATE_STRING_FORMAT)] = gameData;

                        }
                    }
                    catch (Exception e)
                    {
                        string str = e.StackTrace;
                    }
                }
            }
            IEnumerable<BkBFDownload> down = DownReal.Where(p => p.IsUpdateData);
            Dictionary<string, List<string>> teamData = new Dictionary<string, List<string>>();

            //存在尚未下载的赛事资料，才需要更新队伍信息
            if (down != null && down.Any())
            {
                // 判斷資料
                if (gameList.Count > 0)
                {
                    // 聯盟/隊伍資料集合
                    foreach (Dictionary<string, BasicInfo> gameData in gameList.Values)
                    {
                        foreach (BasicInfo info in gameData.Values)
                        {
                            // 聯盟/隊伍資料
                            string alliance = info.AllianceName;
                            string teamA = info.Away;
                            string teamB = info.Home;

                            List<string> teamList = (teamData.ContainsKey(alliance)) ? teamData[alliance] : new List<string>();
                            if (!teamList.Contains(teamA)) { teamList.Add(teamA); }
                            if (!teamList.Contains(teamB)) { teamList.Add(teamB); }

                            teamData[alliance] = teamList;
                        }
                    }
                }
                // 動態新增隊伍與聯盟
                AddAllianceAndTeamToTable(teamData);
            }
            // 取代隊伍名稱
            ReplaceAllTeamName(gameList);
            if (gameList.ContainsKey(DateTime.Now.ToString(DATE_STRING_FORMAT)))
            {
                MergeData(gameList[DateTime.Now.ToString(DATE_STRING_FORMAT)], updateScroe);
            }
            CompareData(gameList);
            result = DataToJson(gameList);
            // 傳回
            return result;
        }

        //更新比赛分数、剩余时间
        private void MergeData(Dictionary<string, BasicInfo> dictionary, Dictionary<string, BFUpdateScroe> updateScroe)
        {
            foreach (KeyValuePair<string, BFUpdateScroe> pair in updateScroe)
            {
                string allianceID = pair.Key;
                BFUpdateScroe scroe = pair.Value;
                BasicInfo game = dictionary[allianceID];
                DateTime gameTime = game.GameTime;
                if (dictionary.ContainsKey(allianceID))
                {
                    int quarter = game.Quarter;//比赛节数
                    //更新主队小节分数
                    if (quarter > 0 && scroe.HomeScore != null)
                    {
                        if (quarter == scroe.Quarter)//更新当前节数
                        {
                            game.HomeBoard[quarter - 1] = scroe.HomeScore;
                        }
                        else if (quarter < scroe.Quarter)//开始新的小节比赛
                        {
                            game.HomeBoard.Add(scroe.HomeScore);
                        }
                    }
                    //新开始的比赛
                    else if (quarter == 0 && scroe.HomeScore != null)
                    {
                        game.HomeBoard.Add(scroe.HomeScore);
                    }
                    //更新客队小节分数
                    if (quarter > 0 && scroe.AwayScore != null)
                    {
                        if (quarter == scroe.Quarter)//更新当前节数
                        {
                            game.AwayBoard[quarter - 1] = scroe.AwayScore;
                        }
                        else if (quarter < scroe.Quarter)//开始新的小节比赛
                        {
                            game.AwayBoard.Add(scroe.AwayScore);
                        }
                    }
                    //新开始的比赛
                    else if (quarter == 0 && scroe.AwayScore != null)
                    {
                        game.AwayBoard.Add(scroe.AwayScore);
                    }
                    if (scroe.GameStatus != null)
                    {
                        #region 比賽狀態
                        switch (scroe.GameStatus)
                        {
                            //只显示最后结果的赛事修改成速报要抓取到讯息“只显示最终比分”，状态要为“结束”
                            case "1":
                                if (DateTime.Compare(gameTime, DateTime.Now) <= 0)//比賽時間到了才顯示已結束
                                {
                                    game.TrackerText = "只顯示最終比分";
                                    game.GameStates = "E";
                                }
                                break;
                            case "2":
                                game.GameStates = "S";
                                // 狀態
                                if (scroe.GameInfo != null &&
                                    (scroe.GameInfo == "38" ||
                                     scroe.GameInfo == "46"))
                                {
                                    game.Status = "中場休息";
                                }

                                break;
                            case "3":
                                if (scroe.GameInfo != null)
                                {
                                    switch (scroe.GameInfo)
                                    {
                                        case "4":
                                        case "5":
                                            game.GameStates = "P";
                                            game.Status = "中止";
                                            break;
                                        default:
                                            game.GameStates = "E";
                                            game.Status = "結束";
                                            break;
                                    }
                                }
                                //else
                                //{
                                //    game.GameStates = "E";
                                //    game.Status = "結束";
                                //}
                                break;
                        }
                        if (scroe.GameStatus != "1")//防止一开始没有确定有没有比分源，所以备注fro，开赛前确定后就把备注去掉了的情况发生
                        {
                            game.TrackerText = "";
                        }
                        #endregion
                    }
                    if (scroe.PlayMinutes != null)
                    {
                        // 剩餘時間
                        int playMinute = 10;
                        //NBA、CBA、NBA發展聯盟、Philippine Cup比賽12分鐘，WCBA10分鐘
                        if (game.AllianceName.IndexOf("NBA") >= 0 || (game.AllianceName.IndexOf("CBA") >= 0 && game.AllianceName.IndexOf("WCBA") == -1) || game.AllianceName.IndexOf("Philippine Cup") >= 0)
                        {
                            playMinute = 12;
                        }
                        game.Status = (playMinute - int.Parse(scroe.PlayMinutes)).ToString();
                    }
                    if (scroe.AwayPoint != null)
                    {
                        game.AwayPoint = scroe.AwayPoint;
                    }
                    if (scroe.HomePoint != null)
                    {
                        game.HomePoint = scroe.HomePoint;
                    }
                }
            }
        }

        public override bool Update(string connectionString, BasicInfo info)
        {
            if (info == null) { return false; }

            bool result = base.UpdateData(connectionString, info);

            // 寫入紀錄
            if (result)
            {
                // 新增 json 記錄
                this.Logs.UpdateJson(info.Status);

                List<BasicInfo> dataInfo = info.Tag as List<BasicInfo>;
                if (dataInfo != null)
                {
                    foreach (BasicInfo game in dataInfo)
                    {
                        // 賽事資料
                        string data = game.ToString();

                        // 最後一場賽事加上分隔線
                        int idx = dataInfo.IndexOf(game);
                        if (idx == dataInfo.Count - 1)
                        {
                            data = String.Format("{0}{1}{2}", data, Environment.NewLine, "--------------------------------------------------------------------------------");
                        }

                        this.Logs.Update(data);
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// 變更賽事的隊伍名稱
        /// </summary>
        /// <param name="gameList">賽事資料</param>
        /// <remarks>原在轉換 json 時取代隊名, 但因取得舊資料時是已轉換的名稱, 比對時就會發生隊名不一致的問題</remarks>
        private void ReplaceAllTeamName(Dictionary<string, Dictionary<string, BasicInfo>> gameList)
        {
            DataTable dt = dtTeam.Copy();
            foreach (string gameDate in gameList.Keys)
            {
                Dictionary<string, BasicInfo> dataList = gameList[gameDate];

                foreach (string webID in dataList.Keys)
                {
                    BasicInfo info = dataList[webID];
                    string alliance = info.AllianceName;
                    info.Away = ReplaceTeamName(alliance, info.Away, dt);
                    info.Home = ReplaceTeamName(alliance, info.Home, dt);
                }
            }
        }
        /// <summary>
        /// 變更隊伍名稱
        /// </summary>
        /// <param name="name">來源網名稱</param>
        /// <returns>顯示名稱</returns>
        private string ReplaceTeamName(string allianceName, string name, DataTable dt)
        {
            if (dt != null)
            {
                name = (from DataRow x in dt.AsEnumerable()
                        where x.Field<string>("TeamName").Equals(name)
                            && x.Field<string>("AllianceName").Equals(allianceName)
                        select x.Field<string>("ShowName")).DefaultIfEmpty(name).FirstOrDefault();
            }
            return name;
        }

        /// <summary>
        /// 新增聯盟/隊伍資料
        /// </summary>
        /// <param name="teamData">隊伍清單 (Key: 聯盟名稱)</param>
        private void AddAllianceAndTeamToTable(Dictionary<string, List<string>> teamData)
        {
            bool updated = false;

            using (TransactionScope scope = new TransactionScope())
            {
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

                        scope.Complete();
                    }
                    catch (Exception)
                    {
                    }
                }
            }

            // 資料有更新, 重新取得資料
            //    if (updated) { this.LoadAllianceAndTeamTable(); }
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
                string sql = @"IF EXISTS(SELECT AllianceID FROM BFAlliance WITH (NOLOCK) WHERE AllianceName = @AllianceName)
                                BEGIN
	                                SELECT AllianceID FROM BFAlliance  WITH (NOLOCK) WHERE AllianceName = @AllianceName;
                                END
                                ELSE
                                BEGIN
	                                INSERT INTO BFAlliance(AllianceName, ShowName)
	                                VALUES(@AllianceName, @AllianceName);
	                                SELECT SCOPE_IDENTITY() AS AllianceID;
                                END";

                SqlCommand cmd = conn.CreateCommand();
                cmd.CommandText = sql;
                cmd.Parameters.Add("@AllianceName", SqlDbType.NVarChar).Value = allianceName;
                object o = cmd.ExecuteScalar();
                allianceID = Convert.ToInt32(o);
                update = true;
            }

            return update;
        }
        private bool AddTeamData(SqlConnection conn, string teamName, int allianceID, out int teamID)
        {
            teamID = 0;
            bool update = false;

            // 檢查暫存表中是否有資料
            var q = from DataRow x in dtTeam.AsEnumerable()
                    where x.Field<string>("TeamName").Equals(teamName)
                        && x.Field<int>("AllianceID") == allianceID
                    select x.Field<int>("TeamID");

            // 已存在資料, 直接取得隊伍 ID
            if (q.Any())
            {
                teamID = q.First();
            }
            else
            {
                // 新增隊伍資料
                string sql = @"IF EXISTS(SELECT TeamID FROM BFTeam  WITH (NOLOCK) WHERE AllianceID = @AllianceID AND TeamName = @TeamName)
                                BEGIN
	                                SELECT TeamID FROM BFTeam WITH (NOLOCK) WHERE AllianceID = @AllianceID AND TeamName = @TeamName;
                                END
                                ELSE
                                BEGIN
	                                INSERT INTO BFTeam(AllianceID, TeamName, ShowName)
	                                VALUES(@AllianceID, @TeamName, @TeamName);
	                                SELECT SCOPE_IDENTITY() AS TeamID;
                                END";

                SqlCommand cmd = conn.CreateCommand();
                cmd.CommandText = sql;
                cmd.Parameters.Add("@AllianceID", SqlDbType.Int).Value = allianceID;
                cmd.Parameters.Add("@TeamName", SqlDbType.NVarChar).Value = teamName;
                object o = cmd.ExecuteScalar();
                allianceID = Convert.ToInt32(o);
                update = true;
            }

            return update;
        }
        /// <summary>
        /// 讀取聯盟/隊伍資料
        /// </summary>
        private void LoadAllianceAndTeamTable()
        {
            // 正在讀取資料時 不處理
            if (_onDataLoading) { return; }

            _onDataLoading = true;

            using (SqlConnection conn = new SqlConnection(frmMain.ConnectionString))
            {
                try
                {
                    conn.Open();

                    string sql = @"SELECT AllianceID, AllianceName, ShowName FROM BFAlliance WITH (NOLOCK)";
                    LoadData(conn, sql, ref dtAlliance);

                    sql = @"SELECT TeamID, TeamName, T.ShowName, T.AllianceID, A.AllianceName FROM BFTeam T WITH (NOLOCK)
                            JOIN BFAlliance A WITH (NOLOCK) ON T.AllianceID = A.AllianceID";
                    LoadData(conn, sql, ref dtTeam);
                }
                catch { }
            }

            _onDataLoading = false;
        }
        /// <summary>
        ///  讀取資料
        /// </summary>
        /// <param name="conn">資料庫連線</param>
        /// <param name="sql">查詢 SQL</param>
        /// <param name="dt">回傳資料表</param>
        private void LoadData(SqlConnection conn, string sql, ref DataTable dt)
        {
            if (dt != null) { dt.Dispose(); }
            dt = new DataTable();

            // 錯誤處理
            try
            {
                SqlDataAdapter da = new SqlDataAdapter(sql, conn);
                // 讀取
                da.Fill(dt);
            }
            catch { }
        }
        private void CompareData(Dictionary<string, Dictionary<string, BasicInfo>> gameList)
        {
            try
            {
                List<string> gameDateList = gameList.Keys.ToList();

                foreach (string gameDate in gameDateList)
                {
                    if (!this.GameData.ContainsKey(gameDate) || !gameList.ContainsKey(gameDate)) { continue; }

                    string data = this.GameData[gameDate].Status;
                    Dictionary<string, BasicInfo> oldGames = JsonToGameList(data);
                    Dictionary<string, BasicInfo> newGames = gameList[gameDate];
                    List<BasicInfo> infoList = newGames.Values.ToList();

                    foreach (BasicInfo info in infoList)
                    {
                        string webID = info.WebID;
                        if (!oldGames.ContainsKey(webID)) { continue; }
                        BasicInfo oldInfo = oldGames[webID];

                        // 當舊資料 Record 有值時, 而新資料沒值時, 覆蓋新資料的 Record
                        if (!String.IsNullOrEmpty(oldInfo.Record) && String.IsNullOrEmpty(info.Record))
                        {
                            info.Record = oldInfo.Record;
                        }

                        // 未開賽直接跳至結束, 視為待定賽事相同, 寫入 Record
                        if ("E".Equals(info.GameStates) && "X".Equals(oldInfo.GameStates))
                        {
                            info.Record = "只顯示最終比分";
                        }

                        // 比對新舊資料是否相同  && IsNormalScore(oldInfo, info)
                        if (IsNormalScore(oldInfo, info))
                       //   if (CheckGame(oldInfo, info))
                        {
                            info.Changed = true;
                        }
                        else
                        {
                            info.Changed = false;
                        }
                        newGames[webID] = info;
                    }
                }
            }
            catch { throw; }
        }

        //private bool CheckGame(BasicInfo oldInfo, BasicInfo newInfo)
        //{
        //    bool isLegal = true;
        //    //先检查新旧资料是否相同，不相同再比较比赛状态、分数
        //    if (!newInfo.ToString().Equals(oldInfo.ToString()))
        //    {
        //        //旧资料是未开赛，还没有比分，数据合法
        //        if (oldInfo.GameStates == "X")// && newInfo.GameStates == "X")
        //        {
        //            isLegal = true;
        //        }
        //        //旧资料是未开赛，还没有比分，数据合法
        //        //else if (newInfo.GameStates != "X")
        //        //{
        //        //    isLegal = false;
        //        //}
        //        else
        //        {
        //            if (int.Parse(oldInfo.AwayPoint) > int.Parse(newInfo.AwayPoint))
        //            {
        //                isLegal = false;
        //            }
        //            else
        //            {
        //                if (int.Parse(oldInfo.HomePoint) > int.Parse(newInfo.HomePoint))
        //                {
        //                    isLegal = false;
        //                }
        //            }
        //        }
        //    }

        //    return isLegal;
        //}



        /// <summary>
        /// 是否正常比分 
        /// 如果新的分数小于旧的分  就返回false （不正常的分数）并且把旧的分数赋值给新的
        /// </summary>
        /// <param name="oldInfo">旧的资料</param>
        /// <param name="newInfo">新的资料</param>
        /// <returns></returns>
        private bool IsNormalScore(BasicInfo oldInfo, BasicInfo newInfo)
        {
            bool ret = true;
            if (!newInfo.ToString().Equals(oldInfo.ToString()))
            {
                if (oldInfo.HomeBoard != null && oldInfo.AwayBoard != null && newInfo.AwayBoard != null && newInfo.HomeBoard != null)
                {
                    int oldHomeCount = 0, oldAwayCount = 0, newHomeCount = 0, newAwayCount = 0;
                    oldHomeCount = oldInfo.HomeBoard.Select<string, int>(q => Convert.ToInt32(q)).Sum();
                    oldAwayCount = oldInfo.AwayBoard.Select<string, int>(q => Convert.ToInt32(q)).Sum();
                    newHomeCount = newInfo.HomeBoard.Select<string, int>(q => Convert.ToInt32(q)).Sum();
                    newAwayCount = newInfo.AwayBoard.Select<string, int>(q => Convert.ToInt32(q)).Sum();
                    //foreach (string item in oldInfo.HomeBoard)
                    //{
                    //    int o=0;
                    //    int.TryParse(item, out o);
                    //    oldHomeCount += o;
                    //}
                    if (newHomeCount < oldHomeCount || newAwayCount < oldAwayCount)
                    {
                        newInfo.HomeBoard = oldInfo.HomeBoard;
                        newInfo.AwayBoard = oldInfo.AwayBoard;
                        ret = false;
                    }

                }
            }
            else
            {
                ret = false;
            }
            return ret;
        }


        /// <summary>
        /// 轉換 Json 資料
        /// </summary>
        /// <param name="gameData">單日賽事資料</param>
        /// <returns>Json 物件</returns>
        private JObject DataToJson(Dictionary<string, BasicInfo>.ValueCollection gameData)
        {
            JObject json = new JObject();
            try
            {
                // 賽程排序 => 聯盟. 開賽時間
                List<BasicInfo> gameList = gameData.OrderBy(x => x.AllianceName).ThenBy(x => x.GameTime).ToList();

                foreach (BasicInfo game in gameList)
                {
                    BasicInfo info = game.Clone() as BasicInfo;

                    string webID = info.WebID;
                    // 聯盟名稱
                    //string allianceName = ReplaceAllianceName(info.AllianceName);
                    string allianceName = info.AllianceName;
                    JObject jsonAlliance = (json[allianceName] as JObject) ?? new JObject();
                    JObject jsonGame = new JObject();

                    // 開賽時間/日期
                    jsonGame["GameDate"] = info.GameTime.ToString("yyyy") + "-" + info.GameTime.ToString("MM") + "-" + info.GameTime.ToString("dd");
                    jsonGame["GameTime"] = info.GameTime.ToString("HH:mm");
                    // 開賽狀態
                    jsonGame["GameStates"] = info.GameStates;
                    // 狀態文字
                    jsonGame["StatusText"] = info.Status;
                    // 訊息文字
                    jsonGame["TrackerText"] = String.IsNullOrEmpty(info.TrackerText) ? String.Empty : info.TrackerText;
                    // 紀錄
                    jsonGame["Record"] = info.Record;

                    // 判斷是否有主客互換
                    if (!info.AcH)
                    {
                        // 隊伍
                        jsonGame["TeamA"] = info.Home;
                        jsonGame["TeamB"] = info.Away;
                        // 各局比分
                        jsonGame["RunsA"] = String.Join(",", info.HomeBoard.ToArray());
                        jsonGame["RunsB"] = String.Join(",", info.AwayBoard.ToArray());
                        // 總分
                        jsonGame["RA"] = info.HomePoint;
                        jsonGame["RB"] = info.AwayPoint;
                    }
                    else
                    {
                        // 隊伍
                        jsonGame["TeamA"] = info.Away;
                        jsonGame["TeamB"] = info.Home;
                        // 各局比分
                        jsonGame["RunsA"] = String.Join(",", info.AwayBoard.ToArray());
                        jsonGame["RunsB"] = String.Join(",", info.HomeBoard.ToArray());
                        // 總分
                        jsonGame["RA"] = info.AwayPoint;
                        jsonGame["RB"] = info.HomePoint;
                    }
                    jsonAlliance[webID] = jsonGame;
                    //jsonAlliance["AllianceShowName"]= (from DataRow x in dtAlliance.AsEnumerable()
                    //where x.Field<string>("AllianceName").Equals(allianceName) select x.Field<string>("ShowName")).ToList<string>()[0];
                    json[allianceName] = jsonAlliance;
                }
            }
            catch (Exception)
            {

                throw;
            }
            return json;
        }
        /// <summary>
        /// 轉換 Json 資料
        /// </summary>
        /// <param name="gameList">全部賽事資料</param>
        /// <returns>變更個數</returns>
        private int DataToJson(Dictionary<string, Dictionary<string, BasicInfo>> gameList)
        {
            int result = 0;

            foreach (KeyValuePair<string, Dictionary<string, BasicInfo>> pair in gameList)
            {
                DateTime gameDate = Convert.ToDateTime(pair.Key);
                Dictionary<string, BasicInfo> gameData = pair.Value;
                //今天赛事尚未取得更新数据，不拼接json
                //if (gameDate.ToString(DATE_STRING_FORMAT) == DateTime.Now.ToString(DATE_STRING_FORMAT) && updateScroe.Count == 0)
                //{
                //    continue;
                //}
                // 取得 JSON 資料
                JObject json = DataToJson(gameData.Values);

                if (json.Count > 0)
                {
                    // 建立比賽資料
                    BasicInfo gameInfo = new BasicInfo(this.AllianceID, this.GameType, gameDate, gameDate.ToString(DATE_STRING_FORMAT));
                    gameInfo.Away = "BF篮球";
                    gameInfo.Home = "跟盤";
                    gameInfo.Status = json.ToString(Formatting.None);
                    gameInfo.Tag = gameData.Values.Where(x => x.Changed).ToList(); // 只取有變動的資料

                    // 加入
                    this.GameData[gameInfo.WebID] = gameInfo;

                    // 累計
                    result++;
                }
            }

            return result;
        }
        /// <summary>
        /// 轉換 Json 資料
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private Dictionary<string, BasicInfo> JsonToGameList(string data)
        {
            Dictionary<string, BasicInfo> infoList = new Dictionary<string, BasicInfo>();

            if (!String.IsNullOrEmpty(data))
            {
                JObject json = JObject.Parse(data);
                foreach (JProperty prop in json.Children())
                {
                    // 聯盟
                    string alliance = prop.Name;

                    foreach (JProperty game in prop.Values())
                    {
                        string webID = game.Name;
                        JObject detail = game.Value as JObject;
                        DateTime gameTime = Convert.ToDateTime(String.Format("{0} {1}", detail["GameDate"], detail["GameTime"]));
                        string state = detail["GameStates"].ToString();
                        string statusText = (detail["StatusText"] ?? String.Empty).ToString();
                        string trackerText = (detail["trackerText"] ?? String.Empty).ToString();
                        string record = (detail["Record"] ?? String.Empty).ToString();

                        string teamA = detail["TeamA"].ToString();
                        string teamB = detail["TeamB"].ToString();

                        string[] runsA = detail["RunsA"].ToString().Trim().Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                        string[] runsB = detail["RunsB"].ToString().Trim().Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                        string ptAway = detail["RA"].ToString();
                        string ptHome = detail["RB"].ToString();

                        BasicInfo info = new BasicInfo(AllianceID, GameType, gameTime, webID);
                        info.AllianceName = alliance;
                        info.GameStates = state;
                        info.Status = statusText;
                        info.TrackerText = trackerText;
                        info.Record = record;
                        info.AcH = AcH;

                        // 主隊互換
                        if (!AcH)
                        {
                            //info.Away = teamB;
                            //info.Home = teamA;
                            //info.AwayBoard.AddRange(runsB);
                            //info.HomeBoard.AddRange(runsA);
                            //info.AwayPoint = ptHome;
                            //info.HomePoint = ptAway;
                        }
                        else
                        {
                            info.Away = teamA;
                            info.Home = teamB;
                            info.AwayBoard.AddRange(runsA);
                            info.HomeBoard.AddRange(runsB);
                            info.AwayPoint = ptAway;
                            info.HomePoint = ptHome;
                        }

                        infoList[info.WebID] = info;
                    }
                }
            }

            return infoList;
        }

        #region 複寫 dispose
        public override void Dispose()
        {
            // 停止計時器
            timer.Enabled = false;
            base.Dispose();
        }
        #endregion 複寫 dispose
    }
    public class BkBFDownload : BasicDownload
    {
        // 抓取賽事時間
        public DateTime GameDate { private set; get; }
        public bool IsUpdateData { get; set; }//是否需要下载资料
        public long ticks { get; set; }

        // 建構式
        public BkBFDownload(ESport sport, DateTime gameDate, string url, Encoding encoding, long ticks, bool IsUpdateData = false, string fileType = null)
            : base(sport, url, encoding, fileType)
        {
            this.GameDate = gameDate;
            this.IsUpdateData = IsUpdateData;
            this.ticks = ticks;
        }
    }

    public class BFUpdateScroe
    {
        public string HomeScore { get; set; }
        public string AwayScore { get; set; }
        public string HomePoint { get; set; }
        public string AwayPoint { get; set; }
        public string Alliance { get; set; }
        public string PlayMinutes { get; set; }
        public int Quarter { get; set; }//目前节数
        public string GameStatus { get; set; }//比赛状态
        public string GameInfo { get; set; }//比赛状态信息

    }
}
