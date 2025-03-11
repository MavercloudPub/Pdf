using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using iText.Kernel.Pdf.Colorspace;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Renderer;
using Mavercloud.PDF.ElementHandler;
using Mavercloud.PDF.Elements;
using System;
using System.Collections.Generic;
using System.Text;

namespace Mavercloud.PDF.Renderer
{
    public class BackgroundShadingCellRenderer : CellRenderer
    {
        private AxialShadingStyle shadingStyle;
        public BackgroundShadingCellRenderer(Cell modelElement, AxialShadingStyle shadingStyle) : base(modelElement)
        {
            this.shadingStyle = shadingStyle;
        }

        //If renderer overflows on the next area, iText uses getNextRender() method to create a renderer for the overflow part.
        //If getNextRenderer isn't overriden, the default method will be used and thus a default rather than custom
        // renderer will be created
        public override IRenderer GetNextRenderer()
        {
            return new BackgroundShadingCellRenderer((Cell)modelElement, shadingStyle);
        }

        public override void Draw(DrawContext drawContext)
        {

            PDFHelpers.DrawBackShading(drawContext, occupiedArea.GetBBox(), shadingStyle);

            base.Draw(drawContext);
            //PdfPage currentPage = drawContext.GetDocument().GetPage(GetOccupiedArea().GetPageNumber());

            //PdfCanvas shadigCanvas = new PdfCanvas(currentPage.NewContentStreamAfter(), currentPage.GetResources(),
            //    drawContext.GetDocument());

            //var tableOccupied = this.GetOccupiedAreaBBox();

            //var startColor = ElementGenerator.GetColor(shadingStyle.StartColor).GetColorValue();
            //var endColor = ElementGenerator.GetColor(shadingStyle.EndColor).GetColorValue();
            //var axialShading = new PdfShading.Axial(new PdfDeviceCs.Rgb(), 0, 0,
            //    startColor,
            //    tableOccupied.GetWidth(),
            //    0,
            //    endColor);
            //shadigCanvas.SetFillColorShading(new PdfPattern.Shading(axialShading)).Fill();

            //new Canvas(shadigCanvas, tableOccupied)
            //    .SetFixedPosition(tableOccupied.GetLeft(), tableOccupied.GetTop(), tableOccupied.GetWidth())
            //    .Close();
        }
    }
}
