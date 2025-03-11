using iText.Layout.Properties;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace Mavercloud.PDF.Elements
{
    [Serializable]
    public class ElementPosition
    {
        [XmlElement("Left")]
        public float? Left { get; set; }

        [XmlElement("Right")]
        public float? Right { get; set; }

        [XmlElement("Bottom")]
        public float? Bottom { get; set; }

        [XmlElement("Top")]
        public float? Top { get; set; }

        [XmlElement("HorizontalAlignment")]
        public HorizontalAlignment? HorizontalAlignment { get; set; }

        public float? Width { get; set; }
    }
}
