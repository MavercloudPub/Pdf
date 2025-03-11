using iText.Layout.Element;
using iText.Layout.Renderer;
using Mavercloud.PDF.ElementHandler;
using Mavercloud.PDF.Elements;
using System;
using System.Collections.Generic;
using System.Text;

namespace Mavercloud.PDF.Renderer
{
    public class RoundTableCellRenderer : CellRenderer
    {
        private RoundRectangleStyle roundRectangleStyle;
        public RoundTableCellRenderer(Cell modelElement,
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
            if (!string.IsNullOrEmpty(roundRectangleStyle.BackColor))
            {
                drawContext.GetCanvas()
                    .SetFillColor(ElementGenerator.GetColor(roundRectangleStyle.BackColor))
                    .Fill();
            }
            if (roundRectangleStyle.Border != null)
            {
                drawContext.GetCanvas().SetLineWidth(roundRectangleStyle.Border.Width.GetValueOrDefault());
                if (!string.IsNullOrEmpty(roundRectangleStyle.Border.Color))
                {
                    drawContext.GetCanvas().SetStrokeColor(ElementGenerator.GetColor(roundRectangleStyle.Border.Color));
                }
                drawContext.GetCanvas().Stroke();
            }
            else
            {
                drawContext.GetCanvas().SetLineWidth(0).Stroke();
            }
            base.Draw(drawContext);
        }
    }
}
