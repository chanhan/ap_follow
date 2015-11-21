using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Follow.Sports.Basic
{
    /// <summary>
    /// 列舉的操作。
    /// </summary>
    public static class EnumHelper
    {
        /// <summary>
        /// 傳回列舉的說明。
        /// </summary>
        /// <param name="value">列舉</param>
        /// <returns>傳回列舉的說明。</returns>
        public static string GetDescription(Enum value)
        {
            if (value == null) return null;

            string result = null;
            string description = value.ToString();
            FieldInfo fieldInfo = value.GetType().GetField(description);
            DescriptionAttribute[] attributes = (DescriptionAttribute[])fieldInfo.GetCustomAttributes(typeof(DescriptionAttribute), false);
            // 判斷資料
            if (attributes != null &&
                attributes.Length > 0)
            {
                result = attributes[0].Description;
            }
            // 傳回
            return result;
        }
    }
}
