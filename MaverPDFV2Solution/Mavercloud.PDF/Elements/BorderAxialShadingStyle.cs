using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace Mavercloud.PDF.Elements
{
    [Serializable]
    public class BorderAxialShadingStyle
    {
        [XmlElement("AxialShading")]
        public AxialShadingStyle AxialShading { get; set; }

        [XmlElement("Top")]
        public bool? Top { get; set; }

        [XmlElement("Bottom")]
        public bool? Bottom { get; set; }

        [XmlElement("Margin")]
        public float? Margin { get; set; }

        [XmlElement("Width")]
        public float? Width { get; set; }
    }
}
