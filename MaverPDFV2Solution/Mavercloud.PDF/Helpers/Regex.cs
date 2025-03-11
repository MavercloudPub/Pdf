using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Mavercloud.PDF.Helpers
{
    /// <summary>
    /// 正则操作
    /// </summary>
    public static class Regex {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="text"></param>
        /// <param name="match"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static List<string> GetValues(string text, string match, RegexOptions options = RegexOptions.IgnoreCase)
        {
            List<string> matchList = new List<string>();
            if (!string.IsNullOrEmpty(text))
            {
                System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex(match, options);
                //find items that matches with our pattern
                MatchCollection matches = regex.Matches(text);

                if (matches != null && matches.Count > 0)
                {
                    foreach (Match matchValue in matches)
                    {
                        matchList.Add(matchValue.Value);
                    }
                }
            }
            return matchList;
        }
        /// <summary>
        /// 获取匹配值集合
        /// </summary>
        /// <param name="input">输入字符串</param>
        /// <param name="pattern">模式字符串</param>
        /// <param name="resultPatterns">结果模式字符串数组,范例：new[]{"$1","$2"}</param>
        /// <param name="options">选项</param>
        public static Dictionary<string, string> GetValues( string input, string pattern, string[] resultPatterns, RegexOptions options = RegexOptions.IgnoreCase ) {
            var result = new Dictionary<string, string>();
            if( string.IsNullOrWhiteSpace( input ) )
                return result;
            var match = System.Text.RegularExpressions.Regex.Match( input, pattern, options );
            if( match.Success == false )
                return result;
            AddResults( result, match, resultPatterns );
            return result;
        }

        /// <summary>
        /// 添加匹配结果
        /// </summary>
        private static void AddResults( Dictionary<string, string> result, Match match, string[] resultPatterns ) {
            if( resultPatterns == null ) {
                result.Add( string.Empty, match.Value );
                return;
            }
            foreach( var resultPattern in resultPatterns )
                result.Add( resultPattern, match.Result( resultPattern ) );
        }

        /// <summary>
        /// 获取匹配值
        /// </summary>
        /// <param name="input">输入字符串</param>
        /// <param name="pattern">模式字符串</param>
        /// <param name="resultPattern">结果模式字符串,范例："$1"用来获取第一个()内的值</param>
        /// <param name="options">选项</param>
        public static string GetValue( string input, string pattern, string resultPattern = "", RegexOptions options = RegexOptions.IgnoreCase ) {
            if( string.IsNullOrWhiteSpace( input ) )
                return string.Empty;
            var match = System.Text.RegularExpressions.Regex.Match( input, pattern, options );
            if( match.Success == false )
                return string.Empty;
            return string.IsNullOrWhiteSpace( resultPattern ) ? match.Value : match.Result( resultPattern );
        }

        /// <summary>
        /// 分割成字符串数组
        /// </summary>
        /// <param name="input">输入字符串</param>
        /// <param name="pattern">模式字符串</param>
        /// <param name="options">选项</param>
        public static string[] Split( string input, string pattern, RegexOptions options = RegexOptions.IgnoreCase ) {
            if( string.IsNullOrWhiteSpace( input ) )
                return new string[]{};
            return System.Text.RegularExpressions.Regex.Split( input, pattern, options );
        }

        /// <summary>
        /// 替换
        /// </summary>
        /// <param name="input">输入字符串</param>
        /// <param name="pattern">模式字符串</param>
        /// <param name="replacement">替换字符串</param>
        /// <param name="options">选项</param>
        public static string Replace( string input, string pattern,string replacement, RegexOptions options = RegexOptions.IgnoreCase ) {
            if( string.IsNullOrWhiteSpace( input ) )
                return string.Empty;
            return System.Text.RegularExpressions.Regex.Replace( input, pattern, replacement, options );
        }

        public static string GetFirstEmailFromText(string text)
        {
            string email = null;
            if (!string.IsNullOrEmpty(text))
            {
                System.Text.RegularExpressions.Regex emailRegex = new System.Text.RegularExpressions.Regex(@"\w+([-+.]\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*",
                RegexOptions.IgnoreCase);
                //find items that matches with our pattern
                MatchCollection emailMatches = emailRegex.Matches(text);

                if (emailMatches != null && emailMatches.Count > 0)
                {
                    email = emailMatches[0].Value;
                }
            }
            return email;
        }

        public static List<string> GetEmailsFromText(string text)
        {
            List<string> emails = new List<string>();
            if (!string.IsNullOrEmpty(text))
            {
                System.Text.RegularExpressions.Regex emailRegex = new System.Text.RegularExpressions.Regex(@"\w+([-+.]\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*",
                RegexOptions.IgnoreCase);
                //find items that matches with our pattern
                MatchCollection emailMatches = emailRegex.Matches(text);
                if (emailMatches != null && emailMatches.Count > 0)
                {
                    foreach (Match emailMatch in emailMatches)
                    {
                        emails.Add(emailMatch.Value);
                    }
                }
            }
            return emails;
        }

        public static bool IsEmail(string text)
        {
            return System.Text.RegularExpressions.Regex.IsMatch(text, @"\w+([-+.]\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*");
        }
    }
}
