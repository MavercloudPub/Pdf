using iText.Kernel.Events;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using Mavercloud.PDF.ElementHandler;
using Mavercloud.PDF.Elements;
using Mavercloud.PDF.EventHandler;
using System;
using System.Collections.Generic;
using System.Text;

namespace Mavercloud.PDF.General.Pages
{
    public class WaterMarkEventHandler : EventHandlerBase
    {
        private Document document;
        private string content;
        private ElementItem styleItem;
        private bool enabled;

        public void SetEnabled(bool enabled)
        {
            this.enabled = enabled;
        }

        public WaterMarkEventHandler(Document document, string content, ElementItem styleItem)
        {
            this.document = document;
            this.content = content;
            this.styleItem = styleItem;
            this.styleItem.ConstantValue = content;
        }
        public override void HandleEvent(Event @event)
        {
            if (enabled)
            {
                PdfDocumentEvent docEvent = (PdfDocumentEvent)@event;
                var pdfDoc = docEvent.GetDocument();
                PdfPage page = docEvent.GetPage();

                int pageNumber = pdfDoc.GetPageNumber(page);
                var pageSize = page.GetPageSize();

                float x = (pageSize.GetLeft() + pageSize.GetRight()) / 2;
                float y = (pageSize.GetTop() + pageSize.GetBottom()) / 2;

                var paragraph = ElementGenerator.GetBlockElement(content, this.styleItem, document, string.Empty) as Paragraph;

                var pdfCanvas = new PdfCanvas(page.NewContentStreamAfter(), page.GetResources(), pdfDoc);

                new Canvas(pdfCanvas, pageSize).ShowTextAligned(paragraph, x, y, pageNumber, TextAlignment.CENTER, VerticalAlignment.TOP, -0.2f * (float)Math.PI);
            }
        }
    }
}
