using Mavercloud.PDF.Config.Element;
using Mavercloud.PDF.Elements;
using System;
using System.Collections.Generic;
using System.Text;

namespace Mavercloud.PDF.General.Entities
{
    [Serializable]
    public class TableInfo : BodyItemBase
    {
        public bool? Rotation { get; set; }

        public TableType? TableType { get; set; }

        public TableDisplayType? TableDisplayType { get; set; }

        public int? DrawAtPdfPage { get; set; }

        public float? Width { get; set; }

        public string Id { get; set; }

        public string IdInXml { get; set; }
        public string Label
        {
            get;
            set;
        }

        public string Caption
        {
            get;
            set;
        }

        public List<GraphicInfo> Graphics
        {
            get;set;
        }
        public string CaptionParagraphId
        {
            get; set;
        }

        public string InnerXml
        {
            get;
            set;
        }

        //public List<string> Footnotes
        //{
        //    get;
        //    set;
        //}
        public List<FootnoteInfo> Footnotes
        {
            get;
            set;
        }

    }

    [Serializable]
    public class FootnoteInfo
    {
        public string Id { get; set; }

        public string Label { get; set; }

        public string Contents { get; set; }

        public string ContentParagraphId { get; set; }
    }
}
