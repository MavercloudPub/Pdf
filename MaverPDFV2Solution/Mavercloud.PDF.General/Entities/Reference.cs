using System;
using System.Collections.Generic;
using System.Text;

namespace Mavercloud.PDF.General.Entities
{
    [Serializable]
    public class Reference
    {
        public string OriginalId { get; set; }
        public string Id { get; set; }

        public string SymbolText { get; set; }

        public string Label { get; set; }

        public string Text { get; set; }
    }

    [Serializable]
    public class AuthorName
    {
        public string Name { get; set; }

        public string NameType { get; set; }
    }
}
