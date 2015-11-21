using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Follow.Sports.Basic
{
    public enum EGameSource {
    
        /// <summary>
        /// 無
        /// </summary>
        [Description("無")]
        None,

        /// <summary>
        /// Asiascore
        /// </summary>
        [Description("Asiascore")]
        Asiascore,

        /// <summary>
        /// 奧訊
        /// </summary>
        [Description("奧訊")]
        Bet007,

        /// <summary>
        /// 官網
        /// </summary>
        [Description("官網")]
        Official_Website
    }
}
