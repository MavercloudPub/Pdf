using iText.IO.Font.Otf;
using iText.Layout.Splitting;
using System;
using System.Collections.Generic;
using System.Text;

namespace Mavercloud.PDF.Layout
{
    public class LinkSplitCharacters : ISplitCharacters
    {
        static List<char> SPLIT_CHARS = new List<char>() { '|', ' ', '-', '/', '+', '&', '=', '%', '_', '.', '@', '%' };

        static List<char> ADD_SPLIT_CHARS = new List<char>() { };
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

                var isSplit = false;

                foreach (var c in SPLIT_CHARS)
                {
                    if (charCode == c)
                    {
                        isSplit = true;
                        break;
                    }
                }

                if (!isSplit)
                {
                    isSplit = IsExplicitSplitChar(charCode);
                }
                return isSplit;

            }
            catch (Exception ex)
            {
                //ex.printStackTrace();
            }

            return true;
        }

        private bool IsExplicitSplitChar(int charCode)
        {
            var splitChar = false;
            foreach (var c in ADD_SPLIT_CHARS)
            {
                if (charCode == c)
                {
                    splitChar = true;
                    break;
                }
            }
            return splitChar;
        }

        public static void AddSplitChar(char c)
        {
            if (!ADD_SPLIT_CHARS.Contains(c))
            {
                ADD_SPLIT_CHARS.Add(c);
            }
        }
    }
}
