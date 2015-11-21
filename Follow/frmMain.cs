using Follow.Sports;
using Follow.Sports.Basic;
using Newtonsoft.Json.Linq;
using SHGG.FileService;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Threading.Tasks;

namespace Follow
{
    public partial class frmMain : Form
    {
        #region Form
        private LogFile Logs;
        System.Timers.Timer timerFollow = new System.Timers.Timer(5000);// 跟盤計時器 預設值

        //private DateTime assignGameDate;//指定的開賽時間
        public frmMain(bool startToFollow, ESport sport, DateTime gameDate)
        {
            InitializeComponent();
            // 設定
            this.Icon = Properties.Resources.Flag;
            this.Tag = this.Text;
            this.tspcbbSport.Font = new Font(this.tspcbbSport.Font.FontFamily, 11);
            this.tspbtnStart.Font = new Font(this.tspbtnStart.Font.FontFamily, 11);
            this.tspbtnStop.Font = new Font(this.tspbtnStop.Font.FontFamily, 11);
            this.lsbInfo.Font = new Font(this.lsbInfo.Font.FontFamily, 14);

            #region 設定跟盤
            this.AddSport(ESport.None, sport);
            // 足球
            this.AddSport(ESport.Football, sport);
            this.AddSport(ESport.Football_NFL, sport);
            // 棒球
            this.AddSport(ESport.Baseball_CPBL, sport);
            this.AddSport(ESport.Baseball_CPBL2, sport);
            this.AddSport(ESport.Baseball_NPB, sport);
            this.AddSport(ESport.Baseball_NPB2, sport);
            this.AddSport(ESport.Baseball_NPB3, sport);
            this.AddSport(ESport.Baseball_TBS, sport);
            this.AddSport(ESport.Baseball_KBO, sport);
            this.AddSport(ESport.Baseball_KBO2, sport);
            this.AddSport(ESport.Baseball_KBO3, sport);
            this.AddSport(ESport.Baseball_MLB, sport);
            this.AddSport(ESport.Baseball_MLB2, sport);
            this.AddSport(ESport.Baseball_MLB3, sport);
            this.AddSport(ESport.Baseball_IL, sport);
            this.AddSport(ESport.Baseball_PCL, sport);
            this.AddSport(ESport.Baseball_LMP, sport);
            this.AddSport(ESport.Baseball_LMB, sport);
            this.AddSport(ESport.Baseball_ABL, sport);
            this.AddSport(ESport.Baseball_HB, sport);
            // 籃球
            //this.AddSport(ESport.Basketball_SBL, sport);
            this.AddSport(ESport.Basketball_CBA, sport);
            this.AddSport(ESport.Basketball_BJ, sport);
            this.AddSport(ESport.Basketball_KBL, sport);
            this.AddSport(ESport.Basketball_WKBL, sport);
            this.AddSport(ESport.Basketball_NBA, sport);
            this.AddSport(ESport.Basketball_WNBA, sport);
            this.AddSport(ESport.Basketball_Euroleague, sport);
            this.AddSport(ESport.Basketball_Eurocup, sport);
            this.AddSport(ESport.Basketball_VTB, sport);
            this.AddSport(ESport.Basketball_NBL, sport);
            this.AddSport(ESport.Basketball_FIBA, sport);
            this.AddSport(ESport.Basketball_EBT, sport);
            this.AddSport(ESport.Basketball_ACB, sport);
            this.AddSport(ESport.Basketball_BBL, sport);
            this.AddSport(ESport.Basketball_NCAA, sport);
            this.AddSport(ESport.Basketball_CNBL, sport);
            // 籃球奧訊
            this.AddSport(ESport.Basketball_OS, sport);
            //BF篮球
            this.AddSport(ESport.Basketball_BF, sport);
            // 網球
            this.AddSport(ESport.Tennis, sport);
            // 曲棍球
            this.AddSport(ESport.Hockey_NHL, sport);
            this.AddSport(ESport.Hockey_AHL, sport);
            this.AddSport(ESport.Hockey_KHL, sport);

            this.AddSport(ESport.Hockey_IHBF, sport);
            //其他 大球盤口角球比分
            this.AddSport(ESport.DaQiuData, sport);

            #endregion

            #region 日期

            // 建立目前跟盤日期
            this.dtpFollow = new DateTimePicker();
            //this.dtpFollow.ShowCheckBox = true;
            this.dtpFollow.Checked = true;
            this.dtpFollow.Width = 170;
            this.dtpFollow.Font = new Font(this.dtpFollow.Font.FontFamily, 13);

            if (gameDate > DateTime.Now.AddDays(-1))//時間參數合法
                this.dtpFollow.Value = gameDate;

            //加入介面
            this.tspMain.Items.Add(new ToolStripSeparator());
            this.tspMain.Items.Add(new ToolStripControlHost(this.dtpFollow));            

            #endregion
            #region 計時器
            this.timerFollow.Elapsed += (sender, e) =>
            {
                // 錯誤處理
                try
                {
                    // 委派處理跟盤 (有控制代碼才不會出錯)
                    if (this.IsHandleCreated)
                    {
                        this.BeginInvoke(new timerDelegate(this.OnFollow), new object[] { e.SignalTime });
                    }
                }
                catch (Exception) { }
            };
            #endregion

            // 開啟時跟盤
            this.StartToFollow = startToFollow;
        }
        private void frmMain_Load(object sender, EventArgs e)
        {
            //  測試連線資料庫
            if (!this.TestConnection())
            {
                MessageBox.Show("無法連接資料庫，應用程式無法執行。", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.CanExit = true;
                this.Close();
            }

            // 焦點
            this.lsbInfo.Focus();
        }
        private void frmMain_Shown(object sender, EventArgs e)
        {
            // 開啟時跟盤
            if (this.StartToFollow)
            {
                // 最小化
                this.WindowState = FormWindowState.Minimized;
                this.tspbtnStart_Click(this.tspbtnStart, new EventArgs());
            }
        }
        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            // 使用者結束程式時詢問
            if (e.CloseReason == CloseReason.UserClosing && !this.CanExit)
            {
                e.Cancel = !(MessageBox.Show("您確定要關閉程式？\n\n需要花費一些時間來進行程式關閉的動作。", this.Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) == System.Windows.Forms.DialogResult.Yes);
            }
        }
        private void frmMain_FormClosed(object sender, FormClosedEventArgs e)
        {
            // 查詢通知 - 關閉
            //SqlDependency.Stop(ConnectionString);
            //this.Sport.Dispose();

            if (this.Sport != null) { this.Sport.Dispose(); }
        }

        private bool CanExit;       // 可關閉程式
        private bool StartToFollow; // 開啟時跟盤
        #endregion
        #region Tools
        private void tspbtnStart_Click(object sender, EventArgs e)
        {
            // 判斷跟盤
            if (this.tspcbbSport.SelectedIndex == 0)
            {
                MessageBox.Show(this, "您尚未選擇跟盤的比賽！", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            ESport sport = (ESport)this.tspcbbSport.SelectedIndex;
 
            switch (sport) 
            {
                case ESport.Basketball_ACB:
                case ESport.Basketball_BBL:
                case ESport.Basketball_BJ:
                case ESport.Basketball_CBA:
                case ESport.Basketball_CNBL:
                case ESport.Basketball_EBT:
                case ESport.Basketball_Eurocup:
                case ESport.Basketball_Euroleague:
                case ESport.Basketball_FIBA:
                case ESport.Basketball_KBL:
                case ESport.Basketball_NBA:
                case ESport.Basketball_NBL:
                case ESport.Basketball_NCAA:          
                case ESport.Basketball_VTB:
                case ESport.Basketball_WKBL:
                case ESport.Basketball_WNBA:                 
                    timerFollow.Interval = 1000;
                    break;
                case ESport.Basketball_OS:
                case ESport.Basketball_BF:
                    timerFollow.Interval = 2000;
                    break;
                case ESport.DaQiuData:
                    timerFollow.Interval = 20000;
                    break;
                default:
                    timerFollow.Interval = 5000;
                    break;
            }

            this.Logs = new LogFile(sport);//設定log type

            // 清除跟盤
            if (this.Sport != null)
            {
                this.Sport.Dispose();
                this.Sport = null;
            }
            // 按鈕
            this.tspbtnStart.Enabled = false;
            this.tspbtnStop.Enabled = true;
            this.tspcbbSport.Enabled = false;
            this.dtpFollow.Enabled = false;
            // 啟動計時器
            this.timerFollow.Start();
            // 訊息 非同步處理
            this.InvokeIfRequired(() => this.AddInfo(null));            
            this.InvokeIfRequired(() => this.AddInfo("開始跟盤：" + this.tspcbbSport.Text));
            this.InvokeIfRequired(() => this.AddInfo('-'));
            // 焦點
            this.lsbInfo.Focus();          
        }
        private void tspbtnStop_Click(object sender, EventArgs e)
        {
            // 按鈕
            this.tspbtnStart.Enabled = true;
            this.tspbtnStop.Enabled = false;
            this.tspcbbSport.Enabled = true;
            this.dtpFollow.Enabled = true;
            // 停止計時器
            this.timerFollow.Stop();
            // 訊息 非同步處理
            this.InvokeIfRequired(() => this.AddInfo('-'));
            this.InvokeIfRequired(() => this.AddInfo("停止跟盤"));
            this.InvokeIfRequired(() => this.AddInfo(null));
            // 焦點
            this.lsbInfo.Focus();
        }
        private void tspcbbSport_SelectedIndexChanged(object sender, EventArgs e)
        {
            // 判斷選擇
            if (this.tspcbbSport.SelectedIndex == 0)
            {
                this.Text = this.Tag.ToString();
                this.Icon = Properties.Resources.Flag;
            }
            else
            {
                this.Text = "跟盤" + this.tspcbbSport.Text;
                ESport sport = (ESport)this.tspcbbSport.SelectedIndex;
                // 圖示
                switch (sport)
                {
                    // 足球
                    case ESport.Football:
                    case ESport.Football_NFL:
                        this.Icon = Properties.Resources.Football;
                        break;
                    // 棒球
                    case ESport.Baseball_CPBL:
                    case ESport.Baseball_CPBL2:
                    case ESport.Baseball_NPB:
                    case ESport.Baseball_NPB2:
                    case ESport.Baseball_NPB3:
                    case ESport.Baseball_TBS:
                    case ESport.Baseball_KBO:
                    case ESport.Baseball_KBO2:
                    case ESport.Baseball_KBO3:
                    case ESport.Baseball_MLB:
                    case ESport.Baseball_MLB2:
                    case ESport.Baseball_MLB3:
                    case ESport.Baseball_IL:
                    case ESport.Baseball_PCL:
                    case ESport.Baseball_LMP:
                    case ESport.Baseball_LMB:
                    case ESport.Baseball_ABL:
                    case ESport.Baseball_HB:
                        this.Icon = Properties.Resources.Baseball;
                        break;
                    // 籃球
                    //case ESport.Basketball_SBL:
                    case ESport.Basketball_CBA:
                    case ESport.Basketball_BJ:
                    case ESport.Basketball_KBL:
                    case ESport.Basketball_WKBL:
                    case ESport.Basketball_NBA:
                    case ESport.Basketball_WNBA:
                    case ESport.Basketball_Euroleague:
                    case ESport.Basketball_Eurocup:
                    case ESport.Basketball_VTB:
                    case ESport.Basketball_NBL:
                    case ESport.Basketball_FIBA:
                    case ESport.Basketball_EBT:
                    case ESport.Basketball_ACB:
                    case ESport.Basketball_BBL:
                    case ESport.Basketball_NCAA:
                    case ESport.Basketball_CNBL:
                    case ESport.Basketball_OS:
                    case ESport.Basketball_BF:
                        this.Icon = Properties.Resources.Basketball;
                        break;
                    // 網球
                    case ESport.Tennis:
                        this.Icon = Properties.Resources.Tennis;
                        break;
                    // 曲棍球
                    case ESport.Hockey_NHL:
                    case ESport.Hockey_AHL:
                    case ESport.Hockey_KHL:
                    case ESport.Hockey_IHBF:
                        this.Icon = Properties.Resources.Hockey;
                        break;
                }
            }
            // 版本
            this.Text += " v" + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
        }
        #endregion
        #region Follow
        private delegate void timerDelegate(DateTime signalTime);
        //System.Timers.Timer timerFollow = new System.Timers.Timer(1000);    // 跟盤計時器
//        System.Timers.Timer timerFollow = new System.Timers.Timer(5000);    // 跟盤計時器 修改更新間隔
        private DateTimePicker dtpFollow = null;                            // 目前跟盤日期
        // 跟盤
        private void OnFollow(DateTime signalTime)
        {
            // 不是跟盤就離開
            if (this.tspbtnStart.Enabled) return;
            // 停止計時器
            this.timerFollow.Stop();

            #region 建立跟盤資料
            if (this.Sport == null)
            {
                // 取出日期
                //DateTime today = (this.dtpFollow.Checked) ? (this.dtpFollow.Value.Date) : (DateTime.Now.Date);
                DateTime today = DateTime.Now;
                if (this.dtpFollow.Checked)
                    today = this.dtpFollow.Value;

                // 加入目前時間
                //today = DateTime.Parse(today.ToString("yyyy-MM-dd") + " " + DateTime.Now.ToString("HH:mm:ss"));
                // 建立
                this.Sport =this.GetSport(today);
            }
            // 沒有跟盤就離開
            if (this.Sport == null) return;
            #endregion

            Stopwatch sw = new Stopwatch();
            // 開始計時
            sw.Reset();
            sw.Start();
            // 錯誤處理
            try
            {
                // 讀取資料
                this.Sport.Download();
                // 跟盤比賽
                int count = this.Sport.Follow();
                string processTime = sw.ElapsedMilliseconds.ToString("N0").PadLeft(4);

                sw.Reset();
                sw.Start();

                // 讀取的比賽數量
                if (count > 0 && frmMain.WRITE) //是否寫入DB
                {
                    // 更新資料
                    count = this.Sport.Update(ConnectionString);
                    // 有更新資料
                    if (count > 0)
                    {
                        // 停止計時
                        sw.Stop();
                        // 訊息 非同步處理
                        string updateTime = sw.ElapsedMilliseconds.ToString("N0").PadLeft(4);
                        string msg = string.Format("完成跟盤 {0} (處理使用: {1} 毫秒，寫入使用：{2} 毫秒)", count.ToString().PadLeft(2, ' '), processTime, updateTime);
                        this.InvokeIfRequired(() => this.AddInfo(msg)); 
                        
                        // 顯示訊息
                        Console.Write(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " ");
                        Console.WriteLine(msg);

                        //回收釋放的記憶體
                        GC.Collect();
                    }
                    else
                        this.InvokeIfRequired(() => this.AddInfo("資料未變動"));

                }
            }
            catch (Exception ex)
            {
                this.Logs.Error("OnFollow Error Message:{0},\r\nStackTrace:{1}\r\n", ex.Message, ex.StackTrace);
            }

            // 啟動計時器
            if (!this.tspbtnStart.Enabled)
                this.timerFollow.Start();
        }

        private BasicFollow Sport;   // 跟盤資料
        private BasicFollow GetSport(DateTime today)
        {
            ESport sport = (ESport)this.tspcbbSport.SelectedIndex;
            EGameSource gameSource = GetGameSourceName(GetSportGameType(sport));
            BasicFollow result = null;
            // 依跟盤
            switch (sport)
            {
                // 足球
                case ESport.Football:
                    result = new Football(today);
                    break;
                case ESport.Football_NFL:
                    result = new FbNFL(today);
                    break;

                // 棒球
                case ESport.Baseball_CPBL:
                    result = new BbCPBL(today);
                    break;
                case ESport.Baseball_CPBL2:
                    result = new BbCPBL2(today);
                    break;
                case ESport.Baseball_NPB:
                    result = new BbNPB(today);
                    break;
                case ESport.Baseball_NPB2:
                    result = new BbNPB2(today);
                    break;
                case ESport.Baseball_NPB3:
                    result = new BbNPB3(today);
                    break;
                case ESport.Baseball_TBS:
                    result = new BbTBS(today);
                    break;
                case ESport.Baseball_KBO:
                    result = new BbKBO(today);
                    break;
                case ESport.Baseball_KBO2:
                    result = new BbKBO2(today);
                    break;
                case ESport.Baseball_KBO3:
                    result = new BBKBO3(today);
                    break;
                case ESport.Baseball_MLB:
                    result = new BbMLB(today);
                    break;
                case ESport.Baseball_MLB2:
                    result = new BbMLB2(today);
                    break;
                case ESport.Baseball_MLB3:
                    result = new BbMLB3(today);
                    break;
                case ESport.Baseball_IL:
                    result = new BbIL(today);
                    break;
                case ESport.Baseball_PCL:
                    result = new BbPCL(today);
                    break;
                case ESport.Baseball_LMP:
                    result = new BbLMP(today);
                    break;
                case ESport.Baseball_LMB:
                    result = new BbLMB(today);
                    break;
                case ESport.Baseball_ABL:
                    result = new BbABL(today);
                    break;
                case ESport.Baseball_HB:
                    result = new BbHB(today);
                    break;
                // 籃球
                case ESport.Basketball_CBA:
                    if (gameSource == EGameSource.Asiascore)
                        result = new BkCBA(today);
                    else if (gameSource == EGameSource.Bet007)
                        result = new BkCBABet007(today);
                    else
                        result = new BkCBABet007(today);
                    break;
                case ESport.Basketball_BJ:
                    result = new BkBJ(today);
                    break;
                case ESport.Basketball_KBL:
                    if (gameSource == EGameSource.Asiascore)
                        result = new BkKBL_NV(today);
                    else if (gameSource == EGameSource.Bet007)
                        result = new BkKBLBet007(today);
                    else
                        result = new BkKBLBet007(today);
                    break;
                case ESport.Basketball_WKBL:
                    if (gameSource == EGameSource.Official_Website)
                        result = new BkWKBL(today);
                    else if (gameSource == EGameSource.Bet007)
                        result = new BkWKBLBet007(today);
                    else
                        result = new BkWKBLBet007(today);
                    break;
                case ESport.Basketball_NBA:
                    result = new BkNBA(today);
                    break;
                case ESport.Basketball_WNBA:
                    result = new BkWNBA(today);
                    break;
                case ESport.Basketball_Euroleague:
                    if (gameSource == EGameSource.Asiascore)
                        result = new BkEuroleague(today);
                    else if (gameSource == EGameSource.Bet007)
                        result = new BkEuroleagueBet007(today);
                    else
                        result = new BkEuroleagueBet007(today);
                    break;
                case ESport.Basketball_Eurocup:
                    if (gameSource == EGameSource.Asiascore)
                        result = new BkEurocup(today);
                    else if (gameSource == EGameSource.Bet007)
                        result = new BkEurocupBet007(today);
                    else
                        result = new BkEurocupBet007(today);
                    break;
                case ESport.Basketball_VTB:
                    if (gameSource == EGameSource.Asiascore)
                        result = new BkVTB(today);
                    else if (gameSource == EGameSource.Bet007)
                        result = new BkVTBBet007(today);
                    else
                        result = new BkVTBBet007(today);
                    break;
                case ESport.Basketball_NBL:
                    if (gameSource == EGameSource.Asiascore)
                        result = new BkNBL(today);
                    else if (gameSource == EGameSource.Bet007)
                        result = new BkNBLBet007(today);
                    else
                        result = new BkNBLBet007(today);
                    break;
                case ESport.Basketball_FIBA:
                    result = new BkFIBA(today);
                    break;
                case ESport.Basketball_EBT:
                    result = new BkEBT(today);
                    break;
                case ESport.Basketball_ACB:
                    if (gameSource == EGameSource.Asiascore)
                        result = new BkACB(today);
                    else if (gameSource == EGameSource.Bet007)
                        result = new BkACBBet007(today);
                    else
                        result = new BkACBBet007(today);
                    break;
                case ESport.Basketball_BBL:
                    result = new BkBBL(today);
                    break;
                case ESport.Basketball_NCAA:
                    //result = new BkNCAABet007(today); 
                    result = new BkNCAA(today);//官網
                    break;
                case ESport.Basketball_CNBL:
                    result = new BkCNBLBet007(today);
                    break;

                // 籃球(奧訊)
                case ESport.Basketball_OS:
                    result = new BkOS(today);
                    break;

                // BF籃球
                case ESport.Basketball_BF:
                    result = new BKBF(today);
                    break;

                // 網球
                case ESport.Tennis:
                    result = new Tennis(today);
                    break;

                // 曲棍球
                case ESport.Hockey_NHL:
                    result = new IhNHL(today);
                    break;
                case ESport.Hockey_AHL:
                    result = new IhAHL(today);
                    break;
                case ESport.Hockey_KHL:
                    result = new IhKHL(today);
                    break;
                case ESport.Hockey_IHBF:
                    result = new IhBF(today);
                    break;
                case ESport.DaQiuData:
                    result = new DaqiuData(today);
                    break;
            }
            // 傳回
            return result;

        }

        /// <summary>
        /// 取得比賽遊戲類型代碼
        /// </summary>
        /// <param name="sport"></param>
        /// <returns></returns>
        private string GetSportGameType(ESport sport)
        {
            switch (sport)
            {
                // 足球
                case ESport.Football:
                    return string.Empty;
                case ESport.Football_NFL:
                    return string.Empty;
                // 棒球
                case ESport.Baseball_CPBL:
                case ESport.Baseball_CPBL2:
                    return string.Empty;
                case ESport.Baseball_NPB:
                case ESport.Baseball_NPB2:
                case ESport.Baseball_NPB3:
                case ESport.Baseball_TBS:
                    return string.Empty;
                case ESport.Baseball_KBO:
                case ESport.Baseball_KBO2:
                case ESport.Baseball_KBO3:
                    return string.Empty;
                case ESport.Baseball_MLB:
                case ESport.Baseball_MLB2:
                case ESport.Baseball_MLB3:
                    return string.Empty;
                case ESport.Baseball_IL:
                    return string.Empty;
                case ESport.Baseball_PCL:
                    return string.Empty;
                case ESport.Baseball_LMP:
                    return string.Empty;
                case ESport.Baseball_LMB:
                    return string.Empty;

                // 籃球
                case ESport.Basketball_CBA:
                    return "BKCN";
                case ESport.Basketball_BJ:
                    return string.Empty;
                case ESport.Basketball_KBL:
                    return "BKKR";
                case ESport.Basketball_WKBL:
                    return "BKKRW";
                case ESport.Basketball_NBA:
                    return string.Empty;
                case ESport.Basketball_WNBA:
                    return string.Empty;
                case ESport.Basketball_Euroleague:
                    return "BKEL";
                case ESport.Basketball_Eurocup:
                    return "BKEL2";
                case ESport.Basketball_VTB:
                    return "BKVTB";
                case ESport.Basketball_NBL:
                    return "BKAU";
                case ESport.Basketball_FIBA:
                    return string.Empty;
                case ESport.Basketball_EBT:
                    return string.Empty;
                case ESport.Basketball_ACB:
                    return "BKACB";
                case ESport.Basketball_BBL:
                    return string.Empty;
                case ESport.Basketball_NCAA:
                    return "BKNCAA";
                case ESport.Basketball_CNBL:
                    return "BKNBL";
                // 籃球 (奧訊)
                case ESport.Basketball_OS:
                    return "BKOS";
                //BF篮球
                case ESport.Basketball_BF:
                    return "BKBF";

                // 網球
                case ESport.Tennis:
                    return string.Empty;

                // 曲棍球
                case ESport.Hockey_NHL:
                    return string.Empty;
                case ESport.Hockey_AHL:
                    return string.Empty;
                case ESport.Hockey_KHL:
                    return string.Empty;

                default:
                    return string.Empty;
            }
        }

        #endregion
        #region ConnectionString
        //  測試連線資料庫
        private bool TestConnection()
        {
            bool result = false;
            SqlConnection conn = null;
            // 錯誤處理
            try
            {
                conn = new SqlConnection(ConnectionString);
                // 開啟
                conn.Open();
                // 關閉
                conn.Close();
                // 完成
                result = true;
            }
            catch { }
            conn = null;
            // 傳回
            return result;
        }
        // 連接字串
        public static string ConnectionString
        {
            get
            {
                return string.Format("Data Source={0};Initial Catalog={1};UID={2};PWD={3};Integrated Security=false;", new string[] { SqlServer, SqlDB, SqlUID, SqlPWD });
            }
        }
        public static string SqlServer { get; set; }
        public static string SqlDB { get; set; }
        public static string SqlUID { get; set; }
        public static string SqlPWD { get; set; }
        public static bool WRITE { get; set; }//是否寫入DB

        public static string bblmp_season { get; set; }

        public static string bkbj_user { get; set; }

        public static string bkbj_pwd { get; set; }
        public static int UseProxy { get; set; }
        #endregion
        #region Other

        /// <summary>
        /// 取得比賽來源名稱
        /// </summary>
        /// <param name="gameType"></param>
        /// <returns></returns>
        private EGameSource GetGameSourceName(string gameType)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(ConnectionString))
                {
                    string sqlStr = "SELECT ST.GameSource FROM [SourceSettings] SS WITH (NOLOCK) INNER JOIN [SourceType] ST WITH (NOLOCK) ON SS.SourceType = ST.SourceID WHERE SS.GameType=@GameType";

                    SqlCommand cmd = new SqlCommand();
                    cmd.CommandText = sqlStr;
                    cmd.CommandType = CommandType.Text;
                    cmd.Connection = conn;
                    cmd.Parameters.AddWithValue("@GameType", gameType);
                    cmd.Connection.Open();
                    object result = cmd.ExecuteScalar();
                    if (result != DBNull.Value && result != null)
                    {
                        switch (result.ToString())
                        {
                            case "Asiascore":
                                return EGameSource.Asiascore;
                            case "奧訊":
                                return EGameSource.Bet007;
                            case "官網":
                                return EGameSource.Official_Website;
                            default:
                                return EGameSource.None;
                        }
                    }
                    else
                        return EGameSource.None;
                }
            }
            catch (Exception)
            {
                return EGameSource.None;
            }
        }

        public static int GetGameSourceTime(string gameSource)
        {
            try
            {
                switch(gameSource)
                {
                    case "MexicoTime":
                    case "RussiaTime":
                    case "EasternTime":
                    case "JanpenTime":
                        break;//允許的條件

                    default:
                        return -1;
                }

                using (SqlConnection conn = new SqlConnection(ConnectionString))
                {
                    string sqlStr = "SELECT [val] FROM [dbo].[SetTypeVal] WITH (NOLOCK) WHERE [type]=@SourceType";

                    SqlCommand cmd = new SqlCommand();
                    cmd.CommandText = sqlStr;
                    cmd.CommandType = CommandType.Text;
                    cmd.Connection = conn;
                    cmd.Parameters.AddWithValue("@SourceType", gameSource);
                    cmd.Connection.Open();

                    object result = cmd.ExecuteScalar();
                    if (result != DBNull.Value && result != null)
                    {
                        string sDiffTime = result.ToString();
                        int iDiffTime = 0;
                        if (int.TryParse(sDiffTime, out iDiffTime))
                            return iDiffTime;
                    }
                }
            }
            catch { }

            return -1;
        }

        // 加入跟盤
        private void AddSport(ESport sport, ESport select)
        {
            // 加入
            int index = this.tspcbbSport.Items.Add(EnumHelper.GetDescription(sport));

            // 判斷選擇
            if (sport == select)
            {
                this.tspcbbSport.SelectedIndex = index;
            }
        }
        // 加入訊息
        private void AddInfo(string msg)
        {
            try
            {
                // 空白行
                if (msg == null || string.IsNullOrEmpty(msg.Trim()))
                    this.lsbInfo.Items.Add("");
                else
                {
                    // 加入
                    int index = this.lsbInfo.Items.Add(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " " + msg);
                    // 焦點
                    this.lsbInfo.SelectedIndex = index;

                    this.Logs.Info(msg);
                }
            }
            catch { }
        }
        private void AddInfo(char cLine = '-')
        {
            // 加入
            this.lsbInfo.Items.Add("".PadLeft(60, cLine));
        }

        #endregion
    }    

    //扩展方法必须在非泛型静态类中定义
    public static class ExtensionControl
    {
        //非同步委派更新UI
        public static void InvokeIfRequired(this Control control, MethodInvoker action)
        {
            if (control.InvokeRequired)//在非當前執行緒內 使用委派
            {
                control.Invoke(action);
            }
            else
            {
                action();
            }
        }
    }
}
