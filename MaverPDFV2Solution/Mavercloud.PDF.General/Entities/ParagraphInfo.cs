using System;
using System.Collections.Generic;
using System.Text;

namespace Mavercloud.PDF.General.Entities
{
    [Serializable]
    public class ParagraphInfo : BodyItemBase
    {
        public string Text
        {
            get;
            set;
        }

        public bool FirstPhraseIsTitle
        {
            get; set;
        }

        public string HorizantalAlignment
        {
            get;set;
        }

        public string Id { get; set; }

        public bool SetDestination { get; set; }

    }
}
