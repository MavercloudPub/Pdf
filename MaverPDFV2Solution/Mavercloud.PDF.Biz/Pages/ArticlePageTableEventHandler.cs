using iText.Kernel.Events;
using iText.Kernel.Pdf.Canvas;
using iText.Kernel.Pdf;
using Mavercloud.PDF.EventHandler;
using System;
using System.Collections.Generic;
using System.Text;
using Mavercloud.PDF.Biz;

namespace Mavercloud.PDF.Biz.Pages
{
    public class ArticlePageTableEventHandler : EventHandlerBase
    {
        private BizPdfCreator creator;

        private bool enabled;

        public ArticlePageTableEventHandler(BizPdfCreator creator)
        {
            this.creator = creator;
        }

        public override void HandleEvent(Event @event)
        {
            if (this.enabled)
            {
                PdfDocumentEvent docEvent = (PdfDocumentEvent)@event;
                PdfDocument pdfDoc = docEvent.GetDocument();
                PdfPage page = docEvent.GetPage();
                this.enabled = false;
                creator.DrawTables();
                this.enabled = true;

            }
        }

        public void SetEnabled(bool enabled)
        {
            this.enabled = enabled;
        }

        

        
    }
}
