using Follow.Sports.Basic;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;

// 跟盤以 ID 為依據
// 跟盤日期以日本時間顯示
// 特例 日本篮球需要登录访问

namespace Follow.Sports
{
    public class BkBJ : Basic.BasicBasketball
    {
        public BkBJ(DateTime today)
            : base(ESport.Basketball_BJ)
        {
            // 設定
            this.AllianceID = 10;
            this.GameType = "BKJP";
            this.GameDate = GetUtcJp(today).Date; // 只取日期
            this.DownHome = new BasicDownload(this.Sport, @"http://www.bj-league.com/");
            this.DownReal = new Dictionary<string, string[]>();
            #region
            //CookieContainer cc = new CookieContainer();
            //string url = "https://www.basketballjapantv.com/logins/login";
            //HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            //request.Method = "POST";
            //request.ContentType = "application/x-www-form-urlencoded";
            //cc.Add(new Cookie("PHPSESSID","ubot579m9ehssok33d6spl71c0"));
            //cc.Add(new Cookie("__ulfpc", "201412151109329186_f"));
            //cc.Add(new Cookie("__utmc", "145334439"));
            //cc.Add(new Cookie("__utma", "145334439.376807439.1418609418.1418624249.1418634144.4"));
            //cc.Add(new Cookie("__utmz", "145334439.1418609418.1.1.utmcsr=bj-league.com|utmccn=(referral)|utmcmd=referral|utmcct=/"));
            //cc.Add(new Cookie("__utmb", "145334439.6.10.1418634144"));

            //request.CookieContainer = cc;
            //request.Accept = "text/html, application/xhtml+xml, */*";
            //request.Referer = "https://www.basketballjapantv.com/logins/login";
            //request.Host = "www.basketballjapantv.com";
            //string data = "&data%5BUser%5D%5Blogin_name%5D=tt888001@yahoo.com.tw&data%5BUser%5D%5Blogin_password%5D=qaz369&data%5BUser%5D%5Bremember_me%5D=0&x=49&y=24";
            //request.ContentLength = data.Length;
            ////StreamWriter writer = new StreamWriter(request.GetRequestStream(), Encoding.UTF8);
            ////writer.Write(data);
            ////writer.Flush();
            //request.UserAgent = @"Mozilla/5.0 (Windows; U; Windows NT 5.2; ru; rv:1.9.0.8) Gecko/2009032609 Firefox/3.0.8 (.NET CLR 4.0.20506)";
            //Encoding encoding = Encoding.UTF8;//根据网站的编码自定义  
            //byte[] postData = encoding.GetBytes(data);//postDataStr即为发送的数据
            //request.ContentLength = postData.Length;
            //Stream requestStream = request.GetRequestStream();
            //requestStream.Write(postData, 0, postData.Length);

            //HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            //Stream responseStream = response.GetResponseStream();
            //StreamReader streamReader = new StreamReader(responseStream, encoding);
            //string retString = streamReader.ReadToEnd();

            //Uri u= new Uri("http://www.basketballjapantv.com/files/xml/play_by_play/games/2014121301/status.json");

            //streamReader.Close();
            //responseStream.Close();

            //string postString = "&data%5BUser%5D%5Blogin_name%5D=tt888001@yahoo.com.tw&data%5BUser%5D%5Blogin_password%5D=qaz369&data%5BUser%5D%5Bremember_me%5D=0&x=49&y=24";//这里即为传递的参数，可以用工具抓包分析，也可以自己分析，主要是form里面每一个name都要加进来  
            //byte[] postData = Encoding.UTF8.GetBytes(postString);//编码，尤其是汉字，事先要看下抓取网页的编码方式  
            //string url = "https://www.basketballjapantv.com/logins/login";//地址  
            //WebClient webClient = new WebClient();
            //webClient.Headers.Add("Content-Type", "application/x-www-form-urlencoded");//采取POST方式必须加的header，如果改为GET方式的话就去掉这句话即可  
            //byte[] responseData = webClient.UploadData(url, "POST", postData);//得到返回字符流  
            //string srcString = Encoding.UTF8.GetString(responseData);//解码  
            #endregion

            //
            GetCookieContainer();

            //获取帐号
            loginUser = frmMain.bkbj_user.Trim().Split(',');
            loginPwd = frmMain.bkbj_pwd.Trim().Split(',');

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

            //登录
            if (!IsLogin)
            {
                GetIndex();
                loginParam = string.Format("data[User][login_name]={0}&data[User][login_password]={1}&data[User][remember_me]=0&x=33&y=18", loginUser[loginIndex], loginPwd[loginIndex]);
                SendLoginHttpWedRequest();
            }

            // 下載比分資料
            foreach (KeyValuePair<string, string[]> real in this.DownReal)
            {
                // 沒有資料或下載時間超過 2 秒才讀取資料。
                if (string.IsNullOrWhiteSpace(real.Value[0]) ||
                    DateTime.Now >= Convert.ToDateTime(real.Value[0]).AddSeconds(2))
                {
                    SendHttpWedRequest2(real.Key);
                }
            }
        }
        public override int Follow()
        {
            // 沒有資料就離開
            if (string.IsNullOrEmpty(this.DownHome.Data)) return 0;

            int result = 0;
            BasicInfo gameInfo = null;
            HtmlAgilityPack.HtmlDocument document = new HtmlAgilityPack.HtmlDocument();
            string xPath = "/html[1]/body[1]/div[1]/div[3]/div[1]/div[3]/div[1]/div[1]/table[1]";
            // 載入資料
            document.LoadHtml(this.DownHome.Data);
            // 資料位置
            HtmlAgilityPack.HtmlNode nodeGames = document.DocumentNode.SelectSingleNode(xPath);
            // 判斷資料
            if (nodeGames != null && nodeGames.ChildNodes != null)
            {
                DateTime gameDate = this.GameDate;
                DateTime gameTime = this.GameDate;
                // 資料
                foreach (HtmlAgilityPack.HtmlNode game in nodeGames.ChildNodes)
                {
                    // 不是資料就往下處理
                    if (game.Name != "tr") continue;

                    string webID = game.ChildNodes[3].Id;
                    string doc = game.ChildNodes[1].InnerText;
                    string[] data = null;
                    string[] team = null;

                    #region 跟盤 ID
                    // 取代文字
                    doc = doc.Replace("\r", "");
                    doc = doc.Replace("\n", "");
                    doc = doc.Replace("\t", "");
                    // 分割字串
                    data = doc.Split(new string[] { "　" }, StringSplitOptions.RemoveEmptyEntries);
                    // 資料是錯的就往下處理
                    if (data.Length != 4)
                    {
                        continue;
                    }
                    team = data[2].Split(new string[] { "vs." }, StringSplitOptions.RemoveEmptyEntries);
                    // 資料是錯的就往下處理
                    if (team.Length != 2)
                    {
                        continue;
                    }
                    // 刪除無用資料
                    if (data[0].IndexOf("（") != -1)
                    {
                        data[0] = data[0].Substring(0, data[0].IndexOf("（")).Trim();
                    }
                    data[0].Replace("月", "/");
                    data[0].Replace("日", "/");
                    // 日期是錯的就往下處理
                    if (!DateTime.TryParse(data[0] + " " + data[1], out gameTime))
                    {
                        continue;
                    }
                    #endregion

                    // 不是今天就往下處理
                    if (gameTime.Date != this.GameDate.Date) continue;

                    // 建立比賽資料
                    gameInfo = null;
                    gameInfo = new BasicInfo(this.AllianceID, this.GameType, gameTime, webID);
                    gameInfo.AcH = true; // 第一個資料是 home 的，所以要交換隊伍資料。
                    gameInfo.Away = team[1].Trim();
                    gameInfo.Home = team[0].Trim();

                    #region 下載比賽資料，比賽時間 10 分鐘前就開始處理
                    if (!this.DownReal.ContainsKey(webID) &&
                        GetUtcJp(DateTime.Now) >= gameTime.AddMinutes(-10))
                    {
                        this.DownReal.Add(webID, new string[] { DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), null });
                        SendHttpWedRequest2(webID);
                    }
                    #endregion
                    #region 處理比賽資料
                    if (this.DownReal.ContainsKey(webID) &&
                        !string.IsNullOrEmpty(this.DownReal[webID][1]))
                    {
                        // 錯誤處理
                        try
                        {
                            JObject json = JObject.Parse(this.DownReal[webID][1]);
                            // 總分
                            gameInfo.AwayPoint = json["game_score"]["away_point"].ToString();
                            gameInfo.HomePoint = json["game_score"]["home_point"].ToString();
                            // 分數
                            if (json["game_score"]["point"] != null && !string.IsNullOrEmpty(json["game_score"]["point"].ToString().Trim()))
                            {
                                JArray board = JArray.Parse(json["game_score"]["point"].ToString());
                                // 資料
                                for (int i = 0; i < board.Count; i++)
                                {
                                    gameInfo.AwayBoard.Add(board[i]["away_point"].ToString());
                                    gameInfo.HomeBoard.Add(board[i]["home_point"].ToString());
                                }
                            }
                            // 判斷比賽狀態 0 = 比賽未開始
                            if (json["game_score"]["quarter"].ToString() != "0")
                            {
                                // Status
                                if (json["game_score"]["game_status"].ToString() == "end")
                                {
                                    gameInfo.GameStates = "E";
                                    gameInfo.Status = null;
                                }
                                else
                                {
                                    gameInfo.GameStates = "S";
                                    gameInfo.Status = json["game_score"]["quarter_remaining_minutes"].ToString() + ":"
                                                    + json["game_score"]["quarter_remaining_seconds"].ToString();
                                    // 判斷
                                    if (gameInfo.Status == "00:00")
                                    {
                                        gameInfo.Status = "結束";
                                    }
                                }
                            }
                        }
                        catch { }
                    }
                    #endregion

                    // 加入
                    this.GameData[gameInfo.WebID] = gameInfo;
                    // 累計
                    result++;
                }
            }
            // 傳回
            return result;
        }
        // 下載資料
        private BasicDownload DownHome;
        private Dictionary<string, string[]> DownReal;
        private DateTime DownLastTime = DateTime.Now;
        private CookieContainer CookieContainer = new CookieContainer();
        private HttpWebRequest req;
        //登录参数
        private string loginParam = "";
        private string loginUrl = "https://www.basketballjapantv.com/logins/login";
        private DateTime loginLastDate = DateTime.Now;
        private bool IsLogin = false;//是否登录
        private string[] loginUser = null;//帐号数组
        private string[] loginPwd = null;
        private int loginIndex = -1; //帐号下标

        /// <summary>
        /// 设置cookie
        /// </summary>
        private void GetCookieContainer()
        {
            Cookie c1 = new Cookie("PHPSESSID", "n12c9fn1eson7n0r0nckh838u5", "/", ".www.basketballjapantv.com");
            Cookie c2 = new Cookie("__utma", "145334439.1514699796.1418626227.1418626227.1418626227.1", "/", ".basketballjapantv.com");
            Cookie c3 = new Cookie("__utmb", "145334439.3.10.1418626227", "/", ".basketballjapantv.com");
            Cookie c4 = new Cookie("__utmc", "145334439", "/", ".basketballjapantv.com");
            Cookie c5 = new Cookie("__utmz", "145334439.1418626227.1.1.utmcsr=bj-league.com|utmccn=(referral)|utmcmd=referral|utmcct=/", "/", ".basketballjapantv.com");

            CookieContainer.Add(c1);
            CookieContainer.Add(c2);
            CookieContainer.Add(c3);
            CookieContainer.Add(c4);
            CookieContainer.Add(c5);
        }
        /// <summary>
        /// 发送登录请求
        /// </summary>
        /// <param name="param"></param>
        /// <param name="url"></param>
        /// <returns></returns>
        private void SendLoginHttpWedRequest()
        {
            string retStr = string.Empty;
            byte[] bs = Encoding.UTF8.GetBytes(loginParam);
            req = (HttpWebRequest)HttpWebRequest.Create(loginUrl);
            req.CookieContainer = CookieContainer;
            req.Method = "POST";
            req.ContentType = "application/x-www-form-urlencoded";
            req.ContentLength = bs.Length;
            Stream reqStream = req.GetRequestStream();
            reqStream.Write(bs, 0, bs.Length);
            reqStream.Close();
            HttpWebResponse wr = req.GetResponse() as HttpWebResponse;
            StreamReader reader = new StreamReader(wr.GetResponseStream(), Encoding.UTF8);
            retStr = reader.ReadToEnd();
            //string server = req.Address.Host;
            //string strcrook = req.CookieContainer.GetCookieHeader(req.RequestUri);
            wr.Close();
            loginLastDate = DateTime.Now;
            //检查是否登录成功  成员菜单                              我的账户                               会员退出
            if (retStr.IndexOf("会員メニュー") != -1 && retStr.IndexOf("マイアカウント") != -1 && retStr.IndexOf("会員退会") != -1)
            {
                IsLogin = true;
            }
        }
        /// <summary>
        /// 下载比分资料
        /// </summary>
        /// <param name="wid"></param>
        /// <returns></returns>
        private void SendHttpWedRequest2(string wid)
        {
            string retStr = string.Empty;
            string url = string.Format("http://www.basketballjapantv.com/get_pbp.php?key={0}.games.status.json&path=/play_by_play/games/{0}/status.json", wid);

            try
            {
                byte[] bs = Encoding.UTF8.GetBytes("");
                req = (HttpWebRequest)HttpWebRequest.Create(url);
                req.Headers.Set("x-requested-with", "XMLHttpRequest");
                req.CookieContainer = CookieContainer;
                req.Method = "POST";
                req.ContentType = "application/x-www-form-urlencoded";
                req.ContentLength = bs.Length;
                req.Referer = string.Format("http://www.basketballjapantv.com/game_lives/vod/{0}/", wid);
                Stream reqStream = req.GetRequestStream();
                reqStream.Write(bs, 0, bs.Length);
                reqStream.Close();

                HttpWebResponse wr = req.GetResponse() as HttpWebResponse;
                StreamReader reader = new StreamReader(wr.GetResponseStream(), Encoding.UTF8);
                retStr = reader.ReadToEnd();
                //string server = req.Address.Host;
                //string strcrook = req.CookieContainer.GetCookieHeader(req.RequestUri);
                wr.Close();
                this.DownReal[wid][0] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                this.DownReal[wid][1] = retStr;
            }
            catch (Exception)
            {
                IsLogin = false;
            }
        }

        /// <summary>
        /// 获取帐号下标
        /// </summary>
        private void GetIndex()
        {
            if (loginUser != null)
            {
                loginIndex++;
                if (loginIndex < 0 || loginIndex >= loginUser.Length)
                {
                    loginIndex = 0;
                }
            }
        }
    }
}
