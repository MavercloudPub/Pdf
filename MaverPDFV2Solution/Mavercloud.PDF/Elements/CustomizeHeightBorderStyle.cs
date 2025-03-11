using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace Mavercloud.PDF.Elements
{
    [Serializable]
    public class CustomizeHeightBorderStyle
    {
        [XmlElement("BorderRight")]
        public BorderStyle BorderRight { get; set; }

        [XmlElement("BorderTop")]
        public BorderStyle BorderTop { get; set; }

        [XmlElement("BorderBottom")]
        public BorderStyle BorderBottom { get; set; }

        [XmlElement("BorderLeft")]
        public BorderStyle BorderLeft { get; set; }
    }
}
