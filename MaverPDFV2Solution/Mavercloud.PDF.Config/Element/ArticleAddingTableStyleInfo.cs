
using System;
using System.Collections.Generic;
using System.Text;

namespace Mavercloud.PDF.Config.Element
{
    [Serializable]
    public class ArticleAddingTableStyleInfo
    {
        public int? DrawAtPage { get; set; }

        public TableDisplayType? DisplayType { get; set; }

        public TableType? TableType { get; set; }

        public float? ImageWidth { get; set; }

        public float? TableWidth { get; set; }

        public TablePosition? Position { get; set; }

        public float? TableFontSize { get; set; }

        public float? TableMultipliedLeading { get; set; }

        public float? TableCellParagraphPaddingBottom { get; set; }

        public Dictionary<int, float> TableColumnWidths { get; set; }
    }
}
