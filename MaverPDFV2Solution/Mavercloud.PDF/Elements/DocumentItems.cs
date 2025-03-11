using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace Mavercloud.PDF.Elements
{
    [XmlRoot("DocumentStyle")]
    public class DocumentItems
    {
        [XmlElement("Item")]
        public List<ElementItem> Items { get; set; }
    }
}
