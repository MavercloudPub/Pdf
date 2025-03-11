using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace Mavercloud.PDF.Elements
{
    [Serializable]
    public class BorderStyle
    {
        [XmlElement("Type")]
        public BorderType Type { get; set; }

        [XmlElement("Width")]
        public float? Width { get; set; }

        [XmlElement("Color")]
        public string Color { get; set; }

        [XmlElement("TopDistance")]
        public float? TopDistance { get; set; }

        [XmlElement("BottomDistance")]
        public float? BottomDistance { get; set; }

        [XmlElement("Radius")]
        public float? Radius { get; set; }
    }
}
