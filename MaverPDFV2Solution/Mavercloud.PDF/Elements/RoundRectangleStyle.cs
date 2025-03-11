using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace Mavercloud.PDF.Elements
{
    [Serializable]
    public class RoundRectangleStyle
    {
        [XmlElement("XOffset")]
        public float? XOffset { get; set; }

        [XmlElement("YOffset")]
        public float? YOffset { get; set; }

        [XmlElement("WidthOffset")]
        public float? WidthOffset { get; set; }

        [XmlElement("HeightOffset")]
        public float? HeightOffset { get; set; }

        [XmlElement("Radius")]
        public float? Radius { get; set; }

        [XmlElement("BackColor")]
        public string BackColor { get; set; }

        [XmlElement("Border")]
        public BorderStyle Border { get; set; }
    }
}
