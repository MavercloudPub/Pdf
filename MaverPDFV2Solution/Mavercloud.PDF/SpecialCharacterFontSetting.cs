using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace Mavercloud.PDF
{
    [XmlRoot("SpecialCharacterFontSetting")]
    public class SpecialCharacterFontSetting
    {
        [XmlElement("Letters")]
        public List<Letters> LettersList { get; set; }
    }

    public class Letters
    {
        [XmlAttribute]
        public string FontTag { get; set; }

        [XmlAttribute]
        public string FontName { get; set; }

        [XmlAttribute]
        public bool Bold { get; set; }

        [XmlElement("Letter")]
        public List<Letter> LetterList { get; set; }
    }

    public class Letter
    {
        [XmlAttribute]
        public string Value { get; set; }

        [XmlAttribute]
        public string Entity { get; set; }

        [XmlAttribute]
        public string Hex { get; set; }

        [XmlAttribute]
        public string Html { get; set; }
    }
}
