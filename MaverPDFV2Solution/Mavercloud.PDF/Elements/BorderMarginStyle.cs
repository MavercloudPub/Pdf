using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace Mavercloud.PDF.Elements
{
    [Serializable]
    public class BorderMarginStyle : BorderStyle
    {
        [XmlElement("Top")]
        public bool? Top { get; set; }

        [XmlElement("Bottom")]
        public bool? Bottom { get; set; }

        [XmlElement("Margin")]
        public float? Margin { get; set; }

        [XmlElement("Left")]
        public bool? Left { get; set; }

        [XmlElement("Right")]
        public bool? Right { get; set; }
    }
}
