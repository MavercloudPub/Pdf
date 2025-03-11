using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace Mavercloud.PDF.Elements
{
    [Serializable]
    public class AxialShadingStyle
    {
        [XmlElement("StartColor")]
        public string StartColor { get; set; }

        [XmlElement("EndColor")]
        public string EndColor { get; set; }

        [XmlElement("Rotation")]
        public bool? Rotation { get; set; }
    }
}
