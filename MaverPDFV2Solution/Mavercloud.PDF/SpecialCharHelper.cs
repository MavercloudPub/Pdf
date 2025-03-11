using Mavercloud.PDF.Elements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mavercloud.PDF
{
    public static class SpecialCharHelper
    {
        private static SpecialCharacterFontSetting fontSetting;

        private static List<ChunkTag> generalChunkTags;

        public static void Initialize(SpecialCharacterFontSetting pdfFontSetting)
        {
            fontSetting = pdfFontSetting;
            InitializeLetters();
        }
        public static void Initialize(string xml)
        {
            if (fontSetting == null)
            {
                fontSetting = Mavercloud.PDF.Helpers.Xml.XmlStrToObject<SpecialCharacterFontSetting>(xml);
                InitializeLetters();
            }
        }

        private static void InitializeLetters()
        {
            if (fontSetting.LettersList != null)
            {
                generalChunkTags = new List<ChunkTag>();
                foreach (var letters in fontSetting.LettersList)
                {
                    generalChunkTags.Add(new ChunkTag() { Name = letters.FontTag, FontName = letters.FontName, Type = ElementType.Text, FontBold = letters.Bold });
                    if (letters.LetterList != null)
                    {
                        foreach (var letter in letters.LetterList)
                        {
                            if (!string.IsNullOrEmpty(letter.Html))
                            {
                                letter.Html = "&" + letter.Html + ";";
                            }
                            if (!string.IsNullOrEmpty(letter.Hex))
                            {
                                letter.Hex = "&‌#x" + letter.Hex + ";";
                            }
                            if (!string.IsNullOrEmpty(letter.Entity))
                            {
                                letter.Entity = "&‌#" + letter.Entity + ";";
                            }
                        }
                    }
                }
            }
        }

        public static string SetFont(string text)
        {
            if (fontSetting != null)
            {
                if (fontSetting.LettersList != null)
                {
                    foreach (var letters in fontSetting.LettersList)
                    {
                        if (letters.LetterList != null)
                        {
                            var fontSettingFormat = "<" + letters.FontTag + ">{0}</" + letters.FontTag + ">";
                            foreach (var letter in letters.LetterList)
                            {

                                if (!string.IsNullOrEmpty(letter.Html) && text.Contains(letter.Html))
                                {
                                    text = text.Replace(letter.Html, string.Format(fontSettingFormat, letter.Html));
                                }

                                if (!string.IsNullOrEmpty(letter.Hex) && text.Contains(letter.Hex))
                                {
                                    text = text.Replace(letter.Hex, string.Format(fontSettingFormat, letter.Hex));
                                }

                                if (!string.IsNullOrEmpty(letter.Entity) && text.Contains(letter.Entity))
                                {
                                    text = text.Replace(letter.Entity, string.Format(fontSettingFormat, letter.Entity));
                                }

                                if (!string.IsNullOrEmpty(letter.Value) && text.Contains(letter.Value))
                                {
                                    text = text.Replace(letter.Value, string.Format(fontSettingFormat, letter.Value));
                                }
                            }
                        }
                    }
                }
            }
            return text;
        }

        public static ChunkTag GetFontTag(string tagName)
        {
            ChunkTag tag = null;
            if (generalChunkTags != null && generalChunkTags.Count > 0)
            {
                tag = generalChunkTags.FirstOrDefault(t => t.Name == tagName);
            }
            return tag;
        }

    }
}
