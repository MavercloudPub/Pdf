using iText.IO.Font.Otf;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Mavercloud.PDF.Helpers
{
    public static class TextUtil
    {
        public static bool IsLetterOrDigit(Glyph glyph)
        {
            int unicode = glyph.GetUnicode();
            UnicodeCategory category = GetUnicodeCategory(unicode);

            return category == UnicodeCategory.UppercaseLetter
                   || category == UnicodeCategory.LowercaseLetter
                   || category == UnicodeCategory.TitlecaseLetter
                   || category == UnicodeCategory.ModifierLetter
                   || category == UnicodeCategory.OtherLetter
                   || category == UnicodeCategory.DecimalDigitNumber;
        }

        public static bool IsMark(Glyph glyph)
        {
            int unicode = glyph.GetUnicode();
            UnicodeCategory category = GetUnicodeCategory(unicode);

            return category == UnicodeCategory.NonSpacingMark
                   || category == UnicodeCategory.SpacingCombiningMark
                   || category == UnicodeCategory.EnclosingMark;
        }

        private static UnicodeCategory GetUnicodeCategory(int unicodeCodePoint)
        {
            UnicodeCategory category = unicodeCodePoint <= Char.MaxValue
                ? CharUnicodeInfo.GetUnicodeCategory((char)unicodeCodePoint)
                : CharUnicodeInfo.GetUnicodeCategory(new System.String(ConvertFromUtf32(unicodeCodePoint)), 0);
            return category;
        }

        /// <summary>Converts a UTF32 code point value to a char array with the corresponding character(s).</summary>
        /// <param name="codePoint">a Unicode value</param>
        /// <returns>the corresponding char array</returns>
        public static char[] ConvertFromUtf32(int codePoint)
        {
            if (codePoint < 0x10000)
            {
                return new char[] { (char)codePoint };
            }

            codePoint -= 0x10000;
            return new char[] { (char)(codePoint / 0x400 + 0xd800), (char)(codePoint % 0x400 + 0xdc00) };
        }


    }
}
