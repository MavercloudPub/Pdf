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
    public class BorderShadingParagraphRenderer : ParagraphRenderer
    {
        private BorderAxialShadingStyle axialShadingStyle;

        private bool top;

        private bool bottom;
        public BorderShadingParagraphRenderer(Paragraph modelElement,
            BorderAxialShadingStyle axialShadingStyle) : base(modelElement)
        {
            this.axialShadingStyle = axialShadingStyle;
            this.top = axialShadingStyle.Top.GetValueOrDefault();
            this.bottom = axialShadingStyle.Bottom.GetValueOrDefault();
        }

        public override IRenderer GetNextRenderer()
        {
            return new BorderShadingParagraphRenderer((Paragraph)modelElement, axialShadingStyle);
        }

        protected override ParagraphRenderer[] Split()
        {
            // Being inside this method implies that split has occurred

            ParagraphRenderer[] results = base.Split();

            BorderShadingParagraphRenderer splitRenderer = (BorderShadingParagraphRenderer)results[0];

            // iText shouldn't draw the bottom split renderer's border,
            // because there is an overflow renderer
            if (this.axialShadingStyle.Bottom.GetValueOrDefault())
            {
                splitRenderer.bottom = false;
            }

            // If top is true, the split renderer is the first renderer of the document.
            // If false, iText has already processed the first renderer
            splitRenderer.top = this.top;

            BorderShadingParagraphRenderer overflowRenderer = (BorderShadingParagraphRenderer)results[1];

            // iText shouldn't draw the top overflow renderer's border
            if (this.axialShadingStyle.Top.GetValueOrDefault())
            {
                overflowRenderer.top = false;
            }
            return results;
        }



        public override void DrawBorder(DrawContext drawContext)
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
                var selfMarginBottom = (this.GetModelElement() as Paragraph).GetMarginBottom().GetValue();
                canvas.MoveTo(rectangle.GetRight(), rectangle.GetBottom() - axialShadingStyle.Margin.GetValueOrDefault(0) + selfMarginBottom);
                canvas.LineTo(rectangle.GetLeft(), rectangle.GetBottom() - axialShadingStyle.Margin.GetValueOrDefault(0) + selfMarginBottom);
            }
            if (this.top)
            {
                var selfMarginTop = (this.GetModelElement() as Paragraph).GetMarginTop().GetValue();
                canvas.MoveTo(rectangle.GetRight(), rectangle.GetTop() + axialShadingStyle.Margin.GetValueOrDefault(0) - selfMarginTop);
                canvas.LineTo(rectangle.GetLeft(), rectangle.GetTop() + axialShadingStyle.Margin.GetValueOrDefault(0) - selfMarginTop);
            }

            canvas.Stroke();
            canvas.RestoreState();

        }
    }
}
