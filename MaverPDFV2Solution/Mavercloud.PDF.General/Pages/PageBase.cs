using iText.Layout;
using System;
using System.Collections.Generic;
using System.Text;

namespace Mavercloud.PDF.General.Pages
{
    public abstract class PageBase
    {
        protected Document document;
        public PageBase(Document document)
        {
            this.document = document;
        }

        public abstract void Create();


    }
}
