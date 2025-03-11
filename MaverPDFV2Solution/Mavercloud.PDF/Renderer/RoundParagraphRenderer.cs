using iText.Layout.Element;
using iText.Layout.Renderer;
using Mavercloud.PDF.Elements;
using System;
using System.Collections.Generic;
using System.Text;

namespace Mavercloud.PDF.Renderer
{
    public class RoundParagraphRenderer : ParagraphRenderer
    {
        private RoundRectangleStyle roundRectangleStyle;
        public RoundParagraphRenderer(Paragraph modelElement,
            RoundRectangleStyle roundRectangleStyle) : base(modelElement)
        {
            this.roundRectangleStyle = roundRectangleStyle;
        }

        public override void Draw(DrawContext drawContext)
        {
            drawContext.GetCanvas().RoundRectangle(GetOccupiedAreaBBox().GetX() + roundRectangleStyle.XOffset.GetValueOrDefault(), 
                GetOccupiedAreaBBox().GetY() + roundRectangleStyle.YOffset.GetValueOrDefault(),
                GetOccupiedAreaBBox().GetWidth() + roundRectangleStyle.WidthOffset.GetValueOrDefault(), 
                GetOccupiedAreaBBox().GetHeight() + roundRectangleStyle.HeightOffset.GetValueOrDefault(),
                roundRectangleStyle.Radius.GetValueOrDefault());
            drawContext.GetCanvas().Stroke();
            base.Draw(drawContext);
        }
    }
}
