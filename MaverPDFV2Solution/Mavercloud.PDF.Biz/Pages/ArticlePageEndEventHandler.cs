using iText.Kernel.Events;
using iText.Layout;
using Mavercloud.PDF.EventHandler;
using System;
using System.Collections.Generic;
using System.Text;

namespace Mavercloud.PDF.Biz.Pages
{
    public class ArticlePageEndEventHandler : EventHandlerBase
    {
        private Document document;

        private bool enabled { get; set; }

        private BizPdfCreator pdfCreator;
        public ArticlePageEndEventHandler(
            Document document, BizPdfCreator pdfCreator)
        {
            this.document = document;
            this.pdfCreator = pdfCreator;
            this.SetEnabled(false);
        }

        public override void HandleEvent(Event @event)
        {
            if (this.enabled)
            {
                pdfCreator.pageEndEventFired = true;
            }
        }

        public void SetEnabled(bool enabled)
        {
            this.enabled = enabled;
        }
    }
}
