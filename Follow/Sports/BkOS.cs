using Follow.Helper;
using Follow.Sports.Basic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Timers;
using System.Transactions;

namespace Follow.Sports
{
    /// <summary>
    /// 奧訊自動跟分
    /// </summary>
    public class BkOS : Basic.BasicFB
    {
        #region 成員變數

        // 下載資料
        private BkOSDownload DownToday;

        private BkOSDownload DownChange;
        private List<BkOSDownload> DownReal;
        private DataTable dtAlliance;
        private DataTable dtTeam;

        // 旗標: 是否正在取得資料
        private bool _onDataLoading = false;

        // 時間字串格式
        private const string DATE_STRING_FORMAT = "yyyy-MM-dd";

        private const string TIME_STRING_FORMAT = "HH:mm";
        // 主客互換旗標
        private const bool AcH = true;
        //全部比赛信息
        Dictionary<string, Dictionary<string, BasicInfo>> gameData = new Dictionary<string, Dictionary<string, BasicInfo>>();
        //更新的比赛信息
        Dictionary<string, Dictionary<string, BasicInfo>> updateGameData = new Dictionary<string, Dictionary<string, BasicInfo>>();
        //新增的比赛信息
        Dictionary<string, Dictionary<string, BasicInfo>> addGameData = new Dictionary<string, Dictionary<string, BasicInfo>>();
        //删除的比赛信息
        Dictionary<string, BasicInfo> deleteGameData = new Dictionary<string, BasicInfo>();
        //推迟的比赛信息
        Dictionary<string, BasicInfo> changeDateGameData = new Dictionary<string, BasicInfo>();
        //开始日期索引
        int startIndex = -1;
        //结束日期索引
        int endIndex = 6;
        private string sWebUrl1 = UrlSetting.GetUrl(ESport.Basketball_OS, "Url1");
        private string sWebUrl2 = UrlSetting.GetUrl(ESport.Basketball_OS, "Url2");
        #endregion 成員變數

        #region 建構式

        public BkOS(DateTime today)
            : base(ESport.Basketball_OS)
        {
            // 讀取聯盟/隊伍翻譯表
            this.LoadAllianceAndTeamTable();

            this.Logs = new LogFile(ESport.Basketball_OS);//設定log type

            // 設定
            this.AllianceID = 0;
            this.GameType = "BKOS";
            this.GameDate = GetUtcTw(today).Date; // 只取日期
            this.DownReal = new List<BkOSDownload>();

            if (string.IsNullOrWhiteSpace(this.sWebUrl))
            {
                this.sWebUrl = @"http://interface.win007.com/lq/today.aspx";
            }
            if (string.IsNullOrWhiteSpace(this.sWebUrl1))
            {
                this.sWebUrl1 = @"http://interface.win007.com/lq/change.xml";
            }
            if (string.IsNullOrWhiteSpace(this.sWebUrl2))
            {
                this.sWebUrl2 = @"http://interface.win007.com/lq/LqSchedule.aspx?time={0:yyyy-MM-dd}";
            }
            // 今日
            var todayUrl = this.sWebUrl;
            this.DownToday = new BkOSDownload(this.Sport, GameDate, todayUrl, Encoding.UTF8, "today");

            // 即時資料
            var changeUrl = this.sWebUrl1;
            this.DownChange = new BkOSDownload(this.Sport, GameDate, changeUrl, Encoding.GetEncoding("gb2312"), "change", 5);

            // 昨天/未來一周
            for (int i = startIndex; i <= endIndex; i++)
            {
                DateTime gameDate = this.GameDate.AddDays(i);
                var url = String.Format(this.sWebUrl2, gameDate);
                this.DownReal.Add(new BkOSDownload(this.Sport, gameDate, url, Encoding.UTF8, String.Format("LqSchedule_{0:yyyy-MM-dd}", gameDate)));
            }

            #region 從資料庫中取得舊資料

            GetOldGames();

            #endregion 從資料庫中取得舊資料

        }

        #endregion 建構式

        #region 資料下載

        /// <summary>
        /// 複寫 Download
        /// </summary>
        public override void Download()
        {
            // 下載今日資料
            Download(this.DownToday);
            // 即時資料
            Download(this.DownChange);

            //下載資料
            foreach (BkOSDownload real in this.DownReal)
            {
                Download(real);
            }
        }

        /// <summary>
        /// 下載資料
        /// </summary>
        /// <param name="download"></param>
        private void Download(BkOSDownload download)
        {
            // 沒有資料或下載時間超過更新秒數時才讀取資料。
            if (!download.LastTime.HasValue ||
                    DateTime.Now > download.LastTime.Value.AddSeconds(download.UpdateSecond))
            {
                download.DownloadString();
            }
        }

        #endregion 資料下載

        #region 跟分資料解析

        /// <summary>
        /// 複寫 Follow
        /// </summary>
        /// <returns>賽事個數</returns>
        public override int Follow()
        {
            int result = 0;

            // 賽事資料集合 ( Key: 開賽日期 Value: 當日賽事集合[Key: WebID] )
            Dictionary<string, Dictionary<string, BasicInfo>> gameList = new Dictionary<string, Dictionary<string, BasicInfo>>();
            // 聯盟/隊伍資料集合 ( Key: 聯盟, Value: 隊伍集合 )
            Dictionary<string, List<string>> teamData = new Dictionary<string, List<string>>();

            // 若 today.xml 還沒抓到資料 
            if (String.IsNullOrEmpty(this.DownToday.Data)) { return result; }

            // 解析今日的資料
            Dictionary<string, BasicInfo> gameToday = this.Follow(this.DownToday, teamData);
            //if (gameToday == null)
            //{
            //   // 超过时间为抓取到资料，所有下载切换代理
            //    TimeSpan span = DateTime.Now.Subtract(startDownload);
            //    if (span.TotalSeconds > frmMain.UseProxy)
            //    {
            //        string message = string.Format("超過{0}秒未下載到資料", frmMain.UseProxy);
            //        this.DownToday.WriteLog(message, true);
            //        this.DownChange.WriteLog(message, true);
            //        foreach (BkOSDownload download in this.DownReal)
            //        {
            //            download.WriteLog(message, true);
            //        }
            //        //抓到资料，重置开始下载时间，停止切换代理
            //        startDownload = DateTime.Now;
            //    }
            //    return result;
            //}
            // 將資料依日期做分類解析
            foreach (BkOSDownload download in this.DownReal)
            {
                Dictionary<string, BasicInfo> gameByDate = this.Follow(download, teamData);
                if (gameByDate != null && gameByDate.Count > 0)
                {
                    string gameDate = download.GameDate.ToString(DATE_STRING_FORMAT);
                    gameList[gameDate] = gameByDate;
                }
            }

            // 即時變動資料
            Dictionary<string, BasicInfo> gameChange = this.FollowChange();

            // 動態新增隊伍與聯盟
            AddAllianceAndTeamToTable(teamData);

            // 合併今日/即時資料/其他日期資料
            MergeData(gameList, gameToday, gameChange);

            // 比對每日資料
            CompareData(gameList);

            //   result = FindChange(gameList);
            // 回傳值
            return gameList.Count;
        }

        //private int FindChange(Dictionary<string, Dictionary<string, BasicInfo>> gameList)
        //{
        //    updateGameData = new Dictionary<string, IEnumerable<BasicInfo>>();
        //    addGameData = new Dictionary<string, IEnumerable<BasicInfo>>();

        //    foreach (KeyValuePair<string, Dictionary<string, BasicInfo>> pair in gameList)
        //    {
        //        DateTime gameDate = Convert.ToDateTime(pair.Key);
        //        Dictionary<string, BasicInfo> gameData = pair.Value;
        //        var basic = gameData.Values.Where(p => p.Changed == true);
        //        if (basic != null && basic.Any())
        //        {
        //            foreach (var item in basic)
        //            {

        //            }
        //            updateGameData[pair.Key] = basic;
        //        }
        //    }
        //    this.gameData = gameList;
        //    return gameList.Count;
        //}

        /// <summary>
        /// 合併今日/即時資料/其他日期資料
        /// </summary>
        /// <param name="gameList">各日賽事資料</param>
        /// <param name="gameToday">今日資料</param>
        private void MergeData(Dictionary<string, Dictionary<string, BasicInfo>> gameList, Dictionary<string, BasicInfo> gameToday, Dictionary<string, BasicInfo> gameChange)
        {
            if (gameToday != null && gameToday.Count > 0)
            {
                foreach (BasicInfo info in gameToday.Values)
                {
                    string gameDate = info.GameTime.Date.ToString(DATE_STRING_FORMAT);

                    // 合併即時比分資料
                    if (gameChange != null && gameChange.ContainsKey(info.WebID))
                    {
                        BasicInfo infoChange = gameChange[info.WebID];
                        // 狀態
                        info.Status = infoChange.Status;
                        info.GameStates = infoChange.GameStates;
                        info.TrackerText = infoChange.TrackerText;
                        info.Record = infoChange.Record;
                        // 總分
                        info.HomePoint = infoChange.HomePoint;
                        info.AwayPoint = infoChange.AwayPoint;
                        // 各局比分
                        info.AwayBoard.Clear();
                        info.AwayBoard.AddRange(infoChange.AwayBoard);
                        info.HomeBoard.Clear();
                        info.HomeBoard.AddRange(infoChange.HomeBoard);
                    }

                    // 各日賽事中有開賽日期資料的, 才做合併
                    if (gameList.ContainsKey(gameDate))
                    {
                        Dictionary<string, BasicInfo> gameByDate = gameList[gameDate];
                        gameByDate[info.WebID] = info;
                    }
                }
            }
        }

        /// <summary>
        /// 將資料依日期做分類解析
        /// </summary>
        /// <param name="download">資料下載物件</param>
        /// <param name="teamData">聯盟/隊伍資料集合 ( Key: 聯盟, Value: 隊伍集合 )</param>
        private Dictionary<string, BasicInfo> Follow(BkOSDownload download, Dictionary<string, List<string>> teamData)
        {
            try
            {
                // 沒有資料就離開
                if (string.IsNullOrEmpty(download.Data))
                    return null;

                string data = String.Copy(download.Data);

                bool hasGameStart = false;
                Dictionary<string, BasicInfo> gameData = this.GetDataByBet007Basketball(data, isAcH: AcH, url: download.Uri.OriginalString);
                if (gameData == null || gameData.Count == 0) { return null; }

                // 聯盟/隊伍資料集合
                foreach (BasicInfo info in gameData.Values)
                {
                    // 有開賽時, 設定旗標
                    if ("S".Equals(info.GameStates)) { hasGameStart = true; }

                    // 聯盟/隊伍資料
                    string alliance = info.AllianceName;
                    string teamA = info.Away;
                    string teamB = info.Home;

                    List<string> teamList = (teamData.ContainsKey(alliance)) ? teamData[alliance] : new List<string>();
                    if (!teamList.Contains(teamA)) { teamList.Add(teamA); }
                    if (!teamList.Contains(teamB)) { teamList.Add(teamB); }

                    teamData[alliance] = teamList;
                }

                // 有開賽時 即時比分改每秒抓取
                this.DownChange.UpdateSecond = (hasGameStart) ? 1 : 5;

                return gameData;
            }
            catch { throw; }
        }

        /// <summary>
        /// 將資料依日期做分類解析
        /// </summary>
        /// <param name="download">資料下載物件</param>
        /// <param name="teamData">聯盟/隊伍資料集合 ( Key: 聯盟, Value: 隊伍集合 )</param>
        private Dictionary<string, BasicInfo> FollowChange()
        {
            try
            {
                // 沒有資料就離開
                if (string.IsNullOrEmpty(this.DownChange.Data))
                    return null;

                string data = String.Copy(this.DownChange.Data);

                bool hasGameStart = false;
                Dictionary<string, BasicInfo> gameData = this.GetChangeByBet007Basketball(data, this.DownChange.Uri.OriginalString);
                if (gameData == null || gameData.Count == 0) { return null; }

                // 聯盟/隊伍資料集合
                foreach (BasicInfo info in gameData.Values)
                {
                    // 有開賽時, 設定旗標
                    if ("S".Equals(info.GameStates)) { hasGameStart = true; }
                }

                // 即時比分改每秒抓取
                this.DownChange.UpdateSecond = (hasGameStart) ? 1 : 5;

                return gameData;
            }
            catch { throw; }
        }

        #endregion 跟分資料解析

        #region 新舊比對資料

        /// <summary>
        /// 比對每日賽事新舊資料
        /// </summary>
        /// <param name="gameList"></param>
        private void CompareData(Dictionary<string, Dictionary<string, BasicInfo>> gameList)
        {
            try
            {
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
                                if (newData.ContainsKey(game.WebID) && !changeDateGameData.ContainsKey(game.WebID))
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
                        if (!newGames.ContainsKey(webID) && !changeDateGameData.ContainsKey(webID) && !deleteGameData.ContainsKey(webID))
                        {
                            deleteGameData.Add(webID, info);//比赛信息错误需要删除
                        }
                    }

                    foreach (BasicInfo info in infoList)
                    {
                        string webID = info.WebID;
                        if (!oldGames.ContainsKey(webID))
                        {
                            if (!addGameData.ContainsKey(gameDate))
                            {
                                addGameData[gameDate] = new Dictionary<string, BasicInfo>();
                            }
                            info.Display = 0;
                            addGameData[gameDate].Add(webID, info);//新增一天的某场比赛信息
                            continue;
                        }
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

                        // 比對新舊資料是否相同 只当有变动的的资料的时候才检查是否正常
                        if (IsNormalScore(oldInfo, info))
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
                this.gameData = gameList;
            }
            catch { throw; }
        }

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
            //如果赛事日期大于当前日期 且赛事状态是已开赛、结束、中止 就不跟新
            if (newInfo.GameTime.Date.CompareTo(DateTime.Now.Date) > 0 && (newInfo.GameStates == "S" || newInfo.GameStates == "E" || newInfo.GameStates == "P")) return false;
            if (!newInfo.ToString().Equals(oldInfo.ToString()))
            {
                if (oldInfo.HomeBoard != null && oldInfo.HomeBoard.Count > 0 && oldInfo.AwayBoard != null && oldInfo.AwayBoard.Count > 0 && newInfo.AwayBoard != null && newInfo.AwayBoard.Count > 0 && newInfo.HomeBoard != null && newInfo.HomeBoard.Count > 0)
                {
                    try
                    {  //先判断主客队节数
                        if (newInfo.HomeBoard.Count != newInfo.AwayBoard.Count)
                        {
                            ret = false;
                        }
                        if (ret)
                        {
                            //先比较节数，新数据节数小，不更新
                            if (oldInfo.HomeBoard.Count > newInfo.HomeBoard.Count)
                            {
                                ret = false;
                            }
                            if (oldInfo.AwayBoard.Count > newInfo.AwayBoard.Count)
                            {
                                ret = false;
                            }
                        }
                        //新数据节数大于等于旧数据节数，比较每节比分数
                        if (ret)
                        {
                            for (int i = 0; i < oldInfo.HomeBoard.Count; i++)
                            {
                                //新数据分数小，不更新
                                if (Convert.ToInt32(oldInfo.HomeBoard[i]) > Convert.ToInt32(newInfo.HomeBoard[i]))
                                {
                                    ret = false;
                                    break;
                                }
                            }
                            for (int i = 0; i < oldInfo.AwayBoard.Count; i++)
                            {
                                if (Convert.ToInt32(oldInfo.AwayBoard[i]) > Convert.ToInt32(newInfo.AwayBoard[i]))
                                {
                                    ret = false;
                                    break;
                                }
                            }
                        }
                        //新数据不合理，替换掉
                        if (!ret)
                        {
                            //比分输错，在下场比赛更新过来，作为正常分数处理
                            if (oldInfo.StateValue < newInfo.StateValue)
                            {
                                ret = true;
                            }
                            else
                            {
                                newInfo = oldInfo;
                            }
                        }
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                }
                //旧赛事有比分新赛事每比分，不更新
                else if (oldInfo.HomeBoard != null && oldInfo.HomeBoard.Count > 0 && oldInfo.AwayBoard != null && oldInfo.AwayBoard.Count > 0 && newInfo.AwayBoard != null && newInfo.AwayBoard.Count == 0 && newInfo.HomeBoard != null && newInfo.HomeBoard.Count == 0)
                {
                    ret = false;
                }
            }
            else
            {
                ret = false;
            }
            return ret;
        }

        #endregion 新舊比對資料

        #region 新增隊伍/聯盟資料

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
                    catch { }
                }
            }

            // 資料有更新, 重新取得資料
            if (updated) { this.LoadAllianceAndTeamTable(); }
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
                string sql = @"IF EXISTS(SELECT AllianceID FROM OSAlliance WITH (NOLOCK) WHERE AllianceName = @AllianceName)
                                BEGIN
	                                SELECT AllianceID FROM OSAlliance WITH (NOLOCK) WHERE AllianceName = @AllianceName;
                                END
                                ELSE
                                BEGIN
	                                INSERT INTO OSAlliance(AllianceName, ShowName)
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
                string sql = @"IF EXISTS(SELECT TeamID FROM OSTeam WITH (NOLOCK) WHERE AllianceID = @AllianceID AND TeamName = @TeamName)
                                BEGIN
	                                SELECT TeamID FROM OSTeam WITH (NOLOCK) WHERE AllianceID = @AllianceID AND TeamName = @TeamName;
                                END
                                ELSE
                                BEGIN
	                                INSERT INTO OSTeam(AllianceID, TeamName, ShowName)
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

        #endregion 新增隊伍/聯盟資料

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
                sb.AppendFormat("DELETE FROM [BasketballSchedules] WHERE webid IN({0})\r\n", string.Join(",", list));
                sb.AppendFormat("end\r\n");
                result += ExecuteScalar(connectionString, sb.ToString());
                this.Logs.Update("deleteGameData:\r\n" + sb.ToString());
            }
            sb = new StringBuilder();
            if (changeDateGameData.Count > 0)
            {
                sb.AppendFormat("begin\r\n");
                List<string> list = changeDateGameData.Values.Select(p => "'" + p.WebID + "'").ToList();
                sb.AppendFormat("DELETE FROM [BasketballSchedules] WHERE webid IN({0}) and CtrlStates in(0,2)\r\n", string.Join(",", list));
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
                    if (game.GameStates == "D") continue;//如果是延迟赛事，不写入
                    sb.AppendFormat("set @webid=null\r\n");
                    sb.AppendFormat("select @webid=webid from [BasketballSchedules] with(nolock) where webid='{0}' and gametype='{1}'\r\n", game.WebID, game.GameType);
                    sb.AppendFormat("if @webid is null\r\n");
                    sb.AppendFormat("begin\r\n");
                    sb.AppendFormat("select @allianceid=allianceid from osalliance with(nolock) where alliancename='{0}'\r\n", game.AllianceName);
                    sb.AppendFormat("select @teamaid= teamid from osteam with(nolock) where teamname='{0}' and allianceid=@allianceid\r\n", game.Home);
                    sb.AppendFormat("select @teambid= teamid from osteam with(nolock) where teamname='{0}' and allianceid=@allianceid\r\n", game.Away);
                    sb.AppendFormat("INSERT INTO [BasketballSchedules] ([GameType],[AllianceID],[GameDate],[GameTime],[GameStates],[TeamAID],[TeamBID],[RunsA],[RunsB],[RA],[RB],[WebID],[TrackerText],[StatusText],[Record],[OrderBy],[IsDeleted]) VALUES ('{0}',{1},'{2}','{3}','{4}',{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16})\r\n",
                    game.GameType,
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
                    game.Status != null ? "'" + game.Status + "'" : "NULL",
                    game.Record != null ? "'" + game.Record + "'" : "NULL",
                    game.AllianceName.ToLower() == "nba" ? 1 : 0,
                    game.Display == 0 ? 1 : 0
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
            sb.AppendFormat("declare @isTracker bit\r\n");
            sb.AppendFormat("declare @webid nvarchar(50)\r\n");
            sb.AppendFormat("declare @allianceid int,@teamaid int ,@teambid int\r\n");
            //CtrlStates 1：修改狀態 2：修改时间 3：修改比分：4： 修改全部
            foreach (KeyValuePair<string, Dictionary<string, BasicInfo>> item in updateGameData)
            {
                sb.AppendFormat("begin\r\n");
                string gameDate = item.Key;
                Dictionary<string, BasicInfo> basic = item.Value;
                foreach (KeyValuePair<string, BasicInfo> pair in basic)
                {
                    BasicInfo game = pair.Value;
                    sb.AppendFormat("select @allianceid=allianceid from osalliance with(nolock) where alliancename='{0}'\r\n", game.AllianceName);
                    sb.AppendFormat("select @teamaid= teamid from osteam with(nolock) where teamname='{0}' and allianceid=@allianceid\r\n", game.Home);
                    sb.AppendFormat("select @teambid= teamid from osteam with(nolock) where teamname='{0}' and allianceid=@allianceid\r\n", game.Away);
                    if (game.GameStates == "D")//比赛推迟，但日期未确定
                    {
                        sb.AppendFormat("DELETE FROM [BasketballSchedules]  where webid='{0}' and gametype='{1}'\r\n", game.WebID, game.GameType);
                    }
                    sb.AppendFormat("select @ctrlStates=CtrlStates,@isTracker=IsTracker from [BasketballSchedules] with(nolock) where webid='{0}'\r\n", game.WebID);
                    sb.AppendFormat("IF @ctrlStates=0\r\n");//跟全部
                    sb.AppendFormat("begin\r\n");
                    sb.AppendFormat("UPDATE [BasketballSchedules] SET [GameStates] ='{0}',[RunsA] ={1},[RunsB] = {2},[RA] = {3},[RB] = {4},[TrackerText] ={5},[StatusText] = {6},[Record]={7},[GameDate]='{8}',[GameTime]='{9}',[ChangeCount] =[ChangeCount]+1,TeamAID={10},TeamBID={11},AllianceID={12} where webid={13} and gametype='{14}'\r\n",
                    game.GameStates,
                    game.HomeBoard.Count > 0 ? "'" + string.Join(",", game.HomeBoard) + "'" : "NULL",
                    game.AwayBoard.Count > 0 ? "'" + string.Join(",", game.AwayBoard) + "'" : "NULL",
                    game.HomePoint, game.AwayPoint,
                    string.Format("case when  @isTracker=1 then TrackerText else {0} end",game.TrackerText != null ? "'" + game.TrackerText + "'" : "NULL"),
                    game.Status != null ? "'" + game.Status + "'" : "NULL",
                    game.Record != null ? "'" + game.Record + "'" : "NULL",
                    game.GameTime.ToString(DATE_STRING_FORMAT),
                    game.GameTime.ToString(TIME_STRING_FORMAT),
                    "@teamaid", "@teambid", "@allianceid",
                    game.WebID, game.GameType);
                    sb.AppendFormat("end\r\n");
                    sb.AppendFormat("ELSE IF @ctrlStates=1\r\n");// 比分時間跟来源网 狀態不跟
                    sb.AppendFormat("begin\r\n");
                    sb.AppendFormat("UPDATE [BasketballSchedules] SET [RunsA] ={0},[RunsB] = {1},[RA] = {2},[RB] = {3},[GameDate]='{4}',[GameTime]='{5}',[ChangeCount] =[ChangeCount]+1,TeamAID={6},TeamBID={7},AllianceID={8} where webid='{9}' and gametype='{10}'\r\n",
                    game.HomeBoard.Count > 0 ? "'" + string.Join(",", game.HomeBoard) + "'" : "NULL",
                    game.AwayBoard.Count > 0 ? "'" + string.Join(",", game.AwayBoard) + "'" : "NULL",
                    game.HomePoint, game.AwayPoint,
                    game.GameTime.ToString(DATE_STRING_FORMAT),
                    game.GameTime.ToString(TIME_STRING_FORMAT),
                    "@teamaid", "@teambid", "@allianceid",
                    game.WebID, game.GameType);
                    sb.AppendFormat("end\r\n");
                    sb.AppendFormat("ELSE IF @ctrlStates=2\r\n");// 狀態比分跟来源网 時間不跟
                    sb.AppendFormat("begin\r\n");
                    sb.AppendFormat("UPDATE [BasketballSchedules] SET [GameStates] ='{0}',[TrackerText] ={1},[StatusText] = {2},[Record]={3},[RunsA] ={4},[RunsB] = {5},[RA] = {6},[RB] = {7},[ChangeCount] =[ChangeCount]+1,TeamAID={8},TeamBID={9},AllianceID={10} where webid='{11}' and gametype='{12}'\r\n",
                    game.GameStates,
                    string.Format("case when  @isTracker=1 then TrackerText else {0} end", game.TrackerText != null ? "'" + game.TrackerText + "'" : "NULL"),
                    game.Status != null ? "'" + game.Status + "'" : "NULL",
                    game.Record != null ? "'" + game.Record + "'" : "NULL",
                    game.HomeBoard.Count > 0 ? "'" + string.Join(",", game.HomeBoard) + "'" : "NULL",
                    game.AwayBoard.Count > 0 ? "'" + string.Join(",", game.AwayBoard) + "'" : "NULL",
                    game.HomePoint, game.AwayPoint,
                    "@teamaid", "@teambid", "@allianceid",
                    game.WebID, game.GameType);
                    sb.AppendFormat("end\r\n");
                    sb.AppendFormat("ELSE IF @ctrlStates=3\r\n");//狀態時間跟来源网 比分不跟
                    sb.AppendFormat("begin\r\n");
                    sb.AppendFormat("UPDATE [BasketballSchedules] SET [GameStates] ='{0}',[TrackerText] ={1},[StatusText] = {2},[Record]={3},[GameDate]='{4}',[GameTime]='{5}',[ChangeCount] =[ChangeCount]+1,TeamAID={6},TeamBID={7},AllianceID={8} where webid='{9}' and gametype='{10}'\r\n",
                    game.GameStates,
                    string.Format("case when  @isTracker=1 then TrackerText else {0} end", game.TrackerText != null ? "'" + game.TrackerText + "'" : "NULL"),                    
                    game.Status != null ? "'" + game.Status + "'" : "NULL",
                    game.Record != null ? "'" + game.Record + "'" : "NULL",
                    game.GameTime.ToString(DATE_STRING_FORMAT),
                    game.GameTime.ToString(TIME_STRING_FORMAT),
                    "@teamaid", "@teambid", "@allianceid",
                     game.WebID, game.GameType);
                    sb.AppendFormat("end\r\n");
                }
                sb.AppendFormat("end\r\n");
                result += ExecuteScalar(connectionString, sb.ToString());
                this.Logs.Update("updateGameData:\r\n" + sb.ToString());
                sb = new StringBuilder();
                sb.AppendFormat("declare @ctrlStates int\r\n");
                sb.AppendFormat("declare @isTracker bit\r\n");
                sb.AppendFormat("declare @webid nvarchar(50)\r\n");
                sb.AppendFormat("declare @allianceid int,@teamaid int ,@teambid int\r\n");
            }
            return result;
        }

        //protected int UpdateData(string connectionString, Dictionary<string, Dictionary<string, BasicInfo>> updateGameData, Dictionary<string, Dictionary<string, BasicInfo>> addGameData)
        //{
        //    StringBuilder sb = new StringBuilder();
        //    int result = 0;
        //    foreach (KeyValuePair<string, Dictionary<string,BasicInfo>> item in updateGameData)
        //    {
        //        sb.AppendFormat("declare @webid nvarchar(50)\r\n");
        //        sb.AppendFormat("declare @allianceid int,@teamaid int ,@teambid int\r\n");
        //        string gameDate = item.Key;
        //        Dictionary<string, BasicInfo> basic = item.Value;
        //        foreach (KeyValuePair<string,BasicInfo> pair in basic)
        //        {
        //            BasicInfo game = pair.Value;
        //            sb.AppendFormat("select @webid=webid from [BasketballSchedules] with(nolock) where webid={0} and gametype='{1}' and gamedate='{2}'\r\n", game.WebID, game.GameType, game.GameTime.ToString("yyyy-MM-dd"));
        //            sb.AppendFormat("if @webid is null\r\n");
        //            sb.AppendFormat("begin\r\n");
        //            sb.AppendFormat("select @allianceid=allianceid from osalliance with(nolock) where alliancename='{0}'\r\n", game.AllianceName);
        //            sb.AppendFormat("select @teamaid= teamid from osteam with(nolock) where teamname='{0}'\r\n", game.Home);
        //            sb.AppendFormat("select @teambid= teamid from osteam with(nolock) where teamname='{0}'\r\n", game.Away);
        //            sb.AppendFormat("INSERT INTO [BasketballSchedules] ([GameType],[AllianceID],[GameDate],[GameTime],[GameStates],[TeamAID],[TeamBID],[RunsA],[RunsB],[RA],[RB],[WebID],[TrackerText],[StatusText],[Record]) VALUES ('{0}',{1},'{2}','{3}','{4}',{5},{6},'{7}','{8}',{9},{10},{11},'{12}','{13}','{14}')\r\n", game.GameType, "@allianceid", game.GameTime.ToString("yyyy-MM-dd"),
        //            game.GameTime.ToString("HH:mm"), game.GameStates, "@teamaid", "@teambid",
        //            string.Join(",", game.HomeBoard),
        //            string.Join(",", game.AwayBoard),
        //            game.HomePoint,
        //            game.AwayPoint,
        //            game.WebID,
        //            game.TrackerText,
        //            game.Status,
        //            game.Record
        //            );
        //            sb.AppendFormat("end\r\n");
        //            sb.AppendFormat("else\r\n");
        //            sb.AppendFormat("begin\r\n");
        //            sb.AppendFormat("UPDATE [BasketballSchedules] SET [GameStates] ='{0}',[RunsA] ='{1}',[RunsB] = '{2}',[RA] = {3},[RB] = {4},[TrackerText] ='{5}',[StatusText] = '{6}',[ChangeCount] =[ChangeCount]+1 where webid={7} and gametype='{8}' and gamedate='{9}'\r\n",
        //            game.GameStates,
        //            string.Join(",", game.HomeBoard),
        //            string.Join(",", game.AwayBoard),
        //            game.HomePoint, game.AwayPoint,
        //            game.TrackerText,
        //            game.Status, game.WebID, game.GameType, game.GameTime.ToString("yyyy-MM-dd"));
        //            sb.AppendFormat("end\r\n");
        //        }
        //        result += ExecuteScalar(connectionString, sb.ToString());
        //        sb = new StringBuilder();
        //    }
        //    return result;
        //}
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

                    string sql = @"SELECT AllianceID, AllianceName, ShowName FROM OSAlliance WITH (NOLOCK)";
                    LoadData(conn, sql, ref dtAlliance);

                    sql = @"SELECT TeamID, TeamName, T.ShowName, T.AllianceID, A.AllianceName FROM OSTeam T WITH (NOLOCK)
                            JOIN OSAlliance A WITH (NOLOCK) ON T.AllianceID = A.AllianceID";
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


        #region 從資料庫取得舊資料

        /// <summary>
        /// 從資料庫取得每日的舊資料
        /// </summary>
        /// <returns></returns>
        private void GetOldGames()
        {
            try
            {
                // 昨天/未來一周

                DataTable dt = this.GetData(frmMain.ConnectionString, this.GameType, this.GameDate.AddDays(startIndex), this.GameDate.AddDays(endIndex));
                for (int i = startIndex; i <= endIndex; i++)
                {
                    var rows = from DataRow x in dt.AsEnumerable()
                               where x.Field<DateTime>("GameDate").ToString(DATE_STRING_FORMAT).Equals(this.GameDate.AddDays(i).ToString(DATE_STRING_FORMAT))
                               select x;
                    Dictionary<string, BasicInfo> basic = new Dictionary<string, BasicInfo>();
                    foreach (DataRow item in rows)
                    {
                        BasicInfo info = new BasicInfo(Convert.ToInt32(item["AllianceID"]), GameType, Convert.ToDateTime(item.Field<DateTime>("GameDate").ToString(DATE_STRING_FORMAT) + " " + item["GameTime"]), item["WebID"].ToString());
                        info.AllianceName = item["alliancename"].ToString();
                        info.GameStates = item["GameStates"].ToString();

                        if (item["StatusText"].ToString() != "")
                        {
                            info.Status = item["StatusText"].ToString();
                        }
                        if (item["GameStates"].ToString() != "")
                        {
                            info.GameStates = item["GameStates"].ToString();
                        }
                        if (item["TrackerText"].ToString() != "")
                        {
                            info.TrackerText = item["TrackerText"].ToString();
                        }
                        if (item["Record"].ToString() != "")
                        {
                            info.Record = item["Record"].ToString();
                        }
                        info.AcH = AcH;
                        if (AcH)
                        {
                            info.Away = item["away"].ToString();
                            info.Home = item["home"].ToString();
                            info.AwayID = item["TeamAID"].ToString();
                            info.HomeID = item["TeamBID"].ToString();
                            if (item["RunsB"].ToString() != "")
                            {
                                info.AwayBoard.AddRange(item["RunsB"].ToString().Split(','));
                            }
                            if (item["RunsA"].ToString() != "")
                            {
                                info.HomeBoard.AddRange(item["RunsA"].ToString().Split(','));
                            }
                            info.AwayPoint = item["RB"].ToString();
                            info.HomePoint = item["RA"].ToString();
                        }
                        //else
                        //{
                        //    info.Away = item["home"].ToString();
                        //    info.Home = item["away"].ToString();
                        //    if (item["RunsA"].ToString() != "")
                        //    {
                        //        info.AwayBoard.AddRange(item["RunsA"].ToString().Split(','));
                        //    }
                        //    if (item["RunsB"].ToString() != "")
                        //    {
                        //        info.HomeBoard.AddRange(item["RunsB"].ToString().Split(','));
                        //    }
                        //    info.AwayPoint = item["RA"].ToString();
                        //    info.HomePoint = item["RB"].ToString();
                        //}
                        basic[info.WebID] = info;
                    }
                    if (basic.Count > 0)
                    {
                        gameData[this.GameDate.AddDays(i).ToString(DATE_STRING_FORMAT)] = basic;
                    }
                }
            }
            catch { }
        }

        #endregion 從資料庫取得舊資料
    }

    public class BkOSDownload : BasicDownload
    {
        // 更新時間
        public int UpdateSecond { set; get; }

        // 抓取賽事時間
        public DateTime GameDate { private set; get; }

        // 建構式
        public BkOSDownload(ESport sport, DateTime gameDate, string url, Encoding encoding, string fileType, int updateSecond = 60)
            : base(sport, url, encoding, fileType)
        {
            this.UpdateSecond = updateSecond;
            this.GameDate = gameDate;
        }
    }
}