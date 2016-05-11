﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Security.Cryptography;

namespace LJC.FrameWork.Comm
{
    public static class StringHelper
    {
        private static Regex FilterSqlReg = new Regex(@"select |--|'|insert |delete |from |count \(|drop table |update |truncate |asc\(|mid\(|char\(|xp_cmdshell|exec master|netlocalgroup administrators|:|net user|""| or | and ",RegexOptions.IgnoreCase|RegexOptions.Multiline|RegexOptions.Compiled);
        private static Regex FilterDangerHtmlTag = new Regex(@"<\s{0}(?:html|body|meta|title|attribute|address|acronym|caption|head|bgsound|event|custom|embed|fieldset|comment|style|link|location|center|script|object|param|applet|iframe|frame|external|from|a|div|span|lable|legend|input|button|textarea|xml|select|option|pre|dl|li|ul|ol|em|history|base|basefont|applet|area|img|map|hr)\s{1,}[^>]*>", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);
        private static Regex FilterAllHtmlTag = new Regex(@"(?:\<.*?\>|\<\/.*?\>)", RegexOptions.Multiline | RegexOptions.Compiled);
        private static Regex IsWebUrlReg = new Regex(@"^(?:http|ftp|https):\/\/[\w\-_]+(?:\.[\w\-_]+)+(?:[\w\-\.,@?^=%&amp;:/~\+#]*[\w\-\@?^=%&amp;/~\+#])?$",RegexOptions.Singleline|RegexOptions.Compiled);
        private static Regex IsChinaPostCodeReg = new Regex(@"^[1-9]\d{5}(?!\d)$", RegexOptions.Singleline | RegexOptions.Compiled);
        private static Regex IsQQNumberReg = new Regex("^[1-9][0-9]{4,20}$", RegexOptions.Compiled | RegexOptions.Singleline);
        private static Regex IsChinaTelphoneReg = new Regex(@"^\d{3}-\d{8}|\d{4}-\d{7}$", RegexOptions.Singleline | RegexOptions.Compiled);
        private static Regex IsEmailReg = new Regex(@"^\w+(?:[-+.]\w+)*@\w+(?:[-.]\w+)*\.\w+(?:[-.]\w+)*$", RegexOptions.Compiled | RegexOptions.Singleline);
        private static Regex IsChinaIDReg = new Regex(@"^\d{15}|\d{18}$", RegexOptions.Singleline | RegexOptions.Compiled);
        private static Regex IsMobphoneReg = new Regex(@"^(?<national>\+?(?:86)?)(?<separator>\s?-?)(?<phone>(?<vender>(13|15|18)[0-9])(?<area>\d{4})(?<id>\d{4}))$", RegexOptions.Compiled | RegexOptions.Singleline);
        private static Regex IsChineseReg = new Regex(@"^[\u4e00-\u9fa5]{0,}$", RegexOptions.Multiline | RegexOptions.Compiled);


        /// <summary>
        /// 转全角的函数(SBC case)
        /// </summary>
        /// <param name="input">任意字符串</param>
        /// <returns>全角字符串</returns>
        ///<remarks>
        ///全角空格为12288，半角空格为32
        ///其他字符半角(33-126)与全角(65281-65374)的对应关系是：均相差65248
        ///</remarks>        
        public static string ToSBC(this string input)
        {
            //半角转全角：
            char[] c = input.ToCharArray();
            for (int i = 0; i < c.Length; i++)
            {
                if (c[i] == 32)
                {
                    c[i] = (char)12288;
                    continue;
                }
                if (c[i] < 127)
                    c[i] = (char)(c[i] + 65248);
            }
            return new string(c);
        }


        /// <summary>
        /// 转半角的函数(DBC case)
        /// </summary>
        /// <param name="input">任意字符串</param>
        /// <returns>半角字符串</returns>
        ///<remarks>
        ///全角空格为12288，半角空格为32
        ///其他字符半角(33-126)与全角(65281-65374)的对应关系是：均相差65248
        ///</remarks>
        public static string ToDBC(this string input)
        {
            char[] c = input.ToCharArray();
            for (int i = 0; i < c.Length; i++)
            {
                if (c[i] == 12288)
                {
                    c[i] = (char)32;
                    continue;
                }
                if (c[i] > 65280 && c[i] < 65375)
                    c[i] = (char)(c[i] - 65248);
            }
            return new string(c);
        }

        public static bool IsNumber(string val)
        {
            if (val == null || string.IsNullOrWhiteSpace(val))
                return false;
            int d;
            return int.TryParse(val, out d);
        }

        /// <summary>
        /// 去掉字符串的字符，不分大小写
        /// </summary>
        /// <param name="source"></param>
        /// <param name="trimBody"></param>
        /// <returns></returns>
        public static string TrimString(string source, string trimBody)
        {
            string tempStr = source;
            Regex rg = new Regex(trimBody, RegexOptions.IgnoreCase | RegexOptions.Multiline);
            tempStr = rg.Replace(tempStr, "");
            return tempStr;
        }

        /// <summary>
        /// 去掉string中的某些字符
        /// </summary>
        /// <param name="source"></param>
        /// <param name="trimBody"></param>
        /// <returns></returns>
        public static string TrimString(string source, params string[] trimBody)
        {
            string tempStr = source;
            for (int i = 0; i < trimBody.Length; i++)
            {
                tempStr = tempStr.Replace(trimBody[i], "");
            }
            return tempStr;
        }

        /// <summary>
        /// 替换字符，支持正则表达式
        /// </summary>
        /// <param name="source"></param>
        /// <param name="oldBody"></param>
        /// <param name="newBody"></param>
        /// <returns></returns>
        public static string ReplaceStr(string source, string oldBody, string newBody)
        {
            string tempStr = source;
            Regex rg = new Regex(oldBody, RegexOptions.Multiline | RegexOptions.IgnoreCase);
            tempStr = rg.Replace(tempStr, newBody);
            return tempStr;
        }

        public static string[] GetAllRegxString(string regxExpress, string source)
        {
            try
            {
                source = source.Replace("\\t", "");
                Regex rg = new Regex(regxExpress, RegexOptions.Multiline | RegexOptions.IgnoreCase);

                MatchCollection matchs = rg.Matches(source);
                string[] strResults = new string[matchs.Count];
                for (int i = 0; i < strResults.Length; i++)
                {
                    strResults[i] = matchs[i].Value;
                }
                return strResults;
            }
            catch
            {
                return null;

            }

        }

        /// <summary>
        /// ;
        /// </summary>
        /// <param name="input">源</param>
        /// <param name="regText">正则表达式,中文字符&代表内容</param>
        /// <param name="trim">是否去掉里面的标签</param>
        /// <returns></returns>
        public static string[] GetInnerText(string input, string regText, bool trim)
        {

            if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(regText))
            {
                return null;
            }

            /*
            regText = regText.Replace("?", "\\?");
            regText = regText.Replace("[", "\\[");
            regText = regText.Replace("]", "\\]");
            regText = regText.Replace("{", "\\{");
            regText = regText.Replace("}", "\\}");
            regText = regText.Replace("(", "\\(");
            regText = regText.Replace(")", "\\)");
            regText = regText.Replace("*", "\\*");
            regText = regText.Replace("+", "\\+");
            regText = regText.Replace("$", "\\$");
            regText = regText.Replace("^", "\\^");
             */

            regText = TrimString(regText, "\r", "\n");
            regText = ReplaceStr(regText, "[\\s]{1,}", "[\\s]{1,}");

            string[] subRegText = regText.Split(new string[] { "##" }, StringSplitOptions.RemoveEmptyEntries);

            string _regText = "";

            for (int i = 0; i < subRegText.Length; i++)
            {
                _regText += "(" + subRegText[i] + ")";
                if (i + 1 < subRegText.Length)
                    _regText += "((?!" + subRegText[i + 1] + ("|" + subRegText[i]) + ")[^\n])*";
            }
            /*
            for (int i = 0; i < subRegText.Length; i++)
            {
                _regText += subRegText[i];
                if (i + 1 < subRegText.Length)
                    _regText += "((?!" + subRegText[subRegText.Length-1] + ")[^~])*";
            }
            */
            //_regText = regText.Replace("&", "((?!"+subRegText[0]+")[^?])*");
            //_regText = regText.Replace(" ", "[\\s]{1,}");

            //_regText = _regText.Replace("##", "((?!##)[^~])*");



            string[] _result = GetAllRegxString(_regText, input);

            if (trim)
            {
                if (subRegText.Length < 2)
                    trim = false;
            }

            if (trim)
            {

                for (int k = 0; k < _result.Length; k++)
                {
                    for (int i = 0; i < subRegText.Length; i++)
                    {
                        //_result[k] = _result[k].Replace(subRegText[i], "");
                        _result[k] = ReplaceStr(_result[k], subRegText[i], " ");
                    }
                }
            }

            return _result;
        }

        public static string ChineseCap(string chineseStr)
        {
            if (string.IsNullOrWhiteSpace(chineseStr))
                return string.Empty;
            chineseStr = chineseStr.Replace(" ", "").ToDBC().ToUpper();
            byte[] ZW = new byte[2];
            long ChineseStr_int;
            string CharStr, Capstr = "", ChinaStr = "";
            for (int i = 0; i <= chineseStr.Length - 1; i++)
            {
                CharStr = chineseStr.Substring(i, 1).ToString();
                ZW = System.Text.Encoding.Default.GetBytes(CharStr);                // 得到汉字符的字节数组
                if (CharStr == "行")
                {
                    Capstr = Capstr + "h";
                }
                else if (CharStr == "藏")
                {
                    Capstr = Capstr + "z";
                }
                else if (ZW.Length == 2)
                {
                    int i1 = (short)(ZW[0]);
                    int i2 = (short)(ZW[1]);
                    ChineseStr_int = i1 * 256 + i2;                    //table of the constant list
                    // 'A'; //45217..45252                    
                    // 'B'; //45253..45760                    
                    // 'C'; //45761..46317                    
                    // 'D'; //46318..46825                    
                    // 'E'; //46826..47009                    
                    // 'F'; //47010..47296                   
                    // 'G'; //47297..47613                    
                    // 'H'; //47614..48118                    
                    // 'J'; //48119..49061                    
                    // 'K'; //49062..49323                    
                    // 'L'; //49324..49895                    
                    // 'M'; //49896..50370                    
                    // 'N'; //50371..50613                   
                    // 'O'; //50614..50621                    
                    // 'P'; //50622..50905                    
                    // 'Q'; //50906..51386                    
                    // 'R'; //51387..51445                    
                    // 'S'; //51446..52217                    
                    // 'T'; //52218..52697                   
                    //没有U,V                    
                    // 'W'; //52698..52979                    
                    // 'X'; //52980..53640                    
                    // 'Y'; //53689..54480                   
                    // 'Z'; //54481..55289                    
                    if ((ChineseStr_int >= 45217) && (ChineseStr_int <= 45252))
                    {
                        ChinaStr = "a";
                    }
                    else if ((ChineseStr_int >= 45253) && (ChineseStr_int <= 45760))
                    {
                        ChinaStr = "b";
                    }
                    else if ((ChineseStr_int >= 45761) && (ChineseStr_int <= 46317))
                    {
                        ChinaStr = "c";
                    }
                    else if ((ChineseStr_int >= 46318) && (ChineseStr_int <= 46825))
                    {
                        ChinaStr = "d";
                    }
                    else if ((ChineseStr_int >= 46826) && (ChineseStr_int <= 47009))
                    {
                        ChinaStr = "e";
                    }
                    else if ((ChineseStr_int >= 47010) && (ChineseStr_int <= 47296))
                    {
                        ChinaStr = "f";
                    }
                    else if ((ChineseStr_int >= 47297) && (ChineseStr_int <= 47613))
                    {
                        ChinaStr = "g";
                    }
                    else if ((ChineseStr_int >= 47614) && (ChineseStr_int <= 48118))
                    {
                        ChinaStr = "h";
                    }
                    else if ((ChineseStr_int >= 48119) && (ChineseStr_int <= 49061))
                    {
                        ChinaStr = "j";
                    }
                    else if ((ChineseStr_int >= 49062) && (ChineseStr_int <= 49323))
                    {
                        ChinaStr = "k";
                    }
                    else if ((ChineseStr_int >= 49324) && (ChineseStr_int <= 49895))
                    {
                        ChinaStr = "l";
                    }
                    else if ((ChineseStr_int >= 49896) && (ChineseStr_int <= 50370))
                    { ChinaStr = "m"; }
                    else if ((ChineseStr_int >= 50371) && (ChineseStr_int <= 50613))
                    { ChinaStr = "n"; }
                    else if ((ChineseStr_int >= 50614) && (ChineseStr_int <= 50621))
                    { ChinaStr = "o"; }
                    else if ((ChineseStr_int >= 50622) && (ChineseStr_int <= 50905))
                    { ChinaStr = "p"; }
                    else if ((ChineseStr_int >= 50906) && (ChineseStr_int <= 51386))
                    { ChinaStr = "q"; }
                    else if ((ChineseStr_int >= 51387) && (ChineseStr_int <= 51445))
                    { ChinaStr = "r"; }
                    else if ((ChineseStr_int >= 51446) && (ChineseStr_int <= 52217))
                    { ChinaStr = "s"; }
                    else if ((ChineseStr_int >= 52218) && (ChineseStr_int <= 52697))
                    { ChinaStr = "t"; }
                    else if ((ChineseStr_int >= 52698) && (ChineseStr_int <= 52979))
                    { ChinaStr = "w"; }
                    else if ((ChineseStr_int >= 52980) && (ChineseStr_int <= 53640))
                    { ChinaStr = "x"; }
                    else if ((ChineseStr_int >= 53689) && (ChineseStr_int <= 54480))
                    { ChinaStr = "y"; }
                    else if ((ChineseStr_int >= 54481) && (ChineseStr_int <= 55289))
                    { ChinaStr = "z"; }

                    Capstr = Capstr + ChinaStr;
                }
                else
                {
                    Capstr = Capstr + CharStr;
                }
                //Capstr = Capstr + ChinaStr;
            }
            return Capstr;
        }

        public static string HidString(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return "***";
            }

            if (s.Length <= 3)
                return "***";

            return "******" + s.LastN(3);
        }

        public static string Base64(string input,Encoding encode)
        {
            byte[] bts = encode.GetBytes(input);
            return Convert.ToBase64String(bts);
        }

        public static string FromBase64(string input,Encoding encode)
        {
            byte[] bts= Convert.FromBase64String(input);
            return encode.GetString(bts);
        }

        private static Regex macAddressRegex = new Regex("^(([0-9A-z]{2}-){5}|([0-9A-z]{2}:){5})[0-9A-z]{2}$");
        /// <summary>
        /// 判断是否是mac地址,是12位的十六进制数
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static bool IsMacAddress(string input)
        {
            return macAddressRegex.IsMatch(input);
        }

        [Obsolete("方法已经放到HashEncrypt里面")]
        /// <summary>
        /// SHA256函数
        /// </summary>
        /// <param name="str">原始字符串</param>
        /// <returns>SHA256结果(返回长度为44字节的字符串)</returns>
        public static string SHA256_JS(string str)
        {
            byte[] SHA256Data = Encoding.ASCII.GetBytes(str);
            SHA256Managed Sha256 = new SHA256Managed();
            byte[] Result = Sha256.ComputeHash(SHA256Data);
            //转换为hex
            Sha256.Clear();
            return BitConverter.ToString(Result).Replace("-", "").ToLower(); 
            //return Convert.ToBase64String(Result); //返回长度为44字节的字符串
        }

        [Obsolete("方法已经放到HashEncrypt里面")]
        /// <summary>
        /// SHA256函数
        /// </summary>
        /// <param name="str">原始字符串</param>
        /// <returns>SHA256结果(返回长度为44字节的字符串)</returns>
        public static string MD5_JS(string str)
        {
            byte[] MD5Data = Encoding.ASCII.GetBytes(str);
            byte[] tmpByte;
            MD5 md5 = new MD5CryptoServiceProvider();
            tmpByte = md5.ComputeHash(MD5Data);
            md5.Clear();
            return BitConverter.ToString(tmpByte).Replace("-", "").ToLower(); 
        }

        public static bool IsAscii(string input)
        {

            if (string.IsNullOrEmpty(input))
                return true;

            char[] array = input.ToArray();
            int len = array.Length;
            for (int i = 0; i < len; i++)
            {
                if (array[i] > 127)
                    return false;
            }

            return true;
        }

        public static bool IsAscii(char ch)
        {
            return ch <= 127;
        }

        /// <summary>
        /// 最字符串的后几位字符组成的子串
        /// </summary>
        /// <param name="input"></param>
        /// <param name="n"></param>
        /// <returns></returns>
        public static string LastN(string input, int n)
        {
            if (n <= 0)
                return "";

            if (n >= input.Length)
                return input;

            return input.Substring(input.Length - n, n);
        }

        /// <summary>
        /// sql字符串过滤
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string FilterSql(string input)
        {
            return FilterSqlReg.Replace(input, string.Empty);
        }

        /// <summary>
        /// 去掉html字符串
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string TrimAllHtmlTag(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            return FilterAllHtmlTag.Replace(input, string.Empty);
        }

        /// <summary>
        /// 去掉危险的html标签
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string TrimDangerHtmlTag(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            return FilterDangerHtmlTag.Replace(input, string.Empty);
        }

        /// <summary>
        /// 除掉外部引用的链接
        /// </summary>
        /// <param name="input">输入正文</param>
        /// <param name="domain">受保护的域名，多个之间用|号分隔</param>
        /// <returns></returns>
        public static string TrimOutLink(string input, string domain)
        {
            if (string.IsNullOrEmpty(input)
                ||string.IsNullOrEmpty(domain))
                return input;

            Regex reg = new Regex(@"https?://(?!"+domain+@")[\w\.\\/\-\d]+");

            return reg.Replace(input, string.Empty);
        }

        /// <summary>
        /// 是否是网址
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static bool IsWebUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
                return false;

            return IsWebUrlReg.IsMatch(url);
        }

        /// <summary>
        /// 是否是中国邮政编码
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static bool IsChinaPostCode(string input)
        {
            if (string.IsNullOrEmpty(input))
                return false;

            return IsChinaPostCodeReg.IsMatch(input);
        }

        /// <summary>
        /// 是否是QQ号码
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static bool IsQQNumber(string input)
        {
            if (string.IsNullOrEmpty(input))
                return false;

            
            return IsQQNumberReg.IsMatch(input);
        }

        /// <summary>
        /// 是否是中国电话号码
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static bool IsChinaTelphone(string input)
        {
            if (string.IsNullOrEmpty(input))
                return false;

            return IsChinaTelphoneReg.IsMatch(input);
        }

        /// <summary>
        /// 是否是电子邮件
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static bool IsEmail(string input)
        {
            if (string.IsNullOrEmpty(input))
                return false;

            return IsEmailReg.IsMatch(input);
        }

        /// <summary>
        /// 是否是中国身份证
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static bool IsChinaIDCard(string input)
        {
            if (string.IsNullOrEmpty(input))
                return false;

            return IsChinaIDReg.IsMatch(input);
        }

        /// <summary>
        /// 是否是手机号码
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static bool IsMobphone(string input)
        {
            if (string.IsNullOrEmpty(input))
                return false;

            return IsMobphoneReg.IsMatch(input);
        }

        /// <summary>
        /// 是否是汉字
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static bool IsChinese(string input)
        {
            if (string.IsNullOrEmpty(input))
                return false;

            return IsChineseReg.IsMatch(input);
        }

        public static bool IsChinese(char ch)
        {
            return ch >= '\u4e00' && ch <= '\u9fa5';
        }

        /// <summary>
        /// 是否是有效的帐号输入，只包含有汉字英语数字和下线
        /// </summary>
        /// <returns></returns>
        public static bool IsUserAccount(string input,int minLen=1,int maxLen=20)
        {
            if (string.IsNullOrEmpty(input))
                return false;

            Regex rg = new Regex(string.Format(@"^[\u4E00-\u9FA5A-Za-z0-9_]{{{0},{1}}}$",minLen,maxLen), RegexOptions.Compiled);

            return rg.IsMatch(input);
        }

        /// <summary>
        /// 是否是密码
        /// </summary>
        /// <param name="input">输入</param>
        /// <param name="minlen">最小长度</param>
        /// <param name="maxlen">最大长度</param>
        /// <returns></returns>
        public static bool IsPassword(string input,int minlen=4,int maxlen=20)
        {
            if (string.IsNullOrEmpty(input))
                return false;
            Regex isPwdReg = new Regex(string.Format(@"^[a-zA-Z]\w{{{0},{1}}}$",minlen,maxlen));
            return isPwdReg.IsMatch(input);
        }

        /// <summary>
        /// 是否是强密码
        /// </summary>
        /// <param name="input">输入</param>
        /// <param name="minlen">最小长度</param>
        /// <param name="maxlen">最大长度</param>
        /// <returns></returns>
        public static bool IsStrongPassword(string input, int minlen = 4, int maxlen = 20)
        {
            if (string.IsNullOrEmpty(input))
                return false;

            Regex isStrongPwdReg = new Regex(string.Format(@"^(?=.*\d)(?=.*[a-z])(?=.*[A-Z]).{{{0},{1}}}$",minlen,maxlen));
            return isStrongPwdReg.IsMatch(input);
        }
    }
}
