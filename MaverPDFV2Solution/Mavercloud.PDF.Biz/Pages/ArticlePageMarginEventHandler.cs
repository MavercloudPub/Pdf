using iText.Kernel.Events;
using iText.Kernel.Pdf;
using iText.Layout;
using Mavercloud.PDF.EventHandler;
using System;
using System.Collections.Generic;
using System.Text;
using static iText.IO.Image.Jpeg2000ImageData;

namespace Mavercloud.PDF.Biz.Pages
{
    public class ArticlePageMarginEventHandler : EventHandlerBase
    {
        private Document document;
        private bool enabled;
        private Dictionary<int, float> pageBottomMargins;

        public void SetEnabled(bool enabled)
        {
            this.enabled = enabled;
        }

        public ArticlePageMarginEventHandler(Document document, Dictionary<int, float> pageBottomMargins)
        {
            this.document = document;
            this.pageBottomMargins = pageBottomMargins;
        }

        public override void HandleEvent(Event @event)
        {
            if (enabled)
            {
                PdfDocumentEvent docEvent = (PdfDocumentEvent)@event;
                PdfDocument pdfDoc = docEvent.GetDocument();
                PdfPage page = docEvent.GetPage();
                var pageNumber = pdfDoc.GetPageNumber(page);
                if (pageNumber > 1)
                {
                    if (pageBottomMargins != null && pageBottomMargins.ContainsKey(pageNumber))
                    {
                        document.SetBottomMargin(pageBottomMargins[pageNumber]);
                    }
                    else
                    {
                        document.SetBottomMargin(Constants.DocumentMargins[2]);
                    }
                }
                
            }
        }
    }
}
