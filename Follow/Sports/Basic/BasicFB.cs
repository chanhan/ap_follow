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
    /// 跟盤基本功能。(足球/網球)
    /// </summary>
    public class BasicFB : BasicFollow
    {
        public BasicFB(ESport sport) : base(sport) { this.UpdataWriteLine = false; }
        public override bool Update(string connectionString, BasicInfo info)
        {
            // 沒有資料就離開
            if (info == null)
                return false;
            if (info.Status == null)
                return false;

            bool result = UpdateData(connectionString, info);

            // 記錄: 有更新資料才寫紀錄
            if (!string.IsNullOrEmpty(info.Record) && info.Record.Length > 20)
            {
                this.Logs.Update("\r\n" + info.Record);
            }

            // 傳回
            return result;
        }

        protected bool UpdateData(string connectionString, BasicInfo info)
        {
            string Sql = "DECLARE @id INT" + "\r\n"
                       + "SELECT @id = [id] FROM [FBView] WITH (NOLOCK) WHERE ([GameDate] = @GameDate) AND ([GameType] = @GameType)" + "\r\n"
                       + "IF (@id IS NULL)" + "\r\n"
                       + "BEGIN" + "\r\n"
                       + "    INSERT INTO [FBView]" + "\r\n"
                       + "               ([GameType],[GameDate],[Info],[Changed],[GameCount])" + "\r\n"
                       + "         VALUES(@GameType,@GameDate,@Info,@Changed,@GameCount)" + "\r\n"
                       + "END" + "\r\n"
                       + "ELSE" + "\r\n"
                       + "BEGIN" + "\r\n"
                       + "    UPDATE [FBView] WITH(ROWLOCK)" + "\r\n"
                       + "       SET [Info] = @Info" + "\r\n"
                       + "          ,[Changed] = @Changed" + "\r\n"
                       + "          ,[GameCount] = @GameCount" + "\r\n"
                       + "          ,[ChangeCount] = [ChangeCount] + 1" + "\r\n"
                       + "     WHERE ([GameType] = @GameType)" + "\r\n"
                       + "       AND ([GameDate] = @GameDate)" + "\r\n"
                       + "END" + "\r\n";
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
                cmd.Parameters.Add(new SqlParameter("@Info", DBNull.Value));
                cmd.Parameters.Add(new SqlParameter("@Changed", DBNull.Value));
                cmd.Parameters.Add(new SqlParameter("@GameCount", "0"));

                //足球走势不是json
                if (info.GameType != "SA")
                {
                    Newtonsoft.Json.Linq.JObject.Parse(info.Status);//檢查資料是否合法
                }

                // 資料
                if (!string.IsNullOrEmpty(info.Status))
                {
                    cmd.Parameters["@Info"].Value = info.Status;
                    cmd.Parameters["@Info"].Size = 5000000; // 5M
                }

                if (!string.IsNullOrEmpty(info.Record))
                {
                    cmd.Parameters["@Changed"].Value = info.Record;
                }

                // 數量
                if (!string.IsNullOrEmpty(info.AwayPoint))
                {
                    cmd.Parameters["@GameCount"].Value = info.AwayPoint;
                }
                // 開啟
                conn.Open();
                // 執行
                result = (cmd.ExecuteNonQuery() > 0);
                conn.Close();
            }
            catch {
                this.Logs.Error("ExecuteScalar:\r\n" + Sql);            
            }
            // 沒有關閉資料庫連接就關閉連接
            if (conn != null && conn.State == ConnectionState.Open)
            {
                conn.Close();
            }
            return result;
        }

        /// <summary>
        /// 取得資料。
        /// </summary>
        protected string GetData(string connectionString, BasicInfo info)
        {
            string Sql = "SELECT [Info] FROM [FBView] WITH (NOLOCK)"
                       + " WHERE ([GameDate] = @GameDate)"
                       + "   AND ([GameType] = @GameType)";
            SqlConnection conn = null;
            SqlCommand cmd = null;
            SqlDataReader dr = null;
            string result = "";
            // 錯誤處理
            try
            {
                conn = new SqlConnection(connectionString);
                cmd = new SqlCommand(Sql, conn);
                // 參數
                cmd.Parameters.Add(new SqlParameter("@GameType", info.GameType));
                cmd.Parameters.Add(new SqlParameter("@GameDate", info.GameTime.Date));
                // 開啟
                conn.Open();
                // 執行
                dr = cmd.ExecuteReader();
                // 判斷資料
                if (dr != null && dr.HasRows)
                {
                    // 讀取
                    dr.Read();
                    // 設定
                    result = dr["Info"].ToString();
                }
                conn.Close();
                dr.Close();
            }
            catch { result = null;
            this.Logs.Error("ExecuteScalar:\r\n" + Sql);         
            }
            // 沒有關閉資料庫連接就關閉連接
            if (dr != null && !dr.IsClosed)
            {
                dr.Close();
            }
            // 傳回
            return result;
        }

        /// <summary>
        /// 取得資料。
        /// </summary>
        protected DataTable GetData(string connectionString, string GameType, DateTime startTime, DateTime endDate)
        {
            string Sql = @"SELECT [GID]
      ,[OrderBy]
      ,[GameType]
      ,[AllianceID]
      ,(select alliancename from osalliance with(nolock) where [AllianceID]=bk.[AllianceID]) as alliancename
      ,[Number]
      ,[GameDate]
      ,[GameTime]
      ,[GameStates]
	 ,(select teamname from osteam with(nolock) where teamid=bk.[TeamAID]) as home
	 ,(select teamname from osteam with(nolock) where teamid=bk.[TeamBID]) as away
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
      ,[IsDeleted]
      ,[Record]
      ,[ChangeCount]
      ,[ShowJS]
  FROM [BasketballSchedules] as bk WITH (NOLOCK) WHERE [GameDate] between @GameStartDate and @GameEndDate AND [GameType] = @GameType";
            DataTable table = new DataTable();
            try
            {
                table = ExecuteDataTable(connectionString, Sql, new SqlParameter("@GameType", GameType), new SqlParameter("@GameStartDate", startTime.Date), new SqlParameter("@GameEndDate", endDate.Date));
            }
            catch {
                this.Logs.Error("ExecuteScalar:\r\n" + Sql);            
            }
            return table;
        }
        protected DataTable GetTennisData(string connectionString, DateTime startTime, DateTime endDate)
        {
            string sql = @"SELECT [GID]
      ,[AllianceID]
	 ,(SELECT alliancename from [TNAlliance] with(nolock) where [AllianceID]=tn.[AllianceID]) as alliancename
      ,[GameDate]
      ,[GameTime]
      ,[GameStates]
	 ,(SELECT SourceText FROM NameControl WITH (NOLOCK) WHERE (GTLangx = N'TN') AND (GameType = N'Name') AND id=tn.[TeamAID]) as home
	 ,(SELECT SourceText FROM NameControl WITH (NOLOCK) WHERE (GTLangx = N'TN') AND (GameType = N'Name') AND id=tn.[TeamBID]) as away
      ,[TeamAID]
      ,[TeamBID]
      ,[RunsA]
      ,[RunsB]
      ,[RA]
      ,[RB]
      ,[WN]
      ,[PR]
      ,[WebID]
      ,[TrackerText]
      ,[IsDeleted]
      ,[ChangeCount]
      ,[CreateTime]
  FROM [TennisSchedules] tn WITH (NOLOCK) WHERE [GameDate] between @GameStartDate and @GameEndDate ";
            DataTable table = new DataTable();
            try
            {
                table = ExecuteDataTable(connectionString, sql,  new SqlParameter("@GameStartDate", startTime.Date), new SqlParameter("@GameEndDate", endDate.Date));
            }
            catch {
                this.Logs.Error("ExecuteScalar:\r\n" + sql);
            }
            return table;
        }
        protected DataTable ExecuteDataTable(string connectionString, string sql, params SqlParameter[] parameters)
        {
            DataTable table = new DataTable();
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    SqlCommand cmd = new SqlCommand(sql, conn);
                    cmd.Parameters.AddRange(parameters);
                    SqlDataAdapter dataAdapter = new SqlDataAdapter(cmd);
                    dataAdapter.Fill(table);
                }
            }
            catch {
                this.Logs.Error("ExecuteScalar:\r\n" + sql);            
            }
            return table;
        }

       
        public int ExecuteScalar(string connectionString, string sql, params SqlParameter[] parameters)
        {
            int obj = 0;
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    SqlCommand cmd = new SqlCommand(sql, conn);
                    cmd.Parameters.AddRange(parameters);
                    obj = cmd.ExecuteNonQuery();

                }
            }
            catch (Exception)
            {
                this.Logs.Error("ExecuteScalar:\r\n" + sql);
            }
            return obj;
        }
    }

    /// <summary>
    /// 跟盤基本功能。(美足)
    /// </summary>
    public class BasicFbAf : BasicFollow
    {
        public BasicFbAf(ESport sport) : base(sport) { this.UpdataWriteLine = true; }
        public override bool Update(string connectionString, BasicInfo info)
        {
            // 沒有資料就離開
            if (info == null)
                return false;
            if (info.GameStates == "X")
                return false;

            string Sql = "UPDATE [AFBSchedules] WITH(ROWLOCK)" + "\r\n"
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
            catch {
                this.Logs.Error("ExecuteScalar:\r\n" + Sql);            
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

            string Sql = "UPDATE [AFBSchedules] WITH(ROWLOCK)" + "\r\n"
                           + "   SET [RunsA] = @RunsA" + "\r\n"
                           + "      ,[RunsB] = @RunsB" + "\r\n"
                           + "      ,[StatusText] = @StatusText" + "\r\n"
                           + "      ,[GameStates] = @GameStates" + "\r\n"
                           + "      ,[ChangeCount] = [ChangeCount] + 1" + "\r\n"
                           + " WHERE ([GameType] = @GameType)" + "\r\n"
                           + "   AND ([GameDate] = @GameDate)" + "\r\n"
                           + "   AND ([CtrlStates] = 2)" + "\r\n"
                           + "   AND ([IsDeleted] = 0)" + "\r\n"
                           + "   AND ([TeamAID] = (SELECT [TeamID] FROM [AFBTeam] WITH (NOLOCK) WHERE ([WebName] = @TeamA)))" + "\r\n"
                           + "   AND ([TeamBID] = (SELECT [TeamID] FROM [AFBTeam] WITH (NOLOCK) WHERE ([WebName] = @TeamB)))";
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
            catch {
                this.Logs.Error("ExecuteScalar:\r\n" + Sql);            
            }
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
