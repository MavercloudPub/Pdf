﻿using Mavercloud.PDF;
using System;
using System.Collections.Generic;
using System.Text;

namespace Mavercloud.PDF.Helpers {
    /// <summary>
    /// 字符串操作 - 工具
    /// </summary>
    public partial class String {

        #region Join(将集合连接为带分隔符的字符串)

        /// <summary>
        /// 将集合连接为带分隔符的字符串
        /// </summary>
        /// <typeparam name="T">集合元素类型</typeparam>
        /// <param name="list">集合</param>
        /// <param name="quotes">引号，默认不带引号，范例：单引号 "'"</param>
        /// <param name="separator">分隔符，默认使用逗号分隔</param>
        public static string Join<T>( IEnumerable<T> list, string quotes = "", string separator = ",", bool removeEmpty = false ) {
            if (list == null)
            {
                return string.Empty;
            }
            else
            {
                var result = new StringBuilder();
                foreach (var each in list)
                {
                    if (!removeEmpty || !string.IsNullOrEmpty(each.SafeString()))
                    {
                        result.AppendFormat("{0}{1}{0}{2}", quotes, each, separator);
                    }
                }
                if (string.IsNullOrEmpty(separator))
                {
                    return result.ToString();
                }
                else
                {
                    return result.ToString().TrimEnd(separator.ToCharArray());
                }
            }
        }

        #endregion

        #region PinYin(获取汉字的拼音简码)

        /// <summary>
        /// 获取汉字的拼音简码，即首字母缩写,范例：中国,返回zg
        /// </summary>
        /// <param name="chineseText">汉字文本,范例： 中国</param>
        public static string PinYin( string chineseText ) {
            if( string.IsNullOrWhiteSpace( chineseText ) )
                return string.Empty;
            var result = new StringBuilder();
            foreach( char text in chineseText )
                result.AppendFormat( "{0}", ResolvePinYin( text ) );
            return result.ToString().ToLower();
        }

        /// <summary>
        /// 解析单个汉字的拼音简码
        /// </summary>
        private static string ResolvePinYin( char text ) {
            byte[] charBytes = Encoding.UTF8.GetBytes( text.ToString() );
            if( charBytes[0] <= 127 )
                return text.ToString();
            var unicode = (ushort)( charBytes[0] * 256 + charBytes[1] );
            string pinYin = ResolveByCode( unicode );
            if( !string.IsNullOrWhiteSpace( pinYin ) )
                return pinYin;
            return ResolveByConst( text.ToString() );
        }

        /// <summary>
        /// 使用字符编码方式获取拼音简码
        /// </summary>
        private static string ResolveByCode( ushort unicode ) {
            if( unicode >= '\uB0A1' && unicode <= '\uB0C4' )
                return "A";
            if( unicode >= '\uB0C5' && unicode <= '\uB2C0' && unicode != 45464 )
                return "B";
            if( unicode >= '\uB2C1' && unicode <= '\uB4ED' )
                return "C";
            if( unicode >= '\uB4EE' && unicode <= '\uB6E9' )
                return "D";
            if( unicode >= '\uB6EA' && unicode <= '\uB7A1' )
                return "E";
            if( unicode >= '\uB7A2' && unicode <= '\uB8C0' )
                return "F";
            if( unicode >= '\uB8C1' && unicode <= '\uB9FD' )
                return "G";
            if( unicode >= '\uB9FE' && unicode <= '\uBBF6' )
                return "H";
            if( unicode >= '\uBBF7' && unicode <= '\uBFA5' )
                return "J";
            if( unicode >= '\uBFA6' && unicode <= '\uC0AB' )
                return "K";
            if( unicode >= '\uC0AC' && unicode <= '\uC2E7' )
                return "L";
            if( unicode >= '\uC2E8' && unicode <= '\uC4C2' )
                return "M";
            if( unicode >= '\uC4C3' && unicode <= '\uC5B5' )
                return "N";
            if( unicode >= '\uC5B6' && unicode <= '\uC5BD' )
                return "O";
            if( unicode >= '\uC5BE' && unicode <= '\uC6D9' )
                return "P";
            if( unicode >= '\uC6DA' && unicode <= '\uC8BA' )
                return "Q";
            if( unicode >= '\uC8BB' && unicode <= '\uC8F5' )
                return "R";
            if( unicode >= '\uC8F6' && unicode <= '\uCBF9' )
                return "S";
            if( unicode >= '\uCBFA' && unicode <= '\uCDD9' )
                return "T";
            if( unicode >= '\uCDDA' && unicode <= '\uCEF3' )
                return "W";
            if( unicode >= '\uCEF4' && unicode <= '\uD188' )
                return "X";
            if( unicode >= '\uD1B9' && unicode <= '\uD4D0' )
                return "Y";
            if( unicode >= '\uD4D1' && unicode <= '\uD7F9' )
                return "Z";
            return string.Empty;
        }

        /// <summary>
        /// 通过拼音简码常量获取
        /// </summary>
        private static string ResolveByConst( string text ) {
            int index = Const.ChinesePinYin.IndexOf( text, StringComparison.Ordinal );
            if( index < 0 )
                return string.Empty;
            return Const.ChinesePinYin.Substring( index + 1, 1 );
        }

        public static string GetRandomNumbers(int numberCount)
        {
            StringBuilder builder = new StringBuilder();
            System.Random random = new System.Random(Guid.NewGuid().GetHashCode());
            for (int i = 1; i <= numberCount; i++)
            {
                int number = random.Next(10);
                builder.Append(number);
            }
            return builder.ToString();
        }

        public static string ReplaceFirst(string text, string search, string replace, string ignoreText = null)
        {
            if (!string.IsNullOrEmpty(ignoreText))
            {
                text = text.Replace(ignoreText, "*********||||||||||&&&&&&&&");
            }
            int pos = text.IndexOf(search);
            if (pos < 0)
            {
                return text;
            }
            text = text.Substring(0, pos) + replace + text.Substring(pos + search.Length);
            if (!string.IsNullOrEmpty(ignoreText))
            {
                text = text.Replace("*********||||||||||&&&&&&&&", ignoreText);
            }
            return text;
        }

        public static string ReplaceLast(string text, string search, string replace)
        {
            int pos = text.LastIndexOf(search);
            if (pos < 0)
            {
                return text;
            }
            else
            {
                return text.Substring(0, pos) + replace + text.Substring(pos + search.Length);
            }
        }

        #endregion
    }
}
