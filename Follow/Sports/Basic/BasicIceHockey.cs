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
    /// 籃球跟盤基本功能。
    /// </summary>
    public class BasicIceHockey : BasicFollow
    {
        public BasicIceHockey(ESport sport) : base(sport) { this.UpdataWriteLine = true; }
        public override bool Update(string connectionString, BasicInfo info)
        {
            // 沒有資料就離開
            if (info == null) return false;
            if (info.GameStates == "X") return false;

            string Sql = "UPDATE [IceHockeySchedules] WITH(ROWLOCK)" + "\r\n"
                           + "   SET [RunsA] = @RunsA" + "\r\n"
                           + "      ,[RunsB] = @RunsB" + "\r\n"
                           + "      ,[StatusText] = @StatusText" + "\r\n"
                           + "      ,[GameStates] = @GameStates" + "\r\n"
                           + "      ,[ChangeCount] = [ChangeCount] + 1" + "\r\n"
                           + " WHERE ([GameType] = @GameType)" + "\r\n"
                           + "   AND ([WebID] = @WebID)"
                           + "   AND ([CtrlStates] = 2)" + "\r\n"
                           + "   AND ([IsDeleted] = 0)";
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
                // 分數 (主客交換)
                if (!string.IsNullOrEmpty(info.AwayPoint) && !string.IsNullOrEmpty(info.HomePoint))
                {
                    cmd.Parameters["@RunsA"].Value = BasicInfo.ConvertBoard((info.AcH) ? (info.HomeBoard) : (info.AwayBoard));
                    cmd.Parameters["@RunsB"].Value = BasicInfo.ConvertBoard((info.AcH) ? (info.AwayBoard) : (info.HomeBoard));
                }
                // 狀態
                if (!string.IsNullOrEmpty(info.Status))
                {
                    cmd.Parameters["@StatusText"].Value = info.Status;
                }
                // 開啟
                conn.Open();
                // 執行
                result = (cmd.ExecuteNonQuery() > 0);
                conn.Close();
            }
            catch { }
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
            if (info == null) return false;
            if (info.GameStates == "X") return false;

            string Sql = "UPDATE [IceHockeySchedules] WITH(ROWLOCK)" + "\r\n"
                           + "   SET [RunsA] = @RunsA" + "\r\n"
                           + "      ,[RunsB] = @RunsB" + "\r\n"
                           + "      ,[StatusText] = @StatusText" + "\r\n"
                           + "      ,[GameStates] = @GameStates" + "\r\n"
                           + "      ,[ChangeCount] = [ChangeCount] + 1" + "\r\n"
                           + " WHERE ([GameType] = @GameType)" + "\r\n"
                           + "   AND ([GameDate] = @GameDate)" + "\r\n"
                           + "   AND ([CtrlStates] = 2)" + "\r\n"
                           + "   AND ([IsDeleted] = 0)"
                           + "   AND ([TeamAID] = (SELECT [TeamID] FROM [IceHockeyTeam] WITH (NOLOCK) WHERE ([WebName] = @TeamA)))" + "\r\n"
                           + "   AND ([TeamBID] = (SELECT [TeamID] FROM [IceHockeyTeam] WITH (NOLOCK) WHERE ([WebName] = @TeamB)))";
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
                // 隊伍 (主客交換)
                cmd.Parameters["@TeamA"].Value = (info.AcH) ? (info.Home) : (info.Away);
                cmd.Parameters["@TeamB"].Value = (info.AcH) ? (info.Away) : (info.Home);
                // 分數 (主客交換)
                if (!string.IsNullOrEmpty(info.AwayPoint) && !string.IsNullOrEmpty(info.HomePoint))
                {
                    cmd.Parameters["@RunsA"].Value = BasicInfo.ConvertBoard((info.AcH) ? (info.HomeBoard) : (info.AwayBoard));
                    cmd.Parameters["@RunsB"].Value = BasicInfo.ConvertBoard((info.AcH) ? (info.AwayBoard) : (info.HomeBoard));
                }
                // 狀態
                if (!string.IsNullOrEmpty(info.Status))
                {
                    cmd.Parameters["@StatusText"].Value = info.Status;
                }
                // 開啟
                conn.Open();
                // 執行
                result = (cmd.ExecuteNonQuery() > 0);
                conn.Close();
            }
            catch { }
            // 沒有關閉資料庫連接就關閉連接
            if (conn != null && conn.State == ConnectionState.Open)
            {
                conn.Close();
            }
            // 傳回
            return result;
        }

        protected bool Update3(string connectionString, BasicInfo info)//昨天跟今天的範圍日期
        {
            // 沒有資料就離開
            if (info == null) return false;
            if (info.GameStates == "X") return false;

            string Sql = "UPDATE [IceHockeySchedules] WITH(ROWLOCK)" + "\r\n"
                        + "   SET [RunsA] = @RunsA" + "\r\n"
                        + "      ,[RunsB] = @RunsB" + "\r\n"
                        + "      ,[StatusText] = @StatusText" + "\r\n"
                        + "      ,[GameStates] = @GameStates" + "\r\n"
                        + "      ,[ChangeCount] = [ChangeCount] + 1" + "\r\n"
                        + " WHERE ([GameType] = @GameType)" + "\r\n"
                        + "   AND ([GameDate] BETWEEN @GameDate1 AND @GameDate2)" + "\r\n"
                        + "   AND ([CtrlStates] = 2)" + "\r\n"
                        + "   AND ([IsDeleted] = 0)"
                        + "   AND ([GameStates] <> 'E')" + "\r\n"
                        + "   AND ([TeamAID] = (SELECT [TeamID] FROM [IceHockeyTeam] WITH (NOLOCK) WHERE ([WebName] = @TeamA)))" + "\r\n"
                        + "   AND ([TeamBID] = (SELECT [TeamID] FROM [IceHockeyTeam] WITH (NOLOCK) WHERE ([WebName] = @TeamB)))";
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
                cmd.Parameters.Add(new SqlParameter("@GameDate1", info.GameTime.Date.AddDays(-1)));
                cmd.Parameters.Add(new SqlParameter("@GameDate2", info.GameTime.Date));
                cmd.Parameters.Add(new SqlParameter("@GameStates", info.GameStates));
                cmd.Parameters.Add(new SqlParameter("@RunsA", DBNull.Value));
                cmd.Parameters.Add(new SqlParameter("@RunsB", DBNull.Value));
                cmd.Parameters.Add(new SqlParameter("@StatusText", DBNull.Value));
                cmd.Parameters.Add(new SqlParameter("@Record", DBNull.Value));
                cmd.Parameters.Add(new SqlParameter("@TeamA", DBNull.Value));
                cmd.Parameters.Add(new SqlParameter("@TeamB", DBNull.Value));
                // 隊伍 (主客交換)
                cmd.Parameters["@TeamA"].Value = (info.AcH) ? (info.Home) : (info.Away);
                cmd.Parameters["@TeamB"].Value = (info.AcH) ? (info.Away) : (info.Home);
                // 分數 (主客交換)
                if (!string.IsNullOrEmpty(info.AwayPoint) && !string.IsNullOrEmpty(info.HomePoint))
                {
                    cmd.Parameters["@RunsA"].Value = BasicInfo.ConvertBoard((info.AcH) ? (info.HomeBoard) : (info.AwayBoard));
                    cmd.Parameters["@RunsB"].Value = BasicInfo.ConvertBoard((info.AcH) ? (info.AwayBoard) : (info.HomeBoard));
                }
                // 狀態
                if (!string.IsNullOrEmpty(info.Status))
                {
                    cmd.Parameters["@StatusText"].Value = info.Status;
                }
                // 開啟
                conn.Open();
                // 執行
                result = (cmd.ExecuteNonQuery() > 0);
                conn.Close();
            }
            catch { }
            // 沒有關閉資料庫連接就關閉連接
            if (conn != null && conn.State == ConnectionState.Open)
            {
                conn.Close();
            }
            // 傳回
            return result;
        }
    }
}
