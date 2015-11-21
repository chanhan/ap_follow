using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;

namespace Follow.Sports.Basic
{
    public class LogFile
    {
        private static Logger log = LogManager.GetLogger("Follow");
        private static Logger logDown = LogManager.GetLogger("Follow_down");
        private static Logger logUpdate = LogManager.GetLogger("Follow_update");
        private static ESport sport;
        private string FileType;

        public LogFile(ESport? tmp = null, string fileType = null)
        {
            if(tmp != null)
                sport = (ESport)tmp;

            if (!string.IsNullOrEmpty(fileType))
                FileType = fileType;
            
            if(string.IsNullOrEmpty(FileType))
                FileType = "mainfile";
        }

        public void Error(string text, params object[] args)
        {
            message("_error", text, args);
        }

        public void Proxy(string text, params object[] args)
        {
            message("_proxy", text, args);
        }

        public void Update(string text, params object[] args)
        {
            message("_update", text, args);
        }

        public void UpdateJson(string text, params object[] args)
        {
            message("_update_json", text, args);
        }

        public void DownloadBrowser(string text, params object[] args)
        {
            message("_downBrowser", text, args);
        }

        public void Download(string text, params object[] args)
        {
            message("_down", text, args);
        }
        public void DownGameInfo(string text, params object[] args)
        {
            message("_downgame_info", text, args);
        }

        public void GameInfo(string text, params object[] args)
        {
            message("_gameinfo", text, args);
        }

        public void Info(string text, params object[] args)
        {
            message("", text, args);
        }

        private void message(string type, string text, params object[] args)
        {
            try
            {
                text = (args.Length == 0) ? text : string.Format(text, args);

                LogEventInfo evt = new LogEventInfo(LogLevel.Info, "", text);
                evt.Level = (type == "_error") ? LogLevel.Error : LogLevel.Info;
                evt.Properties["Sport"] = sport;
                evt.Properties["LogType"] = type;
                evt.Properties["FileType"] = FileType;
                if (type == "_down")//資料下載使用不同的log處理
                {
                    //檔案名稱不能有 ':'字元
                    evt.Properties["LogTime"] = string.Format("_{0}'{1}", DateTime.Now.Hour.ToString(), DateTime.Now.Minute.ToString());
                    logDown.Log(evt);
                }
                else if (type == "_update")
                {
                    evt.Properties["FileType"] = "update";
                    evt.Properties["LogTime"] = string.Format("_{0}", DateTime.Now.Hour.ToString());
                    logUpdate.Log(evt);
                }
                else if (type == "_update_json")
                {
                    evt.Properties["FileType"] = "update_json";
                    evt.Properties["LogTime"] = string.Format("_{0}", DateTime.Now.Hour.ToString());
                    logUpdate.Log(evt);
                }
                else if (type == "_gameinfo")
                {
                    evt.Properties["FileType"] = "game_info";
                    evt.Properties["LogTime"] = string.Format("_{0}'{1}", DateTime.Now.Hour.ToString(), DateTime.Now.Minute.ToString());
                    logDown.Log(evt);
                }
                else if (type == "_downgame_info")
                {
                    evt.Properties["FileType"] = "downgame_info";
                    evt.Properties["LogTime"] = string.Format("_{0}'{1}", DateTime.Now.Hour.ToString(), DateTime.Now.Minute.ToString());
                    logDown.Log(evt);
                }
                else
                    log.Log(evt);
            }
            catch { }
        }
    
    }
}
