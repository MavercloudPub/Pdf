using iText.Layout;
using iText.Layout.Layout;
using iText.Layout.Renderer;
using System;
using System.Collections.Generic;
using System.Text;

namespace Mavercloud.PDF.Renderer
{
    public class CustomLayoutDocumentRenderer : DocumentRenderer
    {
        public CustomLayoutDocumentRenderer(Document document, bool immediateFlush, RootLayoutArea currentArea) : base(document, immediateFlush)
        {
            this.currentArea = new RootLayoutArea(currentArea.GetPageNumber(), currentArea.GetBBox().Clone());
            //this.currentPageNumber = this.currentArea.GetPageNumber();
        }

        public void SetImmediateFlush(bool immediateFlush)
        {
            this.immediateFlush = immediateFlush;
        }
    }
}
