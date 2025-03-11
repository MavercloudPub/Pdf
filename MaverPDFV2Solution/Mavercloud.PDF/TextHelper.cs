using System;
using System.Collections.Generic;
using System.Text;

namespace Mavercloud.PDF
{
    public static class TextHelper
    {
        public static string InitXml(string paragraph)
        {
            var text = paragraph;
            text = text.Replace("&lt;", "***|||").Replace("&gt;", "|||***");
            text = text.Replace("&nbsp;", "^^^###");
            text = text.Replace("&#160;", "!!!~~~");

            text = text.Replace("&amp;", "&").Replace("&", "&amp;");
            text = text.Replace("***|||", "&lt;");
            text = text.Replace("|||***", "&gt;");
            text = text.Replace("^^^###", "&nbsp;");
            text = text.Replace("!!!~~~", "&#160;");
            return text;
        }
    }
}
