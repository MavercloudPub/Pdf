using iText.Layout.Element;
using Mavercloud.PDF.Config.Element;
using Mavercloud.PDF.Config.Element;
using Mavercloud.PDF.Elements;
using Mavercloud.PDF.General.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Mavercloud.PDF.General.Element
{
    public class ArticleAddingTable
    {
        /// <summary>
        /// 0 Table 1 Figure 2 Scheme
        /// </summary>
        public int Type { get; set; }

        public bool MultiImages { get; set; }

        public int Number { get; set; }

        public string Id { get; set; }

        public Div Container { get; set; }

        public Table Table { get; set; }

        public List<BlockElement<Table>> Elements { get; set; }

        public float TableHeight { get; set; }

        public bool AlignRight { get; set; }

        public int PageNumber { get; set; }

        public float? Left { get; set; }

        public float? Top { get; set; }

        public bool Prepared { get; set; }

        public bool DonotPrepare { get; set; }

        public bool Drawn { get; set; }

        public bool Rotation { get; set; }

        public TableDisplayType DisplayType { get; set; }

        public int? DrawAtPdfPage { get; set; }

        public float? Width { get; set; }

        public float? Height { get; set; }

        public ArticleAddingTableStyleInfo StyleInfo { get; set; }

        public bool? DrawBeginAtNextColumn { get; set; }

        public BodyItemBase BodyItem { get; set; }
    }
}
