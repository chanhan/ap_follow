using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;

namespace Follow.Sports.Basic
{
    /// <summary>
    /// 運動類型。
    /// </summary>
    public enum ESport
    {
        /// <summary>
        /// 無。
        /// </summary>
        [DescriptionAttribute("無")]
        None = 0,

        /// <summary>
        /// 足球。
        /// </summary>
        [DescriptionAttribute("足球 (Football)")]
        Football,
        /// <summary>
        /// 足球 國家美式足球聯盟 (NFL)。
        /// </summary>
        [DescriptionAttribute("足球 國家美式足球聯盟 (NFL)")]
        Football_NFL,

        /// <summary>
        /// 棒球 中華職棒 (CPBL)。
        /// </summary>
        [DescriptionAttribute("棒球 中華職棒 (CPBL)")]
        Baseball_CPBL,

        /// <summary>
        /// 棒球 中華職棒 (CPBL)。
        /// </summary>
        [DescriptionAttribute("棒球 中華職棒 (CPBL-PlaySport)")]
        Baseball_CPBL2,

        /// <summary>
        /// 棒球 日本職棒 (NPB)。
        /// </summary>
        [DescriptionAttribute("棒球 日本職棒 (NPB-YAHOO)")]
        Baseball_NPB,
        /// <summary>
        /// 棒球 日本職棒 (NPB)。
        /// </summary>
        [DescriptionAttribute("棒球 日本職棒  (NPB-PlaySport)")]
        Baseball_NPB2,
        /// <summary>
        /// 棒球 日本職棒 (NPB)。
        /// </summary>
        [DescriptionAttribute("棒球 日本職棒  (NPB-TSLC)")]
        Baseball_NPB3,
        /// <summary>
        /// 棒球 日本職棒 (NPB)。
        /// </summary>
        [DescriptionAttribute("棒球 日本職棒  (NPB-TBS)")]
        Baseball_TBS,
        /// <summary>
        /// 棒球 韓國職棒 (KBO)。
        /// </summary>
        [DescriptionAttribute("棒球 韓國職棒 (KBO-NAVER)")]
        Baseball_KBO,
        /// <summary>
        /// 棒球 韓國職棒 (KBO)。
        /// </summary>
        [DescriptionAttribute("棒球 韓國職棒 (KBO-PlaySport)")]
        Baseball_KBO2,
        /// <summary>
        /// 棒球 韓國職棒 (KBO)。
        /// </summary>
        [DescriptionAttribute("棒球 韓國職棒 (KBO-Daum)")]
        Baseball_KBO3,
        /// <summary>
        /// 棒球 美國職棒 (MLB)。
        /// </summary>
        [DescriptionAttribute("棒球 美國職棒 (MLB-CBS)")]
        Baseball_MLB,
        /// <summary>
        /// 棒球 美國職棒 (MLB)。
        /// </summary>
        [DescriptionAttribute("棒球 美國職棒 (MLB-ESPN)")]
        Baseball_MLB2,
        /// <summary>
        /// 棒球 美國職棒 (MLB)。
        /// </summary>
        [DescriptionAttribute("棒球 美國職棒 (MLB-PlaySport)")]
        Baseball_MLB3,
        /// <summary>
        /// 棒球 美國職棒 3A 國際聯盟 (IL)。
        /// </summary>
        [DescriptionAttribute("棒球 美國職棒 3A 國際聯盟 (IL)")]
        Baseball_IL,
        /// <summary>
        /// 棒球 美國職棒 3A 太平洋岸聯盟 (PCL)。
        /// </summary>
        [DescriptionAttribute("棒球 美國職棒 3A 太平洋岸聯盟 (PCL)")]
        Baseball_PCL,
        /// <summary>
        /// 棒球 墨西哥冬季聯盟 (LMP)。
        /// </summary>
        [DescriptionAttribute("棒球 墨西哥冬季聯盟 (LMP)")]
        Baseball_LMP,
        /// <summary>
        /// 棒球 墨西哥夏季聯盟 (LMB)。
        /// </summary>
        [DescriptionAttribute("棒球 墨西哥夏季聯盟 (LMB)")]
        Baseball_LMB,
        /// <summary>
        /// 棒球 澳洲棒球 (ABL)。
        /// </summary>
        [DescriptionAttribute("棒球 澳洲棒球 (ABL)")]
        Baseball_ABL,
        /// <summary>
        /// 棒球 荷蘭棒球 (HB)。
        /// </summary>
        [DescriptionAttribute("棒球 荷蘭棒球 (HB)")]
        Baseball_HB,
        ///// <summary>
        ///// 籃球 中華職籃 (SBL)。
        ///// </summary>
        //[DescriptionAttribute("籃球 中華職籃 (SBL)")]
        //Basketball_SBL,
        /// <summary>
        /// 籃球 中國職籃 (CBA)。
        /// </summary>
        [DescriptionAttribute("籃球 中國職籃 (CBA)")]
        Basketball_CBA,
        /// <summary>
        /// 籃球 日本職籃 (BJ)。
        /// </summary>
        [DescriptionAttribute("籃球 日本職籃 (BJ)")]
        Basketball_BJ,
        /// <summary>
        /// 籃球 韓國職籃 - 男子 (KBL)。
        /// </summary>
        [DescriptionAttribute("籃球 韓國職籃 - 男子 (KBL)")]
        Basketball_KBL,
        /// <summary>
        /// 籃球 韓國職籃 - 女子 (WKBL)。
        /// </summary>
        [DescriptionAttribute("籃球 韓國職籃 - 女子 (WKBL)")]
        Basketball_WKBL,
        /// <summary>
        /// 籃球 美國職籃 (NBA)。
        /// </summary>
        [DescriptionAttribute("籃球 美國職籃 (NBA)")]
        Basketball_NBA,
        /// <summary>
        /// 籃球 美國女子職籃 (WNBA)。
        /// </summary>
        [DescriptionAttribute("籃球 美國女子職籃 (WNBA)")]
        Basketball_WNBA,
        /// <summary>
        /// 籃球 歐洲職籃 (Euroleague)。
        /// </summary>
        [DescriptionAttribute("籃球 歐洲職籃 (Euroleague)")]
        Basketball_Euroleague,
        /// <summary>
        /// 籃球 歐洲籃球聯盟盃 (Eurocup)。
        /// </summary>
        [DescriptionAttribute("籃球 歐洲籃球聯盟盃 (Eurocup)")]
        Basketball_Eurocup,
        /// <summary>
        /// 籃球 籃球聯賽 (VTB)。
        /// </summary>
        [DescriptionAttribute("籃球 籃球聯賽 (VTB)")]
        Basketball_VTB,
        /// <summary>
        /// 籃球 澳洲職籃 (NBL)。
        /// </summary>
        [DescriptionAttribute("籃球 澳洲職籃 (NBL)")]
        Basketball_NBL,
        /// <summary>
        /// 籃球 亞洲籃球錦標賽 (FIBA)。
        /// </summary>
        [DescriptionAttribute("籃球 亞洲籃球錦標賽 (FIBA)")]
        Basketball_FIBA,
        /// <summary>
        /// 籃球 歐洲籃球錦標賽 (EBT)。
        /// </summary>
        [DescriptionAttribute("籃球 歐洲籃球錦標賽 (EBT)")]
        Basketball_EBT,
        /// <summary>
        /// 籃球 西班牙籃球 (ACB)。
        /// </summary>
        [DescriptionAttribute("籃球 西班牙籃球 (ACB)")]
        Basketball_ACB,

        /// <summary>
        /// 籃球 德國籃球甲級聯賽 (BBL)。
        /// </summary>
        [DescriptionAttribute("籃球 德國籃球甲級聯賽 (BBL)")]
        Basketball_BBL,

        /// <summary>
        /// 籃球 美國大學男子籃球 (NCAA)。
        /// </summary>
        [DescriptionAttribute("籃球 美國大學男子籃球 (NCAA)")]
        Basketball_NCAA,

        /// <summary>
        /// 籃球 中國男子籃球甲級聯賽 (NBL)。
        /// </summary>
        [DescriptionAttribute("籃球 中國男子籃球甲級聯賽 (CNBL)")]
        Basketball_CNBL,

        [DescriptionAttribute("籃球 奧訊 (BKOS)")]
        Basketball_OS,

        [DescriptionAttribute("籃球 BF (BKBF)")]
        Basketball_BF,
        /// <summary>
        /// 網球。
        /// </summary>
        [DescriptionAttribute("網球 (Tennis)")]
        Tennis,

        /// <summary>
        /// 曲棍球 國家冰球 (NHL)。
        /// </summary>
        [DescriptionAttribute("曲棍球 國家冰球 (NHL)")]
        Hockey_NHL,
        /// <summary>
        /// 曲棍球 美國冰球 (AHL)。
        /// </summary>
        [DescriptionAttribute("曲棍球 美國冰球 (AHL)")]
        Hockey_AHL,
        /// <summary>
        /// 曲棍球 俄羅斯冰球 (KHL)。
        /// </summary>
        [DescriptionAttribute("曲棍球 俄羅斯冰球 (KHL)")]
        Hockey_KHL,
        /// <summary>
        /// BF冰球
        /// </summary>
        [DescriptionAttribute("冰球 BF (IHBF)")]
        Hockey_IHBF,
        /// <summary>
        /// 大球盤口， 大球足球角球比分 數據
        /// </summary>
        [DescriptionAttribute("大球盤口角球比分 (PKJQ)")]
        DaQiuData,
    }
}
