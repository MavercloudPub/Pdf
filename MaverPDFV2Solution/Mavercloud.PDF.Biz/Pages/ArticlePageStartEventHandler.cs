using iText.Kernel.Events;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using iText.Layout;
using iText.Layout.Element;
using Mavercloud.PDF.ElementHandler;
using Mavercloud.PDF.Elements;
using Mavercloud.PDF.EventHandler;
using Mavercloud.PDF.Biz.Entries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mavercloud.PDF.Biz.Pages
{
    public class ArticlePageStartEventHandler : EventHandlerBase
    {
        private Document document;

        private DocumentItems layoutItems;

        private bool enabled { get; set; }

        private ElementItem pageFooterItem;

        private ElementItem firstPageLicenseItem;

        private ArticleInfo sourceData;

        private Paragraph currentParagraph;

        private BizPdfCreator pdfCreator;

        public ArticlePageStartEventHandler(
            Document document, DocumentItems layoutItems, ArticleInfo sourceData, BizPdfCreator pdfCreator)
        {
            this.document = document;
            this.layoutItems = layoutItems;
            pageFooterItem = this.layoutItems.Items.First(t => t.Name == "PageFooter");
            firstPageLicenseItem = this.layoutItems.Items.First(t => t.Name == "LicenseAndQrCode");
            this.sourceData = sourceData;
            this.pdfCreator = pdfCreator;
            this.SetEnabled(false);
        }
        public override void HandleEvent(Event @event)
        {
            if (this.enabled)
            {
                PdfDocumentEvent docEvent = (PdfDocumentEvent)@event;
                PdfDocument pdfDoc = docEvent.GetDocument();
                PdfPage page = docEvent.GetPage();
                PdfCanvas canvas = new PdfCanvas(page.NewContentStreamBefore(), page.GetResources(), pdfDoc);
                var thisPageWidth = page.GetPageSize().GetWidth();
                var pageNumber = pdfDoc.GetPageNumber(page);

                sourceData.PageText = string.Format("Page {0}", pageNumber + sourceData.StartPage.GetValueOrDefault() - 1);

                var footerElement = ElementGenerator.GetTable(sourceData, pageFooterItem, document, string.Empty);

                var newCanvas = new Canvas(canvas, new Rectangle(thisPageWidth, Constants.DocumentMargins[0]));
                newCanvas.Add(footerElement);
                if (pageNumber == 1 /*&& !sourceData.ContinuousPublish.GetValueOrDefault()*/)
                { 
                    var licenseAndQrCodeElement = ElementGenerator.GetTable(sourceData, firstPageLicenseItem, document, string.Empty);
                    newCanvas.Add(licenseAndQrCodeElement);
                }
                newCanvas.Close();
                
                canvas.Release();

                if (currentParagraph != null)
                {
                    currentParagraph.SetPaddingTop(0f);
                    currentParagraph.SetMarginTop(0f);
                    currentParagraph = null;
                }

            }
        }

        public void SetEnabled(bool enabled)
        {
            this.enabled = enabled;
        }

        public void SetCurrentParagraph(Paragraph paragraph)
        {
            this.currentParagraph = paragraph;
        }
    }
}
