using iText.Kernel.Pdf.Colorspace;
using iText.Layout.Element;
using iText.Layout.Renderer;
using Mavercloud.PDF.ElementHandler;
using Mavercloud.PDF.Elements;
using System;
using System.Collections.Generic;
using System.Text;

namespace Mavercloud.PDF.Renderer
{
    public class BorderShadingTableRenderer : TableRenderer
    {
        private BorderAxialShadingStyle axialShadingStyle;

        private bool top;

        private bool bottom;
        public BorderShadingTableRenderer(Table modelElement, BorderAxialShadingStyle axialShadingStyle) : base(modelElement)
        {
            this.axialShadingStyle = axialShadingStyle;
            this.top = axialShadingStyle.Top.GetValueOrDefault();
            this.bottom = axialShadingStyle.Bottom.GetValueOrDefault();
        }

        // If renderer overflows on the next area, iText uses getNextRender() method to create a renderer for the overflow part.
        // If getNextRenderer isn't overriden, the default method will be used and thus a default rather than custom
        // renderer will be created
        public override IRenderer GetNextRenderer()
        {
            return new BorderShadingTableRenderer((Table)modelElement, axialShadingStyle);
        }

        protected override TableRenderer[] Split(int row, bool hasContent, bool cellWithBigRowspanAdded)
        {
            // Being inside this method implies that split has occurred

            TableRenderer[] results = base.Split(row, hasContent, cellWithBigRowspanAdded);

            BorderShadingTableRenderer splitRenderer = (BorderShadingTableRenderer)results[0];

            // iText shouldn't draw the bottom split renderer's border,
            // because there is an overflow renderer
            if (this.axialShadingStyle.Bottom.GetValueOrDefault())
            {
                splitRenderer.bottom = false;
            }

            // If top is true, the split renderer is the first renderer of the document.
            // If false, iText has already processed the first renderer
            splitRenderer.top = this.top;

            BorderShadingTableRenderer overflowRenderer = (BorderShadingTableRenderer)results[1];

            // iText shouldn't draw the top overflow renderer's border
            if (this.axialShadingStyle.Top.GetValueOrDefault() && hasContent)
            {
                overflowRenderer.top = false;
            }

            return results;
        }

        protected override void DrawBorders(DrawContext drawContext)
        {
            var startColor = ElementGenerator.GetColor(axialShadingStyle.AxialShading.StartColor).GetColorValue();
            var endColor = ElementGenerator.GetColor(axialShadingStyle.AxialShading.EndColor).GetColorValue();

            var rectangle = occupiedArea.GetBBox();
            var axialShading = new PdfShading.Axial(new PdfDeviceCs.Rgb(),
                rectangle.GetLeft(), 0,
                startColor,
                rectangle.GetLeft() + rectangle.GetWidth(),
                0,
                endColor);

            var canvas = drawContext.GetCanvas()
                .SaveState()
                .SetLineWidth(axialShadingStyle.Width.GetValueOrDefault(1))
                .SetStrokeColorShading(new PdfPattern.Shading(axialShading));
            if (this.bottom)
            {
                canvas.MoveTo(rectangle.GetRight(), rectangle.GetBottom() - axialShadingStyle.Margin.GetValueOrDefault(0));
                canvas.LineTo(rectangle.GetLeft(), rectangle.GetBottom() - axialShadingStyle.Margin.GetValueOrDefault(0));
            }
            if (this.top)
            {
                canvas.MoveTo(rectangle.GetRight(), rectangle.GetTop() + axialShadingStyle.Margin.GetValueOrDefault(0));
                canvas.LineTo(rectangle.GetLeft(), rectangle.GetTop() + axialShadingStyle.Margin.GetValueOrDefault(0));
            }

            //if (axialShadingStyle.Bottom.GetValueOrDefault())
            //{
            //    canvas.MoveTo(rectangle.GetLeft(), rectangle.GetBottom() - axialShadingStyle.Margin.GetValueOrDefault(0))
            //    .LineTo(rectangle.GetWidth() + rectangle.GetLeft(), rectangle.GetBottom() - axialShadingStyle.Margin.GetValueOrDefault(0));
            //}
            //if (axialShadingStyle.Top.GetValueOrDefault())
            //{
            //    canvas.MoveTo(rectangle.GetLeft(), rectangle.GetTop() + axialShadingStyle.Margin.GetValueOrDefault(0))
            //    .LineTo(rectangle.GetWidth() + rectangle.GetLeft(), rectangle.GetTop() + axialShadingStyle.Margin.GetValueOrDefault(0));
            //}

            canvas.Stroke();
            canvas.RestoreState();

        }
    }
}
