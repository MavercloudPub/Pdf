using System;
using System.Collections.Generic;
using System.Text;

namespace Mavercloud.PDF.Config.Element
{
    public class ParagraphStyleInfo
    {
        public float? SpaceFontSize { get; set; }

        public float? PaddingTop { get; set; }

        public float? PaddingBottom { get; set; }

        public float? MultipliedLeading { get; set; }

        public int? HypenLeftMin { get; set; }

        public int? HypenRightMin { get; set; }

        public string Hypenation { get; set; }
    }
}
