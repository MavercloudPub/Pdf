using System;
using System.Collections.Generic;
using System.Text;

namespace Mavercloud.PDF.General.Entities
{
    [Serializable]
    public class SectionInfo : BodyItemBase
    {
        public string Id { get; set; }

        public string Title
        {
            get;
            set;
        }

        public string TitleId { get; set; }

        public List<BodyItemBase> Items
        {
            get;
            set;
        }

        public int Level
        {
            get; set;
        }
    }
}
