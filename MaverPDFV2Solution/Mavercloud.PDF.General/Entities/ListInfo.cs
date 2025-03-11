using iText.Layout.Properties;
using System;
using System.Collections.Generic;
using System.Text;

namespace Mavercloud.PDF.General.Entities
{
    [Serializable]
    public class ListInfo: BodyItemBase
    {
        public ListNumberingType? ListNumberingType { get; set; }

        public List<ListItemInfo> Items { get; set; }

        public float? Indent { get; set; }
    }

    public class ListItemInfo
    { 
        public string Symbol { get; set; }

        public string Text { get; set; }
    }
}
