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
    public class BackgroundShadingParagraphRenderer : ParagraphRenderer
    {
        private AxialShadingStyle shadingStyle;
        public BackgroundShadingParagraphRenderer(Paragraph modelElement, AxialShadingStyle shadingStyle) : base(modelElement)
        {
            this.shadingStyle = shadingStyle;
        }

        //If renderer overflows on the next area, iText uses getNextRender() method to create a renderer for the overflow part.
        //If getNextRenderer isn't overriden, the default method will be used and thus a default rather than custom
        // renderer will be created
        public override IRenderer GetNextRenderer()
        {
            return new BackgroundShadingParagraphRenderer((Paragraph)modelElement, shadingStyle);
        }

        public override void Draw(DrawContext drawContext)
        {
            var marginTop = 0f;
            var marginBottom = 0f;

            var marginTopUnitValue = ((Paragraph)modelElement).GetMarginTop();
            var marginBottomUnitValue = ((Paragraph)modelElement).GetMarginBottom();
            if (marginTopUnitValue != null)
            {
                marginTop = marginTopUnitValue.GetValue();
            }
            if (marginBottomUnitValue != null)
            {
                marginBottom = marginBottomUnitValue.GetValue();
            }

            var realRectangle = occupiedArea.GetBBox().Clone();
            realRectangle.SetHeight(realRectangle.GetHeight() - marginTop - marginBottom);
            realRectangle.SetY(realRectangle.GetBottom() + marginBottom);

            PDFHelpers.DrawBackShading(drawContext, realRectangle, shadingStyle);
            base.Draw(drawContext);
        }
    }
}
