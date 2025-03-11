using iText.IO.Font.Otf;
using iText.Layout.Splitting;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace Mavercloud.PDF.Layout
{
    public class CustomizedCNSplitCharacters : ISplitCharacters
    {

        /**
 * An instance of the default SplitCharacter.
 */
        //public static SplitCharacter DEFAULT = new DefaultSplitCharacter();

        // line of text cannot start or end with this character
        static char u2060 = '\u2060'; // - ZERO WIDTH NO BREAK SPACE

        // a line of text cannot start with any following characters in
        // NOT_BEGIN_CHARACTERS[]
        static char u30fb = '\u30fb'; // ・ - KATAKANA MIDDLE DOT
        static char u2022 = '\u2022'; // • - BLACK SMALL CIRCLE (BULLET)
        static char uff65 = '\uff65'; // ･ - HALFWIDTH KATAKANA MIDDLE DOT
        static char u300d = '\u300d'; // 」 - RIGHT CORNER BRACKET
        static char uff09 = '\uff09'; // ） - FULLWIDTH RIGHT PARENTHESIS
        static char u0021 = '\u0021'; // ! - EXCLAMATION MARK
        static char u0025 = '\u0025'; // % - PERCENT SIGN
        static char u0029 = '\u0029'; // ) - RIGHT PARENTHESIS
        static char u002c = '\u002c'; // , - COMMA
        static char u002e = '\u002e'; // . - FULL STOP
        static char u003f = '\u003f'; // ? - QUESTION MARK
        static char u005d = '\u005d'; // ] - RIGHT SQUARE BRACKET
        static char u007d = '\u007d'; // } - RIGHT CURLY BRACKET
        static char uff61 = '\uff61'; // ｡ - HALFWIDTH IDEOGRAPHIC FULL STOP

        static char uff70 = '\uff70'; // ｰ - HALFWIDTH KATAKANA-HIRAGANA PROLONGED SOUND MARK
        static char uff9e = '\uff9e'; // ﾞ - HALFWIDTH KATAKANA VOICED SOUND MARK
        static char uff9f = '\uff9f'; // ﾟ - HALFWIDTH KATAKANA SEMI-VOICED SOUND MARK
        static char u3001 = '\u3001'; // 、 - IDEOGRAPHIC COMMA
        static char u3002 = '\u3002'; // 。 - IDEOGRAPHIC FULL STOP
        static char uff0c = '\uff0c'; // ， - FULLWIDTH COMMA
        static char uff0e = '\uff0e'; // ． - FULLWIDTH FULL STOP
        static char uff1a = '\uff1a'; // ： - FULLWIDTH COLON
        static char uff1b = '\uff1b'; // ； - FULLWIDTH SEMICOLON
        static char uff1f = '\uff1f'; // ？ - FULLWIDTH QUESTION MARK
        static char uff01 = '\uff01'; // ！ - FULLWIDTH EXCLAMATION MARK
        static char u309b = '\u309b'; // ゛ - KATAKANA-HIRAGANA VOICED SOUND MARK
        static char u309c = '\u309c'; // ゜ - KATAKANA-HIRAGANA SEMI-VOICED SOUND MARK
        static char u30fd = '\u30fd'; // ヽ - KATAKANA ITERATION MARK

        static char u2019 = '\u2019'; // ’ - RIGHT SINGLE QUOTATION MARK
        static char u201d = '\u201d'; // ” - RIGHT DOUBLE QUOTATION MARK
        static char u3015 = '\u3015'; // 〕 - RIGHT TORTOISE SHELL BRACKET
        static char uff3d = '\uff3d'; // ］ - FULLWIDTH RIGHT SQUARE BRACKET
        static char uff5d = '\uff5d'; // ｝ - FULLWIDTH RIGHT CURLY BRACKET
        static char u3009 = '\u3009'; // 〉 - RIGHT ANGLE BRACKET
        static char u300b = '\u300b'; // 》 - RIGHT DOUBLE ANGLE BRACKET
        static char u300f = '\u300f'; // 』 - RIGHT WHITE CORNER BRACKET
        static char u3011 = '\u3011'; // 】 - RIGHT BLACK LENTICULAR BRACKET
        static char u00b0 = '\u00b0'; // ° - DEGREE SIGN
        static char u2032 = '\u2032'; // ′ - PRIME
        static char u2033 = '\u2033'; // ″ - DOUBLE PRIME

        static char[] NOT_BEGIN_CHARACTERS = new char[] { u30fb, u2022, uff65, u300d, uff09, u0021, u0025, u0029,
            u002c, u002e, u003f, u005d, u007d, uff61, uff70, uff9e, uff9f, u3001, u3002, uff0c, uff0e, uff1a, uff1b,
            uff1f, uff01, u309b, u309c, u30fd, u2019, u201d, u3015, uff3d, uff5d, u3009, u300b, u300f, u3011, u00b0,
            u2032, u2033, u2060 };

        // a line of text cannot end with any following characters in
        // NOT_ENDING_CHARACTERS[]
        static char u0024 = '\u0024'; // $ - DOLLAR SIGN
        static char u0028 = '\u0028'; // ( - LEFT PARENTHESIS
        static char u005b = '\u005b'; // [ - LEFT SQUARE BRACKET
        static char u007b = '\u007b'; // { - LEFT CURLY BRACKET
        static char u00a3 = '\u00a3'; // £ - POUND SIGN
        static char u00a5 = '\u00a5'; // ¥ - YEN SIGN
        static char u201c = '\u201c'; // “ - LEFT DOUBLE QUOTATION MARK
        static char u2018 = '\u2018'; // ‘ - LEFT SINGLE QUOTATION MARK
        static char u300a = '\u300a'; // 《 - LEFT DOUBLE ANGLE BRACKET
        static char u3008 = '\u3008'; // 〈 - LEFT ANGLE BRACKET
        static char u300c = '\u300c'; // 「 - LEFT CORNER BRACKET
        static char u300e = '\u300e'; // 『 - LEFT WHITE CORNER BRACKET
        static char u3010 = '\u3010'; // 【 - LEFT BLACK LENTICULAR BRACKET
        static char u3014 = '\u3014'; // 〔 - LEFT TORTOISE SHELL BRACKET
        static char uff62 = '\uff62'; // ｢ - HALFWIDTH LEFT CORNER BRACKET
        static char uff08 = '\uff08'; // （ - FULLWIDTH LEFT PARENTHESIS
        static char uff3b = '\uff3b'; // ［ - FULLWIDTH LEFT SQUARE BRACKET
        static char uff5b = '\uff5b'; // ｛ - FULLWIDTH LEFT CURLY BRACKET
        static char uffe5 = '\uffe5'; // ￥ - FULLWIDTH YEN SIGN
        static char uff04 = '\uff04'; // ＄ - FULLWIDTH DOLLAR SIGN

        static char[] NOT_ENDING_CHARACTERS = new char[] { u0024, u0028, u005b, u007b, u00a3, u00a5, u201c, u2018,
            u3008, u300a, u300c, u300e, u3010, u3014, uff62, uff08, uff3b, uff5b, uffe5, uff04, u2060 };

        /**
         * Custom method to for SplitCharacter to handle Japanese characters. Returns
         * <CODE>true</CODE> if the character can split a line. The splitting
         * implementation is free to look ahead or look behind characters to make a
         * decision.
         *
         * @param start   the lower limit of <CODE>cc</CODE> inclusive
         * @param current the pointer to the character in <CODE>cc</CODE>
         * @param end     the upper limit of <CODE>cc</CODE> exclusive
         * @param cc      an array of characters at least <CODE>end</CODE> sized
         * @param ck      an array of <CODE>PdfChunk</CODE>. The main use is to be able
         *                to call {@link com.lowagie.text.pdf.PdfChunk#getUnicodeEquivalent(int)}. It may be
         *                <CODE>null</CODE> or shorter than <CODE>end</CODE>. If
         *                <CODE>null</CODE> no conversion takes place. If shorter than
         *                <CODE>end</CODE> the last element is used
         * @return <CODE>true</CODE> if the character(s) can split a line
         */
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

                if ((glyphPos == 0) && (charCode == '-') && (text.Size() - 1 > glyphPos) && (IsADigitChar(text, glyphPos +
                 1)))
                {
                    return false;
                }
                //char c = getCharacter(current, cc, ck);

                int next = glyphPos + 1;
                if (next < text.Size())
                {
                    int charNext = text.Get(next).GetUnicode();
                    //char charNext = getCharacter(next, cc, ck);
                    foreach (char not_begin_character in NOT_BEGIN_CHARACTERS)
                    {
                        if (charNext == not_begin_character)
                        {
                            return false;
                        }
                    }
                }

                foreach (char not_ending_character in NOT_ENDING_CHARACTERS)
                {
                    if (charCode == not_ending_character)
                    {
                        return false;
                    }
                }

                if (charCode <= ' ' || charCode == '-' || charCode == '\u2010')
                {
                    return true;
                }
                if (charCode < 0x2002)
                    return false;
                return ((charCode >= 0x2002 && charCode <= 0x200b)
                        || (charCode >= 0x2e80 && charCode < 0xd7a0)
                        || (charCode >= 0xf900 && charCode < 0xfb00)
                        || (charCode >= 0xfe30 && charCode < 0xfe50)
                        || (charCode >= 0xff61 && charCode < 0xffa0));
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
    }
}
