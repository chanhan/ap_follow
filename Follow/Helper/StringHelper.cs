using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Follow.Helper
{
    public class StringHelper
    {
        public static string IsNullOrEmptyToZero(string number)
        {
            if (string.IsNullOrEmpty(number))
                return "0";
            return number;
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
    }
}
