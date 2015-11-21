using CsharpHttpHelper;
using Follow.Sports.Basic;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Follow.Sports
{
    class BbTBS : Basic.BasicBaseball
    {
        // 下載資料
        private string DownHome;
        private Dictionary<string, string[]> DownReal;
        private DateTime DownLastTime = DateTime.Now;
        private DateTime dt = DateTime.Now;
        //创建Httphelper对象
        private HttpHelper http = null;

        private string sWebUrl1 = UrlSetting.GetUrl(ESport.Baseball_TBS, "Url1");
        public BbTBS(DateTime today)
            : base(ESport.Baseball_TBS)
        {
            // 設定
            this.AllianceID = 46;
            this.GameType = "BBJP";
            this.GameDate = today.Date; // 只取日期

            this.DownHome = string.Empty;
            this.DownReal = new Dictionary<string, string[]>();

            http = new HttpHelper();

            if (string.IsNullOrWhiteSpace(this.sWebUrl))
            {
                this.sWebUrl = "http://www.tbs.co.jp/baseball/top/main.htm";
            }
            if (string.IsNullOrWhiteSpace(this.sWebUrl1))
            {
                this.sWebUrl1 = "http://www.tbs.co.jp/baseball/game/{0}";
            }
        }

        public override void Download()
        {
            // 沒有資料或下載時間超過 0.5 分鐘才讀取首頁資料。
            if (this.GameData.Count == 0 ||
                DateTime.Now > this.DownLastTime.AddSeconds(30))
            {
                //获取请请求的Html
                this.DownHome = HttpDownload(this.sWebUrl, out this.DownLastTime);
            }

            // 下載比賽資料
            foreach (KeyValuePair<string, string[]> real in this.DownReal)
            {
                // 沒有資料或下載時間超過 5 秒才讀取資料。
                if (real.Value == null ||
                    DateTime.Now >= Convert.ToDateTime(real.Value[0]).AddSeconds(5))
                {
                    string sURL = string.Format(this.sWebUrl1, real.Key);//URL               

                    dt = DateTime.Now;
                    real.Value[1] = HttpDownload(sURL, out dt);
                    real.Value[0] = dt.ToString("yyyy-MM-dd HH:mm:ss");
                }
            }
        }
        public override int Follow()
        {
            return this.FollowByWeb();
        }

        private int FollowByWeb()
        {
            // 沒有資料就離開
            if (string.IsNullOrEmpty(this.DownHome)) return 0;

            int result = 0;
            BasicInfo gameInfo = null;
            DateTime gameDate = this.GameDate;
            HtmlDocument document = new HtmlDocument();
            // 載入資料
            document.LoadHtml(this.DownHome);

            HtmlNode contentNode = document.GetElementbyId("wrapper");
            if (contentNode == null) { return 0; }

            //查找日期
            HtmlNode nodeDate = contentNode.SelectSingleNode(".//dt");
            if (nodeDate != null)
            {
                string DateString = nodeDate.InnerText;
                DateString = DateString.Substring(0, DateString.IndexOf("日") + 1);
                DateTime.TryParse(DateString, out gameDate);
            }

            HtmlNodeCollection nodeGames = contentNode.SelectNodes(".//div[contains(@class,'t002')]/table");

            // 判斷資料
            if (nodeGames != null)
            {
                foreach (HtmlNode item in nodeGames)
                {
                    //未开赛
                    if (item.SelectSingleNode(".//th[@class='state']").InnerText.IndexOf("開始") != -1)
                    {
                        continue;
                    }

                    //内页网址
                    string sWeb = "";
                    //状态文字
                    string sState = "";
                    //状态
                    HtmlNode state = item.SelectSingleNode(".//th[@class='state']/strong[1]");
                    sState = state.InnerText;
                    if (state.ChildNodes.Count != 0)
                    {
                        sWeb = state.ChildNodes[0].GetAttributeValue("href", "");
                        sWeb = sWeb.Substring(sWeb.LastIndexOf("/") + 1);
                    }

                    
                    // 建立比賽資料
                    gameInfo = null;
                    gameInfo = new BasicInfo(this.AllianceID, this.GameType, gameDate, sWeb);
                    gameInfo.IsBall = true; // 賽程的主客隊與資料是相反的
                    gameInfo.Away = item.SelectSingleNode(".//th[@class='teamVisitor']").InnerText.Replace("　", "").Replace(" ", "");
                    gameInfo.AwayPoint = "";
                    gameInfo.Home = item.SelectSingleNode(".//th[@class='teamHome']").InnerText.Replace("　", "").Replace(" ", "");
                    gameInfo.HomePoint = "";
                    if (sState.IndexOf("中止") != -1)
                    {
                        gameInfo.GameStates = "P";
                    }
                    else if (sState.IndexOf("試合終了") != -1)
                    {
                        gameInfo.GameStates = "E";
                    }
                    else
                    {
                        gameInfo.GameStates = "S";
                    }

                    #region 下載比賽資料
                    if (!this.DownReal.ContainsKey(sWeb))
                    {
                        this.DownReal[sWeb] = new string[] { "", "" };
                        string sURL = string.Format(this.sWebUrl1, sWeb);//URL      
                        dt = DateTime.Now;
                        this.DownReal[sWeb][1] = HttpDownload(sURL, out dt);
                        this.DownReal[sWeb][0] = dt.ToString("yyyy-MM-dd HH:mm:ss");
                    }
                    #endregion 下載比賽資料
                    #region 处理比賽資料
                    if (this.DownReal.ContainsKey(sWeb) && !string.IsNullOrEmpty(this.DownReal[sWeb][1]))
                    {
                        if (gameInfo.GameStates != "P")
                        {
                            // 載入資料
                            HtmlDocument document2 = new HtmlDocument();
                            HtmlNode contentNode2 = null;
                            document2.LoadHtml(this.DownReal[sWeb][1]);
                            contentNode2 = document2.GetElementbyId("wrapper");
                            if (contentNode2 == null) { return 0; }
                            #region 分數
                            HtmlNode nodeTop = contentNode2.SelectSingleNode(".//table[@class='t003']/tbody/tr[2]");
                            foreach (HtmlNode td in nodeTop.ChildNodes)
                            {
                                if (td.Name != "td" || td.InnerText == " " || td.GetAttributeValue("class", "").IndexOf("team") != -1)
                                {
                                    continue;
                                }
                                // 分數
                                gameInfo.AwayBoard.Add(td.InnerText.Replace("&nbsp;", ""));
                            }
                            gameInfo.AwayPoint = gameInfo.AwayBoard[gameInfo.AwayBoard.Count - 3];
                            gameInfo.AwayH = gameInfo.AwayBoard[gameInfo.AwayBoard.Count - 2];
                            gameInfo.AwayE = gameInfo.AwayBoard[gameInfo.AwayBoard.Count - 1];
                            gameInfo.AwayBoard.RemoveAt(gameInfo.AwayBoard.Count - 1);
                            gameInfo.AwayBoard.RemoveAt(gameInfo.AwayBoard.Count - 1);
                            gameInfo.AwayBoard.RemoveAt(gameInfo.AwayBoard.Count - 1);
                            HtmlNode nodeBottom = contentNode2.SelectSingleNode(".//table[@class='t003']/tbody/tr[3]");
                            foreach (HtmlNode td in nodeBottom.ChildNodes)
                            {
                                if (td.Name != "td" || td.InnerText == " " || td.GetAttributeValue("class", "").IndexOf("team") != -1)
                                {
                                    continue;
                                }
                                // 分數
                                gameInfo.HomeBoard.Add(td.InnerText.Replace("&nbsp;", ""));
                            }
                            gameInfo.HomePoint = gameInfo.HomeBoard[gameInfo.HomeBoard.Count - 3];
                            gameInfo.HomeH = gameInfo.HomeBoard[gameInfo.HomeBoard.Count - 2];
                            gameInfo.HomeE = gameInfo.HomeBoard[gameInfo.HomeBoard.Count - 1];
                            gameInfo.HomeBoard.RemoveAt(gameInfo.HomeBoard.Count - 1);
                            gameInfo.HomeBoard.RemoveAt(gameInfo.HomeBoard.Count - 1);
                            gameInfo.HomeBoard.RemoveAt(gameInfo.HomeBoard.Count - 1);
                            #endregion 分數

                            #region  BSO bases  TBS来源网没有
                            gameInfo.BallB = 0;
                            gameInfo.BallS = 0;
                            gameInfo.BallO = 0;
                            gameInfo.Bases = 0;
                            #endregion BSO bases

                            #region 状态
                            HtmlNode nodeStatic = contentNode2.SelectSingleNode(".//div[@class='clearfix']/dl[@class!='gameComments']");
                            if (nodeStatic != null)
                            {
                                if (nodeStatic.InnerText.IndexOf("試合前") != -1)
                                {
                                    gameInfo.GameStates = "X";
                                    gameInfo.AwayPoint = "";
                                    gameInfo.AwayBoard.Clear();
                                    gameInfo.AwayH = null;
                                    gameInfo.AwayE = null;
                                    gameInfo.HomePoint = "";
                                    gameInfo.HomeBoard.Clear();
                                    gameInfo.HomeH = null;
                                    gameInfo.HomeE = null;
                                }
                                if (nodeStatic.InnerText.IndexOf("試合終了") != -1)
                                {
                                    gameInfo.GameStates = "E";
                                }
                            }
                            #endregion 状态
                        }

                        if (base.CheckGame(gameInfo))//檢查比分是否合法
                        {
                            this.GameData[gameInfo.WebID] = gameInfo;// 加入
                            result++;// 累計
                        }
                    }
                    #endregion 处理比賽資料
                }
            }
            return result;
        }
        public override bool Update(string connectionString, BasicInfo info)
        {
            // 多來源跟分
            return this.Update4(connectionString, info);
        }
        private string HttpDownload(string url, out DateTime date)
        {
            //创建Httphelper参数对象
            HttpItem item = new HttpItem()
            {
                URL = string.Format("{0}?tk={1}", url, DateTime.Now.Ticks.ToString()),//URL     必需项    
                Method = "get",//URL     可选项 默认为Get   
                ContentType = "text/html",//返回类型    可选项有默认值   
            };
            //请求的返回值对象
            HttpResult result = http.GetHtml(item);
            date = DateTime.Now;
            //获取请请求的Html
            return result.Html;
        }
    }
}
