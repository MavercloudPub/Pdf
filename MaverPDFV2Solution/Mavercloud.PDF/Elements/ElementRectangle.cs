using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace Mavercloud.PDF.Elements
{
    [Serializable]
    public class ElementRectangle
    {
        [XmlElement("Left")]
        public float? Left { get; set; }

        [XmlElement("Bottom")]
        public float? Bottom { get; set; }

        [XmlElement("Width")]
        public float? Width { get; set; }

        [XmlElement("Height")]
        public float? Height { get; set; }
    }
}
