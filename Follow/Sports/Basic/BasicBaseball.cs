using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Follow.Sports.Basic
{
    /// <summary>
    /// 棒球跟盤基本功能。
    /// </summary>
    public partial class BasicBaseball : BasicFollow
    {
        public BasicBaseball(ESport sport) : base(sport) { this.UpdataWriteLine = true; }

        protected bool CheckGame(BasicInfo gameInfo)//檢察比賽
        {
            if (!this.GameData.ContainsKey(gameInfo.WebID))//新資料
                return true;

            BasicInfo oldInfo = this.GameData[gameInfo.WebID];
            if (oldInfo.GameStates == "X" && gameInfo.GameStates == "S")//從未開賽轉變成已開賽
                return true;

            if (gameInfo.GameStates == oldInfo.GameStates && //狀態一樣
                gameInfo.Quarter > oldInfo.Quarter + 1)//局數差異2局以上
                return false;

            int runCount = oldInfo.HomeBoard.Count;
            if (runCount > 2)//超過3局，檢查主隊比分是否吻合
            {
                int run1, run2;
                runCount--;//不計算最新一局，避免
                for (int i = 0; i < runCount; i++)
                {
                    if (gameInfo.HomeBoard.Count >= (i + 1))
                    {
                        if (int.TryParse(gameInfo.HomeBoard[i], out run1) && int.TryParse(oldInfo.HomeBoard[i], out run2))
                        {
                            if (run1 != run2)
                                return false;
                        }
                    }
                }
            }

            runCount = oldInfo.AwayBoard.Count;
            if (runCount > 2)//超過3局，檢查客隊比分是否吻合
            {
                int run1, run2;
                runCount--;//不計算最新一局，避免
                for (int i = 0; i < runCount; i++)
                {
                    if (gameInfo.AwayBoard.Count >= (i + 1))
                    {
                        if (int.TryParse(gameInfo.AwayBoard[i], out run1) && int.TryParse(oldInfo.AwayBoard[i], out run2))
                        {
                            if (run1 != run2)
                                return false;
                        }
                    }
                }
            }

            return true;
        }

        #region 更新賽事資訊

        public override bool Update(string connectionString, BasicInfo info)
        {
            // 沒有資料就離開
            if (info == null)
                return false;
            if (info.GameStates == "X")
                return false;

            string Sql = "UPDATE [BaseballSchedules] WITH(ROWLOCK)" + "\r\n"
                       + "   SET [RunsA] = @RunsA" + "\r\n"
                       + "      ,[RunsB] = @RunsB" + "\r\n"
                       + "      ,[StatusText] = @StatusText" + "\r\n"
                       + "      ,[GameStates] = @GameStates" + "\r\n"
                       + "      ,[B] = @B" + "\r\n"
                       + "      ,[S] = @S" + "\r\n"
                       + "      ,[O] = @O" + "\r\n"
                       + "      ,[Bases] = @Bases" + "\r\n"
                       + "      ,[RA] = @RA" + "\r\n"
                       + "      ,[HA] = @HA" + "\r\n"
                       + "      ,[EA] = @EA" + "\r\n"
                       + "      ,[RB] = @RB" + "\r\n"
                       + "      ,[HB] = @HB" + "\r\n"
                       + "      ,[EB] = @EB" + "\r\n"
                       + "      ,[TrackerText] = @TrackerText" + "\r\n"
                       + "      ,[ChangeCount] = [ChangeCount] + 1" + "\r\n"
                       + " WHERE ([GameType] = @GameType)" + "\r\n"
                       + "   AND ([WebID] = @WebID)" + "\r\n"
                       + "   AND ([CtrlStates] = 2)" + "\r\n"
                       + "   AND ([IsDeleted] = 0)" + "\r\n";
            SqlConnection conn = null;
            SqlCommand cmd = null;
            bool result = false;
            // 錯誤處理
            try
            {
                conn = new SqlConnection(connectionString);
                cmd = new SqlCommand(Sql, conn);
                // 參數
                cmd.Parameters.Add(new SqlParameter("@GameType", info.GameType));
                cmd.Parameters.Add(new SqlParameter("@GameStates", info.GameStates));
                cmd.Parameters.Add(new SqlParameter("@WebID", info.WebID));
                cmd.Parameters.Add(new SqlParameter("@RunsA", DBNull.Value));
                cmd.Parameters.Add(new SqlParameter("@RunsB", DBNull.Value));
                cmd.Parameters.Add(new SqlParameter("@StatusText", DBNull.Value));
                cmd.Parameters.Add(new SqlParameter("@Record", DBNull.Value));
                cmd.Parameters.Add(new SqlParameter("@B", info.BallB));
                cmd.Parameters.Add(new SqlParameter("@S", info.BallS));
                cmd.Parameters.Add(new SqlParameter("@O", info.BallO));
                cmd.Parameters.Add(new SqlParameter("@Bases", info.Bases));
                cmd.Parameters.Add(new SqlParameter("@RA", DBNull.Value));
                cmd.Parameters.Add(new SqlParameter("@HA", DBNull.Value));
                cmd.Parameters.Add(new SqlParameter("@EA", DBNull.Value));
                cmd.Parameters.Add(new SqlParameter("@RB", DBNull.Value));
                cmd.Parameters.Add(new SqlParameter("@HB", DBNull.Value));
                cmd.Parameters.Add(new SqlParameter("@EB", DBNull.Value));
                cmd.Parameters.Add(new SqlParameter("@TrackerText", DBNull.Value));
                // 分數 (主客交換)
                if (!string.IsNullOrEmpty(info.AwayPoint) && !string.IsNullOrEmpty(info.HomePoint))
                {
                    cmd.Parameters["@RunsA"].Value = BasicInfo.ConvertBoard((info.AcH) ? (info.HomeBoard) : (info.AwayBoard));
                    cmd.Parameters["@RunsB"].Value = BasicInfo.ConvertBoard((info.AcH) ? (info.AwayBoard) : (info.HomeBoard));
                    cmd.Parameters["@RA"].Value = (info.AcH) ? (info.HomePoint) : (info.AwayPoint);
                    cmd.Parameters["@HA"].Value = (info.AcH) ? (info.HomeH) : (info.AwayH);
                    cmd.Parameters["@EA"].Value = (info.AcH) ? (info.HomeE) : (info.AwayE);
                    cmd.Parameters["@RB"].Value = (info.AcH) ? (info.AwayPoint) : (info.HomePoint);
                    cmd.Parameters["@HB"].Value = (info.AcH) ? (info.AwayH) : (info.HomeH);
                    cmd.Parameters["@EB"].Value = (info.AcH) ? (info.AwayE) : (info.HomeE);
                }
                // 狀態
                if (!string.IsNullOrEmpty(info.Status))
                {
                    cmd.Parameters["@StatusText"].Value = info.Status;
                }
                // 訊息
                if (!string.IsNullOrEmpty(info.TrackerText))
                {
                    GetTrackerText(info);
                    cmd.Parameters["@TrackerText"].Value = info.TrackerText;
                }
                // 開啟
                conn.Open();
                // 執行
                result = (cmd.ExecuteNonQuery() > 0);
                conn.Close();
            }
            catch (Exception ex)
            {
                string games = string.Format("GameType:{0}, WebID:{1}", info.GameType, info.WebID);
                this.Logs.Error("Update Error!\r\nMessage:{0},\r\nStackTrace:{1}\r\n {2}", ex.Message, ex.StackTrace, games);
            }
            // 沒有關閉資料庫連接就關閉連接
            if (conn != null && conn.State == ConnectionState.Open)
            {
                conn.Close();
            }
            // 傳回
            return result;
        }
        protected bool Update2(string connectionString, BasicInfo info)
        {
            // 沒有資料就離開
            if (info == null)
                return false;
            if (info.GameStates == "X")
                return false;

            string Sql = "UPDATE [BaseballSchedules] WITH(ROWLOCK)" + "\r\n"
                           + "   SET [RunsA] = @RunsA" + "\r\n"
                           + "      ,[RunsB] = @RunsB" + "\r\n"
                           + "      ,[StatusText] = @StatusText" + "\r\n"
                           + "      ,[GameStates] = @GameStates" + "\r\n"
                           + "      ,[B] = @B" + "\r\n"
                           + "      ,[S] = @S" + "\r\n"
                           + "      ,[O] = @O" + "\r\n"
                           + "      ,[Bases] = @Bases" + "\r\n"
                           + "      ,[RA] = @RA" + "\r\n"
                           + "      ,[HA] = @HA" + "\r\n"
                           + "      ,[EA] = @EA" + "\r\n"
                           + "      ,[RB] = @RB" + "\r\n"
                           + "      ,[HB] = @HB" + "\r\n"
                           + "      ,[EB] = @EB" + "\r\n"
                           + "      ,[TrackerText] = @TrackerText" + "\r\n"
                           + "      ,[ChangeCount] = [ChangeCount] + 1" + "\r\n"
                           + " WHERE ([GameType] = @GameType)" + "\r\n"
                           + "   AND ([GameDate] = @GameDate)" + "\r\n"
                           + "   AND ([CtrlStates] = 2)" + "\r\n"
                           + "   AND ([IsDeleted] = 0)" + "\r\n"
                           + "   AND ([TeamAID] = (SELECT [TeamID] FROM [BaseballTeam] WITH (NOLOCK) WHERE ([WebName] = @TeamA)))" + "\r\n"
                           + "   AND ([TeamBID] = (SELECT [TeamID] FROM [BaseballTeam] WITH (NOLOCK) WHERE ([WebName] = @TeamB)))";
            SqlConnection conn = null;
            SqlCommand cmd = null;
            bool result = false;
            // 錯誤處理
            try
            {
                conn = new SqlConnection(connectionString);
                cmd = new SqlCommand(Sql, conn);
                // 參數
                cmd.Parameters.Add(new SqlParameter("@GameType", info.GameType));
                cmd.Parameters.Add(new SqlParameter("@GameDate", info.GameTime.Date));
                cmd.Parameters.Add(new SqlParameter("@GameStates", info.GameStates));
                cmd.Parameters.Add(new SqlParameter("@RunsA", DBNull.Value));
                cmd.Parameters.Add(new SqlParameter("@RunsB", DBNull.Value));
                cmd.Parameters.Add(new SqlParameter("@StatusText", DBNull.Value));
                cmd.Parameters.Add(new SqlParameter("@Record", DBNull.Value));
                cmd.Parameters.Add(new SqlParameter("@TeamA", DBNull.Value));
                cmd.Parameters.Add(new SqlParameter("@TeamB", DBNull.Value));
                cmd.Parameters.Add(new SqlParameter("@B", info.BallB));
                cmd.Parameters.Add(new SqlParameter("@S", info.BallS));
                cmd.Parameters.Add(new SqlParameter("@O", info.BallO));
                cmd.Parameters.Add(new SqlParameter("@Bases", info.Bases));
                cmd.Parameters.Add(new SqlParameter("@RA", DBNull.Value));
                cmd.Parameters.Add(new SqlParameter("@HA", DBNull.Value));
                cmd.Parameters.Add(new SqlParameter("@EA", DBNull.Value));
                cmd.Parameters.Add(new SqlParameter("@RB", DBNull.Value));
                cmd.Parameters.Add(new SqlParameter("@HB", DBNull.Value));
                cmd.Parameters.Add(new SqlParameter("@EB", DBNull.Value));
                cmd.Parameters.Add(new SqlParameter("@TrackerText", DBNull.Value));
                // 隊伍 (主客交換)
                cmd.Parameters["@TeamA"].Value = (info.AcH) ? (info.Home) : (info.Away);
                cmd.Parameters["@TeamB"].Value = (info.AcH) ? (info.Away) : (info.Home);
                // 分數 (主客交換)
                if (!string.IsNullOrEmpty(info.AwayPoint) && !string.IsNullOrEmpty(info.HomePoint))
                {
                    cmd.Parameters["@RunsA"].Value = BasicInfo.ConvertBoard((info.AcH) ? (info.HomeBoard) : (info.AwayBoard));
                    cmd.Parameters["@RunsB"].Value = BasicInfo.ConvertBoard((info.AcH) ? (info.AwayBoard) : (info.HomeBoard));
                    cmd.Parameters["@RA"].Value = (info.AcH) ? (info.HomePoint) : (info.AwayPoint);
                    cmd.Parameters["@HA"].Value = (info.AcH) ? (info.HomeH) : (info.AwayH);
                    cmd.Parameters["@EA"].Value = (info.AcH) ? (info.HomeE) : (info.AwayE);
                    cmd.Parameters["@RB"].Value = (info.AcH) ? (info.AwayPoint) : (info.HomePoint);
                    cmd.Parameters["@HB"].Value = (info.AcH) ? (info.AwayH) : (info.HomeH);
                    cmd.Parameters["@EB"].Value = (info.AcH) ? (info.AwayE) : (info.HomeE);
                }
                // 狀態
                if (!string.IsNullOrEmpty(info.Status))
                {
                    cmd.Parameters["@StatusText"].Value = info.Status;
                }
                // 訊息
                if (!string.IsNullOrEmpty(info.TrackerText))
                {
                    GetTrackerText(info);
                    cmd.Parameters["@TrackerText"].Value = info.TrackerText;
                }
                // 開啟
                conn.Open();
                // 執行
                result = (cmd.ExecuteNonQuery() > 0);
                conn.Close();
            }
            catch (Exception ex)
            {
                string games = string.Format("GameType:{0}, GameTime:{1}, Home:{2}, Away:{3}", info.GameType, info.GameTime.Date, info.Home, info.Away);
                this.Logs.Error("Update2 Error!\r\nMessage:{0},\r\nStackTrace:{1}\r\n {2}", ex.Message, ex.StackTrace, games);
            }
            // 沒有關閉資料庫連接就關閉連接
            if (conn != null && conn.State == ConnectionState.Open)
            {
                conn.Close();
            }
            // 傳回
            return result;
        }

        // 原使用隊伍比對(MLB-ESPN), 統一改用多來源跟分
        /// <summary>
        /// 儲存: 使用多來源跟分 (比對開賽時間)
        /// </summary>
        protected bool Update3(string connectionString, BasicInfo info)
        {
            // 沒有資料就離開
            if (info == null)
                return false;
            if (info.GameStates == "X")
                return false;

            string Sql = "UPDATE [BaseballSchedules] WITH(ROWLOCK)" + "\r\n"
                           + "   SET [RunsA] = @RunsA" + "\r\n"
                           + "      ,[RunsB] = @RunsB" + "\r\n"
                           + "      ,[StatusText] = @StatusText" + "\r\n"
                           + "      ,[GameStates] = @GameStates" + "\r\n"
                           + "      ,[B] = @B" + "\r\n"
                           + "      ,[S] = @S" + "\r\n"
                           + "      ,[O] = @O" + "\r\n"
                           + "      ,[Bases] = @Bases" + "\r\n"
                           + "      ,[RA] = @RA" + "\r\n"
                           + "      ,[HA] = @HA" + "\r\n"
                           + "      ,[EA] = @EA" + "\r\n"
                           + "      ,[RB] = @RB" + "\r\n"
                           + "      ,[HB] = @HB" + "\r\n"
                           + "      ,[EB] = @EB" + "\r\n"
                           + "      ,[TrackerText] = @TrackerText" + "\r\n"
                           + "      ,[ChangeCount] = [ChangeCount] + 1" + "\r\n"
                           + " WHERE ([GameType] = @GameType)" + "\r\n"
                           + "   AND ([GameDate] = @GameDate)" + "\r\n"
                           + "   AND ([GameTime] BETWEEN @GameTime1 AND @GameTime2)" + "\r\n"//尚未結束
                           + "   AND ([CtrlStates] = 2)" + "\r\n"
                           + "   AND ([IsDeleted] = 0)" + "\r\n"
                           + "   AND ([TeamAID] = (SELECT [TeamID] FROM [BaseballTeam] WITH (NOLOCK) WHERE ([IsDeleted] = 0) AND ([GameType] = @GameType) AND ((SELECT COUNT(1) FROM [dbo].[fn_splitParameterToTable]([WebName], ',') WHERE COL1 = @TeamA) > 0)))" + "\r\n"
                           + "   AND ([TeamBID] = (SELECT [TeamID] FROM [BaseballTeam] WITH (NOLOCK) WHERE ([IsDeleted] = 0) AND ([GameType] = @GameType) AND ((SELECT COUNT(1) FROM [dbo].[fn_splitParameterToTable]([WebName], ',') WHERE COL1 = @TeamB) > 0)))";

            SqlConnection conn = null;
            SqlCommand cmd = null;
            bool result = false;
            // 錯誤處理
            try
            {
                conn = new SqlConnection(connectionString);
                cmd = new SqlCommand(Sql, conn);
                // 參數
                cmd.Parameters.Add(new SqlParameter("@GameType", info.GameType));
                cmd.Parameters.Add(new SqlParameter("@GameDate", info.GameTime.Date));

                if (info.GameTime.Hour == 0)//開賽時間是0點
                    cmd.Parameters.Add(new SqlParameter("@GameTime1", info.GameTime.ToString("HH:00")));
                else
                    cmd.Parameters.Add(new SqlParameter("@GameTime1", info.GameTime.AddHours(-1).ToString("HH:00")));

                cmd.Parameters.Add(new SqlParameter("@GameTime2", info.GameTime.AddHours(1).ToString("HH:00")));
                cmd.Parameters.Add(new SqlParameter("@GameStates", info.GameStates));
                cmd.Parameters.Add(new SqlParameter("@RunsA", DBNull.Value));
                cmd.Parameters.Add(new SqlParameter("@RunsB", DBNull.Value));
                cmd.Parameters.Add(new SqlParameter("@StatusText", DBNull.Value));
                cmd.Parameters.Add(new SqlParameter("@Record", DBNull.Value));
                cmd.Parameters.Add(new SqlParameter("@TeamA", DBNull.Value));
                cmd.Parameters.Add(new SqlParameter("@TeamB", DBNull.Value));
                cmd.Parameters.Add(new SqlParameter("@B", info.BallB));
                cmd.Parameters.Add(new SqlParameter("@S", info.BallS));
                cmd.Parameters.Add(new SqlParameter("@O", info.BallO));
                cmd.Parameters.Add(new SqlParameter("@Bases", info.Bases));
                cmd.Parameters.Add(new SqlParameter("@RA", DBNull.Value));
                cmd.Parameters.Add(new SqlParameter("@HA", DBNull.Value));
                cmd.Parameters.Add(new SqlParameter("@EA", DBNull.Value));
                cmd.Parameters.Add(new SqlParameter("@RB", DBNull.Value));
                cmd.Parameters.Add(new SqlParameter("@HB", DBNull.Value));
                cmd.Parameters.Add(new SqlParameter("@EB", DBNull.Value));
                cmd.Parameters.Add(new SqlParameter("@TrackerText", DBNull.Value));

                // 隊伍 (主客交換)
                cmd.Parameters["@TeamA"].Value = (info.AcH) ? (info.Home) : (info.Away);
                cmd.Parameters["@TeamB"].Value = (info.AcH) ? (info.Away) : (info.Home);

                // 分數 (主客交換)
                if (!string.IsNullOrEmpty(info.AwayPoint) && !string.IsNullOrEmpty(info.HomePoint))
                {
                    cmd.Parameters["@RunsA"].Value = BasicInfo.ConvertBoard((info.AcH) ? (info.HomeBoard) : (info.AwayBoard));
                    cmd.Parameters["@RunsB"].Value = BasicInfo.ConvertBoard((info.AcH) ? (info.AwayBoard) : (info.HomeBoard));
                    cmd.Parameters["@RA"].Value = (info.AcH) ? (info.HomePoint) : (info.AwayPoint);
                    cmd.Parameters["@HA"].Value = (info.AcH) ? (info.HomeH) : (info.AwayH);
                    cmd.Parameters["@EA"].Value = (info.AcH) ? (info.HomeE) : (info.AwayE);
                    cmd.Parameters["@RB"].Value = (info.AcH) ? (info.AwayPoint) : (info.HomePoint);
                    cmd.Parameters["@HB"].Value = (info.AcH) ? (info.AwayH) : (info.HomeH);
                    cmd.Parameters["@EB"].Value = (info.AcH) ? (info.AwayE) : (info.HomeE);
                }
                // 狀態
                if (!string.IsNullOrEmpty(info.Status))
                {
                    cmd.Parameters["@StatusText"].Value = info.Status;
                }
                // 訊息
                if (!string.IsNullOrEmpty(info.TrackerText))
                {
                    GetTrackerText(info);
                    cmd.Parameters["@TrackerText"].Value = info.TrackerText;
                }
                // 開啟
                conn.Open();
                // 執行
                result = (cmd.ExecuteNonQuery() > 0);
                conn.Close();
            }
            catch (Exception ex)
            {
                string games = string.Format("GameType:{0}, GameTime:{1}, Home:{2}, Away:{3}", info.GameType, info.GameTime.Date, info.Home, info.Away);
                this.Logs.Error("Update3 Error!\r\nMessage:{0},\r\nStackTrace:{1}\r\n {2}", ex.Message, ex.StackTrace, games);
            }
            // 沒有關閉資料庫連接就關閉連接
            if (conn != null && conn.State == ConnectionState.Open)
            {
                conn.Close();
            }
            // 傳回
            return result;
        }

        /// <summary>
        /// 儲存: 多來源跟分 (不比對開賽時間)
        /// </summary>
        protected bool Update4(string connectionString, BasicInfo info)
        {
            // 沒有資料就離開
            if (info == null)
                return false;
            if (info.GameStates == "X")
                return false;

            string Sql = "UPDATE [BaseballSchedules] WITH(ROWLOCK)" + "\r\n"
                           + "   SET [RunsA] = @RunsA" + "\r\n"
                           + "      ,[RunsB] = @RunsB" + "\r\n"
                           + "      ,[StatusText] = @StatusText" + "\r\n"
                           + "      ,[GameStates] = @GameStates" + "\r\n"
                           + "      ,[B] = @B" + "\r\n"
                           + "      ,[S] = @S" + "\r\n"
                           + "      ,[O] = @O" + "\r\n"
                           + "      ,[Bases] = @Bases" + "\r\n"
                           + "      ,[RA] = @RA" + "\r\n"
                           + "      ,[HA] = @HA" + "\r\n"
                           + "      ,[EA] = @EA" + "\r\n"
                           + "      ,[RB] = @RB" + "\r\n"
                           + "      ,[HB] = @HB" + "\r\n"
                           + "      ,[EB] = @EB" + "\r\n"
                           + "      ,[TrackerText] = @TrackerText" + "\r\n"
                           + "      ,[ChangeCount] = [ChangeCount] + 1" + "\r\n"
                           + " WHERE ([GameType] = @GameType)" + "\r\n"
                           + "   AND ([GameDate] = @GameDate)" + "\r\n"
                           + "   AND ([CtrlStates] = 2)" + "\r\n"
                           + "   AND ([IsDeleted] = 0)" + "\r\n"
                           + "   AND ([TeamAID] = (SELECT [TeamID] FROM [BaseballTeam] WITH (NOLOCK) WHERE ([IsDeleted] = 0) AND ([GameType] = @GameType) AND ((SELECT COUNT(1) FROM [dbo].[fn_splitParameterToTable]([WebName], ',') WHERE COL1 = @TeamA) > 0)))" + "\r\n"
                           + "   AND ([TeamBID] = (SELECT [TeamID] FROM [BaseballTeam] WITH (NOLOCK) WHERE ([IsDeleted] = 0) AND ([GameType] = @GameType) AND ((SELECT COUNT(1) FROM [dbo].[fn_splitParameterToTable]([WebName], ',') WHERE COL1 = @TeamB) > 0)))";
            SqlConnection conn = null;
            SqlCommand cmd = null;
            bool result = false;
            // 錯誤處理
            try
            {
                conn = new SqlConnection(connectionString);
                cmd = new SqlCommand(Sql, conn);
                // 參數
                cmd.Parameters.Add(new SqlParameter("@GameType", info.GameType));
                cmd.Parameters.Add(new SqlParameter("@GameDate", info.GameTime.Date));
                cmd.Parameters.Add(new SqlParameter("@GameStates", info.GameStates));
                cmd.Parameters.Add(new SqlParameter("@RunsA", DBNull.Value));
                cmd.Parameters.Add(new SqlParameter("@RunsB", DBNull.Value));
                cmd.Parameters.Add(new SqlParameter("@StatusText", DBNull.Value));
                cmd.Parameters.Add(new SqlParameter("@Record", DBNull.Value));
                cmd.Parameters.Add(new SqlParameter("@TeamA", DBNull.Value));
                cmd.Parameters.Add(new SqlParameter("@TeamB", DBNull.Value));
                cmd.Parameters.Add(new SqlParameter("@B", info.BallB));
                cmd.Parameters.Add(new SqlParameter("@S", info.BallS));
                cmd.Parameters.Add(new SqlParameter("@O", info.BallO));
                cmd.Parameters.Add(new SqlParameter("@Bases", info.Bases));
                cmd.Parameters.Add(new SqlParameter("@RA", DBNull.Value));
                cmd.Parameters.Add(new SqlParameter("@HA", DBNull.Value));
                cmd.Parameters.Add(new SqlParameter("@EA", DBNull.Value));
                cmd.Parameters.Add(new SqlParameter("@RB", DBNull.Value));
                cmd.Parameters.Add(new SqlParameter("@HB", DBNull.Value));
                cmd.Parameters.Add(new SqlParameter("@EB", DBNull.Value));
                cmd.Parameters.Add(new SqlParameter("@TrackerText", DBNull.Value));
                // 隊伍 (主客交換)
                cmd.Parameters["@TeamA"].Value = (info.AcH) ? (info.Home) : (info.Away);
                cmd.Parameters["@TeamB"].Value = (info.AcH) ? (info.Away) : (info.Home);
                // 分數 (主客交換)
                if (!string.IsNullOrEmpty(info.AwayPoint) && !string.IsNullOrEmpty(info.HomePoint))
                {
                    cmd.Parameters["@RunsA"].Value = BasicInfo.ConvertBoard((info.AcH) ? (info.HomeBoard) : (info.AwayBoard));
                    cmd.Parameters["@RunsB"].Value = BasicInfo.ConvertBoard((info.AcH) ? (info.AwayBoard) : (info.HomeBoard));
                    cmd.Parameters["@RA"].Value = (info.AcH) ? (info.HomePoint) : (info.AwayPoint);
                    cmd.Parameters["@HA"].Value = (info.AcH) ? (info.HomeH) : (info.AwayH);
                    cmd.Parameters["@EA"].Value = (info.AcH) ? (info.HomeE) : (info.AwayE);
                    cmd.Parameters["@RB"].Value = (info.AcH) ? (info.AwayPoint) : (info.HomePoint);
                    cmd.Parameters["@HB"].Value = (info.AcH) ? (info.AwayH) : (info.HomeH);
                    cmd.Parameters["@EB"].Value = (info.AcH) ? (info.AwayE) : (info.HomeE);
                }
                // 狀態
                if (!string.IsNullOrEmpty(info.Status))
                {
                    cmd.Parameters["@StatusText"].Value = info.Status;
                }
                // 訊息
                if (!string.IsNullOrEmpty(info.TrackerText))
                {
                    GetTrackerText(info);
                    cmd.Parameters["@TrackerText"].Value = info.TrackerText;
                }
                // 開啟
                conn.Open();
                // 執行
                result = (cmd.ExecuteNonQuery() > 0);
                conn.Close();
            }
            catch (Exception ex)
            {
                string games = string.Format("GameType:{0}, GameTime:{1}, Home:{2}, Away:{3}", info.GameType, info.GameTime.Date, info.Home, info.Away);
                this.Logs.Error("Update2 Error!\r\nMessage:{0},\r\nStackTrace:{1}\r\n {2}", ex.Message, ex.StackTrace, games);
            }
            // 沒有關閉資料庫連接就關閉連接
            if (conn != null && conn.State == ConnectionState.Open)
            {
                conn.Close();
            }
            // 傳回
            return result;
        }


        #endregion

        #region 取得遺漏比分

        /// <summary>
        /// 取得遺漏比分
        /// </summary>
        /// <param name="game">賽事資料</param>
        /// <param name="inning">局數</param>
        /// <param name="compareTeamWithWebName">是否用 WebName 比較隊伍
        /// <para>如果為 false, 直接用隊伍名稱比對</para>
        /// </param>
        /// <param name="compareGameTime">是否比對開賽時間</param>
        protected void GetGameScore(BasicInfo game, int inning, bool compareTeamWithWebName = false, bool compareGameTime = false)//取得遺漏比分
        {
            List<string> AwayBoard = new List<string>();
            List<string> HomeBoard = new List<string>();
            if (this.GameData.ContainsKey(game.WebID))
            {
                AwayBoard = this.GameData[game.WebID].AwayBoard;
                HomeBoard = this.GameData[game.WebID].HomeBoard;

                game.AwayPoint = this.GameData[game.WebID].AwayPoint;
                game.HomePoint = this.GameData[game.WebID].HomePoint;

                game.AwayH = this.GameData[game.WebID].AwayH;
                game.HomeH = this.GameData[game.WebID].HomeH;

                game.AwayE = this.GameData[game.WebID].AwayE;
                game.HomeE = this.GameData[game.WebID].HomeE;
            }
            else
            {
                DataTable dt = null;
                if (!compareTeamWithWebName)
                {
                    // 用隊伍名比對
                    dt = GetGameDBInfo(game.Home, game.Away, game.GameTime, compareGameTime);
                }
                else
                {
                    // 用 WebName 比對
                    dt = GetGameDBInfoByWebName(game.Home, game.Away, game.GameTime, compareGameTime);
                }

                //從資料庫取得舊資料
                if (dt != null && dt.Rows.Count > 0)
                {
                    DataRow row = dt.Rows[0];

                    if (dt.Columns.Contains("RunsA") && dt.Columns.Contains("RunsB"))
                    {
                        string RunsA = row.Field<string>("RunsA") ?? String.Empty;
                        AwayBoard = new List<string>(RunsA.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries));

                        string RunsB = row.Field<string>("RunsB") ?? String.Empty;
                        HomeBoard = new List<string>(RunsB.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries));
                    }

                    //R.H.E 
                    if (dt.Columns.Contains("RA") && dt.Columns.Contains("RB") &&
                        dt.Columns.Contains("HA") && dt.Columns.Contains("HB") &&
                        dt.Columns.Contains("EA") && dt.Columns.Contains("EB"))
                    {
                        game.AwayPoint = row.Field<int?>("RA").GetValueOrDefault(0).ToString();
                        game.HomePoint = row.Field<int?>("RB").GetValueOrDefault(0).ToString();

                        game.AwayH = row.Field<int?>("HA").GetValueOrDefault(0).ToString();
                        game.HomeH = row.Field<int?>("HB").GetValueOrDefault(0).ToString();

                        game.AwayE = row.Field<int?>("EA").GetValueOrDefault(0).ToString();
                        game.HomeE = row.Field<int?>("EB").GetValueOrDefault(0).ToString();
                    }
                }
            }

            if (AwayBoard.Count > 0 && HomeBoard.Count > 0)
            {
                if (inning == -1)//未設定局數，取全部資料
                    inning = AwayBoard.Count;

                for (int i = 0; i < inning; i++)
                {
                    string oldscore = AwayBoard[i];
                    if (!string.IsNullOrEmpty(oldscore))
                        game.AwayBoard.Add(oldscore);

                    if (i >= HomeBoard.Count)//超過資料限界
                        break;

                    oldscore = HomeBoard[i];
                    if (!string.IsNullOrEmpty(oldscore))
                        game.HomeBoard.Add(oldscore);
                }
            }
        }

        /// <summary>
        /// 從資料庫取得舊資料
        /// </summary>
        /// <param name="TeamHome">主隊名稱</param>
        /// <param name="TeamAway">客隊名稱</param>
        /// <param name="GameTime">比賽時間</param>
        /// <param name="compareGameTime">是否比對開賽時間</param>
        /// <returns>資料表</returns>
        private DataTable GetGameDBInfo(string TeamHome, string TeamAway, DateTime GameTime, bool compareGameTime)
        {
            string sSqlCommand = "SELECT [GID],[GameType],[GameStates],[GameDate],[GameTime],[TeamAID],[TeamBID],[RunsA],[RunsB]" + "\r\n"
                               + ",[RA],[RB],[EA],[EB],[HA],[HB]" + "\r\n"
                               + "FROM [dbo].[BaseballSchedules] WITH (NOLOCK)" + "\r\n"
                               + "WHERE ([GameType] = @GameType)" + "\r\n"
                               + "  AND ([GameDate] = @GameDate)" + "\r\n"
                               + "  AND ([CtrlStates] = 2)" + "\r\n"
                               + "  AND ([IsDeleted] = 0)" + "\r\n"
                               + "  AND ([TeamAID] = (SELECT [TeamID] FROM [BaseballTeam] WITH (NOLOCK) WHERE ([TeamName] LIKE @TeamA) AND ([IsDeleted] = 0) AND ([GameType] = @GameType)))" + "\r\n"
                               + "  AND ([TeamBID] = (SELECT [TeamID] FROM [BaseballTeam] WITH (NOLOCK) WHERE ([TeamName] LIKE @TeamB) AND ([IsDeleted] = 0) AND ([GameType] = @GameType)))";

            if (compareGameTime)
            {
                sSqlCommand = String.Format("{0}{1}{2}", sSqlCommand, Environment.NewLine, "  AND ([GameTime] BETWEEN @GameTime1 AND @GameTime2)");
            }

            using (SqlConnection con = new SqlConnection(frmMain.ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand(sSqlCommand, con))
                {
                    cmd.Parameters.Add(new SqlParameter("@GameType", this.GameType));
                    cmd.Parameters.Add(new SqlParameter("@GameDate", GameTime.Date));
                    cmd.Parameters.Add(new SqlParameter("@TeamA", string.Format("%{0}%", TeamAway)));
                    cmd.Parameters.Add(new SqlParameter("@TeamB", string.Format("%{0}%", TeamHome)));

                    if (GameTime.Hour == 0)//開賽時間是0點
                    {
                        cmd.Parameters.Add(new SqlParameter("@GameTime1", GameTime.ToString("HH:00")));
                    }
                    else
                    {
                        cmd.Parameters.Add(new SqlParameter("@GameTime1", GameTime.AddHours(-1).ToString("HH:00")));
                    }

                    cmd.Parameters.Add(new SqlParameter("@GameTime2", GameTime.AddHours(1).ToString("HH:00")));

                    // 讀取資料
                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        DataTable dt = new DataTable();
                        da.Fill(dt);

                        dt.PrimaryKey = new DataColumn[] { dt.Columns["GID"] };

                        return dt;
                    }
                }
            }
        }

        /// <summary>
        /// 從資料庫取得舊資料
        /// </summary>
        /// <param name="TeamHome">主隊 WebName</param>
        /// <param name="TeamAway">客隊 WebName</param>
        /// <param name="GameTime">比賽時間</param>
        /// <param name="compareGameTime">是否比對開賽時間</param>
        /// <returns>資料表</returns>
        private DataTable GetGameDBInfoByWebName(string TeamHome, string TeamAway, DateTime GameTime, bool compareGameTime)//從資料庫取得舊資料
        {
            string sSqlCommand = "SELECT [GID],[GameType],[GameStates],[GameDate],[GameTime],[TeamAID],[TeamBID],[RunsA],[RunsB]" + "\r\n"
                               + ",[RA],[RB],[EA],[EB],[HA],[HB]" + "\r\n"
                               + "FROM [dbo].[BaseballSchedules] WITH (NOLOCK)" + "\r\n"
                               + "WHERE ([GameType] = @GameType)" + "\r\n"
                               + "  AND ([GameDate] = @GameDate)" + "\r\n"
                               + "  AND ([CtrlStates] = 2)" + "\r\n"
                               + "  AND ([IsDeleted] = 0)" + "\r\n"
                           + "   AND ([TeamAID] = (SELECT [TeamID] FROM [BaseballTeam] WITH (NOLOCK) WHERE ([IsDeleted] = 0) AND ([GameType] = @GameType) AND ((SELECT COUNT(1) FROM [dbo].[fn_splitParameterToTable]([WebName], ',') WHERE COL1 = @TeamA) > 0)))" + "\r\n"
                           + "   AND ([TeamBID] = (SELECT [TeamID] FROM [BaseballTeam] WITH (NOLOCK) WHERE ([IsDeleted] = 0) AND ([GameType] = @GameType) AND ((SELECT COUNT(1) FROM [dbo].[fn_splitParameterToTable]([WebName], ',') WHERE COL1 = @TeamB) > 0)))";

            if (compareGameTime)
            {
                sSqlCommand = String.Format("{0}{1}{2}", sSqlCommand, Environment.NewLine, "  AND ([GameTime] BETWEEN @GameTime1 AND @GameTime2)");
            }

            using (SqlConnection con = new SqlConnection(frmMain.ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand(sSqlCommand, con))
                {
                    cmd.Parameters.Add(new SqlParameter("@GameType", this.GameType));
                    cmd.Parameters.Add(new SqlParameter("@GameDate", GameTime.Date));
                    cmd.Parameters.Add(new SqlParameter("@TeamA", TeamAway));
                    cmd.Parameters.Add(new SqlParameter("@TeamB", TeamHome));

                    if (GameTime.Hour == 0)//開賽時間是0點
                    {
                        cmd.Parameters.Add(new SqlParameter("@GameTime1", GameTime.ToString("HH:00")));
                    }
                    else
                    {
                        cmd.Parameters.Add(new SqlParameter("@GameTime1", GameTime.AddHours(-1).ToString("HH:00")));
                    }

                    cmd.Parameters.Add(new SqlParameter("@GameTime2", GameTime.AddHours(1).ToString("HH:00")));
                    // 讀取資料
                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {

                        DataTable dt = new DataTable();
                        da.Fill(dt);

                        dt.PrimaryKey = new DataColumn[] { dt.Columns["GID"] };

                        return dt;
                    }
                }
            }
        }

        #endregion


        #region 取得 TrackerText

        /// <summary>
        /// 取得 TrackerText
        /// </summary>
        /// <param name="info">賽事物件</param>
        private static void GetTrackerText(BasicInfo info)
        {
            switch (info.TrackerText)
            {
                case "Inclement Weather":
                case "Rain":
                case "雨天のため試合前中止":
                case "雨天のためノーゲーム":
                    info.TrackerText = "因雨延賽";
                    break;
            }
        }

        #endregion
    }
}
