using Follow.Sports.Basic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Follow.Sports
{
    /// <summary>
    /// 足球新版 2015012901
    /// </summary>
    public class Football : Basic.BasicFB
    {
        //去配置里的来源
        private string spboUrl = UrlSetting.GetFootballUrl("todayUrl");//http://bf.spbo.com/"; // 來源網domain被綁架，暫時使用備用來源http://live.spbo1.com/ ///http://69.4.235.78:88/
        private string yesterdayUrl = UrlSetting.GetFootballUrl("yesterdayUrl");
        private string tomorrowUrl = UrlSetting.GetFootballUrl("tomorrowUrl");
        public Football(DateTime today)
            : base(ESport.Football)
        {
            //XML无来源网配置 取默认
            if (string.IsNullOrWhiteSpace(spboUrl))
            {
                spboUrl = @"http://bf.spbo.com/";
            }
            if (string.IsNullOrWhiteSpace(yesterdayUrl))
            {
                yesterdayUrl = @"http://www.spbo.com/end0.htm";
            }
            if (string.IsNullOrWhiteSpace(tomorrowUrl))
            {
                tomorrowUrl = @"http://www.spbo.com/new";
            }
            // 設定
            this.AllianceID = 0;
            this.GameType = "SB";
            this.GameDate = today.Date; // 只取日期
            this.DownHome = new BasicDownload(this.Sport, spboUrl);
            this.DownHomeData = new List<BasicDownload>();

            this.DownReal = new List<BasicDownload>();
            // 昨天
            //this.DownReal.Add(new BasicDownload(this.Sport, @"http://www8.spbo.com/history.plex?day=&l=cn2"));
            this.DownReal.Add(new BasicDownload(this.Sport, yesterdayUrl, Encoding.GetEncoding(936)));  // 原昨日跟分網址因賽事資料有落差 改跟此網頁資料
            //來源網domain被綁架，暫時使用備用來源 http://www8.spbo1.com/

            // 往後 7 天
            for (int i = 0; i < 7; i++)
            {
                this.DownReal.Add(new BasicDownload(this.Sport, tomorrowUrl + i + ".htm", Encoding.GetEncoding(936))); // gb2312
                //來源網domain被綁架，暫時使用備用來源 http://www.spbo1.com/
            }

            //this.DownAnalysisData = new ConcurrentDictionary<string, BasicDownload>();
            this.DownAnalysisData = new ConcurrentDictionary<string, BasicDownload>();
            this.AlreadyEndingData = new List<string>();

            GetOldData();
        }

        public override void Download()
        {
            // 沒有資料或下載時間超過 10 分鐘才讀取首頁資料。
            if (this.GameData.Count == 0 ||
                DateTime.Now >= this.DownLastTime.AddMinutes(10))
            {
                this.DownHome.DownloadString();
                this.DownLastTime = DateTime.Now;
            }
            // 下載資料
            foreach (BasicDownload real in this.DownHomeData)
            {
                // 沒有資料或下載時間超過 10 秒才讀取資料。
                if (real.LastTime == null ||
                    DateTime.Now >= real.LastTime.Value.AddSeconds(10))
                {
                    real.DownloadString();
                }
            }
            // 下載昨天資料
            foreach (BasicDownload real in this.DownReal)
            {
                // 沒有資料或下載時間超過 2 小時才讀取資料。
                if (real.LastTime == null ||
                    DateTime.Now >= real.LastTime.Value.AddHours(1))
                {
                    real.FileWrite = false;
                    real.DownloadString();
                }
            }
            // 下載讓分資料
            //foreach (KeyValuePair<string, BasicDownload> real in this.DownAnalysisData)
            //{
            //    // 沒有資料或下載時間超過 10 秒才讀取資料。
            //    if (real.Value.LastTime == null ||
            //        DateTime.Now >= real.Value.LastTime.Value.AddSeconds(30))
            //    {
            //        real.Value.FileWrite = false;
            //        real.Value.DownloadString();
            //    }
            //}
            foreach (KeyValuePair<string, BasicDownload> real in this.DownAnalysisData)
            {
                // 沒有資料或下載時間超過 10 秒才讀取資料。
                if (real.Value.LastTime == null ||
                    DateTime.Now >= real.Value.LastTime.Value.AddSeconds(30))
                {
                    BasicDownload down = real.Value;
                    if (this.AlreadyEndingData.Contains(real.Key))
                        down.Cancel();//取消下載
                    else
                        down.DownloadString();//若賽程未結束 則繼續更新資料
                }
            }

            //每2个小时取数据库中的数据 进行对比 保证来源网 和数据库中一致
            if (DownOldDate.AddHours(2) == DateTime.Now)
            {
                GetOldData();
            }
        }

        public override int Follow()
        {
            // 沒有資料就離開
            if (string.IsNullOrEmpty(this.DownHome.Data)) return 0;

            int result = 0;
            //BasicInfo gameInfo = null;
            DateTime gameDate = this.GameDate;
            HtmlAgilityPack.HtmlDocument document = new HtmlAgilityPack.HtmlDocument();
            //string xPath = "/html[1]/body[1]/table[2]/tbody[1]/tr[1]/td[1]/table[1]/tbody[1]/tr[2]/td[2]/table[2]/tbody[1]/tr[2]/td[1]/iframe[1]";
            //2015.05.10  04:47改为直接使用ID查找
            // 載入資料
            document.LoadHtml(this.DownHome.Data);
            // 資料位置
            HtmlAgilityPack.HtmlNode nodeGame = document.GetElementbyId("timebf");//document.DocumentNode.SelectSingleNode(xPath);
            // 判斷資料
            if (nodeGame != null)
            {
                #region 下載比賽資料

                // 取出網址
                string webUrl = nodeGame.GetAttributeValue("src", "");
                // 正確的網址
                webUrl = this.DownHome.Uri.ToString() + webUrl;
                // 下載比賽資料
                if (this.DownHomeData.Count == 0)
                {
                    this.DownHomeData.Add(new BasicDownload(this.Sport, webUrl, Encoding.GetEncoding(936)));
                    this.DownHomeData[0].DownloadString();
                }

                #endregion 下載比賽資料

                #region 下載提示資料

                if (this.DownHomeData.Count == 1)
                {
                    this.DownHomeData.Add(new BasicDownload(this.Sport, string.Format(spboUrl + "/s/i.xml"), Encoding.GetEncoding(936)));
                    this.DownHomeData[1].DownloadString();
                }

                #endregion 下載提示資料

                if (!string.IsNullOrEmpty(this.DownHomeData[0].Data))
                {
                    // 載入資料
                    document.LoadHtml(this.DownHomeData[0].Data);

                    #region 取出日期

                    string date = document.DocumentNode.SelectSingleNode("/html[1]/script[1]").InnerText.Trim();
                    string regstr = @"(?i)(?<=<td.*?.*?>)[^<]+(?=日</td>)"; // 提取td的文字
                    Regex reg = new Regex(regstr, RegexOptions.None);
                    if (reg.Match(date).Success)
                    {
                        string gameDateStr = reg.Match(date).Groups[0].Value.Replace("月", "/");
                        // 轉換日期
                        if (!DateTime.TryParse(DateTime.Now.ToString("yyyy") + "/" + gameDateStr, out gameDate))
                        {
                            gameDate = this.GameDate;
                        }
                        // 日期比今天還大，可能是跨年，扣掉一年
                        if (gameDate.Date > DateTime.Now.Date)
                        {
                            gameDate = gameDate.AddYears(-1);
                        }
                    }

                    #endregion 取出日期

                    #region 處理比賽資料

                    if (this.DownHomeData[0].HasChanged)
                    {
                        this.DownHomeData[0].HasChanged = false;

                        string doc = document.DocumentNode.SelectSingleNode("/html[1]").InnerText.Trim();
                        if (string.IsNullOrWhiteSpace(doc))
                        {
                            return 0;
                        }
                        string[] data = doc.Split(new string[] { "var" }, StringSplitOptions.RemoveEmptyEntries);
                        // 判斷資料
                        if (data.Length > 0)
                        {
                            data[0] = data[0].Replace("bf=\"", "");
                            data[0] = data[0].Replace("\";", "");
                            data[0] = data[0].Trim();
                            data = data[0].Split(new string[] { "!" }, StringSplitOptions.RemoveEmptyEntries);

                            ConcurrentDictionary<string, BasicInfo> dicGame = new ConcurrentDictionary<string, BasicInfo>();
                            // 資料
                            int OrderBy = 1;//按照来源网排序
                            foreach (string d in data)
                            {
                                string[] info = d.Split(new string[] { "," }, StringSplitOptions.None);
                                // 資料錯誤就往下處理
                                if (info.Length < 29 || info[14].Trim() == "") continue;
                                // 排除非足球比賽
                                if (info[0] == "NBA" || info[0] == "WNBA" || info[0] == "NCAA" || info[0] == "EBA" ||
                                                info[0] == "STAR" || info[0] == "EuroB" || info[0] == "WORLD" || info[0] == "CBA" ||
                                                info[0] == "AsiaB" || info[0] == "AmerB" || info[0] == "ABA" || info[0] == "KBA" ||
                                                info[0] == "SBA" || info[0] == "WYB" || info[0] == "OlymB")
                                    continue;

                                BasicInfo game = new BasicInfo(0, this.GameType, gameDate, GetMd5Str(string.Concat(info[0].Trim(), gameDate.ToString("yyyyMMdd"), info[5].Trim(), info[6].Trim())));

                                #region 設定

                                game.AllianceName = info[0]; //AL
                                game.AC = info[1];
                                game.GameStates = info[3]; //KO
                                game.Status = this.GetUP(info[2], info[4]);//UP
                                game.Home = info[5];//NA
                                game.Away = info[6];//NB
                                game.HomePoint = info[11];//OA
                                game.AwayPoint = info[12];//OB
                                game.HomeBoard.Add("");//RA
                                game.AwayBoard.Add("");//RB
                                game.WN = 0;
                                game.NAR = "";
                                game.NBR = "";
                                //game["DA"] = info[17];
                                game.ZA = info[21];
                                game.ZB = info[23];
                                game.ZC = info[22];
                                webIDData[info[14]] = game.WebID; //记录来源网ID 和我们自己生成的ID的键值对 key:来源网ID val:md5ID
                                #endregion 設定

                                #region 半場

                                string[] nums = info[13].Split(new string[] { "-" }, StringSplitOptions.RemoveEmptyEntries);
                                // 判斷半場分數
                                if (nums.Length == 2)
                                {
                                    game.HomeBoard[0] = nums[0].Trim();
                                    game.AwayBoard[0] = nums[1].Trim();
                                }

                                #endregion 半場

                                #region 紅牌

                                if (!string.IsNullOrEmpty(info[8]))
                                {
                                    game.NAR = string.Format("<span>{0}</span>", info[8]);
                                }
                                if (!string.IsNullOrEmpty(info[9]))
                                {
                                    game.NBR = string.Format("<span>{0}</span>", info[9]);
                                }

                                #endregion 紅牌

                                #region 輸贏

                                int teamA = 0;
                                int teamB = 0;
                                if (int.TryParse(game.HomePoint, out teamA) &&
                                    int.TryParse(game.AwayPoint, out teamB) &&
                                    game.Status.IndexOf("完") != -1)
                                {
                                    if (teamA > teamB) game.WN = 1;
                                    if (teamA < teamB) game.WN = 2;
                                }

                                #endregion 輸贏

                                #region 盤口

                                if (info[24] != "")
                                {
                                    string key = info[14] + "+" + info[17] + "+" + info[24];
                                    // 日期不同就清除下載資料
                                    if (this.DownAnalysisDate != gameDate.Date)
                                    {
                                        this.DownAnalysisDate = gameDate.Date;//日期

                                        foreach (KeyValuePair<string, BasicDownload> real in this.DownAnalysisData)
                                            real.Value.Dispose();//釋放資源

                                        this.DownAnalysisData.Clear();
                                        this.AlreadyEndingData.Clear();

                                        // 強制更新 下載昨天資料
                                        foreach (BasicDownload real in this.DownReal)
                                        {
                                            real.FileWrite = false;
                                            real.DownloadString();
                                        }
                                    }

                                    //若已完賽 則加入完賽容器中 
                                    //2015-6-2 新增判斷
                                    //至少下载成功过一次的key才add到已完赛容器
                                    //防止第一次启动，导致当下已完赛的走势图不显示
                                    if (game.Status.IndexOf("完") != -1 &&
                                        this.DownAnalysisData.ContainsKey(key) && 
                                        this.DownAnalysisData[key].Data != null &&
                                        !this.AlreadyEndingData.Contains(key))
                                    {
                                        this.AlreadyEndingData.Add(key);//若已完賽 則加入完賽容器中
                                    }

                                    // 加入下載
                                    if (!this.DownAnalysisData.ContainsKey(key))//未在排程中 則加入
                                    {
                                        string url = @"http://www8.spbo.com/pl_sb_rq.plex?id=" + info[24] + "&z=" + info[5] + "&k=" + info[6];
                                        //來源網domain被綁架，暫時使用備用來源 http://www8.spbo1.com/
                                        Console.WriteLine("新加入 " + url);
                                        this.DownAnalysisData[key] = new BasicDownload(this.Sport, url);
                                        this.DownAnalysisData[key].FileWrite = false;
                                        //this.DownAnalysisData[key].DownloadAnalysisString();
                                    }
                                }
                                if (game.ZC != "")
                                {
                                    decimal dAway = 0;
                                    decimal dHome = 0;
                                    decimal.TryParse(game.ZA, out dAway); if (dAway > 0) dAway += 0.01m;
                                    decimal.TryParse(game.ZB, out dHome); if (dHome > 0) dHome += 0.01m;
                                    game.ZA = dAway.ToString("n2");
                                    game.ZB = dHome.ToString("n2");
                                }

                                #endregion 盤口

                                #region 黃牌

                                game.CA = info[25];
                                game.CB = info[26];

                                #endregion 黃牌

                                #region 排名

                                game.SA = info[27];
                                game.SB = info[28];

                                #endregion 排名

                                #region OrderBy排序逻辑
                                game.PR = OrderBy;
                                OrderBy++;
                                #endregion OrderBy排序逻辑

                                // 加入
                                dicGame[game.WebID] = game; // 原本是 info[17]
                            }

                            #region 處理提示資料

                            if (!string.IsNullOrEmpty(this.DownHomeData[1].Data))
                            {
                                BasicInfo message = new BasicInfo(0, "message", gameDate, "m_" + gameDate.ToString("yyyyMMdd"));
                                message.HomeBoard.Add("");// 这2个add是为了
                                message.AwayBoard.Add("");// 防止报错
                                message.Record = "-1";// 当 gametype=message  記錄 我們自己生成的 md5 webid
                                message.PR = -10;
                                System.Xml.XmlDocument xml = new System.Xml.XmlDocument();
                                xml.LoadXml(this.DownHomeData[1].Data);
                                string[] goal = xml.DocumentElement.InnerText.Split(new string[] { "__" }, StringSplitOptions.None);
                                //string[] goal = "16__1__7701810135*1*1 - 0_".Split(new string[] { "__" }, StringSplitOptions.None);
                                if (goal.Length == 3)
                                {
                                    foreach (string str in goal[2].Split('_'))
                                    {
                                        string[] txt = str.Split('*');

                                        if (txt.Length < 3) continue;

                                        //根據來源網中的webid 找到 BasicInfo
                                        var obj = (from c in webIDData where c.Key == (txt[0]) select c.Value).ToList();

                                        if (obj.Count > 0)
                                        {
                                            BasicInfo game = dicGame[obj[0]];

                                            string al = game.AllianceName;
                                            // 找到訊息
                                            if (al != "NBA" && al != "WNBA" && al != "NCAA" && al != "EBA" &&
                                                al != "STAR" && al != "EuroB" && al != "WORLD" && al != "CBA" &&
                                                al != "AsiaB" && al != "AmerB" && al != "ABA" && al != "KBA" &&
                                                al != "SBA" && al != "WYB" && al != "OlymB")
                                            {
                                                string Home = game.Home;
                                                string Away = game.Away;
                                                // 判斷
                                                if (txt[1] == "1")
                                                    Home = "<font color=#FF0000>" + Home + "</font>";
                                                else
                                                    Away = "<font color=#FF0000>" + Away + "</font>";
                                                // 訊息
                                                message.Status += al + "：" + Home + "<font color=blue>&nbsp;" + txt[2] + "&nbsp;</font>" + Away;
                                                message.Record = game.WebID;
                                            }
                                        }
                                    }
                                }
                                // 加入
                                dicGame[message.WebID] = message;
                            }

                            #endregion 處理提示資料

                            // 建立比賽資料
                            //gameInfo = null;
                            //gameInfo = new BasicInfo(this.AllianceID, this.GameType, gameDate, gameDate.ToString("yyyy-MM-dd"));
                            //gameInfo.Away = "足球";
                            //gameInfo.Home = "跟盤";
                            //gameInfo.Status = json.ToString(Formatting.None);

                            dicGameData[gameDate.ToString("yyyyMMdd")] = dicGame;
                            // 加入
                            //this.GameData[gameInfo.WebID] = gameInfo;
                            // 累計
                            result++;
                        }
                    }

                    #endregion 處理比賽資料

                    // 分析資料 SA
                    result += this.FollowAnalysisData(gameDate);
                }
            }

            if (!string.IsNullOrEmpty(this.DownHomeData[0].Data))
            {
                // 儲存跟盤網頁的當天日期
                if (this.DownHomeDate == null ||
                    this.DownHomeDate.Value.Date != gameDate.Date)
                {
                    this.DownHomeDate = gameDate;
                    this.UpdateSetFootballDate(frmMain.ConnectionString, gameDate.Date);
                }
                // 昨天
                if (this.DownReal[0].HasChanged)
                {
                    this.DownReal[0].HasChanged = false;
                    result += this.FollowYesterday(this.DownReal[0].Data, gameDate.AddDays(-1));
                }

                // 往後 7 天
                for (int i = 1; i <= 7; i++)
                {
                    if (this.DownReal[i].HasChanged)
                    {
                        this.DownReal[i].HasChanged = false;
                        result += this.FollowTomorrow(this.DownReal[i].Data, gameDate.Date.AddDays(i));
                    }
                }
            }

            //result = this.GameData.Count;
            result = dicGameData.Count;
            // 傳回
            return result;
        }

        /// <summary>
        /// SA走势处理
        /// </summary>
        private int FollowAnalysisData(DateTime gameDate)
        {

            int result = 0;
            BasicInfo gameInfo = null;
            HtmlAgilityPack.HtmlDocument document = new HtmlAgilityPack.HtmlDocument();
            string xPath = "/html[1]/body[1]/table[1]/tbody[1]";
            StringBuilder sbData = new StringBuilder();
            StringBuilder sbInfo = new StringBuilder();
            decimal dAway = 0;
            decimal dHome = 0;
            // 資料
            //foreach (KeyValuePair<string, BasicDownload> real in this.DownAnalysisData)
            foreach (KeyValuePair<string, BasicDownload> real in this.DownAnalysisData)
            {
                // 沒有資料就往下處理
                if (real.Value == null || string.IsNullOrEmpty(real.Value.Data)) continue;

                // 載入資料
                document.LoadHtml(real.Value.Data);
                // 資料位置
                HtmlAgilityPack.HtmlNode nodeGames = document.DocumentNode.SelectSingleNode(xPath);
                // 判斷資料
                if (nodeGames != null && nodeGames.ChildNodes != null)
                {
                    sbInfo.Clear();
                    // 資料
                    foreach (HtmlAgilityPack.HtmlNode info in nodeGames.ChildNodes)
                    {
                        // 不是資料就往下處理
                        if (info.Name != "tr") continue;

                        if (info.ChildNodes.Count != 5) continue;

                        if (info.ChildNodes[0].Name != "td" ||
                            info.ChildNodes[0].GetAttributeValue("class", "") != "tm") continue;

                        decimal.TryParse(info.ChildNodes[2].InnerText, out dAway); if (dAway > 0) dAway += 0.01m;
                        decimal.TryParse(info.ChildNodes[4].InnerText, out dHome); if (dHome > 0) dHome += 0.01m;
                        // 資料
                        sbInfo.Append(info.ChildNodes[0].InnerHtml.Replace("\"", "'") + "//");
                        sbInfo.Append(info.ChildNodes[1].InnerHtml.Replace("\"", "'") + "//");
                        sbInfo.Append(dAway.ToString("n2") + "//");//sbInfo.Append(info.ChildNodes[2].InnerHtml.Replace("\"", "'") + "//");
                        sbInfo.Append(info.ChildNodes[3].InnerHtml.Replace("\"", "'") + "//");
                        sbInfo.Append(dHome.ToString("n2") + "//");//sbInfo.Append(info.ChildNodes[4].InnerHtml.Replace("\"", "'") + "//");
                        sbInfo.Append(info.ChildNodes[3].GetAttributeValue("title", "") + ";;");
                    }
                    // 有資料才處理
                    if (sbInfo.Length > 0)
                    {
                        string key = "";
                        //根據來源網中的webid 找到 我們自己生成的 webid
                        var obj = (from c in webIDData where real.Key.Contains(c.Key) select c.Value).ToList();
                        key = obj.Count > 0 ? obj[0].ToLower() : "";


                        sbData.Append(real.Key + "+" + key + ":");
                        sbData.Append(sbInfo.ToString() + "\\");
                    }
                }
            }
            // 判斷是否有資料
            if (sbData.Length > 0)
            {
                // 建立比賽資料
                gameInfo = null;
                gameInfo = new BasicInfo(this.AllianceID, "SA", gameDate, gameDate.ToString("yyyyMMdd") + "AY");
                gameInfo.Away = "足球";
                gameInfo.Home = "跟盤";
                gameInfo.Status = sbData.ToString();

                ConcurrentDictionary<string, BasicInfo> dicGame = new ConcurrentDictionary<string, BasicInfo>();
                dicGame[gameInfo.WebID] = gameInfo;
                // 加入
                //this.GameData[gameInfo.WebID] = gameInfo;
                dicGameData[gameInfo.WebID] = dicGame;
                // 累計
                result++;
            }
            // 傳回
            return result;
        }

        /// <summary>
        /// 昨天的数据处理
        /// </summary>
        private int FollowYesterday(string data, DateTime gameDate)
        {
            // 沒有資料就離開
            if (string.IsNullOrEmpty(data)) return 0;

            int result = 0;
            HtmlAgilityPack.HtmlDocument document = new HtmlAgilityPack.HtmlDocument();
            string xPath = "/html/body/script";
            // 載入資料
            document.LoadHtml(data);
            // 資料位置
            HtmlAgilityPack.HtmlNode nodeGame = document.DocumentNode.SelectSingleNode(xPath);
            // 判斷資料
            if (nodeGame != null && nodeGame.Name == "script")
            {
                int iStart = nodeGame.InnerText.IndexOf('"');
                int iEnd = nodeGame.InnerText.IndexOf('"', iStart + 1);
                string sData = nodeGame.InnerText.Substring(iStart + 1, iEnd - iStart - 1);
                string[] sText = sData.Split(new string[] { "!" }, StringSplitOptions.RemoveEmptyEntries);
                //JObject json = new JObject();

                ConcurrentDictionary<string, BasicInfo> dicGame = new ConcurrentDictionary<string, BasicInfo>();
                // 資料
                int OrderBy = 1;//按照来源网排序
                foreach (string str in sText)
                {
                    string[] info = str.Split(',');
                    // 資料錯誤就往下處理
                    if (info.Length < 17) continue;

                    // 排除非足球比賽
                    if (info[2] == "NBA" || info[2] == "WNBA" || info[2] == "NCAA" || info[2] == "EBA" ||
                                    info[2] == "STAR" || info[2] == "EuroB" || info[2] == "WORLD" || info[2] == "CBA" ||
                                    info[2] == "AsiaB" || info[2] == "AmerB" || info[2] == "ABA" || info[2] == "KBA" ||
                                    info[2] == "SBA" || info[2] == "WYB" || info[2] == "OlymB")
                        continue;

                    BasicInfo game = new BasicInfo(0, this.GameType, gameDate, GetMd5Str(string.Concat(info[2].Trim(), gameDate.ToString("yyyyMMdd"), info[6].Trim(), info[9].Trim())));

                    #region 設定

                    game.AllianceName = info[2];
                    game.AC = info[1];
                    game.GameStates = info[3];//KO
                    game.Status = this.GetUP("0", info[4]);//UP
                    game.Home = info[6];
                    game.Away = info[9];
                    game.HomePoint = info[7]; //OA
                    game.AwayPoint = info[8];//OB
                    game.HomeBoard.Add(""); //RA
                    game.AwayBoard.Add("");//RB
                    game.WN = 0;
                    game.NAR = "";
                    game.NBR = "";

                    #endregion 設定
                    #region 黄牌
                    game.CA = info[21];
                    game.CB = info[22]; ;
                    #endregion 黄牌
                    #region 半場

                    string[] nums = info[11].Split(new string[] { "-" }, StringSplitOptions.RemoveEmptyEntries);
                    // 判斷半場分數
                    if (nums.Length == 2)
                    {
                        game.HomeBoard[0] = nums[0].Trim();
                        game.AwayBoard[0] = nums[1].Trim();
                    }

                    #endregion 半場
                    #region 紅牌

                    if (!string.IsNullOrEmpty(info[5]) && info[5] != "0")
                    {
                        game.NAR = string.Format("<span>{0}</span>", info[5]);
                    }
                    if (!string.IsNullOrEmpty(info[10]) && info[10] != "0")
                    {
                        game.NBR = string.Format("<span>{0}</span>", info[10]);
                    }

                    #endregion 紅牌
                    #region 輸贏

                    int teamA = 0;
                    int teamB = 0;
                    if (int.TryParse(game.HomePoint, out teamA) &&
                        int.TryParse(game.AwayPoint, out teamB) &&
                        game.Status.IndexOf("完") != -1)
                    {
                        if (teamA > teamB) game.WN = 1;
                        if (teamA < teamB) game.WN = 2;
                    }

                    #endregion 輸贏
                    #region OrderBy排序逻辑

                    game.PR = OrderBy;
                    OrderBy++;
                    #endregion OrderBy排序逻辑

                    // 加入
                    //json["tr_" + info[0]] = game;
                    dicGame[game.WebID] = game;
                }

                //// 建立比賽資料
                //gameInfo = null;
                //gameInfo = new BasicInfo(this.AllianceID, this.GameType, gameDate, gameDate.ToString("yyyy-MM-dd"));
                //gameInfo.Away = "足球";
                //gameInfo.Home = "跟盤";
                //gameInfo.Status = json.ToString(Formatting.None);

                // 加入
                dicGameData[gameDate.ToString("yyyyMMdd")] = dicGame;
                //this.GameData[gameInfo.WebID] = gameInfo;
                // 累計
                result++;
            }
            // 傳回
            return result;
        }

        /// <summary>
        /// 往后7天的数据处理
        /// </summary>
        private int FollowTomorrow(string data, DateTime gameDate)
        {
            // 沒有資料就離開
            if (string.IsNullOrEmpty(data)) return 0;

            int result = 0;
            //BasicInfo gameInfo = null;
            HtmlAgilityPack.HtmlDocument document = new HtmlAgilityPack.HtmlDocument();
            string xPath = "/html[1]/body[1]/center[2]/table[1]/tr[1]/td[1]/table[1]/tbody[1]";
            // 載入資料
            document.LoadHtml(data);
            // 資料位置
            HtmlAgilityPack.HtmlNode nodeGames = document.DocumentNode.SelectSingleNode(xPath);
            // 判斷資料
            if (nodeGames != null && nodeGames.ChildNodes != null)
            {
                //JObject json = new JObject();
                ConcurrentDictionary<string, BasicInfo> dicGame = new ConcurrentDictionary<string, BasicInfo>();
                // 資料
                int OrderBy = 1;//按照来源网排序
                foreach (HtmlAgilityPack.HtmlNode info in nodeGames.ChildNodes)
                {
                    // 不是資料就往下處理
                    if (info.Name != "tr" ||
                        info.ChildNodes.Count != 6) continue;

                    // 排除非足球比賽
                    if (info.ChildNodes[0].InnerText == "NBA" || info.ChildNodes[0].InnerText == "WNBA" || info.ChildNodes[0].InnerText == "NCAA" || info.ChildNodes[0].InnerText == "EBA" ||
                                    info.ChildNodes[0].InnerText == "STAR" || info.ChildNodes[0].InnerText == "EuroB" || info.ChildNodes[0].InnerText == "WORLD" || info.ChildNodes[0].InnerText == "CBA" ||
                                    info.ChildNodes[0].InnerText == "AsiaB" || info.ChildNodes[0].InnerText == "AmerB" || info.ChildNodes[0].InnerText == "ABA" || info.ChildNodes[0].InnerText == "KBA" ||
                                    info.ChildNodes[0].InnerText == "SBA" || info.ChildNodes[0].InnerText == "WYB" || info.ChildNodes[0].InnerText == "OlymB")
                        continue;
                    //window.open('http://wwww.spbo.com/pl/14218093201.htm','','width=680,height=496,top=130,left=190,resizable=yes,scrollbars=yes');

                    ////根据2个队伍中的地址 获取webid
                    //string h = info.ChildNodes[3].ChildNodes["a"].Attributes["onclick"].Value;
                    //h = h.Substring(h.LastIndexOf("/") + 1, h.LastIndexOf(".") - h.LastIndexOf("/") - 2);
                    //string a = info.ChildNodes[5].ChildNodes["a"].Attributes["onclick"].Value;
                    //a = a.Substring(a.LastIndexOf("/") + 1, a.LastIndexOf(".") - a.LastIndexOf("/") - 2);
                    //if (h != a)
                    //{
                    //    continue;
                    //}

                    BasicInfo game = new BasicInfo(0, this.GameType, gameDate, GetMd5Str(string.Concat(info.ChildNodes[0].InnerText.Trim(), gameDate.ToString("yyyyMMdd"), info.ChildNodes[3].InnerText.Trim(), info.ChildNodes[5].InnerText.Trim())));
                    game.AllianceName = info.ChildNodes[0].InnerText;
                    game.AC = info.ChildNodes[0].Attributes["bgcolor"].Value;
                    game.GameStates = info.ChildNodes[1].InnerText;
                    game.Status = "未開賽";
                    game.Home = info.ChildNodes[3].InnerText;
                    game.Away = info.ChildNodes[5].InnerText;
                    game.HomePoint = "";
                    game.AwayPoint = "";
                    game.HomeBoard.Add("");
                    game.AwayBoard.Add("");
                    game.WN = 0;
                    game.NAR = "";
                    game.NBR = "";
                    game.PR = OrderBy;
                    OrderBy++;
                    // 加入
                    dicGame[game.WebID] = game;
                }

                // 建立比賽資料
                //gameInfo = null;
                //gameInfo = new BasicInfo(this.AllianceID, this.GameType, gameDate, gameDate.ToString("yyyy-MM-dd"));
                //gameInfo.Away = "足球";
                //gameInfo.Home = "跟盤";
                //gameInfo.Status = json.ToString(Formatting.None);

                // 加入
                //this.GameData[gameInfo.WebID] = gameInfo;
                dicGameData[gameDate.ToString("yyyyMMdd")] = dicGame;
                // 累計
                result++;
            }
            // 傳回
            return result;
        }

        /// <summary>
        /// 重写 BasicFollow 里的Update
        /// </summary>
        public override int Update(string connectionString)
        {
            int result = 0;
            bool haveUpdate = true;
            StringBuilder sb = new StringBuilder();
            foreach (KeyValuePair<string, ConcurrentDictionary<string, BasicInfo>> item in dicGameData)
            {
                ConcurrentDictionary<string, BasicInfo> dicGame = item.Value;
                sb.Clear();
                sb.AppendFormat(" DECLARE @a int = -1  \r\n");
                foreach (KeyValuePair<string, BasicInfo> game in dicGame)
                {
                    BasicInfo info = game.Value;
                    //与旧的数据对比
                    haveUpdate = true;
                    if (dicOldGameData.ContainsKey(item.Key) && dicOldGameData[item.Key].ContainsKey(game.Key)
                        && dicOldGameData[item.Key][game.Key].ToString() == info.ToString())
                    {
                        haveUpdate = false;
                    }
                    if (haveUpdate)
                    {
                        //记录资料
                        if (!dicOldGameData.ContainsKey(item.Key))
                        {
                            dicOldGameData[item.Key] = new ConcurrentDictionary<string, BasicInfo>();
                        }
                        dicOldGameData[item.Key][game.Key] = info;
                        if (info.GameType == "SA")
                        {
                            #region 处理走势

                            //更新
                            result += Convert.ToInt32(base.Update(connectionString, info));

                            #endregion 处理走势
                        }
                        else if (info.GameType == "message")
                        {
                            #region 入球提示
                            StringBuilder s = new StringBuilder(" DECLARE @a int = -1  \r\n");
                            GetUpdateSql(info, s);
                            //更新
                            result += ExecuteScalar(connectionString, s.ToString());
                            #endregion 入球提示
                        }
                        else
                        {
                            #region 处理sb

                            sb.AppendFormat("\r\n");
                            GetUpdateSql(info, sb);
                            sb.AppendFormat("\r\n");

                            #endregion 处理sb
                        }
                    }

                }
                //处理 以除掉的赛事 C= -1
                #region 处理移除掉的赛事
                //跨天原因 取 当前日期 -1 -2 作判断条件
                if ((item.Key == this.GameDate.AddDays(1).ToString("yyyyMMdd") || item.Key == this.GameDate.ToString("yyyyMMdd") || item.Key == this.GameDate.AddDays(-1).ToString("yyyyMMdd") || item.Key == DateTime.Now.AddDays(-2).ToString("yyyyMMdd"))
                    && dicOldGameData.ContainsKey(item.Key))
                {
                    var a = dicOldGameData[item.Key].Values.Where(p => !dicGame.Values.Select(b => b.WebID).Contains(p.WebID)).ToList();
                    foreach (BasicInfo d in a)
                    {
                        sb.AppendFormat(" \r\n");
                        sb.AppendFormat("  UPDATE FootballSchedules  SET  [C]= -1 WHERE WebID='{0}' \r\n", d.WebID);
                        sb.AppendFormat(" \r\n");

                        //移除内存中的
                        dicOldGameData[item.Key].TryRemove(d.WebID, out bi);
                    }
                }

                #endregion 处理移除掉的赛事
                //执行数据库
                if (sb.ToString().IndexOf("UPDATE") != -1)
                {
                    try
                    {
                        result += ExecuteScalar(connectionString, sb.ToString());
                        sb.AppendFormat("\r\n =====================" + dicGame.Count() + "====================" + item.Key + "================== \r\n");
                        this.Logs.Update(sb.ToString());
                    }
                    catch (Exception ex)
                    {
                        this.Logs.Error("ExecuteScalar Error Message:{0},\r\nStackTrace:{1}\r\n \r\nSQL:{2}\r\n", ex.Message, ex.StackTrace, sb.ToString());
                    }
                    finally
                    {
                        sb.Clear();
                    }
                }

            }
            return result;
        }

        /// <summary>
        /// 拼写sql语句
        /// </summary>
        private string GetUpdateSql(BasicInfo info, StringBuilder sb)
        {
            sb.AppendFormat(" SET @a = -1 \r\n");
            sb.AppendFormat(" SELECT @a=[CtrlStates] FROM FootballSchedules WITH(NOLOCK) WHERE GameDate='{0}' ", info.GameTime.Date);
            if (info.GameType != "message")
            {
                sb.AppendFormat(" AND WebID='{0}' ", info.WebID);
            }
            sb.AppendFormat(" AND GameType='{0}' \r\n", info.GameType);
            sb.AppendFormat(" IF (@a <> -1) \r\n");
            sb.AppendFormat(" BEGIN \r\n");
            sb.AppendFormat("    UPDATE FootballSchedules  SET [KO] =(CASE WHEN @a = 31 THEN [KO] ELSE  '{1}' END),[UP] = (CASE WHEN @a = 31 THEN [UP] ELSE '{2}' END) ,[OA] =(CASE WHEN @a = 21 THEN [OA] ELSE {3} END) ,[OB] = (CASE WHEN @a = 21 THEN [OB] ELSE {4} END),[RA] = (CASE WHEN @a = 21 THEN [RA] ELSE {5} END),[RB] = (CASE WHEN @a = 21 THEN [RB] ELSE {6} END),[WN] = '{7}', [NAR] =(CASE WHEN @a = 21 THEN [NAR] ELSE {8} END),[NBR] = (CASE WHEN @a = 21 THEN [NBR] ELSE {9} END), [ZA] = {10},[ZB] = {11},[ZC] = {12},[CA] = (CASE WHEN @a = 21 THEN [CA] ELSE {13} END),[CB] = (CASE WHEN @a = 21 THEN [CB] ELSE {14} END),[SA] = {15},[SB] = {16},[OrderBy] = {17},[C] = [C] + 1 ",
                            "",
                            (info.GameType == "message" ? DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") : info.GameStates), //--记录message 最后更新时间
                            info.Status,
                            GetNull(info.HomePoint),
                            GetNull(info.AwayPoint),
                            GetNull(info.HomeBoard[0]),
                            GetNull(info.AwayBoard[0]),
                            info.WN,
                            GetNull(info.NAR),
                            GetNull(info.NBR),
                            GetNull(info.ZA),
                            GetNull(info.ZB),
                            GetNull(info.ZC),
                            GetNull(info.CA),
                            GetNull(info.CB),
                            GetNull(info.SA),
                            GetNull(info.SB),
                            info.PR//足球中表示 存储OrderBy 字段
                            );
            if (info.GameType == "message")
            {
                //如果是入球提示 就修改本次入球的 webid
                sb.AppendFormat(",[WebID] = '{0}' ", info.Record);
            }
            sb.AppendFormat(" WHERE [GameDate] = '{0}'", info.GameTime.Date);
            if (info.GameType != "message")
            {
                //入球提示 webid 不作為條件
                sb.AppendFormat(" AND [WebID] = '{0}' ", info.WebID);
            }
            sb.AppendFormat(" AND [GameType] = '{0}'  AND [CtrlStates] <> 1 \r\n", info.GameType);
            if (info.GameType == "message" && string.IsNullOrWhiteSpace(info.Status))
            {
                //入球提示  至少要在数据库存放2秒钟之后 才会更新为空白
                sb.AppendFormat(" AND KO < DATEADD(s,-2,GETDATE()) \r\n");
            }
            sb.AppendFormat(" END \r\n");
            sb.AppendFormat(" ELSE \r\n");
            sb.AppendFormat(" BEGIN \r\n");
            sb.AppendFormat(@"     INSERT INTO FootballSchedules([GameType],[WebID],[GameDate] ,[AL],[AC],[KO],[UP],[NA],[NB],[OA],[OB],[RA],[RB] ,[WN],[NAR],[NBR],[ZA],[ZB],[ZC],[CA],[CB],[SA],[SB],[C],[CtrlStates],[CtrlAdmin],[OrderBy],[CreateDate]) VALUES('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}',{9},{10},{11},{12},{13},{14},{15},{16},{17},{18},{19},{20},{21},{22},0,0,null,{23},getdate())   ",
                             info.GameType,
                             (info.GameType == "message" ? info.Record : info.WebID),
                             info.GameTime.Date,
                             info.AllianceName,
                             info.AC,
                             (info.GameType == "message" ? DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") : info.GameStates), //--记录message 最后更新时间
                             info.Status,
                             info.Home,
                             info.Away,
                             GetNull(info.HomePoint),
                             GetNull(info.AwayPoint),
                             GetNull(info.HomeBoard[0]),
                             GetNull(info.AwayBoard[0]),
                             info.WN,
                             GetNull(info.NAR),
                             GetNull(info.NBR),
                             GetNull(info.ZA),
                             GetNull(info.ZB),
                             GetNull(info.ZC),
                             GetNull(info.CA),
                             GetNull(info.CB),
                             GetNull(info.SA),
                             GetNull(info.SB),
                             info.PR
                             );
            sb.AppendFormat(" END \r\n");
            return string.Empty;
        }

        //如果字段值为空 改为null
        private string GetNull(string val)
        {
            return (string.IsNullOrWhiteSpace(val) ? "null" : "'" + val + "'");
        }

        /// <summary>
        /// MD5 16位加密 加密后密码为大写
        /// </summary>
        /// <param name="ConvertString"></param>
        /// <returns></returns>
        public static string GetMd5Str(string ConvertString)
        {
            System.Security.Cryptography.MD5CryptoServiceProvider md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
            string t2 = BitConverter.ToString(md5.ComputeHash(UTF8Encoding.Default.GetBytes(ConvertString)), 4, 8);
            t2 = t2.Replace("-", "");
            return t2;
        }

        /// <summary>
        /// 儲存跟盤網頁的當天日期
        /// </summary>
        private bool UpdateSetFootballDate(string connectionString, DateTime today)
        {
            SqlConnection conn = null;
            SqlCommand cmd = null;
            bool result = false;
            // 錯誤處理
            try
            {
                conn = new SqlConnection(connectionString);
                cmd = new SqlCommand("UPDATE [SetTypeVal] SET [val]=@val WHERE ([type]='FootballDate')", conn);
                // 參數
                cmd.Parameters.Add(new SqlParameter("@val", today.ToString("yyyy-MM-dd")));
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

        private string GetUP(string status, string up)
        {
            string result = null;
            // 依狀態處理
            switch (status)
            {
                case "1": result = "取消"; break;
                case "2": result = "腰斩"; break;
                case "3": result = "改期"; break;
                case "4": result = "中断"; break;
                case "5": result = "待定"; break;
                default:
                    // 依資料處理
                    switch (up)
                    {
                        case "0": result = ""; break;
                        case "1": result = "上"; break;
                        case "2": result = "中"; break;
                        case "3": result = "下"; break;
                        case "4": result = "完"; break;
                        case "5": result = "1节"; break;
                        case "6": result = "2节"; break;
                        case "7": result = "3节"; break;
                        case "8": result = "4节"; break;
                        case "9": result = "加"; break;
                        case "10": result = "休"; break;
                        case "11": result = ""; break;
                        default: result = "未開賽"; break;
                    }
                    break;
            }
            // 傳回
            return result;
        }

        /// <summary>
        /// 第一次从数据库中 获取数据
        /// </summary>
        private void GetOldData()
        {
            //实例化 2天的dic
            dicOldGameData[this.GameDate.ToString("yyyyMMdd")] = new ConcurrentDictionary<string, BasicInfo>();
            dicOldGameData[this.GameDate.AddDays(-1).ToString("yyyyMMdd")] = new ConcurrentDictionary<string, BasicInfo>();
            dicOldGameData[this.GameDate.AddDays(1).ToString("yyyyMMdd")] = new ConcurrentDictionary<string, BasicInfo>();
            dicOldGameData[this.GameDate.AddDays(2).ToString("yyyyMMdd")] = new ConcurrentDictionary<string, BasicInfo>();
            dicOldGameData[this.GameDate.AddDays(3).ToString("yyyyMMdd")] = new ConcurrentDictionary<string, BasicInfo>();
            dicOldGameData[this.GameDate.AddDays(4).ToString("yyyyMMdd")] = new ConcurrentDictionary<string, BasicInfo>();
            dicOldGameData[this.GameDate.AddDays(5).ToString("yyyyMMdd")] = new ConcurrentDictionary<string, BasicInfo>();
            dicOldGameData[this.GameDate.AddDays(6).ToString("yyyyMMdd")] = new ConcurrentDictionary<string, BasicInfo>();
            dicOldGameData[this.GameDate.AddDays(7).ToString("yyyyMMdd")] = new ConcurrentDictionary<string, BasicInfo>();
            string sSql = "SELECT [GameType],[WebID],[GameDate],[AL],[AC],[KO],[UP],[NA],[NB],[OA],[OB],[RA],[RB],[WN],[NAR],[NBR],[ZA],[ZB],[ZC],[CA],[CB],[SA],[SB],[OrderBy] FROM [dbo].[FootballSchedules] WITH(NOLOCK) WHERE GameDate between @sDate AND @eDate AND C <> -1";
            DateTime dGameDate;
            using (SqlConnection conn = new SqlConnection(frmMain.ConnectionString))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand(sSql, conn);
                cmd.Parameters.Add("@sDate", SqlDbType.Date).Value = this.GameDate.AddDays(-1).Date;
                cmd.Parameters.Add("@eDate", SqlDbType.Date).Value = this.GameDate.AddDays(7).Date;
                try
                {
                    SqlDataReader dr = cmd.ExecuteReader();

                    while (dr.Read())
                    {
                        dGameDate = Convert.ToDateTime(dr["GameDate"]);
                        if (dr["GameType"].ToString() == "message")
                        {
                            BasicInfo message = new BasicInfo(0, "message", dGameDate, "m_" + dGameDate.ToString("yyyyMMdd"));
                            message.HomeBoard.Add("");// 这2个add是为了
                            message.AwayBoard.Add("");// 防止报错
                            message.Record = dr["WebID"].ToString();// 当 gametype=message  記錄 我們自己生成的 md5 webid
                            message.PR = -10;
                            message.Status = dr["UP"].ToString();
                            dicOldGameData[dGameDate.ToString("yyyyMMdd")][message.WebID] = message;
                            continue;
                        }
                        BasicInfo game = new BasicInfo(0, dr["GameType"].ToString(), dGameDate, dr["WebID"].ToString());
                        game.AllianceName = dr["AL"].ToString(); //AL
                        game.AC = dr["AC"].ToString();
                        game.GameStates = dr["KO"].ToString(); //KO
                        game.Status = dr["UP"].ToString();//UP
                        game.Home = dr["NA"].ToString(); ;//NA
                        game.Away = dr["NB"].ToString(); ;//NB
                        game.HomePoint = dr["OA"].ToString();//OA
                        game.AwayPoint = dr["OB"].ToString();//OB
                        game.HomeBoard.Add(dr["RA"].ToString());//RA
                        game.AwayBoard.Add(dr["RB"].ToString());//RB
                        game.WN = Convert.ToInt32(dr["WN"]);
                        game.NAR = dr["NAR"].ToString();
                        game.NBR = dr["NBR"].ToString();
                        game.ZA = dr["ZA"].ToString();
                        game.ZB = dr["ZB"].ToString();
                        game.ZC = dr["ZC"].ToString();
                        game.CA = dr["CA"].ToString();
                        game.CB = dr["CB"].ToString();
                        game.SA = dr["SA"].ToString();
                        game.SB = dr["SB"].ToString();
                        game.PR = Convert.ToInt32(dr["OrderBy"]);
                        dicOldGameData[dGameDate.ToString("yyyyMMdd")][game.WebID] = game;

                    }
                }
                catch (Exception)
                {
                    throw;
                }
            }
            DownOldDate = DateTime.Now;
        }

        // 下載資料
        private BasicDownload DownHome;

        private List<BasicDownload> DownReal;
        private DateTime DownLastTime = DateTime.Now;
        private List<BasicDownload> DownHomeData;
        private DateTime? DownHomeDate;

        //private ConcurrentDictionary<string, BasicDownload> DownAnalysisData;
        private ConcurrentDictionary<string, BasicDownload> DownAnalysisData;

        private DateTime? DownAnalysisDate;
        private List<string> AlreadyEndingData;//已完賽的賽程
        private ConcurrentDictionary<string, string> webIDData = new ConcurrentDictionary<string, string>(); //当天的  来源网 webid 与 我们生成的 md5 键值对
        private DateTime DownOldDate;

        /// <summary>
        /// 比赛数据
        /// </summary>
        private ConcurrentDictionary<string, ConcurrentDictionary<string, BasicInfo>> dicGameData = new ConcurrentDictionary<string, ConcurrentDictionary<string, BasicInfo>>();

        /// <summary>
        /// 旧的比赛数据
        /// </summary>
        private ConcurrentDictionary<string, ConcurrentDictionary<string, BasicInfo>> dicOldGameData = new ConcurrentDictionary<string, ConcurrentDictionary<string, BasicInfo>>();
        /// <summary>
        /// 用于移除内存中的
        /// </summary>
        private BasicInfo bi = new BasicInfo(0, "", DateTime.Now, "");
    }
}