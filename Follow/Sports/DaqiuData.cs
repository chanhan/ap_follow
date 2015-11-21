using Follow.Sports.Basic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Xml.Linq;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
using System.Data.SqlClient;
using System.Data;
using System.Xml;

namespace Follow.Sports
{
    public class DaqiuData : Basic.BasicFollow
    {
        #region 變量聲明
        // 盤口網址
        private string ts = UrlSetting.GetUrl(ESport.DaQiuData, "ts");
        // 變動記錄網址      
        private string tsRecordSource = UrlSetting.GetUrl(ESport.DaQiuData, "tsRecordSource");
        // 走勢圖網址
        private string tsFloatSource = UrlSetting.GetUrl(ESport.DaQiuData, "tsFloatSource");
        // 角球網址
        private string corner = UrlSetting.GetUrl(ESport.DaQiuData, "corner");
        // 分數網址
        private string score = UrlSetting.GetUrl(ESport.DaQiuData, "score");

        private string TsName;
        private string SpName;

        private BasicDownload DownCorner;
        private BasicDownload DownScore;
        private List<BasicDownload> DownTs;
        private List<BasicDownload> DownTsRecordSource;
        private List<BasicDownload> DownTsFloatSource;
        //足球對照隊伍名
        private ConcurrentDictionary<string, string> FoolballCorner = new ConcurrentDictionary<string, string>();
        //足球打球比分资料
        private ConcurrentDictionary<string, BasicInfo> Data;
        //足球旧资料
        private ConcurrentDictionary<string, BasicInfo> oldData;


        #endregion 變量聲明
        public DaqiuData(DateTime today)
            : base(ESport.DaQiuData)
        {
            //读取队伍配置信息
            this.AllianceID = 0;
            this.GameType = "PKJQ";
            this.GameDate = today.Date; // 只取日期

            this.DownCorner = new BasicDownload(this.Sport, corner);

            //获取打球比分网址
            string gamedate = DateTime.Now.ToString("yyyy-MM-dd");
            string gamecode = EasyEncryption(gamedate, true);
            //this.LastLogTime = DateTime.Now;
            string url = this.score + "?GameDate=" + gamedate + "&GameType=" + 10 + "&GameCode=" +
                         gamecode;
            this.DownScore = new BasicDownload(this.Sport, url);

            //根据下载球类拼接网址，放到 list。或者你可以用 ConcurrentDictionary
            //参照 Football.cs  54行
            DownTs = new List<BasicDownload>();

            DownTsRecordSource = new List<BasicDownload>();

            DownTsFloatSource = new List<BasicDownload>();

            //加载足球对应队伍
            LoadXml(UrlSetting.GetUrl(ESport.DaQiuData, "FoolballCornerPath"));
        }

        public override void Download()
        {
            //这个方法是每5秒 进一次。如果要改动 frmMain.cs  210行 添加case

            //下載角球 下載時間間隔 按需求設置
            if (string.IsNullOrWhiteSpace(this.DownCorner.Data)
                || DateTime.Now >= this.DownCorner.LastTime.Value.AddSeconds(3))
            {
                this.DownCorner.DownloadString();
            }
            //下載分數 下載時間間隔 按需求設置
            if (string.IsNullOrWhiteSpace(this.DownScore.Data)
                || DateTime.Now >= this.DownScore.LastTime.Value.AddSeconds(3))
            {
                this.DownScore.DownloadString();
            }

            foreach (BasicDownload item in this.DownTs)
            {
                //下載時間間隔 按需求設置
                if (item.LastTime == null || DateTime.Now >= item.LastTime.Value.AddSeconds(3))
                {
                    item.DownloadString();
                }
            }

            foreach (BasicDownload item in this.DownTsRecordSource)
            {
                //下載時間間隔 按需求設置
                if (item.LastTime == null || DateTime.Now >= item.LastTime.Value.AddSeconds(3))
                {
                    item.DownloadString();
                }
            }

            foreach (BasicDownload item in this.DownTsFloatSource)
            {
                //下載時間間隔 按需求設置
                if (item.LastTime == null || DateTime.Now >= item.LastTime.Value.AddSeconds(3))
                {
                    item.DownloadString();
                }
            }
        }

        public override int Follow()
        {
            int result = 0;
            // 若沒有抓到資料 
            if (String.IsNullOrEmpty(this.DownScore.Data)) { return result; }
            JavaScriptSerializer Serializer = new JavaScriptSerializer();
            //获取资料
            List<BasicInfo> basic = Serializer.Deserialize<List<BasicInfo>>(this.DownScore.Data);
            if (Data != null)
            {
                Data.Clear();
            }
            try
            {
                Dictionary<string, string> DictTeamNameDic = FoolballCorner.ToDictionary(Obj => Obj.Key, Obj => Obj.Value);
                foreach (BasicInfo item in basic)
                {
                    string checkTeamA = TeamNameCheck(item.TeamA, DictTeamNameDic);
                    string checkTeamB = TeamNameCheck(item.TeamB, DictTeamNameDic);
                    string gid = GetGid(checkTeamA, checkTeamB, item.Scene);
                    if (gid == null)
                    {
                        continue;
                    }
                    //把队伍替换成速报的队伍
                    item.TeamA = checkTeamA;
                    item.TeamB = checkTeamB;
                    this.Data[gid] = item;
                }
                result = Data.Count;
            }
            catch
            {

            }
            //根据逻辑处理数据 this.DownCorner.Data   this.DownScore.Data 循环处理DownTs  DownTsRecordSource  DownTsFloatSource
            // BasicInfo 这个实体类看需要添加什么字段
            return result;
        }

        public override int Update(string connectionString)
        {
            int result = 0;
            //这边自己决定下是按照 足球 奥逊一样 拼写sql批量 还是 想普通的一条一条的更新，
            bool haveUpdate = true;
            foreach (KeyValuePair<string, BasicInfo> item in Data)
            {
                BasicInfo data = item.Value;
                //更新之前把获取数据与数据库中的数据比较，有变化才做更新
                if (oldData.ContainsKey(item.Key) && oldData[item.Key].bsjg1 == data.bsjg1 && oldData[item.Key].bsjg2 == data.bsjg2)
                {
                    haveUpdate = false;
                }
                if (haveUpdate)
                {
                    string sql = "update FootballSchedules set GameDate=@GameDate,AResult=@AResult,BResult=@BResult,ScoreScene=@ScoreScene,ScoreStatus=@ScoreStatus where NA=@NA and NB=@NB";
                    using (SqlConnection conn = new SqlConnection(frmMain.ConnectionString))
                    {
                        try
                        {
                            conn.Open();
                            SqlCommand cmd = new SqlCommand(sql, conn);
                            cmd.Parameters.Add("@GameDate", SqlDbType.Date).Value = data.GameDate;
                            cmd.Parameters.Add("@AResult", SqlDbType.NVarChar).Value = data.bsjg1;
                            cmd.Parameters.Add("@BResult", SqlDbType.NVarChar).Value = data.bsjg2;
                            cmd.Parameters.Add("@ScoreScene", SqlDbType.NVarChar).Value = data.Scene;
                            cmd.Parameters.Add("@ScoreStatus", SqlDbType.Int).Value = data.Status;
                            cmd.Parameters.Add("@TeamA", SqlDbType.NVarChar).Value = data.TeamA;
                            cmd.Parameters.Add("@TeamB", SqlDbType.NVarChar).Value = data.TeamB;
                            result = cmd.ExecuteNonQuery();
                        }
                        catch (Exception ex)
                        {
                            this.Logs.Error("ExecuteScalar Error Message:{0},\r\nStackTrace:{1}\r\n \r\nSQL:{2}\r\n", ex.Message, ex.StackTrace, sql);
                        }
                    }
                }
            }
            return result;
        }

        //如果决定用一条一条的更新就用这个
        //public override bool Update(string connectionString, BasicInfo info)
        //{
        //    // 多來源跟分
        //    return 
        //}

        private void LoadXml(string path)
        {
            FoolballCorner.Clear();
            XElement doc = XElement.Load(path);
            foreach (var item in doc.Descendants("data"))
            {
                string TsName = item.Attribute("TsName").Value;
                string SpName = item.Attribute("SpName").Value;
                if (!FoolballCorner.ContainsKey(TsName))
                {
                    FoolballCorner.AddOrUpdate(SpName, TsName, (m, n) => TsName);
                }
            }
        }

        /// <summary>
        /// 加密GameCode
        /// </summary>
        /// <param name="str">需要加密的字符串: yyyy-MM-dd</param>
        /// <param name="sort">true.加密 false.解密</param>
        /// <returns></returns>
        public string EasyEncryption(string str, bool sort)
        {
            if (str == "") return "";
            string tempstr = "";
            if (sort)
            {
                Random random = new Random();
                int temprnd = (random.Next(1, 100));
                Regex r = new Regex("^[\u4e00-\u9fa5]+$");
                System.Text.ASCIIEncoding asciiEncoding = new System.Text.ASCIIEncoding();
                for (int i = 1; i <= str.Length; i++)
                {
                    string strs = "";
                    strs = str.Substring(0, i);
                    if (i != 1)
                    {
                        strs = strs.Substring(strs.Length - 1);
                    }
                    string Hh = asciiEncoding.GetBytes(strs)[0].ToString();
                    bool Bools = r.IsMatch(strs);
                    if (Bools)
                    {
                        tempstr += strs + ",";
                    }
                    else
                    {
                        tempstr += int.Parse(Hh) + temprnd + ",";
                    }
                }
                tempstr = tempstr + temprnd;
            }
            else
            {
                string[] sm = str.Split(',');
                int temprnd = Convert.ToInt32(sm[sm.Length - 1]);
                for (int i = 0; i < sm.Length - 1; i++)
                {
                    int tempSm = 0;
                    if (Int32.TryParse(sm[i], out tempSm))
                        tempstr = tempstr + (char)(tempSm - temprnd);
                    else
                        tempstr = tempstr + "(" + sm[i] + ")";
                }
            }
            return tempstr;
        }
        /// <summary>
        /// 验证队伍名
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private string TeamNameCheck(string name, Dictionary<string, string> DicTeamNameDic)
        {
            if (DicTeamNameDic.ContainsKey(name))
            {
                return DicTeamNameDic[name];
            }
            return name;
        }

        /// <summary>
        /// 通过Team获取Gid且根据队伍A和B来查询数据库中的数据
        /// </summary>
        /// <param name="teamA"></param>
        /// <param name="teamB"></param>
        /// <param name="scene">场次,区分半场全场</param>
        /// <returns></returns>
        private string GetGid(string teamA, string teamB, string scene)
        {
            string sSql = "SELECT [GameType],[WebID],[GameDate],[AL],[AC],[KO],[UP],[NA],[NB],[OA],[OB],[RA],[RB],[WN],[NAR],[NBR],[ZA],[ZB],[ZC],[CA],[CB],[SA],[SB],[OrderBy] FROM [dbo].[FootballSchedules] WITH(NOLOCK) WHERE C <> -1 AND NA = @TeamA AND NB=@TeamB AND GameType<>'Message'";
            using (SqlConnection conn = new SqlConnection(frmMain.ConnectionString))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand(sSql, conn);
                cmd.Parameters.Add("@TeamA", SqlDbType.NVarChar).Value = teamA;
                cmd.Parameters.Add("@TeamB", SqlDbType.NVarChar).Value = teamB;
                try
                {
                    SqlDataReader dr = cmd.ExecuteReader();
                    while (dr.Read())
                    {
                        DateTime dGameDate = Convert.ToDateTime(dr["GameDate"]);
                        BasicInfo game = new BasicInfo(0, dr["GameType"].ToString(), dGameDate, dr["WebID"].ToString());
                        game.GameDate = Convert.ToDateTime(dr["GameDate"]);
                        game.TeamA = dr["NA"].ToString();
                        game.TeamB = dr["NB"].ToString();
                        game.bsjg1 = dr["AResult"].ToString();
                        game.bsjg2 = dr["BResult"].ToString();
                        game.Scene = dr["ScoreScene"].ToString();
                        game.Status = dr["ScoreStatus"].ToString();
                        string key = dr["WebID"].ToString() + scene;
                        //存入就数据
                        oldData[key] = game;
                        return key;
                    }
                }
                catch (Exception)
                {
                    throw;
                }
            }
            return null;
        }
    }
}
