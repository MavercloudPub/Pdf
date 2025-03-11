using iText.IO.Font.Otf;
using iText.Layout.Splitting;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace Mavercloud.PDF.Layout
{
    public class CustomizedSplitCharacters : ISplitCharacters
    {
        static List<char> NOT_BEGIN_CHARACTERS = new List<char> { ')', '!', ' ', '%', ',', '.', '@', '#', '$', '^', '&', '\\', '*', '?', ':', '"', ']', '\'', '}', ';', '>' };

        static List<char> NOT_ENDING_CHARACTERS = new List<char> { '(', '{', '[', '<' };

        static List<char> NOT_SPLIT_CHARS = new List<char>() { };
        public bool IsSplitCharacter(GlyphLine text, int glyphPos)
        {
            // Note: If you don't add an try/catch and there is an issue with
            // isSplitCharacter(), iText silently fails and
            // you have no idea there was a problem.
            try
            {

                if (!text.Get(glyphPos).HasValidUnicode())
                {
                    return false;
                }

                int charCode = text.Get(glyphPos).GetUnicode();
                if (charCode == '(')
                { 
                    
                }

                var isSplit = (charCode == '|' || charCode == ' ' || charCode == '-' || charCode == '/' || charCode == '—') 
                    && !IsExplicitNotSplitChar(charCode);
                return isSplit;

            }
            catch (Exception ex)
            {
                //ex.printStackTrace();
            }

            return true;
        }

        private bool IsADigitChar(GlyphLine text, int glyphPos)
        {
            return char.IsDigit(text.Get(glyphPos).GetChars()[0]);
        }

        private bool IsExplicitNotSplitChar(int charCode)
        {
            var notSplitChar = false;
            foreach (var c in NOT_SPLIT_CHARS)
            {
                if (charCode == c)
                {
                    notSplitChar = true;
                    break;
                }
            }
            return notSplitChar;
        }

        public static void AddNotSplitChar(char c)
        {
            if (!NOT_SPLIT_CHARS.Contains(c))
            {
                NOT_SPLIT_CHARS.Add(c);
            }
        }
    }
}
