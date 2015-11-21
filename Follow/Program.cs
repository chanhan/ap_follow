using Follow.Sports.Basic;
using SHGG.DataStructerService;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Follow
{
    static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            bool startToFollow = false;
            ESport sport = ESport.None;
            DateTime gameDate = DateTime.Now.AddDays(-100);

            #region 讀取參數
            string sGameDate = null;
            foreach (string cmd in args)
            {
                if (cmd.ToLower() == "follow")
                    startToFollow = true;
                if (cmd.IndexOf("_") != -1)//有找到特殊的時間參數格式 yyyy/MM/dd_hh:mm                
                    sGameDate = cmd.Replace("_", " ");//還原時間格式
                else
                    sport = GetSportType(cmd);
            }

            if (!string.IsNullOrEmpty(sGameDate))//時間參數不為空
                DateTime.TryParse(sGameDate, out gameDate);

            #endregion
            #region 讀取設定
            string xmlFile = string.Format(@"{0}\{1}.xml", Application.StartupPath, System.IO.Path.GetFileNameWithoutExtension(Application.ExecutablePath));
            // 取得資料
            if (System.IO.File.Exists(xmlFile))
            {
                try
                {
                    XmlAdapter xmlAdapter = new XmlAdapter(xmlFile);
                    xmlAdapter.GoToNode("XML", "Database");
                    // 設定
                    frmMain.SqlServer = xmlAdapter.ReadXmlNode("SqlServer");
                    frmMain.SqlDB = xmlAdapter.ReadXmlNode("DB");
                    frmMain.SqlUID = xmlAdapter.ReadXmlNode("UID");
                    frmMain.SqlPWD = xmlAdapter.ReadXmlNode("PWD");

                    bool tmpBool = true;
                    if (bool.TryParse(xmlAdapter.ReadXmlNode("WRITE"), out tmpBool))//是否寫入DB
                        frmMain.WRITE = tmpBool;
                    else
                        frmMain.WRITE = true;

                    //讀取墨西哥棒球 冬季賽季
                    xmlAdapter = new XmlAdapter(xmlFile);
                    xmlAdapter.GoToNode("XML", "BBLMP");
                    frmMain.bblmp_season = xmlAdapter.ReadXmlNode("season");

                    //读取日本篮球帐号密码
                    xmlAdapter = new XmlAdapter(xmlFile);
                    xmlAdapter.GoToNode("XML", "BKBJ");
                    frmMain.bkbj_user = xmlAdapter.ReadXmlNode("user");
                    frmMain.bkbj_pwd = xmlAdapter.ReadXmlNode("pwd");

                    //读取使用代理设定的秒数
                    xmlAdapter = new XmlAdapter(xmlFile);
                    xmlAdapter.GoToNode("XML", "ProxySettings");
                    frmMain.UseProxy = Convert.ToInt32(xmlAdapter.ReadXmlNode("UseProxySeconds"));
                    // WKBL
                    //xmlAdapter = new XmlAdapter(xmlFile);
                    //xmlAdapter.GoToNode("XML", "WKBL");
                    //Sports.BkWKBL.Proxy = xmlAdapter.ReadXmlNode("Proxy");
                }
                catch { }
            }
            #endregion

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new frmMain(startToFollow, sport, gameDate));
        }

        public static ESport GetSportType(string cmd)
        {
            ESport sport = ESport.None;

            // 轉成小寫，判斷跟盤類型
            switch (cmd.ToLower())
            {
                // 足球
                case "football":
                    sport = ESport.Football;
                    break;
                case "nfl":
                    sport = ESport.Football_NFL;
                    break;

                // 棒球
                case "cpbl":
                    sport = ESport.Baseball_CPBL;
                    break;
                case "cpbl2":
                    sport = ESport.Baseball_CPBL2;
                    break;
                case "npb":
                    sport = ESport.Baseball_NPB;
                    break;
                case "npb2":
                    sport = ESport.Baseball_NPB2;
                    break;
                case "npb3":
                    sport = ESport.Baseball_NPB3;
                    break;
                case "tbs":
                    sport = ESport.Baseball_TBS;
                    break;
                case "kbo":
                    sport = ESport.Baseball_KBO;
                    break;
                case "kbo2":
                    sport = ESport.Baseball_KBO2;
                    break;
                case "kbo3":
                    sport = ESport.Baseball_KBO3;
                    break;
                case "mlb":
                    sport = ESport.Baseball_MLB;
                    break;
                case "mlb2":
                    sport = ESport.Baseball_MLB2;
                    break;
                case "mlb3":
                    sport = ESport.Baseball_MLB3;
                    break;
                case "il":
                    sport = ESport.Baseball_IL;
                    break;
                case "pcl":
                    sport = ESport.Baseball_PCL;
                    break;
                case "lmp":
                    sport = ESport.Baseball_LMP;
                    break;
                case "lmb":
                    sport = ESport.Baseball_LMB;
                    break;
                case "abl":
                    sport = ESport.Baseball_ABL;
                    break;
                case "hb":
                    sport = ESport.Baseball_HB;
                    break;
                // 籃球
                //case "sbl":
                //    sport = ESport.Basketball_SBL;
                //    break;
                case "cba":
                    sport = ESport.Basketball_CBA;
                    break;
                case "bj":
                    sport = ESport.Basketball_BJ;
                    break;
                case "kbl":
                    sport = ESport.Basketball_KBL;
                    break;
                case "wkbl":
                    sport = ESport.Basketball_WKBL;
                    break;
                case "nba":
                    sport = ESport.Basketball_NBA;
                    break;
                case "wnba":
                    sport = ESport.Basketball_WNBA;
                    break;
                case "euroleague":
                    sport = ESport.Basketball_Euroleague;
                    break;
                case "eurocup":
                    sport = ESport.Basketball_Eurocup;
                    break;
                case "vtb":
                    sport = ESport.Basketball_VTB;
                    break;
                case "nbl":
                    sport = ESport.Basketball_NBL;
                    break;
                case "fiba":
                    sport = ESport.Basketball_FIBA;
                    break;
                case "ebt":
                    sport = ESport.Basketball_EBT;
                    break;
                case "acb":
                    sport = ESport.Basketball_ACB;
                    break;
                case "bbl":
                    sport = ESport.Basketball_BBL;
                    break;
                case "ncaa":
                    sport = ESport.Basketball_NCAA;
                    break;
                case "cnbl":
                    sport = ESport.Basketball_CNBL;
                    break;
                case "bkos":
                    sport = ESport.Basketball_OS;
                    break;
                case "bkbf":
                    sport = ESport.Basketball_BF;
                    break;

                // 網球
                case "tennis":
                    sport = ESport.Tennis;
                    break;
                // 曲棍球
                case "nhl":
                    sport = ESport.Hockey_NHL;
                    break;
                case "ahl":
                    sport = ESport.Hockey_AHL;
                    break;
                case "khl":
                    sport = ESport.Hockey_KHL;
                    break;
                case "ihbf":
                    sport = ESport.Hockey_IHBF;
                    break;
                case "pkjq":
                    sport = ESport.DaQiuData;
                    break;
            }
            return sport;
        }
    }
}
