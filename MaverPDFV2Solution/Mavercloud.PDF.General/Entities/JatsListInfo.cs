using System;
using System.Collections.Generic;
using System.Text;

namespace Mavercloud.PDF.General.Entities
{
    public class JatsListInfo: BodyItemBase
    {
        public string Id { get; set; }

        public string ListType { get; set; }
        public List<JatsListItemInfo> Items { get; set; }
    }

    public class JatsListItemInfo
    { 
        public string Symbol { get; set; }
        public List<BodyItemBase> Elements { get; set; }
    }
}
