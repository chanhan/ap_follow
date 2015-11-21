using Follow.Sports.Basic;
using System;
using System.Collections.Generic;
using System.Text;
using System.Web;

// 跟盤以 ID 為依據
// 跟盤日期以台灣時間顯示

namespace Follow.Sports
{
    public class IhNHL : Basic.BasicIceHockey
    {
        public IhNHL(DateTime today) : base(ESport.Hockey_NHL)
        {
            // 設定
            this.AllianceID = 1;
            this.GameType = "IHUS";
            this.GameDate = GetUtcTw(today).Date; // 只取日期
            this.DownHome = new BasicDownload(this.Sport, @"http://d.asiascore.com/x/feed/f_4_0_8_asia_1");
            this.DownHomeHeader = new Dictionary<string, string>();
            this.DownHomeHeader["Accept"] = "*/*";
            this.DownHomeHeader["Accept-Charset"] = "utf-8;q=0.7,*;q=0.3";
            this.DownHomeHeader["Accept-Encoding"] = "gzip,deflate,sdch";
            this.DownHomeHeader["Accept-Language"] = "*";
            this.DownHomeHeader["X-Fsign"] = "SW9D1eZo";
            this.DownHomeHeader["X-GeoIP"] = "1";
            this.DownHomeHeader["X-utime"] = "1";
            this.DownHomeHeader["Cookie"] = "__utma=190588603.635099785.1357704170.1360998879.1361003436.71; __utmb=190588603.1.10.1361003436; __utmc=190588603; __utmz=190588603.1361003436.71.19.utmcsr=asiascore.com|utmccn=(referral)|utmcmd=referral|utmcct=/";
            this.DownHomeHeader["Host"] = "d.asiascore.com";
            this.DownHomeHeader["Referer"] = "http://d.asiascore.com/x/feed/proxy";
            //this.DownRealHeader["User-Agent"] = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.17 (KHTML, like Gecko) Chrome/24.0.1312.57 Safari/537.17";
            //this.DownRealHeader["Connection"] = "keep-alive";
        }
        public override void Download()
        {
            // 讀取首頁資料。
            this.DownHome.DownloadData(this.DownHomeHeader);
            this.DownLastTime = DateTime.Now;
        }
        public override int Follow()
        {
            // 沒有資料就離開
            if (string.IsNullOrEmpty(this.DownHome.Data)) return 0;

            int result = 0;
            // 取得資料
            Dictionary<string, BasicInfo> gameData = this.GetDataByAsiaScore(this.DownHome.Data, "usa", " nhl", 20);
            // 判斷資料
            if (gameData != null && gameData.Count > 0)
            {
                // 資料
                foreach (KeyValuePair<string, BasicInfo> data in gameData)
                {
                    // 隊伍交換
                    data.Value.AcH = true;
                    // 比賽狀態
                    if (data.Value.GameStates == "S")
                    {
                        int num = 0;
                        // 加時
                        // 2014/04/18: NHL季後賽: 不限次數的20分鐘5對5加時賽
                        if (data.Value.Quarter >= 4)
                        {
                            if (!string.IsNullOrEmpty(data.Value.Status) &&
                                int.TryParse(data.Value.Status,out num))
                            {
                                num = 20 - num;                             // 還原時間

                                // 2014/04/18 判斷季後賽 (play offs), 時間設為 20 min, 其餘賽事仍為 5 min
                                //data.Value.Status = (5 - num).ToString();   // 經過時間
                                int time = (data.Value.AllianceName.ToLower().Contains("play offs")) ? 20 : 5;
                                data.Value.Status = (time - num).ToString();   // 經過時間
                            }
                        }
                    }
                    // 加入
                    this.GameData[data.Key] = data.Value;
                }
                result = gameData.Count;
            }
            // 傳回
            return result;
        }
        // 下載資料
        private BasicDownload DownHome;
        private DateTime DownLastTime = DateTime.Now;
        private Dictionary<string, string> DownHomeHeader;
    }
}
