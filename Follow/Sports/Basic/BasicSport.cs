using Follow.Helper;
using Newtonsoft.Json.Linq;
using SHGG.DataStructerService;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using SHGG.FileService;
using System.Xml;

namespace Follow.Sports.Basic
{
    /// <summary>
    /// 跟盤基本功能。
    /// </summary>
    public class BasicFollow : IDisposable
    {
        protected LogFile Logs;
        protected UrlSetting UrlSetting;

        public bool FileWrite { get; set; }// 寫入檔案記錄。

        /// <summary>
        /// 建立。
        /// </summary>
        /// <param name="sport">運動類型</param>
        public BasicFollow(ESport sport)
        {
            // 設定
            this.Sport = sport;
            this.GameData = new Dictionary<string, BasicInfo>();
            this.OldData = new Dictionary<string, BasicInfo>();
            this.UpdataWriteLine = true;

            // 建立記錄檔操作
            this.Logs = new LogFile(sport);
            this.FileWrite = true;

            this.UrlSetting = new UrlSetting();
            //读取来源网
            this.sWebUrl = UrlSetting.GetUrl(sport);
        }

        #region Property
        /// <summary>
        /// 来源网
        /// </summary>
        public string sWebUrl { get; set; }
        /// <summary>
        /// 聯盟代號。
        /// </summary>
        public int AllianceID { get; protected set; }
        /// <summary>
        /// 比賽類型。
        /// </summary>
        public string GameType { get; protected set; }
        /// <summary>
        /// 比賽日期。
        /// </summary>
        public DateTime GameDate { get; protected set; }
        /// <summary>
        /// 運動類型。
        /// </summary>
        public ESport Sport { get; protected set; }
        /// <summary>
        /// 更新資料時顯示內容。
        /// </summary>
        protected bool UpdataWriteLine { get; set; }
        /// <summary>
        /// 最後更新時間。
        /// </summary>
        protected DateTime UpdateLastTime { get; set; }
        /// <summary>
        /// 比賽資料。
        /// </summary>
        public Dictionary<string, BasicInfo> GameData { get; protected set; }
        private Dictionary<string, BasicInfo> OldData;
        #endregion
        #region Function
        /// <summary>
        /// 下載資料。
        /// </summary>
        public virtual void Download()
        {

        }
        /// <summary>
        /// 跟盤。
        /// </summary>
        /// <returns>傳回比賽數量。</returns>
        public virtual int Follow()
        {
            return 0;
        }
        /// <summary>
        /// 釋放記憶體。
        /// </summary>
        public virtual void Dispose()
        {
            // 釋放記憶體
            this.GameData.Clear();
            this.OldData.Clear();
        }
        /// <summary>
        /// 更新資料。
        /// </summary>
        /// <param name="connectionString">資料庫連接字串</param>
        /// <returns>傳回更新數量。</returns>
        public virtual int Update(string connectionString)
        {
            int count = 0;
            // 比賽資料
            foreach (KeyValuePair<string, BasicInfo> data in this.GameData)
            {
                bool haveUpdate = true;
                // 與舊資料進行比對
                if (this.OldData.ContainsKey(data.Key) &&
                    this.OldData[data.Key].ToString() == data.Value.ToString())
                {
                    haveUpdate = false;
                }
                // 需要儲存
                if (haveUpdate)
                {
                    // 記錄資料
                    this.OldData[data.Key] = data.Value;
                    // 更新成功後累計次數
                    if (this.Update(connectionString, data.Value))
                    {
                        count++;

                        try
                        {
                            if (this.FileWrite)
                            {
                                if (this.Sport != ESport.Football &&
                                    this.Sport != ESport.Football_NFL &&
                                    this.Sport != ESport.Tennis &&
                                    this.Sport != ESport.Basketball_OS)
                                {
                                    this.Logs.Update("\r\n" + data.Value.ToString());
                                }

                            }
                        }
                        catch (Exception ex)
                        {
                            this.Logs.Error("Update Error Message:{0},\r\nStackTrace:{1}\r\n Key:{2}", ex.Message, ex.StackTrace, data.Key);
                        }
                    }
                    // 最後更新時間
                    this.UpdateLastTime = DateTime.Now;
                }
            }
            // 傳回
            return count;
        }

        /// <summary>
        /// 更新資料。
        /// </summary>
        /// <param name="info">比賽資料</param>
        public virtual bool Update(string connectionString, BasicInfo info)
        {
            return false;
        }

        /// <summary>
        /// 取得資料。(Asiascore)
        /// </summary>
        /// <param name="data">資料</param>
        /// <param name="find1">搜尋條件</param>
        /// <param name="find2">搜尋條件</param>
        /// <param name="playMinute">比賽的時間長度</param>
        protected Dictionary<string, BasicInfo> GetDataByAsiaScore(string html, string find1, string find2, int playMinute)
        {
            // 沒有資料就離開
            if (string.IsNullOrEmpty(html))
                return null;

            Dictionary<string, BasicInfo> result = new Dictionary<string, BasicInfo>();
            BasicInfo gameInfo = null;
            DateTime gameDate = this.GameDate;
            // 判斷資料
            string[] all = html.Split(new string[] { "¬~ZA÷" }, StringSplitOptions.RemoveEmptyEntries);

            // 聯盟 (第一筆是多餘的)
            for (int allianceIndex = 1; allianceIndex < all.Length; allianceIndex++)
            {
                // 比賽集合
                string[] games = ("ZA÷" + all[allianceIndex]).Split(new string[] { "~" }, StringSplitOptions.RemoveEmptyEntries);
                string allianceName = null;
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
                        continue;
                    }
                    else
                    {
                        // 沒有編號就往下處理
                        if (!info.ContainsKey("AA"))
                            continue;
                    }
                    #endregion

                    // 不是正確的聯盟就離開
                    if (allianceName.ToLower().IndexOf(find1) == -1 ||
                        allianceName.ToLower().IndexOf(find2) == -1)
                        break;

                    // 沒有隊伍就往下處理
                    if (!info.ContainsKey("AE") || !info.ContainsKey("AF"))
                        continue;

                    // 時間是 1970 年加上 Ti
                    DateTime gameTime = DateTime.Parse("1970/1/1 00:00:00").AddTicks(long.Parse(info["AD"]) * 10000000);
                    // 轉成台灣時間 UTC+8
                    gameTime = gameTime.AddHours(8);

                    // 建立比賽資料
                    gameInfo = null;
                    gameInfo = new BasicInfo(this.AllianceID, this.GameType, gameDate, info["AA"]);
                    gameInfo.Away = info["AE"].Replace("GOAL", "");
                    gameInfo.Home = info["AF"].Replace("GOAL", "");
                    // 2014/04/18, 新增聯盟(含賽別)
                    gameInfo.AllianceName = allianceName;

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
                            gameInfo.AwayBoard.Add(info["B" + nums[i]]);
                            gameInfo.HomeBoard.Add(info["B" + nums[i + 1]]);
                        }
                        gameInfo.AwayPoint = info["AG"];
                        gameInfo.HomePoint = info["AH"];
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
                    #endregion

                    // 加入
                    result[gameInfo.WebID] = gameInfo;
                }
            }
            // 傳回
            return result;
        }
        protected Dictionary<string, BasicInfo> GetDataBKBF(string html, ref DateTime dateTime, bool AcH = false)
        {
            // 沒有資料就離開
            if (string.IsNullOrEmpty(html))
                return null;

            Dictionary<string, BasicInfo> result = new Dictionary<string, BasicInfo>();
            BasicInfo gameInfo = null;
            // 判斷資料
            string[] all = html.Split(new string[] { "¬~ZA÷" }, StringSplitOptions.RemoveEmptyEntries);

            // 聯盟 (第一筆是多餘的)
            for (int allianceIndex = 1; allianceIndex < all.Length; allianceIndex++)
            {
                // 比賽集合
                string[] games = ("ZA÷" + all[allianceIndex]).Split(new string[] { "~" }, StringSplitOptions.RemoveEmptyEntries);
                string allianceName = null;
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
                        continue;
                    }
                    else
                    {
                        // 沒有編號就往下處理
                        if (!info.ContainsKey("AA"))
                            continue;
                    }
                    #endregion

                    // 不是正確的聯盟就離開
                    //if (allianceName.ToLower().IndexOf(find1) == -1 ||
                    //    allianceName.ToLower().IndexOf(find2) == -1)
                    //    break;

                    // 沒有隊伍就往下處理
                    if (!info.ContainsKey("AE") || !info.ContainsKey("AF"))
                        continue;

                    // 時間是 1970 年加上 Ti
                    DateTime gameTime = DateTime.Parse("1970/1/1 00:00:00").AddTicks(long.Parse(info["AD"]) * 10000000);
                    // 轉成台灣時間 UTC+8
                    gameTime = gameTime.AddHours(8);
                    dateTime = gameTime.Date;
                    // 建立比賽資料
                    gameInfo = null;
                    gameInfo = new BasicInfo(this.AllianceID, this.GameType, gameTime, info["AA"]);
                    gameInfo.Away = info["AE"].Replace("GOAL", "");
                    gameInfo.Home = info["AF"].Replace("GOAL", "");
                    // 2014/04/18, 新增聯盟(含賽別)
                    gameInfo.AllianceName = allianceName;

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
                            gameInfo.AwayBoard.Add(info["B" + nums[i]]);
                            gameInfo.HomeBoard.Add(info["B" + nums[i + 1]]);
                        }
                        //if (gameInfo.Away == "Houston Rockets" || gameInfo.Home == "Houston Rockets")
                        //{
                        //    string str = "gameInfo";
                        //}
                        gameInfo.AwayPoint = info["AG"];
                        gameInfo.HomePoint = info["AH"];
                    }
                    #endregion
                    #region 比賽狀態
                    switch (info["AB"])
                    {
                        //只显示最后结果的赛事修改成速报要抓取到讯息“只显示最终比分”，状态要为“结束”
                        case "1":
                            if (DateTime.Compare(gameTime, DateTime.Now) <= 0)//比賽時間到了才顯示已結束
                            {
                                gameInfo.TrackerText = "只顯示最終比分";
                                gameInfo.GameStates = "E";
                            }
                            break;
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
                                int playMinute = 10;
                                //NBA、CBA、NBA發展聯盟、Philippine Cup比賽12分鐘，WCBA10分鐘
                                if (gameInfo.AllianceName.IndexOf("NBA") >= 0 || (gameInfo.AllianceName.IndexOf("CBA") >= 0 && gameInfo.AllianceName.IndexOf("WCBA") == -1) || gameInfo.AllianceName.IndexOf("Philippine Cup") >= 0)
                                {
                                    playMinute = 12;
                                }
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
                    if (info["AB"] != "1")//防止一开始没有确定有没有比分源，所以备注fro，开赛前确定后就把备注去掉了的情况发生
                    {
                        gameInfo.TrackerText = "";
                    }
                    if (gameInfo.GameStates == "X")
                    {
                        gameInfo.HomePoint = "0";
                        gameInfo.AwayPoint = "0";
                    }
                    gameInfo.AcH = AcH;
                    #endregion

                    // 加入
                    result[gameInfo.WebID] = gameInfo;
                }
            }
            // 傳回
            return result;
        }

        /// <summary>
        /// 取得資料。(奧訊)
        /// </summary>
        /// <param name="html">下載的內容</param>
        /// <param name="find1">關鍵字1</param>
        /// <param name="find2">關鍵字2</param>
        /// <param name="isAcH">是否主客互換</param>
        /// <returns></returns>
        protected Dictionary<string, BasicInfo> GetDataByBet007Basketball(string html, string find1 = null, string find2 = null, bool isAcH = false, string url = null)
        {
            // 沒有資料就離開
            if (string.IsNullOrEmpty(html)) { return null; }

            Dictionary<string, BasicInfo> result = new Dictionary<string, BasicInfo>();
            BasicInfo gameInfo = null;
            DateTime gameDate = this.GameDate;

            XmlAdapter xmlAdapter = null;

            try
            {
                xmlAdapter = new XmlAdapter(html, false);
            }
            catch
            {
                string msg = String.Format("解析網頁資料錯誤。{0}Url: {1}{0}Content: {2}{0}", Environment.NewLine, (url ?? String.Empty), html);
                //throw new Exception(msg, e);
                this.Logs.Error(msg);
                return null;
            }

            if (xmlAdapter == null) { return null; }

            xmlAdapter.GoToNode("c");

            // 所有比賽集合
            List<string> gameRecord = xmlAdapter.GetAllSubColumns("h");

            // 目標比賽集合
            List<string> targetGames = new List<string>();
            foreach (string gameRow in gameRecord)
            {
                if (!String.IsNullOrEmpty(find1) || !String.IsNullOrEmpty(find2))
                {
                    if (gameRow.IndexOf(find1) >= 0 || gameRow.IndexOf(find2) >= 0)
                    {
                        targetGames.Add(gameRow);
                    }
                }
                else
                {
                    // 沒有關鍵字 取全部資料
                    targetGames.Add(gameRow);
                }
            }

            // 有找到目標比賽才繼續執行
            if (targetGames.Count > 0)
            {
                foreach (string game in targetGames)
                {
                    #region 取出資料

                    // 0:賽事ID/3:聯盟(簡,繁)/4:分幾節進行/6:開賽時間/7:狀態/8:小節剩餘時間/10:主隊名(簡,繁,英)/12:客隊名(簡,繁,英)/15:主隊總分/16:客隊總分
                    // 17:主隊1節得分/18:客隊1節得分/19:主隊2節得分/20:客隊2節得分/21:主隊3節得分/22:客隊3節得分/23:主隊4節得分/24:客隊4節得分
                    // 26:主隊ot1得分/27:客隊ot1得分/28:主隊ot2得分/29:客隊ot2得分/30:主隊ot3得分/31:客隊ot3得分
                    string[] data = game.Split('^');

                    #endregion

                    DateTime gameTime = DateTime.Parse(data[6].Replace("<br>", " "));

                    gameInfo = null;
                    gameInfo = new BasicInfo(this.AllianceID, this.GameType, gameTime, data[0], true);
                    gameInfo.AllianceName = data[3].Split(',')[1];
                    gameInfo.Away = GetBet007Team(data[12]);
                    gameInfo.Home = GetBet007Team(data[10]);
                    gameInfo.AcH = isAcH;

                    #region 比賽狀態
                    // 局數
                    int innings = 0;
                    // 比賽狀態
                    string state = data[7];
                    // 剩餘時間
                    string remainingTime = data[8].Trim();
                    // 分節數 ( 2: 上下半場, 4: 4 小節 )
                    int classType = Int32.Parse(data[4]);

                    switch (state)
                    {
                        case "1":
                        case "2":
                        case "3":
                        case "4":
                        case "5":
                        case "6":
                        case "7":
                            innings = Int32.Parse(state);
                            innings = (innings > 4) ? 4 : innings; // 超過4局, 表示 OT, 取4局
                            gameInfo.Status = "0".Equals(remainingTime) ? "結束" : remainingTime; // 剩餘時間
                            gameInfo.GameStates = "S";
                            gameInfo.StateValue = Convert.ToInt32(state);
                            break;
                        case "50":
                            // 中場 (上下半場: 1局, 4小節: 2局)
                            innings = (classType == 2) ? 1 : 2;
                            gameInfo.Status = "中場休息";
                            gameInfo.GameStates = "S";
                            break;
                        case "-1":
                            innings = 4;
                            gameInfo.Status = "結束";
                            gameInfo.GameStates = "E";
                            gameInfo.StateValue = 8;
                            break;
                        case "-2": // 待定
                            gameInfo.Status = "";
                            gameInfo.TrackerText = "只顯示最終比分";
                            gameInfo.Record = "只顯示最終比分";
                            gameInfo.GameStates = "X";
                            break;
                        case "-3":
                            innings = 4;
                            gameInfo.Status = "中止";
                            gameInfo.GameStates = "P";
                            break;
                        case "-4":
                            innings = 4;
                            gameInfo.Status = "取消";
                            gameInfo.GameStates = "C";
                            break;
                        case "-5":
                            innings = 4;
                            gameInfo.Status = "延遲";
                            gameInfo.GameStates = "D";
                            break;
                    }
                    #endregion

                    #region 分數

                    // 算四小節分數
                    for (int i = 0; i < innings; i++)
                    {
                        string ptHome = data[17 + 2 * i].Trim();
                        string ptAway = data[18 + 2 * i].Trim();

                        if (!String.IsNullOrEmpty(ptAway) && !String.IsNullOrEmpty(ptHome))
                        {
                            gameInfo.AwayBoard.Add(ptAway);
                            gameInfo.HomeBoard.Add(ptHome);
                        }
                    }

                    // 取得 OT 數
                    string otCount = data[25].Trim();
                    if (!String.IsNullOrEmpty(otCount))
                    {
                        // 取得 OT 比分
                        int inningOT = Int32.Parse(otCount);
                        for (int i = 0; i < inningOT; i++)
                        {
                            string otHome = StringHelper.IsNullOrEmptyToZero(data[26 + 2 * i]);
                            string otAway = StringHelper.IsNullOrEmptyToZero(data[27 + 2 * i]);

                            gameInfo.AwayBoard.Add(otAway);
                            gameInfo.HomeBoard.Add(otHome);
                        }
                    }

                    // 總分
                    gameInfo.AwayPoint = StringHelper.IsNullOrEmptyToZero(data[16]);
                    gameInfo.HomePoint = StringHelper.IsNullOrEmptyToZero(data[15]);

                    #endregion
                    gameInfo.Display = 1;
                    // 加入
                    result[gameInfo.WebID] = gameInfo;
                }
            }
            else
                return null;

            return result;
        }

        /// <summary>
        /// 取得即時比分資料。(奧訊)
        /// </summary>
        /// <param name="html">下載的內容</param>
        /// <returns></returns>
        protected Dictionary<string, BasicInfo> GetChangeByBet007Basketball(string html, string url = null)
        {
            // 沒有資料就離開
            if (string.IsNullOrEmpty(html))
                return null;

            Dictionary<string, BasicInfo> result = new Dictionary<string, BasicInfo>();
            BasicInfo gameInfo = null;
            DateTime gameDate = this.GameDate;

            XmlAdapter xmlAdapter = null;

            try
            {
                xmlAdapter = new XmlAdapter(html, false);
            }
            catch (Exception e)
            {
                string msg = String.Format("解析網頁資料錯誤。{0}Url: {1}{0}Content: {2}{0}", Environment.NewLine, (url ?? String.Empty), html);
                throw new Exception(msg, e);
            }

            if (xmlAdapter == null) { return null; }

            xmlAdapter.GoToNode("c");

            // 所有比賽集合
            List<string> gameRecord = xmlAdapter.GetAllSubColumns("h");

            foreach (string game in gameRecord)
            {
                #region 取出資料

                // 0:賽事ID/1:狀態/2:小節剩餘時間/3:主隊總分/4:客隊總分
                // 5:主隊1節得分/6:客隊1節得分/7:主隊2節得分/8:客隊2節得分/9:主隊3節得分/10:客隊3節得分/11:主隊4節得分/12:客隊4節得分/13:加時數
                // 15:分節數/16:主隊ot1得分/17:客隊ot1得分/18:主隊ot2得分/19:客隊ot2得分/20:主隊ot3得分/21:客隊ot3得分
                string[] data = game.Split('^');

                #endregion

                gameInfo = null;
                gameInfo = new BasicInfo(this.AllianceID, this.GameType, gameDate, data[0], true);

                #region 比賽狀態
                // 局數
                int innings = 0;
                // 比賽狀態
                string state = data[1];
                // 剩餘時間
                string remainingTime = data[2].Trim();
                // 分節數 ( 2: 上下半場, 4: 4 小節 )
                int classType = Int32.Parse(data[15]);
                switch (state)
                {

                    case "1":
                    case "2":
                    case "3":
                    case "4":
                    case "5":
                    case "6":
                    case "7":
                        innings = Int32.Parse(state);
                        innings = (innings > 4) ? 4 : innings; // 超過4局, 表示 OT, 取4局
                        gameInfo.Status = "0".Equals(remainingTime) ? "結束" : remainingTime; // 剩餘時間
                        gameInfo.GameStates = "S";
                        break;
                    case "50":
                        // 中場 (上下半場: 1局, 4小節: 2局)
                        innings = (classType == 2) ? 1 : 2;
                        gameInfo.Status = "中場休息";
                        gameInfo.GameStates = "S";
                        break;
                    case "-1":
                        innings = 4;
                        gameInfo.Status = "結束";
                        gameInfo.GameStates = "E";
                        break;
                    case "-2": // 待定
                        gameInfo.Status = "";
                        gameInfo.TrackerText = "只顯示最終比分";
                        gameInfo.Record = "只顯示最終比分";
                        gameInfo.GameStates = "X";
                        break;
                    case "-3":
                        innings = 4;
                        gameInfo.Status = "中止";
                        gameInfo.GameStates = "P";
                        break;
                    case "-4":
                        innings = 4;
                        gameInfo.Status = "取消";
                        gameInfo.GameStates = "C";
                        break;
                    case "-5":
                        innings = 4;
                        gameInfo.Status = "延遲";
                        gameInfo.GameStates = "D";
                        break;
                }
                #endregion

                #region 分數

                // 算四小節分數
                for (int i = 0; i < innings; i++)
                {
                    string ptHome = data[5 + 2 * i].Trim();
                    string ptAway = data[6 + 2 * i].Trim();

                    if (!String.IsNullOrEmpty(ptAway) && !String.IsNullOrEmpty(ptHome))
                    {
                        gameInfo.AwayBoard.Add(ptAway);
                        gameInfo.HomeBoard.Add(ptHome);
                    }
                }

                // 取得 OT 數
                string otCount = data[13].Trim();
                if (!String.IsNullOrEmpty(otCount))
                {
                    // 取得 OT 比分
                    int inningOT = Int32.Parse(otCount);
                    for (int i = 0; i < inningOT; i++)
                    {
                        string otHome = StringHelper.IsNullOrEmptyToZero(data[16 + 2 * i]);
                        string otAway = StringHelper.IsNullOrEmptyToZero(data[17 + 2 * i]);

                        gameInfo.AwayBoard.Add(otAway);
                        gameInfo.HomeBoard.Add(otHome);
                    }
                }

                // 總分
                gameInfo.AwayPoint = StringHelper.IsNullOrEmptyToZero(data[4]);
                gameInfo.HomePoint = StringHelper.IsNullOrEmptyToZero(data[3]);

                #endregion

                gameInfo.Display = 1;
                // 加入
                result[gameInfo.WebID] = gameInfo;
            }

            return result;
        }

        /// <summary>
        /// 取到资料 ABL 澳洲棒球
        /// </summary>
        /// <param name="xml">下載的內容</param>
        protected Dictionary<string, BasicInfo> GetDataByABLGame(string xml, string url = null)
        {
            // 沒有資料就離開
            if (string.IsNullOrEmpty(xml))
                return null;

            Dictionary<string, BasicInfo> result = new Dictionary<string, BasicInfo>();
            BasicInfo gameInfo = null;
            XmlDocument doc = new XmlDocument();

            try
            {
                doc.LoadXml(xml);
            }
            catch (Exception e)
            {
                string msg = String.Format("解析網頁資料錯誤。{0}Url: {1}{0}Content: {2}{0}", Environment.NewLine, (url ?? String.Empty), xml);
                throw new Exception(msg, e);
            }
            XmlElement rootElem = doc.DocumentElement;
            //获取到ABL联盟的gamelist
            XmlNodeList personNodes = rootElem.SelectNodes("//game[@league='ABL']");
            //检查有没有读到资料
            if (personNodes == null && personNodes.Count == 0) { return null; }
            //循环game节点
            foreach (XmlNode node in personNodes)
            {
                //node 为game节点
                XmlElement game = (XmlElement)node;
                string webID = game.GetAttribute("id").Trim();
                gameInfo = new BasicInfo(this.AllianceID, this.GameType, this.GameDate, webID);
                //status 节点元素
                XmlElement status = (XmlElement)game.SelectSingleNode("./status");
                //linescore 节点
                XmlNode linescore = game.SelectSingleNode("./linescore");

                //队伍名称 【城市 队名】中间空格
                gameInfo.Home = string.Format("{0} {1}", game.GetAttribute("home_team_city"), game.GetAttribute("home_team_name"));
                gameInfo.Away = string.Format("{0} {1}", game.GetAttribute("away_team_city"), game.GetAttribute("away_team_name"));
                if (linescore != null)
                {
                    #region 分數
                    foreach (XmlNode item in linescore.SelectNodes("./inning"))
                    {
                        XmlElement linning = (XmlElement)item;
                        string tempA = linning.GetAttribute("away");
                        string tempH = linning.GetAttribute("home");
                        gameInfo.AwayBoard.Add(tempA);
                        gameInfo.HomeBoard.Add(tempH);
                    }
                    #endregion
                    #region  RHE
                    XmlElement r = (XmlElement)linescore.SelectSingleNode("./r");
                    XmlElement h = (XmlElement)linescore.SelectSingleNode("./h");
                    XmlElement e = (XmlElement)linescore.SelectSingleNode("./e");
                    if (r != null)
                    {
                        gameInfo.AwayPoint = r.GetAttribute("away");
                        gameInfo.HomePoint = r.GetAttribute("home");
                    }
                    if (h != null)
                    {
                        gameInfo.AwayH = h.GetAttribute("away");
                        gameInfo.HomeH = h.GetAttribute("home");
                    }
                    if (e != null)
                    {
                        gameInfo.AwayE = e.GetAttribute("away");
                        gameInfo.HomeE = e.GetAttribute("home");
                    }
                    #endregion
                }
                #region BSOB
                //壘包節點
                XmlNode runners_on_base = game.SelectSingleNode("./runners_on_base");
                int iBase = 0;
                if (runners_on_base != null)
                {
                    if (runners_on_base.SelectSingleNode("runner_on_1b") != null)
                    {
                        iBase += 1;
                    }
                    if (runners_on_base.SelectSingleNode("runner_on_2b") != null)
                    {
                        iBase += 2;
                    }
                    if (runners_on_base.SelectSingleNode("runner_on_3b") != null)
                    {
                        iBase += 4;
                    }
                }
                gameInfo.Bases = iBase;
                int o = 0, s = 0, b = 0;
                if (status.GetAttribute("o") != null && int.TryParse(status.GetAttribute("o").Trim(), out o))
                {
                    gameInfo.BallO = o;
                }
                if (status.GetAttribute("s") != null && int.TryParse(status.GetAttribute("s").Trim(), out s))
                {
                    gameInfo.BallS = s;
                }
                if (status.GetAttribute("b") != null && int.TryParse(status.GetAttribute("b").Trim(), out b))
                {
                    gameInfo.BallB = b;
                }
                #endregion

                #region 状态
                switch (status.GetAttribute("status").ToLower().Trim())
                {
                    case "final":
                    case "game over":
                    case "gameover":
                    case "completed early":
                        gameInfo.GameStates = "E";
                        gameInfo.Status = "結束";
                        gameInfo.BallB = 0;
                        gameInfo.BallS = 0;
                        gameInfo.BallO = 0;
                        gameInfo.Bases = 0;
                        // 補上 X
                        if (gameInfo.AwayBoard.Count > gameInfo.HomeBoard.Count)
                        {
                            gameInfo.HomeBoard.Add("X");
                        }
                        else if (string.IsNullOrEmpty(gameInfo.HomeBoard[gameInfo.HomeBoard.Count - 1]))
                        {
                            gameInfo.HomeBoard[gameInfo.HomeBoard.Count - 1] = "X";
                        }
                        break;
                    case "postponed": // 中止
                        gameInfo.GameStates = "P";
                        gameInfo.Status = "中止";
                        gameInfo.BallB = 0;
                        gameInfo.BallS = 0;
                        gameInfo.BallO = 0;
                        gameInfo.Bases = 0;
                        break;
                    case "cancelled": // 取消
                        gameInfo.GameStates = "C";
                        gameInfo.Status = "取消";
                        gameInfo.BallB = 0;
                        gameInfo.BallS = 0;
                        gameInfo.BallO = 0;
                        gameInfo.Bases = 0;
                        break;
                    case "suspended": // 暫停
                    case "delayed": // 延遲
                        gameInfo.GameStates = "D";
                        gameInfo.Status = "延遲";
                        break;
                    case "delayed start": // 延遲
                        gameInfo.GameStates = "D";
                        gameInfo.Status = "延遲";
                        gameInfo.BallB = 0;
                        gameInfo.BallS = 0;
                        gameInfo.BallO = 0;
                        gameInfo.Bases = 0;
                        break;
                    case "warmup": //熱身
                        gameInfo.GameStates = "W";
                        gameInfo.Status = "準備開賽";
                        gameInfo.BallB = 0;
                        gameInfo.BallS = 0;
                        gameInfo.BallO = 0;
                        gameInfo.Bases = 0;
                        break;
                    case "pre-game":
                        gameInfo.GameStates = "W";
                        gameInfo.Status = "賽前";
                        gameInfo.BallB = 0;
                        gameInfo.BallS = 0;
                        gameInfo.BallO = 0;
                        gameInfo.Bases = 0;
                        break;
                    //case "In Progress"://比賽中
                    //    break;
                    default:
                        // 有分數才是比賽開始
                        if (gameInfo.Quarter > 0)
                        {
                            gameInfo.GameStates = "S";
                            // 分數判斷
                            if (string.IsNullOrEmpty(gameInfo.HomeBoard[gameInfo.HomeBoard.Count - 1]))
                            {
                                gameInfo.HomeBoard[gameInfo.HomeBoard.Count - 1] = "0";
                            }
                            if (string.IsNullOrEmpty(gameInfo.AwayBoard[gameInfo.AwayBoard.Count - 1]))
                            {
                                gameInfo.AwayBoard[gameInfo.AwayBoard.Count - 1] = "0";
                            }
                            // 移除下半局
                            if (status.GetAttribute("inning_state") != null &&
                                status.GetAttribute("inning_state").ToString().ToLower() == "top")
                            {
                                if (gameInfo.AwayBoard.Count == gameInfo.HomeBoard.Count)
                                {
                                    gameInfo.HomeBoard.RemoveAt(gameInfo.HomeBoard.Count - 1);
                                }
                            }
                            // 其它判斷
                            if (gameInfo.BallB > 3)
                            { gameInfo.BallB = 3; }
                            if (gameInfo.BallS > 2)
                            { gameInfo.BallS = 2; }
                            if (gameInfo.BallO > 2)
                            { gameInfo.BallO = 2; }
                        }
                        break;
                }
                //讯息
                gameInfo.TrackerText = status.GetAttribute("reason").ToLower();
                #endregion

                result[gameInfo.WebID] = gameInfo;
            }
            return result;
        }
        private string GetBet007Team(string teamName)
        {
            string team = teamName.Split(',')[1];
            int idx = team.IndexOf("[");
            if (idx >= 0) { team = team.Substring(0, idx); }
            return team;
        }

        /// <summary>
        /// 取得資料。(Milb)(賽程資料)
        /// </summary>
        /// <param name="data">資料</param>
        protected Dictionary<string, BasicInfo> GetDataByMilbForSchedules(string html)
        {
            // 沒有資料就離開
            if (string.IsNullOrEmpty(html))
                return null;

            Dictionary<string, BasicInfo> result = new Dictionary<string, BasicInfo>();
            BasicInfo gameInfo = null;
            DateTime gameTime = DateTime.Now;
            JObject jsonSchedules = JObject.Parse(html);
            // 判斷資料
            if (jsonSchedules["schedule_vw_complete"] != null &&
                jsonSchedules["schedule_vw_complete"]["queryResults"] != null &&
                jsonSchedules["schedule_vw_complete"]["queryResults"]["row"] != null)
            {
                JArray games = new JArray();
                // 判斷是 Arraj OR Object
                if (jsonSchedules["schedule_vw_complete"]["queryResults"]["row"] is JArray)
                {
                    games = (JArray)jsonSchedules["schedule_vw_complete"]["queryResults"]["row"];
                }
                else
                {
                    games.Add(jsonSchedules["schedule_vw_complete"]["queryResults"]["row"]);
                }
                // 資料
                for (int index = 0; index < games.Count; index++)
                {
                    JObject game = (JObject)games[index];
                    // 判斷時間
                    if (DateTime.TryParse(game["game_time_local"].ToString(), out gameTime))
                    {
                        string webID = "gid_" + game["game_id"].ToString().Replace("/", "_").Replace("-", "_");

                        // 建立比賽資料
                        gameInfo = null;
                        gameInfo = new BasicInfo(this.AllianceID, this.GameType, gameTime, webID);
                        gameInfo.IsBall = true;
                        gameInfo.Away = game["away_team_short"].ToString();
                        gameInfo.Home = game["home_team_short"].ToString();

                        // 加入
                        result[gameInfo.WebID] = gameInfo;
                    }
                }
            }
            // 傳回
            return result;
        }
        /// <summary>
        /// 取得資料。(Milb)(比賽資料)
        /// </summary>
        /// <param name="data">資料</param>
        protected BasicInfo GetDataByMilbForGame(string html, string webID)
        {
            // 沒有資料就離開
            if (string.IsNullOrEmpty(html))
                return null;

            BasicInfo gameInfo = null;
            JObject json = JObject.Parse(html);
            // 判斷資料
            if (json["data"] != null &&
                json["data"]["game"] != null)
            {
                json = (JObject)json["data"]["game"];
                gameInfo = new BasicInfo(this.AllianceID, this.GameType, this.GameDate, webID);
                gameInfo.Status = json["status"].ToString();
                // 熱身賽或賽前就離開不處理
                if (!string.IsNullOrEmpty(gameInfo.Status) &&
                    (gameInfo.Status.ToLower() == "warmup" ||
                     gameInfo.Status.ToLower() == "pre-game"))
                    return null;

                #region 分數
                if (json["linescore"] != null)
                {
                    if (json["linescore"] is JObject)
                    {
                        if (json["linescore"]["away_inning_runs"] != null)
                            gameInfo.AwayBoard.Add(json["linescore"]["away_inning_runs"].ToString());
                        if (json["linescore"]["home_inning_runs"] != null)
                            gameInfo.HomeBoard.Add(json["linescore"]["home_inning_runs"].ToString());
                    }
                    else
                    {
                        foreach (JObject board in (JArray)json["linescore"])
                        {
                            if (board["away_inning_runs"] != null)
                                gameInfo.AwayBoard.Add(board["away_inning_runs"].ToString());
                            if (board["home_inning_runs"] != null)
                                gameInfo.HomeBoard.Add(board["home_inning_runs"].ToString());
                        }
                    }
                }
                #endregion
                #region RHE
                if (json["away_team_runs"] != null &&
                    json["home_team_runs"] != null)
                {
                    gameInfo.AwayPoint = json["away_team_runs"].ToString();
                    gameInfo.AwayH = json["away_team_hits"].ToString();
                    gameInfo.AwayE = json["away_team_errors"].ToString();
                    gameInfo.HomePoint = json["home_team_runs"].ToString();
                    gameInfo.HomeH = json["home_team_hits"].ToString();
                    gameInfo.HomeE = json["home_team_errors"].ToString();
                }
                #endregion
                #region BSOB
                int bsob = 0;
                // Bases
                //if (json["runner_on_base_status"] != null && int.TryParse(json["runner_on_base_status"].ToString(), out bsob)) gameInfo.Bases = bsob;
                if (json["runner_on_1b"] != null)
                    bsob += 1;
                if (json["runner_on_2b"] != null)
                    bsob += 2;
                if (json["runner_on_3b"] != null)
                    bsob += 4;
                gameInfo.Bases = bsob;
                // BSO
                if (json["balls"] != null && int.TryParse(json["balls"].ToString(), out bsob))
                    gameInfo.BallB = bsob;
                if (json["strikes"] != null && int.TryParse(json["strikes"].ToString(), out bsob))
                    gameInfo.BallS = bsob;
                if (json["outs"] != null && int.TryParse(json["outs"].ToString(), out bsob))
                    gameInfo.BallO = bsob;
                #endregion
                #region 比賽狀態
                switch (json["status"].ToString().ToLower())
                {
                    case "final":
                    case "game over":
                    case "completed early": // 結束
                        gameInfo.GameStates = "E";
                        gameInfo.Status = "結束";
                        gameInfo.BallB = 0;
                        gameInfo.BallS = 0;
                        gameInfo.BallO = 0;
                        gameInfo.Bases = 0;
                        // 補上 X
                        if (gameInfo.AwayBoard.Count > gameInfo.HomeBoard.Count)
                            gameInfo.HomeBoard.Add("X");
                        else if (string.IsNullOrEmpty(gameInfo.HomeBoard[gameInfo.HomeBoard.Count - 1]))
                            gameInfo.HomeBoard[gameInfo.HomeBoard.Count - 1] = "X";
                        // 抓取訊息顯示
                        if (json["reason"] != null)
                            gameInfo.TrackerText = json["reason"].ToString();
                        break;
                    case "postponed": // 中止
                        gameInfo.GameStates = "P";
                        gameInfo.Status = "中止";
                        gameInfo.BallB = 0;
                        gameInfo.BallS = 0;
                        gameInfo.BallO = 0;
                        gameInfo.Bases = 0;
                        // 抓取訊息顯示
                        if (json["reason"] != null)
                            gameInfo.TrackerText = json["reason"].ToString();
                        break;
                    case "cancelled": // 取消
                        gameInfo.GameStates = "C";
                        gameInfo.Status = "取消";
                        gameInfo.BallB = 0;
                        gameInfo.BallS = 0;
                        gameInfo.BallO = 0;
                        gameInfo.Bases = 0;
                        // 抓取訊息顯示
                        if (json["reason"] != null)
                            gameInfo.TrackerText = json["reason"].ToString();
                        break;
                    case "suspended": // 暫停
                    case "delayed": // 延遲
                        gameInfo.GameStates = "D";
                        gameInfo.Status = "延遲";
                        // 抓取訊息顯示
                        if (json["reason"] != null)
                            gameInfo.TrackerText = json["reason"].ToString();
                        break;
                    case "delayed start": // 延遲
                        gameInfo.GameStates = "D";
                        gameInfo.Status = "延遲";
                        gameInfo.BallB = 0;
                        gameInfo.BallS = 0;
                        gameInfo.BallO = 0;
                        gameInfo.Bases = 0;
                        // 抓取訊息顯示
                        if (json["reason"] != null)
                            gameInfo.TrackerText = json["reason"].ToString();
                        break;
                    case "warmup": //熱身
                        gameInfo.GameStates = "W";
                        gameInfo.Status = "準備開賽";
                        gameInfo.BallB = 0;
                        gameInfo.BallS = 0;
                        gameInfo.BallO = 0;
                        gameInfo.Bases = 0;
                        // 抓取訊息顯示
                        if (json["reason"] != null)
                            gameInfo.TrackerText = json["reason"].ToString();
                        break;
                    default:
                        // 有分數才是比賽開始
                        if (gameInfo.Quarter > 0)
                        {
                            gameInfo.GameStates = "S";
                            // 分數判斷
                            if (string.IsNullOrEmpty(gameInfo.HomeBoard[gameInfo.HomeBoard.Count - 1]))
                            {
                                gameInfo.HomeBoard[gameInfo.HomeBoard.Count - 1] = "0";
                            }
                            if (string.IsNullOrEmpty(gameInfo.AwayBoard[gameInfo.AwayBoard.Count - 1]))
                            {
                                gameInfo.AwayBoard[gameInfo.AwayBoard.Count - 1] = "0";
                            }
                            // 移除下半局
                            if (json["inning_state"] != null &&
                                json["inning_state"].ToString().ToLower() == "top")
                            {
                                if (gameInfo.AwayBoard.Count == gameInfo.HomeBoard.Count)
                                {
                                    gameInfo.HomeBoard.RemoveAt(gameInfo.HomeBoard.Count - 1);
                                }
                            }
                            // 其它判斷
                            if (gameInfo.BallB > 3)
                                gameInfo.BallB = 3;
                            if (gameInfo.BallS > 2)
                                gameInfo.BallS = 2;
                            if (gameInfo.BallO > 2)
                                gameInfo.BallO = 2;
                        }
                        break;
                }
                #endregion
            }
            // 傳回
            return gameInfo;
        }
        /// <summary>
        /// 傳回台灣時間。
        /// </summary>
        /// <param name="time">當地時間</param>
        public static DateTime GetUtcTw(DateTime time)
        {
            DateTime utc = time.ToUniversalTime();
            // 傳回 (東部時間)
            return utc.AddHours(+8);
        }
        /// <summary>
        /// 傳回韓國時間。
        /// </summary>
        /// <param name="time">當地時間</param>
        public static DateTime GetUtcKr(DateTime time)
        {
            DateTime utc = time.ToUniversalTime();
            // 傳回 (UTC+9)
            return utc.AddHours(+9);
        }
        /// <summary>
        /// 傳回日本時間。
        /// </summary>
        /// <param name="time">當地時間</param>
        public static DateTime GetUtcJp(DateTime time)
        {
            DateTime utc = time.ToUniversalTime();
            // 傳回 (UTC+9)
            return utc.AddHours(+9);
        }
        /// <summary>
        /// 傳回美國時間。
        /// </summary>
        /// <param name="time">當地時間</param>
        public static DateTime GetUtcUsaEt(DateTime time)
        {
            DateTime utc = time.ToUniversalTime();
            // 傳回 (東部時間)
            return utc.AddHours(-5);
        }
        /// <summary>
        /// 傳回俄羅斯時間。
        /// </summary>
        /// <param name="time">當地時間</param>
        public static DateTime GetUtcRu(DateTime time)
        {
            DateTime utc = time.ToUniversalTime();
            // 傳回 (UTC+9)
            return utc.AddHours(+4);
        }
        /// <summary>
        /// 尋找資料。
        /// </summary>
        /// <param name="node">資料</param>
        /// <param name="id">編號</param>
        /// <returns>傳回決對路徑。</returns>
        public static string FindXPath(HtmlAgilityPack.HtmlNode node, string id)
        {
            // 沒有資料就離開
            if (node == null || node.ChildNodes.Count == 0)
                return String.Empty;

            string result = String.Empty;
            // 資料
            foreach (HtmlAgilityPack.HtmlNode data in node.ChildNodes)
            {
                // 判斷編號
                if (data.Id == id)
                {
                    //Console.Write("找到了");
                    result = data.XPath;
                }
                else
                {
                    // 往下找
                    result = FindXPath(data, id);
                }
                // 有資料就離開
                if (!string.IsNullOrEmpty(result))
                    break;
            }
            // 傳回
            return result;
        }

        #endregion
    }

    /// <summary>
    /// 比賽資料。
    /// </summary>
    [Serializable]
    public class BasicInfo : ICloneable
    {
        #region Property
        /// <summary>
        /// 聯盟代號。
        /// </summary>
        public int AllianceID { get; protected set; }
        /// <summary>
        /// 聯盟名稱(含賽局類別)  足球表示 AL字段 联盟名
        /// </summary>
        public string AllianceName { get; set; }
        /// <summary>
        /// 網頁 ID。
        /// </summary>
        public string WebID { get; protected set; }
        /// <summary>
        /// 比賽類別。
        /// </summary>
        public string GameType { get; protected set; }
        /// <summary>
        /// 比賽時間。
        /// </summary>
        public DateTime GameTime { get; protected set; }
        /// <summary>
        /// 比賽狀況。  //足球中表示 KO 字段  开赛时间 字符串好处理
        /// </summary>
        public string GameStates { get; set; }
        /// <summary>
        /// 客隊。
        /// </summary>
        public string Away { get; set; }
        public string AwayID { get; set; }
        /// <summary>
        /// 客隊總分。 //足球中表示  OB 字段 客队总分
        /// </summary>
        public string AwayPoint { get; set; }
        public string AwayH { get; set; }
        public string AwayE { get; set; }
        /// <summary>
        /// 客隊分數。 //足球中表示 RB字段 客队半场 只取[0]
        /// </summary>
        public List<string> AwayBoard { get; set; }
        /// <summary>
        /// 主隊。
        /// </summary>
        public string Home { get; set; }

        public string HomeID { get; set; }
        /// <summary>
        /// 主隊總分。 //足球中表示  OA 字段 主队总分
        /// </summary>
        public string HomePoint { get; set; }
        public string HomeH { get; set; }
        public string HomeE { get; set; }
        /// <summary>
        /// 主隊分數。  //足球中表示 RA字段 主队半场 只取[0]
        /// </summary>
        public List<string> HomeBoard { get; set; }
        /// <summary>
        /// 各小节比赛状态值，奥逊用 比较比赛状态（防止分数输错不跟分的情况）
        /// </summary>
        public int StateValue { get; set; }
        /// <summary>
        /// 目前局數。
        /// </summary>
        public int Quarter
        {
            get
            {
                int result = 0;
                // 判斷
                if (this.AwayBoard.Count > 0)
                {
                    // 目前局數
                    result = this.AwayBoard.Count;
                }
                // 傳回
                return result;
            }
        }
        /// <summary>
        /// 狀態。   //足球中表示 UP字段 状态
        /// </summary>
        public string Status { get; set; }
        /// <summary>
        /// 記錄。   //足球中記錄 webid  1.type=message 就記錄 我們自己生成的md5 webid  (type=sb 不记录)
        /// </summary>
        public string Record { get; set; }
        /// <summary>
        /// 訊息。
        /// </summary>
        public string TrackerText { get; set; }
        /// <summary>
        /// 主客隊伍交換。
        /// </summary>
        public bool AcH { get; set; }
        public int BallB { get; set; }
        public int BallS { get; set; }
        public int BallO { get; set; }
        public int Bases { get; set; }
        public bool IsBall { get; set; }

        #region 足球
        /// <summary>
        /// 足球 
        /// </summary>
        public string AC { get; set; }
        /// <summary>
        /// 足球 主队红牌
        /// </summary>
        public string NAR { get; set; }
        /// <summary>
        /// 足球 客队队红牌
        /// </summary>
        public string NBR { get; set; }
        /// <summary>
        /// 足球 盘口A
        /// </summary>
        public string ZA { get; set; }
        /// <summary>
        /// 足球 盘口B
        /// </summary>
        public string ZB { get; set; }
        /// <summary>
        /// 足球 盘口C
        /// </summary>
        public string ZC { get; set; }
        /// <summary>
        /// 足球 黄牌主队
        /// </summary>
        public string CA { get; set; }
        /// <summary>
        /// 足球 黄牌客队
        /// </summary>
        public string CB { get; set; }
        /// <summary>
        /// 足球 排名主队
        /// </summary>
        public string SA { get; set; }
        /// <summary>
        /// 足球 排名客队
        /// </summary>
        public string SB { get; set; }

        #endregion 足球

        #region  足球大球比分

        /// <summary>
        /// 比赛时间: 精确到时分秒
        /// </summary>
        public DateTime GameDate { get; set; }

        /// <summary>
        /// 队伍A
        /// </summary>
        public string TeamA { get; set; }

        /// <summary>
        /// 队伍B
        /// </summary>
        public string TeamB { get; set; }

        /// <summary>
        /// 场次: 0.全场 1.半场
        /// </summary>
        public string Scene { get; set; }

        /// <summary>
        /// 队伍A比赛结果
        /// </summary>
        public string bsjg1 { get; set; }

        /// <summary>
        /// 队伍B比赛结果
        /// </summary>
        public string bsjg2 { get; set; }

        #endregion  足球大球比分

        /// <summary>
        /// 儲存額外資料
        /// </summary>
        public object Tag { get; set; }
        /// <summary>
        /// 資料是否有變動
        /// </summary>
        public bool Changed { get; set; }
        /// <summary>
        /// 勝負 0:無結果 1:A隊贏 2:B隊贏(网球/足球用)
        /// </summary>
        public int WN { get; set; }
        /// <summary>
        /// 球權 1:A队球权 2:B队球权 (网球用)  //足球中表示 存储OrderBy 字段 
        /// </summary>
        public int PR { get; set; }
        /// <summary>
        /// 排序（网球用）
        /// </summary>
        public int OrderBy { get; set; }
        /// <summary>
        /// 赛事是否跨天
        /// </summary>
        public bool OverDayGame { get; set; }
        /// <summary>
        /// 赛事是否显示
        /// </summary>
        public int Display { get; set; }

        #endregion
        #region Function
        /// <summary>
        /// 建立。
        /// </summary>
        /// <param name="webID">網頁 ID</param>
        /// <param name="gameTime">比賽日期</param>
        public BasicInfo(int allianceID, string gameType, DateTime gameTime, string webID, bool changed = false)
        {
            // 設定
            this.AllianceID = allianceID;
            this.WebID = webID;
            this.GameType = gameType;
            this.GameTime = gameTime;
            this.GameStates = "X";
            this.AwayBoard = new List<string>();
            this.HomeBoard = new List<string>();
            this.Changed = changed;
        }
        public BasicInfo() { }
        /// <summary>
        /// 傳回比賽文字。
        /// </summary>
        public override string ToString()
        {
            string result = "《" + this.WebID + "》";
            // 時間
            result += " " + GameTime.ToString("yyyy-MM-dd HH:mm") + " ";
            // 比賽狀況
            result += "『" + this.GameStates + "』";
            // 狀態
            if (!String.IsNullOrEmpty(this.Status))
            {
                result += "【" + this.Status + "】";
            }
            // 有資料
            if (this.Away != null && this.Home != null)
            {
                // 有分數
                if (!string.IsNullOrEmpty(this.AwayPoint) && !string.IsNullOrEmpty(this.HomePoint))
                {
                    result += "\r\n";
                    result += CHT_PadRight((this.AcH) ? (this.Home) : (this.Away), 25, ' ');
                    result += (this.AcH) ? (this.HomePoint.PadRight(3, ' ')) : (this.AwayPoint.PadRight(3, ' '));
                    result += "|" + ConvertBoard((this.AcH) ? (this.HomeBoard) : (this.AwayBoard), 2);
                    result += (this.IsBall) ? ((this.AcH) ? ("|" + this.HomeH.PadLeft(2, ' ') + this.HomeE.PadLeft(2, ' ')) : ("|" + this.AwayH.PadLeft(2, ' ') + this.AwayE.PadLeft(2, ' '))) : ("");
                    result += "\r\n";
                    result += CHT_PadRight((this.AcH) ? (this.Away) : (this.Home), 25, ' ');
                    result += (this.AcH) ? (this.AwayPoint.PadRight(3, ' ')) : (this.HomePoint.PadRight(3, ' '));
                    result += "|" + ConvertBoard((this.AcH) ? (this.AwayBoard) : (this.HomeBoard), 2);
                    result += (this.IsBall) ? ((this.AcH) ? ("|" + this.AwayH.PadLeft(2, ' ') + this.AwayE.PadLeft(2, ' ')) : ("|" + this.HomeH.PadLeft(2, ' ') + this.HomeE.PadLeft(2, ' '))) : ("");
                    result += "\r\n";
                    // 判斷
                    if (this.IsBall)
                    {
                        result += string.Format("BSOB：{0},{1},{2},{3}\r\n", this.BallB, this.BallS, this.BallO, this.Bases);
                    }
                }
                else
                {
                    result += CHT_PadLeft((this.AcH) ? (this.Home) : (this.Away), 25, ' ');
                    result += " VS ";
                    result += CHT_PadRight((this.AcH) ? (this.Away) : (this.Home), 25, ' ');
                }
            }
            // 訊息
            if (!String.IsNullOrEmpty(this.TrackerText))
            {
                result += "\r\n" + this.TrackerText;
            }
            if (!String.IsNullOrEmpty(this.Record))
            {
                result += "\r\n" + string.Format("Record:{0}", this.Record);
            }
            #region 足球
            if (this.GameType.ToUpper() == "SB")
            {
                result += "\r\nN:";
                if (!string.IsNullOrEmpty(this.NAR))
                {
                    result += " " + this.NAR;
                }
                if (!string.IsNullOrEmpty(this.NBR))
                {
                    result += " " + this.NBR;
                }
                result += "\r\nZ:";
                if (!string.IsNullOrEmpty(this.ZA))
                {
                    result += " " + this.ZA;
                }
                if (!string.IsNullOrEmpty(this.ZB))
                {
                    result += " " + this.ZB;
                }
                if (!string.IsNullOrEmpty(this.ZC))
                {
                    result += " " + this.ZC;
                }
                result += "\r\nC:";
                if (!string.IsNullOrEmpty(this.CA))
                {
                    result += " " + this.CA;
                }
                if (!string.IsNullOrEmpty(this.CB))
                {
                    result += " " + this.CB;
                }
                result += "\r\nS:";
                if (!string.IsNullOrEmpty(this.SA))
                {
                    result += " " + this.SA;
                }
                if (!string.IsNullOrEmpty(this.SB))
                {
                    result += " " + this.SB;
                }
            }
            #endregion
            if (!String.IsNullOrEmpty(this.PR.ToString()))
            {
                result += "\r\n" + this.PR;
            }
            if (!String.IsNullOrEmpty(this.WN.ToString()))
            {
                result += "\r\n" + this.WN;
            }
            if (!String.IsNullOrEmpty(this.OrderBy.ToString()))
            {
                result += "\r\n" + this.OrderBy;
            }
            if (!String.IsNullOrEmpty(this.AwayID))
            {
                result += "\r\n" + this.AwayID;
            }
            if (!String.IsNullOrEmpty(this.HomeID))
            {
                result += "\r\n" + this.HomeID;
            }
            result += "\r\n" + this.AllianceID;

            // 傳回
            return result;
        }
        /// <summary>
        /// 複製。
        /// </summary>
        public object Clone()
        {
            return this.MemberwiseClone();
        }

        // 傳回分數
        public static string ConvertBoard(List<string> board, int len = 0)
        {
            if (board == null || board.Count == 0)
                return "";

            string result = "";
            // 資料
            foreach (string num in board)
            {
                if (len != 0)
                {
                    result += "," + num.Trim().PadLeft(len, ' ');
                }
                else
                {
                    result += "," + num.Trim();
                }
            }
            // 傳回
            return result.Substring(1);
        }
        #region Other
        /// <summary>
        /// 中文字截字，不足補左側字串。
        /// </summary>
        /// <param name="org">原始字串</param>
        /// <param name="sLen">長度</param>
        /// <param name="padStr">替代字元</param>
        private static string CHT_PadLeft(string org, int sLen, char padStr)
        {
            var sResult = "";
            int orgLen = 0;
            int tLen = 0;
            // 計算轉換過實際的總長
            for (int i = 0; i < org.Length; i++)
            {
                string s = org.Substring(i, 1);
                int vLen = 0;
                //判斷 asc 表是否介於 0~128
                if (Convert.ToInt32(s[0]) > 128 || Convert.ToInt32(s[0]) < 0)
                {
                    vLen = 2;
                }
                else
                {
                    vLen = 1;
                }
                orgLen += vLen;
                if (orgLen > sLen)
                {
                    orgLen -= vLen;
                    break;
                }
                sResult += s;
            }
            // 計算轉換過後，最後實際的長度
            tLen = sLen - (orgLen - org.Length);
            // 傳回
            return sResult.PadLeft(tLen, padStr);
        }
        /// <summary>
        /// 中文字截字，不足補右側字串。
        /// </summary>
        /// <param name="org">原始字串</param>
        /// <param name="sLen">長度</param>
        /// <param name="padStr">替代字元</param>
        private static string CHT_PadRight(string org, int sLen, char padStr)
        {
            var sResult = "";
            int orgLen = 0;
            int tLen = 0;
            // 計算轉換過實際的總長
            for (int i = 0; i < org.Length; i++)
            {
                string s = org.Substring(i, 1);
                int vLen = 0;
                // 判斷 ASC 表是否介於 0~128
                if (Convert.ToInt32(s[0]) > 128 || Convert.ToInt32(s[0]) < 0)
                {
                    vLen = 2;
                }
                else
                {
                    vLen = 1;
                }
                orgLen += vLen;
                if (orgLen > sLen)
                {
                    orgLen -= vLen;
                    break;
                }
                sResult += s;
            }
            // 計算轉換過後，最後實際的長度
            tLen = sLen - (orgLen - org.Length);
            // 傳回
            return sResult.PadRight(tLen, padStr);
        }
        #endregion
        #endregion
    }
}
