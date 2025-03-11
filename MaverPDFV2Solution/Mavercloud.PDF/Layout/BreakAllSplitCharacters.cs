using iText.IO.Font.Otf;
using iText.Layout.Splitting;
using System;
using System.Collections.Generic;
using System.Text;

namespace Mavercloud.PDF.Layout
{
    /// <summary>
    /// The implementation of
    /// <see cref="ISplitCharacters"/>
    /// that allows breaking within words.
    /// </summary>
    public class BreakAllSplitCharacters : ISplitCharacters
    {
        public virtual bool IsSplitCharacter(GlyphLine text, int glyphPos)
        {
            if (text.Size() - 1 == glyphPos)
            {
                return true;
            }
            Glyph glyphToCheck = text.Get(glyphPos);
            if (!glyphToCheck.HasValidUnicode())
            {
                return true;
            }
            int charCode = glyphToCheck.GetUnicode();
            Glyph nextGlyph = text.Get(glyphPos + 1);
            if (!nextGlyph.HasValidUnicode())
            {
                return true;
            }
            bool nextGlyphIsLetterOrDigit = Mavercloud.PDF.Helpers.TextUtil.IsLetterOrDigit(nextGlyph);
            bool nextGlyphIsMark = Mavercloud.PDF.Helpers.TextUtil.IsMark(nextGlyph);
            bool currentGlyphIsDefaultSplitCharacter = charCode <= ' ' || charCode == '-' || charCode == '\u2010' ||
                        // block of whitespaces
                        (charCode >= 0x2002 && charCode <= 0x200b);
            return (currentGlyphIsDefaultSplitCharacter || nextGlyphIsLetterOrDigit || nextGlyphIsMark) && !iText.IO.Util.TextUtil
                .IsNonBreakingHyphen(glyphToCheck);
        }
    }
}
