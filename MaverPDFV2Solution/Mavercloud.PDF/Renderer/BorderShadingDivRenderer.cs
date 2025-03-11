using iText.Kernel.Pdf.Colorspace;
using iText.Layout.Element;
using iText.Layout.Layout;
using iText.Layout.Renderer;
using Mavercloud.PDF.ElementHandler;
using Mavercloud.PDF.Elements;
using System;
using System.Collections.Generic;
using System.Text;

namespace Mavercloud.PDF.Renderer
{
    public class BorderShadingDivRenderer : DivRenderer
    {
        private BorderAxialShadingStyle axialShadingStyle;

        private bool top;

        private bool bottom;

        private bool isLeftOver = false;


        public BorderShadingDivRenderer(Div modelElement,
            BorderAxialShadingStyle axialShadingStyle) : base(modelElement)
        {
            this.axialShadingStyle = axialShadingStyle;
            this.top = axialShadingStyle.Top.GetValueOrDefault();
            this.bottom = axialShadingStyle.Bottom.GetValueOrDefault();
        }

        public BorderShadingDivRenderer(Div modelElement,
            BorderAxialShadingStyle axialShadingStyle, bool isLeftOver) : this(modelElement, axialShadingStyle)
        {
            this.isLeftOver = isLeftOver;
        }

        public override IRenderer GetNextRenderer()
        {
            return new BorderShadingDivRenderer((Div)modelElement, axialShadingStyle, true);
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

            if (this.bottom && this.isLastRendererForModelElement)
            {
                var selfMarginBottom = (this.GetModelElement() as Div).GetMarginBottom().GetValue();
                canvas.MoveTo(rectangle.GetRight(), rectangle.GetBottom() - axialShadingStyle.Margin.GetValueOrDefault(0) + selfMarginBottom);
                canvas.LineTo(rectangle.GetLeft(), rectangle.GetBottom() - axialShadingStyle.Margin.GetValueOrDefault(0) + selfMarginBottom);
            }
            if (this.top && !isLeftOver)
            {
                var selfMarginTop = (this.GetModelElement() as Div).GetMarginTop().GetValue();
                canvas.MoveTo(rectangle.GetRight(), rectangle.GetTop() + axialShadingStyle.Margin.GetValueOrDefault(0) - selfMarginTop);
                canvas.LineTo(rectangle.GetLeft(), rectangle.GetTop() + axialShadingStyle.Margin.GetValueOrDefault(0) - selfMarginTop);
            }

            canvas.Stroke();
            canvas.RestoreState();

        }

        public void SetIsLeftOver()
        {
            isLeftOver = true;
        }

    }
}
